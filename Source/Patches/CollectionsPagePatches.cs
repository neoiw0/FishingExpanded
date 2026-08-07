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
                int level = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
                string rankKey = DifficultyCalculator.GetRankKey(level);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);

                var additions = new List<string>();
                if (level > 0)
                    additions.Add(ModEntry.ModHelper.Translation.Get(
                        "collections.challengeRank", new { rankName, level }));

                // BATCH-025: 加成按鱼种独立展示，只有该鱼已有收藏星标时才显示
                // 本条鱼的 +0.5；不再把全局累计加成显示在每一条鱼上。
                // 2026-08-06 用户确认措辞：钓鱼条长度额外加成。
                if (DifficultyManager.HasCollectionStar(fishId, Game1.player))
                    additions.Add(ModEntry.ModHelper.Translation.Get("collections.fishingBonus"));

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

        /// <summary>BATCH-029: 在已收藏鱼类图标左上角绘制皇冠（Characters\Farmer\hats 的 Infinity Crown，源矩形 (20,800,20,20)）。</summary>
        private static Texture2D _crownTexture;

        private static Texture2D GetCrownTexture()
        {
            if (_crownTexture == null || _crownTexture.IsDisposed)
                _crownTexture = Game1.content.Load<Texture2D>("Characters\\Farmer\\hats");
            return _crownTexture;
        }

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

                Texture2D crownTexture = GetCrownTexture();
                foreach (ClickableTextureComponent item in pages[___currentPage])
                {
                    string[] parts = item.name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0 || !DifficultyManager.HasCollectionStar(parts[0], Game1.player))
                        continue;

                    b.Draw(
                        crownTexture,
                        new Rectangle(item.bounds.X + 3, item.bounds.Y + 3, 24, 24),
                        new Rectangle(20, 800, 20, 20),
                        Color.White);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[CollectionsPage] 皇冠绘制失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
