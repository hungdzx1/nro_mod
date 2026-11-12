using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Game2.Assets.src.g;
using UnityEngine;

namespace Game2.Mod;

public class AutoGop : IChatable
{
	public enum ZenoStepState
	{
		None,
		ApproachNpc,
		WaitMenu1,
		WaitMenu2,
		WaitInput,
		WaitServerAck,
		Cooldown
	}

	public enum ZenoEndCode
	{
		None,
		ZENO_DONE,
		ZENO_LACK_TICKETS,
		WRONG_MAP_ZENO,
		ZENO_MENU_ERROR,
		ZENO_INPUT_TIMEOUT,
		ZENO_ACK_TIMEOUT,
		ZENO_SERVER_REJECT
	}

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

	public int delayBuyBanhMs = 600;

	public bool stopBanhCond2 = true; // ĐK 2: Đầy rương / Hết tài nguyên

	public bool stopBanhCond3 = true; // ĐK 3: Đạt mốc bánh trong rương -> Tự dùng 1 lần

	public int targetItemCountBanh = 5000; // Mốc số bánh trong rương cho ĐK 3

	public bool enableRoutingBanh; // Mặc định tắt (false)

	public int routeTargetTabBanh = 3; // Mặc định chuyển sang Tab Điểm (3)

	public int completedBatchesBanh;

	private bool isUsingAllBanh;

	private bool banhUseSent;

	private long banhUseSentTimestamp;

	private const int BanhUseVerifyTimeoutMs = 1200;

	// === TAB 2: BOX HỒNG NGỌC SSS (Item ID: 1581, NPC ID: 56) ===
	public int targetItemIdHn = 1581;

	public string targetItemNameHn = "BOX HỒNG NGỌC SSS";

	public sbyte targetBuyTypeHn = 3;

	public int batchQtyHn = 1000;

	public int targetBatchesHn = 50; // 0 = vô hạn

	public int delayBuyHnMs = 600;

	public int stopConditionHn = 1; // 1: ĐK 1 Đủ đợt. 3: ĐK 3 Zeno < SL x 40K

	public bool enableRoutingHn = true; // Bật tính năng chuyển hướng cục bộ

	public int routeWhenConditionHn = 2; // Rẽ nhánh khi thỏa mãn ĐK

	public int routeTargetTabHn = 4; // Nhảy sang Tab Zeno (ID 4)

	public int completedBatchesHn;

	public const int ZenoItemId = 1378; // Vật phẩm Zeno dùng để mua HN

	public const long ZenoPerHn = 40000L; // Giá 1 BOX HN SSS = 40.000 Zeno

	// === CHỐNG ĐỆ QUY THÔNG BÁO (Fix đơ game) ===
	private bool suppressInfoHook;

	private bool inServerInfo;

	private bool progressThisLoop; // Có mua/đổi được gì đã được server xác nhận trong vòng hiện tại không

	// === TAB 3: ĐIỂM (NPC ID: 66) ===
	public int pointIndex = 1; // 0: 10Tr, 1: 5K, 2: 3K, 3: 200, 4: 50

	public bool autoSelectMocDiem = false; // Mặc định tắt: giữ đúng mốc người dùng chọn

	public int targetBatchesDiem = 1; // Mặc định 1 lượt (0 = vô hạn)

	public int delayDiemMs = 350;

	public int stopConditionDiem = 1; // 1: ĐK 1 Đủ lượt, 2: ĐK 2 Đầy rương / Hết điểm (< Mốc)

	public bool enableRoutingDiem; // Bật/tắt rẽ nhánh khi xong Tab Điểm

	public int routeTargetTabDiem = 4; // Mặc định chuyển sang Tab Zeno (4)

	public int currentDiemPoints = -1; // Điểm hiện có cập nhật Real-time từ lời thoại Menu 1 của NPC 66

	public int completedBatchesDiem;

	private bool pendingDiemAck;

	private int pendingDiemPick = -1;

	private long diemAckTimestamp;

	private int diemBagUnitsSnapshot;

	private int diemBagSlotsSnapshot;

	public static readonly int[] RequiredPoints = new int[5] { 10000000, 5000, 3000, 200, 50 };

	public static readonly string[] MocShortNames = new string[5] { "10Tr", "5K", "3K", "200", "50" };

	public static readonly string[] MocFullNames = new string[5] { "10.000.000đ", "5.000đ", "3.000đ", "200đ", "50đ" };

	// === TAB 4: ZENO (Item ID: 1583, NPC ID: 18 - Tháp Karin Map 5) ===
	public int batchQtyZeno = 19800; // Mặc định 19.800

	public int targetBatchesZeno = 1; // Mặc định 1

	public int delayZenoMs = 350;

	public bool enableRouteDoneZeno = true; // Bật/tắt Nhánh 1 (Đạt mục tiêu)

	public int routeTargetWhenDoneZeno = 2; // Nhánh 1: Đổi đủ mục tiêu -> Nhảy sang Tab HN (2)

	public bool enableRouteLackZeno = true; // Bật/tắt Nhánh 2 (Thiếu vé < SL)

	public int routeTargetWhenLackZeno = 1; // Nhánh 2: Thiếu vé thật -> Nhảy sang Tab Bánh (1)

	public int completedBatchesZeno;

	public ZenoStepState zenoState = ZenoStepState.None;

	public ZenoEndCode lastZenoEndCode = ZenoEndCode.None;

	public string lastZenoEndReason = string.Empty;

	private bool zenoPendingTransaction;

	private int zenoTicketsBeforeBatch = -1;

	private bool zenoServerConfirmedSuccess;

	private long zenoConfirmInfoMs;

	private int zenoTalkPassCount;

	private int zenoMenu1ResyncCount;

	private int zenoMenu1CmdType = 32;

	private int zenoMenu1CmdIdx;

	private int zenoMenu1CmdSubIdx;

	private short zenoMenu1CmdId;

	private int zenoMenu2CmdType = 32;

	private int zenoMenu2CmdIdx;

	private int zenoMenu2CmdSubIdx;

	private short zenoMenu2CmdId;

	private long approachMoveIssuedMs;

	private string lastZenoDebug = string.Empty;

	public readonly List<string> zenoDebugLogs = new List<string>();

	// === ENGINE STATE ===
	private int stepState; // 0: Idle, 1: Approach & Open, 2: WaitMenu, 3: WaitShop, 4: WaitInput, 5: Cooldown, 6: TransitionDelay

	private int pendingRouteTab = -1;

	private string pendingRouteReason = string.Empty;

	private int transitionChainCount;

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

	public static void InvalidateBagCache()
	{
		lastBagCheckTime = 0;
		cachedBagCounts.Clear();
	}

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

