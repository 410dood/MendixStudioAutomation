param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Tab = "",
    [int]$DelayMs = 250,
    [switch]$DryRun
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Tab) {
    throw "An open Studio Pro tab name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$match = Select-OpenEditorTabByName -Root $attached.Element -Name $Tab
if (-not $match) {
    throw "Could not find an open Studio Pro tab named '$Tab'."
}

$method = "dryRun"
if (-not $DryRun) {
    Set-StudioProForegroundWindow -Process $attached.Process
    Send-KeysToForegroundWindow -Keys "^{F4}" -DelayMs ($DelayMs + 150)
    $method = "ctrl+f4"
}

$payload = @{
    ok = $true
    action = "close-tab"
    tab = $Tab
    method = $method
    dryRun = [bool]$DryRun
    target = $match
}

$payload | ConvertTo-Json -Depth 20
