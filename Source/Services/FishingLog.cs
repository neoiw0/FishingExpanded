using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace FishingExpanded.Services
{
    /// <summary>统一日志入口（唯一日志写入者）。分级门控：Trace/Debug/Info 受 config.EnableLogging 开关控制
    /// （默认 false=玩家安装无调试输出）；Warn/Error/Alert 无视开关始终输出——"没有调试信息，除非出现错误"。
    /// 每帧/高频路径必须使用 LogRateLimited，禁止直接 Log 造成疏忽性每帧输出（BATCH-040）。</summary>
    public static class FishingLog
    {
        /// <summary>全局调试日志开关（由 ModEntry 从 config 载入；GMCM 修改时实时更新）。
        /// 仅约束 Trace/Debug/Info；Warn 及以上始终输出。</summary>
        public static bool Enabled { get; set; } = false;

        /// <summary>限频缓存键数上限；超过时整体清空，防止无界增长。</summary>
        public const int MaxRateLimitEntries = 64;

        // key -> 上次输出时刻（Environment.TickCount64）
        private static readonly Dictionary<string, long> _lastLoggedByKey = new Dictionary<string, long>();

        /// <summary>常规日志：Trace/Debug/Info 受 EnableLogging 控制；Warn/Error/Alert 始终输出。</summary>
        public static void Log(string message, LogLevel level)
        {
            if (ModEntry.ModMonitor == null)
                return;

            if (!Enabled && level < LogLevel.Warn)
                return;

            ModEntry.ModMonitor.Log(message, level);
        }

        /// <summary>限频日志：同一 key 在 intervalSeconds 内最多输出一条；用于每帧路径的异常兜底
        /// （有意为之的有限每帧输出，非疏忽性刷屏）。Warn 及以上无视开关始终输出。</summary>
        public static void LogRateLimited(string key, string message, LogLevel level, double intervalSeconds = 30.0)
        {
            if (ModEntry.ModMonitor == null)
                return;

            if (!Enabled && level < LogLevel.Warn)
                return;

            long now = Environment.TickCount64;
            if (_lastLoggedByKey.TryGetValue(key, out long last) &&
                now - last < (long)(intervalSeconds * 1000.0))
            {
                return;
            }

            if (_lastLoggedByKey.Count >= MaxRateLimitEntries)
            {
                // 上限清理：限频键均为低频异常路径，清空损失可接受；防无界增长。
                _lastLoggedByKey.Clear();
            }

            _lastLoggedByKey[key] = now;
            ModEntry.ModMonitor.Log(message, level);
        }

        /// <summary>只读自测：当前限频缓存条目数。</summary>
        public static int RateLimitCacheCount()
        {
            return _lastLoggedByKey.Count;
        }
    }
}