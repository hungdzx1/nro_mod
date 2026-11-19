using System;
using Game1.Mod.XMAP;
using UnityEngine;

namespace Game1.Mod;

// Coordinates one barracks run. All server actions are gated by observed map/menu/clan state.
public sealed class AutoDoanhTrai : IActionListener, IChatable
{
	private enum Phase { Idle, ToBamboo, OpenBamboo, WaitBambooMenu, EnterBarracks, Fight, ChangeRoom, ToKame, OpenKame, WaitKameMenu, WaitDisband, WaitCreate, NextRun }
	private static readonly int[] Rooms = { 53, 58, 59, 60, 61, 62, 55, 56, 54, 57 };
	private const int ThirdBossRoomId = 55;
	private const int MaxPickAttempts = 10;
	private static AutoDoanhTrai instance;
	public static AutoDoanhTrai gI() => instance ??= new AutoDoanhTrai();

	[AutoSetting] public string clanName = "";
	[AutoSetting] public int flagId;
	[AutoSetting] public int runLimit = 1; // 0 means continuous.
	[AutoSetting] public int gateRoomId = 59;
	[AutoSetting] public int defaultSkillId = -1;
	[AutoSetting] public int gateBossSkillId = -1;
	[AutoSetting] public int delayTravelMs = 700;
	[AutoSetting] public int delayMenuMs = 500;
	[AutoSetting] public int delayFightMs = 400;
	[AutoSetting] public int delayRoomMs = 1500;
	[AutoSetting] public int delayClanMs = 1200;
	[AutoSetting] public int delayNextRunMs = 2000;
	[AutoSetting] private bool resumeAvailable;
	[AutoSetting] private int checkpointPhase;
	[AutoSetting] private int checkpointRoom;
	[AutoSetting] private int checkpointMap;
	[AutoSetting] private int checkpointCharacter;
	[AutoSetting] private int checkpointClan;
	[AutoSetting] private int checkpointRuns;
	[AutoSetting] private bool checkpointBossSeen;
	[AutoSetting] private bool checkpointWaveCleared;

	public bool isRunning;
	public int completedRuns;
	private Phase phase;
	private long phaseSince;
	private long nextAction;
	private long lastAttack;
	private long lastMove;
	private long lastPick;
	private int currentPickItemId = -1;
	private int pickAttempts;
	private int roomIndex;
	private bool bossSeen;
	private bool bossKilled;
	private bool mobWaveCleared;
	private bool sawNormalMob;
	private int bambooMenuStep;
	private int kameMenuStep;
	private long clearSince;
	private int pendingNpcId = -1;
	private int pendingOption = -1;
	private bool pendingFinalMenu;
	private bool pendingUiMenu;
	private bool recovering;
	private long recoverySince;
	private long lastRecoveryAction;
	private int recoveryMap;
	private int editField;
	private string settingsSnapshot;
	private string detail = "Sẵn sàng";
	private int clanIdBeforeDisband;
	private int activeClanId;
	public bool isShowWindow;
	private bool delayPage;
	private bool dragging;
	private int dragX, dragY;
	private int winX = -1, winY = -1;
	private const int winW = 238, winH = 294;

	private AutoDoanhTrai()
	{
		settingsSnapshot = AutoSettingsStore.Load(this, "Game1.AutoDoanhTrai");
	}

	private static long Now => mSystem.currentTimeMillis();

	private void SaveCheckpoint()
	{
		if (!isRunning) return;
		Char me = Char.myCharz();
		if (me == null) return;
		resumeAvailable = true;
		checkpointPhase = (int)phase;
		checkpointRoom = roomIndex;
		checkpointMap = TileMap.mapID;
		checkpointCharacter = me.charID;
		checkpointClan = activeClanId;
		checkpointRuns = completedRuns;
		checkpointBossSeen = bossSeen;
		checkpointWaveCleared = mobWaveCleared;
		AutoSettingsStore.SaveIfChanged(this, "Game1.AutoDoanhTrai", ref settingsSnapshot);
	}

	private void ClearCheckpoint()
	{
		resumeAvailable = false;
		checkpointPhase = checkpointRoom = checkpointMap = checkpointCharacter = checkpointClan = checkpointRuns = 0;
		checkpointBossSeen = checkpointWaveCleared = false;
		AutoSettingsStore.SaveIfChanged(this, "Game1.AutoDoanhTrai", ref settingsSnapshot);
	}

	private void SetPhase(Phase value, string message, int delay)
	{
		phase = value;
		phaseSince = Now;
		nextAction = phaseSince + delay;
		detail = message;
		GameScr.info1.addInfo("Doanh trại: " + message, 0);
		SaveCheckpoint();
	}

	public void Stop(string reason)
	{
		SaveCheckpoint();
		isRunning = false;
		phase = Phase.Idle;
		pendingNpcId = -1;
		pendingOption = -1;
		pendingFinalMenu = false;
		pendingUiMenu = false;
		kameMenuStep = 0;
		currentPickItemId = -1;
		pickAttempts = 0;
		recovering = false;
		if (AutoXmap.IsXmapRunning && (XmapController.IdMapEnd == 27 || XmapController.IdMapEnd == 5 || Array.IndexOf(Rooms, XmapController.IdMapEnd) >= 0))
		{
			XmapController.FinishXmap();
		}
		detail = reason;
		GameScr.info1.addInfo("Doanh trại: " + reason, 0);
	}

	private void Start()
	{
		Char me = Char.myCharz();
		if (me == null || me.meDead || me.cHP <= 0) { Stop("Nhân vật đang chết"); return; }
		if (string.IsNullOrWhiteSpace(clanName)) { Stop("Hãy nhập tên bang trong cấu hình"); return; }
		if (me.clan == null || me.clan.currMember != 1 || me.role != 0) { Stop("Chỉ chạy với bang một thành viên do bạn làm bang chủ"); return; }
		if (PickMob.tanSat || AutoXmap.IsXmapRunning || AutoGop.gI().isRunning || AutoMuaNhieu.gI().isRunning || NpcMenuHelper.gI().isAutoRunning)
		{
			Stop("Hãy dừng các auto chiến đấu, Xmap và NPC khác trước");
			return;
		}
		completedRuns = 0;
		activeClanId = me.clan.ID;
		isRunning = true;
		isShowWindow = false;
		ClearCheckpoint();
		BeginRun();
	}

