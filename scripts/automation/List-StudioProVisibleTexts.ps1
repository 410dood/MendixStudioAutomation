param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Scope = "editor",
    [int]$Limit = 200
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern

switch ($Scope) {
    "appExplorer" {
        Activate-DockTabByName -Root $attached.Element -Name "App Explorer" | Out-Null
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    }
    "pageExplorer" {
        Activate-DockTabByName -Root $attached.Element -Name "Page Explorer" | Out-Null
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    }
    "toolbox" {
        Activate-DockTabByName -Root $attached.Element -Name "Toolbox" | Out-Null
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    }
}

$matches = Get-VisibleTextMatches -Root $attached.Element -Scope $Scope -Limit $Limit

$payload = @{
    ok = $true
    scope = $Scope
    count = $matches.Length
    items = $matches
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
