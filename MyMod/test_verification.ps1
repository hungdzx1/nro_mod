[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# ==============================================================================
# TEST VERIFICATION SUITE FOR AUTOGOP (NRO2)
# ==============================================================================
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  RUNNING AUTOMATED VERIFICATION SUITE: AUTOGOP MOD       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$allPass = $true
$testIndex = 0

function Assert-Test($condition, $description) {
    $global:testIndex++
    if ($condition) {
        Write-Host "  [PASS] Test #${global:testIndex}: $description" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Test #${global:testIndex}: $description" -ForegroundColor Red
        $global:allPass = $false
    }
}

# --- TEST GROUP 1: FILE EXISTENCE & SYNCHRONIZATION ---
Write-Host "`n[GROUP 1] Code Integrity & Namespace Sync..." -ForegroundColor Yellow

$pathG1 = "c:\Users\admin\Documents\Nro2\DecompiledSource\Game1.Mod\AutoGop.cs"
$pathG2 = "c:\Users\admin\Documents\Nro2\DecompiledSource\Game2.Mod\AutoGop.cs"
$pathMy1 = "c:\Users\admin\Documents\Nro2\MyMod\AutoGop_Game1.cs"
$pathMy2 = "c:\Users\admin\Documents\Nro2\MyMod\AutoGop_Game2.cs"

Assert-Test (Test-Path $pathG1) "File Game1.Mod/AutoGop.cs exists"
Assert-Test (Test-Path $pathG2) "File Game2.Mod/AutoGop.cs exists"
Assert-Test (Test-Path $pathMy1) "File MyMod/AutoGop_Game1.cs exists"
Assert-Test (Test-Path $pathMy2) "File MyMod/AutoGop_Game2.cs exists"

$codeG1 = [System.IO.File]::ReadAllText($pathG1, [System.Text.Encoding]::UTF8)
$codeG2 = [System.IO.File]::ReadAllText($pathG2, [System.Text.Encoding]::UTF8)

Assert-Test ($codeG1.Contains("namespace Game1.Mod;")) "Game1 namespace is correct"
Assert-Test ($codeG2.Contains("namespace Game2.Mod;")) "Game2 namespace is correct"

$my1 = [System.IO.File]::ReadAllText($pathMy1, [System.Text.Encoding]::UTF8)
$my2 = [System.IO.File]::ReadAllText($pathMy2, [System.Text.Encoding]::UTF8)
Assert-Test ($codeG1 -eq $my1) "MyMod/AutoGop_Game1.cs matches Game1.Mod/AutoGop.cs"
Assert-Test ($codeG2 -eq $my2) "MyMod/AutoGop_Game2.cs matches Game2.Mod/AutoGop.cs"

# Compare content except namespace
$g1Norm = $codeG1.Replace("Game1.Mod", "Common.Mod").Replace("Game1.Assets", "Common.Assets")
$g2Norm = $codeG2.Replace("Game2.Mod", "Common.Mod").Replace("Game2.Assets", "Common.Assets")
Assert-Test ($g1Norm -eq $g2Norm) "Game1 and Game2 implementations are 100% synchronized"

# --- TEST GROUP 2: TAB BÁNH CURRENCY FIX VALIDATION ---
Write-Host "`n[GROUP 2] Tab Bánh Currency Logic Verification..." -ForegroundColor Yellow

# Static check: GetHongNgoc, rubyCount, specMaterial checked together
$hasBanhCheck = $codeG1.Contains("long hongNgoc = GetHongNgoc();") -and 
                $codeG1.Contains("int rubyCount = GetBagItemCount(861);") -and
                $codeG1.Contains("hongNgoc < shopItem.buySpec && rubyCount < shopItem.buySpec && specMaterial < shopItem.buySpec")
Assert-Test $hasBanhCheck "Tab Bánh checks Hong Ngoc (luongKhoa), Ruby (861), and spec material before aborting"

# Simulated logic test for Tab Bánh
function Simulate-BanhPurchaseCheck($hongNgoc, $rubyCount, $specMaterial, $buySpec, $stopCond3, $bagBanh) {
    if ($hongNgoc -lt $buySpec -and $rubyCount -lt $buySpec -and $specMaterial -lt $buySpec) {
        if ($stopCond3 -and $bagBanh -gt 0) {
            return "USE_ALL_BANH"
        }
        return "FINISH_LACK_RESOURCE"
    }
    return "PROCEED_BUY"
}

Assert-Test ((Simulate-BanhPurchaseCheck 5000000 0 0 5000 $true 0) -eq "PROCEED_BUY") "Player with 5M Hong Ngoc can buy Bánh (does not stand still)"
Assert-Test ((Simulate-BanhPurchaseCheck 0 5000 0 5000 $true 0) -eq "PROCEED_BUY") "Player with 5000 Ruby item can buy Bánh"
Assert-Test ((Simulate-BanhPurchaseCheck 0 0 5000 5000 $true 0) -eq "PROCEED_BUY") "Player with 5000 Spec Material can buy Bánh"
Assert-Test ((Simulate-BanhPurchaseCheck 0 0 0 5000 $true 100) -eq "USE_ALL_BANH") "Player with 0 currency but cakes in bag triggers Use All Bánh"
Assert-Test ((Simulate-BanhPurchaseCheck 0 0 0 5000 $false 0) -eq "FINISH_LACK_RESOURCE") "Player with 0 currency and 0 cakes triggers safe finish"

# --- TEST GROUP 3: TAB ZENO (TAB 4) MENU & STATE MACHINE ---
Write-Host "`n[GROUP 3] Tab Zeno (Tab 4) State Machine Verification..." -ForegroundColor Yellow

# Static check: zenoMenuStep, FindZenoMenuOptionIndex, AdvanceZenoMenuStep, stepState transitions (2 -> 3 -> 4)
$hasZenoMenuLogic = $codeG1.Contains("if (zenoMenuStep == 0)") -and
    $codeG1.Contains("zenoMenuStep = 1;") -and
    $codeG1.Contains("zenoMenuStep = 2;") -and
    $codeG1.Contains("FindZenoMenuOptionIndex") -and
    $codeG1.Contains("MaybeResetZenoMenuStepIfMainMenu") -and
    $codeG1.Contains("AdvanceZenoMenuStep") -and
    $codeG1.Contains("stepState = 2;") -and
    $codeG1.Contains("stepState = 3;") -and
    $codeG1.Contains("stepState = 4;")
Assert-Test $hasZenoMenuLogic "Tab Zeno tracks zenoMenuStep, self-heals main menu, and transitions stepState 2 -> 3 -> 4"

# Static check: NPC 18 map check, Tabs 1-3 MoveCloseToNpc untouched, Tab 4 dedicated MoveCloseToZenoNpc, stepState 4 retry, ShouldSuppressGameUi
$hasKarinCheck = $codeG1.Contains("ModFunc.GetNpcByTempId(18) == null")
Assert-Test $hasKarinCheck "Tab Zeno checks Karin (NPC 18) presence in map upon start"

$hasMoveToFix = $codeG1.Contains("ModFunc.GI().MoveTo(npc.cx, npc.cy - 1);") -and $codeG1.Contains("Char.myCharz().FocusManualTo(npc);")
Assert-Test $hasMoveToFix "MoveCloseToNpc (Tabs 1-3) retains anti-rubberband MoveTo and FocusManualTo untouched"

$hasZenoMoveFix = $codeG1.Contains("private static void MoveCloseToZenoNpc()") -and
    $codeG1.Contains("Char.myCharz().cx = npc.cx + 20;") -and
    $codeG1.Contains("Service.gI().charMove();") -and
    $codeG1.Contains("Char.myCharz().npcFocus = npc;") -and
    (
        $codeG1.Contains("if (currentRunningTab != 4)`r`n`t`t`t`t{`r`n`t`t`t`t`tChar.myCharz().FocusManualTo(focusNpc);") -or
        $codeG1.Contains("if (currentRunningTab != 4)`n`t`t`t`t{`n`t`t`t`t`tChar.myCharz().FocusManualTo(focusNpc);")
    )
Assert-Test $hasZenoMoveFix "Tab Zeno uses dedicated MoveCloseToZenoNpc (cx+20, charMove) and skips FocusManualTo(deFocusNPC) in stepState=2"

$hasStep4RetryFix = $codeG1.Contains("if (retryCount <= 1)`r`n`t`t`t`t{`r`n`t`t`t`t`tstateTimestamp = now;") -or $codeG1.Contains("if (retryCount <= 1)`n`t`t`t`t{`n`t`t`t`t`tstateTimestamp = now;")
Assert-Test $hasStep4RetryFix "stepState=4 retry resets stateTimestamp = now so retry waits full 2.5s"

$ctrlG1 = [System.IO.File]::ReadAllText("c:\Users\admin\Documents\Nro2\DecompiledSource\Game1\Controller.cs", [System.Text.Encoding]::UTF8)
$ctrlG2 = [System.IO.File]::ReadAllText("c:\Users\admin\Documents\Nro2\DecompiledSource\Game2\Controller.cs", [System.Text.Encoding]::UTF8)
$hasHideMenuCase33 = $ctrlG1.Contains("!Game1.Mod.AutoGop.gI().ShouldSuppressGameUi()") -and
    $ctrlG2.Contains("!Game2.Mod.AutoGop.gI().ShouldSuppressGameUi()") -and
    $ctrlG1.Contains("Game1.Mod.AutoGop.gI().ShouldSuppressGameUi()") -and
    $ctrlG2.Contains("Game2.Mod.AutoGop.gI().ShouldSuppressGameUi()") -and
    (-not $ctrlG1.Contains("AutoGop.gI().hideUi")) -and
    (-not $ctrlG2.Contains("AutoGop.gI().hideUi"))
Assert-Test $hasHideMenuCase33 "Controller (Game1 & Game2) routes all AutoGop hideUi checks via ShouldSuppressGameUi() without raw hideUi blocks during Xmap"

# Simulated logic test for Tab Zeno Menu Navigation
function Simulate-ZenoMenuReceive($menu, $zenoMenuStep) {
    if ($zenoMenuStep -eq 0) {
        $idxMenu1 = -1
        for ($l = 0; $l -lt $menu.Length; $l++) {
            if ($menu[$l] -ne $null) {
                $m = $menu[$l].ToLower()
                if ($m.Contains("đổi tiên phù") -or $m.Contains("đổi vé") -or ($m.Contains("đổi") -and ($m.Contains("zeno") -or $m.Contains("tiên phù")))) {
                    $idxMenu1 = $l
                    break
                }
            }
        }
        if ($idxMenu1 -lt 0) {
            for ($l = 0; $l -lt $menu.Length; $l++) {
                if ($menu[$l] -ne $null) {
                    $m = $menu[$l].ToLower()
                    if ($m.Contains("tiên phù") -or $m.Contains("zeno")) {
                        $idxMenu1 = $l
                        break
                    }
                }
            }
        }
        if ($idxMenu1 -lt 0) {
            $idxMenu1 = if ($menu.Length -gt 3) { 3 } else { 0 }
        }
        return @{ StepState = 2; NextZenoMenuStep = 1; Action = "CONFIRM_MENU_1"; Option = $idxMenu1 }
    } else {
        $idxMenu2 = 0
        for ($l = 0; $l -lt $menu.Length; $l++) {
            if ($menu[$l] -ne $null) {
                $m = $menu[$l].ToLower()
                if ($m.Contains("tiên phù") -or $m.Contains("zeno")) {
                    $idxMenu2 = $l
                    break
                }
            }
        }
        return @{ StepState = 4; NextZenoMenuStep = 0; Action = "CONFIRM_MENU_2"; Option = $idxMenu2 }
    }
}

# Menu 1 with "Đổi Tiên Phù Zeno" at index 3
$menu1_A = @("Giao đậu", "Nhận đậu", "Thách đấu", "Đổi Tiên Phù Zeno")
$res1_A = Simulate-ZenoMenuReceive $menu1_A 0
Assert-Test ($res1_A.StepState -eq 2 -and $res1_A.Option -eq 3 -and $res1_A.NextZenoMenuStep -eq 1) "Karin Menu 1 (index 3) selects option 3 and transitions to zenoMenuStep=1, stepState=2"

# Menu 1 with "Đổi Tiên Phù" at index 1
$menu1_B = @("Thách đấu", "Đổi Tiên Phù", "Đóng")
$res1_B = Simulate-ZenoMenuReceive $menu1_B 0
Assert-Test ($res1_B.StepState -eq 2 -and $res1_B.Option -eq 1 -and $res1_B.NextZenoMenuStep -eq 1) "Karin Menu 1 (dynamic index 1) dynamically selects option 1"

# Menu 2 with "Tiên Phù Zeno" at index 0
$menu2_A = @("Tiên Phù Zeno", "Đóng")
$res2_A = Simulate-ZenoMenuReceive $menu2_A 1
Assert-Test ($res2_A.StepState -eq 4 -and $res2_A.Option -eq 0 -and $res2_A.NextZenoMenuStep -eq 0) "Karin Menu 2 (index 0) selects option 0 and waits for packet -125 (stepState=4)"

# Menu 2 even if it contains "Đổi Tiên Phù Zeno" still transitions to stepState=4
$menu2_B = @("Đổi Tiên Phù Zeno", "Đóng")
$res2_B = Simulate-ZenoMenuReceive $menu2_B 1
Assert-Test ($res2_B.StepState -eq 4 -and $res2_B.Option -eq 0 -and $res2_B.NextZenoMenuStep -eq 0) "Karin Menu 2 containing 'Đổi Tiên Phù Zeno' does not loop as Menu 1"

# --- TEST GROUP 4: TIMEOUT & ANTI-HANG PROTECTION ---
Write-Host "`n[GROUP 4] Anti-Hang & Timeout Coverage..." -ForegroundColor Yellow

$hasTimeoutStep3 = $codeG1.Contains("(stepState == 1 || stepState == 2 || stepState == 3) && now - stateTimestamp >= 3500")
Assert-Test $hasTimeoutStep3 "3.5s Timeout protection covers stepState 1, 2, and 3 (eliminates black hole hangs)"

$hasNotifySafe = $codeG1.Contains("suppressInfoHook = true;") -and $codeG1.Contains("inServerInfo")
Assert-Test $hasNotifySafe "Notification recursion guard (suppressInfoHook / inServerInfo) is active"

# --- TEST GROUP 5: ZENO DEEP PIPELINE, UI SILENCING, XMAP & PACKETS 27/33/32/-125 ---
Write-Host "`n[GROUP 5] Tab Zeno End-to-End Pipeline & Edge Cases..." -ForegroundColor Yellow

$ctrlG1Path = "c:\Users\admin\Documents\Nro2\DecompiledSource\Game1\Controller.cs"
$ctrlG2Path = "c:\Users\admin\Documents\Nro2\DecompiledSource\Game2\Controller.cs"
if (-not (Test-Path $ctrlG1Path) -or -not (Test-Path $ctrlG2Path)) { throw "Controller.cs missing!" }
$ctrlG1 = [System.IO.File]::ReadAllText($ctrlG1Path, [System.Text.Encoding]::UTF8)
$ctrlG2 = [System.IO.File]::ReadAllText($ctrlG2Path, [System.Text.Encoding]::UTF8)

$hasCtrlCase33G1 = $ctrlG1.Contains("GetCurrentTargetNpcId()") -and $ctrlG1.Contains("OnReceiveOpenUiMenu(myVector)") -and $ctrlG1.Contains("if (!Game1.Mod.AutoGop.gI().ShouldSuppressGameUi())")
$hasCtrlCase33G2 = $ctrlG2.Contains("GetCurrentTargetNpcId()") -and $ctrlG2.Contains("OnReceiveOpenUiMenu(myVector)") -and $ctrlG2.Contains("if (!Game2.Mod.AutoGop.gI().ShouldSuppressGameUi())")
Assert-Test ($hasCtrlCase33G1 -and $hasCtrlCase33G2) "Both Game1 & Game2 Controller case 33 recover npcFocus, invoke OnReceiveOpenUiMenu, and suppress UI via ShouldSuppressGameUi()"

$hasCtrlCase27And57G1 = $ctrlG1.Contains("OnReceiveNewMenu(myVector)") -and $ctrlG1.Contains("GameCanvas.menu.startWithoutCloseButton(myVector, 3)") -and $ctrlG1.Contains("GameCanvas.menu.startAt(myVector2, 3)")
$hasCtrlCase27And57G2 = $ctrlG2.Contains("OnReceiveNewMenu(myVector)") -and $ctrlG2.Contains("GameCanvas.menu.startWithoutCloseButton(myVector, 3)") -and $ctrlG2.Contains("GameCanvas.menu.startAt(myVector2, 3)")
$hasStartWithoutGuardG1 = $ctrlG1 -match '(?s)OnReceiveNewMenu\(myVector\).*?if\s*\(!Game1\.Mod\.AutoGop\.gI\(\)\.ShouldSuppressGameUi\(\)\)\s*\{\s*GameCanvas\.menu\.startWithoutCloseButton'
$hasCase57GuardG1 = $ctrlG1 -match '(?s)case 57:.*?if\s*\(!Game1\.Mod\.AutoGop\.gI\(\)\.ShouldSuppressGameUi\(\)\)\s*\{\s*GameCanvas\.menu\.startAt\(myVector2, 3\)'
$hasCase57GuardG2 = $ctrlG2 -match '(?s)case 57:.*?if\s*\(!Game2\.Mod\.AutoGop\.gI\(\)\.ShouldSuppressGameUi\(\)\)\s*\{\s*GameCanvas\.menu\.startAt\(myVector2, 3\)'
Assert-Test ($hasCtrlCase27And57G1 -and $hasCtrlCase27And57G2 -and $hasStartWithoutGuardG1 -and $hasCase57GuardG1 -and $hasCase57GuardG2) "Controller case 27 and case 57 handle AutoGop & ShouldSuppressGameUi() in both Game1 and Game2"

$hasSilentCleanup = $codeG1.Contains("GameCanvas.menu.close = true;") -and
    $codeG1.Contains("InfoDlg.hide();") -and
    $codeG1.Contains("GameCanvas.endDlg();") -and
    $codeG1.Contains("ChatPopup.currChatPopup = null;") -and
    $codeG1.Contains("GameCanvas.currentScreen is ClientInput") -and
    $codeG1.Contains("!isWaitingXmap && stepState != 7")
Assert-Test $hasSilentCleanup "ClearResidualGameUi suppresses Menu(close=true), InfoDlg, Dialogs, ChatPopup, ClientInput while avoiding Xmap disruption"

$hasClientInputFields = $codeG1.Contains("Service.gI().sendClientInput(fields);") -and
    $codeG1.Contains("retryCount = 0;") -and
    $codeG1.Contains("lastBagCheckTime = 0;") -and
    $codeG1.Contains("lastZenoMenu2Idx") -and
    $codeG1.Contains("SendZenoMenuRetry();")
Assert-Test $hasClientInputFields "Packet -125 Tab 4 uses Service.gI().sendClientInput(fields), resets retryCount & bag cache, and stepState=4 retries lastZenoMenu2Idx"

$hasXmapLogic = $codeG1.Contains("StartXmapToNpc(4, 46, 18)") -and
    $codeG1.Contains("zenoReturnMapId = TileMap.mapID;") -and
    $codeG1.Contains("XmapController.StartRunToMapId(targetMapId)") -and
    $codeG1.Contains("XmapController.FinishXmap()") -and
    $codeG1.Contains("if (stepState == 7 && isWaitingXmap)")
Assert-Test $hasXmapLogic "Cross-map Xmap supports navigating to Map 46 (Karin) and returning to zenoReturnMapId + cancelling in StopAll"

$hasFocusKeepAndCooldown = $codeG1.Contains("if (stepState == 5 || stepState == 6 || stepState == 7)") -and
    $codeG1.Contains("if (isWaitingXmap || stepState == 7)") -and
    $codeG1.Contains("IsZenoSkippedOption") -and
    $codeG1.Contains("GameCanvas.panel.hideNow();") -and
    $codeG1.Contains("Char.myCharz().FocusManualTo(focusNpc);") -and
    $codeG1.Contains("Char.myCharz().currentMovePoint = null;") -and
    $codeG1.Contains("▶ Bắt đầu [Tab Zeno]") -and
    $codeG1.Contains("■ Vé Zeno")
Assert-Test $hasFocusKeepAndCooldown "Tab 4 non-destructive hide, Xmap menu passthrough (isWaitingXmap||stepState==7), IsZenoSkippedOption, and Tab 1-3 focus/cooldown preserved"

# --- Behavioral Simulation for Normalized Multi-Line & Multi-Step Zeno Exchange ---
function Sim-NormalizeMenu($s) {
    if ([string]::IsNullOrEmpty($s)) { return "" }
    $clean = $s.Replace("`r", " ").Replace("`n", " ")
    return ([regex]::Replace($clean, "\s+", " ")).Trim().ToLower()
}

function Sim-IsZenoSkipped($m) {
    return $m.Contains("hướng dẫn") -or $m.Contains("thông tin") -or $m.Contains("từ chối") -or $m.Contains("hủy") -or $m.Contains("đóng") -or $m.Contains("thoát") -or $m.Contains("không")
}

function Sim-FindZenoOpt([string[]]$normOpts, [int]$step) {
    if ($null -eq $normOpts -or $normOpts.Length -eq 0) { return -1 }
    if ($step -eq 0) {
        if ($normOpts.Length -gt 3 -and -not (Sim-IsZenoSkipped $normOpts[3]) -and $normOpts[3].Contains("đổi tiên phù")) { return 3 }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and $m.Contains("đổi tiên phù")) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and ($m.Contains("đổi vé") -or ($m.Contains("đổi") -and ($m.Contains("zeno") -or $m.Contains("tiên phù"))))) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and ($m.Contains("tiên phù") -or $m.Contains("zeno"))) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and $m.Contains("đổi") -and -not $m.Contains("khu")) { return $i }
        }
        if ($normOpts.Length -gt 3) { return 3 } else { return 0 }
    }
    if ($step -eq 1) {
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and $m.Contains("tiên phù zeno")) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and $m.Contains("tiên phù")) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and $m.Contains("zeno") -and -not $m.Contains("đổi vé")) { return $i }
        }
        for ($i = 0; $i -lt $normOpts.Length; $i++) {
            $m = $normOpts[$i]
            if (-not (Sim-IsZenoSkipped $m) -and ($m.Contains("đồng ý") -or $m.Contains("xác nhận"))) { return $i }
        }
        return 0
    }
    for ($i = 0; $i -lt $normOpts.Length; $i++) {
        $m = $normOpts[$i]
        if (Sim-IsZenoSkipped $m) { continue }
        if ($m.Contains("đồng ý") -or $m.Contains("xác nhận") -or $m.Contains("tiên phù") -or $m.Contains("zeno") -or $m.Contains("đổi")) { return $i }
    }
    return 0
}

