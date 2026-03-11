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

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Page -Scope "pageExplorer" -DelayMs $DelayMs
$attached = $context.Attached
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
    openMethod = $context.OpenMethod
    tab = $context.Tab
    target = $match
}

$payload | ConvertTo-Json -Depth 20
