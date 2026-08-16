using System;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(Event))]
internal static class EventFestivalPatches
{
	[HarmonyPatch("caughtFish")]
	[HarmonyPostfix]
	public static void CaughtFish_Postfix(Event __instance, string itemId, int size, Farmer who)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (who == null || !who.IsLocalPlayer || !__instance.isSpecificFestival("winter8") || !ModEntry.Config.EnableFestivalFishingMods || size <= 0 || ((Character)who).TilePoint.X >= 79 || ((Character)who).TilePoint.Y >= 43)
			{
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(itemId);
			if (FishingRodPatches.TryGetPending(who, text, out var data))
			{
				int num = data.Multiplier - 1;
				if (num > 0)
				{
					who.festivalScore += num;
					FishingLog.Log($"[节日] 冰雪节分数补差 | 玩家: {who.UniqueMultiplayerID} | 鱼ID: {text} | 数量倍数: {data.Multiplier} | 补分: +{num} | 总分: {who.festivalScore}", (LogLevel)2);
				}
				FishingRodPatches.RemovePending(who, text);
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"Event.caughtFish Postfix 失败: {value}", (LogLevel)4);
		}
	}
}
