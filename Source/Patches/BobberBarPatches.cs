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
        /// <summary>BATCH-059: 高难鱼“招式短语”状态机（调整后难度 ≥150）。</summary>
        private enum PhraseMode
        {
            Free,          // 等待下一次瞬移（开局/切换后）
            WaitingMiddle, // 瞬移后等待第一次到达中线（带 ±10，只判到达不判穿越）
            Recording,     // 录制 中线→瞬移→中线 的轨迹
            Playing        // 循环播放
        }

        /// <summary>BATCH-038/039: 小游戏浮动提示（起点固定；默认 5 秒内上移 30px 并线性淡出；纯显示状态，不写任何游戏状态）。
        /// BATCH-060: 助战文案支持 15 秒（LifetimeOverride=15f），其余提示保持 5 秒。</summary>
        private class FloatingTip
        {
            public const float Lifetime = 5f; // BATCH-039: 绿条旁提示统一 5 秒淡出（用户确认）
            public const float RisePixels = 30f;

            public string Text { get; set; }
            public float StartX { get; set; }
            public float StartY { get; set; }
            public float Age { get; set; }
            public bool Centered { get; set; }
            public bool RightAligned { get; set; } // BATCH-041: 左侧通道右对齐，保证文字与绿条间距恒定
            public bool IsActionTip { get; set; } // BATCH-054: 行动提示（鱼跃/甩尾）用宝蓝描边；其他提示用深红描边
            public float PendingDelay { get; set; } // BATCH-058M: 续集提示延迟（前一条结束后才开始显示）
            public long Id { get; set; } // BATCH-058O: 诊断标识（每条提示唯一）
            public bool FlipLogged { get; set; } // BATCH-058O: 侧翻诊断已记录（每条提示最多 1 条）
            public bool DrawLogged { get; set; } // BATCH-058P: 坐标诊断已记录（每条提示最多 1 条）
            public float LifetimeOverride { get; set; } = -1f; // BATCH-060: >0 时覆盖默认 5 秒（助战=15 秒）

            public float DisplayLifetime => LifetimeOverride > 0f ? LifetimeOverride : Lifetime;
            public float Alpha => Math.Max(0f, 1f - Age / DisplayLifetime);
            public float YOffset => RisePixels * (Age / DisplayLifetime);
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
            public bool ProtectionEngaged { get; set; } // BATCH-040: 蓄力槽保护状态（生效/解除各记一条，禁止每帧输出）
            public bool EscapeBonusEngaged { get; set; } // BATCH-051: 逃逸减速加成状态（生效/解除各记一条，禁止每帧输出）
            public bool BarInputDiagnosticLogged { get; set; } // BATCH-052: 手感系统激活诊断（每实例 1 条，防重）
            public int LastChallengeStarsLogged { get; set; } = 3; // BATCH-056: 挑战星掉星日志防重（每掉一颗 1 条）
            // BATCH-058: 停战休息（3 秒绿条不动触发；鱼下次出绿条外 5px 才停；期间蓄力槽不掉）
            public float IdleSeconds { get; set; }
            public bool IdlePending { get; set; }
            public bool IsIdle { get; set; }
            public bool IdleTipShown { get; set; }
            public float LastBarPos { get; set; }
            public float IdleFrozenPosition { get; set; }
            // BATCH-058/058T: 鱼跃/甩尾 0.88 秒前摇（0.77s 转 70° + 0.11s 转回，再瞬移）
            public bool JumpWindupActive { get; set; }
            public float JumpWindupSeconds { get; set; }
            public float JumpWindupStartPosition { get; set; }
            // BATCH-058: 挑战鱼饵背板（固定种子随机源；钓起后清除种子）
            public int PatternSeed { get; set; }
            public Random PatternRandom { get; set; }
            public Random SavedGameRandom { get; set; }
            // BATCH-059: 招式短语录制/回放
            public PhraseMode PhraseMode { get; set; } = PhraseMode.Free;
            public List<(float Time, float Position)> PhraseSamples { get; } = new List<(float, float)>();
            public float PhraseRecordTime { get; set; }
            public bool PhraseJumpSeen { get; set; }
            public float PhraseWindupStartTime { get; set; } = -1f;
            public bool PhraseJumpIsUp { get; set; }
            public string PhraseJumpText { get; set; }
            public float PhraseDuration { get; set; }
            public float PhrasePlayTime { get; set; }
            public bool PlaybackWindupShown { get; set; }
            public float NextPhraseThreshold { get; set; } = 1f / 3f;
            public bool PhraseSwitchPending { get; set; }
            public int QuantityMultiplier { get; set; } = 1;
            public long PlayerId { get; set; } = -1L;
            public Farmer Owner { get; set; }
            public bool FailureRecorded { get; set; }
            public bool ResultStarted { get; set; }
            public int MissCount { get; set; } = 0; // BATCH-010: 脱杆次数（完美=0次脱杆）
            public bool WasBobberInBar { get; set; } // 上一帧鱼是否在绿条内

            // BATCH-034/039: 鱼竿熟练度 α（构造时按玩家可计数皇冠快照；0=原生手感，1=终点手感）
            public float Alpha { get; set; }

            // BATCH-038/039: 双提示通道（列表化；起点固定、5 秒内上移 30px 线性淡出；可同时多条，上限 8）
            public List<FloatingTip> ActionTips { get; } = new List<FloatingTip>();
            public List<FloatingTip> OtherTips { get; } = new List<FloatingTip>();

            // BATCH-035/038: 助战（临时钓鱼等级 + 鱼其他提示通道；纯显示）
            public int AssistLevel { get; set; }
            public string AssistFishId { get; set; }

            // BATCH-038/039: 力竭机制与挑战鱼饵状态（战斗秒数对全部非鱼王实例累计，BATCH-039）
            public string BaitId { get; set; }
            public bool HasChallengeBait { get; set; }
            public float BattleElapsedSeconds { get; set; }
            public int NextExhaustionNodeIndex { get; set; }
            public float EffectiveDifficulty { get; set; }

            // BATCH-039: 持久战机制（30 秒巅峰提示单发；fish_persisttest 强制秒数，0=未强制）
            public bool PeakTipShown { get; set; }
            public float ForcedPerseveranceSeconds { get; set; }

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
        private static long _nextTipId; // BATCH-058O: 提示诊断 ID 递增源

        // BATCH-058K/058N: 提示锚点写死为“距绿条的绝对像素距离”。绿条左缘 = xPositionOnScreen+64，宽 36px（原生 9×4 缩放）。
        // 鱼其他提示：右缘距绿条左缘 50px（右对齐，2026-08-14 用户定稿）；鱼动作提示：左缘距绿条右缘 24px（左对齐）。
        private const float BarLeftX = 64f;
        private const float BarWidth = 36f;
        private const float OtherTipBarGapPixels = 50f; // 其他提示右缘距绿条左缘（用户定稿）
        private const float ActionTipBarGapPixels = 24f; // 动作提示左缘距绿条右缘（用户定稿）
        private const float TipMaxWidthPixels = 420f; // 提示文字最大宽度（写死，防止长文案一路铺到屏幕右缘）
        private const int TipMaxLines = 3; // BATCH-058L: 最多 3 行，超出截断加省略号
        private const float OtherTipAnchorX = BarLeftX - OtherTipBarGapPixels;   // 14（右缘距绿条左缘 50px）
        private const float ActionTipAnchorX = BarLeftX + BarWidth + ActionTipBarGapPixels; // 124（左缘距绿条右缘 24px）

        /// <summary>BATCH-038: 单通道同时显示上限（超过丢最旧）。</summary>
        private const int MaxFloatingTips = 8;

        /// <summary>BATCH-038/039: 浮动提示计时（5 秒生命周期；纯显示状态）。</summary>
        private static void AgeTips(List<FloatingTip> tips, float dt)
        {
            for (int i = tips.Count - 1; i >= 0; i--)
            {
                if (tips[i].PendingDelay > 0f)
                {
                    tips[i].PendingDelay = Math.Max(0f, tips[i].PendingDelay - dt);
                    continue;
                }
                tips[i].Age += dt;
                if (tips[i].Age >= tips[i].DisplayLifetime)
                    tips.RemoveAt(i);
            }
        }

        /// <summary>BATCH-038: 追加一条浮动提示（起点固定；超过上限丢最旧）。BATCH-060: lifetimeOverride>0 时覆盖默认 5 秒（助战=15 秒）。</summary>
        private static void AddTip(List<FloatingTip> tips, string text, float x, float y, bool centered, bool rightAligned = false, bool actionTip = false, float lifetimeOverride = -1f)
        {
            // BATCH-058M: 超长文案按最多 3 行拆成一段 + 续集；续集延迟到前一条结束后排队显示，不丢内容。
            // BATCH-058Q: 分块宽度与 DrawTip 同坐标系（UI 系），保证折行/分块一致。
            float k = Game1.viewport.Width > 0 ? (float)Game1.uiViewport.Width / Game1.viewport.Width : 1f;
            if (k <= 0f || float.IsNaN(k) || float.IsInfinity(k))
                k = 1f;
            float ux = x * k;
            float availableWidth = Math.Min(TipMaxWidthPixels * k,
                rightAligned
                    ? Math.Max(120f * k, ux - 8f * 2f * k)
                    : Math.Max(120f * k, Game1.uiViewport.Width - ux - 8f * 2f * k));
            List<string> chunks = SplitTipChunks(Game1.dialogueFont, text, availableWidth, 1.0f, TipMaxLines);
            for (int i = 0; i < chunks.Count; i++)
            {
                string chunkText = chunks[i];
                if (i < chunks.Count - 1)
                    chunkText += "…";
                if (tips.Count >= MaxFloatingTips)
                    tips.RemoveAt(0);
                tips.Add(new FloatingTip
                {
                    Id = System.Threading.Interlocked.Increment(ref _nextTipId),
                    Text = chunkText,
                    StartX = x,
                    StartY = y,
                    Centered = centered,
                    RightAligned = rightAligned,
                    IsActionTip = actionTip,
                    PendingDelay = i * ((lifetimeOverride > 0f ? lifetimeOverride : FloatingTip.Lifetime) + 0.2f),
                    LifetimeOverride = lifetimeOverride
                });
            }
        }

        /// <summary>BATCH-058M: 把文案按每段最多 maxLinesPerChunk 行拆成多段（每行已在最大宽度内折好）。</summary>
        private static List<string> SplitTipChunks(SpriteFont font, string text, float maxWidth, float scale, int maxLinesPerChunk)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                result.Add(text ?? string.Empty);
                return result;
            }

            List<string> allLines = WrapTipText(font, text, maxWidth, scale);
            for (int i = 0; i < allLines.Count; i += maxLinesPerChunk)
            {
                int count = Math.Min(maxLinesPerChunk, allLines.Count - i);
                result.Add(string.Join(" ", allLines.GetRange(i, count)));
            }
            if (result.Count == 0)
                result.Add(text);
            return result;
        }

        /// <summary>BATCH-038/039/054/058C: 绘制单条浮动提示（5 秒内上移 30px 并线性淡出；
        /// BATCH-058C：改用星露谷 dialogueFont + 右下黑色软阴影；行动提示=白字细宝蓝描边、其他提示=白字细深红描边）。</summary>
        private static void DrawTip(SpriteBatch b, FloatingTip tip)
        {
            if (tip.PendingDelay > 0f || string.IsNullOrEmpty(tip.Text))
                return;

            // BATCH-058Q: 坐标系换算。原生 BobberBar.draw 开头 StartWorldDrawInUI 切到世界 render target
            // （screen buffer，viewport 物理像素系），结尾 EndWorldDrawInUI 恢复 UI render target
            // （uiScreen，uiViewport 逻辑系）；我们的 Draw_Postfix 在 UI 系绘制，但提示几何记录的是
            // 世界系坐标 → 必须乘 k = uiViewport/viewport。否则 uiScale>zoomLevel 时（如 2560x1440
            // 窗口自动 uiScale=2），文字按数值直接画进 1280x720 的 uiScreen，再被 uiScale 拉伸后
            // 跑到屏幕最右侧和下侧（根因已证实，2026-08-14）。
            float k = Game1.viewport.Width > 0 ? (float)Game1.uiViewport.Width / Game1.viewport.Width : 1f;
            if (k <= 0f || float.IsNaN(k) || float.IsInfinity(k))
                k = 1f;

            const float tipScaleBase = 1.0f; // BATCH-058C: dialogueFont 原尺寸（比 smallFont 更粗更清晰）
            float tipScale = tipScaleBase * k;
            float screenMargin = 8f * k; // BATCH-058G: 屏幕边距（换算到 UI 系），防止长文本出屏
            Color baseBorder = tip.IsActionTip ? Color.RoyalBlue : Color.DarkRed;
            Color borderColor = Color.Lerp(baseBorder, Color.White, 0.85f); // BATCH-058H: 非常淡的蓝/红，只留一点点
            SpriteFont tipFont = Game1.dialogueFont;
            float ux = tip.StartX * k;

            // BATCH-058G: 按可用宽度换行（其他提示向左扩展、动作提示向右扩展；中英文都按词/字符折行）
            float availableWidth = Math.Min(TipMaxWidthPixels * k, tip.RightAligned
                ? Math.Max(120f * k, ux - screenMargin * 2f)
                : Math.Max(120f * k, Game1.uiViewport.Width - ux - screenMargin * 2f));
            List<string> lines = WrapTipText(tipFont, tip.Text, availableWidth, tipScale);
            if (lines.Count > TipMaxLines)
            {
                while (lines.Count > TipMaxLines)
                    lines.RemoveAt(lines.Count - 1);
                string lastLine = lines[TipMaxLines - 1];
                const string ellipsis = "…";
                while (tipFont.MeasureString(lastLine + ellipsis).X * tipScale > availableWidth && lastLine.Length > 0)
                    lastLine = lastLine.Substring(0, lastLine.Length - 1);
                lines[TipMaxLines - 1] = lastLine + ellipsis;
            }

            float lineHeight = tipFont.LineSpacing * tipScale;
            float totalHeight = lines.Count * lineHeight;
            float maxWidth = 0f;
            foreach (string line in lines)
                maxWidth = Math.Max(maxWidth, tipFont.MeasureString(line).X * tipScale);

            // BATCH-058O: 钓鱼条可停靠屏幕任意横向位置（原生 clamp 到 [0, viewport.Width-96]）。
            // 原定一侧放不下文字时翻到绿条另一侧（与原生鱼图标翻转同思路），避免被屏幕钳制推到最右缘。
            // BATCH-058Q: 以下 barLeft/barRight/侧翻判断/钳制全部换算到 UI 系（ux=StartX×k）。
            float barLeft;
            float barRight;
            if (tip.RightAligned)
            {
                barLeft = ux + OtherTipBarGapPixels * k;
                barRight = barLeft + BarWidth * k;
            }
            else if (!tip.Centered)
            {
                barRight = ux - ActionTipBarGapPixels * k;
                barLeft = barRight - BarWidth * k;
            }
            else
            {
                barLeft = ux - BarLeftX * k;
                barRight = barLeft + BarWidth * k;
            }

            bool flipped = false;
            float x;
            if (tip.RightAligned)
            {
                if (ux - screenMargin < maxWidth)
                {
                    x = barRight + ActionTipBarGapPixels * k; // 左侧放不下 → 翻到绿条右侧
                    flipped = true;
                }
                else
                {
                    x = ux - maxWidth;
                }
            }
            else if (!tip.Centered)
            {
                if (ux + maxWidth > Game1.uiViewport.Width - screenMargin)
                {
                    x = barLeft - OtherTipBarGapPixels * k - maxWidth; // 右侧放不下 → 翻到绿条左侧
                    flipped = true;
                }
                else
                {
                    x = ux;
                }
            }
            else
            {
                x = ux - maxWidth / 2f;
            }
            x = Math.Max(screenMargin, Math.Min(x, Game1.uiViewport.Width - screenMargin - maxWidth));

            if (flipped && !tip.FlipLogged)
            {
                tip.FlipLogged = true;
                FishingLog.LogRateLimited(
                    "TipSideFlip:" + tip.Id,
                    $"[BobberBar] 提示侧翻 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {barLeft - BarLeftX * k:F0} | StartX: {tip.StartX:F0} | maxWidth: {maxWidth:F0} | finalX: {x:F0} | viewportW: {Game1.viewport.Width}",
                    LogLevel.Info);
            }
            if (!tip.DrawLogged)
            {
                tip.DrawLogged = true;
                FishingLog.LogRateLimited(
                    "TipDraw:" + tip.Id,
                    $"[BobberBar] 提示坐标 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {barLeft - BarLeftX * k:F0} | startX: {tip.StartX:F0} | maxWidth: {maxWidth:F0} | finalX: {x:F0} | viewportW: {Game1.viewport.Width} | uiViewportW: {Game1.uiViewport.Width} | flipped: {flipped}",
                    LogLevel.Info);
            }
            float y = (tip.Centered ? tip.StartY : tip.StartY - totalHeight / 2f) * k + tip.YOffset * k;

            for (int li = 0; li < lines.Count; li++)
            {
                Vector2 linePos = new Vector2(x, y + li * lineHeight);

                // BATCH-058A/058C: 星露谷风黑色软阴影（右下 2px，强度 0.65）
                b.DrawString(tipFont, lines[li], linePos + new Vector2(2f * k, 2f * k), Color.Black * (tip.Alpha * 0.65f), 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);

                // BATCH-054/058A: 4 向细描边（白字 + 彩色边，更精致）
                for (int d = 0; d < 4; d++)
                {
                    Vector2 dir = d switch
                    {
                        0 => new Vector2(1f * k, 0f),
                        1 => new Vector2(-1f * k, 0f),
                        2 => new Vector2(0f, 1f * k),
                        _ => new Vector2(0f, -1f * k)
                    };
                    b.DrawString(tipFont, lines[li], linePos + dir, borderColor * (tip.Alpha * 0.85f), 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
                }
                b.DrawString(tipFont, lines[li], linePos, Color.White * tip.Alpha, 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>BATCH-058G: 按最大宽度折行（英文按词、中文/超长词按字符），返回至少 1 行。</summary>
        private static List<string> WrapTipText(SpriteFont font, string text, float maxWidth, float scale)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                lines.Add(text ?? string.Empty);
                return lines;
            }

            string current = "";
            foreach (string word in text.Split(' '))
            {
                if (word.Length == 0)
                    continue;

                string candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureString(candidate).X * scale <= maxWidth || current.Length == 0)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                    current = "";
                }

                current = word;
                while (current.Length > 0 && font.MeasureString(current).X * scale > maxWidth)
                {
                    int cut = current.Length;
                    while (cut > 1 && font.MeasureString(current.Substring(0, cut)).X * scale > maxWidth)
                        cut--;
                    if (cut < 1)
                        cut = 1;
                    lines.Add(current.Substring(0, cut));
                    current = current.Substring(cut);
                }
            }

            if (current.Length > 0)
                lines.Add(current);
            if (lines.Count == 0)
                lines.Add(text);
            return lines;
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
                // BATCH-039: 消费持久战测试标志（构造边界；鱼王也消费并丢弃，避免泄漏到下一局）
                float forcedPerseveranceSeconds = ModEntry.ConsumeForcePerseveranceSeconds();

                // BATCH-066: 节日原生模式（开关关闭）——不注册实例数据、不改任何 ref 字段。
                // 未注册实例 → Update_Prefix/Postfix、Draw_Postfix 全部自动跳过，Transpiler 注入的
                // 静态方法（GetAlpha=0、GetFrameScale 60fps=1、内联 150 难度封顶对原生 difficulty<150
                // 无影响）全部回落原生，实现"完全原生"。
                if (Services.FestivalFishingService.IsVanillaFestivalMode())
                {
                    FishingLog.Log(
                        $"[节日] 原生模式跳过 BobberBar 注入 | 鱼ID: {SpecialFishHelper.NormalizeItemId(whichFish)} | " +
                        $"开关关闭（节日完全原生）",
                        LogLevel.Info);
                    return;
                }

                // BATCH-014: 鱼王类豁免所有规则
                string normalizedFishId = SpecialFishHelper.NormalizeItemId(whichFish);
                if (SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                {
                    FishingLog.Log(
                        $"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId} | " +
                        $"保持原始difficulty: {___difficulty:F1}",
                        LogLevel.Info);
                    if (forcedPerseveranceSeconds > 0f)
                    {
                        FishingLog.Log(
                            $"[BobberBar] 持久战测试标志被鱼王豁免消耗 | 强制秒数: {forcedPerseveranceSeconds:F0}s",
                            LogLevel.Info);
                    }
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
                    Alpha = DifficultyManager.GetAlpha(Game1.player), // BATCH-034/039: 鱼竿熟练度 α（皇冠分段线性曲线）
                    BaitId = baitID, // BATCH-038
                    HasChallengeBait = baitID == "(O)ChallengeBait", // BATCH-038
                    EffectiveDifficulty = ___difficulty,
                    ForcedPerseveranceSeconds = forcedPerseveranceSeconds // BATCH-039
                };
                _instanceData.Add(__instance, instanceData);

                // BATCH-058: 挑战鱼饵背板种子（同鱼同等级钓起前行为固定；成功钓起后清除）
                // BATCH-058R/S: config.EnableRandomFishBehavior=true 时为全随机模式（更难）——不生成/不使用种子，
                // 整帧随机源保持原生（PatternRandom 为 null 时 Prefix/Postfix 的替换/恢复天然跳过）；默认 false=背板。
                if (instanceData.HasChallengeBait && !ModEntry.Config.EnableRandomFishBehavior)
                {
                    instanceData.PatternSeed = DifficultyManager.GetOrCreateChallengePatternSeed(
                        normalizedFishId, difficultyLevel, Game1.player);
                    instanceData.PatternRandom = new Random(instanceData.PatternSeed);
                }

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

                // BATCH-052: 构造日志数值（加速度实际增幅倍数、跳鱼间隔），每实例 1 条，用于 96/97 与 ≥98 断层取证
                float accelerationBoost = GetAccelerationBoost(__instance, ___difficulty);

                HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
                HUDNotifier.ShowStarChallengeNotification(normalizedFishId, difficultyLevel, Game1.player);

                FishingLog.Log(
                    $"[BobberBar] 钓鱼小游戏开始 | 实例: {__instance.GetHashCode()} | 鱼ID: {normalizedFishId} | " +
                    $"难度等级: {difficultyLevel} | " +
                    $"原始difficulty: {originalDifficulty:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | " +
                    $"数量倍数: {quantityMultiplier} | fishSize(原生): {nativeFishSize} (结算×{DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel):F2}) | 品质: {___fishQuality} | " +
                    $"加速增幅档: {GetAccelerationTier(difficultyLevel):P0} | 加速度增幅: ×{accelerationBoost:F2} | 跳鱼间隔: {instanceData.JumpIntervalSeconds:F0}s | 鱼竿熟练度α: {instanceData.Alpha:P0} | " +
                    $"力竭: {instanceData.AdjustedDifficulty:F0}{(instanceData.AdjustedDifficulty >= 100f ? "（参与）" : "（不参与）")} | 挑战鱼饵: {instanceData.HasChallengeBait}" +
                    $" | barX: {___xPositionOnScreen} | barY: {___yPositionOnScreen} | viewportW: {Game1.viewport.Width}" +
                    (forcedPerseveranceSeconds > 0f ? $" | 持久战测试强制: {forcedPerseveranceSeconds:F0}s" : ""),
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"BobberBar 构造函数 Patch 失败: {ex}", LogLevel.Error);
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
            ref float ___floaterSinkerAcceleration,
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

                // BATCH-058: 挑战鱼饵背板——整帧随机源换成固定种子（Update_Postfix 恢复）
                if (data.PatternRandom != null)
                {
                    data.SavedGameRandom = Game1.random;
                    Game1.random = data.PatternRandom;
                }

                float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (dt <= 0f)
                {
                    dt = 1f / 60f;
                }

                // BATCH-038/039: 双提示通道计时（起点固定、5 秒内上移 30px 淡出；纯显示状态）
                AgeTips(data.ActionTips, dt);
                AgeTips(data.OtherTips, dt);

                // BATCH-058: 挂机待命→停战（鱼下一次出现在绿条外超过 5 像素才停）
                if (data.IdlePending && !data.IsIdle)
                {
                    float barTop = ___bobberBarPos - 32f;
                    float barBottom = barTop + ___bobberBarHeight;
                    bool outsideByMargin =
                        ___bobberPosition - 16f > barBottom + 5f ||
                        ___bobberPosition + 12f < barTop - 5f;
                    if (outsideByMargin)
                    {
                        data.IsIdle = true;
                        data.IdlePending = false;
                        data.IdleFrozenPosition = ___bobberPosition;
                        if (!data.IdleTipShown)
                        {
                            data.IdleTipShown = true;
                            AddTip(data.OtherTips, PickIdleText(),
                                ___xPositionOnScreen + OtherTipAnchorX,
                                ___yPositionOnScreen + 12f + ___bobberBarPos + ___bobberBarHeight / 2f,
                                centered: false, rightAligned: true);
                        }
                    }
                }

                // BATCH-038/039: 战斗计时对全部非鱼王实例累计（dt 由 update 驱动，暂停/菜单不累计）；
                // 力竭难度衰减仍只对调整后难度≥100 生效（BATCH-038）。
                if (!data.ResultStarted && !data.IsIdle)
                {
                    data.BattleElapsedSeconds += dt;

                    // BATCH-039: 30 秒整在“鱼其他提示”通道追加巅峰文案（10 条随机；单实例只触发一次）。
                    if (!data.PeakTipShown && data.BattleElapsedSeconds >= 30f)
                    {
                        data.PeakTipShown = true;
                        string peakText = PickPeakText(data.DifficultyLevel);
                        AddTip(data.OtherTips, peakText,
                            ___xPositionOnScreen + OtherTipAnchorX,
                            ___yPositionOnScreen + 12f + ___bobberBarPos + ___bobberBarHeight / 2f,
                            centered: false, rightAligned: true);
                        FishingLog.Log(
                            $"[BobberBar] 持久战巅峰提示 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                            $"耗时: {data.BattleElapsedSeconds:F0}s | 文案: {peakText}",
                            LogLevel.Info);
                    }

                    if (data.AdjustedDifficulty >= 100f)
                    {
                        // 节点提示：1/3/5/7/9/12/15 分钟各触发一次（挑战鱼饵时追加一句随机文案）
                        while (data.NextExhaustionNodeIndex < DifficultyCalculator.ExhaustionNodes.Length &&
                               data.BattleElapsedSeconds >= DifficultyCalculator.ExhaustionNodes[data.NextExhaustionNodeIndex].Minute * 60f)
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
                                centered: false, rightAligned: true);
                            // BATCH-059A: 非挑战鱼饵下，力竭节点到达 → 打断录播（短语立即退出，
                            // 鱼按已降难度原生运动；仍 ≥150 则下次跳鱼按新难度重录）。挑战鱼饵难度不衰减，不打断。
                            bool phraseInterrupted = !data.HasChallengeBait && data.PhraseMode != PhraseMode.Free;
                            if (phraseInterrupted)
                                InterruptPhrase(data);
                            FishingLog.Log(
                                $"[BobberBar] 力竭节点 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                                $"节点: {node.Minute:0}分钟 ({node.Percent:P0}) | 耗时: {data.BattleElapsedSeconds:F0}s | " +
                                $"挑战鱼饵: {data.HasChallengeBait} | 文案: {text}" +
                                (phraseInterrupted ? " | 短语打断（重录）" : ""),
                                LogLevel.Info);
                        }

                        // 有效难度：挑战鱼饵下不衰减（保持开局调整后难度）；否则按节点线性衰减到 80。
                        data.EffectiveDifficulty = data.HasChallengeBait
                            ? data.AdjustedDifficulty
                            : DifficultyCalculator.GetExhaustedDifficulty(data.AdjustedDifficulty, data.BattleElapsedSeconds);
                        if (Math.Abs(___difficulty - data.EffectiveDifficulty) > 0.001f)
                        {
                            ___difficulty = data.EffectiveDifficulty;
                        }
                    }
                    else
                    {
                        data.EffectiveDifficulty = data.AdjustedDifficulty;
                    }
                }
                else
                {
                    data.EffectiveDifficulty = data.AdjustedDifficulty;
                }

                // BATCH-058: 停战期间鱼冻结（位置/速度/漂移不动；蓄力槽在下方统一置 0）
                if (data.IsIdle)
                {
                    ___bobberSpeed = 0f;
                    ___bobberTargetPosition = ___bobberPosition;
                    ___floaterSinkerAcceleration = 0f;
                }

                // BATCH-038: 脱杆尺寸惩罚取消（难度等级>0）：重置原生缩水计时器，鱼尺寸不再随脱杆缩小。
                if (data.DifficultyLevel > 0)
                {
                    ___fishSizeReductionTimer = 800;
                }

                // BATCH-038: 挑战鱼饵改版（调整后难度>100）：原生“3 次脱杆失败”禁用（重置剩余次数），
                // 改为 5 分钟加成时限 + BATCH-056 原生 3 星接管（5:00 起每分钟掉 1 颗，不可恢复；等级≥95 豁免）。
                if (data.HasChallengeBait && data.AdjustedDifficulty > 100f)
                {
                    int targetStars = GetChallengeStars(data.DifficultyLevel, data.BattleElapsedSeconds);
                    if (targetStars < data.LastChallengeStarsLogged)
                    {
                        data.LastChallengeStarsLogged = targetStars;
                        FishingLog.Log(
                            $"[BobberBar] 挑战星减少 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                            $"剩余星星: {targetStars}/3 | 鱼获惩罚: -{20 * (3 - targetStars)}% | 无法恢复",
                            LogLevel.Info);
                        // BATCH-060（2026-08-15 用户确认）: 掉星时左下角 FIFO 提示（小游戏期间可见），提示鱼获减少
                        HUDNotifier.ShowChallengeStarLoss(targetStars);
                    }
                    // BATCH-056: 原生计数保持 3（禁用脱杆失败）；星星显示由 Draw_Postfix 用原生贴图接管。
                    ___challengeBaitFishes = 3;
                }

                // BATCH-010: 追踪脱杆次数（检测状态切换：从在绿条内到脱离）
                if (data.WasBobberInBar && !___bobberInBar)
                {
                    // 鱼刚刚离开绿条，计为一次脱杆
                    data.MissCount++;
                }
                data.WasBobberInBar = ___bobberInBar;

                // BATCH-020/055: 全局蓄力槽保护（任意难度等级）；阈值滞回 0.5%（未生效用进度+ε、已生效用进度−ε）
                float oldModifier = ___distanceFromCatchPenaltyModifier;
                // 字段值若不是上一帧由本 Mod 写入的值，视为原生或其他 Mod 更新了基准倍率。
                if (Math.Abs(oldModifier - data.LastAppliedCatchPenaltyModifier) > 0.0001f)
                {
                    data.NativeCatchPenaltyModifier = oldModifier;
                }

                const float Hysteresis = 0.005f; // BATCH-055: 1%/20%/40% 阈值边界死区，防止生效/解除逐帧横跳
                float protectionProgress = data.ProtectionEngaged
                    ? Math.Max(0f, ___distanceFromCatching - Hysteresis)
                    : Math.Min(1f, ___distanceFromCatching + Hysteresis);

                float newModifier = DifficultyCalculator.GetCatchPenaltyModifier(
                    data.DifficultyLevel, protectionProgress);

                // BATCH-051/055/057: 调整后难度>100 的连续失败逃跑减速（0→5 次线性插值到 -10 级等效；
                // BATCH-057 用户指令：挑战鱼饵同样参与，95 级以上也吃）。
                bool escapeBonusActive = false;
                int consecutiveFails = 0;
                if (data.AdjustedDifficulty > 100f)
                {
                    consecutiveFails = DifficultyManager.GetConsecutiveFailCount(
                        data.FishId, data.Owner ?? Game1.player);
                    if (consecutiveFails > 0)
                    {
                        float escapeProgress = data.EscapeBonusEngaged
                            ? Math.Max(0f, ___distanceFromCatching - Hysteresis)
                            : Math.Min(1f, ___distanceFromCatching + Hysteresis);
                        newModifier = DifficultyCalculator.GetEscapeFailBonusModifier(
                            data.DifficultyLevel, consecutiveFails, escapeProgress);
                        escapeBonusActive = newModifier < DifficultyCalculator.GetCatchPenaltyModifier(
                            data.DifficultyLevel, escapeProgress) - 0.0001f;
                    }
                }
                if (escapeBonusActive && !data.EscapeBonusEngaged)
                {
                    data.EscapeBonusEngaged = true;
                    FishingLog.Log(
                        $"[BobberBar] 逃逸减速加成生效 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                        $"连续失败: {consecutiveFails} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {newModifier:F2}",
                        LogLevel.Debug);
                }
                else if (!escapeBonusActive && data.EscapeBonusEngaged)
                {
                    data.EscapeBonusEngaged = false;
                    FishingLog.Log(
                        $"[BobberBar] 逃逸减速加成解除 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                        $"减速倍率: {newModifier:F2}",
                        LogLevel.Debug);
                }

                float combinedModifier = Math.Min(data.NativeCatchPenaltyModifier, newModifier);
                if (data.IsIdle)
                    combinedModifier = 0f; // BATCH-058: 停战期间蓄力槽不掉
                ___distanceFromCatchPenaltyModifier = combinedModifier;
                data.LastAppliedCatchPenaltyModifier = combinedModifier;

                // BATCH-040: 蓄力槽保护按“生效/解除”状态转换单发记录（每段保护周期 ≤2 条），禁止每帧输出。
                bool protectionActive = combinedModifier < data.NativeCatchPenaltyModifier - 0.0001f;
                if (protectionActive && !data.ProtectionEngaged)
                {
                    data.ProtectionEngaged = true;
                    FishingLog.Log(
                        $"[BobberBar] 蓄力槽保护生效 | 实例: {__instance.GetHashCode()} | 等级: {data.DifficultyLevel} | " +
                        $"蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {oldModifier:F2} → {combinedModifier:F2}",
                        LogLevel.Debug);
                }
                else if (!protectionActive && data.ProtectionEngaged)
                {
                    data.ProtectionEngaged = false;
                    FishingLog.Log(
                        $"[BobberBar] 蓄力槽保护解除 | 实例: {__instance.GetHashCode()} | 等级: {data.DifficultyLevel} | " +
                        $"蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {combinedModifier:F2} → {oldModifier:F2}",
                        LogLevel.Debug);
                }

                // BATCH-028/038/058: 高难度鱼跳机制（有效难度 150+ 或已有待完成跳跃，非鱼王；鱼王无 InstanceData 天然豁免）。
                // 状态机：冷却（上次跳完成才重新计时）→ 每 1 秒检测上下 25% 区域 → 命中后 0.5 秒延迟
                // → BATCH-058/058T 追加 0.88 秒前摇（原地停留 + 旋转动画）→ 瞬移到对侧 25% 区域内随机位置。
                if ((data.EffectiveDifficulty >= 150f || data.JumpPending) && !data.ResultStarted && !data.IsIdle &&
                    data.PhraseMode != PhraseMode.Playing)
                {
                    if (data.JumpPending)
                    {
                        if (data.JumpWindupActive)
                        {
                            // 0.88 秒前摇：原地停留（冻结），旋转动画由原生鱼图标旋转注入绘制
                            data.JumpWindupSeconds -= dt;
                            ___bobberSpeed = 0f;
                            ___bobberTargetPosition = ___bobberPosition;
                            ___floaterSinkerAcceleration = 0f;
                            if (data.JumpWindupSeconds <= 0f)
                            {
                                // 瞬间位移：位置与目标同置，速度归零，避免跳后滑行。
                                ___bobberPosition = data.JumpPendingTarget;
                                ___bobberTargetPosition = data.JumpPendingTarget;
                                ___bobberSpeed = 0f;
                                data.JumpPending = false;
                                data.JumpWindupActive = false;
                                // BATCH-059A: 跳鱼间隔按当前有效难度动态分档（力竭降档后下一跳变慢；<150 由跳鱼门槛整体停止）
                                data.JumpIntervalSeconds = GetJumpInterval(data.EffectiveDifficulty);
                                data.JumpCooldownSeconds = data.JumpIntervalSeconds;
                                data.JumpDetectionSeconds = 0f;

                                // BATCH-059: 瞬移钩子——短语起点（Free→等待中线）；录制中记录已发生跳鱼
                                if (data.EffectiveDifficulty >= 150f)
                                {
                                    if (data.PhraseMode == PhraseMode.Free)
                                        data.PhraseMode = PhraseMode.WaitingMiddle;
                                    else if (data.PhraseMode == PhraseMode.Recording)
                                        data.PhraseJumpSeen = true;
                                }
                            }
                        }
                        else
                        {
                            data.JumpPendingSeconds -= dt;
                            if (data.JumpPendingSeconds <= 0f)
                            {
                                // BATCH-058/058T: 延迟结束进入 0.88 秒前摇阶段（原地停留 + 旋转动画），再瞬移
                                data.JumpWindupActive = true;
                                data.JumpWindupSeconds = 0.88f; // BATCH-058 用户改 0.22→0.88（同比例）
                                data.JumpWindupStartPosition = ___bobberPosition;
                                if (data.PhraseMode == PhraseMode.Recording)
                                    data.PhraseWindupStartTime = data.PhraseRecordTime;
                                ___bobberSpeed = 0f;
                                ___bobberTargetPosition = ___bobberPosition;
                                ___floaterSinkerAcceleration = 0f;
                            }
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

                            // BATCH-058I: 鱼行动提示在“跳鱼判定、0.5 秒延迟开始”时就显示（比前摇再早 0.5 秒）。
                            // 上跳目标区 0~133=鱼跃类，下跳目标区 399~532=甩尾类；位置=鱼当前位置上方 30px。
                            if (data.JumpPending)
                            {
                                bool jumpUp = data.JumpPendingTarget <= 133f;
                                string jumpText = PickJumpText(jumpUp);
                                AddTip(data.ActionTips, jumpText,
                                    ___xPositionOnScreen + ActionTipAnchorX,
                                    ___yPositionOnScreen + 36f + ___bobberPosition - 30f,
                                    centered: false, actionTip: true);
                                if (data.PhraseMode == PhraseMode.Recording)
                                {
                                    data.PhraseJumpIsUp = jumpUp;
                                    data.PhraseJumpText = jumpText;
                                }

                                FishingLog.Log(
                                    $"[BobberBar] 高难度鱼跳 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                                    $"难度: {data.EffectiveDifficulty:F0} | 跳至: {data.JumpPendingTarget:F0} | " +
                                    $"文案: {(jumpUp ? "上跳(鱼跃)" : "下跳(甩尾)")}: {jumpText}",
                                    LogLevel.Info);
                            }
                        }
                    }
                }

                // BATCH-059: 招式短语（前缀部分：回放位置写入、循环/切换、前摇视觉重放）
                HandlePhrasePrefix(__instance, data,
                    ref ___bobberPosition, ref ___bobberSpeed, ref ___bobberTargetPosition,
                    ref ___floaterSinkerAcceleration, dt);
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("BobberBar.Update_Prefix", $"BobberBar.update Prefix 失败: {ex}", LogLevel.Error);
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

        /// <summary>BATCH-068: 读取该实例的原生难度（构造边界快照，未乘难度倍数；无实例返回 0）。
        /// 供经验基数补偿使用：负数等级/力竭衰减使传入难度低于原生难度时，按原生难度重算经验。</summary>
        public static float GetOriginalDifficulty(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.OriginalDifficulty : 0f;
        }

        /// <summary>BATCH-038: 当前小游戏是否使用挑战鱼饵（结算边界读取）。</summary>
        public static bool HasChallengeBait(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) && data.HasChallengeBait;
        }

        /// <summary>BATCH-038/039: 当前小游戏已战斗秒数（挑战鱼饵 5 分钟时限判定；全部非鱼王实例均累计）。</summary>
        public static float GetElapsedSeconds(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.BattleElapsedSeconds : 0f;
        }

        /// <summary>BATCH-039: 30 秒失败奖励概率（50%）与奖励物品池（+3 钓鱼料理；海泡布丁 (O)265 为 60 秒专属）。
        /// BATCH-052: 228 实为生鱼寿司（Maki Roll，无钓鱼加成）；海之菜肴真实 ID=242（Wiki 物品编号工具核验）。</summary>
        private const double PerseveranceChance = 0.5;
        private const string PerseveranceSeaFoamPudding = "(O)265";
        private static readonly string[] PerseverancePlusThreeFoods = { "(O)242", "(O)728", "(O)730" };

        /// <summary>BATCH-039: 持久战安慰奖励（失败单发边界调用一次；≥60 秒必得海泡布丁 (+4 钓鱼)，
        /// 30~60 秒 50% 概率随机 +3 钓鱼料理；60 秒不叠加 30 秒抽奖（用户确认）；fish_persisttest 强制秒数优先）。
        /// 发放走原生溢出菜单；提示入 FIFO 队列；每次失败最多一次。</summary>
        private static void TryGrantPerseveranceReward(InstanceData data)
        {
            try
            {
                if (data == null || (data.Owner == null && Game1.player == null))
                    return;

                float forcedSeconds = data.ForcedPerseveranceSeconds;
                float elapsed = forcedSeconds > 0f ? forcedSeconds : data.BattleElapsedSeconds;

                string itemId;
                if (elapsed >= 60f)
                {
                    itemId = PerseveranceSeaFoamPudding;
                }
                else if (elapsed >= 30f && Game1.random.NextDouble() < PerseveranceChance)
                {
                    itemId = PerseverancePlusThreeFoods[Game1.random.Next(PerseverancePlusThreeFoods.Length)];
                }
                else
                {
                    return;
                }

                Farmer owner = data.Owner ?? Game1.player;
                Item item = ItemRegistry.Create(itemId, 1);
                owner.addItemByMenuIfNecessary(item);
                HUDNotifier.ShowPerseveranceRewardNotification(data.FishId, data.DifficultyLevel);

                FishingLog.Log(
                    $"[BobberBar] 持久战奖励 | 实例: {data.GetHashCode()} | 鱼ID: {data.FishId} | " +
                    $"耗时: {elapsed:F0}s | 奖励: {item.DisplayName} | 玩家: {owner.UniqueMultiplayerID}" +
                    (forcedSeconds > 0f ? " | 测试强制" : ""),
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"BobberBar 持久战奖励失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>update Postfix：检测钓鱼结果并记录</summary>
        [HarmonyPatch(nameof(BobberBar.update))]
        [HarmonyPostfix]
        public static void Update_Postfix(
            BobberBar __instance,
            float ___distanceFromCatching,
            bool ___fadeOut,
            float ___bobberBarPos,
            ref float ___bobberPosition)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // BATCH-058: 恢复全局随机源（挑战鱼饵背板整帧替换）
                if (data.PatternRandom != null && data.SavedGameRandom != null)
                {
                    Game1.random = data.SavedGameRandom;
                    data.SavedGameRandom = null;
                }

                // BATCH-058: 停战检测——只算绿条：绿条 3 秒不动 → 待命（鱼出绿条外 5px 才停）
                float barDelta = Math.Abs(___bobberBarPos - data.LastBarPos);
                data.LastBarPos = ___bobberBarPos;
                float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (dt <= 0f)
                    dt = 1f / 60f;
                if (barDelta > 0.5f)
                {
                    data.IdleSeconds = 0f;
                    data.IdlePending = false;
                    if (data.IsIdle)
                        data.IsIdle = false; // 玩家重新操作绿条 → 恢复
                }
                else if (!data.IsIdle && !data.IdlePending)
                {
                    data.IdleSeconds += dt;
                    if (data.IdleSeconds >= 3f)
                        data.IdlePending = true;
                }

                // BATCH-058 反证修复：原生漂移（motionType 3/4 的 ±0.01/帧）会破坏绝对静止，
                // 在 Postfix 把鱼位置回正到冻结位置（停战/前摇期间不上下移动）。
                if (data.IsIdle)
                    ___bobberPosition = data.IdleFrozenPosition;
                else if (data.JumpWindupActive && data.PhraseMode != PhraseMode.Playing)
                    ___bobberPosition = data.JumpWindupStartPosition;

                // BATCH-059: 招式短语（后置部分：中线到达判定、轨迹录制、录制结束、1/3 切换）
                HandlePhrasePostfix(data, ___bobberPosition, ___distanceFromCatching);

                // 钓鱼失败（只记录一次）
                if (___fadeOut && ___distanceFromCatching <= 0f && !data.FailureRecorded)
                {
                    FishingLog.Log(
                        $"[BobberBar] 钓鱼失败 | 实例: {__instance.GetHashCode()} | 鱼ID: {data.FishId} | " +
                        $"蓄力槽耗尽: {___distanceFromCatching:F3}",
                        LogLevel.Info);

                    // BATCH-058: 挑战鱼饵失败不掉等级（keepLevel=true；连续失败计数照常）
                    DifficultyManager.RecordFailure(data.FishId, data.Owner ?? Game1.player, keepLevel: data.HasChallengeBait);

                    // 显示失败 HUD 提示（BATCH-029: 同鱼种连续失败 ≥2 次且本次调整后难度 ≥150 时改用史诗提示）
                    int newLevel = DifficultyManager.GetDifficultyLevel(data.FishId, data.Owner ?? Game1.player);
                    bool isEpicChampion = data.AdjustedDifficulty >= 150f &&
                        DifficultyManager.GetConsecutiveFailCount(data.FishId, data.Owner ?? Game1.player) >= 2;
                    HUDNotifier.ShowFailureNotification(data.FishId, newLevel, isEpicChampion);

                    // BATCH-039: 持久战安慰奖励（30 秒 50% +3 料理；60 秒必得海泡布丁；失败单发边界，只发一次）
                    TryGrantPerseveranceReward(data);

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
                FishingLog.LogRateLimited("BobberBar.Update_Postfix", $"BobberBar.update Postfix 失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>清理单个实例数据</summary>
        private static void CleanupInstance(BobberBar instance)
        {
            if (_instanceData.Remove(instance))
            {
                FishingLog.Log(
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
                FishingLog.LogRateLimited("BobberBar.PeriodicCleanup", $"BobberBar 定期清理失败: {ex}", LogLevel.Error);
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
                    // BATCH-029/034/053: 高难度加速度线性增幅。d>100 时按等级锚点曲线取档位
                    // （0→10%、50→20%、70→40%、80→70%、90→100%、100→100%，点间线性）；d≤100 返回 1（原生不变）。
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

        /// <summary>BATCH-058: 把原生鱼图标的旋转参数替换为 GetFishIconRotation（停战摇头摆尾/前摇旋转），
        /// 不新增第二条鱼。匹配链：源矩形含 1840 + Color.White + ldc.r4 0 + Vector2(10,10) + scale 2 + SpriteEffects.None + 0.88。</summary>
        [HarmonyPatch(nameof(BobberBar.draw))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            MethodInfo drawMethod = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[]
            {
                typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color),
                typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)
            });
            MethodInfo rotationMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetFishIconRotation), BindingFlags.Static | BindingFlags.Public);
            MethodInfo whiteGetter = AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));
            MethodInfo colorMethod = typeof(BobberBarPatches).GetMethod(
                nameof(GetFishIconColor), BindingFlags.Static | BindingFlags.Public);
            ConstructorInfo vector2Ctor = typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) });

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Callvirt || !Equals(codes[i].operand, drawMethod) || i < 8)
                    continue;

                bool chainMatches =
                    codes[i - 1].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 1].operand, 0.88f) &&
                    codes[i - 2].opcode == OpCodes.Ldc_I4_0 &&
                    codes[i - 3].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 3].operand, 2f) &&
                    codes[i - 4].opcode == OpCodes.Newobj && Equals(codes[i - 4].operand, vector2Ctor) &&
                    codes[i - 5].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 5].operand, 10f) &&
                    codes[i - 6].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 6].operand, 10f) &&
                    codes[i - 7].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 7].operand, 0f) &&
                    codes[i - 8].opcode == OpCodes.Call && Equals(codes[i - 8].operand, whiteGetter);
                if (!chainMatches)
                    continue;

                bool isFish = false;
                for (int k = i - 9; k >= Math.Max(0, i - 35); k--)
                {
                    if (codes[k].opcode == OpCodes.Ldc_I4 && Equals(codes[k].operand, 1840))
                    {
                        isFish = true;
                        break;
                    }
                }
                if (!isFish)
                    continue;

                // 颜色 → GetFishIconColor（0.88s 前摇发出红光）
                codes.RemoveAt(i - 8);
                codes.InsertRange(i - 8, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, colorMethod)
                });
                // 旋转 → GetFishIconRotation（原 i-7 经上一步移位到 i-6）
                codes.RemoveAt(i - 6);
                codes.InsertRange(i - 6, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, rotationMethod)
                });
                i += 2;
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

        /// <summary>BATCH-028/029/034/053: 高难度加速度线性增幅。d>100 时按等级锚点曲线取档位
        /// （0→10%、50→20%、70→40%、80→70%、90→100%、100→100%，点间线性）；d≤100 返回 1（原生不变）。</summary>
        public static float GetAccelerationBoost(BobberBar instance, float difficulty)
        {
            // BATCH-066: 节日原生模式（开关关闭）→ 原生 1（无模组增幅）
            if (Services.FestivalFishingService.IsVanillaFestivalMode())
                return 1f;

            if (difficulty <= 100f)
                return 1f;

            int level = _instanceData.TryGetValue(instance, out var data) ? data.DifficultyLevel : 100;
            float tier = GetAccelerationTier(level);
            return 1f + tier * (difficulty - 100f) / 100f;
        }

        /// <summary>BATCH-053: 加速度增幅档位锚点曲线（用户 2026-08-12 定稿）：
        /// 0→10%、50→20%、70→40%、80→70%、90→100%、100→100%，点间线性；≤0 钳 10%、≥90 钳 100%。</summary>
        public static float GetAccelerationTier(int level)
        {
            if (level <= 0) return 0.10f;
            if (level >= 90) return 1.00f;
            if (level < 50) return 0.10f + 0.10f * level / 50f;
            if (level < 70) return 0.20f + 0.20f * (level - 50) / 20f;
            if (level < 80) return 0.40f + 0.30f * (level - 70) / 10f;
            return 0.70f + 0.30f * (level - 80) / 10f;
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

        /// <summary>BATCH-034/039: 当前实例鱼竿熟练度 α（构造时快照；无实例数据=0 原生手感）。</summary>
        public static float GetAlpha(BobberBar instance)
        {
            return _instanceData.TryGetValue(instance, out var data) ? data.Alpha : 0f;
        }

        /// <summary>BATCH-056: 挑战星剩余数量（原生 3 星；5:00 起每分钟掉 1 颗，不可恢复；难度等级≥95 豁免）。</summary>
        public static int GetChallengeStars(int difficultyLevel, float elapsedSeconds)
        {
            if (difficultyLevel >= 95)
                return 3;
            float overFiveMinutes = elapsedSeconds - 300f;
            if (overFiveMinutes < 0f)
                return 3;
            return Math.Max(0, 3 - ((int)Math.Floor(overFiveMinutes / 60f) + 1));
        }

        /// <summary>BATCH-056: 每掉 1 颗星鱼获 −20%（3 星=100%、2 星=80%、1 星=60%、0 星=40%）。</summary>
        public static float GetChallengeStarMultiplier(int stars)
        {
            return 1f - 0.2f * (3 - stars);
        }

        /// <summary>BATCH-034: 帧率解耦缩放 k = dt×60（60fps=1 与原版逐帧完全一致；异常帧回退 1）。</summary>
        public static float GetFrameScale()
        {
            // BATCH-066: 节日原生模式（开关关闭）→ 原生逐帧（k=1，60fps 恒等）
            if (Services.FestivalFishingService.IsVanillaFestivalMode())
                return 1f;

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
        /// BATCH-038/056（用户确认）: 定速最大值 `30−5α`（α=1 → 25px/帧），并叠加相对速度系数：
        /// 绿条中间朝鱼中间移动 ×(1+0.2α)，相背离 ×(1-0.2α)（α=1 → ×1.2/×0.8）。</summary>
        public static float ApplyBarInput(BobberBar instance, float speed, float num5)
        {
            float k = GetFrameScale();
            float alpha = GetAlpha(instance);
            if (alpha <= 0f)
                return speed + num5 * k;

            float baseMaxSpeed = 30f - 5f * alpha; // BATCH-056: α=1 → 25px/帧（原 30×(1−0.3α)=21）
            float directionFactor = GetDirectionFactor(instance, num5, alpha);
            float targetSpeed = (num5 < 0f ? -baseMaxSpeed : baseMaxSpeed) * directionFactor;

            // BATCH-052: 手感系统激活诊断（每实例首帧 1 条；验证 Transpiler 是否运行时命中；验收后删除或转长期低频）
            if (_instanceData.TryGetValue(instance, out var data) && !data.BarInputDiagnosticLogged)
            {
                data.BarInputDiagnosticLogged = true;
                FishingLog.Log(
                    $"[BobberBar] 手感系统激活 | 实例: {instance.GetHashCode()} | α: {alpha:P0} | " +
                    $"方向系数: {directionFactor:F2} | 目标速度: {targetSpeed:F1}px/帧 | 原生分量: {speed + num5 * k:F1}",
                    LogLevel.Info);
            }

            return (1f - alpha) * (speed + num5 * k) + alpha * targetSpeed;
        }

        /// <summary>BATCH-038/056: 相对速度系数——绿条中间朝鱼中间移动 ×(1+0.2α)，相背离 ×(1-0.2α)，随 α 线性（α=1 → ×1.2/×0.8）。</summary>
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
                // BATCH-060（2026-08-15 用户确认）: 助战提示持续时间 15 秒（仅助战文案，其他提示保持 5 秒）
                AddTip(data.OtherTips, assistText, tipX, tipY, centered: false, rightAligned: true, lifetimeOverride: 15f);

                FishingLog.Log(
                    $"[BobberBar] 助战触发 | 助战鱼: {assistFishId} | 难度: {DifficultyManager.GetDifficultyLevel(assistFishId, player)} | 排位: {assistRank:P0} | 临时钓鱼等级: +{assistLevel} | " +
                    $"绿条高度: {oldHeight} → {bobberBarHeight} | 总概率: {chance:P1}{(forced ? " | 测试强制触发" : "")} | 文案: {assistText}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"BobberBar 助战判定失败: {ex}", LogLevel.Error);
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
                FishingLog.Log($"[BobberBar] 助战文案生成失败: {ex.Message}", LogLevel.Error);
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
                FishingLog.Log($"[BobberBar] 跳鱼文案生成失败: {ex.Message}", LogLevel.Error);
                return jumpUp ? "鱼跃！" : "甩尾！";
            }
        }

        /// <summary>BATCH-039: 30 秒巅峰提示文案（10 条随机；i18n hud.peak.1~10，含职阶；缺失回退）。</summary>
        private static string PickPeakText(int level)
        {
            try
            {
                string rankName = ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level));
                int index = Game1.random.Next(1, 11);
                string key = $"hud.peak.{index}";
                string text = ModEntry.ModHelper.Translation.Get(key, new { rankName });
                return string.IsNullOrWhiteSpace(text) || text == key
                    ? $"[{rankName}]的力气达到巅峰"
                    : text;
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[BobberBar] 巅峰文案生成失败: {ex.Message}", LogLevel.Error);
                return "[职阶]的力气达到巅峰";
            }
        }

        /// <summary>BATCH-034/038: 小游戏浮动提示绘制（动作提示贴鱼居中；其他提示在钓鱼条左侧；
        /// 起点固定、5 秒内上移 30px 线性淡出；多条可同时显示，纯显示不改变任何状态）。</summary>
        [HarmonyPatch(nameof(BobberBar.draw))]
        [HarmonyPostfix]
        public static void Draw_Postfix(BobberBar __instance, SpriteBatch b)
        {
            try
            {
                if (!_instanceData.TryGetValue(__instance, out var data))
                    return;

                // BATCH-056: 原生挑战星接管——剩余星照常实心，掉落的星用原生空星贴图盖在原生位置上（不新增贴图）。
                // BATCH-058Q: 本 Postfix 在 UI render target（uiViewport 系）绘制，原生几何是世界系 →
                // 位置与缩放乘 k 换算，否则 uiScale>zoomLevel 时覆盖位置/尺寸错乱。
                if (data.HasChallengeBait && data.AdjustedDifficulty > 100f)
                {
                    float starK = Game1.viewport.Width > 0 ? (float)Game1.uiViewport.Width / Game1.viewport.Width : 1f;
                    if (starK <= 0f || float.IsNaN(starK) || float.IsInfinity(starK))
                        starK = 1f;
                    int stars = GetChallengeStars(data.DifficultyLevel, data.BattleElapsedSeconds);
                    if (stars < 3)
                    {
                        int num2 = (__instance.xPositionOnScreen > Game1.viewport.Width * 0.75f)
                            ? (__instance.xPositionOnScreen - 80)
                            : (__instance.xPositionOnScreen + 216);
                        int num3 = __instance.bobbers.Contains("(O)SonarBobber")
                            ? (__instance.yPositionOnScreen + 136)
                            : (__instance.yPositionOnScreen + 40);
                        Rectangle emptyStar = new Rectangle(217, 205, 19, 19);
                        for (int i = stars; i < 3; i++)
                        {
                            b.Draw(
                                Game1.mouseCursors_1_6,
                                new Vector2(num2 - 12, num3 + i * 40) * starK + __instance.everythingShake * starK,
                                emptyStar,
                                Color.White,
                                0f,
                                Vector2.Zero,
                                2f * starK,
                                SpriteEffects.None,
                                0.89f);
                        }
                    }
                }

                foreach (FloatingTip tip in data.ActionTips)
                    DrawTip(b, tip);

                foreach (FloatingTip tip in data.OtherTips)
                    DrawTip(b, tip);
            }
            catch (Exception ex)
            {
                FishingLog.LogRateLimited("BobberBar.Draw_Postfix", $"BobberBar 小游戏文案绘制失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>BATCH-058T: 前摇旋转角曲线（纯函数，供 fish_selftest 只读核验）：0.77s 线性转到 ±70°，
        /// 0.11s 快速转回 0°；上跳逆时针（负）、下跳顺时针（正）。</summary>
        public static float GetJumpWindupRotationAt(float elapsed, bool jumpUp)
        {
            float angle = elapsed <= 0.77f
                ? 70f * (elapsed / 0.77f)
                : 70f * (1f - (elapsed - 0.77f) / 0.11f);
            return (jumpUp ? -1f : 1f) * angle;
        }

        /// <summary>BATCH-058/058T: 前摇旋转角（度）：0.77s 转到 ±70°，0.11s 快速转回 0°；上跳逆时针（负）、下跳顺时针（正）。</summary>
        private static float GetJumpWindupRotation(InstanceData data, bool jumpUp)
        {
            float elapsed = 0.88f - Math.Max(0f, data.JumpWindupSeconds);
            return GetJumpWindupRotationAt(elapsed, jumpUp);
        }

        /// <summary>BATCH-058: 原生鱼图标旋转角（弧度）：停战=一秒一次摇头摆尾；前摇=0.88s 旋转；其余 0。</summary>
        public static float GetFishIconRotation(BobberBar instance)
        {
            if (!_instanceData.TryGetValue(instance, out var data))
                return 0f;
            if (data.IsIdle)
            {
                double swayMs = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                return (float)(Math.Sin(swayMs / 1000.0 * Math.PI * 2.0) * 0.09);
            }
            if (data.JumpWindupActive)
            {
                bool jumpUp = data.JumpPendingTarget <= 133f;
                return MathHelper.ToRadians(GetJumpWindupRotation(data, jumpUp));
            }
            return 0f;
        }

        /// <summary>BATCH-058: 原生鱼图标颜色（0.88s 前摇期间发红光并呼吸脉动；其余保持白色）。</summary>
        public static Color GetFishIconColor(BobberBar instance)
        {
            if (_instanceData.TryGetValue(instance, out var data) && data.JumpWindupActive)
            {
                double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                float pulse = (float)(0.5 + 0.5 * Math.Sin(ms / 180.0 * Math.PI * 2.0));
                return Color.Lerp(Color.Red, Color.White, 0.2f + 0.3f * pulse);
            }
            return Color.White;
        }

        /// <summary>BATCH-059A: 力竭节点打断录播——短语状态机复位为 Free 并清空全部短语标志
        /// （与 1/3 切换块同款清理；不重置 NextPhraseThreshold，蓄力换段进度只前进）。</summary>
        private static void InterruptPhrase(InstanceData data)
        {
            data.PhraseSwitchPending = false;
            data.PhraseMode = PhraseMode.Free;
            data.PhraseSamples.Clear();
            data.PhraseJumpSeen = false;
            data.PhraseWindupStartTime = -1f;
            data.PhraseJumpText = null;
            data.PlaybackWindupShown = false;
            data.JumpWindupActive = false;
        }

        /// <summary>BATCH-059: 招式短语——前缀部分：回放位置写入、循环/切换、前摇视觉重放。</summary>
        private static void HandlePhrasePrefix(
            BobberBar instance, InstanceData data,
            ref float position, ref float speed, ref float target, ref float floaterSinker, float dt)
        {
            if (data.ResultStarted || data.IsIdle)
                return;

            if (data.EffectiveDifficulty < 150f)
            {
                if (data.PhraseMode != PhraseMode.Free)
                {
                    data.PhraseMode = PhraseMode.Free;
                    data.PhraseSamples.Clear();
                    data.JumpWindupActive = false;
                }
                return;
            }

            if (data.PhraseMode != PhraseMode.Playing)
                return;

            data.PhrasePlayTime += dt;
            if (data.PhrasePlayTime >= data.PhraseDuration)
            {
                if (data.PhraseSwitchPending)
                {
                    // 进度净涨 1/3：结束当前短语，回到 Free 等下一次瞬移后再录新短语（只前进不后退）
                    data.PhraseSwitchPending = false;
                    data.PhraseMode = PhraseMode.Free;
                    data.PhraseSamples.Clear();
                    data.PhraseJumpSeen = false;
                    data.PhraseWindupStartTime = -1f;
                    data.PhraseJumpText = null;
                    data.PlaybackWindupShown = false;
                    data.JumpWindupActive = false;
                    data.NextPhraseThreshold = Math.Min(1f, data.NextPhraseThreshold + 1f / 3f);
                }
                else
                {
                    data.PhrasePlayTime -= data.PhraseDuration;
                    data.PlaybackWindupShown = false;
                    data.JumpWindupActive = false;
                }
            }

            if (data.PhraseMode != PhraseMode.Playing)
                return;

            // 按轨迹写入鱼位置，冻结原生运动
            float playPos = InterpolatePhrase(data, data.PhrasePlayTime);
            position = playPos;
            speed = 0f;
            target = playPos;
            floaterSinker = 0f;

            // 前摇视觉重放（旋转 70°+红光+行动提示）
            if (!data.PlaybackWindupShown && data.PhraseWindupStartTime >= 0f &&
                data.PhrasePlayTime >= data.PhraseWindupStartTime)
            {
                data.PlaybackWindupShown = true;
                data.JumpWindupActive = true;
                data.JumpWindupSeconds = 0.88f;
                data.JumpPendingTarget = data.PhraseJumpIsUp ? 0f : 500f;
                if (!string.IsNullOrEmpty(data.PhraseJumpText))
                {
                    AddTip(data.ActionTips, data.PhraseJumpText,
                        instance.xPositionOnScreen + ActionTipAnchorX,
                        instance.yPositionOnScreen + 36f + playPos - 30f,
                        centered: false, actionTip: true);
                }
            }
            if (data.JumpWindupActive)
            {
                data.JumpWindupSeconds -= dt;
                if (data.JumpWindupSeconds <= 0f)
                    data.JumpWindupActive = false;
            }
        }

        /// <summary>BATCH-059: 招式短语——后置部分：中线到达、轨迹录制、录制结束、1/3 切换标记。</summary>
        private static void HandlePhrasePostfix(InstanceData data, float position, float catchProgress)
        {
            if (data.ResultStarted || data.IsIdle || data.EffectiveDifficulty < 150f)
                return;

            const float middle = 266f;
            const float band = 10f;
            bool inMiddle = position >= middle - band && position <= middle + band;

            if (data.PhraseMode == PhraseMode.WaitingMiddle && inMiddle)
            {
                data.PhraseMode = PhraseMode.Recording;
                data.PhraseRecordTime = 0f;
                data.PhraseSamples.Clear();
                data.PhraseSamples.Add((0f, position));
                data.PhraseJumpSeen = false;
                data.PhraseWindupStartTime = -1f;
                data.PhraseJumpText = null;
            }
            else if (data.PhraseMode == PhraseMode.Recording)
            {
                float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                if (dt <= 0f)
                    dt = 1f / 60f;
                data.PhraseRecordTime += dt;
                data.PhraseSamples.Add((data.PhraseRecordTime, position));

                if (data.PhraseJumpSeen && inMiddle)
                {
                    data.PhraseDuration = data.PhraseRecordTime;
                    data.PhraseMode = PhraseMode.Playing;
                    data.PhrasePlayTime = 0f;
                    data.PlaybackWindupShown = false;
                }
            }

            if (data.PhraseMode == PhraseMode.Playing &&
                data.NextPhraseThreshold < 1f &&
                catchProgress >= data.NextPhraseThreshold)
            {
                data.PhraseSwitchPending = true;
            }
        }

        /// <summary>BATCH-059: 轨迹线性插值（时间→位置）。</summary>
        private static float InterpolatePhrase(InstanceData data, float t)
        {
            var samples = data.PhraseSamples;
            if (samples.Count == 0)
                return 0f;
            if (t <= samples[0].Time)
                return samples[0].Position;

            for (int i = 0; i < samples.Count - 1; i++)
            {
                float t0 = samples[i].Time;
                float t1 = samples[i + 1].Time;
                if (t >= t0 && t <= t1)
                {
                    float frac = t1 > t0 ? (t - t0) / (t1 - t0) : 0f;
                    return samples[i].Position + (samples[i + 1].Position - samples[i].Position) * frac;
                }
            }
            return samples[samples.Count - 1].Position;
        }

        /// <summary>BATCH-058: 停战休息提示（10 套随机，i18n hud.idle.1~10）。</summary>
        private static string PickIdleText()
        {
            try
            {
                int index = Game1.random.Next(1, 11);
                string key = $"hud.idle.{index}";
                string text = ModEntry.ModHelper.Translation.Get(key);
                return string.IsNullOrWhiteSpace(text) || text == key ? "它停下来歇口气" : text;
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[BobberBar] 停战文案生成失败: {ex.Message}", LogLevel.Error);
                return "它停下来歇口气";
            }
        }
    }
}
