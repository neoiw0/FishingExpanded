using System;
using FishingExpanded.Data;
using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>难度等级管理器</summary>
    public static class DifficultyManager
    {
        private const int MinDifficultyLevel = -10;
        private const int MaxDifficultyLevel = 100;

        private static FishDifficultyData _data;

        /// <summary>初始化或加载存档数据</summary>
        public static void Initialize()
        {
            LoadData();
        }

        /// <summary>加载存档数据</summary>
        public static void LoadData()
        {
            _data = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>("FishDifficultyData")
                    ?? new FishDifficultyData();

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 加载存档数据完成 | 鱼种类数: {_data.FishStatistics.Count} | " +
                $"钓鱼等级加成: +{_data.FishingLevelBonus:F1} | 星标鱼种: {_data.CollectionStars.Count}",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>保存存档数据</summary>
        public static void SaveData()
        {
            if (_data == null) return;

            ModEntry.ModHelper.Data.WriteSaveData("FishDifficultyData", _data);
            ModEntry.ModMonitor.Log("已保存钓鱼难度数据", StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>获取某种鱼的当前难度等级（BATCH-015: 支持非鱼类上限）</summary>
        public static int GetDifficultyLevel(string fishId)
        {
            if (_data == null || !_data.FishStatistics.TryGetValue(fishId, out var stats))
                return 0;

            // BATCH-015: 获取物品Category检查是否为非鱼类
            var itemData = StardewValley.ItemRegistry.GetDataOrErrorItem(fishId);
            int maxLevel = MaxDifficultyLevel;

            if (itemData != null && Utils.SpecialFishHelper.IsNonFish(itemData.Category))
            {
                maxLevel = Utils.SpecialFishHelper.GetMaxLevelForNonFish(); // 非鱼类上限8
            }

            // 钳制到范围
            return Math.Max(MinDifficultyLevel, Math.Min(maxLevel, stats.DifficultyLevel));
        }

        /// <summary>BATCH-010: 记录钓鱼成功（支持可变等级增长）</summary>
        public static void RecordSuccess(string fishId, int levelGain = 1)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null，无法记录成功", StardewModdingAPI.LogLevel.Error);
                return;
            }

            if (!_data.FishStatistics.TryGetValue(fishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[fishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {fishId}", StardewModdingAPI.LogLevel.Debug);
            }

            int oldLevel = stats.DifficultyLevel;
            stats.SuccessCount += levelGain; // BATCH-010: 使用可变增长
            int newLevel = GetDifficultyLevel(fishId);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼成功记录 | 鱼ID: {fishId} | " +
                $"等级增长: +{levelGain} | 成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                StardewModdingAPI.LogLevel.Info);

            SaveData();
        }

        /// <summary>记录钓鱼失败</summary>
        public static void RecordFailure(string fishId)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null，无法记录失败", StardewModdingAPI.LogLevel.Error);
                return;
            }

            if (!_data.FishStatistics.TryGetValue(fishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[fishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {fishId}", StardewModdingAPI.LogLevel.Debug);
            }

            int oldLevel = stats.DifficultyLevel;
            stats.FailCount++;
            int newLevel = GetDifficultyLevel(fishId);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼失败记录 | 鱼ID: {fishId} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                StardewModdingAPI.LogLevel.Info);

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

            if (adjustedDifficulty >= 120f)
            {
                if (!_data.CollectionStars.Contains(fishId))
                {
                    _data.CollectionStars.Add(fishId);
                    _data.FishingLevelBonus += 0.5f;

                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] ★ 达成高难度里程碑 ★ | 鱼ID: {fishId} | " +
                        $"调整后难度: {adjustedDifficulty:F1} | " +
                        $"新增钓鱼等级加成: +0.5 | 总加成: +{_data.FishingLevelBonus:F1}",
                        StardewModdingAPI.LogLevel.Warn); // Warn级别确保显眼

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
            return _data?.CollectionStars.Contains(fishId) ?? false;
        }

        /// <summary>直接设置某种鱼的难度等级（用于测试）</summary>
        public static void SetDifficultyLevel(string fishId, int targetLevel)
        {
            if (_data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: _data为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            targetLevel = Math.Max(MinDifficultyLevel, Math.Min(MaxDifficultyLevel, targetLevel));

            if (!_data.FishStatistics.TryGetValue(fishId, out var stats))
            {
                stats = new FishStats();
                _data.FishStatistics[fishId] = stats;
            }

            // 通过调整成功次数来达到目标等级
            // 难度等级 = 成功次数 - 失败次数
            stats.SuccessCount = targetLevel + stats.FailCount;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 已设置等级 | 鱼ID: {fishId} | 目标等级: {targetLevel} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount}",
                StardewModdingAPI.LogLevel.Info);

            SaveData();
        }

        /// <summary>获取某种鱼的统计数据</summary>
        public static FishStats GetFishStats(string fishId)
        {
            if (_data == null || !_data.FishStatistics.TryGetValue(fishId, out var stats))
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

            if (!_data.CollectionStars.Contains(fishId))
            {
                _data.CollectionStars.Add(fishId);
                _data.FishingLevelBonus += 0.5f;

                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 添加收藏星标 ★ | 鱼ID: {fishId} | " +
                    $"新增钓鱼等级加成: +0.5 | 总加成: +{_data.FishingLevelBonus:F1}",
                    StardewModdingAPI.LogLevel.Warn);

                SaveData();
            }
            else
            {
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] 鱼 {fishId} 已有收藏星标",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>获取收藏星标总数</summary>
        public static int GetCollectionStarCount()
        {
            return _data?.CollectionStars.Count ?? 0;
        }
    }
}
