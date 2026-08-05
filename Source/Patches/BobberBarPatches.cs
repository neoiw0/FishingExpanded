using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
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
            public float AdjustedDifficulty { get; set; }
            public float NativeCatchPenaltyModifier { get; set; } = 1f;
            public float LastAppliedCatchPenaltyModifier { get; set; } = 1f;
            public bool FailureRecorded { get; set; }
            public bool ResultStarted { get; set; }
            public int MissCount { get; set; } = 0; // BATCH-010: 脱杆次数（完美=0次脱杆）
            public bool WasBobberInBar { get; set; } // 上一帧鱼是否在绿条内
        }

        // BobberBar 生命周期结束后自动释放，避免异常退出导致静态缓存持有实例。
        private static readonly ConditionalWeakTable<BobberBar, InstanceData> _instanceData =
            new ConditionalWeakTable<BobberBar, InstanceData>();

        /// <summary>构造鱼王小游戏时临时屏蔽隐藏钓鱼等级加成。</summary>
        [HarmonyPatch(MethodType.Constructor, new Type[] {
            typeof(string), typeof(float), typeof(bool), typeof(System.Collections.Generic.List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })]
        [HarmonyPrefix]
        public static void Constructor_Prefix(string whichFish)
        {
            FarmerFishingLevelPatches.SuppressHiddenBonusForLegendaryBobber =
                SpecialFishHelper.IsLegendaryFish(whichFish);
        }

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
            ref int ___fishQuality,
            ref float ___distanceFromCatchPenaltyModifier,
            bool ___bobberInBar)
        {
            try
            {
                // BATCH-014: 鱼王类豁免所有规则
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(whichFish);
                if (Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId} | " +
                        $"保持原始difficulty: {___difficulty:F1}",
                        LogLevel.Info);
                    return;
                }

                float originalDifficulty = ___difficulty;
                int difficultyLevel = DifficultyManager.GetDifficultyLevel(normalizedFishId);

                // 存储实例数据
                var instanceData = new InstanceData
                {
                    FishId = normalizedFishId,
                    DifficultyLevel = difficultyLevel,
                    OriginalDifficulty = originalDifficulty,
                    NativeCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
                    LastAppliedCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
                    FailureRecorded = false,
                    MissCount = 0,
                    WasBobberInBar = ___bobberInBar
                };
                _instanceData.Add(__instance, instanceData);

                // 1. 调整 difficulty
                float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
                ___difficulty *= difficultyMultiplier;

                // 2. 调整 fishSize（数量倍数）
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
                ___fishSize = (int)(___fishSize * quantityMultiplier);

                // 3. 调整 fishQuality
                ___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);

                instanceData.AdjustedDifficulty = ___difficulty;

                HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
                HUDNotifier.ShowStarChallengeNotification(normalizedFishId, difficultyLevel);

                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 钓鱼小游戏开始 | 实例: {__instance.GetHashCode()} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {difficultyLevel} | " +
                    $"原始difficulty: {originalDifficulty:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | " +
                    $"数量倍数: {quantityMultiplier} | fishSize: {___fishSize} | 品质: {___fishQuality}",
                    LogLevel.Info);

            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {ex}", LogLevel.Error);
            }
            finally
            {
                FarmerFishingLevelPatches.SuppressHiddenBonusForLegendaryBobber = false;
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
                // 字段值若不是上一帧由本 Mod 写入的值，视为原生或其他 Mod 更新了基准倍率。
                if (Math.Abs(oldModifier - data.LastAppliedCatchPenaltyModifier) > 0.0001f)
                {
                    data.NativeCatchPenaltyModifier = oldModifier;
                }

                float newModifier = DifficultyCalculator.GetCatchPenaltyModifier(
                    data.DifficultyLevel, ___distanceFromCatching);
                float combinedModifier = Math.Min(data.NativeCatchPenaltyModifier, newModifier);
                ___distanceFromCatchPenaltyModifier = combinedModifier;
                data.LastAppliedCatchPenaltyModifier = combinedModifier;

                // 只在减速倍率变化时记录（避免每帧输出）
                if (Math.Abs(oldModifier - combinedModifier) > 0.01f)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 蓄力槽保护触发 | 实例: {__instance.GetHashCode()} | 等级: {data.DifficultyLevel} | " +
                        $"蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {oldModifier:F2} → {combinedModifier:F2}",
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

        /// <summary>获取当前小游戏实际使用的难度，用于成功后记录收藏星标</summary>
        public static float GetAdjustedDifficulty(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.AdjustedDifficulty : 0f;
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
                    data.ResultStarted = true;
                }

                if (___fadeOut && ___distanceFromCatching >= 1f)
                    data.ResultStarted = true;

                // 原生成功路径在淡出动画结束时才调用 FishingRod.pullFishFromWater；
                // 必须等 fadeOut 结束，否则会提前丢失脱杆次数和实际难度。
                if (data.ResultStarted && !___fadeOut)
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
                    $"[BobberBar] 清理实例数据 | 实例: {instance.GetHashCode()}",
                    LogLevel.Debug);
            }
        }

        /// <summary>定期清理无效实例（防止内存泄漏）</summary>
        public static void PeriodicCleanup()
        {
            try
            {
                // ConditionalWeakTable 会在 BobberBar 不再被游戏引用时自动释放条目；
                // 这里保留事件入口兼容旧维护台账，不再遍历或强持有实例。
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 定期清理失败: {ex}", LogLevel.Error);
            }
        }
    }
}
