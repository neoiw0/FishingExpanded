using System;
using StardewValley;

namespace FishingExpanded.Utils
{
    /// <summary>难度等级效果计算器</summary>
    public static class DifficultyCalculator
    {
        /// <summary>计算 difficulty 倍数（BATCH-037：正数区间改为分段线性，越往后每级增长越多）</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>difficulty 倍数</returns>
        public static float GetDifficultyMultiplier(int level)
        {
            if (level < 0)
            {
                // 负数区间：[-10, 0] -> [0.5, 1.0]
                return Lerp(0.5f, 1.0f, (level + 10) / 10f);
            }
            // 正数区间（2026-08-10 用户确认锚点：4级×1.2、20级×5、50级×20、100级×50；0级×1.0 不变）。
            // 分段线性斜率逐段递增（0~4: 0.05/级、4~20: 0.2375/级、20~50: 0.5/级、50~100: 0.6/级），即“越往后加得更多”；
            // 0 级与 100 级数值与原线性公式一致（1.0 / 50.0），Lerp 自带 [0,1] 钳制。
            if (level >= 50)
                return Lerp(20f, 50f, (level - 50) / 50f);
            if (level >= 20)
                return Lerp(5f, 20f, (level - 20) / 30f);
            if (level >= 4)
                return Lerp(1.2f, 5f, (level - 4) / 16f);
            return Lerp(1f, 1.2f, level / 4f);
        }

        /// <summary>BATCH-077: 数量倍数锚点阵列（2026-08-22 用户逐点确认，覆盖未部署的 BATCH-076 阵列）——
        /// 难度等级 → 条数期望锚点；低于首锚点/高于末锚点钳制到端点；
        /// 锚点之间按线性期望概率过渡（见 GetQuantityMultiplier）。</summary>
        public static readonly (int Level, int Multiplier)[] QuantityAnchors =
        {
            (-10, 1), (0, 1), (8, 2), (16, 4), (32, 8), (56, 24), (100, 100)
        };

        /// <summary>BATCH-078: 无小游戏物品（非鱼类：垃圾/藻类等钓竿直取物与蟹笼收获，IsNonFishItem 口径）
        /// 专属数量锚点阵列：0级=1个、8级=8个（非鱼类上限即 8 级）；低于首锚点钳到首值；
        /// 区间内与主曲线同方式"线性期望+余量概率进位"。样例：4级期望 4.5→45% 概率 5 个。</summary>
        public static readonly (int Level, int Multiplier)[] NoMinigameQuantityAnchors =
        {
            (0, 1), (8, 8)
        };

        /// <summary>BATCH-077: 数量倍数精确期望值（确定性，供自测与展示）：
        /// 锚点间线性插值，区间外钳制到最近端点；锚点处恒等于锚点值。
        /// 样例：4级=1.5、12级=3.0、20级=5.0、50级=20.0、78级=62.0、90级≈82.73。</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        public static double GetQuantityMultiplierExact(int level)
        {
            return InterpolateAnchors(QuantityAnchors, level);
        }

        /// <summary>BATCH-077: 计算获得数量倍数——按精确期望值概率过渡取整：
        /// 仅对不足 1 条、会被约掉的余量部分按概率进位（小数部分 f = 进位概率，floor 档概率 = 1−f），
        /// 每次结算掷一次（Game1.random）；锚点与整数期望等级恒定不随机；
        /// 绝不在区间两端之间任意随机取值（用户澄清语义，2026-08-22）。
        /// 用户示例：0级=1条、8级=2条之间，4级期望 1.5 → 约 50% 概率 2 条；
        /// 2级期望 1.25 → 25% 概率 2 条；78级期望恰 62 → 恒为 62 条。</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>本次结算的数量倍数（最低 1）</returns>
        public static int GetQuantityMultiplier(int level)
        {
            return RollByFraction(GetQuantityMultiplierExact(level));
        }

        /// <summary>BATCH-078: 无小游戏物品数量倍数精确期望值（确定性）：0级=1个、8级=8个线性；
        /// 负级钳制到首锚点（=1 个）。供自测与展示。</summary>
        public static double GetNoMinigameQuantityMultiplierExact(int level)
        {
            return InterpolateAnchors(NoMinigameQuantityAnchors, level);
        }

