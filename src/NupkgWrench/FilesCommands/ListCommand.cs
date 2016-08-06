using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class ListCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("list", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "List files inside a nupkg.";
            cmd.HelpOption(Constants.HelpOption);

            var argRoot = cmd.Argument(
                "[root]",
                "Nupkg path",
                multipleValues: false);

            cmd.OnExecute(() =>
            {
                var nupkgPath = argRoot.Value;

                if (string.IsNullOrEmpty(nupkgPath))
                {
                    throw new ArgumentException("Specify the path to a nupkg.");
                }

                using (var stream = File.OpenRead(nupkgPath))
                using (var zip = new ZipArchive(stream))
                {
                    foreach (var entry in zip.Entries.Select(e => e.FullName).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                    {
                        log.LogMinimal(entry);
                    }
                }

                return 0;
            });
        }
    }
}
