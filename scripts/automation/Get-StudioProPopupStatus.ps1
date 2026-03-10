param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$popups = Get-StudioProPopupSummary -Root $attached.Element

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    popupCount = $popups.Length
    popups = $popups
}

$payload | ConvertTo-Json -Depth 20