        /// <summary>BATCH-078: 无小游戏物品数量倍数——与主曲线同方式：仅对不足 1 个的余量
        /// 按概率进位（每次收获掷一次）；锚点与整数期望恒定。</summary>
        public static int GetNoMinigameQuantityMultiplier(int level)
        {
            return RollByFraction(GetNoMinigameQuantityMultiplierExact(level));
        }

        /// <summary>BATCH-077/078: 锚点阵列线性插值（确定性；区间外钳制到最近端点）。</summary>
        private static double InterpolateAnchors((int Level, int Multiplier)[] anchors, int level)
        {
            if (level <= anchors[0].Level)
                return anchors[0].Multiplier;
            for (int i = 1; i < anchors.Length; i++)
            {
                if (level > anchors[i].Level)
                    continue;
                double t = (double)(level - anchors[i - 1].Level) / (anchors[i].Level - anchors[i - 1].Level);
                return anchors[i - 1].Multiplier +
                    (anchors[i].Multiplier - anchors[i - 1].Multiplier) * t;
            }
            return anchors[anchors.Length - 1].Multiplier;
        }

        /// <summary>BATCH-077/078: 按精确期望值概率取整——小数部分 f = 进位概率（floor 档概率 = 1−f），
        /// 每次结算/收获掷一次（Game1.random）；整数期望恒定不随机；最低 1。</summary>
        private static int RollByFraction(double exact)
        {
            double floorValue = Math.Floor(exact);
            int baseValue = (int)Math.Max(1, floorValue);
            double fraction = exact - floorValue;
            if (fraction <= 0d)
                return baseValue;
            return Game1.random.NextDouble() < fraction ? baseValue + 1 : baseValue;
        }

        /// <summary>BATCH-076: 按实际锚点阵列生成设置文案片段（zh 示例 "-10级=1条，0级=1条，…"）。
        /// itemFormat 含 {0}=等级、{1}=倍数；separator 为条目分隔符；供 GMCM 提示动态拼装，
        /// 保证"按实际锚点为准"——修改 QuantityAnchors 即同步改变设置显示。</summary>
        public static string FormatQuantityAnchors(string itemFormat, string separator)
        {
            return FormatQuantityAnchors(QuantityAnchors, itemFormat, separator);
        }

