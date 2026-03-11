Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

if (-not ("NativeMouse" -as [type])) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeMouse
{
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    public const uint LEFTDOWN = 0x0002;
    public const uint LEFTUP = 0x0004;
    public const uint RIGHTDOWN = 0x0008;
    public const uint RIGHTUP = 0x0010;
}
"@
}

if (-not ("NativeWindowMethods" -as [type])) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeWindowMethods
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@
}

function Get-StudioProProcess {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = ""
    )

    $candidates = Get-Process | Where-Object {
        $_.MainWindowHandle -ne 0 -and (
            $_.ProcessName -match "^studiopro$|mendix" -or
            $_.MainWindowTitle -match "Mendix Studio Pro"
        )
    }

    if ($ProcessId -gt 0) {
        $candidates = $candidates | Where-Object { $_.Id -eq $ProcessId }
    }

    if ($WindowTitlePattern) {
        $candidates = $candidates | Where-Object { $_.MainWindowTitle -match $WindowTitlePattern }
    }

    $preferred = $candidates | Where-Object { $_.ProcessName -match "^studiopro$" }
    if ($preferred) {
        return $preferred | Sort-Object Id | Select-Object -First 1
    }

    return $candidates | Sort-Object Id | Select-Object -First 1
}

function Get-StudioProWindowElement {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = ""
    )

    $process = Get-StudioProProcess -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    if (-not $process) {
        throw "Could not find a running Mendix Studio Pro window."
    }

    $element = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    if (-not $element) {
        throw "Could not attach to the Mendix Studio Pro main window."
    }

    return @{
        Process = $process
        Element = $element
    }
}

function Convert-BoundingRectangle {
    param(
        [System.Windows.Rect]$Rect
    )

    $left = if ([double]::IsInfinity($Rect.Left) -or [double]::IsNaN($Rect.Left)) { $null } else { [math]::Round($Rect.Left, 2) }
    $top = if ([double]::IsInfinity($Rect.Top) -or [double]::IsNaN($Rect.Top)) { $null } else { [math]::Round($Rect.Top, 2) }
    $width = if ([double]::IsInfinity($Rect.Width) -or [double]::IsNaN($Rect.Width)) { $null } else { [math]::Round($Rect.Width, 2) }
    $height = if ([double]::IsInfinity($Rect.Height) -or [double]::IsNaN($Rect.Height)) { $null } else { [math]::Round($Rect.Height, 2) }

    return @{
        left = $left
        top = $top
        width = $width
        height = $height
        right = if ($left -ne $null -and $width -ne $null) { [math]::Round($left + $width, 2) } else { $null }
        bottom = if ($top -ne $null -and $height -ne $null) { [math]::Round($top + $height, 2) } else { $null }
    }
}

function Convert-AutomationElement {
    param(
        [System.Windows.Automation.AutomationElement]$Element
    )

    $runtimeId = @()
    try {
        $runtimeId = $Element.GetRuntimeId()
    } catch {
        $runtimeId = @()
    }

    $isSelected = $null
    try {
        $selectionItemPattern = $null
        if ($Element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$selectionItemPattern)) {
            $isSelected = [bool]$selectionItemPattern.Current.IsSelected
        }
    } catch {
        $isSelected = $null
    }

    $textValue = $null
    try {
        $valuePattern = $null
        if ($Element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$valuePattern)) {
            $textValue = $valuePattern.Current.Value
        }
    } catch {
        $textValue = $null
    }

    $toggleState = $null
    $isToggled = $null
    try {
        $togglePattern = $null
        if ($Element.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$togglePattern)) {
            $toggleState = [string]$togglePattern.Current.ToggleState
            if ($togglePattern.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::Indeterminate) {
                $isToggled = $togglePattern.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
            }
        }
    } catch {
        $toggleState = $null
        $isToggled = $null
    }

    return @{
        name = $Element.Current.Name
        automationId = $Element.Current.AutomationId
        className = $Element.Current.ClassName
        controlType = $Element.Current.ControlType.ProgrammaticName -replace "^ControlType\.", ""
        frameworkId = $Element.Current.FrameworkId
        runtimeId = ($runtimeId | ForEach-Object { [string]$_ }) -join "."
        processId = $Element.Current.ProcessId
        isEnabled = $Element.Current.IsEnabled
        isOffscreen = $Element.Current.IsOffscreen
        isSelected = $isSelected
        textValue = $textValue
        toggleState = $toggleState
        isToggled = $isToggled
        boundingRectangle = Convert-BoundingRectangle -Rect $Element.Current.BoundingRectangle
    }
}

function Get-ChildElements {
    param(
        [System.Windows.Automation.AutomationElement]$Element
    )

    $collection = $Element.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition
    )

    if ($null -eq $collection) {
        return @()
    }

    $children = @()
    for ($index = 0; $index -lt $collection.Count; $index++) {
        $children += $collection.Item($index)
    }

    return @($children)
}

function Get-AutomationParent {
    param(
        [System.Windows.Automation.AutomationElement]$Element
    )

    if (-not $Element) {
        return $null
    }

    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    return $walker.GetParent($Element)
}

function Expand-AutomationTree {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [int]$Depth,
        [int]$MaxChildren
    )

    $node = Convert-AutomationElement -Element $Element
    if ($Depth -le 0) {
        return $node
    }

    $children = @(Get-ChildElements -Element $Element)
    $childNodes = @()
    $childCount = $children.Length
    $limit = [math]::Min($childCount, $MaxChildren)
    for ($index = 0; $index -lt $limit; $index++) {
        $childNodes += Expand-AutomationTree -Element $children[$index] -Depth ($Depth - 1) -MaxChildren $MaxChildren
    }

    $node.children = $childNodes
    $node.totalChildren = $childCount
    return $node
}

function Test-ElementMatch {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Name = "",
        [string]$AutomationId = "",
        [string]$ClassName = "",
        [string]$ControlType = "",
        [string]$RuntimeId = ""
    )

    if ($Name -and $Element.Current.Name -notlike "*$Name*") {
        return $false
    }

    if ($AutomationId -and $Element.Current.AutomationId -ne $AutomationId) {
        return $false
    }

    if ($ClassName -and $Element.Current.ClassName -ne $ClassName) {
        return $false
    }

    if ($ControlType) {
        $programmaticName = $null
        try {
            $programmaticName = $Element.Current.ControlType.ProgrammaticName
        } catch {
            $programmaticName = $null
        }

        if (-not $programmaticName) {
            return $false
        }

        $actualControlType = $programmaticName -replace "^ControlType\.", ""
        if ($actualControlType -ne $ControlType) {
            return $false
        }
    }

    if ($RuntimeId) {
        $actualRuntimeId = ""
        try {
            $actualRuntimeId = ($Element.GetRuntimeId() | ForEach-Object { [string]$_ }) -join "."
        } catch {
            $actualRuntimeId = ""
        }

        if ($actualRuntimeId -ne $RuntimeId) {
            return $false
        }
    }

    return $true
}

function Find-MatchingElements {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [int]$Depth,
        [int]$MaxResults,
        [string]$Name = "",
        [string]$AutomationId = "",
        [string]$ClassName = "",
        [string]$ControlType = "",
        [string]$RuntimeId = ""
    )

    $results = New-Object System.Collections.Generic.List[object]

    function Search-Node {
        param(
            [System.Windows.Automation.AutomationElement]$Node,
            [int]$CurrentDepth
        )

        if ($results.Count -ge $MaxResults) {
            return
        }

        if (Test-ElementMatch -Element $Node -Name $Name -AutomationId $AutomationId -ClassName $ClassName -ControlType $ControlType -RuntimeId $RuntimeId) {
            $results.Add((Convert-AutomationElement -Element $Node))
        }

        if ($CurrentDepth -le 0) {
            return
        }

        $children = @(Get-ChildElements -Element $Node)
        $childCount = $children.Length
        for ($index = 0; $index -lt $childCount; $index++) {
            Search-Node -Node $children[$index] -CurrentDepth ($CurrentDepth - 1)
            if ($results.Count -ge $MaxResults) {
                return
            }
        }
    }

    Search-Node -Node $Root -CurrentDepth $Depth
    return @($results.ToArray())
}

function Invoke-ElementAction {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Action = "click"
    )

    switch ($Action) {
        "focus" {
            $Element.SetFocus()
            return "focus"
        }
        "invoke" {
            $pattern = $null
            if ($Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
                $pattern.Invoke()
                return "invokePattern"
            }
            throw "Element does not support InvokePattern."
        }
        default {
            $invokePattern = $null
            if ($Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invokePattern)) {
                $invokePattern.Invoke()
                return "invokePattern"
            }

            $selectionPattern = $null
            if ($Element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$selectionPattern)) {
                $selectionPattern.Select()
                return "selectionItemPattern"
            }

            $rect = $Element.Current.BoundingRectangle
            if ($rect.Width -le 1 -or $rect.Height -le 1) {
                throw "Element cannot be clicked because it has no usable bounding rectangle."
            }

            $x = [int]($rect.Left + ($rect.Width / 2))
            $y = [int]($rect.Top + ($rect.Height / 2))
            [NativeMouse]::SetCursorPos($x, $y) | Out-Null
            Start-Sleep -Milliseconds 50
            [NativeMouse]::mouse_event([NativeMouse]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 50
            [NativeMouse]::mouse_event([NativeMouse]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
            return "nativeClick"
        }
    }
}