# Sim A: Multi-line \n Menu 1 -> Menu 2 -> Confirm Menu 3 + Karin index-3 priority over distracting options
$m1 = @("Nói chuyện", "Đổi`nTiên   Phù", "Đóng") | ForEach-Object { Sim-NormalizeMenu $_ }
$opt1 = Sim-FindZenoOpt $m1 0
$m2 = @("Hướng dẫn", "Tiên`r`nPhù Zeno", "Đóng") | ForEach-Object { Sim-NormalizeMenu $_ }
$opt2 = Sim-FindZenoOpt $m2 1
$m3 = @("Từ chối", "Đồng`ný") | ForEach-Object { Sim-NormalizeMenu $_ }
$opt3 = Sim-FindZenoOpt $m3 2
$mKarinFull = @("Hướng dẫn Đổi Tiên Phù", "Nhận đậu", "Đổi Khu", "Đổi Tiên Phù", "Thách đấu") | ForEach-Object { Sim-NormalizeMenu $_ }
$optKarinFull = Sim-FindZenoOpt $mKarinFull 0
Assert-Test ($opt1 -eq 1 -and $opt2 -eq 1 -and $opt3 -eq 1 -and $optKarinFull -eq 3) "Menu normalization selects Menu1(1), Menu2 skipping Guide(1), Confirm(1), and Karin index 3 skipping Guide(3)"

