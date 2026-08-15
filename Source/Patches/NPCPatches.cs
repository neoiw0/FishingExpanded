using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using FishingExpanded.Services;

namespace FishingExpanded.Patches
{
    /// <summary>巨型鱼主动对话替换入口。</summary>
    [HarmonyPatch(typeof(NPC))]
    internal static class NPCPatches
    {
        [HarmonyPatch(nameof(NPC.checkAction))]
        [HarmonyPrefix]
        public static bool CheckAction_Prefix(
            NPC __instance,
            Farmer who,
            GameLocation l,
            ref bool __result)
        {
            try
            {
                if (who == null || !who.IsLocalPlayer)
                    return true;

                string replacement = GiantFishManager.TryGetReplacementDialogue(__instance, who);
                if (string.IsNullOrEmpty(replacement))
                    return true;

                __instance.CurrentDialogue.Clear();
                __instance.CurrentDialogue.Push(
                    new Dialogue(__instance, "FishingExpanded.GiantFishPraise", replacement));
                Game1.drawDialogue(__instance);
                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                FishingLog.Log($"NPC.checkAction 巨型鱼对话替换失败: {ex}", LogLevel.Error);
                return true;
            }
        }
    }
}
