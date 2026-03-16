using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharpNamespacer.Utilities
{
    internal static class PathUtilities
    {
        internal static Queue<string> GetRelativePathSegments(string rootPath, string childItemPath)
        {
            var queue = new Queue<string>();

            // Get the directory of the root path (the .fsproj file)
            var rootDirectory = Path.GetDirectoryName(rootPath);

            // Normalize paths for comparison
            rootDirectory = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar);
            var fullChildPath = Path.GetFullPath(childItemPath);

            // Check if child is under root directory
            bool isUnderRoot = fullChildPath.StartsWith(rootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            if (isUnderRoot)
            {
                // Get the relative path from root
                var relativePath = fullChildPath.Substring(rootDirectory.Length + 1);

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
                // File is not under root directory, just use filename segments
                var fileName = Path.GetFileName(fullChildPath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var fileNameSegments = fileNameWithoutExtension.Split('.');

                foreach (var segment in fileNameSegments)
                {
                    queue.Enqueue(segment);
                }
            }

            return queue;
        }
    }
}
