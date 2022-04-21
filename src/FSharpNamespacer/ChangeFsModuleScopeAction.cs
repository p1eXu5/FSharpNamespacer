using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace FSharpNamespacer
{
    internal class ChangeFsModuleScopeAction : ChangeFsModuleNameActionBase
    {
        public ChangeFsModuleScopeAction(ITrackingSpan trackingSpan, ModuleSuggestedActionSource.IFsModule fsModule)
            : base(trackingSpan)
        {
            if (fsModule.IsModule)
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
                DisplayText = $"Change module to {suggested} namespace";
                ReplacingText = $"namespace {suggested}";
            }
            else
            {
                var suggested = String.Join(".", fsModule.SuggestedFsModuleName);
                DisplayText = $"Change namespace to {suggested} module";
                ReplacingText = $"module {suggested}";
            }
        }

        public override string DisplayText { get; }

        protected override string ReplacingText { get; }
    }
}
