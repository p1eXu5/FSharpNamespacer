using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace FSharpNamespacer.Utilities
{
    internal static class PathUtilities
    {
        internal static Queue<string> GetRelativePathSegments(string? rootPath, string childItemPath)
        {
            var queue = new Queue<string>();

            if (string.IsNullOrEmpty(childItemPath))
            {
                return queue;
            }

            if (rootPath is null)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(childItemPath);
                var fileNameSegments = fileNameWithoutExtension.Split('.');

                foreach (var segment in fileNameSegments)
                {
                    queue.Enqueue(segment);
                }

                return queue;
            }

            // Get the directory of the root path (the .fsproj file)
            var rootDirectory = Path.GetDirectoryName(rootPath);

            // Normalize paths for comparison
            rootDirectory = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar);
            var normalizedChildPath = Path.GetFullPath(childItemPath);

            // Check if child is under root directory
            bool isUnderRoot = normalizedChildPath.StartsWith(rootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            if (isUnderRoot)
            {
                // Add project name segments first (from the .fsproj filename)
                var projectFileName = Path.GetFileNameWithoutExtension(rootPath);
                var projectNameSegments = projectFileName.Split('.');
                foreach (var segment in projectNameSegments)
                {
                    queue.Enqueue(segment);
                }

                // Get the relative path from root
                var relativePath = normalizedChildPath.Substring(rootDirectory.Length + 1);

                // Split by both forward and backward slashes to handle both path formats
                var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Process all parts except the last one (directories)
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    queue.Enqueue(parts[i]);
                }

                // Process the last part (filename) - remove extension and split by dots
                var fileName = parts[parts.Length - 1];
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var fileNameSegments = fileNameWithoutExtension.Split('.');

                // If the first filename segment matches the last directory segment, skip it
                int startIndex = 0;
                if (parts.Length > 1 && fileNameSegments.Length > 0 &&
                    string.Equals(parts[parts.Length - 2], fileNameSegments[0], StringComparison.OrdinalIgnoreCase))
                {
                    startIndex = 1;
                }

                for (int i = startIndex; i < fileNameSegments.Length; i++)
                {
                    queue.Enqueue(fileNameSegments[i]);
                }
            }
            else
            {
                // File is not directly under root directory, look for common parent
                var rootParent = Path.GetDirectoryName(rootDirectory);
                rootParent = Path.GetFullPath(rootParent).TrimEnd(Path.DirectorySeparatorChar);

                bool isUnderRootParent = normalizedChildPath.StartsWith(rootParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

                if (isUnderRootParent)
                {
                    // Get the relative path from parent
                    var relativePath = normalizedChildPath.Substring(rootParent.Length + 1);
                    var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Process all parts except the last one (directories)
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        queue.Enqueue(parts[i]);
                    }

                    // Process the last part (filename) - remove extension and split by dots
                    var fileName = parts[parts.Length - 1];
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    var fileNameSegments = fileNameWithoutExtension.Split('.');

                    // If the first filename segment matches the last directory segment, skip it
                    int startIndex = 0;
                    if (parts.Length > 1 && fileNameSegments.Length > 0 &&
                        string.Equals(parts[parts.Length - 2], fileNameSegments[0], StringComparison.OrdinalIgnoreCase))
                    {
                        startIndex = 1;
                    }

                    for (int i = startIndex; i < fileNameSegments.Length; i++)
                    {
                        queue.Enqueue(fileNameSegments[i]);
                    }
                }
                else
                {
                    // File is not under root parent directory, just use filename segments
                    var fileName = Path.GetFileName(childItemPath);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    var fileNameSegments = fileNameWithoutExtension.Split('.');

                    foreach (var segment in fileNameSegments)
                    {
                        queue.Enqueue(segment);
                    }
                }
            }

            return queue;
        }
    }
}
