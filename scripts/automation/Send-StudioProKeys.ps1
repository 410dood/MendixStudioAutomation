param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Page = "",
    [string]$Microflow = "",
    [string]$Scope = "",
    [string]$Keys = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Keys) {
    throw "A Studio Pro key chord is required."
}

$contextItem = if ($Item) { $Item } elseif ($Page) { $Page } elseif ($Microflow) { $Microflow } else { "" }

if ($Scope) {
    $context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $contextItem -Scope $Scope -DelayMs $DelayMs
    $attached = $context.Attached
    $openMethod = $context.OpenMethod
    $tab = $context.Tab
} else {
    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    $openMethod = $null
    $tab = $null

    if ($contextItem) {
        $context = Open-OrSelectStudioProItemAndAttach -Process $attached.Process -Root $attached.Element -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Item $contextItem -DelayMs $DelayMs
        $attached = $context.Attached
        $openMethod = $context.OpenMethod
        $tab = $context.Tab
    }

    Set-StudioProForegroundWindow -Process $attached.Process
}

Send-KeysToForegroundWindow -Keys $Keys -DelayMs $DelayMs

$payload = @{
    ok = $true
    action = "send-keys"
    item = $contextItem
    scope = $Scope
    keys = $Keys
    openMethod = $openMethod
    tab = $tab
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
