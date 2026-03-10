param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [int]$Depth = 5,
    [int]$MaxResults = 20,
    [string]$Name = "",
    [string]$AutomationId = "",
    [string]$ClassName = "",
    [string]$ControlType = "",
    [string]$RuntimeId = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$matches = Find-MatchingElements `
    -Root $attached.Element `
    -Depth $Depth `
    -MaxResults $MaxResults `
    -Name $Name `
    -AutomationId $AutomationId `
    -ClassName $ClassName `
    -ControlType $ControlType `
    -RuntimeId $RuntimeId

$payload = @{
    ok = $true
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
    count = $matches.Length
    matches = $matches
}

$payload | ConvertTo-Json -Depth 20
