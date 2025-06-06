param (
    [switch]$SkipTests,
    [switch]$SkipPack,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$RepoName = "NupkgWrench"
$RepoRoot = $PSScriptRoot
pushd $RepoRoot

# Load common build script helper methods
. "$PSScriptRoot\build\common\common.ps1"

# Download tools
Install-CommonBuildTools $RepoRoot

# Clean and write git info
Remove-Artifacts $RepoRoot
Invoke-DotnetMSBuild $RepoRoot ("build\build.proj", "/t:Clean;WriteGitInfo", "/p:Configuration=$Configuration")

# Build Exe
Invoke-DotnetExe $RepoRoot ("publish", "--force", "-r", "win-x64", "-p:PublishSingleFile=true", "-p:PublishTrimmed=false", "--self-contained", "true", "-f", "net8.0", "-o", (Join-Path $RepoRoot "artifacts\publish"), (Join-Path $RepoRoot "\src\NupkgWrench\NupkgWrench.csproj"))

# Restore
Invoke-DotnetMSBuild $RepoRoot ("build\build.proj", "/t:Restore", "/p:Configuration=$Configuration")

# Run main build
$buildTargets = "Build"

if (-not $SkipTests)
{
    $buildTargets += ";Test"
}

if (-not $SkipPack)
{
    $buildTargets += ";Pack"
}

# Run build.proj
Invoke-DotnetMSBuild $RepoRoot ("build\build.proj", "/t:$buildTargets", "/p:Configuration=$Configuration")

popd
Write-Host "Success!"
