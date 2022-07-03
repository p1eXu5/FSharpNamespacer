using System;
using System.Linq;
using Microsoft.VisualStudio.Text;

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


    internal class FsScope : IFsScope
    {
        private bool? _isFileModuleName;
        private bool? _isNamespaceName;

        public FsScopeType FsScopeType { get; internal set; }
        public SnapshotSpan Range { get; internal set; }
        public int NameStartIndex { get; internal set; }
        public string[] FsModuleOrNamespaceName { get; internal set; }
        public string[] SuggestedFsModuleName { get; set; }

        public bool IsModuleScope => FsScopeType == FsScopeType.Module;
        public bool IsNotModuleScope => !IsModuleScope;

        public bool IsNamespaceScope => FsScopeType == FsScopeType.Namespace;
        public bool IsNotNamespaceScope => !IsNamespaceScope;

        public bool IsFileModuleName
        {
            get
            {
                if (_isFileModuleName.HasValue)
                {
                    return _isFileModuleName.Value;
                }

                _isFileModuleName = FsModuleOrNamespaceName.SequenceEqual(SuggestedFsModuleName);
                return _isFileModuleName.Value;
            }
        }

        public bool IsNotFileModuleName => !IsFileModuleName;

        public bool IsNamespaceName
        {
            get
            {
                if (_isNamespaceName.HasValue)
                {
                    return _isNamespaceName.Value;
                }

                _isNamespaceName = FsModuleOrNamespaceName.SequenceEqual(SuggestedFsModuleName.Take(SuggestedFsModuleName.Length - 1));
                return _isNamespaceName.Value;
            }
        }

        public bool IsNotNamespaceName => !IsNamespaceName;
    }

    [Obsolete("Deprecated")]
    internal class FsInvalidScope : FsScope
    {
    }

}
