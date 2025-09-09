using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace FSharpNamespacer.Models
{
    internal sealed class FsScope : IFsScope
    {
        private bool? _isFileModuleName;
        private bool? _isNamespaceName;
        
        private FsScope()
        { }

        public FsScopeType FsScopeType { get; private set; }
        
        public int NameStartIndex { get; private set; }

        public string[] FsModuleOrNamespaceName { get; private set; }
        
        public string[] SuggestedFsModuleName { get; private set; } = Array.Empty<string>();

        public ITextSnapshotLine TextSnapshotLine { get; private set; }

        public bool IsModuleScope => FsScopeType == FsScopeType.Module;

        public bool IsNotModuleScope => !IsModuleScope;

        public bool IsNamespaceScope => FsScopeType == FsScopeType.Namespace;

        public bool IsNotNamespaceScope => !IsNamespaceScope;

        public bool IsNameEqualToSuggestedModuleName
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

        public bool IsNameNotEqualToSuggestedModuleName => !IsNameEqualToSuggestedModuleName;

        public bool IsNameEqualToSuggestedNamespaceName
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

        public bool IsNameNotEqualToSuggestedNamespaceName => !IsNameEqualToSuggestedNamespaceName;

        /// <summary>
        /// Creates <see cref="FsScope"/> if file is newly created and empty or
        /// <paramref name="range"/> refers to top level "module" or "namespace".
        /// </summary>
        /// <param name="range"> A span of text in the <see cref="ITextBuffer"/> over which to check for suggested actions. </param>
        /// <param name="fsScope"></param>
        /// <returns></returns>
        internal static bool TryCreate(SnapshotSpan range, out FsScope fsScope)
        {
            /*
             * In VS Version 17.14.13 F# quick action is triggered on current position
             * and range length is equal to 1 (as when "Quick Actions and Refactorings..." is chosen from context menu).
             * 
             * In testing IDE and earlier versions when `Ctrl + .` is triggering "Quick Actions and Refactorings..."
             * range contains whole line.
             */

            fsScope = default;

            if (range.IsEmpty && range.Start == 0)
            {
                fsScope =
                    new FsScope
                    {
                        FsScopeType = FsScopeType.Undefined,
                        NameStartIndex = 0,
                        FsModuleOrNamespaceName = Array.Empty<string>(),
                        TextSnapshotLine = range.Snapshot.Lines.First(),
                    };
                return true;
            }

            // get first line in selection
            ITextSnapshotLine line = range.Snapshot.Lines.First(l => l.Start <= range.Start && (range.Start < l.EndIncludingLineBreak));
            string lineText = line.GetText();

            bool isModule = lineText.StartsWith("module"); // ignore trailing spaces
            bool isNamespace = lineText.StartsWith("namespace"); // ignore trailing spaces

            // skip suggestion if inner module
            // TODO: Add suggestion to transform file-scoped module/namespace to inner module and back
            if (!(isModule || isNamespace) || lineText.Contains("="))
            {
                return false;
            }

            // define module name start position
            int nameStartIndex =
                isModule
                    ? "module".Length
                    : "namespace".Length;

            while (Char.IsWhiteSpace(lineText[nameStartIndex++]));
            --nameStartIndex;

            // define name segments
            string[] nameSegments =
                lineText
                    .Substring(nameStartIndex)
                    .Split('.')
                    .Select(s => s.Trim())
                    .Where(s => !String.IsNullOrEmpty(s))
                    .ToArray();

            fsScope =
                new FsScope
                {
                    FsScopeType = isModule ? FsScopeType.Module : FsScopeType.Namespace,
                    NameStartIndex = nameStartIndex,
                    FsModuleOrNamespaceName = nameSegments,
                    TextSnapshotLine = line,
                };

            return true;
        }

        internal bool TrySetSuggestedFsModuleName(string fsProjectFilePath, string fsFilePath)
        {
            string[] filePathUriSegments = new Uri(fsFilePath, UriKind.Absolute).Segments; // include .fs file
            string[] projectPathUriSegments = new Uri(fsProjectFilePath, UriKind.Absolute).Segments; // include .fsproj file

            if (projectPathUriSegments.Length > filePathUriSegments.Length)
            {
                return false;
            }

            int i = 0;

            while (projectPathUriSegments[i] == filePathUriSegments[i]) ++i;

            var suggestedFsModuleNameSegments =
                new[] { Path.GetFileNameWithoutExtension(fsProjectFilePath) }
                    .Concat(filePathUriSegments.Skip(i).Take(filePathUriSegments.Length - i - 1))
                    .Select(s => s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar));

            var fileNameSuggestedSegments =
                Path.GetFileNameWithoutExtension(filePathUriSegments[filePathUriSegments.Length - 1]).Split('.');


            SuggestedFsModuleName =
                (
                    suggestedFsModuleNameSegments.Last() == fileNameSuggestedSegments.First()
                        ? suggestedFsModuleNameSegments.Concat(fileNameSuggestedSegments.Skip(1))
                        : suggestedFsModuleNameSegments.Concat(fileNameSuggestedSegments)
                )
                .ToArray();

            return true;
        }
    }
}
