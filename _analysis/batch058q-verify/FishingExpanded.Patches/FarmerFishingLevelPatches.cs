using System;
using FishingExpanded.Services;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(Farmer))]
internal class FarmerFishingLevelPatches
{
	[HarmonyPatch("gainExperience")]
	[HarmonyPrefix]
	public static void GainExperience_Prefix(Farmer __instance, int which, ref int howMuch)
	{
		try
		{
			if (which == 1 && howMuch > 0 && __instance.IsLocalPlayer && FishingRodPatches.TryBeginExperienceAdjustment(__instance, out var data))
			{
				int experienceMultiplier = data.ExperienceMultiplier;
				if (experienceMultiplier > 1)
				{
					long num = (long)howMuch * (long)experienceMultiplier;
					howMuch = (int)Math.Min(2147483647L, num);
					FishingLog.Log($"[FarmerFishingExperience] 经验倍率 | 难度等级: {data.DifficultyLevel} | 倍率: ×{experienceMultiplier} | 经验: {num / experienceMultiplier} → {howMuch}", (LogLevel)1);
				}
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"Fishing experience patch 失败: {value}", (LogLevel)4);
		}
	}
}
