using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer.Actions
{
    /// <summary>
    /// Suggestion: <c>`module SuggestedFsModuleName</c>
    /// </summary>
    internal class RenameToFileModuleAction : FsScopeActionBase
    {
        /// <summary>
        /// <inheritdoc cref="ChangeToNamespaceAction"/>
        /// </summary>
        /// <param name="trackingSpan"></param>
        /// <param name="fsModule"></param>
        public RenameToFileModuleAction(ITrackingSpan trackingSpan, IFsScope fsModule)
            : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
            DisplayText = $"module {suggested}";
            ReplacingText = $"module {suggested}";
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
