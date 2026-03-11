param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Element = "",
    [string]$RuntimeId = "",
    [int]$OffsetX = 0,
    [int]$OffsetY = 0,
    [string]$MenuItem = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An editor item name is required."
}

if (-not $MenuItem) {
    throw "A context menu item name is required."
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

$menuItems = @($contextMenu.MenuItems)
$menuMatch = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItem

if (-not $menuMatch) {
    throw "Could not resolve a '$MenuItem' context menu item for '$Element'."
}

$selection = Select-AutomationMatch -Root $contextMenu.Attached.Element -Match $menuMatch -DelayMs ($DelayMs + 100)
$postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $contextMenu.Attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(1200, ($DelayMs * 4))) -PollMs 150 -Limit 30

$payload = @{
    ok = $true
    action = "invoke-editor-menu-item"
    item = $Item
    element = $Element
    runtimeId = $RuntimeId
    offsetX = $OffsetX
    offsetY = $OffsetY
    menuItem = $MenuItem
    openMethod = $contextMenu.EditorContext.OpenMethod
    tab = $contextMenu.EditorContext.Tab
    target = $contextMenu.Target
    targetSelection = $contextMenu.TargetSelection
    trigger = $contextMenu.Trigger
    availableMenuItemCount = $menuItems.Length
    menuSelection = $selection
    postDialog = $postDialog
}

$payload | ConvertTo-Json -Depth 20
