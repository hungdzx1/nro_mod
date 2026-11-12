using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game2.Mod;

public class NpcMenuHelper : IChatable
{
	private static NpcMenuHelper _instance;

	public bool isShowWindow;

	public int selectedTab; // 0: Đổi Điểm (NPC 66), 1: Đổi Zeno (NPC 18)

	public bool isSpyEnabled;

	public bool isLogToFile;

	public bool hideUi = true; // Ẩn hoàn toàn Menu và ô nhập khi chạy Auto

	public bool isAutoRunning;

	public int autoType; // 0: Custom steps, 1: Nồi Bánh (ID 66), 2: Thần Mèo Karin Tiên Phù Zeno (ID 18)

	public int autoNpcId = -1;

	public string autoNpcName = "";

	public int[] autoSteps;

	public int currentStepIndex;

	// Cấu hình Tab 0: Đổi Điểm Nồi Bánh (ID 66)
	public int pointIndex = 1; // 0: 10Tr, 1: 5K, 2: 3K, 3: 200, 4: 50

	public int targetBatchesBanh = 50; // Số lượt cần đổi (0 = vô hạn)

	public int delayBanhMs = 350;

	// Cấu hình Tab 1: Đổi Tiên Phù Zeno Thần Mèo Karin (ID 18)
	public int inputQuantity = 1000; // Số lượng Tiên Phù nhập mỗi đợt

	public int targetBatchesZeno = 50; // Số đợt cần đổi (0 = vô hạn)

	public int delayZenoMs = 350;

	public int totalZenoBought;

	// Biến trạng thái chạy thực tế
	public int delayMs = 350;

	public long lastActionTime;

	public long lastStopTimeMs;

	public int completedCount;

	public int state; // 0: Idle, 1: WaitMenu1, 2: WaitMenu2, 3: WaitInput, 4: Cooldown

	private int editFieldMode; // 1: targetBatchesBanh, 2: delayBanhMs, 3: inputQuantity, 4: targetBatchesZeno, 5: delayZenoMs

	private int winX = -1;

	private int winY = -1;

	private readonly int winW = 216;

	private readonly int winH = 232;

	private bool isDragging;

	private int dragOffsetX;

	private int dragOffsetY;

	private static readonly string[] MocShortNames = new string[5] { "10Tr", "5K", "3K", "200", "50" };

	private static readonly string[] MocFullNames = new string[5] { "10.000.000 Điểm", "5.000 Điểm", "3.000 Điểm", "200 Điểm", "50 Điểm" };

	public static NpcMenuHelper gI()
	{
		if (_instance == null)
		{
			_instance = new NpcMenuHelper();
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

	public void ToggleWindow(int tab = -1)
	{
		EnsureWindowPosition();
		if (tab >= 0 && tab <= 1)
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
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hideNow();
			}
		}
	}

	public void LogToFile(string text)
	{
		if (!isLogToFile)
		{
			return;
		}
		try
		{
			string logLine = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine;
			File.AppendAllText("menu_spy.txt", logLine, Encoding.UTF8);
		}
		catch
		{
		}
	}

