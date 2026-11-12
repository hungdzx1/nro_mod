using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game1.Mod;

public class NpcMenuHelper
{
	private static NpcMenuHelper _instance;

	public bool isSpyEnabled;

	public bool isLogToFile;

	public bool hideUi = true; // Ẩn hoàn toàn Menu và ô nhập khi chạy Auto

	public bool isAutoRunning;

	public int autoType; // 0: Custom steps, 1: Nồi Bánh (ID 66), 2: Thần Mèo Karin Tiên Phù Zeno (ID 18)

	public int autoNpcId = -1;

	public string autoNpcName = "";

	public int[] autoSteps;

	public int currentStepIndex;

	public int pointIndex = 1; // Mốc điểm Nồi Bánh: 0: 10tr, 1: 5k, 2: 3k, 3: 200, 4: 50

	public int inputQuantity = 1000; // Số lượng Tiên Phù cần đổi mỗi đợt ở Thần Mèo Karin

	public int delayMs = 350;

	public long lastActionTime;

	public int completedCount;

	public int state; // 0: Idle, 1: WaitMenu1, 2: WaitMenu2, 3: WaitInput, 4: Cooldown

	public static NpcMenuHelper gI()
	{
		if (_instance == null)
		{
			_instance = new NpcMenuHelper();
		}
		return _instance;
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
			// Nhận diện Menu cấp 1 (có chứa "Đổi điểm")
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
				// Menu cấp 2: Chọn mốc điểm pointIndex (0: 10tr, 1: 5k, 2: 3k, 3: 200, 4: 50)
				int pick = (pointIndex >= 0 && pointIndex < menu.Length) ? pointIndex : 1;
				LogToFile("-> Auto Nồi Bánh: Chọn [" + pick + "] " + menu[pick]);
				completedCount++;
				GameScr.info1.addInfo("Đã đổi Nồi Bánh đợt " + completedCount + " (" + menu[pick].Replace("\n", " ") + ")", 0);
				state = 4; // Chuyển sang Cooldown
				lastActionTime = mSystem.currentTimeMillis();
				Service.gI().confirmMenu(66, (sbyte)pick);
				return hideUi;
			}
		}

		// === AUTO TYPE 2: THẦN MÈO KARIN - TIÊN PHÙ ZENO (ID 18) ===
		if (autoType == 2 && npcTempId == 18)
		{
			// Nhận diện Menu cấp 1 (có chứa "Đổi Tiên Phù" hoặc "Thức tỉnh")
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
				// Menu cấp 2: Chọn [0] Tiên Phù Zeno
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
				GameScr.info1.addInfo("Đã đổi " + qty + " Tiên Phù Zeno (Đợt " + completedCount + ")", 0);
				LogToFile("-> Auto Thần Mèo: Bắn ngầm số lượng = " + qty);

				state = 4; // Chuyển sang Cooldown
				lastActionTime = mSystem.currentTimeMillis();
				return hideUi; // ẨN HOÀN TOÀN Ô NHẬP CLIENTINPUT
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

	public void StartAutoBanh(int pointMoc = 1, int delay = 350)
	{
		autoType = 1;
		autoNpcId = 66;
		autoNpcName = "Nồi Bánh";
		pointIndex = pointMoc;
		delayMs = System.Math.Max(150, delay);
		completedCount = 0;
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		string mocName = (pointMoc == 0) ? "10.000.000đ" : ((pointMoc == 1) ? "5.000đ" : ((pointMoc == 2) ? "3.000đ" : ((pointMoc == 3) ? "200đ" : "50đ")));
		GameScr.info1.addInfo("BẮT ĐẦU AUTO NỒI BÁNH: Đổi mốc [" + pointMoc + "] " + mocName + " (Ẩn UI)", 0);
		LogToFile(">>> BẮT ĐẦU AUTO NỒI BÁNH: Mốc [" + pointMoc + "] " + mocName + " - Delay: " + delayMs + "ms");

		TriggerOpenNpcMenu();
	}

	public void StartAutoZeno(int qty = 1000, int delay = 350)
	{
		autoType = 2;
		autoNpcId = 18;
		autoNpcName = "Thần Mèo Karin";
		inputQuantity = (qty <= 0) ? 1000 : qty;
		delayMs = System.Math.Max(150, delay);
		completedCount = 0;
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		GameScr.info1.addInfo("BẮT ĐẦU AUTO ĐỔI TIÊN PHÙ: " + inputQuantity + " cái/đợt (Ẩn UI)", 0);
		LogToFile(">>> BẮT ĐẦU AUTO TIÊN PHÙ ZENO: " + inputQuantity + " cái/đợt - Delay: " + delayMs + "ms");

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
		delayMs = System.Math.Max(150, delay);
		isAutoRunning = true;
		state = 1;
		lastActionTime = mSystem.currentTimeMillis();

		if (GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}
		ChatTextField.gI().isShow = false;

		string stepsStr = string.Join(" -> ", Array.ConvertAll(steps, s => "[" + s + "]"));
		GameScr.info1.addInfo("Bắt đầu Auto: " + npcName + " (ID: " + npcId + ") Chuỗi: " + stepsStr + " (Ẩn UI)", 0);
		LogToFile(">>> BẮT ĐẦU AUTO CUSTOM: " + npcName + " (ID: " + npcId + ") Chuỗi: " + stepsStr + " - Delay: " + delayMs + "ms");

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
			// Tự động nhảy lại sát bên cạnh NPC (cách 20px) nếu đang đứng xa hơn 40px
			if (System.Math.Abs(targetNpc.cx - Char.myCharz().cx) > 40 || System.Math.Abs(targetNpc.cy - Char.myCharz().cy) > 40)
			{
				Char.myCharz().cx = targetNpc.cx + 20;
				Char.myCharz().cy = targetNpc.cy;
				Service.gI().charMove();
			}
			Char.myCharz().npcFocus = targetNpc;
		}
		// Gửi đúng mã npcTemplateId (66 hoặc 18) để Server mở menu
		Service.gI().openMenu(autoNpcId);
	}

	public void StopAuto(string reason = "Đã dừng Auto")
	{
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
			LogToFile("<<< DỪNG AUTO: " + reason + " - Tổng hoàn thành: " + completedCount);
		}
	}

	public void Update()
	{
		if (isAutoRunning && !ChatTextField.gI().isShow && Input.GetKey(KeyCode.A))
		{
			StopAuto("Đã dừng Auto (Phím A)");
			return;
		}

		if (!isAutoRunning)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();

		// Nếu đang ở trạng thái Cooldown giữa các đợt:
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

		// Timeout bảo vệ (nếu server bị lag quá 2.5s không phản hồi Menu hoặc Input):
		if ((state == 1 || state == 2 || state == 3) && now - lastActionTime >= 2500)
		{
			state = 1;
			lastActionTime = now;
			TriggerOpenNpcMenu();
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
		// autobanh [mốc_điểm: 0..4] [delay_ms]
		if (lower.StartsWith("autobanh") || lower.StartsWith("doidiem"))
		{
			string pStr = lower.Replace("autobanh", "").Replace("doidiem", "").Trim();
			int moc = 1;
			int d = 350;
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
			}
			StartAutoBanh(moc, d);
			return true;
		}

		// 2. Lệnh Thần Mèo Karin (ID 18) Đổi Tiên Phù Zeno:
		// autozeno [số_lượng] [delay_ms]
		if (lower.StartsWith("autozeno") || lower.StartsWith("doizeno") || lower.StartsWith("tienphu"))
		{
			string pStr = lower.Replace("autozeno", "").Replace("doizeno", "").Replace("tienphu", "").Trim();
			int qty = 1000;
			int d = 350;
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
			}
			StartAutoZeno(qty, d);
			return true;
		}

		// 3. Lệnh Auto chuỗi tuỳ biến cho NPC bất kỳ:
		// auto <npcId> <opt1> [opt2...] [delay]
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
