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
    internal static class ExtractCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("extract", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Extract a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var output = cmd.Option("-o|--output", "Output folder", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Nupkg path",
                multipleValues: false);

            var required = new List<CommandOption>()
            {
                output
            };

            cmd.OnExecute(() =>
            {
                // Validate parameters
                foreach (var requiredOption in required)
                {
                    if (!requiredOption.HasValue())
                    {
                        throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                    }
                }

                var nupkgPath = argRoot.Value;

                if (string.IsNullOrEmpty(nupkgPath))
                {
                    throw new ArgumentException("Specify the path to a nupkg.");
                }

                Directory.CreateDirectory(output.Value());

                using (var stream = File.OpenRead(nupkgPath))
                using (var zip = new ZipArchive(stream))
                {
                    log.LogMinimal($"Extracting {nupkgPath} -> {output.Value()}");
                    
                    foreach (var entry in zip.Entries)
                    {
                        var path = Path.Combine(output.Value(), entry.FullName);

                        log.LogInformation($"writing {path}");

                        using (var entryStream = entry.Open())
                        using (var outputStream = File.Create(path))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }

                return 0;
            });
        }
    }
}
