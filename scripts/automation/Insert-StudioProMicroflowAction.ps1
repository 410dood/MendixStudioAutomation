param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Microflow = "",
    [string]$Target = "",
    [string]$ActionName = "",
    [int]$DelayMs = 250,
    [switch]$DryRun
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Microflow) {
    throw "A microflow name is required."
}

if (-not $Target) {
    throw "A target node name is required."
}

if (-not $ActionName) {
    throw "A toolbox action name is required."
}

$microflowContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Microflow -Scope "editor" -DelayMs $DelayMs
$attached = $microflowContext.Attached
$targetMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Target -Surface "editor" -Item $Microflow
if (-not $targetMatch) {
    throw "Could not find a visible microflow node named '$Target'."
}

Invoke-BoundsClick -Bounds $targetMatch.boundingRectangle | Out-Null
Start-Sleep -Milliseconds $DelayMs

$toolboxContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "toolbox" -DelayMs $DelayMs
$attached = $toolboxContext.Attached
$actionMatch = Select-ToolboxItemByName -Root $attached.Element -Item $ActionName
if (-not $actionMatch) {
    throw "Could not find a visible Toolbox action named '$ActionName'."
}

$method = "dryRun"
if (-not $DryRun) {
    $method = Invoke-BoundsDoubleClick -Bounds $actionMatch.boundingRectangle
    Start-Sleep -Milliseconds ($DelayMs + 150)
}

$payload = @{
    ok = $true
    action = "insert-action"
    microflow = $Microflow
    target = $Target
    actionName = $ActionName
    dryRun = [bool]$DryRun
    method = $method
    openMethod = $microflowContext.OpenMethod
    tab = $microflowContext.Tab
    resolvedTarget = $targetMatch
    resolvedAction = $actionMatch
}

$payload | ConvertTo-Json -Depth 20
