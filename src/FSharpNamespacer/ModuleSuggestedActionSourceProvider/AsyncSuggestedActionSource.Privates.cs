using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;

namespace FSharpNamespacer.ModuleSuggestedActionSourceProvider
{
    /*
    public sealed partial class ModuleSuggestedActionSourceProvider
    {
        private sealed partial class AsyncSuggestedActionSource : IAsyncSuggestedActionsSource
        {
            
            private FsFileScopeBuilderState DetermineFsFileScopeBuilderState(SnapshotSpan range)
            {
                ITextStructureNavigator navigator = _provider.NavigatorService
                    .GetTextStructureNavigator(_textBuffer);

                // returns all file content or single line:
                // SnapshotSpan spanOfEnclosing = navigator.GetSpanOfEnclosing(range);

                IClassifier classifier = _provider.ClassifierAggregatorService.GetClassifier(_textBuffer);
                var classificationSpans = classifier.GetClassificationSpans(range);

                var first = navigator.GetExtentOfWord(range.Start);
                var span = first.Span;

                if (!first.IsSignificant)
                {
                    return new FsFileScopeBuilderState.None();
                }

                if (IsPositionInMultilineComment(range.Snapshot, span.Start))
                {
                    return new FsFileScopeBuilderState.None();
                }

                if (span.Length == NAMESPACE_WORD_LENGTH
                    && span.GetText().Equals(NAMESPACE_WORD, StringComparison.Ordinal))
                {
                    return new FsFileScopeBuilderState.NamespaceDetected(
                        range.Span,
                        range.Snapshot.Version.VersionNumber);
                }

                if (span.Length == MODULE_WORD_LENGTH
                    && span.GetText().Equals(MODULE_WORD, StringComparison.Ordinal))
                {
                    var start = span.End;

                    bool nextIsDot = false;
                    var rentSegments = ArrayPool<string>.Shared.Rent(10);
                    int segmentInd = 0;
                    int? commentStart = null;

                    while (start < range.End)
                    {
                        var next = navigator.GetExtentOfWord(start);
                        if (!next.IsSignificant)
                        {
                            start = next.Span.End;
                            continue;
                        }

                        string text = next.Span.GetText();

                        if (text[0] == '=')
                        {
                            return new FsFileScopeBuilderState.None();
                        }

                        if (text.Length == 2
                            && (
                                text[0] == '/' && text[1] == '/')
                                || (text[0] == '(' && text[1] == '*')
                            )
                        {
                            commentStart = next.Span.Start;
                            break;
                        }

                        if (nextIsDot && text[0] == '.')
                        {
                            start = next.Span.End;
                            continue;
                        }

                        if (!nextIsDot && text[0] != '_' && !char.IsLetter(text[0]))
                        {
                            return new FsFileScopeBuilderState.None();
                        }

                        rentSegments[segmentInd++] = text;
                        start = next.Span.End;
                        nextIsDot = true;
                    }

                    string[] segments =
                        rentSegments.Length > 0
                            ? rentSegments.Take(segmentInd).ToArray()
                            : Array.Empty<string>();

                    ArrayPool<string>.Shared.Return(rentSegments);

                    return new FsFileScopeBuilderState.ModuleDetected(
                        segments,
                        range.Span,
                        range.Snapshot.Version.VersionNumber,
                        commentStart);
                }

                return new FsFileScopeBuilderState.None();
            }

            private string[] GetNameSegments(ITextStructureNavigator navigator, SnapshotSpan range, SnapshotPoint start)
            {
                bool nextIsDot = false;
                var rentSegments = ArrayPool<string>.Shared.Rent(10);
                int segmentInd = 0;
                int? commentStart = null;

                while (start < range.End)
                {
                    var next = navigator.GetExtentOfWord(start);
                    if (!next.IsSignificant)
                    {
                        start = next.Span.End;
                        continue;
                    }

                    string text = next.Span.GetText();

                    if (text[0] == '=')
                    {

                        return Array.Empty<string>();
                    }

                    if (text.Length == 2
                        && (
                            text[0] == '/' && text[1] == '/')
                            || (text[0] == '(' && text[1] == '*')
                        )
                    {
                        commentStart = next.Span.Start;
                        break;
                    }

                    if (nextIsDot && text[0] == '.')
                    {
                        start = next.Span.End;
                        continue;
                    }

                    if (!nextIsDot && text[0] != '_' && !char.IsLetter(text[0]))
                    {
                        return Array.Empty<string>();
                    }

                    rentSegments[segmentInd++] = text;
                    start = next.Span.End;
                    nextIsDot = true;
                }

                string[] segments =
                    rentSegments.Length > 0
                        ? rentSegments.Take(segmentInd).ToArray()
                        : Array.Empty<string>();

                ArrayPool<string>.Shared.Return(rentSegments);

                return segments;
            }

        }
    }
            */
}
