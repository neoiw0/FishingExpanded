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
            public bool SuccessRecorded { get; set; }
            public bool ExperienceAdjusted { get; set; }
            public int CreateFishCalls { get; set; }
            public bool AllowAdditionalCreateFish { get; set; }
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

                float visualScale = DifficultyCalculator.GetVisualScale(data.Multiplier);
                ObjectPatches.LogVisualDiagnostic(
                    "FishingRod.draw",
                    $"{owner.UniqueMultiplayerID}:{normalizedFishId}",
                    $"anchor=center | visualScale={visualScale:F3} | stateHit={visualScale > 1.001f}");
                return visualScale;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[FishingRodPatches] 结算视觉缩放读取失败: {ex}", LogLevel.Warn);
                return 1f;
            }
        }

        /// <summary>落地真鱼（结算第二处鱼图及原生多鱼图）的底边中点位置补偿；鱼王和没有待处理事实的路径返回零偏移。</summary>
        public static Vector2 GetLandingFishPositionOffset(FishingRod rod)
        {
            float visualScale = GetFishVisualScale(rod);
            if (visualScale <= 1.001f || rod?.whichFish == null)
                return Vector2.Zero;

            try
            {
                // 原生落地鱼图使用 (8,8) 中心原点、固定 3f 缩放；放大时保持底边中点不变。
                Rectangle sourceRect = GetCaughtItemSourceRect(rod);
                const float nativeScale = 3f;
                return new Vector2(0f, sourceRect.Height * nativeScale * (1f - visualScale) / 2f);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[FishingRodPatches] 落地鱼图底边锚点补偿失败: {ex}", LogLevel.Warn);
                return Vector2.Zero;
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
        // [HarmonyPatch("doPullFishFromWater")]  BISECT-DISABLED
        // [HarmonyPostfix]  BISECT-DISABLED
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

                float visualScale = DifficultyCalculator.GetVisualScale(data.Multiplier);
                if (visualScale <= 1.001f)
                    return;

                Rectangle sourceRect = GetCaughtItemSourceRect(__instance);
                string textureName = GetCaughtItemTextureName(__instance);
                const float nativeScale = 4f;
                Vector2 centerOffset = new Vector2(
                    sourceRect.Width * nativeScale * (1f - visualScale) / 2f,
                    sourceRect.Height * nativeScale * (1f - visualScale) / 2f);

                int patchedCount = 0;
                foreach (TemporaryAnimatedSprite anim in __instance.animations)
                {
                    if (anim.textureName != textureName || anim.sourceRect != sourceRect)
                        continue;

                    anim.scale *= visualScale;
                    anim.position += centerOffset;
                    patchedCount++;
                }

                ObjectPatches.LogVisualDiagnostic(
                    "FishingRod.fly",
                    $"{owner.UniqueMultiplayerID}:{normalizedFishId}",
                    $"anchor=center | visualScale={visualScale:F3} | sprites={patchedCount} | stateHit={patchedCount > 0}");
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[FishingRodPatches] 飞行动画视觉缩放失败: {ex}", LogLevel.Warn);
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
                typeof(FishingRodPatches), nameof(GetLandingFishPositionOffset));

            int fishMarkerIndex = codes.FindIndex(code => IsLdcI4(code, 1870));
            if (fishMarkerIndex < 0)
            {
                ModEntry.ModMonitor.Log(
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

                    // BISECT-3: 位置补偿禁用（保留缩放注入）
                    landingPositionCount++;
                    lastGlobalToLocalIndex = -1;
                }
            }

            ModEntry.ModMonitor.Log(
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

    /// <summary>Farmer Patch - 记录成功结算并登记展示事实</summary>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerFishingPatches
    {
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