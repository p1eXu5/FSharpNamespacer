using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using FSharpNamespacer;
using FSharpNamespacer.Actions;

namespace FSharpNamespacer.Actions
{
    internal class ChangeFsScopeAction : FsScopeActionBase
    {
        public ChangeFsScopeAction(ITrackingSpan trackingSpan, IFsScope fsModule)
            : base(trackingSpan)
        {
            if (FsScopeType.Module == fsModule.FsScopeType)
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
                DisplayText = $"Change to `namespace {suggested}`";
                ReplacingText = $"namespace {suggested}";
            }
            else
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
                DisplayText = $"Change to `module {suggested}`";
                ReplacingText = $"module {suggested}";
            }
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
