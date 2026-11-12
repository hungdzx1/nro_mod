using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game1;
using Game1.Assets.src.g;
using Game1.Mod;
using Char = Game1.Char;

namespace AutoGopTests
{
	class Program
	{
		static int _passed = 0;
		static int _failed = 0;

		static void Assert(bool cond, string name)
		{
			if (cond)
			{
				_passed++;
				Console.WriteLine($"[PASS] {name}");
			}
			else
			{
				_failed++;
				Console.WriteLine($"[FAIL] {name}");
			}
		}

		static void ResetEnv(int mapId = 5, long nowMs = 1000000)
		{
			var f = typeof(AutoGop).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			f?.SetValue(null, null);
			AutoGop.InvalidateBagCache();
			mSystem.NowMs = nowMs;
			TileMap.mapID = mapId;
			Char.Reset();
			ClientInput.Reset();
			Service.Calls.Clear();
			Session_ME.SentMessages.Clear();
			GameScr.info1.Messages.Clear();
			Cout.Logs.Clear();
			GameCanvas.panel.isShow = false;
			GameCanvas.menu.showMenu = false;
			GameCanvas.EndDlgCount = 0;
			ModFunc.Npcs = Array.Empty<Npc>();
		}

		static Npc MakeNpc(int id, int cx = 300, int cy = 200)
		{
			return new Npc { template = new NpcTemplate { npcTemplateId = id }, cx = cx, cy = cy, statusMe = 0 };
		}

		static Item MakeItem(int id, int qty, string name = "", bool isSpec = true, int specCost = 0)
		{
			return new Item
			{
				template = new ItemTemplate { id = id, name = name, isUpToUp = true },
				quantity = qty,
				isBuySpec = isSpec,
				buySpec = specCost
			};
		}

		static Message BuildPacketMinus125(string title, (string name, sbyte type)[] fields)
		{
			var mw = new myWriter();
			mw.writeUTF(title);
			mw.writeByte(fields.Length);
			foreach (var fld in fields)
			{
				mw.writeUTF(fld.name);
				mw.writeByte(fld.type);
			}
			return new Message(-125, mw.ToArray());
		}

		// Mirrors Game1.Assets.src.f.Controller2 case -125
		static bool SimulateController2PacketMinus125(AutoGop gop, Message msg)
		{
			if (gop.OnReceiveServerInputPacket(msg))
			{
				return true;
			}
			string t = msg.reader().readUTF();
			sbyte n = msg.reader().readByte();
			ClientInput.gI().setInput(n, t);
			for (int i = 0; i < n; i++)
			{
				ClientInput.gI().tf[i].name = msg.reader().readUTF();
				ClientInput.gI().tf[i].setIputType(msg.reader().readByte());
			}
			return false;
		}

		static MyVector MakeUiMenu33(params string[] captions)
		{
			var v = new MyVector();
			foreach (var c in captions) v.addElement(new Command(c, null, 88822, null));
			return v;
		}

		static MyVector MakeSubMenu88820(string title, params string[] subs)
		{
			var v = new MyVector();
			var arr = new string[subs.Length + 1];
			arr[0] = title;
			Array.Copy(subs, 0, arr, 1, subs.Length);
			v.addElement(new Command(title, null, 88820, arr));
			return v;
		}

		static MyVector MakeMenu27((string caption, short id)[] entries)
		{
			var v = new MyVector();
			foreach (var e in entries) v.addElement(new Command(e.caption, null, 88819, e.id));
			return v;
		}

		static void AdvanceTicks(AutoGop gop, long stepMs, int count)
		{
			for (int i = 0; i < count; i++)
			{
				mSystem.NowMs += stepMs;
				gop.Update();
			}
		}

