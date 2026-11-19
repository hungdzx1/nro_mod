$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$checks = 0

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $script:checks++
}

# 1. Static parity and hook checks for Game1, Game2, MyMod, and do_backup.ps1
$g1Mod = Get-Content -LiteralPath (Join-Path $root 'DecompiledSource/Game1.Mod/AutoPhaLe.cs') -Raw -Encoding UTF8
$g2Mod = Get-Content -LiteralPath (Join-Path $root 'DecompiledSource/Game2.Mod/AutoPhaLe.cs') -Raw -Encoding UTF8
$m1 = Get-Content -LiteralPath (Join-Path $root 'MyMod/AutoPhaLe_Game1.cs') -Raw -Encoding UTF8
$m2 = Get-Content -LiteralPath (Join-Path $root 'MyMod/AutoPhaLe_Game2.cs') -Raw -Encoding UTF8

Assert-True ($g1Mod -ceq $m1) 'MyMod/AutoPhaLe_Game1.cs matches Game1.Mod/AutoPhaLe.cs'
Assert-True ($g2Mod -ceq $m2) 'MyMod/AutoPhaLe_Game2.cs matches Game2.Mod/AutoPhaLe.cs'
Assert-True ($g1Mod.Replace('Game1', 'Game2') -ceq $g2Mod) 'Game1.Mod/AutoPhaLe.cs and Game2.Mod/AutoPhaLe.cs are 100% synced'
Assert-True (([regex]::Matches($g1Mod, '\{')).Count -eq ([regex]::Matches($g1Mod, '\}')).Count) 'Balanced braces in AutoPhaLe.cs'

# Ensure no unsupported bitmap font chars like '★' or '|' are passed to mFont
Assert-True (-not $g1Mod.Contains('★')) 'No unsupported star glyph in AutoPhaLe.cs'
Assert-True (-not [regex]::IsMatch($g1Mod, 'drawString(Border)?\([^\n;]*\|')) 'No unsupported pipe glyph in mFont drawString calls'

$backupScript = Get-Content -LiteralPath (Join-Path $root 'MyMod/do_backup.ps1') -Raw -Encoding UTF8
Assert-True ($backupScript.Contains("'Game1.Mod\AutoPhaLe.cs'") -and $backupScript.Contains("'Game2.Mod\AutoPhaLe.cs'")) 'do_backup.ps1 includes Game1 and Game2 AutoPhaLe.cs'

