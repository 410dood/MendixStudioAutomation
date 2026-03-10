param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$tabs = @(Get-OpenEditorTabs -Root $attached.Element)

$payload = @{
    ok = $true
    count = $tabs.Length
    items = $tabs
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
