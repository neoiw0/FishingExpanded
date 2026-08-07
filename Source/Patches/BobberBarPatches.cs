using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>BobberBar 钓鱼小游戏 UI 的 Patch（支持多人模式）</summary>
    [HarmonyPatch(typeof(BobberBar))]
    internal class BobberBarPatches
    {
        /// <summary>每个BobberBar实例的数据</summary>
        private class InstanceData
        {
            public string FishId { get; set; }
            public int DifficultyLevel { get; set; }
            public float OriginalDifficulty { get; set; }
            public float AdjustedDifficulty { get; set; }
            public float NativeCatchPenaltyModifier { get; set; } = 1f;
            public float LastAppliedCatchPenaltyModifier { get; set; } = 1f;
            public int QuantityMultiplier { get; set; } = 1;
            public long PlayerId { get; set; } = -1L;
            public Farmer Owner { get; set; }
            public bool FailureRecorded { get; set; }
            public bool ResultStarted { get; set; }
            public int MissCount { get; set; } = 0; // BATCH-010: 脱杆次数（完美=0次脱杆）
            public bool WasBobberInBar { get; set; } // 上一帧鱼是否在绿条内

            // BATCH-028: 高难度鱼跳机制状态（调整后难度 150+，非鱼王）
            public float JumpIntervalSeconds { get; set; } // 触发间隔（8/6/5/4/3 秒分档）
            public float JumpCooldownSeconds { get; set; } // 上次跳跃完成后的剩余冷却
            public float JumpDetectionSeconds { get; set; } // 冷却结束后的 1 秒检测计时
            public bool JumpPending { get; set; } // 0.5 秒延迟中
            public float JumpPendingSeconds { get; set; } // 延迟剩余时间
            public float JumpPendingTarget { get; set; } // 待跳目标位置
        }

        // BobberBar 生命周期结束后自动释放，避免异常退出导致静态缓存持有实例。
        private static readonly ConditionalWeakTable<BobberBar, InstanceData> _instanceData =
            new ConditionalWeakTable<BobberBar, InstanceData>();

        /// <summary>构造鱼王小游戏时临时屏蔽隐藏钓鱼等级加成。</summary>
        [HarmonyPatch(MethodType.Constructor, new Type[] {
            typeof(string), typeof(float), typeof(bool), typeof(System.Collections.Generic.List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })]
        [HarmonyPrefix]
        public static void Constructor_Prefix(string whichFish)
        {
            FarmerFishingLevelPatches.SetLegendarySuppression(
                Game1.player, SpecialFishHelper.IsLegendaryFish(whichFish));
        }

        /// <summary>构造函数 Postfix：调整 difficulty、fishSize、fishQuality（BATCH-014: 鱼王豁免）</summary>
        [HarmonyPatch(MethodType.Constructor, new Type[] {
            typeof(string), typeof(float), typeof(bool), typeof(System.Collections.Generic.List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })]
        [HarmonyPostfix]
        public static void Constructor_Postfix(
            BobberBar __instance,
            string whichFish,
            ref float ___difficulty,
            ref int ___fishSize,
            ref int ___fishQuality,
            ref float ___distanceFromCatchPenaltyModifier,
            ref float ___bobberTargetPosition,
            bool ___bobberInBar)
        {
            try
            {
                // BATCH-014: 鱼王类豁免所有规则
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(whichFish);
                if (Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId} | " +
                        $"保持原始difficulty: {___difficulty:F1}",
                        LogLevel.Info);
                    return;
                }

                float originalDifficulty = ___difficulty;
                int difficultyLevel = DifficultyManager.GetDifficultyLevel(normalizedFishId, Game1.player);

                // 存储实例数据
                var instanceData = new InstanceData
                {
                    FishId = normalizedFishId,
                    DifficultyLevel = difficultyLevel,
                    OriginalDifficulty = originalDifficulty,
                    NativeCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
                    LastAppliedCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
                    PlayerId = Game1.player?.UniqueMultiplayerID ?? -1L,
                    Owner = Game1.player,
                    FailureRecorded = false,
                    MissCount = 0,
                    WasBobberInBar = ___bobberInBar
                };
                _instanceData.Add(__instance, instanceData);

                // 1. 调整 difficulty
                float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
                ___difficulty *= difficultyMultiplier;

                // 2. 调整 fishSize（尺寸数字，BATCH-029 按等级线性；数量倍数只用于鱼获数量）
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
                instanceData.QuantityMultiplier = quantityMultiplier;
                ___fishSize = (int)Math.Round(___fishSize * DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel)); // BATCH-029: 尺寸数字按等级线性（正 +10%/级、负 -5%/级），数量倍数只用于鱼获数量

                // 3. 调整 fishQuality
                ___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);

                instanceData.AdjustedDifficulty = ___difficulty;

                // BATCH-028: 高难度运动公式修正——初始目标固定为顶部。
                // 原生公式 (100-难度)/100×548 在难度>100 时为负数（无目标/贴顶），修正为顶部。
                if (instanceData.AdjustedDifficulty > 100f)
                {
                    ___bobberTargetPosition = 0f;
                }

                // BATCH-028: 高难度鱼跳触发间隔按调整后难度分档（150+ 才参与）。
                instanceData.JumpIntervalSeconds = GetJumpInterval(instanceData.AdjustedDifficulty);

                HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
                HUDNotifier.ShowStarChallengeNotification(normalizedFishId, difficultyLevel, Game1.player);

                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 钓鱼小游戏开始 | 实例: {__instance.GetHashCode()} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {difficultyLevel} | " +
                    $"原始difficulty: {originalDifficulty:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | " +
                    $"数量倍数: {quantityMultiplier} | fishSize: {___fishSize} | 品质: {___fishQuality} | " +
                    $"加速增幅档: {(difficultyLevel >= 89 ? "100%（等级≥89）" : "30%（等级≤88）")}",
                    LogLevel.Info);

            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {ex}", LogLevel.Error);
            }
            finally
            {
                FarmerFishingLevelPatches.SetLegendarySuppression(Game1.player, false);
            }
        }

        /// <summary>update Prefix：动态修改蓄力槽减速倍率 + 追踪脱杆次数</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPrefix]
        public static void Update_Prefix(
            BobberBar __instance,
            ref float ___distanceFromCatchPenaltyModifier,
            ref float ___bobberPosition,
            ref float ___bobberTargetPosition,
            ref float ___bobberSpeed,
            float ___distanceFromCatching,
            bool ___bobberInBar)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // BATCH-010: 追踪脱杆次数（检测状态切换：从在绿条内到脱离）
                if (data.WasBobberInBar && !___bobberInBar)
                {
                    // 鱼刚刚离开绿条，计为一次脱杆
                    data.MissCount++;
                }
                data.WasBobberInBar = ___bobberInBar;

                // BATCH-020: 全局蓄力槽保护（任意难度等级）
                float oldModifier = ___distanceFromCatchPenaltyModifier;
                // 字段值若不是上一帧由本 Mod 写入的值，视为原生或其他 Mod 更新了基准倍率。
                if (Math.Abs(oldModifier - data.LastAppliedCatchPenaltyModifier) > 0.0001f)
                {
                    data.NativeCatchPenaltyModifier = oldModifier;
                }

                float newModifier = DifficultyCalculator.GetCatchPenaltyModifier(
                    data.DifficultyLevel, ___distanceFromCatching);
                float combinedModifier = Math.Min(data.NativeCatchPenaltyModifier, newModifier);
                ___distanceFromCatchPenaltyModifier = combinedModifier;
                data.LastAppliedCatchPenaltyModifier = combinedModifier;

                // 只在减速倍率变化时记录（避免每帧输出）
                if (Math.Abs(oldModifier - combinedModifier) > 0.01f)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 蓄力槽保护触发 | 实例: {__instance.GetHashCode()} | 等级: {data.DifficultyLevel} | " +
                        $"蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {oldModifier:F2} → {combinedModifier:F2}",
                        LogLevel.Debug);
                }

                // BATCH-028: 高难度鱼跳机制（调整后难度 150+，非鱼王；鱼王无 InstanceData 天然豁免）。
                // 状态机：冷却（上次跳完成才重新计时）→ 每 1 秒检测上下 25% 区域 → 命中后 0.5 秒延迟
                // → 瞬移到对侧 25% 区域内随机位置；延迟期间鱼游走仍照跳。
                if (data.AdjustedDifficulty >= 150f && !data.ResultStarted)
                {
                    float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                    if (dt <= 0f)
                    {
                        dt = 1f / 60f;
                    }

                    if (data.JumpPending)
                    {
                        data.JumpPendingSeconds -= dt;
                        if (data.JumpPendingSeconds <= 0f)
                        {
                            // 瞬间位移：位置与目标同置，速度归零，避免跳后滑行。
                            ___bobberPosition = data.JumpPendingTarget;
                            ___bobberTargetPosition = data.JumpPendingTarget;
                            ___bobberSpeed = 0f;
                            data.JumpPending = false;
                            data.JumpCooldownSeconds = data.JumpIntervalSeconds;
                            data.JumpDetectionSeconds = 0f;

                            ModEntry.ModMonitor.Log(
                                $"[BobberBar] 高难度鱼跳 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                                $"难度: {data.AdjustedDifficulty:F0} | 跳至: {data.JumpPendingTarget:F0}",
                                LogLevel.Info);
                        }
                    }
                    else if (data.JumpCooldownSeconds > 0f)
                    {
                        data.JumpCooldownSeconds -= dt;
                        if (data.JumpCooldownSeconds < 0f)
                        {
                            data.JumpCooldownSeconds = 0f;
                        }
                    }
                    else
                    {
                        // 冷却完成：统一每 1 秒检测一次鱼是否在上下 25% 区域。
                        data.JumpDetectionSeconds -= dt;
                        if (data.JumpDetectionSeconds <= 0f)
                        {
                            data.JumpDetectionSeconds = 1f;
                            float fishPosition = ___bobberPosition;
                            if (fishPosition >= 399f)
                            {
                                // 下 25% → 跳上 25%（0~133 随机位置）
                                data.JumpPending = true;
                                data.JumpPendingSeconds = 0.5f;
                                data.JumpPendingTarget = Game1.random.Next(0, 134);
                            }
                            else if (fishPosition <= 133f)
                            {
                                // 上 25% → 跳下 25%（399~532 随机位置）
                                data.JumpPending = true;
                                data.JumpPendingSeconds = 0.5f;
                                data.JumpPendingTarget = Game1.random.Next(399, 533);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar.update Prefix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-010: 获取脱杆次数</summary>
        public static int GetMissCount(BobberBar instance)
        {
            if (_instanceData.TryGetValue(instance, out var data))
                return data.MissCount;
            return 0;
        }

        /// <summary>获取当前小游戏实际使用的难度，用于成功后记录收藏星标</summary>
        public static float GetAdjustedDifficulty(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.AdjustedDifficulty : 0f;
        }

        /// <summary>update Postfix：检测钓鱼结果并记录</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPostfix]
        public static void Update_Postfix(
            BobberBar __instance,
            float ___distanceFromCatching,
            bool ___fadeOut)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // 钓鱼失败（只记录一次）
                if (___fadeOut && ___distanceFromCatching <= 0f && !data.FailureRecorded)
                {
                    ModEntry.ModMonitor.Log(
                        $"[BobberBar] 钓鱼失败 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                        $"蓄力槽耗尽: {___distanceFromCatching:F3}",
                        LogLevel.Info);

                    DifficultyManager.RecordFailure(data.FishId, data.Owner ?? Game1.player);

                    // 显示失败 HUD 提示（BATCH-029: 同鱼种连续失败 ≥2 次且本次调整后难度 ≥150 时改用史诗提示）
                    int newLevel = DifficultyManager.GetDifficultyLevel(data.FishId, data.Owner ?? Game1.player);
                    bool isEpicChampion = data.AdjustedDifficulty >= 150f &&
                        DifficultyManager.GetConsecutiveFailCount(data.FishId, data.Owner ?? Game1.player) >= 2;
                    HUDNotifier.ShowFailureNotification(data.FishId, newLevel, isEpicChampion);
                    data.FailureRecorded = true; // BATCH-030: 防止淡出动画期间重复记录失败（BATCH-029 重写时误删）
                    data.ResultStarted = true;
                }

                if (___fadeOut && ___distanceFromCatching >= 1f)
                    data.ResultStarted = true;

                // 原生成功路径在淡出动画结束时才调用 FishingRod.pullFishFromWater；
                // 必须等 fadeOut 结束，否则会提前丢失脱杆次数和实际难度。
                if (data.ResultStarted && !___fadeOut)
                {
                    CleanupInstance(__instance);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar.update Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>清理单个实例数据</summary>
        private static void CleanupInstance(BobberBar instance)
        {
            if (_instanceData.Remove(instance))
            {
                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 清理实例数据 | 实例: {instance.GetHashCode()}",
                    LogLevel.Debug);
            }
        }

        /// <summary>定期清理无效实例（防止内存泄漏）</summary>
        public static void PeriodicCleanup()
        {
            try
            {
                // ConditionalWeakTable 会在 BobberBar 不再被游戏引用时自动释放条目；
                // 这里保留事件入口兼容旧维护台账，不再遍历或强持有实例。
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 定期清理失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-028: 高难度运动公式修正 Transpiler（核对 IL 证据 _analysis/bobberbar-il-20260806）。
        /// 1) 换目标频率三处与 dart 偏移量中的 difficulty 按 150 封顶：
        ///    在 5 处目标换向难度读取（大目标概率/小偏移概率/dart 概率/dart 偏移×2）后注入 Math.Min(d,150)。
        /// 2) 加速度线性增幅：d>100 时 bobberAcceleration × (1+(d-100)/100)，无上限；
        ///    在唯一一处 stfld bobberAcceleration 前注入 boost 乘法。</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            FieldInfo difficultyField = AccessTools.Field(typeof(BobberBar), nameof(BobberBar.difficulty));
            FieldInfo motionTypeField = AccessTools.Field(typeof(BobberBar), nameof(BobberBar.motionType));
            FieldInfo accelerationField = AccessTools.Field(typeof(BobberBar), nameof(BobberBar.bobberAcceleration));
            MethodInfo minMethod = typeof(Math).GetMethod(nameof(Math.Min), new[] { typeof(float), typeof(float) });
            MethodInfo boostMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetAccelerationBoost), BindingFlags.Public | BindingFlags.Static);

            CodeInstruction[] capMin = new[]
            {
                new CodeInstruction(OpCodes.Ldc_R4, 150f),
                new CodeInstruction(OpCodes.Call, minMethod)
            };

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction current = codes[i];

                if (current.opcode == OpCodes.Ldfld && Equals(current.operand, difficultyField))
                {
                    // 按下一条指令唯一区分 5 处注入点：
                    // ldfld motionType = 大目标概率；ldc.r4 2000 = 小偏移概率；ldc.r4 1000 = dart 概率；
                    // conv.i4 + ldc.i4.2 = dart 偏移（正/负两处）。
                    CodeInstruction next = (i + 1 < codes.Count) ? codes[i + 1] : null;
                    CodeInstruction next2 = (i + 2 < codes.Count) ? codes[i + 2] : null;

                    bool isCapped =
                        (next != null && next.opcode == OpCodes.Ldfld && Equals(next.operand, motionTypeField)) ||
                        (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 2000f)) ||
                        (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 1000f)) ||
                        (next != null && next.opcode == OpCodes.Conv_I4 &&
                         next2 != null && next2.opcode == OpCodes.Ldc_I4_2);

                    if (isCapped)
                    {
                        codes.InsertRange(i + 1, capMin);
                        i += capMin.Length;
                    }
                }
                else if (current.opcode == OpCodes.Stfld && Equals(current.operand, accelerationField) && i > 0)
                {
                    // 加速度增幅：栈顶为 (target-pos)/分母，乘 GetAccelerationBoost(instance, difficulty)。
                    // BATCH-029 修复（2026-08-07 首次实测推翻）：注入的 ldfld 会消费注入的 this，
                    // 必须 dup 保留一个 this 作为 (BobberBar, float) 的实例参数，否则 call 参数栈类型不匹配，
                    // PrepareMethod JIT 时抛 InvalidProgramException（原 4 指令序列只适用于单参 float 签名）。
                    codes.InsertRange(i, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld, difficultyField),
                        new CodeInstruction(OpCodes.Call, boostMethod),
                        new CodeInstruction(OpCodes.Mul)
                    });
                    i += 5;
                }
            }

            return codes;
        }

        /// <summary>判断 ldc.r4 操作数是否为指定浮点常量。</summary>
        private static bool IsFloat(object operand, float value)
        {
            return operand is float f && Math.Abs(f - value) < 0.001f;
        }

        /// <summary>BATCH-028/029: 高难度加速度线性增幅。d>100 时按等级档位：等级 ≥89 保持 100% 增幅，
        /// 等级 ≤88 降为现增幅的 30%（例：190 → ×1.27）；d≤100 返回 1（原生不变）。</summary>
        public static float GetAccelerationBoost(BobberBar instance, float difficulty)
        {
            if (difficulty <= 100f) return 1f;
            int level = _instanceData.TryGetValue(instance, out var data) ? data.DifficultyLevel : 89; // 鱼王无 InstanceData：默认 89 档，沿用 BATCH-028 全额增幅（其原生难度>100 时仍生效，未引入新档位）
            float ratio = level >= 89 ? 1f : 0.3f;
            return 1f + ratio * (difficulty - 100f) / 100f;
        }

        /// <summary>BATCH-028: 高难度鱼换目标跳触发间隔（按调整后难度分档，150 以下不参与）。</summary>
        public static float GetJumpInterval(float adjustedDifficulty)
        {
            if (adjustedDifficulty >= 551f) return 3f;
            if (adjustedDifficulty >= 451f) return 4f;
            if (adjustedDifficulty >= 351f) return 5f;
            if (adjustedDifficulty >= 251f) return 6f;
            return 8f; // 150~250
        }
    }
}
