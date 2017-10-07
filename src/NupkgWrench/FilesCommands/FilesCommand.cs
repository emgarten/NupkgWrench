using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class FilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            var parentCommand = cmdApp.Command("files", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);

            ListFilesCommand.Register(parentCommand, log);
            AddFilesCommand.Register(parentCommand, log);
            RemoveFilesCommand.Register(parentCommand, log);
            EmptyFolderCommand.Register(parentCommand, log);
            CopySymbolsCommand.Register(parentCommand, log);
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