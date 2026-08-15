using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace FishingExpanded.Services;

public static class FishingLog
{
	public const int MaxRateLimitEntries = 64;

	private static readonly Dictionary<string, long> _lastLoggedByKey = new Dictionary<string, long>();

	public static bool Enabled { get; set; } = true;

	public static void Log(string message, LogLevel level)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (Enabled && ModEntry.ModMonitor != null)
		{
			ModEntry.ModMonitor.Log(message, level);
		}
	}

	public static void LogRateLimited(string key, string message, LogLevel level, double intervalSeconds = 30.0)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled || ModEntry.ModMonitor == null)
		{
			return;
		}
		long tickCount = Environment.TickCount64;
		if (!_lastLoggedByKey.TryGetValue(key, out var value) || tickCount - value >= (long)(intervalSeconds * 1000.0))
		{
			if (_lastLoggedByKey.Count >= 64)
			{
				_lastLoggedByKey.Clear();
			}
			_lastLoggedByKey[key] = tickCount;
			ModEntry.ModMonitor.Log(message, level);
		}
	}

	public static int RateLimitCacheCount()
	{
		return _lastLoggedByKey.Count;
	}
}
