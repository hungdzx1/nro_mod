using System;
using UnityEngine;

namespace Game1.Mod;

public class AutoMuaNhieu : IChatable
{
	private static AutoMuaNhieu _instance;

	public bool isShowWindow;

	public bool isRunning;

	public int targetItemId = -1;

	public string targetItemName = "Chưa chọn";

	public sbyte targetBuyType = 3;

	public int batchQuantity = 1000;

	public int targetBatches = 160;

	public int delayMs = 150;

	public bool hideClientInput = true;

	public bool autoStopOnError = true;

	public int completedBatches;

	public int totalBought;

	public long startTimeMs;

	public long lastStopTimeMs;

	public bool serverMuaNhieuEnabled;

	private int state; // 0: Idle, 1: WaitEnableMuaNhieu, 2: ReadyToSendBuy, 3: WaitingServerPrompt, 4: Cooldown

	private long stateTimestamp;

	private int retryCount;

	private int editFieldMode; // 1: batchQuantity, 2: targetBatches, 3: delayMs

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 216;

	private readonly int winH = 226;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	public static AutoMuaNhieu gI()
	{
		if (_instance == null)
		{
			_instance = new AutoMuaNhieu();
		}
		return _instance;
	}

	private void EnsureWindowPosition()
	{
		if (winX < 0 || winY < 0 || winX > GameCanvas.w - 40 || winY > GameCanvas.h - 40)
		{
			winX = System.Math.Max(4, GameCanvas.w - winW - 54);
			winY = System.Math.Max(6, System.Math.Min(GameCanvas.h - winH - 6, (GameCanvas.h - winH) / 2 + 12));
		}
	}

	public void OnFocusShopItem(Item item)
	{
		if (item == null || item.template == null)
		{
			return;
		}
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
		SetTargetItem(item, buyType);
	}

	public void SetTargetItem(Item item, sbyte buyType)
	{
		if (item != null && item.template != null)
		{
			targetItemId = item.template.id;
			targetItemName = item.template.name;
			targetBuyType = buyType;
		}
	}

