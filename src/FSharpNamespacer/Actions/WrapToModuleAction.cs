using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

#nullable enable

namespace FSharpNamespacer.Actions
{
    internal sealed class WrapToModuleAction
    {
        private readonly ITrackingSpan _trackingSpan;

        /// <summary>
        /// <inheritdoc cref="ChangeLineAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public WrapToModuleAction(
            ITrackingSpan trackingSpan,
            string keyword,
            IEnumerable<string> suggestedNameSegments,
            IEnumerable<string> commentSegments
        )
        {
            _trackingSpan = trackingSpan;

            var name = String.Join(".", suggestedNameSegments);
            var comment = String.Join(" ", commentSegments);
            string text = $"{keyword} {name} {comment}".TrimEnd();
            DisplayText = text;
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
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = DisplayText });

            return Task.FromResult<object?>(textBlock);
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