	private void Resume()
	{
		if (!resumeAvailable || isRunning) return;
		Char me = Char.myCharz();
		if (me == null || me.charID != checkpointCharacter || me.meDead || me.cHP <= 0) { GameScr.info1.addInfo("Checkpoint không đúng nhân vật hoặc nhân vật đang chết", 0); return; }
		if (PickMob.tanSat || AutoXmap.IsXmapRunning || AutoGop.gI().isRunning || AutoMuaNhieu.gI().isRunning || NpcMenuHelper.gI().isAutoRunning) { GameScr.info1.addInfo("Hãy dừng các auto khác trước", 0); return; }
		Phase saved = (Phase)checkpointPhase;
		if (saved <= Phase.Idle || saved > Phase.NextRun) { ClearCheckpoint(); return; }
		bool afterDisband = (saved >= Phase.WaitCreate && saved <= Phase.NextRun) || (saved == Phase.WaitDisband && me.clan == null);
		if (!afterDisband && (me.clan == null || me.clan.ID != checkpointClan || me.clan.currMember != 1 || me.role != 0)) { GameScr.info1.addInfo("Bang đã thay đổi, không thể tiếp tục", 0); return; }
		if (afterDisband && me.clan != null && me.clan.ID != checkpointClan)
		{
			if (saved == Phase.NextRun && me.clan.name == clanName && me.clan.currMember == 1 && me.role == 0)
			{
				completedRuns = checkpointRuns + 1;
				activeClanId = me.clan.ID;
				if (runLimit > 0 && completedRuns >= runLimit) { ClearCheckpoint(); GameScr.info1.addInfo("Doanh trại: Đã hoàn thành " + completedRuns + " lượt", 0); }
				else { isRunning = true; BeginRun(); }
				return;
			}
			GameScr.info1.addInfo("Bang đã thay đổi, không thể tiếp tục", 0); return;
		}
		activeClanId = checkpointClan;
		clanIdBeforeDisband = checkpointClan;
		completedRuns = checkpointRuns;
		int savedRoom = checkpointRoom;
		roomIndex = savedRoom;
		bossSeen = checkpointBossSeen;
		mobWaveCleared = checkpointWaveCleared;
		bossKilled = false;
		clearSince = 0;
		sawNormalMob = false;
		pendingNpcId = pendingOption = -1;
		pendingFinalMenu = false;
		pendingUiMenu = false;
		recovering = false;
		bambooMenuStep = 0;
		kameMenuStep = 0;
		lastPick = 0;
		currentPickItemId = -1;
		pickAttempts = 0;
		isRunning = true;
		isShowWindow = false;
		int currentRoom = Array.IndexOf(Rooms, TileMap.mapID);
		if (currentRoom >= 0 && saved < Phase.ToKame)
		{
			if (currentRoom != savedRoom) { bossSeen = false; mobWaveCleared = false; }
			EnterRoom(currentRoom, currentRoom == savedRoom);
		}
		else if (TileMap.mapID == 5 && saved >= Phase.ToKame)
		{
			if (me.clan == null) SetPhase(Phase.WaitCreate, "Tiếp tục lập bang", delayClanMs);
			else if (me.clan.ID != checkpointClan) { Stop("Bang đã thay đổi"); return; }
			else SetPhase(Phase.OpenKame, "Tiếp tục ở Kame", delayMenuMs);
		}
		else if (saved >= Phase.WaitDisband && me.clan == null) SetPhase(Phase.WaitCreate, "Tiếp tục lập bang", delayClanMs);
		else if (saved >= Phase.ToKame && TileMap.mapID == Rooms[Rooms.Length - 1]) FinishBarracks();
		else BeginRun(); // Instance expired: start this run again, never skip straight to clan disband.
	}

	private void BeginRun()
	{
		roomIndex = 0;
		bossSeen = false;
		bossKilled = false;
		mobWaveCleared = false;
		sawNormalMob = false;
		bambooMenuStep = 0;
		kameMenuStep = 0;
		lastPick = 0;
		currentPickItemId = -1;
		pickAttempts = 0;
		clearSince = 0;
		pendingNpcId = -1;
		pendingOption = -1;
		pendingFinalMenu = false;
		pendingUiMenu = false;
		recovering = false;
		SetPhase(Phase.ToBamboo, "Bay đến Rừng Bamboo", delayTravelMs);
		if (TileMap.mapID != 27) XmapController.StartRunToMapId(27);
	}

	public void Update()
	{
		AutoSettingsStore.SaveIfChanged(this, "Game1.AutoDoanhTrai", ref settingsSnapshot);
		if (isShowWindow && AutoPhaLe.gI().isShowWindow) isShowWindow = false;
		if (isShowWindow) UpdateTouch();
		if (!isRunning) return;
		if (Input.GetKeyDown(KeyCode.F6)) { Stop("Đã dừng bởi F6"); return; }
		try { Tick(); }
		catch (Exception ex) { Stop("Lỗi " + phase + ": " + ex.Message); }
	}

	private void RecoverDeath(Char me)
	{
		if (!recovering)
		{
			recovering = true;
			recoverySince = Now;
			recoveryMap = TileMap.mapID;
			lastRecoveryAction = 0;
			SaveCheckpoint();
			if (me.doUsePotion()) { lastRecoveryAction = Now; detail = "Đã dùng đậu, chờ hồi sinh"; return; }
		}
		if (Now - recoverySince > 120000) { Stop("Không hồi sinh được sau 2 phút"); return; }
		if (Now - lastRecoveryAction >= 2500)
		{
			Service.gI().wakeUpFromDead();
			lastRecoveryAction = Now;
			detail = "Đang hồi sinh để tiếp tục";
		}
	}

	private void ContinueAfterRecovery(Char me)
	{
		if (Char.isLoadingMap) return;
		recovering = false;
		pendingNpcId = pendingOption = -1;
		pendingFinalMenu = pendingUiMenu = false;
		kameMenuStep = 0;
		if (phase >= Phase.WaitDisband && me.clan == null) { SetPhase(Phase.WaitCreate, "Hồi sinh: tiếp tục lập bang", delayClanMs); return; }
		if (phase == Phase.NextRun && me.clan != null && me.clan.ID != clanIdBeforeDisband) { SetPhase(Phase.NextRun, "Hồi sinh: xác nhận bang mới", delayClanMs); return; }
		if (TileMap.mapID != recoveryMap)
		{
			if (phase >= Phase.ToKame) { SetPhase(Phase.ToKame, "Hồi sinh: về Kame", delayTravelMs); XmapController.StartRunToMapId(5); return; }
			if (me.clan == null || me.clan.ID != activeClanId) { Stop("Bang đổi sau khi hồi sinh"); return; }
			BeginRun(); // The old barracks instance may have expired after a respawn.
			return;
		}
		if (phase == Phase.WaitBambooMenu || phase == Phase.EnterBarracks) SetPhase(Phase.OpenBamboo, "Hồi sinh: mở lại NPC", delayMenuMs);
		else if (phase == Phase.WaitKameMenu || phase == Phase.WaitDisband) SetPhase(Phase.OpenKame, "Hồi sinh: mở lại Kame", delayMenuMs);
		else if (phase == Phase.ChangeRoom && Array.IndexOf(Rooms, TileMap.mapID) >= 0) EnterRoom(Array.IndexOf(Rooms, TileMap.mapID));
		else SetPhase(phase, "Hồi sinh: tiếp tục " + phase, delayFightMs);
	}

