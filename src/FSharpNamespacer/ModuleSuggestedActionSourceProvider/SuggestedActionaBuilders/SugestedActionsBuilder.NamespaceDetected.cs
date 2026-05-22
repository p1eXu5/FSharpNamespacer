using System;
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
                /// <summary>
                /// Provides suggested actions for module and namespace naming based on code analysis and project
                /// structure.
                /// </summary>
                internal sealed class NamespaceDetected : SuggestedActionsBuilder
                {
                    public NamespaceDetected(Span span, int versionNumber, Queue<(CodeCommentType, string)> nameSegments, int indentSize)
                        : base(BuilderType.NamespaceDetected, span, versionNumber, indentSize)
                    {
                        NameSegments = nameSegments;
                    }

                    internal Queue<(CodeCommentType, string)> NameSegments { get; }

                    /// <summary>
                    /// Returns suggested action sets for module and namespace naming based on the specified file and
                    /// project context.
                    /// </summary>
                    /// <param name="textBuffer">The text buffer containing the code.</param>
                    /// <param name="range">The span of text to analyze for suggested actions.</param>
                    /// <param name="sourceFilePath">The full path to the source file.</param>
                    /// <param name="projectFilePath">The full path to the project file, or null if not available.</param>
                    /// <returns>A collection of suggested action sets for module and namespace names.</returns>
                    internal override IEnumerable<SuggestedActionSet> GetSuggestedActionSets(
                        ITextBuffer textBuffer,
                        SnapshotSpan range,
                        string sourceFilePath,
                        string? projectFilePath)
                    {
                        LogUtilities.LogDebug("Constructing suggestions...");

                        if (!TryGetSuggestedNameSegments(sourceFilePath, projectFilePath, out var suggestedNameSegments))
                        {
                            return Enumerable.Empty<SuggestedActionSet>();
                        }

                        bool isSame = suggestedNameSegments.SequenceEqual(
                            NameSegments.Where(t => t.Item1 == CodeCommentType.Code).Select(t => t.Item2));
                        
                        bool suggestedNameContainsNamespace = suggestedNameSegments.Count > 1;

                        bool isSame1 =
                            suggestedNameContainsNamespace
                            && !isSame
                            && suggestedNameSegments.Take(suggestedNameSegments.Count - 1).SequenceEqual(
                                NameSegments.Where(t => t.Item1 == CodeCommentType.Code).Select(t => t.Item2));

                        Queue<string> suggestedOwnNameSegments = PathUtilities.GetRelativePathSegments(null, sourceFilePath);
                        // TODO: implement default suggested module name

                        ISuggestedAction[] moduleActions =
                            GetModuleSuggestedActions(
                                range,
                                suggestedNameSegments,
                                isSame,
                                isSame1,
                                suggestedNameContainsNamespace
                            )
                            .ToArray();

                        ISuggestedAction[] namespaceActions =
                            GetNamespaceSuggestedActions(
                                range,
                                suggestedNameSegments,
                                isSame,
                                isSame1,
                                suggestedNameContainsNamespace
                            )
                            .ToArray();

                        if (namespaceActions.Length == 0)
                        {
                            SuggestedActionSet moduleSet = new SuggestedActionSet(
                                categoryName: SuggestedActionSetCategoryName,
                                title: "F# Suggested Module Names",
                                actions: moduleActions);

                            LogUtilities.LogDebug("Have no namespace actions. Returning module suggestions...");

                            return suggestedNameContainsNamespace
                                ? new[] { 
                                    moduleSet, 
                                    GetWrappedModuleActionsSet(range, NAMESPACE_WORD, NameSegments, suggestedNameSegments),
                                    GetWrappedTypeActionsSet(range, NAMESPACE_WORD, NameSegments, suggestedNameSegments),
                                }
                                : new[] { moduleSet };
                        }

                        if (namespaceActions.Length > 0)
                        {
                            SuggestedActionSet namespaceSet = new SuggestedActionSet(
                                categoryName: SuggestedActionSetCategoryName,
                                title: "F# Suggested Namespace Names",
                                actions: namespaceActions);

                            SuggestedActionSet moduleSet = new SuggestedActionSet(
                                categoryName: SuggestedActionSetCategoryName,
                                title: "F# Suggested Module Names",
                                actions: moduleActions);

                            LogUtilities.LogDebug("Returning module and namespace suggestions...");

                            return suggestedNameContainsNamespace
                                ? new[] { 
                                    namespaceSet, 
                                    moduleSet, 
                                    GetWrappedModuleActionsSet(range, NAMESPACE_WORD, NameSegments, suggestedNameSegments),
                                    GetWrappedTypeActionsSet(range, NAMESPACE_WORD, NameSegments, suggestedNameSegments),
                                }
                                : new[] { namespaceSet, moduleSet };
                        }

                        return base.GetSuggestedActionSets(textBuffer, range, sourceFilePath, projectFilePath);
                    }

                    private IEnumerable<ISuggestedAction> GetNamespaceSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame,
                        bool isSame1,
                        bool suggestedNameContainsNamespace
                    )
                    {
                        if (!isSame)
                        {
                            yield return new ChangeLineAction(
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                NAMESPACE_WORD,
                                NameSegments,
                                suggestedNameSegments
                            );
                        }

                        if (!isSame1 && suggestedNameContainsNamespace)
                        {
                            yield return new ChangeLineAction(
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                NAMESPACE_WORD,
                                NameSegments,
                                suggestedNameSegments.Take(suggestedNameSegments.Count - 1)
                            );
                        }
                    }

                    private IEnumerable<ISuggestedAction> GetModuleSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame,
                        bool isSame1,
                        bool suggestedNameContainsNamespace
                    )
                    {
                        ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive);

                        // replace namespace with module
                        yield return new ChangeLineAction(
                            trackingSpan,
                            NAMESPACE_WORD,
                            NameSegments,
                            MODULE_WORD);

                        if (!isSame)
                        {
                            yield return new ChangeLineAction(
                                trackingSpan,
                                NAMESPACE_WORD,
                                NameSegments,
                                MODULE_WORD,
                                suggestedNameSegments);
                        }

                        if (!isSame1 && suggestedNameSegments.Count > 2)
                        {
                            yield return new ChangeLineAction(
                                trackingSpan,
                                NAMESPACE_WORD,
                                NameSegments,
                                MODULE_WORD,
                                suggestedNameSegments.Take(suggestedNameSegments.Count - 1));
                        }
                    }
                }
            }
        }
    }
}