	public void OpenWindowWithItem(Item item)
	{
		if (item != null)
		{
			OnFocusShopItem(item);
			GameScr.info1.addInfo("Đã chọn: " + targetItemName + " [ID: " + targetItemId + "]", 0);
		}
		EnsureWindowPosition();
		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.hideNow();
		}
		NpcMenuHelper.gI().isShowWindow = false;
		isShowWindow = true;
	}

	public void ToggleWindow()
	{
		EnsureWindowPosition();
		isShowWindow = !isShowWindow;
		if (isShowWindow)
		{
			NpcMenuHelper.gI().isShowWindow = false;
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
		}
	}

	public void StartAuto()
	{
		if (targetItemId < 0)
		{
			GameScr.info1.addInfo("Hãy mở Shop và bấm chọn 1 vật phẩm trước!", 0);
			return;
		}
		if (batchQuantity <= 0)
		{
			batchQuantity = 1;
		}
		if (batchQuantity > 1000)
		{
			batchQuantity = 1000;
		}
		if (delayMs < 20)
		{
			delayMs = 20;
		}
		completedBatches = 0;
		totalBought = 0;
		retryCount = 0;
		startTimeMs = mSystem.currentTimeMillis();
		isRunning = true;

		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.hideNow();
		}

		if (!serverMuaNhieuEnabled)
		{
			Service.gI().chat("muanhieu");
			serverMuaNhieuEnabled = true;
			state = 1;
			stateTimestamp = mSystem.currentTimeMillis();
			GameScr.info1.addInfo("Đang bật 'muanhieu' và bắt đầu mua " + targetItemName + " [ID: " + targetItemId + "]...", 0);
		}
		else
		{
			state = 2;
			stateTimestamp = mSystem.currentTimeMillis();
			GameScr.info1.addInfo("Bắt đầu Auto Mua Nhiều: " + targetItemName + " [ID: " + targetItemId + "]", 0);
		}
	}

	public void StopAuto(string reason = "Đã dừng Auto Mua Nhiều")
	{
		isRunning = false;
		state = 0;
		lastStopTimeMs = mSystem.currentTimeMillis();
		if (!string.IsNullOrEmpty(reason))
		{
			GameScr.info1.addInfo(reason, 0);
		}
	}

	public void Update()
	{
		if (isRunning && !ChatTextField.gI().isShow && Input.GetKey(KeyCode.A))
		{
			StopAuto("Đã dừng Auto Mua Nhiều (Phím A)");
			return;
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
		case 1:
			if (now - stateTimestamp >= 350)
			{
				state = 2;
				stateTimestamp = now;
			}
			break;
		case 2:
			Service.gI().buyItem(targetBuyType, targetItemId, 0);
			state = 3;
			stateTimestamp = now;
			break;
		case 3:
			if (now - stateTimestamp >= 2500)
			{
				retryCount++;
				if (retryCount > 2)
				{
					StopAuto("Quá thời gian chờ Server phản hồi (-125). Hãy mở lại Shop 1 lần!");
				}
				else
				{
					state = 2;
					stateTimestamp = now;
				}
			}
			break;
		case 4:
			if (now - stateTimestamp >= delayMs)
			{
				state = 2;
				stateTimestamp = now;
			}
			break;
		}
	}

	public bool OnReceiveServerInputPacket(Message msg)
	{
		if (!isRunning)
		{
			return false;
		}
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

			int qtyToSend = batchQuantity;
			TField tf = new TField();
			tf.setText(qtyToSend.ToString());
			Service.gI().sendClientInput(new TField[1] { tf });

			retryCount = 0;
			completedBatches++;
			totalBought += qtyToSend;

			if (targetBatches > 0 && completedBatches >= targetBatches)
			{
				StopAuto("Đã mua hoàn tất " + totalBought + " " + targetItemName + " [ID: " + targetItemId + "]!");
			}
			else
			{
				state = 4;
				stateTimestamp = mSystem.currentTimeMillis();
			}
			return hideClientInput;
		}
		catch (Exception)
		{
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
			if (isRunning)
			{
				Service.gI().chat("muanhieu");
				serverMuaNhieuEnabled = true;
			}
		}

		if (isRunning && autoStopOnError)
		{
			if (lower.Contains("hành trang đã đầy") || lower.Contains("chỗ trống") || lower.Contains("không đủ") || lower.Contains("còn thiếu"))
			{
				StopAuto("Tự dừng Auto Mua: " + s);
			}
		}
	}

	public bool HandleChatCommand(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string trimmed = text.Trim();
		string lower = trimmed.ToLower();

		if (lower == "amn" || lower == "automua")
		{
			ToggleWindow();
			return true;
		}
		if (lower == "npc" || lower == "idnpc" || lower == "dsnpc")
		{
			if (GameScr.vNpc == null || GameScr.vNpc.size() == 0)
			{
				GameScr.info1.addInfo("Map này không có NPC nào!", 0);
			}
			else
			{
				GameScr.info1.addInfo("=== NPC MAP " + TileMap.mapID + " (" + TileMap.mapName + ") ===", 0);
				for (int i = 0; i < GameScr.vNpc.size(); i++)
				{
					Npc n = (Npc)GameScr.vNpc.elementAt(i);
					if (n != null && n.template != null)
					{
						GameScr.info1.addInfo(n.template.name + " -> ID: " + n.template.npcTemplateId + " (cx=" + n.cx + ", cy=" + n.cy + ")", 0);
					}
				}
			}
			return true;
		}
		if (lower == "muanhieu")
		{
			Service.gI().chat("muanhieu");
			serverMuaNhieuEnabled = true;
			EnsureWindowPosition();
			isShowWindow = true;
			return true;
		}
		if (lower.StartsWith("amn id "))
		{
			if (int.TryParse(lower.Replace("amn id ", "").Trim(), out var newId) && newId >= 0)
			{
				targetItemId = newId;
				ItemTemplate it = ItemTemplates.get((short)newId);
				targetItemName = (it != null && !string.IsNullOrEmpty(it.name)) ? it.name : ("Item " + newId);
				GameScr.info1.addInfo("Đã chọn vật phẩm: " + targetItemName + " [ID: " + targetItemId + "]", 0);
				EnsureWindowPosition();
				isShowWindow = true;
			}
			return true;
		}
		if (lower.StartsWith("amn sl "))
		{
			if (int.TryParse(lower.Replace("amn sl ", "").Trim(), out var sl) && sl >= 1 && sl <= 1000)
			{
				batchQuantity = sl;
				GameScr.info1.addInfo("Số lượng mỗi đợt: " + batchQuantity, 0);
			}
			else
			{
				GameScr.info1.addInfo("Số lượng phải từ 1 đến 1000", 0);
			}
			return true;
		}
		if (lower.StartsWith("amn lap "))
		{
			if (int.TryParse(lower.Replace("amn lap ", "").Trim(), out var lap) && lap >= 0)
			{
				targetBatches = lap;
				GameScr.info1.addInfo("Số đợt cần mua: " + ((targetBatches == 0) ? "Vô hạn" : targetBatches.ToString()), 0);
			}
			return true;
		}
		if (lower.StartsWith("amn delay "))
		{
			if (int.TryParse(lower.Replace("amn delay ", "").Trim(), out var d) && d >= 20 && d <= 5000)
			{
				delayMs = d;
				GameScr.info1.addInfo("Độ trễ mỗi đợt: " + delayMs + " ms", 0);
			}
			return true;
		}
		return false;
	}

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
			if (editFieldMode == 1)
			{
				batchQuantity = System.Math.Max(1, System.Math.Min(1000, val));
				GameScr.info1.addInfo("Đã cài số lượng mỗi đợt: " + batchQuantity, 0);
			}
			else if (editFieldMode == 2)
			{
				targetBatches = System.Math.Max(0, val);
				GameScr.info1.addInfo("Đã cài số đợt mua: " + ((targetBatches == 0) ? "Vô hạn" : targetBatches.ToString()), 0);
			}
			else if (editFieldMode == 3)
			{
				delayMs = System.Math.Max(20, System.Math.Min(5000, val));
				GameScr.info1.addInfo("Đã cài Delay: " + delayMs + " ms", 0);
			}
			else if (editFieldMode == 4)
			{
				if (val >= 0)
				{
					targetItemId = val;
					ItemTemplate it = ItemTemplates.get((short)val);
					targetItemName = (it != null && !string.IsNullOrEmpty(it.name)) ? it.name : ("Item " + val);
					GameScr.info1.addInfo("Đã chọn vật phẩm: " + targetItemName + " [ID: " + targetItemId + "]", 0);
				}
			}
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
		EnsureWindowPosition();
		int closeX = winX + winW - 24;
		int closeY = winY + 7;

		// Close button [X]
		if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(closeX, closeY, 18, 18))
		{
			isShowWindow = false;
			isDragging = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Dragging by header bar
		if (GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(winX, winY, winW - 28, 32))
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

		int inputX = winX + winW - 68;
		int row1Y = winY + 38;
		int row2Y = winY + 59;
		int row3Y = winY + 80;

		// Click batchQuantity box
		if (GameCanvas.isPointerHoldIn(inputX, row1Y, 58, 18))
		{
			GameCanvas.clearAllPointerEvent();
			OpenInputField(1, "Số lượng mỗi đợt (1-1000)", "VD: 1000");
			return;
		}
		// Click targetBatches box
		if (GameCanvas.isPointerHoldIn(inputX, row2Y, 58, 18))
		{
			GameCanvas.clearAllPointerEvent();
			OpenInputField(2, "Số đợt cần mua (0 = vô hạn)", "VD: 160");
			return;
		}
		// Click delayMs box
		if (GameCanvas.isPointerHoldIn(inputX, row3Y, 58, 18))
		{
			GameCanvas.clearAllPointerEvent();
			OpenInputField(3, "Độ trễ mỗi đợt (ms)", "VD: 150");
			return;
		}

		// Click item line to change ID directly
		int itemLineY = winY + 101;
		if (GameCanvas.isPointerHoldIn(winX + 10, itemLineY - 3, winW - 20, 13))
		{
			GameCanvas.clearAllPointerEvent();
			OpenInputField(4, "Nhập ID vật phẩm cần mua", "VD: 457");
			return;
		}

		// Click Checkbox 1
		int cb1Y = winY + 126;
		if (GameCanvas.isPointerHoldIn(winX + 10, cb1Y - 2, winW - 20, 14))
		{
			hideClientInput = !hideClientInput;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Click Checkbox 2
		int cb2Y = winY + 141;
		if (GameCanvas.isPointerHoldIn(winX + 10, cb2Y - 2, winW - 20, 14))
		{
			autoStopOnError = !autoStopOnError;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Click Stacked Start / Stop Pill Buttons
		int btnX = winX + 16;
		int btnW = winW - 32;
		int startBtnY = winY + 178;
		int stopBtnY = winY + 200;

		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			StartAuto();
			return;
		}
		if (GameCanvas.isPointerHoldIn(btnX, stopBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			StopAuto("Đã dừng Auto Mua Nhiều");
			return;
		}
	}

	public void Paint(mGraphics g)
	{
		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowPosition();

		// Outer drop shadow & gold border frame
		g.setColor(0x160A03, 0.92f);
		g.fillRect(winX - 2, winY - 2, winW + 4, winH + 4, 6);

		g.setColor(0xD49A2A);
		g.fillRect(winX, winY, winW, winH, 5);

		g.setColor(0xF7CE68);
		g.drawRect(winX + 1, winY + 1, winW - 3, winH - 3);

		// Main dark brown wood background
		g.setColor(0x3B1F0E);
		g.fillRect(winX + 4, winY + 4, winW - 8, winH - 8, 4);

		g.setColor(0x472511);
		g.fillRect(winX + 6, winY + 34, winW - 12, winH - 40, 3);

		// Header bar
		g.setColor(0x693414);
		g.fillRect(winX + 4, winY + 4, winW - 8, 29, 3);
		g.setColor(0xD49A2A);
		g.drawLine(winX + 4, winY + 33, winX + winW - 5, winY + 33);

		// 4 Gold Corner Ornaments
		g.setColor(0xF7CE68);
		g.fillRect(winX + 2, winY + 2, 9, 3);
		g.fillRect(winX + 2, winY + 2, 3, 9);
		g.fillRect(winX + winW - 11, winY + 2, 9, 3);
		g.fillRect(winX + winW - 5, winY + 2, 3, 9);
		g.fillRect(winX + 2, winY + winH - 5, 9, 3);
		g.fillRect(winX + 2, winY + winH - 11, 3, 9);
		g.fillRect(winX + winW - 11, winY + winH - 5, 9, 3);
		g.fillRect(winX + winW - 5, winY + winH - 11, 3, 9);

		// Dragon Ball 3-star badge on header left
		int dbX = winX + 10;
		int dbY = winY + 9;
		g.setColor(0xE65100);
		g.fillRect(dbX, dbY, 18, 18, 9);
		g.setColor(0xFFB300);
		g.fillRect(dbX + 1, dbY + 1, 16, 16, 8);
		g.setColor(0xD50000);
		g.fillRect(dbX + 8, dbY + 4, 2, 2);
		g.fillRect(dbX + 5, dbY + 10, 2, 2);
		g.fillRect(dbX + 11, dbY + 10, 2, 2);

		// 2-Line Centered Header Title
		int headerCenterX = winX + winW / 2 + 1;
		mFont.tahoma_7b_yellow.drawStringBorder(g, "CÀI ĐẶT AUTO MUA NHIỀU", headerCenterX, winY + 6, mFont.CENTER, mFont.tahoma_7_grey);
		string subHeader;
		if (targetItemId >= 0)
		{
			subHeader = targetItemName + " [ID: " + targetItemId + "]";
			if (mFont.tahoma_7b_yellow.getWidth(subHeader) > winW - 54)
			{
				int maxLen = 13;
				string shortName = (targetItemName.Length > maxLen) ? (targetItemName.Substring(0, maxLen) + "..") : targetItemName;
				subHeader = shortName + " [ID: " + targetItemId + "]";
			}
		}
		else
		{
			subHeader = "(Chưa chọn item - Mở Shop)";
		}
		mFont.tahoma_7b_yellow.drawStringBorder(g, subHeader, headerCenterX, winY + 18, mFont.CENTER, mFont.tahoma_7_grey);

		// Close button [X]
		int closeX = winX + winW - 24;
		int closeY = winY + 7;
		g.setColor(0xB71C1C);
		g.fillRect(closeX, closeY, 17, 17, 3);
		g.setColor(0xE53935);
		g.fillRect(closeX + 1, closeY + 1, 15, 15, 3);
		g.setColor(0xFFCDD2);
		g.drawRect(closeX, closeY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", closeX + 9, closeY + 3, mFont.CENTER);

		int iconX = winX + 11;
		int labelX = winX + 24;
		int inputX = winX + winW - 68;

		// Row 1: Batch Quantity
		int row1Y = winY + 38;
		DrawStackIcon(g, iconX, row1Y + 4);
		mFont.tahoma_7b_white.drawString(g, "Số lượng mỗi đợt:", labelX, row1Y + 3, mFont.LEFT);
		DrawInputBox(g, inputX, row1Y, 58, 18, batchQuantity.ToString(), isGreenText: true);

		// Row 2: Target Batches
		int row2Y = winY + 59;
		DrawCartIcon(g, iconX, row2Y + 4);
		mFont.tahoma_7b_white.drawString(g, "Số đợt cần mua:", labelX, row2Y + 3, mFont.LEFT);
		DrawInputBox(g, inputX, row2Y, 58, 18, targetBatches.ToString(), isGreenText: true);

		// Row 3: Delay
		int row3Y = winY + 80;
		DrawClockIcon(g, iconX, row3Y + 4);
		mFont.tahoma_7b_white.drawString(g, "Độ trễ mỗi đợt:", labelX, row3Y + 3, mFont.LEFT);
		DrawInputBox(g, inputX, row3Y, 58, 18, delayMs + " ms", isGreenText: false);

		// 2 Centered Summary / Note Lines
		string totalCalc = (targetBatches > 0) ? (NinjaUtil.getMoneys((long)batchQuantity * (long)targetBatches) + " cái") : "Vô hạn";
		if (targetItemId >= 0)
		{
			string itemText = "Vật phẩm: " + targetItemName + " [ID: " + targetItemId + "]";
			if (mFont.tahoma_7_yellow.getWidth(itemText) > winW - 20)
			{
				int maxLen = 16;
				string shortName = (targetItemName.Length > maxLen) ? (targetItemName.Substring(0, maxLen) + "..") : targetItemName;
				itemText = "Vật phẩm: " + shortName + " [ID: " + targetItemId + "]";
			}
			mFont.tahoma_7_yellow.drawString(g, itemText, winX + winW / 2, winY + 101, mFont.CENTER);
		}
		else
		{
			mFont.tahoma_7_red.drawString(g, "Chưa chọn item (Mở Shop hoặc bấm nhập ID)", winX + winW / 2, winY + 101, mFont.CENTER);
		}
		mFont.tahoma_7_white.drawString(g, "(Tối đa 1.000/đợt - Tổng: " + totalCalc + ")", winX + winW / 2, winY + 112, mFont.CENTER);

		// Checkbox 1
		int cb1Y = winY + 126;
		DrawCheckbox(g, winX + 11, cb1Y, hideClientInput, "Ẩn hoàn toàn ô nhập ClientInput");

		// Checkbox 2
		int cb2Y = winY + 141;
		DrawCheckbox(g, winX + 11, cb2Y, autoStopOnError, "Tự dừng khi hành trang đầy hoặc không đủ tiền/ngọc");

		// Progress Bar Row
		int progY = winY + 157;
		mFont.tahoma_7b_white.drawString(g, "Tiến độ :", winX + 11, progY + 2, mFont.LEFT);
		int barX = winX + 54;
		int barW = winW - 64;
		int barH = 16;
		g.setColor(0x18100A);
		g.fillRect(barX, progY, barW, barH, 3);

		int totalTargetItems = (targetBatches > 0) ? (batchQuantity * targetBatches) : 0;
		float ratio = 0f;
		if (totalTargetItems > 0)
		{
			ratio = Mathf.Clamp01((float)totalBought / (float)totalTargetItems);
		}
		else if (isRunning)
		{
			ratio = 1f;
		}
		int fillW = (int)((float)(barW - 2) * ratio);
		if (fillW > 0)
		{
			g.setColor(0x1565C0);
			g.fillRect(barX + 1, progY + 1, fillW, barH - 2, 2);
			g.setColor(0x42A5F5);
			g.fillRect(barX + 1, progY + 1, fillW, (barH - 2) / 2, 2);
		}
		g.setColor(0xD49A2A);
		g.drawRect(barX, progY, barW - 1, barH - 1);

		string progStr = (totalTargetItems > 0)
			? ("Đã mua: " + NinjaUtil.getMoneys(totalBought) + " / " + NinjaUtil.getMoneys(totalTargetItems) + " (" + (int)(ratio * 100f) + "%)")
			: ("Đã mua: " + NinjaUtil.getMoneys(totalBought) + " cái");
		mFont.tahoma_7b_white.drawStringBorder(g, progStr, barX + barW / 2, progY + 2, mFont.CENTER, mFont.tahoma_7_grey);

		// 2 Vertically Stacked Pill Buttons
		int btnX = winX + 16;
		int btnW = winW - 32;
		int btnH = 19;
		int startBtnY = winY + 178;
		int stopBtnY = winY + 200;

		// Top Button: Green Pill (Start)
		DrawPillButton(g, btnX, startBtnY, btnW, btnH, isGreen: true, isRunning);
		string startLabel = isRunning ? "ĐANG CHẠY..." : "BẮT ĐẦU CHẠY";
		int startTextW = mFont.tahoma_7b_white.getWidth(startLabel);
		int startIconX = btnX + (btnW - startTextW - 10) / 2;
		DrawPlayIcon(g, startIconX, startBtnY + 5);
		mFont.tahoma_7b_white.drawStringBorder(g, startLabel, startIconX + 10, startBtnY + 3, mFont.LEFT, mFont.tahoma_7_grey);

		// Bottom Button: Red Pill (Stop - Phím A)
		DrawPillButton(g, btnX, stopBtnY, btnW, btnH, isGreen: false, isActive: false);
		string stopLabel = "DỪNG LẠI (Phím A)";
		int stopTextW = mFont.tahoma_7b_white.getWidth(stopLabel);
		int stopIconX = btnX + (btnW - stopTextW - 10) / 2;
		DrawStopIcon(g, stopIconX, stopBtnY + 6);
		mFont.tahoma_7b_white.drawStringBorder(g, stopLabel, stopIconX + 10, stopBtnY + 3, mFont.LEFT, mFont.tahoma_7_grey);
	}

	private void DrawPillButton(mGraphics g, int x, int y, int w, int h, bool isGreen, bool isActive)
	{
		int borderCol = isGreen ? 0xA5D6A7 : 0xEF9A9A;
		int darkCol = isGreen ? 0x1B5E20 : 0x7F0000;
		int midCol = isGreen ? (isActive ? 0x43A047 : 0x2E7D32) : 0xC62828;
		int topCol = isGreen ? 0x66BB6A : 0xE53935;

		// Rounded pill outer shape
		g.setColor(darkCol);
		g.fillRect(x + 2, y, w - 4, h);
		g.fillRect(x, y + 2, w, h - 4);
		g.fillRect(x + 1, y + 1, w - 2, h - 2);

		// Mid fill
		g.setColor(midCol);
		g.fillRect(x + 2, y + 1, w - 4, h - 3);
		g.fillRect(x + 1, y + 2, w - 2, h - 5);

		// Glossy top half
		g.setColor(topCol);
		g.fillRect(x + 3, y + 2, w - 6, (h - 4) / 2);

		// Pill border highlight
		g.setColor(borderCol);
		g.drawLine(x + 2, y, x + w - 3, y);
		g.drawLine(x + 2, y + h - 1, x + w - 3, y + h - 1);
		g.drawLine(x, y + 2, x, y + h - 3);
		g.drawLine(x + w - 1, y + 2, x + w - 1, y + h - 3);
	}

	private void DrawPlayIcon(mGraphics g, int x, int y)
	{
		g.setColor(0xFFFFFF);
		g.fillRect(x, y, 2, 8);
		g.fillRect(x + 2, y + 1, 2, 6);
		g.fillRect(x + 4, y + 2, 2, 4);
		g.fillRect(x + 6, y + 3, 1, 2);
	}

	private void DrawStopIcon(mGraphics g, int x, int y)
	{
		g.setColor(0xFFFFFF);
		g.fillRect(x, y, 7, 7);
	}

	private void DrawStackIcon(mGraphics g, int x, int y)
	{
		g.setColor(0xF7CE68);
		g.fillRect(x + 1, y, 8, 2);
		g.fillRect(x, y + 2, 10, 2);
		g.setColor(0xD49A2A);
		g.fillRect(x, y + 5, 10, 2);
		g.fillRect(x, y + 8, 10, 2);
	}

	private void DrawCartIcon(mGraphics g, int x, int y)
	{
		g.setColor(0xF7CE68);
		g.fillRect(x, y, 2, 2);
		g.fillRect(x + 2, y + 2, 7, 5);
		g.setColor(0xD49A2A);
		g.fillRect(x + 3, y + 8, 2, 2);
		g.fillRect(x + 7, y + 8, 2, 2);
	}

	private void DrawClockIcon(mGraphics g, int x, int y)
	{
		g.setColor(0xD49A2A);
		g.fillRect(x + 1, y, 8, 10);
		g.fillRect(x, y + 1, 10, 8);
		g.setColor(0x3B1F0E);
		g.fillRect(x + 2, y + 2, 6, 6);
		g.setColor(0xF7CE68);
		g.fillRect(x + 4, y + 2, 2, 3);
		g.fillRect(x + 4, y + 4, 3, 2);
	}

	private void DrawInputBox(mGraphics g, int x, int y, int w, int h, string val, bool isGreenText)
	{
		g.setColor(0x160D06);
		g.fillRect(x, y, w, h, 3);
		g.setColor(0xD49A2A);
		g.drawRect(x, y, w - 1, h - 1);
		if (isGreenText)
		{
			mFont.tahoma_7b_green.drawString(g, val, x + w / 2, y + 3, mFont.CENTER);
		}
		else
		{
			mFont.tahoma_7b_white.drawString(g, val, x + w / 2, y + 3, mFont.CENTER);
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
}
