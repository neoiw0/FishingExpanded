using System;
using System.Collections.Generic;
using System.Text.Json;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>难度等级管理器</summary>
    public static class DifficultyManager
    {
        private const int MinDifficultyLevel = -10;
        private const int MaxDifficultyLevel = 100;
        private const string PlayerDataKey = "FishingExpanded/FishDifficultyData";
        private const string LegacySaveDataKey = "FishDifficultyData";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static FishDifficultyData _data;

        /// <summary>初始化或加载存档数据</summary>
        public static void Initialize()
        {
            LoadData();
        }

        /// <summary>加载存档数据</summary>
        public static void LoadData()
        {
            _data = null;

            if (Game1.player != null && Game1.player.modData.TryGetValue(PlayerDataKey, out string serializedData) &&
                !string.IsNullOrWhiteSpace(serializedData))
            {
                try
                {
                    _data = JsonSerializer.Deserialize<FishDifficultyData>(serializedData, JsonOptions);
                }
                catch (JsonException ex)
                {
                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {Game1.player.UniqueMultiplayerID} | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }

            // 只让主玩家迁移旧的存档级数据，避免把旧共享进度复制给每个联机玩家。
            if (_data == null && Context.IsMainPlayer)
            {
                try
                {
                    _data = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>(LegacySaveDataKey);
                    if (_data != null)
                    {
                        ModEntry.ModMonitor.Log(
                            "[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据",
                            LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }

            _data ??= new FishDifficultyData();
            NormalizeData();

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 加载玩家数据完成 | 玩家: {Game1.player?.UniqueMultiplayerID} | 鱼种类数: {_data.FishStatistics.Count} | " +
                $"钓鱼等级加成: +{_data.FishingLevelBonus:F1} | 星标鱼种: {_data.CollectionStars.Count}",
                LogLevel.Info);
        }

        /// <summary>保存存档数据</summary>
        public static void SaveData()
        {
            if (_data == null || Game1.player == null)
                return;

            NormalizeData();
            Game1.player.modData[PlayerDataKey] = JsonSerializer.Serialize(_data, JsonOptions);
            ModEntry.ModMonitor.Log(
                $"已保存玩家钓鱼难度数据 | 玩家: {Game1.player.UniqueMultiplayerID}",
                LogLevel.Debug);
        }

        /// <summary>清除当前进程的玩家数据缓存</summary>
        public static void UnloadData()
        {
            _data = null;
        }

        /// <summary>获取某个物品的难度上限</summary>
        public static int GetMaxDifficultyLevel(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId))
                return MaxDifficultyLevel;

            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(normalizedFishId);
                return itemData != null && Utils.SpecialFishHelper.IsNonFish(itemData.Category)
                    ? Utils.SpecialFishHelper.GetMaxLevelForNonFish()
                    : MaxDifficultyLevel;
            }
            catch
            {
                return MaxDifficultyLevel;
            }
        }

        /// <summary>获取某种鱼的当前难度等级（BATCH-015: 支持非鱼类上限）</summary>
        public static int GetDifficultyLevel(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (_data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !_data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return 0;

            // 钳制到范围
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            return Math.Max(MinDifficultyLevel, Math.Min(maxLevel, stats.DifficultyLevel));
        }

        /// <summary>BATCH-010: 记录钓鱼成功（支持可变等级增长）</summary>
        public static int RecordSuccess(string fishId, int levelGain = 1)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null，无法记录成功", StardewModdingAPI.LogLevel.Error);
                return 0;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return 0;

            if (!_data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId);
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            int nextRankCeiling = Math.Min(DifficultyCalculator.GetNextRankCeiling(oldLevel), maxLevel);
            int allowedGain = Math.Max(0, Math.Min(levelGain, nextRankCeiling - oldLevel));
            stats.SuccessCount = SaturatingAdd(stats.SuccessCount, allowedGain); // BATCH-010: 使用可变增长
            int newLevel = GetDifficultyLevel(normalizedFishId);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼成功记录 | 鱼ID: {normalizedFishId} | " +
                $"请求增长: +{levelGain} | 实际增长: +{allowedGain} | 成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData();
            return allowedGain;
        }

        /// <summary>记录钓鱼失败</summary>
        public static void RecordFailure(string fishId)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null，无法记录失败", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!_data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId);
            stats.FailCount = SaturatingAdd(stats.FailCount, 1);
            int newLevel = GetDifficultyLevel(normalizedFishId);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼失败记录 | 鱼ID: {normalizedFishId} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData();
        }

        /// <summary>记录达到difficulty≥120的鱼</summary>
        public static void RecordHighDifficulty(string fishId, float adjustedDifficulty)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null，无法记录高难度", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (adjustedDifficulty >= 120f)
            {
                if (!_data.CollectionStars.Contains(normalizedFishId))
                {
                    _data.CollectionStars.Add(normalizedFishId);
                    _data.FishingLevelBonus = _data.CollectionStars.Count * 0.5f;

                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] ★ 达成高难度里程碑 ★ | 鱼ID: {normalizedFishId} | " +
                        $"调整后难度: {adjustedDifficulty:F1} | " +
                        $"新增钓鱼等级加成: +0.5 | 总加成: +{_data.FishingLevelBonus:F1}",
                        LogLevel.Warn); // Warn级别确保显眼

                    SaveData();
                }
            }
        }

        /// <summary>获取钓鱼等级隐藏加成</summary>
        public static float GetFishingLevelBonus()
        {
            return _data?.FishingLevelBonus ?? 0f;
        }

        /// <summary>检查某种鱼是否有收藏星标</summary>
        public static bool HasCollectionStar(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            return _data != null && !string.IsNullOrEmpty(normalizedFishId) &&
                _data.CollectionStars.Contains(normalizedFishId);
        }

        /// <summary>直接设置某种鱼的难度等级（用于测试）</summary>
        public static void SetDifficultyLevel(string fishId, int targetLevel)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;
            targetLevel = Math.Max(MinDifficultyLevel, Math.Min(GetMaxDifficultyLevel(normalizedFishId), targetLevel));

            if (!_data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[normalizedFishId] = stats;
            }

            // 通过调整成功次数来达到目标等级
            // 难度等级 = 成功次数 - 失败次数
            stats.SuccessCount = targetLevel + stats.FailCount;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 已设置等级 | 鱼ID: {normalizedFishId} | 目标等级: {targetLevel} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount}",
                StardewModdingAPI.LogLevel.Info);

            SaveData();
        }

        /// <summary>获取某种鱼的统计数据</summary>
        public static FishStats GetFishStats(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (_data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !_data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return null;

            return stats;
        }

        /// <summary>获取所有鱼的统计数据</summary>
        public static System.Collections.Generic.Dictionary<string, FishStats> GetAllFishStats()
        {
            return _data?.FishStatistics ?? new System.Collections.Generic.Dictionary<string, FishStats>();
        }

        /// <summary>清空所有数据（用于测试）</summary>
        public static void ClearAllData()
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            _data.FishStatistics.Clear();
            _data.CollectionStars.Clear();
            _data.FishingLevelBonus = 0f;

            ModEntry.ModMonitor.Log("[DifficultyManager] 已清空所有数据", StardewModdingAPI.LogLevel.Info);
            SaveData();
        }

        /// <summary>记录高难度鱼（强制添加星标，用于测试）</summary>
        public static void RecordHighDifficulty(string fishId)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!_data.CollectionStars.Contains(normalizedFishId))
            {
                _data.CollectionStars.Add(normalizedFishId);
                _data.FishingLevelBonus = _data.CollectionStars.Count * 0.5f;

                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 添加收藏星标 ★ | 鱼ID: {normalizedFishId} | " +
                    $"新增钓鱼等级加成: +0.5 | 总加成: +{_data.FishingLevelBonus:F1}",
                    StardewModdingAPI.LogLevel.Warn);

                SaveData();
            }
            else
            {
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] 鱼 {normalizedFishId} 已有收藏星标",
                    LogLevel.Info);
            }
        }

        /// <summary>获取收藏星标总数</summary>
        public static int GetCollectionStarCount()
        {
            return _data?.CollectionStars.Count ?? 0;
        }

        private static void NormalizeData()
        {
            if (_data == null)
                _data = new FishDifficultyData();

            var normalizedStats = new Dictionary<string, FishStats>();
            foreach (var entry in _data.FishStatistics ?? new Dictionary<string, FishStats>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(entry.Key);
                if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) || entry.Value == null)
                    continue;

                if (!normalizedStats.TryGetValue(normalizedFishId, out var existing))
                {
                    normalizedStats[normalizedFishId] = entry.Value;
                    continue;
                }

                existing.SuccessCount = SaturatingAdd(existing.SuccessCount, entry.Value.SuccessCount);
                existing.FailCount = SaturatingAdd(existing.FailCount, entry.Value.FailCount);
            }

            var normalizedStars = new HashSet<string>();
            foreach (string fishId in _data.CollectionStars ?? new HashSet<string>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                if (!string.IsNullOrEmpty(normalizedFishId) && !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                    normalizedStars.Add(normalizedFishId);
            }

            _data.FishStatistics = normalizedStats;
            _data.CollectionStars = normalizedStars;
            _data.FishingLevelBonus = normalizedStars.Count * 0.5f;
        }

        private static int SaturatingAdd(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : total < int.MinValue ? int.MinValue : (int)total;
        }
    }
}
