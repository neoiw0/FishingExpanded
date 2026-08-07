using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Patches
{
    /// <summary>原生玩家手持物绘制入口的低频诊断。</summary>
    [HarmonyPatch(typeof(Game1))]
    internal static class Game1Patches
    {
        [HarmonyPatch(nameof(Game1.drawPlayerHeldObject))]
        [HarmonyPrefix]
        public static void DrawPlayerHeldObject_Prefix(Farmer f)
        {
            try
            {
                StardewValley.Object activeObject = f?.ActiveObject;
                string itemId = activeObject?.QualifiedItemId ?? "<null>";
                long playerId = f?.UniqueMultiplayerID ?? -1L;
                int? category = activeObject?.Category;
                bool isFishCategory = category == StardewValley.Object.FishCategory;

                ObjectPatches.LogVisualDiagnostic(
                    "Game1.drawPlayerHeldObject",
                    playerId.ToString(),
                    $"item={itemId} | category={category?.ToString() ?? "<null>"} | " +
                    $"isFishCategory={isFishCategory} | activeObject={activeObject != null} | " +
                    $"isCarrying={f?.IsCarrying() ?? false} | isLocal={f?.IsLocalPlayer ?? false} | " +
                    $"location={f?.currentLocation?.Name ?? "<null>"}");
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[DIAG-FISH-VISUAL] Game1入口诊断失败: {ex}", LogLevel.Warn);
            }
        }
    }
}
