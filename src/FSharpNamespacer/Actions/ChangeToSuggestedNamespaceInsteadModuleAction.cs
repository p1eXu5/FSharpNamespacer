using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`namespace SuggestedFsModuleName</c>
    /// </summary>
    internal sealed class ChangeToSuggestedNamespaceInsteadModuleAction : FsScopeActionBase
    {
        /// <summary>
        /// <inheritdoc cref="ChangeToSuggestedNamespaceInsteadModuleAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public ChangeToSuggestedNamespaceInsteadModuleAction(ITrackingSpan trackingSpan, IFsScope fsModule)
            : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
            DisplayText = $"namespace {suggested}";
            ReplacingText = $"namespace {suggested}";
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
