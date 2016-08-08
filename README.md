# NupkgWrench

NupkgWrench is a cross platform command line tool for listing and modifying nupkgs and nuspecs.

## Build Status

| AppVeyor | Travis |
| --- | --- |
| [![AppVeyor](https://ci.appveyor.com/api/projects/status/jovo9wvxbqgws4ob?svg=true)](https://ci.appveyor.com/project/emgarten/nupkgwrench) | [![Travis](https://travis-ci.org/emgarten/NupkgWrench.svg?branch=master)](https://travis-ci.org/emgarten/NupkgWrench) |

## Getting NupkgWrench

* [Github releases](https://github.com/emgarten/NupkgWrench/releases/latest)
* [NuGet package](https://www.nuget.org/packages/NupkgWrench)

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

# Quick start

Download the latest release from [github](https://github.com/emgarten/NupkgWrench/releases/latest).
