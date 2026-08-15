using System;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(Farmer))]
internal class FarmerFishingPatches
{
	[HarmonyPatch("caughtFish")]
	[HarmonyPrefix]
	public static void CaughtFish_Prefix(Farmer __instance, string itemId, ref int size, bool from_fish_pond)
	{
		try
		{
			if (!from_fish_pond && __instance.IsLocalPlayer && size > 0)
			{
				string text = SpecialFishHelper.NormalizeItemId(itemId);
				if (FishingRodPatches.TryGetPending(__instance, text, out var data) && data.FishSize > 0 && data.DifficultyLevel != 0 && data.FishSize != size)
				{
					FishingLog.Log($"[Farmer] 收藏尺寸使用结算值 | 鱼ID: {text} | {size} → {data.FishSize}", (LogLevel)1);
					size = data.FishSize;
				}
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"caughtFish Prefix 失败: {value}", (LogLevel)4);
		}
	}

	[HarmonyPatch("caughtFish")]
	[HarmonyPostfix]
	public static void CaughtFish_Postfix(Farmer __instance, string itemId, int size, bool from_fish_pond, int numberCaught)
	{
		try
		{
			if (from_fish_pond || !__instance.IsLocalPlayer)
			{
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(itemId);
			if (FishingRodPatches.TryGetPending(__instance, text, out var data) && !data.SuccessRecorded)
			{
				data.SuccessRecorded = true;
				int num = data.MissCount switch
				{
					0 => 10, 
					1 => 5, 
					2 => 2, 
					_ => 1, 
				};
				int num2 = (int)Math.Round(data.AdjustedDifficulty / 50f);
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, __instance);
				int value = DifficultyManager.RecordSuccess(text, num + num2, __instance);
				int difficultyLevel2 = DifficultyManager.GetDifficultyLevel(text, __instance);
				DifficultyManager.RecordHighDifficulty(text, data.AdjustedDifficulty, __instance);
				if (data.DifficultyLevel >= 95 && data.HasChallengeBait)
				{
					DifficultyManager.RecordChallengeCrown(text, __instance, data.DifficultyLevel >= 100);
				}
				if (data.HasChallengeBait)
				{
					DifficultyManager.ClearChallengePatternSeed(text, data.DifficultyLevel, __instance);
				}
				if (data.Multiplier > 15)
				{
					int fishSize = ((data.FishSize > 0) ? data.FishSize : (Math.Max(1, data.OriginalNum) * data.Multiplier));
					GiantFishManager.RecordGiantFish(__instance, text, data.Multiplier, fishSize);
				}
				FishingLog.Log($"[Farmer] 难度等级更新 | 鱼ID: {text} | 脱杆: {data.MissCount}次 | 基础增长: +{num} | 额外难度增益: +{num2} | 实际增长: +{value} | {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
				HUDNotifier.ShowSuccessNotification(text, difficultyLevel2, difficultyLevel);
				if (difficultyLevel >= 50 && DifficultyCalculator.TryGetStarfruitTeaDrop(difficultyLevel))
				{
					Item val = ItemRegistry.Create("(O)StardropTea", 1, 0, false);
					__instance.addItemByMenuIfNecessary(val, (behaviorOnItemSelect)null, false);
					HUDNotifier.ShowStarfruitTeaNotification(text);
					FishingLog.Log($"[Farmer] 星之果茶掉落 | 鱼ID: {text} | 难度等级: {difficultyLevel} | 概率: {(double)difficultyLevel / 400.0:P1}", (LogLevel)2);
				}
			}
		}
		catch (Exception value2)
		{
			FishingLog.Log($"caughtFish Postfix 失败: {value2}", (LogLevel)4);
		}
	}
}
