using Microsoft;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpNamespacer
{
    public class AsyncModuleSuggestedActionSource : IAsyncSuggestedActionsSource
    {
        private ModuleSuggestedActionSourceProvider _moduleSuggestedActionSourceProvider;
        private ITextBuffer _textBuffer;

        public AsyncModuleSuggestedActionSource( ModuleSuggestedActionSourceProvider moduleSuggestedActionSourceProvider, 
                                                 ITextBuffer textBuffer)
        {
            _moduleSuggestedActionSourceProvider = moduleSuggestedActionSourceProvider;
            _textBuffer = textBuffer;
        }

#pragma warning disable CS0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore CS0067

        public Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync( ISuggestedActionCategorySet requestedActionCategories, 
                                                                                    SnapshotSpan range, 
                                                                                    CancellationToken cancellationToken)
        {
            return Task.FromResult(requestedActionCategories);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions( ISuggestedActionCategorySet requestedActionCategories, 
                                                                    SnapshotSpan range, 
                                                                    CancellationToken cancellationToken
        ) {
            return null;
        }

        public async Task GetSuggestedActionsAsync( ISuggestedActionCategorySet requestedActionCategories, 
                                              SnapshotSpan range, 
                                              ImmutableArray<ISuggestedActionSetCollector> suggestedActionSetCollectors, 
                                              CancellationToken cancellationToken)
        {
            var (res, fsModule) = await CanModifyModuleNameAsync(range);

            if (res) {

                ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range, SpanTrackingMode.EdgeInclusive);

                var changeFsModuleScopeAction = new ChangeFsModuleScopeAction(trackingSpan, fsModule);

                if (fsModule is FsSuggested fsSuggested) {
                    var changeFsModuleNameAction = new ChangeFsModuleNameAction(trackingSpan, fsSuggested);
                    suggestedActionSetCollectors[0]
                        .Add(new SuggestedActionSet(
                            "F# Category", 
                            new ISuggestedAction[] { changeFsModuleNameAction, changeFsModuleScopeAction }, 
                            "F# Title"));

                }
                else if (fsModule is FsModuleBase) {
                    suggestedActionSetCollectors[0]
                        .Add(new SuggestedActionSet(
                            "F# Category", 
                            new ISuggestedAction[] { changeFsModuleScopeAction },
                            "F# Title") );
                }

                //suggestedActionSetCollectors[0].Complete();
            }
        }


        public async Task<bool> HasSuggestedActionsAsync( ISuggestedActionCategorySet requestedActionCategories, 
                                                          SnapshotSpan range, 
                                                          CancellationToken cancellationToken
        ) {
            var (res, _) = await CanModifyModuleNameAsync(range);
            return res;
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
        }


        private async Task<(bool, IFsModule)> CanModifyModuleNameAsync(SnapshotSpan range)
        {
            if ( TryGetTextDocument(out ITextDocument doc)
                 && Path.GetExtension(doc.FilePath) == ".fs"
                 && CheckCaretLine(range, out var f) ) 
            {
                var fsModule = f;

                var projectFileName = await GetProjectFileNameAsync();
                Assumes.False(string.IsNullOrWhiteSpace(projectFileName));


                string[] filePathUriSegments = new Uri(doc.FilePath, UriKind.Absolute).Segments; // include .fs file
                string[] projectPathUriSegments = new Uri(projectFileName, UriKind.Absolute).Segments; // include .fsproj file

                if (projectPathUriSegments.Length > filePathUriSegments.Length) {
                    return (false, null);
                }


                int i = 0;

                while (projectPathUriSegments[i] == filePathUriSegments[i]) ++i;

                var suggestedFsModuleNameSegments =
                    new[] { Path.GetFileNameWithoutExtension(projectFileName) }
                    .Concat(filePathUriSegments.Skip(i))
                    .Select(s => s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar))
                    .ToArray();


                suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1] =
                    suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1]
                        .Substring(0, suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1].Length - 3);

                fsModule.SuggestedFsModuleName = suggestedFsModuleNameSegments;

                if (fsModule.IsModule && fsModule.FsModuleName.SequenceEqual(suggestedFsModuleNameSegments)) {
                    return (true, fsModule); // TODO: to change module with namespace or backward
                }

                if (!fsModule.IsModule && fsModule.FsModuleName.SequenceEqual(suggestedFsModuleNameSegments.Take(suggestedFsModuleNameSegments.Length - 1))) {
                    return (true, fsModule); // TODO: to change module with namespace or backward
                }

                fsModule = new FsSuggested {
                    IsModule = fsModule.IsModule,
                    Extend = fsModule.Extend,
                    Ind = fsModule.Ind,
                    FsModuleName = fsModule.FsModuleName,
                    SuggestedFsModuleName = suggestedFsModuleNameSegments,
                };

                return (true, fsModule);
            }

            return (false, null);
        }


        private bool TryGetTextDocument(out ITextDocument doc)
            => _moduleSuggestedActionSourceProvider.TextDocumentFactoryService.TryGetTextDocument(_textBuffer, out doc);

        private async Task<string> GetProjectFileNameAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return _moduleSuggestedActionSourceProvider.Dte.ActiveDocument?.ProjectItem?.ContainingProject.FileName;
        }

        private bool CheckCaretLine(SnapshotSpan range, out IFsModule fsModule)
        {
            fsModule = default;

            if (range.IsEmpty) {
                return false;
            }


            string line = range.GetText();
            bool isModule = line.StartsWith("module"); // ignore trailing spaces
            bool isNamespace = line.StartsWith("namespace"); // ignore trailing spaces


            if (!(isModule || isNamespace) || line.Contains("=")) {
                return false;
            }

            // define module name start position
            int nameStartIndex =
                isModule
                    ? "module".Length
                    : "namespace".Length;

            while (Char.IsWhiteSpace(line[nameStartIndex++])) ;
            --nameStartIndex;

            // define name segments
            string[] nameSegments =
                line.Substring(nameStartIndex).Split('.').Select(s => s.Trim()).Where(s => !String.IsNullOrEmpty(s)).ToArray();

            fsModule =
                new FsModuleBase {
                    IsModule = isModule,
                    Extend = range,
                    Ind = nameStartIndex,
                    FsModuleName = nameSegments
                };
            return true;
        }


        internal interface IFsModule
        {
            bool IsModule { get; }
            SnapshotSpan Extend { get; }
            int Ind { get; }
            string[] FsModuleName { get; }
            string[] SuggestedFsModuleName { get; set; }
        }


        internal class FsModuleBase : IFsModule
        {
            public bool IsModule { get; internal set; }
            public SnapshotSpan Extend { get; internal set; }
            public int Ind { get; internal set; }
            public string[] FsModuleName { get; internal set; }
            public string[] SuggestedFsModuleName { get; set; }
        }

        internal class FsSuggested : FsModuleBase
        {
        }

        private enum Suggestion
        {
            No,
            CreateModuleOrNamespace,
            EditModuleNamespace,
        }
    }
}
