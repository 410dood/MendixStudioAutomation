param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Microflow = "",
    [string]$Node = "",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Microflow) {
    throw "A microflow name is required."
}

if (-not $Node) {
    throw "A microflow node name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
Open-StudioProItemByName -Process $attached.Process -Item $Microflow -DelayMs $DelayMs
Start-Sleep -Milliseconds ($DelayMs + 150)

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
Set-StudioProForegroundWindow -Process $attached.Process
$bestMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Node -Surface "editor"
if (-not $bestMatch) {
    throw "Could not find a visible microflow node named '$Node'."
}

$method = Invoke-BoundsClick -Bounds $bestMatch.boundingRectangle

$payload = @{
    ok = $true
    action = "select-microflow-node"
    microflow = $Microflow
    node = $Node
    method = $method
    target = $bestMatch
}

$payload | ConvertTo-Json -Depth 20
