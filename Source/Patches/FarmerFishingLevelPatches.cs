using System;
using HarmonyLib;
using StardewValley;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>Farmer 钓鱼等级相关的 Patch</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingLevelPatches
    {
        /// <summary>由 BobberBar 构造边界设置，仅覆盖鱼王的原生等级读取。</summary>
        internal static bool SuppressHiddenBonusForLegendaryBobber { get; set; }

        // 缓存上次记录的加成值，避免高频日志
        private static float _lastLoggedBonus = -1f;

        /// <summary>FishingLevel 属性 Getter Postfix：注入隐藏加成</summary>
        [HarmonyPatch(nameof(Farmer.FishingLevel), MethodType.Getter)]
        [HarmonyPostfix]
        public static void FishingLevel_Getter_Postfix(Farmer __instance, ref int __result)
        {
            try
            {
                // 只对当前玩家应用加成
                if (__instance != Game1.player || SuppressHiddenBonusForLegendaryBobber) return;

                float bonus = DifficultyManager.GetFishingLevelBonus();

                if (bonus > 0)
                {
                    int originalLevel = __result;
                    // 将隐藏加成加到等级上（向下取整）
                    int hiddenBonus = (int)Math.Floor(bonus);
                    __result += hiddenBonus;

                    // 低频诊断：仅在加成值变化时记录一次
                    if (Math.Abs(bonus - _lastLoggedBonus) > 0.01f)
                    {
                        _lastLoggedBonus = bonus;
                        ModEntry.ModMonitor.Log(
                            $"[FarmerFishingLevel] 钓鱼等级加成变化 | " +
                            $"原始等级: {originalLevel} | 隐藏加成: +{bonus:F1} (整数部分:{hiddenBonus}) | " +
                            $"最终等级: {__result}",
                            LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingLevel Getter Patch 失败: {ex}", LogLevel.Error);
            }
        }

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

                int multiplier = pendingFish.ExperienceMultiplier;
                if (multiplier <= 1)
                    return;

                long adjustedExperience = (long)howMuch * multiplier;
                howMuch = (int)Math.Min(int.MaxValue, adjustedExperience);
                ModEntry.ModMonitor.Log(
                    $"[FarmerFishingExperience] 经验倍率 | 难度等级: {pendingFish.DifficultyLevel} | " +
                    $"倍率: ×{multiplier} | 经验: {adjustedExperience / multiplier} → {howMuch}",
                    LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Fishing experience patch 失败: {ex}", LogLevel.Error);
            }
        }
    }
}
