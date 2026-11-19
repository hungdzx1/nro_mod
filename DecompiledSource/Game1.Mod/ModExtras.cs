using System;
using System.Text;
using Game1.Mod.XMAP;
using UnityEngine;

namespace Game1.Mod;

public static class ModExtras
{
	public static bool ShowStatus = true;

	public static bool ReturnToMap = true;

	public static bool HideOthersAura;

	public static bool HideOthersBag;

	public static bool AutoLod = true;

	private const int LodTrigger = 10;

	private const int LodRelease = 7;

	private const int LodFull = 5;

	private static bool lodApplied;

	private static int lodTick;

	private static readonly Char[] nearChars = new Char[5];

	private static readonly int[] nearDist = new int[5];

	private static int lastMap = -1;

	private static int lastZone = -1;

	private static bool pendingReturn;

	private static long pendingSince;

	private static long readySince;

	private static bool xmapStarted;

	private static bool zoneRequested;

	private static int retries;

	private static bool statsStarted;

	private static long onlineMs;

	private static long lastOnlineTick;

	private static int deaths;

	private static bool wasDead;

	private static readonly string[] statLines = new string[2];

	private static long statBuilt;

	private static readonly StringBuilder sb = new StringBuilder();

	private static string hudText = string.Empty;

	private static long hudBuilt;

	private static GUIStyle sleepStyle;

	private static string sleepText = string.Empty;

	private static long sleepBuilt;

	private static bool wasSaving;

	private static bool resumeSaving;

	private static long resumeSince;

	private static long resumeReadySince;

	private static long powerSaveSince;

	private static bool skipErrorLogged;

	public static bool PowerSave { get; private set; }

	public static bool LodActive { get; private set; }

	public static bool SyncTabs => PlayerPrefs.GetInt("mxSyncTabs", 0) == 1;

	private static void Info(string s)
	{
		if (GameScr.info1 != null)
		{
			GameScr.info1.addInfo(s, 0);
		}
	}

	public static void Load()
	{
		ShowStatus = Rms.loadRMSInt("mxStatus") != 0;
		ReturnToMap = Rms.loadRMSInt("mxReturn") != 0;
		AutoLod = Rms.loadRMSInt("mxLod") != 0;
		HideOthersAura = Rms.loadRMSInt("mxHideAura") == 1;
		HideOthersBag = Rms.loadRMSInt("mxHideBag") == 1;
	}

	public static bool HandleChat(string text)
	{
		if (text == null || ChatTextField.gI().strChat == "Tên bản ghi")
		{
			return false;
		}
		switch (text.Trim().ToLower())
		{
		case "hud":
			ToggleStatus();
			return true;
		case "tietkiem":
			TogglePowerSave();
			return true;
		case "vemap":
			ToggleReturn();
			return true;
		case "lod":
			ToggleLod();
			return true;
		case "dongbo":
			ToggleSyncTabs();
			return true;
		case "anaura":
			ToggleHideAura();
			return true;
		case "andeolung":
			ToggleHideBag();
			return true;
		default:
			return false;
		}
	}

	public static void ToggleStatus()
	{
		ShowStatus = !ShowStatus;
		Rms.saveRMSInt("mxStatus", ShowStatus ? 1 : 0);
		Info("Dòng trạng thái mod: " + (ShowStatus ? "Bật" : "Tắt"));
	}

	public static void ToggleHideAura()
	{
		HideOthersAura = !HideOthersAura;
		Rms.saveRMSInt("mxHideAura", HideOthersAura ? 1 : 0);
		Info("Ẩn aura người khác: " + (HideOthersAura ? "Bật" : "Tắt"));
	}

	public static void ToggleHideBag()
	{
		HideOthersBag = !HideOthersBag;
		Rms.saveRMSInt("mxHideBag", HideOthersBag ? 1 : 0);
		Info("Ẩn đeo lưng người khác: " + (HideOthersBag ? "Bật" : "Tắt"));
	}

	public static void ToggleReturn()
	{
		ReturnToMap = !ReturnToMap;
		Rms.saveRMSInt("mxReturn", ReturnToMap ? 1 : 0);
		Info("Về map cũ sau đăng nhập lại: " + (ReturnToMap ? "Bật" : "Tắt"));
	}

