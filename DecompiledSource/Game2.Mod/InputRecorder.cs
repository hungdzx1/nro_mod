using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Game2.Mod;

public static class InputRecorder
{
	private enum Kind : byte
	{
		PointerDown,
		PointerDrag,
		PointerUp,
		KeyDown,
		KeyUp
	}

	private struct Ev
	{
		public long t;

		public Kind kind;

		public int a;

		public int b;

		public bool world;
	}

	private const string TabName = "tab2";

	private const long LoopGapMs = 500L;

	private static readonly List<Ev> events = new List<Ev>();

	private static readonly HashSet<int> downKeys = new HashSet<int>();

	private static bool pointerDown;

	private static bool curWorld;

	private static int lastDownFrame = -1;

	private static int lastDragX;

	private static int lastDragY;

	private static bool chatWasShown;

	private static long chatOpenT = -1L;

	private static long recStart;

	private static long duration;

	private static int startMap = -1;

	private static int recW;

	private static int recH;

	private static int playIndex;

	private static long playStart;

	private static int loopsDone;

	private static int loopsTarget;

	public static bool IsRecording { get; private set; }

	public static bool IsPlaying { get; private set; }

	public static int RecKey { get; private set; }

	public static int ReplayKey { get; private set; }

	public static string CurrentName { get; private set; } = "1";


	private static string LegacyPath => Path.Combine(Application.persistentDataPath, "record_tab2.txt");

	private static string FilePath
	{
		get
		{
			string text = PathFor(CurrentName);
			if (!File.Exists(text) && CurrentName == "1" && File.Exists(LegacyPath))
			{
				return LegacyPath;
			}
			return text;
		}
	}

	private static void Info(string s)
	{
		if (GameScr.info1 != null)
		{
			GameScr.info1.addInfo(s, 0);
		}
	}

	public static void LoadHotkeys()
	{
		string text = CleanName(Rms.loadRMSString("macroName_tab2"));
		if (text.Length > 0)
		{
			CurrentName = text;
		}
		RecKey = System.Math.Max(0, Rms.loadRMSInt("hkRec_tab2"));
		ReplayKey = System.Math.Max(0, Rms.loadRMSInt("hkReplay_tab2"));
	}

	public static void SetRecKey(int key)
	{
		RecKey = key;
		Rms.saveRMSInt("hkRec_tab2", key);
	}

	public static void SetReplayKey(int key)
	{
		ReplayKey = key;
		Rms.saveRMSInt("hkReplay_tab2", key);
	}

	public static string KeyName(int key)
	{
		if (key <= 0)
		{
			return "chưa gán";
		}
		return ((char)key).ToString().ToUpper();
	}

	public static void ToggleRecord()
	{
		if (IsRecording)
		{
			StopRecord();
		}
		else
		{
			StartRecord();
		}
	}

	public static void ToggleReplay()
	{
		if (IsPlaying)
		{
			StopPlay("Đã dừng phát lại");
		}
		else
		{
			StartPlay(0);
		}
	}

	public static bool HandleKey(int key)
	{
		if (key <= 0)
		{
			return false;
		}
		if (IsRecording && (key == RecKey || key == ReplayKey))
		{
			int num = events.Count - 1;
			if (num >= 0 && events[num].kind == Kind.KeyDown && events[num].a == key)
			{
				chatOpenT = events[num].t;
				events.RemoveAt(num);
			}
			else
			{
				chatOpenT = mSystem.currentTimeMillis() - recStart;
			}
			downKeys.Remove(key);
		}
		if (key == RecKey)
		{
			if (IsRecording)
			{
				StopRecord();
			}
			else
			{
				StartRecord();
			}
			return true;
		}
		if (key == ReplayKey)
		{
			if (IsPlaying)
			{
				StopPlay("Đã dừng phát lại");
			}
			else
			{
				StartPlay(0);
			}
			return true;
		}
		return false;
	}

