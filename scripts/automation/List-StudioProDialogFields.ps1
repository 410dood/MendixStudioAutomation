param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Dialog = "",
    [string]$ControlType = "",
    [string]$LabelContains = "",
    [int]$Limit = 200
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Dialog) {
    throw "A dialog name is required."
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

$items = @(Get-DialogNamedTextMatches -Dialog $nativeDialog -Limit ($Limit * 4))
$seenLabels = @{}
$fields = @()

foreach ($item in $items) {
    $label = [string]$item.name
    if (-not $label) {
        continue
    }

    $label = $label.Trim()
    if (-not $label) {
        continue
    }

    if ($LabelContains -and $label -notlike "*$LabelContains*") {
        continue
    }

    if ($seenLabels.ContainsKey($label)) {
        continue
    }
    $seenLabels[$label] = $true

    $fieldMatch = Find-DialogFieldByLabel -Dialog $nativeDialog -Label $label -ControlType $ControlType -Limit $Limit
    if (-not $fieldMatch -or -not $fieldMatch.field) {
        continue
    }

    $nativeField = $null
    if ($fieldMatch.field.runtimeId) {
        $nativeField = Resolve-NativeElementByRuntimeId -Root $nativeDialog -ExpectedRuntimeId $fieldMatch.field.runtimeId -Depth 15
    }

    $fields += @{
        label = $fieldMatch.label
        field = $fieldMatch.field
        observedValue = Get-DialogFieldObservedValue -Field $nativeField
    }

    if ($fields.Count -ge $Limit) {
        break
    }
}

$payload = @{
    ok = $true
    action = "list-dialog-fields"
    dialog = $Dialog
    controlType = $ControlType
    labelContains = $LabelContains
    count = $fields.Count
    fields = $fields
    window = $dialogMatch
}

$payload | ConvertTo-Json -Depth 20
