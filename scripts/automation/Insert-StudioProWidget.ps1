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

$pageContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Page -Scope "pageExplorer" -DelayMs $DelayMs
$attached = $pageContext.Attached
$targetMatch = Select-PageExplorerItemByName -Root $attached.Element -Item $Target
if (-not $targetMatch) {
    throw "Could not find a visible Page Explorer item named '$Target'."
}

Invoke-BoundsClick -Bounds $targetMatch.boundingRectangle | Out-Null
Start-Sleep -Milliseconds $DelayMs

$toolboxContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "toolbox" -DelayMs $DelayMs
$attached = $toolboxContext.Attached
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
    openMethod = $pageContext.OpenMethod
    tab = $pageContext.Tab
    resolvedTarget = $targetMatch
    resolvedWidget = $widgetMatch
}

$payload | ConvertTo-Json -Depth 20