function Invoke-BoundsClick {
    param(
        [hashtable]$Bounds
    )

    if ($null -eq $Bounds -or $null -eq $Bounds.left -or $null -eq $Bounds.top -or $null -eq $Bounds.width -or $null -eq $Bounds.height) {
        throw "Cannot click because the target bounds are incomplete."
    }

    if ($Bounds.width -le 1 -or $Bounds.height -le 1) {
        throw "Cannot click because the target bounds are too small."
    }

    $x = [int]($Bounds.left + ($Bounds.width / 2))
    $y = [int]($Bounds.top + ($Bounds.height / 2))
    [NativeMouse]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 50
    [NativeMouse]::mouse_event([NativeMouse]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [NativeMouse]::mouse_event([NativeMouse]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    return "nativeBoundsClick"
}

function Invoke-BoundsDoubleClick {
    param(
        [hashtable]$Bounds
    )

    $first = Invoke-BoundsClick -Bounds $Bounds
    Start-Sleep -Milliseconds 100
    $second = Invoke-BoundsClick -Bounds $Bounds
    return "nativeBoundsDoubleClick"
}

function Invoke-BoundsRightClick {
    param(
        [hashtable]$Bounds,
        [ValidateSet("left", "center", "right")]
        [string]$Horizontal = "center",
        [ValidateSet("top", "center", "bottom")]
        [string]$Vertical = "center",
        [int]$Inset = 12
    )

    if ($null -eq $Bounds -or $null -eq $Bounds.left -or $null -eq $Bounds.top -or $null -eq $Bounds.width -or $null -eq $Bounds.height) {
        throw "Cannot right-click because the target bounds are incomplete."
    }

    if ($Bounds.width -le 1 -or $Bounds.height -le 1) {
        throw "Cannot right-click because the target bounds are too small."
    }

    $point = Get-BoundsPoint -Bounds $Bounds -Horizontal $Horizontal -Vertical $Vertical -Inset $Inset
    [NativeMouse]::SetCursorPos([int]$point.x, [int]$point.y) | Out-Null
    Start-Sleep -Milliseconds 50
    [NativeMouse]::mouse_event([NativeMouse]::RIGHTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [NativeMouse]::mouse_event([NativeMouse]::RIGHTUP, 0, 0, 0, [UIntPtr]::Zero)
    return "nativeBoundsRightClick"
}

function Get-BoundsPoint {
    param(
        [hashtable]$Bounds,
        [ValidateSet("left", "center", "right")]
        [string]$Horizontal = "center",
        [ValidateSet("top", "center", "bottom")]
        [string]$Vertical = "center",
        [int]$Inset = 12
    )

    if ($null -eq $Bounds -or $null -eq $Bounds.left -or $null -eq $Bounds.top -or $null -eq $Bounds.width -or $null -eq $Bounds.height) {
        throw "Cannot derive a point because the target bounds are incomplete."
    }

    if ($Bounds.width -le 1 -or $Bounds.height -le 1) {
        throw "Cannot derive a point because the target bounds are too small."
    }

    $left = [double]$Bounds.left
    $top = [double]$Bounds.top
    $right = $left + [double]$Bounds.width
    $bottom = $top + [double]$Bounds.height
    $safeInsetX = [math]::Min([math]::Max($Inset, 0), [math]::Max([int]($Bounds.width / 2), 0))
    $safeInsetY = [math]::Min([math]::Max($Inset, 0), [math]::Max([int]($Bounds.height / 2), 0))

    $x = switch ($Horizontal) {
        "left" { $left + $safeInsetX }
        "right" { $right - $safeInsetX }
        default { $left + ([double]$Bounds.width / 2) }
    }

    $y = switch ($Vertical) {
        "top" { $top + $safeInsetY }
        "bottom" { $bottom - $safeInsetY }
        default { $top + ([double]$Bounds.height / 2) }
    }

    return @{
        x = [int][math]::Round($x)
        y = [int][math]::Round($y)
    }
}

function Invoke-PointDrag {
    param(
        [hashtable]$StartPoint,
        [hashtable]$EndPoint,
        [int]$Steps = 18,
        [int]$InitialHoldMs = 120,
        [int]$StepDelayMs = 18,
        [int]$FinalHoldMs = 120
    )

    if ($null -eq $StartPoint -or $null -eq $StartPoint.x -or $null -eq $StartPoint.y) {
        throw "Cannot drag because the start point is incomplete."
    }

    if ($null -eq $EndPoint -or $null -eq $EndPoint.x -or $null -eq $EndPoint.y) {
        throw "Cannot drag because the end point is incomplete."
    }

    $stepCount = [math]::Max($Steps, 2)
    [NativeMouse]::SetCursorPos([int]$StartPoint.x, [int]$StartPoint.y) | Out-Null
    Start-Sleep -Milliseconds 60
    [NativeMouse]::mouse_event([NativeMouse]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $InitialHoldMs

    for ($step = 1; $step -le $stepCount; $step++) {
        $progress = [double]$step / [double]$stepCount
        $x = [int][math]::Round($StartPoint.x + (($EndPoint.x - $StartPoint.x) * $progress))
        $y = [int][math]::Round($StartPoint.y + (($EndPoint.y - $StartPoint.y) * $progress))
        [NativeMouse]::SetCursorPos($x, $y) | Out-Null
        Start-Sleep -Milliseconds $StepDelayMs
    }

    Start-Sleep -Milliseconds $FinalHoldMs
    [NativeMouse]::mouse_event([NativeMouse]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    return "nativePointDrag"
}

function Invoke-BoundsDrag {
    param(
        [hashtable]$SourceBounds,
        [hashtable]$TargetBounds,
        [ValidateSet("left", "center", "right")]
        [string]$SourceHorizontal = "center",
        [ValidateSet("top", "center", "bottom")]
        [string]$SourceVertical = "center",
        [ValidateSet("left", "center", "right")]
        [string]$TargetHorizontal = "center",
        [ValidateSet("top", "center", "bottom")]
        [string]$TargetVertical = "center",
        [int]$Inset = 12,
        [int]$Steps = 18,
        [int]$InitialHoldMs = 120,
        [int]$StepDelayMs = 18,
        [int]$FinalHoldMs = 120
    )

    $startPoint = Get-BoundsPoint -Bounds $SourceBounds -Horizontal $SourceHorizontal -Vertical $SourceVertical -Inset $Inset
    $endPoint = Get-BoundsPoint -Bounds $TargetBounds -Horizontal $TargetHorizontal -Vertical $TargetVertical -Inset $Inset
    $method = Invoke-PointDrag -StartPoint $startPoint -EndPoint $endPoint -Steps $Steps -InitialHoldMs $InitialHoldMs -StepDelayMs $StepDelayMs -FinalHoldMs $FinalHoldMs
    return @{
        method = "nativeBoundsDrag"
        innerMethod = $method
        startPoint = $startPoint
        endPoint = $endPoint
    }
}

function Test-RectangleWithinBounds {
    param(
        [hashtable]$Bounds,
        [hashtable]$Container
    )

    if ($null -eq $Bounds -or $null -eq $Container) {
        return $false
    }

    foreach ($property in @("left", "top", "right", "bottom")) {
        if ($null -eq $Bounds[$property] -or $null -eq $Container[$property]) {
            return $false
        }
    }

    return (
        $Bounds.left -ge $Container.left -and
        $Bounds.top -ge $Container.top -and
        $Bounds.right -le $Container.right -and
        $Bounds.bottom -le $Container.bottom
    )
}

function Set-StudioProForegroundWindow {
    param(
        [System.Diagnostics.Process]$Process
    )

    if (-not $Process -or $Process.MainWindowHandle -eq 0) {
        throw "Cannot foreground Studio Pro because no main window handle is available."
    }

    [NativeWindowMethods]::ShowWindowAsync($Process.MainWindowHandle, 9) | Out-Null
    Start-Sleep -Milliseconds 100
    [NativeWindowMethods]::SetForegroundWindow($Process.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 150
}

function Send-KeysToForegroundWindow {
    param(
        [string]$Keys,
        [int]$DelayMs = 200
    )

    [System.Windows.Forms.SendKeys]::SendWait($Keys)
    Start-Sleep -Milliseconds $DelayMs
}

function Escape-SendKeysText {
    param(
        [string]$Text
    )

    if ($null -eq $Text) {
        return ""
    }

    $escaped = $Text
    foreach ($symbol in @("{", "}", "+", "^", "%", "~", "(", ")")) {
        $escaped = $escaped.Replace($symbol, "{$symbol}")
    }

    return $escaped
}

function Normalize-MenuCaption {
    param(
        [string]$Text
    )

    if ($null -eq $Text) {
        return ""
    }

    $normalized = $Text.Trim().ToLowerInvariant()
    $normalized = $normalized.Replace("…", "")
    $normalized = $normalized.Replace("...", "")
    $normalized = $normalized.Replace("â€¦", "")
    return $normalized.Trim()
}

function Find-MenuItemMatch {
    param(
        [object[]]$MenuItems,
        [string]$MenuItemName
    )

    if (-not $MenuItems -or -not $MenuItemName) {
        return $null
    }

    $normalizedExpected = Normalize-MenuCaption -Text $MenuItemName
    $exact = @($MenuItems | Where-Object {
        (Normalize-MenuCaption -Text $_.name) -eq $normalizedExpected
    } | Select-Object -First 1)
    if ($exact.Length -gt 0) {
        return $exact[0]
    }

    $prefix = @($MenuItems | Where-Object {
        $candidate = Normalize-MenuCaption -Text $_.name
        $candidate.StartsWith($normalizedExpected)
    } | Select-Object -First 1)
    if ($prefix.Length -gt 0) {
        return $prefix[0]
    }

    return $null
}

function Open-StudioProItemByName {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Item,
        [int]$DelayMs = 250
    )

    if (-not $Item) {
        throw "An item name is required."
    }

    Set-StudioProForegroundWindow -Process $Process
    Send-KeysToForegroundWindow -Keys "^{g}" -DelayMs $DelayMs
    Send-KeysToForegroundWindow -Keys "^(a)" -DelayMs 100
    Send-KeysToForegroundWindow -Keys "{BACKSPACE}" -DelayMs 100
    Send-KeysToForegroundWindow -Keys (Escape-SendKeysText -Text $Item) -DelayMs $DelayMs
    Send-KeysToForegroundWindow -Keys "{ENTER}" -DelayMs ($DelayMs + 150)
}

function Get-DockTabMatch {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $matches = @(Find-MatchingElements -Root $Root -Depth 12 -MaxResults 10 -Name $Name -ControlType "TabItem")
    if ($matches.Length -eq 0) {
        return $null
    }

    return ($matches | Where-Object { -not $_.isOffscreen } | Sort-Object @{
        Expression = { $_.boundingRectangle.left }
    } | Select-Object -First 1)
}

function Activate-DockTabByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [int]$DelayMs = 250
    )

    $match = Get-DockTabMatch -Root $Root -Name $Name
    if (-not $match) {
        throw "Could not find the '$Name' dock tab."
    }

    $nativeTab = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $match.runtimeId -Depth 12
    if (-not $nativeTab) {
        throw "Could not resolve the native '$Name' dock tab."
    }

    Invoke-BoundsClick -Bounds $match.boundingRectangle | Out-Null
    Start-Sleep -Milliseconds $DelayMs
    return $match
}

function Get-DockContainerByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $match = Get-DockTabMatch -Root $Root -Name $Name
    if (-not $match) {
        return $null
    }

    $nativeTab = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $match.runtimeId -Depth 12
    if (-not $nativeTab) {
        return $null
    }

    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    return $walker.GetParent($nativeTab)
}

function Get-OpenEditorContainerByItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    if (-not $Item) {
        return $null
    }

    $tab = Find-OpenEditorTabForItem -Root $Root -Item $Item
    if (-not $tab) {
        return $null
    }

    $nativeTab = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $tab.runtimeId -Depth 12
    if (-not $nativeTab) {
        return $null
    }

    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    return $walker.GetParent($nativeTab)
}

function Get-OpenEditorContentBoundsByItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    $container = Get-OpenEditorContainerByItem -Root $Root -Item $Item
    $tab = Find-OpenEditorTabForItem -Root $Root -Item $Item
    if (-not $container -or -not $tab) {
        return $null
    }

    $containerBounds = (Convert-AutomationElement -Element $container).boundingRectangle
    if ($null -eq $containerBounds.left -or $null -eq $containerBounds.top -or $null -eq $containerBounds.right -or $null -eq $containerBounds.bottom) {
        return $null
    }

    return @{
        left = $containerBounds.left
        top = $tab.boundingRectangle.bottom + 4
        right = $containerBounds.right
        bottom = $containerBounds.bottom
    }
}

