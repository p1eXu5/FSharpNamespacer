using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

#nullable enable

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`module FsModuleOrNamespaceName</c>
    /// </summary>
    internal sealed class ChangeLineAction : ISuggestedAction
    {
        //------------------------------------------------------
        //
        //  static
        //
        //------------------------------------------------------

        #region static

        private static readonly SolidColorBrush _backgroundBrush;
        private static readonly SolidColorBrush _nameBrush;
        private static readonly SolidColorBrush _redBrush;
        private static readonly SolidColorBrush _lightRedBrush;
        private static readonly SolidColorBrush _greenBrush;
        private static readonly SolidColorBrush _lightGreenBrush;
        private static readonly SolidColorBrush _diffForegroundBrush;
        private static readonly SolidColorBrush _commentBrush;
        private static readonly SolidColorBrush _keywordBrush;
        private static readonly SolidColorBrush _typeBrush;

        static ChangeLineAction()
        {
            var backgroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E));
            backgroundBrush.Freeze();
            _backgroundBrush = backgroundBrush;

            var nameBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6));
            nameBrush.Freeze();
            _nameBrush = nameBrush;

            var redBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x2D, 0x00, 0x00));
            redBrush.Freeze();
            _redBrush = redBrush;

            var lightRedBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3C, 0x00, 0x00));
            lightRedBrush.Freeze();
            _lightRedBrush = lightRedBrush;

            var greenBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x15, 0x35, 0x2C));
            greenBrush.Freeze();
            _greenBrush = greenBrush;

            var lightGreenBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0X26, 0x5E, 0x4D));
            lightGreenBrush.Freeze();
            _lightGreenBrush = lightGreenBrush;

            var diffForegroundBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
            diffForegroundBrush.Freeze();
            _diffForegroundBrush = diffForegroundBrush;

            var commentBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x57, 0xA6, 0x4A));
            commentBrush.Freeze();
            _commentBrush = commentBrush;

            var keywordBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x56, 0x9C, 0xD6));
            keywordBrush.Freeze();
            _keywordBrush = keywordBrush;

            var typeBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4E, 0xC9, 0xB0));
            typeBrush.Freeze();
            _typeBrush = typeBrush;
        }

        #endregion static

        private readonly ITrackingSpan _trackingSpan;
        private readonly string _suggestedkeyword;
        private readonly string _newName;
        private readonly bool _isSuggestedModule;
        private readonly string _comment;
        private readonly string _originKeyword;
        private readonly Queue<(CodeCommentType, string)> _originLine;

        public ChangeLineAction(
            ITrackingSpan trackingSpan,
            string originKeyword,
            Queue<(CodeCommentType, string)> originLine,
            IEnumerable<string> suggestedNameSegments
        )
            : this(
                  trackingSpan,
                  originKeyword,
                  originLine,
                  originKeyword,
                  suggestedNameSegments
            )
        {
        }

        public ChangeLineAction(
            ITrackingSpan trackingSpan,
            string originKeyword,
            Queue<(CodeCommentType, string)> originLine,
            string suggestedKeyword
        )
            : this(
                trackingSpan,
                originKeyword,
                originLine,
                suggestedKeyword,
                originLine
                    .Where(t => t.Item1 == CodeCommentType.Code)
                    .Select(t => t.Item2)
            )
        {
        }

        public ChangeLineAction(
            ITrackingSpan trackingSpan,
            string originKeyword,
            Queue<(CodeCommentType, string)> originLine,
            string suggestedKeyword,
            IEnumerable<string> suggestedNameSegments
        )
        {
            if (originLine == null)
            {
                throw new ArgumentNullException(nameof(originLine));
            }

            if (originLine.Count == 0)
            {
                throw new ArgumentException("originLine must contain items.");
            }

            _originLine = originLine;
            _originKeyword = originKeyword;

            IEnumerable<string> commentSegments =
                originLine
                      .Where(t => t.Item1 == CodeCommentType.InlineComment || t.Item1 == CodeCommentType.TerminateComment)
                      .Select(t => t.Item2);

            _trackingSpan = trackingSpan;
            _suggestedkeyword = suggestedKeyword;

            var name = String.Join(".", suggestedNameSegments);
            _newName = name;

            _isSuggestedModule = suggestedKeyword == "module";

            var comment = String.Join(" ", commentSegments);
            _comment = comment;

            DisplayText = $"{suggestedKeyword} {name} {comment}".TrimEnd();
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
        //  ISuggestedAction implementation
        //
        //------------------------------------------------------

        #region ISuggestedAction implementation

        public bool HasActionSets { get; } = false;

        public string DisplayText { get; }

        public ImageMoniker IconMoniker { get; } = default;

        public string? IconAutomationText { get; } = null;

        public string? InputGestureText { get; } = null;

        public bool HasPreview { get; } = true;

        public Task<IEnumerable<SuggestedActionSet>?> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>?>(null);
        }

        public Task<object?> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var existingTextBlock = new TextBlock
            {
                Background = _redBrush,
                Padding = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.ExtraLight,
            };
            existingTextBlock.Inlines.Add(new Run() { Text = "-", Foreground = _diffForegroundBrush, Background = _lightRedBrush });
            existingTextBlock.Inlines.Add(new Run() { Text = _originKeyword + ' ', Foreground = _keywordBrush });

            (CodeCommentType, string) codeComment;
            int i = 0;
            
            using var enumerator = _originLine.GetEnumerator();
            enumerator.MoveNext();

            bool isLastCode = false;

            codeComment = enumerator.Current;
            switch (codeComment.Item1)
            {
                case CodeCommentType.Code:
                    existingTextBlock.Inlines.Add(new Run() { Text = codeComment.Item2, Foreground = _nameBrush });
                    isLastCode = true;
                    break;

                case CodeCommentType.InlineComment:
                case CodeCommentType.TerminateComment:
                    existingTextBlock.Inlines.Add(new Run() { Text = codeComment.Item2, Foreground = _commentBrush });
                    isLastCode = false;
                    break;
            }

            ++i;

            if (_originLine.Count > 1)
            {
                for (; i <= _originLine.Count - 1; ++i)
                {
                    enumerator.MoveNext();
                    codeComment = enumerator.Current;

                    switch (codeComment.Item1)
                    {
                        case CodeCommentType.Code:
                            existingTextBlock.Inlines.Add(
                                new Run() { Text = (isLastCode ? '.' : ' ') + codeComment.Item2, Foreground = _nameBrush }
                            );

                            isLastCode = true;

                            break;

                        case CodeCommentType.InlineComment:
                        case CodeCommentType.TerminateComment:
                            existingTextBlock.Inlines.Add(
                                new Run() { Text =  ' ' + codeComment.Item2, Foreground = _commentBrush }
                            );

                            isLastCode = false;
                            
                            break;
                    }
                }
            }


            var replacementTextBlock = new TextBlock
            {
                Background = _greenBrush,
                Padding = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.ExtraLight,
            };
            replacementTextBlock.Inlines.Add(new Run() { Text = "+", Foreground = _diffForegroundBrush, Background = _lightGreenBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = _suggestedkeyword + ' ', Foreground = _keywordBrush });

            if (_isSuggestedModule)
            {
                var dotInd = _newName.LastIndexOf(".");
                if (dotInd >= 0 && dotInd < _newName.Length - 1)
                {
                    replacementTextBlock.Inlines.Add(new Run()
                    {
                        Text = _newName.Substring(0, dotInd + 1),
                        Foreground = _nameBrush 
                    });

                    replacementTextBlock.Inlines.Add(new Run()
                    {
                        Text = _newName.Substring(dotInd + 1) + ' ',
                        Foreground = _typeBrush
                    });
                }
                else
                {
                    replacementTextBlock.Inlines.Add(new Run() { Text = _newName + ' ', Foreground = _typeBrush });
                }
            }
            else
            {
                replacementTextBlock.Inlines.Add(new Run() { Text = _newName + ' ', Foreground = _nameBrush });
            }
            replacementTextBlock.Inlines.Add(new Run() { Text = _comment, Foreground = _commentBrush });

            var stackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
            };
            stackPanel.Children.Add(new TextBlock
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.ExtraLight,
                Text = " ...",
            });
            stackPanel.Children.Add(existingTextBlock);
            stackPanel.Children.Add( replacementTextBlock );
            stackPanel.Children.Add(new TextBlock
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.ExtraLight,
                Text = " ...",
            });

            var border = new Border
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 5, 10, 5),
                Background = _backgroundBrush,
                Child = stackPanel,
            };

            return Task.FromResult<object?>(border);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            _trackingSpan.TextBuffer.Replace(_trackingSpan.GetSpan(_trackingSpan.TextBuffer.CurrentSnapshot), DisplayText);
        }

        #endregion ISuggestedAction implementation

        //------------------------------------------------------
        //
        //  ITelemetryIdProvider<Guid> implementation
        //
        //------------------------------------------------------

        #region ITelemetryIdProvider<Guid> implementation

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample action and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }

        #endregion ITelemetryIdProvider<Guid> implementation
    }
}
