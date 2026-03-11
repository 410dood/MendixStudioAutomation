param(
    [Parameter(Mandatory = $true)]
    [string]$AppDirectory,

    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$extensionRoot = Join-Path $repoRoot "extensions\MendixStudioAutomation.Extension"
$projectFile = Join-Path $extensionRoot "MendixStudioAutomation.Extension.csproj"
$targetRoot = Join-Path $AppDirectory "extensions\MendixStudioAutomation.Extension"
$automationStateDir = Join-Path $repoRoot ".automation-state"
$installMetadataFile = Join-Path $automationStateDir "hybrid-extension-install.json"

if (-not (Test-Path $AppDirectory)) {
    throw "App directory not found: $AppDirectory"
}

if ($Build) {
    dotnet build $projectFile
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $projectFile"
    }
}

New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

$robocopyArgs = @(
    $extensionRoot,
    $targetRoot,
    "/MIR",
    "/XD", "bin", "obj", "runtime",
    "/XF", "*.user"
)

robocopy @robocopyArgs | Out-Host
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -ge 8) {
    throw "robocopy failed with exit code $robocopyExitCode"
}

New-Item -ItemType Directory -Path $automationStateDir -Force | Out-Null
$metadata = [ordered]@{
    appDirectory = $AppDirectory
    extensionRoot = $targetRoot
    endpointFile = (Join-Path $targetRoot "runtime\endpoint.json")
    installedAtUtc = [DateTime]::UtcNow.ToString("o")
}
$metadata | ConvertTo-Json | Set-Content -Path $installMetadataFile -Encoding UTF8

Write-Output "Installed Mendix Studio Automation extension to $targetRoot"
