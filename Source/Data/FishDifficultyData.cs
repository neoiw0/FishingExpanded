using System;
using System.Collections.Generic;

namespace FishingExpanded.Data
{
    /// <summary>当前玩家的鱼难度与收藏皇冠（BATCH-034：隐藏钓鱼等级加成已整体删除）</summary>
    public class FishDifficultyData
    {
        /// <summary>鱼的统计数据字典，键为鱼的QualifiedItemId</summary>
        public Dictionary<string, FishStats> FishStatistics { get; set; } = new Dictionary<string, FishStats>();

        /// <summary>已获得收藏皇冠的鱼种类（用于Collections皇冠与手感进度α计数；含原版传奇一次钓获与Mod鱼）</summary>
        public HashSet<string> CollectionStars { get; set; } = new HashSet<string>();

        /// <summary>BATCH-038: 挑战鱼饵成功且难度等级≥95的鱼（图鉴皇冠以流动金色绘制；旧存档缺失默认空）</summary>
        public HashSet<string> ChallengeCrowns { get; set; } = new HashSet<string>();
    }

    /// <summary>单种鱼的统计数据</summary>
    public class FishStats
    {
        /// <summary>成功钓起次数</summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>失败次数</summary>
        public int FailCount { get; set; } = 0;

        /// <summary>连续失败次数（BATCH-029：同一鱼种连续失败计数，成功时清零；旧存档缺失字段默认 0）</summary>
        public int ConsecutiveFailCount { get; set; } = 0;

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
