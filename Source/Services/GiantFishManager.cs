using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using FishingExpanded.Data;
using FishingExpanded.Utils;

namespace FishingExpanded.Services
{
    /// <summary>超大鱼展示管理器</summary>
    public static class GiantFishManager
    {
        // 展示事实按玩家隔离；难度/星标的 modData 隔离不能替代这里的运行时隔离。
        private static readonly Dictionary<long, FishDisplayData> _displayDataByPlayer =
            new Dictionary<long, FishDisplayData>();

        private static readonly Dictionary<(long playerId, string fishId), int> _lastLoggedNearbyCheck =
            new Dictionary<(long, string), int>();

        private static readonly Dictionary<(long playerId, string npcName), Dialogue[]> _originalDialogues =
            new Dictionary<(long, string), Dialogue[]>();

        private static FishDisplayData GetDisplayData(Farmer player, bool create)
        {
            if (player == null)
                return null;

            if (!_displayDataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var data) && create)
            {
                data = new FishDisplayData();
                _displayDataByPlayer[player.UniqueMultiplayerID] = data;
            }

            return data;
        }

        /// <summary>记录刚钓到的超大鱼</summary>
        public static void RecordGiantFish(string fishId, int multiplier, int fishSize)
        {
            RecordGiantFish(Game1.player, fishId, multiplier, fishSize);
        }

