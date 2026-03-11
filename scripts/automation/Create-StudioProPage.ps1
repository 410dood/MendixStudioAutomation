param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Module = "",
    [string]$PageName = "",
    [string]$Template = "",
    [int]$DelayMs = 250,
    [int]$TimeoutMs = 15000
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

if (-not $Module) {
    throw "A Mendix module name is required."
}

if (-not $PageName) {
    throw "A Mendix page name is required."
}

function Invoke-DialogControlSelection {
    param(
        [System.Windows.Automation.AutomationElement]$Dialog,
        [string]$Name,
        [string]$ControlType = "",
        [int]$DelayMs = 250
    )

    $matches = @(Find-DialogNamedElements -Dialog $Dialog -Name $Name -Limit 80 | Where-Object {
        $_.name -eq $Name -and (-not $ControlType -or $_.controlType -eq $ControlType)
    })

    if ($matches.Length -eq 0) {
        throw "Could not find a '$Name' control in the current Studio Pro dialog."
    }

    $ordered = @($matches | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })

    return Select-AutomationMatch -Root $Dialog -Match $ordered[0] -DelayMs $DelayMs
}

function Wait-DialogWindow {
    param(
        [int]$ProcessId,
        [string]$WindowTitlePattern,
        [string]$Name,
        [int]$TimeoutMs = 10000
    )

    $dialogWait = Wait-ForStudioProWindowByName `
        -ProcessId $ProcessId `
        -WindowTitlePattern $WindowTitlePattern `
        -Name $Name `
        -TimeoutMs $TimeoutMs `
        -PollMs 250

    if (-not $dialogWait -or -not $dialogWait.Window) {
        throw "Studio Pro did not open the '$Name' dialog."
    }

    $nativeDialog = Resolve-NativeWindowByName -Root $dialogWait.Attached.Element -Name $Name -Depth 20
    if (-not $nativeDialog) {
        throw "Could not attach to the native Studio Pro dialog '$Name'."
    }

    return @{
        Attached = $dialogWait.Attached
        Window = $dialogWait.Window
        NativeDialog = $nativeDialog
    }
}

function Wait-DialogClosed {
    param(
        [int]$ProcessId,
        [string]$WindowTitlePattern,
        [string]$Name,
        [int]$TimeoutMs = 6000
    )

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    do {
        Start-Sleep -Milliseconds 200
        $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
        $window = Get-StudioProWindowMatchByName -Root $attached.Element -Name $Name
        if (-not $window) {
            return $true
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    return $false
}

function Close-DialogByNameBestEffort {
    param(
        [int]$ProcessId,
        [string]$WindowTitlePattern,
        [string]$Name,
        [int]$DelayMs = 250
    )

    $attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
    $window = Get-StudioProWindowMatchByName -Root $attached.Element -Name $Name
    if (-not $window) {
        return $null
    }

    $dialog = Resolve-NativeWindowByName -Root $attached.Element -Name $Name -Depth 20
    if (-not $dialog) {
        return @{
            name = $Name
            closed = $false
            method = "notAttached"
        }
    }

    $cancelButtons = @(Find-DialogNamedElements -Dialog $dialog -Name "Cancel" -Limit 20 | Where-Object {
        $_.name -eq "Cancel" -and $_.controlType -eq "Button"
    })
    if ($cancelButtons.Count -gt 0) {
        $selection = Select-AutomationMatch -Root $dialog -Match ($cancelButtons | Select-Object -First 1) -DelayMs $DelayMs
        return @{
            name = $Name
            closed = Wait-DialogClosed -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Name $Name -TimeoutMs 2500
            method = "cancelButton"
            selection = $selection
        }
    }

    $closeButtons = @(Get-ChildElements -Element $dialog | Where-Object {
        $_.Current.ControlType.ProgrammaticName -eq "ControlType.Button" -and $_.Current.AutomationId -eq "CloseWindow"
    })
    if ($closeButtons.Count -gt 0) {
        $closeSelection = Invoke-AutomationAction -Element ($closeButtons | Select-Object -First 1) -Action "click"
        Start-Sleep -Milliseconds $DelayMs
        return @{
            name = $Name
            closed = Wait-DialogClosed -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Name $Name -TimeoutMs 2500
            method = $closeSelection
        }
    }

    return @{
        name = $Name
        closed = $false
        method = "noCloseControl"
    }
}

$attached = Get-StudioProWindowElement -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern
Set-StudioProForegroundWindow -Process $attached.Process

Send-KeysToForegroundWindow -Keys "^n" -DelayMs ($DelayMs + 100)
try {
    $newDocumentDialog = Wait-DialogWindow -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "New Document" -TimeoutMs ([Math]::Min($TimeoutMs, 3000))
} catch {
    Set-StudioProForegroundWindow -Process $attached.Process
    Send-KeysToForegroundWindow -Keys "%f" -DelayMs 180
    Send-KeysToForegroundWindow -Keys "n" -DelayMs ($DelayMs + 100)
    $newDocumentDialog = Wait-DialogWindow -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "New Document" -TimeoutMs ([Math]::Min($TimeoutMs, 5000))
}

$documentTypeSelection = Invoke-DialogControlSelection -Dialog $newDocumentDialog.NativeDialog -Name "Page" -DelayMs $DelayMs
$moduleSelection = Invoke-DialogControlSelection -Dialog $newDocumentDialog.NativeDialog -Name $Module -DelayMs $DelayMs
$createSelection = Invoke-DialogControlSelection -Dialog $newDocumentDialog.NativeDialog -Name "Create" -ControlType "Button" -DelayMs ($DelayMs + 100)

$createPageDialog = Wait-DialogWindow -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Create Page" -TimeoutMs ([Math]::Min($TimeoutMs, 8000))
$pageNameField = Find-DialogFieldByLabel -Dialog $createPageDialog.NativeDialog -Label "Page name" -ControlType "Edit"
if (-not $pageNameField -or -not $pageNameField.field) {
    throw "Could not resolve the Page name field in the Create Page dialog."
}

$pageNameSelection = Set-DialogFieldValue -Dialog $createPageDialog.NativeDialog -FieldMatch $pageNameField -Value $PageName -DelayMs $DelayMs
$templateSelection = Select-CreatePageTemplateCard -Dialog $createPageDialog.NativeDialog -Template $Template -DelayMs ($DelayMs + 100)
if (-not $templateSelection) {
    $available = @(Get-CreatePageTemplateChoices -Dialog $createPageDialog.NativeDialog -Limit 80 | ForEach-Object { $_.name })
    $availableText = if ($available.Length -gt 0) { $available -join ", " } else { "(none visible)" }
    throw "Could not resolve a visible Create Page template. Visible template choices: $availableText"
}

$acceptStrategies = @()

Set-StudioProForegroundWindow -Process $attached.Process
Send-KeysToForegroundWindow -Keys "{ENTER}" -DelayMs ($DelayMs + 150)
$enterClosed = Wait-DialogClosed -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Create Page" -TimeoutMs ([Math]::Min($TimeoutMs, 4000))
$acceptStrategies += @(
    @{
        strategy = "clickThenEnter"
        dialogClosed = $enterClosed
    }
)

if (-not $enterClosed) {
    $okSelection = Invoke-DialogControlSelection -Dialog $createPageDialog.NativeDialog -Name "OK" -ControlType "Button" -DelayMs ($DelayMs + 150)
    $okClosed = Wait-DialogClosed -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Create Page" -TimeoutMs ([Math]::Min($TimeoutMs, 4000))
    $acceptStrategies += @(
        @{
            strategy = "okButton"
            selection = $okSelection
            dialogClosed = $okClosed
        }
    )

    if (-not $okClosed) {
        $doubleClickMethod = Invoke-BoundsDoubleClick -Bounds $templateSelection.card.boundingRectangle
        Start-Sleep -Milliseconds ($DelayMs + 100)
        $doubleClosed = Wait-DialogClosed -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Create Page" -TimeoutMs ([Math]::Min($TimeoutMs, 4000))
        $acceptStrategies += @(
            @{
                strategy = "doubleClickCard"
                method = $doubleClickMethod
                dialogClosed = $doubleClosed
            }
        )
    }
}

$pageTab = $null
$tabDeadline = [DateTime]::UtcNow.AddMilliseconds([Math]::Min($TimeoutMs, 8000))
do {
    $pageTab = Find-OpenEditorTabForItem -Root (Get-StudioProWindowElement -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern).Element -Item $PageName
    if ($pageTab) {
        break
    }

    Start-Sleep -Milliseconds 250
} while ([DateTime]::UtcNow -lt $tabDeadline)

$cleanup = @()
if ($pageTab) {
    $createPageCleanup = Close-DialogByNameBestEffort -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -Name "Create Page" -DelayMs $DelayMs
    if ($createPageCleanup) {
        $cleanup += @($createPageCleanup)
    }
}

$payload = @{
    ok = $true
    action = "create-page"
    module = $Module
    pageName = $PageName
    template = if ($Template) { $Template } else { $templateSelection.choice.name }
    documentTypeSelection = $documentTypeSelection
    moduleSelection = $moduleSelection
    createSelection = $createSelection
    pageNameSelection = $pageNameSelection
    templateSelection = $templateSelection
    acceptStrategies = $acceptStrategies
    cleanup = $cleanup
    tab = $pageTab
    pageCreated = [bool]$pageTab
    process = @{
        id = $attached.Process.Id
        name = $attached.Process.ProcessName
        mainWindowTitle = $attached.Process.MainWindowTitle
    }
}

$payload | ConvertTo-Json -Depth 20
