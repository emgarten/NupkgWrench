# Release Notes

## 2.0.0
* netcoreapp3.0 support

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
