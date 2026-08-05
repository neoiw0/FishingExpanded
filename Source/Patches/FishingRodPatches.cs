using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Objects;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>FishingRod 钓鱼竿的 Patch</summary>
    [HarmonyPatch(typeof(FishingRod))]
    internal class FishingRodPatches
    {
        /// <summary>一次本地鱼获从小游戏成功到物品创建的事实</summary>
        internal sealed class PendingFishData
        {
            public int OriginalNum { get; set; }
            public int Multiplier { get; set; }
            public int DifficultyLevel { get; set; }
            public int ExperienceMultiplier { get; set; }
            public int MissCount { get; set; }
            public float AdjustedDifficulty { get; set; }
            public int FishSize { get; set; }
            public bool SuccessRecorded { get; set; }
            public bool ExperienceAdjusted { get; set; }
            public int CreateFishCalls { get; set; }
            public bool AllowAdditionalCreateFish { get; set; }
            public Item PendingOverflowItem { get; set; }
        }

        // BATCH-009: 以玩家+鱼ID隔离待处理事实，不修改原生动画参数
        internal static readonly Dictionary<string, PendingFishData> _pendingFish =
            new Dictionary<string, PendingFishData>();

        /// <summary>BATCH-009/014: pullFishFromWater Prefix - 只记录数据，不修改numCaught（鱼王豁免）</summary>
        [HarmonyPatch(nameof(FishingRod.pullFishFromWater))]
        [HarmonyPrefix]
        public static void PullFishFromWater_Prefix(
            FishingRod __instance,
            string fishId,
            int fishSize,
            int fishDifficulty,
            int numCaught,
            bool fromFishPond)
        {
            try
            {
                Farmer owner = __instance.getLastFarmerToUse();
                if (owner == null || !owner.IsLocalPlayer || fromFishPond)
                    return;

                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);

                // BATCH-014: 鱼王类豁免所有规则
                if (Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                {
                    ModEntry.ModMonitor.Log(
                        $"[FishingRod] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId}",
                        LogLevel.Info);

                    // 显示鱼王提示
                    HUDNotifier.ShowSuccessNotification(fishId, 0);
                    return;
                }

                int difficultyLevel = DifficultyManager.GetDifficultyLevel(normalizedFishId);
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);

                // BATCH-010: 获取脱杆次数
                int missCount = 0;
                float adjustedDifficulty = fishDifficulty;
                if (Game1.activeClickableMenu is StardewValley.Menus.BobberBar bobberBar)
                {
                    missCount = BobberBarPatches.GetMissCount(bobberBar);
                    float trackedDifficulty = BobberBarPatches.GetAdjustedDifficulty(bobberBar);
                    if (trackedDifficulty > 0f)
                        adjustedDifficulty = trackedDifficulty;
                }

                // 保存数据供Postfix使用
                _pendingFish[GetPendingKey(owner, normalizedFishId)] = new PendingFishData
                {
                    OriginalNum = numCaught,
                    Multiplier = quantityMultiplier,
                    DifficultyLevel = difficultyLevel,
                    ExperienceMultiplier = DifficultyCalculator.GetExperienceMultiplier(difficultyLevel),
                    MissCount = missCount,
                    AdjustedDifficulty = adjustedDifficulty,
                    // 使用原生成功调用最终传入的尺寸；鱼在小游戏中逃跑时尺寸可能已经下降。
                    FishSize = fishSize
                };

                ModEntry.ModMonitor.Log(
                    $"[FishingRod] 钓鱼成功（动画阶段）| 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {difficultyLevel} | 数量倍数: {quantityMultiplier} | " +
                    $"脱杆次数: {missCount} | 动画显示: {numCaught}条",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"pullFishFromWater Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>在原生 CreateFish 返回物品时转换数量，覆盖背包和 ItemGrabMenu 两条原生路径</summary>
        [HarmonyPatch("CreateFish")]
        [HarmonyPostfix]
        public static void CreateFish_Postfix(FishingRod __instance, ref Item __result)
        {
            try
            {
                Farmer owner = __instance.getLastFarmerToUse();
                if (owner == null || !owner.IsLocalPlayer || __result == null)
                    return;

                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(__result.QualifiedItemId);
                string key = GetPendingKey(owner, normalizedFishId);
                if (!_pendingFish.TryGetValue(key, out var data))
                    return;

                bool isFinalFish = data.CreateFishCalls == 0 || data.AllowAdditionalCreateFish;
                data.CreateFishCalls++;
                data.AllowAdditionalCreateFish = false;
                if (!isFinalFish)
                    return;

                if (data.Multiplier > 1)
                {
                    int nativeStack = Math.Max(1, __result.Stack);
                    long finalStack = (long)nativeStack * data.Multiplier;
                    __result.Stack = (int)Math.Min(int.MaxValue, finalStack);
                    ModEntry.ModMonitor.Log(
                        $"[FishingRod] 数量转化完成 | 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                        $"原生堆叠: {nativeStack} → 最终: {__result.Stack} (×{data.Multiplier})",
                        LogLevel.Info);
                }

            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.CreateFish Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        internal static bool TryGetPending(Farmer owner, string fishId, out PendingFishData data)
        {
            return _pendingFish.TryGetValue(GetPendingKey(owner, fishId), out data);
        }

        /// <summary>
        /// 开始一次本地钓鱼经验结算。经验写入发生在 NetEventBinary.Poll 阶段，
        /// 此时 BobberBar 已经退出，所以不能依赖 Game1.activeClickableMenu 查找难度。
        /// </summary>
        internal static bool TryBeginExperienceAdjustment(Farmer owner, out PendingFishData data)
        {
            data = null;
            if (owner == null)
                return false;

            string prefix = owner.UniqueMultiplayerID + ":";
            foreach (var entry in _pendingFish)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal) || entry.Value.ExperienceAdjusted)
                    continue;

                entry.Value.ExperienceAdjusted = true;
                data = entry.Value;
                return true;
            }

            return false;
        }

        internal static void ClearPending()
        {
            _pendingFish.Clear();
        }

        private static void ClearPending(FishingRod rod)
        {
            Farmer owner = rod.getLastFarmerToUse();
            string fishId = rod.whichFish?.QualifiedItemId;
            if (owner == null || string.IsNullOrEmpty(fishId))
                return;

            _pendingFish.Remove(GetPendingKey(owner, fishId));
        }

        private static void PrepareAdditionalFishCreation(FishingRod rod, int remainingFish)
        {
            Farmer owner = rod.getLastFarmerToUse();
            string fishId = rod.whichFish?.QualifiedItemId;
            if (owner == null || string.IsNullOrEmpty(fishId) ||
                !_pendingFish.TryGetValue(GetPendingKey(owner, fishId), out var data))
            {
                return;
            }

            // 原生在 remainingFish == 1 时才会再次创建最终鱼获；其余 CreateFish 调用
            // 可能只是鱼卵等奖励的临时取样，不应重复应用数量倍数。
            data.AllowAdditionalCreateFish = remainingFish == 1;
        }

        internal static bool TryTrackPendingOverflow(Farmer owner, Item item)
        {
            if (owner == null || item == null || item.Stack <= 0 || owner.Items.Contains(item))
                return false;

            string fishId = Utils.SpecialFishHelper.NormalizeItemId(item.QualifiedItemId);
            if (!_pendingFish.TryGetValue(GetPendingKey(owner, fishId), out var data))
                return false;

            data.PendingOverflowItem = item;
            return true;
        }

        internal static bool TryTakePendingOverflow(
            FishingRod rod,
            out Item item,
            out PendingFishData data)
        {
            item = null;
            data = null;
            Farmer owner = rod.getLastFarmerToUse();
            string fishId = rod.whichFish?.QualifiedItemId;
            if (owner == null || string.IsNullOrEmpty(fishId) ||
                !_pendingFish.TryGetValue(GetPendingKey(owner, fishId), out data) ||
                data.PendingOverflowItem == null || data.PendingOverflowItem.Stack <= 0)
            {
                return false;
            }

            item = data.PendingOverflowItem;
            return true;
        }

        private static void ClearPendingOverflow(PendingFishData data)
        {
            if (data != null)
                data.PendingOverflowItem = null;
        }

        private static void AddPendingOverflowToMenu(
            FishingRod rod,
            int remainingFish)
        {
            if (!TryTakePendingOverflow(rod, out Item item, out PendingFishData data))
                return;

            // 当原生 remainingFish == 1 时，原生已经重新创建并放入一条鱼，
            // 所以只清掉旧引用，避免把同一份溢出鱼获重复加入菜单。
            if (remainingFish == 1)
            {
                ClearPendingOverflow(data);
                return;
            }

            if (StardewValley.Game1.activeClickableMenu is StardewValley.Menus.ItemGrabMenu menu)
            {
                if (!menu.ItemsToGrabMenu.actualInventory.Contains(item))
                    menu.ItemsToGrabMenu.actualInventory.Add(item);
                ClearPendingOverflow(data);
                return;
            }

            var fallbackMenu = new StardewValley.Menus.ItemGrabMenu(
                new List<Item> { item }, rod).setEssential(essential: true);
            fallbackMenu.source = 3;
            StardewValley.Game1.activeClickableMenu = fallbackMenu;
            ClearPendingOverflow(data);
        }

        /// <summary>普通钓鱼只有一次 CreateFish；宝箱和 Trout Derby 需保留到原生后续回调。</summary>
        [HarmonyPatch(nameof(FishingRod.doneHoldingFish))]
        [HarmonyPostfix]
        public static void DoneHoldingFish_Postfix(
            FishingRod __instance,
            bool ___treasureCaught,
            bool ___gotTroutDerbyTag)
        {
            try
            {
                if (!___treasureCaught && !___gotTroutDerbyTag)
                    ClearPending(__instance);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.doneHoldingFish Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>清理宝箱分支可能延迟创建的溢出鱼获后，结束本次待处理事实。</summary>
        [HarmonyPatch(nameof(FishingRod.openTreasureMenuEndFunction))]
        [HarmonyPrefix]
        public static void OpenTreasureMenuEndFunction_Prefix(
            FishingRod __instance,
            int remainingFish)
        {
            try
            {
                PrepareAdditionalFishCreation(__instance, remainingFish);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.openTreasureMenuEndFunction Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        [HarmonyPatch(nameof(FishingRod.openTreasureMenuEndFunction))]
        [HarmonyPostfix]
        public static void OpenTreasureMenuEndFunction_Postfix(
            FishingRod __instance,
            int remainingFish)
        {
            try
            {
                AddPendingOverflowToMenu(__instance, remainingFish);
                ClearPending(__instance);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.openTreasureMenuEndFunction Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>清理 Trout Derby 分支最后一次原生 CreateFish 后的待处理事实。</summary>
        [HarmonyPatch(nameof(FishingRod.justGotDerbyTagEndFunction))]
        [HarmonyPrefix]
        public static void JustGotDerbyTagEndFunction_Prefix(
            FishingRod __instance,
            int remainingFish)
        {
            try
            {
                PrepareAdditionalFishCreation(__instance, remainingFish);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.justGotDerbyTagEndFunction Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        [HarmonyPatch(nameof(FishingRod.justGotDerbyTagEndFunction))]
        [HarmonyPostfix]
        public static void JustGotDerbyTagEndFunction_Postfix(
            FishingRod __instance,
            int remainingFish)
        {
            try
            {
                AddPendingOverflowToMenu(__instance, remainingFish);
                ClearPending(__instance);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"FishingRod.justGotDerbyTagEndFunction Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        private static string GetPendingKey(Farmer owner, string fishId)
        {
            return $"{owner.UniqueMultiplayerID}:{Utils.SpecialFishHelper.NormalizeItemId(fishId)}";
        }
    }

    /// <summary>Farmer Patch - 钓鱼数量转化</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingPatches
    {
        /// <summary>
        /// 原生 addItemToInventoryBool 允许“部分加入”并返回 true，但普通钓鱼流程
        /// 只在返回 false 时打开 ItemGrabMenu；高倍鱼的剩余堆叠必须在这里补入原生菜单。
        /// </summary>
        [HarmonyPatch(nameof(Farmer.addItemToInventoryBool))]
        [HarmonyPostfix]
        public static void AddItemToInventoryBool_Postfix(
            Farmer __instance,
            Item item,
            bool __result)
        {
            try
            {
                if (!__instance.IsLocalPlayer || item == null || item.Stack <= 0 ||
                    __instance.Items.Contains(item))
                {
                    return;
                }

                string fishId = Utils.SpecialFishHelper.NormalizeItemId(item.QualifiedItemId);
                if (!FishingRodPatches.TryGetPending(__instance, fishId, out var data))
                    return;

                FishingRod rod = __instance.CurrentTool as FishingRod;
                bool deferToFishingRewardMenu = rod != null &&
                    (rod.treasureCaught || rod.gotTroutDerbyTag);
                if (deferToFishingRewardMenu)
                {
                    FishingRodPatches.TryTrackPendingOverflow(__instance, item);
                    return;
                }

                // 返回 false 时原生 doneHoldingFish 会负责打开菜单；这里只补“部分加入但
                // 返回 true”的剩余堆叠，避免重复打开两个菜单。
                if (!__result)
                    return;

                var menu = new StardewValley.Menus.ItemGrabMenu(
                    new List<Item> { item }, rod).setEssential(essential: true);
                StardewValley.Game1.activeClickableMenu = menu;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Farmer.addItemToInventoryBool Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-009: caughtFish Postfix - 修改进入背包的数量</summary>
        [HarmonyPatch(nameof(Farmer.caughtFish))]
        [HarmonyPostfix]
        public static void CaughtFish_Postfix(
            Farmer __instance,
            string itemId,
            int size,
            bool from_fish_pond,
            int numberCaught)
        {
            try
            {
                // 只处理正常钓鱼（非鱼塘）
                if (from_fish_pond || !__instance.IsLocalPlayer)
                    return;

                string fishId = Utils.SpecialFishHelper.NormalizeItemId(itemId);
                if (!FishingRodPatches.TryGetPending(__instance, fishId, out var data) || data.SuccessRecorded)
                    return;

                data.SuccessRecorded = true;

                // BATCH-010: 根据脱杆次数记录原始等级增长（区间限制由 DifficultyManager 统一执行）
                int baseLevelGain = data.MissCount switch
                {
                    0 => 10,  // 完美
                    1 => 5,
                    2 => 2,
                    _ => 1
                };

                int oldLevel = DifficultyManager.GetDifficultyLevel(fishId);
                int actualLevelGain = DifficultyManager.RecordSuccess(fishId, baseLevelGain);
                int newLevel = DifficultyManager.GetDifficultyLevel(fishId);

                // 高难度星标必须在成功钓起后才写入；鱼王没有待处理数据，不会进入这里。
                DifficultyManager.RecordHighDifficulty(fishId, data.AdjustedDifficulty);

                if (data.Multiplier > 15)
                {
                    int fishSize = data.FishSize > 0
                        ? data.FishSize
                        : Math.Max(1, data.OriginalNum) * data.Multiplier;
                    GiantFishManager.RecordGiantFish(fishId, data.Multiplier, fishSize);
                }

                ModEntry.ModMonitor.Log(
                    $"[Farmer] 难度等级更新 | 鱼ID: {fishId} | " +
                    $"脱杆: {data.MissCount}次 | 基础增长: +{baseLevelGain} | 实际增长: +{actualLevelGain} | {oldLevel} → {newLevel}",
                    LogLevel.Info);

                // BATCH-012/022: 显示成功HUD提示（封顶检测，传入旧等级）
                HUDNotifier.ShowSuccessNotification(fishId, newLevel, oldLevel);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"caughtFish Postfix 失败: {ex}", LogLevel.Error);
            }
        }
    }
}
