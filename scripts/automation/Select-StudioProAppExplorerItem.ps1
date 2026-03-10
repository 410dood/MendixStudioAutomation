param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An App Explorer item name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$tabMatches = Find-MatchingElements -Root $attached.Element -Depth 10 -MaxResults 10 -Name "App Explorer" -ControlType "TabItem"
if ($tabMatches.Length -eq 0) {
    throw "Could not find the App Explorer tab."
}

$tab = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $tabMatches[0].runtimeId -Depth 10
if (-not $tab) {
    throw "Could not resolve the native App Explorer tab."
}

Invoke-ElementAction -Element $tab -Action "click" | Out-Null
Start-Sleep -Milliseconds $DelayMs

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$match = Select-AppExplorerItemByName -Root $attached.Element -Item $Item
if (-not $match) {
    throw "Could not find a visible App Explorer item named '$Item'."
}

$method = Invoke-BoundsDoubleClick -Bounds $match.boundingRectangle

$payload = @{
    ok = $true
    action = "select-app-explorer-item"
    item = $Item
    method = $method
    target = $match
}

$payload | ConvertTo-Json -Depth 20
