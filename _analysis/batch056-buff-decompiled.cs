using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(Buff))]
internal class BuffPatches
{
	[HarmonyPatch("update")]
	[HarmonyPrefix]
	public static bool Update_Prefix(Buff __instance)
	{
		if (__instance != null && __instance.id == "food" && Game1.activeClickableMenu is BobberBar)
		{
			return false;
		}
		return true;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
