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

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "appExplorer" -DelayMs $DelayMs
$attached = $context.Attached
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
    openMethod = $context.OpenMethod
    tab = $context.Tab
    target = $match
}

$payload | ConvertTo-Json -Depth 20
