using FSharpNamespacer.Actions;
using FSharpNamespacer.Models;
using Microsoft;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpNamespacer
{
    public sealed class AsyncModuleSuggestedActionSource : IAsyncSuggestedActionsSource
    {
        private ModuleSuggestedActionSourceProvider _moduleSuggestedActionSourceProvider;
        private ITextBuffer _textBuffer;

        public AsyncModuleSuggestedActionSource(ModuleSuggestedActionSourceProvider moduleSuggestedActionSourceProvider,
                                                 ITextBuffer textBuffer)
        {
            _moduleSuggestedActionSourceProvider = moduleSuggestedActionSourceProvider;
            _textBuffer = textBuffer;
        }

#pragma warning disable CS0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore CS0067

        public Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(ISuggestedActionCategorySet requestedActionCategories,
                                                                                    SnapshotSpan range,
                                                                                    CancellationToken cancellationToken)
        {
            return Task.FromResult(requestedActionCategories);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories,
                                                                    SnapshotSpan range,
                                                                    CancellationToken cancellationToken
        )
        {
            return null;
        }

        public async Task GetSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
                                              SnapshotSpan range,
                                              ImmutableArray<ISuggestedActionSetCollector> suggestedActionSetCollectors,
                                              CancellationToken cancellationToken)
        {
            (bool canAddAction, FsScope fsScope) = await CanModifyModuleNameAsync(range);

            if (canAddAction)
            {
                void AddActions(string categoryName, params ISuggestedAction[] actions)
                =>
                    suggestedActionSetCollectors[0]
                        .Add(new SuggestedActionSet(
                            categoryName,
                            actions,
                            categoryName));


                ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range, SpanTrackingMode.EdgeInclusive);

                List<ISuggestedAction> moduleSuggestedActions = new List<ISuggestedAction>(4);
                List<ISuggestedAction> namespaceSuggestedActions = new List<ISuggestedAction>(4);

                void TryAddAction( List<ISuggestedAction> suggestedActionList, 
                                   Func<ISuggestedAction> suggestedAction, 
                                   Predicate<FsScope> predicate
                ) {
                    if (predicate(fsScope))
                    {
                        suggestedActionList.Add(suggestedAction());
                    }
                }

                // -----------------
                // module actions
                // -----------------
                TryAddAction(
                    moduleSuggestedActions,
                    () => new ChangeToModuleAction(trackingSpan, fsScope),
                    fs => fs.IsNamespaceScope && fs.IsNameNotEqualToSuggestedModuleName && fs.IsNameNotEqualToSuggestedNamespaceName);

                TryAddAction(
                    moduleSuggestedActions,
                    () => new ChangeToSuggestedModuleAction(trackingSpan, fsScope),
                    fs => fs.IsNotModuleScope || fs.IsNameNotEqualToSuggestedModuleName || (fs.IsModuleScope && fs.IsNameNotEqualToSuggestedModuleName));

                TryAddAction(
                    moduleSuggestedActions,
                    () => new ChangeToSuggestedModuleInsteadNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsNotModuleScope || fs.IsNameNotEqualToSuggestedNamespaceName || (fs.IsModuleScope && fs.IsNameNotEqualToSuggestedNamespaceName));

                // -----------------
                // namespace actions
                // -----------------
                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsModuleScope && fs.IsNameNotEqualToSuggestedModuleName && fs.IsNameNotEqualToSuggestedNamespaceName);

                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToSuggestedNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsNotNamespaceScope || fs.IsNameNotEqualToSuggestedNamespaceName || (fs.IsNamespaceScope && fs.IsNameNotEqualToSuggestedNamespaceName));

                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToSuggestedNamespaceInsteadModuleAction(trackingSpan, fsScope),
                    fs => fs.IsNotNamespaceScope || fs.IsNameNotEqualToSuggestedModuleName || fs.IsNamespaceScope && fs.IsNameNotEqualToSuggestedModuleName);

                AddActions("F# Suggested Module Names", moduleSuggestedActions.ToArray());
                AddActions("F# Suggested Namespace Names", namespaceSuggestedActions.ToArray());
            }
        }


        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
                                                          SnapshotSpan range,
                                                          CancellationToken cancellationToken
        )
        {
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

        private async Task<(bool canModifyModuleName, FsScope fsScope)> CanModifyModuleNameAsync(SnapshotSpan range)
        {
            if (TryGetTextDocument(out ITextDocument doc)
                 && Path.GetExtension(doc.FilePath) == ".fs"
                 && FsScope.TryCreate(range, out FsScope fsScope))
            {
                var projectFileName = await GetProjectFileNameAsync();
                Assumes.False(string.IsNullOrWhiteSpace(projectFileName));

                if (fsScope.TrySetSuggestedFsModuleName(projectFileName, doc.FilePath))
                {
                    return (true, fsScope);
                }
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
    }
}
