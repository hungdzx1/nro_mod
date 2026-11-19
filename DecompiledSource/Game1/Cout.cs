using System.Collections.Generic;
using UnityEngine;

namespace Game1;

public class Cout
{
	public static int count;

	private static readonly Dictionary<string, long> lastLogTime = new Dictionary<string, long>();

	public static void LogErrorThrottled(string key, string msg)
	{
		long num = mSystem.currentTimeMillis();
		if (!lastLogTime.TryGetValue(key, out var value) || num - value >= 5000)
		{
			lastLogTime[key] = num;
			Debug.LogError(msg);
		}
	}

	public static void println(string s)
	{
		if (mSystem.isTest)
		{
			Debug.Log(((count % 2 != 0) ? "***--- " : ">>>--- ") + s);
			count++;
		}
	}

	public static void Log(string str)
	{
		if (mSystem.isTest)
		{
			Debug.Log(str);
		}
	}

	public static void LogError(string str)
	{
		if (mSystem.isTest)
		{
			Debug.LogError(str);
		}
	}

	public static void LogError2(string str)
	{
		_ = mSystem.isTest;
	}

	public static void LogError3(string str)
	{
		if (mSystem.isTest)
		{
			Debug.LogError(str);
		}
	}

	public static void LogWarning(string str)
	{
		if (mSystem.isTest)
		{
			Debug.LogWarning(str);
		}
	}
}
