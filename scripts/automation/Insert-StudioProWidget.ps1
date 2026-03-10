param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Page = "",
    [string]$Target = "",
    [string]$Widget = "",
    [int]$DelayMs = 250,
    [switch]$DryRun
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Page) {
    throw "A page name is required."
}

if (-not $Target) {
    throw "A target Page Explorer item is required."
}

if (-not $Widget) {
    throw "A widget name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
Open-StudioProItemByName -Process $attached.Process -Item $Page -DelayMs $DelayMs
Start-Sleep -Milliseconds ($DelayMs + 150)

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
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
$targetMatch = Select-PageExplorerItemByName -Root $attached.Element -Item $Target
if (-not $targetMatch) {
    throw "Could not find a visible Page Explorer item named '$Target'."
}

Invoke-BoundsClick -Bounds $targetMatch.boundingRectangle | Out-Null
Start-Sleep -Milliseconds $DelayMs

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
$widgetMatch = Select-ToolboxItemByName -Root $attached.Element -Item $Widget
if (-not $widgetMatch) {
    throw "Could not find a visible Toolbox item named '$Widget'."
}

$method = "dryRun"
if (-not $DryRun) {
    $method = Invoke-BoundsDoubleClick -Bounds $widgetMatch.boundingRectangle
    Start-Sleep -Milliseconds ($DelayMs + 150)
}

$payload = @{
    ok = $true
    action = "insert-widget"
    page = $Page
    target = $Target
    widget = $Widget
    dryRun = [bool]$DryRun
    method = $method
    resolvedTarget = $targetMatch
    resolvedWidget = $widgetMatch
}

$payload | ConvertTo-Json -Depth 20
