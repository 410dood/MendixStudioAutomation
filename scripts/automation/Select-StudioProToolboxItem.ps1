param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "A toolbox item name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$toolboxMatches = Find-MatchingElements -Root $attached.Element -Depth 10 -MaxResults 10 -Name "Toolbox" -ControlType "TabItem"
if ($toolboxMatches.Length -eq 0) {
    throw "Could not find the Toolbox tab."
}

$toolboxTab = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $toolboxMatches[0].runtimeId -Depth 10
if (-not $toolboxTab) {
    throw "Could not resolve the native Toolbox tab."
}

Invoke-ElementAction -Element $toolboxTab -Action "click" | Out-Null
Start-Sleep -Milliseconds $DelayMs

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$match = Select-ToolboxItemByName -Root $attached.Element -Item $Item
if (-not $match) {
    throw "Could not find a visible Toolbox item named '$Item'."
}

$method = Invoke-BoundsClick -Bounds $match.boundingRectangle

$payload = @{
    ok = $true
    action = "select-toolbox-item"
    item = $Item
    method = $method
    target = $match
}

$payload | ConvertTo-Json -Depth 20