	public static void Update()
	{
		Char @char = Char.myCharz();
		if (GameCanvas.currentScreen != GameScr.instance || @char == null)
		{
			return;
		}
		if (!statsStarted)
		{
			ResetStats();
		}
		long num = mSystem.currentTimeMillis();
		if (lastOnlineTick > 0 && num - lastOnlineTick < 5000)
		{
			onlineMs += num - lastOnlineTick;
		}
		lastOnlineTick = num;
		int num2;
		if (@char.cHP > 0)
		{
			num2 = ((@char.statusMe == 14) ? 1 : 0);
			if (num2 == 0)
			{
				goto IL_0085;
			}
		}
		else
		{
			num2 = 1;
		}
		if (!wasDead)
		{
			deaths++;
		}
		goto IL_0085;
		IL_0085:
		wasDead = (byte)num2 != 0;
		TrackMap(@char);
		UpdateReturn(@char);
		UpdateLod(@char);
		UpdateResumeSaving(@char);
	}

	public static void ToggleSyncTabs()
	{
		bool flag = !SyncTabs;
		PlayerPrefs.SetInt("mxSyncTabs", flag ? 1 : 0);
		PlayerPrefs.Save();
		Info("Đồng bộ cài đặt 2 tab: " + (flag ? "Bật" : "Tắt"));
		if (flag)
		{
			ModFunc.GI().PushSettingsToOtherTab();
		}
	}

	public static void ToggleLod()
	{
		AutoLod = !AutoLod;
		Rms.saveRMSInt("mxLod", AutoLod ? 1 : 0);
		Info("Tự giảm hiệu ứng khi đông người: " + (AutoLod ? "Bật" : "Tắt"));
	}

	private static void ClearLodFlags(MyVector v)
	{
		for (int i = 0; i < v.size(); i++)
		{
			Char @char = (Char)v.elementAt(i);
			if (@char != null)
			{
				@char.lodSkip = false;
			}
		}
		lodApplied = false;
		LodActive = false;
	}

	private static void UpdateLod(Char me)
	{
		lodTick++;
		if (((uint)lodTick & 3u) != 0)
		{
			return;
		}
		MyVector vCharInMap = GameScr.vCharInMap;
		if (vCharInMap == null)
		{
			return;
		}
		if (!AutoLod)
		{
			if (lodApplied)
			{
				ClearLodFlags(vCharInMap);
			}
			return;
		}
		int num = 0;
		for (int i = 0; i < vCharInMap.size(); i++)
		{
			Char @char = (Char)vCharInMap.elementAt(i);
			if (@char != null && !@char.me && @char.isPaint())
			{
				num++;
			}
		}
		LodActive = (LodActive ? (num >= 7) : (num > 10));
		if (!LodActive)
		{
			if (lodApplied)
			{
				ClearLodFlags(vCharInMap);
			}
			return;
		}
		for (int j = 0; j < 5; j++)
		{
			nearChars[j] = null;
			nearDist[j] = int.MaxValue;
		}
		for (int k = 0; k < vCharInMap.size(); k++)
		{
			Char char2 = (Char)vCharInMap.elementAt(k);
			if (char2 == null || char2.me)
			{
				continue;
			}
			if (!char2.isPaint())
			{
				char2.lodSkip = false;
				continue;
			}
			char2.lodSkip = true;
			int num2 = System.Math.Abs(char2.cx - me.cx) + System.Math.Abs(char2.cy - me.cy);
			for (int l = 0; l < 5; l++)
			{
				if (num2 < nearDist[l])
				{
					for (int num3 = 4; num3 > l; num3--)
					{
						nearDist[num3] = nearDist[num3 - 1];
						nearChars[num3] = nearChars[num3 - 1];
					}
					nearDist[l] = num2;
					nearChars[l] = char2;
					break;
				}
			}
		}
		for (int m = 0; m < 5; m++)
		{
			if (nearChars[m] != null)
			{
				nearChars[m].lodSkip = false;
			}
		}
		lodApplied = true;
	}

	private static bool IsReady(Char me)
	{
		if (!Char.isLoadingMap && !Char.ischangingMap && me.cHP > 0)
		{
			return TileMap.mapID > 0;
		}
		return false;
	}

