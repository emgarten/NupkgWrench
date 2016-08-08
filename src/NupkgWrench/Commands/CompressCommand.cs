using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class CompressCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("compress", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Create a nupkg from a folder.";
            cmd.HelpOption(Constants.HelpOption);
            var output = cmd.Option("-o|--output", "Output folder, the nupkg will be added here.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Nupkg folder path",
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

                // Normalize dir ending
                var inputFolder = argRoot.Value.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
                {
                    throw new ArgumentException("Specify the path to a folder containg nupkg files.");
                }

                Directory.CreateDirectory(output.Value());

                var folderReader = new PackageFolderReader(inputFolder);
                var identity = folderReader.GetIdentity();

                var nupkgName = $"{identity.Id}.{identity.Version.ToString()}.nupkg";
                var outputPath = Path.Combine(output.Value(), nupkgName);

                log.LogMinimal($"compressing {inputFolder} -> {outputPath}");

                using (var stream = File.Create(outputPath))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories))
                    {
                        log.LogInformation($"adding {file}");

                        var zipPath = file.Substring(inputFolder.Length).Replace("\\", "/");
                        var entry = zip.CreateEntry(zipPath, CompressionLevel.Optimal);

                        using (var entryStream = entry.Open())
                        using (var inputStream = File.OpenRead(file))
                        {
                            inputStream.CopyTo(entryStream);
                        }
                    }
                }

                return 0;
            });
        }
    }
}
