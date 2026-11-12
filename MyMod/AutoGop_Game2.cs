using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game2.Assets.src.g;
using Game2.Mod.XMAP;
using UnityEngine;

namespace Game2.Mod;

public class AutoGop : IChatable
{
	private static AutoGop _instance;

	public bool isShowWindow;

	public int selectedTab; // 0: Chung, 1: Bánh, 2: HN, 3: Điểm, 4: Zeno

	public bool isRunning; // Đang chạy bất kỳ tiến trình nào

	public bool isRunningWorkflow; // Đang chạy toàn bộ quy trình (Tab Chung - F3)

	public int currentRunningTab; // Tab đang thực thi: 1 (Bánh), 2 (HN), 3 (Điểm), 4 (Zeno)

	public bool hideUi = true; // Ẩn hoàn toàn giao diện NPC / ô nhập khi chạy

	public bool hidePlayersAndMobs = true; // [v] Ẩn người & quái khi auto

	public bool hideFlyInfoMessages = true; // [v] Tắt thông báo chữ bay khi auto

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

	public int targetWorkflowLoops; // 0 = vô hạn

	public int completedWorkflowLoops;

	private List<int> workflowQueue = new List<int>();

	private int queueIndex;

	// === TAB 1: BÁNH (Item ID: 1337, NPC ID: 92) ===
	public int targetItemIdBanh = 1337;

	public string targetItemNameBanh = "Bánh";

	public sbyte targetBuyTypeBanh = 3;

	public int batchQtyBanh = 1000;

	public int targetBatchesBanh = 50; // 0 = vô hạn

	public int delayBuyBanhMs = 200;

	public bool stopBanhCond1; // ĐK 1: Đủ số đợt mua

	public bool stopBanhCond2 = true; // ĐK 2: Đầy rương / Hết tài nguyên

	public bool stopBanhCond3 = true; // ĐK 3: Đạt mốc bánh trong rương -> Tự dùng hết

	public int targetItemCountBanh = 5000; // Mốc số bánh trong rương cho ĐK 3

	public int delayUseBanhMs = 200; // Delay khi dùng bánh

	public bool enableRoutingBanh; // Mặc định tắt (false)

	public int routeTargetTabBanh = 3; // Mặc định chuyển sang Tab Điểm (3)

	public int completedBatchesBanh;

	private bool isUsingAllBanh;

	// === TAB 2: HN (Item ID: 1581, NPC ID: 56) ===
	public int targetItemIdHn = 1581;

	public string targetItemNameHn = "HN";

	public sbyte targetBuyTypeHn = 3;

	public int batchQtyHn = 1000;

	public int targetBatchesHn = 50; // 0 = vô hạn

	public int delayBuyHnMs = 200;

	public bool autoUseHn = true; // Vừa mua vừa dùng vật phẩm

	public int stopConditionHn = 1; // 1: Đạt đủ số lượng. 2: Hết tài nguyên / hành trang đầy

	public bool enableRoutingHn = true; // Bật tính năng chuyển hướng cục bộ

	public int routeWhenConditionHn = 2; // Rẽ nhánh khi thỏa mãn ĐK

	public int routeTargetTabHn = 4; // Nhảy sang Tab Zeno (ID 4)

	public int completedBatchesHn;

	public bool stopHnCond3 = true; // ĐK 3: Zeno trong rương < Số lượng/đợt x 40K

	public const int ZenoItemId = 1378; // Vật phẩm Zeno dùng để mua HN

	public const long ZenoPerHn = 40000L; // Giá 1 HN = 40.000 Zeno

	// === CHỐNG ĐỆ QUY THÔNG BÁO (Fix đơ game) ===
	private bool suppressInfoHook;

	private bool inServerInfo;

	private bool progressThisLoop; // Có mua/đổi được gì trong vòng hiện tại không

	// === TAB 3: ĐIỂM (NPC ID: 66) ===
	public int pointIndex = 1; // 0: 10Tr, 1: 5K, 2: 3K, 3: 200, 4: 50

	public bool autoSelectMocDiem = true; // Tự động hạ mốc từ cao xuống thấp (10Tr -> 5K -> 3K)

	public int targetBatchesDiem = 50; // 0 = vô hạn

	public int delayDiemMs = 350;

	public int stopConditionDiem = 2; // 1: Đổi đủ số lượt, 2: Đầy rương / Hết điểm (< Mốc)

	public bool enableRoutingDiem; // Bật/tắt rẽ nhánh khi xong Tab Điểm

	public int routeTargetTabDiem = 4; // Mặc định chuyển sang Tab Zeno (4)

	public int currentDiemPoints = -1; // Điểm hiện có cập nhật Real-time từ lời thoại Menu 1 của NPC 66

	public int completedBatchesDiem;

	public static readonly int[] RequiredPoints = new int[5] { 10000000, 5000, 3000, 200, 50 };

	public static readonly string[] MocShortNames = new string[5] { "10Tr", "5K", "3K", "200", "50" };

	public static readonly string[] MocFullNames = new string[5] { "10.000.000đ", "5.000đ", "3.000đ", "200đ", "50đ" };

	// === TAB 4: ZENO (Item ID: 1583, NPC ID: 18) ===
	public int batchQtyZeno = 19800; // Mặc định 19.800

	public int targetBatchesZeno = 1; // Mặc định 1

	public int delayZenoMs = 350;

	public bool enableRouteDoneZeno = true; // Bật/tắt Nhánh 1 (Đạt mục tiêu)

	public int routeTargetWhenDoneZeno = 2; // Nhánh 1: Đổi đủ mục tiêu -> Nhảy sang Tab HN (2)

	public bool enableRouteLackZeno = true; // Bật/tắt Nhánh 2 (Thiếu vé < 19.800)

	public int routeTargetWhenLackZeno = 1; // Nhánh 2: Thiếu vé (< 19.800) -> Nhảy sang Tab Bánh (1)

	public int completedBatchesZeno;

	private int zenoMenuStep; // 0: Đợi Menu 1, 1: Đợi Menu 2, 2: Đợi xác nhận

	private int lastZenoMenu2Idx;

	private int zenoLastMenuType = 32; // 32: createMenu, 33: OPEN_UI_MENU, 27: menuId

	private int zenoLastSubIdx;

	private short zenoLastNewMenuId;

	private bool isWaitingXmap;

	private int xmapTargetMapId = -1;

	private int xmapTargetNpcId = -1;

	private int xmapPendingTab;

	private long xmapArrivedTimestamp;

	private long xmapStartTimestamp;

	private int zenoReturnMapId = -1;

	private string lastZenoDebug = string.Empty;

	// === ENGINE STATE ===
	private int stepState; // 0: Idle, 1: Approach & Open, 2: WaitMenu, 3: WaitShop, 4: WaitInput, 5: Cooldown, 6: TransitionDelay, 7: WaitXmap

	private long stateTimestamp;

	public long lastStopTimeMs;

	private int retryCount;

	private int npcRetryCount;

	private int editFieldMode;

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 228;

	private readonly int winH = 250;

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
	private static long lastBagCheckTime;
	private static readonly Dictionary<int, int> cachedBagCounts = new Dictionary<int, int>();

	public static int GetBagItemCount(int itemId)
	{
		long now = mSystem.currentTimeMillis();
		if (now - lastBagCheckTime < 100 && cachedBagCounts.ContainsKey(itemId))
		{
			return cachedBagCounts[itemId];
		}

		if (now - lastBagCheckTime >= 100)
		{
			cachedBagCounts.Clear();
			lastBagCheckTime = now;
		}

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
		cachedBagCounts[itemId] = count;
		return count;
	}

	// Gửi thông báo nội bộ mà KHÔNG kích hoạt lại OnServerInfo (tránh đệ quy vô hạn gây đơ game)
	private void Notify(string s)
	{
		bool prev = suppressInfoHook;
		suppressInfoHook = true;
		try
		{
			GameScr.info1.addInfo(s, 0);
		}
		finally
		{
			suppressInfoHook = prev;
		}
	}

	public int GetCurrentTargetNpcId()
	{
		if (!isRunning)
		{
			return -1;
		}
		return GetNpcIdForTab(currentRunningTab);
	}

	public bool ShouldSuppressGameUi()
	{
		if (!isRunning || !hideUi)
		{
			return false;
		}
		// Cho phép Xmap mở Menu NPC/Capsule khi Tab Zeno đang tự di chuyển tới Tháp Karin (Map 46)
		if (currentRunningTab == 4 && isWaitingXmap && stepState == 7)
		{
			return false;
		}
		return true;
	}

	public static int GetNpcIdForTab(int tabId)
	{
		return tabId switch
		{
			1 => 92,
			2 => 56,
			3 => 66,
			4 => 18,
			_ => -1,
		};
	}

