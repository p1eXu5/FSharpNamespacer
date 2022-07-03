using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`namespace SuggestedFsModuleName[..^1]</c>
    /// </summary>
    internal class ChangeToNamespaceAction : FsScopeActionBase
    {
        /// <summary>
        /// <inheritdoc cref="ChangeToNamespaceAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public ChangeToNamespaceAction(ITrackingSpan trackingSpan, IFsScope fsModule)
            : base(trackingSpan)
        {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
                DisplayText = $"namespace {suggested}";
                ReplacingText = $"namespace {suggested}";
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
