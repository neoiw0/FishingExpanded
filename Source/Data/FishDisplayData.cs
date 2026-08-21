using System.Collections.Generic;

namespace FishingExpanded.Data
{
    /// <summary>单个玩家的超大鱼展示数据</summary>
    public class FishDisplayData
    {
        /// <summary>当前举起的超大鱼信息（鱼ID -> (难度等级, fishSize)；BATCH-060 巨型鱼与数量倍数解耦、改按难度等级）</summary>
        public Dictionary<string, (int level, int fishSize)> ActiveGiantFish { get; set; } = new Dictionary<string, (int, int)>();

        /// <summary>每个NPC今天已触发冒泡的鱼种类</summary>
        public Dictionary<string, HashSet<string>> NPCBubbleTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>每个NPC今天已触发对话的鱼种类</summary>
        public Dictionary<string, HashSet<string>> NPCDialogueTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>BATCH-074：整段超大鱼会话重置——展示事实、NPC 冒泡与对话触发记录全部归零。
        /// 进入 FarmHouse 或换日时调用，等价于“只消除进入该会话前的超大鱼效果”。
        /// 替代旧 `ResetDailyTriggers`（只清赞美）与 `ClearActiveGiantFish`（只清展示）两条路径。</summary>
        public void ResetSessionEffects()
        {
            ActiveGiantFish.Clear();
            NPCBubbleTriggered.Clear();
            NPCDialogueTriggered.Clear();
        }
    }
}
