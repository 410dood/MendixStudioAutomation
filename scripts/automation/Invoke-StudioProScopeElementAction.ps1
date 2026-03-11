param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Scope = "editor",
    [string]$RuntimeId = "",
    [string]$Action = "click",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "A Studio Pro item is required."
}

if (-not $RuntimeId) {
    throw "A runtime id is required."
}

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Item -Scope $Scope -DelayMs $DelayMs
$attached = $context.Attached
$searchRoot = Get-ScopeSearchRoot -Root $attached.Element -Scope $Scope -Item $Item

$matches = @(Find-MatchingElements -Root $searchRoot -Depth 25 -MaxResults 5 -RuntimeId $RuntimeId)
if ($matches.Length -eq 0) {
    throw "No scoped Studio Pro element matched runtime id '$RuntimeId'."
}

$match = $matches[0]
$native = Resolve-NativeElementByRuntimeId -Root $searchRoot -ExpectedRuntimeId $RuntimeId -Depth 25
if (-not $native) {
    throw "Could not resolve the native scoped element by runtime id."
}

switch ($Action) {
    "rightClick" {
        $method = Invoke-BoundsRightClick -Bounds $match.boundingRectangle
    }
    "doubleClick" {
        $method = Invoke-BoundsDoubleClick -Bounds $match.boundingRectangle
    }
    default {
        $method = Invoke-ElementAction -Element $native -Action "click"
    }
}

Start-Sleep -Milliseconds $DelayMs

$payload = @{
    ok = $true
    action = "scope-element-action"
    item = $Item
    scope = $Scope
    requestedAction = $Action
    runtimeId = $RuntimeId
    openMethod = $context.OpenMethod
    tab = $context.Tab
    method = $method
    target = Convert-AutomationElement -Element $native
}

$payload | ConvertTo-Json -Depth 12