foreach ($game in @('Game1', 'Game2')) {
    $autoSrc = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game.Mod/AutoPhaLe.cs") -Raw -Encoding UTF8
    Assert-True ($autoSrc.Contains("namespace $game.Mod;")) "$game namespace"
    Assert-True ($autoSrc.Contains("`"$game.AutoPhaLe`"")) "$game AutoSettingsStore ID"
    Assert-True ($autoSrc.Contains('public int upgradeBatchMode = 100;')) "$game default batch mode is 100"
    Assert-True ($autoSrc.Contains('public int selectedTab = 0;') -and $autoSrc.Contains('public int generalBatchMode = 100;') -and $autoSrc.Contains('public int companionMaterialId = -1;')) "$game Tab 2 fields exist with defaults"
    Assert-True ($autoSrc.Contains('public bool allowAddFromBag = true;')) "$game allowAddFromBag defaults true"
    Assert-True ($autoSrc.Contains('public static bool CanAddItem(Item item)') -and -not $autoSrc.Contains('isTypeBody()')) "$game CanAddItem accepts non-isTypeBody items like Lenh Bai Tap Dich"
    Assert-True ($autoSrc.Contains('public int pickerMode;') -and $autoSrc.Contains('RefreshBagPickerList()')) "$game supports in-panel bag item & material picker"
    Assert-True ($autoSrc.Contains('ImportCombineSnapshotToTab(selectedTab);')) "$game imports open combine items into active tab queue on OpenWindow"
    Assert-True ($autoSrc.Contains('public static bool IsMaxLevelReachedText(string text)')) "$game defines IsMaxLevelReachedText helper"
    Assert-True ($autoSrc.Contains('public static int SelectFirstConfirmMenuIndex(string[] menu)') -and $autoSrc.Contains('public static bool IsMainNpcMenu(string[] menu)')) "$game defines SelectFirstConfirmMenuIndex and IsMainNpcMenu"
    Assert-True (-not $autoSrc.Contains('Sound.start')) "$game has no Sound.start calls (audio disabled)"
    Assert-True ($autoSrc.Contains('private bool isAddingInternalInfo;') -and $autoSrc.Contains('private void NotifyInfo(string text)')) "$game guards InfoMe recursion via isAddingInternalInfo/NotifyInfo"
    Assert-True ($autoSrc.Contains('public bool OnNpcSay(int npcId, string sayText)')) "$game defines OnNpcSay handler"
    Assert-True ($autoSrc.Contains('private void HideCombinePanelsIfNeeded()')) "$game defines HideCombinePanelsIfNeeded"
    $hideStart = $autoSrc.IndexOf('private void HideCombinePanelsIfNeeded()')
    $hideSlice = if ($hideStart -ge 0) { $autoSrc.Substring($hideStart, [Math]::Min(350, $autoSrc.Length - $hideStart)) } else { '' }
    Assert-True (-not $hideSlice.Contains('hideNow()')) "$game HideCombinePanelsIfNeeded hides lightly without hideNow/mSystem.gcc"
    Assert-True (-not $autoSrc.Contains('if (tab == 1 && i == 1 && companionMaterialId == -1)')) "$game Tab 2 combine import never falls through slot 2 material into generalQueue"
    Assert-True ($autoSrc.Contains('if (tab == 1 && IsCompanionMaterial(item))')) "$game Tab 2 ToggleItemInTab blocks adding companion material to equipment queue"
    Assert-True ($autoSrc.Contains('public bool hideUi = true;')) "$game hideUi defaults true"
    Assert-True ($autoSrc.Contains('public bool showOldHud = false;')) "$game showOldHud defaults false"
    Assert-True ($autoSrc.Contains('Input.GetKeyDown(KeyCode.F6)')) "$game F6 stop hook"
    Assert-True ($autoSrc.Contains('ModFunc.GI().isAutoPhaLe = false;')) "$game disables old auto when new starts"
    Assert-True ($autoSrc.Contains('goldStallCount >= 2')) "$game protects against server gold cap"
    Assert-True (-not $autoSrc.Contains('luong + Char.myCharz().luongKhoa')) "$game does not sum int luong + luongKhoa (avoids 32-bit overflow)"
    Assert-True ($autoSrc.Contains('NinjaUtil.getMoneys(Char.myCharz().luong)') -and $autoSrc.Contains('NinjaUtil.getMoneys(Char.myCharz().luongKhoa)')) "$game formats luong and luongKhoa individually like original AutoPhaLe"

    $modFunc = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/ModFunc.cs") -Raw -Encoding UTF8
    Assert-True ($modFunc.Contains('public void AutoPhaLe()') -and $modFunc.Contains('Service.gI().confirmMenu(21, 0);')) "$game preserves original ModFunc.AutoPhaLe intact"
    Assert-True ($modFunc.Contains("case 105:`r`n`t`t`t$game.Mod.AutoPhaLe.gI().ToggleWindow();") -or $modFunc.Contains("case 105:`n`t`t`t$game.Mod.AutoPhaLe.gI().ToggleWindow();")) "$game key I (105) opens AutoPhaLe panel"
    Assert-True ($modFunc.Contains("new Command(`"Pha lê nhanh`", $game.Mod.AutoPhaLe.gI(), 100, null)")) "$game key Z menu includes Pha lê nhanh"
    Assert-True ($modFunc.Contains("$game.Mod.AutoPhaLe.gI().Update();")) "$game Update hook"
    Assert-True ($modFunc.Contains("$game.Mod.AutoPhaLe.gI().Paint(g);")) "$game Paint hook"
    Assert-True ($modFunc.Contains("$game.Mod.AutoPhaLe.gI().ShouldShowOldHud(isAutoPhaLe)")) "$game old HUD gated by ShouldShowOldHud"

    $panel = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/Panel.cs") -Raw -Encoding UTF8
    Assert-True ($panel.Contains("$game.Mod.AutoPhaLe.gI().allowAddFromBag") -and $panel.Contains("$game.Mod.AutoPhaLe.CanAddItem(currItem)")) "$game Panel checks allowAddFromBag and CanAddItem"
    Assert-True ($panel.Contains("isBagSlotPhaLe && GameCanvas.panel.type != 13 && $game.Mod.AutoPhaLe.CanAddItem(currItem)")) "$game Panel supports both panel and panel2 (combine inventory)"
    Assert-True ($panel.Contains("$game.Mod.AutoPhaLe.gI().OnCombineSlotItemAdded(item2, GameCanvas.panel.vItemCombine.size());")) "$game Panel auto-adds item on combine slot action 6000"
    Assert-True ($panel.Contains("$game.Mod.AutoPhaLe.gI().IsCompanionMaterial(currItem)")) "$game Panel shows companion material command"
    Assert-True ($panel.Contains("$game.Mod.AutoPhaLe.gI().IsItemInQueue(currItem)")) "$game Panel shows add/remove queue button"
    Assert-True ($panel.Contains("$game.Mod.AutoPhaLe.gI().OnOldAutoToggledOn();")) "$game toggling old auto stops new auto"

    $ctrl = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/Controller.cs") -Raw -Encoding UTF8
    Assert-True ($ctrl.Contains("$game.Mod.AutoPhaLe.gI().ShouldSkipCombineEffect()") -and $ctrl.Contains("$game.Mod.AutoPhaLe.gI().OnCombinePacket(b30);")) "$game Controller combine packet hook"
    Assert-True (([regex]::Matches($ctrl, [regex]::Escape("$game.Mod.AutoPhaLe.gI().ShouldSkipCombineEffect()"))).Count -ge 9) "$game Controller suppresses b30 == 0..8 UI and effects"
    Assert-True (([regex]::Matches($ctrl, [regex]::Escape("$game.Mod.AutoPhaLe.gI().OnCombinePacket(b30)"))).Count -ge 7) "$game Controller notifies OnCombinePacket for b30 == 2..8"
    Assert-True ($ctrl.Contains("$game.Mod.AutoPhaLe.gI().OnBagUpdated();")) "$game Controller bag update hook"
    Assert-True ($ctrl.Contains("bool suppressPhaLeUi = $game.Mod.AutoPhaLe.gI().OnNpcMenu(num177, text8, array18);")) "$game Controller NPC menu hook 1"
    Assert-True ($ctrl.Contains("bool suppressPhaLeUi2 = $game.Mod.AutoPhaLe.gI().OnNpcMenu(num177, chat2, array19);")) "$game Controller NPC menu hook 2"
    Assert-True (([regex]::Matches($ctrl, [regex]::Escape("$game.Mod.AutoPhaLe.gI().OnNpcSay(num159, str6)"))).Count -ge 2) "$game Controller hooks case 38 OnNpcSay in both branches"

    $info = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/InfoMe.cs") -Raw -Encoding UTF8
    Assert-True ($info.Contains("$game.Mod.AutoPhaLe.gI().OnServerInfo(s);")) "$game InfoMe OnServerInfo hook"

    $srv = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/ServerListScreen.cs") -Raw -Encoding UTF8
    Assert-True ($srv.Contains("$game.Mod.AutoPhaLe.gI().OnDisconnectOrReset();")) "$game ServerListScreen disconnect reset hook"
}

# 2. Extract SelectUpgradeMenuIndex + helpers from AutoPhaLe.cs and run C# simulation tests
function Get-MethodSource([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing method: $signature" }
    $open = $source.IndexOf('{', $start)
    $depth = 0
    for ($i = $open; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        elseif ($source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($start, $i - $start + 1) }
        }
    }
    throw "Unclosed method: $signature"
}

$mNorm   = Get-MethodSource $g1Mod 'private static string NormalizeMenuText(string raw)'
$mMain   = Get-MethodSource $g1Mod 'public static bool IsMainNpcMenu(string[] menu)'
$mFirst  = Get-MethodSource $g1Mod 'public static int SelectFirstConfirmMenuIndex(string[] menu)'
$mSelect = Get-MethodSource $g1Mod 'public static int SelectUpgradeMenuIndex(string[] menu, int batchMode)'
$mCancel = Get-MethodSource $g1Mod 'private static bool IsCancelOption(string norm)'
$mBatch  = Get-MethodSource $g1Mod 'private static bool ContainsBatchCount(string norm, string countStr)'
$mCanAdd = Get-MethodSource $g1Mod 'public static bool CanAddItem(Item item)'
$mMaxLvl = Get-MethodSource $g1Mod 'public static bool IsMaxLevelReachedText(string text)'

$harness = @"
using System;
using System.Collections.Generic;

public class ItemTemplate { public int id; public int type; public string name; }
public class Item { public ItemTemplate template; }

public static class PhaLeMenuHarness
{
    private const int GoldBarItemId = 457;
$mCanAdd
$mMaxLvl
$mNorm
$mMain
$mFirst
$mSelect
$mCancel
$mBatch
}

public class SimItem { public int indexUI; public int id; public string name; public int stars; }
public class SimEntry { public int indexUI; public int itemId; public string itemName; public int currStar; public int targetStar; public bool isDone; public bool isMissing; public bool maxedByServer; }

public class SimStateMachine
{
    public enum State { Idle, CheckAndPrepare, SellGold, SendCombine, WaitMenu, WaitResult }
    public bool isRunning;
    public bool oldAutoRunning;
    public State state = State.Idle;
    public int mapId = 5;
    public long gem = 10000;
    public long xu = 50000000000L;
    public int goldBars = 10;
    public long serverGoldCap = long.MaxValue;
    public int minGoldBillion = 10;
    public int maxGoldBillion = 60;
    public int upgradeBatchMode = 100;
    public bool hideUi = true;
    public bool showOldHud = false;
    public int goldStallCount;
    public bool needSellGoldNow;
    public int activeQueueIndex = -1;
    public int totalCombineSent;
    public int totalSuccess;
    public int totalBarsSold;
    public string lastStopReason = "";
    public List<SimEntry> queue = new List<SimEntry>();
    public List<SimItem> bag = new List<SimItem>();

    public bool ShouldShowOldHud(bool oldActive) { return showOldHud && oldActive; }
    public bool ShouldSkipCombineEffect() { return isRunning && hideUi; }
    public void OnOldAutoToggledOn() { if (isRunning) StopAuto("stopped by old"); }

    public void StartAuto()
    {
        if (oldAutoRunning) oldAutoRunning = false;
        if (mapId != 5) { StopAuto("need kame"); return; }
        if (queue.Count == 0) { StopAuto("empty queue"); return; }
        SyncBag();
        if (FindNextPending() < 0) { StopAuto("all done"); return; }
        totalCombineSent = 0; totalSuccess = 0; totalBarsSold = 0;
        goldStallCount = 0; needSellGoldNow = false;
        isRunning = true; state = State.CheckAndPrepare;
    }

    public void StopAuto(string reason)
    {
        isRunning = false; state = State.Idle; activeQueueIndex = -1; lastStopReason = reason;
    }

    public void SyncBag()
    {
        foreach (var e in queue)
        {
            SimItem found = bag.Find(x => x.indexUI == e.indexUI && x.id == e.itemId);
            if (found == null) { e.isMissing = true; continue; }
            e.isMissing = false;
            e.currStar = found.stars;
            if (e.maxedByServer || e.currStar >= e.targetStar) e.isDone = true;
            else e.isDone = false;
        }
    }

    private int FindNextPending()
    {
        for (int i = 0; i < queue.Count; i++) if (!queue[i].isDone && !queue[i].isMissing) return i;
        return -1;
    }

    public void Step()
    {
        if (!isRunning) return;
        if (mapId != 5) { StopAuto("need kame"); return; }
        if (state == State.CheckAndPrepare)
        {
            SyncBag();
            int next = FindNextPending();
            if (next < 0) { StopAuto("all completed"); return; }
            activeQueueIndex = next;
            long minGold = (long)minGoldBillion * 1000000000L;
            if ((xu < minGold || needSellGoldNow) && goldStallCount < 2)
            {
                if (goldBars > 0) { state = State.SellGold; return; }
                if (xu <= 0 || needSellGoldNow) { StopAuto("out of gold bars"); return; }
            }
            state = State.SendCombine;
        }
        else if (state == State.SellGold)
        {
            long targetGold = (long)Math.Max(minGoldBillion, maxGoldBillion) * 1000000000L;
            if (xu >= targetGold) { needSellGoldNow = false; state = State.CheckAndPrepare; return; }
            if (goldBars <= 0)
            {
                if (needSellGoldNow || xu <= 0) { StopAuto("out of gold bars"); return; }
                needSellGoldNow = false; state = State.CheckAndPrepare; return;
            }
            long before = xu;
            goldBars--; totalBarsSold++;
            xu = Math.Min(serverGoldCap, xu + 500000000L);
            if (xu > before) goldStallCount = 0;
            else
            {
                goldStallCount++;
                if (goldStallCount >= 2)
                {
                    needSellGoldNow = false;
                    if (xu <= 0) { StopAuto("gold stall"); return; }
                    state = State.CheckAndPrepare;
                }
            }
        }
        else if (state == State.SendCombine)
        {
            state = State.WaitMenu;
        }
    }

    public bool OnNpcMenu(int npcId, string chat, string[] menu)
    {
        if (!isRunning || npcId != 21) return false;
        if (PhaLeMenuHarness.IsMainNpcMenu(menu)) { StopAuto("main menu"); return false; }
        int idx = PhaLeMenuHarness.SelectUpgradeMenuIndex(menu, upgradeBatchMode);
        if (idx < 0)
        {
            if (PhaLeMenuHarness.IsMaxLevelReachedText(chat))
            {
                queue[activeQueueIndex].maxedByServer = true;
                queue[activeQueueIndex].isDone = true;
                state = State.CheckAndPrepare;
                return hideUi;
            }
            StopAuto("no upgrade menu");
            return false;
        }
        totalCombineSent++;
        state = State.WaitResult;
        return hideUi;
    }

    public void OnCombineResult(bool success, int newStars)
    {
        if (success) totalSuccess++;
        if (activeQueueIndex >= 0 && activeQueueIndex < queue.Count)
        {
            var e = queue[activeQueueIndex];
            var item = bag.Find(x => x.indexUI == e.indexUI && x.id == e.itemId);
            if (item != null) item.stars = newStars;
        }
        SyncBag();
        state = State.CheckAndPrepare;
    }

    public void OnServerInfo(string s)
    {
        if (!isRunning || string.IsNullOrEmpty(s)) return;
        string lower = s.ToLower();
        if (lower.Contains("không đủ vàng"))
        {
            long maxGold = (long)Math.Max(minGoldBillion, maxGoldBillion) * 1000000000L;
            if (xu < maxGold && goldBars > 0 && goldStallCount < 2)
            {
                needSellGoldNow = true;
                state = State.SellGold;
            }
            else StopAuto("insufficient gold");
        }
        else if (lower.Contains("không đủ ngọc"))
        {
            StopAuto("out of gem");
        }
    }
}

public class SimTab2
{
    public int companionMaterialId = -1;
    public int activeIndex = 0;
    public List<SimEntry> generalQueue = new List<SimEntry>();
    public List<SimItem> bag = new List<SimItem>();
    public bool isRunning = true;
    public string stopReason = "";
    public List<int> lastCombinedIds = new List<int>();

    public bool isAddingInternalInfo;
    public int infoCallbackDepth;
    public int notifyCount;

    public void NotifyInfo(string text)
    {
        bool prev = isAddingInternalInfo;
        isAddingInternalInfo = true;
        try
        {
            notifyCount++;
            // Simulate InfoMe.addInfo forwarding back into OnServerInfo
            OnServerInfo(text);
        }
        finally
        {
            isAddingInternalInfo = prev;
        }
    }

    public void OnServerInfo(string s)
    {
        if (isAddingInternalInfo || !isRunning || string.IsNullOrEmpty(s)) return;
        infoCallbackDepth++;
        if (infoCallbackDepth > 5) throw new InvalidOperationException("Recursion detected!");
        if (PhaLeMenuHarness.IsMaxLevelReachedText(s))
        {
            generalQueue[activeIndex].isDone = true;
            NotifyInfo("Đã đạt tối đa: " + generalQueue[activeIndex].itemName);
            return;
        }
    }

    public void ImportCombine(List<SimItem> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var it = slots[i];
            if (i >= 1)
            {
                if (companionMaterialId == -1) companionMaterialId = it.id;
                continue;
            }
            if (companionMaterialId >= 0 && it.id == companionMaterialId) continue;
            if (!generalQueue.Exists(x => x.indexUI == it.indexUI && x.itemId == it.id))
            {
                generalQueue.Add(new SimEntry { indexUI = it.indexUI, itemId = it.id, itemName = it.name });
            }
        }
    }

    public void SetCompanionMaterial(int itemId)
    {
        companionMaterialId = itemId;
        generalQueue.RemoveAll(x => x.itemId == itemId);
    }

    public int RunMenuStep(string serverChat, string[] menu)
    {
        if (!isRunning) return -1;
        while (activeIndex < generalQueue.Count && generalQueue[activeIndex].isDone) activeIndex++;
        if (activeIndex >= generalQueue.Count) { isRunning = false; stopReason = "all done"; return -1; }
        var eq = bag.Find(x => x.indexUI == generalQueue[activeIndex].indexUI && x.id == generalQueue[activeIndex].itemId);
        if (eq == null) { isRunning = false; stopReason = "missing eq"; return -1; }
        lastCombinedIds.Clear();
        lastCombinedIds.Add(eq.id);
        if (companionMaterialId >= 0)
        {
            var mat = bag.Find(x => x.id == companionMaterialId && x.indexUI != eq.indexUI);
            if (mat == null) { isRunning = false; stopReason = "out of material"; return -1; }
            lastCombinedIds.Add(mat.id);
        }
        if (PhaLeMenuHarness.IsMainNpcMenu(menu)) { isRunning = false; stopReason = "main menu"; return -1; }
        int idx = PhaLeMenuHarness.SelectFirstConfirmMenuIndex(menu);
        if (idx < 0 && PhaLeMenuHarness.IsMaxLevelReachedText(serverChat))
        {
            generalQueue[activeIndex].isDone = true;
            NotifyInfo("Đã đạt tối đa: " + eq.name);
        }
        return idx;
    }

    public void RunStep(string serverChat)
    {
        RunMenuStep(serverChat, new string[] { "Từ chối" });
    }
}
"@

Add-Type -TypeDefinition $harness -ErrorAction Stop

# Test SelectUpgradeMenuIndex (100 / 10 / 1 / fallbacks / anti-substring collision / cancel safety)
$menu3 = @("Nâng cấp`n1 lần`n(50 ngọc)", "Nâng cấp`n10 lần`n(500 ngọc)", "Nâng cấp`n100 lần`n(5000 ngọc)")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menu3, 100) -eq 2) '100 mode picks index 2'
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menu3, 10) -eq 1) '10 mode picks index 1'
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menu3, 1) -eq 0) '1 mode picks index 0'

$menuNo100 = @("Nâng cấp`n1 lần", "Nâng cấp`n10 lần", "Từ chối")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menuNo100, 100) -eq 1) '100 mode falls back to 10 lan when 100 lan absent (never picks Tu choi)'

$menuSingle = @("Nâng cấp`n200 Tr vàng", "Từ chối")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menuSingle, 100) -eq 0) '100 mode falls back to single upgrade option without hitting Tu choi'

$menuPrefixCancel = @("Từ chối", "Nâng cấp 1 lần", "Nâng cấp 10 lần", "Nâng cấp 100 lần")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menuPrefixCancel, 100) -eq 3) '100 mode finds 100 lan regardless of position'

$menu21vs1 = @("Nâng cấp 21 lần", "Nâng cấp 1 lần")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menu21vs1, 1) -eq 1) '1 mode does not falsely match 21 lan'

$menuCancelOnly = @("Từ chối", "Đóng")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($menuCancelOnly, 100) -eq -1) 'cancel-only menu returns -1'

