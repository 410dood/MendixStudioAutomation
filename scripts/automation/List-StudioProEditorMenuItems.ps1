param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Element = "",
    [string]$RuntimeId = "",
    [int]$OffsetX = 0,
    [int]$OffsetY = 0,
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An editor item name is required."
}

$contextMenu = Open-EditorElementContextMenu `
    -ProcessId $ProcessId `
    -WindowTitlePattern $WindowTitlePattern `
    -Item $Item `
    -ElementName $Element `
    -ElementRuntimeId $RuntimeId `
    -OffsetX $OffsetX `
    -OffsetY $OffsetY `
    -DelayMs $DelayMs

if (-not $contextMenu) {
    throw "Could not open the editor context menu for '$Element'."
}

$payload = @{
    ok = $true
    action = "list-editor-menu-items"
    item = $Item
    element = $Element
    openMethod = $contextMenu.EditorContext.OpenMethod
    tab = $contextMenu.EditorContext.Tab
    target = $contextMenu.Target
    targetSelection = $contextMenu.TargetSelection
    trigger = $contextMenu.Trigger
    postDialog = $contextMenu.PostDialog
    items = @($contextMenu.MenuItems | Select-Object -First 40)
    count = @($contextMenu.MenuItems).Length
}

$payload | ConvertTo-Json -Depth 20