	public bool OnReceiveMenu(string[] menu, Npc npc)
	{
		if (npc == null || npc.template == null || menu == null)
		{
			return false;
		}

		int npcTempId = npc.template.npcTemplateId;
		string npcName = string.IsNullOrEmpty(npc.template.name) ? ("NPC " + npcTempId) : npc.template.name;

		if (isSpyEnabled)
		{
			LogToFile("=== MENU: " + npcName + " [ID: " + npcTempId + "] ===");
			for (int i = 0; i < menu.Length; i++)
			{
				string optStr = " [" + i + "] " + menu[i];
				LogToFile(optStr);
			}
			if (!isAutoRunning)
			{
				GameScr.info1.addInfo("=== MENU: " + npcName + " [ID: " + npcTempId + "] ===", 0);
				for (int j = 0; j < menu.Length; j++)
				{
					GameScr.info1.addInfo(" [" + j + "] " + menu[j], 0);
				}
			}
		}

		if (!isAutoRunning || autoNpcId != npcTempId)
		{
			return false;
		}

		// === AUTO TYPE 1: NỒI BÁNH (ID 66) ===
		if (autoType == 1 && npcTempId == 66)
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
				LogToFile("-> Auto Nồi Bánh: Chọn [2] Đổi điểm");
				state = 2; // Đợi Menu cấp 2
				lastActionTime = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(66, 2);
				return hideUi;
			}
			else
			{
				int pick = (pointIndex >= 0 && pointIndex < menu.Length) ? pointIndex : 1;
				LogToFile("-> Auto Nồi Bánh: Chọn [" + pick + "] " + menu[pick]);
				completedCount++;
				Service.gI().confirmMenu(66, (sbyte)pick);

				if (targetBatchesBanh > 0 && completedCount >= targetBatchesBanh)
				{
					StopAuto("Đã đổi hoàn tất " + completedCount + " lượt (" + MocFullNames[pick] + ")!");
				}
				else
				{
					GameScr.info1.addInfo("Đã đổi Nồi Bánh lượt " + completedCount + " (" + MocShortNames[pick] + ")", 0);
					state = 4; // Chuyển sang Cooldown
					lastActionTime = mSystem.currentTimeMillis();
				}
				return hideUi;
			}
		}

		// === AUTO TYPE 2: THẦN MÈO KARIN - TIÊN PHÙ ZENO (ID 18) ===
		if (autoType == 2 && npcTempId == 18)
		{
			bool isMenu1 = false;
			for (int k = 0; k < menu.Length; k++)
			{
				if (menu[k].ToLower().Contains("đổi tiên phù"))
				{
					isMenu1 = true;
					break;
				}
			}

			if (isMenu1)
			{
				LogToFile("-> Auto Thần Mèo: Chọn [3] Đổi Tiên Phù Zeno");
				state = 2; // Đợi Menu cấp 2
				lastActionTime = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(18, 3);
				return hideUi;
			}
			else
			{
				LogToFile("-> Auto Thần Mèo: Chọn [0] Tiên Phù Zeno");
				state = 3; // Đợi gói -125 ClientInput
				lastActionTime = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(18, 0);
				return hideUi;
			}
		}

		// === AUTO TYPE 0: CUSTOM STEPS BẤT KỲ ===
		if (autoType == 0 && autoSteps != null && currentStepIndex < autoSteps.Length)
		{
			int selectOpt = autoSteps[currentStepIndex];
			if (selectOpt >= 0 && selectOpt < menu.Length)
			{
				currentStepIndex++;
				LogToFile("-> Auto Custom: Chọn [" + selectOpt + "] " + menu[selectOpt]);
				Service.gI().confirmMenu((short)npcTempId, (sbyte)selectOpt);

				if (currentStepIndex >= autoSteps.Length)
				{
					completedCount++;
					GameScr.info1.addInfo("Đã hoàn thành đợt " + completedCount, 0);
					currentStepIndex = 0;
					state = 4;
					lastActionTime = mSystem.currentTimeMillis();
				}
				return hideUi;
			}
			else
			{
				StopAuto("Tuỳ chọn [" + selectOpt + "] không tồn tại trong menu!");
				return false;
			}
		}

		return false;
	}

	public bool OnReceiveServerInputPacket(Message msg)
	{
		if (!isAutoRunning)
		{
			return false;
		}

		// Xử lý gói tin -125 khi đang đổi Tiên Phù Zeno ở Thần Mèo Karin
		if (autoType == 2 && autoNpcId == 18)
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

				int qty = (inputQuantity <= 0) ? 1000 : inputQuantity;
				TField tf = new TField();
				tf.setText(qty.ToString());
				Service.gI().sendClientInput(new TField[1] { tf });

				completedCount++;
				totalZenoBought += qty;
				LogToFile("-> Auto Thần Mèo: Bắn ngầm số lượng = " + qty);

				if (targetBatchesZeno > 0 && completedCount >= targetBatchesZeno)
				{
					StopAuto("Đã đổi hoàn tất " + NinjaUtil.getMoneys(totalZenoBought) + " Tiên Phù Zeno!");
				}
				else
				{
					GameScr.info1.addInfo("Đã đổi " + qty + " Tiên Phù Zeno (Đợt " + completedCount + ")", 0);
					state = 4; // Chuyển sang Cooldown
					lastActionTime = mSystem.currentTimeMillis();
				}
				return hideUi;
			}
			catch (Exception ex)
			{
				LogToFile("Lỗi xử lý -125: " + ex.Message);
				return false;
			}
		}

		return false;
	}

	public void OnConfirmMenu(short npcId, sbyte select)
	{
		if (isSpyEnabled)
		{
			string log = "-> Gửi confirmMenu(NPC: " + npcId + ", Lựa chọn: " + select + ")";
			LogToFile(log);
		}
	}

	public void OnServerInfo(string s)
	{
		if (string.IsNullOrEmpty(s) || !isAutoRunning)
		{
			return;
		}
		string lower = s.ToLower();
		if (lower.Contains("hành trang đã đầy") || lower.Contains("chỗ trống") || lower.Contains("không đủ") || lower.Contains("còn thiếu") || lower.Contains("hết điểm") || lower.Contains("hết vé"))
		{
			StopAuto("Tự dừng Auto: " + s);
		}
	}

	public void StartAutoBanh(int pointMoc = -1, int delay = -1, int batches = -1)
	{
		Npc targetNpc = ModFunc.GetNpcByTempId(66);
		if (targetNpc == null)
		{
			GameScr.info1.addInfo("Không tìm thấy NPC Nồi Bánh (ID: 66) trong Map này!", 0);
			return;
		}

		if (pointMoc >= 0 && pointMoc <= 4)
		{
			pointIndex = pointMoc;
		}
		if (delay >= 50)
		{
			delayBanhMs = delay;
		}
		if (batches >= 0)
		{
			targetBatchesBanh = batches;
		}

		selectedTab = 0;
		autoType = 1;
		autoNpcId = 66;
		autoNpcName = "Nồi Bánh";
		delayMs = System.Math.Max(50, delayBanhMs);
		completedCount = 0;
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		string mocName = MocFullNames[pointIndex];
		GameScr.info1.addInfo("BẮT ĐẦU ĐỔI ĐIỂM NỒI BÁNH: Mốc " + mocName, 0);
		TriggerOpenNpcMenu();
	}

	public void StartAutoZeno(int qty = -1, int delay = -1, int batches = -1)
	{
		Npc targetNpc = ModFunc.GetNpcByTempId(18);
		if (targetNpc == null)
		{
			GameScr.info1.addInfo("Không tìm thấy NPC Thần Mèo Karin (ID: 18) trong Map này!", 0);
			return;
		}

		if (qty > 0)
		{
			inputQuantity = qty;
		}
		if (delay >= 50)
		{
			delayZenoMs = delay;
		}
		if (batches >= 0)
		{
			targetBatchesZeno = batches;
		}

		selectedTab = 1;
		autoType = 2;
		autoNpcId = 18;
		autoNpcName = "Thần Mèo Karin";
		delayMs = System.Math.Max(50, delayZenoMs);
		completedCount = 0;
		totalZenoBought = 0;
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		GameScr.info1.addInfo("BẮT ĐẦU ĐỔI TIÊN PHÙ ZENO: " + inputQuantity + " cái/đợt", 0);
		TriggerOpenNpcMenu();
	}

	public void StartAutoCustom(int npcId, string npcName, int[] steps, int delay = 350)
	{
		if (steps == null || steps.Length == 0)
		{
			GameScr.info1.addInfo("Cần chỉ định ít nhất 1 lựa chọn menu!", 0);
			return;
		}

		autoType = 0;
		autoNpcId = npcId;
		autoNpcName = npcName;
		autoSteps = steps;
		currentStepIndex = 0;
		completedCount = 0;
		delayMs = System.Math.Max(50, delay);
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		string stepsStr = string.Join(" -> ", Array.ConvertAll(steps, s => "[" + s + "]"));
		GameScr.info1.addInfo("Bắt đầu Auto: " + npcName + " (ID: " + npcId + ") Chuỗi: " + stepsStr, 0);
		TriggerOpenNpcMenu();
	}

	private void TriggerOpenNpcMenu()
	{
		if (autoNpcId <= 0)
		{
			return;
		}
		Npc targetNpc = ModFunc.GetNpcByTempId(autoNpcId);
		if (targetNpc != null)
		{
			if (System.Math.Abs(targetNpc.cx - Char.myCharz().cx) > 40 || System.Math.Abs(targetNpc.cy - Char.myCharz().cy) > 40)
			{
				Char.myCharz().cx = targetNpc.cx + 20;
				Char.myCharz().cy = targetNpc.cy;
				Service.gI().charMove();
			}
			Char.myCharz().npcFocus = targetNpc;
		}
		Service.gI().openMenu(autoNpcId);
	}

	public void StopAuto(string reason = "Đã dừng Auto")
	{
		lastStopTimeMs = mSystem.currentTimeMillis();
		if (isAutoRunning)
		{
			isAutoRunning = false;
			autoNpcId = -1;
			autoSteps = null;
			currentStepIndex = 0;
			state = 0;
			if (GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.doCloseMenu();
			}
			ChatTextField.gI().isShow = false;
			GameScr.info1.addInfo(reason + " (Tổng: " + completedCount + " đợt)", 0);
		}
		else if (!string.IsNullOrEmpty(reason))
		{
			GameScr.info1.addInfo(reason, 0);
		}
	}

	public void Update()
	{
		if (isAutoRunning && !ChatTextField.gI().isShow && Input.GetKey(KeyCode.A))
		{
			StopAuto("Đã dừng Auto (Phím A)");
			return;
		}

		if (isShowWindow)
		{
			UpdateTouch();
		}

		if (!isAutoRunning)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();

		if (state == 4)
		{
			if (now - lastActionTime >= delayMs)
			{
				state = 1;
				lastActionTime = now;
				TriggerOpenNpcMenu();
			}
			return;
		}

		if ((state == 1 || state == 2 || state == 3) && now - lastActionTime >= 2500)
		{
			state = 1;
			lastActionTime = now;
			TriggerOpenNpcMenu();
		}
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
				targetBatchesBanh = System.Math.Max(0, val);
				GameScr.info1.addInfo("Số lượt đổi điểm: " + ((targetBatchesBanh == 0) ? "Vô hạn" : targetBatchesBanh.ToString()), 0);
			}
			else if (editFieldMode == 2)
			{
				delayBanhMs = System.Math.Max(50, System.Math.Min(10000, val));
				if (isAutoRunning && autoType == 1)
				{
					delayMs = delayBanhMs;
				}
				GameScr.info1.addInfo("Độ trễ Đổi Điểm: " + delayBanhMs + " ms", 0);
			}
			else if (editFieldMode == 3)
			{
				inputQuantity = System.Math.Max(1, System.Math.Min(99999, val));
				GameScr.info1.addInfo("Số lượng Tiên Phù mỗi đợt: " + inputQuantity, 0);
			}
			else if (editFieldMode == 4)
			{
				targetBatchesZeno = System.Math.Max(0, val);
				GameScr.info1.addInfo("Số đợt đổi Zeno: " + ((targetBatchesZeno == 0) ? "Vô hạn" : targetBatchesZeno.ToString()), 0);
			}
			else if (editFieldMode == 5)
			{
				delayZenoMs = System.Math.Max(50, System.Math.Min(10000, val));
				if (isAutoRunning && autoType == 2)
				{
					delayMs = delayZenoMs;
				}
				GameScr.info1.addInfo("Độ trễ Đổi Zeno: " + delayZenoMs + " ms", 0);
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
		int closeY = winY + 6;

		// Nút đóng [X]
		if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(closeX, closeY, 18, 18))
		{
			isShowWindow = false;
			isDragging = false;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// Kéo thả cửa sổ bằng thanh tiêu đề
		if (GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(winX, winY, winW - 28, 30))
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

		// 2 Nút chuyển Tab
		int tabY = winY + 34;
		if (GameCanvas.isPointerHoldIn(winX + 10, tabY, 96, 19))
		{
			selectedTab = 0;
			GameCanvas.clearAllPointerEvent();
			return;
		}
		if (GameCanvas.isPointerHoldIn(winX + 110, tabY, 96, 19))
		{
			selectedTab = 1;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		int inputX = winX + winW - 68;

		if (selectedTab == 0)
		{
			// 5 Nút chọn mốc điểm Nồi Bánh [10Tr] [5K] [3K] [200] [50]
			int mocY = winY + 71;
			for (int i = 0; i < 5; i++)
			{
				int mocX = winX + 10 + i * 40;
				if (GameCanvas.isPointerHoldIn(mocX, mocY, 36, 18))
				{
					pointIndex = i;
					GameCanvas.clearAllPointerEvent();
					return;
				}
			}

			// Ô nhập Số lượt cần đổi
			int rowBatchesY = winY + 94;
			if (GameCanvas.isPointerHoldIn(inputX, rowBatchesY, 58, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(1, "Số lượt cần đổi (0 = vô hạn)", "VD: 50");
				return;
			}

			// Ô nhập Độ trễ mỗi lượt
			int rowDelayY = winY + 115;
			if (GameCanvas.isPointerHoldIn(inputX, rowDelayY, 58, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(2, "Độ trễ mỗi lượt (ms)", "VD: 350");
				return;
			}
		}
		else
		{
			// Tab 1: Đổi Zeno
			int row1Y = winY + 58;
			int row2Y = winY + 79;
			int row3Y = winY + 100;

			if (GameCanvas.isPointerHoldIn(inputX, row1Y, 58, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(3, "Số lượng Tiên Phù mỗi đợt", "VD: 1000");
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, row2Y, 58, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(4, "Số đợt cần đổi (0 = vô hạn)", "VD: 50");
				return;
			}
			if (GameCanvas.isPointerHoldIn(inputX, row3Y, 58, 18))
			{
				GameCanvas.clearAllPointerEvent();
				OpenInputField(5, "Độ trễ mỗi đợt (ms)", "VD: 350");
				return;
			}
		}

		// Checkbox Ẩn giao diện
		int cbY = winY + 147;
		if (GameCanvas.isPointerHoldIn(winX + 10, cbY - 2, winW - 20, 14))
		{
			hideUi = !hideUi;
			GameCanvas.clearAllPointerEvent();
			return;
		}

		// 2 Nút BẮT ĐẦU CHẠY / DỪNG LẠI (Phím A)
		int btnX = winX + 16;
		int btnW = winW - 32;
		int startBtnY = winY + 184;
		int stopBtnY = winY + 206;

		if (GameCanvas.isPointerHoldIn(btnX, startBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			if (selectedTab == 0)
			{
				StartAutoBanh(pointIndex, delayBanhMs, targetBatchesBanh);
			}
			else
			{
				StartAutoZeno(inputQuantity, delayZenoMs, targetBatchesZeno);
			}
			return;
		}

		if (GameCanvas.isPointerHoldIn(btnX, stopBtnY, btnW, 19))
		{
			GameCanvas.clearAllPointerEvent();
			StopAuto("Đã dừng Auto");
			return;
		}

		// Chặn click xuyên qua cửa sổ
		if (GameCanvas.isPointerHoldIn(winX, winY, winW, winH))
		{
			GameCanvas.clearAllPointerEvent();
		}
	}

	public void Paint(mGraphics g)
	{
		if (!isShowWindow)
		{
			return;
		}
		EnsureWindowPosition();

		// Viền bóng đổ & khung vàng
		g.setColor(0x160A03, 0.92f);
		g.fillRect(winX - 2, winY - 2, winW + 4, winH + 4, 6);

		g.setColor(0xD49A2A);
		g.fillRect(winX, winY, winW, winH, 5);

		g.setColor(0xF7CE68);
		g.drawRect(winX + 1, winY + 1, winW - 3, winH - 3);

		// Nền gỗ nâu đậm
		g.setColor(0x3B1F0E);
		g.fillRect(winX + 4, winY + 4, winW - 8, winH - 8, 4);

		g.setColor(0x472511);
		g.fillRect(winX + 6, winY + 32, winW - 12, winH - 38, 3);

		// Thanh tiêu đề (Header)
		g.setColor(0x693414);
		g.fillRect(winX + 4, winY + 4, winW - 8, 27, 3);
		g.setColor(0xD49A2A);
		g.drawLine(winX + 4, winY + 31, winX + winW - 5, winY + 31);

		// 4 góc trang trí vàng
		g.setColor(0xF7CE68);
		g.fillRect(winX + 2, winY + 2, 9, 3);
		g.fillRect(winX + 2, winY + 2, 3, 9);
		g.fillRect(winX + winW - 11, winY + 2, 9, 3);
		g.fillRect(winX + winW - 5, winY + 2, 3, 9);
		g.fillRect(winX + 2, winY + winH - 5, 9, 3);
		g.fillRect(winX + 2, winY + winH - 11, 3, 9);
		g.fillRect(winX + winW - 11, winY + winH - 5, 9, 3);
		g.fillRect(winX + winW - 5, winY + winH - 11, 3, 9);

		// Icon Ngọc Rồng 3 sao góc trái Header
		int dbX = winX + 10;
		int dbY = winY + 8;
		g.setColor(0xE65100);
		g.fillRect(dbX, dbY, 18, 18, 9);
		g.setColor(0xFFB300);
		g.fillRect(dbX + 1, dbY + 1, 16, 16, 8);
		g.setColor(0xD50000);
		g.fillRect(dbX + 8, dbY + 4, 2, 2);
		g.fillRect(dbX + 5, dbY + 10, 2, 2);
		g.fillRect(dbX + 11, dbY + 10, 2, 2);

		// Tiêu đề 2 dòng căn giữa
		int headerCenterX = winX + winW / 2 + 1;
		mFont.tahoma_7b_yellow.drawStringBorder(g, "AUTO ĐỔI ĐIỂM & TIÊN PHÙ", headerCenterX, winY + 5, mFont.CENTER, mFont.tahoma_7_grey);
		string subHeader = (selectedTab == 0) ? "(NPC Nồi Bánh - ID: 66)" : "(NPC Thần Mèo Karin - ID: 18)";
		mFont.tahoma_7b_yellow.drawStringBorder(g, subHeader, headerCenterX, winY + 16, mFont.CENTER, mFont.tahoma_7_grey);

		// Nút đóng [X]
		int closeX = winX + winW - 24;
		int closeY = winY + 6;
		g.setColor(0xB71C1C);
		g.fillRect(closeX, closeY, 17, 17, 3);
		g.setColor(0xE53935);
		g.fillRect(closeX + 1, closeY + 1, 15, 15, 3);
		g.setColor(0xFFCDD2);
		g.drawRect(closeX, closeY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", closeX + 9, closeY + 3, mFont.CENTER);

		// Thanh 2 Tab: [ ĐỔI ĐIỂM (66) ] | [ ĐỔI ZENO (18) ]
		int tabY = winY + 34;
		DrawTabButton(g, winX + 10, tabY, 96, 19, "ĐỔI ĐIỂM (66)", selectedTab == 0);
		DrawTabButton(g, winX + 110, tabY, 96, 19, "ĐỔI ZENO (18)", selectedTab == 1);

		int iconX = winX + 11;
		int labelX = winX + 24;
		int inputX = winX + winW - 68;

		if (selectedTab == 0)
		{
			// === NỘI DUNG TAB 0: ĐỔI ĐIỂM NỒI BÁNH ===
			int lblMocY = winY + 56;
			DrawStackIcon(g, iconX, lblMocY + 2);
			mFont.tahoma_7b_white.drawString(g, "Chọn mốc điểm cần đổi:", labelX, lblMocY, mFont.LEFT);

			int mocY = winY + 71;
			for (int i = 0; i < 5; i++)
			{
				int mocX = winX + 10 + i * 40;
				bool isSel = (pointIndex == i);
				g.setColor(isSel ? 0x1B5E20 : 0x160D06);
				g.fillRect(mocX, mocY, 36, 18, 3);
				if (isSel)
				{
					g.setColor(0x2E7D32);
					g.fillRect(mocX + 1, mocY + 1, 34, 16, 2);
					g.setColor(0x43A047);
					g.fillRect(mocX + 1, mocY + 1, 34, 8, 2);
				}
				g.setColor(isSel ? 0xF7CE68 : 0x8D6E63);
				g.drawRect(mocX, mocY, 35, 17);
				if (isSel)
				{
					mFont.tahoma_7b_yellow.drawStringBorder(g, MocShortNames[i], mocX + 18, mocY + 3, mFont.CENTER, mFont.tahoma_7_grey);
				}
				else
				{
					mFont.tahoma_7b_white.drawString(g, MocShortNames[i], mocX + 18, mocY + 3, mFont.CENTER);
				}
			}

			// Dòng nhập Số lượt cần đổi
			int rowBatchesY = winY + 94;
			DrawCartIcon(g, iconX, rowBatchesY + 4);
			mFont.tahoma_7b_white.drawString(g, "Số lượt cần đổi:", labelX, rowBatchesY + 3, mFont.LEFT);
			DrawInputBox(g, inputX, rowBatchesY, 58, 18, targetBatchesBanh.ToString(), isGreenText: true);

			// Dòng nhập Độ trễ mỗi lượt
			int rowDelayY = winY + 115;
			DrawClockIcon(g, iconX, rowDelayY + 4);
			mFont.tahoma_7b_white.drawString(g, "Độ trễ mỗi lượt:", labelX, rowDelayY + 3, mFont.LEFT);
			DrawInputBox(g, inputX, rowDelayY, 58, 18, delayBanhMs + " ms", isGreenText: false);

			// Ghi chú tóm tắt
			string batchNote = (targetBatchesBanh > 0) ? (targetBatchesBanh + " lượt") : "Vô hạn";
			mFont.tahoma_7_yellow.drawString(g, "Đang chọn: " + MocFullNames[pointIndex] + " (" + batchNote + ")", winX + winW / 2, winY + 135, mFont.CENTER);

			// Checkbox ẩn giao diện
			DrawCheckbox(g, winX + 11, winY + 147, hideUi, "Ẩn hoàn toàn Menu & hội thoại NPC");
		}
		else
		{
			// === NỘI DUNG TAB 1: ĐỔI TIÊN PHÙ ZENO ===
			int row1Y = winY + 58;
			DrawStackIcon(g, iconX, row1Y + 4);
			mFont.tahoma_7b_white.drawString(g, "Số lượng mỗi đợt:", labelX, row1Y + 3, mFont.LEFT);
			DrawInputBox(g, inputX, row1Y, 58, 18, inputQuantity.ToString(), isGreenText: true);

			int row2Y = winY + 79;
			DrawCartIcon(g, iconX, row2Y + 4);
			mFont.tahoma_7b_white.drawString(g, "Số đợt cần đổi:", labelX, row2Y + 3, mFont.LEFT);
			DrawInputBox(g, inputX, row2Y, 58, 18, targetBatchesZeno.ToString(), isGreenText: true);

			int row3Y = winY + 100;
			DrawClockIcon(g, iconX, row3Y + 4);
			mFont.tahoma_7b_white.drawString(g, "Độ trễ mỗi đợt:", labelX, row3Y + 3, mFont.LEFT);
			DrawInputBox(g, inputX, row3Y, 58, 18, delayZenoMs + " ms", isGreenText: false);

			string totalZenoCalc = (targetBatchesZeno > 0) ? (NinjaUtil.getMoneys((long)inputQuantity * (long)targetBatchesZeno) + " cái") : "Vô hạn";
			mFont.tahoma_7_yellow.drawString(g, "(Mỗi đợt: " + inputQuantity + " - Tổng đổi: " + totalZenoCalc + ")", winX + winW / 2, winY + 121, mFont.CENTER);
			mFont.tahoma_7_white.drawString(g, "(Tự chọn [3] -> [0] -> Bắn số lượng ngầm)", winX + winW / 2, winY + 133, mFont.CENTER);

			DrawCheckbox(g, winX + 11, winY + 147, hideUi, "Ẩn hoàn toàn Menu & Ô nhập số lượng");
		}

		// Thanh tiến độ (Progress Bar)
		int progY = winY + 163;
		mFont.tahoma_7b_white.drawString(g, "Tiến độ :", winX + 11, progY + 2, mFont.LEFT);
		int barX = winX + 54;
		int barW = winW - 64;
		int barH = 16;
		g.setColor(0x18100A);
		g.fillRect(barX, progY, barW, barH, 3);

		float ratio = 0f;
		string progStr;
		if (selectedTab == 0)
		{
			if (targetBatchesBanh > 0)
			{
				ratio = Mathf.Clamp01((float)completedCount / (float)targetBatchesBanh);
				progStr = "Đã đổi: " + completedCount + " / " + targetBatchesBanh + " lượt (" + (int)(ratio * 100f) + "%)";
			}
			else
			{
				ratio = (isAutoRunning && autoType == 1) ? 1f : 0f;
				progStr = "Đã đổi: " + completedCount + " lượt";
			}
		}
		else
		{
			int totalTargetZeno = (targetBatchesZeno > 0) ? (inputQuantity * targetBatchesZeno) : 0;
			if (totalTargetZeno > 0)
			{
				ratio = Mathf.Clamp01((float)totalZenoBought / (float)totalTargetZeno);
				progStr = "Đã đổi: " + NinjaUtil.getMoneys(totalZenoBought) + " / " + NinjaUtil.getMoneys(totalTargetZeno) + " (" + (int)(ratio * 100f) + "%)";
			}
			else
			{
				ratio = (isAutoRunning && autoType == 2) ? 1f : 0f;
				progStr = "Đã đổi: " + NinjaUtil.getMoneys(totalZenoBought) + " cái";
			}
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
		mFont.tahoma_7b_white.drawStringBorder(g, progStr, barX + barW / 2, progY + 2, mFont.CENTER, mFont.tahoma_7_grey);

		// 2 Nút bấm xếp dọc: BẮT ĐẦU CHẠY & DỪNG LẠI (Phím A)
		int btnX = winX + 16;
		int btnW = winW - 32;
		int btnH = 19;
		int startBtnY = winY + 184;
		int stopBtnY = winY + 206;

		DrawPillButton(g, btnX, startBtnY, btnW, btnH, isGreen: true, isAutoRunning);
		string startLabel = isAutoRunning ? "ĐANG CHẠY AUTO..." : ((selectedTab == 0) ? "BẮT ĐẦU ĐỔI ĐIỂM" : "BẮT ĐẦU ĐỔI ZENO");
		int startTextW = mFont.tahoma_7b_white.getWidth(startLabel);
		int startIconX = btnX + (btnW - startTextW - 10) / 2;
		DrawPlayIcon(g, startIconX, startBtnY + 5);
		mFont.tahoma_7b_white.drawStringBorder(g, startLabel, startIconX + 10, startBtnY + 3, mFont.LEFT, mFont.tahoma_7_grey);

		DrawPillButton(g, btnX, stopBtnY, btnW, btnH, isGreen: false, isActive: false);
		string stopLabel = "DỪNG LẠI (Phím A)";
		int stopTextW = mFont.tahoma_7b_white.getWidth(stopLabel);
		int stopIconX = btnX + (btnW - stopTextW - 10) / 2;
		DrawStopIcon(g, stopIconX, stopBtnY + 6);
		mFont.tahoma_7b_white.drawStringBorder(g, stopLabel, stopIconX + 10, stopBtnY + 3, mFont.LEFT, mFont.tahoma_7_grey);
	}

	private void DrawTabButton(mGraphics g, int x, int y, int w, int h, string text, bool isSelected)
	{
		g.setColor(isSelected ? 0x8D4918 : 0x261409);
		g.fillRect(x, y, w, h, 3);
		if (isSelected)
		{
			g.setColor(0xA8581E);
			g.fillRect(x + 1, y + 1, w - 2, (h - 2) / 2, 2);
		}
		g.setColor(isSelected ? 0xF7CE68 : 0x6E4720);
		g.drawRect(x, y, w - 1, h - 1);
		if (isSelected)
		{
			mFont.tahoma_7b_yellow.drawStringBorder(g, text, x + w / 2, y + 3, mFont.CENTER, mFont.tahoma_7_grey);
		}
		else
		{
			mFont.tahoma_7b_white.drawString(g, text, x + w / 2, y + 3, mFont.CENTER);
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

	public bool HandleChatCommand(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string trimmed = text.Trim();
		string lower = trimmed.ToLower();

		if (lower == "spy" || lower == "logmenu")
		{
			isSpyEnabled = !isSpyEnabled;
			isLogToFile = isSpyEnabled;
			GameScr.info1.addInfo("Menu Spy: " + (isSpyEnabled ? "[BẬT] (Ghi log vào menu_spy.txt)" : "[TẮT] (Không ghi log)"), 0);
			return true;
		}

		if (lower == "anui" || lower == "hideui")
		{
			hideUi = true;
			GameScr.info1.addInfo("Ẩn giao diện Auto Menu: [BẬT]", 0);
			return true;
		}

		if (lower == "hienui" || lower == "showui")
		{
			hideUi = false;
			GameScr.info1.addInfo("Ẩn giao diện Auto Menu: [TẮT] (Sẽ hiện menu khi chạy)", 0);
			return true;
		}

		if (lower == "stop" || lower == "dung" || lower == "dungmenu")
		{
			StopAuto("Đã dừng Auto bằng lệnh");
			return true;
		}

		// 1. Lệnh Nồi Bánh (ID 66):
		if (lower.StartsWith("autobanh") || lower.StartsWith("doidiem"))
		{
			string pStr = lower.Replace("autobanh", "").Replace("doidiem", "").Trim();
			int moc = pointIndex;
			int d = delayBanhMs;
			if (!string.IsNullOrEmpty(pStr))
			{
				string[] arr = pStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (arr.Length >= 1 && int.TryParse(arr[0], out var mVal))
				{
					moc = mVal;
				}
				if (arr.Length >= 2 && int.TryParse(arr[1], out var dVal))
				{
					d = dVal;
				}
				StartAutoBanh(moc, d, targetBatchesBanh);
			}
			else
			{
				ToggleWindow(0);
			}
			return true;
		}

		// 2. Lệnh Thần Mèo Karin (ID 18) Đổi Tiên Phù Zeno:
		if (lower.StartsWith("autozeno") || lower.StartsWith("doizeno") || lower.StartsWith("tienphu"))
		{
			string pStr = lower.Replace("autozeno", "").Replace("doizeno", "").Replace("tienphu", "").Trim();
			int qty = inputQuantity;
			int d = delayZenoMs;
			if (!string.IsNullOrEmpty(pStr))
			{
				string[] arr = pStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (arr.Length >= 1 && int.TryParse(arr[0], out var qVal))
				{
					qty = qVal;
				}
				if (arr.Length >= 2 && int.TryParse(arr[1], out var dVal))
				{
					d = dVal;
				}
				StartAutoZeno(qty, d, targetBatchesZeno);
			}
			else
			{
				ToggleWindow(1);
			}
			return true;
		}

		// 3. Lệnh Auto chuỗi tuỳ biến cho NPC bất kỳ:
		if (lower.StartsWith("auto "))
		{
			string[] parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 3 && int.TryParse(parts[1], out var npcId))
			{
				var stepList = new System.Collections.Generic.List<int>();
				int d = 350;
				for (int i = 2; i < parts.Length; i++)
				{
					if (int.TryParse(parts[i], out var val))
					{
						if (i == parts.Length - 1 && val >= 100 && stepList.Count > 0)
						{
							d = val;
						}
						else
						{
							stepList.Add(val);
						}
					}
				}
				if (stepList.Count > 0)
				{
					string name = (npcId == 66) ? "Nồi Bánh" : ((npcId == 18) ? "Thần Mèo Karin" : ("NPC " + npcId));
					StartAutoCustom(npcId, name, stepList.ToArray(), d);
					return true;
				}
			}
			GameScr.info1.addInfo("Cú pháp: auto <npcId> <lựa_chọn_1> [lựa_chọn_2...] [delay_ms]", 0);
			return true;
		}

		return false;
	}
}
