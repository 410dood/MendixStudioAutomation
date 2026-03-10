param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [int]$TimeoutMs = 60000,
    [int]$PollMs = 1000
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$result = Wait-ForStudioProReady -Process $attached.Process -TimeoutMs $TimeoutMs -PollMs $PollMs

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    ready = $result.ready
    popupCount = $result.popupCount
    timedOut = $result.timedOut
}

if ($result.ContainsKey("popups")) {
    $payload.popups = $result.popups
}

$payload | ConvertTo-Json -Depth 20
