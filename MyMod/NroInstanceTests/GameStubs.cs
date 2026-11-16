using System;
using System.Collections.Generic;

public enum TabType { Tab1, Tab2 }

namespace UnityEngine
{
    public enum RuntimePlatform { WindowsPlayer, Other }
    public static class Application { public static RuntimePlatform platform = RuntimePlatform.Other; }
    public static class Debug { public static void LogError(string message) => throw new Exception(message); }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, string> Strings = new();
        public static readonly Dictionary<string, int> Ints = new();
        public static string GetString(string key) => Strings.TryGetValue(key, out string value) ? value : string.Empty;
        public static void SetString(string key, string value) => Strings[key] = value;
        public static int GetInt(string key, int fallback = 0) => Ints.TryGetValue(key, out int value) ? value : fallback;
        public static void SetInt(string key, int value) => Ints[key] = value;
        public static bool HasKey(string key) => Strings.ContainsKey(key) || Ints.ContainsKey(key);
        public static void DeleteKey(string key) { Strings.Remove(key); Ints.Remove(key); }
        public static void DeleteAll() { Strings.Clear(); Ints.Clear(); }
    }
}

namespace Game1
{
    public static class Main { public static string mainThreadName; }
    public static class Cout { public static void println(string text) => throw new Exception(text); }
    public static class ArrayCast
    {
        public static byte[] cast(sbyte[] data) => Array.ConvertAll(data, x => unchecked((byte)x));
        public static sbyte[] cast(byte[] data) => Array.ConvertAll(data, x => unchecked((sbyte)x));
    }
    public sealed class DataInputStream
    {
        public DataInputStream(sbyte[] data) { }
        public string readUTF() => string.Empty;
        public void close() { }
    }
    public sealed class DataOutputStream
    {
        public void writeUTF(string text) { }
        public sbyte[] toByteArray() => Array.Empty<sbyte>();
        public void close() { }
    }
}

namespace Game2
{
    public static class Main { public static string mainThreadName; }
    public static class Cout { public static void println(string text) => throw new Exception(text); }
    public static class ArrayCast
    {
        public static byte[] cast(sbyte[] data) => Array.ConvertAll(data, x => unchecked((byte)x));
        public static sbyte[] cast(byte[] data) => Array.ConvertAll(data, x => unchecked((sbyte)x));
    }
    public sealed class DataInputStream
    {
        public DataInputStream(sbyte[] data) { }
        public string readUTF() => string.Empty;
        public void close() { }
    }
    public sealed class DataOutputStream
    {
        public void writeUTF(string text) { }
        public sbyte[] toByteArray() => Array.Empty<sbyte>();
        public void close() { }
    }
}
