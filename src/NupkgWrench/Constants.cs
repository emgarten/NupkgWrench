namespace NupkgWrench
{
    internal class Constants
    {
        internal const string HelpOption = "-h|--help";

        internal const string ExcludeSymbolsTemplate = "--exclude-symbols";
        internal const string ExcludeSymbolsDesc = "Filter out symbol packages.";

        internal const string VersionFilterTemplate = "-v|--version";
        internal const string VersionFilterDesc = "Filter to only packages matching the version or wildcard.";

        internal const string IdFilterTemplate = "-i|--id";
        internal const string IdFilterDesc = "Filter to only packages matching the id or wildcard.";
    }
}