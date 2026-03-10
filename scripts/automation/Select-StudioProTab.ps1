param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Tab = "",
    [int]$DelayMs = 250
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

Start-Sleep -Milliseconds $DelayMs

$payload = @{
    ok = $true
    action = "select-tab"
    tab = $Tab
    method = "nativeBoundsClick"
    target = $match
}

$payload | ConvertTo-Json -Depth 20
