using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class AddFilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("add", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add a file to a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var pathOption = cmd.Option("-p|--path", "Path to add file at within the nupkg, this must contain the file name also.", CommandOptionType.SingleValue);
            var fileOption = cmd.Option("-f|--file", "Path on disk to the file that will be added to the nupkg.", CommandOptionType.SingleValue);
            var idFilter = cmd.Option("-i|--id", "Filter to only packages matching the id or wildcard.", CommandOptionType.SingleValue);
            var versionFilter = cmd.Option("-v|--version", "Filter to only packages matching the version or wildcard.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            var required = new List<CommandOption>()
            {
                pathOption,
                fileOption
            };

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                // Gather all package data
                var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, inputs.ToArray());

                // Validate parameters
                foreach (var requiredOption in required)
                {
                    if (!requiredOption.HasValue())
                    {
                        throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                    }
                }

                foreach (var nupkgPath in packages)
                {
                    using (var fileInput = File.OpenRead(fileOption.Value()))
                    {
                        Util.AddOrReplaceZipEntry(nupkgPath, pathOption.Value(), fileInput, log);
                    }
                }

                return 0;
            });
        }
    }
}
