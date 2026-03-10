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
Set-StudioProForegroundWindow -Process $attached.Process

Send-KeysToForegroundWindow -Keys "^{g}" -DelayMs $DelayMs
Send-KeysToForegroundWindow -Keys "^(a)" -DelayMs 100
Send-KeysToForegroundWindow -Keys "{BACKSPACE}" -DelayMs 100
Send-KeysToForegroundWindow -Keys (Escape-SendKeysText -Text $Item) -DelayMs $DelayMs
Send-KeysToForegroundWindow -Keys "{ENTER}" -DelayMs ($DelayMs + 150)

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    action = "open-item"
    item = $Item
    method = "ctrl+g"
}

$payload | ConvertTo-Json -Depth 10
