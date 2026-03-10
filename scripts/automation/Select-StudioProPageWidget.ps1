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
if ($Page) {
    Open-StudioProItemByName -Process $attached.Process -Item $Page -DelayMs $DelayMs
    Start-Sleep -Milliseconds ($DelayMs + 150)
    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
}

Set-StudioProForegroundWindow -Process $attached.Process
$bestMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Widget -Surface $Surface
if (-not $bestMatch) {
    throw "Could not find a visible '$Widget' element on the requested surface."
}

$nativeTarget = Resolve-NativeElementByRuntimeId -Root $attached.Element -ExpectedRuntimeId $bestMatch.runtimeId -Depth 15
if (-not $nativeTarget) {
    throw "Could not resolve the native automation element for '$Widget'."
}

$method = Invoke-ElementAction -Element $nativeTarget -Action "click"

$payload = @{
    ok = $true
    action = "select-widget"
    page = $Page
    widget = $Widget
    surface = $Surface
    method = $method
    target = Convert-AutomationElement -Element $nativeTarget
}

$payload | ConvertTo-Json -Depth 20
