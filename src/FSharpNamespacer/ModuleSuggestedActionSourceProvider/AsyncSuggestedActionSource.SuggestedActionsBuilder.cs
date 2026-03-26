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
            private abstract class SuggestedActionsBuilder
            {
                //------------------------------------------------------
                //
                //  Constants
                //
                //------------------------------------------------------

                #region Constants

                private const string MODULE_WORD = "module";
                private const int MODULE_WORD_LENGTH = 6;

                private const string NAMESPACE_WORD = "namespace";
                private const int NAMESPACE_WORD_LENGTH = 9;

                #endregion Constants

                private readonly BuilderType _state;

                private SuggestedActionsBuilder(BuilderType state, Span span, int versionNumber)
                {
                    _state = state;
                    Span = span;
                    VersionNumber = versionNumber;
                }

                //------------------------------------------------------
                //
                //  properties
                //
                //------------------------------------------------------

                #region properties

                internal bool IsNone => _state == BuilderType.None;

                internal bool IsNamespaceDetected => _state == BuilderType.NamespaceDetected;

                internal bool IsModuleDetected => _state == BuilderType.ModuleDetected;

                internal Span Span { get; }

                internal int VersionNumber { get; }

                #endregion properties

                //------------------------------------------------------
                //
                //  builder
                //
                //------------------------------------------------------

                #region builder

                internal static SuggestedActionsBuilder Create(AsyncSuggestedActionSource source, SnapshotSpan range)
                {
                    if (range.Length < MODULE_WORD_LENGTH + 2)
                    {
                        return CreateNone(range);
                    }

                    ITextStructureNavigator navigator = source._provider.NavigatorService
                        .GetTextStructureNavigator(source._textBuffer);

                    TextExtent firstWord = navigator.GetExtentOfWord(range.Start);
                    SnapshotSpan firstWordSpan = firstWord.Span;

                    if (!firstWord.IsSignificant)
                    {
                        return CreateNone(range);
                    }

                    if (firstWordSpan.Length != MODULE_WORD_LENGTH && firstWordSpan.Length != NAMESPACE_WORD_LENGTH)
                    {
                        return CreateNone(range);
                    }

                    string text = firstWordSpan.GetText();
                    if (text == MODULE_WORD)
                    {
                        TextExtent running = navigator.GetExtentOfWord(firstWordSpan.End);

                        // check that 'module' follows whitespace
                        if (running.IsSignificant)
                        {
                            return CreateNone(range);
                        }

                        bool isInComment = range.Start > 0 && IsInComment(range.Start - 1, navigator);

                        if ((range.Start == 0 || !isInComment) && NameParser.TryGetNameSegments(navigator, range, running, out var result) && !result.hasEqualSign)
                        {
                            return new ModuleDetected(range.Span, range.Snapshot.Version.VersionNumber, result.nameSegments);
                        }
                    }
                    else if (text == NAMESPACE_WORD)
                    {
                        TextExtent running = navigator.GetExtentOfWord(firstWordSpan.End);

                        // check that 'module' follows whitespace
                        if (running.IsSignificant)
                        {
                            return CreateNone(range);
                        }

                        bool isInComment = range.Start > 0 && IsInComment(range.Start - 1, navigator);

                        if ((range.Start == 0 || !isInComment) && NameParser.TryGetNameSegments(navigator, range, running, out var result) && !result.hasEqualSign)
                        {
                            return new NamespaceDetected(range.Span, range.Snapshot.Version.VersionNumber, result.nameSegments);
                        }
                    }

                    return CreateNone(range);
                }

                private static bool IsInComment(SnapshotPoint start, ITextStructureNavigator navigator)
                {
                    var snapshot = start.Snapshot;
                    var currentLine = snapshot.GetLineFromPosition(start.Position);
                    foreach (var line in snapshot.Lines.Where(l => l.LineNumber <= currentLine.LineNumber).Reverse())
                    {
                        if (line.Length < 2)
                        {
                            continue;
                        }

                        TextExtent textExtent = navigator.GetExtentOfWord(line.End - 1);
                        if (textExtent.IsSignificant
                            && textExtent.Span.GetText().EndsWith("*)", System.StringComparison.Ordinal))
                        {
                            return false;
                        }

                        textExtent = navigator.GetExtentOfWord(line.Start);
                        if (textExtent.IsSignificant
                            && textExtent.Span.GetText().StartsWith("(*", System.StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                #endregion builder

                // internal abstract 

                internal static SuggestedActionsBuilder CreateNone(SnapshotSpan range)
                    => new None(range.Span, range.Snapshot.Version.VersionNumber);

                internal bool CorrespondsTo(SnapshotSpan range)
                    => Span == range.Span && range.Snapshot.Version.VersionNumber == VersionNumber;

                internal virtual IEnumerable<SuggestedActionSet> GetSuggestedActionSets(
                    ITextBuffer textBuffer,
                    SnapshotSpan range,
                    string sourceFilePath,
                    string? projectFilePath
                )
                    => Enumerable.Empty<SuggestedActionSet>();

                //------------------------------------------------------
                //
                //  types
                //
                //------------------------------------------------------

                #region types

                private enum BuilderType
                {
                    None,
                    NamespaceDetected,
                    ModuleDetected
                }

                internal sealed class None : SuggestedActionsBuilder
                {
                    public None(Span span, int versionNumber)
                        : base(BuilderType.None, span, versionNumber)
                    { }
                }

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
                        // TODO:

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
                                "namespace",
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                suggestedNameSegments,
                                NameSegments
                                    .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                    .Select(t => t.Item2));
                        }
                    }

                    private IEnumerable<ISuggestedAction> GetModuleSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame
                    )
                    {
                        yield return new ChangeLineAction(
                            "module",
                            range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                            NameSegments
                                .Where(t => t.Item1 == CodeCommentType.Code)
                                .Select(t => t.Item2),
                            NameSegments
                                .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                .Select(t => t.Item2));

                        if (!isSame)
                        {
                            yield return new ChangeLineAction(
                                "module",
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                suggestedNameSegments,
                                NameSegments
                                    .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                    .Select(t => t.Item2));
                        }
                    }
                }

                /// <summary>
                /// Provides suggested actions for detected F# modules, including namespace and module name
                /// recommendations based on code comments and file paths.
                /// </summary>
                internal sealed class ModuleDetected : SuggestedActionsBuilder
                {
                    public ModuleDetected(Span span, int versionNumber, Queue<(CodeCommentType, string)> nameSegments)
                        : base(BuilderType.ModuleDetected, span, versionNumber)
                    {
                        NameSegments = nameSegments;
                    }

                    internal Queue<(CodeCommentType, string)> NameSegments { get; }

                    /// <summary>
                    /// Returns suggested action sets for namespace and module naming based on the specified file and
                    /// project paths within the given text range.
                    /// </summary>
                    /// <param name="textBuffer">The text buffer containing the code.</param>
                    /// <param name="range">The span of text to analyze for suggested actions.</param>
                    /// <param name="sourceFilePath">The full path to the source file.</param>
                    /// <param name="projectFilePath">The full path to the project file, or null if unavailable.</param>
                    /// <returns>A collection of suggested action sets for namespace and module names.</returns>
                    internal override IEnumerable<SuggestedActionSet> GetSuggestedActionSets(
                        ITextBuffer textBuffer,
                        SnapshotSpan range,
                        string sourceFilePath,
                        string? projectFilePath)
                    {
                        Queue<string> suggestedNameSegments = PathUtilities.GetRelativePathSegments(projectFilePath, sourceFilePath);
                        bool isSame = suggestedNameSegments.SequenceEqual(
                            NameSegments.Where(t => t.Item1 == CodeCommentType.Code).Select(t => t.Item2));

                        var namespaceActions = GetNamespaceSuggestedActions(range, suggestedNameSegments, isSame).ToArray();
                        var moduleActions = GetModuleSuggestedActions(range, suggestedNameSegments, isSame).ToArray();

                        if (moduleActions.Length == 0)
                        {
                            SuggestedActionSet namespaceSet = new SuggestedActionSet(
                                "F# Suggested Namespace Names",
                                namespaceActions);

                            return new[] { namespaceSet };
                        }

                        if (moduleActions.Length > 0)
                        {
                            SuggestedActionSet moduleSet = new SuggestedActionSet(
                                "F# Suggested Module Names",
                                moduleActions);

                            SuggestedActionSet namespaceSet = new SuggestedActionSet(
                                "F# Suggested Namespace Names",
                                namespaceActions);

                            return new[] { namespaceSet, moduleSet };
                        }

                        return base.GetSuggestedActionSets(textBuffer, range, sourceFilePath, projectFilePath);
                    }

                    private IEnumerable<ISuggestedAction> GetModuleSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame
                    )
                    {
                        if (!isSame)
                        {
                            yield return new ChangeLineAction(
                                "module",
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                suggestedNameSegments,
                                NameSegments
                                    .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                    .Select(t => t.Item2));
                        }
                    }

                    private IEnumerable<ISuggestedAction> GetNamespaceSuggestedActions(
                        SnapshotSpan range,
                        Queue<string> suggestedNameSegments,
                        bool isSame
                    )
                    {
                        var trackingSpan = range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive);

                        yield return new ChangeLineAction(
                            "namespace",
                            range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                            NameSegments
                                .Where(t => t.Item1 == CodeCommentType.Code)
                                .Select(t => t.Item2),
                            NameSegments
                                .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                .Select(t => t.Item2));

                        if (!isSame)
                        {
                            yield return new ChangeLineAction(
                                "namespace",
                                range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                suggestedNameSegments,
                                NameSegments
                                    .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                                    .Select(t => t.Item2));
                        }
                    }
                }

                #endregion types
            }
        }
    }
}