function Wait-ForOpenEditorTabByItem {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Item,
        [int]$TimeoutMs = 4000,
        [int]$PollMs = 250
    )

    if (-not $Item) {
        return $null
    }

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    do {
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
        $tab = Find-OpenEditorTabForItem -Root $attached.Element -Item $Item
        if ($tab) {
            return @{
                Attached = $attached
                Tab = $tab
            }
        }

        Start-Sleep -Milliseconds $PollMs
    } while ([DateTime]::UtcNow -lt $deadline)

    return $null
}

function Open-OrSelectStudioProItemAndAttach {
    param(
        [System.Diagnostics.Process]$Process,
        [System.Windows.Automation.AutomationElement]$Root,
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Item = "",
        [int]$DelayMs = 250,
        [int]$TimeoutMs = 4000
    )

    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    if (-not $Item) {
        return @{
            Attached = $attached
            OpenMethod = $null
            Tab = Get-ActiveEditorTab -Root $attached.Element
        }
    }

    $openMethod = Open-OrSelectStudioProItem -Process $Process -Root $Root -Item $Item -DelayMs $DelayMs
    Start-Sleep -Milliseconds ($DelayMs + 150)

    $waitResult = Wait-ForOpenEditorTabByItem -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern -Item $Item -TimeoutMs $TimeoutMs
    if ($waitResult) {
        $attached = $waitResult.Attached
        $tab = $waitResult.Tab
    } else {
        $attached = Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern
        $tab = Find-OpenEditorTabForItem -Root $attached.Element -Item $Item
    }

    if ($tab) {
        if (-not $tab.isOffscreen) {
            Invoke-BoundsClick -Bounds $tab.boundingRectangle | Out-Null
            Start-Sleep -Milliseconds $DelayMs
            $attached = Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern
        }
    }

    return @{
        Attached = $attached
        OpenMethod = $openMethod
        Tab = $tab
    }
}

function Focus-StudioProScope {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor",
        [string]$Item = "",
        [int]$DelayMs = 250
    )

    switch ($Scope) {
        "appExplorer" {
            Activate-DockTabByName -Root $Root -Name "App Explorer" -DelayMs $DelayMs | Out-Null
            return
        }
        "pageExplorer" {
            Activate-DockTabByName -Root $Root -Name "Page Explorer" -DelayMs $DelayMs | Out-Null
            return
        }
        "toolbox" {
            Activate-DockTabByName -Root $Root -Name "Toolbox" -DelayMs $DelayMs | Out-Null
            return
        }
        "properties" {
            Activate-DockTabByName -Root $Root -Name "Properties" -DelayMs $DelayMs | Out-Null
            return
        }
        default {
            $bounds = if ($Item) {
                $editorBounds = Get-OpenEditorContentBoundsByItem -Root $Root -Item $Item
                if ($editorBounds) { $editorBounds } else { Get-ScopeBounds -Root $Root -Scope "editor" }
            } else {
                Get-ScopeBounds -Root $Root -Scope "editor"
            }
            if ($null -eq $bounds.left -or $null -eq $bounds.top -or $null -eq $bounds.right -or $null -eq $bounds.bottom) {
                return
            }

            $width = $bounds.right - $bounds.left
            $height = $bounds.bottom - $bounds.top
            if ($width -le 20 -or $height -le 20) {
                return
            }

            Invoke-BoundsClick -Bounds @{
                left = $bounds.left + [Math]::Max(16, [int]($width * 0.25))
                top = $bounds.top + [Math]::Max(16, [int]($height * 0.25))
                width = [Math]::Max(24, [int]($width * 0.5))
                height = [Math]::Max(24, [int]($height * 0.5))
            } | Out-Null
            Start-Sleep -Milliseconds $DelayMs
            return
        }
    }
}

function Enter-StudioProScope {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Item = "",
        [string]$Scope = "editor",
        [int]$DelayMs = 250
    )

    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    $openMethod = $null
    $tab = $null

    if ($Item) {
        $context = Open-OrSelectStudioProItemAndAttach -Process $attached.Process -Root $attached.Element -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Item $Item -DelayMs $DelayMs
        $attached = $context.Attached
        $openMethod = $context.OpenMethod
        $tab = $context.Tab
        if (-not $tab) {
            throw "Could not confirm that Studio Pro opened '$Item'."
        }
    }

    Set-StudioProForegroundWindow -Process $attached.Process
    Focus-StudioProScope -Root $attached.Element -Scope $Scope -Item $Item -DelayMs $DelayMs
    $attached = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern

    return @{
        Attached = $attached
        OpenMethod = $openMethod
        Tab = $tab
        Scope = $Scope
    }
}

function Get-ScopeBounds {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor"
    )

    $rootBounds = (Convert-AutomationElement -Element $Root).boundingRectangle
    $rawTabItems = @(Find-MatchingElements -Root $Root -Depth 12 -MaxResults 100 -ControlType "TabItem")
    $tabItems = @()
    foreach ($candidate in $rawTabItems) {
        if ($null -eq $candidate) {
            continue
        }

        $bounds = $candidate.boundingRectangle
        if ($candidate.isOffscreen -or $null -eq $bounds) {
            continue
        }

        if ($null -eq $bounds.left -or $null -eq $bounds.top -or $null -eq $bounds.bottom) {
            continue
        }

        $tabItems += $candidate
    }

    $topTabs = @()
    $bottomTabs = @()
    foreach ($tab in $tabItems) {
        if ($tab.boundingRectangle.top -lt ($rootBounds.top + 300)) {
            $topTabs += $tab
        } else {
            $bottomTabs += $tab
        }
    }

    $documentTabs = @($topTabs | Where-Object { $_.name -match "\[[^\]]+\]" })
    $leftTabs = @($topTabs | Where-Object { $_.name -in @("App Explorer", "Page Explorer", "Variables") })
    $rightTabs = @($topTabs | Where-Object { $_.name -in @("Properties", "Toolbox", "Marketplace", "Integration", "Connector", "Maia") })

    $bottomBoundary = if ($bottomTabs.Count -gt 0) {
        (($bottomTabs | Sort-Object @{ Expression = { $_.boundingRectangle.top } } | Select-Object -First 1).boundingRectangle.top) - 8
    } else {
        $rootBounds.bottom
    }

    $leftDockLeft = if ($leftTabs.Count -gt 0) {
        ($leftTabs | Sort-Object @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1).boundingRectangle.left
    } else {
        $rootBounds.left
    }

    $leftDockRight = if ($leftTabs.Count -gt 0) {
        ($leftTabs | Sort-Object @{ Expression = { $_.boundingRectangle.right } } -Descending | Select-Object -First 1).boundingRectangle.right
    } else {
        $rootBounds.left + 320
    }

    $rightDockLeft = if ($rightTabs.Count -gt 0) {
        ($rightTabs | Sort-Object @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1).boundingRectangle.left
    } else {
        $rootBounds.right
    }

    $editorLeft = if ($documentTabs.Count -gt 0) {
        [math]::Min(
            ($documentTabs | Sort-Object @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1).boundingRectangle.left,
            $leftDockRight + 8
        )
    } else {
        $leftDockRight + 8
    }

    $editorRight = if ($rightDockLeft -gt ($editorLeft + 40)) {
        $rightDockLeft - 8
    } else {
        $rootBounds.right - 8
    }

    $leftDockTop = if ($leftTabs.Count -gt 0) {
        (($leftTabs | Sort-Object @{ Expression = { $_.boundingRectangle.bottom } } -Descending | Select-Object -First 1).boundingRectangle.bottom) + 4
    } else {
        $rootBounds.top
    }

    $rightDockTop = if ($rightTabs.Count -gt 0) {
        (($rightTabs | Sort-Object @{ Expression = { $_.boundingRectangle.bottom } } -Descending | Select-Object -First 1).boundingRectangle.bottom) + 4
    } else {
        $rootBounds.top
    }

    $editorTop = if ($documentTabs.Count -gt 0) {
        (($documentTabs | Sort-Object @{ Expression = { $_.boundingRectangle.bottom } } -Descending | Select-Object -First 1).boundingRectangle.bottom) + 4
    } else {
        [math]::Min($leftDockTop, $rightDockTop)
    }

    switch ($Scope) {
        "appExplorer" {
            return @{
                left = $leftDockLeft
                top = $leftDockTop
                right = $leftDockRight
                bottom = $bottomBoundary
            }
        }
        "pageExplorer" {
            return @{
                left = $leftDockLeft
                top = $leftDockTop
                right = $leftDockRight
                bottom = $bottomBoundary
            }
        }
        "toolbox" {
            return @{
                left = $rightDockLeft
                top = $rightDockTop
                right = $rootBounds.right
                bottom = $bottomBoundary
            }
        }
        "properties" {
            return @{
                left = $rightDockLeft
                top = $rightDockTop
                right = $rootBounds.right
                bottom = $bottomBoundary
            }
        }
        default {
            return @{
                left = $editorLeft
                top = $editorTop
                right = $editorRight
                bottom = $bottomBoundary
            }
        }
    }
}

