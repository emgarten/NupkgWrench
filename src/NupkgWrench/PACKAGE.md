# NupkgWrench

NupkgWrench is a cross platform command line tool for listing and modifying nupkgs and nuspecs. Change package versions, edit nuspec metadata and dependencies, add or remove files, and find packages in folders without repacking them from scratch.

## Install

NupkgWrench is a .NET tool and requires the .NET 8 SDK or later.

```
dotnet tool install -g nupkgwrench
```

In CI scripts, pin a major version so a new major release can't change your build unexpectedly:

```
dotnet tool install -g nupkgwrench --version "5.*"
```

To pin an exact version for a repository, use a [local tool manifest](https://learn.microsoft.com/dotnet/core/tools/local-tools-how-to-use):

```
dotnet new tool-manifest
dotnet tool install nupkgwrench
```

Commit the *dotnet-tools.json* file it creates, then run `dotnet tool restore` and use `dotnet nupkgwrench`.

With the .NET 10 SDK or later, [dnx](https://learn.microsoft.com/dotnet/core/tools/dotnet-tool-exec) runs NupkgWrench without installing it:

```
dnx nupkgwrench list ./nupkgs
```

## Quick start

```
# Display the id, version, and files of a package
nupkgwrench id packageA.1.0.0-beta.nupkg
nupkgwrench version packageA.1.0.0-beta.nupkg
nupkgwrench files list packageA.1.0.0-beta.nupkg

# List the packages in a folder that match an id filter
nupkgwrench list ./nupkgs --id "packageA*"

# Convert pre-release packages to stable and update dependency ranges to match
nupkgwrench release ./nupkgs
```

## Documentation

* [Commands and examples](https://github.com/emgarten/NupkgWrench#commands)
* [Release notes](https://github.com/emgarten/NupkgWrench/blob/main/ReleaseNotes.md)

Source code and issues: [github.com/emgarten/NupkgWrench](https://github.com/emgarten/NupkgWrench)
