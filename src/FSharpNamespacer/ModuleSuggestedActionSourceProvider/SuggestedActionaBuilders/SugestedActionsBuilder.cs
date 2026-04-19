using System;
using System.Collections.Generic;
using System.Linq;
using FSharpNamespacer.Actions;
using FSharpNamespacer.Models;
using FSharpNamespacer.Utilities;
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

                //------------------------------------------------------
                //
                //  static
                //
                //------------------------------------------------------

                #region static

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

                        if ((range.Start == 0 || !isInComment) && NameParser.TryGetNameSegments(navigator, range, running, out (Queue<(CodeCommentType, string)> nameSegments, bool hasEqualSign) result) && !result.hasEqualSign)
                        {
                            return new ModuleDetected(range.Span, range.Snapshot.Version.VersionNumber, result.nameSegments, source._indentSize);
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

                        if ((range.Start == 0 || !isInComment) && NameParser.TryGetNameSegments(navigator, range, running, out (Queue<(CodeCommentType, string)> nameSegments, bool hasEqualSign) result) && !result.hasEqualSign)
                        {
                            return new NamespaceDetected(range.Span, range.Snapshot.Version.VersionNumber, result.nameSegments, source._indentSize);
                        }
                    }

                    return CreateNone(range);
                }

                private static bool IsInComment(SnapshotPoint start, ITextStructureNavigator navigator)
                {
                    ITextSnapshot snapshot = start.Snapshot;
                    ITextSnapshotLine currentLine = snapshot.GetLineFromPosition(start.Position);
                    foreach (ITextSnapshotLine? line in snapshot.Lines.Where(l => l.LineNumber <= currentLine.LineNumber).Reverse())
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

                internal static SuggestedActionsBuilder CreateNone(SnapshotSpan range)
                    => new None(range.Span, range.Snapshot.Version.VersionNumber);

                #endregion static

                private readonly BuilderType _builderType;

                private SuggestedActionsBuilder(BuilderType state, Span span, int versionNumber, int indentSize)
                {
                    _builderType = state;
                    Span = span;
                    VersionNumber = versionNumber;
                    IndentSize = indentSize;
                }

                //------------------------------------------------------
                //
                //  properties
                //
                //------------------------------------------------------

                #region properties

                internal bool IsNone => _builderType == BuilderType.None;

                internal bool IsNamespaceDetected => _builderType == BuilderType.NamespaceDetected;

                internal bool IsModuleDetected => _builderType == BuilderType.ModuleDetected;

                internal Span Span { get; }

                internal int VersionNumber { get; }

                public int IndentSize { get; }

                protected string SuggestedActionSetCategoryName => PredefinedSuggestedActionCategoryNames.Any;

                public string Tag => _builderType.ToString();

                #endregion properties

                //------------------------------------------------------
                //
                //  methods
                //
                //------------------------------------------------------

                #region methods

                internal bool CorrespondsTo(SnapshotSpan range)
                    => Span == range.Span && range.Snapshot.Version.VersionNumber == VersionNumber;

                internal virtual IEnumerable<SuggestedActionSet> GetSuggestedActionSets(
                    ITextBuffer textBuffer,
                    SnapshotSpan range,
                    string sourceFilePath,
                    string? projectFilePath
                )
                    => Enumerable.Empty<SuggestedActionSet>();

                protected SuggestedActionSet GetWrappedModuleActionsSet(
                    SnapshotSpan range,
                    string originKeyword,
                    Queue<(CodeCommentType, string)> originNameSegments,
                    Queue<string> suggestedNameSegments
                )
                {
                    ISuggestedAction[] wrappedModuleActions =
                        suggestedNameSegments.Count > 1
                            ? new[] {
                                new WrapToModuleAction(
                                    range.Snapshot.CreateTrackingSpan(range.Span, SpanTrackingMode.EdgeExclusive),
                                    originKeyword,
                                    originNameSegments,
                                    suggestedNameSegments,
                                    IndentSize
                                )
                            }
                            : Array.Empty<ISuggestedAction>();

                    return new SuggestedActionSet(
                        categoryName: SuggestedActionSetCategoryName,
                        title: "F# Suggested Wrapped Module",
                        actions: wrappedModuleActions);
                }

                #endregion methods

                //------------------------------------------------------
                //
                //  BuilderType enum
                //
                //------------------------------------------------------

                #region BuilderType enum

                private enum BuilderType
                {
                    None,
                    NamespaceDetected,
                    ModuleDetected
                }

                #endregion BuilderType enum
            }
        }
    }
}