function Get-OpenEditorTabs {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    $rootBounds = (Convert-AutomationElement -Element $Root).boundingRectangle
    $tabs = @()
    foreach ($candidate in (Find-MatchingElements -Root $Root -Depth 12 -MaxResults 100 -ControlType "TabItem")) {
        if ($null -eq $candidate) {
            continue
        }

        $bounds = $candidate.boundingRectangle
        if ($null -eq $bounds -or $null -eq $bounds.left -or $null -eq $bounds.top -or $null -eq $bounds.bottom) {
            continue
        }

        if ($bounds.top -ge ($rootBounds.top + 300)) {
            continue
        }

        if ($candidate.name -notmatch "\[[^\]]+\]") {
            continue
        }

        $tabs += $candidate
    }

    return @($tabs | Sort-Object @{ Expression = { $_.boundingRectangle.left } })
}

function Select-OpenEditorTabByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $tabs = @(Get-OpenEditorTabs -Root $Root)
    $match = $tabs | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if (-not $match) {
        return $null
    }

    Invoke-BoundsClick -Bounds $match.boundingRectangle | Out-Null
    return $match
}

function Get-ActiveEditorTab {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    $tabs = @(Get-OpenEditorTabs -Root $Root)
    $selectedTab = $tabs | Where-Object { $_.isSelected -eq $true } | Select-Object -First 1
    if ($selectedTab) {
        return $selectedTab
    }

    return $null
}

function Find-OpenEditorTabForItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    if (-not $Item) {
        return $null
    }

    $tabs = @(Get-OpenEditorTabs -Root $Root)
    $exact = $tabs | Where-Object { $_.name -eq $Item } | Select-Object -First 1
    if ($exact) {
        return $exact
    }

    $moduleQualified = $tabs | Where-Object { $_.name -like "$Item [*]" } | Select-Object -First 1
    if ($moduleQualified) {
        return $moduleQualified
    }

    return ($tabs | Where-Object { $_.name -like "*$Item*" } | Select-Object -First 1)
}

function Open-OrSelectStudioProItem {
    param(
        [System.Diagnostics.Process]$Process,
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item,
        [int]$DelayMs = 250
    )

    $openTab = Find-OpenEditorTabForItem -Root $Root -Item $Item
    if ($openTab) {
        if (-not $openTab.isOffscreen) {
            Invoke-BoundsClick -Bounds $openTab.boundingRectangle | Out-Null
            Start-Sleep -Milliseconds $DelayMs
        }

        return @{
            method = if ($openTab.isOffscreen) { "reuseOpenTab" } else { "selectOpenTab" }
            tab = $openTab
        }
    }

    Open-StudioProItemByName -Process $Process -Item $Item -DelayMs $DelayMs
    return @{
        method = "goTo"
        tab = $null
    }
}

function Find-BestVisibleNamedElement {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [string]$Surface = "editor",
        [string]$Item = ""
    )

    $scopeName = if ($Surface -eq "any") { "editor" } else { $Surface }
    $candidates = @(Get-VisibleNamedElementsInScope -Root $Root -Scope $scopeName -Name $Name -Item $Item -Limit 50 | Where-Object {
        $_.name -eq $Name -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ne $null
    })

    if (-not $candidates -or $candidates.Length -eq 0) {
        return $null
    }

    $preferred = $candidates | Sort-Object `
        @{ Expression = { if ($_.controlType -eq "Text") { 0 } else { 1 } } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

    return $preferred
}

function Get-ControlTypePriority {
    param(
        [string]$ControlType
    )

    switch ($ControlType) {
        "TreeItem" { return 0 }
        "ListItem" { return 1 }
        "DataItem" { return 2 }
        "Text" { return 3 }
        "Button" { return 4 }
        "Edit" { return 5 }
        "ComboBox" { return 6 }
        "CheckBox" { return 7 }
        "RadioButton" { return 8 }
        "MenuItem" { return 9 }
        default { return 20 }
    }
}

function Get-ScopeSearchRoot {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor",
        [string]$Item = ""
    )

    switch ($Scope) {
        "appExplorer" {
            $container = Get-DockContainerByName -Root $Root -Name "App Explorer"
            if ($container) { return $container }
            return $Root
        }
        "pageExplorer" {
            $container = Get-DockContainerByName -Root $Root -Name "Page Explorer"
            if ($container) { return $container }
            return $Root
        }
        "toolbox" {
            $container = Get-DockContainerByName -Root $Root -Name "Toolbox"
            if ($container) { return $container }
            return $Root
        }
        "properties" {
            $container = Get-DockContainerByName -Root $Root -Name "Properties"
            if ($container) { return $container }
            return $Root
        }
        default {
            $container = Get-OpenEditorContainerByItem -Root $Root -Item $Item
            if ($container) { return $container }
            return $Root
        }
    }
}

function Get-VisibleNamedElementsInScope {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor",
        [string]$Name = "",
        [string]$Item = "",
        [int]$Limit = 200
    )

    $searchRoot = Get-ScopeSearchRoot -Root $Root -Scope $Scope -Item $Item
    $scopeBounds = if ($Scope -eq "editor" -and $Item) {
        $editorBounds = Get-OpenEditorContentBoundsByItem -Root $Root -Item $Item
        if ($editorBounds) { $editorBounds } else { (Convert-AutomationElement -Element $searchRoot).boundingRectangle }
    } elseif ($searchRoot -ne $Root) {
        (Convert-AutomationElement -Element $searchRoot).boundingRectangle
    } else {
        Get-ScopeBounds -Root $Root -Scope $Scope
    }
    $rootBounds = (Convert-AutomationElement -Element $searchRoot).boundingRectangle
    $controlTypes = if ($Scope -eq "editor") {
        @("Text", "Button", "Edit", "Hyperlink", "ListItem", "TreeItem", "DataItem")
    } else {
        @("TreeItem", "ListItem", "DataItem", "Text", "Button", "MenuItem", "CheckBox")
    }

    $matches = @()
    foreach ($controlType in $controlTypes) {
        $matches += @(Find-MatchingElements -Root $searchRoot -Depth 25 -MaxResults 1000 -Name $Name -ControlType $controlType)
    }

    $results = @()
    $seen = @{}
    foreach ($match in ($matches | Where-Object {
        $_ -and
        $_.name -and
        -not $_.isOffscreen -and
        (Test-RectangleWithinBounds -Bounds $_.boundingRectangle -Container $rootBounds) -and
        (Test-RectangleWithinBounds -Bounds $_.boundingRectangle -Container $scopeBounds)
    } | Sort-Object `
        @{ Expression = { Get-ControlTypePriority -ControlType $_.controlType } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })) {
        $key = "{0}|{1}|{2}|{3}" -f $match.name, $match.controlType, $match.boundingRectangle.left, $match.boundingRectangle.top
        if ($seen.ContainsKey($key)) {
            continue
        }

        $seen[$key] = $true
        $results += $match
        if ($results.Length -ge $Limit) {
            break
        }
    }

    return @($results)
}

