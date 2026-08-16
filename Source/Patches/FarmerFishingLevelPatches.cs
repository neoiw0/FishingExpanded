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
                // BATCH-066: 节日原生模式（开关关闭）——经验倍数不生效（被动节日；冰雪节原生就不给经验）
                if (Services.FestivalFishingService.IsVanillaFestivalMode())
                    return;

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

                // BATCH-068: 经验基数重算（用户 2026-08-16 确认：低于原生难度→抬到原生水平，高于 120 上限→压到上限）——
                // 负数难度等级（难度 ×0.5~0.95）或力竭衰减（非挑战鱼饵有效难度降到 80）使实际传入
                // pullFishFromWater 的难度低于原生难度时，原生经验公式（(品质+1)×3 + 难度/3 + 宝箱/完美/Boss 加成）
                // 会算出少于未装模组的经验；正数高等级调整后难度超 120 时经验基数不再随难度爆炸。
                // 重算值 = f(clamp(传入难度, 原生难度, 120))，随后继续走经验倍数（负数倍数=1 自然不变）。
                if (pendingFish.NativeDifficulty > 0f && pendingFish.PassedDifficulty > 0f &&
                    (pendingFish.PassedDifficulty < pendingFish.NativeDifficulty ||
                     pendingFish.PassedDifficulty > Utils.DifficultyCalculator.ExperienceDifficultyCap))
                {
                    float experienceDifficulty = Utils.DifficultyCalculator.GetExperienceDifficulty(
                        pendingFish.PassedDifficulty, pendingFish.NativeDifficulty);
                    int cappedExperience = Utils.DifficultyCalculator.GetNativeExperience(
                        pendingFish.FishQuality, experienceDifficulty,
                        pendingFish.TreasureCaught, pendingFish.WasPerfect, pendingFish.IsBossFish);
                    FishingLog.Log(
                        $"[FarmerFishingExperience] 经验基数重算 | 难度等级: {pendingFish.DifficultyLevel} | " +
                        $"传入难度: {pendingFish.PassedDifficulty:F0} | 原生难度: {pendingFish.NativeDifficulty:F0} | " +
                        $"钳制后: {experienceDifficulty:F0} | 经验: {howMuch} → {cappedExperience}",
                        LogLevel.Info);
                    howMuch = cappedExperience;
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
