using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine
{
	public enum KeyCode
	{
		None, A, F3, F4, F5, F6, F7, F8
	}

	public static class Input
	{
		public static KeyCode DownKey = KeyCode.None;
		public static KeyCode HeldKey = KeyCode.None;
		public static bool GetKeyDown(KeyCode k) => DownKey == k;
		public static bool GetKey(KeyCode k) => HeldKey == k;
	}
}

namespace Game1.Assets.src.g
{
	public class ClientInput
	{
		private static ClientInput _instance;
		public Game1.TField[] tf;
		public string lastTitle;

		public static ClientInput gI() => _instance ??= new ClientInput();
		public static void Reset() => _instance = new ClientInput();

		public void setInput(int nFields, string title)
		{
			lastTitle = title;
			tf = new Game1.TField[nFields];
			for (int i = 0; i < nFields; i++)
			{
				tf[i] = new Game1.TField();
			}
		}
	}
}

namespace Game1.Mod
{
	public class ModFunc
	{
		private static ModFunc _instance;
		public static ModFunc GI() => _instance ??= new ModFunc();
		public static Npc[] Npcs = Array.Empty<Npc>();

		public static Npc GetNpcByTempId(int id)
		{
			foreach (var n in Npcs)
			{
				if (n != null && n.template != null && n.template.npcTemplateId == id)
				{
					return n;
				}
			}
			return null;
		}

		public void MoveTo(int x, int y)
		{
			if (Char.myCharz() != null)
			{
				Char.myCharz().cx = x;
				Char.myCharz().cy = y;
			}
		}
	}

	public static class AutoItem
	{
		public static void ResetTF(ChatTextField tf) { }
	}

	public class NpcMenuHelper
	{
		private static NpcMenuHelper _instance;
		public bool isShowWindow;
		public static NpcMenuHelper gI() => _instance ??= new NpcMenuHelper();
	}

	public class AutoMuaNhieu
	{
		private static AutoMuaNhieu _instance;
		public bool isShowWindow;
		public static AutoMuaNhieu gI() => _instance ??= new AutoMuaNhieu();
	}
}

namespace Game1
{
	public interface IChatable
	{
		void onChatFromMe(string text, string to);
		void onCancelChat();
	}

	public static class mSystem
	{
		public static long NowMs = 1000000;
		public static long currentTimeMillis() => NowMs;
	}

	public static class Cout
	{
		public static readonly List<string> Logs = new List<string>();
		public static void println(string s) => Logs.Add(s);
	}

	public static class Main
	{
		public static bool isPC = true;
	}

	public static class TileMap
	{
		public static int mapID = 5;
	}

	public class NpcTemplate
	{
		public int npcTemplateId;
	}

	public class Npc
	{
		public NpcTemplate template = new NpcTemplate();
		public int cx;
		public int cy;
		public int statusMe;
	}

	public class ItemTemplate
	{
		public int id;
		public string name = "";
		public bool isUpToUp;
		public int iconID;
	}

	public class Item
	{
		public ItemTemplate template;
		public int quantity;
		public bool isBuySpec;
		public int buyCoin;
		public int buyGold;
		public int buySpec;
		public int iconSpec;
	}

	public class MovePoint
	{
		public int xEnd;
		public int yEnd;
		public MovePoint(int x, int y) { xEnd = x; yEnd = y; }
	}

	public class Char
	{
		private static Char _me;
		public static bool isManualFocus;
		public static object chatPopup;
		public static Char myCharz() => _me ??= new Char();
		public static void Reset() { _me = new Char(); isManualFocus = false; chatPopup = null; }

		public int cx;
		public int cy;
		public long xu;
		public int luong;
		public int luongKhoa;
		public Item[] arrItemBag = new Item[40];
		public Item[][] arrItemShop;
		public Npc npcFocus;
		public MovePoint currentMovePoint;

		public void FocusManualTo(object obj)
		{
			npcFocus = obj as Npc;
			isManualFocus = true;
		}
	}

	public class MyVector
	{
		private readonly List<object> _items = new List<object>();
		public int size() => _items.Count;
		public object elementAt(int idx) => _items[idx];
		public void addElement(object o) => _items.Add(o);
	}

	public class Command
	{
		public string caption;
		public int idAction;
		public object p;
		public Command(string caption, object listener, int idAction, object p)
		{
			this.caption = caption;
			this.idAction = idAction;
			this.p = p;
		}
	}

	public class TField
	{
		public const int INPUT_TYPE_ANY = 0;
		public const int INPUT_TYPE_NUMERIC = 1;
		public const int INPUT_TYPE_PASSWORD = 2;

		public string name = string.Empty;
		public bool isFocus;
		public int inputType;
		private string _text = string.Empty;

		public void setText(string t) => _text = t ?? string.Empty;
		public string getText() => _text;
		public void setIputType(int type) => inputType = type;
		public void doChangeToTextBox() { }
	}

	public class myWriter
	{
		private readonly MemoryStream _ms = new MemoryStream();
		private readonly BinaryWriter _bw;
		public myWriter() { _bw = new BinaryWriter(_ms, Encoding.UTF8, leaveOpen: true); }
		public void writeByte(int v) => _bw.Write((sbyte)v);
		public void writeShort(int v) => _bw.Write((short)v);
		public void writeUTF(string s) => _bw.Write(s ?? string.Empty);
		public byte[] ToArray() { _bw.Flush(); return _ms.ToArray(); }
	}

