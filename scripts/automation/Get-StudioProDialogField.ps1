param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Dialog = "",
    [string]$Label = "",
    [string]$ControlType = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Dialog) {
    throw "A dialog name is required."
}

if (-not $Label) {
    throw "A dialog field label is required."
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

$fieldMatch = Find-DialogFieldByLabel -Dialog $nativeDialog -Label $Label -ControlType $ControlType
if (-not $fieldMatch) {
    throw "Could not find a visible '$Label' label in the Studio Pro dialog '$Dialog'."
}

if (-not $fieldMatch.field) {
    throw "Could not resolve an input field for label '$Label' in dialog '$Dialog'."
}

$nativeField = $null
if ($fieldMatch.field.runtimeId) {
    $nativeField = Resolve-NativeElementByRuntimeId -Root $nativeDialog -ExpectedRuntimeId $fieldMatch.field.runtimeId -Depth 15
}

$payload = @{
    ok = $true
    action = "get-dialog-field"
    dialog = $Dialog
    label = $Label
    controlType = $ControlType
    field = $fieldMatch.field
    labelMatch = $fieldMatch.label
    observedValue = Get-DialogFieldObservedValue -Field $nativeField
    window = $dialogMatch
}

$payload | ConvertTo-Json -Depth 20
