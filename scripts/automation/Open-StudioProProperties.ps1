param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Page = "",
    [string]$Microflow = "",
    [string]$Item = "",
    [string]$Scope = "editor",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An item name is required."
}

$contextItem = if ($Page) { $Page } elseif ($Microflow) { $Microflow } else { "" }
$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $contextItem -Scope $Scope -DelayMs $DelayMs
$attached = $context.Attached

$match = $null
switch ($Scope) {
    "pageExplorer" {
        $match = Select-PageExplorerItemByName -Root $attached.Element -Item $Item
        if (-not $match) {
            Start-Sleep -Milliseconds 200
            $attached = (Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern)
            $match = Select-PageExplorerItemByName -Root $attached.Element -Item $Item
        }
    }
    "toolbox" {
        $match = Select-ToolboxItemByName -Root $attached.Element -Item $Item
    }
    default {
        $match = Find-BestVisibleNamedElement -Root $attached.Element -Name $Item -Surface $Scope -Item $contextItem
    }
}

if (-not $match) {
    throw "Could not find a visible '$Item' element on the '$Scope' surface."
}

$selection = Select-AutomationMatch -Root $attached.Element -Match $match -DelayMs $DelayMs
Set-StudioProForegroundWindow -Process $attached.Process

$dialog = $null
if ($Scope -eq "pageExplorer") {
    Send-KeysToForegroundWindow -Keys "+{F10}" -DelayMs ($DelayMs + 100)
    $menuSnapshot = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs 800 -PollMs 100 -Limit 20
    if ($menuSnapshot -and $menuSnapshot.window.name -ne $attached.Process.MainWindowTitle) {
        $propertiesItem = @($menuSnapshot.items | Where-Object { $_.name -eq "Properties" -and $_.controlType -eq "MenuItem" } | Select-Object -First 1)
        if ($propertiesItem.Length -gt 0) {
            $propertiesSelection = Select-AutomationMatch -Root (Resolve-NativeWindowByName -Root $attached.Element -Name $menuSnapshot.window.name -Depth 15) -Match $propertiesItem[0] -DelayMs ($DelayMs + 100)
            $dialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs 2500 -PollMs 150 -Limit 30
            if ($dialog) {
                $selection = @{
                    method = $selection.method
                    supportsSelectionItem = $selection.supportsSelectionItem
                    supportsInvoke = $selection.supportsInvoke
                    isSelected = $selection.isSelected
                    target = $selection.target
                    propertiesMenuSelection = $propertiesSelection
                }
            }
        } else {
            Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs 100
        }
    }
}

if (-not $dialog) {
    Send-KeysToForegroundWindow -Keys "^," -DelayMs ($DelayMs + 100)
    $dialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs 2500 -PollMs 150 -Limit 30
}

$payload = @{
    ok = $true
    action = "open-properties"
    page = $Page
    microflow = $Microflow
    item = $Item
    scope = $Scope
    openMethod = $context.OpenMethod
    tab = $context.Tab
    selection = $selection
    dialog = $dialog
}

$payload | ConvertTo-Json -Depth 20
