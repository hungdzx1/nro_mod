using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Mod;

/// <summary>
/// Auto Pha Lê Hóa &amp; Nâng Cấp Chung Bà Hạt Mít (2 Tab, phím I / menu Z, dừng F6).
/// </summary>
public class AutoPhaLe : IChatable, IActionListener
{
	public class PhaLeQueueEntry
	{
		public int indexUI;

		public int itemId;

		public string itemName;

		public int currStar;

		public int targetStar;

		public bool isDone;

		public bool isMissing;

		public bool maxedByServer;
	}

	public enum State
	{
		Idle,
		CheckAndPrepare,
		SellGold,
		SendCombine,
		WaitMenu,
		WaitResult
	}

	private static AutoPhaLe _instance;

	public bool isShowWindow;

	public bool isRunning;

	public State state = State.Idle;

	[AutoSetting]
	public int selectedTab = 0;

	public int runningTab = 0;

	[AutoSetting]
	public int defaultTargetStar = 7;

	[AutoSetting]
	public int upgradeBatchMode = 100;

	[AutoSetting]
	public int upgradeDelayMs = 150;

	[AutoSetting]
	public int menuRetryDelayMs = 200;

	[AutoSetting]
	public int generalBatchMode = 100;

	[AutoSetting]
	public int generalUpgradeDelayMs = 150;

	[AutoSetting]
	public int generalMenuDelayMs = 200;

	[AutoSetting]
	public int companionMaterialId = -1;

	[AutoSetting]
	public string companionMaterialName = string.Empty;

	[AutoSetting]
	public int sellGoldDelayMs = 120;

	[AutoSetting]
	public int minGoldBillion = 10;

	[AutoSetting]
	public int maxGoldBillion = 60;

	[AutoSetting]
	public bool allowAddFromBag = true;

	[AutoSetting]
	public bool hideUi = true;

	[AutoSetting]
	public bool showOldHud = false;

	public readonly List<PhaLeQueueEntry> queue = new List<PhaLeQueueEntry>();

	public readonly List<PhaLeQueueEntry> generalQueue = new List<PhaLeQueueEntry>();

	private readonly List<PhaLeQueueEntry> lastCombineSnapshot = new List<PhaLeQueueEntry>();

	private readonly List<Item> bagPickerItems = new List<Item>();

	public int pickerMode; // 0:none, 1:pick equipment for current tab, 2:pick companion material for tab 2

	public bool isPickingFromBag;

	public int pickerPage;

	public int activeQueueIndex = -1;

	public int currentPage;

	public int generalPage;

	public int totalCombineSent;

	public int totalSuccessCount;

	public int totalFailCount;

	public int totalGoldBarsSold;

	public string statusText = "Sẵn sàng (I: Bảng - F6: Dừng)";

	private long lastResultTimeMs;

	private long stateEnteredAtMs;

	private long lastSellGoldAtMs;

	private long lastGoldBeforeUse;

	private int lastGoldBarCount;

	private int goldStallCount;

	private bool sellPending;

	private bool needSellGoldNow;

	private int menuRetryCount;

	private long waitResultPacketAtMs;

	private bool waitResultGotCombine;

	private bool waitResultGotBag;

	private bool isAddingInternalInfo;

	private int editFieldMode; // 1:defaultStar, 2:upgradeDelay, 3:menuDelay, 4:sellDelay, 5:minGold, 6:maxGold, 7:genUpgradeDelay, 8:genMenuDelay, 100+idx:rowStar

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 260;

	private readonly int winH = 319;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	private const int RowsPerPage = 3;

	private const int GoldBarItemId = 457;

	private const int BaHatMitNpcId = 21;

	private const int StarOptionId = 107;

	private string lastSettingsSnapshot;

	private AutoPhaLe()
	{
		lastSettingsSnapshot = AutoSettingsStore.Load(this, "Game1.AutoPhaLe");
		ClampAllSettings();
	}

	public static AutoPhaLe gI()
	{
		if (_instance == null)
		{
			_instance = new AutoPhaLe();
		}
		return _instance;
	}

	private void NotifyInfo(string text)
	{
		if (string.IsNullOrEmpty(text) || GameScr.info1 == null)
		{
			return;
		}
		bool prev = isAddingInternalInfo;
		isAddingInternalInfo = true;
		try
		{
			GameScr.info1.addInfo(text, 0);
		}
		finally
		{
			isAddingInternalInfo = prev;
		}
	}

	private void ClampAllSettings()
	{
		selectedTab = (selectedTab == 1) ? 1 : 0;
		SetDefaultTargetStar(defaultTargetStar);
		SetUpgradeBatchMode(upgradeBatchMode);
		SetUpgradeDelayMs(upgradeDelayMs);
		SetMenuRetryDelayMs(menuRetryDelayMs);
		SetGeneralBatchMode(generalBatchMode);
		SetGeneralUpgradeDelayMs(generalUpgradeDelayMs);
		SetGeneralMenuDelayMs(generalMenuDelayMs);
		SetSellGoldDelayMs(sellGoldDelayMs);
		SetMinGoldBillion(minGoldBillion);
		SetMaxGoldBillion(maxGoldBillion);
		if (companionMaterialId < 0)
		{
			companionMaterialId = -1;
			companionMaterialName = string.Empty;
		}
		else if (companionMaterialName == null)
		{
			companionMaterialName = string.Empty;
		}
	}

	public void SetDefaultTargetStar(int star)
	{
		defaultTargetStar = System.Math.Max(1, System.Math.Min(30, star));
	}

	private static int NormalizeBatchMode(int mode)
	{
		return (mode == 1 || mode == 10 || mode == 100) ? mode : 100;
	}

	private static int NextBatchMode(int mode)
	{
		if (mode == 100)
		{
			return 10;
		}
		if (mode == 10)
		{
			return 1;
		}
		return 100;
	}

	public void SetUpgradeBatchMode(int mode)
	{
		upgradeBatchMode = NormalizeBatchMode(mode);
	}

	public void CycleUpgradeBatchMode()
	{
		upgradeBatchMode = NextBatchMode(upgradeBatchMode);
	}

	public void SetGeneralBatchMode(int mode)
	{
		generalBatchMode = NormalizeBatchMode(mode);
	}

	public void CycleGeneralBatchMode()
	{
		generalBatchMode = NextBatchMode(generalBatchMode);
	}

	public void SetUpgradeDelayMs(int ms)
	{
		upgradeDelayMs = System.Math.Max(0, System.Math.Min(10000, ms));
	}

	public void SetMenuRetryDelayMs(int ms)
	{
		menuRetryDelayMs = System.Math.Max(50, System.Math.Min(10000, ms));
	}

	public void SetGeneralUpgradeDelayMs(int ms)
	{
		generalUpgradeDelayMs = System.Math.Max(0, System.Math.Min(10000, ms));
	}

	public void SetGeneralMenuDelayMs(int ms)
	{
		generalMenuDelayMs = System.Math.Max(50, System.Math.Min(10000, ms));
	}

	public void SetSellGoldDelayMs(int ms)
	{
		sellGoldDelayMs = System.Math.Max(20, System.Math.Min(10000, ms));
	}

	public void SetMinGoldBillion(int billion)
	{
		minGoldBillion = System.Math.Max(1, System.Math.Min(1000, billion));
	}

	public void SetMaxGoldBillion(int billion)
	{
		maxGoldBillion = System.Math.Max(1, System.Math.Min(2000, billion));
	}

	private List<PhaLeQueueEntry> GetQueueForTab(int tab)
	{
		return (tab == 1) ? generalQueue : queue;
	}

	private int GetPageForTab(int tab)
	{
		return (tab == 1) ? generalPage : currentPage;
	}

	private void SetPageForTab(int tab, int page)
	{
		if (tab == 1)
		{
			generalPage = page;
		}
		else
		{
			currentPage = page;
		}
	}

	private void SetPickerMode(int mode)
	{
		pickerMode = mode;
		isPickingFromBag = mode != 0;
		if (mode != 0)
		{
			RefreshBagPickerList();
			pickerPage = 0;
		}
		else
		{
			ClampPagingForTab(selectedTab);
		}
	}

	private void EnsureWindowPosition()
	{
		int w = GameCanvas.w > 0 ? GameCanvas.w : 800;
		int h = GameCanvas.h > 0 ? GameCanvas.h : 480;
		if (winX < 0 || winY < 0 || winX > w - 40 || winY > h - 40)
		{
			winX = System.Math.Max(4, (w - winW) / 2);
			winY = System.Math.Max(4, System.Math.Min(h - winH - 4, (h - winH) / 2));
		}
	}

	public static bool CanAddItem(Item item)
	{
		return item != null && item.template != null && item.template.id != GoldBarItemId;
	}

	private void SnapshotCombineItemsIfOpen()
	{
		if (GameCanvas.panel == null || !GameCanvas.panel.isShow || GameCanvas.panel.type != 12 || GameCanvas.panel.vItemCombine == null)
		{
			return;
		}
		lastCombineSnapshot.Clear();
		for (int i = 0; i < GameCanvas.panel.vItemCombine.size(); i++)
		{
			Item item = GameCanvas.panel.vItemCombine.elementAt(i) as Item;
			if (CanAddItem(item))
			{
				lastCombineSnapshot.Add(new PhaLeQueueEntry
				{
					indexUI = item.indexUI,
					itemId = item.template.id,
					itemName = string.IsNullOrEmpty(item.template.name) ? ("ID " + item.template.id) : item.template.name,
					currStar = GetCurrPhaLe(item),
					targetStar = defaultTargetStar
				});
			}
		}
	}

