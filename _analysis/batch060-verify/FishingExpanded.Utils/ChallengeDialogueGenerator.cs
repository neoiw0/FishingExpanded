using System;
using System.Collections.Generic;
using FishingExpanded.Services;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Utils;

public static class ChallengeDialogueGenerator
{
	private static readonly Dictionary<string, string> HonorificKeys = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["rank.weak"] = "hud.starChallenge.honor.weak",
		["rank.elite"] = "hud.starChallenge.honor.elite",
		["rank.knight"] = "hud.starChallenge.honor.knight",
		["rank.lord"] = "hud.starChallenge.honor.lord",
		["rank.count"] = "hud.starChallenge.honor.count",
		["rank.duke"] = "hud.starChallenge.honor.duke",
		["rank.prince"] = "hud.starChallenge.honor.prince",
		["rank.emperor"] = "hud.starChallenge.honor.emperor",
		["rank.godking"] = "hud.starChallenge.honor.godking",
		["rank.divineking"] = "hud.starChallenge.honor.divineking",
		["rank.creator"] = "hud.starChallenge.honor.creator",
		["rank.taiyi"] = "hud.starChallenge.honor.taiyi"
	};

	private static readonly string[] FallbackHonorifics = new string[3] { "可敬的", "令人敬重的", "值得敬佩的" };

	private static readonly string[] FallbackResolve = new string[1] { "毅然决然" };

	private static readonly string[] FallbackActions = new string[1] { "应战" };

	public static string Generate(string fishName, int level)
	{
		try
		{
			string rankKey = DifficultyCalculator.GetRankKey(level);
			string rank = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
			string value;
			string translationKey = (HonorificKeys.TryGetValue(rankKey, out value) ? value : "hud.starChallenge.honor.weak");
			string honorific = Pick(LoadOptions(translationKey, FallbackHonorifics));
			string resolve = Pick(LoadOptions("hud.starChallenge.resolve", FallbackResolve));
			string action = Pick(LoadOptions("hud.starChallenge.action", FallbackActions));
			string text = ((rankKey == "rank.weak") ? "hud.starChallenge.template.weak" : "hud.starChallenge.template");
			return Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text, (object)new
			{
				honorific = honorific,
				fishName = (string.IsNullOrWhiteSpace(fishName) ? "未知鱼类" : fishName),
				rank = rank,
				resolve = resolve,
				action = action
			}));
		}
		catch (Exception ex)
		{
			FishingLog.Log("[ChallengeDialogue] 生成挑战宣言失败: " + ex.Message, (LogLevel)4);
			return "可敬的" + (fishName ?? "未知鱼类") + "毅然决然应战。";
		}
	}

	private static string[] LoadOptions(string translationKey, string[] fallback)
	{
		string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(translationKey));
		if (string.IsNullOrWhiteSpace(text) || text == translationKey)
		{
			return fallback;
		}
		string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			return array;
		}
		return fallback;
	}

	private static string Pick(string[] options)
	{
		if (options == null || options.Length == 0)
		{
			return string.Empty;
		}
		if (Game1.random == null)
		{
			return options[0];
		}
		return options[Game1.random.Next(options.Length)];
	}
}
