# NupkgWrench

NupkgWrench is a cross platform command line tool for listing and modifying nupkgs and nuspecs.

## Build Status

| AppVeyor | Travis |
| --- | --- |
| [![AppVeyor](https://ci.appveyor.com/api/projects/status/jovo9wvxbqgws4ob?svg=true)](https://ci.appveyor.com/project/emgarten/nupkgwrench) | [![Travis](https://travis-ci.org/emgarten/NupkgWrench.svg?branch=master)](https://travis-ci.org/emgarten/NupkgWrench) |

## Getting NupkgWrench

* [Github releases](https://github.com/emgarten/NupkgWrench/releases/latest)
* [NuGet package](https://www.nuget.org/packages/NupkgWrench)
* [Nightly nupkg on myget](https://www.myget.org/F/nupkgwrench/api/v2/package/NupkgWrench/)
* [Nightly builds on appveyor](https://ci.appveyor.com/project/emgarten/nupkgwrench/build/artifacts)

## Features
* Change the version, release label, or convert packages from pre-release to stable across a folder of nupkgs. NupkgWrench will modify all dependency version ranges to match the new package versions.
* Simple commands write nupkg metadata to the console to allow scripts to read the id, version, nuspec, and files in a nupkg.
* Add/edit nuspec properties from the command line.
* Add contentFiles entries to existing packages.
* Add empty folders with ``_._`` and empty dependency groups multiple target framework scenarios.
* Search folders for packages using wildcard patterns for ids and version, NupkgWrench handles non-normalized versions such as 1.0.01 where constructing the file name manually is error prone.
* Validate nupkgs

## Coding
This solution uses .NET Core, get the tools [here](http://dot.net/).

### License
[MIT License](https://github.com/emgarten/NupkgWrench/blob/master/LICENSE.md)

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
| ``nuspec edit`` | Modifies or adds a top level property to the nuspec in a package. |
| ``nuspec frameworkassemblies clear`` | Clear all framework assemblies from a nuspec file. |
| ``nuspec show`` | Display the XML contents of a nuspec file from a package. |
| ``id`` | Display the package id of a nupkg. |
| ``list`` | Lists all nupkgs or nupkgs matching the id and version filters. |
| ``release`` | Convert a set of pre-release packages to stable or the specified version/release label. Package dependencies will also be modified to match. Defaults to stable. |
| ``updatefilename`` | Update the file name of a package to match the id and version in the nuspec. |
| ``validate`` | Verify a nupkg can be read using NuGet's package reader. |
| ``version`` | Display the package version of a nupkg. |

# Quick start

Download the latest release from [github](https://github.com/emgarten/NupkgWrench/releases/latest).

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