        /// <summary>记录指定玩家刚钓到的超大鱼。</summary>
        public static void RecordGiantFish(Farmer player, string fishId, int multiplier, int fishSize)
        {
            string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
            FishDisplayData displayData = GetDisplayData(player, create: true);
            if (displayData != null && multiplier > 15 && !SpecialFishHelper.IsLegendaryFish(normalizedFishId) && IsFish(normalizedFishId))
            {
                displayData.ActiveGiantFish[normalizedFishId] = (multiplier, fishSize);
                ModEntry.ModMonitor.Log(
                    $"[GiantFishManager] 超大鱼记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"倍数: {multiplier} | fishSize: {fishSize} | " +
                    $"视觉缩放: ×{Math.Pow(multiplier, 1.0/3.0):F2}",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>检查并触发NPC反应（性能优化：仅在必要时执行）</summary>
        public static void CheckAndTriggerNPCReactions()
        {
            if (!Context.IsWorldReady || Game1.player == null) return;

            FishDisplayData displayData = GetDisplayData(Game1.player, create: false);
            // 性能优化：快速路径 - 没有激活的超大鱼时直接返回
            if (displayData == null || displayData.ActiveGiantFish.Count == 0) return;

            // 必须是真正举在手上的物品；仅切换到物品栏不触发巨型鱼效果。
            var currentItem = Game1.player.ActiveObject;
            if (currentItem == null || !Game1.player.IsCarrying()) return;

            string fishId = SpecialFishHelper.NormalizeItemId(currentItem.QualifiedItemId);

            // 检查是否是激活的超大鱼
            if (!displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData)) return;

            int multiplier = fishData.multiplier;
            int fishSize = fishData.fishSize;

            // 获取5格内的NPC（性能优化：使用平方距离避免开方）
            var nearbyNPCs = GetNearbyNPCs(Game1.player.Position, 5 * 64);

            if (nearbyNPCs.Count > 0)
            {
                // 只在有NPC时记录一次（避免每半秒输出）
                var logKey = (Game1.player.UniqueMultiplayerID, fishId);
                if (!_lastLoggedNearbyCheck.ContainsKey(logKey) ||
                    _lastLoggedNearbyCheck[logKey] != nearbyNPCs.Count)
                {
                    ModEntry.ModMonitor.Log(
                        $"[GiantFishManager] 检测到附近NPC | 玩家: {Game1.player.UniqueMultiplayerID} | 鱼ID: {fishId} | " +
                        $"5格内NPC数量: {nearbyNPCs.Count}",
                        StardewModdingAPI.LogLevel.Debug);
                    _lastLoggedNearbyCheck[logKey] = nearbyNPCs.Count;
                }
            }

            foreach (var npc in nearbyNPCs)
            {
                TriggerNPCBubble(npc, fishId, fishSize);
            }
        }

        /// <summary>触发NPC冒泡</summary>
        private static void TriggerNPCBubble(NPC npc, string fishId, int fishSize)
        {
            FishDisplayData displayData = GetDisplayData(Game1.player, create: true);
            if (displayData == null)
                return;

            // 检查今天是否已触发过这种鱼
            if (!displayData.NPCBubbleTriggered.ContainsKey(npc.Name))
            {
                displayData.NPCBubbleTriggered[npc.Name] = new HashSet<string>();
            }

            if (displayData.NPCBubbleTriggered[npc.Name].Contains(fishId)) return;

            // 生成随机文案
            var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
            string fishName = itemData?.DisplayName ?? "未知鱼类";
            string message = NPCDialogueGenerator.GenerateFishPraise(fishName, fishSize);

            // 显示文本气泡
            npc.showTextAboveHead(message);

            // 记录触发
            displayData.NPCBubbleTriggered[npc.Name].Add(fishId);

            ModEntry.ModMonitor.Log(
                $"[GiantFishManager] NPC冒泡触发 | NPC: {npc.Name} | 鱼ID: {fishId} | " +
                $"fishSize: {fishSize} | 文案: {message.Substring(0, Math.Min(30, message.Length))}...",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>获取玩家附近的NPC（性能优化：使用平方距离，直接遍历）</summary>
        private static List<NPC> GetNearbyNPCs(Vector2 position, float radius)
        {
            if (Game1.currentLocation == null) return new List<NPC>();

            var result = new List<NPC>();
            float radiusSquared = radius * radius; // 避免重复计算

            // 性能优化：直接遍历替代LINQ，使用平方距离避免开方
            foreach (var npc in Game1.currentLocation.characters)
            {
                float dx = npc.Position.X - position.X;
                float dy = npc.Position.Y - position.Y;
                float distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= radiusSquared)
                {
                    result.Add(npc);
                }
            }

            return result;
        }

        /// <summary>进入FarmHouse时清空超大鱼</summary>
        public static void OnEnterFarmHouse()
        {
            OnEnterFarmHouse(Game1.player);
        }

        /// <summary>只清理进入 FarmHouse 的玩家自己的超大鱼事实。</summary>
        public static void OnEnterFarmHouse(Farmer player)
        {
            FishDisplayData displayData = GetDisplayData(player, create: false);
            int clearedCount = displayData?.ActiveGiantFish.Count ?? 0;
            displayData?.ClearActiveGiantFish();
            ClearDialogueSnapshots(player);

            foreach (var key in _lastLoggedNearbyCheck.Keys.Where(key => key.playerId == player?.UniqueMultiplayerID).ToList())
                _lastLoggedNearbyCheck.Remove(key);

            // BATCH-007: FarmerPatches已删除，ObjectPatches无需缓存管理
            if (clearedCount > 0)
            {
                ModEntry.ModMonitor.Log(
                    $"[GiantFishManager] 进入FarmHouse | 玩家: {player?.UniqueMultiplayerID} | 清空超大鱼记录: {clearedCount}种",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>每日重置</summary>
        public static void OnDayStarted()
        {
            int bubbleCount = 0;
            int dialogueCount = 0;
            foreach (FishDisplayData displayData in _displayDataByPlayer.Values)
            {
                bubbleCount += displayData.NPCBubbleTriggered.Count;
                dialogueCount += displayData.NPCDialogueTriggered.Count;
                // 设计要求：巨型鱼只在进入 FarmHouse 后永久清除；每日只重置 NPC 当日触发次数。
                displayData.ResetDailyTriggers();
            }
            _originalDialogues.Clear();
            _lastLoggedNearbyCheck.Clear();

            ModEntry.ModMonitor.Log(
                    $"[GiantFishManager] 每日重置 | 玩家数: {_displayDataByPlayer.Count} | " +
                $"清空冒泡记录: {bubbleCount}个NPC | 对话记录: {dialogueCount}个NPC",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>切换存档或返回标题时清除运行时展示数据。</summary>
        public static void ResetForSave()
        {
            _displayDataByPlayer.Clear();
            _lastLoggedNearbyCheck.Clear();
            _originalDialogues.Clear();
        }

        /// <summary>获取鱼的视觉缩放倍数</summary>
        public static float GetFishVisualScale(string fishId)
        {
            return GetFishVisualScale(fishId, Game1.player);
        }

        /// <summary>获取指定玩家手持鱼的视觉缩放倍数。</summary>
        public static float GetFishVisualScale(string fishId, Farmer player)
        {
            string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
            FishDisplayData displayData = GetDisplayData(player, create: false);
            if (displayData != null && displayData.ActiveGiantFish.TryGetValue(normalizedFishId, out var fishData))
            {
                return DifficultyCalculator.GetVisualScale(fishData.multiplier);
            }
            return 1.0f;
        }

        /// <summary>检查对话替换（主动对话NPC时调用）</summary>
        public static string TryGetReplacementDialogue(NPC npc)
        {
            return TryGetReplacementDialogue(npc, Game1.player);
        }

        /// <summary>检查指定本地玩家是否触发巨型鱼主动对话替换。</summary>
        public static string TryGetReplacementDialogue(NPC npc, Farmer player)
        {
            if (npc == null || player == null || !player.IsLocalPlayer ||
                player.ActiveObject == null || !player.IsCarrying()) return null;

            string fishId = SpecialFishHelper.NormalizeItemId(player.ActiveObject.QualifiedItemId);

            FishDisplayData displayData = GetDisplayData(player, create: true);
            // 检查是否是激活的超大鱼
            if (displayData == null || !displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData)) return null;

            // 检查今天是否已触发过对话
            if (!displayData.NPCDialogueTriggered.ContainsKey(npc.Name))
            {
                displayData.NPCDialogueTriggered[npc.Name] = new HashSet<string>();
            }

            if (displayData.NPCDialogueTriggered[npc.Name].Contains(fishId))
            {
                RestoreOriginalDialogue(npc, player);
                return null;
            }

            // 生成替换对话
            var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
            string fishName = itemData?.DisplayName ?? "未知鱼类";
            string dialogue = NPCDialogueGenerator.GenerateFishPraise(fishName, fishData.fishSize);

            // 记录触发
            displayData.NPCDialogueTriggered[npc.Name].Add(fishId);
            _originalDialogues[(player.UniqueMultiplayerID, npc.Name)] = npc.CurrentDialogue?.ToArray() ?? Array.Empty<Dialogue>();

            return dialogue;
        }

        private static void RestoreOriginalDialogue(NPC npc, Farmer player)
        {
            if (npc == null || player == null ||
                !_originalDialogues.TryGetValue((player.UniqueMultiplayerID, npc.Name), out Dialogue[] original))
                return;

            npc.CurrentDialogue.Clear();
            for (int i = original.Length - 1; i >= 0; i--)
                npc.CurrentDialogue.Push(original[i]);
            _originalDialogues.Remove((player.UniqueMultiplayerID, npc.Name));
        }

        private static void ClearDialogueSnapshots(Farmer player)
        {
            if (player == null)
                return;

            foreach (var key in _originalDialogues.Keys.Where(key => key.playerId == player.UniqueMultiplayerID).ToList())
                _originalDialogues.Remove(key);
        }

        private static bool IsFish(string fishId)
        {
            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
                return itemData != null && itemData.Category == StardewValley.Object.FishCategory;
            }
            catch
            {
                return false;
            }
        }
    }
}
