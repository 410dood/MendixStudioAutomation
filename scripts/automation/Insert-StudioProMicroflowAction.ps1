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

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$openMethod = Open-OrSelectStudioProItem -Process $attached.Process -Root $attached.Element -Item $Microflow -DelayMs $DelayMs
Start-Sleep -Milliseconds ($DelayMs + 150)

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
Set-StudioProForegroundWindow -Process $attached.Process
$targetMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Target -Surface "editor"
if (-not $targetMatch) {
    throw "Could not find a visible microflow node named '$Target'."
}

Invoke-BoundsClick -Bounds $targetMatch.boundingRectangle | Out-Null
Start-Sleep -Milliseconds $DelayMs

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$toolboxMatches = Find-MatchingElements -Root $attached.Element -Depth 10 -MaxResults 10 -Name "Toolbox" -ControlType "TabItem"
if ($toolboxMatches.Length -eq 0) {
    throw "Could not find the Toolbox tab."
}

$toolboxTab = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $toolboxMatches[0].runtimeId -Depth 10
if (-not $toolboxTab) {
    throw "Could not resolve the native Toolbox tab."
}

Invoke-ElementAction -Element $toolboxTab -Action "click" | Out-Null
Start-Sleep -Milliseconds $DelayMs

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
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
    openMethod = $openMethod
    resolvedTarget = $targetMatch
    resolvedAction = $actionMatch
}

$payload | ConvertTo-Json -Depth 20
