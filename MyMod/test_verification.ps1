$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  AUTO GOP V2 VERIFICATION TEST SUITE (STATIC + C# HARNESS)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$allPass = $true
$passCount = 0
$failCount = 0

function Assert-Test($condition, $name) {
    if ($condition) {
        $script:passCount++
        Write-Host "[PASS] $name" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "[FAIL] $name" -ForegroundColor Red
        $script:allPass = $false
    }
}

$root = "c:\Users\admin\Documents\Nro2"

# --- GROUP 1: FILE SYNCHRONIZATION & XMAP ISOLATION ---
Write-Host "`n[GROUP 1] File Synchronization & XMap Isolation..." -ForegroundColor Yellow

$pathG1 = "$root\DecompiledSource\Game1.Mod\AutoGop.cs"
$pathG2 = "$root\DecompiledSource\Game2.Mod\AutoGop.cs"
$pathMy1 = "$root\MyMod\AutoGop_Game1.cs"
$pathMy2 = "$root\MyMod\AutoGop_Game2.cs"

$existsAll = (Test-Path $pathG1) -and (Test-Path $pathG2) -and (Test-Path $pathMy1) -and (Test-Path $pathMy2)
Assert-Test $existsAll "All 4 AutoGop source/backup files exist"
if (-not $existsAll) { exit 1 }

$codeG1 = [System.IO.File]::ReadAllText($pathG1, [System.Text.Encoding]::UTF8)
$codeG2 = [System.IO.File]::ReadAllText($pathG2, [System.Text.Encoding]::UTF8)
$codeMy1 = [System.IO.File]::ReadAllText($pathMy1, [System.Text.Encoding]::UTF8)
$codeMy2 = [System.IO.File]::ReadAllText($pathMy2, [System.Text.Encoding]::UTF8)

Assert-Test ($codeG1 -eq $codeMy1) "Game1.Mod\AutoGop.cs is 100% synced with MyMod\AutoGop_Game1.cs"
Assert-Test ($codeG2 -eq $codeMy2) "Game2.Mod\AutoGop.cs is 100% synced with MyMod\AutoGop_Game2.cs"

$g1Norm = $codeG1.Replace("Game1.Mod", "Common.Mod").Replace("Game1.Assets", "Common.Assets")
$g2Norm = $codeG2.Replace("Game2.Mod", "Common.Mod").Replace("Game2.Assets", "Common.Assets")
Assert-Test ($g1Norm -eq $g2Norm) "Game1.Mod and Game2.Mod AutoGop logic are 100% mirrored"

$xd1 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game1.Mod.XMAP\XmapData.cs", [System.Text.Encoding]::UTF8)
$xd2 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game2.Mod.XMAP\XmapData.cs", [System.Text.Encoding]::UTF8)
$xc1 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game1.Mod.XMAP\XmapController.cs", [System.Text.Encoding]::UTF8)
$xc2 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game2.Mod.XMAP\XmapController.cs", [System.Text.Encoding]::UTF8)
Assert-Test ($xd1.Replace("Game1.", "GameX.") -eq $xd2.Replace("Game2.", "GameX.")) "Standalone Game1 & Game2 XmapData.cs are synchronized"
Assert-Test ($xc1.Replace("Game1.", "GameX.") -eq $xc2.Replace("Game2.", "GameX.")) "Standalone Game1 & Game2 XmapController.cs are synchronized"

$xmapDirty = (& git -C $root status --porcelain -- DecompiledSource/Game1.Mod.XMAP DecompiledSource/Game2.Mod.XMAP)
Assert-Test ([string]::IsNullOrWhiteSpace(($xmapDirty -join ""))) "Standalone XMAP modules are clean vs git HEAD"

# --- GROUP 2: AUTOGOP V2 INVARIANTS (NO XMAP, TABS 1-4 UPDATES) ---
Write-Host "`n[GROUP 2] AutoGop V2 Static Invariants (Tabs 0-4)..." -ForegroundColor Yellow

$noXmapInAutoGop = (-not $codeG1.Contains("XMAP")) -and
    (-not $codeG1.Contains("XmapController")) -and
    (-not $codeG1.Contains("isWaitingXmap")) -and
    (-not $codeG1.Contains("StartXmapToNpc")) -and
    (-not $codeG1.Contains("zenoReturnMapId")) -and
    (-not $codeG1.Contains("stepState == 7")) -and
    (-not $codeG1.Contains("MoveCloseToZenoNpc("))
Assert-Test $noXmapInAutoGop "AutoGop.cs contains zero XMap dependencies, return-map state, or instant cx/cy teleport"

$tab1V2 = $codeG1.Contains("public int targetItemIdBanh = 1337;") -and
    $codeG1.Contains("public int delayBuyBanhMs = 600;") -and
    (-not $codeG1.Contains("stopBanhCond1")) -and
    (-not $codeG1.Contains("targetBatchesBanh")) -and
    (-not $codeG1.Contains("delayUseBanhMs")) -and
    $codeG1.Contains("banhUseSent") -and
    $codeG1.Contains("CompleteBanhUsePhase") -and
    $codeG1.Contains("UseItemInBag(targetItemIdBanh)")
Assert-Test $tab1V2 "Tab 1 (Banh): delayBuyBanhMs=600, removed DK1 & delayUseBanhMs, single UseItemInBag(targetItemIdBanh=1337)"

$expectedHnTitle = "BOX H" + [char]0x1ED2 + "NG NG" + [char]0x1ECC + "C SSS"
$tab2V2 = $codeG1.Contains($expectedHnTitle) -and
    $codeG1.Contains("public int targetItemIdHn = 1581;") -and
    $codeG1.Contains("public int delayBuyHnMs = 600;") -and
    (-not $codeG1.Contains("autoUseHn")) -and
    (-not $codeG1.Contains("stopHnCond3")) -and
    $codeG1.Contains("public int stopConditionHn = 1;") -and
    $codeG1.Contains("public const long ZenoPerHn = 40000L;") -and
    $codeG1.Contains("(long)batchQtyHn * ZenoPerHn")
Assert-Test $tab2V2 "Tab 2 (HN): BOX HONG NGOC SSS [1581], delay=600, removed autoUseHn/DK2, 64-bit Zeno check"

$tab3V2 = $codeG1.Contains("public bool autoSelectMocDiem = false;") -and
    $codeG1.Contains("public int targetBatchesDiem = 1;") -and
    $codeG1.Contains("public int stopConditionDiem = 1;") -and
    $codeG1.Contains("pendingDiemAck") -and
    $codeG1.Contains("ConfirmDiemBatchSuccess")
Assert-Test $tab3V2 "Tab 3 (Diem): defaults (autoSelect=false, target=1, stopCond=1) and confirmed batch ack"

$tab4V2 = $codeG1.Contains("public enum ZenoStepState") -and
    $codeG1.Contains("public enum ZenoEndCode") -and
    $codeG1.Contains("WRONG_MAP_ZENO") -and
    $codeG1.Contains("ZENO_MENU_ERROR") -and
    $codeG1.Contains("ZENO_INPUT_TIMEOUT") -and
    $codeG1.Contains("ZENO_ACK_TIMEOUT") -and
    $codeG1.Contains("ZENO_SERVER_REJECT") -and
    $codeG1.Contains("QueueTabTransition") -and
    $codeG1.Contains("IsWithinNpcInteractRange") -and
    $codeG1.Contains("FocusNpcSafely") -and
    $codeG1.Contains("MoveCloseToZenoNpcSmoothly") -and
    $codeG1.Contains("InvalidateBagCache") -and
    $codeG1.Contains("OnBagUpdated") -and
    $codeG1.Contains("OnReceiveMenuChat") -and
    $codeG1.Contains("FindZenoMenuMatch") -and
    $codeG1.Contains("[AutoGop][Zeno]")
Assert-Test $tab4V2 "Tab 4 (Zeno): state machine, ZenoEndCode, smooth approach, deferred routing, bag invalidation & logging"

# --- GROUP 3: CONTROLLER & CONTROLLER2 HOOKS (GAME1 & GAME2) ---
Write-Host "`n[GROUP 3] Controller & Controller2 Packet Hooks..." -ForegroundColor Yellow

$ctrlG1 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game1\Controller.cs", [System.Text.Encoding]::UTF8)
$ctrlG2 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game2\Controller.cs", [System.Text.Encoding]::UTF8)
$ctrl2G1 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game1.Assets.src.f\Controller2.cs", [System.Text.Encoding]::UTF8)
$ctrl2G2 = [System.IO.File]::ReadAllText("$root\DecompiledSource\Game2.Assets.src.f\Controller2.cs", [System.Text.Encoding]::UTF8)

$hooksG1 = $ctrlG1.Contains("Game1.Mod.AutoGop.gI().OnBagUpdated()") -and
    $ctrlG1.Contains("Game1.Mod.AutoGop.gI().OnReceiveMenuChat(") -and
    $ctrlG1.Contains("Game1.Mod.AutoGop.gI().GetCurrentTargetNpcId()") -and
    $ctrlG1.Contains("Game1.Mod.AutoGop.gI().OnReceiveOpenUiMenu(") -and
    $ctrlG1.Contains("Game1.Mod.AutoGop.gI().OnReceiveNewMenu(") -and
    $ctrlG1.Contains("ShouldSuppressGameUi()") -and
    (-not $ctrlG1.Contains("AutoGop.gI().hideUi"))
Assert-Test $hooksG1 "Game1\Controller.cs has -36 OnBagUpdated, case 32 OnReceiveMenuChat, case 27/33/57 hooks & ShouldSuppressGameUi"

$hooksG2 = $ctrlG2.Contains("Game2.Mod.AutoGop.gI().OnBagUpdated()") -and
    $ctrlG2.Contains("Game2.Mod.AutoGop.gI().OnReceiveMenuChat(") -and
    $ctrlG2.Contains("Game2.Mod.AutoGop.gI().GetCurrentTargetNpcId()") -and
    $ctrlG2.Contains("Game2.Mod.AutoGop.gI().OnReceiveOpenUiMenu(") -and
    $ctrlG2.Contains("Game2.Mod.AutoGop.gI().OnReceiveNewMenu(") -and
    $ctrlG2.Contains("ShouldSuppressGameUi()") -and
    (-not $ctrlG2.Contains("AutoGop.gI().hideUi"))
Assert-Test $hooksG2 "Game2\Controller.cs has -36 OnBagUpdated, case 32 OnReceiveMenuChat, case 27/33/57 hooks & ShouldSuppressGameUi"

$hasCtrl2_1 = $ctrl2G1.Contains("using Game1.Mod;") -and ($ctrl2G1 -match '(?s)case -125:.*?AutoGop\.gI\(\)\.OnReceiveServerInputPacket\(msg\).*?msg\.reader\(\)\.readUTF\(\)')
$hasCtrl2_2 = $ctrl2G2.Contains("using Game2.Mod;") -and ($ctrl2G2 -match '(?s)case -125:.*?AutoGop\.gI\(\)\.OnReceiveServerInputPacket\(msg\).*?msg\.reader\(\)\.readUTF\(\)')
Assert-Test ($hasCtrl2_1 -and $hasCtrl2_2) "Both Game1 & Game2 Controller2.cs guard packet -125 via OnReceiveServerInputPacket(msg) before reader"

# --- GROUP 4: REAL C# BEHAVIORAL & STATE MACHINE TESTS (.NET 8) ---
Write-Host "`n[GROUP 4] Executing Real Compiled C# Behavioral Tests..." -ForegroundColor Yellow

$dotnet = "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Data\DotNetSdk\dotnet.exe"
$testProj = "$root\MyMod\AutoGopTests\AutoGopTests.csproj"
$hasDotnet = (Test-Path $dotnet) -and (Test-Path $testProj)
Assert-Test $hasDotnet "Unity .NET SDK and AutoGopTests.csproj exist"

if ($hasDotnet) {
    $output = & $dotnet run --project $testProj -c Release 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        $str = "$line"
        if ($str.StartsWith("[PASS]")) {
            Write-Host "  $str" -ForegroundColor DarkGreen
        } elseif ($str.StartsWith("[FAIL]") -or $str.StartsWith("TOTAL PASSED")) {
            Write-Host "  $str" -ForegroundColor $(if ($str.Contains("FAILED: 0")) { "Green" } else { "Red" })
        } elseif ($str -match "error CS") {
            Write-Host "  $str" -ForegroundColor Red
        }
    }
    $summaryOk = ($exitCode -eq 0) -and (($output -join "`n") -match "FAILED:\s*0")
    Assert-Test $summaryOk "All compiled C# AutoGop unit & integration tests passed (exit=$exitCode)"
}

# --- SUMMARY ---
Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  SUMMARY: $passCount PASSED, $failCount FAILED" -ForegroundColor $(if ($allPass) { "Green" } else { "Red" })
if ($allPass) {
    Write-Host "  >>> ALL VERIFICATION TESTS PASSED SUCCESSFULLY! (100%) <<<" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  >>> SOME TESTS FAILED! DO NOT BUILD! <<<" -ForegroundColor Red
    Write-Host "==========================================================" -ForegroundColor Cyan
    exit 1
}