# Test State Machine: Queue 2 items + gold selling + server gold cap protection + maxed by server + mutual exclusion
$sim = [SimStateMachine]::new()
$sim.oldAutoRunning = $true
$sim.bag.Add([SimItem]@{ indexUI = 1; id = 100; name = 'Áo'; stars = 5 })
$sim.bag.Add([SimItem]@{ indexUI = 2; id = 200; name = 'Quần'; stars = 6 })
$sim.queue.Add([SimEntry]@{ indexUI = 1; itemId = 100; itemName = 'Áo'; currStar = 5; targetStar = 7 })
$sim.queue.Add([SimEntry]@{ indexUI = 2; itemId = 200; itemName = 'Quần'; currStar = 6; targetStar = 8 })
$sim.xu = 5000000000L # 5B < 10B -> must sell gold bars
$sim.goldBars = 15
$sim.maxGoldBillion = 12 # sell up to 12B (14 bars of 0.5B)
$sim.StartAuto()
Assert-True (-not $sim.oldAutoRunning) 'StartAuto disables old AutoPhaLe'
Assert-True ($sim.isRunning -and $sim.state -eq [SimStateMachine+State]::CheckAndPrepare) 'StartAuto enters CheckAndPrepare'

# Step -> SellGold -> sell until >= 12B -> CheckAndPrepare -> SendCombine -> WaitMenu
for ($i = 0; $i -lt 25 -and $sim.state -ne [SimStateMachine+State]::WaitMenu; $i++) { $sim.Step() }
Assert-True ($sim.xu -eq 12000000000L -and $sim.totalBarsSold -eq 14) 'Sold exact 14 bars to reach 12B target'
Assert-True ($sim.state -eq [SimStateMachine+State]::WaitMenu -and $sim.activeQueueIndex -eq 0) 'Proceeded to WaitMenu on first item (Áo)'

