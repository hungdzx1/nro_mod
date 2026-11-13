using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Mod;

public class AutoDungNhanh : IChatable, IActionListener
{
	public class FastUseEntry
	{
		public int itemId;

		public string itemName;

		public bool enabled = true;
	}

	private static AutoDungNhanh _instance;

	public bool isShowWindow;

	public bool isRunning;

	public int delayMs = 200;

	public bool stopOnBagFull = true;

	public bool autoRemoveWhenEmpty = true;

	public bool continuousRun;

	public long lastUseTimeMs;

	public int currentPage;

	public readonly List<FastUseEntry> items = new List<FastUseEntry>();

	private int roundRobinIndex;

	private int editFieldMode; // 1: delayMs

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 224;

	private readonly int winH = 264;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	private const int RowsPerPage = 4;

	public static AutoDungNhanh gI()
	{
		if (_instance == null)
		{
			_instance = new AutoDungNhanh();
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

	public bool IsItemAdded(int itemId)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].itemId == itemId)
			{
				return true;
			}
		}
		return false;
	}

	public void OpenQuickMenu()
	{
		MyVector myVector = new MyVector();
		myVector.addElement(new Command("Auto nhanh", this, 100, null));
		myVector.addElement(new Command(isRunning ? "Tắt auto\nnhanh" : "Bật auto\nnhanh", this, 101, null));
		GameCanvas.menu.startAt(myVector, 4);
	}

	public void OpenWindow()
	{
		EnsureWindowPosition();
		isShowWindow = true;
		CloseOtherPanels();
	}

	public void ToggleWindow()
	{
		EnsureWindowPosition();
		isShowWindow = !isShowWindow;
		if (isShowWindow)
		{
			CloseOtherPanels();
		}
	}

	private static void CloseOtherPanels()
	{
		AutoGop.gI().isShowWindow = false;
		AutoMuaNhieu.gI().isShowWindow = false;
		NpcMenuHelper.gI().isShowWindow = false;
		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.hideNow();
		}
	}

	public void SetDelayMs(int ms)
	{
		delayMs = System.Math.Max(50, System.Math.Min(60000, ms));
	}

	public void ToggleItemFromBag(Item item)
	{
		if (item == null || item.template == null)
		{
			return;
		}
		int itemId = item.template.id;
		string itemName = string.IsNullOrEmpty(item.template.name) ? ("ID " + itemId) : item.template.name;

		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].itemId == itemId)
			{
				items.RemoveAt(i);
				ClampPaging();
				if (items.Count == 0 || !HasEnabledItem())
				{
					isRunning = false;
				}
				GameScr.info1.addInfo("Đã tắt Auto Dùng Nhanh: " + itemName, 0);
				return;
			}
		}

		items.Add(new FastUseEntry
		{
			itemId = itemId,
			itemName = itemName,
			enabled = true
		});
		isRunning = true;
		lastUseTimeMs = 0L;
		GameScr.info1.addInfo("Auto Dùng Nhanh: " + itemName + " (" + delayMs + "ms) [Z -> Auto nhanh]", 0);
	}

	public void RemoveItemAt(int index)
	{
		if (index < 0 || index >= items.Count)
		{
			return;
		}
		string name = items[index].itemName;
		items.RemoveAt(index);
		ClampPaging();
		if (items.Count == 0 || !HasEnabledItem())
		{
			isRunning = false;
		}
		GameScr.info1.addInfo("Đã xóa khỏi Auto Nhanh: " + name, 0);
	}

	public void ClearAllItems()
	{
		items.Clear();
		currentPage = 0;
		roundRobinIndex = 0;
		StopAll("Đã xóa danh sách Auto Dùng Nhanh", disableContinuous: false);
	}

	public void ToggleStartPause()
	{
		if (isRunning)
		{
			StopAll("Đã dừng Auto Dùng Nhanh");
			return;
		}
		if (!HasEnabledItem())
		{
			GameScr.info1.addInfo("Chưa có item nào được bật trong Auto Dùng Nhanh", 0);
			return;
		}
		isRunning = true;
		lastUseTimeMs = 0L;
		GameScr.info1.addInfo("▶ Bắt đầu Auto Dùng Nhanh (" + delayMs + "ms)", 0);
	}

	public void ToggleContinuousRun()
	{
		continuousRun = !continuousRun;
		if (continuousRun)
		{
			autoRemoveWhenEmpty = false;
			if (HasEnabledItem())
			{
				isRunning = true;
				lastUseTimeMs = 0L;
				GameScr.info1.addInfo("Đã bật Chạy liên tục (tự tắt Xóa khi hết)", 0);
			}
			else
			{
				GameScr.info1.addInfo("Đã bật Chạy liên tục (chờ có >= 1 item)", 0);
			}
		}
		else
		{
			GameScr.info1.addInfo("Đã tắt Chạy liên tục", 0);
		}
	}

	public void StopAll(string reason = null, bool disableContinuous = true)
	{
		isRunning = false;
		if (disableContinuous)
		{
			continuousRun = false;
		}
		if (!string.IsNullOrEmpty(reason))
		{
			GameScr.info1.addInfo(reason, 0);
		}
	}

	public void OnDisconnectOrReset()
	{
		isRunning = false;
		continuousRun = false;
		isShowWindow = false;
		isDragging = false;
		editFieldMode = 0;
	}

	public void OnServerInfo(string s)
	{
		if (!isRunning || !stopOnBagFull || string.IsNullOrEmpty(s))
		{
			return;
		}
		string lower = s.ToLower();
		if (lower.Contains("hành trang đã đầy") || lower.Contains("hành trang đầy") || lower.Contains("chỗ trống"))
		{
			StopAll(continuousRun ? ("Tạm dừng (đã tắt Chạy liên tục): " + s) : ("Tạm dừng Auto Nhanh: " + s));
		}
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
		case 1:
			ToggleItemFromBag(p as Item);
			break;
		case 100:
			ToggleWindow();
			break;
		case 101:
			ToggleStartPause();
			break;
		}
	}

	public static int GetBagItemCount(int itemId)
	{
		if (Char.myCharz() == null || Char.myCharz().arrItemBag == null)
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

	private bool HasEnabledItem()
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].enabled)
			{
				return true;
			}
		}
		return false;
	}

	private int GetTotalPages()
	{
		if (items.Count <= RowsPerPage)
		{
			return 1;
		}
		return (items.Count + RowsPerPage - 1) / RowsPerPage;
	}

	private void ClampPaging()
	{
		int totalPages = GetTotalPages();
		if (currentPage >= totalPages)
		{
			currentPage = totalPages - 1;
		}
		if (currentPage < 0)
		{
			currentPage = 0;
		}
		if (items.Count == 0)
		{
			roundRobinIndex = 0;
		}
		else if (roundRobinIndex >= items.Count)
		{
			roundRobinIndex %= items.Count;
		}
	}

	public void Update()
	{
		if (isShowWindow)
		{
			UpdateTouch();
		}

		if (continuousRun && !isRunning && HasEnabledItem())
		{
			isRunning = true;
			lastUseTimeMs = 0L;
		}

		if (!isRunning || items.Count == 0)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();
		if (now - lastUseTimeMs < delayMs)
		{
			return;
		}

		int checkedCount = 0;
		while (items.Count > 0 && checkedCount < items.Count)
		{
			if (roundRobinIndex >= items.Count)
			{
				roundRobinIndex = 0;
			}
			FastUseEntry entry = items[roundRobinIndex];
			if (!entry.enabled)
			{
				roundRobinIndex = (roundRobinIndex + 1) % items.Count;
				checkedCount++;
				continue;
			}

			int bagIndexUI = FindBagItemIndexUI(entry.itemId);
			if (bagIndexUI >= 0)
			{
				Service.gI().useItem(0, 1, (sbyte)bagIndexUI, -1);
				lastUseTimeMs = now;
				roundRobinIndex = (roundRobinIndex + 1) % items.Count;
				return;
			}

			if (autoRemoveWhenEmpty)
			{
				string emptyName = entry.itemName;
				items.RemoveAt(roundRobinIndex);
				ClampPaging();
				GameScr.info1.addInfo("Đã hết " + emptyName + " -> xóa khỏi Auto Nhanh", 0);
			}
			else
			{
				roundRobinIndex = (roundRobinIndex + 1) % items.Count;
				checkedCount++;
			}
		}

		if (items.Count == 0 || !HasEnabledItem())
		{
			StopAll("Đã dùng hết vật phẩm trong Auto Dùng Nhanh", disableContinuous: false);
		}
		else if (!continuousRun)
		{
			StopAll("Tạm dừng: Không còn vật phẩm trong hành trang", disableContinuous: false);
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
		if (editFieldMode == 1)
		{
			if (!string.IsNullOrEmpty(text) && int.TryParse(text.Trim(), out var val))
			{
				SetDelayMs(val);
				GameScr.info1.addInfo("Đã đặt Delay Auto Nhanh: " + delayMs + " ms", 0);
			}
			else
			{
				GameScr.info1.addInfo("Vui lòng nhập số ms hợp lệ (>= 50)!", 0);
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

		// Row 1: Delay input box
		int delayRowY = winY + 34;
		int delayBoxX = winX + winW - 70;
		if (GameCanvas.isPointerHoldIn(delayBoxX, delayRowY, 58, 16))
		{
			GameCanvas.clearAllPointerEvent();
			OpenInputField(1, "Delay Auto Dùng Nhanh (ms, >= 50)", delayMs.ToString());
			return;
		}

		// Row 2: Option checkboxes
		int optRowY = winY + 53;
		if (GameCanvas.isPointerHoldIn(winX + 10, optRowY, 102, 15))
		{
			stopOnBagFull = !stopOnBagFull;
			GameCanvas.clearAllPointerEvent();
			return;
		}
		if (GameCanvas.isPointerHoldIn(winX + 114, optRowY, 100, 15))
		{
			autoRemoveWhenEmpty = !autoRemoveWhenEmpty;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Row 2b: Chạy liên tục
		int optRow2Y = winY + 69;
		if (GameCanvas.isPointerHoldIn(winX + 10, optRow2Y, 150, 15))
		{
			GameCanvas.clearAllPointerEvent();
			ToggleContinuousRun();
			return;
		}

		// Row 3: Pagination if > RowsPerPage
		int headerRowY = winY + 87;
		int totalPages = GetTotalPages();
		if (totalPages > 1)
		{
			if (GameCanvas.isPointerHoldIn(winX + winW - 60, headerRowY - 1, 18, 15))
			{
				currentPage = (currentPage - 1 + totalPages) % totalPages;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (GameCanvas.isPointerHoldIn(winX + winW - 28, headerRowY - 1, 18, 15))
			{
				currentPage = (currentPage + 1) % totalPages;
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// Item list rows
		int listY = winY + 102;
		int startIdx = currentPage * RowsPerPage;
		for (int slot = 0; slot < RowsPerPage; slot++)
		{
			int idx = startIdx + slot;
			if (idx >= items.Count)
			{
				break;
			}
			int rowY = listY + 2 + slot * 23;
			// Delete [x] button
			if (GameCanvas.isPointerHoldIn(winX + winW - 31, rowY + 2, 18, 17))
			{
				GameCanvas.clearAllPointerEvent();
				RemoveItemAt(idx);
				return;
			}
			// Toggle enable checkbox / row
			if (GameCanvas.isPointerHoldIn(winX + 10, rowY, winW - 44, 21))
			{
				items[idx].enabled = !items[idx].enabled;
				if (!items[idx].enabled && !HasEnabledItem())
				{
					isRunning = false;
				}
				else if (continuousRun && HasEnabledItem() && !isRunning)
				{
					isRunning = true;
					lastUseTimeMs = 0L;
				}
				GameCanvas.clearAllPointerEvent();
				return;
			}
		}

		// Bottom buttons
		int btnX = winX + 12;
		int btnW = winW - 24;
		int startBtnY = winY + 215;
		int clearBtnY = winY + 238;
		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			ToggleStartPause();
			return;
		}
		if (GameCanvas.isPointerHoldIn(btnX, clearBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			ClearAllItems();
			return;
		}

		if (GameCanvas.isPointerHoldIn(winX, winY, winW, winH))
		{
			GameCanvas.clearAllPointerEvent();
		}
	}

	// === PAINT UI ===
	public void Paint(mGraphics g)
	{
		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowPosition();

		// Khung gỗ vàng Dragon Ball đồng bộ với AutoGop
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

		// 4 Corner highlights
		g.setColor(0xF7CE68);
		g.fillRect(winX + 2, winY + 2, 8, 2);
		g.fillRect(winX + 2, winY + 2, 2, 8);
		g.fillRect(winX + winW - 10, winY + 2, 8, 2);
		g.fillRect(winX + winW - 4, winY + 2, 2, 8);
		g.fillRect(winX + 2, winY + winH - 4, 8, 2);
		g.fillRect(winX + 2, winY + winH - 10, 2, 8);
		g.fillRect(winX + winW - 10, winY + winH - 4, 8, 2);
		g.fillRect(winX + winW - 4, winY + winH - 10, 2, 8);

		int headerCenterX = winX + winW / 2;
		mFont.tahoma_7b_yellow.drawStringBorder(g, "AUTO DÙNG NHANH", headerCenterX, winY + 6, mFont.CENTER, mFont.tahoma_7_grey);

		// Close [X]
		int closeX = winX + winW - 22;
		int closeY = winY + 5;
		g.setColor(0xB71C1C);
		g.fillRect(closeX, closeY, 16, 16, 3);
		g.setColor(0xE53935);
		g.fillRect(closeX + 1, closeY + 1, 14, 14, 2);
		mFont.tahoma_7b_white.drawString(g, "X", closeX + 8, closeY + 2, mFont.CENTER);

		// Row 1: Delay ms
		int delayRowY = winY + 34;
		int delayBoxX = winX + winW - 70;
		mFont.tahoma_7b_white.drawString(g, "Delay mỗi lần dùng:", winX + 12, delayRowY + 2, mFont.LEFT);
		g.setColor(0x231107);
		g.fillRect(delayBoxX, delayRowY, 58, 15, 3);
		g.setColor(0xC58927);
		g.drawRect(delayBoxX, delayRowY, 57, 14);
		mFont.tahoma_7b_green.drawString(g, delayMs + " ms", delayBoxX + 29, delayRowY + 2, mFont.CENTER);

		// Row 2: Checkboxes
		int optRowY = winY + 53;
		DrawCheckbox(g, winX + 12, optRowY, stopOnBagFull, "Dừng đầy rương");
		DrawCheckbox(g, winX + 116, optRowY, autoRemoveWhenEmpty, "Xóa khi hết");

		// Row 2b: Chạy liên tục
		int optRow2Y = winY + 69;
		DrawCheckbox(g, winX + 12, optRow2Y, continuousRun, "Chạy liên tục");

		// Row 3: List header & paging
		int headerRowY = winY + 87;
		mFont.tahoma_7_yellow.drawString(g, "Danh sách item (" + items.Count + "):", winX + 12, headerRowY, mFont.LEFT);
		int totalPages = GetTotalPages();
		if (totalPages > 1)
		{
			g.setColor(0x2B1609);
			g.fillRect(winX + winW - 58, headerRowY - 1, 14, 13, 2);
			g.fillRect(winX + winW - 26, headerRowY - 1, 14, 13, 2);
			mFont.tahoma_7b_white.drawString(g, "<", winX + winW - 51, headerRowY, mFont.CENTER);
			mFont.tahoma_7_white.drawString(g, (currentPage + 1) + "/" + totalPages, winX + winW - 35, headerRowY, mFont.CENTER);
			mFont.tahoma_7b_white.drawString(g, ">", winX + winW - 19, headerRowY, mFont.CENTER);
		}

		// Item list frame
		int listX = winX + 10;
		int listY = winY + 102;
		int listW = winW - 20;
		int listH = 96;
		g.setColor(0x2B1609);
		g.fillRect(listX, listY, listW, listH, 3);
		g.setColor(0x8D5B1C);
		g.drawRect(listX, listY, listW - 1, listH - 1);

		if (items.Count == 0)
		{
			mFont.tahoma_7_white.drawString(g, "Chưa có item nào", headerCenterX, listY + 30, mFont.CENTER);
			mFont.tahoma_7_yellow.drawString(g, "Vào Hành trang -> Auto dùng nhanh", headerCenterX, listY + 46, mFont.CENTER);
		}
		else
		{
			int startIdx = currentPage * RowsPerPage;
			for (int slot = 0; slot < RowsPerPage; slot++)
			{
				int idx = startIdx + slot;
				if (idx >= items.Count)
				{
					break;
				}
				FastUseEntry entry = items[idx];
				int rowY = listY + 3 + slot * 23;
				g.setColor(0x3B1F0E);
				g.fillRect(listX + 3, rowY, listW - 6, 20, 2);

				int bagQty = GetBagItemCount(entry.itemId);
				string shortName = (entry.itemName.Length > 13) ? (entry.itemName.Substring(0, 12) + "..") : entry.itemName;
				string rowLabel = shortName + " [" + entry.itemId + "] x" + bagQty;
				DrawCheckbox(g, listX + 6, rowY + 4, entry.enabled, rowLabel);

				// Red remove button
				int delX = winX + winW - 29;
				int delY = rowY + 2;
				g.setColor(0xB71C1C);
				g.fillRect(delX, delY, 15, 15, 2);
				mFont.tahoma_7b_white.drawString(g, "x", delX + 7, delY + 1, mFont.CENTER);
			}
		}

		// Status
		string statusText = isRunning
			? ("Đang chạy (" + delayMs + " ms)" + (continuousRun ? " [Liên tục]" : ""))
			: ("Trạng thái: Đang dừng (Phím Z)" + (continuousRun ? " [Liên tục]" : ""));
		mFont.tahoma_7_yellow.drawString(g, statusText, headerCenterX, winY + 201, mFont.CENTER);

		// Buttons
		int btnX = winX + 12;
		int btnW = winW - 24;
		int startBtnY = winY + 215;
		int clearBtnY = winY + 238;
		DrawActionButton(g, btnX, startBtnY, btnW, 19, isRunning ? 0xD35400 : 0x2E7D32, isRunning ? 0xE67E22 : 0x43A047, isRunning ? 0xF39C12 : 0x66BB6A);
		mFont.tahoma_7b_white.drawStringBorder(g, isRunning ? "■ DỪNG AUTO DÙNG NHANH" : "▶ BẮT ĐẦU AUTO NHANH", btnX + btnW / 2, startBtnY + 3, mFont.CENTER, mFont.tahoma_7_grey);

		DrawActionButton(g, btnX, clearBtnY, btnW, 19, 0x8E1616, 0xC62828, 0xE53935);
		mFont.tahoma_7b_white.drawStringBorder(g, "XÓA DANH SÁCH ITEM", btnX + btnW / 2, clearBtnY + 3, mFont.CENTER, mFont.tahoma_7_grey);
	}

	private void DrawCheckbox(mGraphics g, int x, int y, bool isChecked, string text)
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
		if (isChecked)
		{
			mFont.tahoma_7_green.drawString(g, text, x + 14, y, mFont.LEFT);
		}
		else
		{
			mFont.tahoma_7_white.drawString(g, text, x + 14, y, mFont.LEFT);
		}
	}

	private void DrawActionButton(mGraphics g, int x, int y, int w, int h, int colorDark, int colorMid, int colorHighlight)
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

