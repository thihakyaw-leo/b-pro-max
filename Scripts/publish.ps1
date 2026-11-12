<#
.SYNOPSIS
    Publish the b-pro-max WinUI 3 (.NET 9) desktop app for win-x64.

.DESCRIPTION
    Runs `dotnet publish -c Release -r win-x64` against
    src\b-pro-max.csproj and produces a deployable folder of the app.

    Supports both self-contained and framework-dependent deployment,
    optional ZIP archiving of the published output, and comment-based
    help (run `Get-Help .\scripts\publish.ps1 -Detailed`).

    App target settings: net9.0-windows10.0.19041.0, Platform=x64,
    RuntimeIdentifiers=win-x64, UseWinUI=true, TreatWarningsAsErrors=true.

.PARAMETER Project
    Path to the project file to publish. Default: src\b-pro-max.csproj
    (resolved relative to the repository root, i.e. the parent of this
    script's folder).

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER Runtime
    Runtime identifier passed to dotnet publish. Default: win-x64.

.PARAMETER Output
    Destination folder for the published app. Default: ./publish (repo root).
    Passed to dotnet publish via -o. When omitted, dotnet publish writes to
    bin\<Configuration>\net9.0-windows10.0.19041.0\<Runtime>\publish under
    the project directory.

.PARAMETER SelfContained
    Publish a self-contained application that includes the .NET runtime.
    When absent, a framework-dependent deployment is produced (requires the
    .NET 9 runtime on the target machine).

.PARAMETER NoBuild
    Passed through to dotnet publish (--no-build). Skips the build step and
    reuses previously built assets.

.PARAMETER Zip
    After publishing, zips the contents of the output folder into a ZIP
    archive next to the output folder.

.EXAMPLE
    .\scripts\publish.ps1

    Framework-dependent publish to ./publish.

.EXAMPLE
    .\scripts\publish.ps1 -SelfContained

    Self-contained publish to ./publish (includes the .NET runtime).

.EXAMPLE
    .\scripts\publish.ps1 -SelfContained -Output C:\deploy\b-pro-max -Zip

    Self-contained publish to C:\deploy\b-pro-max, then create a ZIP archive.

.EXAMPLE
    .\scripts\publish.ps1 -SelfContained -NoBuild

    Self-contained publish without rebuilding (faster iteration when the
    project was already built).

.INPUTS
    None. You cannot pipe objects to this script.

.OUTPUTS
    None. This script writes informational messages to the host.

.NOTES
    Requires the .NET 9 SDK (pinned to 9.0.100 in global.json). Run from the
    repository root. TreatWarningsAsErrors is enabled, so the publish must
    produce zero warnings to succeed.
#>

[CmdletBinding()]
param (
    [Parameter()]
    [string] $Project = "src\b-pro-max.csproj",

    [Parameter()]
    [string] $Configuration = "Release",

    [Parameter()]
    [string] $Runtime = "win-x64",

    [Parameter()]
    [string] $Output = "./publish",

    [Parameter()]
    [switch] $SelfContained,

    [Parameter()]
    [switch] $NoBuild,

    [Parameter()]
    [switch] $Zip
)

# ---------------------------------------------------------------------------
# Resolve paths relative to the repository root (parent of the scripts folder)
# so the script works no matter where it is invoked from.
# ---------------------------------------------------------------------------
if (-not $PSScriptRoot) {
    throw "Unable to determine the script folder ($PSScriptRoot is empty). Run the script as a file, e.g. .\scripts\publish.ps1"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..") -ErrorAction Stop
$projectPath = Join-Path $repoRoot $Project

if (-not (Test-Path -PathType Leaf $projectPath)) {
    throw "Project file not found: $projectPath"
}

# ---------------------------------------------------------------------------
# Header / progress messages
# ---------------------------------------------------------------------------
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " b-pro-max - Publish" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Project      : $projectPath"
Write-Host " Configuration: $Configuration"
Write-Host " Runtime      : $Runtime"
Write-Host " Self-contained: $([bool]$SelfContained.IsPresent)"
Write-Host " Output       : $Output"
Write-Host ""

# ---------------------------------------------------------------------------
# Verify the .NET SDK is available
# ---------------------------------------------------------------------------
try {
    $dotnetVersion = dotnet --version 2>&1 | Out-String | Trim
    Write-Host ".NET SDK: $dotnetVersion" -ForegroundColor Gray
}
catch {
    throw "dotnet CLI is not available on PATH. Install the .NET 9 SDK (see global.json)."
}

# ---------------------------------------------------------------------------
# Build the dotnet publish command
# ---------------------------------------------------------------------------
$publishArgs = @(
    "publish"
    $projectPath
    "-c"
    $Configuration
    "-r"
    $Runtime
    "--nologo"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", $true
}
else {
    $publishArgs += "--self-contained", $false
}

if ($NoBuild) {
    $publishArgs += "--no-build"
}

if ($Output) {
    $publishArgs += "-o", $Output
}

Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    & dotnet @publishArgs
}
catch {
    Write-Host "ERROR: dotnet publish failed to start." -ForegroundColor Red
    throw
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish exited with code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

$stopwatch.Stop()

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Publish succeeded in $($stopwatch.Elapsed.TotalSeconds) seconds." -ForegroundColor Green
Write-Host "Output folder: $Output" -ForegroundColor Green

# ---------------------------------------------------------------------------
# Optional ZIP archive of the published output
# ---------------------------------------------------------------------------
if ($Zip) {
    $zipParent = Split-Path -Parent (Resolve-Path $Output -ErrorAction Stop)
    $zipName = "b-pro-max-$Configuration-$Runtime.zip"
    $zipPath = Join-Path $zipParent $zipName

    Write-Host ""
    Write-Host "Creating ZIP archive: $zipPath" -ForegroundColor Cyan

    try {
        Compress-Archive -Path "$Output\*" -DestinationPath $zipPath -Force
        Write-Host "ZIP archive created: $zipPath" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: failed to create ZIP archive." -ForegroundColor Red
        throw
    }
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
