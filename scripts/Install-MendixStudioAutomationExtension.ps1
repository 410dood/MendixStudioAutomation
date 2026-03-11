param(
    [Parameter(Mandatory = $true)]
    [string]$AppDirectory,

    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$extensionRoot = Join-Path $repoRoot "extensions\MendixStudioAutomation_Extension"
$projectFile = Join-Path $extensionRoot "MendixStudioAutomation_Extension.csproj"
$targetRoot = Join-Path $AppDirectory "extensions\MendixStudioAutomation_Extension"
$automationStateDir = Join-Path $repoRoot ".automation-state"
$installMetadataFile = Join-Path $automationStateDir "hybrid-extension-install.json"
$buildOutputRoot = Join-Path $extensionRoot "bin\Debug\net8.0-windows"

if (-not (Test-Path $AppDirectory)) {
    throw "App directory not found: $AppDirectory"
}

if ($Build) {
    dotnet build $projectFile
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $projectFile"
    }
}

@(
    (Join-Path $AppDirectory "extensions\MendixStudioAutomation.Extension"),
    (Join-Path $AppDirectory "extensions\MendixStudioAutomation.ProbeExtension"),
    (Join-Path $AppDirectory "extensions\MendixStudioAutomation_ProbeExtension")
) | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item -Path $_ -Recurse -Force
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
    "/XD", "runtime",
    "/XF", "*.user"
)

robocopy @robocopyArgs | Out-Host
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -ge 8) {
    throw "robocopy failed with exit code $robocopyExitCode"
}

Copy-Item -Path (Join-Path $buildOutputRoot "manifest.json") -Destination (Join-Path $targetRoot "manifest.json") -Force

New-Item -ItemType Directory -Path $automationStateDir -Force | Out-Null
$metadata = [ordered]@{
    appDirectory = $AppDirectory
    extensionRoot = $targetRoot
    endpointFile = (Join-Path $targetRoot "runtime\endpoint.json")
    installedAtUtc = [DateTime]::UtcNow.ToString("o")
}
$metadata | ConvertTo-Json | Set-Content -Path $installMetadataFile -Encoding UTF8

Write-Output "Installed Mendix Studio Automation extension to $targetRoot"
