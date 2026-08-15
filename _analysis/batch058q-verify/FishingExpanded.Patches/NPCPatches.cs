using System;
using FishingExpanded.Services;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(NPC))]
internal static class NPCPatches
{
	[HarmonyPatch("checkAction")]
	[HarmonyPrefix]
	public static bool CheckAction_Prefix(NPC __instance, Farmer who, GameLocation l, ref bool __result)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		try
		{
			if (who == null || !who.IsLocalPlayer)
			{
				return true;
			}
			string text = GiantFishManager.TryGetReplacementDialogue(__instance, who);
			if (string.IsNullOrEmpty(text))
			{
				return true;
			}
			__instance.CurrentDialogue.Clear();
			__instance.CurrentDialogue.Push(new Dialogue(__instance, "FishingExpanded.GiantFishPraise", text));
			Game1.drawDialogue(__instance);
			__result = true;
			return false;
		}
		catch (Exception value)
		{
			FishingLog.Log($"NPC.checkAction 巨型鱼对话替换失败: {value}", (LogLevel)4);
			return true;
		}
	}
}
