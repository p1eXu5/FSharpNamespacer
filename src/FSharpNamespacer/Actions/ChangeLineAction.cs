using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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
        }

        #endregion static

        private readonly ITrackingSpan _trackingSpan;
        private readonly string _keyword;
        private readonly string _newName;
        private readonly string _comment;

        /// <summary>
        /// <inheritdoc cref="ChangeLineAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public ChangeLineAction(
            string keyword,
            ITrackingSpan trackingSpan,
            IEnumerable<string> suggestedNameSegments,
            IEnumerable<string> commentSegments
        )
        {
            _trackingSpan = trackingSpan;

            _keyword = keyword;

            var name = String.Join(".", suggestedNameSegments);
            _newName = name;

            var comment = String.Join(" ", commentSegments);
            _comment = comment;

            DisplayText = $"{keyword} {name} {comment}".TrimEnd();
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
            existingTextBlock.Inlines.Add(new Run() { Text = _keyword, Foreground = _keywordBrush });
            existingTextBlock.Inlines.Add(new Run() { Text = " " });
            existingTextBlock.Inlines.Add(new Run() { Text = _newName, Foreground = _nameBrush });
            existingTextBlock.Inlines.Add(new Run() { Text = " " });
            existingTextBlock.Inlines.Add(new Run() { Text = _comment, Foreground = _commentBrush });

            var replacementTextBlock = new TextBlock
            {
                Background = _greenBrush,
                Padding = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.ExtraLight,
            };
            replacementTextBlock.Inlines.Add(new Run() { Text = "+", Foreground = _diffForegroundBrush, Background = _lightGreenBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = _keyword, Foreground = _keywordBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = " " });
            replacementTextBlock.Inlines.Add(new Run() { Text = _newName, Foreground = _nameBrush });
            replacementTextBlock.Inlines.Add(new Run() { Text = " " });
            replacementTextBlock.Inlines.Add(new Run() { Text = _comment, Foreground = _commentBrush });

            var stackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
            };
            stackPanel.Children.Add(existingTextBlock);
            stackPanel.Children.Add( replacementTextBlock );

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
