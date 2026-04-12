using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

#nullable enable

namespace FSharpNamespacer.Actions
{
    internal sealed class WrapToModuleAction : ChangeLineAction
    {
        private readonly IReadOnlyCollection<string> _suggestedNameSegments;
        private readonly int _indentSize;
        private readonly string _moduleName;

        public WrapToModuleAction(
            ITrackingSpan trackingSpan,
            string originKeyword,
            Queue<(CodeCommentType, string)> originLine,
            IReadOnlyCollection<string> suggestedNameSegments,
            int indentSize
        )
            : base(trackingSpan, originKeyword, originLine, suggestedNameSegments)
        {
            if (suggestedNameSegments.Count <= 1)
            {
                throw new ArgumentException("Suggested name must contain namespace.", nameof(suggestedNameSegments));
            }

            _suggestedNameSegments = suggestedNameSegments;
            _indentSize = indentSize;
            _moduleName = suggestedNameSegments.Last();
        }

        //------------------------------------------------------
        //
        //  ISuggestedAction implementation
        //
        //------------------------------------------------------

        #region ISuggestedAction implementation

        private string? _displayText;
        public override string DisplayText
        {
            get
            {
                if (_displayText != null)
                {
                    return _displayText;
                }

                _displayText = $"namespace {NamespaceName} ... module {_moduleName} =".TrimEnd();

                return _displayText;
            }
        }

        #endregion ISuggestedAction implementation

        private string? _namespaceName;

        private string NamespaceName
        {
            get
            {
                if (_namespaceName != null)
                {
                    return _namespaceName;
                }

                _namespaceName = String.Join(".", _suggestedNameSegments.Take(_suggestedNameSegments.Count - 1));
                return _namespaceName;
            }
        }

        protected override TextBlock GetReplacementTextBlock()
        {
            TextBlock replacementTextBlock = GetTextBlock(_greenBrush);
            void AddPlusSign()
            {
                replacementTextBlock.Inlines.Add(
                    new Run() { Text = "+", Foreground = _diffForegroundBrush, Background = _lightGreenBrush }
                );
            }

            AddPlusSign();
            replacementTextBlock.Inlines.Add(new Run() { Text = "namespace ", Foreground = _keywordBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = NamespaceName, Foreground = _nameBrush });
            replacementTextBlock.Inlines.Add(new LineBreak());
            AddPlusSign();
            replacementTextBlock.Inlines.Add(new LineBreak());
            if (_comment != string.Empty)
            {
                AddPlusSign();
                replacementTextBlock.Inlines.Add(new Run() { Text = _comment, Foreground = _commentBrush });
                replacementTextBlock.Inlines.Add(new LineBreak());
            }
            AddPlusSign();
            replacementTextBlock.Inlines.Add(new Run() { Text = "module ", Foreground = _keywordBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = _moduleName, Foreground = _typeBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = " =", Foreground = _nameBrush });

            return replacementTextBlock;
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshot snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;
            SnapshotSpan span = _trackingSpan.GetSpan(snapshot);

            // Get the updated snapshot after replacement
            int currentLineNumber = snapshot.GetLineNumberFromPosition(span.Start);
            ITextSnapshotLine currentLine = snapshot.GetLineFromLineNumber(currentLineNumber);

            // Detect the line ending used in the current file
            string lineEnding = GetNewLineText(currentLine);

            List<string> replacementLines;
            if (_comment == string.Empty)
            {
                replacementLines = new List<string>
                {
                    $"namespace {NamespaceName}",
                    "",
                    $"module {_moduleName} ="
                };
            }
            else
            {
                replacementLines = new List<string>
                {
                    $"namespace {NamespaceName}",
                    "",
                    _comment,
                    $"module {_moduleName} ="
                };
            }

            string replacementText = String.Join(lineEnding, replacementLines);
            _trackingSpan.TextBuffer.Replace(span, replacementText);

            snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;

            var indentString = new String(' ', _indentSize);

            // Indent all lines after the replacement
            for (int i = currentLineNumber + replacementLines.Count; i < snapshot.LineCount; i++)
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                string lineText = line.GetText();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                string indentedText = indentString + lineText;
                _trackingSpan.TextBuffer.Replace(line.Extent, indentedText);
                snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;
            }
        }

        static string GetNewLineText(ITextSnapshotLine line)
        {
            if (line.LineBreakLength > 0)
            {
                return line.GetLineBreakText();
            }
            else if (line.LineNumber - 1 >= 0)
            {
                // If this is the last line then there is no line break, use the line above 
                var lineAbove = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                return lineAbove.GetLineBreakText();
            }
            else
            {
                // Buffer only hase a single line, use the default new line sequence 
                return Environment.NewLine;
            }
        }
    }
}
