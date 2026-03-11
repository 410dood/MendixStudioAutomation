param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Dialog = "",
    [string]$Control = "",
    [string]$ControlType = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Dialog) {
    throw "A dialog name is required."
}

if (-not $Control) {
    throw "A dialog control name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$dialogMatch = Get-StudioProWindowMatchByName -Root $attached.Element -Name $Dialog
if (-not $dialogMatch) {
    throw "Could not find an open Studio Pro dialog named '$Dialog'."
}

$nativeDialog = Resolve-NativeWindowByName -Root $attached.Element -Name $Dialog -Depth 15
if (-not $nativeDialog) {
    throw "Could not attach to the native Studio Pro dialog '$Dialog'."
}

$matches = @(Find-DialogNamedElements -Dialog $nativeDialog -Name $Control -Limit 200 | Where-Object {
    $_.name -eq $Control -and (-not $ControlType -or $_.controlType -eq $ControlType)
})
if ($matches.Length -eq 0) {
    throw "Could not find a visible '$Control' control in the Studio Pro dialog '$Dialog'."
}

function Get-DialogControlPreference {
    param(
        [hashtable]$Match
    )

    switch ($Match.controlType) {
        "Button" { return 0 }
        "DataItem" { return 1 }
        "ListItem" { return 2 }
        "TreeItem" { return 3 }
        "CheckBox" { return 4 }
        "RadioButton" { return 5 }
        "ComboBox" { return 6 }
        "Edit" { return 7 }
        "MenuItem" { return 8 }
        "Text" { return 20 }
        default { return 30 }
    }
}

$match = $matches | Sort-Object `
    @{ Expression = { Get-DialogControlPreference -Match $_ } }, `
    @{ Expression = { $_.boundingRectangle.top } }, `
    @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

$selection = Select-AutomationMatch -Root $nativeDialog -Match $match -DelayMs $DelayMs

$dialogClosed = $false
$remainingDialog = $null
$deadline = [DateTime]::UtcNow.AddMilliseconds([Math]::Max(1200, ($DelayMs * 4)))
do {
    Start-Sleep -Milliseconds 120
    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    $remainingDialog = Get-StudioProWindowMatchByName -Root $attached.Element -Name $Dialog
    if (-not $remainingDialog) {
        $dialogClosed = $true
        break
    }
} while ([DateTime]::UtcNow -lt $deadline)

$payload = @{
    ok = $true
    action = "invoke-dialog-control"
    dialog = $Dialog
    control = $Control
    controlType = $ControlType
    method = $selection.method
    target = $selection.target
    isSelected = $selection.isSelected
    supportsSelectionItem = $selection.supportsSelectionItem
    supportsInvoke = $selection.supportsInvoke
    window = $dialogMatch
    dialogClosed = $dialogClosed
    remainingDialog = $remainingDialog
}

$payload | ConvertTo-Json -Depth 20
