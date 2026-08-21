using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StardewValley;
using FishingExpanded.Services;
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
                FishingLog.Log($"HUD 提示队列驱动失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-032: 返回标题/切换存档时清空全部玩家队列与活动记录。</summary>
        public static void ClearPending()
        {
            PendingByPlayer.Clear();
            ActiveMessageText.Clear();
        }

        /// <summary>BATCH-070: 构造按 UI 视口 1/3 宽换行的本 Mod HUD 消息。</summary>
        private static HUDMessage CreateMessage(string message, int whatType)
        {
            return new WrappingHUDMessage(message, whatType, Math.Max(1f, Game1.uiViewport.Width / 3f));
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
                EnqueueMessage(Game1.player, CreateMessage(legendaryMessage, HUDMessage.achievement_type));

                FishingLog.Log(
                    $"[HUDNotifier] 传奇鱼提示 | 鱼ID: {fishId} | 文案: {legendaryMessage}",
                    StardewModdingAPI.LogLevel.Info);
                return;
            }

            string fishName = GetFishDisplayName(fishId);

            // BATCH-015: 检测非鱼类封顶（等级8）；BATCH-065: 蟹笼鱼经 IsNonFishItem 归入非鱼（同一判定所有者）
            bool isNonFish = DifficultyManager.IsNonFishItem(fishId);
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
                string rankName = ModEntry.GetDisplayRankName(newLevel);
                message = ModEntry.ModHelper.Translation.Get("hud.challenge.title",
                    new { fishName, rank = rankName });
            }

            EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.newQuest_type));

            FishingLog.Log(
                $"[HUDNotifier] 成功提示显示 | 鱼: {fishName} ({fishId}) | " +
                $"等级: {newLevel} | 封顶: {newLevel >= maxLevel} | 非鱼类: {isNonFish}",
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>显示已有图鉴星标鱼进入小游戏时的挑战宣言。</summary>
        public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
        {
            // BATCH-033: 弱称号区间（难度等级 <1，即 0 和负数）即使已有收藏皇冠也不显示挑战宣言；
            // 只有难度等级 >=1 且拥有收藏皇冠的鱼才显示（2026-08-08 用户确认）。
            if (Utils.SpecialFishHelper.IsLegendaryFish(fishId) ||
                !DifficultyManager.HasCollectionStar(fishId, player) ||
                difficultyLevel < 1)
            {
                return;
            }

            string fishName = GetFishDisplayName(Utils.SpecialFishHelper.NormalizeItemId(fishId));
            string message = ChallengeDialogueGenerator.Generate(fishName, difficultyLevel);
            EnqueueMessage(player, CreateMessage(message, HUDMessage.newQuest_type));

            FishingLog.Log(
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

                EnqueueMessage(player, CreateMessage(message, HUDMessage.error_type));
                FishingLog.Log(
                    $"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | " +
                    $"鱼等级: {difficultyLevel} | 建议等级: {recommendedLevelText} | 当前钓鱼等级: {fishingLevel} | " +
                    $"不可能挑战: {recommendedLevel > fishingLevel * 2f}",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"钓鱼等级建议提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
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

            // BATCH-029/038: 史诗提示优先（同鱼种连续失败 ≥2 次且调整后难度 ≥150；满皇冠 α=1 时
            // 不再提示收集更多皇冠，改用“人鱼合一”文案——用户确认：满皇冠脱杆两次的高难度挑战）。
            if (isEpicChampion)
            {
                bool hasAllCrowns = DifficultyManager.GetAlpha(Game1.player) >= 1f;
                message = ModEntry.ModHelper.Translation.Get(hasAllCrowns ? "hud.fail.union" : "hud.fail.epic");
            }
            // BATCH-013: 检测触底（等级-10）
            else if (currentLevel <= -10)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.bottom", new { fishName, absLevel = Math.Abs(currentLevel) });
            }
            else if (currentLevel < 0)
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.negative", new { fishName }) + $"({Math.Abs(currentLevel)})";
            }
            else
            {
                message = ModEntry.ModHelper.Translation.Get("hud.fail.positive", new { fishName });
            }

            EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.error_type));

            FishingLog.Log(
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
                EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.newQuest_type));

                FishingLog.Log(
                    $"[HUDNotifier] 星之果茶掉落提示 | 鱼: {fishName} ({fishId}) | 文案: #{index}",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"星之果茶掉落提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-039: 持久战安慰奖励提示（20 条文案随机，含鱼名+职阶，与其他提示一起排队显示）。</summary>
        /// <param name="fishId">鱼的QualifiedItemId</param>
        /// <param name="level">本次小游戏难度等级（用于职阶称号）</param>
        public static void ShowPerseveranceRewardNotification(string fishId, int level)
        {
            try
            {
                string fishName = GetFishDisplayName(fishId);
                string rankName = ModEntry.GetDisplayRankName(level);
                int index = Game1.random.Next(1, 21);
                string key = $"hud.battleReward.{index}";
                string message = ModEntry.ModHelper.Translation.Get(key, new { fishName, rankName });
                if (string.IsNullOrWhiteSpace(message) || message == key)
                    message = $"{fishName}{rankName}希望下次能更尽兴些";

                EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.newQuest_type));

                FishingLog.Log(
                    $"[HUDNotifier] 持久战奖励提示 | 鱼: {fishName} ({fishId}) | 文案: #{index}",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"持久战奖励提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-060（2026-08-15 用户确认）: 挑战鱼饵掉星提示——5:00 起每分钟掉 1 颗时在左下角 FIFO 队列提示
        /// （小游戏期间可见），告知剩余星数与鱼获减少百分比；难度等级 ≥95 豁免不掉星，天然不触发。</summary>
        /// <param name="stars">剩余挑战星数（2/1/0）</param>
        public static void ShowChallengeStarLoss(int stars)
        {
            try
            {
                int percent = (3 - stars) * 20;
                string message = ModEntry.ModHelper.Translation.Get("hud.starLoss", new { stars, percent });
                EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.error_type));

                FishingLog.Log(
                    $"[HUDNotifier] 挑战星掉落提示 | 剩余星: {stars}/3 | 鱼获减少: -{percent}%",
                    StardewModdingAPI.LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"挑战星掉落提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>BATCH-061: 每日收获限额提示（Mod 鱼/非鱼类达到 333/天后再钓到；每类每天首次触发，FIFO 队列）。</summary>
        public static void ShowDailyLimitReached(string fishId)
        {
            try
            {
                string fishName = GetFishDisplayName(fishId);
                string message = ModEntry.ModHelper.Translation.Get("hud.dailyLimit", new { fishName });
                EnqueueMessage(Game1.player, CreateMessage(message, HUDMessage.newQuest_type));

                FishingLog.Log(
                    $"[HUDNotifier] 每日收获限额提示 | 鱼: {fishName} ({fishId})",
                    StardewModdingAPI.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"每日收获限额提示失败: {ex}", StardewModdingAPI.LogLevel.Error);
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
                    FishingLog.Log(
                        $"[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: {fishId}",
                        StardewModdingAPI.LogLevel.Warn);
                    return "未知鱼类";
                }
                return itemData.DisplayName;
            }
            catch (Exception ex)
            {
                FishingLog.Log(
                    $"[HUDNotifier] 获取鱼名称失败 | 鱼ID: {fishId} | 错误: {ex.Message}",
                    StardewModdingAPI.LogLevel.Error);
                return "未知鱼类";
            }
        }
    }
}

