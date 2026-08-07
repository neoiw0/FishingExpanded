using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using FishingExpanded.Utils;

namespace FishingExpanded.Services
{
    /// <summary>HUD 通知管理器</summary>
    public static class HUDNotifier
    {
        /// <summary>BATCH-032: 每位玩家独立待显示提示队列（FIFO；每玩家上限 5 满丢该玩家最旧；双人同屏各屏幕独立排队）</summary>
        private static readonly Dictionary<long, Queue<HUDMessage>> PendingByPlayer = new Dictionary<long, Queue<HUDMessage>>();

        /// <summary>BATCH-032: 每位玩家当前正在显示的本 Mod 提示文案（判断是否已从原生 hudMessages 消失）</summary>
        private static readonly Dictionary<long, string> ActiveMessageText = new Dictionary<long, string>();

        /// <summary>BATCH-032: 统一入队：该玩家无活动提示时立即显示，否则排入该玩家队列（上限 5 丢该玩家最旧）。</summary>
        private static void EnqueueMessage(Farmer player, HUDMessage message)
        {
            if (player == null || message == null)
                return;
            long playerId = player.UniqueMultiplayerID;
            if (!ActiveMessageText.ContainsKey(playerId))
            {
                Game1.addHUDMessage(message);
                ActiveMessageText[playerId] = message.message;
                return;
            }
            if (!PendingByPlayer.TryGetValue(playerId, out Queue<HUDMessage> queue))
            {
                queue = new Queue<HUDMessage>();
                PendingByPlayer[playerId] = queue;
            }
            if (queue.Count >= 5)
                queue.Dequeue();
            queue.Enqueue(message);
        }

        /// <summary>BATCH-032: 低频驱动（ModEntry 每约 0.25 秒调用）：当前屏幕玩家前一条从原生 hudMessages 消失后显示其下一条。</summary>
        public static void ProcessQueue()
        {
            try
            {
                if (Game1.player == null || Game1.hudMessages == null)
                    return;
                long playerId = Game1.player.UniqueMultiplayerID;

                if (ActiveMessageText.TryGetValue(playerId, out string activeText) && !string.IsNullOrEmpty(activeText))
                {
                    bool stillVisible = Game1.hudMessages.Any(m => m != null && m.message != null && m.message == activeText);
                    if (stillVisible)
                        return;
                    ActiveMessageText.Remove(playerId);
                }

                if (!PendingByPlayer.TryGetValue(playerId, out Queue<HUDMessage> queue) || queue.Count == 0)
                    return;
                HUDMessage next = queue.Dequeue();
                Game1.addHUDMessage(next);
                ActiveMessageText[playerId] = next.message;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"HUD 提示队列驱动失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-032: 返回标题/切换存档时清空全部玩家队列与活动记录。</summary>
        public static void ClearPending()
        {
            PendingByPlayer.Clear();
            ActiveMessageText.Clear();
        }

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
                EnqueueMessage(Game1.player, new HUDMessage(legendaryMessage, HUDMessage.achievement_type));

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
                    message = ModEntry.ModHelper.Translation.Get("hud.success.nonFishCap", new { fishName });
                }
                else
                {
                    message = ModEntry.ModHelper.Translation.Get("hud.success.fishCap", new { fishName });
                }
            }
            else
            {
                // BATCH-030: 难度等级 <1（0 级和负数）的胜利也显示称号提示（弱称号“额...稍微强一点的个体”）；
                // 取代 BATCH-022 的 ≤0 跳过逻辑。
                string rankKey = DifficultyCalculator.GetRankKey(newLevel);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);
                message = ModEntry.ModHelper.Translation.Get("hud.challenge.title",
                    new { fishName, rank = rankName });
            }

            EnqueueMessage(Game1.player, new HUDMessage(message, HUDMessage.newQuest_type));

            ModEntry.ModMonitor.Log(
                $"[HUDNotifier] 成功提示显示 | 鱼: {fishName} ({fishId}) | " +
                $"等级: {newLevel} | 封顶: {newLevel >= maxLevel} | 非鱼类: {isNonFish}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>显示已有图鉴星标鱼进入小游戏时的挑战宣言。</summary>
        public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
        {
            if (Utils.SpecialFishHelper.IsLegendaryFish(fishId) ||
                !DifficultyManager.HasCollectionStar(fishId, player))
            {
                return;
            }

            string fishName = GetFishDisplayName(Utils.SpecialFishHelper.NormalizeItemId(fishId));
            string message = ChallengeDialogueGenerator.Generate(fishName, difficultyLevel);
            EnqueueMessage(player, new HUDMessage(message, HUDMessage.newQuest_type));

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

                EnqueueMessage(player, new HUDMessage(message, HUDMessage.error_type));
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
        /// <param name="isEpicChampion">BATCH-029: 同鱼种连续失败 ≥2 次且调整后难度 ≥150 时显示史诗提示</param>
        public static void ShowFailureNotification(string fishId, int currentLevel, bool isEpicChampion = false)
        {
            string fishName = GetFishDisplayName(fishId);
            string message;

            // BATCH-029: 史诗提示优先（同鱼种连续失败 ≥2 次且调整后难度 ≥150）
            if (isEpicChampion)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.epic");
            }
            // BATCH-013: 检测触底（等级-10）
            else if (currentLevel <= -10)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.bottom", new { fishName, absLevel = Math.Abs(currentLevel) });
            }
            else if (currentLevel < 0)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.negative", new { fishName }) + $"（{Math.Abs(currentLevel)}）";
            }
            else
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.positive", new { fishName });
            }

            EnqueueMessage(Game1.player, new HUDMessage(message, HUDMessage.error_type));

            ModEntry.ModMonitor.Log(
                $"[HUDNotifier] 失败提示显示 | 鱼: {fishName} ({fishId}) | " +
                $"等级: {currentLevel} | 触底: {currentLevel <= -10} | 史诗提示: {isEpicChampion}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>BATCH-032: 星之果茶掉落提示（15 条文案随机，与其他提示一起排队显示）。</summary>
        public static void ShowStarfruitTeaNotification(string fishId)
        {
            try
            {
                string fishName = GetFishDisplayName(fishId);
                int index = Game1.random.Next(1, 16);
                string message = ModEntry.ModHelper.Translation.Get($"hud.starfruitTea.{index}", new { fishName });
                EnqueueMessage(Game1.player, new HUDMessage(message, HUDMessage.newQuest_type));

                ModEntry.ModMonitor.Log(
                    $"[HUDNotifier] 星之果茶掉落提示 | 鱼: {fishName} ({fishId}) | 文案: #{index}",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"星之果茶掉落提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
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
