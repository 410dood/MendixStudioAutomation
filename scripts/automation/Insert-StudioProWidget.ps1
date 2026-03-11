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

function Get-PageExplorerSnapshot {
    param(
        [int]$ProcessId,
        [string]$WindowTitlePattern,
        [string]$Page,
        [int]$Limit = 40
    )

    try {
        $context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Page -Scope "pageExplorer" -DelayMs 150
        $items = @(Get-VisibleTextMatches -Root $context.Attached.Element -Scope "pageExplorer" -Item $Page -Limit $Limit | Where-Object {
            $_.name
        })
    } catch {
        return $null
    }

    return @{
        count = $items.Length
        names = @($items | ForEach-Object { $_.name })
    }
}

function Test-PageExplorerSnapshotChanged {
    param(
        [hashtable]$Before,
        [hashtable]$After
    )

    if (-not $Before -or -not $After) {
        return $false
    }

    if ($Before.count -ne $After.count) {
        return $true
    }

    $beforeNames = @($Before.names)
    $afterNames = @($After.names)
    if ($beforeNames.Length -ne $afterNames.Length) {
        return $true
    }

    for ($index = 0; $index -lt $beforeNames.Length; $index++) {
        if ($beforeNames[$index] -ne $afterNames[$index]) {
            return $true
        }
    }

    return $false
}

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
$postDialog = $null
$pageExplorerBefore = Get-PageExplorerSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Page $Page -Limit 40
$pageExplorerAfter = $pageExplorerBefore
$mutationDetectedByDialog = $false
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

        $acceptAttempts = @()
        foreach ($acceptStrategy in @("button", "enter", "doubleClick")) {
            $acceptSelection = Invoke-WidgetDialogAccept -Dialog $dialogContext.NativeDialog -WidgetSelection $widgetSelection -Strategy $acceptStrategy -DelayMs ($DelayMs + 100)
            $postDialogCandidate = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(900, ($DelayMs * 3))) -PollMs 150 -Limit 30
            $dialogIndicatesMutation = [bool](
                $postDialogCandidate -and
                $postDialogCandidate.window -and
                $postDialogCandidate.window.name -and
                $postDialogCandidate.window.name -ne "Select Widget"
            )
            $pageExplorerAfterCandidate = $null
            $pageExplorerChanged = $false
            if (-not $dialogIndicatesMutation) {
                $pageExplorerAfterCandidate = Get-PageExplorerSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Page $Page -Limit 24
                $pageExplorerChanged = Test-PageExplorerSnapshotChanged -Before $pageExplorerBefore -After $pageExplorerAfterCandidate
            }

            $acceptAttempts += @(
                @{
                    strategy = $acceptStrategy
                    acceptSelection = $acceptSelection
                    postDialog = $postDialogCandidate
                    pageExplorerAfter = $pageExplorerAfterCandidate
                    pageExplorerChanged = $pageExplorerChanged
                    dialogIndicatesMutation = $dialogIndicatesMutation
                }
            )

            if ($dialogIndicatesMutation -or $pageExplorerChanged -or $postDialogCandidate) {
                $postDialog = $postDialogCandidate
                $pageExplorerAfter = if ($pageExplorerAfterCandidate) { $pageExplorerAfterCandidate } else { $pageExplorerAfter }
                $mutationDetectedByDialog = $dialogIndicatesMutation
                break
            }

            if ($acceptStrategy -ne "doubleClick") {
                $dialogContext = Open-AddWidgetDialogForPageExplorerItem `
                    -ProcessId $ProcessId `
                    -WindowTitlePattern $WindowTitlePattern `
                    -Page $Page `
                    -Target $Target `
                    -DelayMs $DelayMs
                $widgetSelection = Select-WidgetDialogItemByName -Dialog $dialogContext.NativeDialog -Widget $Widget -DelayMs ($DelayMs + 50)
                if (-not $widgetSelection) {
                    throw "Could not re-select a '$Widget' row in the native Select Widget dialog."
                }
            }
        }

        $dialogStrategy = @{
            targetSelection = $dialogContext.TargetSelection
            contextMenu = if ($dialogContext.ContextMenu) {
                @{
                    menuItem = $dialogContext.ContextMenu.MenuItem
                    menuItems = @($dialogContext.ContextMenu.MenuItems | Select-Object -First 20)
                    trigger = $dialogContext.ContextMenu.Trigger
                }
            } else {
                $null
            }
            menuSelection = $dialogContext.MenuSelection
            dialogWindow = $dialogContext.DialogWindow
            widgetSelection = $widgetSelection
            acceptAttempts = $acceptAttempts
        }

        $method = "contextMenuDialog"
        if (-not $postDialog) {
            $postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(1200, ($DelayMs * 4))) -PollMs 150 -Limit 30
            if (-not $mutationDetectedByDialog) {
                $pageExplorerAfter = Get-PageExplorerSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Page $Page -Limit 24
            }
        }
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
        $postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(1200, ($DelayMs * 4))) -PollMs 150 -Limit 30
        $pageExplorerAfter = Get-PageExplorerSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Page $Page -Limit 24
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
    postDialog = $postDialog
    mutationDetectedByDialog = $mutationDetectedByDialog
    pageExplorerBefore = $pageExplorerBefore
    pageExplorerAfter = $pageExplorerAfter
    pageExplorerChanged = (Test-PageExplorerSnapshotChanged -Before $pageExplorerBefore -After $pageExplorerAfter)
}

$payload | ConvertTo-Json -Depth 20
