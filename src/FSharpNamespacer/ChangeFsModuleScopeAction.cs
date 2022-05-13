using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer
{
    internal class ChangeFsModuleScopeAction : ChangeFsModuleNameActionBase
    {
        public ChangeFsModuleScopeAction(ITrackingSpan trackingSpan, AsyncModuleSuggestedActionSource.IFsModule fsModule)
            : base(trackingSpan)
        {
            if (fsModule.IsModule)
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