$suppressed = $sim.OnNpcMenu(21, 'Tỉ lệ 5%', $menu3)
Assert-True ($suppressed -and $sim.state -eq [SimStateMachine+State]::WaitResult -and $sim.totalCombineSent -eq 1) 'OnNpcMenu suppressed UI and moved to WaitResult'

# Simulate hitting 7 stars on Áo -> moves to Quần
$sim.OnCombineResult($true, 7)
Assert-True ($sim.queue[0].isDone -and $sim.queue.Count -eq 2) 'Finished item stays in queue with isDone=true'
$sim.Step()
Assert-True ($sim.activeQueueIndex -eq 1 -and $sim.state -eq [SimStateMachine+State]::SendCombine) 'Moved to second item (Quần)'
$sim.Step()
Assert-True ($sim.state -eq [SimStateMachine+State]::WaitMenu) 'Sent combine for Quần'

# Simulate NPC saying "tối đa" on Quần -> marks done -> all completed -> stops cleanly
$sim.OnNpcMenu(21, 'Trang bị đã đạt số sao pha lê tối đa', $menuCancelOnly)
$sim.Step()
Assert-True ($sim.queue[1].isDone -and -not $sim.isRunning -and $sim.lastStopReason -eq 'all completed') 'Stops cleanly after all queue items done or maxed'

