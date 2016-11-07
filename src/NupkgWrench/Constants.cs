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

        internal const string HighestVersionFilterTemplate = "--highest-version";
        internal const string HighestVersionFilterDesc = "Filter to only the highest version for a package id.";

        internal const string SinglePackageRootDesc = "Path to a package, directory containing, or a file globbing pattern that resolves to a single package (ex: path/**/*.nupkg).";
        internal const string MultiplePackagesRootDesc = "Paths to individual packages, directories containing packages, or file globbing patterns (ex: path/**/*.nupkg). Multiple values may be passed in.";
    }
}