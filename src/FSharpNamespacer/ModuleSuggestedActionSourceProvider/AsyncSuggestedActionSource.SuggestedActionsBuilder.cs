using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

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

                private readonly State _state;

                private SuggestedActionsBuilder(State state, Span span, int versionNumber)
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

                internal bool IsNone => _state == State.None;

                internal bool IsNamespaceDetected => _state == State.NamespaceDetected;

                internal bool IsModuleDetected => _state == State.ModuleDetected;

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

                        bool isInComment = IsInComment(range.Start - 1, navigator);

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

                        bool isInComment = IsInComment(range.Start - 1, navigator);

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

                //------------------------------------------------------
                //
                //  types
                //
                //------------------------------------------------------

                #region types

                private enum State
                {
                    None,
                    NamespaceDetected,
                    ModuleDetected
                }

                internal sealed class None : SuggestedActionsBuilder
                {
                    public None(Span span, int versionNumber)
                        : base(State.None, span, versionNumber)
                    { }
                }

                internal sealed class NamespaceDetected : SuggestedActionsBuilder
                {
                    public NamespaceDetected(Span span, int versionNumber, Queue<string> nameSegments)
                        : base(State.NamespaceDetected, span, versionNumber)
                    {
                        NameSegments = nameSegments;
                    }

                    internal Queue<string> NameSegments { get; }
                }

                internal sealed class ModuleDetected : SuggestedActionsBuilder
                {
                    public ModuleDetected(Span span, int versionNumber, Queue<string> nameSegments)
                        : base(State.ModuleDetected, span, versionNumber)
                    {
                        NameSegments = nameSegments;
                    }

                    internal Queue<string> NameSegments { get; }
                }

                #endregion types
            }
        }
    }
}