# Test server gold cap protection (serverGoldCap = 8B, minGold = 10B, maxGold = 60B, 20 bars)
$sim2 = [SimStateMachine]::new()
$sim2.bag.Add([SimItem]@{ indexUI = 1; id = 100; name = 'Găng'; stars = 0 })
$sim2.queue.Add([SimEntry]@{ indexUI = 1; itemId = 100; itemName = 'Găng'; currStar = 0; targetStar = 7 })
$sim2.xu = 7500000000L
$sim2.serverGoldCap = 8000000000L
$sim2.goldBars = 20
$sim2.StartAuto()
for ($i = 0; $i -lt 15 -and $sim2.state -ne [SimStateMachine+State]::WaitMenu; $i++) { $sim2.Step() }
Assert-True ($sim2.goldBars -eq 17 -and $sim2.state -eq [SimStateMachine+State]::WaitMenu) 'Gold cap stops selling after 2 stall checks (17/20 bars preserved) and continues combining'

# Test mutual exclusion reverse + old HUD visibility
$sim2.OnOldAutoToggledOn()
Assert-True (-not $sim2.isRunning -and $sim2.lastStopReason -eq 'stopped by old') 'OnOldAutoToggledOn stops new AutoPhaLe'
Assert-True (-not $sim2.ShouldShowOldHud($true)) 'Old HUD hidden by default even when old auto runs'
$sim2.showOldHud = $true
Assert-True ($sim2.ShouldShowOldHud($true)) 'Old HUD shown when showOldHud toggled ON'

