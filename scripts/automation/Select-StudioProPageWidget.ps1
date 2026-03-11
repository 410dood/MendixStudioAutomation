param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Page = "",
    [string]$Widget = "",
    [string]$Surface = "editor",
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Widget) {
    throw "A widget name is required."
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Page -Scope $Surface -DelayMs $DelayMs
$attached = $context.Attached
$bestMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Widget -Surface $Surface -Item $Page
if (-not $bestMatch) {
    throw "Could not find a visible '$Widget' element on the requested surface."
}

$nativeTarget = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $bestMatch.runtimeId -Depth 15
$method = $null
$target = $bestMatch
if ($nativeTarget) {
    $method = Invoke-ElementAction -Element $nativeTarget -Action "click"
    $target = Convert-AutomationElement -Element $nativeTarget
} else {
    $method = Invoke-BoundsClick -Bounds $bestMatch.boundingRectangle
}

$payload = @{
    ok = $true
    action = "select-widget"
    page = $Page
    widget = $Widget
    surface = $Surface
    method = $method
    openMethod = $context.OpenMethod
    tab = $context.Tab
    target = $target
}

$payload | ConvertTo-Json -Depth 20