function Find-VisibleNamedElementInScope {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor",
        [string]$Name,
        [string]$Item = ""
    )

    if (-not $Name) {
        return $null
    }

    $matches = @(Get-VisibleNamedElementsInScope -Root $Root -Scope $Scope -Name $Name -Item $Item -Limit 50)
    if ($matches.Length -eq 0) {
        return $null
    }

    return ($matches | Sort-Object `
        @{ Expression = { Get-ControlTypePriority -ControlType $_.controlType } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)
}

function Get-StudioProPopupWindows {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    $windows = @(Find-MatchingElements -Root $Root -Depth 6 -MaxResults 25 -ControlType "Window")
    $popups = @($windows | Where-Object {
        $_.className -eq "Popup" -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.top -ne $null
    })

    return @($popups)
}

function Get-StudioProWindowMatches {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name = "",
        [int]$Depth = 10,
        [int]$MaxResults = 40
    )

    $windows = @(Find-MatchingElements -Root $Root -Depth $Depth -MaxResults $MaxResults -ControlType "Window")
    $visible = @($windows | Where-Object {
        $_ -and
        -not $_.isOffscreen -and
        ($Name -eq "" -or $_.name -eq $Name)
    })

    return @($visible)
}

function Get-StudioProWindowMatchByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    if (-not $Name) {
        return $null
    }

    $windows = @(Get-StudioProWindowMatches -Root $Root -Name $Name)
    if ($windows.Length -eq 0) {
        return $null
    }

    return ($windows | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)
}

function Wait-ForStudioProWindowByName {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Name,
        [int]$TimeoutMs = 4000,
        [int]$PollMs = 250
    )

    if (-not $Name) {
        return $null
    }

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    do {
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
        $window = Get-StudioProWindowMatchByName -Root $attached.Element -Name $Name
        if ($window) {
            return @{
                Attached = $attached
                Window = $window
            }
        }

        Start-Sleep -Milliseconds $PollMs
    } while ([DateTime]::UtcNow -lt $deadline)

    return $null
}

function Resolve-NativeWindowByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [int]$Depth = 15
    )

    $match = Get-StudioProWindowMatchByName -Root $Root -Name $Name
    if (-not $match) {
        return $null
    }

    return Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $match.runtimeId -Depth $Depth
}

function Get-StudioProPopupSummary {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    $popups = Get-StudioProPopupWindows -Root $Root
    $summaries = @()

    foreach ($popup in $popups) {
        $nativePopup = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $popup.runtimeId -Depth 8
        $texts = @()
        $buttons = @()

        if ($nativePopup) {
            $textMatches = @(Find-MatchingElements -Root $nativePopup -Depth 8 -MaxResults 50 -ControlType "Text")
            $buttonMatches = @(Find-MatchingElements -Root $nativePopup -Depth 8 -MaxResults 20 -ControlType "Button")
            $texts = @($textMatches | Where-Object { $_.name } | ForEach-Object { $_.name } | Select-Object -Unique)
            $buttons = @($buttonMatches | Where-Object { $_.name } | ForEach-Object { $_.name } | Select-Object -Unique)
        }

        $summaries += @{
            popup = $popup
            texts = $texts
            buttons = $buttons
        }
    }

    return @($summaries)
}

function Wait-ForStudioProReady {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutMs = 60000,
        [int]$PollMs = 1000
    )

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    do {
        $attached = Get-StudioProWindowElement -ProcessId $Process.Id
        $popups = @(Get-StudioProPopupWindows -Root $attached.Element)
        if ($popups.Length -eq 0) {
            return @{
                ready = $true
                popupCount = 0
                timedOut = $false
            }
        }

        Start-Sleep -Milliseconds $PollMs
    } while ([DateTime]::UtcNow -lt $deadline)

    $attached = Get-StudioProWindowElement -ProcessId $Process.Id
    $remaining = @(Get-StudioProPopupSummary -Root $attached.Element)
    return @{
        ready = $false
        popupCount = $remaining.Length
        timedOut = $true
        popups = $remaining
    }
}

function Select-PageExplorerItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    $scopeBounds = Get-ScopeBounds -Root $Root -Scope "pageExplorer"

    $matches = @(Get-VisibleNamedElementsInScope -Root $Root -Scope "pageExplorer" -Name $Item -Limit 100 | Where-Object {
        $_.name -eq $Item
    })

    if ($matches.Length -gt 0) {
        $match = ($matches | Sort-Object `
            @{ Expression = { Get-ControlTypePriority -ControlType $_.controlType } }, `
            @{ Expression = { $_.boundingRectangle.top } }, `
            @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)

        if ($match.controlType -eq "Text" -and $match.runtimeId) {
            $nativeMatch = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $match.runtimeId -Depth 25
            $ancestor = Get-AutomationParent -Element $nativeMatch
            while ($ancestor) {
                $ancestorMatch = Convert-AutomationElement -Element $ancestor
                $ancestorBounds = $ancestorMatch.boundingRectangle
                $ancestorWithinPageExplorer = (
                    $ancestorBounds.left -ne $null -and
                    $ancestorBounds.top -ne $null -and
                    $ancestorBounds.bottom -ne $null -and
                    $ancestorBounds.left -ge $scopeBounds.left -and
                    $ancestorBounds.top -ge $scopeBounds.top -and
                    $ancestorBounds.bottom -le $scopeBounds.bottom
                )

                if (
                    $ancestorWithinPageExplorer -and
                    $ancestorMatch.controlType -in @("TreeItem", "ListItem", "DataItem", "Custom")
                ) {
                    return $ancestorMatch
                }

                $ancestor = Get-AutomationParent -Element $ancestor
            }
        }

        return $match
    }

    return $null
}

function Select-AppExplorerItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    return Find-VisibleNamedElementInScope -Root $Root -Scope "appExplorer" -Name $Item
}

function Select-ToolboxItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    return Find-VisibleNamedElementInScope -Root $Root -Scope "toolbox" -Name $Item
}

function Find-DialogNamedElements {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Name = "",
        [int]$Limit = 200
    )

    $controlTypes = @("DataItem", "ListItem", "TreeItem", "Button", "Text", "Edit", "ComboBox", "CheckBox", "RadioButton", "MenuItem")
    $matches = @()
    foreach ($controlType in $controlTypes) {
        $matches += @(Find-MatchingElements -Root $Dialog -Depth 15 -MaxResults 500 -Name $Name -ControlType $controlType)
    }

    $results = @()
    $seen = @{}
    foreach ($match in ($matches | Where-Object {
        $_ -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ne $null
    } | Sort-Object `
        @{ Expression = { Get-ControlTypePriority -ControlType $_.controlType } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })) {
        $key = "{0}|{1}|{2}|{3}" -f $match.name, $match.controlType, $match.boundingRectangle.left, $match.boundingRectangle.top
        if ($seen.ContainsKey($key)) {
            continue
        }

        $seen[$key] = $true
        $results += $match
        if ($results.Length -ge $Limit) {
            break
        }
    }

    return @($results)
}

function Find-DialogFieldByLabel {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Label,
        [string]$ControlType = "",
        [int]$Limit = 300
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    if (-not $Label) {
        throw "A dialog field label is required."
    }

    $labelMatch = @(Find-DialogNamedElements -Dialog $Dialog -Name $Label -Limit $Limit | Where-Object {
        $_.name -eq $Label -and $_.controlType -eq "Text"
    } | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)

    if ($labelMatch.Length -eq 0) {
        return $null
    }

    $labelTarget = $labelMatch[0]
    $labelBounds = $labelTarget.boundingRectangle
    $labelCenterY = ($labelBounds.top + $labelBounds.bottom) / 2
    $candidateTypes = if ($ControlType) {
        @($ControlType)
    } else {
        @("Edit", "ComboBox", "CheckBox", "ToggleButton")
    }

    $allCandidates = @()
    foreach ($candidateType in $candidateTypes) {
        $typeMatches = @(Find-DialogNamedElements -Dialog $Dialog -Limit $Limit | Where-Object {
            $_.controlType -eq $candidateType
        })
        $allCandidates += $typeMatches
    }

    $candidates = @($allCandidates | Where-Object {
        $_.boundingRectangle.left -ne $null -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ge ($labelBounds.right - 32) -and
        ([Math]::Abs((($_.boundingRectangle.top + $_.boundingRectangle.bottom) / 2) - $labelCenterY) -le 140)
    })

    if ($candidates.Length -eq 0) {
        $candidates = @($allCandidates | Where-Object {
            $_.boundingRectangle.top -ne $null -and
            $_.boundingRectangle.bottom -ne $null -and
            $_.boundingRectangle.top -ge ($labelBounds.top - 48)
        })
    }

    if ($candidates.Length -eq 0) {
        return @{
            label = $labelTarget
            field = $null
        }
    }

    $field = $candidates | Sort-Object `
        @{ Expression = { [Math]::Abs((($_.boundingRectangle.top + $_.boundingRectangle.bottom) / 2) - $labelCenterY) } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

    return @{
        label = $labelTarget
        field = $field
    }
}

function Set-ClipboardTextValue {
    param(
        [string]$Value
    )

    [System.Windows.Forms.Clipboard]::SetText($Value)
}

function ConvertTo-DialogBooleanValue {
    param(
        [string]$Value
    )

    if ($null -eq $Value) {
        throw "A boolean dialog field value is required."
    }

    switch -Regex ($Value.Trim().ToLowerInvariant()) {
        "^(true|1|yes|y|on|checked)$" { return $true }
        "^(false|0|no|n|off|unchecked)$" { return $false }
        default {
            throw "The value '$Value' is not a supported boolean dialog field value. Use true/false, yes/no, on/off, or 1/0."
        }
    }
}

function Get-DialogFieldObservedValue {
    param(
        [System.Windows.Automation.AutomationElement]$Field
    )

    if (-not $Field) {
        return @{
            textValue = $null
            toggleState = $null
            isToggled = $null
        }
    }

    $textValue = $null
    try {
        $valuePattern = $null
        if ($Field.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$valuePattern)) {
            $textValue = $valuePattern.Current.Value
        }
    } catch {
        $textValue = $null
    }

    $toggleState = $null
    $isToggled = $null
    try {
        $togglePattern = $null
        if ($Field.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$togglePattern)) {
            $toggleState = [string]$togglePattern.Current.ToggleState
            if ($togglePattern.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::Indeterminate) {
                $isToggled = $togglePattern.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
            }
        }
    } catch {
        $toggleState = $null
        $isToggled = $null
    }

    return @{
        textValue = $textValue
        toggleState = $toggleState
        isToggled = $isToggled
    }
}

