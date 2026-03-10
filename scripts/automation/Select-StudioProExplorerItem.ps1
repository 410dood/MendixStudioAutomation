param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Page = "",
    [string]$Item = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An explorer item name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
if ($Page) {
    Open-StudioProItemByName -Process $attached.Process -Item $Page -DelayMs $DelayMs
    Start-Sleep -Milliseconds ($DelayMs + 150)
    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
}

$pageExplorerMatches = Find-MatchingElements -Root $attached.Element -Depth 10 -MaxResults 10 -Name "Page Explorer" -ControlType "TabItem"
if ($pageExplorerMatches.Length -eq 0) {
    throw "Could not find the Page Explorer tab."
}

$pageExplorer = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $pageExplorerMatches[0].runtimeId -Depth 10
if (-not $pageExplorer) {
    throw "Could not resolve the native Page Explorer tab."
}

Invoke-ElementAction -Element $pageExplorer -Action "click" | Out-Null
Start-Sleep -Milliseconds $DelayMs

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$match = Select-PageExplorerItemByName -Root $attached.Element -Item $Item
if (-not $match) {
    throw "Could not find a visible Page Explorer row named '$Item'."
}

$method = Invoke-BoundsClick -Bounds $match.boundingRectangle

$payload = @{
    ok = $true
    action = "select-explorer-item"
    page = $Page
    item = $Item
    method = $method
    target = $match
}

$payload | ConvertTo-Json -Depth 20
