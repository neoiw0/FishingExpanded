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

                string fishId = SpecialFishHelper.NormalizeItemId(id);
                bool hasCrown = DifficultyManager.HasCollectionStar(fishId, Game1.player);

                var additions = new List<string>();

                // BATCH-014: 鱼王豁免挑战等级展示（皇冠加成行不受影响）
                if (!SpecialFishHelper.IsLegendaryFish(id))
                {
                    int level = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
                    if (level > 0)
                    {
                        string rankKey = DifficultyCalculator.GetRankKey(level);
                        string rankName = ModEntry.ModHelper.Translation.Get(rankKey);
                        additions.Add(ModEntry.ModHelper.Translation.Get(
                            "collections.challengeRank", new { rankName, level }));
                    }
                }

                // BATCH-036: 有皇冠的鱼显示其提供的加成（用户确认文案“钓鱼控制力+1”；只读展示，不新增状态）
                if (hasCrown)
                    additions.Add(ModEntry.ModHelper.Translation.Get("collections.crownControl"));

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
                Rectangle crownSource = new Rectangle(20, 800, 20, 20);

                // BATCH-038: 流动金色皇冠呼吸脉冲（挑战鱼饵生效且难度等级≥95 时成功获得的鱼）
                double pulseTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0;
                float pulse = (float)Math.Sin(pulseTime);

                foreach (ClickableTextureComponent item in pages[___currentPage])
                {
                    string[] parts = item.name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0 || !DifficultyManager.HasCollectionStar(parts[0], Game1.player))
                        continue;

                    if (DifficultyManager.HasChallengeCrown(parts[0], Game1.player))
                    {
                        // 流动金色：金/白呼吸 + 1±0.05 缩放脉冲（视觉待真实游戏确认）
                        Color flowColor = Color.Lerp(new Color(218, 165, 32), Color.White, (pulse + 1f) / 2f * 0.4f);
                        float flowScale = 24f * (1f + 0.05f * pulse);
                        Vector2 center = new Vector2(item.bounds.X + 3 + 12, item.bounds.Y + 3 + 12);
                        b.Draw(
                            crownTexture,
                            center,
                            crownSource,
                            flowColor,
                            0f,
                            new Vector2(10f, 10f),
                            flowScale / 20f,
                            SpriteEffects.None,
                            0.99f);
                    }
                    else
                    {
                        b.Draw(
                            crownTexture,
                            new Rectangle(item.bounds.X + 3, item.bounds.Y + 3, 24, 24),
                            crownSource,
                            Color.White);
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[CollectionsPage] 皇冠绘制失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