	public class myReader
	{
		private readonly BinaryReader _br;
		public myReader(byte[] data) { _br = new BinaryReader(new MemoryStream(data), Encoding.UTF8); }
		public sbyte readByte() => _br.ReadSByte();
		public short readShort() => _br.ReadInt16();
		public string readUTF() => _br.ReadString();
	}

	public class Message
	{
		public sbyte command;
		private readonly myWriter _writer = new myWriter();
		private myReader _reader;
		public Message() { }
		public Message(sbyte cmd) { command = cmd; }
		public Message(sbyte cmd, byte[] data) { command = cmd; _reader = new myReader(data); }
		public myWriter writer() => _writer;
		public myReader reader() => _reader ??= new myReader(_writer.ToArray());
		public void cleanup() { }
	}

	public class Session_ME
	{
		private static Session_ME _instance;
		public static readonly List<Message> SentMessages = new List<Message>();
		public static Session_ME gI() => _instance ??= new Session_ME();
		public void sendMessage(Message m) => SentMessages.Add(m);
	}

	public class Service
	{
		private static Service _instance;
		public static readonly List<string> Calls = new List<string>();
		public static Service gI() => _instance ??= new Service();

		public void chat(string t) => Calls.Add("chat:" + t);
		public void charMove() => Calls.Add("charMove:" + Char.myCharz().cx + "," + Char.myCharz().cy);
		public void openMenu(int npcId) => Calls.Add("openMenu:" + npcId);
		public void confirmMenu(short npcID, sbyte select) => Calls.Add("confirmMenu:" + npcID + ":" + select);
		public void menu(int npcId, int menuId, int optionId) => Calls.Add("menu:" + npcId + ":" + menuId + ":" + optionId);
		public void menuId(short menuId) => Calls.Add("menuId:" + menuId);
		public void buyItem(sbyte type, int id, int quantity) => Calls.Add("buyItem:" + type + ":" + id + ":" + quantity);
		public void useItem(sbyte type, sbyte where, sbyte index, short template) => Calls.Add("useItem:" + type + ":" + where + ":" + index + ":" + template);
		public void sendClientInput(TField[] t)
		{
			var vals = new string[t.Length];
			for (int i = 0; i < t.Length; i++) vals[i] = t[i].getText();
			Calls.Add("sendClientInput:" + t.Length + ":" + string.Join("|", vals));
		}
	}

	public class ChatTextField
	{
		private static ChatTextField _instance;
		public bool isShow;
		public string strChat;
		public TField tfChat = new TField();
		public string to;
		public IChatable parentScreen;
		public static ChatTextField gI() => _instance ??= new ChatTextField();
		public void startChat(IChatable p, string t) { parentScreen = p; }
	}

	public class Panel
	{
		public bool isShow;
		public void hideNow() { isShow = false; }
	}

	public class Menu
	{
		public bool showMenu;
		public bool close;
		public void doCloseMenu() { showMenu = false; }
	}

	public static class InfoDlg
	{
		public static bool isShow;
		public static void hide() { isShow = false; }
	}

	public static class ChatPopup
	{
		public static object currChatPopup;
	}

	public static class GameCanvas
	{
		public static int w = 800;
		public static int h = 600;
		public static Panel panel = new Panel();
		public static Panel panel2 = null;
		public static Menu menu = new Menu();
		public static object currentDialog;
		public static object currentScreen;
		public static bool isPointerClick;
		public static bool isPointerJustRelease;
		public static bool isPointerDown;
		public static bool isPointerJustDown;
		public static int px;
		public static int py;
		public static bool isTouch = true;
		public static int EndDlgCount;
		public static void endDlg() { currentDialog = null; EndDlgCount++; }
		public static void clearAllPointerEvent() { isPointerClick = false; isPointerJustRelease = false; isPointerDown = false; }
		public static bool isPointerHoldIn(int x, int y, int w, int h) => false;
	}

	public class Info1
	{
		public readonly List<string> Messages = new List<string>();
		public void addInfo(string s, int c) => Messages.Add(s);
	}

	public class GameScr
	{
		public static GameScr instance = new GameScr();
		public static Info1 info1 = new Info1();
		public void switchToMe() { GameCanvas.currentScreen = this; }
	}

	public static class NinjaUtil
	{
		public static string getMoneys(long m) => m.ToString("N0");
	}

	public class mGraphics
	{
		public void setColor(int rgb) { }
		public void setColor(int rgb, float a) { }
		public void fillRect(int x, int y, int w, int h, int r = 0) { }
		public void drawRect(int x, int y, int w, int h) { }
		public void drawLine(int x1, int y1, int x2, int y2) { }
	}

	public class mFont
	{
		public const int LEFT = 0;
		public const int RIGHT = 1;
		public const int CENTER = 2;
		public static mFont tahoma_7b_yellow = new mFont();
		public static mFont tahoma_7_grey = new mFont();
		public static mFont tahoma_7b_white = new mFont();
		public static mFont tahoma_7_yellow = new mFont();
		public static mFont tahoma_7_white = new mFont();
		public static mFont tahoma_7_red = new mFont();
		public static mFont tahoma_7_green = new mFont();
		public static mFont tahoma_7b_green = new mFont();
		public void drawString(mGraphics g, string s, int x, int y, int align) { }
		public void drawStringBorder(mGraphics g, string s, int x, int y, int align, mFont border) { }
	}
}

