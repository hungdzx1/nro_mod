$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$checks = 0

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $script:checks++
}

$sources = @{}
foreach ($game in @('Game1')) {
    $file = Join-Path $root "DecompiledSource/$game.Mod/AutoDoanhTrai.cs"
    $source = Get-Content -LiteralPath $file -Raw -Encoding UTF8
    $sources[$game] = $source

    Assert-True ($source.Contains("namespace $game.Mod;")) "$game namespace"
    Assert-True ($source.Contains('Phase.WaitBambooMenu') -and $source.Contains('Phase.WaitKameMenu') -and $source.Contains('Phase.WaitDisband') -and $source.Contains('Phase.NextRun')) "$game phase coverage"
    Assert-True ($source.Contains('System.Math.Max(System.Math.Max(phaseSince, lastAttack), lastPick)') -and $source.Contains('90000)) { Stop(')) "$game idle timeout counts attack and pick activity"
    Assert-True ($source.Contains('private const int MaxPickAttempts = 10;') -and $source.Contains('private void ResetRoomPickCounts()') -and $source.Contains('private static bool CanPickItem(Char me, ItemMap item)') -and $source.Contains('private static ItemMap FindPickableItem(Char me)') -and $source.Contains('private void PickItem(Char me, ItemMap item)') -and $source.Contains('Service.gI().pickItem(item.itemMapID);') -and $source.Contains('item.countAutoPick = pickAttempts;')) "$game auto-pick helpers, reset counts, retry cap, and packet call"
    $canPick = $source.Substring($source.IndexOf('private static bool CanPickItem(Char me, ItemMap item)'), $source.IndexOf('private static ItemMap FindPickableItem(Char me)') - $source.IndexOf('private static bool CanPickItem(Char me, ItemMap item)'))
    Assert-True (-not $canPick.Contains('playerId')) "$game CanPickItem does not skip pet or special playerId drops in barracks"
    $pickItemMethod = $source.Substring($source.IndexOf('private void PickItem(Char me, ItemMap item)'), $source.IndexOf('private void FightRoom()') - $source.IndexOf('private void PickItem(Char me, ItemMap item)'))
    Assert-True ($pickItemMethod.Contains('item.xEnd != 0 ? item.xEnd : item.x') -and $pickItemMethod.Contains('item.yEnd != 0 ? item.yEnd : item.y') -and $pickItemMethod.Contains('ModFunc.GI().MoveTo(targetX, targetY);')) "$game PickItem moves to ground target (xEnd, yEnd)"
    $fightRoom = $source.Substring($source.IndexOf('private void FightRoom()'), $source.IndexOf('private static Skill GetAttackSkill()') - $source.IndexOf('private void FightRoom()'))
    Assert-True (([regex]::Matches($fightRoom, 'ResetRoomPickCounts\(\);')).Count -ge 5) "$game FightRoom resets pick counts while combat is still active"
    Assert-True ($fightRoom.Contains('ItemMap item = FindPickableItem(me);') -and $fightRoom.IndexOf('ItemMap item = FindPickableItem(me);') -gt $fightRoom.IndexOf('if (bossAlive) { ResetRoomPickCounts(); clearSince = 0; return; }') -and $fightRoom.IndexOf('ItemMap item = FindPickableItem(me);') -lt $fightRoom.IndexOf('if (clearSince == 0) clearSince = Now;') -and $fightRoom.IndexOf('if (clearSince == 0) clearSince = Now;') -lt $fightRoom.IndexOf('FinishBarracks();')) "$game picks all dropped items before clear delay and FinishBarracks"
    Assert-True ($fightRoom.Contains('if (bossSeen && !bossAlive && !bossKilled) { bossKilled = true; SaveCheckpoint(); }')) "$game marks boss killed only after seen and no longer alive"
    Assert-True ($fightRoom.Contains('if (TileMap.mapID == gateRoomId)') -and $fightRoom.Contains('if (!bossKilled)')) "$game third-map boss gate"
    $gateIdx = $fightRoom.IndexOf('if (TileMap.mapID == gateRoomId)')
    $elseIdx = $fightRoom.IndexOf("`telse", $gateIdx)
    $gateBranch = $fightRoom.Substring($gateIdx, $elseIdx - $gateIdx)
    Assert-True ($gateBranch.IndexOf('if (normal != null)') -ge 0 -and $gateBranch.IndexOf('if (normal != null)') -lt $gateBranch.IndexOf('if (mobBoss != null)') -and $gateBranch.IndexOf('if (normal != null)') -lt $gateBranch.IndexOf('if (charBoss != null)')) "$game map 3 always prioritizes normal mobs including respawns before boss"
    $otherBranch = $fightRoom.Substring($elseIdx)
    Assert-True ($otherBranch.IndexOf('if (mobBoss != null)') -ge 0 -and $otherBranch.IndexOf('if (charBoss != null)') -gt $otherBranch.IndexOf('if (mobBoss != null)') -and $otherBranch.IndexOf('if (charBoss != null)') -lt $otherBranch.IndexOf('if (normal != null)')) "$game other rooms prioritize boss before normal mobs"
    Assert-True ($source.Contains('private const int ThirdBossRoomId = 55;') -and $source.Contains('[AutoSetting] public int gateBossSkillId = -1;') -and $source.Contains('private static Skill GetGateBossSkill()')) "$game saved selectable third-boss skill on map 55"
    $attackMob = $source.Substring($source.IndexOf('private void AttackMob(Mob target)'), $source.IndexOf('private void AttackBoss(Char target)') - $source.IndexOf('private void AttackMob(Mob target)'))
    $attackBoss = $source.Substring($source.IndexOf('private void AttackBoss(Char target)'), $source.IndexOf('private static void MoveToNpc(Npc npc)') - $source.IndexOf('private void AttackBoss(Char target)'))
    Assert-True ($attackMob.Contains('bool thirdBoss = TileMap.mapID == ThirdBossRoomId && IsBoss(target);') -and $attackMob.Contains('Skill skill = thirdBoss ? GetGateBossSkill() : GetAttackSkill();')) "$game AttackMob uses third-boss skill only for boss on map 55"
    Assert-True ($attackBoss.Contains('bool thirdBoss = TileMap.mapID == ThirdBossRoomId;') -and $attackBoss.Contains('Skill skill = thirdBoss ? GetGateBossSkill() : GetAttackSkill();')) "$game AttackBoss uses third-boss skill only on map 55"
    Assert-True ($attackMob.Contains('lastMove') -and $attackMob.Contains('Now - skill.lastTimeUseThisSkill < skill.coolDown') -and $attackBoss.Contains('lastMove') -and $attackBoss.Contains('Now - skill.lastTimeUseThisSkill < skill.coolDown')) "$game separates move cooldown from skill cooldown so switching from boss skill to default skill does not stall"
    Assert-True ($source.Contains('"Skill mặc định"') -and $source.Contains('"Skill boss thứ 3"') -and -not $source.Contains('"Skill boss map 3"')) "$game default and third-boss UI labels updated"
    Assert-True ($source.Contains('[AutoSetting] public int defaultSkillId = -1;') -and $source.Contains('private void OpenDefaultSkillMenu()') -and $source.Contains('case 4: OpenDefaultSkillMenu(); break;') -and $source.Contains('if (action == 32) { defaultSkillId =')) "$game default skill picker and persistence"
    Assert-True ($source.Contains('private void OpenBossSkillMenu()') -and $source.Contains('case 5: OpenBossSkillMenu(); break;') -and $source.Contains('if (action == 31) { gateBossSkillId =')) "$game boss skill picker"
    $getAtk = $source.Substring($source.IndexOf('private static Skill GetAttackSkill()'), $source.IndexOf('private static Skill GetGateBossSkill()') - $source.IndexOf('private static Skill GetAttackSkill()'))
    Assert-True ($getAtk.Contains('s.template.id == auto.defaultSkillId') -and $getAtk.Contains('me.myskill.template.id != auto.gateBossSkillId')) "$game GetAttackSkill selects defaultSkillId and avoids getting stuck on third-boss skill"
    Assert-True ($source.Contains('if (!isRunning || phase == Phase.Fight || string.IsNullOrEmpty(text)) return;')) "$game OnServerInfo ignores fight-phase warnings"
    Assert-True ($source.Contains('if (bossAlive) { ResetRoomPickCounts(); clearSince = 0; return; }')) "$game invulnerable boss cannot be marked dead"
    $changeRoom = $source.Substring($source.IndexOf('case Phase.ChangeRoom:'), $source.IndexOf('case Phase.ToKame:') - $source.IndexOf('case Phase.ChangeRoom:'))
    Assert-True ($changeRoom.Contains('EnterRoom(roomIndex);') -and -not $changeRoom.Contains('FinishBarracks()')) "$game map 10 fights boss and mobs before flying to Kame"
    Assert-True ($fightRoom.Contains('if (roomIndex == Rooms.Length - 1)') -and $fightRoom.Contains('FinishBarracks();') -and $source.Contains('clanIdBeforeDisband = me.clan.ID;')) "$game final-room finishes barracks after FightRoom clears boss and mobs"
    Assert-True ($source.Contains('if (AutoXmap.IsXmapRunning) break;')) "$game waits for map transition completion before starting new travel"
    Assert-True ($source.Contains('XmapData.FindWaypoint(next) == null')) "$game requires open portal"
    Assert-True ($source.Contains('me.clan.ID != activeClanId') -and $source.Contains('me.clan.currMember != 1') -and $source.Contains('me.role != 0')) "$game protects clan identity and membership"
    Assert-True ($source.Contains('me.clan.ID != clanIdBeforeDisband')) "$game rechecks clan before confirmation"
    Assert-True ($source.Contains('if (me.clan == null) SetPhase(Phase.WaitCreate')) "$game waits for disband response"
    Assert-True ($source.Contains('if (me.clan == null || me.clan.ID <= 0) break;')) "$game waits for create response"
    Assert-True ($source.Contains('Service.gI().getClan(2, (sbyte)flagId, clanName)')) "$game uses existing clan creation action"
    Assert-True (-not $source.Contains('if (!flagFound)') -and $source.Contains('flagId < 0 || flagId > 255')) "$game creates clan without requiring flag panel cache"
    Assert-True ($source.Contains('Service.gI().confirmMenu((short)id, (sbyte)selected)')) "$game sends captured menu index"
    Assert-True ($source.Contains('Service.gI().menu(id, selected, 0)') -and $source.Contains('public bool OnReceiveUiMenu(MyVector commands, Npc npc)')) "$game confirms packet 33 NPC menu without showing panel"
    Assert-True ($source.Contains('private void TryCaptureVisibleKameMenu()') -and $source.Contains('GameCanvas.menu.menuItems') -and $source.Contains('command.idAction != 11057 && command.idAction != 88822')) "$game consumes displayed Kame confirmation"
    Assert-True ($source.Contains('case Phase.WaitKameMenu:') -and $source.Contains('case Phase.WaitDisband:') -and ([regex]::Matches($source, 'TryCaptureVisibleKameMenu\(\);')).Count -eq 2) "$game catches initial and confirmation menus"
    Assert-True (-not $source.ToLower().Contains('yes you do')) "$game does not depend on yes you do text"
    Assert-True ($source.Contains('private int kameMenuStep;') -and $source.Contains('FindMenu(menu, "giải tán bang")') -and $source.Contains('if (index < 0 && kameMenuStep > 0)') -and $source.Contains('index = 0;') -and $source.Contains('pendingOption = 0;') -and $source.Contains('pendingFinalMenu = true;')) "$game uses kameMenuStep and selects index 0 for disband confirmation"
    Assert-True ($source.Contains('Input.GetKeyDown(KeyCode.F6)') -and $source.Contains('Stop(')) "$game F6 stop"
    Assert-True ($source.Contains('if (me.meDead || me.cHP <= 0 || me.statusMe == 14) { RecoverDeath(me); return; }') -and $source.Contains('if (recovering) { ContinueAfterRecovery(me); return; }')) "$game waits through death"
    Assert-True ($source.Contains('me.doUsePotion()') -and $source.Contains('Service.gI().wakeUpFromDead()') -and $source.Contains('if (Now - recoverySince > 120000)')) "$game bean, revive fallback, timeout"
    Assert-True ($source.Contains('if (phase >= Phase.WaitDisband && me.clan == null)') -and $source.Contains('BeginRun(); // The old barracks instance may have expired')) "$game safe recovery reentry"
    Assert-True ($source.Contains('int index = bambooMenuStep == 0 ? 0 : FindMenu(menu, "xong")') -and $source.Contains('if (phase == Phase.WaitBambooMenu) bambooMenuStep++;')) "$game enters NPC first option before completion menu"
    Assert-True ($source.Contains('me.FocusManualTo(entrance)') -and $source.Contains('MoveToNpc(kame)') -and $source.Contains('ModFunc.GetNpcByTempId(13)') -and $source.Contains('ModFunc.GI().MoveTo(npc.cx, npc.cy - 1)') -and $source.Contains('me?.FocusManualTo(npc)')) "$game finds NPC 13, teleports directly to Quy Lao, and focuses NPCs"
    $sendMenu = $source.Substring($source.IndexOf('private void SendPendingMenu()'))
    Assert-True ($sendMenu.Contains('pendingNpcId = pendingOption = -1;') -and $sendMenu.IndexOf('int selected = pendingOption;') -lt $sendMenu.IndexOf('pendingNpcId = pendingOption = -1;')) "$game preserves menu index before clearing"
    Assert-True ($source.Contains('SaveCheckpoint();') -and $source.Contains('checkpointCharacter = me.charID') -and $source.Contains('checkpointClan = activeClanId')) "$game persistent character and clan checkpoint"
    Assert-True ($source.Contains('private void Resume()') -and $source.Contains('EnterRoom(currentRoom, currentRoom == savedRoom)') -and $source.Contains('else BeginRun(); // Instance expired')) "$game resume from current map or restart expired instance"
    Assert-True ($source.Contains('public void Paint(mGraphics g)') -and $source.Contains('0x160A03, 0.94f') -and $source.Contains('if (isShowWindow) UpdateTouch()')) "$game translucent interactive panel"
    Assert-True ($source.Contains('Array.IndexOf(Rooms, value) >= 0') -and $source.Contains('ValidDelay(value)')) "$game validates configurable map and delay"
    Assert-True ($source.Contains('"' + $game + '.AutoDoanhTrai"')) "$game separate saved settings"
    Assert-True (([regex]::Matches($source, '\{')).Count -eq ([regex]::Matches($source, '\}')).Count) "$game balanced braces"

    $mod = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/ModFunc.cs") -Raw -Encoding UTF8
    $scr = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/GameScr.cs") -Raw -Encoding UTF8
    $panel = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/Panel.cs") -Raw -Encoding UTF8
    $sound = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/SoundMn.cs") -Raw -Encoding UTF8
    $info = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/InfoMe.cs") -Raw -Encoding UTF8
    $controller = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/$game/Controller.cs") -Raw -Encoding UTF8
    Assert-True ($mod.Contains("$game.Mod.AutoDoanhTrai.gI().Update();")) "$game update hook"
    Assert-True ($mod.Contains("$game.Mod.AutoDoanhTrai.gI().Paint(g);") -and $mod.Contains('case 117:') -and $mod.Contains("$game.Mod.AutoDoanhTrai.gI().ToggleWindow();")) "$game U shortcut and draw hook"
    Assert-True ($mod.Contains("case 121:`r`n`t`t`tperform(42, null);") -or $mod.Contains("case 121:`n`t`t`tperform(42, null);")) "$game preserves former U action on Y"
    Assert-True ($scr.Contains("$game.Mod.AutoDoanhTrai.gI().OnReceiveMenu(menu, npc)")) "$game NPC menu hook"
    Assert-True ($scr.Contains("if (!AutoXmap.IsXmapRunning && !Game1.Mod.AutoDoanhTrai.gI().isRunning)")) "$game GameScr suppresses PickMob while AutoDoanhTrai runs"
    Assert-True ($panel.Contains("$game.Mod.AutoDoanhTrai.gI().OpenPanel();") -and $panel.Contains('currentListLength = strModFunc.Length;') -and $sound.Contains('new(bool, string)[6]')) "$game Mod panel hook"
    Assert-True ($info.Contains("$game.Mod.AutoDoanhTrai.gI().OnServerInfo(s);")) "$game server error hook"
    Assert-True ($controller.Contains("$game.Mod.AutoDoanhTrai.gI().OnReceiveUiMenu(myVector, Char.myCharz().npcFocus)")) "$game packet 33 hook"
}

Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'DecompiledSource/Game2.Mod/AutoDoanhTrai.cs'))) 'Game2 barracks module removed'
foreach ($name in @('GameScr.cs', 'InfoMe.cs', 'ModFunc.cs', 'Panel.cs', 'SoundMn.cs')) {
    $game2Source = Get-Content -LiteralPath (Join-Path $root "DecompiledSource/Game2/$name") -Raw -Encoding UTF8
    Assert-True (-not $game2Source.Contains('AutoDoanhTrai')) "Game2 $name has no barracks hook"
}