# Test CanAddItem for custom non-isTypeBody items like Lệnh Bài Tạp Dịch and exclude gold bar 457
$lenhBai = [Item]@{ template = [ItemTemplate]@{ id = 1500; type = 27; name = 'Lệnh Bài Tạp Dịch' } }
$goldBar = [Item]@{ template = [ItemTemplate]@{ id = 457; type = 27; name = 'Thỏi vàng' } }
Assert-True ([PhaLeMenuHarness]::CanAddItem($lenhBai)) 'CanAddItem accepts Lenh Bai Tap Dich (non-isTypeBody)'
Assert-True (-not [PhaLeMenuHarness]::CanAddItem($goldBar)) 'CanAddItem rejects Gold Bar 457'
Assert-True (-not [PhaLeMenuHarness]::CanAddItem($null)) 'CanAddItem rejects null'

# Test overflowed int gem value (> 2.14B causing negative int in Char.luongKhoa) does NOT false-stop, only server info stops
$sim3 = [SimStateMachine]::new()
$sim3.gem = -1174967296 # 3.12B overflowed as signed 32-bit int
$sim3.bag.Add([SimItem]@{ indexUI = 1; id = 1500; name = 'Lệnh Bài'; stars = 18 })
$sim3.queue.Add([SimEntry]@{ indexUI = 1; itemId = 1500; itemName = 'Lệnh Bài'; currStar = 18; targetStar = 20 })
$sim3.StartAuto()
$sim3.Step()
$sim3.Step()
Assert-True ($sim3.isRunning -and $sim3.state -eq [SimStateMachine+State]::WaitMenu) 'Overflowed negative int gem count does not cause false out-of-gem stop'
$sim3.OnServerInfo('Bạn không đủ ngọc để nâng cấp')
Assert-True (-not $sim3.isRunning -and $sim3.lastStopReason -eq 'out of gem') 'Stops cleanly when server reports insufficient gems'

