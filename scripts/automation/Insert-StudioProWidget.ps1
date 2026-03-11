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

$targetSelection = Select-AutomationMatch -Root $attached.Element -Match $targetMatch -DelayMs $DelayMs

$toolboxContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "toolbox" -DelayMs $DelayMs
$attached = $toolboxContext.Attached
$widgetMatch = Select-ToolboxItemByName -Root $attached.Element -Item $Widget
if (-not $widgetMatch) {
    throw "Could not find a visible Toolbox item named '$Widget'."
}

$method = "dryRun"
$dragDetails = $null
$dialogStrategy = $null
$dialogError = $null
if (-not $DryRun) {
    try {
        $dialogContext = Open-AddWidgetDialogForPageExplorerItem `
            -ProcessId $ProcessId `
            -WindowTitlePattern $WindowTitlePattern `
            -Page $Page `
            -Target $Target `
            -DelayMs $DelayMs

        $widgetSelection = Select-WidgetDialogItemByName -Dialog $dialogContext.NativeDialog -Widget $Widget -DelayMs ($DelayMs + 50)
        if (-not $widgetSelection) {
            throw "Could not find a '$Widget' row in the native Select Widget dialog."
        }

        $selectButton = Invoke-WidgetDialogButton -Dialog $dialogContext.NativeDialog -ButtonName "Select" -DelayMs ($DelayMs + 100)
        if (-not $selectButton) {
            throw "Could not find the Select button in the native Select Widget dialog."
        }

        $dialogStrategy = @{
            targetSelection = $dialogContext.TargetSelection
            contextMenu = $dialogContext.ContextMenu
            menuSelection = $dialogContext.MenuSelection
            dialogWindow = $dialogContext.DialogWindow
            widgetSelection = $widgetSelection
            selectButton = $selectButton
        }

        $method = "contextMenuDialog"
        Start-Sleep -Milliseconds ($DelayMs + 350)
    } catch {
        $dialogError = $_.Exception.Message

        $dragDetails = Invoke-BoundsDrag `
            -SourceBounds $widgetMatch.boundingRectangle `
            -TargetBounds $targetMatch.boundingRectangle `
            -SourceHorizontal "left" `
            -TargetHorizontal "left" `
            -Inset 18 `
            -Steps 24 `
            -InitialHoldMs 180 `
            -StepDelayMs 18 `
            -FinalHoldMs 180

        $method = $dragDetails.method
        Start-Sleep -Milliseconds ($DelayMs + 300)
    }
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
    resolvedTarget = $targetSelection.target
    targetSelection = $targetSelection
    resolvedWidget = $widgetMatch
    drag = $dragDetails
    dialogStrategy = $dialogStrategy
    dialogError = $dialogError
}

$payload | ConvertTo-Json -Depth 20
