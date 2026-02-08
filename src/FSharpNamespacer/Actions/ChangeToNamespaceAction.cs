using System;
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
        public ChangeToNamespaceAction(ITrackingSpan trackingSpan, IFsFileRootScope fsModule)
            : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.FsModuleOrNamespaceName);
            DisplayText = $"namespace {suggested}";
            ReplacingText = $"namespace {suggested}";
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
