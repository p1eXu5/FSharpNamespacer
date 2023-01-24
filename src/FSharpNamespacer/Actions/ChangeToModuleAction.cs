using System;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`module FsModuleOrNamespaceName</c>
    /// </summary>
    internal sealed class ChangeToModuleAction : FsScopeActionBase
    {
        /// <summary>
        /// <inheritdoc cref="ChangeToModuleAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public ChangeToModuleAction(ITrackingSpan trackingSpan, IFsScope fsModule)
            : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.FsModuleOrNamespaceName);
            DisplayText = $"module {suggested}";
            ReplacingText = $"module {suggested}";
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
