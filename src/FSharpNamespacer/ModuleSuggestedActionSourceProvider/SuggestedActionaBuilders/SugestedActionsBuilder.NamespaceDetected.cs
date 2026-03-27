using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FSharpNamespacer.Actions;
using FSharpNamespacer.Models;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

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
                    public NamespaceDetected(Span span, int versionNumber, Queue<(CodeCommentType, string)> nameSegments)
                        : base(BuilderType.NamespaceDetected, span, versionNumber)
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
                        Queue<string> suggestedNameSegments = PathUtilities.GetRelativePathSegments(projectFilePath, sourceFilePath);
                        bool isSame = suggestedNameSegments.SequenceEqual(
                            NameSegments.Where(t => t.Item1 == CodeCommentType.Code).Select(t => t.Item2));

                        Queue<string> suggestedOwnNameSegments = PathUtilities.GetRelativePathSegments(null, sourceFilePath);
                        // TODO: implement default suggested module name

                        var moduleActions = GetModuleSuggestedActions(range, suggestedNameSegments, isSame).ToArray();
                        var namespaceActions = GetNamespaceSuggestedActions(range, suggestedNameSegments, isSame).ToArray();

                        if (namespaceActions.Length == 0)
                        {
                            SuggestedActionSet moduleSet = new SuggestedActionSet(
                                "F# Suggested Module Names",
                                moduleActions);

                            return new[] { moduleSet };
                        }

                        if (namespaceActions.Length > 0)
                        {
                            SuggestedActionSet namespaceSet = new SuggestedActionSet(
                                "F# Suggested Namespace Names",
                                namespaceActions);

                            SuggestedActionSet moduleSet = new SuggestedActionSet(
                                "F# Suggested Module Names",
                                moduleActions);

                            return new[] { namespaceSet, moduleSet };
                        }

                        return base.GetSuggestedActionSets(textBuffer, range, sourceFilePath, projectFilePath);
                    }

                    private IEnumerable<ISuggestedAction> GetNamespaceSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame
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
                    }

                    private IEnumerable<ISuggestedAction> GetModuleSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame
                    )
                    {
                        ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive);

                        // change only keyword
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
                    }
                }
            }
        }
    }
}
