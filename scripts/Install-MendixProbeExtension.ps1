param(
    [Parameter(Mandatory = $true)]
    [string]$AppDirectory,

    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$extensionRoot = Join-Path $repoRoot "extensions\MendixStudioAutomation_ProbeExtension"
$projectFile = Join-Path $extensionRoot "MendixStudioAutomation_ProbeExtension.csproj"
$buildOutputRoot = Join-Path $extensionRoot "bin\Debug\net8.0-windows"
$targetRoot = Join-Path $AppDirectory "extensions\MendixStudioAutomation_ProbeExtension"

if ($Build) {
    dotnet build $projectFile
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $projectFile"
    }
}

if (-not (Test-Path $buildOutputRoot)) {
    throw "Build output not found: $buildOutputRoot"
}

New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

$robocopyArgs = @(
    $buildOutputRoot,
    $targetRoot,
    "/MIR",
    "/XF", "*.user"
)

robocopy @robocopyArgs | Out-Host
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -ge 8) {
    throw "robocopy failed with exit code $robocopyExitCode"
}

Write-Output "Installed Mendix Studio Automation probe extension to $targetRoot"
