using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NupkgWrench
{
    public static class Util
    {
        public static void ApplyXSLT()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Wildcard pattern match
        /// </summary>
        public static bool IsMatch(string input, string wildcardPattern)
        {
            if (string.IsNullOrEmpty(wildcardPattern))
            {
                // Match everything if no pattern was given
                return true;
            }

            var regexPattern = Regex.Escape(wildcardPattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".");

            return Regex.IsMatch(input, $"^{wildcardPattern}$");
        }

        /// <summary>
        /// Convert inputs to a set of nupkg files.
        /// </summary>
        public static SortedSet<string> GetPackages(List<string> inputs)
        {
            var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in inputs)
            {
                var inputFile = Path.GetFullPath(input);

                if (File.Exists(inputFile))
                {
                    files.Add(inputFile);
                }
                else if (Directory.Exists(inputFile))
                {
                    var directoryFiles = Directory.GetFiles(inputFile, "*.nupkg", SearchOption.AllDirectories).ToList();

                    if (directoryFiles.Count < 1)
                    {
                        throw new FileNotFoundException($"Unable to find nupkgs in '{inputFile}'.");
                    }

                    files.UnionWith(directoryFiles);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to find '{inputFile}'.");
                }
            }

            return files;
        }
    }
}
