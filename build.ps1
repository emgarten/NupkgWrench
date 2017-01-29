param (
    [switch]$SkipTests,
    [switch]$SkipPack,
    [switch]$StableVersion
)

$BuildNumberDateBase = "2016-11-01"
$RepoRoot = $PSScriptRoot

# Load common build script helper methods
. "$PSScriptRoot\build\common.ps1"

# Ensure dotnet.exe exists in .cli
Install-DotnetCLI $RepoRoot

# Ensure packages.config packages
Install-PackagesConfig $RepoRoot

$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$nugetExe = Join-Path $RepoRoot '.nuget\nuget.exe'
$ILMergeExe = Join-Path $RepoRoot 'packages\ILRepack.2.0.12\tools\ILRepack.exe'
$dotnetExe = Get-DotnetCLIExe $RepoRoot
$nupkgWrenchExe = Join-Path $ArtifactsDir 'nupkgwrench.exe'
$zipExe = Join-Path $RepoRoot 'packages\7ZipCLI.9.20.0\tools\7za.exe'

# Clear artifacts
Remove-Item -Recurse -Force $ArtifactsDir | Out-Null

# Restore project.json files
& $dotnetExe restore $RepoRoot

if (-not $?)
{
    Write-Host "Restore failed!"
    exit 1
}

& $dotnetExe clean $RepoRoot --configuration release /m
& $dotnetExe build $RepoRoot --configuration release /m

if (-not $?)
{
    Write-Host "Build failed!"
    exit 1
}

# Run tests
if (-not $SkipTests)
{
    Run-Tests $RepoRoot $DotnetExe

    if (-not $?)
    {
        Write-Host "tests failed!!!"
        exit 1
    }
}

# Publish for ILMerge
$net46Root = (Join-Path $RepoRoot 'artifacts\publish\net46')

& $dotnetExe publish src\NupkgWrench -f net46 -r win7-x86 --configuration release -o $net46Root

$ILMergeOpts = , (Join-Path $net46Root 'NupkgWrench.exe')
$ILMergeOpts += (Join-Path $net46Root 'System.IO.Compression.dll')
$ILMergeOpts += Get-ChildItem $net46Root -Exclude @('*.exe', '*compression*', '*System.*', '*.config', '*.pdb', '*.json', "*.xml") | where { ! $_.PSIsContainer } | %{ $_.FullName }
$ILMergeOpts += '/out:' + (Join-Path $ArtifactsDir 'NupkgWrench.exe')
$ILMergeOpts += '/log'
$ILMergeOpts += '/ndebug'
$ILMergeOpts += '/parallel'

Write-Host "ILMerging NupkgWrench.exe"
& $ILMergeExe $ILMergeOpts | Out-Null

if (-not $?)
{
    # Get failure message
    Write-Host $ILMergeExe $ILMergeOpts
    & $ILMergeExe $ILMergeOpts
    Write-Host "ILMerge failed!"
    exit 1
}

if (-not $SkipPack)
{
    # Pack
    if ($StableVersion)
    {
        & $dotnetExe pack (Join-Path $RepoRoot "src\NupkgWrench") --no-build --output $ArtifactsDir --configuration release
    }
    else
    {
        $buildNumber = Get-BuildNumber $BuildNumberDateBase

        & $dotnetExe pack (Join-Path $RepoRoot "src\NupkgWrench") --no-build --output $ArtifactsDir --version-suffix "beta.$buildNumber" --configuration release
    }

    if (-not $?)
    {
        Write-Host "Pack failed!"
        exit 1
    }

    # use the build to modify the nupkg
    & $nupkgWrenchExe files emptyfolder artifacts -p lib/net46
    & $nupkgWrenchExe nuspec frameworkassemblies clear artifacts
    & $nupkgWrenchExe nuspec dependencies emptygroup artifacts -f net46

    # Get version number
    $nupkgVersion = (& $nupkgWrenchExe version artifacts --exclude-symbols --id nupkgwrench) | Out-String
    $nupkgVersion = $nupkgVersion.Trim()

    Write-Host "-----------------------------"
    Write-Host "Version: $nupkgVersion"
    Write-Host "-----------------------------"

    # Create xplat tar
    $versionFolderName = "nupkgwrench.$nupkgVersion".ToLowerInvariant()
    $versionFolder = "$RepoRoot\artifacts\publish\$versionFolderName"

    & $dotnetExe publish src\NupkgWrench -o $versionFolder -f netcoreapp1.0 --configuration release

    if (-not $?)
    {
        Write-Host "Publish failed!"
        exit 1
    }

    pushd "$RepoRoot\artifacts\publish"

    # clean up pdbs
    rm $versionFolderName\*.pdb

    # bzip the portable netcore app folder
    & $zipExe "a" "$versionFolderName.tar" $versionFolderName
    & $zipExe "a" "..\$versionFolderName.tar.bz2" "$versionFolderName.tar"

    if (-not $?)
    {
        Write-Host "Zip failed!"
        exit 1
    }

    popd
}

Write-Host "Success!"