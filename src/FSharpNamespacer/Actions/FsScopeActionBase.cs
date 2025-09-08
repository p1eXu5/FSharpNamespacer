using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace FSharpNamespacer.Actions
{
    public abstract class FsScopeActionBase : ISuggestedAction
    {
        private readonly ITrackingSpan _trackingSpan;
        private readonly ITextSnapshot _snapshot;

        protected FsScopeActionBase(ITrackingSpan trackingSpan)
        {
            _trackingSpan = trackingSpan;
            _snapshot = trackingSpan.TextBuffer.CurrentSnapshot;
        }

        public bool HasActionSets { get; } = false;

        /// <summary>
        /// Used in context menu.
        /// </summary>
        public abstract string DisplayText { get; }

        public ImageMoniker IconMoniker { get; } = default;

        public string IconAutomationText { get; } = null;

        public string InputGestureText { get; } = null;

        public bool HasPreview { get; } = true;

        /// <summary>
        /// Using for preview and replacement.
        /// </summary>
        protected abstract string ReplacingText { get; }

        public void Dispose()
        {
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = ReplacingText } );
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            _trackingSpan.TextBuffer.Replace(_trackingSpan.GetSpan(_snapshot), ReplacingText);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample action and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
