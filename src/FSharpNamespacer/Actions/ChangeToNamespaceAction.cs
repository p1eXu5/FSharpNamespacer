using System;
using System.Collections.Generic;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`namespace FsModuleOrNamespaceName</c>
    /// </summary>
    internal sealed class ChangeToNamespaceAction : FsScopeActionBase
    {
        /// <summary>
        /// <inheritdoc cref="ChangeToNamespaceAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public ChangeToNamespaceAction(
            ITrackingSpan trackingSpan,
            IEnumerable<string> suggestedNameSegments,
            IEnumerable<string> commentSegments
        )
            : base(trackingSpan)
        {
            var name = String.Join(".", suggestedNameSegments);
            var comment = String.Join(" ", commentSegments);
            string text = $"namespace {name} {comment}".TrimEnd();
            ReplacingText = DisplayText = text;
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
