using System;
using System.Threading;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("FAIL: " + message);
    }

    static void Main()
    {
        Check(NroInstanceProfile.ParseSlot(new[] { "game.exe", "--nro-slot=10" }) == 10, "valid slot");
        Check(NroInstanceProfile.ParseSlot(new[] { "--nro-slot=0" }) == 0, "zero rejected");
        Check(NroInstanceProfile.ParseSlot(new[] { "--nro-slot=1000" }) == 0, "oversized slot rejected");
        Check(NroInstanceProfile.ParseSlot(new[] { "--nro-slot=oops" }) == 0, "invalid slot rejected");
        int slot = NroInstanceProfile.Slot;
        Check(slot == 0 || slot == 1 || slot == 2, "test slot argument");

        if (Array.Exists(Environment.GetCommandLineArgs(), x => x == "--test-duplicate"))
        {
            using var blocker = new Mutex(true, "Local\\NroGoc.Profile." + slot, out bool createdNew);
            Check(createdNew, "test blocker created");
            Check(!NroInstanceProfile.EnsureUniqueSlot(), "duplicate slot rejected");
            Console.WriteLine($"PASS: duplicate slot {slot}, {checks} checks");
            return;
        }

        if (slot == 1)
        {
            PlayerPrefs.SetString("accTab1", "0102");
            PlayerPrefs.SetString("accTab2", "0304");
            PlayerPrefs.SetString("NroGoc.Profile.2.accTab1", "0909");
            Check(Game1.Rms.loadRMS("acc")[0] == 1, "Game1 legacy import");
            Check(Game2.Rms.loadRMS("acc")[0] == 3, "Game2 legacy import");
            Check(PlayerPrefs.GetString("NroGoc.Profile.1.accTab1") == "0102", "Game1 import persisted");
            Check(PlayerPrefs.GetString("NroGoc.Profile.1.accTab2") == "0304", "Game2 import persisted");
            Game1.Rms.saveRMS("new", new sbyte[] { 5 });
            Check(PlayerPrefs.GetString("NroGoc.Profile.1.newTab1") == "05", "Game1 scoped write");
            PlayerPrefs.SetString("deletedTab1", "0708");
            NroInstanceProfile.DeleteKey("deletedTab1");
            Check(NroInstanceProfile.GetString("deletedTab1") == string.Empty, "deleted legacy value does not reappear");
            Game1.Rms.clearAll();
            Check(Game1.Rms.loadRMS("acc") == null, "legacy does not reappear after clear");
            Check(Game2.Rms.loadRMS("acc") == null, "Game2 cleared with Game1");
            Check(PlayerPrefs.GetString("NroGoc.Profile.2.accTab1") == "0909", "slot 2 preserved");
            Check(PlayerPrefs.GetString("accTab1") == "0102", "legacy data preserved physically");
            Check(NroInstanceProfile.EnsureUniqueSlot(), "slot 1 mutex acquired");
            Check(NroInstanceProfile.EnsureUniqueSlot(), "slot 1 mutex reused");
        }
        else if (slot == 2)
        {
            PlayerPrefs.SetString("accTab1", "0102");
            PlayerPrefs.SetString("NroGoc.Profile.1.accTab1", "0304");
            Check(Game1.Rms.loadRMS("acc") == null, "slot 2 does not import legacy login");
            Game1.Rms.saveRMS("acc", new sbyte[] { 6 });
            Game2.Rms.saveRMS("acc", new sbyte[] { 7 });
            Check(Game1.Rms.loadRMS("acc")[0] == 6, "Game1 slot 2 read");
            Check(Game2.Rms.loadRMS("acc")[0] == 7, "Game2 slot 2 read");
            Game2.Rms.clearAll();
            Check(Game1.Rms.loadRMS("acc") == null, "Game1 cleared with Game2");
            Check(PlayerPrefs.GetString("NroGoc.Profile.1.accTab1") == "0304", "slot 1 preserved");
            Check(PlayerPrefs.GetString("accTab1") == "0102", "legacy preserved");
            Check(NroInstanceProfile.EnsureUniqueSlot(), "slot 2 mutex acquired");
            Check(NroInstanceProfile.EnsureUniqueSlot(), "slot 2 mutex reused");
        }
        else
        {
            PlayerPrefs.SetString("accTab1", "0102");
            Check(Game1.Rms.loadRMS("acc")[0] == 1, "legacy direct launch unchanged");
            Game1.Rms.clearAll();
            Check(Game1.Rms.loadRMS("acc") == null, "legacy clear without profiles");
            PlayerPrefs.SetString("NroGoc.Profile.1.accTab1", "0304");
            PlayerPrefs.SetInt("NroGoc.ProfilesActive", 1);
            Game1.Rms.clearAll();
            Check(PlayerPrefs.GetString("NroGoc.Profile.1.accTab1") == "0304", "direct launch cannot clear profiles");
            Game1.Rms.saveRMS("acc", new sbyte[] { 8 });
            Check(PlayerPrefs.GetString("NroGoc.Profile.LegacyReset.accTab1") == "08", "legacy reset namespace");
        }
        NroInstanceProfile.UpdateWindowTitle();
        Console.WriteLine($"PASS: slot {slot}, {checks} checks");
    }
}
