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
        /// <summary>BATCH-064-2（2026-08-15 用户指令）：非鱼类/其他 Mod 鱼星星回退早期画法——
        /// `Game1.mouseCursors (346,392,8,8)` 小金星，图标左上角 +3/+3 的 20×20 `Color.Gold`（原 BATCH-016 实现，
        /// 见 git 0020e30；BATCH-029 后改皇冠）。不再使用成就 tab 星星抠图。</summary>
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
                        string rankName = ModEntry.GetDisplayRankName(level);
                        additions.Add(ModEntry.ModHelper.Translation.Get(
                            "collections.challengeRank", new { rankName, level }));
                    }
                }

                // BATCH-036/039/061 + 064-3（2026-08-15）：有皇冠/星星的条目显示加成行——
                // 可计数 61 原生鱼显示"鱼竿手感增强(已有/61)"；非鱼类与其他 Mod 鱼（星星）显示"无手感增强"。
                if (hasCrown)
                {
                    if (!DifficultyManager.IsCountableFish(fishId))
                    {
                        additions.Add(ModEntry.ModHelper.Translation.Get("collections.noCrownControl"));
                    }
                    else
                    {
                        additions.Add(ModEntry.ModHelper.Translation.Get("collections.crownControl", new
                        {
                            crownCount = DifficultyManager.GetCountableCrownCount(Game1.player),
                            crownTarget = DifficultyManager.CountableCrownTarget
                        }));
                    }
                }

                if (additions.Count > 0)
                    __result += Environment.NewLine + string.Join(" | ", additions);
            }
            catch (Exception ex)
            {
                FishingLog.Log(
                    $"[CollectionsPage] createDescription Postfix 失败: {ex}",
                    StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-029: 在已收藏鱼类图标左上角绘制皇冠（Characters\Farmer\hats 的 Infinity Crown，源矩形 (20,800,20,20)）。</summary>
        private static Texture2D _crownTexture;

        // BATCH-056 反证修复：收藏页是 GameMenu 的子页面，Game1.activeClickableMenu 是 GameMenu 而非 CollectionsPage，
        // 原组件后置的菜单门永远不成立导致皇冠从未绘制；改为 CollectionsPage.draw 前后置标志限定绘制窗口。
        private static bool _collectionsDrawing;
        private static int _collectionsTab = -1;

        [HarmonyPatch(nameof(CollectionsPage.draw))]
        [HarmonyPrefix]
        public static void CollectionsDraw_Prefix(CollectionsPage __instance)
        {
            _collectionsDrawing = true;
            _collectionsTab = __instance.currentTab;
        }

        [HarmonyPatch(nameof(CollectionsPage.draw))]
        [HarmonyPostfix]
        public static void CollectionsDraw_Postfix()
        {
            _collectionsDrawing = false;
            _collectionsTab = -1;
        }

        private static Texture2D GetCrownTexture()
        {
            if (_crownTexture == null || _crownTexture.IsDisposed)
                _crownTexture = Game1.content.Load<Texture2D>("Characters\\Farmer\\hats");
            return _crownTexture;
        }

        /// <summary>BATCH-048: 皇冠跟随每个鱼图标绘制（同一 FrontToBack 批次、layer=图标+0.01），
        /// 因此位于收藏页悬停详情 UI 与鼠标之下；100 级流动皇冠按 1.2 倍绘制。</summary>
        [HarmonyPatch(typeof(ClickableTextureComponent), nameof(ClickableTextureComponent.draw),
            new Type[] { typeof(SpriteBatch), typeof(Color), typeof(float), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPostfix]
        public static void Draw_Postfix(SpriteBatch b, ClickableTextureComponent __instance, float layerDepth)
        {
            try
            {
                if (!_collectionsDrawing || _collectionsTab != CollectionsPage.fishTab)
                    return;

                string[] parts = __instance.name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || !DifficultyManager.HasCollectionStar(parts[0], Game1.player))
                    return;

                // BATCH-064-2/3（2026-08-15 用户指令）：非鱼类（垃圾/藻类等）与其他 Mod 鱼达到条件后
                // 画早期金星——`Game1.mouseCursors (346,392,8,8)`，左上角 +3/+3 的 20×20 Color.Gold；
                // 皇冠只保留给可计数 61 原生鱼（CountableFishIds）。
                if (!DifficultyManager.IsCountableFish(parts[0]))
                {
                    // 非鱼类/其他 Mod 鱼：原生图标正常绘制（BATCH-064j 透明化已按用户指令退回），
                    // 在此之上叠加早期金星。
                    b.Draw(
                        Game1.mouseCursors,
                        new Rectangle(__instance.bounds.X + 3, __instance.bounds.Y + 3, 20, 20),
                        new Rectangle(346, 392, 8, 8),
                        Color.Gold,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        layerDepth + 0.01f);
                    return;
                }

                Texture2D crownTexture = GetCrownTexture();
                Rectangle crownSource = new Rectangle(20, 800, 20, 20);
                bool flow = DifficultyManager.HasChallengeCrown(parts[0], Game1.player);
                bool level100 = DifficultyManager.HasLevel100FlowCrown(parts[0], Game1.player);

                if (flow)
                {
                    // 流动金色：金/白呼吸 + 1±0.05 缩放脉冲；BATCH-048 100 级流动皇冠基础尺寸 ×1.2。
                    // BATCH-074：困难模式（随机无背板）下 100 级流动皇冠基础尺寸 ×1.3，仅图鉴绘制，助战权重不变。
                    double pulseTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0;
                    float pulse = (float)Math.Sin(pulseTime);
                    Color flowColor = Color.Lerp(new Color(218, 165, 32), Color.White, (pulse + 1f) / 2f * 0.4f);
                    float level100Factor = level100
                        ? (ModEntry.Config.EnableRandomFishBehavior ? 1.3f : 1.2f)
                        : 1f;
                    float flowScale = 24f * level100Factor * (1f + 0.05f * pulse);
                    Vector2 center = new Vector2(__instance.bounds.X + 3 + 12, __instance.bounds.Y + 3 + 12);
                    b.Draw(
                        crownTexture,
                        center,
                        crownSource,
                        flowColor,
                        0f,
                        new Vector2(10f, 10f),
                        flowScale / 20f,
                        SpriteEffects.None,
                        layerDepth + 0.01f);
                }
                else
                {
                    // BATCH-056: 普通皇冠补 layerDepth（原重载默认 0.0f，在 FrontToBack 批次被压在页面底层不可见）
                    b.Draw(
                        crownTexture,
                        new Vector2(__instance.bounds.X + 3, __instance.bounds.Y + 3),
                        crownSource,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        24f / 20f,
                        SpriteEffects.None,
                        layerDepth + 0.01f);
                }
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[CollectionsPage] 皇冠绘制失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
