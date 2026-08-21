using System;
using StardewModdingAPI;

namespace FishingExpanded.Services
{
    /// <summary>与 VanillaTips.IVanillaTipsApi 同形状的鸭子接口（参数全为简单类型，无需引用其程序集）。
    /// SMAPI 的 <c>ModRegistry.GetApi&lt;T&gt;</c> 要求“顶级 public 接口”；嵌套接口即使声明为 public，也只会是
    /// <c>IsNestedPublic</c> 而非 <c>IsPublic</c>，会被 SMAPI 拒绝映射（BATCH-069 修复）。</summary>
    public interface IVanillaTipsApi
    {
        void RegisterTips(string sourceId, float weight, string[] ids, string[] categories, string[] zh, string[] en);
        void RemoveTips(string sourceId, params string[] tipIds);
    }

    /// <summary>
    /// VanillaTips（neoiw.vanillatips）第三方来源注入（BATCH-060 第 9 项，2026-08-15 用户指令）。
    /// 未安装 VanillaTips 时 GetApi 返回 null，静默跳过（无硬依赖）；安装时以来源权重 11 注册 7 条钓鱼机制提示。
    /// 权重语义：来源级 0~50（11 为第三方默认 10 略高的一档）；同一 sourceId 重复注册=整体覆盖；玩家可在 GMCM 调整。
    /// </summary>
    public static class VanillaTipsIntegration
    {
        /// <summary>来源级权重（用户指定：11）。</summary>
        public const float SourceWeight = 11f;

        private static readonly string[] Ids =
        {
            "fe-exhaust", "fe-tea", "fe-giant", "fe-assist", "fe-assist-rank", "fe-alpha", "fe-challenge-bait"
        };

        private static readonly string[] Categories =
        {
            "general", "general", "general", "general", "general", "general", "general"
        };

        private static readonly string[] ZhTexts =
        {
            "【渔】时间站在渔夫这边：再倔的鱼，也熬不过一刻钟。",
            "【渔】五十级以后，运气好时能闻到茶香。",
            "【渔】八级以上的鱼，值得举起来走遍全村——趁还没回家。",
            "【渔】你钓过的老朋友，偶尔会游来帮忙，把绿条悄悄变长。",
            "【渔】最不服输的那条朋友，帮起忙来也最卖力。",
            "【渔】传说集齐六十一顶皇冠的人，手会像水一样顺。",
            "【渔】上了挑战鱼饵，帮手们就不会来了。"
        };

        private static readonly string[] EnTexts =
        {
            "Fisher: Time favors the angler - even the most stubborn fish gives in within a quarter of an hour.",
            "Fisher: Past level 50, luck may bring the scent of tea to your haul.",
            "Fisher: Fish at level 8 or above deserve a parade through town - while you still can, before heading home.",
            "Fisher: Old friends you've caught sometimes swim by to help, quietly lengthening your catch bar.",
            "Fisher: The friend who never yields helps the hardest.",
            "Fisher: They say the one who gathers sixty-one crowns wields the rod as smoothly as water.",
            "Fisher: With challenge bait on, no helpers will come."
        };

        /// <summary>GameLaunched 时调用：尝试注册到 VanillaTips；未安装则静默跳过。</summary>
        public static void TryRegister(IModHelper helper, IMonitor monitor)
        {
            try
            {
                var api = helper.ModRegistry.GetApi<IVanillaTipsApi>("neoiw.vanillatips");
                if (api == null)
                {
                    monitor.Log("[FishingExpanded] VanillaTips 未安装，跳过提示注入。", LogLevel.Debug);
                    return;
                }

                api.RegisterTips(
                    "neoiw.FishingExpanded",
                    SourceWeight,
                    Ids,
                    Categories,
                    ZhTexts,
                    EnTexts);

                monitor.Log($"[FishingExpanded] 已向 VanillaTips 注入 {Ids.Length} 条提示(来源权重 {SourceWeight:0.##})。", LogLevel.Info);
            }
            catch (Exception ex)
            {
                monitor.Log($"[FishingExpanded] VanillaTips 注入失败(不影响本模组)：{ex}", LogLevel.Warn);
            }
        }
    }
}
