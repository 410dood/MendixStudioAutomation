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
Send-KeysToForegroundWindow -Keys "^," -DelayMs ($DelayMs + 100)
$dialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs 2500 -PollMs 150 -Limit 30

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
