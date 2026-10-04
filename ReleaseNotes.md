# Release Notes

## 5.1.0
* `extract` now fails without writing any files when a package contains entries with absolute paths or paths that resolve outside of the output folder
* Fixed `extract` failing on packages that contain directory entries, such as `lib/`
* Fixed `compress` failing with an unclear error when no folder is given
* Fixed `release` updating some packages before failing on a later one. All packages are now processed and checked before any are modified, and `release` fails when an output file already exists or when output file names differ only by case
* Fixed `release` failing on invalid dependency ranges, such as `[2.0.0, 1.0.0]`. These ranges are now skipped with a warning
* Fixed `release` not updating dependency ranges when the dependency id differs in case from the package id
* Fixed `nuspec dependencies add` and `nuspec dependencies emptygroup` dropping the platform or profile from new group frameworks, such as `net6.0-windows` or `net40-client`. Frameworks are now written the same way as `dotnet pack` writes them, for example `net8.0` instead of `.NETCoreApp8.0`
* Fixed `nuspec dependencies emptygroup` losing a single ungrouped dependency. Ungrouped dependencies are now moved into a group only when the nuspec has no groups, since NuGet ignores them otherwise
* Fixed `nuspec dependencies emptygroup` skipping packages after the first package that already had a group for the framework
* Fixed `nuspec dependencies add` without `--framework` adding the dependency to a new group instead of the existing groups, for packages after one with no dependencies
* Fixed `nuspec frameworkassemblies clear` and `nuspec edit` with an empty value adding empty elements
* Fixed `nuspec edit` lowercasing the names of new elements. Known nuspec elements now use the schema casing, such as `releaseNotes`, and other names are kept as given
* Fixed `nupkgwrench` failing to start when installed with `dotnet tool install --allow-roll-forward`
* Added readmes to the NupkgWrench and NupkgWrenchExe packages
* NupkgWrench.exe in NupkgWrenchExe is now built with .NET 10
* Update NuGet.* packages to 7.9.0
* Update misc dependency packages

## 5.0.0
* Add net10.0 support, remove net6.0
* Update NuGet.* packages to 7.3.0
* Update misc dependency packages

## 4.3.0
* Add net9.0 support
* Update NuGet.* packages to 6.12.1

## 4.2.0
* Add net8.0 support, remove net7.0 support
* Update NuGet.* packages to 6.9.1

## 4.1.0
* Add net7.0 support

## 4.0.1
* Update NuGet.* packages to 6.2.1

## 4.0.0
* Update to net6.0

## 3.0.0
* Update to net5.0
* Build NupkgWrench.exe as a standalone file instead of ILMerging

## 2.0.0
* netcoreapp3.0 support
* release command support for --four-part-version

## 1.4.25
* Fixed dependencies command for nuspec dependency nodes without groups

## 1.4.0
* Added NupkgWrenchExe nupkg, this will not have a package type and will work for nuget.exe install
* Added dependencies add/remove/modify commands
* Converted from DotnetCliTool to DotnetTool package to support dotnet install -g
* Updated nuget libraries to 4.6.2

## 1.3.0
* netcoreapp2.0 support
* Fixed exit codes for invalid arguments

## 1.2.0
* symbol packages retain the symbols extension when updating the name
* Added nuspec frameworkassemblies add command
* Added files copysymbols command for merging pdb files
* Adding dotnet-nupkgwrench for DotNetCliToolReference support
