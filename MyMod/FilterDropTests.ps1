$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$passes = 0

function Assert-True($condition, $message) {
    if (-not $condition) { throw $message }
    $script:passes++
}

foreach ($game in @('Game1', 'Game2')) {
    $mod = Get-Content (Join-Path $root "DecompiledSource/$game/ModFunc.cs") -Raw -Encoding UTF8
    $controller = Get-Content (Join-Path $root "DecompiledSource/$game/Controller.cs") -Raw -Encoding UTF8
    $gameScr = Get-Content (Join-Path $root "DecompiledSource/$game/GameScr.cs") -Raw -Encoding UTF8
    $panel = Get-Content (Join-Path $root "DecompiledSource/$game/Panel.cs") -Raw -Encoding UTF8
    $start = $mod.IndexOf('private void DoFilter()')
    $end = $mod.IndexOf('public static void DoBoss()', $start)
    Assert-True ($start -ge 0 -and $end -gt $start) "${game}: filter method is missing"
    $filter = $mod.Substring($start, $end - $start)

    Assert-True ($mod.Contains('public int delayMs = 500;') -and $mod.Contains('[AutoSetting]')) "${game}: default delay is missing"
    Assert-True ($mod.Contains('num - lastFilterTime >= filterDropSettings.delayMs')) "${game}: configured delay does not gate filtering"
    Assert-True ($mod.Contains('filterDropSettings.delayMs + "ms", 96)') -and $mod.Contains('case 96:')) "${game}: delay menu is missing"
    Assert-True ($mod.Contains('delay < 50 || delay > 10000') -and $mod.Contains('filterDropSettings.delayMs = delay;')) "${game}: delay range is not validated"
    Assert-True ($mod.Contains('AutoSettingsStore.Load(filterDropSettings,') -and $mod.Contains('AutoSettingsStore.SaveIfChanged(filterDropSettings,') -and $mod.Contains('"' + $game + '.AutoFilterDrop"')) "${game}: per-window delay is not persisted"
    Assert-True ($gameScr.Contains('ModFunc.GI().SetFilterDropDelay(text);') -and $panel.Contains('ModFunc.GI().SetFilterDropDelay(text);')) "${game}: a chat input path is missing"
    Assert-True ($filter.Contains('if (!isAutoFilterItem)') -and $filter.Contains('listFilterItem.id == item.template.id')) "${game}: automatic filter guards are missing"
    Assert-True ($filter.Contains('Service.gI().useItem(1, 1, (sbyte)item.indexUI, -1);')) "${game}: old drop behavior is missing"
    Assert-True ($filter.Contains('Thread.Sleep(50);')) "${game}: old request pacing is missing"
    Assert-True (-not $mod.Contains('waitForConfirmation') -and -not $mod.Contains('filterDropPending') -and -not $mod.Contains('OnFilterDropAcknowledged') -and -not $mod.Contains('case 95:')) "${game}: confirmation mode remains"
    Assert-True (-not $controller.Contains('OnFilterDropAcknowledged')) "${game}: controller still calls confirmation handler"
}

function Is-ValidDelay($value) { return ($value -ge 50 -and $value -le 10000) }
Assert-True (-not (Is-ValidDelay 49)) 'Delay below minimum was accepted'
Assert-True (Is-ValidDelay 50) 'Minimum delay was rejected'
Assert-True (Is-ValidDelay 10000) 'Maximum delay was rejected'
Assert-True (-not (Is-ValidDelay 10001)) 'Delay above maximum was accepted'

# Simulate timing while the bag changes; only an item in the filter list is selected.
$bag = @(1, 50)
$filterId = 1
$delay = 500
$last = 0
$sent = @()
function Filter-Tick($now) {
    if ($now - $script:last -lt $script:delay) { return }
    $script:last = $now
    for ($slot = 0; $slot -lt $script:bag.Count; $slot++) {
        if ($script:bag[$slot] -eq $script:filterId) {
            $script:sent += "${slot}:$($script:bag[$slot])"
            return
        }
    }
}
Filter-Tick 499
Assert-True ($sent.Count -eq 0) 'Filter sent before the delay elapsed'
Filter-Tick 500
Assert-True ($sent.Count -eq 1 -and $sent[0] -eq '0:1') 'Filtered item was not selected at the delay boundary'
$bag[0] = 50
$bag[1] = $null
Filter-Tick 1000
Assert-True ($sent.Count -eq 1) 'Unfiltered item was selected after bag compaction'
$bag[1] = 1
Filter-Tick 1499
Assert-True ($sent.Count -eq 1) 'Filter sent before the next delay elapsed'
Filter-Tick 1500
Assert-True ($sent.Count -eq 2 -and $sent[1] -eq '1:1') 'Filtered item in a new slot was skipped'

Write-Host "FilterDropTests: $passes PASS" -ForegroundColor Green
