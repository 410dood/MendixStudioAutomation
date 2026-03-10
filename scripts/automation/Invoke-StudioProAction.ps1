param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Action = "click",
    [string]$RuntimeId = "",
    [string]$Name = "",
    [string]$AutomationId = "",
    [string]$ClassName = "",
    [string]$ControlType = ""
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$matches = Find-MatchingElements `
    -Root $attached.Element `
    -Depth 8 `
    -MaxResults 2 `
    -Name $Name `
    -AutomationId $AutomationId `
    -ClassName $ClassName `
    -ControlType $ControlType `
    -RuntimeId $RuntimeId

if ($matches.Length -eq 0) {
    throw "No matching Studio Pro element found for action."
}

if ($matches.Length -gt 1 -and -not $RuntimeId) {
    throw "Action selector matched multiple Studio Pro elements. Narrow the selector or pass a runtime id."
}

$targetMatch = $matches[0]
$elements = Find-MatchingElements `
    -Root $attached.Element `
    -Depth 8 `
    -MaxResults 1 `
    -RuntimeId $targetMatch.runtimeId

if ($elements.Length -eq 0) {
    throw "Could not resolve the target element for the requested action."
}

$resolved = $elements[0]
$targetElements = Find-MatchingElements `
    -Root $attached.Element `
    -Depth 8 `
    -MaxResults 1 `
    -RuntimeId $resolved.runtimeId

if ($targetElements.Length -eq 0) {
    throw "Could not reacquire the target automation element."
}

$targetRuntimeId = $resolved.runtimeId
$nativeTarget = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $targetRuntimeId -Depth 8
if (-not $nativeTarget) {
    throw "Could not find the native automation element by runtime id."
}

$method = Invoke-ElementAction -Element $nativeTarget -Action $Action

$payload = @{
    ok = $true
    action = $Action
    method = $method
    target = Convert-AutomationElement -Element $nativeTarget
}

$payload | ConvertTo-Json -Depth 20