	public static bool HandleChat(string text)
	{
		if (text == null || ChatTextField.gI().strChat == "Tên bản ghi")
		{
			return false;
		}
		string[] array = text.Trim().ToLower().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return false;
		}
		switch (array[0])
		{
		case "rec":
			if (array.Length >= 2 && !SetName(array[1]))
			{
				return true;
			}
			StartRecord();
			return true;
		case "stoprec":
			StopRecord();
			return true;
		case "stopreplay":
			StopPlay("Đã dừng phát lại");
			return true;
		case "replay":
		{
			int result = 0;
			if ((array.Length != 2 || !int.TryParse(array[1], out result)) && array.Length >= 2)
			{
				if (!SetName(array[1]))
				{
					return true;
				}
				if (array.Length >= 3 && !int.TryParse(array[2], out result))
				{
					Info("Cú pháp: replay [tên] [số vòng]");
					return true;
				}
			}
			StartPlay(result);
			return true;
		}
		case "reclist":
			Info("Bản ghi: " + string.Join(", ", ListNames().ToArray()) + " (đang chọn: " + CurrentName + ")");
			return true;
		case "recsel":
			if (array.Length == 2)
			{
				SetName(array[1]);
			}
			else
			{
				Info("Cú pháp: recsel [tên]");
			}
			return true;
		case "recdel":
			DeleteCurrent();
			return true;
		case "recexport":
			ExportToClipboard();
			return true;
		case "recimport":
			ImportFromClipboard();
			return true;
		default:
			return false;
		}
	}

	private static string PathFor(string name)
	{
		return Path.Combine(Application.persistentDataPath, "record_tab2_" + name + ".txt");
	}

	public static string CleanName(string s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = (s ?? string.Empty).Trim().ToLower();
		foreach (char c in text)
		{
			if ((char.IsLetterOrDigit(c) || c == '_' || c == '-') && stringBuilder.Length < 20)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public static List<string> ListNames()
	{
		List<string> list = new List<string>();
		try
		{
			string text = "record_tab2_";
			string[] files = Directory.GetFiles(Application.persistentDataPath, text + "*.txt");
			foreach (string path in files)
			{
				list.Add(Path.GetFileNameWithoutExtension(path).Substring(text.Length));
			}
			if (File.Exists(LegacyPath) && !list.Contains("1"))
			{
				list.Add("1");
			}
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.ListNames: " + ex.Message);
		}
		list.Sort();
		return list;
	}

	public static bool SetName(string name)
	{
		name = CleanName(name);
		if (name.Length == 0)
		{
			Info("Tên không hợp lệ (chỉ a-z, 0-9, _ -)");
			return false;
		}
		if (name == CurrentName)
		{
			return true;
		}
		if (IsRecording || IsPlaying)
		{
			Info("Hãy dừng ghi/phát lại trước khi đổi bản ghi");
			return false;
		}
		CurrentName = name;
		events.Clear();
		duration = 0L;
		startMap = -1;
		recW = 0;
		recH = 0;
		Rms.saveRMSString("macroName_tab2", name);
		Info("Bản ghi đang chọn: " + name + (File.Exists(FilePath) ? string.Empty : " (mới)"));
		return true;
	}

	public static void DeleteCurrent()
	{
		if (IsRecording || IsPlaying)
		{
			Info("Hãy dừng ghi/phát lại trước khi xóa");
			return;
		}
		try
		{
			File.Delete(PathFor(CurrentName));
			if (CurrentName == "1")
			{
				File.Delete(LegacyPath);
			}
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.Delete: " + ex.Message);
		}
		events.Clear();
		Info("Đã xóa bản ghi " + CurrentName);
	}

	public static void StartRecord()
	{
		StopPlay(null);
		events.Clear();
		downKeys.Clear();
		pointerDown = false;
		lastDragX = (lastDragY = int.MinValue);
		chatWasShown = IsChatOpen();
		chatOpenT = -1L;
		startMap = TileMap.mapID;
		recW = GameCanvas.w;
		recH = GameCanvas.h;
		recStart = mSystem.currentTimeMillis();
		IsRecording = true;
		Info("Bắt đầu ghi [" + CurrentName + "] (gõ stoprec để dừng)");
	}

	public static void StopRecord()
	{
		if (!IsRecording)
		{
			Info("Chưa bật ghi");
			return;
		}
		IsRecording = false;
		duration = ((chatOpenT >= 0) ? chatOpenT : (mSystem.currentTimeMillis() - recStart));
		chatOpenT = -1L;
		events.RemoveAll((Ev e) => e.t >= duration);
		Save();
		Info("Đã lưu [" + CurrentName + "]: " + events.Count + " thao tác, " + duration / 1000 + "s");
	}

	private static void Add(Kind kind, int a, int b, bool world = false)
	{
		Ev item = default(Ev);
		item.world = world;
		item.t = mSystem.currentTimeMillis() - recStart;
		item.kind = kind;
		item.a = a;
		item.b = b;
		events.Add(item);
	}

	private static bool IsChatOpen()
	{
		if (ChatTextField.gI().isShow || ModFunc.isShowMenuChat || (GameCanvas.menu.showMenu && ModFunc.modMenuOpen))
		{
			return true;
		}
		Panel panel = GameCanvas.panel;
		if (panel != null && panel.chatTField != null)
		{
			return panel.chatTField.isShow;
		}
		return false;
	}

	private static bool CanRecord()
	{
		if (IsRecording && !IsPlaying)
		{
			return !IsChatOpen();
		}
		return false;
	}

	private static bool HitsWorldObject(int x, int y)
	{
		if (GameCanvas.currentScreen != GameScr.instance || GameCanvas.panel.isShow || GameCanvas.currentDialog != null)
		{
			return false;
		}
		return GameScr.gI().IsWorldObjectAt(x, y);
	}

	private static void AddPointer(Kind kind, int x, int y)
	{
		if (curWorld)
		{
			Add(kind, x + GameScr.cmx, y + GameScr.cmy, world: true);
		}
		else
		{
			Add(kind, x, y);
		}
	}

	public static void OnPointerPressed(int x, int y)
	{
		if (CanRecord() && (!pointerDown || Time.frameCount != lastDownFrame))
		{
			lastDownFrame = Time.frameCount;
			if (pointerDown)
			{
				AddPointer(Kind.PointerUp, lastDragX, lastDragY);
			}
			pointerDown = true;
			curWorld = HitsWorldObject(x, y);
			lastDragX = x;
			lastDragY = y;
			AddPointer(Kind.PointerDown, x, y);
		}
	}

	public static void OnPointerDragged(int x, int y)
	{
		if (CanRecord() && pointerDown && (x != lastDragX || y != lastDragY))
		{
			lastDragX = x;
			lastDragY = y;
			AddPointer(Kind.PointerDrag, x, y);
		}
	}

	public static void OnPointerReleased(int x, int y)
	{
		if (IsRecording && !IsPlaying && pointerDown)
		{
			pointerDown = false;
			AddPointer(Kind.PointerUp, x, y);
		}
	}

	public static void OnKeyPressed(int key)
	{
		if (CanRecord() && downKeys.Add(key))
		{
			Add(Kind.KeyDown, key, 0);
		}
	}

	public static void OnKeyReleased(int key)
	{
		if (IsRecording && !IsPlaying && downKeys.Remove(key))
		{
			Add(Kind.KeyUp, key, 0);
		}
	}

	private static void TrimChatOpenAction()
	{
		chatOpenT = mSystem.currentTimeMillis() - recStart;
		int num = events.Count - 1;
		while (num >= 0 && events[num].kind != 0 && events[num].kind != Kind.KeyDown)
		{
			num--;
		}
		if (num >= 0)
		{
			chatOpenT = events[num].t;
			events.RemoveRange(num, events.Count - num);
		}
		pointerDown = false;
		downKeys.Clear();
	}

	public static void StartPlay(int loops)
	{
		if (IsRecording)
		{
			StopRecord();
		}
		if (events.Count == 0 && !Load())
		{
			Info("Chưa có bản ghi [" + CurrentName + "] (gõ rec trước)");
			return;
		}
		if (startMap >= 0 && TileMap.mapID != startMap)
		{
			Info("Hãy đứng đúng map lúc ghi (map " + startMap + ") rồi replay");
			return;
		}
		if (recW > 0 && (recW != GameCanvas.w || recH != GameCanvas.h))
		{
			Info("Bản ghi từ màn hình " + recW + "x" + recH + ", máy này " + GameCanvas.w + "x" + GameCanvas.h + ": click vào nút giao diện có thể lệch");
		}
		loopsTarget = ((loops >= 0) ? loops : 0);
		loopsDone = 0;
		playIndex = 0;
		playStart = mSystem.currentTimeMillis();
		downKeys.Clear();
		pointerDown = false;
		IsPlaying = true;
		Info("Phát lại [" + CurrentName + "] " + ((loopsTarget == 0) ? "vô hạn" : (loopsTarget + " vòng")) + " (gõ stopreplay để dừng)");
	}

	public static void StopPlay(string msg)
	{
		if (IsPlaying)
		{
			IsPlaying = false;
			ReleaseAll();
			if (msg != null)
			{
				Info(msg);
			}
		}
	}

	private static void ReleaseAll()
	{
		GameCanvas gameCanvas = GameMidlet.gameCanvas;
		if (gameCanvas == null)
		{
			return;
		}
		if (pointerDown)
		{
			gameCanvas.pointerReleased(lastDragX, lastDragY);
		}
		foreach (int downKey in downKeys)
		{
			gameCanvas.keyReleasedz(downKey);
		}
		pointerDown = false;
		downKeys.Clear();
	}

	private static void Dispatch(Ev ev)
	{
		GameCanvas gameCanvas = GameMidlet.gameCanvas;
		if (ev.world)
		{
			ev.a -= GameScr.cmx;
			ev.b -= GameScr.cmy;
		}
		if (ev.kind == Kind.PointerDown || ev.kind == Kind.PointerUp)
		{
			Debug.Log("[Macro] " + ev.kind.ToString() + " " + ev.a + "," + ev.b + " t=" + ev.t + " real=" + (mSystem.currentTimeMillis() - playStart));
		}
		switch (ev.kind)
		{
		case Kind.PointerDown:
			pointerDown = true;
			lastDragX = ev.a;
			lastDragY = ev.b;
			gameCanvas.pointerPressed(ev.a, ev.b);
			break;
		case Kind.PointerDrag:
			lastDragX = ev.a;
			lastDragY = ev.b;
			gameCanvas.pointerDragged(ev.a, ev.b);
			break;
		case Kind.PointerUp:
			pointerDown = false;
			gameCanvas.pointerReleased(ev.a, ev.b);
			break;
		case Kind.KeyDown:
			downKeys.Add(ev.a);
			gameCanvas.keyPressedz(ev.a);
			break;
		case Kind.KeyUp:
			downKeys.Remove(ev.a);
			gameCanvas.keyReleasedz(ev.a);
			break;
		}
	}

	public static void Tick()
	{
		if (!GameCanvas.menu.showMenu)
		{
			ModFunc.modMenuOpen = false;
		}
		if (IsRecording)
		{
			bool flag = IsChatOpen();
			if (flag && !chatWasShown)
			{
				TrimChatOpenAction();
			}
			else if (!flag && chatWasShown)
			{
				chatOpenT = -1L;
			}
			chatWasShown = flag;
		}
		else
		{
			if (!IsPlaying || GameMidlet.gameCanvas == null)
			{
				return;
			}
			long num = mSystem.currentTimeMillis() - playStart;
			while (playIndex < events.Count && events[playIndex].t <= num)
			{
				Ev ev = events[playIndex];
				Dispatch(ev);
				playIndex++;
				if (ev.kind != Kind.PointerDrag)
				{
					break;
				}
			}
			if (pointerDown)
			{
				GameMidlet.gameCanvas.pointerDragged(lastDragX, lastDragY);
			}
			if (playIndex >= events.Count && num >= duration)
			{
				ReleaseAll();
				loopsDone++;
				if (loopsTarget > 0 && loopsDone >= loopsTarget)
				{
					StopPlay("Phát lại xong " + loopsDone + " vòng");
					return;
				}
				if (startMap >= 0 && TileMap.mapID != startMap)
				{
					StopPlay("Đã dừng: sai map so với lúc ghi");
					return;
				}
				playIndex = 0;
				playStart = mSystem.currentTimeMillis() + 500;
			}
		}
	}

	private static void Save()
	{
		try
		{
			File.WriteAllText(PathFor(CurrentName), BuildText());
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.Save: " + ex.Message);
		}
	}

	private static bool Load()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				return false;
			}
			if (!TryParse(File.ReadAllLines(FilePath), out var list, out var dur, out var map, out var w, out var h))
			{
				return false;
			}
			duration = dur;
			startMap = map;
			recW = w;
			recH = h;
			events.Clear();
			events.AddRange(list);
			return true;
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.Load: " + ex.Message);
			return false;
		}
	}

	public static void ExportToClipboard()
	{
		if (IsRecording || IsPlaying)
		{
			Info("Hãy dừng ghi/phát lại trước khi xuất");
			return;
		}
		if (events.Count == 0 && !Load())
		{
			Info("Chưa có bản ghi [" + CurrentName + "] để xuất");
			return;
		}
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(BuildText());
			byte[] inArray;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
				{
					gZipStream.Write(bytes, 0, bytes.Length);
				}
				inArray = memoryStream.ToArray();
			}
			string text2 = (GUIUtility.systemCopyBuffer = "MACRO1|" + CurrentName + "|" + Convert.ToBase64String(inArray));
			Info("Đã sao chép mã bản ghi [" + CurrentName + "] (" + text2.Length + " ký tự). Dán vào nơi muốn gửi");
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.Export: " + ex.Message);
			Info("Xuất bản ghi thất bại");
		}
	}

	public static void ImportFromClipboard()
	{
		if (IsRecording || IsPlaying)
		{
			Info("Hãy dừng ghi/phát lại trước khi nhập");
			return;
		}
		string[] array = (GUIUtility.systemCopyBuffer ?? string.Empty).Trim().Split('|');
		if (array.Length != 3 || array[0] != "MACRO1")
		{
			Info("Clipboard không có mã bản ghi hợp lệ (MACRO1|...)");
			return;
		}
		try
		{
			byte[] array2 = Convert.FromBase64String(array[2]);
			if (array2.Length > 2000000)
			{
				Info("Mã bản ghi quá lớn");
				return;
			}
			string @string;
			using (MemoryStream stream = new MemoryStream(array2))
			{
				using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
				using MemoryStream memoryStream = new MemoryStream();
				byte[] array3 = new byte[8192];
				int count;
				while ((count = gZipStream.Read(array3, 0, array3.Length)) > 0)
				{
					memoryStream.Write(array3, 0, count);
					if (memoryStream.Length > 8000000)
					{
						Info("Mã bản ghi quá lớn");
						return;
					}
				}
				@string = Encoding.UTF8.GetString(memoryStream.ToArray());
			}
			string[] array4 = @string.Replace("\r", string.Empty).Split('\n');
			if (!TryParse(array4, out var list, out var _, out var map, out var _, out var _))
			{
				Info("Mã bản ghi không đúng định dạng");
				return;
			}
			string text = CleanName(array[1]);
			if (text.Length == 0)
			{
				text = "nhap";
			}
			string text2 = text;
			int num = 2;
			while (File.Exists(PathFor(text2)))
			{
				text2 = text + "_" + num;
				num++;
			}
			File.WriteAllText(PathFor(text2), string.Join("\n", array4));
			SetName(text2);
			Info("Đã nhập bản ghi [" + text2 + "]: " + list.Count + " thao tác. Đứng đúng map " + map + " rồi phát lại");
		}
		catch (Exception ex)
		{
			Debug.Log("InputRecorder.Import: " + ex.Message);
			Info("Nhập bản ghi thất bại (mã sai hoặc hỏng)");
		}
	}

	private static string BuildText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("v1 ").Append(duration).Append(' ')
			.Append(startMap)
			.Append(' ')
			.Append(recW)
			.Append(' ')
			.Append(recH)
			.Append('\n');
		foreach (Ev @event in events)
		{
			stringBuilder.Append(@event.t).Append(' ').Append((int)@event.kind)
				.Append(' ')
				.Append(@event.a)
				.Append(' ')
				.Append(@event.b)
				.Append(' ')
				.Append(@event.world ? 1 : 0)
				.Append('\n');
		}
		return stringBuilder.ToString();
	}

	private static bool TryParse(string[] lines, out List<Ev> list, out long dur, out int map, out int w, out int h)
	{
		list = new List<Ev>();
		dur = 0L;
		map = -1;
		w = 0;
		h = 0;
		if (lines == null || lines.Length == 0 || lines.Length > 200000)
		{
			return false;
		}
		string[] array = lines[0].Trim().Split(' ');
		if (array.Length < 3 || array[0] != "v1")
		{
			return false;
		}
		if (!long.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dur) || dur < 0 || !int.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out map))
		{
			return false;
		}
		if (array.Length >= 5)
		{
			int.TryParse(array[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out w);
			int.TryParse(array[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out h);
		}
		for (int i = 1; i < lines.Length; i++)
		{
			string text = lines[i].Trim();
			if (text.Length != 0)
			{
				string[] array2 = text.Split(' ');
				Ev item = default(Ev);
				if (array2.Length < 4 || !long.TryParse(array2[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out item.t) || !int.TryParse(array2[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < 0 || result > 4 || !int.TryParse(array2[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out item.a) || !int.TryParse(array2[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out item.b))
				{
					return false;
				}
				item.kind = (Kind)result;
				item.world = array2.Length > 4 && array2[4] == "1";
				list.Add(item);
			}
		}
		return list.Count > 0;
	}
}
