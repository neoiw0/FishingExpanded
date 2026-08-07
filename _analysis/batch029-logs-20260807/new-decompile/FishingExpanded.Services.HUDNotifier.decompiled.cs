using System;
using System.Globalization;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace FishingExpanded.Services;

public static class HUDNotifier
{
	public static void ShowSuccessNotification(string fishId, int newLevel, int oldLevel = -1)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		if (SpecialFishHelper.IsLegendaryFish(fishId))
		{
			string randomLegendaryMessage = SpecialFishHelper.GetRandomLegendaryMessage();
			Game1.addHUDMessage(new HUDMessage(randomLegendaryMessage, 1));
			ModEntry.ModMonitor.Log("[HUDNotifier] 传奇鱼提示 | 鱼ID: " + fishId + " | 文案: " + randomLegendaryMessage, (LogLevel)2);
			return;
		}
		string fishDisplayName = GetFishDisplayName(fishId);
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
		bool flag = dataOrErrorItem != null && SpecialFishHelper.IsNonFish(dataOrErrorItem.Category);
		int num = (flag ? SpecialFishHelper.GetMaxLevelForNonFish() : 100);
		string text;
		if (newLevel >= num)
		{
			text = ((!flag) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.success.fishCap", (object)new
			{
				fishName = fishDisplayName
			})) : Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.success.nonFishCap", (object)new
			{
				fishName = fishDisplayName
			})));
		}
		else
		{
			string rankKey = DifficultyCalculator.GetRankKey(newLevel);
			string rank = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
			text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.title", (object)new
			{
				fishName = fishDisplayName,
				rank = rank
			}));
		}
		Game1.addHUDMessage(new HUDMessage(text, 2));
		ModEntry.ModMonitor.Log($"[HUDNotifier] 成功提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {newLevel} | 封顶: {newLevel >= num} | 非鱼类: {flag}", (LogLevel)1);
	}

	public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if (!SpecialFishHelper.IsLegendaryFish(fishId) && DifficultyManager.HasCollectionStar(fishId, player))
		{
			string fishDisplayName = GetFishDisplayName(SpecialFishHelper.NormalizeItemId(fishId));
			string text = ChallengeDialogueGenerator.Generate(fishDisplayName, difficultyLevel);
			Game1.addHUDMessage(new HUDMessage(text, 2));
			ModEntry.ModMonitor.Log($"[HUDNotifier] 星标鱼挑战宣言 | 鱼: {fishDisplayName} ({fishId}) | 等级: {difficultyLevel} | 文案: {text}", (LogLevel)1);
		}
	}

	public static void ShowDifficultyRecommendation(int difficultyLevel, Farmer player)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		try
		{
			if (player == null || difficultyLevel <= 0)
			{
				return;
			}
			float num = (float)difficultyLevel / 10f;
			int fishingLevel = player.FishingLevel;
			if (!(num <= (float)fishingLevel))
			{
				string text = num.ToString("0.##", CultureInfo.InvariantCulture);
				string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.recommendation", (object)new
				{
					recommendedLevel = text
				}));
				if (num > (float)fishingLevel * 2f)
				{
					text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.impossible")) + text2;
				}
				Game1.addHUDMessage(new HUDMessage(text2, 3));
				ModEntry.ModMonitor.Log($"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | 鱼等级: {difficultyLevel} | 建议等级: {text} | 当前钓鱼等级: {fishingLevel} | 不可能挑战: {num > (float)fishingLevel * 2f}", (LogLevel)1);
			}
		}
		catch (Exception value)
		{
			ModEntry.ModMonitor.Log($"钓鱼等级建议提示失败: {value}", (LogLevel)4);
		}
	}

	public static void ShowFailureNotification(string fishId, int currentLevel, bool isEpicChampion = false)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		string fishDisplayName = GetFishDisplayName(fishId);
		string text = (isEpicChampion ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.epic")) : ((currentLevel <= -10) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.bottom", (object)new
		{
			fishName = fishDisplayName,
			absLevel = Math.Abs(currentLevel)
		})) : ((currentLevel >= 0) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.positive", (object)new
		{
			fishName = fishDisplayName
		})) : (Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.negative", (object)new
		{
			fishName = fishDisplayName
		})) + $"（{Math.Abs(currentLevel)}）"))));
		Game1.addHUDMessage(new HUDMessage(text, 3));
		ModEntry.ModMonitor.Log($"[HUDNotifier] 失败提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {currentLevel} | 触底: {currentLevel <= -10} | 史诗提示: {isEpicChampion}", (LogLevel)1);
	}

	private static string GetFishDisplayName(string fishId)
	{
		try
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
			if (dataOrErrorItem == null || string.IsNullOrEmpty(dataOrErrorItem.DisplayName))
			{
				ModEntry.ModMonitor.Log("[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: " + fishId, (LogLevel)3);
				return "未知鱼类";
			}
			return dataOrErrorItem.DisplayName;
		}
		catch (Exception ex)
		{
			ModEntry.ModMonitor.Log("[HUDNotifier] 获取鱼名称失败 | 鱼ID: " + fishId + " | 错误: " + ex.Message, (LogLevel)4);
			return "未知鱼类";
		}
	}
}
