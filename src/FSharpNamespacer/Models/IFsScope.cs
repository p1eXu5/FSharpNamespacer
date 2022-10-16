using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Models
{
    internal interface IFsScope
    {
        FsScopeType FsScopeType { get; }
        SnapshotSpan Range { get; }
        int NameStartIndex { get; }
        string[] FsModuleOrNamespaceName { get; }
        string[] SuggestedFsModuleName { get; }
    }
}