	private void Tick()
	{
		Char me = Char.myCharz();
		if (me == null) return;
		if (me.meDead || me.cHP <= 0 || me.statusMe == 14) { RecoverDeath(me); return; }
		if (recovering) { ContinueAfterRecovery(me); return; }
		if (Char.isLoadingMap || Now < nextAction) return;
		if (Now - (phase == Phase.Fight ? System.Math.Max(System.Math.Max(phaseSince, lastAttack), lastPick) : phaseSince) > ((phase == Phase.Fight) ? 600000 : 90000)) { Stop("Quá thời gian ở bước " + phase); return; }
		switch (phase)
		{
		case Phase.ToBamboo:
			if (TileMap.mapID == 27 && !AutoXmap.IsXmapRunning) SetPhase(Phase.OpenBamboo, "Mở NPC vào doanh trại", delayTravelMs);
			else if (!AutoXmap.IsXmapRunning) Stop("Không đến được Rừng Bamboo");
			break;
		case Phase.OpenBamboo:
			Npc entrance = ModFunc.GetNpcByTempId(25);
			if (entrance == null) { Stop("Không tìm thấy NPC doanh trại ở Bamboo"); break; }
			ModFunc.GI().GotoNpc(25);
			me.FocusManualTo(entrance);
			Service.gI().openMenu(25);
			bambooMenuStep = 0;
			SetPhase(Phase.WaitBambooMenu, "Chờ menu doanh trại", delayMenuMs);
			break;
		case Phase.WaitBambooMenu:
			if (TileMap.mapID == Rooms[0]) { EnterRoom(0); break; }
			SendPendingMenu();
			break;
		case Phase.WaitKameMenu:
			TryCaptureVisibleKameMenu();
			if (Now >= nextAction) SendPendingMenu();
			if (kameMenuStep == 0 && pendingNpcId < 0 && !GameCanvas.menu.showMenu && Now - nextAction >= 1500)
			{
				Npc retryKame = FindKame();
				if (retryKame != null)
				{
					MoveToNpc(retryKame);
					Service.gI().openMenu(retryKame.template.npcTemplateId);
					nextAction = Now + delayMenuMs;
				}
			}
			break;
		case Phase.EnterBarracks:
			if (TileMap.mapID == Rooms[0]) EnterRoom(0);
			break;
		case Phase.Fight:
			FightRoom();
			break;
		case Phase.ChangeRoom:
			if (TileMap.mapID == Rooms[roomIndex])
			{
				if (AutoXmap.IsXmapRunning) break;
				EnterRoom(roomIndex);
			}
			else if (!AutoXmap.IsXmapRunning) Stop("Không chuyển được sang map " + Rooms[roomIndex]);
			break;
		case Phase.ToKame:
			if (TileMap.mapID == 5 && !AutoXmap.IsXmapRunning) SetPhase(Phase.OpenKame, "Mở menu Quy Lão Kame", delayTravelMs);
			else if (!AutoXmap.IsXmapRunning) Stop("Không đến được Đảo Kame");
			break;
		case Phase.OpenKame:
			if (me.clan == null || me.clan.ID != clanIdBeforeDisband || me.clan.currMember != 1 || me.role != 0) { Stop("Thông tin bang đã thay đổi"); break; }
			Npc kame = FindKame();
			if (kame == null) { nextAction = Now + 300; break; }
			GameCanvas.menu.showMenu = false;
			MoveToNpc(kame);
			Service.gI().openMenu(kame.template.npcTemplateId);
			kameMenuStep = 0;
			SetPhase(Phase.WaitKameMenu, "Chờ menu giải tán bang", delayMenuMs);
			break;
		case Phase.WaitDisband:
			TryCaptureVisibleKameMenu();
			if (Now >= nextAction) SendPendingMenu();
			if (me.clan == null) SetPhase(Phase.WaitCreate, "Lập lại bang", delayClanMs);
			else if (me.clan.ID != clanIdBeforeDisband) Stop("Bang thay đổi khi chờ giải tán");
			break;
		case Phase.WaitCreate:
			CreateClan();
			break;
		case Phase.NextRun:
				if (me.clan == null || me.clan.ID <= 0) break;
				if (me.clan.name != clanName || me.clan.currMember != 1 || me.role != 0) { Stop("Bang mới không khớp cấu hình"); break; }
				activeClanId = me.clan.ID;
			completedRuns++;
			if (runLimit > 0 && completedRuns >= runLimit) { ClearCheckpoint(); Stop("Đã hoàn thành " + completedRuns + " lượt"); ClearCheckpoint(); }
			else BeginRun();
			break;
		}
	}

	private void EnterRoom(int index, bool keepProgress = false)
	{
		roomIndex = index;
		if (!keepProgress) bossSeen = false;
		bossKilled = false;
		if (!keepProgress) mobWaveCleared = false;
		sawNormalMob = keepProgress && mobWaveCleared;
		lastPick = 0;
		currentPickItemId = -1;
		pickAttempts = 0;
		clearSince = 0;
		SetPhase(Phase.Fight, "Đánh map " + Rooms[index] + " (" + (index + 1) + "/" + Rooms.Length + ")", delayFightMs);
	}

	private void FinishBarracks()
	{
		Char me = Char.myCharz();
		if (me.clan == null || me.clan.ID != activeClanId || me.clan.currMember != 1 || me.role != 0) { Stop("Bang đã thay đổi, không giải tán"); return; }
		clanIdBeforeDisband = me.clan.ID;
		SetPhase(Phase.ToKame, "Đã đến map cuối, bay đến Kame", delayTravelMs);
		XmapController.StartRunToMapId(5);
	}