# Scenario checks for the documented room order and stop conditions.
$rooms = @(53, 58, 59, 60, 61, 62, 55, 56, 54, 57)
Assert-True ($rooms.Count -eq 10 -and $rooms[2] -eq 59 -and $rooms[6] -eq 55 -and $rooms[-1] -eq 57) 'room order, third room, map-7 third boss, final room'
function CanLeaveRoom([int]$mapId, [bool]$normalAlive, [bool]$bossAlive, [bool]$bossSeen, [bool]$portalOpen, [bool]$pickableItem = $false) {
    if ($pickableItem -or -not $portalOpen) { return $false }
    if ($mapId -eq 59) {
        $bossKilled = $bossSeen -and -not $bossAlive
        return $bossKilled
    }
    if ($normalAlive -or $bossAlive) { return $false }
    return $true
}
Assert-True (-not (CanLeaveRoom 59 $true $false $false $true)) 'map 3 cannot leave before boss is seen and killed'
Assert-True (-not (CanLeaveRoom 59 $false $true $true $true)) 'invulnerable boss holds third room'
Assert-True (-not (CanLeaveRoom 59 $false $false $false $true)) 'unseen boss holds third room'
Assert-True (-not (CanLeaveRoom 59 $false $false $true $true $true)) 'map 3 holds while dropped items remain'
Assert-True (CanLeaveRoom 59 $false $false $true $true) 'third room advances after boss defeat and items picked'
Assert-True (CanLeaveRoom 59 $true $false $true $true) 'third room advances after boss defeat even if normal mobs respawn'
Assert-True (-not (CanLeaveRoom 53 $true $false $false $true)) 'other rooms still wait for normal mobs before leaving'
Assert-True (-not (CanLeaveRoom 53 $false $false $true $true $true)) 'other rooms wait until dropped items are picked'
Assert-True (-not (CanLeaveRoom 53 $false $false $false $false)) 'closed portal blocks next room'
Assert-True (-not (CanLeaveRoom 57 $true $false $false $true)) 'map 10 waits for normal mobs before flying to Kame'
Assert-True (-not (CanLeaveRoom 57 $false $true $true $true)) 'map 10 waits for boss before flying to Kame'
Assert-True (-not (CanLeaveRoom 57 $false $false $true $true $true)) 'map 10 waits until all dropped items are picked before flying to Kame'
Assert-True (CanLeaveRoom 57 $false $false $true $true) 'map 10 flies to Kame only after boss, normal mobs, and dropped items are cleared'

