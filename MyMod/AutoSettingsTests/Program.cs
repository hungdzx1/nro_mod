using System;
using System.Reflection;
using Game1.Mod;

internal sealed class FutureAuto
{
	[AutoSetting]
	public int Delay = 100;

	[AutoSetting]
	public bool Enabled;

	public bool IsRunning;
}

internal static class Program
{
	private static int checks;

	private static void Check(bool condition, string label)
	{
		if (!condition)
		{
			throw new Exception("FAIL: " + label);
		}
		checks++;
	}

	private static void NewInstances()
	{
		typeof(AutoGop).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
		typeof(AutoDungNhanh).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
	}

	public static void Main()
	{
		NroInstanceProfile.Clear();
		NroInstanceProfile.Slot = 1;
		NewInstances();
		AutoGop gop = AutoGop.gI();
		AutoDungNhanh quick = AutoDungNhanh.gI();
		gop.enableBanh = false;
		gop.delayBetweenTabsMs = 875;
		gop.pointIndex = 3;
		gop.autoUseRubyBoxBanh = true;
		gop.rubyBoxUseQuantityBanh = 1234;
		quick.SetDelayMs(75);
		quick.stopOnBagFull = false;
		quick.followBanh = true;
		quick.items.Clear();
		quick.items.Add(new AutoDungNhanh.FastUseEntry { itemId = 42, itemName = "Hộp Việt, A; B", enabled = false });
		quick.items.Add(new AutoDungNhanh.FastUseEntry { itemId = 1581, itemName = "Hộp hồng ngọc", enabled = false });
		quick.ToggleContinuousRun();
		gop.Update();
		quick.Update();
		int savedWrites = NroInstanceProfile.Writes;
		gop.Update();
		quick.Update();
		Check(NroInstanceProfile.Writes == savedWrites, "unchanged settings do not write again");

		NewInstances();
		gop = AutoGop.gI();
		quick = AutoDungNhanh.gI();
		Check(!gop.enableBanh && gop.delayBetweenTabsMs == 875 && gop.pointIndex == 3, "Auto Gộp settings restore");
		Check(gop.autoUseRubyBoxBanh && gop.rubyBoxUseQuantityBanh == 1234, "Bánh settings restore");
		Check(!gop.isRunning && !gop.isRunningWorkflow && gop.completedBatchesBanh == 0, "Auto Gộp runtime remains idle");
		Check(quick.delayMs == 75 && !quick.stopOnBagFull && quick.followBanh && quick.continuousRun, "Dùng Nhanh options restore");
		Check(quick.items.Count == 2 && quick.items[0].itemName == "Hộp Việt, A; B" && !quick.items[0].enabled, "item list and Unicode name restore");
		Check(!quick.isRunning && !quick.isBanhBatchRunning, "Dùng Nhanh runtime remains idle");
		quick.items[0].enabled = true;
		quick.Update();
		Check(!quick.isRunning, "saved continuous mode does not auto start after relaunch");
		quick.ToggleStartPause();
		Check(quick.isRunning, "manual start works after restore");

		NroInstanceProfile.Slot = 2;
		NewInstances();
		gop = AutoGop.gI();
		quick = AutoDungNhanh.gI();
		Check(gop.enableBanh && gop.delayBetweenTabsMs == 500 && quick.delayMs == 200, "second window starts with own defaults");
		gop.delayBetweenTabsMs = 456;
		quick.SetDelayMs(30);
		gop.Update();
		quick.Update();
		NroInstanceProfile.Slot = 1;
		NewInstances();
		Check(AutoGop.gI().delayBetweenTabsMs == 875 && AutoDungNhanh.gI().delayMs == 75, "window settings remain isolated");

		quick = AutoDungNhanh.gI();
		quick.items.Clear();
		quick.continuousRun = false;
		quick.Update();
		NewInstances();
		Check(AutoDungNhanh.gI().items.Count == 0, "cleared item list remains empty");

		FutureAuto future = new FutureAuto { Delay = 321, Enabled = true, IsRunning = true };
		string snapshot = null;
		AutoSettingsStore.SaveIfChanged(future, "FutureAuto", ref snapshot);
		FutureAuto restored = new FutureAuto();
		AutoSettingsStore.Load(restored, "FutureAuto");
		Check(restored.Delay == 321 && restored.Enabled && !restored.IsRunning, "future autos use shared opt-in fields");
		FutureAuto otherAuto = new FutureAuto();
		AutoSettingsStore.Load(otherAuto, "Game2.FutureAuto");
		Check(otherAuto.Delay == 100 && !otherAuto.Enabled, "auto IDs remain isolated");

		AutoGop allSettings = (AutoGop)Activator.CreateInstance(typeof(AutoGop), true);
		int settingCount = 0;
		foreach (FieldInfo field in typeof(AutoGop).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (!Attribute.IsDefined(field, typeof(AutoSettingAttribute)))
			{
				continue;
			}
			settingCount++;
			if (field.FieldType == typeof(bool))
			{
				field.SetValue(allSettings, !(bool)field.GetValue(allSettings));
			}
			else if (field.FieldType == typeof(int))
			{
				field.SetValue(allSettings, (int)field.GetValue(allSettings) + 1);
			}
		}
		string allSnapshot = null;
		AutoSettingsStore.SaveIfChanged(allSettings, "AllGopFields", ref allSnapshot);
		AutoGop roundtrip = (AutoGop)Activator.CreateInstance(typeof(AutoGop), true);
		AutoSettingsStore.Load(roundtrip, "AllGopFields");
		foreach (FieldInfo field in typeof(AutoGop).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (Attribute.IsDefined(field, typeof(AutoSettingAttribute)))
			{
				Check(Equals(field.GetValue(allSettings), field.GetValue(roundtrip)), "Auto Gộp field " + field.Name + " roundtrip");
			}
		}
		Check(settingCount == 41, "all 41 Auto Gộp preferences are selected");

		NroInstanceProfile.SetString("NroGoc.AutoSettings.v1.FutureAuto", "1\nDelay:%%%\nEnabled:MA==\n");
		restored = new FutureAuto { Delay = 777, Enabled = true };
		AutoSettingsStore.Load(restored, "FutureAuto");
		Check(restored.Delay == 777 && !restored.Enabled, "damaged field falls back without losing valid fields");
		Console.WriteLine("AutoSettingsTests PASS: " + checks + " checks");
	}
}
