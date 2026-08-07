using System;
using System.Globalization;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using FishingExpanded.Utils;

namespace FishingExpanded.Services
{
    /// <summary>HUD 通知管理器</summary>
    public static class HUDNotifier
    {
        /// <summary>BATCH-012/013/014/015/022: 显示钓鱼成功提示（支持封顶、触底、鱼王、非鱼类，修复第一次封顶问题）</summary>
        /// <param name="fishId">鱼的QualifiedItemId</param>
        /// <param name="newLevel">新的难度等级</param>
        /// <param name="oldLevel">旧的难度等级（用于判断是否真的达到封顶）</param>
        public static void ShowSuccessNotification(string fishId, int newLevel, int oldLevel = -1)
        {
            // BATCH-014: 鱼王类特殊处理
            if (Utils.SpecialFishHelper.IsLegendaryFish(fishId))
            {
                string legendaryMessage = Utils.SpecialFishHelper.GetRandomLegendaryMessage();
                Game1.addHUDMessage(new HUDMessage(legendaryMessage, HUDMessage.achievement_type));

                ModEntry.ModMonitor.Log(
                    $"[HUDNotifier] 传奇鱼提示 | 鱼ID: {fishId} | 文案: {legendaryMessage}",
                    StardewModdingAPI.LogLevel.Info);
                return;
            }

            string fishName = GetFishDisplayName(fishId);

            // BATCH-015: 检测非鱼类封顶（等级8）
            var itemData = StardewValley.ItemRegistry.GetDataOrErrorItem(fishId);
            bool isNonFish = itemData != null && Utils.SpecialFishHelper.IsNonFish(itemData.Category);
            int maxLevel = isNonFish ? Utils.SpecialFishHelper.GetMaxLevelForNonFish() : 100;

            string message;

            // 达到封顶后的后续成功也继续显示封顶文案；等级/统计仍由 DifficultyManager 保持不变。
            if (newLevel >= maxLevel)
            {
                if (isNonFish)
                {
                    message = $"对于{fishName}而言，你已是帝王";
                }
                else
                {
                    message = $"你已经成为{fishName}中的神明，这一刻你是鱼，也是人，更是王。";
                }
            }
            else
            {
                // BATCH-022: 低等级时不显示提示（避免水草等第一次钓就提示）
                if (newLevel <= 0)
                {
                    ModEntry.ModMonitor.Log(
                        $"[HUDNotifier] 等级过低，跳过提示 | 鱼: {fishName} ({fishId}) | 等级: {newLevel}",
                        StardewModdingAPI.LogLevel.Debug);
                    return;
                }

                string rankKey = DifficultyCalculator.GetRankKey(newLevel);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);
                message = ModEntry.ModHelper.Translation.Get("hud.challenge.title",
                    new { fishName, rank = rankName });
            }

            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));

            ModEntry.ModMonitor.Log(
                $"[HUDNotifier] 成功提示显示 | 鱼: {fishName} ({fishId}) | " +
                $"等级: {newLevel} | 封顶: {newLevel >= maxLevel} | 非鱼类: {isNonFish}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>显示已有图鉴星标鱼进入小游戏时的挑战宣言。</summary>
        public static void ShowStarChallengeNotification(string fishId, int difficultyLevel)
        {
            if (Utils.SpecialFishHelper.IsLegendaryFish(fishId) ||
                !DifficultyManager.HasCollectionStar(fishId))
            {
                return;
            }

            string fishName = GetFishDisplayName(Utils.SpecialFishHelper.NormalizeItemId(fishId));
            string message = ChallengeDialogueGenerator.Generate(fishName, difficultyLevel);
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));

            ModEntry.ModMonitor.Log(
                $"[HUDNotifier] 星标鱼挑战宣言 | 鱼: {fishName} ({fishId}) | 等级: {difficultyLevel} | 文案: {message}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>显示小游戏出现时的钓鱼等级建议。</summary>
        public static void ShowDifficultyRecommendation(int difficultyLevel, Farmer player)
        {
            try
            {
                if (player == null || difficultyLevel <= 0)
                    return;

                float recommendedLevel = difficultyLevel / 10f;
                int fishingLevel = player.FishingLevel;
                if (recommendedLevel <= fishingLevel)
                    return;

                string recommendedLevelText = recommendedLevel.ToString("0.##", CultureInfo.InvariantCulture);
                string message = ModEntry.ModHelper.Translation.Get(
                    "hud.challenge.recommendation",
                    new { recommendedLevel = recommendedLevelText });

                if (recommendedLevel > fishingLevel * 2f)
                {
                    message = ModEntry.ModHelper.Translation.Get("hud.challenge.impossible") + message;
                }

                Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
                ModEntry.ModMonitor.Log(
                    $"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | " +
                    $"鱼等级: {difficultyLevel} | 建议等级: {recommendedLevelText} | 当前钓鱼等级: {fishingLevel} | " +
                    $"不可能挑战: {recommendedLevel > fishingLevel * 2f}",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"钓鱼等级建议提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-013: 显示钓鱼失败提示（支持触底检测）</summary>
        /// <param name="fishId">鱼的QualifiedItemId</param>
        /// <param name="currentLevel">当前难度等级</param>
        public static void ShowFailureNotification(string fishId, int currentLevel)
        {
            string fishName = GetFishDisplayName(fishId);
            string message;

            // BATCH-013: 检测触底（等级-10）
            if (currentLevel <= -10)
            {
                message = $"最平庸的{fishName}（{Math.Abs(currentLevel)}）依然太难了，请升级鱼竿，钓鱼等级，使用料理增加钓鱼等级，使用陷阱/浮木渔具等";
            }
            else if (currentLevel < 0)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.negative", new { fishName }) + $"（{Math.Abs(currentLevel)}）";
            }
            else
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.positive", new { fishName });
            }

            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));

            ModEntry.ModMonitor.Log(
                $"[HUDNotifier] 失败提示显示 | 鱼: {fishName} ({fishId}) | " +
                $"等级: {currentLevel} | 触底: {currentLevel <= -10}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>获取鱼的显示名称（带空检查）</summary>
        private static string GetFishDisplayName(string fishId)
        {
            try
            {
                ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(fishId);
                // Bug修复：DisplayName可能为null或空
                if (itemData == null || string.IsNullOrEmpty(itemData.DisplayName))
                {
                    ModEntry.ModMonitor.Log(
                        $"[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: {fishId}",
                        StardewModdingAPI.LogLevel.Warn);
                    return "未知鱼类";
                }
                return itemData.DisplayName;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log(
                    $"[HUDNotifier] 获取鱼名称失败 | 鱼ID: {fishId} | 错误: {ex.Message}",
                    StardewModdingAPI.LogLevel.Error);
                return "未知鱼类";
            }
        }
    }
}
