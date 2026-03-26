using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSharpNamespacer.Actions;
using FSharpNamespacer.Models;
using Microsoft;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.RpcContracts.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

#nullable enable

namespace FSharpNamespacer.ModuleSuggestedActionSourceProvider
{
    internal sealed partial class ModuleSuggestedActionSourceProvider
    {
        private sealed partial class AsyncSuggestedActionSource : ISuggestedActionsSource
        {
            private readonly ModuleSuggestedActionSourceProvider _provider;
            private readonly ITextView _textView;
            private readonly ITextDocument _textDocument;
            private readonly ITextBuffer _textBuffer;
            private readonly string? _projectFileName;

            private SuggestedActionsBuilder? _lastBuilder;

            public AsyncSuggestedActionSource(
                ModuleSuggestedActionSourceProvider provider,
                ITextView textView,
                ITextBuffer textBuffer,
                ITextDocument textDocument,
                string? projectFileName)
            {
                _provider = provider;
                _textView = textView;
                _textBuffer = textBuffer;
                _textDocument = textDocument;
                _projectFileName = projectFileName;
            }

#pragma warning disable CS0067 // is never used
            public event EventHandler<EventArgs>? SuggestedActionsChanged;
#pragma warning restore CS0067 // is never used

            public void Dispose()
            {
            }

            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = default;
                return false;
            }

            // Using in ISuggestedActionsSource2 and ISuggestedActionsSource3
            public async Task<ISuggestedActionCategorySet?> GetSuggestedActionCategoriesAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken
            )
            {
                await SwitchToTaskPoolAsync(cancellationToken);
                await WaitToScrollAsync(cancellationToken);

                var builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return null;
                }