	private static string NormalizeMenuText(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return string.Empty;
		}
		string clean = s.Replace("\r", " ").Replace("\n", " ");
		return Regex.Replace(clean, "\\s+", " ").Trim().ToLower();
	}

	private static void ClearResidualGameUi()
	{
		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.isShow = false;
		}
		GameCanvas.panel2 = null;
		if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.close = true;
			GameCanvas.menu.doCloseMenu();
		}
		if (ChatTextField.gI().isShow)
		{
			ChatTextField.gI().isShow = false;
		}
		if (InfoDlg.isShow)
		{
			InfoDlg.hide();
		}
		if (GameCanvas.currentDialog != null)
		{
			GameCanvas.endDlg();
		}
		ChatPopup.currChatPopup = null;
		Char.chatPopup = null;
		if (GameCanvas.currentScreen is ClientInput && GameScr.instance != null)
		{
			GameScr.instance.switchToMe();
		}
	}

	private static bool IsZenoSkippedOption(string m)
	{
		return m.Contains("hướng dẫn") || m.Contains("thông tin") || m.Contains("từ chối") || m.Contains("hủy") || m.Contains("đóng") || m.Contains("thoát") || m.Contains("không");
	}

	private static int FindZenoMenuOptionIndex(string[] normalizedOptions, int step)
	{
		if (normalizedOptions == null || normalizedOptions.Length == 0)
		{
			return -1;
		}
		if (step == 0)
		{
			// Ưu tiên đúng vị trí index 3 của bản Backup 144724 nếu chứa "đổi tiên phù"
			if (normalizedOptions.Length > 3 && !IsZenoSkippedOption(normalizedOptions[3]) && normalizedOptions[3].Contains("đổi tiên phù"))
			{
				return 3;
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("đổi tiên phù"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && (m.Contains("đổi vé") || (m.Contains("đổi") && (m.Contains("zeno") || m.Contains("tiên phù")))))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && (m.Contains("tiên phù") || m.Contains("zeno")))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("đổi") && !m.Contains("khu"))
				{
					return i;
				}
			}
			return (normalizedOptions.Length > 3) ? 3 : 0;
		}
		if (step == 1)
		{
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("tiên phù zeno"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("tiên phù"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("zeno") && !m.Contains("đổi vé"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Length; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && (m.Contains("đồng ý") || m.Contains("xác nhận")))
				{
					return i;
				}
			}
			return 0;
		}
		for (int i = 0; i < normalizedOptions.Length; i++)
		{
			string m = normalizedOptions[i];
			if (IsZenoSkippedOption(m))
			{
				continue;
			}
			if (m.Contains("đồng ý") || m.Contains("xác nhận") || m.Contains("tiên phù") || m.Contains("zeno") || m.Contains("đổi"))
			{
				return i;
			}
		}
		return 0;
	}

	private void MaybeResetZenoMenuStepIfMainMenu(IList<string> normalizedOptions)
	{
		if (zenoMenuStep == 0 || normalizedOptions == null || normalizedOptions.Count < 3)
		{
			return;
		}
		bool hasTier1 = false;
		bool hasTier2 = false;
		for (int i = 0; i < normalizedOptions.Count; i++)
		{
			string m = normalizedOptions[i];
			if (IsZenoSkippedOption(m))
			{
				continue;
			}
			if (m.Contains("đổi tiên phù") && !m.Contains("tiên phù zeno"))
			{
				hasTier1 = true;
			}
			if (m.Contains("tiên phù zeno"))
			{
				hasTier2 = true;
			}
		}
		if (hasTier1 && !hasTier2)
		{
			zenoMenuStep = 0;
		}
	}

	private void AdvanceZenoMenuStep(int chosenOption, int menuType, int subIdx = 0, short newMenuId = 0)
	{
		npcRetryCount = 0;
		retryCount = 0;
		stateTimestamp = mSystem.currentTimeMillis();
		zenoLastMenuType = menuType;
		zenoLastSubIdx = subIdx;
		zenoLastNewMenuId = newMenuId;
		if (zenoMenuStep == 0)
		{
			zenoMenuStep = 1;
			stepState = 3; // Đợi Menu cấp 2 (matching 144724 stepState = 3 + 3.5s timeout coverage)
		}
		else
		{
			lastZenoMenu2Idx = chosenOption;
			zenoMenuStep = 2;
			stepState = 4; // Chờ gói -125 ClientInput (hoặc Menu xác nhận)
		}
	}

	private void SendZenoMenuRetry()
	{
		if (zenoLastMenuType == 33)
		{
			Service.gI().menu(18, lastZenoMenu2Idx, zenoLastSubIdx);
		}
		else if (zenoLastMenuType == 27)
		{
			Service.gI().menuId(zenoLastNewMenuId);
		}
		else
		{
			Service.gI().confirmMenu(18, (sbyte)lastZenoMenu2Idx);
		}
	}

	public static string FormatShortNumber(long v)
	{
		if (v >= 1000000000L)
		{
			return (v / 1000000000.0).ToString("0.##") + "B";
		}
		if (v >= 1000000L)
		{
			return (v / 1000000.0).ToString("0.##") + "M";
		}
		if (v >= 1000L)
		{
			return (v / 1000.0).ToString("0.##") + "K";
		}
		return v.ToString();
	}

	public long GetHongNgoc()
	{
		return (Char.myCharz() != null) ? Char.myCharz().luongKhoa : 0;
	}

	public long GetZenoRequiredHn()
	{
		return (long)batchQtyHn * ZenoPerHn;
	}

	// ĐK 3 Tab HN: Zeno < Số lượng/đợt x 40K
	private bool CheckHnZenoLack()
	{
		if (!stopHnCond3)
		{
			return false;
		}
		int zeno = GetBagItemCount(ZenoItemId);
		long need = GetZenoRequiredHn();
		if (zeno < need)
		{
			FinishTabHn("Zeno (" + FormatShortNumber(zeno) + ") < " + FormatShortNumber(need) + " (ĐK 3)");
			return true;
		}
		return false;
	}

	public static int GetEmptyBagSlots()
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return 0;
		}
		int empty = 0;
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			if (Char.myCharz().arrItemBag[i] == null)
			{
				empty++;
			}
		}
		return empty;
	}

	public static bool CanItemBeStackedInBag(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return false;
		}
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			Item item = Char.myCharz().arrItemBag[i];
			if (item != null && item.template != null && item.template.id == itemId && item.template.isUpToUp && item.quantity < 99)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetBagItemCountByIcon(int iconId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			Item item = Char.myCharz().arrItemBag[i];
			if (item != null && item.template != null && item.template.iconID == iconId)
			{
				count += System.Math.Max(1, item.quantity);
			}
		}
		return count;
	}

	public static void SendClientInputDirect(string text)
	{
		Message message = null;
		try
		{
			message = new Message(-125);
			message.writer().writeByte(1);
			message.writer().writeUTF(text);
			Session_ME.gI().sendMessage(message);
		}
		catch (Exception)
		{
		}
		finally
		{
			if (message != null)
			{
				message.cleanup();
			}
		}
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
		if (npc != null && Char.myCharz() != null)
		{
			Char.myCharz().currentMovePoint = null;
			if (System.Math.Abs(npc.cx - Char.myCharz().cx) > 40 || System.Math.Abs(npc.cy - Char.myCharz().cy) > 40)
			{
				ModFunc.GI().MoveTo(npc.cx, npc.cy - 1);
			}
			Char.myCharz().FocusManualTo(npc);
			Char.myCharz().npcFocus = npc;
		}
	}

	private static void MoveCloseToZenoNpc()
	{
		Npc npc = ModFunc.GetNpcByTempId(18);
		if (npc != null && Char.myCharz() != null)
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
			Notify("Đã chọn cho [Tab Bánh]: " + targetItemNameBanh + " [ID: " + targetItemIdBanh + "]");
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
			Notify("Đã chọn cho [Tab HN]: " + targetItemNameHn + " [ID: " + targetItemIdHn + "]");
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

	public Item FindShopItemByName(string keyword)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemShop == null || string.IsNullOrEmpty(keyword))
		{
			return null;
		}
		string kw = keyword.ToLower();
		for (int t = 0; t < Char.myCharz().arrItemShop.Length; t++)
		{
			if (Char.myCharz().arrItemShop[t] == null)
			{
				continue;
			}
			for (int i = 0; i < Char.myCharz().arrItemShop[t].Length; i++)
			{
				Item it = Char.myCharz().arrItemShop[t][i];
				if (it != null && it.template != null && !string.IsNullOrEmpty(it.template.name) && it.template.name.ToLower().Contains(kw))
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
		npcRetryCount = 0;
		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.isShow = false;
			}
			GameCanvas.panel2 = null;
		}

		if (currentRunningTab == 1 || currentRunningTab == 2)
		{
			stepState = 3;
			stateTimestamp = mSystem.currentTimeMillis();
		}
	}

	public void TriggerBuyCurrentTab()
	{
		if (!isRunning)
		{
			return;
		}
		if (currentRunningTab == 2 && CheckHnZenoLack())
		{
			return;
		}
		int targetId = (currentRunningTab == 1) ? targetItemIdBanh : targetItemIdHn;
		sbyte buyType = (currentRunningTab == 1) ? targetBuyTypeBanh : targetBuyTypeHn;
		Item shopItem = FindShopItem(targetId);
		if (shopItem == null)
		{
			string kw = (currentRunningTab == 1) ? "bánh" : "hồn";
			shopItem = FindShopItemByName(kw);
			if (shopItem != null && shopItem.template != null)
			{
				if (currentRunningTab == 1)
				{
					targetItemIdBanh = shopItem.template.id;
					targetItemNameBanh = shopItem.template.name;
				}
				else
				{
					targetItemIdHn = shopItem.template.id;
					targetItemNameHn = shopItem.template.name;
				}
				targetId = shopItem.template.id;
				Notify("Tự nhận diện trong Shop: " + shopItem.template.name + " [" + targetId + "]");
			}
		}

		if (shopItem != null && shopItem.template != null)
		{
			sbyte calcType = shopItem.isBuySpec ? ((sbyte)3) : ((shopItem.buyCoin > 0) ? ((sbyte)0) : ((sbyte)1));
			if (currentRunningTab == 1)
			{
				targetItemNameBanh = shopItem.template.name;
				targetBuyTypeBanh = calcType;
				buyType = targetBuyTypeBanh;
			}
			else
			{
				targetItemNameHn = shopItem.template.name;
				targetBuyTypeHn = calcType;
				buyType = targetBuyTypeHn;
			}
		}
		else if (Char.myCharz() != null && Char.myCharz().arrItemShop != null && Char.myCharz().arrItemShop.Length > 0 && Char.myCharz().arrItemShop[0] != null && Char.myCharz().arrItemShop[0].Length > 0)
		{
			shopItem = Char.myCharz().arrItemShop[0][0];
			if (shopItem != null && shopItem.template != null)
			{
				sbyte calcType = shopItem.isBuySpec ? ((sbyte)3) : ((shopItem.buyCoin > 0) ? ((sbyte)0) : ((sbyte)1));
				if (currentRunningTab == 1)
				{
					targetItemIdBanh = shopItem.template.id;
					targetItemNameBanh = shopItem.template.name;
					targetBuyTypeBanh = calcType;
					buyType = targetBuyTypeBanh;
				}
				else
				{
					targetItemIdHn = shopItem.template.id;
					targetItemNameHn = shopItem.template.name;
					targetBuyTypeHn = calcType;
					buyType = targetBuyTypeHn;
				}
				targetId = shopItem.template.id;
				Notify("Chọn vật phẩm đầu Shop: " + shopItem.template.name + " [" + targetId + "]");
			}
		}

		if (hideUi)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.isShow = false;
			}
			GameCanvas.panel2 = null;
		}

		// 1. Kiểm tra hành trang đầy
		if (GetEmptyBagSlots() == 0 && !CanItemBeStackedInBag(targetId))
		{
			if (currentRunningTab == 1)
			{
				FinishTabBanh("Hành trang đã đầy");
			}
			else if (currentRunningTab == 2)
			{
				FinishTabHn("Hành trang đã đầy");
			}
			return;
		}

		// 2. Kiểm tra tài nguyên mua (Xu, Ngọc, Nguyên liệu đặc biệt)
		if (shopItem != null)
		{
			// Kiểm tra Xu
			if (shopItem.buyCoin > 0 && Char.myCharz() != null && Char.myCharz().xu < shopItem.buyCoin)
			{
				if (currentRunningTab == 1)
				{
					FinishTabBanh("Không đủ Xu để mua " + targetItemNameBanh);
				}
				else if (currentRunningTab == 2)
				{
					FinishTabHn("Không đủ Xu để mua " + targetItemNameHn);
				}
				return;
			}

			// Kiểm tra Ngọc
			if (shopItem.buyGold > 0 && Char.myCharz() != null && (Char.myCharz().luong + Char.myCharz().luongKhoa) < shopItem.buyGold)
			{
				if (currentRunningTab == 1)
				{
					FinishTabBanh("Không đủ Ngọc để mua " + targetItemNameBanh);
				}
				else if (currentRunningTab == 2)
				{
					FinishTabHn("Không đủ Ngọc để mua " + targetItemNameHn);
				}
				return;
			}

			// Kiểm tra Nguyên liệu đặc biệt (iconSpec / buySpec)
			if (shopItem.isBuySpec && shopItem.buySpec > 0)
			{
				if (currentRunningTab == 1)
				{
					long hongNgoc = GetHongNgoc();
					int rubyCount = GetBagItemCount(861);
					int specMaterial = (shopItem.iconSpec > 0) ? GetBagItemCountByIcon(shopItem.iconSpec) : 0;
					if (hongNgoc < shopItem.buySpec && rubyCount < shopItem.buySpec && specMaterial < shopItem.buySpec)
					{
						// Nếu ở Tab Bánh và có chọn ĐK 3 (dùng hết bánh trong rương), kiểm tra xem trong rương còn bánh không
						if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) > 0)
						{
							isUsingAllBanh = true;
							stepState = 5;
							stateTimestamp = mSystem.currentTimeMillis();
							Notify("Hết Hồng Ngọc/NL mua -> Tự dùng hết " + targetItemNameBanh + " trong rương...");
							return;
						}

						FinishTabBanh("Hết Hồng Ngọc/NL mua " + targetItemNameBanh);
						return;
					}
				}
				else if (currentRunningTab == 2)
				{
					int zenoCount = GetBagItemCount(ZenoItemId);
					int specMaterial = (shopItem.iconSpec > 0) ? GetBagItemCountByIcon(shopItem.iconSpec) : 0;
					if (zenoCount < shopItem.buySpec && specMaterial < shopItem.buySpec)
					{
						FinishTabHn("Hết nguyên liệu mua " + targetItemNameHn);
						return;
					}
				}
				else
				{
					int currentSpecMaterial = (shopItem.iconSpec > 0) ? GetBagItemCountByIcon(shopItem.iconSpec) : 0;
					if (currentSpecMaterial < shopItem.buySpec)
					{
						return;
					}
				}
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
			Notify("Chưa chọn chức năng nào trong Tab Chung!");
			return;
		}

		isRunningWorkflow = true;
		completedWorkflowLoops = 0;
		progressThisLoop = false;
		queueIndex = 0;
		string loopDesc = (targetWorkflowLoops == 0) ? "Vô hạn" : (targetWorkflowLoops + " vòng");
		Notify("▶ BẮT ĐẦU QUY TRÌNH AUTO GỘP (" + loopDesc + ")");
		StartTab(workflowQueue[0]);
	}

	public void StartTab(int tabId)
	{
		currentRunningTab = tabId;
		isRunning = true;
		stepState = 1;
		stateTimestamp = mSystem.currentTimeMillis();
		retryCount = 0;
		npcRetryCount = 0;
		isWaitingXmap = false;
		xmapTargetMapId = -1;
		xmapTargetNpcId = -1;
		xmapPendingTab = 0;
		xmapArrivedTimestamp = 0;
		xmapStartTimestamp = 0;

		if (hideUi)
		{
			ClearResidualGameUi();
		}
		else
		{
			if (GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.close = true;
				GameCanvas.menu.doCloseMenu();
			}
			ChatTextField.gI().isShow = false;
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
			if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
			{
				isUsingAllBanh = true;
				stepState = 5;
				stateTimestamp = mSystem.currentTimeMillis();
				Notify("▶ " + targetItemNameBanh + " đã đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng hết trong rương...");
				break;
			}
			if (ModFunc.GetNpcByTempId(92) == null && (zenoReturnMapId >= 0 || TileMap.mapID == 46))
			{
				int retMap1 = (zenoReturnMapId >= 0 && zenoReturnMapId != 46) ? zenoReturnMapId : 5;
				if (TileMap.mapID != retMap1)
				{
					StartXmapToNpc(1, retMap1, 92);
					Notify("▶ [Tab Bánh] Đang quay lại Map " + retMap1 + " gặp Goku (92)...");
					return;
				}
			}
			MoveCloseToNpc(92);
			Notify("▶ Bắt đầu [Tab Bánh]: Mua " + targetItemNameBanh + " [" + targetItemIdBanh + "] tại Goku (92)");
			stepState = 1;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 2:
			completedBatchesHn = 0;
			if (CheckHnZenoLack())
			{
				return;
			}
			if (ModFunc.GetNpcByTempId(56) == null && (zenoReturnMapId >= 0 || TileMap.mapID == 46))
			{
				int retMap2 = (zenoReturnMapId >= 0 && zenoReturnMapId != 46) ? zenoReturnMapId : 5;
				if (TileMap.mapID != retMap2)
				{
					StartXmapToNpc(2, retMap2, 56);
					Notify("▶ [Tab HN] Đang quay lại Map " + retMap2 + " gặp NPC 56...");
					return;
				}
			}
			MoveCloseToNpc(56);
			Notify("▶ Bắt đầu [Tab HN]: Mua " + targetItemNameHn + " [" + targetItemIdHn + "] tại NPC 56");
			stepState = 1;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 3:
			completedBatchesDiem = 0;
			if (autoSelectMocDiem && currentDiemPoints >= 0)
			{
				if (currentDiemPoints >= RequiredPoints[0]) pointIndex = 0;
				else if (currentDiemPoints >= RequiredPoints[1]) pointIndex = 1;
				else if (currentDiemPoints >= RequiredPoints[2]) pointIndex = 2;
			}
			if (ModFunc.GetNpcByTempId(66) == null && (zenoReturnMapId >= 0 || TileMap.mapID == 46))
			{
				int retMap3 = (zenoReturnMapId >= 0 && zenoReturnMapId != 46) ? zenoReturnMapId : 5;
				if (TileMap.mapID != retMap3)
				{
					StartXmapToNpc(3, retMap3, 66);
					Notify("▶ [Tab Điểm] Đang quay lại Map " + retMap3 + " gặp Nồi Bánh (66)...");
					return;
				}
			}
			MoveCloseToNpc(66);
			string mocStr = autoSelectMocDiem ? ("Tự động (" + MocShortNames[pointIndex] + ")") : MocShortNames[pointIndex];
			Notify("▶ Bắt đầu [Tab Điểm]: Đổi Nồi Bánh mốc " + mocStr);
			stepState = 1;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 4:
			completedBatchesZeno = 0;
			zenoMenuStep = 0;
			lastZenoMenu2Idx = 0;
			lastZenoDebug = "StartTab";
			lastBagCheckTime = 0;
			int zenoCount = GetBagItemCount(1583);
			if (zenoCount < batchQtyZeno)
			{
				FinishZenoLack("■ Vé Zeno (" + zenoCount + ") < " + batchQtyZeno);
				return;
			}
			if (ModFunc.GetNpcByTempId(18) == null)
			{
				if (TileMap.mapID != 46)
				{
					zenoReturnMapId = TileMap.mapID;
					StartXmapToNpc(4, 46, 18);
					Notify("▶ [Tab Zeno] Đang tự di chuyển tới Tháp Karin (Map 46)...");
				}
				else
				{
					StartXmapToNpc(4, 46, 18);
					Notify("▶ [Tab Zeno] Đang chờ tải NPC Thần Mèo Karin (18)...");
				}
				return;
			}
			MoveCloseToZenoNpc();
			Notify("▶ Bắt đầu [Tab Zeno]: Đổi " + batchQtyZeno + " Tiên Phù tại Karin (18)");
			lastZenoDebug = "SentOpenMenu";
			Service.gI().openMenu(18);
			stepState = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			break;
		}
	}

	private void StartXmapToNpc(int tabId, int targetMapId, int npcId)
	{
		isWaitingXmap = true;
		xmapTargetMapId = targetMapId;
		xmapTargetNpcId = npcId;
		xmapPendingTab = tabId;
		xmapArrivedTimestamp = 0;
		xmapStartTimestamp = mSystem.currentTimeMillis();
		retryCount = 0;
		stepState = 7;
		stateTimestamp = xmapStartTimestamp;
		if (TileMap.mapID != targetMapId)
		{
			XmapController.FinishXmap();
			XmapController.StartRunToMapId(targetMapId);
		}
	}

	// === CÁC HÀM XỬ LÝ KẾT THÚC & RẼ NHÁNH TỪNG TAB ===
	public void FinishTabBanh(string reason)
	{
		if (enableRoutingBanh && routeTargetTabBanh >= 1 && routeTargetTabBanh <= 4 && routeTargetTabBanh != 1)
		{
			Notify("Tab Bánh (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabBanh));
			StartTab(routeTargetTabBanh);
		}
		else
		{
			OnTabCompleted(1, reason);
		}
	}

	public void FinishTabHn(string reason)
	{
		if (enableRoutingHn && routeTargetTabHn >= 1 && routeTargetTabHn <= 4 && routeTargetTabHn != 2)
		{
			Notify("Tab HN (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabHn));
			StartTab(routeTargetTabHn);
		}
		else
		{
			OnTabCompleted(2, reason);
		}
	}

	public void FinishTabDiem(string reason)
	{
		if (enableRoutingDiem && routeTargetTabDiem >= 1 && routeTargetTabDiem <= 4 && routeTargetTabDiem != 3)
		{
			Notify("Tab Điểm (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabDiem));
			StartTab(routeTargetTabDiem);
		}
		else
		{
			OnTabCompleted(3, reason);
		}
	}

	public void FinishZenoDone(string reason)
	{
		if (enableRouteDoneZeno && routeTargetWhenDoneZeno >= 1 && routeTargetWhenDoneZeno <= 4 && routeTargetWhenDoneZeno != 4)
		{
			Notify("▶ Zeno đạt mục tiêu -> Chuyển sang Tab " + GetTabName(routeTargetWhenDoneZeno));
			StartTab(routeTargetWhenDoneZeno);
		}
		else
		{
			OnTabCompleted(4, reason);
		}
	}

	public void FinishZenoLack(string reason)
	{
		if (enableRouteLackZeno && routeTargetWhenLackZeno >= 1 && routeTargetWhenLackZeno <= 4 && routeTargetWhenLackZeno != 4)
		{
			Notify((reason.StartsWith("■") ? reason : ("■ " + reason)) + " -> Chuyển sang Tab " + GetTabName(routeTargetWhenLackZeno));
			StartTab(routeTargetWhenLackZeno);
		}
		else
		{
			OnTabCompleted(4, reason);
		}
	}

	public void OnTabCompleted(int tabId, string reason)
	{
		Notify("Hoàn tất Tab " + GetTabName(tabId) + ": " + reason);

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
			Notify("Chờ " + delayBetweenTabsMs + "ms -> Chuyển sang Tab " + GetTabName(nextTab));
			stepState = 6; // Chờ delay chuyển giao
			stateTimestamp = mSystem.currentTimeMillis();
		}
		else
		{
			completedWorkflowLoops++;
			if (!progressThisLoop)
			{
				StopAll("■ Dừng: Cả vòng không mua/đổi được gì (hết tài nguyên toàn bộ)");
				return;
			}
			progressThisLoop = false;
			if (targetWorkflowLoops == 0 || completedWorkflowLoops < targetWorkflowLoops)
			{
				queueIndex = 0;
				string loopInfo = (targetWorkflowLoops == 0)
					? (completedWorkflowLoops + "/∞")
					: (completedWorkflowLoops + "/" + targetWorkflowLoops);
				Notify("Xong vòng " + loopInfo + " -> Lặp lại từ Tab " + GetTabName(workflowQueue[0]));
				stepState = 6;
				stateTimestamp = mSystem.currentTimeMillis();
			}
			else
			{
				StopAll("ĐÃ HOÀN THÀNH " + completedWorkflowLoops + "/" + targetWorkflowLoops + " VÒNG QUY TRÌNH AUTO GỘP!");
			}
		}
	}

	public void StopAll(string reason = "Đã dừng tất cả (F6)")
	{
		isRunning = false;
		isRunningWorkflow = false;
		currentRunningTab = 0;
		stepState = 0;
		isUsingAllBanh = false;
		retryCount = 0;
		npcRetryCount = 0;
		zenoMenuStep = 0;
		lastZenoMenu2Idx = 0;
		isWaitingXmap = false;
		xmapTargetMapId = -1;
		xmapTargetNpcId = -1;
		xmapPendingTab = 0;
		xmapArrivedTimestamp = 0;
		xmapStartTimestamp = 0;
		zenoReturnMapId = -1;
		lastZenoDebug = string.Empty;
		if (AutoXmap.IsXmapRunning)
		{
			XmapController.FinishXmap();
		}
		lastStopTimeMs = mSystem.currentTimeMillis();
		if (hideUi)
		{
			ClearResidualGameUi();
		}
		else
		{
			if (GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.close = true;
				GameCanvas.menu.doCloseMenu();
			}
			ChatTextField.gI().isShow = false;
		}
		if (!string.IsNullOrEmpty(reason))
		{
			Notify(reason);
		}
	}

	// === TRÍCH XUẤT ĐIỂM REAL-TIME TỪ LỜI THOẠI MENU 1 CỦA NPC 66 ===
	public void ParseDiemPoints(string text, int npcId = 66, string[] menu = null)
	{
		if (npcId != 66 || string.IsNullOrEmpty(text))
		{
			return;
		}

		// Nếu có danh sách menu đi kèm, kiểm tra xem đây là Menu 1 hay Menu 2
		if (menu != null)
		{
			bool isMenu1 = false;
			for (int i = 0; i < menu.Length; i++)
			{
				if (menu[i] == null)
				{
					continue;
				}
				string m = menu[i].ToLower();
				// Menu 2 chứa các mốc 10.000.000, 5.000, 3.000 -> tuyệt đối không parse lời thoại của Menu 2!
				if (m.Contains("10.000.000") || m.Contains("5.000") || m.Contains("3.000"))
				{
					return;
				}
				if (m.Contains("đổi điểm") || m.Contains("thể lệ") || m.Contains("làm bánh"))
				{
					isMenu1 = true;
				}
			}
			if (!isMenu1)
			{
				return;
			}
		}

		string lowerText = text.ToLower();
		// Chặn trường hợp lời thoại liệt kê danh sách các mốc điểm (10.000.000 ... 5.000 ...)
		if (lowerText.Contains("10.000.000") && (lowerText.Contains("5.000") || lowerText.Contains("3.000")))
		{
			return;
		}

		try
		{
			Match match = Regex.Match(text, @"(?:đang có|hiện có|hiện tại|bạn có|số điểm|điểm của bạn|điểm sự kiện)[\s:là]*([0-9.,]+)", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				match = Regex.Match(text, @"([0-9.,]+)\s*(?:điểm|diem)", RegexOptions.IgnoreCase);
			}
			if (!match.Success)
			{
				match = Regex.Match(text, @"(?:điểm|diem|có|co)[\s:]*([0-9.,]+)", RegexOptions.IgnoreCase);
			}
			if (match.Success)
			{
				string numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "").Trim();
				if (int.TryParse(numStr, out var pt))
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

		// Trích xuất điểm Real-time nếu là Menu 1 của Nồi Bánh (66)
		if (npcTempId == 66 && !string.IsNullOrEmpty(chatText))
		{
			ParseDiemPoints(chatText, npcTempId, menu);
		}

		if (!isRunning)
		{
			return false;
		}

		npcRetryCount = 0;

		// === TAB 1: BÁNH (NPC 92) ===
		if (currentRunningTab == 1 && npcTempId == 92)
		{
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
				if (menu[k] != null && menu[k].ToLower().Contains("đổi điểm"))
				{
					isMenu1 = true;
					break;
				}
			}

			// Tự động chọn mốc từ cao xuống thấp (10Tr -> 5K -> 3K)
			if (autoSelectMocDiem && currentDiemPoints >= 0)
			{
				if (currentDiemPoints >= RequiredPoints[0])
				{
					pointIndex = 0; // 10Tr
				}
				else if (currentDiemPoints >= RequiredPoints[1])
				{
					pointIndex = 1; // 5K
				}
				else if (currentDiemPoints >= RequiredPoints[2])
				{
					pointIndex = 2; // 3K
				}
			}

			int minRequired = autoSelectMocDiem ? RequiredPoints[2] : RequiredPoints[pointIndex];

			// Nếu điểm hiện có nhỏ hơn mốc tối thiểu cần đổi -> Dừng hoặc Rẽ nhánh ngay!
			if (currentDiemPoints >= 0 && currentDiemPoints < minRequired)
			{
				string reqName = autoSelectMocDiem ? "3.000đ" : MocFullNames[pointIndex];
				FinishTabDiem("Điểm (" + NinjaUtil.getMoneys(currentDiemPoints) + ") < " + reqName);
				return hideUi;
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
				int pick = (pointIndex >= 0 && pointIndex < menu.Length) ? pointIndex : 1;
				completedBatchesDiem++;
				progressThisLoop = true;
				if (currentDiemPoints >= RequiredPoints[pick])
				{
					currentDiemPoints -= RequiredPoints[pick];
				}
				Service.gI().confirmMenu(66, (sbyte)pick);

				// Đánh giá lại mốc tự động sau khi trừ điểm
				if (autoSelectMocDiem && currentDiemPoints >= 0)
				{
					if (currentDiemPoints >= RequiredPoints[0]) pointIndex = 0;
					else if (currentDiemPoints >= RequiredPoints[1]) pointIndex = 1;
					else if (currentDiemPoints >= RequiredPoints[2]) pointIndex = 2;
				}

				int nextMinRequired = autoSelectMocDiem ? RequiredPoints[2] : RequiredPoints[pointIndex];

				if (stopConditionDiem == 1 && targetBatchesDiem > 0 && completedBatchesDiem >= targetBatchesDiem)
				{
					FinishTabDiem("Đã đổi đủ " + completedBatchesDiem + " lượt (" + MocShortNames[pick] + ")");
				}
				else if (stopConditionDiem == 2 && currentDiemPoints >= 0 && currentDiemPoints < nextMinRequired)
				{
					string reqName = autoSelectMocDiem ? "3.000đ" : MocFullNames[pointIndex];
					FinishTabDiem("Hết điểm đổi (" + NinjaUtil.getMoneys(currentDiemPoints) + " < " + reqName + ")");
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
			if (isWaitingXmap || stepState == 7)
			{
				return false;
			}
			if (stepState == 5 || stepState == 6 || stepState == 7)
			{
				return hideUi;
			}
			string[] norm = new string[menu.Length];
			for (int l = 0; l < menu.Length; l++)
			{
				norm[l] = NormalizeMenuText(menu[l]);
			}
			MaybeResetZenoMenuStepIfMainMenu(norm);
			int chosenIdx = FindZenoMenuOptionIndex(norm, zenoMenuStep);
			if (chosenIdx < 0)
			{
				chosenIdx = 0;
			}
			lastZenoDebug = "s=" + zenoMenuStep + ",i=" + chosenIdx + ",t=32,n=" + norm.Length + ",m0=" + (norm.Length > 0 ? norm[0] : "");
			AdvanceZenoMenuStep(chosenIdx, 32, 0, 0);
			Service.gI().confirmMenu(18, (sbyte)chosenIdx);
			return hideUi;
		}

		return false;
	}

	public bool OnReceiveMenu(string[] menu, Npc npc)
	{
		return OnReceiveMenu(menu, npc, "");
	}

	public bool OnReceiveOpenUiMenu(MyVector menuCmds)
	{
		if (!isRunning || currentRunningTab != 4 || menuCmds == null || menuCmds.size() == 0)
		{
			return false;
		}
		if (isWaitingXmap || stepState == 7)
		{
			return false;
		}
		Npc focus = (Char.myCharz() != null) ? Char.myCharz().npcFocus : null;
		if (focus == null)
		{
			focus = ModFunc.GetNpcByTempId(18);
		}
		if (focus != null && focus.template != null && focus.template.npcTemplateId != 18)
		{
			return false;
		}
		if (stepState == 5 || stepState == 6 || stepState == 7)
		{
			return hideUi;
		}
		List<string> normList = new List<string>();
		List<int> menuIdxList = new List<int>();
		List<int> subIdxList = new List<int>();
		for (int i = 0; i < menuCmds.size(); i++)
		{
			Command cmd = menuCmds.elementAt(i) as Command;
			if (cmd == null)
			{
				continue;
			}
			if (cmd.idAction == 88820 && cmd.p is string[] arr && arr.Length > 1)
			{
				for (int k = 1; k < arr.Length; k++)
				{
					normList.Add(NormalizeMenuText((arr[0] ?? "") + " " + (arr[k] ?? "")));
					menuIdxList.Add(i);
					subIdxList.Add(k - 1);
				}
			}
			else
			{
				normList.Add(NormalizeMenuText(cmd.caption));
				menuIdxList.Add(i);
				subIdxList.Add(0);
			}
		}
		if (normList.Count == 0)
		{
			return false;
		}
		MaybeResetZenoMenuStepIfMainMenu(normList);
		int flatIdx = FindZenoMenuOptionIndex(normList.ToArray(), zenoMenuStep);
		if (flatIdx < 0)
		{
			flatIdx = 0;
		}
		int chosenMenuIdx = menuIdxList[flatIdx];
		int chosenSubIdx = subIdxList[flatIdx];
		lastZenoDebug = "s=" + zenoMenuStep + ",i=" + chosenMenuIdx + "/" + chosenSubIdx + ",t=33,n=" + normList.Count + ",m0=" + normList[0];
		AdvanceZenoMenuStep(chosenMenuIdx, 33, chosenSubIdx, 0);
		Service.gI().menu(18, chosenMenuIdx, chosenSubIdx);
		return hideUi;
	}

	public bool OnReceiveNewMenu(MyVector menuCmds)
	{
		if (!isRunning || currentRunningTab != 4 || menuCmds == null || menuCmds.size() == 0)
		{
			return false;
		}
		if (isWaitingXmap || stepState == 7)
		{
			return false;
		}
		if (stepState == 5 || stepState == 6 || stepState == 7)
		{
			return hideUi;
		}
		string[] norm = new string[menuCmds.size()];
		short[] menuIds = new short[menuCmds.size()];
		for (int i = 0; i < menuCmds.size(); i++)
		{
			Command cmd = menuCmds.elementAt(i) as Command;
			norm[i] = (cmd != null) ? NormalizeMenuText(cmd.caption) : "";
			menuIds[i] = (cmd != null && cmd.p is short idVal) ? idVal : (short)0;
		}
		MaybeResetZenoMenuStepIfMainMenu(norm);
		int flatIdx = FindZenoMenuOptionIndex(norm, zenoMenuStep);
		if (flatIdx < 0)
		{
			flatIdx = 0;
		}
		short targetMenuId = menuIds[flatIdx];
		lastZenoDebug = "s=" + zenoMenuStep + ",i=" + flatIdx + ",t=27,id=" + targetMenuId + ",n=" + norm.Length + ",m0=" + (norm.Length > 0 ? norm[0] : "");
		AdvanceZenoMenuStep(flatIdx, 27, 0, targetMenuId);
		Service.gI().menuId(targetMenuId);
		return hideUi;
	}

	public bool OnReceiveServerInputPacket(Message msg)
	{
		if (!isRunning)
		{
			return false;
		}

		npcRetryCount = 0;

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

				if (currentRunningTab == 4)
				{
					int fieldCount = System.Math.Max(1, (int)nFields);
					TField[] fields = new TField[fieldCount];
					for (int i = 0; i < fieldCount; i++)
					{
						fields[i] = new TField();
						fields[i].setText(qtyToSend.ToString());
					}
					Service.gI().sendClientInput(fields);
				}
				else
				{
					SendClientInputDirect(qtyToSend.ToString());
				}
				progressThisLoop = true;

				if (currentRunningTab == 1)
				{
					completedBatchesBanh++;
					retryCount = 0;
					// ĐK 3: Số bánh trong rương đạt mốc -> Tự dùng hết bánh trong rương
					if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
					{
						isUsingAllBanh = true;
						stepState = 5;
						stateTimestamp = mSystem.currentTimeMillis();
						Notify(targetItemNameBanh + " đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng hết trong rương...");
					}
					else if (stopBanhCond1 && targetBatchesBanh > 0 && completedBatchesBanh >= targetBatchesBanh)
					{
						FinishTabBanh("Đã mua đủ " + completedBatchesBanh + " đợt " + targetItemNameBanh + " (ĐK 1)");
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
					retryCount = 0;
					if (autoUseHn)
					{
						UseItemInBag(targetItemIdHn);
					}

					if (stopConditionHn == 1 && targetBatchesHn > 0 && completedBatchesHn >= targetBatchesHn)
					{
						FinishTabHn("Đã mua đủ " + completedBatchesHn + " đợt " + targetItemNameHn);
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
					retryCount = 0;
					zenoMenuStep = 0;
					lastBagCheckTime = 0;
					Notify("▶ Đã đổi " + qtyToSend + " Tiên Phù Zeno (Đợt " + completedBatchesZeno + ")");

					// Nhánh 1: Đạt mục tiêu chỉ định
					if (targetBatchesZeno > 0 && completedBatchesZeno >= targetBatchesZeno)
					{
						FinishZenoDone("▶ Đã đổi đủ " + completedBatchesZeno + " đợt Zeno");
					}
					else
					{
						// Kiểm tra lại số vé trong hành trang
						int currentTicket = GetBagItemCount(1583);
						if (currentTicket < batchQtyZeno)
						{
							FinishZenoLack("■ Vé Zeno (" + currentTicket + ") < " + batchQtyZeno);
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
			catch (Exception ex)
			{
				Notify("■ Lỗi xử lý gói -125: " + ex.Message);
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

		if (!isRunning || suppressInfoHook || inServerInfo)
		{
			return;
		}

		bool narrowMatch = lower.Contains("hành trang đã đầy")
			|| lower.Contains("hành trang đầy")
			|| lower.Contains("rương đã đầy")
			|| lower.Contains("chỗ trống")
			|| lower.Contains("ô trống")
			|| lower.Contains("không đủ")
			|| lower.Contains("không có đủ")
			|| lower.Contains("còn thiếu")
			|| lower.Contains("hết điểm")
			|| lower.Contains("hết vé");

		// Từ khóa mở rộng chỉ áp dụng khi đang chờ phản hồi mua (Bánh/HN) để tránh bắt nhầm
		bool waitingBuy = (currentRunningTab == 1 || currentRunningTab == 2) && (stepState == 3 || stepState == 4);
		bool wideMatch = waitingBuy && (lower.Contains("thiếu")
			|| lower.Contains("cần có")
			|| lower.Contains("cần thêm")
			|| lower.Contains("không thể mua")
			|| lower.Contains("thất bại")
			|| lower.Contains("hết nguyên liệu")
			|| lower.Contains("chưa có"));

		bool isResourceEmptyOrBagFull = narrowMatch || wideMatch;

		if (!isResourceEmptyOrBagFull)
		{
			return;
		}

		inServerInfo = true;
		try
		{
			HandleResourceEmpty(s, lower);
		}
		finally
		{
			inServerInfo = false;
		}
	}

	private void HandleResourceEmpty(string s, string lower)
	{
		bool isResourceEmptyOrBagFull = true;
		if (isResourceEmptyOrBagFull)
		{
			if (hideUi)
			{
				GameCanvas.endDlg();
				if (GameCanvas.menu.showMenu)
				{
					GameCanvas.menu.doCloseMenu();
				}
			}

			if (currentRunningTab == 1)
			{
				bool isBagFull = lower.Contains("hành trang đã đầy") || lower.Contains("hành trang đầy") || lower.Contains("rương đã đầy") || lower.Contains("chỗ trống") || lower.Contains("ô trống");
				if (isUsingAllBanh)
				{
					// Nếu đang dùng bánh mà bị đầy rương (không thể dùng tiếp) -> hoàn tất/chuyển tab ngay
					if (isBagFull)
					{
						isUsingAllBanh = false;
						FinishTabBanh("Đầy rương khi đang dùng bánh (" + s + ")");
					}
				}
				else if (stopBanhCond2)
				{
					// ĐK 2 được tích: Đầy rương hoặc Hết tài nguyên -> thực hiện dừng/chuyển tab ngay
					FinishTabBanh("Đầy rương / Hết tài nguyên (" + s + ") (ĐK 2)");
				}
				else if (stopBanhCond3 && !isBagFull && GetBagItemCount(targetItemIdBanh) > 0)
				{
					// Chỉ tích ĐK 3 mà hết tài nguyên mua (rương chưa đầy, vẫn còn bánh) -> dùng hết số bánh đang có
					isUsingAllBanh = true;
					stepState = 5;
					stateTimestamp = mSystem.currentTimeMillis();
					Notify("Hết TN -> Tự dùng hết " + targetItemNameBanh + " trong rương...");
				}
				else
				{
					FinishTabBanh("Đầy rương / Hết tài nguyên (" + s + ")");
				}
			}
			else if (currentRunningTab == 2)
			{
				FinishTabHn("Đầy rương / Hết tài nguyên (" + s + ")");
			}
			else if (currentRunningTab == 3)
			{
				FinishTabDiem("Đầy rương / Hết điểm (" + s + ")");
			}
			else if (currentRunningTab == 4)
			{
				FinishZenoLack("Hết vé Zeno / Đầy rương (" + s + ")");
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
				isRunningWorkflow = false;
				StartTab(1);
			}
			else if (Input.GetKeyDown(KeyCode.F5))
			{
				isRunningWorkflow = false;
				StartTab(2);
			}
			else if (Input.GetKeyDown(KeyCode.F6) || (isRunning && Input.GetKey(KeyCode.A)))
			{
				StopAll("Đã dừng tất cả (F6 / Phím A)");
				return;
			}
			else if (Input.GetKeyDown(KeyCode.F7))
			{
				isRunningWorkflow = false;
				StartTab(3);
			}
			else if (Input.GetKeyDown(KeyCode.F8))
			{
				isRunningWorkflow = false;
				StartTab(4);
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

		if (hideUi && !isWaitingXmap && stepState != 7)
		{
			if (currentRunningTab == 4)
			{
				if (GameCanvas.panel != null && GameCanvas.panel.isShow)
				{
					GameCanvas.panel.hideNow();
					GameCanvas.panel.isShow = false;
				}
				if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				{
					GameCanvas.panel2.hideNow();
					GameCanvas.panel2.isShow = false;
				}
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
				{
					GameCanvas.menu.close = true;
					GameCanvas.menu.doCloseMenu();
				}
				if (GameCanvas.currentScreen is ClientInput && GameScr.instance != null)
				{
					GameScr.instance.switchToMe();
				}
			}
			else
			{
				ClearResidualGameUi();
			}
		}

		long now = mSystem.currentTimeMillis();

		// stepState == 7: Chờ Xmap tới Map chứa NPC
		if (stepState == 7 && isWaitingXmap)
		{
			if (TileMap.mapID == xmapTargetMapId)
			{
				if (xmapArrivedTimestamp == 0)
				{
					xmapArrivedTimestamp = now;
				}
				Npc targetNpc = (xmapTargetNpcId >= 0) ? ModFunc.GetNpcByTempId(xmapTargetNpcId) : null;
				if (targetNpc != null)
				{
					if (AutoXmap.IsXmapRunning)
					{
						XmapController.FinishXmap();
					}
					isWaitingXmap = false;
					xmapArrivedTimestamp = 0;
					retryCount = 0;
					if (currentRunningTab == 4)
					{
						zenoMenuStep = 0;
						lastZenoMenu2Idx = 0;
						lastZenoDebug = string.Empty;
						MoveCloseToZenoNpc();
						Notify("▶ [Tab Zeno] Đã tới Karin (18), bắt đầu đổi " + batchQtyZeno + " Tiên Phù...");
						Service.gI().openMenu(18);
						stepState = 2;
					}
					else
					{
						MoveCloseToNpc(xmapTargetNpcId);
						Notify("▶ [" + GetTabName(currentRunningTab) + "] Đã tới Map " + xmapTargetMapId + ", bắt đầu tương tác NPC " + xmapTargetNpcId);
						stepState = 1;
					}
					stateTimestamp = now;
				}
				else if (now - xmapArrivedTimestamp >= 3500)
				{
					if (AutoXmap.IsXmapRunning)
					{
						XmapController.FinishXmap();
					}
					isWaitingXmap = false;
					xmapArrivedTimestamp = 0;
					int failedTab = currentRunningTab;
					if (failedTab == 4) FinishZenoLack("■ Không tìm thấy NPC Thần Mèo Karin (18) tại Map " + xmapTargetMapId);
					else if (failedTab == 1) FinishTabBanh("■ Không tìm thấy NPC 92 tại Map " + xmapTargetMapId);
					else if (failedTab == 2) FinishTabHn("■ Không tìm thấy NPC 56 tại Map " + xmapTargetMapId);
					else if (failedTab == 3) FinishTabDiem("■ Không tìm thấy NPC 66 tại Map " + xmapTargetMapId);
				}
			}
			else
			{
				if (!AutoXmap.IsXmapRunning && !XmapData.GI().IsLoading && !Char.isLoadingMap && !Char.ischangingMap && now - stateTimestamp >= 2000)
				{
					if (retryCount < 1)
					{
						retryCount++;
						stateTimestamp = now;
						XmapController.FinishXmap();
						XmapController.StartRunToMapId(xmapTargetMapId);
					}
					else if (now - stateTimestamp >= 4000)
					{
						isWaitingXmap = false;
						int failedTab = currentRunningTab;
						if (failedTab == 4) FinishZenoLack("■ Không thể Xmap tới Map " + xmapTargetMapId);
						else if (failedTab == 1) FinishTabBanh("■ Không thể Xmap tới Map " + xmapTargetMapId);
						else if (failedTab == 2) FinishTabHn("■ Không thể Xmap tới Map " + xmapTargetMapId);
						else if (failedTab == 3) FinishTabDiem("■ Không thể Xmap tới Map " + xmapTargetMapId);
					}
				}
				long xmapElapsedBase = (xmapStartTimestamp > 0) ? xmapStartTimestamp : stateTimestamp;
				if (isRunning && now - xmapElapsedBase >= 45000)
				{
					if (AutoXmap.IsXmapRunning)
					{
						XmapController.FinishXmap();
					}
					isWaitingXmap = false;
					int failedTab = currentRunningTab;
					if (failedTab == 4) FinishZenoLack("■ Quá thời gian di chuyển tới Tháp Karin (Map " + xmapTargetMapId + ")");
					else if (failedTab == 1) FinishTabBanh("■ Quá thời gian quay lại Map " + xmapTargetMapId);
					else if (failedTab == 2) FinishTabHn("■ Quá thời gian quay lại Map " + xmapTargetMapId);
					else if (failedTab == 3) FinishTabDiem("■ Quá thời gian quay lại Map " + xmapTargetMapId);
				}
			}
			return;
		}

		// stepState == 1: Chuẩn bị di chuyển đến NPC và mở Menu
		if (stepState == 1 && currentRunningTab >= 1 && currentRunningTab <= 4)
		{
			if (now - stateTimestamp >= 200)
			{
				int npcId = GetCurrentTargetNpcId();
				if (npcId < 0)
				{
					return;
				}
				if (ModFunc.GetNpcByTempId(npcId) != null)
				{
					stepState = 2;
					stateTimestamp = now;
					if (currentRunningTab == 4)
					{
						zenoMenuStep = 0;
						lastZenoMenu2Idx = 0;
						MoveCloseToZenoNpc();
					}
					else
					{
						MoveCloseToNpc(npcId);
					}
					Service.gI().openMenu(npcId);
				}
				else if (now - stateTimestamp >= 3500)
				{
					npcRetryCount++;
					if (npcRetryCount > 2)
					{
						npcRetryCount = 0;
						if (currentRunningTab == 1) FinishTabBanh("■ Không thấy NPC 92 trong Map");
						else if (currentRunningTab == 2) FinishTabHn("■ Không thấy NPC 56 trong Map");
						else if (currentRunningTab == 3) FinishTabDiem("■ Không thấy NPC 66 trong Map");
						else if (currentRunningTab == 4) FinishZenoLack("■ Không thấy NPC 18 trong Map");
					}
					else
					{
						stateTimestamp = now;
					}
				}
			}
			return;
		}

		// Giữ vững npcFocus trong khi chờ Menu phản hồi (tránh Char.autoFocus xóa mất npcFocus trước khi gói 33 tới)
		if (stepState == 2 && currentRunningTab >= 1 && currentRunningTab <= 4)
		{
			int focusNpcId = GetCurrentTargetNpcId();
			Npc focusNpc = (focusNpcId >= 0) ? ModFunc.GetNpcByTempId(focusNpcId) : null;
			if (focusNpc != null && Char.myCharz() != null && (Char.myCharz().npcFocus == null || Char.myCharz().npcFocus.template == null || Char.myCharz().npcFocus.template.npcTemplateId != focusNpcId))
			{
				if (currentRunningTab != 4)
				{
					Char.myCharz().FocusManualTo(focusNpc);
				}
				Char.myCharz().npcFocus = focusNpc;
			}
		}

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

		// Xử lý cơ chế dùng hết bánh ở Tab 1
		if (currentRunningTab == 1 && isUsingAllBanh)
		{
			if (now - stateTimestamp >= delayUseBanhMs)
			{
				stateTimestamp = now;
				if (!UseItemInBag(targetItemIdBanh))
				{
					// Đã dùng sạch bánh trong rương!
					isUsingAllBanh = false;
					FinishTabBanh("Đã dùng hết toàn bộ " + targetItemNameBanh + " trong rương");
				}
			}
			return;
		}

		// stepState == 3: Chờ mở Shop và gửi lệnh Mua lần đầu
		if (stepState == 3 && (currentRunningTab == 1 || currentRunningTab == 2))
		{
			if (now - stateTimestamp >= 200)
			{
				TriggerBuyCurrentTab();
			}
			return;
		}

		// stepState == 4: Chờ gói -125 từ Server
		if (stepState == 4)
		{
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount <= 1)
				{
					stateTimestamp = now;
					if (currentRunningTab == 1 || currentRunningTab == 2)
					{
						TriggerBuyCurrentTab();
					}
					else if (currentRunningTab == 4)
					{
						SendZenoMenuRetry();
					}
				}
				else
				{
					retryCount = 0;
					if (currentRunningTab == 1)
					{
						FinishTabBanh("Hết nguyên liệu / Server không gửi gói -125");
					}
					else if (currentRunningTab == 2)
					{
						FinishTabHn("Hết nguyên liệu / Server không gửi gói -125");
					}
					else if (currentRunningTab == 4)
					{
						zenoMenuStep = 0;
						FinishZenoLack(string.IsNullOrEmpty(lastZenoDebug)
							? "■ Hết vé Zeno / Server không gửi gói -125"
							: ("■ Hết vé Zeno / Không thấy gói -125 (" + lastZenoDebug + ")"));
					}
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
					// Kiểm tra lại mốc bánh rương cho ĐK 3 trước khi mua tiếp
					if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
					{
						isUsingAllBanh = true;
						Notify(targetItemNameBanh + " đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng hết trong rương...");
						return;
					}
					TriggerBuyCurrentTab();
				}
				else if (currentRunningTab == 2)
				{
					TriggerBuyCurrentTab();
				}
				else if (currentRunningTab == 3)
				{
					MoveCloseToNpc(66);
					Service.gI().openMenu(66);
					stepState = 2;
				}
				else if (currentRunningTab == 4)
				{
					lastBagCheckTime = 0;
					int currentTicket = GetBagItemCount(1583);
					if (currentTicket < batchQtyZeno)
					{
						FinishZenoLack("■ Vé Zeno (" + currentTicket + ") < " + batchQtyZeno);
						return;
					}
					zenoMenuStep = 0;
					lastZenoMenu2Idx = 0;
					retryCount = 0;
					npcRetryCount = 0;
					stateTimestamp = now;
					MoveCloseToZenoNpc();
					Service.gI().openMenu(18);
					stepState = 2;
				}
			}
			return;
		}

		// Timeout bảo vệ nếu server lag quá 3.5s (áp dụng cho cả stepState 1, 2, 3)
		if ((stepState == 1 || stepState == 2 || stepState == 3) && now - stateTimestamp >= 3500)
		{
			stateTimestamp = now;
			npcRetryCount++;
			if (npcRetryCount > 2)
			{
				npcRetryCount = 0;
				if (currentRunningTab == 1) FinishTabBanh("Không mở được NPC 92 (quá 3 lần)");
				else if (currentRunningTab == 2) FinishTabHn("Không mở được NPC 56 (quá 3 lần)");
				else if (currentRunningTab == 3) FinishTabDiem("Không mở được NPC 66 (quá 3 lần)");
				else if (currentRunningTab == 4)
				{
					zenoMenuStep = 0;
					lastZenoMenu2Idx = 0;
					FinishZenoLack(string.IsNullOrEmpty(lastZenoDebug)
						? "■ Không mở được NPC 18 (quá 3 lần)"
						: ("■ Không mở được NPC 18 (" + lastZenoDebug + ")"));
				}
				return;
			}
			stepState = 1;
			if (currentRunningTab == 1)
			{
				MoveCloseToNpc(92);
			}
			else if (currentRunningTab == 2)
			{
				MoveCloseToNpc(56);
			}
			else if (currentRunningTab == 3)
			{
				MoveCloseToNpc(66);
			}
			else if (currentRunningTab == 4)
			{
				zenoMenuStep = 0;
				lastZenoMenu2Idx = 0;
				MoveCloseToZenoNpc();
				Service.gI().openMenu(18);
				stepState = 2;
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
			case 16: routeTargetTabBanh = System.Math.Max(1, System.Math.Min(4, val)); break;
			// Tab 2 HN
			case 20: targetItemIdHn = System.Math.Max(0, val); break;
			case 21: batchQtyHn = System.Math.Max(1, System.Math.Min(1000, val)); break;
			case 22: targetBatchesHn = System.Math.Max(0, val); break;
			case 23: delayBuyHnMs = System.Math.Max(50, val); break;
			case 24: routeTargetTabHn = System.Math.Max(1, System.Math.Min(4, val)); break;
			// Tab 3 Điểm
			case 31: targetBatchesDiem = System.Math.Max(0, val); break;
			case 32: delayDiemMs = System.Math.Max(50, val); break;
			case 33: routeTargetTabDiem = System.Math.Max(1, System.Math.Min(4, val)); break;
			// Tab 4 Zeno
			case 41: batchQtyZeno = System.Math.Max(1, val); break;
			case 42: targetBatchesZeno = System.Math.Max(0, val); break;
			case 43: delayZenoMs = System.Math.Max(50, val); break;
			case 44: routeTargetWhenDoneZeno = System.Math.Max(1, System.Math.Min(4, val)); break;
			case 45: routeTargetWhenLackZeno = System.Math.Max(1, System.Math.Min(4, val)); break;
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
			int tX = winX + 8 + t * 43;
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
			int rY = winY + 48;
			// Checkbox + Order Bánh
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 110, 15))
			{
				enableBanh = !enableBanh;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(2, "Thứ tự ưu tiên Bánh (1-4)", orderBanh.ToString());
				return;
			}

			// Checkbox + Order HN
			rY += 16;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 110, 15))
			{
				enableHn = !enableHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(3, "Thứ tự ưu tiên HN (1-4)", orderHn.ToString());
				return;
			}

			// Checkbox + Order Điểm
			rY += 16;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 110, 15))
			{
				enableDiem = !enableDiem;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(4, "Thứ tự ưu tiên Điểm (1-4)", orderDiem.ToString());
				return;
			}

			// Checkbox + Order Zeno
			rY += 16;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 110, 15))
			{
				enableZeno = !enableZeno;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(5, "Thứ tự ưu tiên Zeno (1-4)", orderZeno.ToString());
				return;
			}

			// Delay chuyển tiếp
			rY += 16;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(1, "Delay chuyển giao các Tab (ms)", delayBetweenTabsMs.ToString());
				return;
			}

			// Số vòng lặp mỗi lượt (0 = vô hạn)
			rY += 16;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(6, "Số vòng lặp quy trình (0 = Vô hạn)", targetWorkflowLoops.ToString());
				return;
			}

			// Checkbox Tự bật Mua Nhiều (muanhieu)
			rY += 16;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 190, 15))
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

			// Tối ưu giật lag: Checkbox Ẩn người & quái / Tắt chữ bay
			rY += 16;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 100, 15))
			{
				hidePlayersAndMobs = !hidePlayersAndMobs;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 115, rY, 100, 15))
			{
				hideFlyInfoMessages = !hideFlyInfoMessages;
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// === TAB 1: BÁNH ===
		if (selectedTab == 1)
		{
			// Click dòng thông tin item để nhập ID tùy chỉnh
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 50, 145, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(10, "Nhập ID vật phẩm Bánh", targetItemIdBanh.ToString());
				return;
			}

			int rY = winY + 66;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(11, "Số lượng mua mỗi đợt (1-1000)", batchQtyBanh.ToString());
				return;
			}
			// Delay
			rY += 17;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(13, "Độ trễ mỗi đợt mua (ms)", delayBuyBanhMs.ToString());
				return;
			}
			// ĐK 1: Đủ số đợt mua
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				stopBanhCond1 = !stopBanhCond1;
				if (!stopBanhCond1 && !stopBanhCond2 && !stopBanhCond3)
				{
					stopBanhCond1 = true;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				stopBanhCond1 = true;
				GameCanvas.clearAllPointerEvent();
				OpenInputField(12, "Số đợt mua cần đạt (0 = vô hạn)", targetBatchesBanh.ToString());
				return;
			}
			// ĐK 2: Đầy rương / Hết tài nguyên (có thể tích cùng lúc với ĐK 3)
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 190, 15))
			{
				stopBanhCond2 = !stopBanhCond2;
				if (!stopBanhCond1 && !stopBanhCond2 && !stopBanhCond3)
				{
					stopBanhCond2 = true;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// ĐK 3: Đạt mốc bánh trong rương -> Dùng hết (có thể tích cùng lúc với ĐK 2)
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				stopBanhCond3 = !stopBanhCond3;
				if (!stopBanhCond1 && !stopBanhCond2 && !stopBanhCond3)
				{
					stopBanhCond3 = true;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				stopBanhCond3 = true;
				GameCanvas.clearAllPointerEvent();
				OpenInputField(14, "Mốc bánh trong rương cần đạt", targetItemCountBanh.ToString());
				return;
			}
			// Rẽ nhánh khi xong Tab Bánh
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				enableRoutingBanh = !enableRoutingBanh;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(16, "Tab chuyển sang khi xong Bánh (1-4)", routeTargetTabBanh.ToString());
				return;
			}
		}

		// === TAB 2: HN ===
		if (selectedTab == 2)
		{
			// Click dòng thông tin item để nhập ID tùy chỉnh
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 50, 145, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(20, "Nhập ID vật phẩm HN", targetItemIdHn.ToString());
				return;
			}

			int rY = winY + 67;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(21, "Số lượng mua mỗi đợt", batchQtyHn.ToString());
				return;
			}
			// Target Batches
			rY += 17;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(22, "Số đợt cần mua", targetBatchesHn.ToString());
				return;
			}
			// Delay
			rY += 17;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(23, "Độ trễ mỗi đợt (ms)", delayBuyHnMs.ToString());
				return;
			}
			// Radio ĐK dừng HN: Đủ đợt vs Đầy/Hết
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 15))
			{
				stopConditionHn = 1;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 110, rY, 105, 15))
			{
				stopConditionHn = 2;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Checkbox ĐK 3: Zeno < SL x 40K
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 20, 15))
			{
				stopHnCond3 = !stopHnCond3;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Checkbox vừa mua vừa dùng
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 20, 15))
			{
				autoUseHn = !autoUseHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Checkbox rẽ nhánh ưu tiên (Local routing)
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				enableRoutingHn = !enableRoutingHn;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(24, "Tab nhảy sang khi xong HN (1-4)", routeTargetTabHn.ToString());
				return;
			}
		}

		// === TAB 3: ĐIỂM ===
		if (selectedTab == 3)
		{
			// Checkbox tự động chọn mốc (10Tr -> 5K -> 3K)
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 65, winW - 20, 14))
			{
				autoSelectMocDiem = !autoSelectMocDiem;
				if (autoSelectMocDiem && pointIndex > 2)
				{
					pointIndex = 1;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}

			// 5 Mốc điểm
			int mocY = winY + 81;
			for (int m = 0; m < 5; m++)
			{
				int mX = winX + 11 + m * 42;
				if (GameCanvas.isPointerHoldIn(mX, mocY, 38, 16))
				{
					pointIndex = m;
					if (m > 2 && autoSelectMocDiem)
					{
						autoSelectMocDiem = false;
					}
					GameCanvas.clearAllPointerEvent();
					return;
				}
			}

			int rY = winY + 100;
			// Target Batches
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(31, "Số lượt cần đổi (0 = vô hạn)", targetBatchesDiem.ToString());
				return;
			}
			// Delay
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(32, "Độ trễ mỗi lượt (ms)", delayDiemMs.ToString());
				return;
			}
			// ĐK dừng: Cách 1 (Đủ lượt) vs Cách 2 (Đầy/Hết điểm)
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 95, 15))
			{
				stopConditionDiem = 1;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 108, rY, 110, 15))
			{
				stopConditionDiem = 2;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Rẽ nhánh khi xong Tab Điểm
			rY += 17;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				enableRoutingDiem = !enableRoutingDiem;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(33, "Tab chuyển sang khi xong Điểm (1-4)", routeTargetTabDiem.ToString());
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
				OpenInputField(41, "Số lượng mỗi đợt (mặc định 19800)", batchQtyZeno.ToString());
				return;
			}
			// Target Batches
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(42, "Số đợt cần đổi (mặc định 1)", targetBatchesZeno.ToString());
				return;
			}
			// Delay
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(43, "Độ trễ mỗi đợt (ms)", delayZenoMs.ToString());
				return;
			}
			// Nhánh 1: Đạt mục tiêu -> Tab
			rY += 20;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 145, 16))
			{
				enableRouteDoneZeno = !enableRouteDoneZeno;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(44, "Tab khi đạt mục tiêu (VD: 2 là HN)", routeTargetWhenDoneZeno.ToString());
				return;
			}
			// Nhánh 2: Thiếu vé -> Tab
			rY += 19;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 145, 16))
			{
				enableRouteLackZeno = !enableRouteLackZeno;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(45, "Tab khi thiếu vé (VD: 1 là Bánh)", routeTargetWhenLackZeno.ToString());
				return;
			}
		}

		// 2 Nút bấm đáy: BẮT ĐẦU & DỪNG
		int btnX = winX + 14;
		int btnW = winW - 28;
		int startBtnY = winY + 198;
		int stopBtnY = winY + 220;

		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			if (selectedTab == 0) StartWorkflow();
			else
			{
				isRunningWorkflow = false;
				StartTab(selectedTab);
			}
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
			int tX = winX + 8 + t * 43;
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
			int rY = winY + 48;
			DrawCheckbox(g, labelX, rY, enableBanh, "1. Auto Bánh (92)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderBanh.ToString(), isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, enableHn, "2. Auto HN (56)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderHn.ToString(), isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, enableDiem, "3. Đổi Điểm (66)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderDiem.ToString(), isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, enableZeno, "4. Đổi Zeno (18)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderZeno.ToString(), isGreenText: true);

			rY += 16;
			mFont.tahoma_7b_white.drawString(g, "Delay chuyển giao:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, delayBetweenTabsMs + " ms", isGreenText: false);

			rY += 16;
			mFont.tahoma_7b_white.drawString(g, "Số vòng lặp (0=∞):", labelX, rY + 1, mFont.LEFT);
			string loopValStr = (targetWorkflowLoops == 0) ? "0 (∞)" : targetWorkflowLoops.ToString();
			DrawInputBox(g, inputX, rY, 52, 14, loopValStr, isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, autoChatMuaNhieu, "Tự bật Mua Nhiều (muanhieu)");

			rY += 16;
			DrawCheckbox(g, labelX, rY, hidePlayersAndMobs, "Ẩn người&quái");
			DrawCheckbox(g, winX + 115, rY, hideFlyInfoMessages, "Tắt chữ bay");

			string loopRunInfo = (targetWorkflowLoops == 0)
				? ("Vòng " + (completedWorkflowLoops + 1) + "/∞")
				: ("Vòng " + (completedWorkflowLoops + 1) + "/" + targetWorkflowLoops);
			string statusText = isRunningWorkflow
				? ("Đang chạy: " + GetTabName(currentRunningTab) + " (" + loopRunInfo + ")")
				: (isRunning ? ("Chạy lẻ: Tab " + GetTabName(currentRunningTab)) : "Trạng thái: Sẵn sàng (Phím F3)");
			mFont.tahoma_7_yellow.drawString(g, statusText, headerCenterX, winY + 180, mFont.CENTER);
		}

		// === NỘI DUNG TAB 1: BÁNH ===
		if (selectedTab == 1)
		{
			int rY = winY + 50;
			mFont.tahoma_7b_white.drawString(g, targetItemNameBanh + " [" + targetItemIdBanh + "] - NPC 92", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Bánh: " + GetBagItemCount(targetItemIdBanh), inputX + 50, rY - 2, mFont.RIGHT);
			mFont.tahoma_7_red.drawString(g, "H.Ngọc: " + FormatShortNumber(GetHongNgoc()), inputX + 50, rY + 7, mFont.RIGHT);

			rY = winY + 66;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, batchQtyBanh.ToString(), isGreenText: true);

			rY += 17;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayBuyBanhMs + " ms", isGreenText: false);

			rY += 18;
			DrawCheckbox(g, labelX, rY, stopBanhCond1, "ĐK 1: Đủ số đợt mua");
			DrawInputBox(g, inputX, rY, 52, 15, targetBatchesBanh.ToString(), isGreenText: true);

			rY += 17;
			DrawCheckbox(g, labelX, rY, stopBanhCond2, "ĐK 2: Đầy rương / Hết tài nguyên");

			rY += 17;
			DrawCheckbox(g, labelX, rY, stopBanhCond3, "ĐK 3: Đạt mốc -> Dùng hết");
			DrawInputBox(g, inputX, rY, 52, 15, targetItemCountBanh.ToString(), isGreenText: true);

			rY += 18;
			DrawCheckbox(g, labelX, rY, enableRoutingBanh, "Rẽ nhánh khi xong ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetTabBanh) + "(" + routeTargetTabBanh + ")", isGreenText: true);

			string activeConds = "";
			if (stopBanhCond1) activeConds += (activeConds.Length > 0 ? "+" : "") + "1";
			if (stopBanhCond2) activeConds += (activeConds.Length > 0 ? "+" : "") + "2";
			if (stopBanhCond3) activeConds += (activeConds.Length > 0 ? "+" : "") + "3";
			string statusBanh = isUsingAllBanh
				? "Đang tự động dùng hết bánh trong rương..."
				: ("Đã mua: " + completedBatchesBanh + " đợt | ĐK dừng: " + activeConds);
			mFont.tahoma_7_yellow.drawString(g, statusBanh, headerCenterX, winY + 175, mFont.CENTER);
		}

		// === NỘI DUNG TAB 2: HN ===
		if (selectedTab == 2)
		{
			int rY = winY + 50;
			mFont.tahoma_7b_white.drawString(g, targetItemNameHn + " [" + targetItemIdHn + "] - NPC 56", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Rương: " + GetBagItemCount(targetItemIdHn), inputX + 50, rY - 2, mFont.RIGHT);
			mFont.tahoma_7_green.drawString(g, "Zeno: " + FormatShortNumber(GetBagItemCount(ZenoItemId)), inputX + 50, rY + 7, mFont.RIGHT);

			rY = winY + 67;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, batchQtyHn.ToString(), isGreenText: true);

			rY += 17;
			mFont.tahoma_7_white.drawString(g, "Số đợt cần mua:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, targetBatchesHn.ToString(), isGreenText: true);

			rY += 17;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayBuyHnMs + " ms", isGreenText: false);

			rY += 18;
			DrawRadio(g, labelX, rY, stopConditionHn == 1, "ĐK 1: Đủ đợt");
			DrawRadio(g, winX + 110, rY, stopConditionHn == 2, "ĐK 2: Đầy/Hết");

			rY += 17;
			DrawCheckbox(g, labelX, rY, stopHnCond3, "ĐK 3: Zeno < " + FormatShortNumber(GetZenoRequiredHn()) + " (SL x 40K)");

			rY += 17;
			DrawCheckbox(g, labelX, rY, autoUseHn, "Vừa mua vừa dùng (" + targetItemIdHn + ")");

			rY += 17;
			DrawCheckbox(g, labelX, rY, enableRoutingHn, "Rẽ nhánh khi xong ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetTabHn) + "(" + routeTargetTabHn + ")", isGreenText: true);

			mFont.tahoma_7_yellow.drawString(g, "Đã mua: " + completedBatchesHn + " đợt | ĐK dừng: Cách " + stopConditionHn + (stopHnCond3 ? "+3" : ""), headerCenterX, winY + 186, mFont.CENTER);
		}

		// === NỘI DUNG TAB 3: ĐIỂM ===
		if (selectedTab == 3)
		{
			int rY = winY + 50;
			string ptStr = (currentDiemPoints >= 0) ? (NinjaUtil.getMoneys(currentDiemPoints) + "đ") : "Chưa mở NPC";
			mFont.tahoma_7b_yellow.drawString(g, "Điểm: " + ptStr, labelX, rY, mFont.LEFT);

			// Nhắc nhở bật lọc đồ gọn ở góc phải dòng đầu
			g.setColor(0xB71C1C);
			g.fillRect(winX + winW - 88, rY - 1, 76, 13, 2);
			mFont.tahoma_7b_white.drawString(g, "Bật Lọc Đồ!", winX + winW - 50, rY, mFont.CENTER);

			// Checkbox tự động chọn mốc (10Tr -> 5K -> 3K)
			rY = winY + 65;
			DrawCheckbox(g, labelX, rY, autoSelectMocDiem, "Tự động chọn mốc (10Tr -> 5K -> 3K)");

			// 5 Mốc điểm
			int mocY = winY + 81;
			for (int m = 0; m < 5; m++)
			{
				int mX = winX + 11 + m * 42;
				bool isSel = (pointIndex == m);
				bool isAutoTier = autoSelectMocDiem && m <= 2;
				g.setColor(isSel ? 0x1B5E20 : (isAutoTier ? 0x2E3B1F : 0x160D06));
				g.fillRect(mX, mocY, 38, 16, 3);
				if (isSel)
				{
					g.setColor(0x2E7D32);
					g.fillRect(mX + 1, mocY + 1, 36, 7, 2);
				}
				g.setColor(isSel ? 0xF7CE68 : (isAutoTier ? 0x81C784 : 0x8D6E63));
				g.drawRect(mX, mocY, 37, 15);
				if (isSel)
				{
					mFont.tahoma_7b_yellow.drawStringBorder(g, MocShortNames[m], mX + 19, mocY + 2, mFont.CENTER, mFont.tahoma_7_grey);
				}
				else
				{
					mFont.tahoma_7b_white.drawString(g, MocShortNames[m], mX + 19, mocY + 2, mFont.CENTER);
				}
			}

			rY = winY + 100;
			mFont.tahoma_7_white.drawString(g, "Số lượt cần đổi:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, targetBatchesDiem.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi lượt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayDiemMs + " ms", isGreenText: false);

			rY += 18;
			DrawRadio(g, labelX, rY, stopConditionDiem == 1, "ĐK 1: Đủ lượt");
			DrawRadio(g, winX + 108, rY, stopConditionDiem == 2, "ĐK 2: Đầy/Hết điểm");

			rY += 17;
			DrawCheckbox(g, labelX, rY, enableRoutingDiem, "Rẽ nhánh khi xong ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetTabDiem) + "(" + routeTargetTabDiem + ")", isGreenText: true);

			string mocStatus = autoSelectMocDiem ? ("Tự động [" + MocShortNames[pointIndex] + "]") : MocFullNames[pointIndex];
			mFont.tahoma_7_yellow.drawString(g, "Đã đổi: " + completedBatchesDiem + " lượt | Mốc: " + mocStatus, headerCenterX, winY + 175, mFont.CENTER);
		}

		// === NỘI DUNG TAB 4: ZENO ===
		if (selectedTab == 4)
		{
			int rY = winY + 50;
			mFont.tahoma_7b_white.drawString(g, "Vé Zeno (1583) - Karin (18)", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Vé: " + GetBagItemCount(1583) + " cái", inputX + 50, rY, mFont.RIGHT);

			rY = winY + 68;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, batchQtyZeno.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Số đợt cần đổi:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, targetBatchesZeno.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayZenoMs + " ms", isGreenText: false);

			rY += 20;
			DrawCheckbox(g, labelX, rY, enableRouteDoneZeno, "Nhánh 1 (Đủ đợt) ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetWhenDoneZeno) + "(" + routeTargetWhenDoneZeno + ")", isGreenText: true);

			rY += 19;
			DrawCheckbox(g, labelX, rY, enableRouteLackZeno, "Nhánh 2 (Vé < " + batchQtyZeno + ") ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetWhenLackZeno) + "(" + routeTargetWhenLackZeno + ")", isGreenText: true);

			mFont.tahoma_7_yellow.drawString(g, "Đã đổi: " + completedBatchesZeno + " đợt (Tích chọn để bật/tắt nhánh)", headerCenterX, winY + 175, mFont.CENTER);
		}

		// 2 Nút bấm đáy
		int btnX = winX + 14;
		int btnW = winW - 28;
		int btnH = 19;
		int startBtnY = winY + 198;
		int stopBtnY = winY + 220;

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
