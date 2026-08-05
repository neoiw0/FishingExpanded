using System;

namespace FishingExpanded.Utils
{
    /// <summary>难度等级效果计算器</summary>
    public static class DifficultyCalculator
    {
        /// <summary>计算 difficulty 倍数</summary>
        /// <param name="level">难度等级 [-10, 100]</param>
        /// <returns>difficulty 倍数</returns>
        public static float GetDifficultyMultiplier(int level)
        {
            if (level < 0)
            {
                // 负数区间：[-10, 0] -> [0.5, 1.0]
                return Lerp(0.5f, 1.0f, (level + 10) / 10f);
            }
            else
            {
                // 正数区间：[0, 100] -> [1.0, 50.0]
                return Lerp(1.0f, 50.0f, level / 100f);
            }
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

        /// <summary>计算视觉缩放倍数（立方根）</summary>
        /// <param name="quantityMultiplier">数量倍数</param>
        /// <returns>绘制缩放倍数</returns>
        public static float GetVisualScale(int quantityMultiplier)
        {
            return (float)Math.Pow(quantityMultiplier, 1.0 / 3.0);
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
            return "rank.divineking"; // 神王 89-100（同称号）
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
            if (currentLevel <= 88) return 100;// 神王(67-88) → 最高上限
            return 100; // 已经是最高等级(89-100仍是神王)
        }
    }
}
