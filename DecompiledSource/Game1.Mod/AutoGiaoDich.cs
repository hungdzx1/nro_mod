using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Mod;

public class AutoGiaoDich : IChatable, IActionListener
{
	public enum TradeRole
	{
		Sender,
		Receiver
	}

	public enum TradeState
	{
		Idle,
		SenderSendInvite,
		WaitOpenSession,
		PutItems,
		WaitLockDelay,
		WaitPartnerLock,
		WaitAcceptDelay,
		WaitTradeClose,
		WaitCooldownNextTrade,
		ReceiverWaitingInvite
	}

	public struct BagItemSummary
	{
		public int ItemId;

		public string Name;

		public int TotalQuantity;
	}

	public struct PartnerSummary
	{
		public int CharId;

		public string Name;
	}

	private static AutoGiaoDich instance;

	public bool isShowWindow;

	public TradeRole role = TradeRole.Sender;

	public int targetCharId = -1;

	public string targetCharName = "Chưa chọn";

	public int targetItemId = -1;

	public string targetItemName = "Chưa chọn";

	public int qtyPerTrade = 10000;

	public int targetLoops;

	public int delayBetweenTradesMs = 1000;

	public int delayLockMs = 300;

	public int delayAcceptMs = 300;

	public bool hidePanelDuringTrade = true;

	public bool isRunning;

	public TradeState state = TradeState.Idle;

	public long stateStartMs;

	public int completedTrades;

	public int inviteRetryCount;

	public long nextActionAllowedMs;

	public bool inTradeSession;

	public int sessionPartnerId = -1;

	public bool isSelfLocked;

	public bool isFriendLocked;

	public bool sentAccept;

	public bool bagUpdatedSinceTradeClose;

	public bool pendingPanelClose;

	public string lastStatus = "Chưa chạy";

	private int viewMode;

	private int listPage;

	private const int ItemsPerPage = 6;

	private readonly List<PartnerSummary> cachedPartners = new List<PartnerSummary>();

	private readonly List<BagItemSummary> cachedBagItems = new List<BagItemSummary>();

	private int winX = -1;

	private int winY = -1;

	private const int WinW = 248;

	private const int WinH = 248;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	private bool isWaitingInput;

	private string inputContext = string.Empty;

	private const int MaxInviteRetries = 8;

	private const int MaxAbortRetries = 5;

	public static AutoGiaoDich gI()
	{
		if (instance == null)
		{
			instance = new AutoGiaoDich();
		}
		return instance;
	}

	public void OpenWindow()
	{
		isShowWindow = !isShowWindow;
		if (isShowWindow)
		{
			CloseOtherPanels();
			EnsureWindowInBounds();
			viewMode = 0;
			RefreshPartnerList();
			RefreshBagItemList();
		}
		else
		{
			isDragging = false;
		}
	}

	private static void CloseOtherPanels()
	{
		AutoGop.gI().isShowWindow = false;
		AutoMuaNhieu.gI().isShowWindow = false;
		NpcMenuHelper.gI().isShowWindow = false;
		AutoDungNhanh.gI().isShowWindow = false;
	}

	private void EnsureWindowInBounds()
	{
		int w = GameCanvas.w > 0 ? GameCanvas.w : 800;
		int h = GameCanvas.h > 0 ? GameCanvas.h : 480;
		if (winX < 0 || winY < 0)
		{
			winX = (w - WinW) / 2;
			winY = (h - WinH) / 2;
		}
		if (winX < 2)
		{
			winX = 2;
		}
		if (winY < 2)
		{
			winY = 2;
		}
		if (winX + WinW > w - 2)
		{
			winX = global::System.Math.Max(2, w - WinW - 2);
		}
		if (winY + WinH > h - 2)
		{
			winY = global::System.Math.Max(2, h - WinH - 2);
		}
	}

