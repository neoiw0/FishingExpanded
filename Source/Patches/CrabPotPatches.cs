using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>蟹笼（CrabPot）收获 Patch（BATCH-065，2026-08-15 用户确认）。
    /// 蟹笼里面拿到的物品也纳入难度等级系统，一律按水藻类（非鱼）规则处理：
    /// 每次收获 = 一次成功（BATCH-078: 固定 +1 级仅 5% 授予，上限 8，8 级给星星，未中掷签 20% 弹轻提示）；
    /// 应用无小游戏专属数量曲线（BATCH-078: 0级=1个、8级=8个，概率平滑）与独立条数收益%
    /// （原生书《Crabbing》25%×2 之后叠加）；
    /// 左下角提示只在等级实际提升时显示（含首次满 8 级"你已是帝王"）。</summary>
    [HarmonyPatch(typeof(CrabPot))]
    internal static class CrabPotPatches
    {
        /// <summary>一次蟹笼收获的待结算事实（BATCH-065：前缀登记，后缀按是否真正入包结算）。</summary>
        private sealed class PendingCrabPotHarvest
        {
            public string ItemId;
            public int OldLevel;
        }

        /// <summary>按玩家隔离的待结算单槽（每玩家 1 槽；同帧前缀→原生→后缀消费，无并发窗口）。</summary>
        private static readonly Dictionary<long, PendingCrabPotHarvest> _pendingHarvest =
            new Dictionary<long, PendingCrabPotHarvest>();

        /// <summary>
        /// 收获前缀：在原生收获逻辑（书《Crabbing》×2、入包、caughtFish、gainExperience）之前，
        /// 应用数量倍数并登记待结算。只处理"有货收获"分支（tileIndexToShow==714）且为本地玩家操作。
        /// 原生顺序（_analysis\crabpot\StardewValley.Objects.CrabPot.decompiled.cs:270-343）：
        /// Stack 读取 → 书×2 → 入包（失败恢复并 return false）→ caughtFish（仅 Data/Fish 有记录的蟹笼鱼）→ gainExperience(1,5)。
        /// BATCH-079：显式优先级 810，高于 Walk of Life 蟹笼替换前缀(800)与 DaLion.Core 漏斗前缀(600)——
        /// 本前缀先完成倍数缩放+登记，WoL 的特殊捕获/漏斗分支继承乘后 Stack 并保持我方后缀结算语义；
        /// 未装 WoL 时排序无竞争者，行为与历史版本一致。
        /// </summary>
        [HarmonyPatch(nameof(CrabPot.checkForAction))]
        [HarmonyPrefix]
        [HarmonyPriority(810)]
        public static void CheckForAction_Prefix(
            CrabPot __instance,
            Farmer who,
            bool justCheckingForActivity)
        {
            try
            {
                if (justCheckingForActivity || who == null || !who.IsLocalPlayer ||
                    __instance.tileIndexToShow != 714)
                {
                    return;
                }

                StardewValley.Object held = __instance.heldObject?.Value;
                if (held == null)
                    return;

                string fishId = SpecialFishHelper.NormalizeItemId(held.QualifiedItemId);
                int level = DifficultyManager.GetDifficultyLevel(fishId, who);
                int multiplier = DifficultyCalculator.GetNoMinigameQuantityMultiplier(level);
                int incomePercent = ModEntry.Config?.ClampedNoMinigameQuantityPercent ?? 100;

                // BATCH-078: 无小游戏物品专属数量锚点曲线（0级=1个、8级=8个，概率平滑）与独立条数收益%；
                // 均先于原生书《Crabbing》×2 应用（原生随后在其基础上叠加），防溢出钳制；
                // 收益缩放作用于乘完等级倍数后的堆叠，向上取整、至少保留 1 个。
                long newStack = held.Stack;
                if (multiplier > 1 && newStack > 0 && newStack <= int.MaxValue / multiplier)
                    newStack *= multiplier;
                if (incomePercent != 100 && newStack > 0)
                    newStack = Math.Max(1, (long)Math.Ceiling(newStack * (incomePercent / 100.0)));
                if (newStack != held.Stack && newStack >= 1 && newStack <= int.MaxValue)
                    held.Stack = (int)newStack;

                _pendingHarvest[who.UniqueMultiplayerID] = new PendingCrabPotHarvest
                {
                    ItemId = fishId,
                    OldLevel = level
                };
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[CrabPot] 收获前缀失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 收获后缀：只有原生真正把物品放入背包（__result==true 且前缀登记过）才结算一次成功。
        /// 背包满/拾起笼子等路径不结算；等级只在提升时提示（用户确认，避免每日多笼刷屏）。
        /// </summary>
        [HarmonyPatch(nameof(CrabPot.checkForAction))]
        [HarmonyPostfix]
        public static void CheckForAction_Postfix(
            CrabPot __instance,
            Farmer who,
            bool justCheckingForActivity,
            bool __result)
        {
            try
            {
                if (who == null)
                    return;

                if (!__result || !who.IsLocalPlayer)
                {
                    _pendingHarvest.Remove(who.UniqueMultiplayerID);
                    return;
                }

                if (!_pendingHarvest.Remove(who.UniqueMultiplayerID, out PendingCrabPotHarvest pending))
                    return;

                int oldLevel = pending.OldLevel;

                // BATCH-078: 无小游戏物品升级掷签——固定 +1 级仅 NonFishLevelUpChance(5%) 概率授予；
                // 未中仍计一次成功；20% 概率弹一条通用轻提示。
                int maxNonFishLevel = SpecialFishHelper.GetMaxLevelForNonFish();
                bool grantLevel = oldLevel < maxNonFishLevel &&
                    Game1.random.NextDouble() < DifficultyManager.NonFishLevelUpChance;
                int actualGain = DifficultyManager.RecordSuccess(pending.ItemId, 1, who, grantLevel);
                int newLevel = DifficultyManager.GetDifficultyLevel(pending.ItemId, who);

                // BATCH-065（用户确认）：只在等级实际提升时提示（含首次满 8 级封顶文案），平时收获不刷屏。
                if (newLevel > oldLevel)
                {
                    HUDNotifier.ShowSuccessNotification(pending.ItemId, newLevel, oldLevel);
                }
                else if (!grantLevel && Game1.random.NextDouble() < DifficultyManager.NonFishLevelMissHintChance)
                {
                    HUDNotifier.ShowTrashMissHint();
                }

                FishingLog.Log(
                    $"[CrabPot] 蟹笼收获记录 | 玩家: {who.UniqueMultiplayerID} | 物品: {pending.ItemId} | " +
                    $"等级: {oldLevel} → {newLevel} | 实际增长: +{actualGain}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[CrabPot] 收获后缀失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>返回标题/切换存档时清空待结算（防残留跨会话误结算）。</summary>
        public static void ClearPending()
        {
            _pendingHarvest.Clear();
        }
    }
}
