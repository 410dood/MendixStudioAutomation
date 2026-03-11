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

function Get-MicroflowEditorSnapshot {
    param(
        [int]$ProcessId,
        [string]$WindowTitlePattern,
        [string]$Microflow,
        [int]$Limit = 40
    )

    try {
        $context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Microflow -Scope "editor" -DelayMs 150
        $items = @(Get-VisibleTextMatches -Root $context.Attached.Element -Scope "editor" -Item $Microflow -Limit $Limit | Where-Object {
            $_.name
        })
    } catch {
        return $null
    }

    return @{
        count = $items.Length
        names = @($items | ForEach-Object { $_.name })
    }
}

function Test-MicroflowEditorSnapshotChanged {
    param(
        [hashtable]$Before,
        [hashtable]$After
    )

    if (-not $Before -or -not $After) {
        return $false
    }

    if ($Before.count -ne $After.count) {
        return $true
    }

    $beforeNames = @($Before.names)
    $afterNames = @($After.names)
    if ($beforeNames.Length -ne $afterNames.Length) {
        return $true
    }

    for ($index = 0; $index -lt $beforeNames.Length; $index++) {
        if ($beforeNames[$index] -ne $afterNames[$index]) {
            return $true
        }
    }

    return $false
}

if (-not $Microflow) {
    throw "A microflow name is required."
}

if (-not $Target) {
    throw "A target node name is required."
}

if (-not $ActionName) {
    throw "A toolbox action name is required."
}

$microflowContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Microflow -Scope "editor" -DelayMs $DelayMs
$attached = $microflowContext.Attached
$targetMatch = Find-BestVisibleNamedElement -Root $attached.Element -Name $Target -Surface "editor" -Item $Microflow
if (-not $targetMatch) {
    throw "Could not find a visible microflow node named '$Target'."
}

Invoke-BoundsClick -Bounds $targetMatch.boundingRectangle | Out-Null
Start-Sleep -Milliseconds $DelayMs

$toolboxContext = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Scope "toolbox" -DelayMs $DelayMs
$attached = $toolboxContext.Attached
$actionMatch = Select-ToolboxItemByName -Root $attached.Element -Item $ActionName
if (-not $actionMatch) {
    throw "Could not find a visible Toolbox action named '$ActionName'."
}

$method = "dryRun"
$postDialog = $null
$editorBefore = Get-MicroflowEditorSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Microflow $Microflow -Limit 40
$editorAfter = $editorBefore
if (-not $DryRun) {
    $method = Invoke-BoundsDoubleClick -Bounds $actionMatch.boundingRectangle
    Start-Sleep -Milliseconds ($DelayMs + 150)
    $postDialog = Wait-ForStudioProDialogSnapshot -ProcessId $attached.Process.Id -WindowTitlePattern $WindowTitlePattern -TimeoutMs ([Math]::Max(1200, ($DelayMs * 4))) -PollMs 150 -Limit 30
    $editorAfter = Get-MicroflowEditorSnapshot -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Microflow $Microflow -Limit 40
}

$payload = @{
    ok = $true
    action = "insert-action"
    microflow = $Microflow
    target = $Target
    actionName = $ActionName
    dryRun = [bool]$DryRun
    method = $method
    openMethod = $microflowContext.OpenMethod
    tab = $microflowContext.Tab
    resolvedTarget = $targetMatch
    resolvedAction = $actionMatch
    postDialog = $postDialog
    editorBefore = $editorBefore
    editorAfter = $editorAfter
    editorChanged = (Test-MicroflowEditorSnapshotChanged -Before $editorBefore -After $editorAfter)
}

$payload | ConvertTo-Json -Depth 20
