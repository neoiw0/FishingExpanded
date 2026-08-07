using System;
using System.Collections.Generic;
using System.Text.Json;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>难度等级管理器（BATCH-027：按玩家隔离，支持双人同屏与联机）</summary>
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

        // BATCH-027: 每位玩家独立缓存（键=UniqueMultiplayerID）。双人同屏每位本地玩家有独立 Game1 实例，
        // Game1.player 随屏幕切换；联机各客户端只操作本机玩家。难度/星标/加成一律按玩家隔离。
        private static readonly Dictionary<long, FishDifficultyData> _dataByPlayer =
            new Dictionary<long, FishDifficultyData>();

        /// <summary>加载指定玩家的存档数据（惰性；首次访问时执行）。</summary>
        public static void LoadData(Farmer player)
        {
            if (player == null)
                return;

            FishDifficultyData data = ReadFromModData(player);

            // 只让主玩家迁移旧的存档级数据，避免把旧共享进度复制给每个联机玩家。
            if (data == null && Context.IsMainPlayer)
            {
                try
                {
                    data = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>(LegacySaveDataKey);
                    if (data != null)
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

            data ??= new FishDifficultyData();
            NormalizeData(data);
            _dataByPlayer[player.UniqueMultiplayerID] = data;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 加载玩家数据完成 | 玩家: {player.UniqueMultiplayerID} | 鱼种类数: {data.FishStatistics.Count} | " +
                $"钓鱼等级加成: +{data.FishingLevelBonus:F1} | 星标鱼种: {data.CollectionStars.Count}",
                LogLevel.Info);
        }

        /// <summary>保存指定玩家存档数据（每次写入立即持久化到该玩家 modData）。</summary>
        public static void SaveData(Farmer player)
        {
            if (player == null || !_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out FishDifficultyData data))
                return;

            NormalizeData(data);
            player.modData[PlayerDataKey] = JsonSerializer.Serialize(data, JsonOptions);
            ModEntry.ModMonitor.Log(
                $"已保存玩家钓鱼难度数据 | 玩家: {player.UniqueMultiplayerID}",
                LogLevel.Debug);
        }

        /// <summary>保存所有已缓存玩家（Saving 兜底；正常写入路径已即时保存）。</summary>
        public static void SaveAll()
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (_dataByPlayer.ContainsKey(farmer.UniqueMultiplayerID))
                    SaveData(farmer);
            }
        }

        /// <summary>清除所有玩家数据缓存（返回标题/换存档时）。</summary>
        public static void UnloadData()
        {
            _dataByPlayer.Clear();
        }

        /// <summary>获取某个物品的难度上限（与玩家无关的静态规则）。</summary>
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

        /// <summary>获取指定玩家某种鱼的当前难度等级（BATCH-015: 支持非鱼类上限）</summary>
        public static int GetDifficultyLevel(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return 0;

            // 钳制到范围
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            return Math.Max(MinDifficultyLevel, Math.Min(maxLevel, stats.DifficultyLevel));
        }

        /// <summary>BATCH-010: 记录指定玩家钓鱼成功（支持可变等级增长）</summary>
        public static int RecordSuccess(string fishId, int levelGain, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", StardewModdingAPI.LogLevel.Error);
                return 0;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return 0;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            int nextRankCeiling = Math.Min(DifficultyCalculator.GetNextRankCeiling(oldLevel), maxLevel);
            int allowedGain = Math.Max(0, Math.Min(levelGain, nextRankCeiling - oldLevel));
            stats.SuccessCount = SaturatingAdd(stats.SuccessCount, allowedGain); // BATCH-010: 使用可变增长
            stats.ConsecutiveFailCount = 0; // BATCH-029: 成功清零连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"请求增长: +{levelGain} | 实际增长: +{allowedGain} | 成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData(player);
            return allowedGain;
        }

        /// <summary>记录指定玩家钓鱼失败</summary>
        public static void RecordFailure(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            stats.FailCount = SaturatingAdd(stats.FailCount, 1);
            stats.ConsecutiveFailCount = SaturatingAdd(stats.ConsecutiveFailCount, 1); // BATCH-029: 连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | 连续失败次数: {stats.ConsecutiveFailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData(player);
        }

        /// <summary>记录达到difficulty≥120的鱼（按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (adjustedDifficulty >= 120f)
            {
                if (!data.CollectionStars.Contains(normalizedFishId))
                {
                    data.CollectionStars.Add(normalizedFishId);
                    data.FishingLevelBonus = data.CollectionStars.Count * 0.5f;

                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                        $"调整后难度: {adjustedDifficulty:F1} | " +
                        $"新增钓鱼等级加成: +0.5 | 总加成: +{data.FishingLevelBonus:F1}",
                        LogLevel.Warn); // Warn级别确保显眼

                    SaveData(player);
                }
            }
        }

        /// <summary>获取指定玩家钓鱼等级隐藏加成</summary>
        public static float GetFishingLevelBonus(Farmer player)
        {
            return GetData(player)?.FishingLevelBonus ?? 0f;
        }

        /// <summary>检查指定玩家某种鱼是否有收藏星标</summary>
        public static bool HasCollectionStar(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            return data != null && !string.IsNullOrEmpty(normalizedFishId) &&
                data.CollectionStars.Contains(normalizedFishId);
        }

        /// <summary>直接设置指定玩家某种鱼的难度等级（用于测试）</summary>
        public static void SetDifficultyLevel(string fishId, int targetLevel, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;
            targetLevel = Math.Max(MinDifficultyLevel, Math.Min(GetMaxDifficultyLevel(normalizedFishId), targetLevel));

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
            }

            // 通过调整成功次数来达到目标等级
            // 难度等级 = 成功次数 - 失败次数
            stats.SuccessCount = targetLevel + stats.FailCount;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 已设置等级 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | 目标等级: {targetLevel} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount}",
                StardewModdingAPI.LogLevel.Info);

            SaveData(player);
        }

        /// <summary>BATCH-029: 获取指定玩家某种鱼的连续失败次数（鱼王/无数据返回 0）</summary>
        public static int GetConsecutiveFailCount(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return 0;

            return Math.Max(0, stats.ConsecutiveFailCount);
        }

        /// <summary>获取指定玩家某种鱼的统计数据</summary>
        public static FishStats GetFishStats(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return null;

            return stats;
        }

        /// <summary>获取指定玩家所有鱼的统计数据</summary>
        public static System.Collections.Generic.Dictionary<string, FishStats> GetAllFishStats(Farmer player)
        {
            return GetData(player)?.FishStatistics ?? new System.Collections.Generic.Dictionary<string, FishStats>();
        }

        /// <summary>清空指定玩家所有数据（用于测试）</summary>
        public static void ClearAllData(Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            data.FishStatistics.Clear();
            data.CollectionStars.Clear();
            data.FishingLevelBonus = 0f;

            ModEntry.ModMonitor.Log("[DifficultyManager] 已清空所有数据", StardewModdingAPI.LogLevel.Info);
            SaveData(player);
        }

        /// <summary>记录高难度鱼（强制添加星标，用于测试；按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);
                data.FishingLevelBonus = data.CollectionStars.Count * 0.5f;

                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"新增钓鱼等级加成: +0.5 | 总加成: +{data.FishingLevelBonus:F1}",
                    StardewModdingAPI.LogLevel.Warn);

                SaveData(player);
            }
            else
            {
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] 鱼 {normalizedFishId} 已有收藏星标",
                    LogLevel.Info);
            }
        }

        /// <summary>获取指定玩家收藏星标总数</summary>
        public static int GetCollectionStarCount(Farmer player)
        {
            return GetData(player)?.CollectionStars.Count ?? 0;
        }

        /// <summary>获取指定玩家的难度数据（惰性加载并缓存）。</summary>
        private static FishDifficultyData GetData(Farmer player)
        {
            if (player == null)
                return null;

            if (!_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var data))
            {
                LoadData(player);
                _dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out data);
            }
            return data;
        }

        private static FishDifficultyData ReadFromModData(Farmer player)
        {
            if (player.modData.TryGetValue(PlayerDataKey, out string serializedData) &&
                !string.IsNullOrWhiteSpace(serializedData))
            {
                try
                {
                    return JsonSerializer.Deserialize<FishDifficultyData>(serializedData, JsonOptions);
                }
                catch (JsonException ex)
                {
                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {player.UniqueMultiplayerID} | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }
            return null;
        }

        private static void NormalizeData(FishDifficultyData data)
        {
            var normalizedStats = new Dictionary<string, FishStats>();
            foreach (var entry in data.FishStatistics ?? new Dictionary<string, FishStats>())
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
            foreach (string fishId in data.CollectionStars ?? new HashSet<string>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                if (!string.IsNullOrEmpty(normalizedFishId) && !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                    normalizedStars.Add(normalizedFishId);
            }

            data.FishStatistics = normalizedStats;
            data.CollectionStars = normalizedStars;
            data.FishingLevelBonus = normalizedStars.Count * 0.5f;
        }

        private static int SaturatingAdd(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : total < int.MinValue ? int.MinValue : (int)total;
        }
    }
}