	private static void TrackMap(Char me)
	{
		if (!pendingReturn && IsReady(me))
		{
			lastMap = TileMap.mapID;
			lastZone = TileMap.zoneID;
		}
	}

	public static void OnAutoLoginSent()
	{
		if (wasSaving)
		{
			wasSaving = false;
			resumeSaving = true;
			resumeSince = mSystem.currentTimeMillis();
			resumeReadySince = 0L;
		}
		if (ReturnToMap && lastMap > 0)
		{
			pendingReturn = true;
			pendingSince = mSystem.currentTimeMillis();
			readySince = 0L;
			xmapStarted = false;
			zoneRequested = false;
			retries = 0;
		}
	}

	private static void UpdateReturn(Char me)
	{
		if (!pendingReturn)
		{
			return;
		}
		long num = mSystem.currentTimeMillis();
		if (num - pendingSince > 240000)
		{
			pendingReturn = false;
		}
		else if (!IsReady(me))
		{
			readySince = 0L;
		}
		else if (readySince == 0L)
		{
			readySince = num;
		}
		else
		{
			if (num - readySince < 3000)
			{
				return;
			}
			if (TileMap.mapID != lastMap)
			{
				if (AutoXmap.IsXmapRunning)
				{
					return;
				}
				if (xmapStarted)
				{
					retries++;
					if (retries > 3)
					{
						pendingReturn = false;
						Info("Không về được map cũ");
						return;
					}
				}
				XmapController.StartRunToMapId(lastMap);
				xmapStarted = true;
				Info("Đang về map cũ...");
			}
			else if (lastZone >= 0 && TileMap.zoneID != lastZone && !zoneRequested)
			{
				zoneRequested = true;
				Service.gI().requestChangeZone(lastZone, -1);
				readySince = 0L;
			}
			else
			{
				pendingReturn = false;
				Info("Đã về map cũ");
			}
		}
	}

	public static void ResetStats()
	{
		if (Char.myCharz() != null)
		{
			onlineMs = 0L;
			lastOnlineTick = 0L;
			deaths = 0;
			wasDead = false;
			statsStarted = true;
			statBuilt = 0L;
		}
	}

	private static void BuildStats()
	{
		if (Char.myCharz() != null && statsStarted)
		{
			long num = onlineMs / 1000;
			statLines[0] = "Online " + num / 3600 + ":" + (num / 60 % 60).ToString("00") + ":" + (num % 60).ToString("00");
			statLines[1] = "Chết " + deaths;
		}
	}

	private static void Add(string s)
	{
		if (sb.Length > 0)
		{
			sb.Append(" | ");
		}
		sb.Append(s);
	}

	private static void BuildHud()
	{
		sb.Length = 0;
		if (InputRecorder.IsRecording)
		{
			Add("REC");
		}
		if (InputRecorder.IsPlaying)
		{
			Add("PLAY");
		}
		if (PickMob.IsAutoPickItems)
		{
			Add("NHẶT");
		}
		if (PickMob.tanSat)
		{
			Add("TS");
		}
		if (AutoXmap.IsXmapRunning)
		{
			Add("XMAP");
		}
		if (ModFunc.isAutoLogin)
		{
			Add(pendingReturn ? "ĐN→MAP" : "ĐN");
		}
		ModFunc modFunc = ModFunc.GI();
		if (modFunc.autoWakeUp)
		{
			Add("HS");
		}
		if (modFunc.isAutoVQMM)
		{
			Add("VQ");
		}
		if (modFunc.isAutoPhaLe)
		{
			Add("PHA");
		}
		if (LodActive)
		{
			Add("LOD");
		}
		if (PowerSave)
		{
			Add("TK");
		}
		else if (resumeSaving)
		{
			Add("TK…");
		}
		hudText = sb.ToString();
	}

	public static void PaintHud(mGraphics g)
	{
		if (!PowerSave && ShowStatus)
		{
			long num = mSystem.currentTimeMillis();
			if (num - hudBuilt > 500)
			{
				BuildHud();
				BuildStats();
				hudBuilt = num;
			}
			int x = ((GameScr.imgPanel != null) ? GameScr.imgPanel.getWidth() : 0) + 36;
			if (hudText.Length > 0)
			{
				mFont.tahoma_7b_yellow.drawStringBorder(g, hudText, x, 10, mFont.LEFT, mFont.tahoma_7_grey);
			}
		}
	}

