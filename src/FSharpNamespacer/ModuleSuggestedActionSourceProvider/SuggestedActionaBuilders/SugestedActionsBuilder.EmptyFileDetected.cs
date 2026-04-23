using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using FSharpNamespacer.Actions;
using FSharpNamespacer.Models;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

#nullable enable

namespace FSharpNamespacer.ModuleSuggestedActionSourceProvider
{
    internal sealed partial class ModuleSuggestedActionSourceProvider
    {
        private sealed partial class AsyncSuggestedActionSource
        {
            private abstract partial class SuggestedActionsBuilder
            {
                internal sealed class EmptyFileDetected : SuggestedActionsBuilder
                {
                    public EmptyFileDetected(Span span, int versionNumber, int indentSize)
                        : base(BuilderType.EmptyFileDetected, span, versionNumber, indentSize)
                    {
                        NameSegments = new Queue<(CodeCommentType, string)>();
                    }

                    /// <summary>
                    /// Empty queue.
                    /// </summary>
                    internal Queue<(CodeCommentType, string)> NameSegments { get; }

                    internal override IEnumerable<SuggestedActionSet> GetSuggestedActionSets(
                        ITextBuffer textBuffer,
                        SnapshotSpan range,
                        string sourceFilePath,
                        string? projectFilePath)
                    {
                        Queue<string> suggestedNameSegments = PathUtilities.GetRelativePathSegments(projectFilePath, sourceFilePath);

                        ISuggestedAction[] namespaceActions = GetNamespaceSuggestedActions(range, suggestedNameSegments).ToArray();
                        ISuggestedAction[] moduleActions = GetModuleSuggestedActions(range, suggestedNameSegments).ToArray();

                        bool suggestedNameContainsNamespace = suggestedNameSegments.Count > 1;

                        SuggestedActionSet moduleSet = new SuggestedActionSet(
                            categoryName: SuggestedActionSetCategoryName,
                            title: "F# Suggested Module Names",
                            actions: moduleActions);

                        SuggestedActionSet namespaceSet = new SuggestedActionSet(
                            categoryName: SuggestedActionSetCategoryName,
                            title: "F# Suggested Namespace Names",
                            actions: namespaceActions);

                        return suggestedNameContainsNamespace
                            ? new[] { namespaceSet, moduleSet, GetWrappedModuleActionsSet(range, MODULE_WORD, NameSegments, suggestedNameSegments) }
                            : new[] { namespaceSet, moduleSet };
                    }

                    private IEnumerable<ISuggestedAction> GetModuleSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments
                    )
                    {
                        ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive);

                        yield return new ChangeLineAction(
                            trackingSpan,
                            string.Empty,
                            NameSegments,
                            MODULE_WORD,
                            suggestedNameSegments
                        );
                    }

                    private IEnumerable<ISuggestedAction> GetNamespaceSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments
                    )
                    {
                        ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive);

                        // change only keyword
                        yield return new ChangeLineAction(
                            trackingSpan,
                            string.Empty,
                            NameSegments,
                            NAMESPACE_WORD,
                            suggestedNameSegments);
                    }
                }
            }
        }
    }
}