		static void Main()
		{
			// 0. Defaults validation (V2 spec)
			{
				ResetEnv();
				var gop = AutoGop.gI();
				Assert(gop.delayBuyBanhMs == 600, "Default: delayBuyBanhMs == 600");
				Assert(gop.stopBanhCond2 && gop.stopBanhCond3 && gop.targetItemCountBanh == 5000, "Default: Banh cond2/3 + target 5000");
				Assert(typeof(AutoGop).GetField("stopBanhCond1") == null && typeof(AutoGop).GetField("targetBatchesBanh") == null && typeof(AutoGop).GetField("delayUseBanhMs") == null, "Default: removed Banh DK1 and delayUseBanhMs fields");
				Assert(gop.targetItemNameHn == "BOX HỒNG NGỌC SSS" && gop.targetItemIdHn == 1581 && gop.delayBuyHnMs == 600 && gop.stopConditionHn == 1, "Default: HN is BOX HONG NGOC SSS [1581], delay 600, stopConditionHn=1");
				Assert(typeof(AutoGop).GetField("autoUseHn") == null && typeof(AutoGop).GetField("stopHnCond3") == null, "Default: removed autoUseHn and stopHnCond3 checkbox fields");
				Assert(gop.autoSelectMocDiem == false && gop.targetBatchesDiem == 1 && gop.stopConditionDiem == 1 && gop.delayDiemMs == 350, "Default: Diem autoSelectMocDiem=false, target=1, stopConditionDiem=1");
			}

			// 01: F8 at wrong map 46 with plenty of tickets -> WRONG_MAP_ZENO, NO XMap, NO openMenu(18), NO FinishZenoLack
			{
				ResetEnv(mapId: 46);
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 30000, "Vé Zeno");
				ModFunc.Npcs = new[] { MakeNpc(92), MakeNpc(56), MakeNpc(66) };
				var gop = AutoGop.gI();
				gop.StartTab(4);
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.WRONG_MAP_ZENO, "Test #01: F8 at wrong map 46 stops with WRONG_MAP_ZENO");
				Assert(!Service.Calls.Any(c => c.StartsWith("openMenu:18")) && !Service.Calls.Any(c => c.StartsWith("charMove")), "Test #01: Never opens NPC 18 or moves/teleports at wrong map 46");
				Assert(gop.completedBatchesZeno == 0 && gop.currentRunningTab == 0, "Test #01: Does not route to Bánh (not FinishZenoLack)");
			}

