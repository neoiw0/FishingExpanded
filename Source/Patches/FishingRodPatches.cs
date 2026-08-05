using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Objects;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>FishingRod 钓鱼竿的 Patch</summary>
    [HarmonyPatch(typeof(FishingRod))]
    internal class FishingRodPatches
    {
        // BATCH-009: 保存原始数量，不修改动画（internal供FarmerFishingPatches访问）
        internal static Dictionary<string, (int originalNum, int multiplier, int missCount)> _pendingFish =
            new Dictionary<string, (int, int, int)>();

        /// <summary>BATCH-009/014: pullFishFromWater Prefix - 只记录数据，不修改numCaught（鱼王豁免）</summary>
        [HarmonyPatch(nameof(FishingRod.pullFishFromWater))]
        [HarmonyPrefix]
        public static void PullFishFromWater_Prefix(
            FishingRod __instance,
            string fishId,
            int numCaught)
        {
            try
            {
                // BATCH-014: 鱼王类豁免所有规则
                if (Utils.SpecialFishHelper.IsLegendaryFish(fishId))
                {
                    ModEntry.ModMonitor.Log(
                        $"[FishingRod] 传奇鱼（鱼王）豁免规则 | 鱼ID: {fishId}",
                        LogLevel.Info);

                    // 显示鱼王提示
                    HUDNotifier.ShowSuccessNotification(fishId, 0);
                    return;
                }

                int difficultyLevel = DifficultyManager.GetDifficultyLevel(fishId);
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);

                // BATCH-010: 获取脱杆次数
                int missCount = 0;
                if (__instance.isFishing && Game1.activeClickableMenu is StardewValley.Menus.BobberBar bobberBar)
                {
                    missCount = BobberBarPatches.GetMissCount(bobberBar);
                }

                // 保存数据供Postfix使用
                _pendingFish[fishId] = (numCaught, quantityMultiplier, missCount);

                ModEntry.ModMonitor.Log(
                    $"[FishingRod] 钓鱼成功（动画阶段）| 鱼ID: {fishId} | " +
                    $"难度等级: {difficultyLevel} | 数量倍数: {quantityMultiplier} | " +
                    $"脱杆次数: {missCount} | 动画显示: {numCaught}条",
                    LogLevel.Info);

                // 记录超大鱼（用于视觉缩放和NPC反应）
                if (quantityMultiplier > 15)
                {
                    int finalSize = numCaught * quantityMultiplier;
                    GiantFishManager.RecordGiantFish(fishId, quantityMultiplier, finalSize);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"pullFishFromWater Prefix 失败: {ex}", LogLevel.Error);
            }
        }
    }

    /// <summary>Farmer Patch - 钓鱼数量转化</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingPatches
    {
        /// <summary>BATCH-009: caughtFish Postfix - 修改进入背包的数量</summary>
        [HarmonyPatch(nameof(Farmer.caughtFish))]
        [HarmonyPostfix]
        public static void CaughtFish_Postfix(
            Farmer __instance,
            string itemId,
            int size,
            bool from_fish_pond,
            int numberCaught)
        {
            try
            {
                // 只处理正常钓鱼（非鱼塘）
                if (from_fish_pond)
                    return;

                // BATCH-021: 修复ID格式不匹配问题
                // pullFishFromWater传入"144"，但caughtFish接收"(O)144"
                // 需要同时尝试两种格式
                string fishId = itemId;
                if (!FishingRodPatches._pendingFish.TryGetValue(fishId, out var data))
                {
                    // 尝试去掉 (O) 前缀
                    string unqualifiedId = itemId.Replace("(O)", "");
                    if (!FishingRodPatches._pendingFish.TryGetValue(unqualifiedId, out data))
                    {
                        ModEntry.ModMonitor.Log(
                            $"[Farmer] 未找到待处理数据 | itemId: {itemId} | 已尝试: {fishId}, {unqualifiedId} | " +
                            $"字典中的键: {string.Join(", ", FishingRodPatches._pendingFish.Keys)}",
                            LogLevel.Debug);
                        return;
                    }
                    fishId = unqualifiedId; // 使用找到的键
                }

                int originalNum = data.originalNum;
                int multiplier = data.multiplier;
                int missCount = data.missCount;

                // 清除已处理的数据
                FishingRodPatches._pendingFish.Remove(fishId);

                // BATCH-009: 如果倍数>1，修改背包中的数量
                if (multiplier > 1)
                {
                    StardewValley.Object fishItem = null;
                    for (int i = __instance.Items.Count - 1; i >= 0; i--)
                    {
                        if (__instance.Items[i] is StardewValley.Object obj &&
                            obj.QualifiedItemId == itemId &&
                            obj.Stack == originalNum)
                        {
                            fishItem = obj;
                            break;
                        }
                    }

                    if (fishItem != null)
                    {
                        fishItem.Stack = originalNum * multiplier;
                        ModEntry.ModMonitor.Log(
                            $"[Farmer] 数量转化完成 | 鱼ID: {fishId} | " +
                            $"原始: {originalNum} → 最终: {fishItem.Stack} (×{multiplier})",
                            LogLevel.Info);
                    }
                }

                // BATCH-010: 根据脱杆次数记录等级增长（必须始终执行）
                int baseLevelGain = missCount switch
                {
                    0 => 10,  // 完美
                    1 => 5,
                    2 => 2,
                    _ => 1
                };

                int oldLevel = DifficultyManager.GetDifficultyLevel(fishId);

                // BATCH-023: 越级限制 - 一次最多只能晋升到下一个称号区间的上限
                int theoreticalNewLevel = oldLevel + baseLevelGain;
                int nextRankCeiling = DifficultyCalculator.GetNextRankCeiling(oldLevel);
                int actualNewLevel = Math.Min(theoreticalNewLevel, nextRankCeiling);
                int actualLevelGain = actualNewLevel - oldLevel;

                // 如果被越级限制钳制，记录日志
                if (actualLevelGain < baseLevelGain)
                {
                    ModEntry.ModMonitor.Log(
                        $"[Farmer] 越级限制生效 | 鱼ID: {fishId} | " +
                        $"原始增长: +{baseLevelGain} | 实际增长: +{actualLevelGain} | " +
                        $"下一称号上限: {nextRankCeiling}",
                        LogLevel.Info);
                }

                DifficultyManager.RecordSuccess(fishId, actualLevelGain);
                int newLevel = DifficultyManager.GetDifficultyLevel(fishId);

                ModEntry.ModMonitor.Log(
                    $"[Farmer] 难度等级更新 | 鱼ID: {fishId} | " +
                    $"脱杆: {missCount}次 | 基础增长: +{baseLevelGain} | 实际增长: +{actualLevelGain} | {oldLevel} → {newLevel}",
                    LogLevel.Info);

                // BATCH-012/022: 显示成功HUD提示（封顶检测，传入旧等级）
                HUDNotifier.ShowSuccessNotification(fishId, newLevel, oldLevel);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"caughtFish Postfix 失败: {ex}", LogLevel.Error);
            }
        }
    }
}
