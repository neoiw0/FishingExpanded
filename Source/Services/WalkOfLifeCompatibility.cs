using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>BATCH-079：Walk of Life(DaLion.Professions)兼容层——小游戏参数域清场。
    /// 职责：装 WoL 时，把其对 <see cref="StardewValley.Menus.BobberBar"/> 构造函数与
    /// <c>update</c> 的全部补丁（豪华鱼饵绿条+12、Aquarist 蓄力减速、尊贵 Aquarist 秒钓）
    /// 按 owner 精确反注册，维持"钓鱼小游戏参数唯一写入者=BobberBarPatches"的所有权不变量。
    /// 规则排序（经验/蟹笼的 HarmonyPriority 810）不在本类职责内。
    /// 未安装 WoL 时本类完全休眠（IsLoaded 门控），对既有玩家零行为差异。
    /// owner 字符串不硬编码猜测：运行时从 Harmony.GetPatchInfo().Owners 动态发现包含
    /// "DaLion.Professions" 的 owner 后精确摘除；SaveLoaded 幂等复核防动态重挂。</summary>
    internal static class WalkOfLifeCompatibility
    {
        /// <summary>WoL 模组 UniqueID（manifest.json 已核实）。</summary>
        private const string WolModId = "DaLion.Professions";

        /// <summary>BobberBar 主构造（string,float,bool,List&lt;string&gt;,string,bool,string,bool）；
        /// WoL 在其上有 Postfix：豪华鱼饵+Fisher 时绿条高度 +12。</summary>
        private static readonly ConstructorInfo BobberBarCtor = typeof(StardewValley.Menus.BobberBar)
            .GetConstructor(new[]
            {
                typeof(string), typeof(float), typeof(bool), typeof(List<string>),
                typeof(string), typeof(bool), typeof(string), typeof(bool)
            });

        /// <summary>BobberBar.update；WoL 在其上有 Transpiler（Aquarist 减速/饵效）与
        /// Postfix（尊贵 Aquarist 同种满员塘秒钓+宝箱必得）。</summary>
        private static readonly MethodInfo BobberBarUpdate = AccessTools.Method(
            typeof(StardewValley.Menus.BobberBar), "update");

        /// <summary>是否已成功摘除过补丁（决定进档提示是否展示）。</summary>
        private static bool _everRemoved;

        /// <summary>一次性 HUD 提示是否已展示（每进程一次；重进档重现可接受，不做持久化）。</summary>
        private static bool _noticeShown;

        /// <summary>Mod Entry 阶段调用（在 PatchAll 之后）：装 WoL 才执行清场并记录摘要。</summary>
        public static void OnEntry(Harmony harmony, IModHelper helper)
        {
            try
            {
                if (!helper.ModRegistry.IsLoaded(WolModId))
                {
                    FishingLog.Log("[WoL-COMPAT] 未检测到 Walk of Life，兼容层休眠", LogLevel.Debug);
                    return;
                }

                int removed = RetireBoth(harmony);
                if (removed > 0)
                {
                    _everRemoved = true;
                    FishingLog.Log(
                        "[WoL-COMPAT] 已隔离 Walk of Life 对钓鱼小游戏参数的写入" +
                        "（构造/update 共移除 " + removed + " 个补丁）；平衡规则归 FishingExpanded 唯一所有者",
                        LogLevel.Info);
                }
                else
                {
                    // 装了 WoL 但目标方法上没有其补丁：版本变更或清单变化，必须可见
                    FishingLog.Log(
                        "[WoL-COMPAT] 检测到 Walk of Life，但未在预期目标(BobberBar 构造/update)上" +
                        "发现其补丁——WoL 版本可能已变更，请重新核对兼容性",
                        LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                // 清场失败不阻断本 Mod 加载；保留 WoL 原状并显式报告
                FishingLog.Log("[WoL-COMPAT] 初始化失败（WoL 补丁保持原状，其余功能不受影响）：", LogLevel.Error);
                FishingLog.Log(ex.ToString(), LogLevel.Error);
            }
        }

        /// <summary>每次进档复核：幂等重跑清场，防动态重挂；首次成功隔离后向玩家发一次性提示。</summary>
        public static void OnSaveLoaded(Harmony harmony, IModHelper helper)
        {
            try
            {
                if (!helper.ModRegistry.IsLoaded(WolModId))
                    return;

                int removed = RetireBoth(harmony);
                if (removed > 0)
                {
                    bool firstTime = !_everRemoved;
                    _everRemoved = true;
                    FishingLog.Log(
                        "[WoL-COMPAT] 进档复核：" + (firstTime ? "完成隔离" : "检测到补丁重挂，已再次隔离") +
                        "（移除 " + removed + " 个）",
                        firstTime ? LogLevel.Info : LogLevel.Warn);
                }

                if (_everRemoved && !_noticeShown && Game1.game1 != null)
                {
                    _noticeShown = true;
                    string text = helper.Translation.Get("compat.wol.notice");
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Game1.addHUDMessage(new WrappingHUDMessage(
                            text, Math.Max(1f, Game1.uiViewport.Width / 3f)));
                    }
                }
            }
            catch (Exception ex)
            {
                FishingLog.Log("[WoL-COMPAT] 进档复核失败（不影响存档与其余功能）：", LogLevel.Error);
                FishingLog.Log(ex.ToString(), LogLevel.Error);
            }
        }

        /// <summary>对两个目标方法执行 owner 匹配反注册，返回移除的补丁 owner 数。</summary>
        private static int RetireBoth(Harmony harmony)
        {
            return RetireOwnersFrom(harmony, BobberBarCtor) + RetireOwnersFrom(harmony, BobberBarUpdate);
        }

        /// <summary>从指定方法上移除所有 owner 含 "DaLion.Professions" 的补丁（全类型：前缀/后缀/转调器等）。</summary>
        private static int RetireOwnersFrom(Harmony harmony, MethodBase method)
        {
            if (method == null)
                return 0;

            HarmonyLib.Patches info = Harmony.GetPatchInfo(method);
            if (info?.Owners == null)
                return 0;

            int removed = 0;
            foreach (string owner in info.Owners.ToList())
            {
                if (owner == null || owner.IndexOf(WolModId, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                harmony.Unpatch(method, HarmonyPatchType.All, owner);
                removed++;
                FishingLog.Log("[WoL-COMPAT] 已从 " + method.DeclaringType?.Name + "." + method.Name +
                               " 移除 owner=" + owner + " 的全部补丁", LogLevel.Trace);
            }

            return removed;
        }
    }
}
