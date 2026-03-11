param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$tab = Get-ActiveEditorTab -Root $attached.Element

$payload = @{
    ok = $true
    activeTab = $tab
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
