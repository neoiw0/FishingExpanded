using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        /// <summary>BATCH-038: 小游戏浮动提示（起点固定；2 秒内上移 30px 并线性淡出；纯显示状态，不写任何游戏状态）。</summary>
        private class FloatingTip
        {
            public const float Lifetime = 2f;
            public const float RisePixels = 30f;

            public string Text { get; set; }
            public float StartX { get; set; }
            public float StartY { get; set; }
            public float Age { get; set; }
            public bool Centered { get; set; }

            public float Alpha => Math.Max(0f, 1f - Age / Lifetime);
            public float YOffset => RisePixels * (Age / Lifetime);
        }

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

            // BATCH-034: 手感进度 α（构造时按玩家可计数皇冠快照；0=原生手感，1=终点手感）
            public float Alpha { get; set; }

            // BATCH-038: 双提示通道（列表化；起点固定、2 秒内上移 30px 线性淡出；可同时多条，上限 8）
            public List<FloatingTip> ActionTips { get; } = new List<FloatingTip>();
            public List<FloatingTip> OtherTips { get; } = new List<FloatingTip>();

            // BATCH-035/038: 助战（临时钓鱼等级 + 鱼其他提示通道；纯显示）
            public int AssistLevel { get; set; }
            public string AssistFishId { get; set; }

            // BATCH-038: 力竭机制与挑战鱼饵状态
            public string BaitId { get; set; }
            public bool HasChallengeBait { get; set; }
            public float ExhaustionElapsedSeconds { get; set; }
            public int NextExhaustionNodeIndex { get; set; }
            public float EffectiveDifficulty { get; set; }

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

        /// <summary>BATCH-038: 鱼其他提示锚点（钓鱼条左侧空位；原助战在右侧 x+108）。</summary>
        private const float OtherTipAnchorX = 44f;

        /// <summary>BATCH-038: 单通道同时显示上限（超过丢最旧）。</summary>
        private const int MaxFloatingTips = 8;

        /// <summary>BATCH-038: 浮动提示计时（2 秒生命周期；纯显示状态）。</summary>
        private static void AgeTips(List<FloatingTip> tips, float dt)
        {
            for (int i = tips.Count - 1; i >= 0; i--)
            {
                tips[i].Age += dt;
                if (tips[i].Age >= FloatingTip.Lifetime)
                    tips.RemoveAt(i);
            }
        }

        /// <summary>BATCH-038: 追加一条浮动提示（起点固定；超过上限丢最旧）。</summary>
        private static void AddTip(List<FloatingTip> tips, string text, float x, float y, bool centered)
        {
            if (tips.Count >= MaxFloatingTips)
                tips.RemoveAt(0);
            tips.Add(new FloatingTip { Text = text, StartX = x, StartY = y, Centered = centered });
        }

        /// <summary>BATCH-038: 绘制单条浮动提示（2 秒内上移 30px 并线性淡出）。</summary>
        private static void DrawTip(SpriteBatch b, FloatingTip tip)
        {
            if (string.IsNullOrEmpty(tip.Text))
                return;

            Vector2 size = Game1.smallFont.MeasureString(tip.Text);
            float x = tip.Centered ? tip.StartX - size.X / 2f : tip.StartX;
            float y = tip.Centered ? tip.StartY + tip.YOffset : tip.StartY - size.Y / 2f + tip.YOffset;
            Vector2 topLeft = new Vector2(x, y);

            b.DrawString(Game1.smallFont, tip.Text, topLeft + new Vector2(1f, 1f), Color.Black * (tip.Alpha * 0.7f));
            b.DrawString(Game1.smallFont, tip.Text, topLeft, Color.White * tip.Alpha);
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
            bool ___bobberInBar,
            ref int ___bobberBarHeight,
            string baitID,
            float ___bobberBarPos,
            int ___xPositionOnScreen,
            int ___yPositionOnScreen)
        {
            try
            {
                // BATCH-014: 鱼王类豁免所有规则
                string normalizedFishId = SpecialFishHelper.NormalizeItemId(whichFish);
                if (SpecialFishHelper.IsLegendaryFish(normalizedFishId))
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
                    WasBobberInBar = ___bobberInBar,
                    Alpha = DifficultyManager.GetAlpha(Game1.player), // BATCH-034: 手感进度（可计数皇冠/61）
                    BaitId = baitID, // BATCH-038
                    HasChallengeBait = baitID == "(O)ChallengeBait", // BATCH-038
                    EffectiveDifficulty = ___difficulty
                };
                _instanceData.Add(__instance, instanceData);

                // 1. 调整 difficulty
                float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
                ___difficulty *= difficultyMultiplier;

                // 2. fishSize（BATCH-038：等级尺寸倍率移到最终结算边界 pullFishFromWater 统一应用；
                // 构造边界保持原生值，原生“脱杆缩水”在难度等级>0 时由 Update_Prefix 禁用）
                int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
                instanceData.QuantityMultiplier = quantityMultiplier;
                int nativeFishSize = ___fishSize;

                // 3. 调整 fishQuality
                ___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);

                instanceData.AdjustedDifficulty = ___difficulty;
                instanceData.EffectiveDifficulty = ___difficulty;

                // BATCH-035/038: 助战判定（鱼王已豁免；挑战鱼饵下不触发；一次小游戏最多一条助战鱼；
                // 只影响本次小游戏绿条高度，不写玩家状态/存档）；提示起点=触发瞬间鱼条中心。
                float assistTipX = ___xPositionOnScreen + OtherTipAnchorX;
                float assistTipY = ___yPositionOnScreen + 12f + ___bobberBarPos + ___bobberBarHeight / 2f;
                TryTriggerAssist(Game1.player, instanceData, ref ___bobberBarHeight, assistTipX, assistTipY);

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
                    $"数量倍数: {quantityMultiplier} | fishSize(原生): {nativeFishSize} (结算×{DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel):F2}) | 品质: {___fishQuality} | " +
                    $"加速增幅档: {(difficultyLevel >= 98 ? "100%（等级≥98）" : "10%（等级<98）")} | 手感进度α: {instanceData.Alpha:P0} | " +
                    $"力竭: {instanceData.AdjustedDifficulty:F0}{(instanceData.AdjustedDifficulty >= 100f ? "（参与）" : "（不参与）")} | 挑战鱼饵: {instanceData.HasChallengeBait}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>update Prefix：动态修改蓄力槽减速倍率 + 追踪脱杆次数 + 力竭/挑战鱼饵（BATCH-038）</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPrefix]
        public static void Update_Prefix(
            BobberBar __instance,
            ref float ___difficulty,
            ref float ___distanceFromCatchPenaltyModifier,
            ref float ___bobberPosition,
            ref float ___bobberTargetPosition,
            ref float ___bobberSpeed,
            float ___distanceFromCatching,
            bool ___bobberInBar,
            ref int ___fishSizeReductionTimer,
            ref int ___challengeBaitFishes,
            int ___xPositionOnScreen,
            int ___yPositionOnScreen,
            float ___bobberBarPos,
            int ___bobberBarHeight)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (dt <= 0f)
                {
                    dt = 1f / 60f;
                }

                // BATCH-038: 双提示通道计时（起点固定、2 秒内上移 30px 淡出；纯显示状态）
                AgeTips(data.ActionTips, dt);
                AgeTips(data.OtherTips, dt);

                // BATCH-038: 力竭机制（调整后难度≥100 非鱼王非挑战鱼饵：难度随节点衰减；挑战鱼饵：不衰减但节点提示仍显示）。
                // 计时只在小游戏进行中累计（dt 由 update 驱动，暂停/菜单不累计）。
                if (data.AdjustedDifficulty >= 100f && !data.ResultStarted)
                {
                    data.ExhaustionElapsedSeconds += dt;

                    // 节点提示：1/3/5/7/9/12/15 分钟各触发一次（挑战鱼饵时追加一句随机文案）
                    while (data.NextExhaustionNodeIndex < DifficultyCalculator.ExhaustionNodes.Length &&
                           data.ExhaustionElapsedSeconds >= DifficultyCalculator.ExhaustionNodes[data.NextExhaustionNodeIndex].Minute * 60f)
                    {
                        var node = DifficultyCalculator.ExhaustionNodes[data.NextExhaustionNodeIndex];
                        data.NextExhaustionNodeIndex++;
                        string rankName = ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(data.DifficultyLevel));
                        int textIndex = Game1.random.Next(1, 11);
                        string key = $"hud.exhaust.{node.Minute:0}.{textIndex}";
                        string text = ModEntry.ModHelper.Translation.Get(key, new { rankName });
                        if (string.IsNullOrWhiteSpace(text) || text == key)
                            text = $"[{rankName}]体力见底（{node.Minute:0}分钟）";
                        if (data.HasChallengeBait)
                        {
                            int appendIndex = Game1.random.Next(1, 11);
                            string appendKey = $"hud.exhaust.append.{appendIndex}";
                            string appendText = ModEntry.ModHelper.Translation.Get(appendKey);
                            if (string.IsNullOrWhiteSpace(appendText) || appendText == appendKey)
                                appendText = "但这场对决，它还想继续";
                            text = text + appendText;
                        }
                        AddTip(data.OtherTips, text,
                            ___xPositionOnScreen + OtherTipAnchorX,
                            ___yPositionOnScreen + 12f + ___bobberBarPos + ___bobberBarHeight / 2f,
                            centered: false);
                        ModEntry.ModMonitor.Log(
                            $"[BobberBar] 力竭节点 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                            $"节点: {node.Minute:0}分钟 ({node.Percent:P0}) | 耗时: {data.ExhaustionElapsedSeconds:F0}s | " +
                            $"挑战鱼饵: {data.HasChallengeBait} | 文案: {text}",
                            LogLevel.Info);
                    }

                    // 有效难度：挑战鱼饵下不衰减（保持开局调整后难度）；否则按节点线性衰减到 80。
                    data.EffectiveDifficulty = data.HasChallengeBait
                        ? data.AdjustedDifficulty
                        : DifficultyCalculator.GetExhaustedDifficulty(data.AdjustedDifficulty, data.ExhaustionElapsedSeconds);
                    if (Math.Abs(___difficulty - data.EffectiveDifficulty) > 0.001f)
                    {
                        ___difficulty = data.EffectiveDifficulty;
                    }
                }
                else
                {
                    data.EffectiveDifficulty = data.AdjustedDifficulty;
                }

                // BATCH-038: 脱杆尺寸惩罚取消（难度等级>0）：重置原生缩水计时器，鱼尺寸不再随脱杆缩小。
                if (data.DifficultyLevel > 0)
                {
                    ___fishSizeReductionTimer = 800;
                }

                // BATCH-038: 挑战鱼饵改版（调整后难度>100）：原生“3 次脱杆失败”禁用（重置剩余次数），
                // 改为 5 分钟加成时限（超过 5 分钟只取消 50% 数量加成，不影响成功结算）。
                if (data.HasChallengeBait && data.AdjustedDifficulty > 100f)
                {
                    ___challengeBaitFishes = 3;
                }

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

                // BATCH-028/038: 高难度鱼跳机制（有效难度 150+ 或已有待完成跳跃，非鱼王；鱼王无 InstanceData 天然豁免）。
                // 状态机：冷却（上次跳完成才重新计时）→ 每 1 秒检测上下 25% 区域 → 命中后 0.5 秒延迟
                // → 瞬移到对侧 25% 区域内随机位置；延迟期间鱼游走仍照跳。
                if ((data.EffectiveDifficulty >= 150f || data.JumpPending) && !data.ResultStarted)
                {
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

                            // BATCH-034/038: 每次瞬移显示贴鱼动作提示（上跳目标区 0~133=鱼跃类，下跳目标区 399~532=甩尾类）；
                            // 起点固定在触发瞬间（鱼落地位置上方 30px），2 秒内上移淡出，不再跟随鱼。
                            bool jumpUp = data.JumpPendingTarget <= 133f;
                            string jumpText = PickJumpText(jumpUp);
                            AddTip(data.ActionTips, jumpText,
                                ___xPositionOnScreen + 82f,
                                ___yPositionOnScreen + 36f + data.JumpPendingTarget - 30f,
                                centered: true);

                            ModEntry.ModMonitor.Log(
                                $"[BobberBar] 高难度鱼跳 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                                $"难度: {data.EffectiveDifficulty:F0} | 跳至: {data.JumpPendingTarget:F0} | " +
                                $"文案: {(jumpUp ? "上跳(鱼跃)" : "下跳(甩尾)")}: {jumpText}",
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

        /// <summary>BATCH-038: 当前小游戏是否使用挑战鱼饵（结算边界读取）。</summary>
        public static bool HasChallengeBait(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) && data.HasChallengeBait;
        }

        /// <summary>BATCH-038: 当前小游戏已战斗秒数（挑战鱼饵 5 分钟时限判定；仅调整后难度≥100 时累计）。</summary>
        public static float GetElapsedSeconds(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.ExhaustionElapsedSeconds : 0f;
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

        /// <summary>BATCH-028/034: 高难度运动公式修正 + 手感系统 + 帧率解耦 Transpiler
        /// （核对 IL 证据 _analysis/bobberbar-il-20260806，update 方法 986387–987909）。
        /// 1) 换目标频率三处与 dart 偏移量中的 difficulty 按 150 封顶（5 处，BATCH-028 保留）。
        /// 2) 加速度线性增幅（stfld bobberAcceleration 前，BATCH-029 保留，阈值 BATCH-034 改 98）。
        /// 3) 手感系统：绿条速度输入线性混合（ApplyBarInput）、撞边钳制线性（ApplyBounce×2）。
        /// 4) 帧率解耦：概率三处（4000/2000/1000）、漂移 ±0.01×2、平滑 /5、鱼位置积分、绿条位置积分。
        /// 全部注入点按 IL 栈序核对；任何注入点缺失都保留原代码并记录警告。</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            FieldInfo difficultyField = AccessTools.Field(typeof(BobberBar), "difficulty");
            FieldInfo motionTypeField = AccessTools.Field(typeof(BobberBar), "motionType");
            FieldInfo bobberAccelerationField = AccessTools.Field(typeof(BobberBar), "bobberAcceleration");
            FieldInfo bobberSpeedField = AccessTools.Field(typeof(BobberBar), "bobberSpeed");
            FieldInfo bobberPositionField = AccessTools.Field(typeof(BobberBar), "bobberPosition");
            FieldInfo bobberBarSpeedField = AccessTools.Field(typeof(BobberBar), "bobberBarSpeed");
            FieldInfo bobberBarPosField = AccessTools.Field(typeof(BobberBar), "bobberBarPos");
            FieldInfo floaterSinkerAccelerationField = AccessTools.Field(typeof(BobberBar), "floaterSinkerAcceleration");

            MethodInfo mathMinMethod = typeof(Math).GetMethod("Min", new[] { typeof(float), typeof(float) });
            MethodInfo accelerationBoostMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetAccelerationBoost), BindingFlags.Static | BindingFlags.Public);
            MethodInfo frameScaleMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetFrameScale), BindingFlags.Static | BindingFlags.Public);
            MethodInfo smoothFactorMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetSmoothFactor), BindingFlags.Static | BindingFlags.Public);
            MethodInfo scaleProbabilityMethod = typeof(BobberBarPatches).GetMethod(
                nameof(ScaleProbability), BindingFlags.Static | BindingFlags.Public);
            MethodInfo applyBarInputMethod = typeof(BobberBarPatches).GetMethod(
                nameof(ApplyBarInput), BindingFlags.Static | BindingFlags.Public);
            MethodInfo applyBarPositionMethod = typeof(BobberBarPatches).GetMethod(
                nameof(ApplyBarPosition), BindingFlags.Static | BindingFlags.Public);
            MethodInfo applyFishPositionMethod = typeof(BobberBarPatches).GetMethod(
                nameof(ApplyFishPosition), BindingFlags.Static | BindingFlags.Public);
            MethodInfo applyBounceMethod = typeof(BobberBarPatches).GetMethod(
                nameof(ApplyBounce), BindingFlags.Static | BindingFlags.Public);

            // BATCH-028: difficulty 按 150 封顶（换目标频率与 dart 偏移量共用）
            var difficultyCap = new[]
            {
                new CodeInstruction(OpCodes.Ldc_R4, 150f),
                new CodeInstruction(OpCodes.Call, mathMinMethod)
            };

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction current = codes[i];
                CodeInstruction next = i + 1 < codes.Count ? codes[i + 1] : null;
                CodeInstruction nextNext = i + 2 < codes.Count ? codes[i + 2] : null;

                if (current.opcode == OpCodes.Ldfld && Equals(current.operand, difficultyField))
                {
                    if ((next != null && next.opcode == OpCodes.Ldfld && Equals(next.operand, motionTypeField)) ||
                        (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 2000f)) ||
                        (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 1000f)) ||
                        (next != null && next.opcode == OpCodes.Conv_I4 && nextNext != null && nextNext.opcode == OpCodes.Ldc_I4_2))
                    {
                        codes.InsertRange(i + 1, difficultyCap);
                        i += difficultyCap.Length;
                    }
                }
                else if (current.opcode == OpCodes.Stfld && Equals(current.operand, bobberAccelerationField) && i > 0)
                {
                    // BATCH-029/034: 高难度加速度线性增幅。d>100 时按等级档位：等级 ≥98 保持 100% 增幅，
                    // 等级 <98 降为现增幅的 10%（例：190 → ×1.09）；d≤100 返回 1（原生不变）。
                    codes.InsertRange(i, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld, difficultyField),
                        new CodeInstruction(OpCodes.Call, accelerationBoostMethod),
                        new CodeInstruction(OpCodes.Mul)
                    });
                    i += 5;
                }
                else if (current.opcode == OpCodes.Ldc_R4 &&
                    (IsFloat(current.operand, 4000f) || IsFloat(current.operand, 2000f) || IsFloat(current.operand, 1000f)) &&
                    next != null && next.opcode == OpCodes.Div)
                {
                    // 帧率解耦：概率换算 1-(1-p)^k；60fps 恒等。
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, scaleProbabilityMethod));
                    i++;
                }
                else if (current.opcode == OpCodes.Ldc_R4 && IsFloat(current.operand, 0.01f) &&
                    i > 0 && codes[i - 1].opcode == OpCodes.Ldfld &&
                    Equals(codes[i - 1].operand, floaterSinkerAccelerationField))
                {
                    // 帧率解耦：漂移 ±0.01×k。
                    codes.InsertRange(i + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Call, frameScaleMethod),
                        new CodeInstruction(OpCodes.Mul)
                    });
                    i += 2;
                }
                else if (current.opcode == OpCodes.Ldc_R4 && IsFloat(current.operand, 5f) &&
                    next != null && next.opcode == OpCodes.Div && nextNext != null && nextNext.opcode == OpCodes.Add)
                {
                    // 帧率解耦：平滑 /5 → 连续化 ×(1-0.8^k)。
                    codes.InsertRange(i + 2, new[]
                    {
                        new CodeInstruction(OpCodes.Call, smoothFactorMethod),
                        new CodeInstruction(OpCodes.Mul)
                    });
                    i += 2;
                }
                else if (current.opcode == OpCodes.Add && next != null && next.opcode == OpCodes.Stfld &&
                    Equals(next.operand, bobberPositionField))
                {
                    // 帧率解耦：鱼位置积分 pos += delta×k。
                    codes[i] = new CodeInstruction(OpCodes.Call, applyFishPositionMethod);
                }
                else if (current.opcode == OpCodes.Add && i > 3 && IsLocalIndex(codes[i - 1], 4) &&
                    next != null && next.opcode == OpCodes.Stfld && Equals(next.operand, bobberBarSpeedField) &&
                    codes[i - 2].opcode == OpCodes.Ldfld && Equals(codes[i - 2].operand, bobberBarSpeedField) &&
                    codes[i - 3].opcode == OpCodes.Ldarg_0 && codes[i - 4].opcode == OpCodes.Ldarg_0)
                {
                    // 手感系统：绿条输入线性混合（BATCH-034；唯一 num5 累加点）。
                    // 原生 this.bobberBarSpeed += num5 的栈在 add 处为 [inst(stfld 用), speed, num5]，
                    // 底部实例是 stfld 的目标引用且必须保留：ldarg.0; dup; ldfld speed; ldloc num5;
                    // call ApplyBarInput(instance, speed, num5) → [inst, newSpeed] → 原 stfld 直接消费。
                    var ldlocNum5 = codes[i - 1];
                    codes.RemoveRange(i - 4, 5);
                    codes.InsertRange(i - 4, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld, bobberBarSpeedField),
                        ldlocNum5,
                        new CodeInstruction(OpCodes.Call, applyBarInputMethod)
                    });
                }
                else if (current.opcode == OpCodes.Add && next != null && next.opcode == OpCodes.Stfld &&
                    Equals(next.operand, bobberBarPosField))
                {
                    // 帧率解耦：绿条位置积分 pos += speed×k（60fps 恒等）。
                    codes[i] = new CodeInstruction(OpCodes.Call, applyBarPositionMethod);
                }
                else if (current.opcode == OpCodes.Div && i > 7 &&
                    codes[i - 1].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 1].operand, 3f) &&
                    codes[i - 2].opcode == OpCodes.Mul &&
                    codes[i - 3].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 3].operand, 2f) &&
                    codes[i - 4].opcode == OpCodes.Neg &&
                    codes[i - 5].opcode == OpCodes.Ldfld && Equals(codes[i - 5].operand, bobberBarSpeedField) &&
                    codes[i - 6].opcode == OpCodes.Ldarg_0 && codes[i - 7].opcode == OpCodes.Ldarg_0)
                {
                    // 手感系统：撞边反弹保留系数 2/3×(1-α)（底部/顶部两处；α=0 原生反弹，α=1 完全钳制）。
                    // 原生 (0f - speed) * 2f / 3f 在 div 处栈为 [inst(stfld 用), bounced]：
                    // 整体替换为 ldarg.0; dup; dup; ...; call ApplyBounce(instance, bounced) → [inst, bounced×(1-α)]。
                    codes.RemoveRange(i - 7, 8);
                    codes.InsertRange(i - 7, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld, bobberBarSpeedField),
                        new CodeInstruction(OpCodes.Neg),
                        new CodeInstruction(OpCodes.Ldc_R4, 2f),
                        new CodeInstruction(OpCodes.Mul),
                        new CodeInstruction(OpCodes.Ldc_R4, 3f),
                        new CodeInstruction(OpCodes.Div),
                        new CodeInstruction(OpCodes.Call, applyBounceMethod)
                    });
                }
            }

            return codes;
        }

        /// <summary>判断 ldloc/ldloc.s 是否为指定局部变量槽（num5 位于槽 4，核对 IL IL_0719）。</summary>
        private static bool IsLocalIndex(CodeInstruction code, int index)
        {
            if (code.opcode != OpCodes.Ldloc && code.opcode != OpCodes.Ldloc_S)
                return false;
            return code.operand is LocalBuilder local && local.LocalIndex == index;
        }

        /// <summary>判断 ldc.r4 操作数是否为指定浮点常量。</summary>
        private static bool IsFloat(object operand, float value)
        {
            return operand is float f && Math.Abs(f - value) < 0.001f;
        }

        /// <summary>BATCH-028/029/034: 高难度加速度线性增幅。d>100 时按等级档位：等级 ≥98 保持 100% 增幅，
        /// 等级 <98 降为现增幅的 10%（例：190 → ×1.09）；d≤100 返回 1（原生不变）。</summary>
        public static float GetAccelerationBoost(BobberBar instance, float difficulty)
        {
            if (difficulty <= 100f)
                return 1f;

            int level = _instanceData.TryGetValue(instance, out var data) ? data.DifficultyLevel : 98;
            float tierMultiplier = level >= 98 ? 1f : 0.1f;
            return 1f + tierMultiplier * (difficulty - 100f) / 100f;
        }

        /// <summary>BATCH-028: 高难度鱼跳触发间隔（调整后难度分档，数值越小跳得越频繁）。</summary>
        public static float GetJumpInterval(float adjustedDifficulty)
        {
            if (adjustedDifficulty >= 551f) return 3f;
            if (adjustedDifficulty >= 451f) return 4f;
            if (adjustedDifficulty >= 351f) return 5f;
            if (adjustedDifficulty >= 251f) return 6f;
            return 8f; // 150~250
        }

        /// <summary>BATCH-034: 当前实例手感进度 α（构造时快照；无实例数据=0 原生手感）。</summary>
        public static float GetAlpha(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.Alpha : 0f;
        }

        /// <summary>BATCH-034: 帧率解耦缩放 k = dt×60（60fps=1 与原版逐帧完全一致；异常帧回退 1）。</summary>
        public static float GetFrameScale()
        {
            float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
            if (dt <= 0f)
                return 1f;
            float k = dt * 60f;
            return Math.Abs(k - 1f) < 0.0001f ? 1f : k;
        }

        /// <summary>BATCH-034: 帧率解耦——鱼速平滑系数：原生逐帧 x += (t-x)/5 连续化为 ×(1-0.8^k)，60fps 精确 0.2。</summary>
        public static float GetSmoothFactor()
        {
            float k = GetFrameScale();
            if (Math.Abs(k - 1f) < 0.0001f)
                return 0.2f;
            return (float)(1.0 - Math.Pow(0.8, k));
        }

        /// <summary>BATCH-034: 帧率解耦——每帧概率按每秒事件率换算 1-(1-p)^k；60fps 返回原值（统计一致）。</summary>
        public static float ScaleProbability(float probability)
        {
            float k = GetFrameScale();
            if (Math.Abs(k - 1f) < 0.0001f)
                return probability;
            double p = Math.Max(0.0, Math.Min(1.0, probability));
            return (float)(1.0 - Math.Pow(1.0 - p, k));
        }

        /// <summary>BATCH-034/038: 绿条输入响应线性混合。
        /// α=0：原生积分（speed + num5×k）；α=1：直接定速（无加速/阻尼/惯性、撞边完全钳制）。
        /// BATCH-038（用户确认）: 定速最大值随 α 线性减少 30%（α=1 → 21px/s），并叠加相对速度系数：
        /// 绿条中间朝鱼中间移动 ×(1+0.2α)，相背离 ×(1-0.2α)。</summary>
        public static float ApplyBarInput(BobberBar instance, float speed, float num5)
        {
            float k = GetFrameScale();
            float alpha = GetAlpha(instance);
            if (alpha <= 0f)
                return speed + num5 * k;

            float baseMaxSpeed = 30f * (1f - 0.3f * alpha);
            float targetSpeed = (num5 < 0f ? -baseMaxSpeed : baseMaxSpeed) * GetDirectionFactor(instance, num5, alpha);
            return (1f - alpha) * (speed + num5 * k) + alpha * targetSpeed;
        }

        /// <summary>BATCH-038: 相对速度系数——绿条中间朝鱼中间移动 ×(1+0.2α)，相背离 ×(1-0.2α)，随 α 线性。</summary>
        private static float GetDirectionFactor(BobberBar instance, float num5, float alpha)
        {
            if (num5 == 0f || instance == null)
                return 1f;

            float barCenter = instance.bobberBarPos + instance.bobberBarHeight / 2f;
            bool movingUp = num5 < 0f;
            bool towardFish = movingUp ? barCenter > instance.bobberPosition : barCenter < instance.bobberPosition;
            return towardFish ? 1f + 0.2f * alpha : 1f - 0.2f * alpha;
        }

        /// <summary>BATCH-034: 帧率解耦——绿条位置积分 pos += speed×k（60fps 恒等于原生）。</summary>
        public static float ApplyBarPosition(float pos, float speed)
        {
            return pos + speed * GetFrameScale();
        }

        /// <summary>BATCH-034: 帧率解耦——鱼位置积分 pos += delta×k（60fps 恒等于原生）。</summary>
        public static float ApplyFishPosition(float pos, float delta)
        {
            return pos + delta * GetFrameScale();
        }

        /// <summary>BATCH-034: 撞边钳制线性混合——反弹保留系数 (1-α)（α=0 原生 2/3 反弹，α=1 完全钳制速度归零）。</summary>
        public static float ApplyBounce(BobberBar instance, float bounced)
        {
            return bounced * (1f - GetAlpha(instance));
        }

        /// <summary>BATCH-035 自动化验收：单次助战观测（会话内内存，不入存档；fish_assiststats 命令展示）。</summary>
        public readonly struct AssistObservation
        {
            public string FishId { get; }
            public double Rank { get; }
            public int Level { get; }

            public AssistObservation(string fishId, double rank, int level)
            {
                FishId = fishId;
                Rank = rank;
                Level = level;
            }
        }

        private static readonly List<AssistObservation> AssistObservations = new List<AssistObservation>();
        private const int AssistObservationCap = 500;

        /// <summary>BATCH-035 自动化验收：记录一次助战触发（有界 FIFO 上限 500，不写存档；触发边界调用一次）。</summary>
        internal static void RecordAssistObservation(string fishId, double rank, int level)
        {
            if (AssistObservations.Count >= AssistObservationCap)
                AssistObservations.RemoveAt(0);
            AssistObservations.Add(new AssistObservation(fishId, rank, level));
        }

        internal static IReadOnlyList<AssistObservation> GetAssistObservations() => AssistObservations;

        internal static void ClearAssistObservations() => AssistObservations.Clear();

        /// <summary>BATCH-035/038: 助战判定（概率=10%×可计数皇冠/61；命中后按难度加权随机选助战鱼；临时钓鱼等级=round(难度×0.4)；
        /// 绿条高度只加 临时等级×8px，保留原生训练竿/浮标计算；fish_assist 测试命令强制触发；
        /// BATCH-038: 挑战鱼饵生效时不触发助战；提示进入“鱼其他提示”通道）。</summary>
        private static void TryTriggerAssist(Farmer player, InstanceData data, ref int bobberBarHeight, float tipX, float tipY)
        {
            try
            {
                if (player == null || !player.IsLocalPlayer)
                    return;

                bool forced = ModEntry.ConsumeForceAssistFlag();

                // BATCH-038: 挑战鱼饵下不获得其他鱼助战（用户确认）；强制测试标志同样消费。
                if (data.HasChallengeBait)
                    return;

                double chance = DifficultyCalculator.GetAssistChance(
                    DifficultyManager.GetCountableCrownCount(player), DifficultyManager.CountableCrownTarget);
                if (!forced && (chance <= 0.0 || Game1.random.NextDouble() >= chance))
                    return;

                List<string> starredFish = DifficultyManager.GetCountableStarredFish(player);
                if (starredFish.Count == 0)
                    return;

                // BATCH-035（用户修正）: 所有皇冠鱼被选中为助战鱼的概率相同（均匀随机）；助战等级随被选中鱼难度排位倾斜：
                // 最高难度鱼 40 级助战概率 = 最低难度鱼 30 倍；最低+最高两条鱼平均助战等级 = 20。
                string assistFishId = starredFish[Game1.random.Next(starredFish.Count)];
                double assistRank = DifficultyManager.GetAssistRank(assistFishId, player, starredFish);
                int assistLevel = DifficultyCalculator.GetRandomAssistLevel(assistRank);
                RecordAssistObservation(assistFishId, assistRank, assistLevel);

                int oldHeight = bobberBarHeight;
                bobberBarHeight += assistLevel * 8;
                data.AssistLevel = assistLevel;
                data.AssistFishId = assistFishId;
                string assistText = PickAssistText(assistFishId, DifficultyManager.GetDifficultyLevel(assistFishId, player));
                AddTip(data.OtherTips, assistText, tipX, tipY, centered: false);

                ModEntry.ModMonitor.Log(
                    $"[BobberBar] 助战触发 | 助战鱼: {assistFishId} | 难度: {DifficultyManager.GetDifficultyLevel(assistFishId, player)} | 排位: {assistRank:P0} | 临时钓鱼等级: +{assistLevel} | " +
                    $"绿条高度: {oldHeight} → {bobberBarHeight} | 总概率: {chance:P1}{(forced ? " | 测试强制触发" : "")} | 文案: {assistText}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 助战判定失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-035: 助战文案（20 条随机；i18n hud.assist.1~20，含鱼名与鱼职阶称号；缺失回退）。</summary>
        private static string PickAssistText(string fishId, int level)
        {
            try
            {
                string fishName = ItemRegistry.Create(fishId)?.DisplayName ?? fishId;
                string rankName = ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level));
                int index = Game1.random.Next(1, 21);
                string key = $"hud.assist.{index}";
                string text = ModEntry.ModHelper.Translation.Get(key, new { fishName, rankName });
                return string.IsNullOrWhiteSpace(text) || text == key
                    ? $"荣耀的{fishName}{rankName}前来护驾！"
                    : text;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[BobberBar] 助战文案生成失败: {ex.Message}", LogLevel.Error);
                return "皇冠鱼前来护驾！";
            }
        }

        /// <summary>BATCH-034/038: 跳鱼动作提示文案（上跳=鱼跃类 15 条，下跳=甩尾类 15 条；i18n 随机，缺失回退）。</summary>
        private static string PickJumpText(bool jumpUp)
        {
            try
            {
                int index = Game1.random.Next(1, 16);
                string key = jumpUp ? $"hud.jump.up.{index}" : $"hud.jump.down.{index}";
                string text = ModEntry.ModHelper.Translation.Get(key);
                return string.IsNullOrWhiteSpace(text) || text == key
                    ? (jumpUp ? "鱼跃！" : "甩尾！")
                    : text;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[BobberBar] 跳鱼文案生成失败: {ex.Message}", LogLevel.Error);
                return jumpUp ? "鱼跃！" : "甩尾！";
            }
        }

        /// <summary>BATCH-034/038: 小游戏浮动提示绘制（动作提示贴鱼居中；其他提示在钓鱼条左侧；
        /// 起点固定、2 秒内上移 30px 线性淡出；多条可同时显示，纯显示不改变任何状态）。</summary>
        [HarmonyPatch(nameof(BobberBar.draw))]
        [HarmonyPostfix]
        public static void Draw_Postfix(BobberBar __instance, SpriteBatch b)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                foreach (FloatingTip tip in data.ActionTips)
                    DrawTip(b, tip);

                foreach (FloatingTip tip in data.OtherTips)
                    DrawTip(b, tip);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"BobberBar 小游戏文案绘制失败: {ex}", LogLevel.Error);
            }
        }
    }
}