function Set-DialogFieldValue {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [hashtable]$FieldMatch,
        [string]$Value,
        [int]$DelayMs = 250
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    if (-not $FieldMatch) {
        throw "A dialog field match is required."
    }

    if (-not $FieldMatch.field) {
        throw "Could not resolve a dialog field from the supplied label."
    }

    $selection = Select-AutomationMatch -Root $Dialog -Match $FieldMatch.field -DelayMs $DelayMs
    $nativeField = $null
    if ($FieldMatch.field.runtimeId) {
        $nativeField = Resolve-NativeElementByRuntimeId -Root $Dialog -ExpectedRuntimeId $FieldMatch.field.runtimeId -Depth 15
    }

    $method = $selection.method
    if ($nativeField) {
        $fieldControlType = [string]$FieldMatch.field.controlType
        if ($fieldControlType -in @("CheckBox", "ToggleButton")) {
            $targetState = ConvertTo-DialogBooleanValue -Value $Value
            $togglePattern = $null
            if ($nativeField.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$togglePattern)) {
                $currentState = $togglePattern.Current.ToggleState
                if ($currentState -eq [System.Windows.Automation.ToggleState]::Indeterminate) {
                    throw "The dialog field '$($FieldMatch.label.name)' is indeterminate and cannot be set safely."
                }

                $isChecked = $currentState -eq [System.Windows.Automation.ToggleState]::On
                if ($isChecked -ne $targetState) {
                    $togglePattern.Toggle()
                    Start-Sleep -Milliseconds $DelayMs
                }

                $method = "togglePattern"
            } else {
                $invokePattern = $null
                if (-not $nativeField.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invokePattern)) {
                    throw "The dialog field '$($FieldMatch.label.name)' does not support TogglePattern or InvokePattern."
                }

                $legacyPattern = $null
                if ($nativeField.TryGetCurrentPattern([System.Windows.Automation.LegacyIAccessiblePattern]::Pattern, [ref]$legacyPattern)) {
                    $legacyState = [int]$legacyPattern.Current.State
                    $isChecked = ($legacyState -band 0x10) -ne 0
                    if ($isChecked -ne $targetState) {
                        $invokePattern.Invoke()
                        Start-Sleep -Milliseconds $DelayMs
                    }
                    $method = "invokePatternLegacyState"
                } else {
                    $invokePattern.Invoke()
                    Start-Sleep -Milliseconds $DelayMs
                    $method = "invokePatternBlindToggle"
                }
            }
        } else {
            $valuePattern = $null
            if ($nativeField.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$valuePattern)) {
                $valuePattern.SetValue($Value)
                Start-Sleep -Milliseconds $DelayMs
                $method = "valuePattern"
            } else {
                Set-ClipboardTextValue -Value $Value
                Send-KeysToForegroundWindow -Keys "^a" -DelayMs 80
                Send-KeysToForegroundWindow -Keys "^v" -DelayMs $DelayMs
                $method = "clipboardPaste"
            }
        }
    }

    $observedValue = Get-DialogFieldObservedValue -Field $nativeField

    return @{
        method = $method
        selection = $selection
        label = $FieldMatch.label
        field = $FieldMatch.field
        value = $Value
        observedValue = $observedValue
    }
}

function Get-DialogNamedTextMatches {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Name = "",
        [int]$Limit = 200
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    $items = @(Find-DialogNamedElements -Dialog $Dialog -Name $Name -Limit $Limit | Where-Object {
        $_.controlType -eq "Text"
    })

    return @($items | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })
}

function Resolve-DialogVisualCardByText {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [hashtable]$TextMatch,
        [double]$MinWidth = 260,
        [double]$MinHeight = 120,
        [double]$MinLeftOffset = 280,
        [double]$MinTopOffset = 180
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    if (-not $TextMatch) {
        throw "A dialog text match is required."
    }

    $dialogBounds = Convert-BoundingRectangle -Rect $Dialog.Current.BoundingRectangle
    $native = $null
    if ($TextMatch.runtimeId) {
        $native = Resolve-NativeElementByRuntimeId -Root $Dialog -ExpectedRuntimeId $TextMatch.runtimeId -Depth 20
    }

    if (-not $native) {
        return $null
    }

    $current = $native
    $best = $null
    while ($current) {
        $candidate = Convert-AutomationElement -Element $current
        $bounds = $candidate.boundingRectangle
        if (
            $bounds.left -ne $null -and
            $bounds.top -ne $null -and
            $bounds.width -ne $null -and
            $bounds.height -ne $null -and
            $bounds.width -ge $MinWidth -and
            $bounds.height -ge $MinHeight -and
            $bounds.left -ge ($dialogBounds.left + $MinLeftOffset) -and
            $bounds.top -ge ($dialogBounds.top + $MinTopOffset)
        ) {
            $best = $candidate
            break
        }

        $current = Get-AutomationParent -Element $current
    }

    if ($best) {
        return $best
    }

    return @{
        name = $TextMatch.name
        automationId = $TextMatch.automationId
        className = $TextMatch.className
        controlType = $TextMatch.controlType
        frameworkId = $TextMatch.frameworkId
        runtimeId = $TextMatch.runtimeId
        processId = $TextMatch.processId
        isEnabled = $TextMatch.isEnabled
        isOffscreen = $TextMatch.isOffscreen
        isSelected = $TextMatch.isSelected
        boundingRectangle = @{
            left = [math]::Max($dialogBounds.left + $MinLeftOffset, $TextMatch.boundingRectangle.left - 72)
            top = [math]::Max($dialogBounds.top + $MinTopOffset, $TextMatch.boundingRectangle.top - 72)
            width = [math]::Max($MinWidth, $TextMatch.boundingRectangle.width + 144)
            height = [math]::Max($MinHeight, $TextMatch.boundingRectangle.height + 144)
            right = [math]::Max($dialogBounds.left + $MinLeftOffset + $MinWidth, $TextMatch.boundingRectangle.right + 72)
            bottom = [math]::Max($dialogBounds.top + $MinTopOffset + $MinHeight, $TextMatch.boundingRectangle.bottom + 72)
        }
    }
}

function Get-CreatePageTemplateChoices {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [int]$Limit = 60
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    $dialogBounds = Convert-BoundingRectangle -Rect $Dialog.Current.BoundingRectangle
    $ignoredNames = @(
        "Responsive (Web)",
        "Tablet (Web)",
        "Phone (Web)",
        "Native mobile",
        "Page name",
        "Navigation layout",
        "OK",
        "Cancel"
    )

    $matches = @(Get-DialogNamedTextMatches -Dialog $Dialog -Limit $Limit | Where-Object {
        $_.name -and
        ($ignoredNames -notcontains $_.name) -and
        $_.boundingRectangle.left -ge ($dialogBounds.left + 320) -and
        $_.boundingRectangle.top -ge ($dialogBounds.top + 220)
    })

    return @($matches)
}

function Select-CreatePageTemplateCard {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Template = "",
        [int]$DelayMs = 250
    )

    if (-not $Dialog) {
        throw "A native Studio Pro dialog is required."
    }

    $choices = @(Get-CreatePageTemplateChoices -Dialog $Dialog -Limit 80)
    if ($choices.Length -eq 0) {
        return $null
    }

    $choice = @(
        if ($Template) {
            $choices | Where-Object { $_.name -eq $Template } | Select-Object -First 1
        } else {
            $choices | Select-Object -First 1
        }
    )

    if ($choice.Count -eq 0) {
        return $null
    }

    $card = Resolve-DialogVisualCardByText -Dialog $Dialog -TextMatch $choice[0]
    if (-not $card) {
        return $null
    }

    $method = Invoke-BoundsClick -Bounds $card.boundingRectangle
    Start-Sleep -Milliseconds $DelayMs

    return @{
        choice = $choice[0]
        card = $card
        method = $method
        availableChoices = @($choices)
    }
}