	private static bool IsAlive(Mob mob) => mob != null && mob.hp > 0 && mob.status != 0 && mob.status != 1 && !mob.isMobMe;
	private static bool IsBoss(Mob mob) => mob.isBoss || mob.levelBoss > 0;
	private static bool IsAliveBoss(Char target) => target != null && target.charID < 0 && target.cTypePk == 5 && target.cHP > 0 && target.statusMe != 14;
	private void MarkBossSeen() { if (!bossSeen) { bossSeen = true; SaveCheckpoint(); } }

	private void ResetRoomPickCounts()
	{
		currentPickItemId = -1;
		pickAttempts = 0;
		if (GameScr.vItemMap == null) return;
		for (int i = 0; i < GameScr.vItemMap.size(); i++)
		{
			if (GameScr.vItemMap.elementAt(i) is ItemMap item && item.countAutoPick > 0) item.countAutoPick = 0;
		}
	}

	private static bool CanPickItem(Char me, ItemMap item)
	{
		if (me == null || item == null || item.template == null || item.status == 2) return false;
		if (item.countAutoPick >= MaxPickAttempts) return false;
		if (GameScr.gI().isBagFull() && item.template.type != 9 && item.template.type != 10 && item.template.type != 34) return false;
		return true;
	}

	private static ItemMap FindPickableItem(Char me)
	{
		if (me == null || GameScr.vItemMap == null) return null;
		ItemMap best = null;
		int bestDistance = int.MaxValue;
		for (int i = 0; i < GameScr.vItemMap.size(); i++)
		{
			ItemMap item = (ItemMap)GameScr.vItemMap.elementAt(i);
			if (!CanPickItem(me, item)) continue;
			int targetX = item.xEnd != 0 ? item.xEnd : item.x;
			int targetY = item.yEnd != 0 ? item.yEnd : item.y;
			int distance = System.Math.Abs(targetX - me.cx) + System.Math.Abs(targetY - me.cy);
			if (distance < bestDistance)
			{
				best = item;
				bestDistance = distance;
			}
		}
		return best;
	}

	private void PickItem(Char me, ItemMap item)
	{
		me.itemFocus = item;
		int targetX = item.xEnd != 0 ? item.xEnd : item.x;
		int targetY = item.yEnd != 0 ? item.yEnd : item.y;
		if (System.Math.Abs(me.cx - targetX) > 20 || System.Math.Abs(me.cy - targetY) > 20)
		{
			if (Now - lastMove >= 150)
			{
				ModFunc.GI().MoveTo(targetX, targetY);
				lastMove = Now;
			}
		}
		int pickDelay = System.Math.Max(300, delayFightMs);
		if (Now - lastPick < pickDelay) return;
		if (currentPickItemId != item.itemMapID)
		{
			currentPickItemId = item.itemMapID;
			pickAttempts = 0;
		}
		ModFunc.GI().MoveTo(targetX, targetY);
		Service.gI().pickItem(item.itemMapID);
		pickAttempts++;
		item.countAutoPick = pickAttempts;
		lastPick = Now;
		detail = "Nhặt đồ map " + TileMap.mapID;
	}

	private void FightRoom()
	{
		if (TileMap.mapID != Rooms[roomIndex]) { Stop("Rời map doanh trại ngoài dự kiến"); return; }
		Mob normal = null;
		Mob mobBoss = null;
		int normalDistance = int.MaxValue;
		int bossDistance = int.MaxValue;
		Char me = Char.myCharz();
		bool bossAlive = false;
		for (int i = 0; i < GameScr.vMob.size(); i++)
		{
			Mob mob = (Mob)GameScr.vMob.elementAt(i);
			if (mob == null || mob.isMobMe) continue;
			if (IsBoss(mob)) { MarkBossSeen(); if (IsAlive(mob)) bossAlive = true; }
			if (!IsAlive(mob) || !GameScr.gI().isMeCanAttackMob(mob)) continue;
			int distance = System.Math.Abs(mob.xFirst - me.cx) + System.Math.Abs(mob.yFirst - me.cy);
			if (IsBoss(mob)) { if (distance < bossDistance) { mobBoss = mob; bossDistance = distance; } }
			else if (distance < normalDistance) { normal = mob; normalDistance = distance; }
		}
		Char charBoss = null;
		for (int i = 0; i < GameScr.vCharInMap.size(); i++)
		{
			Char target = (Char)GameScr.vCharInMap.elementAt(i);
			if (target != null && target.charID < 0 && target.cTypePk == 5) { MarkBossSeen(); if (IsAliveBoss(target)) bossAlive = true; }
			if (IsAliveBoss(target)) charBoss = target; // Select the attack skill in AttackBoss; the current skill may be a buff.
		}
		if (normal != null) sawNormalMob = true;
		if (TileMap.mapID == gateRoomId && normal == null && sawNormalMob && !mobWaveCleared) { mobWaveCleared = true; SaveCheckpoint(); }
		if (bossSeen && !bossAlive && !bossKilled) { bossKilled = true; SaveCheckpoint(); }
		if (TileMap.mapID == gateRoomId)
		{
			if (!bossKilled)
			{
				ResetRoomPickCounts();
				if (normal != null) { clearSince = 0; AttackMob(normal); return; }
				if (mobBoss != null) { clearSince = 0; AttackMob(mobBoss); return; }
				if (charBoss != null) { clearSince = 0; AttackBoss(charBoss); return; }
				clearSince = 0;
				return;
			}
		}
		else
		{
			if (mobBoss != null) { ResetRoomPickCounts(); clearSince = 0; AttackMob(mobBoss); return; }
			if (charBoss != null) { ResetRoomPickCounts(); clearSince = 0; AttackBoss(charBoss); return; }
			if (normal != null) { ResetRoomPickCounts(); clearSince = 0; AttackMob(normal); return; }
			if (bossAlive) { ResetRoomPickCounts(); clearSince = 0; return; }
		}
		ItemMap item = FindPickableItem(me);
		if (item != null)
		{
			clearSince = 0;
			PickItem(me, item);
			return;
		}
		if (clearSince == 0) clearSince = Now;
		if (Now - clearSince < delayRoomMs) return;
		if (roomIndex == Rooms.Length - 1)
		{
			FinishBarracks();
			return;
		}
		int next = Rooms[roomIndex + 1];
		if (XmapData.FindWaypoint(next) == null) return; // Portal can appear after the last mob dies.
		roomIndex++;
		SetPhase(Phase.ChangeRoom, "Chuyển sang map " + next, delayRoomMs);
		XmapController.StartRunToMapId(next);
	}

