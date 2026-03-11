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

$match = $matches | Sort-Object `
    @{ Expression = { Get-ControlTypePriority -ControlType $_.controlType } }, `
    @{ Expression = { $_.boundingRectangle.top } }, `
    @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

$selection = Select-AutomationMatch -Root $nativeDialog -Match $match -DelayMs $DelayMs

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
}

$payload | ConvertTo-Json -Depth 20
