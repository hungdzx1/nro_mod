using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game1.Mod;

public class AutoGop : IChatable
{
	private static AutoGop _instance;

	public bool isShowWindow;

	public int selectedTab; // 0: Chung, 1: Bánh, 2: HN, 3: Điểm, 4: Zeno

	public bool isRunning; // Đang chạy bất kỳ tiến trình nào

	public bool isRunningWorkflow; // Đang chạy toàn bộ quy trình (Tab Chung - F3)

	public int currentRunningTab; // Tab đang thực thi: 1 (Bánh), 2 (HN), 3 (Điểm), 4 (Zeno)

	public bool hideUi = true; // Ẩn hoàn toàn giao diện NPC / ô nhập khi chạy

	// === TAB 0: CHUNG ===
	public bool enableBanh = true;

	public int orderBanh = 1;

	public bool enableHn = true;

	public int orderHn = 4;

	public bool enableDiem = true;

	public int orderDiem = 2;

	public bool enableZeno = true;

	public int orderZeno = 3;

	public bool autoChatMuaNhieu = true; // Checkbox [v] Bật Mua Nhiều (muanhieu)

	public bool serverMuaNhieuEnabled;

	public int delayBetweenTabsMs = 500;

	public int targetWorkflowLoops = 0; // 0 = vô hạn

	public int completedWorkflowLoops;

	private bool progressThisLoop;

	private int routeHopCount;

	private List<int> workflowQueue = new List<int>();

	private int queueIndex;

	// === TAB 1: BÁNH (Item ID: 1337, NPC ID: 92) ===
	public int targetItemIdBanh = 1337;

	public string targetItemNameBanh = "Bánh";

	public sbyte targetBuyTypeBanh = 3;

	public int batchQtyBanh = 1000;

	public int targetBatchesBanh = 50; // 0 = vô hạn

	public int delayBuyBanhMs = 600;

	public int stopConditionBanh = 2; // 1: Đủ số lần HOẶC hết tài nguyên/hành trang đầy. 2: Số bánh trong rương đạt mốc

	public int targetItemCountBanh = 2000000; // Mốc số bánh trong rương cho Cách 2

	public int delayUseBanhMs = 200; // Delay khi dùng bánh

	public int completedBatchesBanh;

	private bool isUsingAllBanh;

	// === TAB 2: HN (Item ID: 1581, NPC ID: 56) ===
	public int targetItemIdHn = 1581;

	public string targetItemNameHn = "HN";

	public sbyte targetBuyTypeHn = 3;

	public int batchQtyHn = 1000;

	public int targetBatchesHn = 0; // 0 = vô hạn

	public int delayBuyHnMs = 600;

	public bool autoUseHn = true; // Vừa mua vừa dùng vật phẩm

	public int stopConditionHn = 1; // 1: Đạt đủ số lượng. 2: Hết tài nguyên / hành trang đầy

	public bool enableRoutingHn = true; // Bật tính năng chuyển hướng cục bộ

	public int routeWhenConditionHn = 2; // Rẽ nhánh khi thỏa mãn ĐK 2 (Hết tài nguyên / đầy)

	public int routeTargetTabHn = 4; // Nhảy sang Tab Zeno (ID 4)

	public int routeDelayHnMs = 500; // Delay khi chuyển từ HN sang Zeno

	public int completedBatchesHn;

	// === TAB 3: ĐIỂM (NPC ID: 66) ===
	public int pointIndex = 0; // 0: 10Tr, 1: 5K, 2: 3K, 3: 200, 4: 50

	public int targetBatchesDiem = 1; // 0 = vô hạn

	public int delayDiemMs = 350;

	public int stopConditionDiem = 1; // 1: Đổi đủ số lượng. 2: Điểm hiện có < Điểm tối thiểu

	public long currentDiemPoints = -1; // Điểm hiện có cập nhật Real-time từ lời thoại Menu 1 NPC 66

	public int completedBatchesDiem;

	public static readonly int[] RequiredPoints = new int[5] { 10000000, 5000, 3000, 200, 50 };

	public static readonly string[] MocShortNames = new string[5] { "10Tr", "5K", "3K", "200", "50" };

	public static readonly string[] MocFullNames = new string[5] { "10.000.000đ", "5.000đ", "3.000đ", "200đ", "50đ" };

	// === TAB 4: ZENO (Item ID: 1583, NPC ID: 18) ===
	public int batchQtyZeno = 19800; // Mặc định 19.800

	public int targetBatchesZeno = 1; // Mặc định 1

	public int delayZenoMs = 350;

	public int routeTargetWhenDoneZeno = 2; // Nhánh 1: Đổi đủ mục tiêu -> Nhảy sang Tab HN (2)

	public int routeTargetWhenLackZeno = 1; // Nhánh 2: Thiếu vé (< 19.800) -> Nhảy sang Tab Bánh (1)

	public int routeDelayZenoMs = 500; // Delay khi chuyển từ Zeno sang Bánh/HN

	public int completedBatchesZeno;

	// === ENGINE STATE ===
	private int stepState; // 0: Idle, 1: Approach & Open, 2: WaitMenu, 3: WaitShop, 4: WaitInput, 5: Cooldown, 6: TransitionDelay, 7: LocalRouteDelay

	private int pendingRouteTab;

	private int pendingRouteDelayMs;

	private long stateTimestamp;

	public long lastStopTimeMs;

	private int retryCount;

	private int editFieldMode;

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 224;

	private readonly int winH = 258;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	public static AutoGop gI()
	{
		if (_instance == null)
		{
			_instance = new AutoGop();
		}
		return _instance;
	}

	private void EnsureWindowPosition()
	{
		if (winX < 0 || winY < 0 || winX > GameCanvas.w - 40 || winY > GameCanvas.h - 40)
		{
			winX = System.Math.Max(4, GameCanvas.w - winW - 54);
			winY = System.Math.Max(6, System.Math.Min(GameCanvas.h - winH - 6, (GameCanvas.h - winH) / 2 + 10));
		}
	}

	public void ToggleWindow(int tab = -1)
	{
		EnsureWindowPosition();
		if (tab >= 0 && tab <= 4)
		{
			selectedTab = tab;
			isShowWindow = true;
		}
		else
		{
			isShowWindow = !isShowWindow;
		}
		if (isShowWindow)
		{
			AutoMuaNhieu.gI().isShowWindow = false;
			NpcMenuHelper.gI().isShowWindow = false;
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
		}
	}

