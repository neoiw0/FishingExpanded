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

        /// <summary>计算获得数量倍数</summary>
        /// <param name="level">难度等级 [0, 100]</param>
        /// <returns>数量倍数（负数等级返回1）</returns>
        public static int GetQuantityMultiplier(int level)
        {
            if (level <= 0) return 1;

            // [0, 100] -> [1, 200]
            return (int)Math.Round(Lerp(1f, 200f, level / 100f));
        }

        /// <summary>计算经验倍数</summary>
        /// <param name="level">难度等级 [0, 100]</param>
        /// <returns>经验倍数（负数等级返回1）</returns>
        public static int GetExperienceMultiplier(int level)
        {
            // 与数量倍数相同
            return GetQuantityMultiplier(level);
        }

        /// <summary>计算品质提升级数</summary>
        /// <param name="level">难度等级 [0, 100]</param>
        /// <returns>品质提升级数</returns>
        public static int GetQualityBonus(int level)
        {
            if (level <= 0) return 0;

            // 每5级提升1级品质
            return level / 5;
        }

        /// <summary>应用品质提升（0→1→2→4，最高铱星）</summary>
        /// <param name="baseQuality">原始品质</param>
        /// <param name="level">难度等级</param>
        /// <returns>最终品质</returns>
        public static int ApplyQualityBonus(int baseQuality, int level)
        {
            int bonus = GetQualityBonus(level);
            int finalQuality = baseQuality + bonus;

            // 钳制到合法品质值：0=普通, 1=银, 2=金, 4=铱
            if (finalQuality >= 3)
            {
                return 4; // 铱星
            }
            return Math.Min(finalQuality, 2); // 最高金星（如果不够3）
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

        /// <summary>计算视觉缩放倍数（线性：0级=1.0，100级=原25级大小，BATCH-026）</summary>
        /// <param name="quantityMultiplier">数量倍数</param>
        /// <returns>绘制缩放倍数</returns>
        public static float GetVisualScale(int quantityMultiplier)
        {
            if (quantityMultiplier <= 1) return 1f;

            // 线性缩放：倍数200（等级100）→ 原25级大小 51^(1/3) ≈ 3.70843
            // 斜率 = (3.70843 - 1) / 199 ≈ 0.0136102；换算到每级 ≈ +0.0270843
            return 1f + (quantityMultiplier - 1) * 0.0136102f;
        }


        /// <summary>BATCH-035: 助战触发总概率 = 10% × (可计数皇冠数/目标总数)，线性；满皇冠=10%、无皇冠=0%。</summary>
        public static double GetAssistChance(int countableCrownCount, int crownTarget)
        {
            if (countableCrownCount <= 0 || crownTarget <= 0)
                return 0.0;
            return 0.10 * Math.Min(countableCrownCount, crownTarget) / crownTarget;
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

        /// <summary>获取称号名称</summary>
        /// <param name="level">难度等级</param>
        /// <returns>I18n 键名</returns>
        public static string GetRankKey(int level)
        {
            if (level < 1) return "rank.weak";
            if (level <= 3) return "rank.elite";     // 精英 1-3
            if (level <= 6) return "rank.knight";    // 骑士 4-6
            if (level <= 8) return "rank.lord";      // 领主 7-8
            if (level <= 15) return "rank.count";    // 伯爵 9-15
            if (level <= 22) return "rank.duke";     // 大公 16-22
            if (level <= 33) return "rank.prince";   // 亲王 23-33
            if (level <= 45) return "rank.emperor";  // 帝王 34-45
            if (level <= 66) return "rank.godking";  // 神皇 46-66
            if (level <= 88) return "rank.divineking"; // 神王 67-88
            if (level <= 99) return "rank.creator";    // 创世神 89-99（BATCH-028）
            return "rank.taiyi";                       // 太一 100（BATCH-038：原“混沌”改名，用户确认）
        }

        /// <summary>BATCH-023: 获取下一个称号区间的上限（用于越级限制）</summary>
        /// <param name="currentLevel">当前难度等级</param>
        /// <returns>下一个称号区间的上限</returns>
        public static int GetNextRankCeiling(int currentLevel)
        {
            if (currentLevel < 1) return 3;   // 弱小 → 精英上限
            if (currentLevel <= 3) return 6;  // 精英 → 骑士上限
            if (currentLevel <= 6) return 8;  // 骑士 → 领主上限
            if (currentLevel <= 8) return 15; // 领主 → 伯爵上限
            if (currentLevel <= 15) return 22;// 伯爵 → 大公上限
            if (currentLevel <= 22) return 33;// 大公 → 亲王上限
            if (currentLevel <= 33) return 45;// 亲王 → 帝王上限
            if (currentLevel <= 45) return 66;// 帝王 → 神皇上限
            if (currentLevel <= 66) return 88;// 神皇 → 神王上限
            if (currentLevel <= 88) return 99;// 神王(67-88) → 创世神上限（BATCH-028）
            if (currentLevel <= 99) return 100;// 创世神(89-99) → 太一上限（BATCH-028）
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