function Open-PageExplorerContextMenu {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [System.Diagnostics.Process]$Process,
        [System.Windows.Automation.AutomationElement]$Root,
        [hashtable]$TargetMatch,
        [string]$MenuItemName = "",
        [int]$DelayMs = 250,
        [int]$Attempts = 6
    )

    if (-not $Process) {
        throw "A Studio Pro process is required."
    }

    if (-not $Root) {
        throw "A Studio Pro root automation element is required."
    }

    if (-not $TargetMatch) {
        throw "A Page Explorer target match is required."
    }

    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        $currentRoot = (Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern).Element
        $namedCandidates = @(Get-VisibleNamedElementsInScope -Root $currentRoot -Scope "pageExplorer" -Name $TargetMatch.name -Limit 20 | Where-Object {
            $_.name -eq $TargetMatch.name -and $_.controlType -in @("Text", "DataItem", "ListItem", "TreeItem")
        })

        if ($namedCandidates.Length -eq 0) {
            $namedCandidates = @($TargetMatch)
        }

        if ($TargetMatch.runtimeId) {
            $resolvedTarget = Resolve-NativeElementByRuntimeId -Root $currentRoot -ExpectedRuntimeId $TargetMatch.runtimeId -Depth 25
            if ($resolvedTarget) {
                $namedCandidates += @(Convert-AutomationElement -Element $resolvedTarget)
            }
        }

        $candidateOrder = @($namedCandidates | Sort-Object `
            @{ Expression = { if ($_.controlType -eq "Text") { 0 } else { 1 } } }, `
            @{ Expression = { $_.boundingRectangle.left } }, `
            @{ Expression = { $_.boundingRectangle.top } })

        foreach ($candidate in $candidateOrder) {
            $candidateSelection = Select-AutomationMatch -Root $currentRoot -Match $candidate -DelayMs ([Math]::Max(120, $DelayMs))
            Start-Sleep -Milliseconds 80

            Set-StudioProForegroundWindow -Process $Process
            Send-KeysToForegroundWindow -Keys "+{F10}" -DelayMs ($DelayMs + 100)

            $attached = Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern
            $menuItems = @(Find-MatchingElements -Root $attached.Element -Depth 15 -MaxResults 150 -ControlType "MenuItem" | Where-Object {
                $_ -and
                -not $_.isOffscreen -and
                $_.name -and
                $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
            })

            if (-not $MenuItemName -and $menuItems.Length -gt 0) {
                return @{
                    Attached = $attached
                    MenuItems = $menuItems
                    Trigger = @{
                        attempt = $attempt + 1
                        candidate = $candidate
                        candidateSelection = $candidateSelection
                        point = @{
                            method = "shiftF10"
                        }
                    }
                }
            }

            if ($MenuItemName) {
                $match = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItemName
                if ($match) {
                    return @{
                        Attached = $attached
                        MenuItem = $match
                        MenuItems = $menuItems
                        Trigger = @{
                            attempt = $attempt + 1
                            candidate = $candidate
                            candidateSelection = $candidateSelection
                            point = @{
                                method = "shiftF10"
                            }
                        }
                    }
                }
            }

            Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs 100

            $points = if ($candidate.controlType -eq "Text") {
                @(
                    @{ Horizontal = "center"; Vertical = "center"; Inset = 6 },
                    @{ Horizontal = "left"; Vertical = "center"; Inset = 6 }
                )
            } else {
                @(
                    @{ Horizontal = "left"; Vertical = "center"; Inset = 48 },
                    @{ Horizontal = "left"; Vertical = "center"; Inset = 96 },
                    @{ Horizontal = "center"; Vertical = "center"; Inset = 12 }
                )
            }

            foreach ($point in $points) {
                Set-StudioProForegroundWindow -Process $Process
                Invoke-BoundsRightClick -Bounds $candidate.boundingRectangle -Horizontal $point.Horizontal -Vertical $point.Vertical -Inset $point.Inset | Out-Null
                Start-Sleep -Milliseconds ($DelayMs + 100)

                $attached = Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern
                $menuItems = @(Find-MatchingElements -Root $attached.Element -Depth 15 -MaxResults 150 -ControlType "MenuItem" | Where-Object {
                    $_ -and
                    -not $_.isOffscreen -and
                    $_.name -and
                    $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
                })

                if (-not $MenuItemName -and $menuItems.Length -gt 0) {
                    return @{
                        Attached = $attached
                        MenuItems = $menuItems
                        Trigger = @{
                            attempt = $attempt + 1
                            candidate = $candidate
                            point = $point
                        }
                    }
                }

                if ($MenuItemName) {
                    $match = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItemName
                    if ($match) {
                        return @{
                            Attached = $attached
                            MenuItem = $match
                            MenuItems = $menuItems
                            Trigger = @{
                                attempt = $attempt + 1
                                candidate = $candidate
                                point = $point
                            }
                        }
                    }
                }
            }
        }
    }

    return $null
}

function Open-EditorElementContextMenu {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Item = "",
        [string]$ElementName = "",
        [string]$ElementRuntimeId = "",
        [int]$OffsetX = 0,
        [int]$OffsetY = 0,
        [string]$MenuItemName = "",
        [int]$DelayMs = 250,
        [int]$Attempts = 4
    )

    if (-not $Item) {
        throw "An editor item name is required."
    }

    $editorContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Item -Scope "editor" -DelayMs $DelayMs
    $attached = $editorContext.Attached
    $targetMatch = $null
    $lastPostDialog = $null
    $searchRoot = Get-ScopeSearchRoot -Root $attached.Element -Scope "editor" -Item $Item
    if ($ElementRuntimeId) {
        $runtimeMatch = @(Find-MatchingElements -Root $searchRoot -Depth 25 -MaxResults 5 -RuntimeId $ElementRuntimeId)
        if ($runtimeMatch.Length -eq 0) {
            throw "Could not find a visible editor element with runtime id '$ElementRuntimeId'."
        }
        $targetMatch = $runtimeMatch[0]
    } elseif ($ElementName) {
        $targetMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $ElementName -Surface "editor" -Item $Item
        if (-not $targetMatch) {
            throw "Could not find a visible editor element named '$ElementName'."
        }
    }

    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        $currentRoot = (Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern).Element
        $candidate = $null
        $candidateSelection = $null

        if ($targetMatch) {
            if ($ElementRuntimeId) {
                $currentSearchRoot = Get-ScopeSearchRoot -Root $currentRoot -Scope "editor" -Item $Item
                $runtimeMatch = @(Find-MatchingElements -Root $currentSearchRoot -Depth 25 -MaxResults 5 -RuntimeId $ElementRuntimeId)
                $candidate = if ($runtimeMatch.Length -gt 0) { $runtimeMatch[0] } else { $targetMatch }
            } else {
                $candidate = Find-BestVisibleNamedElement -Root $currentRoot -Name $ElementName -Surface "editor" -Item $Item
                if (-not $candidate) {
                    $candidate = $targetMatch
                }
            }
            $candidateSelection = Select-AutomationMatch -Root $currentRoot -Match $candidate -DelayMs ([Math]::Max(120, $DelayMs))
            Start-Sleep -Milliseconds 80
        }

        if ($candidate -and ($OffsetX -ne 0 -or $OffsetY -ne 0)) {
            $offsetPoint = @{
                x = [int][math]::Round($candidate.boundingRectangle.left + ($candidate.boundingRectangle.width / 2) + $OffsetX)
                y = [int][math]::Round($candidate.boundingRectangle.top + ($candidate.boundingRectangle.height / 2) + $OffsetY)
            }

            Set-StudioProForegroundWindow -Process $attached.Process
            [NativeMouse]::SetCursorPos([int]$offsetPoint.x, [int]$offsetPoint.y) | Out-Null
            Start-Sleep -Milliseconds 50
            [NativeMouse]::mouse_event([NativeMouse]::RIGHTDOWN, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 50
            [NativeMouse]::mouse_event([NativeMouse]::RIGHTUP, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds ($DelayMs + 100)

            $attachedAfter = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
            $menuItems = @(Find-MatchingElements -Root $attachedAfter.Element -Depth 15 -MaxResults 150 -ControlType "MenuItem" | Where-Object {
                $_ -and
                -not $_.isOffscreen -and
                $_.name -and
                $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
            })

            if (-not $MenuItemName -and $menuItems.Length -gt 0) {
                return @{
                    Attached = $attachedAfter
                    EditorContext = $editorContext
                    TargetSelection = $candidateSelection
                    Target = $candidate
                    MenuItems = $menuItems
                    PostDialog = $null
                    Trigger = @{
                        attempt = $attempt + 1
                        method = "nativeOffsetRightClick"
                        point = $offsetPoint
                    }
                }
            }

            if ($MenuItemName) {
                $match = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItemName
                if ($match) {
                    return @{
                        Attached = $attachedAfter
                        EditorContext = $editorContext
                        TargetSelection = $candidateSelection
                        Target = $candidate
                        MenuItem = $match
                        MenuItems = $menuItems
                        PostDialog = $null
                        Trigger = @{
                            attempt = $attempt + 1
                            method = "nativeOffsetRightClick"
                            point = $offsetPoint
                        }
                    }
                }
            }

            if ($menuItems.Length -gt 0) {
                Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs 100
            }
        }

        Set-StudioProForegroundWindow -Process $attached.Process
        Send-KeysToForegroundWindow -Keys "+{F10}" -DelayMs ($DelayMs + 100)

        $attachedAfter = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
        $postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(800, ($DelayMs * 3))) -PollMs 120 -Limit 30
        $menuItems = @(Find-MatchingElements -Root $attachedAfter.Element -Depth 15 -MaxResults 150 -ControlType "MenuItem" | Where-Object {
            $_ -and
            -not $_.isOffscreen -and
            $_.name -and
            $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
        })

        if ($postDialog -and $menuItems.Length -eq 0) {
            $lastPostDialog = $postDialog
            Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs ([Math]::Max(150, $DelayMs))

            if ($candidate) {
                $points = if ($candidate.controlType -eq "Text") {
                    @(
                        @{ Horizontal = "center"; Vertical = "center"; Inset = 6 },
                        @{ Horizontal = "left"; Vertical = "center"; Inset = 6 }
                    )
                } else {
                    @(
                        @{ Horizontal = "left"; Vertical = "center"; Inset = 24 },
                        @{ Horizontal = "center"; Vertical = "center"; Inset = 12 }
                    )
                }

                foreach ($point in $points) {
                    Set-StudioProForegroundWindow -Process $attached.Process
                    Invoke-BoundsRightClick -Bounds $candidate.boundingRectangle -Horizontal $point.Horizontal -Vertical $point.Vertical -Inset $point.Inset | Out-Null
                    Start-Sleep -Milliseconds ($DelayMs + 100)

                    $attachedAfter = Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern
                    $menuItems = @(Find-MatchingElements -Root $attachedAfter.Element -Depth 15 -MaxResults 150 -ControlType "MenuItem" | Where-Object {
                        $_ -and
                        -not $_.isOffscreen -and
                        $_.name -and
                        $_.name -notin @("File", "Edit", "View", "App", "Run", "Version Control", "Language", "Help")
                    })

                    if (-not $MenuItemName -and $menuItems.Length -gt 0) {
                        return @{
                            Attached = $attachedAfter
                            EditorContext = $editorContext
                            TargetSelection = $candidateSelection
                            Target = $candidate
                            MenuItems = $menuItems
                            PostDialog = $lastPostDialog
                            Trigger = @{
                                attempt = $attempt + 1
                                method = "nativeRightClick"
                                point = $point
                                fallbackFrom = "shiftF10Dialog"
                            }
                        }
                    }

                    if ($MenuItemName) {
                        $match = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItemName
                        if ($match) {
                            return @{
                                Attached = $attachedAfter
                                EditorContext = $editorContext
                                TargetSelection = $candidateSelection
                                Target = $candidate
                                MenuItem = $match
                                MenuItems = $menuItems
                                PostDialog = $lastPostDialog
                                Trigger = @{
                                    attempt = $attempt + 1
                                    method = "nativeRightClick"
                                    point = $point
                                    fallbackFrom = "shiftF10Dialog"
                                }
                            }
                        }
                    }

                    Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs 100
                }
            }
        }

        if (-not $MenuItemName -and $menuItems.Length -gt 0) {
            return @{
                Attached = $attachedAfter
                EditorContext = $editorContext
                TargetSelection = $candidateSelection
                Target = $candidate
                MenuItems = $menuItems
                PostDialog = $postDialog
                Trigger = @{
                    attempt = $attempt + 1
                    method = "shiftF10"
                }
            }
        }

        if ($MenuItemName) {
            $match = Find-MenuItemMatch -MenuItems $menuItems -MenuItemName $MenuItemName
            if ($match) {
                return @{
                    Attached = $attachedAfter
                    EditorContext = $editorContext
                    TargetSelection = $candidateSelection
                    Target = $candidate
                    MenuItem = $match
                    MenuItems = $menuItems
                    PostDialog = $postDialog
                    Trigger = @{
                        attempt = $attempt + 1
                        method = "shiftF10"
                    }
                }
            }
        }

        Send-KeysToForegroundWindow -Keys "{ESC}" -DelayMs 100
    }

    if ($lastPostDialog) {
        return @{
            Attached = $attached
            EditorContext = $editorContext
            TargetSelection = $null
            Target = $targetMatch
            MenuItems = @()
            PostDialog = $lastPostDialog
            Trigger = @{
                attempt = $Attempts
                method = "shiftF10"
                fallbackAttempted = $true
            }
        }
    }

    return $null
}

function Open-AddWidgetDialogForPageExplorerItem {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Page,
        [string]$Target,
        [int]$DelayMs = 250,
        [int]$ContextMenuAttempts = 6,
        [int]$DialogTimeoutMs = 4000
    )

    if (-not $Page) {
        throw "A page name is required."
    }

    if (-not $Target) {
        throw "A Page Explorer target item is required."
    }

    $pageContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Page -Scope "pageExplorer" -DelayMs $DelayMs
    $attached = $pageContext.Attached
    $targetMatch = Select-PageExplorerItemByName -Root $attached.Element -Item $Target
    if (-not $targetMatch) {
        throw "Could not find a visible Page Explorer item named '$Target'."
    }

    $targetSelection = Select-AutomationMatch -Root $attached.Element -Match $targetMatch -DelayMs $DelayMs
    $contextMenu = Open-PageExplorerContextMenu `
        -ProcessId $attached.Process.Id `
        -WindowTitlePattern $WindowTitlePattern `
        -Process $attached.Process `
        -Root $attached.Element `
        -TargetMatch $targetSelection.target `
        -MenuItemName "Add widget" `
        -DelayMs $DelayMs `
        -Attempts $ContextMenuAttempts

    $menuSelection = $null
    if ($contextMenu -and $contextMenu.MenuItem) {
        $menuSelection = Select-AutomationMatch -Root $contextMenu.Attached.Element -Match $contextMenu.MenuItem -DelayMs ($DelayMs + 100)
    }

    $dialogWait = Wait-ForStudioProWindowByName -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Select Widget" -TimeoutMs $DialogTimeoutMs -PollMs 250
    if ((-not $contextMenu -or -not $contextMenu.MenuItem) -and (-not $dialogWait -or -not $dialogWait.Window)) {
        throw "Could not open the Page Explorer context menu for '$Target'."
    }

    if (-not $dialogWait -or -not $dialogWait.Window) {
        throw "Studio Pro did not open the 'Select Widget' dialog."
    }

    $nativeDialog = Resolve-NativeWindowByName -Root $dialogWait.Attached.Element -Name "Select Widget" -Depth 15
    if (-not $nativeDialog) {
        throw "Could not attach to the native 'Select Widget' dialog."
    }

    return @{
        PageContext = $pageContext
        TargetSelection = $targetSelection
        ContextMenu = $contextMenu
        MenuSelection = $menuSelection
        DialogAttached = $dialogWait.Attached
        DialogWindow = $dialogWait.Window
        NativeDialog = $nativeDialog
    }
}

function Select-WidgetDialogItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Widget,
        [int]$DelayMs = 250
    )

    if (-not $Dialog) {
        throw "A native Select Widget dialog is required."
    }

    if (-not $Widget) {
        throw "A widget name is required."
    }

    $items = @(Find-DialogNamedElements -Dialog $Dialog -Name $Widget -Limit 200 | Where-Object {
        $_.name -eq $Widget -and
        $_.controlType -in @("DataItem", "ListItem", "TreeItem")
    })

    if ($items.Length -eq 0) {
        return $null
    }

    $ordered = @($items | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })

    $firstSelection = $null
    foreach ($match in $ordered) {
        $selection = Select-AutomationMatch -Root $Dialog -Match $match -DelayMs $DelayMs
        if (-not $firstSelection) {
            $firstSelection = $selection
        }

        $selectButtons = @(Find-DialogNamedElements -Dialog $Dialog -Name "Select" -Limit 20 | Where-Object {
            $_.name -eq "Select" -and $_.controlType -eq "Button"
        } | Sort-Object `
            @{ Expression = { $_.boundingRectangle.top } }, `
            @{ Expression = { $_.boundingRectangle.left } })
        $selectButton = if ($selectButtons.Length -gt 0) { $selectButtons[0] } else { $null }

        if ($selectButton -and $selectButton.isEnabled) {
            return @{
                method = $selection.method
                supportsSelectionItem = $selection.supportsSelectionItem
                supportsInvoke = $selection.supportsInvoke
                isSelected = $selection.isSelected
                target = $selection.target
                selectButtonEnabled = $true
            }
        }
    }

    return if ($firstSelection) {
        @{
            method = $firstSelection.method
            supportsSelectionItem = $firstSelection.supportsSelectionItem
            supportsInvoke = $firstSelection.supportsInvoke
            isSelected = $firstSelection.isSelected
            target = $firstSelection.target
            selectButtonEnabled = $false
        }
    } else {
        $null
    }
}

function Invoke-WidgetDialogButton {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$ButtonName,
        [int]$DelayMs = 250
    )

    if (-not $Dialog) {
        throw "A native Select Widget dialog is required."
    }

    if (-not $ButtonName) {
        throw "A dialog button name is required."
    }

    $buttons = @(Find-DialogNamedElements -Dialog $Dialog -Name $ButtonName -Limit 50 | Where-Object {
        $_.name -eq $ButtonName -and $_.controlType -eq "Button"
    })

    if ($buttons.Length -eq 0) {
        return $null
    }

    $match = $buttons | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

    return Select-AutomationMatch -Root $Dialog -Match $match -DelayMs $DelayMs
}

function Invoke-WidgetDialogAccept {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [hashtable]$WidgetSelection,
        [ValidateSet("button", "enter", "doubleClick")]
        [string]$Strategy = "button",
        [int]$DelayMs = 250
    )

    if (-not $Dialog) {
        throw "A native Select Widget dialog is required."
    }

    switch ($Strategy) {
        "button" {
            return Invoke-WidgetDialogButton -Dialog $Dialog -ButtonName "Select" -DelayMs $DelayMs
        }
        "enter" {
            Send-KeysToForegroundWindow -Keys "{ENTER}" -DelayMs $DelayMs
            return @{
                method = "sendKeysEnter"
                target = if ($WidgetSelection) { $WidgetSelection.target } else { $null }
            }
        }
        "doubleClick" {
            if (-not $WidgetSelection -or -not $WidgetSelection.target) {
                throw "A selected widget row is required for the doubleClick accept strategy."
            }

            $method = Invoke-BoundsDoubleClick -Bounds $WidgetSelection.target.boundingRectangle
            Start-Sleep -Milliseconds $DelayMs
            return @{
                method = $method
                target = $WidgetSelection.target
            }
        }
    }
}

function Get-StudioProDialogSnapshot {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Name = "",
        [int]$Limit = 40
    )

    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    $windows = @(Get-StudioProWindowMatches -Root $attached.Element -Name $Name -Depth 15 -MaxResults 20 | Where-Object {
        $_.name -and $_.name -ne $attached.Process.MainWindowTitle
    } | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })

    if ($windows.Length -eq 0) {
        return $null
    }

    $window = $windows[0]
    $nativeDialog = Resolve-NativeWindowByName -Root $attached.Element -Name $window.name -Depth 15
    if (-not $nativeDialog) {
        return @{
            window = $window
            items = @()
        }
    }

    return @{
        window = $window
        items = @(Find-DialogNamedElements -Dialog $nativeDialog -Limit $Limit)
    }
}

function Wait-ForStudioProDialogSnapshot {
    param(
        [int]$ProcessId = 0,
        [string]$WindowTitlePattern = "",
        [string]$Name = "",
        [int]$TimeoutMs = 1500,
        [int]$PollMs = 150,
        [int]$Limit = 40
    )

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    do {
        $snapshot = Get-StudioProDialogSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Name $Name -Limit $Limit
        if ($snapshot) {
            return $snapshot
        }

        Start-Sleep -Milliseconds $PollMs
    } while ([DateTime]::UtcNow -lt $deadline)

    return $null
}

function Get-VisibleTextMatches {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Scope = "editor",
        [string]$Item = "",
        [int]$Limit = 200
    )

    return @(Get-VisibleNamedElementsInScope -Root $Root -Scope $Scope -Item $Item -Limit $Limit)
}

function Resolve-NativeElementByRuntimeId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$ExpectedRuntimeId,
        [int]$Depth
    )

    if ((Convert-AutomationElement -Element $Root).runtimeId -eq $ExpectedRuntimeId) {
        return $Root
    }

    if ($Depth -le 0) {
        return $null
    }

    $children = @(Get-ChildElements -Element $Root)
    $childCount = $children.Length
    for ($index = 0; $index -lt $childCount; $index++) {
        $match = Resolve-NativeElementByRuntimeId -Root $children[$index] -ExpectedRuntimeId $ExpectedRuntimeId -Depth ($Depth - 1)
        if ($match) {
            return $match
        }
    }

    return $null
}

function Select-AutomationMatch {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [hashtable]$Match,
        [int]$DelayMs = 250
    )

    if ($null -eq $Match) {
        throw "Cannot select a null automation match."
    }

    $native = $null
    if ($Match.ContainsKey("runtimeId") -and $Match.runtimeId) {
        $native = Resolve-NativeElementByRuntimeId -Root $Root -ExpectedRuntimeId $Match.runtimeId -Depth 25
    }

    $method = $null
    $supportsSelectionItem = $false
    $supportsInvoke = $false
    $isSelected = $null

    if ($native) {
        $selectionPattern = $null
        if ($native.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$selectionPattern)) {
            try {
                $supportsSelectionItem = $true
                $selectionPattern.Select()
                $method = "selectionItemPattern"
                Start-Sleep -Milliseconds $DelayMs
                $isSelected = [bool]$selectionPattern.Current.IsSelected
            } catch {
                $method = $null
            }
        } else {
            $invokePattern = $null
            if ($native.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invokePattern)) {
                try {
                    $supportsInvoke = $true
                    $invokePattern.Invoke()
                    $method = "invokePattern"
                    Start-Sleep -Milliseconds $DelayMs
                } catch {
                    $method = $null
                }
            }
        }
    }

    if (-not $method) {
        $method = Invoke-BoundsClick -Bounds $Match.boundingRectangle
        Start-Sleep -Milliseconds $DelayMs

        if ($native) {
            $selectionPattern = $null
            if ($native.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$selectionPattern)) {
                $supportsSelectionItem = $true
                $isSelected = [bool]$selectionPattern.Current.IsSelected
            }
        }
    }

    return @{
        method = $method
        supportsSelectionItem = $supportsSelectionItem
        supportsInvoke = $supportsInvoke
        isSelected = $isSelected
        target = $Match
    }
}
