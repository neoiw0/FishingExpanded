using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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
                // 当前原生鱼类 tab 是1；不要使用旧版本的 cooking tab=4。
                if (___currentTab != CollectionsPage.fishTab || string.IsNullOrEmpty(id))
                    return;

                // BATCH-014: 鱼王豁免
                if (SpecialFishHelper.IsLegendaryFish(id))
                    return;

                string fishId = SpecialFishHelper.NormalizeItemId(id);

                // 获取难度等级和称号
                int level = DifficultyManager.GetDifficultyLevel(fishId);
                string rankKey = DifficultyCalculator.GetRankKey(level);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);

                var additions = new List<string>();
                if (level > 0)
                    additions.Add($"挑战等级：{rankName}（等级{level}）");

                // BATCH-025: 钓鱼技能加成按鱼种独立展示，只有该鱼已有收藏星标
                // 时才显示本条鱼的 +0.5；不再把全局累计加成显示在每一条鱼上。
                if (DifficultyManager.HasCollectionStar(fishId))
                    additions.Add("钓鱼技能加成：+0.5");

                if (additions.Count > 0)
                    __result += Environment.NewLine + string.Join(" | ", additions);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log(
                    $"[CollectionsPage] createDescription Postfix 失败: {ex}",
                    StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>在已收藏鱼类图标左上角绘制星标。</summary>
        [HarmonyPatch(nameof(CollectionsPage.draw))]
        [HarmonyPostfix]
        public static void Draw_Postfix(
            SpriteBatch b,
            int ___currentTab,
            int ___currentPage,
            Dictionary<int, List<List<ClickableTextureComponent>>> ___collections)
        {
            try
            {
                if (___currentTab != CollectionsPage.fishTab ||
                    !___collections.TryGetValue(___currentTab, out var pages) ||
                    ___currentPage < 0 || ___currentPage >= pages.Count)
                {
                    return;
                }

                foreach (ClickableTextureComponent item in pages[___currentPage])
                {
                    string[] parts = item.name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0 || !DifficultyManager.HasCollectionStar(parts[0]))
                        continue;

                    b.Draw(
                        Game1.mouseCursors,
                        new Rectangle(item.bounds.X + 3, item.bounds.Y + 3, 20, 20),
                        new Rectangle(346, 392, 8, 8),
                        Color.Gold);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[CollectionsPage] 星标绘制失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