	public static void TogglePowerSave()
	{
		if (PowerSave)
		{
			WakeUp();
			return;
		}
		if (GameCanvas.currentScreen != GameScr.instance)
		{
			Info("Chỉ bật tiết kiệm khi đang trong game");
			return;
		}
		ResetStats();
		wasSaving = false;
		resumeSaving = false;
		EnterPowerSave();
		Info("Tiết kiệm: bật (chạm màn hình hoặc bấm phím để thoát)");
	}

	private static void EnterPowerSave()
	{
		PowerSave = true;
		Application.targetFrameRate = (Application.isEditor ? 30 : 15);
		sleepBuilt = 0L;
		powerSaveSince = mSystem.currentTimeMillis();
		Debug.Log("[TietKiem] bat");
	}

	public static void WakeFromInput()
	{
		if (PowerSave && mSystem.currentTimeMillis() - powerSaveSince > 1500 && (Input.GetMouseButtonDown(0) || Input.anyKeyDown))
		{
			WakeUp();
		}
	}

	public static void WakeUp()
	{
		wasSaving = false;
		resumeSaving = false;
		if (PowerSave)
		{
			PowerSave = false;
			ModFunc.GI().ChangeFPSTarget();
			Debug.Log("[TietKiem] tat (nguoi choi)");
			Info("Tiết kiệm: tắt");
		}
	}

	private static void UpdateResumeSaving(Char me)
	{
		if (!resumeSaving)
		{
			if (wasSaving)
			{
				wasSaving = false;
			}
			return;
		}
		long num = mSystem.currentTimeMillis();
		if (num - resumeSince > 300000)
		{
			resumeSaving = false;
		}
		else if (pendingReturn || !IsReady(me))
		{
			resumeReadySince = 0L;
		}
		else if (resumeReadySince == 0L)
		{
			resumeReadySince = num;
		}
		else if (num - resumeReadySince >= 2000)
		{
			resumeSaving = false;
			EnterPowerSave();
			Info("Tiết kiệm: tự bật lại sau khi đăng nhập");
		}
	}

	public static bool SkipPaint()
	{
		if (!PowerSave)
		{
			return false;
		}
		if (GameCanvas.currentScreen != GameScr.instance)
		{
			PowerSave = false;
			wasSaving = true;
			Debug.Log("[TietKiem] tam tat: roi man hinh game");
			ModFunc.GI().ChangeFPSTarget();
			return false;
		}
		try
		{
			DrawSleepScreen();
			return true;
		}
		catch (Exception ex)
		{
			if (!skipErrorLogged)
			{
				skipErrorLogged = true;
				Debug.LogError("[TietKiem] loi ve man hinh tiet kiem: " + ex);
			}
			PowerSave = false;
			ModFunc.GI().ChangeFPSTarget();
			return false;
		}
	}

	private static void DrawSleepScreen()
	{
		GL.Clear(clearDepth: true, clearColor: true, Color.black);
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.blackTexture);
		if (sleepStyle == null)
		{
			sleepStyle = new GUIStyle(GUI.skin.label);
			sleepStyle.alignment = TextAnchor.MiddleCenter;
			sleepStyle.wordWrap = true;
			sleepStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
		}
		sleepStyle.fontSize = Mathf.Max(14, Screen.height / 28);
		long num = mSystem.currentTimeMillis();
		if (num - sleepBuilt > 1000)
		{
			BuildHud();
			BuildStats();
			StringBuilder stringBuilder = new StringBuilder("TIẾT KIỆM - chạm màn hình để thoát");
			if (hudText.Length > 0)
			{
				stringBuilder.Append("\n").Append(hudText);
			}
			if (statsStarted)
			{
				for (int i = 0; i < statLines.Length; i++)
				{
					if (statLines[i] != null)
					{
						stringBuilder.Append("\n").Append(statLines[i]);
					}
				}
			}
			sleepText = stringBuilder.ToString();
			sleepBuilt = num;
		}
		GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), sleepText, sleepStyle);
	}
}
