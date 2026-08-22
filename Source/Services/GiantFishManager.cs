using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using FishingExpanded.Services;
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

            return GetDisplayData(player.UniqueMultiplayerID, create);
        }

        private static FishDisplayData GetDisplayData(long playerId, bool create)
        {
            if (!_displayDataByPlayer.TryGetValue(playerId, out var data) && create)
            {
                data = new FishDisplayData();
                _displayDataByPlayer[playerId] = data;
            }

            return data;
        }

        /// <summary>记录刚钓到的超大鱼（BATCH-060：触发门槛改难度等级 ≥8，不再与数量倍数挂钩）</summary>
        public static void RecordGiantFish(string fishId, int level, int fishSize)
        {
            RecordGiantFish(Game1.player, fishId, level, fishSize);
        }

        /// <summary>记录指定玩家刚钓到的超大鱼。</summary>
        public static void RecordGiantFish(Farmer player, string fishId, int level, int fishSize)
        {
            string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
            FishDisplayData displayData = GetDisplayData(player, create: true);
            if (displayData != null && level >= 8 && !SpecialFishHelper.IsLegendaryFish(normalizedFishId) && IsFish(normalizedFishId))
            {
                displayData.ActiveGiantFish[normalizedFishId] = (level, fishSize);
                FishingLog.Log(
                    $"[GiantFishManager] 超大鱼记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {level} | fishSize: {fishSize} | " +
                    $"视觉缩放: ×{DifficultyCalculator.GetVisualScale(level):F2}",
                    StardewModdingAPI.LogLevel.Info);

                // BATCH-074：联机时把展示事实广播给其他客户端，使其他玩家屏幕也能看到该玩家手持鱼变大。
                BroadcastGiantFishRecord(player.UniqueMultiplayerID, normalizedFishId, level, fishSize);
            }
        }

        /// <summary>检查并触发NPC反应（性能优化：仅在必要时执行）</summary>
        public static void CheckAndTriggerNPCReactions()
        {
            if (!Context.IsWorldReady) return;

            // BATCH-027: 双人同屏/联机时遍历所有本机玩家，各自检查自己举起的超大鱼。
            foreach (Farmer player in Game1.getOnlineFarmers())
            {
                if (player == null || !player.IsLocalPlayer)
                    continue;

                CheckAndTriggerForPlayer(player);
            }
        }

        /// <summary>检查指定本机玩家举起的超大鱼并触发NPC反应。</summary>
        private static void CheckAndTriggerForPlayer(Farmer player)
        {
            FishDisplayData displayData = GetDisplayData(player, create: false);
            // 性能优化：快速路径 - 没有激活的超大鱼时直接返回
            if (displayData == null || displayData.ActiveGiantFish.Count == 0) return;

            // 必须是真正举在手上的物品；仅切换到物品栏不触发巨型鱼效果。
            var currentItem = player.ActiveObject;
            if (currentItem == null || !player.IsCarrying()) return;

            string fishId = SpecialFishHelper.NormalizeItemId(currentItem.QualifiedItemId);

            // 检查是否是激活的超大鱼
            if (!displayData.ActiveGiantFish.TryGetValue(fishId, out var fishData)) return;

            // BATCH-060: 展示事实存难度等级（level），不再存数量倍数
            int level = fishData.level;
            int fishSize = fishData.fishSize;

            // 获取5格内的NPC（性能优化：使用平方距离避免开方）
            var nearbyNPCs = GetNearbyNPCs(player.Position, 5 * 64);

            if (nearbyNPCs.Count > 0)
            {
                // 只在有NPC时记录一次（避免每半秒输出）
                var logKey = (player.UniqueMultiplayerID, fishId);
                if (!_lastLoggedNearbyCheck.ContainsKey(logKey) ||
                    _lastLoggedNearbyCheck[logKey] != nearbyNPCs.Count)
                {
                    FishingLog.Log(
                        $"[GiantFishManager] 检测到附近NPC | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {fishId} | " +
                        $"5格内NPC数量: {nearbyNPCs.Count}",
                        StardewModdingAPI.LogLevel.Debug);
                    _lastLoggedNearbyCheck[logKey] = nearbyNPCs.Count;
                }
            }

            foreach (var npc in nearbyNPCs)
            {
                TriggerNPCBubble(npc, fishId, fishSize, player);
            }
        }

        /// <summary>触发NPC冒泡</summary>
        private static void TriggerNPCBubble(NPC npc, string fishId, int fishSize, Farmer player)
        {
            FishDisplayData displayData = GetDisplayData(player, create: true);
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
            string message = NPCDialogueGenerator.GenerateFishPraise(fishName, fishSize, GetNpcPraiseKey(npc));

            // BATCH-058: 动物 NPC 先叫一声，再把赞美内容放在括号里（例：汪汪！！！（这条狗鱼竟然有388cm简直是奇迹））
            // BATCH-073: 英文模式使用英文拟声词和英文叹号；中文保持原格式。
            string animalSound = GetAnimalSound(npc);
            if (!string.IsNullOrEmpty(animalSound))
                message = IsEnglishGame()
                    ? $"{animalSound}!!! ({message})"
                    : $"{animalSound}！！！({message})";

            // 显示文本气泡
            npc.showTextAboveHead(message);

            // 记录触发
            displayData.NPCBubbleTriggered[npc.Name].Add(fishId);

            // BATCH-074：联机时广播冒泡文案，使其他玩家屏幕看到同一 NPC 的一致赞美。
            BroadcastNPCBubble(player, npc, fishId, fishSize, message);

            FishingLog.Log(
                $"[GiantFishManager] NPC冒泡触发 | NPC: {npc.Name} | 鱼ID: {fishId} | " +
                $"fishSize: {fishSize} | 文案: {message.Substring(0, Math.Min(30, message.Length))}...",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>英文模式判断。</summary>
        private static bool IsEnglishGame()
        {
            return LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en;
        }

        /// <summary>BATCH-058/073: 按 NPC 类型识别动物叫声（宠物 Pet 按 petType=Dog/Cat；马 Horse；其他返回空=非动物）。
        /// 中文/英文各有多种随机叫声；英文模式使用英文拟声词。</summary>
        private static string GetAnimalSound(NPC npc)
        {
            if (npc is StardewValley.Characters.Pet pet)
            {
                string type = pet.petType?.Value;
                if (string.Equals(type, StardewValley.Characters.Pet.type_dog, StringComparison.OrdinalIgnoreCase))
                    return PickSound(IsEnglishGame()
                        ? new[] { "Woof!", "Woof, woof!", "Arf!", "Ruff!", "Yip!", "Bow-wow!" }
                        : new[] { "汪汪", "汪！", "汪汪汪", "嗷呜～汪", "汪~汪" });
                if (string.Equals(type, StardewValley.Characters.Pet.type_cat, StringComparison.OrdinalIgnoreCase))
                    return PickSound(IsEnglishGame()
                        ? new[] { "Meow!", "Mrow!", "Meow meow!", "Mew!", "Mrrow!", "Mrow?" }
                        : new[] { "喵喵", "喵～", "喵呜", "喵喵喵", "咪" });
                return null; // 乌龟等其他宠物类型暂不加叫声
            }
            if (npc is StardewValley.Characters.Horse)
                return PickSound(IsEnglishGame()
                    ? new[] { "Neigh!", "Whinny!", "Nicker!", "Snort!", "Neigh-heigh!", "Hee hee!" }
                    : new[] { "嘶嘶", "嘶——", "唏律律", "吁——", "嘶～" });
            return null;
        }

        /// <summary>BATCH-058: 从 5 套叫声里随机挑一套。</summary>
        private static string PickSound(string[] sounds)
        {
            if (sounds == null || sounds.Length == 0)
                return null;
            return sounds[Game1.random.Next(sounds.Length)];
        }

        /// <summary>获取 NPC 专属文案查找键：宠物与马按类型映射 Dog/Cat/Horse，其余按 NPC 名。
        /// BATCH-075B：1.6 的马可命名且默认名是本地化字符串（如“格罗佛”），
        /// `npc.Name` 不可作马的身份键，必须与 GetAnimalSound 一样按类型识别。</summary>
        private static string GetNpcPraiseKey(NPC npc)
        {
            if (npc is StardewValley.Characters.Pet pet)
            {
                string type = pet.petType?.Value;
                if (string.Equals(type, StardewValley.Characters.Pet.type_dog, StringComparison.OrdinalIgnoreCase))
                    return "Dog";
                if (string.Equals(type, StardewValley.Characters.Pet.type_cat, StringComparison.OrdinalIgnoreCase))
                    return "Cat";
            }
            if (npc is StardewValley.Characters.Horse)
                return "Horse";
            return npc.Name;
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

        /// <summary>只清理进入 FarmHouse 的玩家自己的超大鱼事实。
        /// BATCH-074：同时重置 NPC 冒泡/对话触发记录，使进房后的下一次超大鱼可再次获得赞美。</summary>
        public static void OnEnterFarmHouse(Farmer player)
        {
            FishDisplayData displayData = GetDisplayData(player, create: false);
            int clearedCount = displayData?.ActiveGiantFish.Count ?? 0;
            displayData?.ResetSessionEffects();
            ClearDialogueSnapshots(player);

            // BATCH-074：联机时广播清空事件，让其他客户端同步移除该玩家的超大鱼展示事实。
            if (player != null)
                BroadcastGiantFishClear(player.UniqueMultiplayerID);

            foreach (var key in _lastLoggedNearbyCheck.Keys.Where(key => key.playerId == player?.UniqueMultiplayerID).ToList())
                _lastLoggedNearbyCheck.Remove(key);

            // BATCH-007: FarmerPatches已删除，ObjectPatches无需缓存管理
            if (clearedCount > 0)
            {
                FishingLog.Log(
                    $"[GiantFishManager] 进入FarmHouse | 玩家: {player?.UniqueMultiplayerID} | 清空超大鱼记录: {clearedCount}种",
                    StardewModdingAPI.LogLevel.Info);
            }
        }

        /// <summary>每日重置。BATCH-074：与进 FarmHouse 一致，展示事实和赞美记录全部归零。</summary>
        public static void OnDayStarted()
        {
            int displayCount = 0;
            int bubbleCount = 0;
            int dialogueCount = 0;
            foreach (FishDisplayData displayData in _displayDataByPlayer.Values)
            {
                displayCount += displayData.ActiveGiantFish.Count;
                bubbleCount += displayData.NPCBubbleTriggered.Count;
                dialogueCount += displayData.NPCDialogueTriggered.Count;
                displayData.ResetSessionEffects();
            }
            _originalDialogues.Clear();
            _lastLoggedNearbyCheck.Clear();

            FishingLog.Log(
                    $"[GiantFishManager] 每日重置 | 玩家数: {_displayDataByPlayer.Count} | " +
                $"清空超大鱼展示: {displayCount}种 | 冒泡记录: {bubbleCount}个NPC | 对话记录: {dialogueCount}个NPC",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>切换存档或返回标题时清除运行时展示数据。</summary>
        public static void ResetForSave()
        {
            _displayDataByPlayer.Clear();
            _lastLoggedNearbyCheck.Clear();
            _originalDialogues.Clear();
        }

        // ---- BATCH-074：联机同步（分屏/远程）----

        private static void BroadcastGiantFishRecord(long playerId, string fishId, int level, int fishSize)
        {
            if (!Context.IsMultiplayer || ModEntry.ModHelper == null)
                return;

            try
            {
                ModEntry.ModHelper.Multiplayer.SendMessage(
                    new GiantFishRecordMessage
                    {
                        PlayerId = playerId,
                        FishId = fishId,
                        Level = level,
                        FishSize = fishSize
                    },
                    "GiantFishRecord");
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("GiantFishManager.BroadcastGiantFishRecord",
                    $"[GiantFishManager] 广播超大鱼记录失败: {ex}", StardewModdingAPI.LogLevel.Warn);
            }
        }

        private static void BroadcastGiantFishClear(long playerId)
        {
            if (!Context.IsMultiplayer || ModEntry.ModHelper == null)
                return;

            try
            {
                ModEntry.ModHelper.Multiplayer.SendMessage(
                    new GiantFishClearMessage { PlayerId = playerId },
                    "GiantFishClear");
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("GiantFishManager.BroadcastGiantFishClear",
                    $"[GiantFishManager] 广播清空失败: {ex}", StardewModdingAPI.LogLevel.Warn);
            }
        }

        private static void BroadcastNPCBubble(Farmer player, NPC npc, string fishId, int fishSize, string message)
        {
            if (!Context.IsMultiplayer || ModEntry.ModHelper == null || player == null || npc == null)
                return;

            try
            {
                string locationName = npc.currentLocation?.NameOrUniqueName ?? Game1.currentLocation?.NameOrUniqueName ?? "";
                ModEntry.ModHelper.Multiplayer.SendMessage(
                    new NPCFishBubbleMessage
                    {
                        PlayerId = player.UniqueMultiplayerID,
                        LocationName = locationName,
                        NPCName = npc.Name,
                        FishId = fishId,
                        FishSize = fishSize,
                        Message = message
                    },
                    "NPCFishBubble");
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("GiantFishManager.BroadcastNPCBubble",
                    $"[GiantFishManager] 广播NPC冒泡失败: {ex}", StardewModdingAPI.LogLevel.Warn);
            }
        }

        /// <summary>接收远程超大鱼展示事实（只写入展示字典，不触发本地 NPC 检查）。</summary>
        public static void ApplyRemoteGiantFishRecord(long playerId, string fishId, int level, int fishSize)
        {
            if (playerId <= 0 || string.IsNullOrEmpty(fishId))
                return;

            string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
            FishDisplayData displayData = GetDisplayData(playerId, create: true);
            if (displayData == null)
                return;

            displayData.ActiveGiantFish[normalizedFishId] = (level, fishSize);
            FishingLog.Log(
                $"[GiantFishManager] 接收远程超大鱼记录 | 玩家: {playerId} | 鱼ID: {normalizedFishId} | " +
                $"难度等级: {level} | fishSize: {fishSize}",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>接收远程清空事件（进 FarmHouse / 换日），同步移除该玩家的展示与赞美记录。</summary>
        public static void ApplyRemoteGiantFishClear(long playerId)
        {
            if (playerId <= 0)
                return;

            FishDisplayData displayData = GetDisplayData(playerId, create: false);
            if (displayData == null)
                return;

            displayData.ResetSessionEffects();
            FishingLog.Log(
                $"[GiantFishManager] 接收远程清空 | 玩家: {playerId}",
                StardewModdingAPI.LogLevel.Info);
        }

        /// <summary>接收远程 NPC 冒泡文案，在本地对应 NPC 上显示（展示同步，不占用本地触发额度）。</summary>
        public static void ApplyRemoteNPCBubble(string locationName, string npcName, string message)
        {
            if (string.IsNullOrEmpty(npcName) || string.IsNullOrEmpty(message))
                return;

            GameLocation location = string.IsNullOrEmpty(locationName)
                ? Game1.currentLocation
                : Game1.getLocationFromName(locationName) ?? Game1.currentLocation;
            if (location == null)
                return;

            NPC npc = location.characters.FirstOrDefault(n => n.Name == npcName);
            if (npc == null)
                return;

            npc.showTextAboveHead(message);
            FishingLog.Log(
                $"[GiantFishManager] 接收远程NPC冒泡 | NPC: {npcName} | 地点: {locationName}",
                StardewModdingAPI.LogLevel.Info);
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
                return DifficultyCalculator.GetVisualScale(fishData.level);
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
            string dialogue = NPCDialogueGenerator.GenerateFishPraise(fishName, fishData.fishSize, GetNpcPraiseKey(npc));

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
