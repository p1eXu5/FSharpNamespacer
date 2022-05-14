using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharpNamespacer.Actions
{
    internal class InsertFsNamespaceAction : FsScopeActionBase
    {
        public InsertFsNamespaceAction(ITrackingSpan trackingSpan, IFsScope fsModule) : base(trackingSpan)
        {
            var suggested = String.Join(".", fsModule.SuggestedFsModuleName.Take(fsModule.SuggestedFsModuleName.Length - 1));
            DisplayText = $"Insert `namespace {suggested}`";
            ReplacingText = $"namespace {suggested}";
        }

        public override string DisplayText { get; }
        protected override string ReplacingText { get; }
    }
}
