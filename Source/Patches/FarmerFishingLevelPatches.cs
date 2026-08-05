using System;
using HarmonyLib;
using StardewValley;
using FishingExpanded.Services;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>Farmer 钓鱼等级相关的 Patch</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingLevelPatches
    {
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
                if (__instance != Game1.player) return;

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
    }
}
