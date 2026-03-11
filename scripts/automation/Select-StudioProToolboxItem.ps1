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

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "toolbox" -DelayMs $DelayMs
$attached = $context.Attached
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
    openMethod = $context.OpenMethod
    tab = $context.Tab
    target = $match
}

$payload | ConvertTo-Json -Depth 20
