param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Element = "",
    [int]$OffsetX = 0,
    [int]$OffsetY = 0,
    [int]$DelayMs = 250
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Item) {
    throw "An editor item name is required."
}

if (-not $Element) {
    throw "An editor element name is required."
}

$editorContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Item -Scope "editor" -DelayMs $DelayMs
$attached = $editorContext.Attached
$target = Find-BestVisibleNamedElement -Root $attached.Element -Name $Element -Surface "editor" -Item $Item
if (-not $target) {
    throw "Could not find a visible editor element named '$Element'."
}

$targetSelection = Select-AutomationMatch -Root $attached.Element -Match $target -DelayMs ([Math]::Max(120, $DelayMs))
$bounds = $target.boundingRectangle
if ($null -eq $bounds.left -or $null -eq $bounds.top -or $null -eq $bounds.width -or $null -eq $bounds.height) {
    throw "The editor element '$Element' does not have usable bounds."
}

$point = @{
    x = [int][math]::Round($bounds.left + ($bounds.width / 2) + $OffsetX)
    y = [int][math]::Round($bounds.top + ($bounds.height / 2) + $OffsetY)
}

Set-StudioProForegroundWindow -Process $attached.Process
[NativeMouse]::SetCursorPos([int]$point.x, [int]$point.y) | Out-Null
Start-Sleep -Milliseconds 50
[NativeMouse]::mouse_event([NativeMouse]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 40
[NativeMouse]::mouse_event([NativeMouse]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
$method = "nativePointClick"
Start-Sleep -Milliseconds $DelayMs

$payload = @{
    ok = $true
    action = "click-editor-offset"
    item = $Item
    element = $Element
    offsetX = $OffsetX
    offsetY = $OffsetY
    openMethod = $editorContext.OpenMethod
    tab = $editorContext.Tab
    target = $target
    targetSelection = $targetSelection
    point = $point
    method = $method
}

$payload | ConvertTo-Json -Depth 12
