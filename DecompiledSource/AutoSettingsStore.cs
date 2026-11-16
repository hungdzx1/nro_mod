using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

// Mark only user preferences. Runtime state (running, retries, timers) must never be restored.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoSettingAttribute : Attribute
{
}

/// <summary>
/// Shared persistence for current and future autos. Each auto supplies a stable ID and marks
/// its configurable fields with AutoSetting. NroInstanceProfile scopes keys to the launcher slot.
/// </summary>
public static class AutoSettingsStore
{
	private const string KeyPrefix = "NroGoc.AutoSettings.v1.";
	private const string FormatHeader = "1\n";
	private static readonly Dictionary<Type, MemberInfo[]> MembersByType = new Dictionary<Type, MemberInfo[]>();

	private static MemberInfo[] Members(Type type)
	{
		if (MembersByType.TryGetValue(type, out MemberInfo[] cached))
		{
			return cached;
		}
		List<MemberInfo> members = new List<MemberInfo>();
		foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (Attribute.IsDefined(field, typeof(AutoSettingAttribute)))
			{
				members.Add(field);
			}
		}
		foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (property.CanRead && property.CanWrite && Attribute.IsDefined(property, typeof(AutoSettingAttribute)))
			{
				members.Add(property);
			}
		}
		members.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
		cached = members.ToArray();
		MembersByType[type] = cached;
		return cached;
	}

	private static Type ValueType(MemberInfo member)
	{
		return member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;
	}

	private static object GetValue(MemberInfo member, object target)
	{
		return member is FieldInfo field ? field.GetValue(target) : ((PropertyInfo)member).GetValue(target, null);
	}

	private static void SetValue(MemberInfo member, object target, object value)
	{
		if (member is FieldInfo field)
		{
			field.SetValue(target, value);
		}
		else
		{
			((PropertyInfo)member).SetValue(target, value, null);
		}
	}

	public static string Capture(object target)
	{
		StringBuilder data = new StringBuilder(FormatHeader);
		foreach (MemberInfo member in Members(target.GetType()))
		{
			object value = GetValue(member, target);
			string raw = ValueType(member) == typeof(bool)
				? ((bool)value ? "1" : "0")
				: Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
			data.Append(member.Name).Append(':').Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))).Append('\n');
		}
		return data.ToString();
	}

	public static string Load(object target, string autoId)
	{
		string data = NroInstanceProfile.GetString(KeyPrefix + autoId);
		if (string.IsNullOrEmpty(data) || !data.StartsWith(FormatHeader, StringComparison.Ordinal))
		{
			return data;
		}
		Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);
		foreach (MemberInfo member in Members(target.GetType()))
		{
			members[member.Name] = member;
		}
		foreach (string line in data.Substring(FormatHeader.Length).Split('\n'))
		{
			int separator = line.IndexOf(':');
			if (separator <= 0 || !members.TryGetValue(line.Substring(0, separator), out MemberInfo member))
			{
				continue;
			}
			try
			{
				string raw = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(separator + 1)));
				Type type = ValueType(member);
				object value = null;
				if (type == typeof(string))
				{
					value = raw;
				}
				else if (type == typeof(bool) && (raw == "0" || raw == "1"))
				{
					value = raw == "1";
				}
				else if (type == typeof(int) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
				{
					value = integer;
				}
				else if (type == typeof(sbyte) && sbyte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte small))
				{
					value = small;
				}
				if (value != null)
				{
					SetValue(member, target, value);
				}
			}
			catch (FormatException)
			{
				// A damaged value leaves only that setting at its default.
			}
		}
		return data;
	}

	public static void SaveIfChanged(object target, string autoId, ref string lastSnapshot)
	{
		string snapshot = Capture(target);
		if (snapshot == lastSnapshot)
		{
			return;
		}
		NroInstanceProfile.SetString(KeyPrefix + autoId, snapshot);
		lastSnapshot = snapshot;
	}
}