	private static Skill GetAttackSkill()
	{
		AutoDoanhTrai auto = gI();
		Char me = Char.myCharz();
		if (me == null) return null;
		if (auto.defaultSkillId >= 0 && me.vSkill != null)
		{
			for (int i = 0; i < me.vSkill.size(); i++)
			{
				Skill s = (Skill)me.vSkill.elementAt(i);
				if (s != null && s.template != null && s.template.id == auto.defaultSkillId && s.template.isAttackSkill()) return s;
			}
		}
		if (me.myskill != null && me.myskill.template != null && me.myskill.template.isAttackSkill() && me.myskill.template.id != auto.gateBossSkillId) return me.myskill;
		SkillTemplate template = new SkillTemplate();
		foreach (sbyte id in PickMob.IdSkillsTanSat)
		{
			template.id = id;
			Skill skill = me.getSkill(template);
			if (skill != null && skill.template != null && skill.template.isAttackSkill()) return skill;
		}
		if (me.vSkill != null)
		{
			for (int i = 0; i < me.vSkill.size(); i++)
			{
				Skill s = (Skill)me.vSkill.elementAt(i);
				if (s != null && s.template != null && s.template.isAttackSkill()) return s;
			}
		}
		return me.myskill != null && me.myskill.template != null && me.myskill.template.isAttackSkill() ? me.myskill : null;
	}

	private static Skill GetGateBossSkill()
	{
		AutoDoanhTrai auto = gI();
		if (auto.gateBossSkillId < 0) return GetAttackSkill();
		Char me = Char.myCharz();
		if (me == null || me.vSkill == null) return null;
		for (int i = 0; i < me.vSkill.size(); i++)
		{
			Skill skill = (Skill)me.vSkill.elementAt(i);
			if (skill != null && skill.template != null && skill.template.id == auto.gateBossSkillId && skill.template.isAttackSkill()) return skill;
		}
		return null;
	}

	private void AttackMob(Mob target)
	{
		bool thirdBoss = TileMap.mapID == ThirdBossRoomId && IsBoss(target);
		Skill skill = thirdBoss ? GetGateBossSkill() : GetAttackSkill();
		if (skill == null) { Stop(thirdBoss ? "Skill boss thứ 3 đã chọn không khả dụng" : "Không có kỹ năng đánh quái"); return; }
		Char me = Char.myCharz();
		me.mobFocus = target;
		if (System.Math.Abs(me.cx - target.xFirst) > 45 || System.Math.Abs(me.cy - target.yFirst) > 45)
		{
			if (Now - lastMove >= delayFightMs) { ModFunc.GI().MoveTo(target.xFirst, target.yFirst); lastMove = Now; }
			return;
		}
		if (Now - skill.lastTimeUseThisSkill > (long)skill.coolDown + 25) skill.paintCanNotUseSkill = false;
		if (Now - lastAttack < delayFightMs || Now - skill.lastTimeUseThisSkill < skill.coolDown || skill.paintCanNotUseSkill || me.cMP < skill.manaUse) return;
		if (me.myskill != skill) GameScr.gI().doSelectSkill(skill, isShortcut: true);
		ModFunc.GI().AttackMob(target);
		ModFunc.GI().SetUsedSkill(skill);
		lastAttack = Now;
	}

	private void AttackBoss(Char target)
	{
		bool thirdBoss = TileMap.mapID == ThirdBossRoomId;
		Skill skill = thirdBoss ? GetGateBossSkill() : GetAttackSkill();
		if (skill == null) { Stop(thirdBoss ? "Skill boss thứ 3 đã chọn không khả dụng" : "Không có kỹ năng đánh boss"); return; }
		Char me = Char.myCharz();
		me.charFocus = target;
		if (System.Math.Abs(me.cx - target.cx) > 45 || System.Math.Abs(me.cy - target.cy) > 45)
		{
			if (Now - lastMove >= delayFightMs) { ModFunc.GI().MoveTo(target.cx, target.cy); lastMove = Now; }
			return;
		}
		if (Now - skill.lastTimeUseThisSkill > (long)skill.coolDown + 25) skill.paintCanNotUseSkill = false;
		if (Now - lastAttack < delayFightMs || Now - skill.lastTimeUseThisSkill < skill.coolDown || skill.paintCanNotUseSkill || me.cMP < skill.manaUse) return;
		if (me.myskill != skill) GameScr.gI().doSelectSkill(skill, isShortcut: true);
		MyVector chars = new MyVector();
		chars.addElement(target);
		Service.gI().sendPlayerAttack(new MyVector(), chars, 2);
		ModFunc.GI().SetUsedSkill(skill);
		lastAttack = Now;
	}

	private static void MoveToNpc(Npc npc)
	{
		if (npc == null) return;
		Char me = Char.myCharz();
		if (me != null && (System.Math.Abs(npc.cx - me.cx) > 24 || System.Math.Abs(npc.cy - me.cy) > 24))
		{
			ModFunc.GI().MoveTo(npc.cx, npc.cy - 1);
		}
		if (npc.template != null) ModFunc.GI().GotoNpc(npc.template.npcTemplateId);
		me?.FocusManualTo(npc);
	}

	private static Npc FindKame()
	{
		Npc byId = ModFunc.GetNpcByTempId(13);
		if (byId != null) return byId;
		for (int i = 0; i < GameScr.vNpc.size(); i++)
		{
			Npc npc = (Npc)GameScr.vNpc.elementAt(i);
			if (npc == null || npc.template == null || npc.template.name == null) continue;
			string name = npc.template.name.ToLower();
			if (name.Contains("kame") || name.Contains("quy lão")) return npc;
		}
		return null;
	}

		// Receives the server menu after the NPC is focused; the entrance action is option zero.
	public bool OnReceiveMenu(string[] menu, Npc npc)
	{
		if (!isRunning || npc == null || npc.template == null || menu == null) return false;
		int id = npc.template.npcTemplateId;
		if (phase == Phase.WaitBambooMenu && id == 25)
		{
			if (menu.Length == 0) return false;
			int index = bambooMenuStep == 0 ? 0 : FindMenu(menu, "xong");
			if (index < 0) index = 0;
			pendingNpcId = id;
			pendingOption = index;
			pendingFinalMenu = bambooMenuStep > 0;
			pendingUiMenu = false;
			nextAction = Now + delayMenuMs;
			return true;
		}
		if ((phase == Phase.WaitKameMenu || phase == Phase.WaitDisband) && TileMap.mapID == 5 && (FindKame()?.template.npcTemplateId == id || kameMenuStep > 0))
		{
			if (menu.Length == 0) return false;
			Char me = Char.myCharz();
			if (me.clan == null || me.clan.ID != clanIdBeforeDisband || me.clan.currMember != 1 || me.role != 0) { Stop("Bang thay đổi trước khi giải tán"); return true; }
			int index = FindMenu(menu, "giải tán bang");
			bool finalMenu = false;
			if (index < 0 && kameMenuStep > 0)
			{
				index = 0;
				finalMenu = true;
			}
			if (index >= 0) { pendingNpcId = id; pendingOption = index; pendingFinalMenu = finalMenu; pendingUiMenu = false; nextAction = Now + delayMenuMs; return true; }
		}
		return false;
	}

