using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using FSharpNamespacer;
using FSharpNamespacer.Actions;

namespace FSharpNamespacer.Actions
{
    internal class RenameFsScopeNameAction : FsScopeActionBase
    {
        public RenameFsScopeNameAction(ITrackingSpan trackingSpan, FsInvalidScope fsModule)
            : base(trackingSpan)
        {
            if (FsScopeType.Module == fsModule.FsScopeType)
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
                DisplayText = $"Rename to {suggested}";
                ReplacingText = $"module {suggested}";
            }
            else
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
                DisplayText = $"Rename to {suggested}";
                ReplacingText = $"namespace {suggested}";
            }
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