        /// <summary>BATCH-078: 按指定锚点阵列生成设置文案片段（供无小游戏曲线等复用）。</summary>
        public static string FormatQuantityAnchors((int Level, int Multiplier)[] anchors, string itemFormat, string separator)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < anchors.Length; i++)
            {
                if (i > 0)
                    builder.Append(separator);
                builder.Append(string.Format(itemFormat, anchors[i].Level, anchors[i].Multiplier));
            }
            return builder.ToString();
        }

        /// <summary>BATCH-067: 固定钓鱼等级系数——难度等级增长与玩家的固定（基础）钓鱼等级挂钩：
        /// 系数 = max(等级,1)/10（1 级 ×0.1、10 级 ×1.0=现有速度；0 级按 0.1 钳制）。
        /// 调用方必须传 `Farmer.fishingLevel.Value`（基础字段，不含食物/饮料 buff 与助战临时等级，
        /// 见 `_analysis\StardewValley.Farmer.decompiled.cs:592-593/1612`：FishingLevel 属性 = 字段 + buffs.FishingLevel）。</summary>
        public static float GetFishingLevelGainFactor(int baseFishingLevel)
        {
            return Math.Max(1, baseFishingLevel) / 10f;
        }

        /// <summary>BATCH-067: 成功钓起的请求等级增益 = max(1, round((脱杆基数 + round(调整后难度/50)) × 系数))。
        /// 保留 BATCH-029 雪球（round(调整后难度/50)）并整体乘固定钓鱼等级系数；保底 +1（低等级/多脱杆时
        /// round 归 0 由 max(1,…) 兜底）。称号区间封顶、88→89 入口与 89+ 每次 +6 仍由 DifficultyManager.RecordSuccess 统一执行。</summary>
        public static int GetRequestedLevelGain(int baseLevelGain, float adjustedDifficulty, int baseFishingLevel)
        {
            int snowball = (int)Math.Round(adjustedDifficulty / 50f);
            float factor = GetFishingLevelGainFactor(baseFishingLevel);
            return Math.Max(1, (int)Math.Round((baseLevelGain + snowball) * factor));
        }

        /// <summary>BATCH-067: 挑战鱼饵掉星折扣（回归 GAME-DESIGN §7.5"每掉 1 颗最终鱼获 −20%"）——
        /// 对乘完等级倍数后的最终数量打折并兜底 ≥1。折扣乘数：0.8/0.6/0.4（2/1/0 星）。
        /// 原生挑战鱼饵必给 3 条（BobberBar.decompiled.cs:240-242/354-357 challengeBaitFishes=3），
        /// round(3×0.4)=1 恒非零；本函数兜底保证任何路径不归零。</summary>
        public static long ApplyChallengeStarMultiplier(long finalStack, float starMultiplier)
        {
            if (starMultiplier >= 1f)
                return finalStack;
            return Math.Max(1, (long)Math.Round(finalStack * starMultiplier));
        }

        /// <summary>BATCH-068: 经验公式难度输入上限（用户 2026-08-16 确认=120：比原生最高难度 110 大一点；
        /// 原生最高=扩展传奇 Legend II 900 难度 110，`_analysis\Fish-data-extracted.txt:71`）。
        /// 经验基数难度部分 = clamp(实际传入难度, 原生难度, 本上限)——低于原生抬到原生（负数/力竭补偿），
        /// 高于上限压到上限（正数高等级不再随调整后难度爆炸）。</summary>
        public const float ExperienceDifficultyCap = 120f;

        /// <summary>BATCH-068: 经验公式难度输入 = clamp(传入难度, 原生难度, 120 上限)。</summary>
        public static float GetExperienceDifficulty(float passedDifficulty, float nativeDifficulty)
        {
            if (nativeDifficulty > 0f)
                return Math.Max(nativeDifficulty, Math.Min(passedDifficulty, ExperienceDifficultyCap));
            return Math.Min(passedDifficulty, ExperienceDifficultyCap);
        }

        /// <summary>BATCH-068: 复刻原生经验公式（`_analysis\StardewValley.FishingRod.decompiled.cs:1188-1203`）：
        /// 经验 = max(1, (品质+1)×3 + (int)难度/3)，宝箱 +120%（基于当前值）、完美 +140%（基于含宝箱的值）、Boss ×5。
        /// 用于经验基数重算：负数难度等级/力竭衰减使传入难度低于原生难度时抬到原生水平，
        /// 正数超过 120 上限时压到上限（调用方先用 GetExperienceDifficulty 得到钳制后难度）。</summary>
        public static int GetNativeExperience(int fishQuality, float experienceDifficulty, bool treasureCaught, bool wasPerfect, bool isBossFish)
        {
            int baseXP = Math.Max(1, (fishQuality + 1) * 3 + (int)experienceDifficulty / 3);
            if (treasureCaught)
                baseXP += (int)((float)baseXP * 1.2f);
            if (wasPerfect)
                baseXP += (int)((float)baseXP * 1.4f);
            if (isBossFish)
                baseXP *= 5;
            return baseXP;
        }

        /// <summary>计算经验倍数（BATCH-068：10 级保持 ×5、100 级改为 ×20，10~100 线性平滑，
        /// 用户 2026-08-16 确认"10难度能力还是乘以5，之后平滑处理，100难度等级乘以20"；
        /// 1~10 级保持 BATCH-060 公式 max(1, round(level×0.5))，BATCH-062 与数量倍数解耦）</summary>
        /// <param name="level">难度等级 [0, 100]</param>
        /// <returns>经验倍数（负数等级返回1）</returns>
        public static int GetExperienceMultiplier(int level)
        {
            if (level <= 0) return 1;
            if (level <= 10)
                return (int)Math.Max(1, Math.Round(level * 0.5, MidpointRounding.AwayFromZero));
            if (level >= 100)
                return 20;
            // 10→×5、100→×20 线性：5 + (level-10)×(15/90)
            return (int)Math.Round(5f + (level - 10) * (15f / 90f), MidpointRounding.AwayFromZero);
        }

        /// <summary>BATCH-060: 品质门槛（提升到式，非累加）：10 级→银、25 级→金、50 级→铱；10 级以下不提升。</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>门槛品质：0=普通, 1=银, 2=金, 4=铱</returns>
        public static int GetQualityTier(int level)
        {
            if (level >= 50) return 4; // 铱
            if (level >= 25) return 2; // 金
            if (level >= 10) return 1; // 银
            return 0;
        }

        /// <summary>应用品质门槛（BATCH-060：最终品质 = max(原品质, 等级门槛)；合法品质映射 0/1/2/4）。</summary>
        /// <param name="baseQuality">原始品质（0/1/2/4）</param>
        /// <param name="level">难度等级</param>
        /// <returns>最终品质</returns>
        public static int ApplyQualityBonus(int baseQuality, int level)
        {
            return Math.Max(baseQuality, GetQualityTier(level));
        }

        /// <summary>计算尺寸数字倍率（BATCH-029：正等级每级 +10%，负等级每级 -5%，与数量倍数解耦）</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>尺寸数字倍率（100级=11，-10级=0.5，防御钳制 ≥0.1）</returns>
        public static float GetFishSizeMultiplier(int level)
        {
            if (level > 0)
                return 1f + 0.10f * level;
            if (level < 0)
                return Math.Max(0.1f, 1f - 0.05f * Math.Abs(level));
            return 1f;
        }

        /// <summary>计算视觉缩放倍数（BATCH-060：与数量倍数解耦，改按难度等级线性；0 级=1.0、100 级≈3.7084）</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>绘制缩放倍数（0 级及以下=1）</returns>
        public static float GetVisualScale(int level)
        {
            if (level <= 0) return 1f;

            // 保持 BATCH-026 定稿端点：100 级 = 原 25 级大小 51^(1/3) ≈ 3.70843；每级 ≈ +0.0270843
            return Math.Min(3.70843f, 1f + level * 0.0270843f);
        }


        /// <summary>BATCH-035: 助战触发总概率 = 10% × (可计数皇冠数/目标总数)，线性；满皇冠=10%、无皇冠=0%。</summary>
        public static double GetAssistChance(int countableCrownCount, int crownTarget)
        {
            if (countableCrownCount <= 0 || crownTarget <= 0)
                return 0.0;
            return 0.10 * Math.Min(countableCrownCount, crownTarget) / crownTarget;
        }

        /// <summary>BATCH-073: 助战触发总概率 = 基础概率 + 每条可计数流动金冠鱼 +0.1% + 每条可计数增大流动金冠鱼再 +0.05%。
        /// 不设上限，由可计数 61 条皇冠上限自然限制。</summary>
        public static double GetAssistChance(int countableCrownCount, int crownTarget, int flowCrownCount, int enlargedFlowCrownCount)
        {
            double baseChance = GetAssistChance(countableCrownCount, crownTarget);
            return baseChance + 0.001 * flowCrownCount + 0.0005 * enlargedFlowCrownCount;
        }

        /// <summary>BATCH-035: 助战临时钓鱼等级 = 0~40，权重随被选中鱼的难度排位 r∈[0,1] 线性倾斜：
        /// w(L) = 1 + s·(L-20)/20，s = -29/31 + 58r/31（2026-08-09 用户澄清：等级随机但随鱼难度排位；
        /// 示例最高鱼 40 级 3% vs 0 级 0.1% → 30 倍比值，采用线性倾斜实现该比值）。
        /// 最高难度鱼（r=1）：P(40)≈4.72%、P(0)≈0.16%，平均 26.55；最低难度鱼（r=0）镜像：P(0)≈4.72%、P(40)≈0.16%，平均 13.45；
        /// 40 级概率最高鱼 = 最低鱼 30 倍；最低+最高两条鱼的平均助战等级恰为 20；中位鱼（r=0.5）= 均匀分布。</summary>
        public static int GetRandomAssistLevel(double rank)
        {
            double clampedRank = Math.Max(0.0, Math.Min(1.0, rank));
            double s = -29.0 / 31.0 + 58.0 / 31.0 * clampedRank;

            // 权重和恒为 41（Σ(L-20)=0），全部权重为正（s∈[-29/31, 29/31] → w ≥ 2/31）
            double total = 0.0;
            for (int level = 0; level <= 40; level++)
                total += 1.0 + s * (level - 20) / 20.0;

            double roll = Game1.random.NextDouble() * total;
            double cumulative = 0.0;
            for (int level = 0; level <= 40; level++)
            {
                cumulative += 1.0 + s * (level - 20) / 20.0;
                if (roll < cumulative)
                    return level;
            }
            return 40;
        }
        /// <summary>计算蓄力槽减速倍率（全局保护 + 负数难度加成，取最小值）</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <param name="catchProgress">当前蓄力槽进度 [0, 1]</param>
        /// <returns>减速倍率（1.0=正常速度）</returns>
        public static float GetCatchPenaltyModifier(int level, float catchProgress)
        {
            // 全局保护：任意难度等级都生效
            float globalModifier = 1.0f;
            if (catchProgress <= 0.01f)
            {
                // 1%处：50%速度（减少到原本的50%）
                globalModifier = 0.50f;
            }
            else if (catchProgress <= 0.20f)
            {
                // 20%处：80%速度（减少到原本的80%）
                globalModifier = 0.80f;
            }

            // 负数难度特有保护（原有逻辑）
            float negativeModifier = 1.0f;
            if (level < 0)
            {
                if (catchProgress <= 0.01f)
                {
                    // 1%处：[-10, 0] -> [0.20, 1.0]
                    negativeModifier = Lerp(0.20f, 1.0f, (level + 10) / 10f);
                }
                else if (catchProgress <= 0.20f)
                {
                    // 20%处：[-10, 0] -> [0.60, 1.0]
                    negativeModifier = Lerp(0.60f, 1.0f, (level + 10) / 10f);
                }
                else if (catchProgress <= 0.40f)
                {
                    // 40%处：[-10, 0] -> [0.99, 1.0]
                    negativeModifier = Lerp(0.99f, 1.0f, (level + 10) / 10f);
                }
            }

            // 取效果大的（最小倍率）
            return Math.Min(globalModifier, negativeModifier);
        }

        /// <summary>BATCH-051: 非挑战鱼饵、调整后难度>100 的连续失败逃跑减速倍率。
        /// 从“当前等级原倍率”向“-10 级等效倍率”按连续失败次数线性插值（0 次=原倍率，5 次及以上=-10 级等效）；
        /// 因 -10 级等效倍率在蓄力进度>40% 时≈1.0，本加成天然只在“鱼快逃跑”（条低）时起作用。</summary>
        /// <param name="level">当前难度等级</param>
        /// <param name="consecutiveFails">该鱼连续失败次数</param>
        /// <param name="catchProgress">蓄力槽进度 [0,1]</param>
        public static float GetEscapeFailBonusModifier(int level, int consecutiveFails, float catchProgress)
        {
            float baseModifier = GetCatchPenaltyModifier(level, catchProgress);
            float targetModifier = GetCatchPenaltyModifier(-10, catchProgress);
            float t = Math.Clamp(consecutiveFails / 5f, 0f, 1f);
            return Lerp(baseModifier, targetModifier, t);
        }

        /// <summary>BATCH-038: 力竭机制节点（分钟 → 调整百分比）。15 分钟=100%（难度降到 80）；节点间线性。</summary>
        public static readonly (float Minute, float Percent)[] ExhaustionNodes =
        {
            (1f, 0.01f), (3f, 0.03f), (5f, 0.10f), (7f, 0.20f), (9f, 0.35f), (12f, 0.50f), (15f, 1.00f)
        };

        /// <summary>BATCH-038: 按战斗耗时（秒）返回力竭调整百分比（0=原难度，1=难度降到 80；节点间分段线性）。</summary>
        public static float GetExhaustionPercent(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f)
                return 0f;
            float minutes = elapsedSeconds / 60f;
            for (int i = 0; i < ExhaustionNodes.Length; i++)
            {
                if (minutes <= ExhaustionNodes[i].Minute)
                {
                    if (i == 0)
                        return Lerp(0f, ExhaustionNodes[i].Percent, minutes / ExhaustionNodes[i].Minute);
                    return Lerp(ExhaustionNodes[i - 1].Percent, ExhaustionNodes[i].Percent,
                        (minutes - ExhaustionNodes[i - 1].Minute) / (ExhaustionNodes[i].Minute - ExhaustionNodes[i - 1].Minute));
                }
            }
            return 1f;
        }

        /// <summary>BATCH-038: 力竭后的有效难度 = 原调整后难度 + (80 - 原调整后难度) × 百分比；15 分钟后保持 80。</summary>
        public static float GetExhaustedDifficulty(float originalAdjustedDifficulty, float elapsedSeconds)
        {
            return originalAdjustedDifficulty + (80f - originalAdjustedDifficulty) * GetExhaustionPercent(elapsedSeconds);
        }

        /// <summary>线性插值</summary>
        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Max(0f, Math.Min(1f, t));
        }

        /// <summary>获取称号名称。内部键名沿用历史命名：rank.lord/rank.duke/rank.divineking/rank.creator
        /// 现分别显示男爵/公爵/众神王/祖龙王（2026-08 用户定稿改名），玩家可见文本以 i18n 为准。</summary>
        /// <param name="level">难度等级</param>
        /// <returns>I18n 键名</returns>
        public static string GetRankKey(int level)
        {
            if (level < 1) return "rank.weak";
            if (level <= 3) return "rank.elite";     // 精英 1-3
            if (level <= 6) return "rank.knight";    // 骑士 4-6
            if (level <= 8) return "rank.lord";      // 男爵 7-8
            if (level <= 15) return "rank.count";    // 伯爵 9-15
            if (level <= 22) return "rank.duke";     // 公爵 16-22
            if (level <= 33) return "rank.prince";   // 亲王 23-33
            if (level <= 45) return "rank.emperor";  // 帝王 34-45
            if (level <= 66) return "rank.godking";  // 神皇 46-66
            if (level <= 88) return "rank.divineking"; // 众神王 67-88
            if (level <= 99) return "rank.creator";    // 祖龙王 89-99（BATCH-028）
            return "rank.taiyi";                       // 太一 100（BATCH-038：原“混沌”改名，用户确认）
        }

        /// <summary>BATCH-023: 获取下一个称号区间的上限（用于越级限制）</summary>
        /// <param name="currentLevel">当前难度等级</param>
        /// <returns>下一个称号区间的上限</returns>
        public static int GetNextRankCeiling(int currentLevel)
        {
            if (currentLevel < 1) return 3;   // 弱小 → 精英上限
            if (currentLevel <= 3) return 6;  // 精英 → 骑士上限
            if (currentLevel <= 6) return 8;  // 骑士 → 男爵上限
            if (currentLevel <= 8) return 15; // 男爵 → 伯爵上限
            if (currentLevel <= 15) return 22;// 伯爵 → 公爵上限
            if (currentLevel <= 22) return 33;// 公爵 → 亲王上限
            if (currentLevel <= 33) return 45;// 亲王 → 帝王上限
            if (currentLevel <= 45) return 66;// 帝王 → 神皇上限
            if (currentLevel <= 66) return 88;// 神皇 → 众神王上限
            if (currentLevel <= 88) return 99;// 众神王(67-88) → 祖龙王上限（BATCH-028）
            if (currentLevel <= 99) return 100;// 祖龙王(89-99) → 太一上限（BATCH-028）
            return 100; // 太一(100) → 太一上限（BATCH-028）
        }

        /// <summary>BATCH-032: 星之果茶掉落判定。难度等级 ≥50 时概率 = 等级/4%（即 等级/400）；鱼王豁免由调用方保证。</summary>
        public static bool TryGetStarfruitTeaDrop(int difficultyLevel)
        {
            if (difficultyLevel < 50)
                return false;
            double chance = difficultyLevel / 400.0;
            return Game1.random.NextDouble() < chance;
        }
    }
}



