using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>BATCH-066: 节日钓鱼判定服务（鱿鱼节/鳟鱼大赛/冰雪节）。
    /// 唯一职责=判定当前是否处于钓鱼节日及模组是否应介入；所有 Patch 边界的节日原生门统一走这里，
    /// 不新增第二份判定逻辑。只读（不写任何状态）。</summary>
    internal static class FestivalFishingService
    {
        /// <summary>鱿鱼节（冬 12-13，沙滩）：被动节日。原生判定与 Farmer.caughtFish 内
        /// SquidFestScore 分支一致（Utility.IsPassiveFestivalDay → ActivePassiveFestivals.Contains）。</summary>
        internal static bool IsSquidFest()
        {
            return Utility.IsPassiveFestivalDay("SquidFest");
        }

        /// <summary>鳟鱼大赛（夏 20-21，森林）：被动节日。</summary>
        internal static bool IsTroutDerby()
        {
            return Utility.IsPassiveFestivalDay("TroutDerby");
        }

        /// <summary>冰雪节（冬 8，Cindersap 森林结冰湖钓鱼比赛）：事件型节日。
        /// 走 Event.caughtFish（festivalScore++）而非 Farmer.caughtFish；判定=
        /// Game1.isFestival() 且当前事件 id == "festival_winter8"（Event.isSpecificFestival 内部检查）。</summary>
        internal static bool IsIceFestival()
        {
            if (!Game1.isFestival())
                return false;
            return Game1.currentLocation?.currentEvent?.isSpecificFestival("winter8") == true;
        }

        /// <summary>当前是否处于任一钓鱼节日（鱿鱼节/鳟鱼大赛/冰雪节）。</summary>
        internal static bool IsFestivalFishingActive()
        {
            return IsSquidFest() || IsTroutDerby() || IsIceFestival();
        }

        /// <summary>节日原生模式：处于钓鱼节日且模组开关关闭 → 节日钓鱼完全原生
        /// （不注入难度/品质/手感、不记录 pending、不乘数量、不乘经验、不结算、无模组提示）。
        /// 各 Patch 边界统一调用本门；false 时不改变任何既有行为。</summary>
        internal static bool IsVanillaFestivalMode()
        {
            return IsFestivalFishingActive() && !ModEntry.Config.EnableFestivalFishingMods;
        }
    }
}