	public static int GetTotalBagItemUnits()
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return 0;
		}
		int total = 0;
		for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
		{
			Item item = Char.myCharz().arrItemBag[i];
			if (item != null && item.template != null)
			{
				total += System.Math.Max(1, item.quantity);
			}
		}
		return total;
	}

	public void OnBagUpdated()
	{
		InvalidateBagCache();
		if (!isRunning)
		{
			return;
		}
		if (currentRunningTab == 4 && zenoState == ZenoStepState.WaitServerAck && zenoPendingTransaction)
		{
			CheckZenoAckConfirmation();
		}
		else if (currentRunningTab == 3 && pendingDiemAck)
		{
			if (GetTotalBagItemUnits() != diemBagUnitsSnapshot || GetEmptyBagSlots() != diemBagSlotsSnapshot)
			{
				ConfirmDiemBatchSuccess("server-bag-update");
			}
		}
		else if (currentRunningTab == 1 && isUsingAllBanh && banhUseSent)
		{
			int rem = GetBagItemCount(targetItemIdBanh);
			if (rem <= 0)
			{
				CompleteBanhUsePhase("Đã dùng hết toàn bộ " + targetItemNameBanh + " trong rương");
			}
		}
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
		return isRunning && hideUi;
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
		string clean = s.Normalize(NormalizationForm.FormC).Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
		return Regex.Replace(clean, "\\s+", " ").Trim().ToLower();
	}

	private static string JoinCaptions(IList<string> norm)
	{
		if (norm == null || norm.Count == 0)
		{
			return "[]";
		}
		return "[" + string.Join(" | ", norm) + "]";
	}

	private void LogZenoEvent(string packet, string caption, int menuIndex, int subIndex, int ticketBefore, int ticketAfter, long elapsedMs, string reason)
	{
		string msg = "[AutoGop][Zeno] map=" + TileMap.mapID
			+ ", npc=18, step=" + zenoState
			+ ", packet=" + packet
			+ ", caption=" + caption
			+ ", menuIndex=" + menuIndex
			+ ", subIndex=" + subIndex
			+ ", ticketBefore=" + ticketBefore
			+ ", ticketAfter=" + ticketAfter
			+ ", elapsed=" + elapsedMs
			+ ", reason=" + reason;
		lastZenoDebug = msg;
		zenoDebugLogs.Add(msg);
		if (zenoDebugLogs.Count > 50)
		{
			zenoDebugLogs.RemoveAt(0);
		}
		Cout.println(msg);
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
		return string.IsNullOrEmpty(m)
			|| m.Contains("hướng dẫn")
			|| m.Contains("thông tin")
			|| m.Contains("từ chối")
			|| m.Contains("hủy")
			|| m.Contains("đóng")
			|| m.Contains("thoát")
			|| m.Contains("quay lại")
			|| m.Contains("trở lại")
			|| m == "không";
	}

	private static int FindZenoTalkOptionIndex(IList<string> normalizedOptions)
	{
		if (normalizedOptions == null)
		{
			return -1;
		}
		for (int i = 0; i < normalizedOptions.Count; i++)
		{
			if (normalizedOptions[i] == "nói chuyện")
			{
				return i;
			}
		}
		for (int i = 0; i < normalizedOptions.Count; i++)
		{
			if (!IsZenoSkippedOption(normalizedOptions[i]) && normalizedOptions[i].Contains("nói chuyện"))
			{
				return i;
			}
		}
		return -1;
	}

	private static int FindZenoMenuMatch(IList<string> normalizedOptions, ZenoStepState expectedStep)
	{
		if (normalizedOptions == null || normalizedOptions.Count == 0)
		{
			return -1;
		}
		if (expectedStep == ZenoStepState.WaitMenu1)
		{
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("đổi tiên phù") && !m.Contains("zeno"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("đổi tiên phù"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("đổi vé"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && (m.Contains("tiên phù") || m.Contains("zeno")))
				{
					return i;
				}
			}
			return -1;
		}
		if (expectedStep == ZenoStepState.WaitMenu2)
		{
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("tiên phù zeno"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("tiên phù") && m.Contains("zeno"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("zeno") && !m.Contains("đổi vé"))
				{
					return i;
				}
			}
			for (int i = 0; i < normalizedOptions.Count; i++)
			{
				string m = normalizedOptions[i];
				if (!IsZenoSkippedOption(m) && m.Contains("tiên phù") && !m.Contains("đổi tiên phù"))
				{
					return i;
				}
			}
			if (normalizedOptions.Count <= 3 && !IsZenoMainMenu(normalizedOptions))
			{
				for (int i = 0; i < normalizedOptions.Count; i++)
				{
					string m = normalizedOptions[i];
					if (!IsZenoSkippedOption(m) && (m == "đồng ý" || m == "xác nhận" || m.Contains("đồng ý đổi") || m.Contains("xác nhận đổi")))
					{
						return i;
					}
				}
			}
			return -1;
		}
		return -1;
	}

	private static bool IsZenoMainMenu(IList<string> normalizedOptions)
	{
		if (normalizedOptions == null || normalizedOptions.Count < 2)
		{
			return false;
		}
		bool hasPureTier1 = false;
		bool hasZenoExchange = false;
		bool hasOtherKarinMainItem = false;
		for (int i = 0; i < normalizedOptions.Count; i++)
		{
			string m = normalizedOptions[i];
			if (IsZenoSkippedOption(m))
			{
				continue;
			}
			if ((m.Contains("đổi tiên phù") && !m.Contains("zeno")) || m.Contains("đổi vé"))
			{
				hasPureTier1 = true;
			}
			else if (m.Contains("đổi tiên phù") || m.Contains("tiên phù zeno"))
			{
				hasZenoExchange = true;
			}
			else if (m.Contains("nói chuyện") || m.Contains("nhiệm vụ") || m.Contains("tập luyện") || m.Contains("thách đấu") || m.Contains("cửa hàng") || m.Contains("mua bùa") || m.Contains("về"))
			{
				hasOtherKarinMainItem = true;
			}
		}
		if (hasPureTier1)
		{
			return true;
		}
		return hasZenoExchange && hasOtherKarinMainItem;
	}

	private static void SendZenoPacketCommand(int cmdType, int cmdIdx, int cmdSubIdx, short cmdId)
	{
		if (cmdType == 33)
		{
			Service.gI().menu(18, cmdIdx, cmdSubIdx);
		}
		else if (cmdType == 27)
		{
			Service.gI().menuId(cmdId);
		}
		else
		{
			Service.gI().confirmMenu(18, (sbyte)cmdIdx);
		}
	}

	private void RecordZenoMenuSelection(ZenoStepState currentStep, int cmdType, int cmdIdx, int cmdSubIdx, short cmdId)
	{
		retryCount = 0;
		npcRetryCount = 0;
		stateTimestamp = mSystem.currentTimeMillis();
		if (currentStep == ZenoStepState.WaitMenu1)
		{
			zenoMenu1CmdType = cmdType;
			zenoMenu1CmdIdx = cmdIdx;
			zenoMenu1CmdSubIdx = cmdSubIdx;
			zenoMenu1CmdId = cmdId;
			zenoState = ZenoStepState.WaitMenu2;
		}
		else
		{
			zenoMenu2CmdType = cmdType;
			zenoMenu2CmdIdx = cmdIdx;
			zenoMenu2CmdSubIdx = cmdSubIdx;
			zenoMenu2CmdId = cmdId;
			zenoState = ZenoStepState.WaitInput;
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
		if (stopConditionHn != 3)
		{
			return false;
		}
		InvalidateBagCache();
		int zeno = GetBagItemCount(ZenoItemId);
		long need = GetZenoRequiredHn();
		if ((long)zeno < need)
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

	public static bool IsWithinNpcInteractRange(Npc npc, int maxDx = 55, int maxDy = 50)
	{
		if (npc == null || Char.myCharz() == null)
		{
			return false;
		}
		return System.Math.Abs(npc.cx - Char.myCharz().cx) <= maxDx
			&& System.Math.Abs(npc.cy - Char.myCharz().cy) <= maxDy;
	}

	private static void FocusNpcSafely(Npc npc)
	{
		if (npc == null || Char.myCharz() == null)
		{
			return;
		}
		Char.myCharz().FocusManualTo(npc);
		Char.myCharz().npcFocus = npc;
		Char.isManualFocus = true;
	}

	private static void MoveCloseToZenoNpcSmoothly(Npc npc)
	{
		if (npc == null || Char.myCharz() == null || IsWithinNpcInteractRange(npc, 55, 50))
		{
			return;
		}
		int offsetX = (Char.myCharz().cx > npc.cx) ? 20 : -20;
		Char.myCharz().currentMovePoint = new MovePoint(npc.cx + offsetX, npc.cy);
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
			if (currentRunningTab == 1)
			{
				shopItem = FindShopItemByName("bánh");
			}
			else
			{
				shopItem = FindShopItemByName("hồng ngọc") ?? FindShopItemByName("box");
			}
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
						InvalidateBagCache();
						// Nếu ở Tab Bánh và có chọn ĐK 3 (dùng 1 lần bánh trong rương), kiểm tra xem trong rương còn bánh không
						if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) > 0)
						{
							isUsingAllBanh = true;
							banhUseSent = false;
							banhUseSentTimestamp = 0;
							stepState = 5;
							stateTimestamp = mSystem.currentTimeMillis();
							Notify("Hết Hồng Ngọc/NL mua -> Tự dùng " + targetItemNameBanh + " 1 lần...");
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
		transitionChainCount = 0;
		pendingRouteTab = -1;
		pendingRouteReason = string.Empty;
		queueIndex = 0;
		string loopDesc = (targetWorkflowLoops == 0) ? "Vô hạn" : (targetWorkflowLoops + " vòng");
		Notify("▶ BẮT ĐẦU QUY TRÌNH AUTO GỘP (" + loopDesc + ")");
		StartTab(workflowQueue[0]);
	}

	private void HandleMissingMapNpc(int tabId, int npcId, int expectedMapId)
	{
		StopAll("■ [WRONG_MAP] Tab " + GetTabName(tabId) + " yêu cầu đứng tại Map " + expectedMapId + " (NPC " + npcId + ") — hãy tự chuyển map!");
	}

	private void EndZenoError(ZenoEndCode code, string detail)
	{
		lastZenoEndCode = code;
		lastZenoEndReason = detail;
		zenoPendingTransaction = false;
		zenoServerConfirmedSuccess = false;
		zenoState = ZenoStepState.None;
		InvalidateBagCache();
		int curTickets = GetBagItemCount(1583);
		long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
		LogZenoEvent("EndError", code.ToString(), -1, -1, zenoTicketsBeforeBatch, curTickets, elapsed, detail);
		StopAll("■ [" + code + "] " + detail);
	}

	public void StartTab(int tabId)
	{
		currentRunningTab = tabId;
		isRunning = true;
		stepState = 1;
		stateTimestamp = mSystem.currentTimeMillis();
		retryCount = 0;
		npcRetryCount = 0;
		pendingRouteTab = -1;
		pendingRouteReason = string.Empty;

		zenoState = ZenoStepState.None;
		zenoPendingTransaction = false;
		zenoTicketsBeforeBatch = -1;
		zenoServerConfirmedSuccess = false;
		zenoTalkPassCount = 0;
		zenoMenu1ResyncCount = 0;
		banhUseSent = false;
		banhUseSentTimestamp = 0;
		pendingDiemAck = false;
		pendingDiemPick = -1;

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
			InvalidateBagCache();
			if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
			{
				isUsingAllBanh = true;
				banhUseSent = false;
				banhUseSentTimestamp = 0;
				stepState = 5;
				stateTimestamp = mSystem.currentTimeMillis();
				Notify("▶ " + targetItemNameBanh + " đã đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng 1 lần...");
				break;
			}
			if (ModFunc.GetNpcByTempId(92) == null)
			{
				HandleMissingMapNpc(1, 92, 5);
				return;
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
			if (ModFunc.GetNpcByTempId(56) == null)
			{
				HandleMissingMapNpc(2, 56, 5);
				return;
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
			if (ModFunc.GetNpcByTempId(66) == null)
			{
				HandleMissingMapNpc(3, 66, 5);
				return;
			}
			MoveCloseToNpc(66);
			string mocStr = autoSelectMocDiem ? ("Tự động (" + MocShortNames[pointIndex] + ")") : MocShortNames[pointIndex];
			Notify("▶ Bắt đầu [Tab Điểm]: Đổi Nồi Bánh mốc " + mocStr);
			stepState = 1;
			stateTimestamp = mSystem.currentTimeMillis();
			break;

		case 4:
			completedBatchesZeno = 0;
			lastZenoEndCode = ZenoEndCode.None;
			lastZenoEndReason = string.Empty;
			lastZenoDebug = "StartTab";
			Npc karinNpc = ModFunc.GetNpcByTempId(18);
			if (TileMap.mapID != 5 || karinNpc == null || karinNpc.statusMe == 15)
			{
				EndZenoError(ZenoEndCode.WRONG_MAP_ZENO, "Zeno yêu cầu đứng sẵn tại Tháp Karin (map 5), NPC 18 — hãy tự di chuyển sang Karin!");
				return;
			}
			InvalidateBagCache();
			int zenoCount = GetBagItemCount(1583);
			if (zenoCount < batchQtyZeno)
			{
				lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
				lastZenoEndReason = "Vé Zeno (" + zenoCount + ") < " + batchQtyZeno;
				LogZenoEvent("PreCheck", "", -1, -1, zenoCount, zenoCount, 0, "ZENO_LACK_TICKETS");
				FinishZenoLack("■ Vé Zeno (" + zenoCount + ") < " + batchQtyZeno);
				return;
			}
			if (IsWithinNpcInteractRange(karinNpc, 55, 50))
			{
				FocusNpcSafely(karinNpc);
				Notify("▶ Bắt đầu [Tab Zeno]: Đổi " + batchQtyZeno + " Tiên Phù tại Karin (18)");
				zenoState = ZenoStepState.WaitMenu1;
				stepState = 2;
				stateTimestamp = mSystem.currentTimeMillis();
				LogZenoEvent("OpenMenu", "", -1, -1, zenoCount, zenoCount, 0, "Immediate openMenu(18)");
				Service.gI().openMenu(18);
			}
			else
			{
				Notify("▶ [Tab Zeno] Đang tiến tới gần Thần Mèo Karin (18)...");
				zenoState = ZenoStepState.ApproachNpc;
				stepState = 1;
				stateTimestamp = mSystem.currentTimeMillis();
				approachMoveIssuedMs = stateTimestamp;
				LogZenoEvent("Approach", "", -1, -1, zenoCount, zenoCount, 0, "Moving to Karin");
				MoveCloseToZenoNpcSmoothly(karinNpc);
			}
			break;
		}
	}

	// === CÁC HÀM XỬ LÝ KẾT THÚC & RẼ NHÁNH TỪNG TAB (CHUYỂN TAB PHI ĐỆ QUY) ===
	private void QueueTabTransition(int targetTab, string notifyMsg)
	{
		if (transitionChainCount >= 8)
		{
			StopAll("■ Dừng an toàn: Phát hiện chuỗi chuyển hướng liên tiếp không tiến triển!");
			return;
		}
		transitionChainCount++;
		pendingRouteTab = targetTab;
		pendingRouteReason = notifyMsg;
		zenoState = (currentRunningTab == 4) ? ZenoStepState.Cooldown : ZenoStepState.None;
		zenoPendingTransaction = false;
		pendingDiemAck = false;
		isUsingAllBanh = false;
		banhUseSent = false;
		stepState = 6;
		stateTimestamp = mSystem.currentTimeMillis();
		if (!string.IsNullOrEmpty(notifyMsg))
		{
			Notify(notifyMsg);
		}
	}

	public void FinishTabBanh(string reason)
	{
		if (enableRoutingBanh && routeTargetTabBanh >= 1 && routeTargetTabBanh <= 4 && routeTargetTabBanh != 1)
		{
			QueueTabTransition(routeTargetTabBanh, "Tab Bánh (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabBanh));
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
			QueueTabTransition(routeTargetTabHn, "Tab HN (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabHn));
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
			QueueTabTransition(routeTargetTabDiem, "Tab Điểm (" + reason + ") -> Chuyển sang Tab " + GetTabName(routeTargetTabDiem));
		}
		else
		{
			OnTabCompleted(3, reason);
		}
	}

	public void FinishZenoDone(string reason)
	{
		lastZenoEndCode = ZenoEndCode.ZENO_DONE;
		lastZenoEndReason = reason;
		if (enableRouteDoneZeno && routeTargetWhenDoneZeno >= 1 && routeTargetWhenDoneZeno <= 4 && routeTargetWhenDoneZeno != 4)
		{
			QueueTabTransition(routeTargetWhenDoneZeno, "▶ Zeno đạt mục tiêu -> Chuyển sang Tab " + GetTabName(routeTargetWhenDoneZeno));
		}
		else
		{
			OnTabCompleted(4, reason);
		}
	}

	public void FinishZenoLack(string reason)
	{
		lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
		lastZenoEndReason = reason;
		if (enableRouteLackZeno && routeTargetWhenLackZeno >= 1 && routeTargetWhenLackZeno <= 4 && routeTargetWhenLackZeno != 4)
		{
			QueueTabTransition(routeTargetWhenLackZeno, (reason.StartsWith("■") ? reason : ("■ " + reason)) + " -> Chuyển sang Tab " + GetTabName(routeTargetWhenLackZeno));
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
			QueueTabTransition(nextTab, "Chờ " + delayBetweenTabsMs + "ms -> Chuyển sang Tab " + GetTabName(nextTab));
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
				QueueTabTransition(workflowQueue[0], "Xong vòng " + loopInfo + " -> Lặp lại từ Tab " + GetTabName(workflowQueue[0]));
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
		banhUseSent = false;
		banhUseSentTimestamp = 0;
		pendingDiemAck = false;
		pendingDiemPick = -1;
		pendingRouteTab = -1;
		pendingRouteReason = string.Empty;
		transitionChainCount = 0;
		retryCount = 0;
		npcRetryCount = 0;
		zenoState = ZenoStepState.None;
		zenoPendingTransaction = false;
		zenoTicketsBeforeBatch = -1;
		zenoServerConfirmedSuccess = false;
		zenoTalkPassCount = 0;
		zenoMenu1ResyncCount = 0;
		lastZenoDebug = string.Empty;
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

	private void ConfirmDiemBatchSuccess(string source)
	{
		if (!pendingDiemAck)
		{
			return;
		}
		int pick = (pendingDiemPick >= 0 && pendingDiemPick < RequiredPoints.Length) ? pendingDiemPick : pointIndex;
		pendingDiemAck = false;
		pendingDiemPick = -1;
		completedBatchesDiem++;
		progressThisLoop = true;
		transitionChainCount = 0;
		if (currentDiemPoints >= RequiredPoints[pick])
		{
			currentDiemPoints -= RequiredPoints[pick];
		}

		// Đánh giá lại mốc tự động chỉ khi autoSelectMocDiem đang bật
		if (autoSelectMocDiem && currentDiemPoints >= 0)
		{
			if (currentDiemPoints >= RequiredPoints[0]) pointIndex = 0;
			else if (currentDiemPoints >= RequiredPoints[1]) pointIndex = 1;
			else if (currentDiemPoints >= RequiredPoints[2]) pointIndex = 2;
		}

		int nextMinRequired = autoSelectMocDiem ? RequiredPoints[2] : RequiredPoints[pointIndex];
		Notify("▶ Xác nhận đổi Điểm (" + MocShortNames[pick] + ") lượt " + completedBatchesDiem + " [" + source + "]");

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
	}

	private void CompleteBanhUsePhase(string reason)
	{
		isUsingAllBanh = false;
		banhUseSent = false;
		banhUseSentTimestamp = 0;
		FinishTabBanh(reason);
	}

	private void CheckZenoAckConfirmation()
	{
		if (!isRunning || currentRunningTab != 4 || zenoState != ZenoStepState.WaitServerAck || !zenoPendingTransaction)
		{
			return;
		}
		InvalidateBagCache();
		int ticketsAfter = GetBagItemCount(1583);
		bool ticketDroppedFull = zenoTicketsBeforeBatch >= 0 && ticketsAfter <= zenoTicketsBeforeBatch - batchQtyZeno;
		bool ticketDroppedWithConfirm = zenoServerConfirmedSuccess && zenoTicketsBeforeBatch >= 0 && ticketsAfter < zenoTicketsBeforeBatch;
		if (ticketDroppedFull || ticketDroppedWithConfirm)
		{
			CompleteZenoBatchSuccess(ticketsAfter);
		}
	}

	private void CompleteZenoBatchSuccess(int ticketsAfter)
	{
		if (!zenoPendingTransaction)
		{
			return;
		}
		zenoPendingTransaction = false;
		zenoServerConfirmedSuccess = false;
		retryCount = 0;
		npcRetryCount = 0;
		completedBatchesZeno++;
		progressThisLoop = true;
		transitionChainCount = 0;
		long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
		LogZenoEvent("AckSuccess", "Tiên Phù Zeno", zenoMenu2CmdIdx, zenoMenu2CmdSubIdx, zenoTicketsBeforeBatch, ticketsAfter, elapsed, "ZENO_BATCH_" + completedBatchesZeno);
		Notify("▶ Đổi thành công " + batchQtyZeno + " Tiên Phù Zeno (Đợt " + completedBatchesZeno + ", Vé còn: " + ticketsAfter + ")");

		if (targetBatchesZeno > 0 && completedBatchesZeno >= targetBatchesZeno)
		{
			lastZenoEndCode = ZenoEndCode.ZENO_DONE;
			lastZenoEndReason = "Đã đổi đủ " + completedBatchesZeno + " đợt Zeno";
			zenoState = ZenoStepState.None;
			FinishZenoDone("▶ Đã đổi đủ " + completedBatchesZeno + " đợt Zeno");
		}
		else if (ticketsAfter < batchQtyZeno)
		{
			lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
			lastZenoEndReason = "Vé Zeno (" + ticketsAfter + ") < " + batchQtyZeno;
			zenoState = ZenoStepState.None;
			FinishZenoLack("■ Vé Zeno (" + ticketsAfter + ") < " + batchQtyZeno);
		}
		else
		{
			zenoState = ZenoStepState.Cooldown;
			stepState = 5;
			stateTimestamp = mSystem.currentTimeMillis();
		}
	}

	public void OnReceiveMenuChat(int npcId, string chatText)
	{
		if (!isRunning || currentRunningTab != 4 || npcId != 18 || string.IsNullOrEmpty(chatText))
		{
			return;
		}
		string lower = chatText.ToLower();
		if (lower.Contains("hết vé") || lower.Contains("không đủ") || lower.Contains("còn thiếu") || lower.Contains("hành trang đã đầy") || lower.Contains("hành trang đầy") || lower.Contains("rương đã đầy") || lower.Contains("thất bại") || lower.Contains("không thể đổi"))
		{
			LogZenoEvent("MenuChat32", chatText, -1, -1, zenoTicketsBeforeBatch, GetBagItemCount(1583), 0, "Case32ErrorText");
			OnServerInfo(chatText);
		}
	}

	private bool ResolveZenoMenuStep(IList<string> norm, string packetName, out int chosenFlatIdx, out bool isTalkPass, out ZenoStepState stepUsed)
	{
		chosenFlatIdx = -1;
		isTalkPass = false;
		stepUsed = zenoState;
		long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
		int curTickets = GetBagItemCount(1583);

		if (zenoState == ZenoStepState.WaitMenu2)
		{
			int tier2Idx = FindZenoMenuMatch(norm, ZenoStepState.WaitMenu2);
			if (tier2Idx >= 0)
			{
				chosenFlatIdx = tier2Idx;
				stepUsed = ZenoStepState.WaitMenu2;
				return true;
			}
			if (IsZenoMainMenu(norm))
			{
				if (zenoMenu1ResyncCount < 1)
				{
					zenoMenu1ResyncCount++;
					LogZenoEvent(packetName, JoinCaptions(norm), -1, -1, zenoTicketsBeforeBatch, curTickets, elapsed, "Resync WaitMenu2->WaitMenu1");
					zenoState = ZenoStepState.WaitMenu1;
				}
				else
				{
					LogZenoEvent(packetName, JoinCaptions(norm), -1, -1, zenoTicketsBeforeBatch, curTickets, elapsed, "MENU_LOOP");
					EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "MENU_LOOP: Lặp lại Menu chính NPC 18 (" + packetName + ") " + JoinCaptions(norm));
					return false;
				}
			}
			else
			{
				LogZenoEvent(packetName, JoinCaptions(norm), -1, -1, zenoTicketsBeforeBatch, curTickets, elapsed, "MENU_NOT_MATCHED_STEP2");
				EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "MENU_NOT_MATCHED (" + packetName + ", WaitMenu2): " + JoinCaptions(norm));
				return false;
			}
		}

		if (zenoState == ZenoStepState.WaitMenu1)
		{
			int tier1Idx = FindZenoMenuMatch(norm, ZenoStepState.WaitMenu1);
			if (tier1Idx >= 0)
			{
				chosenFlatIdx = tier1Idx;
				stepUsed = ZenoStepState.WaitMenu1;
				return true;
			}
			int talkIdx = FindZenoTalkOptionIndex(norm);
			if (talkIdx >= 0 && zenoTalkPassCount < 2)
			{
				zenoTalkPassCount++;
				chosenFlatIdx = talkIdx;
				isTalkPass = true;
				stepUsed = ZenoStepState.WaitMenu1;
				return true;
			}
			LogZenoEvent(packetName, JoinCaptions(norm), -1, -1, zenoTicketsBeforeBatch, curTickets, elapsed, "MENU_NOT_MATCHED_STEP1");
			EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "MENU_NOT_MATCHED (" + packetName + ", WaitMenu1): " + JoinCaptions(norm));
			return false;
		}

		return false;
	}

	private static int ResolveZenoInputFieldIndex(string title, string[] fieldNames, sbyte[] fieldTypes)
	{
		if (fieldNames == null || fieldTypes == null || fieldNames.Length == 0)
		{
			return -1;
		}
		for (int i = 0; i < fieldNames.Length; i++)
		{
			if (fieldTypes[i] == 2)
			{
				continue;
			}
			string fn = NormalizeMenuText(fieldNames[i]);
			if (fn.Contains("số lượng") || fn.Contains("so luong") || fn.Contains("nhập số") || fn.Contains("vé") || fn.Contains("zeno") || fn.Contains("tiên phù"))
			{
				return i;
			}
		}
		if (fieldNames.Length == 1 && fieldTypes[0] != 2)
		{
			return 0;
		}
		int numericIdx = -1;
		int numericCount = 0;
		for (int i = 0; i < fieldTypes.Length; i++)
		{
			if (fieldTypes[i] == 0)
			{
				numericIdx = i;
				numericCount++;
			}
		}
		if (numericCount == 1)
		{
			return numericIdx;
		}
		return -1;
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
		if (npcTempId == 18 && !string.IsNullOrEmpty(chatText))
		{
			OnReceiveMenuChat(18, chatText);
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
			if (pendingDiemAck)
			{
				return hideUi;
			}
			bool isMenu1 = false;
			for (int k = 0; k < menu.Length; k++)
			{
				if (menu[k] != null && menu[k].ToLower().Contains("đổi điểm"))
				{
					isMenu1 = true;
					break;
				}
			}

			// Tự động chọn mốc từ cao xuống thấp chỉ khi autoSelectMocDiem bật
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
				int pick = (pointIndex >= 0 && pointIndex < menu.Length && pointIndex < RequiredPoints.Length) ? pointIndex : 1;
				InvalidateBagCache();
				diemBagUnitsSnapshot = GetTotalBagItemUnits();
				diemBagSlotsSnapshot = GetEmptyBagSlots();
				pendingDiemAck = true;
				pendingDiemPick = pick;
				diemAckTimestamp = mSystem.currentTimeMillis();
				stepState = 5;
				stateTimestamp = diemAckTimestamp;
				Notify("▶ Đã gửi lệnh đổi Điểm mốc " + MocShortNames[pick] + ", đang chờ server xác nhận...");
				Service.gI().confirmMenu(66, (sbyte)pick);
				return hideUi;
			}
		}

		// === TAB 4: ZENO (NPC 18) ===
		if (currentRunningTab == 4 && npcTempId == 18)
		{
			if (zenoState != ZenoStepState.WaitMenu1 && zenoState != ZenoStepState.WaitMenu2)
			{
				LogZenoEvent("pkt32", "ignored", -1, -1, zenoTicketsBeforeBatch, GetBagItemCount(1583), 0, "ignore pkt 32 in state " + zenoState);
				return false;
			}
			string[] norm = new string[menu.Length];
			for (int l = 0; l < menu.Length; l++)
			{
				norm[l] = NormalizeMenuText(menu[l]);
			}
			if (!ResolveZenoMenuStep(norm, "pkt32", out int chosenIdx, out bool isTalkPass, out ZenoStepState stepUsed))
			{
				return hideUi;
			}
			long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
			int ticketsNow = GetBagItemCount(1583);
			if (isTalkPass)
			{
				stateTimestamp = mSystem.currentTimeMillis();
				LogZenoEvent("pkt32", norm[chosenIdx], chosenIdx, 0, zenoTicketsBeforeBatch, ticketsNow, elapsed, "TalkPass");
				Service.gI().confirmMenu(18, (sbyte)chosenIdx);
				return hideUi;
			}
			RecordZenoMenuSelection(stepUsed, 32, chosenIdx, 0, 0);
			LogZenoEvent("pkt32", norm[chosenIdx], chosenIdx, 0, zenoTicketsBeforeBatch, ticketsNow, elapsed, stepUsed + "->" + zenoState);
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
		if (zenoState != ZenoStepState.WaitMenu1 && zenoState != ZenoStepState.WaitMenu2)
		{
			return false;
		}
		Npc focus = (Char.myCharz() != null) ? Char.myCharz().npcFocus : null;
		if (focus == null)
		{
			focus = ModFunc.GetNpcByTempId(18);
		}
		if (focus == null || focus.template == null || focus.template.npcTemplateId != 18)
		{
			return false;
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
			else if (cmd.idAction == 88821 && cmd.p is int parentIdx)
			{
				normList.Add(NormalizeMenuText(cmd.caption));
				menuIdxList.Add(parentIdx);
				subIdxList.Add(i);
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
		if (!ResolveZenoMenuStep(normList, "pkt33", out int flatIdx, out bool isTalkPass, out ZenoStepState stepUsed))
		{
			return hideUi;
		}
		int chosenMenuIdx = menuIdxList[flatIdx];
		int chosenSubIdx = subIdxList[flatIdx];
		long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
		int ticketsNow = GetBagItemCount(1583);
		if (isTalkPass)
		{
			stateTimestamp = mSystem.currentTimeMillis();
			LogZenoEvent("pkt33", normList[flatIdx], chosenMenuIdx, chosenSubIdx, zenoTicketsBeforeBatch, ticketsNow, elapsed, "TalkPass");
			Service.gI().menu(18, chosenMenuIdx, chosenSubIdx);
			return hideUi;
		}
		RecordZenoMenuSelection(stepUsed, 33, chosenMenuIdx, chosenSubIdx, 0);
		LogZenoEvent("pkt33", normList[flatIdx], chosenMenuIdx, chosenSubIdx, zenoTicketsBeforeBatch, ticketsNow, elapsed, stepUsed + "->" + zenoState);
		Service.gI().menu(18, chosenMenuIdx, chosenSubIdx);
		return hideUi;
	}

	public bool OnReceiveNewMenu(MyVector menuCmds)
	{
		if (!isRunning || currentRunningTab != 4 || menuCmds == null || menuCmds.size() == 0)
		{
			return false;
		}
		if (zenoState != ZenoStepState.WaitMenu1 && zenoState != ZenoStepState.WaitMenu2)
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
		string[] norm = new string[menuCmds.size()];
		short[] menuIds = new short[menuCmds.size()];
		for (int i = 0; i < menuCmds.size(); i++)
		{
			Command cmd = menuCmds.elementAt(i) as Command;
			norm[i] = (cmd != null) ? NormalizeMenuText(cmd.caption) : "";
			menuIds[i] = (cmd != null && cmd.p is short idVal) ? idVal : (short)0;
		}
		if (!ResolveZenoMenuStep(norm, "pkt27", out int flatIdx, out bool isTalkPass, out ZenoStepState stepUsed))
		{
			return hideUi;
		}
		short targetMenuId = menuIds[flatIdx];
		long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
		int ticketsNow = GetBagItemCount(1583);
		if (isTalkPass)
		{
			stateTimestamp = mSystem.currentTimeMillis();
			LogZenoEvent("pkt27", norm[flatIdx], flatIdx, targetMenuId, zenoTicketsBeforeBatch, ticketsNow, elapsed, "TalkPass");
			Service.gI().menuId(targetMenuId);
			return hideUi;
		}
		RecordZenoMenuSelection(stepUsed, 27, flatIdx, 0, targetMenuId);
		LogZenoEvent("pkt27", norm[flatIdx], flatIdx, targetMenuId, zenoTicketsBeforeBatch, ticketsNow, elapsed, stepUsed + "->" + zenoState);
		Service.gI().menuId(targetMenuId);
		return hideUi;
	}

	public bool OnReceiveServerInputPacket(Message msg)
	{
		if (!isRunning)
		{
			return false;
		}
		if (currentRunningTab != 1 && currentRunningTab != 2 && currentRunningTab != 4)
		{
			return false;
		}
		if (currentRunningTab == 4 && (zenoState != ZenoStepState.WaitInput || zenoPendingTransaction))
		{
			LogZenoEvent("-125", "", -1, -1, zenoTicketsBeforeBatch, GetBagItemCount(1583), 0, "IGNORE_125: state=" + zenoState + ", pending=" + zenoPendingTransaction);
			return false;
		}

		npcRetryCount = 0;

		// Gói -125 ClientInput khi Mua Bánh (1), Mua BOX HN SSS (2), hoặc Đổi Zeno (4)
		try
		{
			ChatTextField.gI().isShow = false;
			string title = msg.reader().readUTF();
			sbyte nFields = msg.reader().readByte();
			if (nFields <= 0 || nFields > 20)
			{
				if (currentRunningTab == 4)
				{
					EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "INVALID_125_FIELDS: nFields=" + nFields);
				}
				return true;
			}
			string[] fieldNames = new string[nFields];
			sbyte[] fieldTypes = new sbyte[nFields];
			for (int i = 0; i < nFields; i++)
			{
				fieldNames[i] = msg.reader().readUTF();
				fieldTypes[i] = msg.reader().readByte();
			}

			int qtyToSend = 1000;
			if (currentRunningTab == 1) qtyToSend = batchQtyBanh;
			else if (currentRunningTab == 2) qtyToSend = batchQtyHn;
			else if (currentRunningTab == 4) qtyToSend = batchQtyZeno;

			if (currentRunningTab == 4)
			{
				int targetIdx = ResolveZenoInputFieldIndex(title, fieldNames, fieldTypes);
				if (targetIdx < 0)
				{
					EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "INPUT_SCHEMA_UNKNOWN: title=" + title + ", fields=" + JoinCaptions(fieldNames));
					return true;
				}
				InvalidateBagCache();
				int ticketsBefore = GetBagItemCount(1583);
				if (ticketsBefore < batchQtyZeno)
				{
					lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
					lastZenoEndReason = "Vé Zeno (" + ticketsBefore + ") < " + batchQtyZeno;
					zenoState = ZenoStepState.None;
					LogZenoEvent("-125", title, targetIdx, nFields, ticketsBefore, ticketsBefore, 0, "LACK_BEFORE_SEND_125");
					FinishZenoLack("■ Vé Zeno (" + ticketsBefore + ") < " + batchQtyZeno);
					return true;
				}

				TField[] fields = new TField[nFields];
				for (int i = 0; i < nFields; i++)
				{
					fields[i] = new TField();
					fields[i].name = fieldNames[i];
					if (fieldTypes[i] == 0) fields[i].setIputType(TField.INPUT_TYPE_NUMERIC);
					else if (fieldTypes[i] == 1) fields[i].setIputType(TField.INPUT_TYPE_ANY);
					else if (fieldTypes[i] == 2) fields[i].setIputType(TField.INPUT_TYPE_PASSWORD);
					fields[i].setText((i == targetIdx) ? qtyToSend.ToString() : string.Empty);
				}

				if (!hideUi)
				{
					try
					{
						ClientInput.gI().setInput(nFields, title);
						for (int i = 0; i < nFields; i++)
						{
							if (ClientInput.gI().tf != null && i < ClientInput.gI().tf.Length)
							{
								ClientInput.gI().tf[i].name = fieldNames[i];
								if (fieldTypes[i] == 0) ClientInput.gI().tf[i].setIputType(TField.INPUT_TYPE_NUMERIC);
								else if (fieldTypes[i] == 1) ClientInput.gI().tf[i].setIputType(TField.INPUT_TYPE_ANY);
								else if (fieldTypes[i] == 2) ClientInput.gI().tf[i].setIputType(TField.INPUT_TYPE_PASSWORD);
								ClientInput.gI().tf[i].setText(fields[i].getText());
							}
						}
					}
					catch
					{
					}
				}

				zenoTicketsBeforeBatch = ticketsBefore;
				zenoPendingTransaction = true;
				zenoServerConfirmedSuccess = false;
				retryCount = 0;
				npcRetryCount = 0;
				long elapsed = (stateTimestamp > 0) ? (mSystem.currentTimeMillis() - stateTimestamp) : 0;
				zenoState = ZenoStepState.WaitServerAck;
				stepState = 4;
				stateTimestamp = mSystem.currentTimeMillis();
				LogZenoEvent("-125", title, targetIdx, nFields, ticketsBefore, ticketsBefore, elapsed, "SendQty=" + qtyToSend + ", WaitServerAck");
				Service.gI().sendClientInput(fields);
				return true;
			}
			else
			{
				SendClientInputDirect(qtyToSend.ToString());
				progressThisLoop = true;

				if (currentRunningTab == 1)
				{
					completedBatchesBanh++;
					retryCount = 0;
					InvalidateBagCache();
					// ĐK 3: Số bánh trong rương đạt mốc -> Tự dùng 1 lần
					if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
					{
						isUsingAllBanh = true;
						banhUseSent = false;
						banhUseSentTimestamp = 0;
						stepState = 5;
						stateTimestamp = mSystem.currentTimeMillis();
						Notify(targetItemNameBanh + " đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng 1 lần...");
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
				return hideUi;
			}
		}
		catch (Exception ex)
		{
			if (currentRunningTab == 4)
			{
				EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "EX_125: " + ex.Message);
				return true;
			}
			Notify("■ Lỗi xử lý gói -125: " + ex.Message);
			return false;
		}
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

		bool isResourceOrError = lower.Contains("hành trang đã đầy")
			|| lower.Contains("hành trang đầy")
			|| lower.Contains("rương đã đầy")
			|| lower.Contains("chỗ trống")
			|| lower.Contains("ô trống")
			|| lower.Contains("không đủ")
			|| lower.Contains("không có đủ")
			|| lower.Contains("còn thiếu")
			|| lower.Contains("hết điểm")
			|| lower.Contains("hết vé");

		bool waitingBuyOrTrade = ((currentRunningTab == 1 || currentRunningTab == 2) && (stepState == 3 || stepState == 4))
			|| (currentRunningTab == 3 && pendingDiemAck)
			|| (currentRunningTab == 4 && (zenoState == ZenoStepState.WaitMenu1 || zenoState == ZenoStepState.WaitMenu2 || zenoState == ZenoStepState.WaitInput || zenoState == ZenoStepState.WaitServerAck));

		bool wideMatch = waitingBuyOrTrade && (lower.Contains("thiếu")
			|| lower.Contains("cần có")
			|| lower.Contains("cần thêm")
			|| lower.Contains("không thể")
			|| lower.Contains("thất bại")
			|| lower.Contains("hết nguyên liệu")
			|| lower.Contains("chưa có")
			|| lower.Contains("không hợp lệ")
			|| lower.Contains("quá giới hạn")
			|| lower.Contains("từ chối")
			|| lower.Contains("bị hủy")
			|| lower.Contains("đã hủy"));

		if (currentRunningTab == 4 && zenoState == ZenoStepState.WaitServerAck && zenoPendingTransaction && !isResourceOrError && !wideMatch)
		{
			bool isPositiveZeno = lower.Contains("chúc mừng")
				|| lower.Contains("đổi thành công")
				|| lower.Contains("giao dịch thành công")
				|| lower.Contains("bạn nhận được")
				|| lower.Contains("nhận được tiên phù");
			if (isPositiveZeno)
			{
				zenoServerConfirmedSuccess = true;
				zenoConfirmInfoMs = mSystem.currentTimeMillis();
				LogZenoEvent("ServerInfoPos", s, zenoMenu2CmdIdx, zenoMenu2CmdSubIdx, zenoTicketsBeforeBatch, GetBagItemCount(1583), mSystem.currentTimeMillis() - stateTimestamp, "PositiveAck");
				CheckZenoAckConfirmation();
			}
			return;
		}

		if (currentRunningTab == 3 && pendingDiemAck && !isResourceOrError && !wideMatch)
		{
			if (lower.Contains("nhận được") || lower.Contains("thành công") || lower.Contains("chúc mừng"))
			{
				ConfirmDiemBatchSuccess("server-info");
			}
			return;
		}

		if (!isResourceOrError && !wideMatch)
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
				if (isBagFull)
				{
					CompleteBanhUsePhase("Đầy rương khi đang dùng bánh (" + s + ")");
				}
				return;
			}
			InvalidateBagCache();
			if (stopBanhCond3 && !isBagFull && GetBagItemCount(targetItemIdBanh) > 0)
			{
				// Hết TN mua nhưng ĐK 3 đang bật và trong rương còn Bánh -> dùng đúng 1 lần trước khi xong
				isUsingAllBanh = true;
				banhUseSent = false;
				banhUseSentTimestamp = 0;
				stepState = 5;
				stateTimestamp = mSystem.currentTimeMillis();
				Notify("Hết TN -> Tự dùng " + targetItemNameBanh + " 1 lần trong rương...");
			}
			else if (stopBanhCond2)
			{
				FinishTabBanh("Đầy rương / Hết tài nguyên (" + s + ") (ĐK 2)");
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
			pendingDiemAck = false;
			pendingDiemPick = -1;
			FinishTabDiem("Đầy rương / Hết điểm (" + s + ")");
		}
		else if (currentRunningTab == 4)
		{
			if (zenoState != ZenoStepState.WaitMenu1 && zenoState != ZenoStepState.WaitMenu2 && zenoState != ZenoStepState.WaitInput && zenoState != ZenoStepState.WaitServerAck)
			{
				return;
			}
			InvalidateBagCache();
			int curTickets = GetBagItemCount(1583);
			bool isTicketLackMsg = lower.Contains("hết vé")
				|| lower.Contains("không đủ vé")
				|| lower.Contains("thiếu vé")
				|| ((lower.Contains("không đủ") || lower.Contains("còn thiếu") || lower.Contains("thiếu")) && (lower.Contains("vé") || lower.Contains("zeno") || lower.Contains("1583")));
			if (isTicketLackMsg)
			{
				zenoPendingTransaction = false;
				zenoServerConfirmedSuccess = false;
				zenoState = ZenoStepState.None;
				lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
				lastZenoEndReason = s;
				LogZenoEvent("ServerLack", s, -1, -1, zenoTicketsBeforeBatch, curTickets, mSystem.currentTimeMillis() - stateTimestamp, "ZENO_LACK_TICKETS");
				FinishZenoLack("Thiếu vé Zeno (" + s + ")");
			}
			else
			{
				EndZenoError(ZenoEndCode.ZENO_SERVER_REJECT, "Server từ chối đổi Zeno: " + s);
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

		if (hideUi)
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

		// Chờ delay chuyển tiếp phi đệ quy giữa các Tab
		if (stepState == 6)
		{
			if (now - stateTimestamp >= delayBetweenTabsMs)
			{
				int nextTab = pendingRouteTab;
				if (nextTab < 1 || nextTab > 4)
				{
					nextTab = (isRunningWorkflow && queueIndex >= 0 && queueIndex < workflowQueue.Count) ? workflowQueue[queueIndex] : -1;
				}
				pendingRouteTab = -1;
				pendingRouteReason = string.Empty;
				if (nextTab >= 1 && nextTab <= 4)
				{
					stepState = 1;
					stateTimestamp = now;
					StartTab(nextTab);
				}
				else
				{
					StopAll("■ Lỗi chuyển tiếp Tab không hợp lệ");
				}
			}
			return;
		}

		// State machine riêng cho Tab 4 Zeno (KHÔNG XMap, KHÔNG dùng chung stepState 3/4/7 của shop)
		if (currentRunningTab == 4)
		{
			UpdateZenoStateMachine(now);
			return;
		}

		// stepState == 1: Chuẩn bị di chuyển đến NPC và mở Menu (Tabs 1-3)
		if (stepState == 1 && currentRunningTab >= 1 && currentRunningTab <= 3)
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
					MoveCloseToNpc(npcId);
					Service.gI().openMenu(npcId);
				}
				else if (now - stateTimestamp >= 2000)
				{
					HandleMissingMapNpc(currentRunningTab, npcId, 5);
				}
			}
			return;
		}

		// Giữ vững npcFocus trong khi chờ Menu phản hồi (tránh Char.autoFocus xóa mất npcFocus)
		if (stepState == 2 && currentRunningTab >= 1 && currentRunningTab <= 3)
		{
			int focusNpcId = GetCurrentTargetNpcId();
			Npc focusNpc = (focusNpcId >= 0) ? ModFunc.GetNpcByTempId(focusNpcId) : null;
			if (focusNpc != null && Char.myCharz() != null && (Char.myCharz().npcFocus == null || Char.myCharz().npcFocus.template == null || Char.myCharz().npcFocus.template.npcTemplateId != focusNpcId))
			{
				Char.myCharz().FocusManualTo(focusNpc);
				Char.myCharz().npcFocus = focusNpc;
			}
		}

		// Xử lý cơ chế dùng Bánh ĐÚNG 1 LẦN ở Tab 1 (bỏ vòng lặp spam mỗi 200ms)
		if (currentRunningTab == 1 && isUsingAllBanh)
		{
			InvalidateBagCache();
			int curBanh = GetBagItemCount(targetItemIdBanh);
			if (!banhUseSent)
			{
				banhUseSent = true;
				banhUseSentTimestamp = now;
				stateTimestamp = now;
				if (curBanh <= 0)
				{
					CompleteBanhUsePhase("Không còn " + targetItemNameBanh + " trong rương");
					return;
				}
				if (!UseItemInBag(targetItemIdBanh))
				{
					CompleteBanhUsePhase("Không tìm thấy " + targetItemNameBanh + " trong rương để dùng");
					return;
				}
				Notify("▶ Đã gửi 1 lệnh dùng " + targetItemNameBanh + " (hiện có " + curBanh + "), đang xác minh rương...");
				return;
			}
			if (curBanh <= 0)
			{
				CompleteBanhUsePhase("Đã dùng sạch toàn bộ " + targetItemNameBanh + " trong rương");
				return;
			}
			if (now - banhUseSentTimestamp >= BanhUseVerifyTimeoutMs)
			{
				CompleteBanhUsePhase("Đã dùng 1 lần " + targetItemNameBanh + ", trong rương vẫn còn " + curBanh + " cái");
				return;
			}
			return;
		}

		// Xử lý chờ xác nhận giao dịch Tab 3 Điểm
		if (currentRunningTab == 3 && pendingDiemAck)
		{
			int ackWaitMs = System.Math.Max(450, delayDiemMs);
			InvalidateBagCache();
			if (GetTotalBagItemUnits() != diemBagUnitsSnapshot || GetEmptyBagSlots() != diemBagSlotsSnapshot)
			{
				ConfirmDiemBatchSuccess("bag-delta");
				return;
			}
			if (now - diemAckTimestamp >= ackWaitMs)
			{
				ConfirmDiemBatchSuccess("no-reject-timeout");
				return;
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

		// stepState == 4: Chờ gói -125 từ Server (Tabs 1-2)
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
				}
			}
			return;
		}

		// Cooldown giữa các đợt mua / đổi (Tabs 1-3)
		if (stepState == 5)
		{
			int currentDelay = (currentRunningTab == 1) ? delayBuyBanhMs : ((currentRunningTab == 2) ? delayBuyHnMs : delayDiemMs);
			if (now - stateTimestamp >= currentDelay)
			{
				stateTimestamp = now;

				if (currentRunningTab == 1)
				{
					InvalidateBagCache();
					if (stopBanhCond3 && GetBagItemCount(targetItemIdBanh) >= targetItemCountBanh)
					{
						isUsingAllBanh = true;
						banhUseSent = false;
						banhUseSentTimestamp = 0;
						Notify(targetItemNameBanh + " đạt mốc " + targetItemCountBanh + " (ĐK 3) -> Tự dùng 1 lần...");
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
			}
			return;
		}

		// Timeout bảo vệ nếu server lag quá 3.5s (Tabs 1-3)
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
				return;
			}
			stepState = 1;
			if (currentRunningTab == 1) MoveCloseToNpc(92);
			else if (currentRunningTab == 2) MoveCloseToNpc(56);
			else if (currentRunningTab == 3) MoveCloseToNpc(66);
		}
	}

	private void UpdateZenoStateMachine(long now)
	{
		Npc karin = ModFunc.GetNpcByTempId(18);
		if (TileMap.mapID != 5 || karin == null || karin.statusMe == 15)
		{
			EndZenoError(ZenoEndCode.WRONG_MAP_ZENO, "Mất điều kiện Map 5 / NPC 18 trong lúc đổi Zeno — hãy đứng sẵn ở Tháp Karin!");
			return;
		}

		switch (zenoState)
		{
		case ZenoStepState.ApproachNpc:
			if (IsWithinNpcInteractRange(karin, 55, 50))
			{
				if (Char.myCharz() != null)
				{
					Char.myCharz().currentMovePoint = null;
				}
				FocusNpcSafely(karin);
				InvalidateBagCache();
				int curTickets = GetBagItemCount(1583);
				zenoState = ZenoStepState.WaitMenu1;
				stepState = 2;
				retryCount = 0;
				npcRetryCount = 0;
				stateTimestamp = now;
				Notify("▶ Đã tới gần Karin (18), mở Menu đổi " + batchQtyZeno + " Tiên Phù...");
				LogZenoEvent("ApproachDone", "", -1, -1, curTickets, curTickets, now - approachMoveIssuedMs, "Send openMenu(18)");
				Service.gI().openMenu(18);
			}
			else if (now - approachMoveIssuedMs >= 5000)
			{
				EndZenoError(ZenoEndCode.WRONG_MAP_ZENO, "Không thể tới gần NPC Thần Mèo Karin (18) trong Map 5 sau 5s");
			}
			else if (Char.myCharz() != null && Char.myCharz().currentMovePoint == null)
			{
				MoveCloseToZenoNpcSmoothly(karin);
			}
			break;

		case ZenoStepState.WaitMenu1:
			FocusNpcSafely(karin);
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount <= 2)
				{
					stateTimestamp = now;
					LogZenoEvent("RetryMenu1", "", -1, -1, zenoTicketsBeforeBatch, GetBagItemCount(1583), 2500, "Retry openMenu(18) #" + retryCount);
					Service.gI().openMenu(18);
				}
				else
				{
					EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "MENU_TIMEOUT (WaitMenu1): NPC 18 không mở Menu 1 sau 3 lần thử");
				}
			}
			break;

		case ZenoStepState.WaitMenu2:
			FocusNpcSafely(karin);
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount <= 2)
				{
					stateTimestamp = now;
					LogZenoEvent("RetryMenu2", "", zenoMenu1CmdIdx, zenoMenu1CmdSubIdx, zenoTicketsBeforeBatch, GetBagItemCount(1583), 2500, "RetryMenu1Cmd type=" + zenoMenu1CmdType + " #" + retryCount);
					SendZenoPacketCommand(zenoMenu1CmdType, zenoMenu1CmdIdx, zenoMenu1CmdSubIdx, zenoMenu1CmdId);
				}
				else
				{
					EndZenoError(ZenoEndCode.ZENO_MENU_ERROR, "MENU_TIMEOUT (WaitMenu2): Không nhận Menu 2 sau khi chọn Đổi Tiên Phù");
				}
			}
			break;

		case ZenoStepState.WaitInput:
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount <= 1)
				{
					stateTimestamp = now;
					FocusNpcSafely(karin);
					LogZenoEvent("RetryInput", "", zenoMenu2CmdIdx, zenoMenu2CmdSubIdx, zenoTicketsBeforeBatch, GetBagItemCount(1583), 2500, "RetryMenu2Cmd type=" + zenoMenu2CmdType + " #" + retryCount);
					SendZenoPacketCommand(zenoMenu2CmdType, zenoMenu2CmdIdx, zenoMenu2CmdSubIdx, zenoMenu2CmdId);
				}
				else
				{
					EndZenoError(ZenoEndCode.ZENO_INPUT_TIMEOUT, "INPUT_TIMEOUT: Không nhận gói -125 sau khi chọn Tiên Phù Zeno");
				}
			}
			break;

		case ZenoStepState.WaitServerAck:
			CheckZenoAckConfirmation();
			if (!isRunning || currentRunningTab != 4 || zenoState != ZenoStepState.WaitServerAck)
			{
				return;
			}
			if (zenoServerConfirmedSuccess && now - zenoConfirmInfoMs >= 200)
			{
				InvalidateBagCache();
				CompleteZenoBatchSuccess(GetBagItemCount(1583));
				return;
			}
			if (now - stateTimestamp >= 3000)
			{
				EndZenoError(ZenoEndCode.ZENO_ACK_TIMEOUT, "ACK_TIMEOUT: Đã gửi -125 nhưng server không xác nhận/không trừ vé 1583 sau 3s");
			}
			break;

		case ZenoStepState.Cooldown:
			if (now - stateTimestamp >= delayZenoMs)
			{
				InvalidateBagCache();
				int curTicket = GetBagItemCount(1583);
				if (curTicket < batchQtyZeno)
				{
					lastZenoEndCode = ZenoEndCode.ZENO_LACK_TICKETS;
					lastZenoEndReason = "Vé Zeno (" + curTicket + ") < " + batchQtyZeno;
					zenoState = ZenoStepState.None;
					FinishZenoLack("■ Vé Zeno (" + curTicket + ") < " + batchQtyZeno);
					return;
				}
				zenoTalkPassCount = 0;
				zenoMenu1ResyncCount = 0;
				zenoPendingTransaction = false;
				zenoServerConfirmedSuccess = false;
				retryCount = 0;
				npcRetryCount = 0;
				if (IsWithinNpcInteractRange(karin, 55, 50))
				{
					FocusNpcSafely(karin);
					zenoState = ZenoStepState.WaitMenu1;
					stepState = 2;
					stateTimestamp = now;
					LogZenoEvent("NextBatch", "", -1, -1, curTicket, curTicket, 0, "openMenu(18) batch " + (completedBatchesZeno + 1));
					Service.gI().openMenu(18);
				}
				else
				{
					zenoState = ZenoStepState.ApproachNpc;
					stepState = 1;
					stateTimestamp = now;
					approachMoveIssuedMs = now;
					MoveCloseToZenoNpcSmoothly(karin);
				}
			}
			break;
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
			case 13: delayBuyBanhMs = System.Math.Max(50, val); break;
			case 14: targetItemCountBanh = System.Math.Max(1, val); break;
			case 16: routeTargetTabBanh = System.Math.Max(1, System.Math.Min(4, val)); break;
			// Tab 2 BOX HỒNG NGỌC SSS
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

			int rY = winY + 68;
			// Batch Qty
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(11, "Số lượng mua mỗi đợt (1-1000)", batchQtyBanh.ToString());
				return;
			}
			// Delay
			rY += 20;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(13, "Độ trễ mỗi đợt mua (ms)", delayBuyBanhMs.ToString());
				return;
			}
			// ĐK 2: Đầy rương / Hết tài nguyên (có thể tích cùng lúc với ĐK 3)
			rY += 21;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 190, 16))
			{
				stopBanhCond2 = !stopBanhCond2;
				if (!stopBanhCond2 && !stopBanhCond3)
				{
					stopBanhCond2 = true;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// ĐK 3: Đạt mốc bánh trong rương -> Dùng 1 lần (có thể tích cùng lúc với ĐK 2)
			rY += 20;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, 140, 16))
			{
				stopBanhCond3 = !stopBanhCond3;
				if (!stopBanhCond2 && !stopBanhCond3)
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
			rY += 21;
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

		// === TAB 2: BOX HỒNG NGỌC SSS ===
		if (selectedTab == 2)
		{
			// Click dòng thông tin item để nhập ID tùy chỉnh
			if (GameCanvas.isPointerHoldIn(winX + 10, winY + 50, 145, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(20, "Nhập ID vật phẩm BOX HN SSS", targetItemIdHn.ToString());
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
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(22, "Số đợt cần mua", targetBatchesHn.ToString());
				return;
			}
			// Delay
			rY += 18;
			if (GameCanvas.isPointerHoldIn(inputX, rY, 54, 16))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(23, "Độ trễ mỗi đợt (ms)", delayBuyHnMs.ToString());
				return;
			}
			// Radio ĐK 1: Đủ đợt
			rY += 19;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 20, 15))
			{
				stopConditionHn = 1;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Radio ĐK 3: Zeno < SL x 40K
			rY += 18;
			if (GameCanvas.isPointerHoldIn(winX + 10, rY, winW - 20, 15))
			{
				stopConditionHn = 3;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			// Checkbox rẽ nhánh ưu tiên (Local routing)
			rY += 19;
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
			DrawCheckbox(g, labelX, rY, enableHn, "2. BOX HN SSS (56)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderHn.ToString(), isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, enableDiem, "3. Đổi Điểm (66)");
			mFont.tahoma_7_white.drawString(g, "Thứ tự:", inputX - 34, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 14, orderDiem.ToString(), isGreenText: true);

			rY += 16;
			DrawCheckbox(g, labelX, rY, enableZeno, "4. Đổi Zeno (18-Karin)");
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
			mFont.tahoma_7_yellow.drawString(g, statusText, headerCenterX, winY + 174, mFont.CENTER);
			mFont.tahoma_7_white.drawString(g, "Khác map (Kame/Karin): tự chuyển map!", headerCenterX, winY + 185, mFont.CENTER);
		}

		// === NỘI DUNG TAB 1: BÁNH ===
		if (selectedTab == 1)
		{
			int rY = winY + 50;
			mFont.tahoma_7b_white.drawString(g, targetItemNameBanh + " [" + targetItemIdBanh + "] - NPC 92", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Bánh: " + GetBagItemCount(targetItemIdBanh), inputX + 50, rY - 2, mFont.RIGHT);
			mFont.tahoma_7_red.drawString(g, "H.Ngọc: " + FormatShortNumber(GetHongNgoc()), inputX + 50, rY + 7, mFont.RIGHT);

			rY = winY + 68;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, batchQtyBanh.ToString(), isGreenText: true);

			rY += 20;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayBuyBanhMs + " ms", isGreenText: false);

			rY += 21;
			DrawCheckbox(g, labelX, rY, stopBanhCond2, "ĐK 2: Đầy rương / Hết tài nguyên");

			rY += 20;
			DrawCheckbox(g, labelX, rY, stopBanhCond3, "ĐK 3: Đạt mốc -> Dùng 1 lần");
			DrawInputBox(g, inputX, rY, 52, 15, targetItemCountBanh.ToString(), isGreenText: true);

			rY += 21;
			DrawCheckbox(g, labelX, rY, enableRoutingBanh, "Rẽ nhánh khi xong ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetTabBanh) + "(" + routeTargetTabBanh + ")", isGreenText: true);

			string activeConds = "";
			if (stopBanhCond2) activeConds += "2";
			if (stopBanhCond3) activeConds += (activeConds.Length > 0 ? "+" : "") + "3";
			string statusBanh = isUsingAllBanh
				? "Đang tự dùng 1 lần Bánh trong rương..."
				: ("Đã mua: " + completedBatchesBanh + " đợt | ĐK dừng: " + activeConds);
			mFont.tahoma_7_yellow.drawString(g, statusBanh, headerCenterX, winY + 178, mFont.CENTER);
		}

		// === NỘI DUNG TAB 2: BOX HỒNG NGỌC SSS ===
		if (selectedTab == 2)
		{
			int rY = winY + 50;
			mFont.tahoma_7b_white.drawString(g, targetItemNameHn + " [" + targetItemIdHn + "]", labelX, rY, mFont.LEFT);
			mFont.tahoma_7_yellow.drawString(g, "Rương: " + GetBagItemCount(targetItemIdHn), inputX + 50, rY - 2, mFont.RIGHT);
			mFont.tahoma_7_green.drawString(g, "Zeno: " + FormatShortNumber(GetBagItemCount(ZenoItemId)), inputX + 50, rY + 7, mFont.RIGHT);

			rY = winY + 67;
			mFont.tahoma_7_white.drawString(g, "Số lượng / đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, batchQtyHn.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Số đợt cần mua:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, targetBatchesHn.ToString(), isGreenText: true);

			rY += 18;
			mFont.tahoma_7_white.drawString(g, "Độ trễ mỗi đợt:", labelX, rY + 1, mFont.LEFT);
			DrawInputBox(g, inputX, rY, 52, 15, delayBuyHnMs + " ms", isGreenText: false);

			rY += 19;
			DrawRadio(g, labelX, rY, stopConditionHn == 1, "ĐK 1: Đủ số đợt mua");

			rY += 18;
			DrawRadio(g, labelX, rY, stopConditionHn == 3, "ĐK 3: Zeno < " + FormatShortNumber(GetZenoRequiredHn()) + " (SL x 40K)");

			rY += 19;
			DrawCheckbox(g, labelX, rY, enableRoutingHn, "Rẽ nhánh khi xong ->");
			DrawInputBox(g, inputX, rY, 52, 15, GetTabName(routeTargetTabHn) + "(" + routeTargetTabHn + ")", isGreenText: true);

			mFont.tahoma_7_yellow.drawString(g, "Đã mua: " + completedBatchesHn + " đợt | Chế độ: ĐK " + stopConditionHn + " (NPC 56)", headerCenterX, winY + 182, mFont.CENTER);
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
			string ackSuffix = pendingDiemAck ? " (Chờ xác nhận)" : "";
			mFont.tahoma_7_yellow.drawString(g, "Đã đổi: " + completedBatchesDiem + " lượt | Mốc: " + mocStatus + ackSuffix, headerCenterX, winY + 175, mFont.CENTER);
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

			string zenoFooter = isRunning && currentRunningTab == 4
				? ("Đã đổi: " + completedBatchesZeno + " đợt [" + zenoState + "]")
				: (lastZenoEndCode != ZenoEndCode.None
					? ("Đã đổi: " + completedBatchesZeno + " | " + lastZenoEndCode)
					: ("Đứng sẵn Map 5 Karin | Đổi: " + completedBatchesZeno + " đợt"));
			mFont.tahoma_7_yellow.drawString(g, zenoFooter, headerCenterX, winY + 175, mFont.CENTER);
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