	public static int GetBagItemCount(int itemId)
	{
		if (itemId < 0)
		{
			return 0;
		}
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null)
		{
			return 0;
		}
		int total = 0;
		Item[] bag = myChar.arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item != null && item.template != null && item.template.id == itemId && item.quantity > 0)
			{
				total += item.quantity;
			}
		}
		return total;
	}

	public void RefreshPartnerList()
	{
		cachedPartners.Clear();
		Char myChar = Char.myCharz();
		int myId = myChar != null ? myChar.charID : -1;
		if (GameScr.vCharInMap == null)
		{
			return;
		}
		for (int i = 0; i < GameScr.vCharInMap.size(); i++)
		{
			Char c = GameScr.vCharInMap.elementAt(i) as Char;
			if (c == null || c == myChar || c.charID <= 0 || c.charID == myId || c.isPet)
			{
				continue;
			}
			if (string.IsNullOrEmpty(c.cName) || c.cName.StartsWith("$"))
			{
				continue;
			}
			cachedPartners.Add(new PartnerSummary
			{
				CharId = c.charID,
				Name = c.cName
			});
		}
	}

	public void RefreshBagItemList()
	{
		cachedBagItems.Clear();
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null)
		{
			return;
		}
		Dictionary<int, int> indexById = new Dictionary<int, int>();
		Item[] bag = myChar.arrItemBag;
		for (int i = 0; i < bag.Length; i++)
		{
			Item item = bag[i];
			if (item == null || item.template == null || item.quantity <= 0)
			{
				continue;
			}
			int id = item.template.id;
			if (indexById.TryGetValue(id, out int listIdx))
			{
				BagItemSummary existing = cachedBagItems[listIdx];
				existing.TotalQuantity += item.quantity;
				cachedBagItems[listIdx] = existing;
			}
			else
			{
				indexById[id] = cachedBagItems.Count;
				cachedBagItems.Add(new BagItemSummary
				{
					ItemId = id,
					Name = string.IsNullOrEmpty(item.template.name) ? ("ID " + id) : item.template.name,
					TotalQuantity = item.quantity
				});
			}
		}
	}

	public bool PickFocusPartner()
	{
		Char myChar = Char.myCharz();
		Char focus = myChar != null ? myChar.charFocus : null;
		if (focus == null || focus == myChar || focus.charID <= 0 || focus.isPet || string.IsNullOrEmpty(focus.cName))
		{
			Notify("Chưa target vào người chơi nào!");
			return false;
		}
		targetCharId = focus.charID;
		targetCharName = focus.cName;
		Notify("Đã chọn đối tác: " + targetCharName);
		return true;
	}

	public bool StartAuto()
	{
		if (isRunning)
		{
			return false;
		}
		Char myChar = Char.myCharz();
		if (myChar == null)
		{
			lastStatus = "Lỗi: Chưa vào nhân vật";
			return false;
		}
		if (targetCharId <= 0)
		{
			lastStatus = "Lỗi: Chưa chọn đối tác GD!";
			Notify(lastStatus);
			return false;
		}
		if (targetCharId == myChar.charID)
		{
			lastStatus = "Lỗi: Không thể tự GD chính mình!";
			Notify(lastStatus);
			return false;
		}
		if (delayBetweenTradesMs < 0)
		{
			delayBetweenTradesMs = 0;
		}
		if (delayLockMs < 0)
		{
			delayLockMs = 0;
		}
		if (delayAcceptMs < 0)
		{
			delayAcceptMs = 0;
		}
		if (targetLoops < 0)
		{
			targetLoops = 0;
		}

		if (role == TradeRole.Sender)
		{
			if (targetItemId < 0)
			{
				lastStatus = "Lỗi: Chưa chọn vật phẩm GD!";
				Notify(lastStatus);
				return false;
			}
			if (qtyPerTrade <= 0)
			{
				lastStatus = "Lỗi: SL mỗi lượt phải > 0!";
				Notify(lastStatus);
				return false;
			}
			int bagQty = GetBagItemCount(targetItemId);
			if (bagQty < qtyPerTrade)
			{
				lastStatus = "Dừng: Túi (" + bagQty + ") < SL chọn (" + qtyPerTrade + ")";
				Notify(lastStatus);
				return false;
			}
			if (GameScr.findCharInMap(targetCharId) == null)
			{
				lastStatus = "Lỗi: Đối tác không ở trong map!";
				Notify(lastStatus);
				return false;
			}
		}

		long now = mSystem.currentTimeMillis();
		isRunning = true;
		completedTrades = 0;
		inviteRetryCount = 0;
		nextActionAllowedMs = 0;
		ResetSessionFlags();

		if (role == TradeRole.Sender)
		{
			state = TradeState.SenderSendInvite;
			stateStartMs = now;
			lastStatus = "Bắt đầu gửi mời GD tới " + targetCharName;
		}
		else
		{
			state = TradeState.ReceiverWaitingInvite;
			stateStartMs = now;
			lastStatus = "Đang chờ lời mời từ " + targetCharName;
		}
		Notify(lastStatus);
		return true;
	}

	public void StopAuto(string reason = null)
	{
		bool wasRunning = isRunning;
		bool needCancelSession = inTradeSession && !sentAccept;
		isRunning = false;
		state = TradeState.Idle;
		if (needCancelSession)
		{
			Service.gI().giaodich(3, -1, -1, -1);
		}
		if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 13)
		{
			GameCanvas.panel.isAccept = true;
			pendingPanelClose = true;
		}
		ResetSessionFlags();
		if (!string.IsNullOrEmpty(reason))
		{
			lastStatus = reason;
			if (wasRunning)
			{
				Notify(reason);
			}
		}
		else if (wasRunning)
		{
			lastStatus = "Đã dừng Auto GD";
			Notify(lastStatus);
		}
	}

	private void StopAndCancelTrade(string reason)
	{
		StopAuto(reason);
	}

	private void ResetSessionFlags()
	{
		inTradeSession = false;
		sessionPartnerId = -1;
		isSelfLocked = false;
		isFriendLocked = false;
		sentAccept = false;
		bagUpdatedSinceTradeClose = false;
	}

	public bool ShouldSuppressPanelUi()
	{
		return isRunning && hidePanelDuringTrade && inTradeSession && sessionPartnerId == targetCharId;
	}

	private void TryFinalizeTradePanelClose()
	{
		if (GameCanvas.panel == null)
		{
			pendingPanelClose = false;
			return;
		}
		if (!GameCanvas.panel.isShow || GameCanvas.panel.type != 13)
		{
			pendingPanelClose = false;
			return;
		}
		GameCanvas.panel.isAccept = true;
		GameCanvas.panel.hide();
		if (!GameCanvas.panel.isShow)
		{
			pendingPanelClose = false;
		}
	}

	public void Update()
	{
		if (isRunning && !ChatTextField.gI().isShow && Input.GetKeyDown(KeyCode.F6))
		{
			StopAuto("Đã dừng bởi F6");
		}

		if (pendingPanelClose && !inTradeSession)
		{
			TryFinalizeTradePanelClose();
		}

		if (isShowWindow)
		{
			UpdateTouch();
		}

		if (!isRunning)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();
		switch (state)
		{
		case TradeState.SenderSendInvite:
			HandleSenderSendInvite(now);
			break;
		case TradeState.WaitOpenSession:
			HandleWaitOpenSession(now);
			break;
		case TradeState.PutItems:
			HandlePutItems(now);
			break;
		case TradeState.WaitLockDelay:
			HandleWaitLockDelay(now);
			break;
		case TradeState.WaitPartnerLock:
			HandleWaitPartnerLock(now);
			break;
		case TradeState.WaitAcceptDelay:
			HandleWaitAcceptDelay(now);
			break;
		case TradeState.WaitTradeClose:
			HandleWaitTradeClose(now);
			break;
		case TradeState.WaitCooldownNextTrade:
			HandleWaitCooldownNextTrade(now);
			break;
		case TradeState.ReceiverWaitingInvite:
			break;
		}
	}

	private void HandleSenderSendInvite(long now)
	{
		if (now < nextActionAllowedMs)
		{
			return;
		}
		if (targetLoops > 0 && completedTrades >= targetLoops)
		{
			StopAuto("Hoàn tất đủ " + targetLoops + " lượt GD!");
			return;
		}
		int bagQty = GetBagItemCount(targetItemId);
		if (bagQty < qtyPerTrade)
		{
			StopAuto("Dừng: Túi (" + bagQty + ") < SL chọn (" + qtyPerTrade + ")");
			return;
		}
		if (GameScr.findCharInMap(targetCharId) == null)
		{
			StopAuto("Dừng: Đối tác đã rời map!");
			return;
		}
		Service.gI().giaodich(0, targetCharId, -1, -1);
		state = TradeState.WaitOpenSession;
		stateStartMs = now;
		lastStatus = "Gửi mời GD lần " + (inviteRetryCount + 1) + "...";
	}

	private void HandleWaitOpenSession(long now)
	{
		if (role == TradeRole.Sender)
		{
			if (now - stateStartMs >= 2500L && now >= nextActionAllowedMs)
			{
				inviteRetryCount++;
				if (inviteRetryCount >= MaxInviteRetries)
				{
					StopAuto("Dừng: Đối tác không phản hồi lời mời!");
					return;
				}
				state = TradeState.SenderSendInvite;
				stateStartMs = now;
				lastStatus = "Thử mời lại (" + inviteRetryCount + "/" + MaxInviteRetries + ")...";
			}
		}
		else if (now - stateStartMs >= 5000L)
		{
			state = TradeState.ReceiverWaitingInvite;
			stateStartMs = now;
			lastStatus = "Hết giờ mở phiên, chờ mời lại...";
		}
	}

	private void HandlePutItems(long now)
	{
		int bagQty = GetBagItemCount(targetItemId);
		if (bagQty < qtyPerTrade)
		{
			StopAndCancelTrade("Dừng: Túi (" + bagQty + ") < SL chọn (" + qtyPerTrade + ")");
			return;
		}
		Char myChar = Char.myCharz();
		Item[] bag = myChar != null ? myChar.arrItemBag : null;
		if (bag == null)
		{
			StopAndCancelTrade("Lỗi: Không đọc được rương đồ!");
			return;
		}
		int remaining = qtyPerTrade;
		for (int i = 0; i < bag.Length && remaining > 0; i++)
		{
			Item item = bag[i];
			if (item == null || item.template == null || item.template.id != targetItemId || item.quantity <= 0)
			{
				continue;
			}
			int putQty = item.quantity < remaining ? item.quantity : remaining;
			Service.gI().giaodich(2, -1, (sbyte)item.indexUI, putQty);
			if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.type == 13)
			{
				Item copy = new Item
				{
					template = item.template,
					quantity = putQty,
					indexUI = item.indexUI,
					itemOption = item.itemOption
				};
				GameCanvas.panel.vMyGD.addElement(copy);
			}
			remaining -= putQty;
		}
		if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.type == 13 && GameCanvas.panel.currentTabIndex == 1)
		{
			GameCanvas.panel.setTabGiaoDich(isMe: true);
		}
		if (remaining > 0)
		{
			StopAndCancelTrade("Dừng: Không đủ số lượng vật phẩm!");
			return;
		}
		state = TradeState.WaitLockDelay;
		stateStartMs = now;
		lastStatus = "Đã bỏ " + qtyPerTrade + " VP, chờ khóa (" + delayLockMs + "ms)...";
	}

	private void HandleWaitLockDelay(long now)
	{
		if (now - stateStartMs < delayLockMs)
		{
			return;
		}
		if (!isSelfLocked)
		{
			isSelfLocked = true;
			if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.type == 13)
			{
				GameCanvas.panel.isLock = true;
			}
			Service.gI().giaodich(5, -1, -1, -1);
		}
		if (isFriendLocked)
		{
			state = TradeState.WaitAcceptDelay;
			stateStartMs = now;
			lastStatus = "Hai bên đã khóa, chờ xác nhận (" + delayAcceptMs + "ms)...";
		}
		else
		{
			state = TradeState.WaitPartnerLock;
			stateStartMs = now;
			lastStatus = "Đã khóa GD, chờ đối tác khóa...";
		}
	}

	private void HandleWaitPartnerLock(long now)
	{
		if (isFriendLocked)
		{
			state = TradeState.WaitAcceptDelay;
			stateStartMs = now;
			lastStatus = "Đối tác đã khóa, chờ xác nhận (" + delayAcceptMs + "ms)...";
			return;
		}
		long timeoutMs = global::System.Math.Max(20000L, (long)delayLockMs + 15000L);
		if (now - stateStartMs >= timeoutMs)
		{
			StopAndCancelTrade("Dừng: Hết thời gian chờ đối tác khóa!");
		}
	}

	private void HandleWaitAcceptDelay(long now)
	{
		if (now - stateStartMs < delayAcceptMs)
		{
			return;
		}
		if (!sentAccept)
		{
			sentAccept = true;
			if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.type == 13)
			{
				GameCanvas.panel.isAccept = true;
			}
			Service.gI().giaodich(7, -1, -1, -1);
		}
		state = TradeState.WaitTradeClose;
		stateStartMs = now;
		lastStatus = "Đã xác nhận GD, chờ hoàn tất...";
	}

	private void HandleWaitTradeClose(long now)
	{
		long timeoutMs = global::System.Math.Max(20000L, (long)delayAcceptMs + 15000L);
		if (now - stateStartMs >= timeoutMs)
		{
			StopAndCancelTrade("Dừng: Phiên GD quá hạn không đóng!");
		}
	}

	private void HandleWaitCooldownNextTrade(long now)
	{
		long elapsed = now - stateStartMs;
		if (elapsed < delayBetweenTradesMs)
		{
			return;
		}
		if (!bagUpdatedSinceTradeClose && elapsed < global::System.Math.Max((long)delayBetweenTradesMs, 200L))
		{
			return;
		}
		if (now < nextActionAllowedMs)
		{
			return;
		}
		if (targetLoops > 0 && completedTrades >= targetLoops)
		{
			StopAuto("Hoàn tất đủ " + completedTrades + " lượt GD!");
			return;
		}
		int bagQty = GetBagItemCount(targetItemId);
		if (bagQty < qtyPerTrade)
		{
			StopAuto("Dừng: Túi (" + bagQty + ") < SL chọn (" + qtyPerTrade + ")");
			return;
		}
		state = TradeState.SenderSendInvite;
		stateStartMs = now;
	}

	public bool OnReceiveInvite(int playerID)
	{
		if (!isRunning || role != TradeRole.Receiver)
		{
			return false;
		}
		if (playerID != targetCharId)
		{
			return false;
		}
		if (inTradeSession)
		{
			return true;
		}
		if (state != TradeState.ReceiverWaitingInvite && state != TradeState.WaitOpenSession)
		{
			return false;
		}
		Service.gI().giaodich(1, playerID, -1, -1);
		state = TradeState.WaitOpenSession;
		stateStartMs = mSystem.currentTimeMillis();
		lastStatus = "Đã đồng ý lời mời từ " + targetCharName;
		return true;
	}

	public bool OnTradeOpened(int partnerId)
	{
		if (!isRunning || partnerId != targetCharId)
		{
			return false;
		}
		long now = mSystem.currentTimeMillis();
		inTradeSession = true;
		sessionPartnerId = partnerId;
		isSelfLocked = false;
		isFriendLocked = false;
		sentAccept = false;
		bagUpdatedSinceTradeClose = false;
		pendingPanelClose = false;

		if (role == TradeRole.Sender)
		{
			state = TradeState.PutItems;
			stateStartMs = now;
			lastStatus = "Đã mở GD, chuẩn bị đặt vật phẩm...";
		}
		else
		{
			state = TradeState.WaitLockDelay;
			stateStartMs = now;
			lastStatus = "Đã mở GD, chờ khóa (" + delayLockMs + "ms)...";
		}
		return hidePanelDuringTrade;
	}

	public void OnMyItemRemoved(sbyte indexUI)
	{
		if (!isRunning || !inTradeSession || sessionPartnerId != targetCharId)
		{
			return;
		}
		StopAndCancelTrade("Dừng: Server từ chối vật phẩm GD (ô " + indexUI + ")!");
	}

	public void OnFriendLocked()
	{
		if (!isRunning || !inTradeSession || sessionPartnerId != targetCharId)
		{
			return;
		}
		isFriendLocked = true;
		if (state == TradeState.WaitPartnerLock)
		{
			state = TradeState.WaitAcceptDelay;
			stateStartMs = mSystem.currentTimeMillis();
			lastStatus = "Đối tác đã khóa, chờ xác nhận (" + delayAcceptMs + "ms)...";
		}
	}

	public bool OnTradeClosed()
	{
		if (!isRunning || !inTradeSession)
		{
			return false;
		}
		bool suppressUi = ShouldSuppressPanelUi();
		bool wasSuccessFlow = isSelfLocked && isFriendLocked && sentAccept;
		if (!hidePanelDuringTrade && GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 13)
		{
			GameCanvas.panel.isAccept = true;
			pendingPanelClose = true;
		}
		ResetSessionFlags();
		long now = mSystem.currentTimeMillis();

		if (wasSuccessFlow)
		{
			completedTrades++;
			inviteRetryCount = 0;
			if (targetLoops > 0 && completedTrades >= targetLoops)
			{
				StopAuto("Hoàn tất đủ " + completedTrades + " lượt GD!");
				return suppressUi;
			}
			if (role == TradeRole.Sender)
			{
				state = TradeState.WaitCooldownNextTrade;
				stateStartMs = now;
				lastStatus = "Xong lượt " + completedTrades + ", chờ " + delayBetweenTradesMs + "ms...";
			}
			else
			{
				state = TradeState.ReceiverWaitingInvite;
				stateStartMs = now;
				lastStatus = "Đã nhận xong lượt " + completedTrades + ", chờ mời tiếp...";
			}
		}
		else
		{
			inviteRetryCount++;
			if (role == TradeRole.Sender)
			{
				if (inviteRetryCount >= MaxAbortRetries)
				{
					StopAuto("Dừng: Phiên GD bị hủy quá nhiều lần!");
					return suppressUi;
				}
				state = TradeState.WaitCooldownNextTrade;
				stateStartMs = now;
				nextActionAllowedMs = global::System.Math.Max(nextActionAllowedMs, now + 500L);
				lastStatus = "Phiên GD bị hủy, thử lại (" + inviteRetryCount + "/" + MaxAbortRetries + ")...";
			}
			else
			{
				state = TradeState.ReceiverWaitingInvite;
				stateStartMs = now;
				lastStatus = "Phiên GD bị hủy, chờ mời lại...";
			}
		}
		return suppressUi;
	}

	public void OnBagUpdated()
	{
		if (isRunning)
		{
			bagUpdatedSinceTradeClose = true;
		}
	}

	public void OnServerInfo(string s)
	{
		if (!isRunning || string.IsNullOrEmpty(s))
		{
			return;
		}
		string low = s.ToLowerInvariant();
		if (low.Contains("không thể giao dịch") || low.Contains("khong the giao dich")
			|| low.Contains("hành trang đầy") || low.Contains("hanh trang day")
			|| low.Contains("rương đồ đã đầy") || low.Contains("túi đã đầy") || low.Contains("tui da day")
			|| low.Contains("không đủ chỗ") || low.Contains("khong du cho")
			|| low.Contains("đạt giới hạn") || low.Contains("dat gioi han") || low.Contains("vượt quá giới hạn"))
		{
			StopAndCancelTrade("Server: " + s);
			return;
		}
		if (role == TradeRole.Sender && (state == TradeState.WaitOpenSession || state == TradeState.SenderSendInvite || state == TradeState.WaitCooldownNextTrade))
		{
			if (low.Contains("vui lòng chờ") || (low.Contains("chờ ") && low.Contains("giây")))
			{
				int secs = ExtractFirstPositiveInt(s, 2);
				if (secs > 60)
				{
					secs = 60;
				}
				long now = mSystem.currentTimeMillis();
				nextActionAllowedMs = global::System.Math.Max(nextActionAllowedMs, now + secs * 1000L + 200L);
				state = TradeState.SenderSendInvite;
				stateStartMs = now;
				lastStatus = "Chờ cooldown (" + secs + "s): " + s;
			}
		}
	}

	public void OnDisconnectOrReset()
	{
		isRunning = false;
		state = TradeState.Idle;
		ResetSessionFlags();
		pendingPanelClose = false;
		lastStatus = "Đã ngắt kết nối";
	}

	private static int ExtractFirstPositiveInt(string s, int fallback)
	{
		int val = 0;
		bool found = false;
		for (int i = 0; i < s.Length; i++)
		{
			char ch = s[i];
			if (ch >= '0' && ch <= '9')
			{
				found = true;
				val = val * 10 + (ch - '0');
				if (val > 1000)
				{
					return 1000;
				}
			}
			else if (found)
			{
				break;
			}
		}
		return found && val > 0 ? val : fallback;
	}

	private static void Notify(string msg)
	{
		if (GameScr.info1 != null && !string.IsNullOrEmpty(msg))
		{
			GameScr.info1.addInfo("[AutoGD] " + msg, 0);
		}
	}

	public void perform(int idAction, object p)
	{
		if (idAction == 100)
		{
			OpenWindow();
		}
	}

	private void OpenInputField(string title, string context)
	{
		isWaitingInput = true;
		inputContext = context;
		ChatTextField tf = ChatTextField.gI();
		tf.strChat = title;
		tf.tfChat.name = title;
		tf.tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
		tf.startChat2(this, string.Empty);
	}

	public void onChatFromMe(string text, string to)
	{
		AutoItem.ResetTF(ChatTextField.gI());
		if (!isWaitingInput)
		{
			return;
		}
		string ctx = inputContext;
		isWaitingInput = false;
		inputContext = string.Empty;

		string trimmed = (text ?? string.Empty).Trim();
		if (!int.TryParse(trimmed, out int val))
		{
			Notify("Giá trị nhập không hợp lệ!");
			return;
		}
		switch (ctx)
		{
		case "qty":
			if (val <= 0)
			{
				Notify("SL mỗi lượt phải >= 1!");
				return;
			}
			qtyPerTrade = val;
			break;
		case "loops":
			if (val < 0)
			{
				Notify("Số lượt phải >= 0 (0 = vô hạn)!");
				return;
			}
			targetLoops = val;
			break;
		case "delay_trade":
			delayBetweenTradesMs = val < 0 ? 0 : val;
			break;
		case "delay_lock":
			delayLockMs = val < 0 ? 0 : val;
			break;
		case "delay_accept":
			delayAcceptMs = val < 0 ? 0 : val;
			break;
		case "item_id":
			if (val < 0)
			{
				Notify("ID vật phẩm phải >= 0!");
				return;
			}
			targetItemId = val;
			targetItemName = ResolveItemNameById(val);
			viewMode = 0;
			break;
		}
	}

	public void onCancelChat()
	{
		AutoItem.ResetTF(ChatTextField.gI());
		isWaitingInput = false;
		inputContext = string.Empty;
	}

	private static string ResolveItemNameById(int itemId)
	{
		object tplObj = ItemTemplates.itemTemplates != null ? ItemTemplates.itemTemplates.get((short)itemId) : null;
		ItemTemplate tpl = tplObj as ItemTemplate;
		if (tpl != null && !string.IsNullOrEmpty(tpl.name))
		{
			return tpl.name;
		}
		Char myChar = Char.myCharz();
		if (myChar != null && myChar.arrItemBag != null)
		{
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it = myChar.arrItemBag[i];
				if (it != null && it.template != null && it.template.id == itemId && !string.IsNullOrEmpty(it.template.name))
				{
					return it.template.name;
				}
			}
		}
		return "Item ID " + itemId;
	}

	private void UpdateTouch()
	{
		if (ChatTextField.gI().isShow)
		{
			return;
		}
		EnsureWindowInBounds();
		if (isDragging)
		{
			if (GameCanvas.isPointerDown)
			{
				winX = GameCanvas.px - dragOffsetX;
				winY = GameCanvas.py - dragOffsetY;
				EnsureWindowInBounds();
				return;
			}
			isDragging = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}
		if (GameCanvas.isPointerDown && !GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(winX, winY, WinW - 24, 22))
		{
			isDragging = true;
			dragOffsetX = GameCanvas.px - winX;
			dragOffsetY = GameCanvas.py - winY;
			return;
		}
		if (!GameCanvas.isPointerJustRelease)
		{
			return;
		}
		if (!GameCanvas.isPointerHoldIn(winX, winY, WinW, WinH))
		{
			return;
		}
		int px = GameCanvas.px;
		int py = GameCanvas.py;

		if (InRect(px, py, winX + WinW - 22, winY + 3, 18, 16))
		{
			isShowWindow = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		if (viewMode == 0)
		{
			HandleMainViewClick(px, py);
		}
		else if (viewMode == 1)
		{
			HandlePartnerPickerClick(px, py);
		}
		else if (viewMode == 2)
		{
			HandleItemPickerClick(px, py);
		}
		GameCanvas.clearAllPointerEvent();
	}

	private void HandleMainViewClick(int px, int py)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 26;

		// Role buttons
		int halfW = (w - 6) / 2;
		if (InRect(px, py, x, y, halfW, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi vai trò!");
				return;
			}
			role = TradeRole.Sender;
			return;
		}
		if (InRect(px, py, x + halfW + 6, y, halfW, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi vai trò!");
				return;
			}
			role = TradeRole.Receiver;
			return;
		}
		y += 22;

		// Partner row: [Pick List] + [Target]
		int targetBtnW = 54;
		if (InRect(px, py, x, y, w - targetBtnW - 4, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi đối tác!");
				return;
			}
			RefreshPartnerList();
			listPage = 0;
			viewMode = 1;
			return;
		}
		if (InRect(px, py, x + w - targetBtnW, y, targetBtnW, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi đối tác!");
				return;
			}
			PickFocusPartner();
			return;
		}
		y += 22;

		// Item row: [Pick Bag] + [ID]
		int idBtnW = 36;
		if (InRect(px, py, x, y, w - idBtnW - 4, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi vật phẩm!");
				return;
			}
			RefreshBagItemList();
			listPage = 0;
			viewMode = 2;
			return;
		}
		if (InRect(px, py, x + w - idBtnW, y, idBtnW, 18))
		{
			if (isRunning)
			{
				Notify("Hãy dừng Auto (F6) trước khi đổi vật phẩm!");
				return;
			}
			OpenInputField("Nhập ID vật phẩm GD", "item_id");
			return;
		}
		y += 32; // +18 btn + 14 bag qty line

		// Qty & Loops row
		if (InRect(px, py, x, y, halfW, 18))
		{
			OpenInputField("Số lượng mỗi lượt GD", "qty");
			return;
		}
		if (InRect(px, py, x + halfW + 6, y, halfW, 18))
		{
			OpenInputField("Số lượt GD (0 = vô hạn)", "loops");
			return;
		}
		y += 22;

		// Delays row 1: Delay between trades | Delay lock
		if (InRect(px, py, x, y, halfW, 18))
		{
			OpenInputField("Delay giữa 2 lần GD (ms)", "delay_trade");
			return;
		}
		if (InRect(px, py, x + halfW + 6, y, halfW, 18))
		{
			OpenInputField("Delay khóa GD (ms)", "delay_lock");
			return;
		}
		y += 22;

		// Delays row 2: Delay accept | Checkbox hide panel
		if (InRect(px, py, x, y, halfW, 18))
		{
			OpenInputField("Delay xác nhận GD (ms)", "delay_accept");
			return;
		}
		if (InRect(px, py, x + halfW + 6, y, halfW, 18))
		{
			hidePanelDuringTrade = !hidePanelDuringTrade;
			return;
		}
		y += 24;

		// Status area height = 34
		y += 36;

		// Bottom action buttons: [Start/Stop] + [Đóng]
		int startW = w - 60;
		if (InRect(px, py, x, y, startW, 20))
		{
			if (isRunning)
			{
				StopAuto("Đã dừng bởi người dùng");
			}
			else
			{
				StartAuto();
			}
			return;
		}
		if (InRect(px, py, x + startW + 4, y, 56, 20))
		{
			isShowWindow = false;
		}
	}

	private void HandlePartnerPickerClick(int px, int py)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 28;

		// [Làm mới] + [Lấy Target]
		int halfW = (w - 4) / 2;
		if (InRect(px, py, x, y, halfW, 18))
		{
			RefreshPartnerList();
			listPage = 0;
			return;
		}
		if (InRect(px, py, x + halfW + 4, y, halfW, 18))
		{
			if (PickFocusPartner())
			{
				viewMode = 0;
			}
			return;
		}
		y += 22;

		int startIdx = listPage * ItemsPerPage;
		for (int i = 0; i < ItemsPerPage; i++)
		{
			int idx = startIdx + i;
			if (idx >= cachedPartners.Count)
			{
				break;
			}
			if (InRect(px, py, x, y + i * 24, w, 21))
			{
				targetCharId = cachedPartners[idx].CharId;
				targetCharName = cachedPartners[idx].Name;
				viewMode = 0;
				return;
			}
		}

		int navY = winY + WinH - 26;
		if (InRect(px, py, x, navY, 44, 18) && listPage > 0)
		{
			listPage--;
			return;
		}
		if (InRect(px, py, x + 48, navY, 44, 18) && (listPage + 1) * ItemsPerPage < cachedPartners.Count)
		{
			listPage++;
			return;
		}
		if (InRect(px, py, x + w - 70, navY, 70, 18))
		{
			viewMode = 0;
		}
	}

	private void HandleItemPickerClick(int px, int py)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 28;

		// [Làm mới] + [Nhập ID]
		int halfW = (w - 4) / 2;
		if (InRect(px, py, x, y, halfW, 18))
		{
			RefreshBagItemList();
			listPage = 0;
			return;
		}
		if (InRect(px, py, x + halfW + 4, y, halfW, 18))
		{
			OpenInputField("Nhập ID vật phẩm GD", "item_id");
			return;
		}
		y += 22;

		int startIdx = listPage * ItemsPerPage;
		for (int i = 0; i < ItemsPerPage; i++)
		{
			int idx = startIdx + i;
			if (idx >= cachedBagItems.Count)
			{
				break;
			}
			if (InRect(px, py, x, y + i * 24, w, 21))
			{
				targetItemId = cachedBagItems[idx].ItemId;
				targetItemName = cachedBagItems[idx].Name;
				viewMode = 0;
				return;
			}
		}

		int navY = winY + WinH - 26;
		if (InRect(px, py, x, navY, 44, 18) && listPage > 0)
		{
			listPage--;
			return;
		}
		if (InRect(px, py, x + 48, navY, 44, 18) && (listPage + 1) * ItemsPerPage < cachedBagItems.Count)
		{
			listPage++;
			return;
		}
		if (InRect(px, py, x + w - 70, navY, 70, 18))
		{
			viewMode = 0;
		}
	}

	private static bool InRect(int px, int py, int x, int y, int w, int h)
	{
		return px >= x && px <= x + w && py >= y && py <= y + h;
	}

	public void Paint(mGraphics g)
	{
		if (isRunning && !isShowWindow)
		{
			g.FillRect(6, 86, 178, 22, 0x0D1520, 0.78f);
			g.setColor(0x3B82F6);
			g.drawRect(6, 86, 178, 22);
			string roleTag = role == TradeRole.Sender ? "Gửi" : "Nhận";
			mFont.tahoma_7_yellow.drawString(g, "AutoGD(" + roleTag + "): " + completedTrades + " lượt [F6 dừng]", 11, 91, mFont.LEFT);
		}

		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowInBounds();

		// Window container
		g.FillRect(winX, winY, WinW, WinH, 0x111827, 0.92f);
		g.setColor(0x475569);
		g.drawRect(winX, winY, WinW, WinH);

		// Header
		g.setColor(0x1E293B);
		g.fillRect(winX + 1, winY + 1, WinW - 2, 20);
		mFont.tahoma_7b_yellow.drawString(g, "AUTO GIAO DỊCH (F6: Dừng)", winX + 8, winY + 4, mFont.LEFT);
		DrawButton(g, winX + WinW - 22, winY + 3, 18, 16, "X", 0x991B1B, mFont.tahoma_7b_white);

		if (viewMode == 0)
		{
			PaintMainView(g);
		}
		else if (viewMode == 1)
		{
			PaintPartnerPicker(g);
		}
		else if (viewMode == 2)
		{
			PaintItemPicker(g);
		}
	}

	private void PaintMainView(mGraphics g)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 26;
		int halfW = (w - 6) / 2;

		// Role selector
		DrawButton(g, x, y, halfW, 18, "Vai trò: GỬI", role == TradeRole.Sender ? 0x15803D : 0x334155, mFont.tahoma_7_white);
		DrawButton(g, x + halfW + 6, y, halfW, 18, "Vai trò: NHẬN", role == TradeRole.Receiver ? 0x15803D : 0x334155, mFont.tahoma_7_white);
		y += 22;

		// Partner row
		string partnerLabel = targetCharId > 0
			? ("Đối tác: " + Truncate(targetCharName, 14) + " [" + targetCharId + "]")
			: "Đối tác: Chưa chọn";
		DrawButton(g, x, y, w - 58, 18, partnerLabel, 0x1E3A5F, mFont.tahoma_7_white);
		DrawButton(g, x + w - 54, y, 54, 18, "Target", 0x0F766E, mFont.tahoma_7_white);
		y += 22;

		// Item row + bag quantity readout
		string itemLabel = targetItemId >= 0
			? ("VP: " + Truncate(targetItemName, 14) + " (" + targetItemId + ")")
			: (role == TradeRole.Receiver ? "VP: (Bên nhận không cần)" : "VP: Chọn vật phẩm");
		DrawButton(g, x, y, w - 40, 18, itemLabel, role == TradeRole.Receiver ? 0x1F2937 : 0x1E3A5F, mFont.tahoma_7_white);
		DrawButton(g, x + w - 36, y, 36, 18, "ID", 0x475569, mFont.tahoma_7_white);
		y += 20;

		int bagQty = GetBagItemCount(targetItemId);
		if (role == TradeRole.Sender)
		{
			mFont bagFont = (targetItemId >= 0 && bagQty >= qtyPerTrade) ? mFont.tahoma_7_green : mFont.tahoma_7_red;
			bagFont.drawString(g, "Trong rương: " + bagQty + " (Dừng nếu < " + qtyPerTrade + ")", x + 2, y, mFont.LEFT);
		}
		else
		{
			mFont.tahoma_7_grey.drawString(g, "Chỉ tự đồng ý GD từ: " + (targetCharId > 0 ? targetCharName : "Chưa chọn"), x + 2, y, mFont.LEFT);
		}
		y += 12;

		// Qty & Loops
		string loopsStr = targetLoops <= 0 ? "Vô hạn" : targetLoops.ToString();
		DrawButton(g, x, y, halfW, 18, "SL/lượt: " + qtyPerTrade, 0x334155, mFont.tahoma_7_white);
		DrawButton(g, x + halfW + 6, y, halfW, 18, "Số lượt: " + loopsStr, 0x334155, mFont.tahoma_7_white);
		y += 22;

		// Delays
		DrawButton(g, x, y, halfW, 18, "Delay GD: " + delayBetweenTradesMs + "ms", 0x334155, mFont.tahoma_7_white);
		DrawButton(g, x + halfW + 6, y, halfW, 18, "Delay khóa: " + delayLockMs + "ms", 0x334155, mFont.tahoma_7_white);
		y += 22;

		DrawButton(g, x, y, halfW, 18, "Delay XN: " + delayAcceptMs + "ms", 0x334155, mFont.tahoma_7_white);
		DrawCheckbox(g, x + halfW + 6, y, halfW, 18, "Ẩn Panel", hidePanelDuringTrade);
		y += 24;

		// Status panel
		g.setColor(0x0F172A);
		g.fillRect(x, y, w, 32);
		g.setColor(0x334155);
		g.drawRect(x, y, w, 32);
		mFont statusFont = isRunning ? mFont.tahoma_7_green : mFont.tahoma_7_yellow;
		statusFont.drawString(g, "TT: " + Truncate(lastStatus, 34), x + 4, y + 3, mFont.LEFT);
		mFont.tahoma_7_white.drawString(g, "Đã GD: " + completedTrades + (targetLoops > 0 ? ("/" + targetLoops) : "") + " lượt", x + 4, y + 17, mFont.LEFT);
		y += 36;

		// Action row
		int startW = w - 60;
		if (isRunning)
		{
			DrawButton(g, x, y, startW, 20, "DỪNG AUTO (F6)", 0xB91C1C, mFont.tahoma_7b_white);
		}
		else
		{
			DrawButton(g, x, y, startW, 20, "BẮT ĐẦU AUTO", 0x15803D, mFont.tahoma_7b_white);
		}
		DrawButton(g, x + startW + 4, y, 56, 20, "Đóng", 0x475569, mFont.tahoma_7_white);
	}

	private void PaintPartnerPicker(mGraphics g)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 28;
		int halfW = (w - 4) / 2;

		DrawButton(g, x, y, halfW, 18, "Làm mới (" + cachedPartners.Count + ")", 0x334155, mFont.tahoma_7_white);
		DrawButton(g, x + halfW + 4, y, halfW, 18, "Lấy Target", 0x0F766E, mFont.tahoma_7_white);
		y += 22;

		if (cachedPartners.Count == 0)
		{
			mFont.tahoma_7_grey.drawString(g, "Không có người chơi khác trong map.", x + 4, y + 8, mFont.LEFT);
		}
		else
		{
			int startIdx = listPage * ItemsPerPage;
			for (int i = 0; i < ItemsPerPage; i++)
			{
				int idx = startIdx + i;
				if (idx >= cachedPartners.Count)
				{
					break;
				}
				PartnerSummary p = cachedPartners[idx];
				bool selected = p.CharId == targetCharId;
				DrawButton(g, x, y + i * 24, w, 21, p.Name + " (ID: " + p.CharId + ")", selected ? 0x15803D : 0x1E293B, mFont.tahoma_7_white);
			}
		}

		PaintPickerNav(g, x, w, cachedPartners.Count);
	}

	private void PaintItemPicker(mGraphics g)
	{
		int x = winX + 8;
		int w = WinW - 16;
		int y = winY + 28;
		int halfW = (w - 4) / 2;

		DrawButton(g, x, y, halfW, 18, "Làm mới (" + cachedBagItems.Count + ")", 0x334155, mFont.tahoma_7_white);
		DrawButton(g, x + halfW + 4, y, halfW, 18, "Nhập ID", 0x1E3A5F, mFont.tahoma_7_white);
		y += 22;

		if (cachedBagItems.Count == 0)
		{
			mFont.tahoma_7_grey.drawString(g, "Rương đồ trống.", x + 4, y + 8, mFont.LEFT);
		}
		else
		{
			int startIdx = listPage * ItemsPerPage;
			for (int i = 0; i < ItemsPerPage; i++)
			{
				int idx = startIdx + i;
				if (idx >= cachedBagItems.Count)
				{
					break;
				}
				BagItemSummary b = cachedBagItems[idx];
				bool selected = b.ItemId == targetItemId;
				string label = Truncate(b.Name, 16) + " [" + b.ItemId + "] x" + b.TotalQuantity;
				DrawButton(g, x, y + i * 24, w, 21, label, selected ? 0x15803D : 0x1E293B, mFont.tahoma_7_white);
			}
		}

		PaintPickerNav(g, x, w, cachedBagItems.Count);
	}

	private void PaintPickerNav(mGraphics g, int x, int w, int totalCount)
	{
		int navY = winY + WinH - 26;
		int totalPages = global::System.Math.Max(1, (totalCount + ItemsPerPage - 1) / ItemsPerPage);
		DrawButton(g, x, navY, 44, 18, "<", listPage > 0 ? 0x334155 : 0x1E293B, mFont.tahoma_7_white);
		DrawButton(g, x + 48, navY, 44, 18, ">", (listPage + 1) < totalPages ? 0x334155 : 0x1E293B, mFont.tahoma_7_white);
		mFont.tahoma_7_grey.drawString(g, (listPage + 1) + "/" + totalPages, x + 100, navY + 3, mFont.LEFT);
		DrawButton(g, x + w - 70, navY, 70, 18, "Quay lại", 0x475569, mFont.tahoma_7_white);
	}

	private static void DrawButton(mGraphics g, int x, int y, int w, int h, string text, int bgColor, mFont font)
	{
		g.setColor(bgColor);
		g.fillRect(x, y, w, h);
		g.setColor(0x64748B);
		g.drawRect(x, y, w, h);
		font.drawString(g, text, x + w / 2, y + (h - 10) / 2, mFont.CENTER);
	}

	private static void DrawCheckbox(mGraphics g, int x, int y, int w, int h, string label, bool checkedState)
	{
		g.setColor(0x1E293B);
		g.fillRect(x, y, w, h);
		g.setColor(0x64748B);
		g.drawRect(x, y, w, h);
		int boxX = x + 4;
		int boxY = y + (h - 10) / 2;
		g.setColor(checkedState ? 0x16A34A : 0x334155);
		g.fillRect(boxX, boxY, 10, 10);
		g.setColor(0xE2E8F0);
		g.drawRect(boxX, boxY, 10, 10);
		if (checkedState)
		{
			mFont.tahoma_7b_white.drawString(g, "v", boxX + 5, boxY - 1, mFont.CENTER);
		}
		mFont.tahoma_7_white.drawString(g, label, boxX + 14, y + (h - 10) / 2, mFont.LEFT);
	}

	private static string Truncate(string s, int maxLen)
	{
		if (string.IsNullOrEmpty(s) || s.Length <= maxLen)
		{
			return s ?? string.Empty;
		}
		if (maxLen <= 2)
		{
			return s.Substring(0, maxLen);
		}
		return s.Substring(0, maxLen - 2) + "..";
	}
}

