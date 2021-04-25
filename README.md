# What is NupkgWrench?

NupkgWrench is a cross platform command line tool for listing and modifying nupkgs and nuspecs. 

The jump between automatically packing a csproj and building a nupkg from scratch is difficult. NupkgWrench lets you modify nupkgs, make simple changes such as add/remove files, or update dependencies without having to author every part of the package.

NupkgWrench also makes it easy to search for and list nupkgs, filtering on id/version. Incorporate NupkgWrench into your scripts to avoid casing and SemanticVersion problems such as 1.0.0 vs 1.0.0.0. NupkgWrench uses NuGet client libraries and handles packages in the same way as NuGet.exe.

## Getting NupkgWrench

### Manually getting nupkgwrench.exe (Windows and Mono)
1. Download the latest nupkg from [NupkgWrenchExe on NuGet.org](https://www.nuget.org/packages/NupkgWrenchExe)
1. Extract *tools/NupkgWrench.exe* to a local folder and run it.

### NuGet.exe install
1. *nuget.exe install NupkgWrenchExe -ExcludeVersion -Source https://api.nuget.org/v3/index.json*
1. Run *NupkgWrenchExe/tools/NupkgWrench.exe*

### Install dotnet global tool (recommended)
1. `dotnet tool install -g nupkgwrench`
1. `nupkgwrench` should now be on your *PATH*

## Build Status

| AppVeyor | Travis | Visual Studio Online |
| --- | --- | --- |
| [![AppVeyor](https://ci.appveyor.com/api/projects/status/jovo9wvxbqgws4ob?svg=true)](https://ci.appveyor.com/project/emgarten/nupkgwrench) | [![Travis](https://travis-ci.com/emgarten/NupkgWrench.svg?branch=main)](https://travis-ci.com/emgarten/NupkgWrench) | [![VSO](https://hackamore.visualstudio.com/_apis/public/build/definitions/abbff132-0981-4267-a80d-a6e7682a75a9/3/badge)](https://github.com/emgarten/nupkgwrench) |

## CI builds

CI builds are located on the following NuGet feed:

``https://nuget.blob.core.windows.net/packages/index.json``

The list of packages on this feed is [here](https://nuget.blob.core.windows.net/packages/sleet.packageindex.json).

## Features
* Change the version, release label, or convert packages from pre-release to stable across a folder of nupkgs. NupkgWrench will modify all dependency version ranges to match the new package versions.
* Simple commands write nupkg metadata to the console to allow scripts to read the id, version, nuspec, and files in a nupkg.
* Add/edit nuspec properties from the command line.
* Add contentFiles entries to existing packages.
* Add empty folders with ``_._`` and empty dependency groups multiple target framework scenarios.
* Search folders for packages using wildcard patterns for ids and version, NupkgWrench handles non-normalized versions such as 1.0.01 where constructing the file name manually is error prone.
* Validate nupkgs

# Commands

| Command            | Description |
| ------------------ | ----------- |
| ``compress`` | Create a nupkg from a folder. |
| ``extract``  | Extract a nupkg to a folder. |
| ``files add`` | Add a file to a nupkg.
| ``files emptyfolder`` | Add an empty folder _._ placeholder to a nupkg, existing files in the folder will be removed. |
| ``files list`` | List files inside a nupkg. |
| ``files remove`` | Remove files from a nupkg. |
| ``nuspec contentfiles add`` | Add a contentFiles entry in nuspec. |
| ``nuspec dependencies clear`` | Clear all dependencies or a set of target framework group dependencies. |
| ``nuspec dependencies emptygroup`` | Add empty dependency groups or remove dependencies from existing groups. |
| ``nuspec dependencies add`` | Add a nuspec package dependency. |
| ``nuspec dependencies remove`` | Remove a package dependency. |
| ``nuspec dependencies modify`` | Modify a dependency or all package dependencies. Change the version, include, or exclude properties. |
| ``nuspec edit`` | Modifies or adds a top level property to the nuspec in a package. |
| ``nuspec frameworkassemblies clear`` | Clear all framework assemblies from a nuspec file. |
| ``nuspec show`` | Display the XML contents of a nuspec file from a package. |
| ``id`` | Display the package id of a nupkg. |
| ``list`` | Lists all nupkgs or nupkgs matching the id and version filters. |
| ``release`` | Convert a set of pre-release packages to stable or the specified version/release label. Package dependencies will also be modified to match. Defaults to stable. |
| ``updatefilename`` | Update the file name of a package to match the id and version in the nuspec. |
| ``validate`` | Verify a nupkg can be read using NuGet's package reader. |
| ``version`` | Display the package version of a nupkg. |

# Contributing

Need a new command? Send a pull request! We're happy to accept new commands and fixes.

# Quick start

### Inspecting a nupkg

NupkgWrench contains several basic commands for retrieving metadata from nupkgs. This info is written out to the console without any extra data to make it easy for scripts to read and parse the data.

```
> NupkgWrench id packageA.1.0.0.nupkg
packageA

> NupkgWrench version packageA.1.0.0.nupkg
1.0.0

> NupkgWrench files list packageA.1.0.0.nupkg
packageA.nuspec
lib/net45/a.dll
...

> NupkgWrench nuspec show packageA.1.0.0.nupkg > nuspec.xml
```

### Bulk editting packages and filters

Most commands for modifying packages can take a set of file, directory paths, and file globbing patterns. Options are provided to filter on id, version, and symbol packages. This let's NupkgWrench do the work of finding the right packages.

The list command makes it easy to see what an edit command will operate on, it simply lists all matching nupkg files that meet the filter criteria.

Filters: ``--id``, ``--version``, ``--exclude-symbols``, ``--highest-version``

```
> NupkgWrench list c:\nupkgs c:\nupkgs2 c:\nupkgs3\packageA.1.0.0.nupkg d:\morenupkgs
c:\nupkgs\packageX.2.0.0-beta.nupkg
c:\nupkgs3\packageA.1.0.0.nupkg
d:\morenupkgs\packageZ.3.0.0-beta.1.2.nupkg

> NupkgWrench list c:\nupkgs c:\nupkgs2 c:\nupkgs3\packageA.1.0.0.nupkg d:\morenupkgs --id packageX --version 2.0.0-beta
c:\nupkgs\packageX.2.0.0-beta.nupkg
```

Both the id and version filters may contain wildcards. For versions the wildcards are applied to both the exact version string in the package nuspec and the normalized version, making it easy to work with package versions that contain unneeded leading zeros.

```
> NupkgWrench list c:\nupkgs c:\nupkgs2 c:\nupkgs3\packageA.1.0.0.nupkg d:\morenupkgs --id p*age* --version *-beta*
c:\nupkgs\packageX.2.0.0-beta.nupkg
d:\morenupkgs\packageZ.3.0.0-beta.1.2.nupkg

> NupkgWrench list c:\nupkgs c:\nupkgs2 c:\nupkgs3\packageA.1.0.0.nupkg d:\morenupkgs --version 1.0
c:\nupkgs3\packageA.1.0.0.nupkg
```

File globbing may be used along with other filters, the ``**`` and ``*`` patterns can be used to search all sub directories and partial file names. Filters may be applied on top of these globbing patterns to give better control over the selected nupkgs.

```
> NupkgWrench list c:\work\**\packageX*2.*.nupkg --highest-version
c:\work\nupkgs\packageX.2.0.0-beta.nupkg

> NupkgWrench list c:\work\**\*2.*.nupkg --highest-version --id packageX
c:\work\nupkgs\packageX.2.0.0-beta.nupkg
```

### Convert to release

Converting a package from a pre-release version to a stable version, or just changing the version to another version completely can be a tedious task. NupkgWrench automates this by first updating the version of each package passed in, then updating all dependency version ranges that contained the previous versions. 

```
> NupkgWrench release c:\nupkgs c:\nupkgs2
processing c:\nupkgs\packageA.2.0.0-beta.nupkg
packageA.2.0.0-beta -> packageA.2.0.0
dependency packageB [1.0.0-beta, ) -> [1.0.0, )
c:\nupkgs\packageA.2.0.0-beta.nupkg -> packageA.2.0.0.nupkg
processing c:\nupkgs\packageB.1.0.0-beta.nupkg
packageB.1.0.0-beta -> packageB.1.0.0
c:\nupkgs\packageB.1.0.0-beta.nupkg -> packageB.1.0.0.nupkg
```

``--label`` can be used to keep the version number the same while only updating or adding a pre-release label.

```
> NupkgWrench release c:\nupkgs c:\nupkgs2 --label rc1
processing c:\nupkgs\packageA.2.0.0-beta.nupkg
packageA.2.0.0-beta -> packageA.2.0.0-rc1
dependency packageB [1.0.0-beta, ) -> [1.0.0-rc1, )
c:\nupkgs\packageA.2.0.0-beta.nupkg -> packageA.2.0.0-rc1.nupkg
processing c:\nupkgs\packageB.1.0.0-beta.nupkg
packageB.1.0.0-beta -> packageB.1.0.0-rc1
c:\nupkgs\packageB.1.0.0-beta.nupkg -> packageB.1.0.0-rc1.nupkg
```

### Set a non-normalized nupkg version

NuGet normalizes package versions to three parts. To convert back to a four part version number use the ``release`` command with `--four-part-version`.
This command can be used to revert version normalization done in NuGet pack. Since nupkgwrench can work on a directory of nupkgs or filter by id, this provides an easy way to update package versions without needing to discover the original version of the nupkg first in build scripts.

```
> NupkgWrench release c:\nupkgs --four-part-version
processing c:\nupkgs\packageA.2.0.0.nupkg
packageA.2.0.0 -> packageA.2.0.0.0
c:\nupkgs\packageA.2.0.0.nupkg -> packageA.2.0.0.0.nupkg
```

``--new-version`` allows setting the version directly to apply any non-normalized version change.

```
> NupkgWrench release c:\nupkgs --new-version 1.00
processing c:\nupkgs\packageA.1.0.0.nupkg
packageA.1.0.0 -> packageA.1.00
c:\nupkgs\packageA.1.0.0.nupkg -> packageA.1.00.nupkg
```

### Adding after pack

NuGet.exe pack and dotnet pack typically create a nupkg with everything needed, but for advanced scenarios it is sometimes required to pack using a nuspec file. To avoid creating a nupkg with all the needed dependencies and files NupkgWrench can be used to make simple changes on top of existing nupkgs.

Adding a contentFiles entry from the command line:
```
> NupkgWrench nuspec contentfiles add c:\nupkgs --include **/*.* --build-action none --copy-to-output true
```

Adding an ``_._`` file to make a nupkg compatible with additional frameworks.
```
> NupkgWrench files emptygroup c:\nupkgs --path lib/net45/_._
> NupkgWrench nuspec dependencies emptygroup c:\nupkgs --framework net45
``` 

## Coding
This solution uses .NET Core, get the tools [here](http://dot.net/).

### License
[MIT License](https://github.com/emgarten/NupkgWrench/blob/main/LICENSE.md)

# Related projects 

NupkgWrench for VSTS/TFS [NupgkWrenchExtension](https://github.com/dschuermans/NupgkWrenchExtension)
