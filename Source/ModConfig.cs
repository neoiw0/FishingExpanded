using System;
using System.Collections.Generic;

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

        /// <summary>BATCH-076: 条数收益缩放百分比。默认 100；有效范围 [10, 300]（经 ClampedQuantityPercent
        /// 钳制，GMCM 限输入范围，手改 config.json 同样兜底）。作用于钓鱼结算最终条数：
        /// 万能鱼饵/挑战鱼饵/难度等级数量倍数/掉星折扣全部完成之后整体缩放（向上取整、每次渔获至少 1 条）；
        /// 蟹笼在难度等级倍数后、原生书《Crabbing》×2 前缩放；节日原生模式不生效。</summary>
        public int QuantityIncomePercent { get; set; } = 100;

        /// <summary>BATCH-078: 无小游戏物品条数收益缩放百分比（非鱼类：垃圾/藻类等与蟹笼收获）。
        /// 默认 100；有效范围 [10, 300]。作用于该类物品的专属数量曲线结果之后（向上取整、至少 1 个）；
        /// 与主曲线的 QuantityIncomePercent 相互独立。</summary>
        public int NoMinigameQuantityIncomePercent { get; set; } = 100;

        /// <summary>BATCH-078: 有效无小游戏物品条数收益百分比（[10, 300] 钳制后）。</summary>
        public int ClampedNoMinigameQuantityPercent => Math.Min(300, Math.Max(10, NoMinigameQuantityIncomePercent));

        /// <summary>BATCH-076: 经验收益缩放百分比。默认 100；有效范围 [10, 300]（经 ClampedExperiencePercent
        /// 钳制）。作用于钓鱼结算经验：经验基数重算与难度等级经验倍数之后整体缩放（向上取整）；
        /// 每日收获限额的超限清零优先；节日原生模式与蟹笼固定经验不生效。</summary>
        public int ExperienceIncomePercent { get; set; } = 100;

        /// <summary>BATCH-076: 有效条数收益百分比（[10, 300] 钳制后）。</summary>
        public int ClampedQuantityPercent => Math.Min(300, Math.Max(10, QuantityIncomePercent));

        /// <summary>BATCH-076: 有效经验收益百分比（[10, 300] 钳制后）。</summary>
        public int ClampedExperiencePercent => Math.Min(300, Math.Max(10, ExperienceIncomePercent));

        /// <summary>自定义钓鱼称号（BATCH-073，2026-08-18 用户确认）：空字符串=继续使用 i18n 称号；
        /// 非空时替代所有玩家可见的鱼职阶称号显示。该字段只允许手动编辑 config.json，不注册到 GMCM。</summary>
        public string CustomFishingTitle { get; set; } = string.Empty;

        /// <summary>按职阶自定义全部 12 个称号（2026-08-22 用户定稿为数组）：12 个元素按弱→强顺序对应
        /// [0]weak(&lt;1) [1]elite(1-3) [2]knight(4-6) [3]lord(7-8,男爵) [4]count(9-15,伯爵) [5]duke(16-22,公爵)
        /// [6]prince(23-33,亲王) [7]emperor(34-45,帝王) [8]godking(46-66,神皇) [9]divineking(67-88,众神王)
        /// [10]creator(89-99,祖龙王) [11]taiyi(100,隐藏头衔)。
        /// 元素为空/纯空白=该职阶继续使用内置称号；数组不足 12 位按缺省处理，超出部分忽略。
        /// 优先级：本数组（逐职阶）&gt; CustomFishingTitle（整体覆盖）&gt; i18n 内置。
        /// 仅手动编辑 config.json，不注册 GMCM。</summary>
        public List<string> CustomFishingTitles { get; set; } =
            new List<string> { "", "", "", "", "", "", "", "", "", "", "", "" };
    }
}
