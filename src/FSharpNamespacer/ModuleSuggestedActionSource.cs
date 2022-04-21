using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpNamespacer
{
    internal class ModuleSuggestedActionSource : ISuggestedActionsSource
    {
        private ModuleSuggestedActionSourceProvider _moduleSuggestedActionSourceProvider;
        private ITextView _textView;
        private ITextBuffer _textBuffer;
        private readonly string _projectFileName;

        public ModuleSuggestedActionSource(ModuleSuggestedActionSourceProvider moduleSuggestedActionSourceProvider, ITextView textView, ITextBuffer textBuffer, string projectFileName)
        {
            _moduleSuggestedActionSourceProvider = moduleSuggestedActionSourceProvider;
            _textView = textView;
            _textBuffer = textBuffer;
            _projectFileName = projectFileName;
        }

#pragma warning disable CS0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore CS0067


        public void Dispose()
        { }


        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            if (CanModifyModuleName(out var fsModule))
            {
                ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range, SpanTrackingMode.EdgeInclusive);

                var changeFsModuleScopeAction = new ChangeFsModuleScopeAction(trackingSpan, fsModule);

                if (fsModule is FsSuggested fsSuggested)
                {
                    var changeFsModuleNameAction = new ChangeFsModuleNameAction(trackingSpan, fsSuggested);
                    return new SuggestedActionSet[] { new SuggestedActionSet("F# Category", new ISuggestedAction[] { changeFsModuleNameAction, changeFsModuleScopeAction }, "F# Title") };
                }

                if (fsModule is FsModuleBase fsModuleBase)
                {
                    return new SuggestedActionSet[] { new SuggestedActionSet("F# Category", new ISuggestedAction[] { changeFsModuleScopeAction }, "F# Title") };
                }
            }
            return null;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Run(() => CanModifyModuleName(out _));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }

        private bool CanModifyModuleName(out IFsModule fsModule)
        {
            if ( _moduleSuggestedActionSourceProvider.TextDocumentFactoryService.TryGetTextDocument(_textBuffer, out ITextDocument doc) 
                 && CheckCaretLine(out var f) && Path.GetExtension(doc.FilePath) == ".fs" )
            {
                fsModule = f;

                string[] filePathUriSegments = new Uri(doc.FilePath, UriKind.Absolute).Segments; // include .fs file
                string[] projectPathUriSegments = new Uri(_projectFileName, UriKind.Absolute).Segments; // include .fsproj file

                if (projectPathUriSegments.Length > filePathUriSegments.Length)
                {
                    return false;
                }


                int i = 0;

                while (projectPathUriSegments[i] == filePathUriSegments[i]) ++i;

                var suggestedFsModuleNameSegments =
                    new [] { Path.GetFileNameWithoutExtension(_projectFileName)}
                    .Concat( filePathUriSegments.Skip(i) )
                    .Select(s => s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar))
                    .ToArray();


                suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1] = 
                    suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1]
                        .Substring(0, suggestedFsModuleNameSegments[suggestedFsModuleNameSegments.Length - 1].Length - 3);

                fsModule.SuggestedFsModuleName = suggestedFsModuleNameSegments;

                if ( fsModule.IsModule && fsModule.FsModuleName.SequenceEqual(suggestedFsModuleNameSegments) )
                {
                    return true; // TODO: to change module with namespace or backward
                }

                if ( !fsModule.IsModule && fsModule.FsModuleName.SequenceEqual(suggestedFsModuleNameSegments.Take(suggestedFsModuleNameSegments.Length - 1)) )
                {
                    return true; // TODO: to change module with namespace or backward
                }

                fsModule = new FsSuggested
                {
                    Extend = fsModule.Extend,
                    Ind = fsModule.Ind,
                    FsModuleName = fsModule.FsModuleName,
                    SuggestedFsModuleName = suggestedFsModuleNameSegments,
                };

                return true;
            }

            fsModule = default;
            return false;
        }


        private bool CheckCaretLine(out IFsModule fsModule)
        {
            fsModule = default;

            ITextCaret caret = _textView.Caret;
            var carretLineExtent = caret.ContainingTextViewLine.Extent;
            
            if (carretLineExtent.IsEmpty)
            {
                return false;
            }


            string line = carretLineExtent.GetText();
            bool isModule = line.StartsWith("module"); // ignore trailing spaces
            bool isNamespace = line.StartsWith("namespace"); // ignore trailing spaces


            if ( !(isModule || isNamespace) || line.Contains("=") )
            {
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
                new FsModuleBase
                {
                    IsModule = isModule,
                    Extend = carretLineExtent,
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