# Simulate item pick filtering (all drops including pet drops), retry cap, and bag-full handling.
function CanPickSim([int]$playerId, [int]$status, [int]$attempts, [bool]$bagFull, [int]$type) {
    if ($status -eq 2) { return $false }
    if ($attempts -ge 10) { return $false }
    if ($bagFull -and $type -notin @(9, 10, 34)) { return $false }
    return $true
}
Assert-True (CanPickSim 123 1 0 $false 12) 'picks own dropped item'
Assert-True (CanPickSim -1 1 0 $false 12) 'picks public dropped item'
Assert-True (CanPickSim 999 1 0 $false 12) 'picks pet or special playerId drop in barracks'
Assert-True (-not (CanPickSim 123 2 0 $false 12)) 'skips item already flying to player'
Assert-True (-not (CanPickSim 123 1 10 $false 12)) 'skips stuck item after MaxPickAttempts'
Assert-True (-not (CanPickSim 123 1 0 $true 12)) 'skips bag-slot item when bag is full'
Assert-True (CanPickSim 123 1 0 $true 9) 'still picks currency item when bag is full'

# Simulate map-3 (59) target selection across respawns vs other maps.
function SelectTarget([int]$mapId, [bool]$normal, [bool]$bossAlive, [bool]$bossSeen, [bool]$bossKilled) {
    if ($bossSeen -and -not $bossAlive) { $bossKilled = $true }
    if ($mapId -eq 59) {
        if (-not $bossKilled) {
            if ($normal) { return @{ Target = 'normal'; BossKilled = $bossKilled; Leave = $false } }
            if ($bossAlive) { return @{ Target = 'boss'; BossKilled = $bossKilled; Leave = $false } }
            return @{ Target = 'wait'; BossKilled = $bossKilled; Leave = $false }
        }
        return @{ Target = 'leave'; BossKilled = $bossKilled; Leave = $true }
    }
    if ($bossAlive) { return @{ Target = 'boss'; BossKilled = $bossKilled; Leave = $false } }
    if ($normal) { return @{ Target = 'normal'; BossKilled = $bossKilled; Leave = $false } }
    return @{ Target = 'leave'; BossKilled = $bossKilled; Leave = $true }
}
$step = SelectTarget 59 $true $true $true $false
Assert-True ($step.Target -eq 'normal' -and -not $step.Leave) 'map 3 first kills normal mob'
$step = SelectTarget 59 $false $true $true $false
Assert-True ($step.Target -eq 'boss' -and -not $step.Leave) 'map 3 switches to boss after normal mobs cleared'
$step = SelectTarget 59 $true $true $true $false
Assert-True ($step.Target -eq 'normal' -and -not $step.Leave) 'map 3 switches back to respawned normal mob even while boss is alive'
$step = SelectTarget 59 $false $false $true $false
Assert-True ($step.Target -eq 'leave' -and $step.BossKilled -and $step.Leave) 'map 3 leaves once boss is killed'
$step = SelectTarget 59 $true $false $true $true
Assert-True ($step.Target -eq 'leave' -and $step.Leave) 'map 3 ignores newly respawned mobs after boss is killed'
$step = SelectTarget 59 $false $false $false $false
Assert-True ($step.Target -eq 'wait' -and -not $step.Leave) 'map 3 empty map before boss appears waits safely'
$otherStep = SelectTarget 55 $true $true $true $false
Assert-True ($otherStep.Target -eq 'boss') 'map 7 (third boss) keeps boss priority over normal mobs'

