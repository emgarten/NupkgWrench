using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class NuspecCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            var parentCommand = cmdApp.Command("nuspec", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);

            EditCommand.Register(parentCommand, log);
            ShowCommand.Register(parentCommand, log);
            ContentFilesCommand.Register(parentCommand, log);
            DependenciesCommand.Register(parentCommand, log);
            FrameworkAssembliesCommand.Register(parentCommand, log);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Nuspec commands.";

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                cmd.ShowHelp();

                return 0;
            });
        }
    }
}