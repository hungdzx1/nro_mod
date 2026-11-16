using System.Collections.Generic;

// Test-only stand-in for the launcher's per-window PlayerPrefs scope.
public static class NroInstanceProfile
{
	private static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
	public static int Slot;
	public static int Writes;

	private static string Scoped(string key) => Slot + "." + key;

	public static string GetString(string key) => Values.TryGetValue(Scoped(key), out string value) ? value : string.Empty;

	public static void SetString(string key, string value)
	{
		Values[Scoped(key)] = value;
		Writes++;
	}

	public static void Clear()
	{
		Values.Clear();
		Slot = 0;
		Writes = 0;
	}
}