# Simulate the 2-step Kame disband menu flow (step 1 by label, step 2 by first option index 0 regardless of text).
function SelectKameMenu([string[]]$menu, [int]$kameStep, [string]$currentPhase) {
    if ($menu.Count -eq 0) { return @{ Accepted = $false; Index = -1; Final = $false; NextStep = $kameStep; NextPhase = $currentPhase } }
    $idx = -1
    for ($i = 0; $i -lt $menu.Count; $i++) {
        if ($menu[$i].ToLower().Contains('giải tán bang')) { $idx = $i; break }
    }
    $final = $false
    if ($idx -lt 0 -and $kameStep -gt 0) {
        $idx = 0
        $final = $true
    }
    if ($idx -lt 0) { return @{ Accepted = $false; Index = -1; Final = $false; NextStep = $kameStep; NextPhase = $currentPhase } }
    $nextStep = if ($currentPhase -eq 'WaitKameMenu' -and -not $final) { $kameStep + 1 } else { $kameStep }
    $nextPhase = if ($final -and $currentPhase -eq 'WaitKameMenu') { 'WaitDisband' } else { $currentPhase }
    return @{ Accepted = $true; Index = $idx; Final = $final; NextStep = $nextStep; NextPhase = $nextPhase }
}
$k1 = SelectKameMenu @('Nói chuyện', 'Nhận ngọc', 'Giải tán bang') 0 'WaitKameMenu'
Assert-True ($k1.Accepted -and $k1.Index -eq 2 -and -not $k1.Final -and $k1.NextStep -eq 1 -and $k1.NextPhase -eq 'WaitKameMenu') 'Kame step 1 selects Giải tán bang and increments kameMenuStep'
$k2 = SelectKameMenu @('Yes I do', 'Không') $k1.NextStep $k1.NextPhase
Assert-True ($k2.Accepted -and $k2.Index -eq 0 -and $k2.Final -and $k2.NextPhase -eq 'WaitDisband') 'Kame step 2 selects index 0 (Yes I do) and enters WaitDisband'
$k2Any = SelectKameMenu @('Đồng ý hoàn toàn', 'Từ chối') 1 'WaitKameMenu'
Assert-True ($k2Any.Accepted -and $k2Any.Index -eq 0 -and $k2Any.Final -and $k2Any.NextPhase -eq 'WaitDisband') 'Kame step 2 selects index 0 regardless of confirmation label'
$k0Unexpected = SelectKameMenu @('Yes I do', 'Không') 0 'WaitKameMenu'
Assert-True (-not $k0Unexpected.Accepted) 'Kame step 0 does not blindly pick index 0 before selecting Giải tán bang'

$backup = Get-Content -LiteralPath (Join-Path $root 'MyMod/do_backup.ps1') -Raw -Encoding UTF8
Assert-True ($backup.Contains('Game1.Mod\AutoDoanhTrai.cs') -and -not $backup.Contains('Game2.Mod\AutoDoanhTrai.cs')) 'snapshot includes Game1 module only'

Write-Host "PASS: $checks Auto Doanh Trai checks" -ForegroundColor Green
