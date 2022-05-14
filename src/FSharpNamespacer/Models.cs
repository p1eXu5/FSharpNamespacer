using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharpNamespacer.Models
{
    internal enum FsScopeType
    {
        Undefined,
        Module,
        Namespace,
    }
    

    internal interface IFsScope
    {
        FsScopeType FsScopeType { get; }
        SnapshotSpan Range { get; }
        int NameStartIndex { get; }
        string[] FsModuleOrNamespaceName { get; }
        string[] SuggestedFsModuleName { get; set; }
    }


    internal class FsScopeBase : IFsScope
    {
        public FsScopeType FsScopeType { get; internal set; }
        public SnapshotSpan Range { get; internal set; }
        public int NameStartIndex { get; internal set; }
        public string[] FsModuleOrNamespaceName { get; internal set; }
        public string[] SuggestedFsModuleName { get; set; }
    }

    internal class FsInvalidScope : FsScopeBase
    {
    }

}
