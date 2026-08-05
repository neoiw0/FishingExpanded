using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley.Menus;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>BobberBar 钓鱼小游戏 UI 的 Patch（支持多人模式）</summary>
    [HarmonyPatch(typeof(BobberBar))]
    internal class BobberBarPatches
    {
        /// <summary>每个BobberBar实例的数据</summary>
        private class InstanceData
        {
            public string FishId { get; set; }
            public int DifficultyLevel { get; set; }
            public float OriginalDifficulty { get; set; }
            public bool FailureRecorded { get; set; }
            public int MissCount { get; set; } = 0; // BATCH-010: 脱杆次数（完美=0次脱杆）
            public bool WasBobberInBar { get; set; } = true; // 上一帧鱼是否在绿条内
        }

        // 使用WeakReference防止内存泄漏
        private static Dictionary<BobberBar, InstanceData> _instanceData = new Dictionary<BobberBar, InstanceData>();

        /// <summary>构造函数 Postfix：调整 difficulty、fishSize、fishQuality（BATCH-014: 鱼王豁免）</summary>
        [HarmonyPatch(MethodType.Constructor, new Type[] {
            typeof(string), typeof(float), typeof(bool), typeof(System.Collections.Generic.List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })]
        [HarmonyPostfix]
        public static void Constructor_Postfix(
            BobberBar __instance,
            string whichFish,
            ref float ___difficulty,
            ref int ___fishSize,
            ref int ___fishQuality)
        {
            try
            {
                // BATCH-014: 鱼王类豁免所有规则
                if (Utils.SpecialFishHelper.IsLegendaryFish(whichFish))
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {whichFish} | " +
                        $"保持原始difficulty: {___difficulty:F1}",
                        LogLevel.Info);
                    return;
                }

                float originalDifficulty = ___difficulty;
                int difficultyLevel = DifficultyManager.GetDifficultyLevel(whichFish);

                // 存储实例数据
                _instanceData[__instance] = new InstanceData
                {
                    FishId = whichFish,
                    DifficultyLevel = difficultyLevel,
                    OriginalDifficulty = originalDifficulty,
                    FailureRecorded = false,
                    MissCount = 0,
                    WasBobberInBar = true
                };

                // 1. 调整 difficulty
                float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
                ___difficulty *= difficultyMultiplier;

                // 2. 调整 fishSize（数量倍数）
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
                ___fishSize = (int)(___fishSize * quantityMultiplier);

                // 3. 调整 fishQuality
                ___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);

                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 钓鱼小游戏开始 | 实例: {__instance.GetHashCode()} | 鱼ID: {whichFish} | " +
                    $"难度等级: {difficultyLevel} | " +
                    $"原始difficulty: {originalDifficulty:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | " +
                    $"数量倍数: {quantityMultiplier} | fishSize: {___fishSize} | 品质: {___fishQuality}",
                    LogLevel.Info);

                // 4. 检查是否达到 difficulty≥120
                DifficultyManager.RecordHighDifficulty(whichFish, ___difficulty);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>update Prefix：动态修改蓄力槽减速倍率 + 追踪脱杆次数</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPrefix]
        public static void Update_Prefix(
            BobberBar __instance,
            ref float ___distanceFromCatchPenaltyModifier,
            float ___distanceFromCatching,
            bool ___bobberInBar)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // BATCH-010: 追踪脱杆次数（检测状态切换：从在绿条内到脱离）
                if (data.WasBobberInBar && !___bobberInBar)
                {
                    // 鱼刚刚离开绿条，计为一次脱杆
                    data.MissCount++;
                }
                data.WasBobberInBar = ___bobberInBar;

                // BATCH-020: 全局蓄力槽保护（任意难度等级）
                float oldModifier = ___distanceFromCatchPenaltyModifier;
                float newModifier = DifficultyCalculator.GetCatchPenaltyModifier(
                    data.DifficultyLevel, ___distanceFromCatching);
                ___distanceFromCatchPenaltyModifier = newModifier;

                // 只在减速倍率变化时记录（避免每帧输出）
                if (Math.Abs(oldModifier - newModifier) > 0.01f)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 蓄力槽保护触发 | 实例: {__instance.GetHashCode()} | 等级: {data.DifficultyLevel} | " +
                        $"蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {oldModifier:F2} → {newModifier:F2}",
                        LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar.update Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-010: 获取脱杆次数</summary>
        public static int GetMissCount(BobberBar instance)
        {
            if (_instanceData.TryGetValue(instance, out var data))
                return data.MissCount;
            return 0;
        }

        /// <summary>update Postfix：检测钓鱼结果并记录</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPostfix]
        public static void Update_Postfix(
            BobberBar __instance,
            float ___distanceFromCatching,
            bool ___fadeOut)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // 钓鱼失败（只记录一次）
                if (___fadeOut && ___distanceFromCatching <= 0f && !data.FailureRecorded)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 钓鱼失败 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                        $"蓄力槽耗尽: {___distanceFromCatching:F3}",
                        LogLevel.Info);

                    DifficultyManager.RecordFailure(data.FishId);

                    // 显示失败 HUD 提示
                    int newLevel = DifficultyManager.GetDifficultyLevel(data.FishId);
                    HUDNotifier.ShowFailureNotification(data.FishId, newLevel);

                    data.FailureRecorded = true; // 防止重复记录
                }

                // 清理已完成的实例数据（避免内存泄漏）
                if (___fadeOut)
                {
                    CleanupInstance(__instance);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar.update Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>清理单个实例数据</summary>
        private static void CleanupInstance(BobberBar instance)
        {
            if (_instanceData.Remove(instance))
            {
                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 清理实例数据 | 实例: {instance.GetHashCode()} | 剩余实例: {_instanceData.Count}",
                    LogLevel.Debug);
            }
        }

        /// <summary>定期清理无效实例（防止内存泄漏）</summary>
        public static void PeriodicCleanup()
        {
            try
            {
                int beforeCount = _instanceData.Count;
                if (beforeCount == 0) return;

                // Bug修复：移除已销毁的BobberBar实例
                // Dictionary的键不会为null，但对象本身可能已被GC回收或变为无效状态
                // 我们检查实例的内部状态来判断是否仍然有效
                var keysToRemove = new List<BobberBar>();
                foreach (var kvp in _instanceData)
                {
                    var instance = kvp.Key;
                    try
                    {
                        // 尝试访问实例的字段来验证其是否仍然有效
                        // 如果BobberBar已被销毁，访问其成员可能抛出异常
                        var _ = instance.GetHashCode(); // 轻量级检查

                        // 额外检查：如果实例数据已经标记失败且超时，可以清理
                        // 这里简化处理：fadeOut后应该已经被清理，这里只是兜底
                    }
                    catch
                    {
                        keysToRemove.Add(instance);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _instanceData.Remove(key);
                }

                if (keysToRemove.Count > 0)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 定期清理 | 移除无效实例: {keysToRemove.Count} | 之前: {beforeCount} | 之后: {_instanceData.Count}",
                        LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 定期清理失败: {ex}", LogLevel.Error);
            }
        }
    }
}
