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
        private static FishDisplayData _displayData = new FishDisplayData();

        /// <summary>记录刚钓到的超大鱼</summary>
        public static void RecordGiantFish(string fishId, int multiplier, int fishSize)
        {
            if (multiplier > 15)
            {
                _displayData.ActiveGiantFish[fishId] = (multiplier, fishSize);
                ModEntry.ModMonitor.Log(
                    $"[GiantFishManager] 超大鱼记录 | 鱼ID: {fishId} | " +
                    $"倍数: {multiplier} | fishSize: {fishSize} | " +
                    $"视觉缩放: ×{Math.Pow(multiplier, 1.0/3.0):F2}",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>检查并触发NPC反应（性能优化：仅在必要时执行）</summary>
        public static void CheckAndTriggerNPCReactions()
        {
            if (!Context.IsWorldReady || Game1.player == null) return;

            // 性能优化：快速路径 - 没有激活的超大鱼时直接返回
            if (_displayData.ActiveGiantFish.Count == 0) return;

            // 获取当前选中的物品
            var currentItem = Game1.player.CurrentItem;
            if (currentItem == null) return;

            string fishId = currentItem.QualifiedItemId;

            // 检查是否是激活的超大鱼
            if (!_displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData)) return;

            int multiplier = fishData.multiplier;
            int fishSize = fishData.fishSize;

            // 获取5格内的NPC（性能优化：使用平方距离避免开方）
            var nearbyNPCs = GetNearbyNPCs(Game1.player.Position, 5 * 64);

            if (nearbyNPCs.Count > 0)
            {
                // 只在有NPC时记录一次（避免每半秒输出）
                if (!_lastLoggedNearbyCheck.ContainsKey(fishId) ||
                    _lastLoggedNearbyCheck[fishId] != nearbyNPCs.Count)
                {
                    ModEntry.ModMonitor.Log(
                        $"[GiantFishManager] 检测到附近NPC | 鱼ID: {fishId} | " +
                        $"5格内NPC数量: {nearbyNPCs.Count}",
                        StardewModdingAPI.LogLevel.Debug);
                    _lastLoggedNearbyCheck[fishId] = nearbyNPCs.Count;
                }
            }

            foreach (var npc in nearbyNPCs)
            {
                TriggerNPCBubble(npc, fishId, fishSize);
            }
        }

        private static Dictionary<string, int> _lastLoggedNearbyCheck = new Dictionary<string, int>();

        /// <summary>触发NPC冒泡</summary>
        private static void TriggerNPCBubble(NPC npc, string fishId, int fishSize)
        {
            // 检查今天是否已触发过这种鱼
            if (!_displayData.NPCBubbleTriggered.ContainsKey(npc.Name))
            {
                _displayData.NPCBubbleTriggered[npc.Name] = new HashSet<string>();
            }

            if (_displayData.NPCBubbleTriggered[npc.Name].Contains(fishId)) return;

            // 生成随机文案
            var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
            string fishName = itemData?.DisplayName ?? "未知鱼类";
            string message = NPCDialogueGenerator.GenerateFishPraise(fishName, fishSize);

            // 显示文本气泡
            npc.showTextAboveHead(message);

            // 记录触发
            _displayData.NPCBubbleTriggered[npc.Name].Add(fishId);

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
            int clearedCount = _displayData.ActiveGiantFish.Count;
            _displayData.ClearActiveGiantFish();
            _lastLoggedNearbyCheck.Clear();

            // BATCH-007: FarmerPatches已删除，ObjectPatches无需缓存管理
            if (clearedCount > 0)
            {
                ModEntry.ModMonitor.Log(
                    $"[GiantFishManager] 进入FarmHouse | 清空超大鱼记录: {clearedCount}种",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>每日重置</summary>
        public static void OnDayStarted()
        {
            int bubbleCount = _displayData.NPCBubbleTriggered.Count;
            int dialogueCount = _displayData.NPCDialogueTriggered.Count;
            int giantFishCount = _displayData.ActiveGiantFish.Count;

            // Bug修复：每日开始时也清空超大鱼，防止内存无限增长
            _displayData.ClearActiveGiantFish();
            _displayData.ResetDailyTriggers();
            _lastLoggedNearbyCheck.Clear();

            ModEntry.ModMonitor.Log(
                $"[GiantFishManager] 每日重置 | 清空超大鱼: {giantFishCount}种 | " +
                $"清空冒泡记录: {bubbleCount}个NPC | 对话记录: {dialogueCount}个NPC",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>获取鱼的视觉缩放倍数</summary>
        public static float GetFishVisualScale(string fishId)
        {
            if (_displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData))
            {
                return DifficultyCalculator.GetVisualScale(fishData.multiplier);
            }
            return 1.0f;
        }

        /// <summary>检查对话替换（主动对话NPC时调用）</summary>
        public static string TryGetReplacementDialogue(NPC npc)
        {
            if (Game1.player?.CurrentItem == null) return null;

            string fishId = Game1.player.CurrentItem.QualifiedItemId;

            // 检查是否是激活的超大鱼
            if (!_displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData)) return null;

            // 检查今天是否已触发过对话
            if (!_displayData.NPCDialogueTriggered.ContainsKey(npc.Name))
            {
                _displayData.NPCDialogueTriggered[npc.Name] = new HashSet<string>();
            }

            if (_displayData.NPCDialogueTriggered[npc.Name].Contains(fishId)) return null;

            // 生成替换对话
            var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
            string fishName = itemData?.DisplayName ?? "未知鱼类";
            string dialogue = NPCDialogueGenerator.GenerateFishPraise(fishName, fishData.fishSize);

            // 记录触发
            _displayData.NPCDialogueTriggered[npc.Name].Add(fishId);

            return dialogue;
        }
    }
}
