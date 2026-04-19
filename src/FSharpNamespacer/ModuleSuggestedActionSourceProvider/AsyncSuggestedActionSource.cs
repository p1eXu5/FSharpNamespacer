using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
            private readonly int _indentSize;
            private SuggestedActionsBuilder? _lastBuilder;

            public AsyncSuggestedActionSource(
                ModuleSuggestedActionSourceProvider provider,
                ITextView textView,
                ITextBuffer textBuffer,
                ITextDocument textDocument,
                string? projectFileName,
                int indentSize)
            {
                _provider = provider;
                _textView = textView;
                _textBuffer = textBuffer;
                _textDocument = textDocument;
                _projectFileName = projectFileName;
                _indentSize = indentSize;
            }

            //------------------------------------------------------
            //
            //  IDisposable implementation
            //
            //------------------------------------------------------

            #region IDisposable implementation

            public void Dispose()
            {
            }

            #endregion IDisposable implementation

            //------------------------------------------------------
            //
            //  ISuggestedActionsSource implementation
            //
            //------------------------------------------------------

            #region ISuggestedActionsSource implementation

#pragma warning disable CS0067 // is never used
            public event EventHandler<EventArgs>? SuggestedActionsChanged;
#pragma warning restore CS0067 // is never used

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
                    Debug.WriteLine("[FSharpNamespacer.HasSuggestedActionsAsync] Suggested action category set does not contain Refactoring category.");
                    return false;
                }

                SuggestedActionsBuilder builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    Debug.WriteLine("[FSharpNamespacer.HasSuggestedActionsAsync] Suggested action builder is None.");
                    return false;
                }

                Debug.WriteLine($"[FSharpNamespacer.HasSuggestedActionsAsync] Suggested action builder is {builder.Tag}.");
                return true;
            }

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken
            )
            {
                if (!requestedActionCategories.Contains(PredefinedSuggestedActionCategoryNames.Refactoring))
                {
                    Debug.WriteLine("[FSharpNamespacer.GetSuggestedActions] Suggested action category set does not contain Refactoring category.");
                    return Enumerable.Empty<SuggestedActionSet>();
                }

                SuggestedActionsBuilder builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    Debug.WriteLine("[FSharpNamespacer.GetSuggestedActions] Suggested action builder is None.");
                    return Enumerable.Empty<SuggestedActionSet>();
                }

                Debug.WriteLine($"[FSharpNamespacer.GetSuggestedActions] Suggested action builder is {builder.Tag}.");

                return builder.GetSuggestedActionSets(
                    _textBuffer,
                    range
                    , _textDocument.FilePath,
                    _projectFileName);
            }


            #endregion ISuggestedActionsSource implementation

            //------------------------------------------------------
            //
            //  ISuggestedActionsSource2 implementation (reserved)
            //
            //------------------------------------------------------

            #region ISuggestedActionsSource2 implementation (reserved)

            // Using in ISuggestedActionsSource2 and ISuggestedActionsSource3
            public async Task<ISuggestedActionCategorySet?> GetSuggestedActionCategoriesAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                CancellationToken cancellationToken
            )
            {
                await SwitchToTaskPoolAsync(cancellationToken);
                await WaitToScrollAsync(cancellationToken);

                SuggestedActionsBuilder builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return null;
                }

                // AllRefactorings - screw is shown
                // AllCodeFixesAndRefactorings - light bulb is shown
                return _provider.SuggestedActionCategoryRegistryService.AllCodeFixesAndRefactorings;
            }

            #endregion ISuggestedActionsSource2 implementation (reserved)

            //------------------------------------------------------
            //
            //  ISuggestedActionsSource3 implementation (reserved)
            //
            //------------------------------------------------------

            #region ISuggestedActionsSource3 implementation (reserved)

            // Using in ISuggestedActionsSource3
            public IEnumerable<SuggestedActionSet> GetSuggestedActions(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                IUIThreadOperationContext operationContext)
            {
                throw new NotImplementedException();
            }

            #endregion ISuggestedActionsSource3 implementation (reserved)

            //------------------------------------------------------
            //
            //  IAsyncSuggestedActionsSource implementation (reserved)
            //
            //------------------------------------------------------

            #region IAsyncSuggestedActionsSource implementation (reserved)

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

                SuggestedActionsBuilder builder = GetSuggestedActionsBuilder(range, cancellationToken);

                if (builder.IsNone)
                {
                    return;
                }

                return;
            }

            #endregion IAsyncSuggestedActionsSource implementation (reserved)

            //------------------------------------------------------
            //
            //  ITelemetryIdProvider<Guid> implementation
            //
            //------------------------------------------------------

            #region ITelemetryIdProvider<Guid> implementation

            public bool TryGetTelemetryId(out Guid telemetryId)
            {
                telemetryId = default;
                return false;
            }

            #endregion ITelemetryIdProvider<Guid> implementation

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

                SuggestedActionsBuilder? lastBuilder = Volatile.Read(ref _lastBuilder);

                if (lastBuilder != null && lastBuilder.CorrespondsTo(range))
                {
                    return lastBuilder;
                }

                SuggestedActionsBuilder builder = SuggestedActionsBuilder.Create(this, range);
                Volatile.Write(ref _lastBuilder, builder);

                return builder;
            }
        }
    }
}