                // AllRefactorings - screw is shown
                // AllCodeFixesAndRefactorings - light bulb is shown
                return _provider.SuggestedActionCategoryRegistryService.AllCodeFixesAndRefactorings;
            }

            public async Task<bool> HasSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken
            )
            {
                await SwitchToTaskPoolAsync(cancellationToken);
                await WaitToScrollAsync(cancellationToken);

                if (!requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring))
                {
                    return false;
                }

                var builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return false;
                }

                return true;
            }

            // Using in IAsyncSuggestedActionsSource
            public async Task GetSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                ImmutableArray<ISuggestedActionSetCollector> suggestedActionSetCollectors,
                CancellationToken cancellationToken
            )
            {
                await SwitchToTaskPoolAsync(cancellationToken);

                if (!requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring))
                {
                    return;
                }

                var builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return;
                }

                return;
            }

            // Using in ISuggestedActionsSource3
            public IEnumerable<SuggestedActionSet> GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext operationContext)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken
            )
            {
                if (!requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring))
                {
                    return Enumerable.Empty<SuggestedActionSet>();
                }

                var builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return Enumerable.Empty<SuggestedActionSet>();
                }

                return builder.GetSuggestedActionSets(
                    _textBuffer,
                    range
                    , _textDocument.FilePath,
                    _projectFileName);
            }

            private async Task SwitchToTaskPoolAsync(CancellationToken cancellationToken)
            {
                // Calls on every action in file like cursor moving or cursor position changing, typing, etc.
                // If cursor is moving along same line method will be invoked only first time (when cursor moving on this line).

                // Make sure we're explicitly on the background, to do as much as possible in a non-blocking fashion.
                await TaskScheduler.Default;
                cancellationToken.ThrowIfCancellationRequested();
            }

            private async Task WaitToScrollAsync(CancellationToken cancellationToken)
            {
                // This function gets called immediately after operations like scrolling.  We want to wait just a small
                // amount to ensure that we don't immediately start consuming CPU/memory which then impedes the very
                // action the user is trying to perform.  To accomplish this, we wait 100ms.  That's longer than normal
                // keyboard repeat rates (usually around 30ms), but short enough that it's not noticeable to the user.
                await Task.Delay(100, cancellationToken).NoThrowAwaitable();
            }

            private SuggestedActionsBuilder GetSuggestedActionsBuilder(
                SnapshotSpan range,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Volatile.Write(ref _lastBuilder, null);
                    return SuggestedActionsBuilder.CreateNone(range);
                }

                // Make sure the range is from the same buffer that this source was created for.
                Requires.Argument(
                    range.Snapshot.TextBuffer.Equals(_textBuffer),
                    nameof(range),
                    $"Invalid text buffer passed to {nameof(HasSuggestedActionsAsync)}");

                var lastBuilder = Volatile.Read(ref _lastBuilder);

                if (lastBuilder != null && lastBuilder.CorrespondsTo(range))
                {
                    return lastBuilder;
                }

                var builder = SuggestedActionsBuilder.Create(this, range);
                Volatile.Write(ref _lastBuilder, builder);

                return builder;
            }
        }
    }

    /*
        public sealed class AsyncModuleSuggestedActionSource : IAsyncSuggestedActionsSource
    {
        private readonly ModuleSuggestedActionSourceProvider _provider;
        private readonly ITextView _textView;
        private readonly ITextDocument _textDocument;
        private readonly ITextBuffer _textBuffer;
        private readonly string? _projectFileName;

        public AsyncModuleSuggestedActionSource(
            ModuleSuggestedActionSourceProvider provider,
            ITextView textView,
            ITextBuffer textBuffer,
            ITextDocument textDocument,
            string? projectFileName)
        {
            _provider = provider;
            _textView = textView;
            _textBuffer = textBuffer;
            _textDocument = textDocument;
            _projectFileName = projectFileName;
        }

        private void T_ClassificationChanged(object sender, ClassificationChangedEventArgs e)
        {
            //throw new NotImplementedException();
            ;
        }

        public event EventHandler<EventArgs>? SuggestedActionsChanged;

        private const string MODULE_WORD = "module";
        private const int MODULE_WORD_LENGTH = 6;

        private const string NAMESPACE_WORD = "namespace";
        private const int NAMESPACE_WORD_LENGTH = 9;

        private int _version = -1;
        private FsFileScopeBuilderState _fsFileScopeBuilderState = new FsFileScopeBuilderState.None();

        private const string NAMESPACE_REFACTORING_CATEGORY_NAME = "FSHARP_NAMESPACE_REFACTORING";
        private const string MODULE_REFACTORING_CATEGORY_NAME = "FSHARP_MODULE_REFACTORING";

        

        public async Task GetSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            ImmutableArray<ISuggestedActionSetCollector> suggestedActionSetCollectors,
            CancellationToken cancellationToken)
        {
            (bool canAddAction, FsFileRootScope fsScope) = await CanModifyModuleNameAsync(range);

            if (canAddAction)
            {
                void AddActions(string categoryName, params ISuggestedAction[] actions)
                =>
                    suggestedActionSetCollectors[0]
                        .Add(new SuggestedActionSet(
                            categoryName,
                            actions,
                            categoryName));

                ITrackingSpan trackingSpan = fsScope.TextSnapshotLine.Snapshot.CreateTrackingSpan(
                    fsScope.TextSnapshotLine.Start,
                    fsScope.TextSnapshotLine.End,
                    SpanTrackingMode.EdgeInclusive);

                List<FsScopeActionBase> moduleSuggestedActions = new List<FsScopeActionBase>(4);
                List<FsScopeActionBase> namespaceSuggestedActions = new List<FsScopeActionBase>(4);

                void TryAddAction( List<FsScopeActionBase> suggestedActionList, 
                                   Func<FsScopeActionBase> suggestedAction, 
                                   Predicate<FsFileRootScope> predicate
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
                    fs => fs.IsNotModuleScope || fs.IsNameNotEqualToSuggestedModuleName || (fs.IsFsModule && fs.IsNameNotEqualToSuggestedModuleName));

                TryAddAction(
                    moduleSuggestedActions,
                    () => new ChangeToSuggestedModuleInsteadNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsNotModuleScope || fs.IsNameNotEqualToSuggestedNamespaceName || (fs.IsFsModule && fs.IsNameNotEqualToSuggestedNamespaceName));

                // -----------------
                // namespace actions
                // -----------------
                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsFsModule && fs.IsNameNotEqualToSuggestedModuleName && fs.IsNameNotEqualToSuggestedNamespaceName);

                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToSuggestedNamespaceAction(trackingSpan, fsScope),
                    fs => fs.IsNotNamespaceScope || fs.IsNameNotEqualToSuggestedNamespaceName || (fs.IsNamespaceScope && fs.IsNameNotEqualToSuggestedNamespaceName));

                TryAddAction(
                    namespaceSuggestedActions,
                    () => new ChangeToSuggestedNamespaceInsteadModuleAction(trackingSpan, fsScope),
                    fs => fs.IsNotNamespaceScope || fs.IsNameNotEqualToSuggestedModuleName || fs.IsNamespaceScope && fs.IsNameNotEqualToSuggestedModuleName);

                AddActions(
                    "F# Suggested Module Names",
                    moduleSuggestedActions
                        .Distinct(FsScopeActionBaseEqualityComparer.Default)
                        .OrderBy(a => a.DisplayText)
                        .ToArray());

                AddActions(
                    "F# Suggested Namespace Names",
                    namespaceSuggestedActions
                        .Distinct(FsScopeActionBaseEqualityComparer.Default)
                        .OrderBy(a => a.DisplayText)
                        .ToArray());
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

        private async Task<(bool canModifyModuleName, FsFileRootScope? fsScope)> CanModifyModuleNameAsync(SnapshotSpan range)
        {
            if (FsFileRootScope.TryCreate(range, out FsFileRootScope fsScope))
            {
                Assumes.False(string.IsNullOrWhiteSpace(_projectFileName));

                if (fsScope.TrySetSuggestedFsModuleName(_projectFileName, _textDocument.FilePath))
                {
                    return (true, fsScope);
                }
            }

            return (false, null);
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = _textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            ITextStructureNavigator navigator = _provider.NavigatorService.GetTextStructureNavigator(_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }

        private bool IsPositionInMultilineComment(ITextSnapshot snapshot, int position)
        {
            // Scan backward from position to find unclosed multiline comment markers.
            // F# multiline comments are (* ... *) and can be nested.
            // This optimized approach counts only unmatched opening markers.
            int depth = 0;
            int i = position - 1;

            while (i >= 1)
            {
                // Check for closing marker *) - scan backward
                if (snapshot[i] == ')' && snapshot[i - 1] == '*')
                {
                    depth--;
                    i -= 2;
                    continue;
                }

                // Check for opening marker (* - scan backward
                if (snapshot[i] == '*' && snapshot[i - 1] == '(')
                {
                    depth++;
                    i -= 2;
                    continue;
                }

                i--;
            }

            // If depth > 0, we have unclosed opening markers, so we're inside a comment
            return depth > 0;
        }
    }
    */
}
