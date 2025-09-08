using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Models
{
    internal interface IFsScope
    {
        FsScopeType FsScopeType { get; }


        int NameStartIndex { get; }

        /// <summary>
        /// Existing module or namespace name.
        /// </summary>
        string[] FsModuleOrNamespaceName { get; }

        /// <summary>
        /// Suggested module name.
        /// </summary>
        string[] SuggestedFsModuleName { get; }
    }
}
