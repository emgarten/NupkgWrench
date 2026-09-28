# NupkgWrenchExe

NupkgWrenchExe contains *NupkgWrench.exe*, a self-contained single-file build of [NupkgWrench](https://github.com/emgarten/NupkgWrench) for Windows x64 that doesn't require .NET to be installed. On other platforms, or when the .NET SDK is available, use the [NupkgWrench .NET tool](https://www.nuget.org/packages/NupkgWrench) instead.

## Use from MSBuild

Add the package to a project:

```
dotnet add package NupkgWrenchExe
```

The package sets the `$(NupkgWrench)` MSBuild property to the full path of *NupkgWrench.exe*:

```xml
<Target Name="ValidatePackages">
  <Exec Command="&quot;$(NupkgWrench)&quot; validate nupkgs" />
</Target>
```

## Use directly

Download the package from NuGet.org and extract *tools/NupkgWrench.exe*.

## Documentation

* [Commands and examples](https://github.com/emgarten/NupkgWrench#commands)
* [Release notes](https://github.com/emgarten/NupkgWrench/blob/main/ReleaseNotes.md)

Source code and issues: [github.com/emgarten/NupkgWrench](https://github.com/emgarten/NupkgWrench)