# Test IsMaxLevelReachedText and Tab 2 single-button / first-button menu selection
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Trang bị đã đạt cấp tối đa')) 'IsMaxLevelReachedText matches dat cap toi da'
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Trang bị đã đạt số sao pha lê tối đa')) 'IsMaxLevelReachedText matches sao pha le toi da'
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Vật phẩm đã đạt cấp giới hạn')) 'IsMaxLevelReachedText matches da dat cap'
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Trang bị không thể nâng cấp thêm')) 'IsMaxLevelReachedText matches khong the nang cap'
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Đã pha lê hóa tối đa')) 'IsMaxLevelReachedText matches hoa toi da'
Assert-True ([PhaLeMenuHarness]::IsMaxLevelReachedText('Vật phẩm đã đạt giới hạn')) 'IsMaxLevelReachedText matches dat gioi han'
Assert-True (-not [PhaLeMenuHarness]::IsMaxLevelReachedText('Nâng cấp thất bại!')) 'IsMaxLevelReachedText ignores failure text'
Assert-True (-not [PhaLeMenuHarness]::IsMaxLevelReachedText("Set Nguyên Anh`nLinh Khí Tối Đa +3300`nLinh Khí +3300/30s`n30 Sao Pha Lê (Chưa ép)`nĐã pháp sư hóa 500 lần`nĐã bóng tối hóa 200 lần")) 'IsMaxLevelReachedText does NOT false-match Linh Khi Toi Da stat on Nguyen Anh'
Assert-True (-not [PhaLeMenuHarness]::IsMaxLevelReachedText('HP tối đa +10%, KI tối đa +20%, Sát thương tối đa +500')) 'IsMaxLevelReachedText does NOT false-match HP/KI/ST toi da item stats'

$confirmMenu = @("Làm phép", "Từ chối")
Assert-True ([PhaLeMenuHarness]::SelectFirstConfirmMenuIndex($confirmMenu) -eq 0) 'Tab 2 SelectFirstConfirmMenuIndex selects index 0 (Lam phep)'
$multiOptionMenu = @("Nâng cấp`n1 lần", "Nâng cấp`n100 lần", "Đóng")
Assert-True ([PhaLeMenuHarness]::SelectFirstConfirmMenuIndex($multiOptionMenu) -eq 0) 'Tab 2 SelectFirstConfirmMenuIndex always picks first button even when 100 lan present'
Assert-True ([PhaLeMenuHarness]::SelectFirstConfirmMenuIndex(@("Từ chối", "Đóng")) -eq -1) 'Tab 2 SelectFirstConfirmMenuIndex rejects cancel-only menu'

$mainMenu = @("Ép sao trang bị", "Pha lê hóa trang bị", "Tinh ấn trang bị", "Ánh Sáng hoá trang bị")
Assert-True ([PhaLeMenuHarness]::IsMainNpcMenu($mainMenu)) 'IsMainNpcMenu detects Ba Hat Mit main menu'
Assert-True ([PhaLeMenuHarness]::SelectFirstConfirmMenuIndex($mainMenu) -eq -1) 'SelectFirstConfirmMenuIndex refuses main NPC menu'
Assert-True (-not [PhaLeMenuHarness]::IsMainNpcMenu($confirmMenu)) 'IsMainNpcMenu does not flag Lam phep / Tu choi confirm menu'

