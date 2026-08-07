using System.Collections.Generic;

namespace FishingExpanded.Data
{
    /// <summary>单个玩家的超大鱼展示数据</summary>
    public class FishDisplayData
    {
        /// <summary>当前举起的超大鱼信息（鱼ID -> (倍数, fishSize)）</summary>
        public Dictionary<string, (int multiplier, int fishSize)> ActiveGiantFish { get; set; } = new Dictionary<string, (int, int)>();

        /// <summary>每个NPC今天已触发冒泡的鱼种类</summary>
        public Dictionary<string, HashSet<string>> NPCBubbleTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>每个NPC今天已触发对话的鱼种类</summary>
        public Dictionary<string, HashSet<string>> NPCDialogueTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>重置每日触发记录（凌晨调用）</summary>
        public void ResetDailyTriggers()
        {
            NPCBubbleTriggered.Clear();
            NPCDialogueTriggered.Clear();
        }

        /// <summary>清空激活的超大鱼（进入FarmHouse时调用）</summary>
        public void ClearActiveGiantFish()
        {
            ActiveGiantFish.Clear();
        }
    }
}