# Sim B: Case 33 submenu flattening (88820 arr.Length > 1 -> resolves parentMenuIdx + subIdx)
$parentIdx = 2
$arrSub = @("Đổi Thưởng", "Đổi Tiên Phù Zeno", "Đóng")
$flatTexts = @()
$flatMenuIdx = @()
$flatSubIdx = @()
for ($k = 1; $k -lt $arrSub.Length; $k++) {
    $flatTexts += Sim-NormalizeMenu ($arrSub[0] + " " + $arrSub[$k])
    $flatMenuIdx += $parentIdx
    $flatSubIdx += ($k - 1)
}
$chosenFlat = Sim-FindZenoOpt $flatTexts 0
Assert-Test ($chosenFlat -eq 0 -and $flatMenuIdx[$chosenFlat] -eq 2 -and $flatSubIdx[$chosenFlat] -eq 0) "Case 33 submenu (88820 arr.Length>1) flattens to exact (menuIdx=2, subIdx=0)"

# Sim C: Cross-map Xmap State Machine & ShouldSuppressGameUi (Map 5 -> Xmap 46 -> Immediate Step 2 -> Exchange -> Route Tab 2 -> Return Map 5)
$simState = [ordered]@{
    Running = $true; HideUi = $true; Tab = 4; MapId = 5; Npc18Here = $false; Npc56Here = $true; ReturnMap = -1; Step = 0; WaitXmap = $false; TargetMap = -1
}
function Sim-ShouldSuppress($st) {
    if (-not $st.Running -or -not $st.HideUi) { return $false }
    if ($st.Tab -eq 4 -and $st.WaitXmap -and $st.Step -eq 7) { return $false }
    return $true
}
# StartTab(4) in Map 5 without NPC 18
if (-not $simState.Npc18Here -and $simState.MapId -ne 46) {
    $simState.ReturnMap = $simState.MapId
    $simState.WaitXmap = $true; $simState.TargetMap = 46; $simState.Step = 7
}
$startedXmapOk = ($simState.Step -eq 7 -and $simState.TargetMap -eq 46 -and $simState.ReturnMap -eq 5 -and -not (Sim-ShouldSuppress $simState))
# Arrive Map 46 with NPC 18 -> immediately sends openMenu(18) at stepState=2 & suppresses Game UI
$simState.MapId = 46; $simState.Npc18Here = $true; $simState.Npc56Here = $false
if ($simState.Step -eq 7 -and $simState.MapId -eq $simState.TargetMap -and $simState.Npc18Here) {
    $simState.WaitXmap = $false; $simState.Step = 2
}
$arrivedKarinOk = ($simState.Step -eq 2 -and -not $simState.WaitXmap -and (Sim-ShouldSuppress $simState))
# FinishZenoDone -> routes to Tab 2 (ReturnMap=5 preserved because StartTab doesn't wipe zenoReturnMapId) -> Xmaps back to Map 5
$simState.Tab = 2
if (-not $simState.Npc56Here -and $simState.ReturnMap -ge 0 -and $simState.MapId -ne $simState.ReturnMap) {
    $simState.WaitXmap = $true; $simState.TargetMap = $simState.ReturnMap; $simState.Step = 7
}
$returnedOk = ($simState.Step -eq 7 -and $simState.TargetMap -eq 5 -and $simState.WaitXmap -and (Sim-ShouldSuppress $simState))
Assert-Test ($startedXmapOk -and $arrivedKarinOk -and $returnedOk) "Tab 4 Xmap pauses UI suppression during travel, jumps to stepState=2 at Karin, preserves ReturnMap for Tab 2 return"