	private void ImportCombineSnapshotToTab(int tab)
	{
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tab);
		for (int i = 0; i < lastCombineSnapshot.Count; i++)
		{
			PhaLeQueueEntry snap = lastCombineSnapshot[i];
			Item bagItem = FindBagItemByEntry(snap.indexUI, snap.itemId);
			if (bagItem == null)
			{
				continue;
			}
			if (tab == 1)
			{
				if (i >= 1)
				{
					if (companionMaterialId == -1)
					{
						SetCompanionMaterial(bagItem);
					}
					continue;
				}
				if (IsCompanionMaterial(bagItem))
				{
					continue;
				}
			}
			if (!IsItemInQueueList(targetQueue, bagItem))
			{
				AddItemToQueueList(targetQueue, bagItem, tab);
			}
		}
	}

	public void RefreshBagPickerList()
	{
		bagPickerItems.Clear();
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			ClampPickerPaging();
			return;
		}
		Item[] bag = Char.myCharz().arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (CanAddItem(item))
			{
				bagPickerItems.Add(item);
			}
		}
		ClampPickerPaging();
	}

	private int GetBagPickerTotalPages()
	{
		if (bagPickerItems.Count <= RowsPerPage)
		{
			return 1;
		}
		return (bagPickerItems.Count + RowsPerPage - 1) / RowsPerPage;
	}

	private void ClampPickerPaging()
	{
		int totalPages = GetBagPickerTotalPages();
		if (pickerPage >= totalPages)
		{
			pickerPage = totalPages - 1;
		}
		if (pickerPage < 0)
		{
			pickerPage = 0;
		}
	}

	public void OpenWindow()
	{
		EnsureWindowPosition();
		SnapshotCombineItemsIfOpen();
		if (allowAddFromBag && lastCombineSnapshot.Count > 0)
		{
			if (selectedTab == 0 || generalQueue.Count == 0)
			{
				ImportCombineSnapshotToTab(selectedTab);
			}
		}
		if (pickerMode != 0)
		{
			RefreshBagPickerList();
		}
		isShowWindow = true;
		CloseOtherPanels();
	}

	public void ToggleWindow()
	{
		EnsureWindowPosition();
		if (!isShowWindow)
		{
			OpenWindow();
			return;
		}
		isShowWindow = false;
		isDragging = false;
	}

	private static void CloseOtherPanels()
	{
		AutoGop.gI().isShowWindow = false;
		AutoMuaNhieu.gI().isShowWindow = false;
		NpcMenuHelper.gI().isShowWindow = false;
		AutoDungNhanh.gI().isShowWindow = false;
		AutoGiaoDich.gI().isShowWindow = false;
		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			if (GameCanvas.panel.type == 12)
			{
				GameCanvas.panel.isShow = false;
				GameScr.isPaint = true;
			}
			else
			{
				GameCanvas.panel.hideNow();
			}
		}
		if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
		{
			GameCanvas.panel2.isShow = false;
		}
	}

	private void HideCombinePanelsIfNeeded()
	{
		if (!isRunning || !hideUi)
		{
			return;
		}
		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.isShow = false;
			GameScr.isPaint = true;
		}
		if (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
		{
			GameCanvas.panel2.isShow = false;
		}
	}

	public static int GetCurrPhaLe(Item item)
	{
		if (item == null || item.itemOption == null)
		{
			return 0;
		}
		for (int i = 0; i < item.itemOption.Length; i++)
		{
			ItemOption opt = item.itemOption[i];
			if (opt != null && opt.optionTemplate != null && opt.optionTemplate.id == StarOptionId)
			{
				return opt.param;
			}
		}
		return 0;
	}

	private static Item FindBagItemByEntry(int indexUI, int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return null;
		}
		Item[] bag = Char.myCharz().arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item != null && item.template != null && item.indexUI == indexUI && item.template.id == itemId)
			{
				return item;
			}
		}
		return null;
	}

	private static Item FindBagItemById(int itemId, int excludeIndexUI = -1)
	{
		if (itemId < 0 || Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return null;
		}
		Item fallback = null;
		Item[] bag = Char.myCharz().arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item != null && item.template != null && item.template.id == itemId)
			{
				if (item.indexUI != excludeIndexUI)
				{
					return item;
				}
				if (item.quantity > 1 && fallback == null)
				{
					fallback = item;
				}
			}
		}
		return fallback;
	}

	public static int FindBagItemIndexUI(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return -1;
		}
		Item[] bag = Char.myCharz().arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item != null && item.template != null && item.template.id == itemId)
			{
				return (item.indexUI >= 0) ? item.indexUI : i;
			}
		}
		return -1;
	}

	public static int GetBagItemCount(int itemId)
	{
		if (itemId < 0 || Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return 0;
		}
		int count = 0;
		Item[] bag = Char.myCharz().arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item != null && item.template != null && item.template.id == itemId)
			{
				count += System.Math.Max(1, item.quantity);
			}
		}
		return count;
	}

	private static bool IsItemInQueueList(List<PhaLeQueueEntry> list, Item item)
	{
		if (item == null || item.template == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].indexUI == item.indexUI && list[i].itemId == item.template.id)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsItemInQueue(Item item)
	{
		return IsItemInQueueList(GetQueueForTab(selectedTab), item);
	}

	public bool IsCompanionMaterial(Item item)
	{
		return item != null && item.template != null && companionMaterialId >= 0 && item.template.id == companionMaterialId;
	}

	public void SetCompanionMaterial(Item item)
	{
		if (!CanAddItem(item))
		{
			return;
		}
		if (companionMaterialId == item.template.id)
		{
			ClearCompanionMaterial();
			NotifyInfo("Đã bỏ chọn nguyên liệu đi kèm");
			return;
		}
		companionMaterialId = item.template.id;
		companionMaterialName = string.IsNullOrEmpty(item.template.name) ? ("ID " + companionMaterialId) : item.template.name;
		for (int i = generalQueue.Count - 1; i >= 0; i--)
		{
			if (generalQueue[i].itemId == companionMaterialId)
			{
				RemoveItemAt(i, 1);
			}
		}
		NotifyInfo("Đã chọn NL đi kèm: " + companionMaterialName);
	}

	public void ClearCompanionMaterial()
	{
		companionMaterialId = -1;
		companionMaterialName = string.Empty;
	}

	private void AddItemToQueueList(List<PhaLeQueueEntry> targetQueue, Item item, int tab)
	{
		string name = string.IsNullOrEmpty(item.template.name) ? ("ID " + item.template.id) : item.template.name;
		int stars = GetCurrPhaLe(item);
		targetQueue.Add(new PhaLeQueueEntry
		{
			indexUI = item.indexUI,
			itemId = item.template.id,
			itemName = name,
			currStar = stars,
			targetStar = defaultTargetStar,
			isDone = (tab == 0) && (stars >= defaultTargetStar)
		});
		ClampPagingForTab(tab);
		if (tab == 1)
		{
			NotifyInfo("Đã thêm vào DS Nâng Cấp: " + name);
		}
		else
		{
			NotifyInfo("Đã thêm vào DS Pha Lê: " + name + " (" + stars + "/" + defaultTargetStar + " sao)");
		}
	}

	private void ToggleItemInTab(Item item, int tab)
	{
		if (!CanAddItem(item))
		{
			return;
		}
		if (tab == 1 && IsCompanionMaterial(item))
		{
			NotifyInfo("Vật phẩm này đang là nguyên liệu đi kèm!");
			return;
		}
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tab);
		string name = string.IsNullOrEmpty(item.template.name) ? ("ID " + item.template.id) : item.template.name;
		for (int i = 0; i < targetQueue.Count; i++)
		{
			if (targetQueue[i].indexUI == item.indexUI && targetQueue[i].itemId == item.template.id)
			{
				RemoveItemAt(i, tab);
				NotifyInfo((tab == 1 ? "Đã xóa khỏi DS Nâng Cấp: " : "Đã xóa khỏi DS Pha Lê: ") + name);
				return;
			}
		}
		AddItemToQueueList(targetQueue, item, tab);
	}

	public void ToggleItemFromBag(Item item)
	{
		ToggleItemInTab(item, selectedTab);
	}

	public void ToggleGeneralItemFromBag(Item item)
	{
		ToggleItemInTab(item, 1);
	}

	public void OnCombineSlotItemAdded(Item item, int combineCount)
	{
		if (!allowAddFromBag || !CanAddItem(item))
		{
			return;
		}
		if (selectedTab == 1)
		{
			if (combineCount >= 2)
			{
				if (companionMaterialId != item.template.id)
				{
					SetCompanionMaterial(item);
				}
				return;
			}
			if (IsCompanionMaterial(item))
			{
				return;
			}
		}
		if (!IsItemInQueue(item))
		{
			ToggleItemFromBag(item);
		}
	}

	public void RemoveItemAt(int index, int tab = -1)
	{
		int targetTab = (tab < 0) ? selectedTab : tab;
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(targetTab);
		if (index < 0 || index >= targetQueue.Count)
		{
			return;
		}
		targetQueue.RemoveAt(index);
		if (isRunning && runningTab == targetTab)
		{
			if (activeQueueIndex == index)
			{
				activeQueueIndex = -1;
				state = State.CheckAndPrepare;
			}
			else if (activeQueueIndex > index)
			{
				activeQueueIndex--;
			}
			if (targetQueue.Count == 0)
			{
				StopAuto("Danh sách trống");
			}
		}
		ClampPagingForTab(targetTab);
	}

	public void ClearDoneItems(int tab = -1)
	{
		int targetTab = (tab < 0) ? selectedTab : tab;
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(targetTab);
		int removed = 0;
		for (int i = targetQueue.Count - 1; i >= 0; i--)
		{
			if (targetQueue[i].isDone)
			{
				RemoveItemAt(i, targetTab);
				removed++;
			}
		}
		NotifyInfo(removed > 0 ? ("Đã xóa " + removed + " món đã xong") : "Không có món đã xong");
	}

	public void ClearAllItems(int tab = -1)
	{
		int targetTab = (tab < 0) ? selectedTab : tab;
		GetQueueForTab(targetTab).Clear();
		lastCombineSnapshot.Clear();
		SetPageForTab(targetTab, 0);
		if (isRunning && runningTab == targetTab)
		{
			activeQueueIndex = -1;
			StopAuto("Đã xóa toàn bộ danh sách");
		}
		else
		{
			statusText = "Đã xóa danh sách (I: Bảng)";
			NotifyInfo("Đã xóa toàn bộ danh sách");
		}
	}

	private int GetTotalPagesForTab(int tab)
	{
		int count = GetQueueForTab(tab).Count;
		if (count <= RowsPerPage)
		{
			return 1;
		}
		return (count + RowsPerPage - 1) / RowsPerPage;
	}

	private void ClampPagingForTab(int tab)
	{
		int totalPages = GetTotalPagesForTab(tab);
		int page = GetPageForTab(tab);
		if (page >= totalPages)
		{
			page = totalPages - 1;
		}
		if (page < 0)
		{
			page = 0;
		}
		SetPageForTab(tab, page);
	}

	private void SyncQueueFromBag(int tab, bool notifyOnReach)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
		{
			return;
		}
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tab);
		for (int i = 0; i < targetQueue.Count; i++)
		{
			PhaLeQueueEntry entry = targetQueue[i];
			Item bagItem = FindBagItemByEntry(entry.indexUI, entry.itemId);
			if (bagItem == null)
			{
				entry.isMissing = true;
				continue;
			}
			entry.isMissing = false;
			if (bagItem.template != null && !string.IsNullOrEmpty(bagItem.template.name))
			{
				entry.itemName = bagItem.template.name;
			}
			int newStar = GetCurrPhaLe(bagItem);
			bool wasDone = entry.isDone;
			entry.currStar = newStar;
			if (tab == 1)
			{
				entry.isDone = entry.maxedByServer;
			}
			else if (entry.maxedByServer || entry.currStar >= entry.targetStar)
			{
				entry.isDone = true;
				if (!wasDone && notifyOnReach)
				{
					NotifyInfo("Đã đạt " + entry.currStar + " sao: " + entry.itemName);
				}
			}
			else
			{
				entry.isDone = false;
			}
		}
	}

	private void TryImportFromCombineIfEmpty(int tab)
	{
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tab);
		if (targetQueue.Count > 0)
		{
			return;
		}
		if (GameCanvas.panel != null && GameCanvas.panel.vItemCombine != null && GameCanvas.panel.vItemCombine.size() > 0)
		{
			for (int i = 0; i < GameCanvas.panel.vItemCombine.size(); i++)
			{
				Item item = GameCanvas.panel.vItemCombine.elementAt(i) as Item;
				if (!CanAddItem(item))
				{
					continue;
				}
				if (tab == 1)
				{
					if (i >= 1)
					{
						if (companionMaterialId == -1)
						{
							SetCompanionMaterial(item);
						}
						continue;
					}
					if (IsCompanionMaterial(item))
					{
						continue;
					}
				}
				if (!IsItemInQueueList(targetQueue, item))
				{
					AddItemToQueueList(targetQueue, item, tab);
				}
			}
		}
		if (targetQueue.Count == 0 && lastCombineSnapshot.Count > 0)
		{
			ImportCombineSnapshotToTab(tab);
		}
	}

	public void StartAuto()
	{
		if (ModFunc.GI().isAutoPhaLe)
		{
			ModFunc.GI().isAutoPhaLe = false;
			NotifyInfo("Đã tắt Auto Pha Lê cũ để chạy bản mới");
		}
		if (TileMap.mapID != 5)
		{
			StopAuto("Cần đến Đảo Kame để sử dụng Auto");
			return;
		}
		int tabToRun = selectedTab;
		TryImportFromCombineIfEmpty(tabToRun);
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tabToRun);
		if (targetQueue.Count == 0)
		{
			NotifyInfo("Chưa có trang bị trong danh sách!");
			statusText = "Chưa có trang bị trong DS";
			return;
		}
		SyncQueueFromBag(tabToRun, notifyOnReach: false);
		if (FindNextPendingQueueIndex(tabToRun) < 0)
		{
			NotifyInfo("Tất cả trang bị trong DS đã xong!");
			statusText = "Tất cả món đã xong";
			return;
		}
		if (tabToRun == 1 && companionMaterialId >= 0 && FindBagItemIndexUI(companionMaterialId) < 0)
		{
			string matLabel = string.IsNullOrEmpty(companionMaterialName) ? ("ID " + companionMaterialId) : companionMaterialName;
			NotifyInfo("Hết nguyên liệu đi kèm: " + matLabel);
			statusText = "Hết NL: " + matLabel;
			return;
		}
		runningTab = tabToRun;
		totalCombineSent = 0;
		totalSuccessCount = 0;
		totalFailCount = 0;
		totalGoldBarsSold = 0;
		needSellGoldNow = false;
		sellPending = false;
		goldStallCount = 0;
		menuRetryCount = 0;
		lastResultTimeMs = 0L;
		isRunning = true;
		state = State.CheckAndPrepare;
		stateEnteredAtMs = mSystem.currentTimeMillis();
		HideCombinePanelsIfNeeded();
		statusText = "Đang chuẩn bị...";
		if (runningTab == 1)
		{
			NotifyInfo("▶ Bắt đầu Nâng Cấp Chung (Nút đầu - F6: Dừng)");
		}
		else
		{
			NotifyInfo("▶ Bắt đầu Auto Pha Lê (" + upgradeBatchMode + " lần - F6: Dừng)");
		}
	}

	public void StopAuto(string reason = null)
	{
		isRunning = false;
		state = State.Idle;
		activeQueueIndex = -1;
		sellPending = false;
		needSellGoldNow = false;
		menuRetryCount = 0;
		waitResultGotCombine = false;
		waitResultGotBag = false;
		if (!string.IsNullOrEmpty(reason))
		{
			statusText = reason;
			NotifyInfo(reason);
		}
		else
		{
			statusText = "Đã dừng (I: Bảng - F6: Dừng)";
		}
	}

	public void OnOldAutoToggledOn()
	{
		if (isRunning)
		{
			StopAuto("Đã tắt Auto mới do bật bản cũ");
		}
	}

	public void OnDisconnectOrReset()
	{
		isRunning = false;
		state = State.Idle;
		isShowWindow = false;
		isDragging = false;
		editFieldMode = 0;
		activeQueueIndex = -1;
		sellPending = false;
		needSellGoldNow = false;
		menuRetryCount = 0;
		queue.Clear();
		generalQueue.Clear();
		lastCombineSnapshot.Clear();
		bagPickerItems.Clear();
		pickerMode = 0;
		isPickingFromBag = false;
		pickerPage = 0;
		statusText = "Sẵn sàng (I: Bảng - F6: Dừng)";
	}

	public bool ShouldShowOldHud(bool isOldAutoActive)
	{
		return showOldHud && isOldAutoActive;
	}

	public bool ShouldSkipCombineEffect()
	{
		return isRunning && hideUi;
	}

	private int FindNextPendingQueueIndex(int tab)
	{
		List<PhaLeQueueEntry> targetQueue = GetQueueForTab(tab);
		for (int i = 0; i < targetQueue.Count; i++)
		{
			if (!targetQueue[i].isDone && !targetQueue[i].isMissing)
			{
				return i;
			}
		}
		return -1;
	}

	private static Npc FindNpcById(int npcId)
	{
		if (GameScr.vNpc == null)
		{
			return null;
		}
		for (int i = 0; i < GameScr.vNpc.size(); i++)
		{
			Npc npc = GameScr.vNpc.elementAt(i) as Npc;
			if (npc != null && npc.template != null && npc.template.npcTemplateId == npcId)
			{
				return npc;
			}
		}
		return null;
	}

	private static void MoveNearBaHatMit()
	{
		Npc npc = FindNpcById(BaHatMitNpcId);
		if (npc == null || Char.myCharz() == null)
		{
			return;
		}
		if (System.Math.Abs(npc.cx - Char.myCharz().cx) >= 50)
		{
			ModFunc.GI().MoveTo(npc.cx, npc.cy - 1);
		}
		Char.myCharz().FocusManualTo(npc);
	}

	private static MyVector PrepareCombineVector(Item activeBagItem, Item companionItem = null)
	{
		MyVector vec = new MyVector();
		if (activeBagItem != null)
		{
			vec.addElement(activeBagItem);
		}
		if (companionItem != null)
		{
			vec.addElement(companionItem);
		}
		if (GameCanvas.panel != null && GameCanvas.panel.vItemCombine != null)
		{
			for (int i = 0; i < GameCanvas.panel.vItemCombine.size(); i++)
			{
				Item oldItem = GameCanvas.panel.vItemCombine.elementAt(i) as Item;
				if (oldItem != null)
				{
					oldItem.isSelect = false;
				}
			}
			GameCanvas.panel.vItemCombine.removeAllElements();
			if (activeBagItem != null)
			{
				activeBagItem.isSelect = true;
				GameCanvas.panel.vItemCombine.addElement(activeBagItem);
			}
			if (companionItem != null)
			{
				companionItem.isSelect = true;
				GameCanvas.panel.vItemCombine.addElement(companionItem);
			}
		}
		return vec;
	}

	private static string NormalizeMenuText(string raw)
	{
		return (raw ?? string.Empty).Replace("\n", " ").Replace("\r", " ").Trim().ToLower();
	}

	public static bool IsMainNpcMenu(string[] menu)
	{
		if (menu == null || menu.Length < 2)
		{
			return false;
		}
		int functionHits = 0;
		for (int i = 0; i < menu.Length; i++)
		{
			string norm = NormalizeMenuText(menu[i]);
			if (norm.Contains("pha lê hóa") || norm.Contains("pha lê hoá")
				|| norm.Contains("ép sao trang bị")
				|| norm.Contains("tinh ấn")
				|| norm.Contains("pháp sư hóa") || norm.Contains("pháp sư hoá")
				|| norm.Contains("bóng tối hóa") || norm.Contains("bóng tối hoá")
				|| norm.Contains("ánh sáng hóa") || norm.Contains("ánh sáng hoá")
				|| norm.Contains("tẩy pháp sư")
				|| norm.Contains("tẩy bóng tối")
				|| norm.Contains("tẩy ánh sáng")
				|| norm.Contains("cửa hàng")
				|| norm.Contains("hướng dẫn"))
			{
				functionHits++;
			}
		}
		return functionHits >= 2;
	}

	public static int SelectFirstConfirmMenuIndex(string[] menu)
	{
		if (menu == null || menu.Length == 0 || IsMainNpcMenu(menu))
		{
			return -1;
		}
		string firstNorm = NormalizeMenuText(menu[0]);
		if (!string.IsNullOrEmpty(firstNorm) && !IsCancelOption(firstNorm))
		{
			return 0;
		}
		for (int i = 1; i < menu.Length; i++)
		{
			string norm = NormalizeMenuText(menu[i]);
			if (!string.IsNullOrEmpty(norm) && !IsCancelOption(norm))
			{
				return i;
			}
		}
		return -1;
	}

	public static int SelectUpgradeMenuIndex(string[] menu, int batchMode)
	{
		if (menu == null || menu.Length == 0)
		{
			return -1;
		}
		string[] normalized = new string[menu.Length];
		for (int i = 0; i < menu.Length; i++)
		{
			normalized[i] = NormalizeMenuText(menu[i]);
		}

		string[] searchOrder;
		int desiredOrdinal;
		if (batchMode == 100)
		{
			searchOrder = new string[] { "100", "10", "1" };
			desiredOrdinal = 2;
		}
		else if (batchMode == 10)
		{
			searchOrder = new string[] { "10", "1" };
			desiredOrdinal = 1;
		}
		else
		{
			searchOrder = new string[] { "1" };
			desiredOrdinal = 0;
		}

		for (int p = 0; p < searchOrder.Length; p++)
		{
			string token = searchOrder[p];
			for (int i = 0; i < normalized.Length; i++)
			{
				if (!IsCancelOption(normalized[i]) && ContainsBatchCount(normalized[i], token))
				{
					return i;
				}
			}
		}

		int fallbackUpToDesired = -1;
		int firstValid = -1;
		for (int i = 0; i < normalized.Length; i++)
		{
			if (IsCancelOption(normalized[i]))
			{
				continue;
			}
			if (firstValid == -1)
			{
				firstValid = i;
			}
			if (i <= desiredOrdinal)
			{
				fallbackUpToDesired = i;
			}
		}
		return fallbackUpToDesired >= 0 ? fallbackUpToDesired : firstValid;
	}

	private static bool IsCancelOption(string norm)
	{
		return norm.Contains("từ chối") || norm.Contains("đóng") || norm.Contains("hủy") || norm.Contains("quay lại") || norm.Contains("thoát");
	}

	private static bool ContainsBatchCount(string norm, string countStr)
	{
		string pattern = countStr + " lần";
		int start = 0;
		while (start < norm.Length)
		{
			int idx = norm.IndexOf(pattern, start, StringComparison.Ordinal);
			if (idx < 0)
			{
				return false;
			}
			if (idx == 0 || !char.IsDigit(norm[idx - 1]))
			{
				return true;
			}
			start = idx + 1;
		}
		return false;
	}

	public static bool IsMaxLevelReachedText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string lower = text.ToLower();
		return lower.Contains("đạt cấp tối đa")
			|| lower.Contains("đã đạt tối đa")
			|| lower.Contains("cấp tối đa")
			|| lower.Contains("sao tối đa")
			|| lower.Contains("pha lê tối đa")
			|| lower.Contains("hóa tối đa")
			|| lower.Contains("hoá tối đa")
			|| lower.Contains("đã tối đa")
			|| lower.Contains("tối đa rồi")
			|| lower.Contains("đã đạt cấp")
			|| lower.Contains("không thể nâng cấp")
			|| lower.Contains("đạt giới hạn")
			|| lower.Contains("cấp cao nhất");
	}

	private void MarkActiveEntryMaxed(string customNotice = null)
	{
		List<PhaLeQueueEntry> activeQueue = GetQueueForTab(runningTab);
		if (activeQueueIndex >= 0 && activeQueueIndex < activeQueue.Count)
		{
			PhaLeQueueEntry entry = activeQueue[activeQueueIndex];
			entry.maxedByServer = true;
			entry.isDone = true;
			NotifyInfo(customNotice ?? ("Đã đạt tối đa: " + entry.itemName));
		}
		lastResultTimeMs = mSystem.currentTimeMillis();
		state = State.CheckAndPrepare;
		stateEnteredAtMs = lastResultTimeMs;
	}

	public bool OnNpcMenu(int npcId, string chat, string[] menu)
	{
		if (!isRunning || npcId != BaHatMitNpcId)
		{
			return false;
		}
		if (IsMainNpcMenu(menu))
		{
			StopAuto("Hãy mở sẵn chức năng cần nâng cấp ở Bà Hạt Mít trước khi bấm Bắt đầu!");
			return false;
		}

		int selectedIndex = (runningTab == 1)
			? SelectFirstConfirmMenuIndex(menu)
			: SelectUpgradeMenuIndex(menu, upgradeBatchMode);

		if (selectedIndex < 0)
		{
			if (IsMaxLevelReachedText(chat))
			{
				MarkActiveEntryMaxed();
				return hideUi;
			}
			StopAuto("Menu Bà Hạt Mít không có lựa chọn nâng cấp");
			return false;
		}

		Service.gI().confirmMenu(BaHatMitNpcId, (sbyte)selectedIndex);
		if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		totalCombineSent++;
		menuRetryCount = 0;
		waitResultGotCombine = false;
		waitResultGotBag = false;
		waitResultPacketAtMs = 0L;
		state = State.WaitResult;
		stateEnteredAtMs = mSystem.currentTimeMillis();
		if (runningTab == 1)
		{
			List<PhaLeQueueEntry> activeQueue = GetQueueForTab(1);
			string activeName = (activeQueueIndex >= 0 && activeQueueIndex < activeQueue.Count) ? activeQueue[activeQueueIndex].itemName : "trang bị";
			statusText = "Đang nâng cấp: " + activeName + "...";
		}
		else
		{
			statusText = "Đang đập (" + upgradeBatchMode + " lần)...";
		}
		return hideUi;
	}

	public bool OnNpcSay(int npcId, string sayText)
	{
		if (!isRunning || npcId != BaHatMitNpcId)
		{
			return false;
		}
		OnServerInfo(sayText);
		if (isRunning && (state == State.WaitMenu || state == State.WaitResult) && !string.IsNullOrEmpty(sayText))
		{
			string lower = sayText.ToLower();
			if (lower.Contains("không thể") || lower.Contains("không hợp lệ") || lower.Contains("sai ")
				|| lower.Contains("chưa ") || lower.Contains("hết ") || lower.Contains("phải ")
				|| lower.Contains("lỗi") || lower.Contains("từ chối"))
			{
				StopAuto("Bà Hạt Mít: " + sayText);
			}
		}
		return hideUi;
	}

	public void OnCombinePacket(sbyte type)
	{
		if (!isRunning)
		{
			return;
		}
		HideCombinePanelsIfNeeded();
		if (type == 2 || type == 4 || type == 5 || type == 6 || type == 7)
		{
			totalSuccessCount++;
		}
		else if (type == 3 || type == 8)
		{
			totalFailCount++;
		}
		if (type >= 2 && type <= 8 && state == State.WaitResult)
		{
			long now = mSystem.currentTimeMillis();
			waitResultGotCombine = true;
			waitResultPacketAtMs = now;
			if (waitResultGotBag)
			{
				SyncQueueFromBag(runningTab, notifyOnReach: true);
				lastResultTimeMs = now;
				state = State.CheckAndPrepare;
				stateEnteredAtMs = now;
			}
		}
	}

	public void OnBagUpdated()
	{
		if (queue.Count == 0 && generalQueue.Count == 0)
		{
			return;
		}
		int tab = isRunning ? runningTab : selectedTab;
		List<PhaLeQueueEntry> activeQueue = GetQueueForTab(tab);
		int prevStar = (activeQueueIndex >= 0 && activeQueueIndex < activeQueue.Count) ? activeQueue[activeQueueIndex].currStar : -1;
		if (queue.Count > 0)
		{
			SyncQueueFromBag(0, notifyOnReach: isRunning && runningTab == 0);
		}
		if (generalQueue.Count > 0)
		{
			SyncQueueFromBag(1, notifyOnReach: isRunning && runningTab == 1);
		}
		if (isRunning && state == State.WaitResult)
		{
			long now = mSystem.currentTimeMillis();
			waitResultGotBag = true;
			int newStar = (activeQueueIndex >= 0 && activeQueueIndex < activeQueue.Count) ? activeQueue[activeQueueIndex].currStar : -1;
			if (waitResultGotCombine || newStar != prevStar)
			{
				lastResultTimeMs = now;
				state = State.CheckAndPrepare;
				stateEnteredAtMs = now;
			}
		}
	}

	public void OnServerInfo(string s)
	{
		if (isAddingInternalInfo || !isRunning || string.IsNullOrEmpty(s))
		{
			return;
		}
		if (IsMaxLevelReachedText(s))
		{
			MarkActiveEntryMaxed("Đã đạt tối đa -> Chuyển món: " + s);
			return;
		}
		string lower = s.ToLower();
		if (lower.Contains("không đủ vàng") || lower.Contains("thiếu vàng") || lower.Contains("không có đủ vàng"))
		{
			long maxGoldThreshold = (long)System.Math.Max(minGoldBillion, maxGoldBillion) * 1_000_000_000L;
			if (Char.myCharz() != null && Char.myCharz().xu < maxGoldThreshold && FindBagItemIndexUI(GoldBarItemId) >= 0 && goldStallCount < 2)
			{
				needSellGoldNow = true;
				sellPending = false;
				state = State.SellGold;
				stateEnteredAtMs = mSystem.currentTimeMillis();
				statusText = "Thiếu vàng -> Tự bán TV";
			}
			else
			{
				StopAuto("Server báo thiếu vàng: " + s);
			}
			return;
		}
		if (lower.Contains("không đủ") || lower.Contains("thiếu") || lower.Contains("cần có") || lower.Contains("hành trang đã đầy") || lower.Contains("hành trang đầy"))
		{
			StopAuto("Dừng: " + s);
		}
	}

	public void Update()
	{
		if (isRunning && !ChatTextField.gI().isShow && Input.GetKeyDown(KeyCode.F6))
		{
			StopAuto("Đã dừng bởi F6");
		}
		if (isShowWindow)
		{
			if (pickerMode != 0)
			{
				RefreshBagPickerList();
			}
			UpdateTouch();
		}
		AutoSettingsStore.SaveIfChanged(this, "Game1.AutoPhaLe", ref lastSettingsSnapshot);

		if (!isRunning)
		{
			return;
		}
		if (TileMap.mapID != 5)
		{
			StopAuto("Cần đến Đảo Kame để sử dụng Auto");
			return;
		}
		if (Char.myCharz() == null)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();
		switch (state)
		{
		case State.CheckAndPrepare:
			HandleCheckAndPrepare(now);
			break;
		case State.SellGold:
			HandleSellGold(now);
			break;
		case State.SendCombine:
			HandleSendCombine(now);
			break;
		case State.WaitMenu:
			HandleWaitMenu(now);
			break;
		case State.WaitResult:
			HandleWaitResult(now);
			break;
		}
	}

	private void HandleCheckAndPrepare(long now)
	{
		SyncQueueFromBag(runningTab, notifyOnReach: true);
		List<PhaLeQueueEntry> activeQueue = GetQueueForTab(runningTab);
		int nextIdx = FindNextPendingQueueIndex(runningTab);
		if (nextIdx < 0)
		{
			bool anyValid = false;
			for (int i = 0; i < activeQueue.Count; i++)
			{
				if (!activeQueue[i].isMissing)
				{
					anyValid = true;
					break;
				}
			}
			StopAuto(anyValid ? "Đã hoàn thành toàn bộ danh sách!" : "Không tìm thấy trang bị trong hành trang!");
			return;
		}

		activeQueueIndex = nextIdx;
		if (runningTab == 1 && companionMaterialId >= 0)
		{
			if (FindBagItemIndexUI(companionMaterialId) < 0 || GetBagItemCount(companionMaterialId) <= 0)
			{
				string matLabel = string.IsNullOrEmpty(companionMaterialName) ? ("ID " + companionMaterialId) : companionMaterialName;
				StopAuto("Đã hết nguyên liệu đi kèm: " + matLabel);
				return;
			}
		}

		long minGoldThreshold = (long)minGoldBillion * 1_000_000_000L;
		if ((Char.myCharz().xu < minGoldThreshold || needSellGoldNow) && goldStallCount < 2)
		{
			if (FindBagItemIndexUI(GoldBarItemId) >= 0)
			{
				sellPending = false;
				state = State.SellGold;
				stateEnteredAtMs = now;
				statusText = "Đang bán thỏi vàng...";
				return;
			}
			if (Char.myCharz().xu <= 0 || needSellGoldNow)
			{
				StopAuto("Không đủ vàng và đã hết thỏi vàng!");
				return;
			}
		}

		PhaLeQueueEntry active = activeQueue[activeQueueIndex];
		statusText = (runningTab == 1)
			? ("Đang đập: " + active.itemName)
			: ("Đập: " + active.itemName + " (" + active.currStar + "/" + active.targetStar + " sao)");
		state = State.SendCombine;
		stateEnteredAtMs = now;
	}

	private void HandleSellGold(long now)
	{
		long targetGold = (long)System.Math.Max(minGoldBillion, maxGoldBillion) * 1_000_000_000L;
		if (sellPending)
		{
			int curBars = GetBagItemCount(GoldBarItemId);
			if (Char.myCharz().xu > lastGoldBeforeUse)
			{
				sellPending = false;
				goldStallCount = 0;
			}
			else if (curBars < lastGoldBarCount || now - lastSellGoldAtMs >= System.Math.Max(600, sellGoldDelayMs * 3))
			{
				sellPending = false;
				goldStallCount++;
				if (goldStallCount >= 2)
				{
					needSellGoldNow = false;
					NotifyInfo("Đã chạm giới hạn vàng tối đa, ngừng bán thỏi vàng");
					if (Char.myCharz().xu <= 0)
					{
						StopAuto("Không thể bán thêm thỏi vàng");
						return;
					}
					state = State.CheckAndPrepare;
					stateEnteredAtMs = now;
					return;
				}
			}
			else
			{
				return;
			}
		}

		if (Char.myCharz().xu >= targetGold)
		{
			needSellGoldNow = false;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		if (now - lastSellGoldAtMs < sellGoldDelayMs)
		{
			return;
		}
		int idx = FindBagItemIndexUI(GoldBarItemId);
		if (idx < 0)
		{
			if (needSellGoldNow || Char.myCharz().xu <= 0)
			{
				StopAuto("Không đủ vàng và đã hết thỏi vàng!");
				return;
			}
			needSellGoldNow = false;
			NotifyInfo("Hết thỏi vàng, dùng số vàng còn lại");
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}

		lastGoldBeforeUse = Char.myCharz().xu;
		lastGoldBarCount = GetBagItemCount(GoldBarItemId);
		sellPending = true;
		lastSellGoldAtMs = now;
		Service.gI().useItem(0, 1, (sbyte)idx, -1);
		totalGoldBarsSold++;
		statusText = "Đang bán thỏi vàng (" + totalGoldBarsSold + ")...";
	}

	private void HandleSendCombine(long now)
	{
		int curDelay = (runningTab == 1) ? generalUpgradeDelayMs : upgradeDelayMs;
		if (now - lastResultTimeMs < curDelay)
		{
			return;
		}
		List<PhaLeQueueEntry> activeQueue = GetQueueForTab(runningTab);
		if (activeQueueIndex < 0 || activeQueueIndex >= activeQueue.Count)
		{
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		PhaLeQueueEntry active = activeQueue[activeQueueIndex];
		Item bagItem = FindBagItemByEntry(active.indexUI, active.itemId);
		if (bagItem == null)
		{
			active.isMissing = true;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		Item matItem = null;
		if (runningTab == 1 && companionMaterialId >= 0)
		{
			matItem = FindBagItemById(companionMaterialId, bagItem.indexUI);
			if (matItem == null)
			{
				string matLabel = string.IsNullOrEmpty(companionMaterialName) ? ("ID " + companionMaterialId) : companionMaterialName;
				StopAuto("Đã hết nguyên liệu đi kèm: " + matLabel);
				return;
			}
		}

		MoveNearBaHatMit();
		HideCombinePanelsIfNeeded();
		MyVector combineVec = PrepareCombineVector(bagItem, matItem);
		if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		Service.gI().combine(1, combineVec);
		menuRetryCount = 0;
		state = State.WaitMenu;
		stateEnteredAtMs = now;
		statusText = (runningTab == 1)
			? ("Chờ menu: " + active.itemName + "...")
			: ("Chờ menu (" + active.currStar + "/" + active.targetStar + " sao)...");
	}

	private void HandleWaitMenu(long now)
	{
		int curMenuDelay = (runningTab == 1) ? generalMenuDelayMs : menuRetryDelayMs;
		if (now - stateEnteredAtMs < curMenuDelay)
		{
			return;
		}
		menuRetryCount++;
		if (menuRetryCount >= 15)
		{
			StopAuto("Không nhận được phản hồi menu từ Bà Hạt Mít");
			return;
		}
		List<PhaLeQueueEntry> activeQueue = GetQueueForTab(runningTab);
		if (activeQueueIndex < 0 || activeQueueIndex >= activeQueue.Count)
		{
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		PhaLeQueueEntry active = activeQueue[activeQueueIndex];
		Item bagItem = FindBagItemByEntry(active.indexUI, active.itemId);
		if (bagItem == null)
		{
			active.isMissing = true;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		Item matItem = null;
		if (runningTab == 1 && companionMaterialId >= 0)
		{
			matItem = FindBagItemById(companionMaterialId, bagItem.indexUI);
			if (matItem == null)
			{
				string matLabel = string.IsNullOrEmpty(companionMaterialName) ? ("ID " + companionMaterialId) : companionMaterialName;
				StopAuto("Đã hết nguyên liệu đi kèm: " + matLabel);
				return;
			}
		}
		MoveNearBaHatMit();
		HideCombinePanelsIfNeeded();
		MyVector combineVec = PrepareCombineVector(bagItem, matItem);
		Service.gI().combine(1, combineVec);
		stateEnteredAtMs = now;
	}

	private void HandleWaitResult(long now)
	{
		if (waitResultGotCombine && !waitResultGotBag && now - waitResultPacketAtMs >= 200)
		{
			SyncQueueFromBag(runningTab, notifyOnReach: true);
			lastResultTimeMs = now;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		if (!waitResultGotCombine && waitResultGotBag && now - stateEnteredAtMs >= 250)
		{
			lastResultTimeMs = now;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
			return;
		}
		if (now - stateEnteredAtMs >= 3000)
		{
			SyncQueueFromBag(runningTab, notifyOnReach: true);
			lastResultTimeMs = now;
			state = State.CheckAndPrepare;
			stateEnteredAtMs = now;
		}
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
		case 1:
			ToggleItemFromBag(p as Item);
			break;
		case 2:
			ToggleGeneralItemFromBag(p as Item);
			break;
		case 3:
			SetCompanionMaterial(p as Item);
			break;
		case 100:
			ToggleWindow();
			break;
		case 101:
			if (isRunning)
			{
				StopAuto("Đã dừng thủ công");
			}
			else
			{
				StartAuto();
			}
			break;
		}
	}

	private void OpenInputField(int mode, string title, string initialText)
	{
		editFieldMode = mode;
		ChatTextField tf = ChatTextField.gI();
		tf.strChat = title;
		tf.tfChat.name = initialText;
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
		if (editFieldMode > 0 && int.TryParse((text ?? string.Empty).Trim(), out int val))
		{
			if (editFieldMode == 1)
			{
				SetDefaultTargetStar(val);
				NotifyInfo("Sao mục tiêu mặc định: " + defaultTargetStar + " sao");
			}
			else if (editFieldMode == 2)
			{
				SetUpgradeDelayMs(val);
				NotifyInfo("Delay đập Pha Lê: " + upgradeDelayMs + " ms");
			}
			else if (editFieldMode == 3)
			{
				SetMenuRetryDelayMs(val);
				NotifyInfo("Delay menu Pha Lê: " + menuRetryDelayMs + " ms");
			}
			else if (editFieldMode == 4)
			{
				SetSellGoldDelayMs(val);
				NotifyInfo("Delay bán TV: " + sellGoldDelayMs + " ms");
			}
			else if (editFieldMode == 5)
			{
				SetMinGoldBillion(val);
				NotifyInfo("Vàng bán khi dưới: " + minGoldBillion + " Tỷ");
			}
			else if (editFieldMode == 6)
			{
				SetMaxGoldBillion(val);
				NotifyInfo("Bán vàng đến mức: " + maxGoldBillion + " Tỷ");
			}
			else if (editFieldMode == 7)
			{
				SetGeneralUpgradeDelayMs(val);
				NotifyInfo("Delay đập Nâng Cấp: " + generalUpgradeDelayMs + " ms");
			}
			else if (editFieldMode == 8)
			{
				SetGeneralMenuDelayMs(val);
				NotifyInfo("Delay menu Nâng Cấp: " + generalMenuDelayMs + " ms");
			}
			else if (editFieldMode >= 100)
			{
				int idx = editFieldMode - 100;
				if (idx >= 0 && idx < queue.Count)
				{
					int clamped = System.Math.Max(1, System.Math.Min(30, val));
					queue[idx].targetStar = clamped;
					queue[idx].isDone = queue[idx].maxedByServer || queue[idx].currStar >= clamped;
					NotifyInfo("Đặt " + queue[idx].itemName + " -> " + clamped + " sao");
				}
			}
		}
		else if (editFieldMode > 0)
		{
			NotifyInfo("Vui lòng nhập số hợp lệ!");
		}
		onCancelChat();
	}

	public void onCancelChat()
	{
		editFieldMode = 0;
		AutoItem.ResetTF(ChatTextField.gI());
	}

	private void UpdateTouch()
	{
		if (ChatTextField.gI().isShow)
		{
			return;
		}
		EnsureWindowPosition();
		int closeX = winX + winW - 22;
		int closeY = winY + 5;
		if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(closeX, closeY, 18, 18))
		{
			isShowWindow = false;
			isDragging = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		if (GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(winX, winY, winW - 26, 25))
		{
			if (!isDragging)
			{
				isDragging = true;
				dragOffsetX = GameCanvas.px - winX;
				dragOffsetY = GameCanvas.py - winY;
			}
			else
			{
				int w = GameCanvas.w > 0 ? GameCanvas.w : 800;
				int h = GameCanvas.h > 0 ? GameCanvas.h : 480;
				winX = System.Math.Max(0, System.Math.Min(w - winW, GameCanvas.px - dragOffsetX));
				winY = System.Math.Max(0, System.Math.Min(h - winH, GameCanvas.py - dragOffsetY));
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

		// Tab Bar
		int tabY = winY + 28;
		if (GameCanvas.isPointerHoldIn(winX + 8, tabY, 120, 18))
		{
			if (selectedTab != 0)
			{
				selectedTab = 0;
				SetPickerMode(0);
			}
			GameCanvas.clearAllPointerEvent();
			return;
		}
		if (GameCanvas.isPointerHoldIn(winX + 132, tabY, 120, 18))
		{
			if (selectedTab != 1)
			{
				selectedTab = 1;
				SetPickerMode(0);
			}
			GameCanvas.clearAllPointerEvent();
			return;
		}

		if (selectedTab == 0)
		{
			// Tab 0: Pha Lê Hóa
			int row1Y = winY + 50;
			if (GameCanvas.isPointerHoldIn(winX + 52, row1Y, 36, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(1, "Sao mục tiêu mặc định (1-30)", defaultTargetStar.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 124, row1Y, 126, 15))
			{
				CycleUpgradeBatchMode();
				GameCanvas.clearAllPointerEvent();
				return;
			}

			int row2Y = winY + 69;
			if (GameCanvas.isPointerHoldIn(winX + 70, row2Y, 48, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(2, "Delay giữa các lượt đập (ms, >= 0)", upgradeDelayMs.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 162, row2Y, 88, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(3, "Delay thử lại mở menu (ms, >= 50)", menuRetryDelayMs.ToString());
				return;
			}

			int row3Y = winY + 88;
			if (GameCanvas.isPointerHoldIn(winX + 54, row3Y, 46, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(4, "Delay bán thỏi vàng (ms, >= 20)", sellGoldDelayMs.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 142, row3Y, 36, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(5, "Vàng dưới mức này sẽ bán TV (Tỷ)", minGoldBillion.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 198, row3Y, 52, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(6, "Bán thỏi vàng đến mức này (Tỷ)", maxGoldBillion.ToString());
				return;
			}

			int row4Y = winY + 108;
			if (GameCanvas.isPointerHoldIn(winX + 10, row4Y, 120, 14))
			{
				allowAddFromBag = !allowAddFromBag;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 134, row4Y, 116, 14))
			{
				hideUi = !hideUi;
				GameCanvas.clearAllPointerEvent();
				return;
			}

			int row5Y = winY + 124;
			if (GameCanvas.isPointerHoldIn(winX + 10, row5Y, 120, 14))
			{
				showOldHud = !showOldHud;
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}
		else
		{
			// Tab 1: Nâng Cấp Chung
			int row1Y = winY + 50;
			if (GameCanvas.isPointerHoldIn(winX + 44, row1Y, 112, 15))
			{
				NotifyInfo("Tab Nâng Cấp Chung luôn chọn nút đầu (Làm phép)");
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 194, row1Y, 56, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(7, "Delay đập Nâng Cấp (ms, >= 0)", generalUpgradeDelayMs.ToString());
				return;
			}

			int row2Y = winY + 69;
			if (GameCanvas.isPointerHoldIn(winX + 78, row2Y, 54, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(8, "Delay menu Nâng Cấp (ms, >= 50)", generalMenuDelayMs.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 190, row2Y, 60, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(4, "Delay bán thỏi vàng (ms, >= 20)", sellGoldDelayMs.ToString());
				return;
			}

			int row3Y = winY + 88;
			if (GameCanvas.isPointerHoldIn(winX + 50, row3Y, 34, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(5, "Vàng dưới mức này sẽ bán TV (Tỷ)", minGoldBillion.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 100, row3Y, 42, 15))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(6, "Bán thỏi vàng đến mức này (Tỷ)", maxGoldBillion.ToString());
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 146, row3Y + 1, 56, 14))
			{
				hideUi = !hideUi;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 204, row3Y + 1, 50, 14))
			{
				allowAddFromBag = !allowAddFromBag;
				GameCanvas.clearAllPointerEvent();
				return;
			}

			int row4Y = winY + 107;
			if (GameCanvas.isPointerHoldIn(winX + 162, row4Y, 52, 15))
			{
				SetPickerMode(pickerMode == 2 ? 0 : 2);
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + 217, row4Y, 33, 15))
			{
				ClearCompanionMaterial();
				NotifyInfo("Đã bỏ chọn nguyên liệu đi kèm");
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// Row 6: Picker toggle + pagination
		int headerRowY = winY + 142;
		if (GameCanvas.isPointerHoldIn(winX + 126, headerRowY - 2, 64, 15))
		{
			SetPickerMode(pickerMode != 0 ? 0 : 1);
			GameCanvas.clearAllPointerEvent();
			return;
		}
		int totalPages = (pickerMode != 0) ? GetBagPickerTotalPages() : GetTotalPagesForTab(selectedTab);
		if (totalPages > 1)
		{
			if (GameCanvas.isPointerHoldIn(winX + winW - 60, headerRowY - 1, 16, 15))
			{
				if (pickerMode != 0)
				{
					pickerPage = (pickerPage - 1 + totalPages) % totalPages;
				}
				else
				{
					SetPageForTab(selectedTab, (GetPageForTab(selectedTab) - 1 + totalPages) % totalPages);
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + winW - 26, headerRowY - 1, 16, 15))
			{
				if (pickerMode != 0)
				{
					pickerPage = (pickerPage + 1) % totalPages;
				}
				else
				{
					SetPageForTab(selectedTab, (GetPageForTab(selectedTab) + 1) % totalPages);
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// Queue / Bag Picker rows
		int listY = winY + 158;
		List<PhaLeQueueEntry> tabQueue = GetQueueForTab(selectedTab);
		if (pickerMode != 0)
		{
			int pickStart = pickerPage * RowsPerPage;
			for (int slot = 0; slot < RowsPerPage; slot++)
			{
				int idx = pickStart + slot;
				if (idx >= bagPickerItems.Count)
				{
					break;
				}
				int rowY = listY + 3 + slot * 23;
				if (GameCanvas.isPointerHoldIn(winX + 10, rowY, winW - 20, 21))
				{
					GameCanvas.clearAllPointerEvent();
					if (pickerMode == 2)
					{
						SetCompanionMaterial(bagPickerItems[idx]);
						SetPickerMode(0);
					}
					else
					{
						ToggleItemFromBag(bagPickerItems[idx]);
					}
					return;
				}
			}
		}
		else if (tabQueue.Count == 0)
		{
			if (GameCanvas.isPointerHoldIn(winX + 10, listY, winW - 20, 74))
			{
				SetPickerMode(1);
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}
		else
		{
			int startIdx = GetPageForTab(selectedTab) * RowsPerPage;
			for (int slot = 0; slot < RowsPerPage; slot++)
			{
				int idx = startIdx + slot;
				if (idx >= tabQueue.Count)
				{
					break;
				}
				int rowY = listY + 3 + slot * 23;
				if (GameCanvas.isPointerHoldIn(winX + winW - 31, rowY + 2, 17, 16))
				{
					GameCanvas.clearAllPointerEvent();
					RemoveItemAt(idx, selectedTab);
					return;
				}
				if (selectedTab == 0 && GameCanvas.isPointerHoldIn(winX + winW - 55, rowY + 2, 21, 16))
				{
					GameCanvas.clearAllPointerEvent();
					OpenInputField(100 + idx, "Số sao mục tiêu: " + tabQueue[idx].itemName, tabQueue[idx].targetStar.ToString());
					return;
				}
			}
		}

		// Action buttons
		int btnX = winX + 10;
		int btnW = winW - 20;
		int startBtnY = winY + 265;
		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 22))
		{
			GameCanvas.clearAllPointerEvent();
			if (isRunning)
			{
				StopAuto("Đã dừng thủ công");
			}
			else
			{
				StartAuto();
			}
			return;
		}

		int bottomRowY = winY + 292;
		if (GameCanvas.isPointerHoldIn(winX + 10, bottomRowY, 118, 20))
		{
			GameCanvas.clearAllPointerEvent();
			ClearDoneItems(selectedTab);
			return;
		}
		if (GameCanvas.isPointerHoldIn(winX + 132, bottomRowY, 118, 20))
		{
			GameCanvas.clearAllPointerEvent();
			ClearAllItems(selectedTab);
			return;
		}

		if (GameCanvas.isPointerHoldIn(winX, winY, winW, winH))
		{
			GameCanvas.clearAllPointerEvent();
		}
	}

	public void Paint(mGraphics g)
	{
		if (showOldHud && isRunning && runningTab == 0 && !ModFunc.isEditButton && Char.myCharz() != null)
		{
			PaintOldStyleHud(g);
		}
		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowPosition();

		// Khung gỗ vàng đồng bộ
		g.setColor(0x160A03, 0.94f);
		g.fillRect(winX - 2, winY - 2, winW + 4, winH + 4, 6);
		g.setColor(0xD49A2A);
		g.fillRect(winX, winY, winW, winH, 5);
		g.setColor(0xF7CE68);
		g.drawRect(winX + 1, winY + 1, winW - 3, winH - 3);
		g.setColor(0x3B1F0E);
		g.fillRect(winX + 4, winY + 4, winW - 8, winH - 8, 4);
		g.setColor(0x472511);
		g.fillRect(winX + 6, winY + 27, winW - 12, winH - 33, 3);

		// Header bar
		g.setColor(0x693414);
		g.fillRect(winX + 4, winY + 4, winW - 8, 22, 3);
		g.setColor(0xD49A2A);
		g.drawLine(winX + 4, winY + 26, winX + winW - 5, winY + 26);

		int centerX = winX + winW / 2;
		mFont.tahoma_7b_yellow.drawStringBorder(g, "AUTO BÀ HẠT MÍT (I / F6)", centerX - 6, winY + 6, mFont.CENTER, mFont.tahoma_7_grey);

		int closeX = winX + winW - 22;
		int closeY = winY + 5;
		g.setColor(0xB71C1C);
		g.fillRect(closeX, closeY, 16, 16, 3);
		g.setColor(0xE53935);
		g.fillRect(closeX + 1, closeY + 1, 14, 14, 2);
		mFont.tahoma_7b_white.drawString(g, "X", closeX + 8, closeY + 2, mFont.CENTER);

		// Tab Bar
		int tabY = winY + 28;
		DrawTabButton(g, winX + 8, tabY, 120, 18, "Pha Lê Hóa", selectedTab == 0);
		DrawTabButton(g, winX + 132, tabY, 120, 18, "Nâng Cấp Chung", selectedTab == 1);

		if (selectedTab == 0)
		{
			int row1Y = winY + 50;
			mFont.tahoma_7_white.drawString(g, "Sao MT:", winX + 10, row1Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 52, row1Y, 36, 15, defaultTargetStar.ToString(), mFont.tahoma_7b_yellow);
			mFont.tahoma_7_white.drawString(g, "Đập:", winX + 96, row1Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 124, row1Y, 126, 15, "Chế độ: " + upgradeBatchMode + " lần", mFont.tahoma_7b_green);

			int row2Y = winY + 69;
			mFont.tahoma_7_white.drawString(g, "Delay đập:", winX + 10, row2Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 70, row2Y, 48, 15, upgradeDelayMs + "ms", mFont.tahoma_7b_green);
			mFont.tahoma_7_white.drawString(g, "Menu:", winX + 126, row2Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 162, row2Y, 88, 15, menuRetryDelayMs + "ms", mFont.tahoma_7b_green);

			int row3Y = winY + 88;
			mFont.tahoma_7_white.drawString(g, "Bán TV:", winX + 10, row3Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 54, row3Y, 46, 15, sellGoldDelayMs + "ms", mFont.tahoma_7b_green);
			mFont.tahoma_7_white.drawString(g, "Vàng <", winX + 105, row3Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 142, row3Y, 36, 15, minGoldBillion + "T", mFont.tahoma_7b_yellow);
			mFont.tahoma_7_white.drawString(g, "->", winX + 182, row3Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 198, row3Y, 52, 15, maxGoldBillion + "T", mFont.tahoma_7b_yellow);

			int row4Y = winY + 108;
			DrawCheckbox(g, winX + 10, row4Y, allowAddFromBag, "Cho phép thêm đồ");
			DrawCheckbox(g, winX + 134, row4Y, hideUi, "Ẩn hiệu ứng/UI");

			int row5Y = winY + 124;
			DrawCheckbox(g, winX + 10, row5Y, showOldHud, "Hiện UI cũ (HUD)");
			string gemSummary = (Char.myCharz() != null)
				? ("NX:" + NinjaUtil.getMoneys(Char.myCharz().luong) + " NH:" + NinjaUtil.getMoneys(Char.myCharz().luongKhoa))
				: "NX:0 NH:0";
			mFont.tahoma_7_yellow.drawString(g, gemSummary, winX + 250, row5Y, mFont.RIGHT);
		}
		else
		{
			int row1Y = winY + 50;
			mFont.tahoma_7_white.drawString(g, "Menu:", winX + 10, row1Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 44, row1Y, 112, 15, "Nút đầu (Làm phép)", mFont.tahoma_7b_green);
			mFont.tahoma_7_white.drawString(g, "Đập:", winX + 162, row1Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 194, row1Y, 56, 15, generalUpgradeDelayMs + "ms", mFont.tahoma_7b_green);

			int row2Y = winY + 69;
			mFont.tahoma_7_white.drawString(g, "Delay menu:", winX + 10, row2Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 78, row2Y, 54, 15, generalMenuDelayMs + "ms", mFont.tahoma_7b_green);
			mFont.tahoma_7_white.drawString(g, "Bán TV:", winX + 140, row2Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 190, row2Y, 60, 15, sellGoldDelayMs + "ms", mFont.tahoma_7b_green);

			int row3Y = winY + 88;
			mFont.tahoma_7_white.drawString(g, "Vàng <", winX + 10, row3Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 50, row3Y, 34, 15, minGoldBillion + "T", mFont.tahoma_7b_yellow);
			mFont.tahoma_7_white.drawString(g, "->", winX + 87, row3Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 100, row3Y, 42, 15, maxGoldBillion + "T", mFont.tahoma_7b_yellow);
			DrawCheckbox(g, winX + 146, row3Y + 2, hideUi, "Ẩn UI");
			DrawCheckbox(g, winX + 204, row3Y + 2, allowAddFromBag, "Thêm");

			int row4Y = winY + 107;
			string matText;
			if (companionMaterialId >= 0)
			{
				int matCount = GetBagItemCount(companionMaterialId);
				string baseName = string.IsNullOrEmpty(companionMaterialName) ? ("ID " + companionMaterialId) : companionMaterialName;
				string shortMat = baseName.Length > 13 ? (baseName.Substring(0, 12) + "..") : baseName;
				matText = "NL: " + shortMat + " x" + matCount;
			}
			else
			{
				matText = "NL kèm: Không dùng";
			}
			(companionMaterialId >= 0 ? mFont.tahoma_7_green : mFont.tahoma_7_white).drawString(g, matText, winX + 10, row4Y + 2, mFont.LEFT);
			DrawInputBox(g, winX + 162, row4Y, 52, 15, "Chọn NL", mFont.tahoma_7b_yellow);
			DrawInputBox(g, winX + 217, row4Y, 33, 15, "Xóa", mFont.tahoma_7b_red);
		}

		// Row 6: Queue / Picker header & pagination
		int headerRowY = winY + 142;
		List<PhaLeQueueEntry> tabQueue = GetQueueForTab(selectedTab);
		string headerLabel = (pickerMode == 1)
			? ("Chọn từ túi (" + bagPickerItems.Count + "):")
			: (pickerMode == 2)
			? ("Chọn NL kèm (" + bagPickerItems.Count + "):")
			: (selectedTab == 1)
			? ("DS Trang bị (" + tabQueue.Count + "):")
			: ("DS Pha Lê (" + tabQueue.Count + "):");
		mFont.tahoma_7_yellow.drawString(g, headerLabel, winX + 10, headerRowY, mFont.LEFT);

		int pickBtnX = winX + 126;
		g.setColor(pickerMode != 0 ? 0x8C5811 : 0x2E7D32);
		g.fillRect(pickBtnX, headerRowY - 1, 64, 13, 2);
		g.setColor(pickerMode != 0 ? 0xF7CE68 : 0x66BB6A);
		g.drawRect(pickBtnX, headerRowY - 1, 63, 12);
		string pickBtnLabel = (pickerMode != 0) ? "< Quay lại" : (selectedTab == 1 ? "+ Chọn TB" : "+ Chọn đồ");
		mFont.tahoma_7b_white.drawString(g, pickBtnLabel, pickBtnX + 32, headerRowY, mFont.CENTER);

		int totalPages = (pickerMode != 0) ? GetBagPickerTotalPages() : GetTotalPagesForTab(selectedTab);
		int displayPage = (pickerMode != 0) ? pickerPage : GetPageForTab(selectedTab);
		if (totalPages > 1)
		{
			g.setColor(0x2B1609);
			g.fillRect(winX + winW - 58, headerRowY - 1, 14, 13, 2);
			g.fillRect(winX + winW - 26, headerRowY - 1, 14, 13, 2);
			mFont.tahoma_7b_white.drawString(g, "<", winX + winW - 51, headerRowY, mFont.CENTER);
			mFont.tahoma_7_white.drawString(g, (displayPage + 1) + "/" + totalPages, winX + winW - 35, headerRowY, mFont.CENTER);
			mFont.tahoma_7b_white.drawString(g, ">", winX + winW - 19, headerRowY, mFont.CENTER);
		}

		// Queue / Bag picker frame
		int listX = winX + 10;
		int listY = winY + 158;
		int listW = winW - 20;
		int listH = 74;
		g.setColor(0x2B1609);
		g.fillRect(listX, listY, listW, listH, 3);
		g.setColor(0x8D5B1C);
		g.drawRect(listX, listY, listW - 1, listH - 1);

		if (pickerMode != 0)
		{
			if (bagPickerItems.Count == 0)
			{
				mFont.tahoma_7_white.drawString(g, "Hành trang trống", centerX, listY + 26, mFont.CENTER);
			}
			else
			{
				int pickStart = pickerPage * RowsPerPage;
				for (int slot = 0; slot < RowsPerPage; slot++)
				{
					int idx = pickStart + slot;
					if (idx >= bagPickerItems.Count)
					{
						break;
					}
					Item bagItem = bagPickerItems[idx];
					bool added = (pickerMode == 2) ? IsCompanionMaterial(bagItem) : IsItemInQueueList(tabQueue, bagItem);
					int rowY = listY + 3 + slot * 23;
					g.setColor(added ? 0x1F4D2E : 0x3B1F0E);
					g.fillRect(listX + 3, rowY, listW - 6, 20, 2);
					if (added)
					{
						g.setColor(0x66BB6A);
						g.drawRect(listX + 3, rowY, listW - 7, 19);
					}
					string itemName = (bagItem.template != null && !string.IsNullOrEmpty(bagItem.template.name)) ? bagItem.template.name : ("ID " + bagItem.template.id);
					string shortName = itemName.Length > 15 ? (itemName.Substring(0, 14) + "..") : itemName;
					string rowPrefix = (pickerMode == 2)
						? (added ? "[NL kèm] " : "[Chọn NL] ")
						: (added ? "[Đã chọn] " : "[+ Thêm] ");
					string rowSuffix = (pickerMode == 2)
						? (" x" + System.Math.Max(1, bagItem.quantity))
						: (selectedTab == 0 ? (" (" + GetCurrPhaLe(bagItem) + "s)") : (" (#" + bagItem.indexUI + ")"));
					(added ? mFont.tahoma_7_green : mFont.tahoma_7_white).drawString(g, rowPrefix + shortName + rowSuffix, listX + 6, rowY + 4, mFont.LEFT);
				}
			}
		}
		else if (tabQueue.Count == 0)
		{
			mFont.tahoma_7_white.drawString(g, "Chưa có đồ trong danh sách", centerX, listY + 20, mFont.CENTER);
			mFont.tahoma_7_yellow.drawString(g, selectedTab == 1 ? "Bấm '+ Chọn TB' hoặc từ Hành trang" : "Bấm '+ Chọn đồ' hoặc từ Hành trang", centerX, listY + 36, mFont.CENTER);
		}
		else
		{
			int startIdx = GetPageForTab(selectedTab) * RowsPerPage;
			for (int slot = 0; slot < RowsPerPage; slot++)
			{
				int idx = startIdx + slot;
				if (idx >= tabQueue.Count)
				{
					break;
				}
				PhaLeQueueEntry entry = tabQueue[idx];
				int rowY = listY + 3 + slot * 23;
				bool isActive = isRunning && runningTab == selectedTab && idx == activeQueueIndex;
				g.setColor(isActive ? 0x5A3512 : 0x3B1F0E);
				g.fillRect(listX + 3, rowY, listW - 6, 20, 2);
				if (isActive)
				{
					g.setColor(0xF7CE68);
					g.drawRect(listX + 3, rowY, listW - 7, 19);
				}

				string shortName = entry.itemName.Length > 14 ? (entry.itemName.Substring(0, 13) + "..") : entry.itemName;
				string tag = entry.isDone ? (selectedTab == 1 ? "[Đã xong]" : "[Đủ sao]") : (entry.isMissing ? "[Mất]" : (isActive ? "[Đập]" : "[Chờ]"));
				mFont rowFont = entry.isDone ? mFont.tahoma_7_green : (entry.isMissing ? mFont.tahoma_7_red : (isActive ? mFont.tahoma_7_yellow : mFont.tahoma_7_white));
				string rowDetail = (selectedTab == 0) ? (" " + entry.currStar + "/" + entry.targetStar + "s") : (" (#" + entry.indexUI + ")");
				rowFont.drawString(g, tag + " " + shortName + rowDetail, listX + 6, rowY + 4, mFont.LEFT);

				if (selectedTab == 0)
				{
					int starBtnX = winX + winW - 54;
					g.setColor(0x693414);
					g.fillRect(starBtnX, rowY + 2, 20, 15, 2);
					g.setColor(0xD49A2A);
					g.drawRect(starBtnX, rowY + 2, 19, 14);
					mFont.tahoma_7_yellow.drawString(g, "*", starBtnX + 10, rowY + 3, mFont.CENTER);
				}

				int delX = winX + winW - 30;
				g.setColor(0xB71C1C);
				g.fillRect(delX, rowY + 2, 16, 15, 2);
				mFont.tahoma_7b_white.drawString(g, "x", delX + 8, rowY + 3, mFont.CENTER);
			}
		}

		// Stats & Status
		mFont.tahoma_7_white.drawString(g, "Lượt:" + totalCombineSent + " - OK:" + totalSuccessCount + " - Bán TV:" + totalGoldBarsSold, centerX, winY + 236, mFont.CENTER);
		string paintStatus = statusText.Length > 42 ? (statusText.Substring(0, 40) + "..") : statusText;
		mFont.tahoma_7_yellow.drawString(g, paintStatus, centerX, winY + 249, mFont.CENTER);

		// Buttons
		int btnX = winX + 10;
		int btnW = winW - 20;
		int startBtnY = winY + 265;
		DrawActionButton(g, btnX, startBtnY, btnW, 22, isRunning ? 0xD35400 : 0x2E7D32, isRunning ? 0xE67E22 : 0x43A047, isRunning ? 0xF39C12 : 0x66BB6A);
		string runLabel = isRunning
			? ("■ DỪNG AUTO (Tab " + (runningTab + 1) + " - F6)")
			: (selectedTab == 1 ? "▶ BẮT ĐẦU NÂNG CẤP CHUNG" : "▶ BẮT ĐẦU AUTO PHA LÊ");
		mFont.tahoma_7b_white.drawStringBorder(g, runLabel, btnX + btnW / 2, startBtnY + 4, mFont.CENTER, mFont.tahoma_7_grey);

		int bottomRowY = winY + 292;
		DrawActionButton(g, winX + 10, bottomRowY, 118, 20, 0x5D4037, 0x795548, 0xA1887F);
		mFont.tahoma_7b_white.drawStringBorder(g, "XÓA ĐÃ XONG", winX + 69, bottomRowY + 3, mFont.CENTER, mFont.tahoma_7_grey);

		DrawActionButton(g, winX + 132, bottomRowY, 118, 20, 0x8E1616, 0xC62828, 0xE53935);
		mFont.tahoma_7b_white.drawStringBorder(g, "XÓA TẤT CẢ", winX + 191, bottomRowY + 3, mFont.CENTER, mFont.tahoma_7_grey);
	}

	private void PaintOldStyleHud(mGraphics g)
	{
		PhaLeQueueEntry active = (activeQueueIndex >= 0 && activeQueueIndex < queue.Count) ? queue[activeQueueIndex] : null;
		mFont.tahoma_7b_red.drawString(g, (active != null) ? active.itemName : "Chưa Có", GameCanvas.w / 2, 72, mFont.CENTER);
		mFont.tahoma_7b_red.drawString(g, (active != null) ? ("Số Sao : " + active.currStar) : "Số Sao : -1", GameCanvas.w / 2, 82, mFont.CENTER);
		mFont.tahoma_7b_red.drawString(g, "Số Sao Cần Đập : " + ((active != null) ? active.targetStar : defaultTargetStar) + " Sao", GameCanvas.w / 2, 92, mFont.CENTER);
		int tvCount = GetBagItemCount(GoldBarItemId);
		mFont.tahoma_7b_red.drawString(g, "Ngọc Xanh : " + NinjaUtil.getMoneys(Char.myCharz().luong) + " Ngọc Hồng : " + NinjaUtil.getMoneys(Char.myCharz().luongKhoa), GameCanvas.w / 2, 102, mFont.CENTER);
		mFont.tahoma_7b_red.drawString(g, "Vàng : " + NinjaUtil.getMoneys(Char.myCharz().xu) + " Thỏi Vàng : " + tvCount, GameCanvas.w / 2, 112, mFont.CENTER);
	}

	private static void DrawTabButton(mGraphics g, int x, int y, int w, int h, string text, bool active)
	{
		g.setColor(active ? 0x8C5811 : 0x2B1609);
		g.fillRect(x, y, w, h, 3);
		g.setColor(active ? 0xF7CE68 : 0x8D5B1C);
		g.drawRect(x, y, w - 1, h - 1);
		(active ? mFont.tahoma_7b_yellow : mFont.tahoma_7_white).drawString(g, text, x + w / 2, y + 3, mFont.CENTER);
	}

	private static void DrawInputBox(mGraphics g, int x, int y, int w, int h, string text, mFont font)
	{
		g.setColor(0x231107);
		g.fillRect(x, y, w, h, 3);
		g.setColor(0xC58927);
		g.drawRect(x, y, w - 1, h - 1);
		font.drawString(g, text, x + w / 2, y + 2, mFont.CENTER);
	}

	private static void DrawCheckbox(mGraphics g, int x, int y, bool isChecked, string text)
	{
		g.setColor(isChecked ? 0x43A047 : 0x261409);
		g.fillRect(x, y, 11, 11, 2);
		if (isChecked)
		{
			g.setColor(0x66BB6A);
			g.fillRect(x + 1, y + 1, 9, 9, 2);
			mFont.tahoma_7b_white.drawString(g, "v", x + 6, y - 1, mFont.CENTER);
		}
		g.setColor(0xD49A2A);
		g.drawRect(x, y, 10, 10);
		(isChecked ? mFont.tahoma_7_green : mFont.tahoma_7_white).drawString(g, text, x + 14, y, mFont.LEFT);
	}

	private static void DrawActionButton(mGraphics g, int x, int y, int w, int h, int colorDark, int colorMid, int colorHighlight)
	{
		g.setColor(0x160A03);
		g.fillRect(x + 2, y, w - 4, h);
		g.fillRect(x, y + 2, w, h - 4);
		g.fillRect(x + 1, y + 1, w - 2, h - 2);
		g.setColor(colorDark);
		g.fillRect(x + 2, y + 1, w - 4, h - 3);
		g.fillRect(x + 1, y + 2, w - 2, h - 5);
		g.setColor(colorMid);
		g.fillRect(x + 3, y + 2, w - 6, (h - 4) / 2);
		g.setColor(colorHighlight);
		g.drawLine(x + 2, y, x + w - 3, y);
		g.drawLine(x + 2, y + h - 1, x + w - 3, y + h - 1);
		g.drawLine(x, y + 2, x, y + h - 3);
		g.drawLine(x + w - 1, y + 2, x + w - 1, y + h - 3);
	}
}
