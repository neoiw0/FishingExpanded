using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            // BATCH-038: 万能鱼饵加成（难度等级>0 且原生本应给两条鱼时 +10 条）；挑战鱼饵加成（调整后难度>100
            // 且 5 分钟内成功时按原生数量 ×1.5 向上取整；超时只取消数量加成，皇冠/等级照常）；挑战鱼饵标志（流动皇冠判定）。
            public bool WildBaitBonus { get; set; }
            public bool ChallengeBonusActive { get; set; }
            public float ChallengeStarMultiplier { get; set; } = 1f; // BATCH-056: 挑战星惩罚（3 星=1.0、2 星=0.8、1 星=0.6、0 星=0.4）
            public bool HasChallengeBait { get; set; }

            public bool SuccessRecorded { get; set; }
            public bool ExperienceAdjusted { get; set; }
            public int CreateFishCalls { get; set; }
            public bool AllowAdditionalCreateFish { get; set; }
            public bool HarvestLimited { get; set; } // BATCH-061: 本次收获超过每日限额（数量已清零，经验由 gainExperience 前缀读取）
        }

        // BATCH-009: 以玩家+鱼ID隔离待处理事实，不修改原生动画参数
        internal static readonly Dictionary<string, PendingFishData> _pendingFish =
            new Dictionary<string, PendingFishData>();

        /// <summary>获取结算鱼图的视觉缩放；鱼王和没有待处理事实的路径保持原生大小。</summary>
        public static float GetFishVisualScale(FishingRod rod)
        {
            try
            {
                Farmer owner = rod?.getLastFarmerToUse();
                string fishId = rod?.whichFish?.QualifiedItemId;
                if (owner == null || !owner.IsLocalPlayer || string.IsNullOrEmpty(fishId))
                    return 1f;

                string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
                if (!TryGetPending(owner, normalizedFishId, out var data))
                    return 1f;

                float visualScale = DifficultyCalculator.GetVisualScale(data.DifficultyLevel);
                return visualScale;
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("FishingRodPatches.GetFishVisualScale", $"[FishingRodPatches] 结算视觉缩放读取失败: {ex}", LogLevel.Warn);
                return 1f;
            }
        }

        /// <summary>落地真鱼（结算第二处鱼图及原生多鱼图）的底边中点位置调整。
        /// 消费式方法：直接吞掉栈上的位置向量并返回调整后位置，避免在注入 IL 中对 Vector2 使用 Add 指令（会导致 coreclr JIT 访问冲突崩溃）。</summary>
        public static Vector2 AdjustLandingFishPosition(Vector2 position, FishingRod rod)
        {
            float visualScale = GetFishVisualScale(rod);
            if (visualScale <= 1.001f || rod?.whichFish == null)
                return position;

            try
            {
                // 原生落地鱼图使用 (8,8) 中心原点、固定 3f 缩放；放大时保持底边中点不变。
                Rectangle sourceRect = GetCaughtItemSourceRect(rod);
                const float nativeScale = 3f;
                return position + new Vector2(0f, sourceRect.Height * nativeScale * (1f - visualScale) / 2f);
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("FishingRodPatches.AdjustLandingFishPosition", $"[FishingRodPatches] 落地鱼图底边锚点补偿失败: {ex}", LogLevel.Warn);
                return position;
            }
        }

        /// <summary>被钓起物品的绘制源矩形：鱼类使用物品源矩形，非鱼类使用原生固定垃圾图。</summary>
        private static Rectangle GetCaughtItemSourceRect(FishingRod rod)
        {
            if (rod.whichFish.TypeIdentifier == "(O)")
                return rod.whichFish.GetParsedOrErrorData().GetSourceRect();

            return new Rectangle(228, 408, 16, 16);
        }

        /// <summary>被钓起物品的飞行动画纹理名。</summary>
        private static string GetCaughtItemTextureName(FishingRod rod)
        {
            if (rod.whichFish.TypeIdentifier == "(O)")
                return rod.whichFish.GetParsedOrErrorData().TextureName;

            return "LooseSprites\\Cursors";
        }

        /// <summary>从水里飞出的真鱼（原生 TemporaryAnimatedSprite）接入视觉缩放；保持飞行轨迹中心锚点。</summary>
        [HarmonyPatch("doPullFishFromWater")]
        [HarmonyPostfix]
        public static void DoPullFishFromWater_Postfix(FishingRod __instance)
        {
            try
            {
                Farmer owner = __instance?.getLastFarmerToUse();
                string fishId = __instance?.whichFish?.QualifiedItemId;
                if (owner == null || !owner.IsLocalPlayer || string.IsNullOrEmpty(fishId))
                    return;

                string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
                if (!TryGetPending(owner, normalizedFishId, out var data))
                    return;

                float visualScale = DifficultyCalculator.GetVisualScale(data.DifficultyLevel);
                if (visualScale <= 1.001f)
                    return;

                Rectangle sourceRect = GetCaughtItemSourceRect(__instance);
                string textureName = GetCaughtItemTextureName(__instance);
                const float nativeScale = 4f;
                Vector2 centerOffset = new Vector2(
                    sourceRect.Width * nativeScale * (1f - visualScale) / 2f,
                    sourceRect.Height * nativeScale * (1f - visualScale) / 2f);

                foreach (TemporaryAnimatedSprite anim in __instance.animations)
                {
                    if (anim.textureName != textureName || anim.sourceRect != sourceRect)
                        continue;

                    anim.scale *= visualScale;
                    anim.position += centerOffset;
                }
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[FishingRodPatches] 飞行动画视觉缩放失败: {ex}", LogLevel.Warn);
            }
        }

        /// <summary>BATCH-038: 结算尺寸与品质统一应用（doPullFishFromWater Postfix）。
        /// 尺寸：把 __instance.fishSize 改为结算边界算好的乘后值（结算面板/手持数字/记录共用同一字段）；
        /// 品质：难度等级>0 时脱杆不再影响品质——按原生完美规则提升（鱼Quality≥2→铱、≥1→金），原生已完美时幂等。</summary>
        [HarmonyPatch("doPullFishFromWater")]
        [HarmonyPostfix]
        public static void DoPullFishFromWater_SizeQuality_Postfix(FishingRod __instance)
        {
            try
            {
                Farmer owner = __instance?.getLastFarmerToUse();
                string fishId = __instance?.whichFish?.QualifiedItemId;
                if (owner == null || !owner.IsLocalPlayer || string.IsNullOrEmpty(fishId))
                    return;

                string normalizedFishId = SpecialFishHelper.NormalizeItemId(fishId);
                if (!TryGetPending(owner, normalizedFishId, out var data))
                    return;

                if (data.DifficultyLevel != 0 && data.FishSize > 0)
                {
                    __instance.fishSize = data.FishSize;
                }

                if (data.DifficultyLevel > 0)
                {
                    // 原生完美规则：num2>=2 → 4（铱）、num2>=1 → 2（金）；此处等价于“perfect 恒真”。
                    if (__instance.fishQuality >= 2)
                        __instance.fishQuality = 4;
                    else if (__instance.fishQuality >= 1)
                        __instance.fishQuality = 2;
                }

                FishingLog.Log(
                    $"[FishingRod] 结算尺寸/品质应用 | 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"等级: {data.DifficultyLevel} | fishSize: {__instance.fishSize} | fishQuality: {__instance.fishQuality}",
                    LogLevel.Debug);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[FishingRodPatches] 结算尺寸/品质应用失败: {ex}", LogLevel.Warn);
            }
        }

        /// <summary>只缩放落地真鱼（结算第二处鱼图及原生多鱼图），保持底边中点锚点；结算面板与面板内示意图保持原生大小。</summary>
        [HarmonyPatch(nameof(FishingRod.draw))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var getScaleMethod = AccessTools.Method(typeof(FishingRodPatches), nameof(GetFishVisualScale));
            var getPositionOffsetMethod = AccessTools.Method(
                typeof(FishingRodPatches), nameof(AdjustLandingFishPosition));

            int fishMarkerIndex = codes.FindIndex(code => IsLdcI4(code, 1870));
            if (fishMarkerIndex < 0)
            {
                FishingLog.Log(
                    "[FishingRodPatches] 未找到结算面板源矩形，落地真鱼缩放未注入",
                    LogLevel.Warn);
                return codes;
            }

            int firstFishDrawIndex = -1;
            for (int i = fishMarkerIndex; i < codes.Count; i++)
            {
                if (IsSpriteBatchDrawCall(codes[i]))
                {
                    firstFishDrawIndex = i;
                    break;
                }
            }

            // 落地真鱼绘制 = 3f 缩放且其后的下一个调用是 SpriteBatch.Draw 的鱼图；
            // 结算面板（4f）、数量数字（drawTinyDigits 的 3f）和文字绘制不在此列。
            var fishScaleIndices = new HashSet<int>();
            for (int i = Math.Max(0, firstFishDrawIndex + 1); i < codes.Count; i++)
            {
                if (!IsLdcR4(codes[i], 3f))
                    continue;

                for (int j = i + 1; j < Math.Min(codes.Count, i + 12); j++)
                {
                    if (codes[j].opcode != OpCodes.Call && codes[j].opcode != OpCodes.Callvirt)
                        continue;

                    if (IsSpriteBatchDrawCall(codes[j]))
                        fishScaleIndices.Add(i);
                    break;
                }
            }

            var patched = new List<CodeInstruction>(codes.Count + 24);
            int lastGlobalToLocalIndex = -1;
            int landingScaleCount = 0;
            int landingPositionCount = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                patched.Add(code);

                if (i > firstFishDrawIndex && IsGame1GlobalToLocalCall(code))
                    lastGlobalToLocalIndex = patched.Count - 1;

                if (fishScaleIndices.Contains(i))
                {
                    // 栈：[..., 3f] -> [..., 3f * visualScale]
                    patched.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    patched.Add(new CodeInstruction(OpCodes.Call, getScaleMethod));
                    patched.Add(new CodeInstruction(OpCodes.Mul));
                    landingScaleCount++;

                    // 底边中点补偿：紧跟在该鱼图的 GlobalToLocal 之后调用消费式位置调整方法。
                    if (lastGlobalToLocalIndex >= 0)
                    {
                        patched.Insert(lastGlobalToLocalIndex + 1, new CodeInstruction(OpCodes.Ldarg_0));
                        patched.Insert(lastGlobalToLocalIndex + 2, new CodeInstruction(OpCodes.Call, getPositionOffsetMethod));
                        landingPositionCount++;
                        lastGlobalToLocalIndex = -1;
                    }
                }
            }

            FishingLog.Log(
                $"[FishingRodPatches] 落地真鱼缩放注入 | 缩放: {landingScaleCount} | 底边补偿: {landingPositionCount}",
                landingScaleCount >= 1 && landingPositionCount >= 1
                    ? LogLevel.Info
                    : LogLevel.Warn);

            return patched;
        }

        private static bool IsLdcI4(CodeInstruction code, int expected)
        {
            if (code.opcode == OpCodes.Ldc_I4 && code.operand is int value)
                return value == expected;

            return code.opcode switch
            {
                var opcode when opcode == OpCodes.Ldc_I4_M1 => expected == -1,
                var opcode when opcode == OpCodes.Ldc_I4_0 => expected == 0,
                var opcode when opcode == OpCodes.Ldc_I4_1 => expected == 1,
                var opcode when opcode == OpCodes.Ldc_I4_2 => expected == 2,
                var opcode when opcode == OpCodes.Ldc_I4_3 => expected == 3,
                var opcode when opcode == OpCodes.Ldc_I4_4 => expected == 4,
                var opcode when opcode == OpCodes.Ldc_I4_5 => expected == 5,
                var opcode when opcode == OpCodes.Ldc_I4_6 => expected == 6,
                var opcode when opcode == OpCodes.Ldc_I4_7 => expected == 7,
                var opcode when opcode == OpCodes.Ldc_I4_8 => expected == 8,
                _ => false
            };
        }

        private static bool IsLdcR4(CodeInstruction code, float expected)
        {
            return code.opcode == OpCodes.Ldc_R4 && code.operand is float value &&
                Math.Abs(value - expected) < 0.001f;
        }

        private static bool IsGame1GlobalToLocalCall(CodeInstruction code)
        {
            return code.opcode == OpCodes.Call && code.operand is MethodInfo method &&
                method.DeclaringType == typeof(Game1) && method.Name == nameof(Game1.GlobalToLocal);
        }

        private static bool IsSpriteBatchDrawCall(CodeInstruction code)
        {
            return (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) &&
                code.operand is MethodInfo method && method.DeclaringType == typeof(SpriteBatch) &&
                method.Name == nameof(SpriteBatch.Draw);
        }

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
                    FishingLog.Log(
                        $"[FishingRod] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId}",
                        LogLevel.Info);

                    // BATCH-034/039: 原版 5 条传奇鱼钓到一次直接给皇冠（计入可计数皇冠/鱼竿熟练度 α；失败不经过本边界）
                    DifficultyManager.RecordLegendaryCatch(normalizedFishId, owner);

                    // 显示鱼王提示
                    HUDNotifier.ShowSuccessNotification(fishId, 0);
                    return;
                }

                int difficultyLevel = DifficultyManager.GetDifficultyLevel(normalizedFishId, owner);
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);

                // BATCH-010: 获取脱杆次数
                int missCount = 0;
                float adjustedDifficulty = fishDifficulty;
                bool hasChallengeBait = false;
                float elapsedSeconds = 0f;
                if (Game1.activeClickableMenu is StardewValley.Menus.BobberBar bobberBar)
                {
                    missCount = BobberBarPatches.GetMissCount(bobberBar);
                    float trackedDifficulty = BobberBarPatches.GetAdjustedDifficulty(bobberBar);
                    if (trackedDifficulty > 0f)
                        adjustedDifficulty = trackedDifficulty;
                    hasChallengeBait = BobberBarPatches.HasChallengeBait(bobberBar);
                    elapsedSeconds = BobberBarPatches.GetElapsedSeconds(bobberBar);
                }

                // BATCH-038: 万能鱼饵加成判定（难度等级>0 且原生本应给两条鱼，即非挑战鱼饵的 numCaught>=2）；
                // 挑战鱼饵加成判定（调整后难度>100 且 5 分钟内成功；超时只取消数量加成）。
                bool wildBaitBonus = difficultyLevel > 0 && numCaught >= 2 && !hasChallengeBait;
                bool challengeBonusActive = hasChallengeBait && adjustedDifficulty > 100f && elapsedSeconds < 300f;
                float challengeStarMultiplier = 1f;
                if (hasChallengeBait && adjustedDifficulty > 100f && difficultyLevel < 95 && elapsedSeconds >= 300f)
                {
                    challengeStarMultiplier = BobberBarPatches.GetChallengeStarMultiplier(
                        BobberBarPatches.GetChallengeStars(difficultyLevel, elapsedSeconds));
                }

                // BATCH-038: 尺寸数字倍率移到本结算边界统一应用（正等级每级 +10%，负等级每级 -5%）；
                // 与原生“脱杆缩水”解耦（难度等级>0 时缩水已在 BobberBar.update Prefix 禁用）。
                int recordedFishSize = fishSize > 0 && difficultyLevel != 0
                    ? Math.Max(1, (int)Math.Round(fishSize * DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel)))
                    : fishSize;

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
                    FishSize = recordedFishSize,
                    WildBaitBonus = wildBaitBonus,
                    ChallengeBonusActive = challengeBonusActive,
                    ChallengeStarMultiplier = challengeStarMultiplier,
                    HasChallengeBait = hasChallengeBait
                };

                FishingLog.Log(
                    $"[FishingRod] 钓鱼成功（动画阶段）| 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {difficultyLevel} | 数量倍数: {quantityMultiplier} | " +
                    $"脱杆次数: {missCount} | 动画显示: {numCaught}条 | 尺寸(原生→结算): {fishSize} → {recordedFishSize} | " +
                    $"挑战鱼饵: {hasChallengeBait} | 万能加成: {wildBaitBonus} | 挑战加成(5分钟): {challengeBonusActive} | 耗时: {elapsedSeconds:F0}s",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"pullFishFromWater Prefix 失败: {ex}", LogLevel.Error);
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

                bool hasQuantityBonus = data.WildBaitBonus || data.ChallengeBonusActive ||
                    data.ChallengeStarMultiplier < 1f || data.Multiplier > 1;
                if (hasQuantityBonus)
                {
                    int nativeStack = Math.Max(1, __result.Stack);
                    long finalStack = nativeStack;

                    // BATCH-038: 万能鱼饵原本给两条鱼时 +10 条（难度等级>0）；挑战鱼饵 5 分钟内成功 ×1.5 向上取整。
                    // BATCH-056: 超过 5 分钟按掉星惩罚（每颗 −20%，替换原“直接取消 ×1.5”）。
                    if (data.WildBaitBonus)
                        finalStack = nativeStack + 10;
                    else if (data.ChallengeBonusActive)
                        finalStack = (long)Math.Ceiling(nativeStack * 1.5);
                    if (data.ChallengeStarMultiplier < 1f)
                        finalStack = (long)Math.Round(nativeStack * data.ChallengeStarMultiplier);

                    if (data.Multiplier > 1)
                        finalStack *= data.Multiplier;

                    __result.Stack = (int)Math.Min(int.MaxValue, finalStack);
                    FishingLog.Log(
                        $"[FishingRod] 数量转化完成 | 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                        $"原生堆叠: {nativeStack} → 最终: {__result.Stack} | 万能加成: {data.WildBaitBonus} | " +
                        $"挑战加成: {data.ChallengeBonusActive} | 等级倍数: ×{data.Multiplier}",
                        LogLevel.Info);
                }

                // BATCH-061: 每日收获限额——非原版可钓的鱼（Mod 鱼）与非鱼类（垃圾/藻类）按物品单独 333/天；
                // 达到上限后仍可钓（小游戏/难度等级/星星照常），但数量=0 且经验=0（经验由 gainExperience
                // 前缀读取 HarvestLimited 置零）。左下角每类每天首次超限提示一次。
                if (DifficultyManager.IsDailyHarvestLimited(normalizedFishId))
                {
                    int dailyCount = DifficultyManager.ConsumeDailyHarvest(normalizedFishId, owner);
                    if (dailyCount > DifficultyManager.DailyHarvestLimit)
                    {
                        data.HarvestLimited = true;
                        __result.Stack = 0;
                        if (DifficultyManager.MarkDailyLimitNotified(normalizedFishId, owner))
                        {
                            HUDNotifier.ShowDailyLimitReached(normalizedFishId);
                        }
                        FishingLog.Log(
                            $"[FishingRod] 每日收获限额 | 玩家: {owner.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                            $"当日: {dailyCount - 1}/{DifficultyManager.DailyHarvestLimit} → 本次数量清零",
                            LogLevel.Info);
                    }
                }

            }
            catch (Exception ex)
            {
                FishingLog.Log($"FishingRod.CreateFish Postfix 失败: {ex}", LogLevel.Error);
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
                FishingLog.Log($"FishingRod.doneHoldingFish Postfix 失败: {ex}", LogLevel.Error);
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
                FishingLog.Log($"FishingRod.openTreasureMenuEndFunction Prefix 失败: {ex}", LogLevel.Error);
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
                ClearPending(__instance);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"FishingRod.openTreasureMenuEndFunction Postfix 失败: {ex}", LogLevel.Error);
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
                FishingLog.Log($"FishingRod.justGotDerbyTagEndFunction Prefix 失败: {ex}", LogLevel.Error);
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
                ClearPending(__instance);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"FishingRod.justGotDerbyTagEndFunction Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        private static string GetPendingKey(Farmer owner, string fishId)
        {
            return $"{owner.UniqueMultiplayerID}:{Utils.SpecialFishHelper.NormalizeItemId(fishId)}";
        }

    }

    /// <summary>Farmer Patch - 记录成功结算并登记展示事实</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingPatches
    {
        /// <summary>BATCH-038: caughtFish Prefix - 收藏记录尺寸使用结算边界算好的乘后值（幂等：
        /// doPullFishFromWater Postfix 已改字段时值相同；其他路径直接以 PendingFishData 为准）。</summary>
        [HarmonyPatch(nameof(Farmer.caughtFish))]
        [HarmonyPrefix]
        public static void CaughtFish_Prefix(
            Farmer __instance,
            string itemId,
            ref int size,
            bool from_fish_pond)
        {
            try
            {
                if (from_fish_pond || !__instance.IsLocalPlayer || size <= 0)
                    return;

                string fishId = Utils.SpecialFishHelper.NormalizeItemId(itemId);
                if (!FishingRodPatches.TryGetPending(__instance, fishId, out var data) || data.FishSize <= 0)
                    return;

                if (data.DifficultyLevel != 0 && data.FishSize != size)
                {
                    FishingLog.Log(
                        $"[Farmer] 收藏尺寸使用结算值 | 鱼ID: {fishId} | {size} → {data.FishSize}",
                        LogLevel.Debug);
                    size = data.FishSize;
                }
            }
            catch (Exception ex)
            {
                FishingLog.Log($"caughtFish Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>caughtFish Postfix - 记录成功结算并登记展示事实</summary>
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
                // BATCH-029: 每次成功额外增加 round(调整后难度/50)（例：调整后难度500 → +10），与脱杆基数叠加后统一交给称号区间封顶
                int difficultyGain = (int)Math.Round(data.AdjustedDifficulty / 50f);

                int oldLevel = DifficultyManager.GetDifficultyLevel(fishId, __instance);
                int actualLevelGain = DifficultyManager.RecordSuccess(fishId, baseLevelGain + difficultyGain, __instance);
                int newLevel = DifficultyManager.GetDifficultyLevel(fishId, __instance);

                // 高难度星标必须在成功钓起后才写入；鱼王没有待处理数据，不会进入这里。
                DifficultyManager.RecordHighDifficulty(fishId, data.AdjustedDifficulty, __instance);

                // BATCH-038: 流动金色皇冠（难度等级≥95 且挑战鱼饵生效时成功；超 5 分钟只取消数量加成，皇冠照常）
                if (data.DifficultyLevel >= 95 && data.HasChallengeBait)
                {
                    // BATCH-048: 挑战开始时难度等级≥100（100 级鱼）的流动皇冠额外记录 1.2 倍标记。
                    DifficultyManager.RecordChallengeCrown(fishId, __instance, atLevel100: data.DifficultyLevel >= 100);
                }

                // BATCH-058: 挑战鱼饵成功钓起后清除背板种子（下次同鱼同等级重新随机）
                if (data.HasChallengeBait)
                    DifficultyManager.ClearChallengePatternSeed(fishId, data.DifficultyLevel, __instance);

                // BATCH-060: 巨型鱼门槛 = 难度等级 ≥8（与数量倍数解耦；原“数量倍数 >15”等价等级 ≥8）
                if (data.DifficultyLevel >= 8)
                {
                    int fishSize = data.FishSize > 0
                        ? data.FishSize
                        : Math.Max(20, data.OriginalNum * 10);
                    GiantFishManager.RecordGiantFish(__instance, fishId, data.DifficultyLevel, fishSize);
                }

                FishingLog.Log(
                    $"[Farmer] 难度等级更新 | 鱼ID: {fishId} | " +
                    $"脱杆: {data.MissCount}次 | 基础增长: +{baseLevelGain} | 额外难度增益: +{difficultyGain} | 实际增长: +{actualLevelGain} | {oldLevel} → {newLevel}",
                    LogLevel.Info);

                // BATCH-012/022: 显示成功HUD提示（封顶检测，传入旧等级）
                HUDNotifier.ShowSuccessNotification(fishId, newLevel, oldLevel);

                // BATCH-032: 星之果茶掉落（难度等级 ≥50 且概率命中；同一防重边界只发放一次；鱼王无待处理数据天然豁免）
                if (oldLevel >= 50 && DifficultyCalculator.TryGetStarfruitTeaDrop(oldLevel))
                {
                    Item starfruitTea = ItemRegistry.Create("(O)StardropTea", 1);
                    __instance.addItemByMenuIfNecessary(starfruitTea);
                    HUDNotifier.ShowStarfruitTeaNotification(fishId);
                    FishingLog.Log(
                        $"[Farmer] 星之果茶掉落 | 鱼ID: {fishId} | 难度等级: {oldLevel} | 概率: {oldLevel / 400.0:P1}",
                        LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                FishingLog.Log($"caughtFish Postfix 失败: {ex}", LogLevel.Error);
            }
        }
    }
}
