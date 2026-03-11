param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Dialog = "",
    [string]$Name = "",
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

$items = @(Find-DialogNamedElements -Dialog $nativeDialog -Name $Name -Limit $Limit)

$payload = @{
    ok = $true
    dialog = $Dialog
    count = $items.Length
    items = $items
    window = $dialogMatch
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