	// OPEN_UI_MENU (packet 33) is confirmed via Service.menu, not confirmMenu.
	public bool OnReceiveUiMenu(MyVector commands, Npc npc)
	{
		if (!isRunning || (phase != Phase.WaitKameMenu && phase != Phase.WaitDisband) || TileMap.mapID != 5 || npc == null || npc.template == null || commands == null || commands.size() == 0) return false;
		if (FindKame()?.template.npcTemplateId != npc.template.npcTemplateId && kameMenuStep <= 0) return false;
		Char me = Char.myCharz();
		if (me.clan == null || me.clan.ID != clanIdBeforeDisband || me.clan.currMember != 1 || me.role != 0) { Stop("Bang thay đổi trước khi giải tán"); return true; }
		for (int i = 0; i < commands.size(); i++)
		{
			Command command = (Command)commands.elementAt(i);
			if (command == null || command.idAction != 88822) continue;
			string caption = (command.caption ?? "").ToLower();
			if (!caption.Contains("giải tán bang")) continue;
			pendingNpcId = npc.template.npcTemplateId;
			pendingOption = i;
			pendingFinalMenu = false;
			pendingUiMenu = true;
			nextAction = Now + delayMenuMs;
			return true;
		}
		if (kameMenuStep > 0 && commands.elementAt(0) is Command first && first.idAction == 88822)
		{
			pendingNpcId = npc.template.npcTemplateId;
			pendingOption = 0;
			pendingFinalMenu = true;
			pendingUiMenu = true;
			nextAction = Now + delayMenuMs;
			return true;
		}
		return false;
	}

	private static int FindMenu(string[] menu, string label)
	{
		for (int i = 0; i < menu.Length; i++) if ((menu[i] ?? "").ToLower().Contains(label)) return i;
		return -1;
	}

	private void TryCaptureVisibleKameMenu()
	{
		if (pendingNpcId >= 0 || TileMap.mapID != 5 || !GameCanvas.menu.showMenu || GameCanvas.menu.menuItems == null || GameCanvas.menu.menuItems.size() == 0) return;
		Char me = Char.myCharz();
		Npc kame = FindKame();
		if (me == null || kame == null || me.clan == null || me.clan.ID != clanIdBeforeDisband) return;
		if (phase == Phase.WaitKameMenu)
		{
			for (int i = 0; i < GameCanvas.menu.menuItems.size(); i++)
			{
				Command command = (Command)GameCanvas.menu.menuItems.elementAt(i);
				if (command == null) continue;
				string caption = (command.caption ?? "").ToLower();
				if (!caption.Contains("giải tán bang")) continue;
				if (command.idAction != 11057 && command.idAction != 88822 && command.idAction != 88820) continue;
				if (command.idAction == 88820 && command.p is string[] choices && choices.Length > 1) continue;
				if (command.idAction == 11057 && command.p is Npc menuNpc && menuNpc.template != null && menuNpc.template.npcTemplateId != kame.template.npcTemplateId) continue;
				pendingNpcId = (command.idAction == 11057 && command.p is Npc npc && npc.template != null) ? npc.template.npcTemplateId : kame.template.npcTemplateId;
				pendingOption = i;
				pendingFinalMenu = false;
				pendingUiMenu = command.idAction != 11057;
				GameCanvas.menu.showMenu = false;
				GameCanvas.clearAllPointerEvent();
				nextAction = Now + delayMenuMs;
				phaseSince = Now;
				return;
			}
		}
		if ((kameMenuStep > 0 || phase == Phase.WaitDisband) && GameCanvas.menu.menuItems.elementAt(0) is Command first)
		{
			if (first.idAction != 11057 && first.idAction != 88822 && first.idAction != 88820) return;
			if (first.idAction == 88820 && first.p is string[] choices && choices.Length > 1) return;
			pendingNpcId = (first.idAction == 11057 && first.p is Npc npc && npc.template != null) ? npc.template.npcTemplateId : kame.template.npcTemplateId;
			pendingOption = 0;
			pendingFinalMenu = true;
			pendingUiMenu = first.idAction != 11057;
			GameCanvas.menu.showMenu = false;
			GameCanvas.clearAllPointerEvent();
			nextAction = Now + delayMenuMs;
			phaseSince = Now;
		}
	}

	private void SendPendingMenu()
	{
		if (pendingNpcId < 0 || pendingOption < 0) return;
		int id = pendingNpcId;
		int selected = pendingOption;
		bool final = pendingFinalMenu;
		bool uiMenu = pendingUiMenu;
		pendingNpcId = pendingOption = -1;
		pendingFinalMenu = false;
		pendingUiMenu = false;
		if (uiMenu) Service.gI().menu(id, selected, 0);
		else Service.gI().confirmMenu((short)id, (sbyte)selected);
		phaseSince = Now;
		if (phase == Phase.WaitBambooMenu) bambooMenuStep++;
		else if (phase == Phase.WaitKameMenu && !final) kameMenuStep++;
		if (final && phase == Phase.WaitBambooMenu) SetPhase(Phase.EnterBarracks, "Chờ vào map đầu", delayMenuMs);
		else if (final && phase == Phase.WaitKameMenu) SetPhase(Phase.WaitDisband, "Chờ xác nhận giải tán bang", delayClanMs);
	}

	private void CreateClan()
	{
		if (Char.myCharz().clan != null) { Stop("Vẫn còn trong bang cũ"); return; }
		if (Char.myCharz().xu < 10000) { Stop("Không đủ 10.000 vàng để lập bang"); return; }
		if (flagId < 0 || flagId > 255) { Stop("ID cờ bang không hợp lệ"); return; }
		Service.gI().getClan(2, (sbyte)flagId, clanName);
		SetPhase(Phase.NextRun, "Chờ xác nhận tạo bang", delayNextRunMs);
	}

	public void OnServerInfo(string text)
	{
		if (!isRunning || phase == Phase.Fight || string.IsNullOrEmpty(text)) return;
		string lower = text.ToLower();
		if (lower.Contains("không đủ") || lower.Contains("không thể") || lower.Contains("đã tồn tại") || lower.Contains("tên bang đã") || lower.Contains("bảo vệ")) Stop("Máy chủ báo: " + text);
	}

