using System;
using HarmonyLib;
using StardewValley.Menus;
using FishingExpanded.Services;
using FishingExpanded.Utils;

namespace FishingExpanded.Patches
{
    /// <summary>CollectionsPage Patch - 鱼图鉴显示增强（BATCH-016）</summary>
    [HarmonyPatch(typeof(CollectionsPage))]
    internal class CollectionsPagePatches
    {
        /// <summary>BATCH-016: createDescription Postfix - 追加挑战等级和技能加成信息</summary>
        [HarmonyPatch(nameof(CollectionsPage.createDescription))]
        [HarmonyPostfix]
        public static void CreateDescription_Postfix(
            CollectionsPage __instance,
            string id,
            ref string __result,
            int ___currentTab)
        {
            try
            {
                // 只处理鱼类tab（currentTab通常为4）
                if (___currentTab != 4 || string.IsNullOrEmpty(id))
                    return;

                // BATCH-014: 鱼王豁免
                if (SpecialFishHelper.IsLegendaryFish(id))
                    return;

                string fishId = id.StartsWith("(") ? id : $"(O){id}";

                // 获取难度等级和称号
                int level = DifficultyManager.GetDifficultyLevel(fishId);
                string rankKey = DifficultyCalculator.GetRankKey(level);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);

                // 获取钓鱼技能加成
                float fishingBonus = DifficultyManager.GetFishingLevelBonus();

                // 追加自定义信息
                __result += Environment.NewLine;
                __result += $"挑战等级：{rankName}（等级{level}）";

                if (fishingBonus > 0)
                {
                    __result += $" | 钓鱼技能加成：+{fishingBonus:F1}";
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log(
                    $"[CollectionsPage] createDescription Postfix 失败: {ex}",
                    StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
