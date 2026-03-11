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
        $actualControlType = $Element.Current.ControlType.ProgrammaticName -replace "^ControlType\.", ""
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
        Invoke-BoundsClick -Bounds $tab.boundingRectangle | Out-Null
        Start-Sleep -Milliseconds $DelayMs
        $attached = Get-StudioProWindowElement -ProcessId $Process.Id -WindowTitlePattern $WindowTitlePattern
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
            $bounds = Get-ScopeBounds -Root $Root -Scope "editor"
            if ($null -eq $bounds.left -or $null -eq $bounds.top -or $null -eq $bounds.right -or $null -eq $bounds.bottom) {
                return
            }

            $width = $bounds.right - $bounds.left
            $height = $bounds.bottom - $bounds.top
            if ($width -le 20 -or $height -le 20) {
                return
            }

            Invoke-BoundsClick -Bounds @{
                left = $bounds.left + 8
                top = $bounds.top + 8
                width = $width - 16
                height = $height - 16
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
    Focus-StudioProScope -Root $attached.Element -Scope $Scope -DelayMs $DelayMs
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
    if ($openTab -and -not $openTab.isOffscreen) {
        Invoke-BoundsClick -Bounds $openTab.boundingRectangle | Out-Null
        Start-Sleep -Milliseconds $DelayMs
        return @{
            method = "selectOpenTab"
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
        "MenuItem" { return 5 }
        "CheckBox" { return 6 }
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
            $texts = @($textMatches | Where-Object { $_.name } | Select-Object -ExpandProperty name -Unique)
            $buttons = @($buttonMatches | Where-Object { $_.name } | Select-Object -ExpandProperty name -Unique)
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

    foreach ($controlType in @("TreeItem", "ListItem", "DataItem", "Custom", "Text")) {
        $matches = @(Find-MatchingElements -Root $Root -Depth 25 -MaxResults 100 -Name $Item -ControlType $controlType | Where-Object {
            $bounds = $_.boundingRectangle
            $isWithinPageExplorer = $false
            if ($controlType -in @("TreeItem", "ListItem", "DataItem", "Custom")) {
                $isWithinPageExplorer = (
                    $bounds.left -ne $null -and
                    $bounds.top -ne $null -and
                    $bounds.bottom -ne $null -and
                    $bounds.left -ge $scopeBounds.left -and
                    $bounds.top -ge $scopeBounds.top -and
                    $bounds.bottom -le $scopeBounds.bottom
                )
            } else {
                $isWithinPageExplorer = Test-RectangleWithinBounds -Bounds $bounds -Container $scopeBounds
            }

            $_ -and
            $_.name -eq $Item -and
            -not $_.isOffscreen -and
            $isWithinPageExplorer
        })

        if ($matches.Length -gt 0) {
            return ($matches | Sort-Object `
                @{ Expression = { $_.boundingRectangle.top } }, `
                @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)
        }
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
