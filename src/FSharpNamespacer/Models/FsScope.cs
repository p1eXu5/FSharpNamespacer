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
        public SnapshotSpan Range { get; private set; }
        public int NameStartIndex { get; private set; }
        public string[] FsModuleOrNamespaceName { get; private set; }
        public string[] SuggestedFsModuleName { get; private set; } = Array.Empty<string>();

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
            fsScope = default;

            if (range.IsEmpty && range.Start == 0)
            {
                fsScope =
                new FsScope
                {
                    FsScopeType = FsScopeType.Undefined,
                    Range = range,
                    NameStartIndex = 0,
                    FsModuleOrNamespaceName = Array.Empty<string>(),
                };
                return true;
            }

            // get first line in selection
            string line = range.Snapshot.Lines.First(l => l.Start <= range.Start && range.Start < l.End).GetText();

            bool isModule = line.StartsWith("module"); // ignore trailing spaces
            bool isNamespace = line.StartsWith("namespace"); // ignore trailing spaces


            if (!(isModule || isNamespace) || line.Contains("="))
            {
                return false;
            }

            // define module name start position
            int nameStartIndex =
                isModule
                    ? "module".Length
                    : "namespace".Length;

            while (Char.IsWhiteSpace(line[nameStartIndex++])) ;
            --nameStartIndex;

            // define name segments
            string[] nameSegments =
                line
                    .Substring(nameStartIndex)
                    .Split('.')
                    .Select(s => s.Trim())
                    .Where(s => !String.IsNullOrEmpty(s))
                    .ToArray();

            fsScope =
                new FsScope
                {
                    FsScopeType = isModule ? FsScopeType.Module : FsScopeType.Namespace,
                    Range = range,
                    NameStartIndex = nameStartIndex,
                    FsModuleOrNamespaceName = nameSegments
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