	public void OpenPanel()
	{
		if (GameCanvas.panel != null && GameCanvas.panel.isShow) GameCanvas.panel.hideNow();
		AutoGop.gI().isShowWindow = false;
		AutoMuaNhieu.gI().isShowWindow = false;
		NpcMenuHelper.gI().isShowWindow = false;
		AutoDungNhanh.gI().isShowWindow = false;
		AutoGiaoDich.gI().isShowWindow = false;
		AutoPhaLe.gI().isShowWindow = false;
		isShowWindow = true;
		delayPage = false;
		EnsureWindowPosition();
	}

	public void ToggleWindow() { if (isShowWindow) isShowWindow = false; else OpenPanel(); }

	private void EnsureWindowPosition()
	{
		if (winX < 0 || winY < 0) { winX = System.Math.Max(0, (GameCanvas.w - winW) / 2); winY = System.Math.Max(0, (GameCanvas.h - winH) / 2); }
		winX = System.Math.Max(0, System.Math.Min(winX, GameCanvas.w - winW));
		winY = System.Math.Max(0, System.Math.Min(winY, GameCanvas.h - winH));
	}

	private void UpdateTouch()
	{
		EnsureWindowPosition();
		if (ChatTextField.gI().isShow || GameCanvas.menu.showMenu) return;
		if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(winX + winW - 23, winY + 4, 18, 19)) { isShowWindow = false; GameCanvas.clearAllPointerEvent(); return; }
		if (GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(winX, winY, winW - 25, 27))
		{
			if (!dragging) { dragging = true; dragX = GameCanvas.px - winX; dragY = GameCanvas.py - winY; }
			else { winX = GameCanvas.px - dragX; winY = GameCanvas.py - dragY; EnsureWindowPosition(); }
			GameCanvas.isPointerJustDown = false;
			return;
		}
		if (!GameCanvas.isPointerDown) dragging = false;
		if (!GameCanvas.isPointerClick || !GameCanvas.isPointerJustRelease) return;
		if (!GameCanvas.isPointerHoldIn(winX, winY, winW, winH)) return;
		int row = (GameCanvas.py - (winY + 38)) / 25;
		if (GameCanvas.py >= winY + 38 && GameCanvas.py < winY + 238)
		{
			if (delayPage) { if (row >= 0 && row < 6) OpenInput(11 + row); }
			else
			{
				switch (row)
				{
				case 0: OpenInput(2); break;
				case 1: OpenFlags(); break;
				case 2: OpenInput(4); break;
				case 3: OpenInput(6); break;
				case 4: OpenDefaultSkillMenu(); break;
				case 5: OpenBossSkillMenu(); break;
				case 6: delayPage = true; break;
				case 7: GameScr.info1.addInfo("Doanh trại: " + detail, 0); break;
				}
			}
		}
		else if (GameCanvas.py >= winY + 241 && GameCanvas.py < winY + 268)
		{
			if (delayPage) delayPage = false;
			else if (GameCanvas.px < winX + winW / 2) { if (isRunning) Stop("Đã dừng thủ công"); else Start(); }
			else if (resumeAvailable && !isRunning) Resume();
		}
		else if (GameCanvas.py >= winY + 269 && !delayPage && !isRunning && resumeAvailable) ClearCheckpoint();
		GameCanvas.clearAllPointerEvent();
	}

	private static string Short(string value, int max) => value == null ? "" : (value.Length > max ? value.Substring(0, max - 2) + ".." : value);

	public void Paint(mGraphics g)
	{
		if (!isShowWindow) return;
		EnsureWindowPosition();
		g.setColor(0x160A03, 0.94f); g.fillRect(winX - 2, winY - 2, winW + 4, winH + 4, 6);
		g.setColor(0xD49A2A); g.fillRect(winX, winY, winW, winH, 5);
		g.setColor(0x3B1F0E); g.fillRect(winX + 3, winY + 3, winW - 6, winH - 6, 4);
		g.setColor(0x693414); g.fillRect(winX + 4, winY + 4, winW - 8, 24, 3);
		g.setColor(0xF7CE68); g.drawRect(winX + 1, winY + 1, winW - 3, winH - 3);
		mFont.tahoma_7b_yellow.drawString(g, delayPage ? "AUTO DOANH TRẠI - DELAY" : "AUTO DOANH TRẠI [U]", winX + winW / 2, winY + 7, mFont.CENTER);
		g.setColor(0xB71C1C); g.fillRect(winX + winW - 23, winY + 5, 17, 17, 3);
		mFont.tahoma_7b_white.drawString(g, "X", winX + winW - 15, winY + 6, mFont.CENTER);
		string[] labels = delayPage ? new string[] { "Bay map", "Menu NPC", "Đánh quái", "Qua map", "Bang hội", "Lượt tiếp" } : new string[] { "Tên bang", "Cờ bang", "Số lượt", "Map boss", "Skill mặc định", "Skill boss thứ 3", "Delay từng bước", "Trạng thái" };
		string[] values = delayPage ? new string[] { delayTravelMs + " ms", delayMenuMs + " ms", delayFightMs + " ms", delayRoomMs + " ms", delayClanMs + " ms", delayNextRunMs + " ms" } : new string[] { Short(clanName, 13), flagId.ToString(), runLimit == 0 ? "Vô hạn" : runLimit.ToString(), gateRoomId.ToString(), Short(GetDefaultSkillName(), 12), Short(GetGateBossSkillName(), 12), ">", Short(detail, 15) };
		for (int i = 0; i < labels.Length; i++)
		{
			int y = winY + 38 + i * 25;
			g.setColor(i % 2 == 0 ? 0x472511 : 0x573016); g.fillRect(winX + 8, y, winW - 16, 23, 3);
			mFont.tahoma_7b_white.drawString(g, labels[i], winX + 14, y + 5, mFont.LEFT);
			mFont.tahoma_7b_yellow.drawString(g, values[i], winX + winW - 13, y + 5, mFont.RIGHT);
		}
		g.setColor(0x7C4A19); g.fillRect(winX + 8, winY + 239, winW - 16, 27, 3);
		if (delayPage) mFont.tahoma_7b_yellow.drawString(g, "< Quay lại", winX + winW / 2, winY + 245, mFont.CENTER);
		else
		{
			mFont.tahoma_7b_yellow.drawString(g, isRunning ? "DỪNG" : "BẮT ĐẦU", winX + winW / 4, winY + 245, mFont.CENTER);
			mFont.tahoma_7b_yellow.drawString(g, resumeAvailable && !isRunning ? "TIẾP TỤC" : "-", winX + winW * 3 / 4, winY + 245, mFont.CENTER);
		}
		mFont.tahoma_7_white.drawString(g, delayPage ? "Chạm vào từng bước để chỉnh" : (resumeAvailable && !isRunning ? "Chạm đây để xóa tiến độ đã lưu" : "Lượt đã xong: " + completedRuns + " | Map: " + TileMap.mapID), winX + winW / 2, winY + 272, mFont.CENTER);
	}

	private void OpenDelayMenu()
	{
		MyVector options = new MyVector();
		options.addElement(new Command("Bay map: " + delayTravelMs + "ms", this, 11, null));
		options.addElement(new Command("Menu NPC: " + delayMenuMs + "ms", this, 12, null));
		options.addElement(new Command("Đánh quái: " + delayFightMs + "ms", this, 13, null));
		options.addElement(new Command("Qua map: " + delayRoomMs + "ms", this, 14, null));
		options.addElement(new Command("Giải tán/lập bang: " + delayClanMs + "ms", this, 15, null));
		options.addElement(new Command("Lượt tiếp theo: " + delayNextRunMs + "ms", this, 16, null));
		GameCanvas.menu.startAt(options, 4);
	}

	private void OpenFlags()
	{
		MyVector options = new MyVector();
		for (int i = 0; i < ClanImage.vClanImage.size(); i++)
		{
			ClanImage flag = (ClanImage)ClanImage.vClanImage.elementAt(i);
			if (flag != null) options.addElement(new Command(flag.name + " [" + flag.ID + "]", this, 30, flag.ID));
		}
		if (options.size() == 0) { Service.gI().getClan(1, -1, null); GameScr.info1.addInfo("Đang tải cờ bang, hãy mở lại mục Chọn cờ", 0); return; }
		GameCanvas.menu.startAt(options, 4);
	}

	private static string GetSkillName(int skillId, string fallback)
	{
		if (skillId < 0) return fallback;
		Char me = Char.myCharz();
		if (me != null && me.vSkill != null)
		{
			for (int i = 0; i < me.vSkill.size(); i++)
			{
				Skill skill = (Skill)me.vSkill.elementAt(i);
				if (skill != null && skill.template != null && skill.template.id == skillId) return skill.template.name;
			}
		}
		return "ID " + skillId;
	}

	private void OpenSkillMenu(int action, string fallbackLabel)
	{
		MyVector options = new MyVector();
		options.addElement(new Command(fallbackLabel, this, action, -1));
		Char me = Char.myCharz();
		if (me != null && me.vSkill != null)
		{
			for (int i = 0; i < me.vSkill.size(); i++)
			{
				Skill skill = (Skill)me.vSkill.elementAt(i);
				if (skill != null && skill.template != null && skill.template.isAttackSkill()) options.addElement(new Command(skill.template.name + " [" + skill.template.id + "]", this, action, (int)skill.template.id));
			}
		}
		GameCanvas.menu.startAt(options, 4);
	}

	private string GetDefaultSkillName() => GetSkillName(defaultSkillId, "Tự động");
	private void OpenDefaultSkillMenu() => OpenSkillMenu(32, "Tự động");
	private string GetGateBossSkillName() => GetSkillName(gateBossSkillId, "Mặc định");
	private void OpenBossSkillMenu() => OpenSkillMenu(31, "Mặc định");

	public void perform(int action, object value)
	{
		if (action == 1) { if (isRunning) Stop("Đã dừng thủ công"); else Start(); return; }
		if (action == 3) { OpenFlags(); return; }
		if (action == 5) { OpenDelayMenu(); return; }
		if (action == 7) { GameScr.info1.addInfo("Doanh trại: " + detail, 0); return; }
		if (action == 8) { Resume(); return; }
		if (action == 9) { ClearCheckpoint(); return; }
		if (action == 30) { flagId = Convert.ToInt32(value); return; }
		if (action == 31) { gateBossSkillId = Convert.ToInt32(value); AutoSettingsStore.SaveIfChanged(this, "Game1.AutoDoanhTrai", ref settingsSnapshot); return; }
		if (action == 32) { defaultSkillId = Convert.ToInt32(value); AutoSettingsStore.SaveIfChanged(this, "Game1.AutoDoanhTrai", ref settingsSnapshot); return; }
		if (action == 2 || action == 4 || action == 6 || (action >= 11 && action <= 16)) OpenInput(action);
	}

	private void OpenInput(int field)
	{
		editField = field;
		ChatTextField input = ChatTextField.gI();
		input.strChat = "Cài Auto Doanh Trại";
		input.tfChat.name = field == 2 ? "Nhập tên bang" : "Nhập số";
		input.to = string.Empty;
		input.isShow = true;
		input.tfChat.isFocus = true;
		input.tfChat.setIputType(field == 2 ? TField.INPUT_TYPE_ANY : TField.INPUT_TYPE_NUMERIC);
		input.parentScreen = this;
		if (!Main.isPC) input.startChat(this, string.Empty);
		else if (GameCanvas.isTouch) input.tfChat.doChangeToTextBox();
	}

	public void onChatFromMe(string text, string to)
	{
		if (editField == 2)
		{
			string value = (text ?? "").Trim();
			if (value.Length >= 3 && value.Length <= 30) clanName = value;
			else GameScr.info1.addInfo("Tên bang phải dài 3-30 ký tự", 0);
		}
		else if (int.TryParse(text, out int value))
		{
			switch (editField)
			{
			case 4: if (value >= 0 && value <= 10000) runLimit = value; break;
			case 6: if (Array.IndexOf(Rooms, value) >= 0) gateRoomId = value; break;
			case 11: if (ValidDelay(value)) delayTravelMs = value; break;
			case 12: if (ValidDelay(value)) delayMenuMs = value; break;
			case 13: if (ValidDelay(value)) delayFightMs = value; break;
			case 14: if (ValidDelay(value)) delayRoomMs = value; break;
			case 15: if (ValidDelay(value)) delayClanMs = value; break;
			case 16: if (ValidDelay(value)) delayNextRunMs = value; break;
			}
		}
		else GameScr.info1.addInfo("Giá trị không hợp lệ", 0);
		onCancelChat();
	}

	private static bool ValidDelay(int value) => value >= 100 && value <= 30000;
	public void onCancelChat() { editField = 0; AutoItem.ResetTF(ChatTextField.gI()); }
}

