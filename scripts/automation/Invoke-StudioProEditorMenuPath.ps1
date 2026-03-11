param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Element = "",
    [string]$MenuPath = "",
    [int]$DelayMs = 250,
    [switch]$DryRun
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An editor item name is required."
}

if (-not $MenuPath) {
    throw "A context menu path is required."
}

$segments = @($MenuPath -split ">" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($segments.Length -eq 0) {
    throw "A context menu path is required."
}

function Get-VisibleEditorMenuItems {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    return @(Find-MatchingElements -Root $Root -Depth 15 -MaxResults 200 -ControlType "MenuItem" | Where-Object {
        $_ -and
        -not $_.isOffscreen -and
        $_.name -and
        $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
    })
}

$contextMenu = Open-EditorElementContextMenu `
    -ProcessId $ProcessId `
    -WindowTitlePattern $WindowTitlePattern `
    -Item $Item `
    -ElementName $Element `
    -DelayMs $DelayMs

if (-not $contextMenu) {
    throw "Could not open the editor context menu."
}

$attached = $contextMenu.Attached
$steps = @()
$currentMenuItems = @($contextMenu.MenuItems)
$postDialog = $contextMenu.PostDialog

for ($index = 0; $index -lt $segments.Length; $index++) {
    $segment = $segments[$index]
    $match = Find-MenuItemMatch -MenuItems $currentMenuItems -MenuItemName $segment
    if (-not $match) {
        throw "Could not resolve context menu segment '$segment'."
    }

    $shouldInspectOnly = $DryRun -and $index -eq ($segments.Length - 1)
    if ($shouldInspectOnly) {
        Set-StudioProForegroundWindow -Process $attached.Process
        $selection = @{
            method = "inspectOnly"
            supportsSelectionItem = $false
            supportsInvoke = $false
            isSelected = $null
            target = $match
        }
    } else {
        $selection = Select-AutomationMatch -Root $attached.Element -Match $match -DelayMs ($DelayMs + 100)
    }
    $step = @{
        segment = $segment
        selection = $selection
    }

    if ($index -lt ($segments.Length - 1) -or $shouldInspectOnly) {
        $attached = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
        $currentMenuItems = Get-VisibleEditorMenuItems -Root $attached.Element
        $nextSegment = if ($index -lt ($segments.Length - 1)) { $segments[$index + 1] } else { "" }
        $nextMatch = if ($nextSegment) {
            Find-MenuItemMatch -MenuItems $currentMenuItems -MenuItemName $nextSegment
        } else {
            $null
        }
        if ($shouldInspectOnly -or -not $nextMatch) {
            Set-StudioProForegroundWindow -Process $attached.Process
            Send-KeysToForegroundWindow -Keys "{RIGHT}" -DelayMs ($DelayMs + 100)
            $attached = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
            $currentMenuItems = Get-VisibleEditorMenuItems -Root $attached.Element
        }

        $step.availableItems = @($currentMenuItems | Select-Object -First 40)
    } else {
        $postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(1200, ($DelayMs * 4))) -PollMs 150 -Limit 30
        $attached = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
        $currentMenuItems = Get-VisibleEditorMenuItems -Root $attached.Element
    }

    $steps += $step
}

$payload = @{
    ok = $true
    action = "invoke-editor-menu-path"
    item = $Item
    element = $Element
    menuPath = $MenuPath
    dryRun = [bool]$DryRun
    openMethod = $contextMenu.EditorContext.OpenMethod
    tab = $contextMenu.EditorContext.Tab
    target = $contextMenu.Target
    targetSelection = $contextMenu.TargetSelection
    trigger = $contextMenu.Trigger
    steps = $steps
    remainingMenuItems = @($currentMenuItems | Select-Object -First 60)
    remainingMenuItemCount = @($currentMenuItems).Length
    postDialog = $postDialog
}

$payload | ConvertTo-Json -Depth 20