	// === TIỆN ÍCH HÀNH TRANG & NPC ===
	public static int GetBagItemCount(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			Item item = Char.myCharz().arrItemBag[i];
			if (item != null && item.template != null && item.template.id == itemId)
			{
				count += System.Math.Max(1, item.quantity);
			}
		}
		return count;
	}

	public static bool UseItemInBag(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return false;
		}
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			Item item = Char.myCharz().arrItemBag[i];
			if (item != null && item.template != null && item.template.id == itemId)
			{
				Service.gI().useItem(0, 1, (sbyte)i, -1);
				return true;
			}
		}
		return false;
	}

	public static void MoveCloseToNpc(int npcTempId)
	{
		Npc npc = ModFunc.GetNpcByTempId(npcTempId);
		if (npc != null)
		{
			if (System.Math.Abs(npc.cx - Char.myCharz().cx) > 40 || System.Math.Abs(npc.cy - Char.myCharz().cy) > 40)
			{
				Char.myCharz().cx = npc.cx + 20;
				Char.myCharz().cy = npc.cy;
				Service.gI().charMove();
			}
			Char.myCharz().npcFocus = npc;
		}
	}

	public static string GetTabName(int tabId)
	{
		return tabId switch
		{
			0 => "Chung",
			1 => "Bánh",
			2 => "HN",
			3 => "Điểm",
			4 => "Zeno",
			_ => "Tab " + tabId,
		};
	}

	public void SetTargetItemBanh(Item item)
	{
		if (item != null && item.template != null)
		{
			targetItemIdBanh = item.template.id;
			targetItemNameBanh = item.template.name;
			sbyte buyType = 3;
			if (!item.isBuySpec)
			{
				if (item.buyCoin > 0)
				{
					buyType = 0;
				}
				else if (item.buyGold > 0)
				{
					buyType = 1;
				}
			}
			targetBuyTypeBanh = buyType;
			GameScr.info1.addInfo("Đã chọn cho [Tab Bánh]: " + targetItemNameBanh + " [ID: " + targetItemIdBanh + "]", 0);
		}
	}

	public void SetTargetItemHn(Item item)
	{
		if (item != null && item.template != null)
		{
			targetItemIdHn = item.template.id;
			targetItemNameHn = item.template.name;
			sbyte buyType = 3;
			if (!item.isBuySpec)
			{
				if (item.buyCoin > 0)
				{
					buyType = 0;
				}
				else if (item.buyGold > 0)
				{
					buyType = 1;
				}
			}
			targetBuyTypeHn = buyType;
			GameScr.info1.addInfo("Đã chọn cho [Tab HN]: " + targetItemNameHn + " [ID: " + targetItemIdHn + "]", 0);
		}
	}

	public Item FindShopItem(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemShop == null)
		{
			return null;
		}
		for (int t = 0; t < Char.myCharz().arrItemShop.Length; t++)
		{
			if (Char.myCharz().arrItemShop[t] == null)
			{
				continue;
			}
			for (int i = 0; i < Char.myCharz().arrItemShop[t].Length; i++)
			{
				Item it = Char.myCharz().arrItemShop[t][i];
				if (it != null && it.template != null && it.template.id == itemId)
				{
					return it;
				}
			}
		}
		return null;
	}

	public void OnShopOpened(sbyte typeShop)
	{
		if (!isRunning)
		{
			return;
		}
		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				GameCanvas.panel2.hideNow();
			}
		}

		if (currentRunningTab == 1 || currentRunningTab == 2)
		{
			TriggerBuyCurrentTab();
		}
	}

	public void TriggerBuyCurrentTab()
	{
		if (!isRunning)
		{
			return;
		}
		int targetId = (currentRunningTab == 1) ? targetItemIdBanh : targetItemIdHn;
		sbyte buyType = (currentRunningTab == 1) ? targetBuyTypeBanh : targetBuyTypeHn;
		Item shopItem = FindShopItem(targetId);
		if (shopItem != null && shopItem.template != null)
		{
			if (currentRunningTab == 1)
			{
				targetItemNameBanh = shopItem.template.name;
				targetBuyTypeBanh = shopItem.isBuySpec ? ((sbyte)3) : ((shopItem.buyCoin > 0) ? ((sbyte)0) : ((sbyte)1));
				buyType = targetBuyTypeBanh;
			}
			else
			{
				targetItemNameHn = shopItem.template.name;
				targetBuyTypeHn = shopItem.isBuySpec ? ((sbyte)3) : ((shopItem.buyCoin > 0) ? ((sbyte)0) : ((sbyte)1));
				buyType = targetBuyTypeHn;
			}
		}

		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				GameCanvas.panel2.hideNow();
			}
		}

		Service.gI().buyItem(buyType, targetId, 0);
		stepState = 4;
		stateTimestamp = mSystem.currentTimeMillis();
	}

	// === ĐIỀU PHỐI WORKFLOW (F3 / F4 / F5 / F7 / F8 / F6) ===
	public void StartWorkflow()
	{
		workflowQueue.Clear();
		var list = new List<KeyValuePair<int, int>>();
		if (enableBanh) list.Add(new KeyValuePair<int, int>(orderBanh, 1));
		if (enableHn) list.Add(new KeyValuePair<int, int>(orderHn, 2));
		if (enableDiem) list.Add(new KeyValuePair<int, int>(orderDiem, 3));
		if (enableZeno) list.Add(new KeyValuePair<int, int>(orderZeno, 4));

		list.Sort((a, b) => a.Key.CompareTo(b.Key));
		foreach (var kvp in list)
		{
			workflowQueue.Add(kvp.Value);
		}

		if (workflowQueue.Count == 0)
		{
			GameScr.info1.addInfo("Chưa chọn chức năng nào trong Tab Chung!", 0);
			return;
		}

		isRunningWorkflow = true;
		completedWorkflowLoops = 0;
		progressThisLoop = false;
		routeHopCount = 0;
		queueIndex = 0;
		string loopDesc = (targetWorkflowLoops == 0) ? "Vô hạn" : (targetWorkflowLoops + " vòng");
		GameScr.info1.addInfo("▶ BẮT ĐẦU QUY TRÌNH AUTO GỘP (" + loopDesc + ")", 0);
		StartTab(workflowQueue[0]);
	}

	public void StartStandaloneTab(int tabId)
	{
		isRunningWorkflow = false;
		routeHopCount = 0;
		StartTab(tabId);
	}

	private void MarkLoopProgress()
	{
		progressThisLoop = true;
		routeHopCount = 0;
	}

	private void FinishOneWorkflowLoop()
	{
		completedWorkflowLoops++;
		if (!progressThisLoop)
		{
			StopAll("Dừng quy trình: Cả vòng không mua/đổi được gì (hết tài nguyên)");
			return;
		}
		progressThisLoop = false;
		routeHopCount = 0;

		if (targetWorkflowLoops == 0 || completedWorkflowLoops < targetWorkflowLoops)
		{
			queueIndex = 0;
			string loopInfo = (targetWorkflowLoops == 0)
				? (completedWorkflowLoops + "/Vô hạn")
				: (completedWorkflowLoops + "/" + targetWorkflowLoops);
			GameScr.info1.addInfo("Xong vòng " + loopInfo + " -> Chờ " + delayBetweenTabsMs + "ms -> Lặp lại Tab " + GetTabName(workflowQueue[0]), 0);
			stepState = 6;
			stateTimestamp = mSystem.currentTimeMillis();
		}
		else
		{
			StopAll("ĐÃ HOÀN THÀNH " + completedWorkflowLoops + "/" + targetWorkflowLoops + " VÒNG QUY TRÌNH AUTO GỘP!");
		}
	}

	private void HandleLocalRoute(int targetTab, string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			GameScr.info1.addInfo(message, 0);
		}

		if (targetTab == currentRunningTab)
		{
			OnTabCompleted(currentRunningTab, "Đích rẽ nhánh trùng tab hiện tại");
			return;
		}

		if (isRunningWorkflow && workflowQueue.Count > 0)
		{
			// Nếu rẽ nhánh quay về tab đầu quy trình sau khi đã qua tab khác -> hoàn tất 1 vòng
			if (workflowQueue.Count > 1 && targetTab == workflowQueue[0] && currentRunningTab != workflowQueue[0])
			{
				FinishOneWorkflowLoop();
				return;
			}

			routeHopCount++;
			if (routeHopCount > 8)
			{
				if (!progressThisLoop)
				{
					StopAll("Dừng quy trình: Rẽ nhánh lặp lại không phát sinh giao dịch");
					return;
				}
				FinishOneWorkflowLoop();
				return;
			}

			int idx = workflowQueue.IndexOf(targetTab);
			if (idx >= 0)
			{
				queueIndex = idx;
			}
			ScheduleLocalRoute(targetTab, currentRunningTab);
		}
		else
		{
			routeHopCount++;
			if (routeHopCount > 8)
			{
				StopAll("Dừng: Rẽ nhánh lặp quá số lần cho phép");
				return;
			}
			ScheduleLocalRoute(targetTab, currentRunningTab);
		}
	}

	private void ScheduleLocalRoute(int targetTab, int sourceTab)
	{
		int delayToUse = (sourceTab == 2) ? routeDelayHnMs : ((sourceTab == 4) ? routeDelayZenoMs : delayBetweenTabsMs);
		pendingRouteTab = targetTab;
		pendingRouteDelayMs = delayToUse;
		currentRunningTab = 0;
		stepState = 7;
		stateTimestamp = mSystem.currentTimeMillis();
	}

	public void StartTab(int tabId)
	{
		currentRunningTab = tabId;
		isRunning = true;
		stepState = 1;
		stateTimestamp = mSystem.currentTimeMillis();
		retryCount = 0;

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;
		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				GameCanvas.panel2.hideNow();
			}
		}

		if ((tabId == 1 || tabId == 2) && autoChatMuaNhieu && !serverMuaNhieuEnabled)
		{
			Service.gI().chat("muanhieu");
			serverMuaNhieuEnabled = true;
		}

		switch (tabId)
		{
		case 1:
			completedBatchesBanh = 0;
			isUsingAllBanh = false;
			GameScr.info1.addInfo("Bắt đầu [Tab Bánh]: Mua " + targetItemNameBanh + " [" + targetItemIdBanh + "] tại Goku (92)", 0);
			MoveCloseToNpc(92);
			Service.gI().openMenu(92);
			stepState = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 2:
			completedBatchesHn = 0;
			GameScr.info1.addInfo("Bắt đầu [Tab HN]: Mua " + targetItemNameHn + " [" + targetItemIdHn + "] tại NPC 56", 0);
			MoveCloseToNpc(56);
			Service.gI().openMenu(56);
			stepState = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 3:
			completedBatchesDiem = 0;
			GameScr.info1.addInfo("Bắt đầu [Tab Điểm]: Đổi Nồi Bánh mốc " + MocShortNames[pointIndex], 0);
			MoveCloseToNpc(66);
			Service.gI().openMenu(66);
			stepState = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 4:
			completedBatchesZeno = 0;
			// Kiểm tra điều kiện Nhánh 2 (Thiếu vé) ngay từ đầu
			int zenoCount = GetBagItemCount(1583);
			if (zenoCount < batchQtyZeno)
			{
				HandleLocalRoute(routeTargetWhenLackZeno, "Vé Zeno (" + zenoCount + ") < " + batchQtyZeno + " -> Rẽ nhánh sang Tab " + GetTabName(routeTargetWhenLackZeno));
				return;
			}
			GameScr.info1.addInfo("Bắt đầu [Tab Zeno]: Đổi " + batchQtyZeno + " Tiên Phù tại Karin (18)", 0);
			MoveCloseToNpc(18);
			Service.gI().openMenu(18);
			stepState = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			break;
		}
	}

	public void OnTabCompleted(int tabId, string reason)
	{
		GameScr.info1.addInfo("Hoàn tất Tab " + GetTabName(tabId) + ": " + reason, 0);

		if (!isRunningWorkflow)
		{
			StopAll("Đã hoàn tất Tab " + GetTabName(tabId));
			return;
		}

		int idxInQueue = workflowQueue.IndexOf(tabId);
		if (idxInQueue >= 0)
		{
			queueIndex = idxInQueue + 1;
		}
		else
		{
			queueIndex++;
		}

		if (queueIndex < workflowQueue.Count)
		{
			int nextTab = workflowQueue[queueIndex];
			GameScr.info1.addInfo("Chờ " + delayBetweenTabsMs + "ms -> Chuyển sang Tab " + GetTabName(nextTab), 0);
			stepState = 6; // Chờ delay chuyển giao
			stateTimestamp = mSystem.currentTimeMillis();
		}
		else
		{
			FinishOneWorkflowLoop();
		}
	}

	public void StopAll(string reason = "Đã dừng tất cả (F6)")
	{
		isRunning = false;
		isRunningWorkflow = false;
		currentRunningTab = 0;
		stepState = 0;
		pendingRouteTab = 0;
		pendingRouteDelayMs = 0;
		isUsingAllBanh = false;
		progressThisLoop = false;
		routeHopCount = 0;
		lastStopTimeMs = mSystem.currentTimeMillis();
		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;
		if (!string.IsNullOrEmpty(reason))
		{
			GameScr.info1.addInfo(reason, 0);
		}
	}

	// === TRÍCH XUẤT ĐIỂM REAL-TIME TỪ LỜI THOẠI MENU 1 CỦA NPC 66 ===
	public void ParseDiemPoints(string text, int npcId = 66, string[] menu = null)
	{
		if (npcId != 66 || string.IsNullOrEmpty(text))
		{
			return;
		}

		// Nếu có danh sách menu đi kèm, chỉ parse Menu cấp 1 (có "đổi điểm") và tuyệt đối bỏ qua Menu cấp 2 (chứa các mốc 10.000.000, 5.000, 3.000...)
		if (menu != null)
		{
			bool isMenu1 = false;
			for (int i = 0; i < menu.Length; i++)
			{
				if (string.IsNullOrEmpty(menu[i]))
				{
					continue;
				}
				string m = menu[i].ToLower();
				if (m.Contains("10.000.000") || m.Contains("10,000,000") || m.Contains("10tr") || m.Contains("5.000") || m.Contains("5,000") || m.Contains("3.000") || m.Contains("3,000"))
				{
					return;
				}
				if (m.Contains("đổi điểm") || m.Contains("doi diem"))
				{
					isMenu1 = true;
				}
			}
			if (!isMenu1)
			{
				return;
			}
		}

		try
		{
			string cleanText = Regex.Replace(text, @"(?m)^\|-?\d+\|(?:-?\d+\|)?", "");
			// Ưu tiên cụm từ chỉ rõ số dư điểm hiện có của người chơi
			Match match = Regex.Match(cleanText, @"(?:đang có|hiện có|hiện tại|bạn có|số điểm|điểm của bạn|điểm sự kiện|điểm tích lũy|điểm bánh|điểm hiện có)(?:\s*(?:là|:))*\s*([0-9.,]+)", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				string lowerText = cleanText.ToLower();
				if ((lowerText.Contains("10.000.000") || lowerText.Contains("10,000,000")) &&
					(lowerText.Contains("5.000") || lowerText.Contains("5,000") || lowerText.Contains("3.000") || lowerText.Contains("3,000")))
				{
					return;
				}
				match = Regex.Match(cleanText, @"([0-9.,]+)\s*(?:điểm|diem)", RegexOptions.IgnoreCase);
			}
			if (!match.Success)
			{
				match = Regex.Match(cleanText, @"(?:điểm|diem|có|co)(?:\s*(?:là|:))*\s*([0-9.,]+)", RegexOptions.IgnoreCase);
			}
			if (match.Success)
			{
				string numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "").Trim();
				if (long.TryParse(numStr, out var pt) && pt >= 0)
				{
					currentDiemPoints = pt;
				}
			}
		}
		catch
		{
		}
	}

	public bool OnReceiveMenu(string[] menu, Npc npc, string chatText = "")
	{
		if (npc == null || npc.template == null || menu == null)
		{
			return false;
		}

		int npcTempId = npc.template.npcTemplateId;

		// Trích xuất điểm Real-time nếu là Nồi Bánh (66)
		if (npcTempId == 66 && !string.IsNullOrEmpty(chatText))
		{
			ParseDiemPoints(chatText, npcTempId, menu);
		}

		if (!isRunning)
		{
			return false;
		}

		// === TAB 1: BÁNH (NPC 92) ===
		if (currentRunningTab == 1 && npcTempId == 92)
		{
			// Mở Cửa hàng Sự Kiện (thường là option 0)
			for (int i = 0; i < menu.Length; i++)
			{
				if (menu[i].ToLower().Contains("cửa hàng") || menu[i].ToLower().Contains("shop"))
				{
					Service.gI().confirmMenu(92, (sbyte)i);
					stepState = 3;
					stateTimestamp = mSystem.currentTimeMillis();
					return hideUi;
				}
			}
			Service.gI().confirmMenu(92, 0);
			stepState = 3;
			stateTimestamp = mSystem.currentTimeMillis();
			return hideUi;
		}

		// === TAB 2: HN (NPC 56) ===
		if (currentRunningTab == 2 && npcTempId == 56)
		{
			for (int j = 0; j < menu.Length; j++)
			{
				if (menu[j].ToLower().Contains("cửa hàng") || menu[j].ToLower().Contains("shop") || menu[j].ToLower().Contains("mua"))
				{
					Service.gI().confirmMenu(56, (sbyte)j);
					stepState = 3;
					stateTimestamp = mSystem.currentTimeMillis();
					return hideUi;
				}
			}
			Service.gI().confirmMenu(56, 0);
			stepState = 3;
			stateTimestamp = mSystem.currentTimeMillis();
			return hideUi;
		}

		// === TAB 3: ĐIỂM (NPC 66) ===
		if (currentRunningTab == 3 && npcTempId == 66)
		{
			bool isMenu1 = false;
			for (int k = 0; k < menu.Length; k++)
			{
				if (menu[k].ToLower().Contains("đổi điểm"))
				{
					isMenu1 = true;
					break;
				}
			}

			if (isMenu1)
			{
				stepState = 3;
				stateTimestamp = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(66, 2);
				return hideUi;
			}
			else
			{
				// Kiểm tra điều kiện Cách 2: Điểm hiện có < Điểm tối thiểu mốc chọn
				if (stopConditionDiem == 2 && currentDiemPoints >= 0 && currentDiemPoints < RequiredPoints[pointIndex])
				{
					OnTabCompleted(3, "Số điểm hiện có (" + NinjaUtil.getMoneys(currentDiemPoints) + ") < Mốc " + MocShortNames[pointIndex]);
					return hideUi;
				}

				int pick = (pointIndex >= 0 && pointIndex < menu.Length) ? pointIndex : 1;
				completedBatchesDiem++;
				MarkLoopProgress();
				Service.gI().confirmMenu(66, (sbyte)pick);

				if (stopConditionDiem == 1 && targetBatchesDiem > 0 && completedBatchesDiem >= targetBatchesDiem)
				{
					OnTabCompleted(3, "Đã đổi đủ " + completedBatchesDiem + " lượt (" + MocShortNames[pick] + ")");
				}
				else
				{
					stepState = 5;
					stateTimestamp = mSystem.currentTimeMillis();
				}
				return hideUi;
			}
		}

		// === TAB 4: ZENO (NPC 18) ===
		if (currentRunningTab == 4 && npcTempId == 18)
		{
			bool isMenu1 = false;
			for (int l = 0; l < menu.Length; l++)
			{
				if (menu[l].ToLower().Contains("đổi tiên phù"))
				{
					isMenu1 = true;
					break;
				}
			}

			if (isMenu1)
			{
				stepState = 3;
				stateTimestamp = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(18, 3);
				return hideUi;
			}
			else
			{
				stepState = 4;
				stateTimestamp = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(18, 0);
				return hideUi;
			}
		}

		return false;
	}

	public bool OnReceiveMenu(string[] menu, Npc npc)
	{
		return OnReceiveMenu(menu, npc, "");
	}

	public bool OnReceiveServerInputPacket(Message msg)
	{
		if (!isRunning)
		{
			return false;
		}

		// Gói -125 ClientInput khi Mua Bánh (1), Mua HN (2), hoặc Đổi Zeno (4)
		if (currentRunningTab == 1 || currentRunningTab == 2 || currentRunningTab == 4)
		{
			try
			{
				ChatTextField.gI().isShow = false;
				string title = msg.reader().readUTF();
				sbyte nFields = msg.reader().readByte();
				for (int i = 0; i < nFields; i++)
				{
					msg.reader().readUTF();
					msg.reader().readByte();
				}

				int qtyToSend = 1000;
				if (currentRunningTab == 1) qtyToSend = batchQtyBanh;
				else if (currentRunningTab == 2) qtyToSend = batchQtyHn;
				else if (currentRunningTab == 4) qtyToSend = batchQtyZeno;

				TField tf = new TField();
				tf.setText(qtyToSend.ToString());
				Service.gI().sendClientInput(new TField[1] { tf });

				if (currentRunningTab == 1)
				{
					completedBatchesBanh++;
					MarkLoopProgress();
					retryCount = 0;
					// Nếu Tab Bánh Cách 2: kiểm tra số lượng bánh trong rương
					if (stopConditionBanh == 2 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
					{
						isUsingAllBanh = true;
						stepState = 5;
						stateTimestamp = mSystem.currentTimeMillis();
						GameScr.info1.addInfo(targetItemNameBanh + " đạt mốc " + targetItemCountBanh + " -> Chuẩn bị dùng hết trong rương...", 0);
					}
					else if (stopConditionBanh == 1 && targetBatchesBanh > 0 && completedBatchesBanh >= targetBatchesBanh)
					{
						OnTabCompleted(1, "Đã mua đủ " + completedBatchesBanh + " đợt " + targetItemNameBanh);
					}
					else
					{
						stepState = 5;
						stateTimestamp = mSystem.currentTimeMillis();
					}
				}
				else if (currentRunningTab == 2)
				{
					completedBatchesHn++;
					MarkLoopProgress();
					retryCount = 0;
					if (autoUseHn)
					{
						UseItemInBag(targetItemIdHn);
					}

					if (stopConditionHn == 1 && targetBatchesHn > 0 && completedBatchesHn >= targetBatchesHn)
					{
						if (enableRoutingHn && routeWhenConditionHn == 1)
						{
							HandleLocalRoute(routeTargetTabHn, "Tab HN thỏa mãn ĐK 1 -> Rẽ nhánh sang Tab " + GetTabName(routeTargetTabHn));
						}
						else
						{
							OnTabCompleted(2, "Đã mua đủ " + completedBatchesHn + " đợt " + targetItemNameHn);
						}
					}
					else
					{
						stepState = 5;
						stateTimestamp = mSystem.currentTimeMillis();
					}
				}
				else if (currentRunningTab == 4)
				{
					completedBatchesZeno++;
					MarkLoopProgress();
					GameScr.info1.addInfo("Đã đổi " + qtyToSend + " Tiên Phù Zeno (Đợt " + completedBatchesZeno + ")", 0);

					// Nhánh 1: Đạt mục tiêu chỉ định
					if (targetBatchesZeno > 0 && completedBatchesZeno >= targetBatchesZeno)
					{
						HandleLocalRoute(routeTargetWhenDoneZeno, "Đã đổi đủ mục tiêu Zeno -> Rẽ nhánh sang Tab " + GetTabName(routeTargetWhenDoneZeno));
					}
					else
					{
						// Kiểm tra lại số vé trong hành trang
						int currentTicket = GetBagItemCount(1583);
						if (currentTicket < batchQtyZeno)
						{
							HandleLocalRoute(routeTargetWhenLackZeno, "Vé Zeno (" + currentTicket + ") < " + batchQtyZeno + " -> Rẽ nhánh sang Tab " + GetTabName(routeTargetWhenLackZeno));
						}
						else
						{
							stepState = 5;
							stateTimestamp = mSystem.currentTimeMillis();
						}
					}
				}

				return hideUi;
			}
			catch (Exception)
			{
				return false;
			}
		}

		return false;
	}

	public void OnServerInfo(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return;
		}

		string lower = s.ToLower();
		if (lower.Contains("bật auto mua số lượng lớn"))
		{
			serverMuaNhieuEnabled = true;
		}
		else if (lower.Contains("tắt auto mua số lượng lớn") || lower.Contains("đã tắt auto mua"))
		{
			serverMuaNhieuEnabled = false;
			if (isRunning && autoChatMuaNhieu)
			{
				Service.gI().chat("muanhieu");
				serverMuaNhieuEnabled = true;
			}
		}

		if (!isRunning)
		{
			return;
		}

		bool isResourceEmptyOrBagFull = lower.Contains("hành trang đã đầy") || lower.Contains("chỗ trống") || lower.Contains("không đủ") || lower.Contains("còn thiếu") || lower.Contains("hết điểm") || lower.Contains("hết vé");

		if (isResourceEmptyOrBagFull)
		{
			if (currentRunningTab == 1 && stopConditionBanh == 1)
			{
				OnTabCompleted(1, "Hết tài nguyên hoặc hành trang đầy (" + s + ")");
			}
			else if (currentRunningTab == 2)
			{
				// Tab HN: Điều kiện 2 (Hết tài nguyên / Đầy rương)
				if (enableRoutingHn && routeWhenConditionHn == 2)
				{
					HandleLocalRoute(routeTargetTabHn, "Tab HN gặp lỗi/đầy rương -> Rẽ nhánh ưu tiên sang Tab " + GetTabName(routeTargetTabHn));
				}
				else if (stopConditionHn == 2)
				{
					OnTabCompleted(2, "Hết tài nguyên hoặc đầy rương (" + s + ")");
				}
				else
				{
					StopAll("Dừng Tab HN: " + s);
				}
			}
			else if (currentRunningTab == 3)
			{
				OnTabCompleted(3, "Dừng Tab Điểm: " + s);
			}
			else if (currentRunningTab == 4)
			{
				// Tab Zeno hết vé -> Kích hoạt Nhánh 2
				HandleLocalRoute(routeTargetWhenLackZeno, "Hết vé Zeno -> Rẽ nhánh sang Tab " + GetTabName(routeTargetWhenLackZeno));
			}
		}
	}

	public void Update()
	{
		// Lắng nghe phím tắt toàn hệ thống
		if (!ChatTextField.gI().isShow)
		{
			if (Input.GetKeyDown(KeyCode.F3))
			{
				StartWorkflow();
			}
			else if (Input.GetKeyDown(KeyCode.F4))
			{
				StartStandaloneTab(1);
			}
			else if (Input.GetKeyDown(KeyCode.F5))
			{
				StartStandaloneTab(2);
			}
			else if (Input.GetKeyDown(KeyCode.F6) || (isRunning && Input.GetKey(KeyCode.A)))
			{
				StopAll("Đã dừng tất cả (F6 / Phím A)");
				return;
			}
			else if (Input.GetKeyDown(KeyCode.F7))
			{
				StartStandaloneTab(3);
			}
			else if (Input.GetKeyDown(KeyCode.F8))
			{
				StartStandaloneTab(4);
			}
		}

		if (isShowWindow)
		{
			UpdateTouch();
		}

		if (!isRunning)
		{
			return;
		}

		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				GameCanvas.panel2.hideNow();
			}
		}

		long now = mSystem.currentTimeMillis();

		// Chờ delay chuyển tiếp giữa các Tab
		if (stepState == 6)
		{
			if (now - stateTimestamp >= delayBetweenTabsMs)
			{
				stepState = 1;
				stateTimestamp = now;
				StartTab(workflowQueue[queueIndex]);
			}
			return;
		}

		// Chờ delay rẽ nhánh HN -> Zeno hoặc Zeno -> Bánh/HN
		if (stepState == 7)
		{
			if (now - stateTimestamp >= pendingRouteDelayMs)
			{
				int nextTab = pendingRouteTab;
				pendingRouteTab = 0;
				stepState = 1;
				stateTimestamp = now;
				StartTab(nextTab);
			}
			return;
		}

		// Xử lý cơ chế dùng hết bánh ở Tab 1
		if (currentRunningTab == 1 && isUsingAllBanh)
		{
			if (now - stateTimestamp >= delayUseBanhMs)
			{
				stateTimestamp = now;
				if (UseItemInBag(targetItemIdBanh))
				{
					// Vẫn còn bánh, tiếp tục dùng
					MarkLoopProgress();
				}
				else
				{
					// Đã dùng sạch bánh trong rương!
					isUsingAllBanh = false;
					OnTabCompleted(1, "Đã dùng hết toàn bộ " + targetItemNameBanh + " trong rương");
				}
			}
			return;
		}

		// stepState == 3: Chờ mở Shop và gửi lệnh Mua lần đầu
		if (stepState == 3 && (currentRunningTab == 1 || currentRunningTab == 2))
		{
			if (now - stateTimestamp >= 400)
			{
				TriggerBuyCurrentTab();
			}
			return;
		}

		// stepState == 4: Chờ gói -125 từ Server
		if (stepState == 4 && (currentRunningTab == 1 || currentRunningTab == 2))
		{
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount <= 2)
				{
					TriggerBuyCurrentTab();
				}
				else
				{
					retryCount = 0;
					stepState = 1;
					stateTimestamp = now;
					int npcId = (currentRunningTab == 1) ? 92 : 56;
					MoveCloseToNpc(npcId);
					Service.gI().openMenu(npcId);
				}
			}
			return;
		}

		// Cooldown giữa các đợt mua / đổi
		if (stepState == 5)
		{
			int currentDelay = (currentRunningTab == 1) ? delayBuyBanhMs : ((currentRunningTab == 2) ? delayBuyHnMs : ((currentRunningTab == 3) ? delayDiemMs : delayZenoMs));
			if (now - stateTimestamp >= currentDelay)
			{
				stateTimestamp = now;

				// Lặp lại đợt tiếp theo
				if (currentRunningTab == 1)
				{
					Service.gI().buyItem(targetBuyTypeBanh, targetItemIdBanh, 0);
					stepState = 4;
				}
				else if (currentRunningTab == 2)
				{
					Service.gI().buyItem(targetBuyTypeHn, targetItemIdHn, 0);
					stepState = 4;
				}
				else if (currentRunningTab == 3)
				{
					MoveCloseToNpc(66);
					Service.gI().openMenu(66);
					stepState = 2;
				}
				else if (currentRunningTab == 4)
				{
					int currentTicket = GetBagItemCount(1583);
					if (currentTicket < batchQtyZeno)
					{
						HandleLocalRoute(routeTargetWhenLackZeno, "Vé Zeno (" + currentTicket + ") < " + batchQtyZeno + " -> Rẽ nhánh sang Tab " + GetTabName(routeTargetWhenLackZeno));
						return;
					}
					MoveCloseToNpc(18);
					Service.gI().openMenu(18);
					stepState = 2;
				}
			}
			return;
		}

		// Timeout bảo vệ nếu server lag quá 3.5s
		if ((stepState == 1 || stepState == 2) && now - stateTimestamp >= 3500)
		{
			stateTimestamp = now;
			stepState = 1;
			if (currentRunningTab == 1)
			{
				MoveCloseToNpc(92);
				Service.gI().openMenu(92);
			}
			else if (currentRunningTab == 2)
			{
				MoveCloseToNpc(56);
				Service.gI().openMenu(56);
			}
			else if (currentRunningTab == 3)
			{
				MoveCloseToNpc(66);
				Service.gI().openMenu(66);
			}
			else if (currentRunningTab == 4)
			{
				MoveCloseToNpc(18);
				Service.gI().openMenu(18);
			}
		}
	}

	// === CHAT TEXTFIELD INPUT ===
	private void OpenInputField(int mode, string title, string placeholder)
	{
		editFieldMode = mode;
		ChatTextField tf = ChatTextField.gI();
		tf.strChat = title;
		tf.tfChat.name = placeholder;
		tf.to = string.Empty;
		tf.isShow = true;
		tf.tfChat.isFocus = true;
		tf.tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
		tf.parentScreen = this;
		if (!Main.isPC)
		{
			tf.startChat(this, string.Empty);
		}
		else if (GameCanvas.isTouch)
		{
			tf.tfChat.doChangeToTextBox();
		}
	}

	public void onChatFromMe(string text, string to)
	{
		if (!string.IsNullOrEmpty(text) && int.TryParse(text.Trim(), out var val))
		{
			switch (editFieldMode)
			{
			case 1: delayBetweenTabsMs = System.Math.Max(50, val); break;
			case 2: orderBanh = System.Math.Max(1, val); break;
			case 3: orderHn = System.Math.Max(1, val); break;
			case 4: orderDiem = System.Math.Max(1, val); break;
			case 5: orderZeno = System.Math.Max(1, val); break;
			case 6: targetWorkflowLoops = System.Math.Max(0, val); break;
			// Tab 1 Bánh
			case 10: targetItemIdBanh = System.Math.Max(0, val); break;
			case 11: batchQtyBanh = System.Math.Max(1, System.Math.Min(1000, val)); break;
			case 12: targetBatchesBanh = System.Math.Max(0, val); break;
			case 13: delayBuyBanhMs = System.Math.Max(50, val); break;
			case 14: targetItemCountBanh = System.Math.Max(1, val); break;
			case 15: delayUseBanhMs = System.Math.Max(50, val); break;
			// Tab 2 HN
			case 20: targetItemIdHn = System.Math.Max(0, val); break;
			case 21: batchQtyHn = System.Math.Max(1, System.Math.Min(1000, val)); break;
			case 22: targetBatchesHn = System.Math.Max(0, val); break;
			case 23: delayBuyHnMs = System.Math.Max(50, val); break;
			case 24: routeTargetTabHn = System.Math.Max(0, System.Math.Min(4, val)); break;
			case 25: routeDelayHnMs = System.Math.Max(50, val); break;
			// Tab 3 Điểm
			case 31: targetBatchesDiem = System.Math.Max(0, val); break;
			case 32: delayDiemMs = System.Math.Max(50, val); break;
			// Tab 4 Zeno
			case 41: batchQtyZeno = System.Math.Max(1, val); break;
			case 42: targetBatchesZeno = System.Math.Max(0, val); break;
			case 43: delayZenoMs = System.Math.Max(50, val); break;
			case 44: routeTargetWhenDoneZeno = System.Math.Max(0, System.Math.Min(4, val)); break;
			case 45: routeTargetWhenLackZeno = System.Math.Max(0, System.Math.Min(4, val)); break;
			case 46: routeDelayZenoMs = System.Math.Max(50, val); break;
			}
		}
		onCancelChat();
	}

	public void onCancelChat()
	{
		editFieldMode = 0;
		AutoItem.ResetTF(ChatTextField.gI());
	}

	// === TOUCH & CLICK HANDLER ===
	private void UpdateTouch()
	{
		EnsureWindowPosition();
		int closeX = winX + winW - 24;
		int closeY = winY + 6;

		// Close [X]
		if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(closeX, closeY, 18, 18))
		{
			isShowWindow = false;
			isDragging = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Drag header
		if (GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(winX, winY, winW - 28, 28))
		{
			if (!isDragging)
			{
				isDragging = true;
				dragOffsetX = GameCanvas.px - winX;
				dragOffsetY = GameCanvas.py - winY;
			}
			else
			{
				winX = System.Math.Max(0, System.Math.Min(GameCanvas.w - winW, GameCanvas.px - dragOffsetX));
				winY = System.Math.Max(0, System.Math.Min(GameCanvas.h - winH, GameCanvas.py - dragOffsetY));
			}
			GameCanvas.isPointerJustDown = false;
			return;
		}
		if (!GameCanvas.isPointerDown)
		{
			isDragging = false;
		}

		if (!GameCanvas.isPointerClick || !GameCanvas.isPointerJustRelease)
		{
			return;
		}

		// 5 Top Tab Buttons
		int tabY = winY + 31;
		int tabW = 41;
		for (int t = 0; t < 5; t++)
		{
			int tX = winX + 7 + t * 42;
			if (GameCanvas.isPointerHoldIn(tX, tabY, tabW, 17))
			{
				selectedTab = t;
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		int inputX = winX + winW - 64;

		// === TAB 0: CHUNG ===
		if (selectedTab == 0)
		{
			int rY = winY + 52;
			// Checkbox + Order Bánh
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 16))
			{
				enableBanh = !enableBanh;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(2, "Thứ tự ưu tiên Bánh (1-4)", "1");
				return;
			}

			// Checkbox + Order HN
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 16))
			{
				enableHn = !enableHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(3, "Thứ tự ưu tiên HN (1-4)", "2");
				return;
			}

			// Checkbox + Order Điểm
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 16))
			{
				enableDiem = !enableDiem;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(4, "Thứ tự ưu tiên Điểm (1-4)", "3");
				return;
			}

			// Checkbox + Order Zeno
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 16))
			{
				enableZeno = !enableZeno;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(5, "Thứ tự ưu tiên Zeno (1-4)", "4");
				return;
			}

			// Delay chuyển tiếp
			rY += 19;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(1, "Delay chuyển giao các Tab (ms)", "500");
				return;
			}

			// Số vòng lặp (0 = vô hạn)
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(6, "Số vòng lặp quy trình (0 = vô hạn)", targetWorkflowLoops.ToString());
				return;
			}

			// Checkbox Tự bật Mua Nhiều (muanhieu)
			rY += 19;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 190, 16))
			{
				autoChatMuaNhieu = !autoChatMuaNhieu;
				if (autoChatMuaNhieu && !serverMuaNhieuEnabled)
				{
					Service.gI().chat("muanhieu");
					serverMuaNhieuEnabled = true;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// === TAB 1: BÁNH ===
		if (selectedTab == 1)
		{
			// Click dòng thông tin item để nhập ID tùy chỉnh
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 50, 160, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(10, "Nhập ID vật phẩm Bánh", targetItemIdBanh.ToString());
				return;
			}

			int rY = winY + 70;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(11, "Số lượng mua mỗi đợt (1-1000)", "1000");
				return;
			}
			// Delay
			rY += 19;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(13, "Độ trễ mỗi đợt mua (ms)", "600");
				return;
			}
			// Cách 1: Đủ số lần mua
			rY += 21;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 130, 16))
			{
				stopConditionBanh = 1;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				stopConditionBanh = 1;
				GameCanvas.clearAllPointerEvent();
				OpenInputField(12, "Số lần mua (0 = vô hạn)", "50");
				return;
			}
			// Cách 2: Số bánh rương đạt mốc
			rY += 20;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 130, 16))
			{
				stopConditionBanh = 2;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				stopConditionBanh = 2;
				GameCanvas.clearAllPointerEvent();
				OpenInputField(14, "Mốc bánh trong rương cần đạt", "2000000");
				return;
			}
		}

		// === TAB 2: HN ===
		if (selectedTab == 2)
		{
			// Click dòng thông tin item để nhập ID tùy chỉnh
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 50, 160, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(20, "Nhập ID vật phẩm HN", targetItemIdHn.ToString());
				return;
			}

			int rY = winY + 68;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(21, "Số lượng mua mỗi đợt", "1000");
				return;
			}
			// Target Batches
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(22, "Số đợt cần mua", "0");
				return;
			}
			// Delay
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(23, "Độ trễ mỗi đợt (ms)", "600");
				return;
			}
			// Checkbox vừa mua vừa dùng
			rY += 19;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 20, 14))
			{
				autoUseHn = !autoUseHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Checkbox rẽ nhánh ưu tiên (Local routing)
			rY += 15;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 55, 14))
			{
				enableRoutingHn = !enableRoutingHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(24, "Tab nhảy sang khi rẽ nhánh (1-4)", "4");
				return;
			}

			rY += 19;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(25, "Delay chuyển tab HN -> Zeno (ms)", "500");
				return;
			}
		}

		// === TAB 3: ĐIỂM ===
		if (selectedTab == 3)
		{
			// 5 Mốc điểm
			int mocY = winY + 80;
			for (int m = 0; m < 5; m++)
			{
				int mX = winX + 10 + m * 41;
				if (GameCanvas.isPointerHoldIn(mX, mocY, 37, 17))
				{
					pointIndex = m;
					GameCanvas.clearAllPointerEvent();
					return;
				}
			}
			int rY = winY + 101;
			// Target Batches
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(31, "Số lượt cần đổi (0 = vô hạn)", "1");
				return;
			}
			// Delay
			rY += 19;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(32, "Độ trễ mỗi lượt (ms)", "350");
				return;
			}
			// ĐK dừng: Cách 1 vs Cách 2
			rY += 20;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW / 2 - 10, 14))
			{
				stopConditionDiem = 1;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + winW / 2, rY, winW / 2 - 10, 14))
			{
				stopConditionDiem = 2;
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// === TAB 4: ZENO ===
		if (selectedTab == 4)
		{
			int rY = winY + 68;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(41, "Số lượng mỗi đợt (mặc định 19800)", "19800");
				return;
			}
			// Target Batches
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(42, "Số lượng cần đổi (mặc định 1)", "1");
				return;
			}
			// Delay
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(43, "Độ trễ mỗi đợt (ms)", "350");
				return;
			}
			// Nhánh 1: Đạt mục tiêu -> Tab
			rY += 20;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(44, "Tab khi đạt mục tiêu (VD: 2 là HN)", "2");
				return;
			}
			// Nhánh 2: Thiếu vé -> Tab
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(45, "Tab khi thiếu vé (VD: 1 là Bánh)", "1");
				return;
			}

			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(46, "Delay chuyển tab Zeno -> Bánh/HN (ms)", "500");
				return;
			}
		}

		// 2 Nút bấm đáy: BẮT ĐẦU & DỪNG
		int btnX = winX + 14;
		int btnW = winW - 28;
		int startBtnY = winY + 206;
		int stopBtnY = winY + 228;

		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			if (selectedTab == 0) StartWorkflow();
			else StartStandaloneTab(selectedTab);
			return;
		}

		if (GameCanvas.isPointerHoldIn(btnX, stopBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			StopAll("Đã dừng tất cả tiến trình");
			return;
		}

		if (GameCanvas.isPointerHoldIn(winX, winY, winW, winH))
		{
			GameCanvas.clearAllPointerEvent();
		}
	}

	// === GIAO DIỆN VẼ (PAINT) ===
	public void Paint(mGraphics g)
	{
		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowPosition();

		// Viền khung gỗ vàng Dragon Ball
		g.setColor(0x160A03, 0.94f);
		g.fillRect(winX - 2, winY - 2, winW + 4, winH + 4, 6);

		g.setColor(0xD49A2A);
		g.fillRect(winX, winY, winW, winH, 5);

		g.setColor(0xF7CE68);
		g.drawRect(winX + 1, winY + 1, winW - 3, winH - 3);

		g.setColor(0x3B1F0E);
		g.fillRect(winX + 4, winY + 4, winW - 8, winH - 8, 4);

		g.setColor(0x472511);
		g.fillRect(winX + 6, winY + 28, winW - 12, winH - 34, 3);

		// Header bar
		g.setColor(0x693414);
		g.fillRect(winX + 4, winY + 4, winW - 8, 24, 3);
		g.setColor(0xD49A2A);
		g.drawLine(winX + 4, winY + 28, winX + winW - 5, winY + 28);

		// 4 Góc vàng
		g.setColor(0xF7CE68);
		g.fillRect(winX + 2, winY + 2, 8, 2);
		g.fillRect(winX + 2, winY + 2, 2, 8);
		g.fillRect(winX + winW - 10, winY + 2, 8, 2);
		g.fillRect(winX + winW - 4, winY + 2, 2, 8);
		g.fillRect(winX + 2, winY + winH - 4, 8, 2);
		g.fillRect(winX + 2, winY + winH - 10, 2, 8);
		g.fillRect(winX + winW - 10, winY + winH - 4, 8, 2);
		g.fillRect(winX + winW - 4, winY + winH - 10, 2, 8);

		// Tiêu đề Header
		int headerCenterX = winX + winW / 2;
		string headerTitle = "AUTO GỘP - " + GetTabName(selectedTab).ToUpper();
		if (isRunning)
		{
			headerTitle += " (RUN: " + GetTabName(currentRunningTab) + ")";
		}
		mFont.tahoma_7b_yellow.drawStringBorder(g, headerTitle, headerCenterX, winY + 6, mFont.CENTER, mFont.tahoma_7_grey);

		// Nút [X]
		int closeX = winX + winW - 22;
		int closeY = winY + 5;
		g.setColor(0xB71C1C);
		g.fillRect(closeX, closeY, 16, 16, 3);
		g.setColor(0xE53935);
		g.fillRect(closeX + 1, closeY + 1, 14, 14, 2);
		mFont.tahoma_7b_white.drawString(g, "X", closeX + 8, closeY + 2, mFont.CENTER);

		// 5 Top Tab Buttons
		int tabY = winY + 31;
		string[] tabTitles = new string[5] { "Chung", "Bánh", "HN", "Điểm", "Zeno" };
		for (int t = 0; t < 5; t++)
		{
			int tX = winX + 7 + t * 42;
			bool isSel = (selectedTab == t);
			g.setColor(isSel ? 0x8D4918 : 0x261409);
			g.fillRect(tX, tabY, 41, 17, 3);
			if (isSel)
			{
				g.setColor(0xA8581E);
				g.fillRect(tX + 1, tabY + 1, 39, 8, 2);
			}
			g.setColor(isSel ? 0xF7CE68 : 0x6E4720);
			g.drawRect(tX, tabY, 40, 16);
			if (isSel)
			{
				mFont.tahoma_7b_yellow.drawStringBorder(g, tabTitles[t], tX + 20, tabY + 2, mFont.CENTER, mFont.tahoma_7_grey);
			}
			else
			{
				mFont.tahoma_7b_white.drawString(g, tabTitles[t], tX + 20, tabY + 2, mFont.CENTER);
			}
		}

		int labelX = winX + 12;
		int inputX = winX + winW - 64;

		// === NỘI DUNG TAB 0: CHUNG ===
		if (selectedTab == 0)
		{
			int rY = winY + 52;
			DrawCheckbox(g, labelX, rY, enableBanh, "1. Auto Bánh (92)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, orderBanh.ToString(), isGreenText: true);

			rY += 18;
			DrawCheckbox(g, labelX, rY, enableHn, "2. Auto HN (56)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, orderHn.ToString(), isGreenText: true);

			rY += 18;
			DrawCheckbox(g, labelX, rY, enableDiem, "3. Đổi Điểm (66)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, orderDiem.ToString(), isGreenText: true);

			rY += 18;
			DrawCheckbox(g, labelX, rY, enableZeno, "4. Đổi Zeno (18)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, orderZeno.ToString(), isGreenText: true);

			rY += 19;
			mFont.tahoma_7b_white.drawString(g, "Delay chuyển giao:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayBetweenTabsMs + " ms", isGreenText: false);

			rY += 18;
			mFont.tahoma_7b_white.drawString(g, "Vòng lặp (0=vô hạn):", labelX, rY + 2, mFont.LEFT);
			string loopBoxText = (targetWorkflowLoops == 0) ? "0=Vô hạn" : targetWorkflowLoops.ToString();
			DrawInputBox(g, inputX, rY, 52, 15, loopBoxText, isGreenText: true);

			rY += 19;
			DrawCheckbox(g, labelX, rY, autoChatMuaNhieu, "Tự bật Mua Nhiều (muanhieu)");

			string loopRunInfo = (targetWorkflowLoops == 0)
				? ("Vòng " + (completedWorkflowLoops + 1) + "/Vô hạn")
				: ("Vòng " + (completedWorkflowLoops + 1) + "/" + targetWorkflowLoops);
			string statusText = isRunningWorkflow
				? ("Chạy: " + GetTabName(currentRunningTab) + " (" + loopRunInfo + ")")
				: (isRunning ? ("Chạy độc lập: Tab " + GetTabName(currentRunningTab)) : "Trạng thái: Sẵn sàng (Phím F3)");
			mFont.tahoma_7_yellow.drawString(g, statusText, headerCenterX, winY + 181, mFont.CENTER);
		}

		// === NỘI DUNG TAB 1: BÁNH ===
		if (selectedTab == 1)
		{
			int rY = winY + 52;
			mFont.tahoma_7b_white.drawString(g, targetItemNameBanh + " [" + targetItemIdBanh + "] - NPC 92", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Rương: " + GetBagItemCount(targetItemIdBanh), inputX + 48, rY, mFont.RIGHT);

			rY = winY + 70;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, batchQtyBanh.ToString(), isGreenText: true);

			rY += 19;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, delayBuyBanhMs + " ms", isGreenText: false);

			rY += 21;
			DrawRadio(g, labelX, rY, stopConditionBanh == 1, "Cách 1: Đủ số lần mua");
			DrawInputBox(g, inputX, rY, 52, 16, targetBatchesBanh.ToString(), isGreenText: true);

			rY += 20;
			DrawRadio(g, labelX, rY, stopConditionBanh == 2, "Cách 2: Đạt mốc rương");
			DrawInputBox(g, inputX, rY, 52, 16, targetItemCountBanh.ToString(), isGreenText: true);

			string noteBanh = (stopConditionBanh == 2)
				? "(Đạt mốc rương -> Tự dùng hết bánh rồi chuyển)"
				: "(Đủ số lần hoặc hết tài nguyên/đầy rương -> Dừng)";
			mFont.tahoma_7_yellow.drawString(g, noteBanh, headerCenterX, winY + 181, mFont.CENTER);
		}

		// === NỘI DUNG TAB 2: HN ===
		if (selectedTab == 2)
		{
			int rY = winY + 52;
			mFont.tahoma_7b_white.drawString(g, targetItemNameHn + " [" + targetItemIdHn + "] - NPC 56", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Rương: " + GetBagItemCount(targetItemIdHn), inputX + 48, rY, mFont.RIGHT);

			rY = winY + 68;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, batchQtyHn.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Số đợt cần mua:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, targetBatchesHn.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, delayBuyHnMs + " ms", isGreenText: false);

			rY += 19;
			DrawCheckbox(g, labelX, rY, autoUseHn, "Vừa mua vừa dùng (" + targetItemIdHn + ")");

			rY += 15;
			DrawCheckbox(g, labelX, rY, enableRoutingHn, "Rẽ nhánh khi đầy/hết ->");
			mFont.tahoma_7_yellow.drawString(g, "Tab:", inputX - 25, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, GetTabName(routeTargetTabHn) + "(" + routeTargetTabHn + ")", isGreenText: true);

			rY += 19;
			mFont.tahoma_7_white.drawString(g, "Delay chuyển Zeno:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, routeDelayHnMs + " ms", isGreenText: false);

			mFont.tahoma_7_white.drawString(g, "(Rẽ nhánh Tab HN có ưu tiên cao hơn Tab Chung)", headerCenterX, winY + 181, mFont.CENTER);
		}

		// === NỘI DUNG TAB 3: ĐIỂM ===
		if (selectedTab == 3)
		{
			int rY = winY + 50;
			string ptStr = (currentDiemPoints >= 0) ? (NinjaUtil.getMoneys(currentDiemPoints) + " điểm") : "Đang cập nhật...";
			mFont.tahoma_7b_yellow.drawString(g, "Điểm hiện có: " + ptStr, labelX, rY, mFont.LEFT);

			// Alert Lọc đồ
			g.setColor(0xB71C1C);
			g.fillRect(winX + 10, winY + 63, winW - 20, 14, 2);
			mFont.tahoma_7b_white.drawString(g, "⚠️ VUI LÒNG BẬT TỰ ĐỘNG LỌC ĐỒ", headerCenterX, winY + 64, mFont.CENTER);

			// 5 Mốc điểm
			int mocY = winY + 80;
			for (int m = 0; m < 5; m++)
			{
				int mX = winX + 10 + m * 41;
				bool isSel = (pointIndex == m);
				g.setColor(isSel ? 0x1B5E20 : 0x160D06);
				g.fillRect(mX, mocY, 37, 17, 3);
				if (isSel)
				{
					g.setColor(0x2E7D32);
					g.fillRect(mX + 1, mocY + 1, 35, 8, 2);
				}
				g.setColor(isSel ? 0xF7CE68 : 0x8D6E63);
				g.drawRect(mX, mocY, 36, 16);
				if (isSel)
				{
					mFont.tahoma_7b_yellow.drawStringBorder(g, MocShortNames[m], mX + 18, mocY + 3, mFont.CENTER, mFont.tahoma_7_grey);
				}
				else
				{
					mFont.tahoma_7b_white.drawString(g, MocShortNames[m], mX + 18, mocY + 3, mFont.CENTER);
				}
			}

			rY = winY + 101;
			mFont.tahoma_7_white.drawString(g, "Số lượt cần đổi:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, targetBatchesDiem.ToString(), isGreenText: true);

			rY += 19;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi lượt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, delayDiemMs + " ms", isGreenText: false);

			rY += 20;
			DrawRadio(g, labelX, rY, stopConditionDiem == 1, "Đủ số lượt");
			DrawRadio(g, winX + winW / 2, rY, stopConditionDiem == 2, "Điểm < Mốc");

			mFont.tahoma_7_yellow.drawString(g, "Mốc chọn: " + MocFullNames[pointIndex], headerCenterX, winY + 181, mFont.CENTER);
		}

		// === NỘI DUNG TAB 4: ZENO ===
		if (selectedTab == 4)
		{
			int rY = winY + 52;
			mFont.tahoma_7b_white.drawString(g, "Vé Zeno (1583) - Karin (18)", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Vé: " + GetBagItemCount(1583) + " cái", inputX + 48, rY, mFont.RIGHT);

			rY = winY + 68;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, batchQtyZeno.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Số đợt cần đổi:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, targetBatchesZeno.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, delayZenoMs + " ms", isGreenText: false);

			rY += 20;
			mFont.tahoma_7_white.drawString(g, "Nhánh 1 (Đạt mục tiêu) ->", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, GetTabName(routeTargetWhenDoneZeno) + "(" + routeTargetWhenDoneZeno + ")", isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Nhánh 2 (Vé < " + batchQtyZeno + ") ->", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, GetTabName(routeTargetWhenLackZeno) + "(" + routeTargetWhenLackZeno + ")", isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Delay chuyển tab:", labelX, rY + 2, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 16, routeDelayZenoMs + " ms", isGreenText: false);

			mFont.tahoma_7_yellow.drawString(g, "(Chạy song song 2 nhánh: Đạt mục tiêu / Thiếu vé)", headerCenterX, winY + 181, mFont.CENTER);
		}

		// 2 Nút bấm đáy
		int btnX = winX + 14;
		int btnW = winW - 28;
		int btnH = 19;
		int startBtnY = winY + 206;
		int stopBtnY = winY + 228;

		string startText = isRunning
			? ("ĐANG CHẠY " + GetTabName(currentRunningTab).ToUpper() + "...")
			: (selectedTab == 0 ? "▶ BẮT ĐẦU CHẠY QUY TRÌNH (F3)" : ("▶ CHẠY " + GetTabName(selectedTab).ToUpper() + " (F" + (selectedTab == 1 ? "4" : (selectedTab == 2 ? "5" : (selectedTab == 3 ? "7" : "8"))) + ")"));

		DrawPillButton(g, btnX, startBtnY, btnW, btnH, isGreen: true, isRunning);
		mFont.tahoma_7b_white.drawStringBorder(g, startText, btnX + btnW / 2, startBtnY + 3, mFont.CENTER, mFont.tahoma_7_grey);

		DrawPillButton(g, btnX, stopBtnY, btnW, btnH, isGreen: false, isActive: false);
		mFont.tahoma_7b_white.drawStringBorder(g, "■ DỪNG TẤT CẢ (F6 / Phím A)", btnX + btnW / 2, stopBtnY + 3, mFont.CENTER, mFont.tahoma_7_grey);
	}

	private void DrawInputBox(mGraphics g, int x, int y, int w, int h, string val, bool isGreenText)
	{
		g.setColor(0x160D06);
		g.fillRect(x, y, w, h, 3);
		g.setColor(0xD49A2A);
		g.drawRect(x, y, w - 1, h - 1);
		if (isGreenText)
		{
			mFont.tahoma_7b_green.drawString(g, val, x + w / 2, y + 2, mFont.CENTER);
		}
		else
		{
			mFont.tahoma_7b_white.drawString(g, val, x + w / 2, y + 2, mFont.CENTER);
		}
	}

	private void DrawCheckbox(mGraphics g, int x, int y, bool isChecked, string text)
	{
		g.setColor(0x160D06);
		g.fillRect(x, y, 11, 11, 2);
		if (isChecked)
		{
			g.setColor(0x2E7D32);
			g.fillRect(x + 1, y + 1, 9, 9, 2);
			mFont.tahoma_7b_white.drawString(g, "v", x + 6, y - 1, mFont.CENTER);
		}
		g.setColor(0x81C784);
		g.drawRect(x, y, 10, 10);
		if (isChecked)
		{
			mFont.tahoma_7_green.drawString(g, text, x + 15, y, mFont.LEFT);
		}
		else
		{
			mFont.tahoma_7_white.drawString(g, text, x + 15, y, mFont.LEFT);
		}
	}

	private void DrawRadio(mGraphics g, int x, int y, bool isChecked, string text)
	{
		g.setColor(0x160D06);
		g.fillRect(x, y, 11, 11, 5);
		if (isChecked)
		{
			g.setColor(0xF57C00);
			g.fillRect(x + 2, y + 2, 7, 7, 3);
		}
		g.setColor(0xF7CE68);
		g.drawRect(x, y, 10, 10);
		if (isChecked)
		{
			mFont.tahoma_7_yellow.drawString(g, text, x + 15, y, mFont.LEFT);
		}
		else
		{
			mFont.tahoma_7_white.drawString(g, text, x + 15, y, mFont.LEFT);
		}
	}

	private void DrawPillButton(mGraphics g, int x, int y, int w, int h, bool isGreen, bool isActive)
	{
		int borderCol = isGreen ? 0xA5D6A7 : 0xEF9A9A;
		int darkCol = isGreen ? 0x1B5E20 : 0x7F0000;
		int midCol = isGreen ? (isActive ? 0x43A047 : 0x2E7D32) : 0xC62828;
		int topCol = isGreen ? 0x66BB6A : 0xE53935;

		g.setColor(darkCol);
		g.fillRect(x + 2, y, w - 4, h);
		g.fillRect(x, y + 2, w, h - 4);
		g.fillRect(x + 1, y + 1, w - 2, h - 2);

		g.setColor(midCol);
		g.fillRect(x + 2, y + 1, w - 4, h - 3);
		g.fillRect(x + 1, y + 2, w - 2, h - 5);

		g.setColor(topCol);
		g.fillRect(x + 3, y + 2, w - 6, (h - 4) / 2);

		g.setColor(borderCol);
		g.drawLine(x + 2, y, x + w - 3, y);
		g.drawLine(x + 2, y + h - 1, x + w - 3, y + h - 1);
		g.drawLine(x, y + 2, x, y + h - 3);
		g.drawLine(x + w - 1, y + 2, x + w - 1, y + h - 3);
	}

	public bool HandleChatCommand(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string lower = text.Trim().ToLower();

		if (lower == "gop" || lower == "autogop")
		{
			ToggleWindow();
			return true;
		}
		if (lower == "f3" || lower == "chaygop")
		{
			StartWorkflow();
			return true;
		}
		if (lower == "f6" || lower == "dunggop")
		{
			StopAll("Đã dừng Auto Gộp bằng lệnh");
			return true;
		}
		return false;
	}
}
