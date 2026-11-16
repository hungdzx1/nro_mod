using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

/// <summary>Separates RMS data for launcher slots while keeping the original client usable.</summary>
public static class NroInstanceProfile
{
	private const string ArgumentPrefix = "--nro-slot=";
	private const string ProfileRoot = "NroGoc.Profile.";
	private const string ActiveKey = "NroGoc.ProfilesActive";
	private const string LegacyResetKey = "NroGoc.LegacyReset";
	private static readonly int slot = ParseSlot(Environment.GetCommandLineArgs());
	private static long nextTitleCheck;
	private static Mutex slotMutex;
	private static bool slotChecked;
	private static bool slotAvailable;

	public static int Slot => slot;

	private static string Prefix
	{
		get
		{
			if (slot > 0)
			{
				return ProfileRoot + slot + ".";
			}
			return PlayerPrefs.GetInt(LegacyResetKey, 0) == 1 ? ProfileRoot + "LegacyReset." : string.Empty;
		}
	}

	public static int ParseSlot(string[] args)
	{
		foreach (string arg in args)
		{
			if (arg.StartsWith(ArgumentPrefix, StringComparison.Ordinal) &&
				int.TryParse(arg.Substring(ArgumentPrefix.Length), out int parsed) && parsed >= 1 && parsed <= 999)
			{
				return parsed;
			}
		}
		return 0;
	}

	public static bool EnsureUniqueSlot()
	{
		if (slot == 0)
		{
			return true;
		}
		if (slotChecked)
		{
			return slotAvailable;
		}
		slotChecked = true;
		try
		{
			slotMutex = new Mutex(true, "Local\\NroGoc.Profile." + slot, out bool createdNew);
			slotAvailable = createdNew;
			if (!createdNew)
			{
				slotMutex.Dispose();
				slotMutex = null;
				if (Application.platform == RuntimePlatform.WindowsPlayer)
				{
					MessageBoxW(IntPtr.Zero, "Nro " + slot + " dang chay. Hay chon so phien khac.", "Nro Launcher", 0x30u);
				}
			}
		}
		catch (Exception)
		{
			slotAvailable = false;
		}
		return slotAvailable;
	}

	public static string GetString(string key)
	{
		string prefix = Prefix;
		if (prefix.Length == 0)
		{
			return PlayerPrefs.GetString(key);
		}
		PlayerPrefs.SetInt(ActiveKey, 1);
		string scopedKey = prefix + key;
		if (PlayerPrefs.HasKey(scopedKey))
		{
			return PlayerPrefs.GetString(scopedKey);
		}
		if (slot == 1 && PlayerPrefs.GetInt(prefix + "$legacyDisabled", 0) != 1 && PlayerPrefs.HasKey(key))
		{
			string value = PlayerPrefs.GetString(key);
			SetString(key, value);
			return value;
		}
		return string.Empty;
	}

	public static void SetString(string key, string value)
	{
		string prefix = Prefix;
		if (prefix.Length == 0)
		{
			PlayerPrefs.SetString(key, value);
			return;
		}
		TrackKey(prefix, key);
		PlayerPrefs.SetString(prefix + key, value);
	}

	public static void DeleteKey(string key)
	{
		string prefix = Prefix;
		if (prefix.Length == 0)
		{
			PlayerPrefs.DeleteKey(key);
			return;
		}
		// An empty scoped value prevents slot 1 from importing a deleted legacy value again.
		TrackKey(prefix, key);
		PlayerPrefs.SetString(prefix + key, string.Empty);
	}

	public static void ClearAll()
	{
		string prefix = Prefix;
		if (prefix.Length == 0)
		{
			if (PlayerPrefs.GetInt(ActiveKey, 0) != 1)
			{
				PlayerPrefs.DeleteAll();
			}
			else
			{
				// Keep launcher profiles safe if the executable is opened without a slot.
				PlayerPrefs.SetInt(LegacyResetKey, 1);
			}
			return;
		}
		PlayerPrefs.SetInt(ActiveKey, 1);
		string manifestKey = prefix + "$keys";
		string manifest = PlayerPrefs.GetString(manifestKey);
		foreach (string key in manifest.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			PlayerPrefs.DeleteKey(prefix + key);
		}
		PlayerPrefs.DeleteKey(manifestKey);
		if (slot == 1)
		{
			PlayerPrefs.SetInt(prefix + "$legacyDisabled", 1);
		}
	}

	private static void TrackKey(string prefix, string key)
	{
		PlayerPrefs.SetInt(ActiveKey, 1);
		string manifestKey = prefix + "$keys";
		string manifest = PlayerPrefs.GetString(manifestKey);
		if (("\n" + manifest + "\n").Contains("\n" + key + "\n"))
		{
			return;
		}
		PlayerPrefs.SetString(manifestKey, manifest.Length == 0 ? key : manifest + "\n" + key);
	}

	public static void UpdateWindowTitle()
	{
		if (slot == 0 || Application.platform != RuntimePlatform.WindowsPlayer || DateTime.UtcNow.Ticks < nextTitleCheck)
		{
			return;
		}
		nextTitleCheck = DateTime.UtcNow.AddSeconds(2).Ticks;
		try
		{
			using (Process process = Process.GetCurrentProcess())
			{
				process.Refresh();
				IntPtr window = process.MainWindowHandle;
				if (window != IntPtr.Zero)
				{
					SetWindowTextW(window, "Nro " + slot);
				}
			}
		}
		catch (Exception)
		{
			// Window creation can lag behind game startup; retry on the next check.
		}
	}

	[DllImport("user32.dll", EntryPoint = "SetWindowTextW", CharSet = CharSet.Unicode)]
	private static extern bool SetWindowTextW(IntPtr window, string title);

	[DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
	private static extern int MessageBoxW(IntPtr window, string message, string title, uint type);
}
