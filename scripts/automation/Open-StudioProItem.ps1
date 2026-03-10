param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An item name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$openMethod = Open-OrSelectStudioProItem -Process $attached.Process -Root $attached.Element -Item $Item -DelayMs $DelayMs

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    action = "open-item"
    item = $Item
    method = $openMethod.method
    tab = $openMethod.tab
}

$payload | ConvertTo-Json -Depth 10
