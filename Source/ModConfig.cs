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

        /// <summary>节日钓鱼开关（BATCH-066，2026-08-16 用户确认）：false（默认）=三个钓鱼节日
        /// （鱿鱼节/鳟鱼大赛/冰雪节）钓鱼完全原生——无难度等级、无数量倍数、无经验倍数、无模组结算与提示；
        /// true=模组规则照常生效，且节日分数按数量倍数翻倍（鱿鱼节分数=原生数量×倍数，
        /// 冰雪节每次成功 +数量倍数 分）。</summary>
        public bool EnableFestivalFishingMods { get; set; } = false;
    }
}
