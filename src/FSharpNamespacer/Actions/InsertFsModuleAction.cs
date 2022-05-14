using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;

namespace FSharpNamespacer.Actions
{
    internal class InsertFsModuleAction : FsScopeActionBase
    {
        public InsertFsModuleAction(ITrackingSpan trackingSpan, IFsScope fsModule) : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
            DisplayText = $"Insert `module {suggested}`";
            ReplacingText = $"module {suggested}";
        }

        public override string DisplayText { get; }
        protected override string ReplacingText { get; }
    }
}