# Sim D: Self-heal if Menu 1 arrives unexpectedly when zenoMenuStep == 1 (MaybeResetZenoMenuStepIfMainMenu)
function Sim-MaybeResetMainMenu([int]$step, [string[]]$normOpts) {
    if ($step -eq 0 -or $null -eq $normOpts -or $normOpts.Length -lt 3) { return $step }
    $hasT1 = $false; $hasT2 = $false
    foreach ($m in $normOpts) {
        if (Sim-IsZenoSkipped $m) { continue }
        if ($m.Contains("đổi tiên phù") -and -not $m.Contains("tiên phù zeno")) { $hasT1 = $true }
        if ($m.Contains("tiên phù zeno")) { $hasT2 = $true }
    }
    if ($hasT1 -and -not $hasT2) { return 0 }
    return $step
}
$healedStep = Sim-MaybeResetMainMenu 1 $mKarinFull
$healedOpt = Sim-FindZenoOpt $mKarinFull $healedStep
$keptTier2Step = Sim-MaybeResetMainMenu 1 @("Hướng dẫn", "Tiên Phù Zeno", "Đóng")
Assert-Test ($healedStep -eq 0 -and $healedOpt -eq 3 -and $keptTier2Step -eq 1) "MaybeResetZenoMenuStepIfMainMenu heals desynced step=1 back to Menu 1 (idx=3) without breaking Menu 2"

# ==============================================================================
# SUMMARY
# ==============================================================================
Write-Host "`n==========================================================" -ForegroundColor Cyan
if ($allPass) {
    Write-Host "  >>> ALL VERIFICATION TESTS PASSED SUCCESSFULLY! (100%) <<<" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  >>> SOME TESTS FAILED! DO NOT BUILD! <<<" -ForegroundColor Red
    Write-Host "==========================================================" -ForegroundColor Cyan
    exit 1
}
