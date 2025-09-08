using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Models
{
    internal interface IFsScope
    {
        /// <summary>
        /// Module, namespace or undefined.
        /// </summary>
        FsScopeType FsScopeType { get; }

        /// <summary>
        /// Fist not whitespace character in a line with <see cref="FsModuleOrNamespaceName"/>.
        /// </summary>
        int NameStartIndex { get; }

        /// <summary>
        /// Existing module or namespace name.
        /// </summary>
        string[] FsModuleOrNamespaceName { get; }

        /// <summary>
        /// Suggested module name.
        /// </summary>
        string[] SuggestedFsModuleName { get; }

        ITextSnapshotLine TextSnapshotLine { get; }
    }
}
