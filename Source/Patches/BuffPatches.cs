using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches
{
    /// <summary>BATCH-056: 钓鱼小游戏期间暂停食物 buff 计时（海之菜肴等 +钓鱼料理不会在长战斗中耗尽；
    /// 只暂停 id="food" 的食物 buff，饮料/其他 buff 照常走时）。</summary>
    [HarmonyPatch(typeof(Buff))]
    internal class BuffPatches
    {
        /// <summary>BATCH-056: 钓鱼小游戏挂载在 Game1.activeClickableMenu（非 currentMinigame）；
        /// 小游戏期间跳过食物 buff 的逐帧扣时，退出后自动恢复走时。</summary>
        [HarmonyPatch(nameof(Buff.update))]
        [HarmonyPrefix]
        public static bool Update_Prefix(Buff __instance)
        {
            if (__instance != null && __instance.id == "food" && Game1.activeClickableMenu is BobberBar)
                return false;
            return true;
        }
    }
}
