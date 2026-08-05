using System;
using System.Collections.Generic;

namespace FishingExpanded.Data
{
    /// <summary>当前玩家的鱼难度、收藏星标和隐藏钓鱼等级加成</summary>
    public class FishDifficultyData
    {
        /// <summary>鱼的统计数据字典，键为鱼的QualifiedItemId</summary>
        public Dictionary<string, FishStats> FishStatistics { get; set; } = new Dictionary<string, FishStats>();

        /// <summary>钓鱼等级隐藏加成（每达标一种鱼+0.5）</summary>
        public float FishingLevelBonus { get; set; } = 0f;

        /// <summary>已达到difficulty≥120的鱼种类（用于Collections星标）</summary>
        public HashSet<string> CollectionStars { get; set; } = new HashSet<string>();
    }

    /// <summary>单种鱼的统计数据</summary>
    public class FishStats
    {
        /// <summary>成功钓起次数</summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>失败次数</summary>
        public int FailCount { get; set; } = 0;

        /// <summary>当前难度等级（-10到100，防止溢出）</summary>
        public int DifficultyLevel
        {
            get
            {
                // Bug修复：防止整数溢出并钳制到合法范围
                try
                {
                    long diff = (long)SuccessCount - (long)FailCount;
                    return (int)Math.Max(-10, Math.Min(100, diff));
                }
                catch
                {
                    return 0; // 异常时返回默认值
                }
            }
        }
    }
}
