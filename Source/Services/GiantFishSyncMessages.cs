namespace FishingExpanded.Services
{
    /// <summary>BATCH-074：联机同步超大鱼展示事实的消息载荷。
    /// 只传输“其他玩家屏幕也应看到”的共享事实；HUD 等私人提示不广播。</summary>
    public class GiantFishRecordMessage
    {
        public long PlayerId { get; set; }
        public string FishId { get; set; }
        public int Level { get; set; }
        public int FishSize { get; set; }
    }

    /// <summary>BATCH-074：联机清空某玩家超大鱼展示事实（进 FarmHouse / 换日）。</summary>
    public class GiantFishClearMessage
    {
        public long PlayerId { get; set; }
    }

    /// <summary>BATCH-074：联机同步 NPC 冒泡文案，保证其他玩家屏幕看到一致的赞美。</summary>
    public class NPCFishBubbleMessage
    {
        public long PlayerId { get; set; }
        public string LocationName { get; set; }
        public string NPCName { get; set; }
        public string FishId { get; set; }
        public int FishSize { get; set; }
        public string Message { get; set; }
    }
}
