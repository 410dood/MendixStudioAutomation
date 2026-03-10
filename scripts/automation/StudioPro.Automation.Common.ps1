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

    return ,$children
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

    $children = Get-ChildElements -Element $Element
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

        $children = Get-ChildElements -Element $Node
        $childCount = $children.Length
        for ($index = 0; $index -lt $childCount; $index++) {
            Search-Node -Node $children[$index] -CurrentDepth ($CurrentDepth - 1)
            if ($results.Count -ge $MaxResults) {
                return
            }
        }
    }

    Search-Node -Node $Root -CurrentDepth $Depth
    return ,($results.ToArray())
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

function Find-BestVisibleNamedElement {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [string]$Surface = "editor"
    )

    $matches = Find-MatchingElements -Root $Root -Depth 15 -MaxResults 50 -Name $Name
    $candidates = @($matches | Where-Object {
        $_.name -eq $Name -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ne $null -and
        $_.boundingRectangle.top -ge 380
    })

    if ($Surface -eq "editor") {
        $candidates = @($candidates | Where-Object { $_.boundingRectangle.left -ge 900 })
    }

    if (-not $candidates -or $candidates.Length -eq 0) {
        return $null
    }

    $preferred = $candidates | Sort-Object `
        @{ Expression = { if ($_.controlType -eq "Text") { 0 } else { 1 } } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1

    return $preferred
}

function Get-StudioProPopupWindows {
    param(
        [System.Windows.Automation.AutomationElement]$Root
    )

    $windows = Find-MatchingElements -Root $Root -Depth 6 -MaxResults 25 -ControlType "Window"
    $popups = @($windows | Where-Object {
        $_.className -eq "Popup" -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.top -ne $null
    })

    return ,$popups
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
            $textMatches = Find-MatchingElements -Root $nativePopup -Depth 8 -MaxResults 50 -ControlType "Text"
            $buttonMatches = Find-MatchingElements -Root $nativePopup -Depth 8 -MaxResults 20 -ControlType "Button"
            $texts = @($textMatches | Where-Object { $_.name } | Select-Object -ExpandProperty name -Unique)
            $buttons = @($buttonMatches | Where-Object { $_.name } | Select-Object -ExpandProperty name -Unique)
        }

        $summaries += @{
            popup = $popup
            texts = $texts
            buttons = $buttons
        }
    }

    return ,$summaries
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
        $popups = Get-StudioProPopupWindows -Root $attached.Element
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
    $remaining = Get-StudioProPopupSummary -Root $attached.Element
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

    $texts = Find-MatchingElements -Root $Root -Depth 20 -MaxResults 400 -ControlType "Text"
    $matches = @($texts | Where-Object {
        $_.name -eq $Item -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.left -ne $null -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ge 300 -and
        $_.boundingRectangle.left -lt 1050 -and
        $_.boundingRectangle.top -ge 380 -and
        $_.boundingRectangle.width -ne $null -and
        $_.boundingRectangle.width -lt 900
    })

    if ($matches.Length -eq 0) {
        return $null
    }

    return ($matches | Sort-Object `
        @{ Expression = { $_.boundingRectangle.left } }, `
        @{ Expression = { $_.boundingRectangle.top } } | Select-Object -First 1)
}

function Select-AppExplorerItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    $texts = Find-MatchingElements -Root $Root -Depth 20 -MaxResults 400 -ControlType "Text"
    $matches = @($texts | Where-Object {
        $_.name -eq $Item -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.left -ne $null -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ge 300 -and
        $_.boundingRectangle.left -lt 1050 -and
        $_.boundingRectangle.top -ge 380 -and
        $_.boundingRectangle.width -ne $null -and
        $_.boundingRectangle.width -lt 900
    })

    if ($matches.Length -eq 0) {
        return $null
    }

    return ($matches | Sort-Object `
        @{ Expression = { $_.boundingRectangle.left } }, `
        @{ Expression = { $_.boundingRectangle.top } } | Select-Object -First 1)
}

function Select-ToolboxItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Item
    )

    $texts = Find-MatchingElements -Root $Root -Depth 25 -MaxResults 500 -ControlType "Text"
    $matches = @($texts | Where-Object {
        $_.name -eq $Item -and
        -not $_.isOffscreen -and
        $_.boundingRectangle.left -ne $null -and
        $_.boundingRectangle.top -ne $null -and
        $_.boundingRectangle.left -ge 1100 -and
        $_.boundingRectangle.top -ge 380 -and
        $_.boundingRectangle.width -ne $null
    })

    if ($matches.Length -eq 0) {
        return $null
    }

    return ($matches | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } } | Select-Object -First 1)
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

    $children = Get-ChildElements -Element $Root
    $childCount = $children.Length
    for ($index = 0; $index -lt $childCount; $index++) {
        $match = Resolve-NativeElementByRuntimeId -Root $children[$index] -ExpectedRuntimeId $ExpectedRuntimeId -Depth ($Depth - 1)
        if ($match) {
            return $match
        }
    }

    return $null
}
