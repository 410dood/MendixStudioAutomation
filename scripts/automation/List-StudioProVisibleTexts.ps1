param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Page = "",
    [string]$Microflow = "",
    [string]$Scope = "editor",
    [int]$Limit = 200
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$contextItem = if ($Item) { $Item } elseif ($Page) { $Page } elseif ($Microflow) { $Microflow } else { "" }
$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $contextItem -Scope $Scope
$attached = $context.Attached

$matches = @(Get-VisibleTextMatches -Root $attached.Element -Scope $Scope -Item $contextItem -Limit $Limit)

$payload = @{
    ok = $true
    scope = $Scope
    item = $contextItem
    count = $matches.Length
    items = $matches
    openMethod = $context.OpenMethod
    tab = $context.Tab
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