$singleBtnMenu = @("Nâng cấp`n(Cần 100 Tr vàng)", "Từ chối")
Assert-True ([PhaLeMenuHarness]::SelectUpgradeMenuIndex($singleBtnMenu, 100) -eq 0) 'Tab 1 fallback single-button menu selects index 0'

# Verify Tab 2 regression: Linh Khi Toi Da confirmation, already-set companion material import, and NotifyInfo recursion guard
$t2bug = [SimTab2]::new()
$nguyenAnh = [SimItem]@{ indexUI = 106; id = 3001; name = 'Nguyên Anh Hạ Thường' }
$ngoiSao   = [SimItem]@{ indexUI = 10; id = 3002; name = 'Ngôi sao Ánh Sáng' }
$t2bug.bag.Add($nguyenAnh)
$t2bug.bag.Add($ngoiSao)
$t2bug.SetCompanionMaterial(3002)
$t2bug.ImportCombine(@($nguyenAnh, $ngoiSao))
Assert-True ($t2bug.generalQueue.Count -eq 1 -and $t2bug.generalQueue[0].itemId -eq 3001) 'ImportCombine with preset companionMaterialId never adds slot 2 material into generalQueue'
$pickedBtn = $t2bug.RunMenuStep("Set Nguyên Anh`nLinh Khí Tối Đa +3300`nLinh Khí +3300/30s", $confirmMenu)
Assert-True ($pickedBtn -eq 0 -and -not $t2bug.generalQueue[0].isDone -and $t2bug.isRunning) 'Tab 2 selects Lam phep (0) instead of falsely marking Nguyen Anh maxed on Linh Khi Toi Da'
Assert-True ($t2bug.lastCombinedIds.Count -eq 2 -and $t2bug.lastCombinedIds[0] -eq 3001 -and $t2bug.lastCombinedIds[1] -eq 3002) 'Tab 2 attaches companion material without false out-of-material stop'
$t2bug.OnServerInfo('Trang bị đã đạt cấp tối đa')
Assert-True ($t2bug.generalQueue[0].isDone -and $t2bug.infoCallbackDepth -eq 1 -and $t2bug.notifyCount -eq 1) 'NotifyInfo/isAddingInternalInfo prevents infinite recursion on OnServerInfo'

$t2clean = [SimTab2]::new()
$t2clean.generalQueue.Add([SimEntry]@{ indexUI = 106; itemId = 3001; itemName = 'Nguyên Anh' })
$t2clean.generalQueue.Add([SimEntry]@{ indexUI = 10; itemId = 3002; itemName = 'Ngôi sao' })
$t2clean.SetCompanionMaterial(3002)
Assert-True ($t2clean.generalQueue.Count -eq 1 -and $t2clean.generalQueue[0].itemId -eq 3001) 'SetCompanionMaterial removes matching material from generalQueue'

# Verify Tab 2 simulation with companion material + switching items + stopping when companion material runs out
$t2 = [SimTab2]::new()
$t2.bag.Add([SimItem]@{ indexUI = 1; id = 2001; name = 'Lệnh Bài 1'; stars = 0 })
$t2.bag.Add([SimItem]@{ indexUI = 2; id = 2002; name = 'Lệnh Bài 2'; stars = 0 })
$t2.generalQueue.Add([SimEntry]@{ indexUI = 1; itemId = 2001; itemName = 'Lệnh Bài 1' })
$t2.generalQueue.Add([SimEntry]@{ indexUI = 2; itemId = 2002; itemName = 'Lệnh Bài 2' })
# Case 1: No companion material (1 item combined) -> hits max level -> switches to item 2
$t2.RunStep('Trang bị không thể nâng cấp thêm')
Assert-True ($t2.lastCombinedIds.Count -eq 1 -and $t2.lastCombinedIds[0] -eq 2001 -and $t2.generalQueue[0].isDone) 'Tab 2 combines 1 item when no companion material and marks done on max'
# Case 2: With companion material (2 items combined) -> then material runs out -> stops cleanly
$t2.companionMaterialId = 9001
$matItem = [SimItem]@{ indexUI = 5; id = 9001; name = 'Đá Pháp Sư'; stars = 0 }
$t2.bag.Add($matItem)
$t2.RunStep('Tỉ lệ 50%')
Assert-True ($t2.lastCombinedIds.Count -eq 2 -and $t2.lastCombinedIds[0] -eq 2002 -and $t2.lastCombinedIds[1] -eq 9001) 'Tab 2 switches to item 2 and attaches companion material'
$null = $t2.bag.Remove($matItem)
$t2.RunStep('')
Assert-True (-not $t2.isRunning -and $t2.stopReason -eq 'out of material') 'Tab 2 stops cleanly when companion material runs out'

Write-Output "AutoPhaLeTests PASS: $checks checks passed (0 FAILED)."
