using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace FishingExpanded.Services;

public static class HUDNotifier
{
	private static readonly Dictionary<long, Queue<HUDMessage>> PendingByPlayer = new Dictionary<long, Queue<HUDMessage>>();

	private static readonly Dictionary<long, string> ActiveMessageText = new Dictionary<long, string>();

	private static void EnqueueMessage(Farmer player, HUDMessage message)
	{
		if (player == null || message == null)
		{
			return;
		}
		long uniqueMultiplayerID = player.UniqueMultiplayerID;
		if (!ActiveMessageText.ContainsKey(uniqueMultiplayerID))
		{
			Game1.addHUDMessage(message);
			ActiveMessageText[uniqueMultiplayerID] = message.message;
			return;
		}
		if (!PendingByPlayer.TryGetValue(uniqueMultiplayerID, out var value))
		{
			value = new Queue<HUDMessage>();
			PendingByPlayer[uniqueMultiplayerID] = value;
		}
		if (value.Count >= 5)
		{
			value.Dequeue();
		}
		value.Enqueue(message);
	}

	public static void ProcessQueue()
	{
		try
		{
			if (Game1.player == null || Game1.hudMessages == null)
			{
				return;
			}
			long uniqueMultiplayerID = Game1.player.UniqueMultiplayerID;
			if (ActiveMessageText.TryGetValue(uniqueMultiplayerID, out var activeText) && !string.IsNullOrEmpty(activeText))
			{
				if (Game1.hudMessages.Any((HUDMessage m) => m != null && m.message != null && m.message == activeText))
				{
					return;
				}
				ActiveMessageText.Remove(uniqueMultiplayerID);
			}
			if (PendingByPlayer.TryGetValue(uniqueMultiplayerID, out var value) && value.Count != 0)
			{
				HUDMessage val = value.Dequeue();
				Game1.addHUDMessage(val);
				ActiveMessageText[uniqueMultiplayerID] = val.message;
			}
		}
		catch (Exception value2)
		{
			FishingLog.Log($"HUD 提示队列驱动失败: {value2}", (LogLevel)4);
		}
	}

	public static void ClearPending()
	{
		PendingByPlayer.Clear();
		ActiveMessageText.Clear();
	}

	public static void ShowSuccessNotification(string fishId, int newLevel, int oldLevel = -1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		if (SpecialFishHelper.IsLegendaryFish(fishId))
		{
			string randomLegendaryMessage = SpecialFishHelper.GetRandomLegendaryMessage();
			EnqueueMessage(Game1.player, new HUDMessage(randomLegendaryMessage, 1));
			FishingLog.Log("[HUDNotifier] 传奇鱼提示 | 鱼ID: " + fishId + " | 文案: " + randomLegendaryMessage, (LogLevel)2);
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
		EnqueueMessage(Game1.player, new HUDMessage(text, 2));
		FishingLog.Log($"[HUDNotifier] 成功提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {newLevel} | 封顶: {newLevel >= num} | 非鱼类: {flag}", (LogLevel)1);
	}

	public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		if (!SpecialFishHelper.IsLegendaryFish(fishId) && DifficultyManager.HasCollectionStar(fishId, player) && difficultyLevel >= 1)
		{
			string fishDisplayName = GetFishDisplayName(SpecialFishHelper.NormalizeItemId(fishId));
			string text = ChallengeDialogueGenerator.Generate(fishDisplayName, difficultyLevel);
			EnqueueMessage(player, new HUDMessage(text, 2));
			FishingLog.Log($"[HUDNotifier] 星标鱼挑战宣言 | 鱼: {fishDisplayName} ({fishId}) | 等级: {difficultyLevel} | 文案: {text}", (LogLevel)1);
		}
	}

	public static void ShowDifficultyRecommendation(int difficultyLevel, Farmer player)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
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
				EnqueueMessage(player, new HUDMessage(text2, 3));
				FishingLog.Log($"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | 鱼等级: {difficultyLevel} | 建议等级: {text} | 当前钓鱼等级: {fishingLevel} | 不可能挑战: {num > (float)fishingLevel * 2f}", (LogLevel)1);
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"钓鱼等级建议提示失败: {value}", (LogLevel)4);
		}
	}

	public static void ShowFailureNotification(string fishId, int currentLevel, bool isEpicChampion = false)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		string fishDisplayName = GetFishDisplayName(fishId);
		string text;
		if (!isEpicChampion)
		{
			text = ((currentLevel <= -10) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.bottom", (object)new
			{
				fishName = fishDisplayName,
				absLevel = Math.Abs(currentLevel)
			})) : ((currentLevel >= 0) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.positive", (object)new
			{
				fishName = fishDisplayName
			})) : (Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.negative", (object)new
			{
				fishName = fishDisplayName
			})) + $"（{Math.Abs(currentLevel)}）")));
		}
		else
		{
			bool flag = DifficultyManager.GetAlpha(Game1.player) >= 1f;
			text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(flag ? "hud.fail.union" : "hud.fail.epic"));
		}
		EnqueueMessage(Game1.player, new HUDMessage(text, 3));
		FishingLog.Log($"[HUDNotifier] 失败提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {currentLevel} | 触底: {currentLevel <= -10} | 史诗提示: {isEpicChampion}", (LogLevel)1);
	}

	public static void ShowStarfruitTeaNotification(string fishId)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		try
		{
			string fishDisplayName = GetFishDisplayName(fishId);
			int value = Game1.random.Next(1, 16);
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get($"hud.starfruitTea.{value}", (object)new
			{
				fishName = fishDisplayName
			}));
			EnqueueMessage(Game1.player, new HUDMessage(text, 2));
			FishingLog.Log($"[HUDNotifier] 星之果茶掉落提示 | 鱼: {fishDisplayName} ({fishId}) | 文案: #{value}", (LogLevel)1);
		}
		catch (Exception value2)
		{
			FishingLog.Log($"星之果茶掉落提示失败: {value2}", (LogLevel)4);
		}
	}

	public static void ShowPerseveranceRewardNotification(string fishId, int level)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		try
		{
			string fishDisplayName = GetFishDisplayName(fishId);
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
			int value = Game1.random.Next(1, 21);
			string text2 = $"hud.battleReward.{value}";
			string text3 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text2, (object)new
			{
				fishName = fishDisplayName,
				rankName = text
			}));
			if (string.IsNullOrWhiteSpace(text3) || text3 == text2)
			{
				text3 = fishDisplayName + text + "希望下次能更尽兴些";
			}
			EnqueueMessage(Game1.player, new HUDMessage(text3, 2));
			FishingLog.Log($"[HUDNotifier] 持久战奖励提示 | 鱼: {fishDisplayName} ({fishId}) | 文案: #{value}", (LogLevel)1);
		}
		catch (Exception value2)
		{
			FishingLog.Log($"持久战奖励提示失败: {value2}", (LogLevel)4);
		}
	}

	private static string GetFishDisplayName(string fishId)
	{
		try
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
			if (dataOrErrorItem == null || string.IsNullOrEmpty(dataOrErrorItem.DisplayName))
			{
				FishingLog.Log("[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: " + fishId, (LogLevel)3);
				return "未知鱼类";
			}
			return dataOrErrorItem.DisplayName;
		}
		catch (Exception ex)
		{
			FishingLog.Log("[HUDNotifier] 获取鱼名称失败 | 鱼ID: " + fishId + " | 错误: " + ex.Message, (LogLevel)4);
			return "未知鱼类";
		}
	}
}