			// 02: F3 workflow at wrong map 46 reaching Zeno -> stops cleanly with WRONG_MAP_ZENO, zero loop
			{
				ResetEnv(mapId: 46);
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 30000);
				var gop = AutoGop.gI();
				gop.enableBanh = false;
				gop.enableDiem = false;
				gop.enableHn = false;
				gop.enableZeno = true;
				gop.StartWorkflow();
				Assert(!gop.isRunning && !gop.isRunningWorkflow && gop.lastZenoEndCode == AutoGop.ZenoEndCode.WRONG_MAP_ZENO, "Test #02: F3 workflow pauses/stops with WRONG_MAP_ZENO on wrong map 46");
			}

			// 02b: HN -> routing Zeno on Map 5 does NOT loop HN -> Zeno -> Banh
			{
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(56) };
				Char.myCharz().arrItemBag[0] = MakeItem(1378, 1000, "Zeno");
				var gop = AutoGop.gI();
				gop.stopConditionHn = 3;
				gop.enableRoutingHn = true;
				gop.routeTargetTabHn = 4;
				gop.enableRouteLackZeno = true;
				gop.routeTargetWhenLackZeno = 1;
				gop.StartTab(2); // triggers CheckHnZenoLack -> queues route tab 4
				Assert(gop.isRunning, "Test #02b: HN lack queues deferred transition to tab 4");
				AdvanceTicks(gop, 600, 1); // enters StartTab(4) on Map 5
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.WRONG_MAP_ZENO && gop.currentRunningTab == 0, "Test #02b: Deferred HN->Zeno stops cleanly with WRONG_MAP_ZENO without looping to Banh");
			}

			// 03: F8 at map 5, NPC 18 in range, 25k tickets -> pkt32 x2 -> -125 (qty only) -> -36 ack -> ZENO_DONE
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 280; Char.myCharz().cy = 200; // within <=55, <=50
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000, "Vé Zeno");
				var gop = AutoGop.gI();
				gop.targetBatchesZeno = 1;
				gop.enableRouteDoneZeno = false;
				gop.StartTab(4);
				Assert(gop.isRunning && gop.zenoState == AutoGop.ZenoStepState.WaitMenu1, "Test #03: In range enters WaitMenu1 immediately");
				Assert(!Service.Calls.Any(c => c.StartsWith("charMove")), "Test #03: Already in range does NOT overwrite cx/cy or call charMove");
				Assert(Service.Calls.Contains("openMenu:18"), "Test #03: Sent openMenu(18)");
				Assert(gop.OnReceiveMenu(new[] { "Nói chuyện", "Nhiệm vụ", "Tập luyện", "Đổi Tiên Phù" }, karin) == true, "Test #03: Handled Menu 1 pkt 32");
				Assert(gop.zenoState == AutoGop.ZenoStepState.WaitMenu2 && Service.Calls.Contains("confirmMenu:18:3"), "Test #03: Advanced to WaitMenu2 with confirmMenu(18,3)");
				Assert(gop.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, karin) == true, "Test #03: Handled Menu 2 pkt 32");
				Assert(gop.zenoState == AutoGop.ZenoStepState.WaitInput && Service.Calls.Contains("confirmMenu:18:0"), "Test #03: Advanced to WaitInput");

				// Send multi-field -125 (field 0 text note, field 1 numeric qty)
				var pkt125 = BuildPacketMinus125("Đổi Tiên Phù Zeno", new (string, sbyte)[] { ("Ghi chú", 1), ("Số lượng vé", 0) });
				Assert(SimulateController2PacketMinus125(gop, pkt125), "Test #03: Handled -125 packet without Controller2 EOF");
				Assert(Service.Calls.Contains("sendClientInput:2:|19800"), "Test #03: Filled ONLY numeric quantity field, not non-qty field");
				Assert(gop.zenoState == AutoGop.ZenoStepState.WaitServerAck && gop.completedBatchesZeno == 0, "Test #03: WaitServerAck active, completedBatchesZeno STILL 0 before server ack");

				// Now simulate server -36 deducting 19800 tickets (25000 -> 5200)
				Char.myCharz().arrItemBag[0].quantity = 5200;
				gop.OnBagUpdated();
				Assert(gop.completedBatchesZeno == 1 && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_DONE && !gop.isRunning, "Test #03: Confirmed ticket drop increments completedBatchesZeno to 1 and stops ZENO_DONE");
			}

			// 03b: ApproachNpc smoothly via MovePoint when far in Map 5
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 400, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 100; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				Assert(gop.zenoState == AutoGop.ZenoStepState.ApproachNpc && Char.myCharz().currentMovePoint != null, "Test #03b: Far away sets ApproachNpc + currentMovePoint");
				Assert(Char.myCharz().cx == 100 && !Service.Calls.Contains("openMenu:18"), "Test #03b: Does not teleport cx or prematurely openMenu");
				Char.myCharz().cx = 390; Char.myCharz().currentMovePoint = null;
				AdvanceTicks(gop, 100, 1);
				Assert(gop.zenoState == AutoGop.ZenoStepState.WaitMenu1 && Service.Calls.Contains("openMenu:18"), "Test #03b: Opens menu cleanly upon reaching Karin range");
			}

			// 04: Dynamic menu indices & no false reset on "Đổi Tiên Phù Zeno"
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Hướng dẫn", "Đổi Tiên Phù", "Tập luyện", "Đóng" }, karin);
				Assert(Service.Calls.Contains("confirmMenu:18:1") && gop.zenoState == AutoGop.ZenoStepState.WaitMenu2, "Test #04: Dynamically selected idx 1 instead of hardcoded idx 3");
				gop.OnReceiveMenu(new[] { "Đổi Tiên Phù Zeno", "Đóng" }, karin);
				Assert(Service.Calls.Contains("confirmMenu:18:0") && gop.zenoState == AutoGop.ZenoStepState.WaitInput, "Test #04: WaitMenu2 'Đổi Tiên Phù Zeno' did NOT false-reset to MainMenu");
			}

			// 05: Packet 33 Talk -> 88820 Sub-menu -> Packet 27 menuId
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveOpenUiMenu(MakeUiMenu33("Nói chuyện", "Đóng"));
				Assert(Service.Calls.Contains("menu:18:0:0") && gop.zenoState == AutoGop.ZenoStepState.WaitMenu1, "Test #05: Pkt 33 Talk pass keeps WaitMenu1");
				var subVec = new MyVector();
				subVec.addElement(new Command("Hướng dẫn", null, 88822, null));
				subVec.addElement(new Command("Karin", null, 88820, new[] { "Karin", "Tập luyện", "Đổi Tiên Phù" }));
				gop.OnReceiveOpenUiMenu(subVec);
				Assert(Service.Calls.Contains("menu:18:1:1") && gop.zenoState == AutoGop.ZenoStepState.WaitMenu2, "Test #05: Pkt 33 88820 selected exact menuIdx=1, subIdx=1");
				gop.OnReceiveNewMenu(MakeMenu27(new (string, short)[] { ("Tiên Phù VIP", 10), ("Tiên Phù Zeno", 42) }));
				Assert(Service.Calls.Contains("menuId:42") && gop.zenoState == AutoGop.ZenoStepState.WaitInput, "Test #05: Pkt 27 menuId selected exact short ID 42");
			}

			// 06: Menu with no Zeno option -> ZENO_MENU_ERROR (NO click index 0/3, NO FinishZenoLack)
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				Service.Calls.Clear();
				gop.OnReceiveMenu(new[] { "Hướng dẫn", "Đổi mật khẩu", "Mua bùa", "Đóng" }, karin);
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_MENU_ERROR, "Test #06: Unmatched menu triggers ZENO_MENU_ERROR");
				Assert(!Service.Calls.Any(c => c.StartsWith("confirmMenu")), "Test #06: Never blind-clicks 0 or 3 on unmatched menu");
			}

			// 07: Sufficient tickets but server doesn't send -125 -> ZENO_INPUT_TIMEOUT (not FinishZenoLack)
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Nói chuyện", "Nhiệm vụ", "Tập luyện", "Đổi Tiên Phù" }, karin);
				gop.OnReceiveNewMenu(MakeMenu27(new (string, short)[] { ("Tiên Phù Zeno", 77) }));
				Service.Calls.Clear();
				AdvanceTicks(gop, 2600, 1); // 1 retry of Menu 2 cmd -> must re-send menuId:77!
				Assert(Service.Calls.Contains("menuId:77") && gop.isRunning, "Test #07: Retry WaitInput resends exact Menu 2 command menuId:77");
				AdvanceTicks(gop, 2600, 1); // timeout
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_INPUT_TIMEOUT, "Test #07: Ends cleanly with ZENO_INPUT_TIMEOUT, NOT FinishZenoLack");
			}

			// 07b: Sent -125 but server never acknowledges / deducts -> ZENO_ACK_TIMEOUT
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Đổi Tiên Phù", "Đóng" }, karin);
				gop.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, karin);
				gop.OnReceiveServerInputPacket(BuildPacketMinus125("Số lượng", new (string, sbyte)[] { ("SL", 0) }));
				AdvanceTicks(gop, 3100, 1);
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_ACK_TIMEOUT && gop.completedBatchesZeno == 0, "Test #07b: Unconfirmed -125 times out as ZENO_ACK_TIMEOUT with 0 batches");
			}

			// 08: Sent -125 and server rejects -> ZENO_SERVER_REJECT, completedBatchesZeno == 0
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Đổi Tiên Phù", "Đóng" }, karin);
				gop.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, karin);
				gop.OnReceiveServerInputPacket(BuildPacketMinus125("Số lượng", new (string, sbyte)[] { ("SL", 0) }));
				gop.OnServerInfo("Không thể đổi Tiên Phù lúc này");
				Assert(!gop.isRunning && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_SERVER_REJECT && gop.completedBatchesZeno == 0, "Test #08: Rejection stops with ZENO_SERVER_REJECT and 0 batches");
			}

			// 09: Stale 100ms bag cache invalidated + targetBatchesZeno=1 routes to HN after ack
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				var hnNpc = MakeNpc(56, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				AutoGop.GetBagItemCount(1583); // warm 100ms cache!
				var gop = AutoGop.gI();
				gop.targetBatchesZeno = 1;
				gop.enableRouteDoneZeno = true;
				gop.routeTargetWhenDoneZeno = 2;
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Đổi Tiên Phù", "Đóng" }, karin);
				gop.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, karin);
				gop.OnReceiveServerInputPacket(BuildPacketMinus125("SL", new (string, sbyte)[] { ("SL", 0) }));
				Char.myCharz().arrItemBag[0].quantity = 5200;
				mSystem.NowMs += 10; // still within 100ms cache window!
				gop.OnBagUpdated();
				Assert(gop.completedBatchesZeno == 1 && gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_DONE, "Test #09: Invalidates 100ms bag cache immediately upon -36");
				ModFunc.Npcs = new[] { hnNpc }; TileMap.mapID = 5;
				AdvanceTicks(gop, gop.delayBetweenTabsMs + 10, 1);
				Assert(gop.isRunning && gop.currentRunningTab == 2, "Test #09: Deferred routing cleanly transitions Zeno -> HN");
			}

			// 10: True lack of tickets from start -> ZENO_LACK_TICKETS routes to Bánh
			{
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(18, 300, 200) };
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 500);
				var gop = AutoGop.gI();
				gop.enableRouteLackZeno = true;
				gop.routeTargetWhenLackZeno = 1;
				gop.StartTab(4);
				Assert(gop.lastZenoEndCode == AutoGop.ZenoEndCode.ZENO_LACK_TICKETS && gop.isRunning, "Test #10: True lack tickets routes via Nhánh 2 (deferred)");
			}

			// 11: hideUi=false returns menus visually AND does not EOF-crash Controller2 on -125
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.hideUi = false;
				gop.enableRouteDoneZeno = false;
				gop.StartTab(4);
				Assert(gop.OnReceiveMenu(new[] { "Đổi Tiên Phù", "Đóng" }, karin) == false, "Test #11: hideUi=false returns false on menu so menu shows visually");
				Assert(gop.OnReceiveOpenUiMenu(MakeUiMenu33("Tiên Phù Zeno", "Đóng")) == false, "Test #11: hideUi=false returns false on pkt33 menu");
				var pkt = BuildPacketMinus125("Số lượng", new (string, sbyte)[] { ("SL vé", 0) });
				Assert(SimulateController2PacketMinus125(gop, pkt) == true, "Test #11: hideUi=false populates ClientInput and returns true so Controller2 does NOT read EOF");
				Char.myCharz().arrItemBag[0].quantity = 5200;
				gop.OnBagUpdated();
				Assert(gop.completedBatchesZeno == 1, "Test #11: hideUi=false completes exchange cleanly");
			}

			// 12: F6 emergency stop ignores subsequent late packets
			{
				ResetEnv(mapId: 5);
				var karin = MakeNpc(18, 300, 200);
				ModFunc.Npcs = new[] { karin };
				Char.myCharz().cx = 300; Char.myCharz().cy = 200;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000);
				var gop = AutoGop.gI();
				gop.StartTab(4);
				gop.OnReceiveMenu(new[] { "Đổi Tiên Phù", "Đóng" }, karin);
				gop.StopAll("F6");
				Service.Calls.Clear();
				Assert(!gop.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, karin), "Test #12: Late menu packet ignored after F6");
				Assert(!gop.OnReceiveServerInputPacket(BuildPacketMinus125("SL", new (string, sbyte)[] { ("SL", 0) })), "Test #12: Late -125 ignored after F6 without consuming reader");
				Assert(Service.Calls.Count == 0, "Test #12: Zero outgoing packets sent after F6");
			}

			// 13: Tab Bánh single-use UseItemInBag(1337) & partial-bag reporting
			{
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(92) };
				Char.myCharz().arrItemBag[0] = MakeItem(1337, 5000, "Bánh");
				var gop = AutoGop.gI();
				gop.StartTab(1);
				Service.Calls.Clear();
				AdvanceTicks(gop, 100, 1);
				Assert(Service.Calls.Count(c => c.StartsWith("useItem:")) == 1, "Test #13a: Calls UseItemInBag(1337) ONCE at >=5000");
				// Subsequent ticks while item still present do NOT call useItem again
				AdvanceTicks(gop, 250, 3);
				Assert(Service.Calls.Count(c => c.StartsWith("useItem:")) == 1, "Test #13b: Never loops UseItemInBag every 200ms");
				// Timeout still has 200 bánh left -> stops without claiming "sạch"
				Char.myCharz().arrItemBag[0].quantity = 200;
				AdvanceTicks(gop, 600, 2);
				string lastMsg = GameScr.info1.Messages.LastOrDefault(m => m.Contains("Hoàn tất Tab Bánh")) ?? "";
				Assert(!gop.isRunning && !lastMsg.ToLower().Contains("sạch") && lastMsg.Contains("200"), "Test #13c: Reports remaining 200 Bánh and never falsely claims 'sạch'");

				// Subcase: ĐK2 triggered while ĐK3 enabled & Bánh > 0 still uses once
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(92) };
				Char.myCharz().arrItemBag[0] = MakeItem(1337, 50, "Bánh");
				var gop2 = AutoGop.gI();
				gop2.StartTab(1);
				gop2.OnShopOpened(3);
				AdvanceTicks(gop2, 250, 1);
				gop2.OnServerInfo("Không đủ Hồng Ngọc");
				Service.Calls.Clear();
				AdvanceTicks(gop2, 100, 1);
				Assert(Service.Calls.Count(c => c.StartsWith("useItem:")) == 1, "Test #13d: ĐK2 empty currency with ĐK3 triggers single UseItemInBag(1337)");
			}

			// 14: Tab HN BOX HỒNG NGỌC SSS, no autoUse, ĐK3 long check + ĐK1
			{
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(56) };
				Char.myCharz().arrItemBag[0] = MakeItem(1378, 39999999, "Zeno");
				var gop = AutoGop.gI();
				gop.stopConditionHn = 3;
				gop.enableRoutingHn = false;
				gop.batchQtyHn = 1000; // needs 40,000,000 -> long check!
				gop.StartTab(2);
				Assert(!gop.isRunning && !Service.Calls.Any(c => c.StartsWith("buyItem:")), "Test #14a: ĐK 3 stops BEFORE buy when Zeno < SL x 40K");

				// Long overflow check: 60000 * 40000 = 2,400,000,000 (> int.MaxValue!)
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(56) };
				Char.myCharz().arrItemBag[0] = MakeItem(1378, 2000000000, "Zeno");
				var gopLarge = AutoGop.gI();
				gopLarge.stopConditionHn = 3;
				gopLarge.batchQtyHn = 60000;
				gopLarge.enableRoutingHn = false;
				gopLarge.StartTab(2);
				Assert(!gopLarge.isRunning, "Test #14b: Uses long arithmetic for batchQtyHn * 40000 without int32 overflow");

				// ĐK1 + never calls useItem(1581)
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { MakeNpc(56) };
				Char.myCharz().arrItemBag[0] = MakeItem(1378, 100000000, "Zeno");
				Char.myCharz().arrItemBag[1] = MakeItem(1581, 1000, "BOX HN SSS");
				var gopBuy = AutoGop.gI();
				gopBuy.stopConditionHn = 1;
				gopBuy.targetBatchesHn = 1;
				gopBuy.enableRoutingHn = false;
				gopBuy.StartTab(2);
				gopBuy.OnShopOpened(3);
				AdvanceTicks(gopBuy, 250, 1);
				gopBuy.OnReceiveServerInputPacket(BuildPacketMinus125("SL", new (string, sbyte)[] { ("SL", 0) }));
				Assert(!gopBuy.isRunning && gopBuy.completedBatchesHn == 1 && !Service.Calls.Any(c => c.StartsWith("useItem:")), "Test #14c: Tab HN completes ĐK1 and never auto-uses 1581");
			}

			// 15: Tab Điểm preserves selected tier & waits for ack before incrementing
			{
				ResetEnv(mapId: 5);
				var pot = MakeNpc(66);
				ModFunc.Npcs = new[] { pot };
				var gop = AutoGop.gI();
				gop.pointIndex = 2; // 3K user choice
				gop.currentDiemPoints = 10000000;
				gop.StartTab(3);
				gop.OnReceiveMenu(new[] { "Làm bánh", "Thể lệ", "Đổi điểm" }, pot);
				Assert(Service.Calls.Contains("confirmMenu:66:2"), "Test #15a: Opens Diem tier menu");
				gop.OnReceiveMenu(new[] { "10Tr", "5K", "3K", "200", "50" }, pot);
				Assert(gop.pointIndex == 2 && Service.Calls.Last() == "confirmMenu:66:2", "Test #15b: Preserves user pointIndex=2 (autoSelectMocDiem=false)");
				Assert(gop.completedBatchesDiem == 0, "Test #15c: Does NOT increment completedBatchesDiem immediately on send");
				gop.OnServerInfo("Không đủ điểm");
				Assert(!gop.isRunning && gop.completedBatchesDiem == 0, "Test #15d: Server reject leaves completedBatchesDiem == 0");

				// Now confirm success via bag update -> increments to 1 and stops (targetBatchesDiem=1, stopConditionDiem=1)
				ResetEnv(mapId: 5);
				ModFunc.Npcs = new[] { pot };
				var gopOk = AutoGop.gI();
				gopOk.pointIndex = 1;
				gopOk.StartTab(3);
				gopOk.OnReceiveMenu(new[] { "10Tr", "5K", "3K", "200", "50" }, pot);
				Char.myCharz().arrItemBag[0] = MakeItem(1378, 10, "Zeno");
				gopOk.OnBagUpdated();
				Assert(!gopOk.isRunning && gopOk.completedBatchesDiem == 1, "Test #15e: Bag delta confirms completedBatchesDiem=1 and finishes default 1 batch");
			}

			// 16: Verify [AutoGop][Zeno] structured logs captured with full context
			{
				ResetEnv(mapId: 5);
				var npc18Log = MakeNpc(18, cx: 400, cy: 300);
				ModFunc.Npcs = new[] { npc18Log };
				Char.myCharz().cx = 410;
				Char.myCharz().cy = 300;
				Char.myCharz().arrItemBag[0] = MakeItem(1583, 25000, "Vé Zeno");
				var gopLog = AutoGop.gI();
				gopLog.targetBatchesZeno = 1;
				gopLog.StartTab(4);
				gopLog.OnReceiveMenu(new[] { "Thưởng", "Đổi Tiên Phù Zeno" }, npc18Log);
				gopLog.OnReceiveMenu(new[] { "Tiên Phù Zeno", "Đóng" }, npc18Log);
				SimulateController2PacketMinus125(gopLog, BuildPacketMinus125("Đổi Tiên Phù Zeno", new[] { ("Số lượng vé", (sbyte)0) }));
				Char.myCharz().arrItemBag[0].quantity = 5200;
				gopLog.OnBagUpdated();
				bool hasCout = Cout.Logs.Any(l => l.Contains("[AutoGop][Zeno] map=5, npc=18") && l.Contains("packet=") && l.Contains("ticketBefore=25000") && l.Contains("ticketAfter=5200") && l.Contains("reason="));
				bool hasBuf = gopLog.zenoDebugLogs.Any(l => l.Contains("[AutoGop][Zeno]") && l.Contains("AckSuccess"));
				Assert(hasCout && hasBuf, "Test #16: Produces structured [AutoGop][Zeno] logs with full context");
			}

			Console.WriteLine($"\nTOTAL PASSED: {_passed}, FAILED: {_failed}");
			Environment.Exit(_failed == 0 ? 0 : 1);
		}
	}
}
