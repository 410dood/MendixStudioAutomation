param(
    [int]$ProcessId = 0,
    [string]$WindowTitlePattern = "",
    [string]$Item = "",
    [string]$Scope = "editor",
    [string]$ControlType = "",
    [string]$NearName = "",
    [int]$Radius = 0,
    [int]$Limit = 200
)

. "$PSScriptRoot\StudioPro.Automation.Common.ps1"

$context = Enter-StudioProScope -ProcessId $ProcessId -WindowTitlePattern $WindowTitlePattern -Item $Item -Scope $Scope -DelayMs 250
$attached = $context.Attached
$searchRoot = Get-ScopeSearchRoot -Root $attached.Element -Scope $Scope -Item $Item
$scopeBounds = if ($Scope -eq "editor" -and $Item) {
    $editorBounds = Get-OpenEditorContentBoundsByItem -Root $attached.Element -Item $Item
    if ($editorBounds) { $editorBounds } else { (Convert-AutomationElement -Element $searchRoot).boundingRectangle }
} elseif ($searchRoot -ne $attached.Element) {
    (Convert-AutomationElement -Element $searchRoot).boundingRectangle
} else {
    Get-ScopeBounds -Root $attached.Element -Scope $Scope
}
$rootBounds = (Convert-AutomationElement -Element $searchRoot).boundingRectangle

$nearTarget = $null
$nearX = $null
$nearY = $null
if ($NearName) {
    $nearTarget = Find-BestVisibleNamedElement -Root $attached.Element -Name $NearName -Surface $Scope -Item $Item
    if ($nearTarget) {
        $nearX = [double]($nearTarget.boundingRectangle.left + ($nearTarget.boundingRectangle.width / 2))
        $nearY = [double]($nearTarget.boundingRectangle.top + ($nearTarget.boundingRectangle.height / 2))
    }
}

$matches = @(Find-MatchingElements -Root $searchRoot -Depth 25 -MaxResults 5000 -ControlType $ControlType)
$results = @()
$seen = @{}

foreach ($match in ($matches | Where-Object {
    $_ -and
    -not $_.isOffscreen -and
    $_.boundingRectangle.left -ne $null -and
    $_.boundingRectangle.top -ne $null -and
    $_.boundingRectangle.width -ne $null -and
    $_.boundingRectangle.height -ne $null -and
    (Test-RectangleWithinBounds -Bounds $_.boundingRectangle -Container $rootBounds) -and
    (Test-RectangleWithinBounds -Bounds $_.boundingRectangle -Container $scopeBounds)
})) {
    $centerX = [double]($match.boundingRectangle.left + ($match.boundingRectangle.width / 2))
    $centerY = [double]($match.boundingRectangle.top + ($match.boundingRectangle.height / 2))
    $distance = $null
    if ($nearTarget) {
        $distance = [math]::Sqrt(([math]::Pow(($centerX - $nearX), 2)) + ([math]::Pow(($centerY - $nearY), 2)))
        if ($Radius -gt 0 -and $distance -gt $Radius) {
            continue
        }
    }

    $key = "{0}|{1}|{2}|{3}|{4}" -f $match.controlType, $match.name, $match.boundingRectangle.left, $match.boundingRectangle.top, $match.runtimeId
    if ($seen.ContainsKey($key)) {
        continue
    }

    $seen[$key] = $true
    $entry = [ordered]@{
        processId = $match.processId
        className = $match.className
        controlType = $match.controlType
        frameworkId = $match.frameworkId
        name = $match.name
        automationId = $match.automationId
        runtimeId = $match.runtimeId
        isEnabled = $match.isEnabled
        isOffscreen = $match.isOffscreen
        boundingRectangle = $match.boundingRectangle
    }
    if ($distance -ne $null) {
        $entry.distance = [math]::Round($distance, 2)
    }

    $results += [pscustomobject]$entry
}

if ($nearTarget) {
    $results = @($results | Sort-Object `
        @{ Expression = { if ($null -ne $_.distance) { $_.distance } else { [double]::PositiveInfinity } } }, `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } })
} else {
    $results = @($results | Sort-Object `
        @{ Expression = { $_.boundingRectangle.top } }, `
        @{ Expression = { $_.boundingRectangle.left } }, `
        @{ Expression = { $_.controlType } })
}

if ($results.Length -gt $Limit) {
    $results = @($results | Select-Object -First $Limit)
}

$payload = @{
    ok = $true
    action = "list-scope-elements"
    item = $Item
    scope = $Scope
    controlType = $ControlType
    nearName = $NearName
    radius = $Radius
    openMethod = $context.OpenMethod
    tab = $context.Tab
    nearTarget = $nearTarget
    count = @($results).Length
    items = @($results)
}

$payload | ConvertTo-Json -Depth 12
