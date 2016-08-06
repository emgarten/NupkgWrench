using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class FilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            var parentCommand = cmdApp.Command("files", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);

            ListCommand.Register(parentCommand, log);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Nupkg file commands.";

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                cmd.ShowHelp();

                return 0;
            });
        }
    }
}
