using System;
using System.Linq;
using System.Reflection;
using Game1;
using Game1.Mod;
using Char = Game1.Char;

internal static class Program
{
    private static int passed;
    private static int failed;

    private static void Check(bool condition, string name)
    {
        if (condition) { passed++; Console.WriteLine("[PASS] " + name); }
        else { failed++; Console.WriteLine("[FAIL] " + name); }
    }

    private static void Reset()
    {
        NroInstanceProfile.Clear();
        typeof(AutoGop).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(AutoDungNhanh).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        Char.Reset();
        Service.Calls.Clear();
        GameScr.info1.Messages.Clear();
        mSystem.NowMs = 1000000;
        TileMap.mapID = 5;
        ModFunc.Npcs = new[] { new Npc { template = new NpcTemplate { npcTemplateId = 92 }, cx = 300, cy = 200 } };
    }

    private static Item Item(int id, int quantity, int index)
    {
        return new Item { template = new ItemTemplate { id = id, name = "Item " + id }, quantity = quantity, indexUI = index };
    }

    private static int UseCount() => Service.Calls.Count(c => c.StartsWith("useItem:"));

    public static void Main()
    {
        Reset();
        var quick = AutoDungNhanh.gI();
        Check(quick.IsItemAdded(1581) && !quick.items[0].enabled && !quick.isRunning && !quick.followBanh,
            "1581 prelisted, idle, follow disabled");
        Char.myCharz().arrItemBag[0] = Item(1581, 4, 0);
        Char.myCharz().arrItemBag[1] = Item(999, 5, 1);
        quick.items.Add(new AutoDungNhanh.FastUseEntry { itemId = 999, itemName = "Other", enabled = true });
        quick.isRunning = true;
        var gop = AutoGop.gI();
        Check(!gop.autoUseRubyBoxBanh && gop.rubyBoxUseQuantityBanh == 400000,
            "bánh option off and quantity 400000 by default");
        gop.stopConditionBanh = 2;
        gop.rubyBoxUseQuantityBanh = 3;
        gop.StartTab(1);
        gop.TriggerBuyCurrentTab();
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(!quick.isBanhBatchRunning, "both checkboxes required");
        quick.followBanh = true;
        gop.autoUseRubyBoxBanh = true;
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(!quick.isBanhBatchRunning, "old message is not a new failed buy");
        gop.TriggerBuyCurrentTab();
        gop.OnServerInfo("Bạn không đủ vàng");
        Check(!quick.isBanhBatchRunning, "different error ignored");
        gop.TriggerBuyCurrentTab();
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(quick.isBanhBatchRunning && quick.banhBatchRemaining == 3 && gop.isRunning,
            "failed buy with exact message starts batch and leaves buy running");
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(quick.banhBatchRemaining == 3, "duplicate message ignored");
        quick.Update();
        Check(UseCount() == 1 && Service.Calls.Last(c => c.StartsWith("useItem:")).Contains(":0:-1"),
            "1581 has priority over other item");
        mSystem.NowMs += 200;
        quick.Update();
        Check(UseCount() == 1 && quick.banhBatchRemaining == 3, "pending use does not double send or count");
        Char.myCharz().arrItemBag[0].quantity = 3;
        quick.Update();
        mSystem.NowMs += 200;
        quick.Update();
        Char.myCharz().arrItemBag[0].quantity = 2;
        quick.Update();
        mSystem.NowMs += 200;
        quick.Update();
        Char.myCharz().arrItemBag[0].quantity = 1;
        quick.Update();
        Check(!quick.isBanhBatchRunning && quick.banhBatchRemaining == 0 && UseCount() == 3,
            "three confirmed decrements finish exactly three uses");
        mSystem.NowMs += 200;
        quick.Update();
        Check(UseCount() == 4 && Service.Calls.Last().Contains(":1:-1"), "other item resumes");
        gop.TriggerBuyCurrentTab();
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(quick.isBanhBatchRunning && quick.banhBatchRemaining == 3, "new failed buy starts new batch");
        quick.Update();
        Char.myCharz().arrItemBag[0] = null;
        quick.Update();
        Check(!quick.isBanhBatchRunning && quick.banhBatchRemaining == 0, "empty bag ends batch");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.followBanh = true;
        Char.myCharz().arrItemBag[0] = Item(1581, 2, 0);
        Check(quick.TryStartBanhBatch(3), "direct batch starts with stock");
        quick.Update();
        mSystem.NowMs += 10001;
        quick.Update();
        Check(!quick.isBanhBatchRunning && UseCount() == 1, "missing bag acknowledgment times out without retrying");
        Check(quick.TryStartBanhBatch(2), "ready after timeout");
        quick.OnDisconnectOrReset();
        Check(!quick.isBanhBatchRunning, "disconnect cancels batch");
        quick.items.Clear();
        Check(!quick.TryStartBanhBatch(2), "removed 1581 prevents activation");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.followBanh = true;
        Char.myCharz().arrItemBag[0] = Item(1581, 2, 0);
        quick.TryStartBanhBatch(2);
        quick.items.Clear();
        quick.Update();
        Check(!quick.isBanhBatchRunning && UseCount() == 0, "removing 1581 cancels running batch");
        quick.items.Add(new AutoDungNhanh.FastUseEntry { itemId = 1581, itemName = "HN", enabled = false });
        quick.TryStartBanhBatch(2);
        gop = AutoGop.gI();
        gop.StopAll("");
        Check(!quick.isBanhBatchRunning, "stopping bánh cancels linked batch");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.followBanh = true;
        Char.myCharz().arrItemBag[0] = Item(1581, 2, 0);
        gop = AutoGop.gI();
        gop.autoUseRubyBoxBanh = true;
        gop.stopConditionBanh = 1;
        gop.StartTab(1);
        gop.TriggerBuyCurrentTab();
        mSystem.NowMs += 10001;
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(!quick.isBanhBatchRunning, "stale shortage after request timeout is ignored");
        gop.TriggerBuyCurrentTab();
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(quick.isBanhBatchRunning && gop.isRunning, "shortage keeps bánh active even with stop condition one");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.followBanh = true;
        Char.myCharz().arrItemBag[0] = Item(1581, 2, 0);
        gop = AutoGop.gI();
        gop.autoUseRubyBoxBanh = true;
        gop.StartTab(1);
        gop.TriggerBuyCurrentTab();
        Char.myCharz().arrItemBag[1] = Item(gop.targetItemIdBanh, 1, 1);
        gop.OnServerInfo("Bạn không đủ hồng ngọc");
        Check(!quick.isBanhBatchRunning, "successful bag increase invalidates old purchase signal");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.SetDelayMs(1);
        Check(quick.delayMs == 1, "minimum delay accepts 1 ms");
        quick.SetDelayMs(49);
        Check(quick.delayMs == 49, "delay below 50 ms is retained");
        quick.SetDelayMs(60001);
        Check(quick.delayMs == 60000, "maximum delay remains 60000 ms");
        typeof(AutoDungNhanh).GetField("editFieldMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(quick, 1);
        quick.onChatFromMe("25", "");
        Check(quick.delayMs == 25, "panel input accepts 25 ms");
        typeof(AutoDungNhanh).GetField("editFieldMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(quick, 1);
        quick.onChatFromMe("0", "");
        Check(quick.delayMs == 25, "panel rejects zero delay");
        Char.myCharz().arrItemBag[0] = Item(999, 3, 0);
        quick.items.Add(new AutoDungNhanh.FastUseEntry { itemId = 999, itemName = "Other", enabled = true });
        quick.isRunning = true;
        quick.SetDelayMs(20);
        quick.lastUseTimeMs = mSystem.NowMs;
        mSystem.NowMs += 19;
        quick.Update();
        Check(UseCount() == 0, "normal use waits below configured delay");
        mSystem.NowMs += 1;
        quick.Update();
        Check(UseCount() == 1, "normal use fires at 20 ms");
        mSystem.NowMs += 19;
        quick.Update();
        Check(UseCount() == 1, "normal use does not send early again");
        mSystem.NowMs += 1;
        quick.Update();
        Check(UseCount() == 2, "normal use repeats after next delay");

        Reset();
        quick = AutoDungNhanh.gI();
        quick.followBanh = true;
        quick.SetDelayMs(20);
        Char.myCharz().arrItemBag[0] = Item(1581, 3, 0);
        Check(quick.TryStartBanhBatch(2), "1581 batch starts for short delay test");
        quick.lastUseTimeMs = mSystem.NowMs;
        mSystem.NowMs += 19;
        quick.Update();
        Check(UseCount() == 0, "1581 batch waits below configured delay");
        mSystem.NowMs += 1;
        quick.Update();
        Check(UseCount() == 1, "1581 batch sends at 20 ms");
        mSystem.NowMs += 20;
        quick.Update();
        Check(UseCount() == 1, "1581 batch waits for inventory acknowledgment");
        Char.myCharz().arrItemBag[0].quantity = 2;
        quick.Update();
        mSystem.NowMs += 19;
        quick.Update();
        Check(UseCount() == 1, "1581 batch waits after acknowledgment");
        mSystem.NowMs += 1;
        quick.Update();
        Check(UseCount() == 2, "1581 batch sends next use at 20 ms");

        Console.WriteLine("TOTAL PASSED: " + passed + ", FAILED: " + failed);
        Environment.Exit(failed == 0 ? 0 : 1);
    }
}
