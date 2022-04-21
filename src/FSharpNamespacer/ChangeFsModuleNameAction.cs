using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer
{
    internal class ChangeFsModuleNameAction : ChangeFsModuleNameActionBase
    {
        public ChangeFsModuleNameAction(ITrackingSpan trackingSpan, ModuleSuggestedActionSource.FsSuggested fsModule)
            : base(trackingSpan)
        {
            if (fsModule.IsModule)
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
                DisplayText = $"Change module name to {suggested}";
                ReplacingText = $"module {suggested}";
            }
            else
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
                DisplayText = $"Change namespace name to {suggested}";
                ReplacingText = $"namespace {suggested}";
            }
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}