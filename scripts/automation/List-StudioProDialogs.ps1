param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$windows = @(Get-StudioProWindowMatches -Root $attached.Element | Where-Object {
    $_.name -and $_.name -ne $attached.Process.MainWindowTitle
})

$payload = @{
    ok = $true
    count = $windows.Length
    items = $windows
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
