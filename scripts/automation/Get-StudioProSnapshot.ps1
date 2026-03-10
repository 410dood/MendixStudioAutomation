param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [int]$Depth = 2,
    [int]$MaxChildren = 25
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$root = Expand-AutomationTree -Element $attached.Element -Depth $Depth -MaxChildren $MaxChildren

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    snapshot = $root
}

$payload | ConvertTo-Json -Depth 20
