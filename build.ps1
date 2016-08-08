$RepoRoot = $PSScriptRoot

trap
{
    Write-Host "build failed"
    exit 1
}

# Load common build script helper methods
. "$PSScriptRoot\build\common.ps1"

# Ensure dotnet.exe exists in .cli
Install-DotnetCLI $RepoRoot

# Ensure packages.config packages
Install-PackagesConfig $RepoRoot

$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$nugetExe = Join-Path $RepoRoot '.nuget\nuget.exe'
$ILMergeExe = Join-Path $RepoRoot 'packages\ILRepack.2.0.10\tools\ILRepack.exe'
$dotnetExe = Get-DotnetCLIExe $RepoRoot
$nupkgWrenchExe = Join-Path $ArtifactsDir 'nupkgwrench.exe'

# Clear artifacts
Remove-Item -Recurse -Force $ArtifactsDir | Out-Null

# Restore project.json files
& $nugetExe restore $RepoRoot

# Run tests
& $dotnetExe test (Join-Path $RepoRoot "test\NupkgWrench.Tests")

if (-not $?)
{
    Write-Host "tests failed!!!"
    exit 1
}

# Publish for ILMerge
& $dotnetExe publish src\NupkgWrench -o artifacts\publish\net451 -f net451 -r win7-x86 --configuration release

$net46Root = (Join-Path $ArtifactsDir 'publish\net451')
$ILMergeOpts = , (Join-Path $net46Root 'NupkgWrench.exe')
$ILMergeOpts += Get-ChildItem $net46Root -Exclude @('*.exe', '*compression*', '*System.*', '*.config', '*.pdb') | where { ! $_.PSIsContainer } | %{ $_.FullName }
$ILMergeOpts += '/out:' + (Join-Path $ArtifactsDir 'NupkgWrench.exe')
$ILMergeOpts += '/log'
$ILMergeOpts += '/ndebug'

Write-Host "ILMerging NupkgWrench.exe"
& $ILMergeExe $ILMergeOpts | Out-Null

if (-not $?)
{
    Write-Host "ILMerge failed!"
    exit 1
}

# Pack
& $dotnetExe pack (Join-Path $RepoRoot "src\NupkgWrench") --no-build --output $ArtifactsDir

if (-not $?)
{
    Write-Host "Pack failed!"
    exit 1
}

# use the build to modify the nupkg
& $nupkgWrenchExe files emptyfolder artifacts -p lib/net451
& $nupkgWrenchExe nuspec frameworkassemblies clear artifacts
& $nupkgWrenchExe nuspec dependencies emptygroup artifacts -f net451

# Create xplat tar
& $dotnetExe publish src\NupkgWrench -o artifacts\publish\NupkgWrench -f netcoreapp1.0 --configuration release

pushd "artifacts\publish"

# clean up pdbs
rm nupkgwrench\*.pdb

# bzip the portable netcore app folder
& "$RepoRoot\packages\7ZipCLI.9.20.0\tools\7za.exe" "a" "NupkgWrench.tar" "NupkgWrench"
& "$RepoRoot\packages\7ZipCLI.9.20.0\tools\7za.exe" "a" "..\NupkgWrench.tar.bz2" "NupkgWrench.tar"

popd

Write-Host "Success!"