namespace FishingExpanded
{
    /// <summary>玩家可编辑配置（SMAPI 自动读写 config.json；GMCM 提供菜单开关）。</summary>
    public class ModConfig
    {
        /// <summary>主日志开关：false 时抑制本 Mod 全部 SMAPI 日志输出（含调试与限频日志）。</summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>鱼的行为全随机模式（BATCH-058S，2026-08-14 用户改名+默认关）：false（默认）=背板模式，
        /// 挑战鱼饵同鱼同等级在钓起前行为固定（可背板）；true=全随机模式（更难），每局行为完全随机，
        /// 不生成/不使用背板种子。</summary>
        public bool EnableRandomFishBehavior { get; set; } = false;
    }
}
