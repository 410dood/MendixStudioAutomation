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

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Microflow -Scope "editor" -DelayMs $DelayMs
$attached = $context.Attached
$bestMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Node -Surface "editor" -Item $Microflow
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
    openMethod = $context.OpenMethod
    tab = $context.Tab
    target = $bestMatch
}

$payload | ConvertTo-Json -Depth 20
