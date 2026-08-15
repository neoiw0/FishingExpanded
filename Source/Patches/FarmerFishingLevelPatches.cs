using System;
using HarmonyLib;
using StardewValley;
using FishingExpanded.Services;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>Farmer 钓鱼经验相关的 Patch（BATCH-034：隐藏钓鱼等级加成已整体删除，Getter 注入已移除）</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingLevelPatches
    {
        /// <summary>在原生钓鱼经验写入前应用当前难度等级的经验倍率。</summary>
        [HarmonyPatch(nameof(Farmer.gainExperience))]
        [HarmonyPrefix]
        public static void GainExperience_Prefix(
            Farmer __instance,
            int which,
            ref int howMuch)
        {
            try
            {
                if (which != 1 || howMuch <= 0 || !__instance.IsLocalPlayer ||
                    !FishingRodPatches.TryBeginExperienceAdjustment(__instance, out var pendingFish))
                {
                    return;
                }

                // BATCH-061: 每日收获限额——本次收获超限（数量已在 CreateFish 边界清零），经验一并置零。
                if (pendingFish.HarvestLimited)
                {
                    howMuch = 0;
                    return;
                }

                int multiplier = pendingFish.ExperienceMultiplier;
                if (multiplier <= 1)
                    return;

                long adjustedExperience = (long)howMuch * multiplier;
                howMuch = (int)Math.Min(int.MaxValue, adjustedExperience);
                FishingLog.Log(
                    $"[FarmerFishingExperience] 经验倍率 | 难度等级: {pendingFish.DifficultyLevel} | " +
                    $"倍率: ×{multiplier} | 经验: {adjustedExperience / multiplier} → {howMuch}",
                    LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"Fishing experience patch 失败: {ex}", LogLevel.Error);
            }
        }
    }
}
