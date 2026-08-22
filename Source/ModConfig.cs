namespace FishingExpanded
{
    /// <summary>玩家可编辑配置（SMAPI 自动读写 config.json；GMCM 提供菜单开关）。</summary>
    public class ModConfig
    {
        /// <summary>主日志开关（2026-08-22 起默认 false）：false 时本 Mod 不输出调试/信息日志，
        /// 但 Warn/Error/Alert 始终可见（玩家报障的最小证据集，见 FishingLog 分级门控）。</summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>鱼的行为全随机模式（BATCH-058S，2026-08-14 用户改名+默认关）：false（默认）=背板模式，
        /// 挑战鱼饵同鱼同等级在钓起前行为固定（可背板）；true=全随机模式（更难），每局行为完全随机，
        /// 不生成/不使用背板种子。</summary>
        public bool EnableRandomFishBehavior { get; set; } = false;

        /// <summary>节日钓鱼开关（BATCH-066，2026-08-16 用户确认）：false（默认）=三个钓鱼节日
        /// （鱿鱼节/鳟鱼大赛/冰雪节）钓鱼完全原生——无难度等级、无数量倍数、无经验倍数、无模组结算与提示；
        /// true=模组规则照常生效，且节日分数按数量倍数翻倍（鱿鱼节分数=原生数量×倍数，
        /// 冰雪节每次成功 +数量倍数 分）。</summary>
        public bool EnableFestivalFishingMods { get; set; } = false;

        /// <summary>自定义钓鱼称号（BATCH-073，2026-08-18 用户确认）：空字符串=继续使用 i18n 称号；
        /// 非空时替代所有玩家可见的鱼职阶称号显示。该字段只允许手动编辑 config.json，不注册到 GMCM。</summary>
        public string CustomFishingTitle { get; set; } = string.Empty;
    }
}
