using System;
using System.Collections.Generic;
using System.Text.Json;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Network;

namespace FishingExpanded.Services;

public static class DifficultyManager
{
	private const int MinDifficultyLevel = -10;

	private const int MaxDifficultyLevel = 100;

	private const string PlayerDataKey = "FishingExpanded/FishDifficultyData";

	private const string LegacySaveDataKey = "FishDifficultyData";

	public const int CountableCrownTarget = 61;

	private static readonly HashSet<string> CountableFishIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"(O)128", "(O)129", "(O)130", "(O)131", "(O)132", "(O)136", "(O)137", "(O)138", "(O)139", "(O)140",
		"(O)141", "(O)142", "(O)143", "(O)144", "(O)145", "(O)146", "(O)147", "(O)148", "(O)149", "(O)150",
		"(O)151", "(O)154", "(O)155", "(O)156", "(O)158", "(O)159", "(O)160", "(O)161", "(O)162", "(O)163",
		"(O)164", "(O)165", "(O)267", "(O)269", "(O)682", "(O)698", "(O)699", "(O)700", "(O)701", "(O)702",
		"(O)704", "(O)705", "(O)706", "(O)707", "(O)708", "(O)734", "(O)775", "(O)795", "(O)796", "(O)798",
		"(O)799", "(O)800", "(O)836", "(O)837", "(O)838", "(O)898", "(O)899", "(O)900", "(O)901", "(O)902",
		"(O)Goby"
	};

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly Dictionary<long, FishDifficultyData> _dataByPlayer = new Dictionary<long, FishDifficultyData>();

	public static void LoadData(Farmer player)
	{
		if (player == null)
		{
			return;
		}
		FishDifficultyData fishDifficultyData = ReadFromModData(player);
		if (fishDifficultyData == null && Context.IsMainPlayer)
		{
			try
			{
				fishDifficultyData = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>("FishDifficultyData");
				if (fishDifficultyData != null)
				{
					FishingLog.Log("[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据", (LogLevel)3);
				}
			}
			catch (Exception ex)
			{
				FishingLog.Log("[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: " + ex.Message, (LogLevel)4);
			}
		}
		if (fishDifficultyData == null)
		{
			fishDifficultyData = new FishDifficultyData();
		}
		NormalizeData(fishDifficultyData);
		_dataByPlayer[player.UniqueMultiplayerID] = fishDifficultyData;
		FishingLog.Log($"[DifficultyManager] 加载玩家数据完成 | 玩家: {player.UniqueMultiplayerID} | 鱼种类数: {fishDifficultyData.FishStatistics.Count} | 皇冠鱼种: {fishDifficultyData.CollectionStars.Count} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)2);
	}

	public static void SaveData(Farmer player)
	{
		if (player != null && _dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
		{
			NormalizeData(value);
			((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Character)player).modData)["FishingExpanded/FishDifficultyData"] = JsonSerializer.Serialize(value, JsonOptions);
			FishingLog.Log($"已保存玩家钓鱼难度数据 | 玩家: {player.UniqueMultiplayerID}", (LogLevel)1);
		}
	}

	public unsafe static void SaveAll()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Enumerator enumerator = Game1.getOnlineFarmers().GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				Farmer current = ((Enumerator)(ref enumerator)).Current;
				if (_dataByPlayer.ContainsKey(current.UniqueMultiplayerID))
				{
					SaveData(current);
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
	}

	public static void UnloadData()
	{
		_dataByPlayer.Clear();
	}

	public static int GetMaxDifficultyLevel(string fishId)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (string.IsNullOrEmpty(text))
		{
			return 100;
		}
		try
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(text);
			return (dataOrErrorItem != null && SpecialFishHelper.IsNonFish(dataOrErrorItem.Category)) ? SpecialFishHelper.GetMaxLevelForNonFish() : 100;
		}
		catch
		{
			return 100;
		}
	}

	public static int GetDifficultyLevel(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
		{
			return 0;
		}
		int maxDifficultyLevel = GetMaxDifficultyLevel(text);
		return Math.Max(-10, Math.Min(maxDifficultyLevel, value.DifficultyLevel));
	}

	public static int RecordSuccess(string fishId, int levelGain, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", (LogLevel)4);
			return 0;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
		{
			return 0;
		}
		if (!data.FishStatistics.TryGetValue(text, out var value))
		{
			value = new FishStats();
			data.FishStatistics[text] = value;
			FishingLog.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
		}
		int difficultyLevel = GetDifficultyLevel(text, player);
		int maxDifficultyLevel = GetMaxDifficultyLevel(text);
		int num = Math.Min(DifficultyCalculator.GetNextRankCeiling(difficultyLevel), maxDifficultyLevel);
		int val = Math.Max(0, Math.Min(levelGain, num - difficultyLevel));
		val = ((difficultyLevel >= 89) ? Math.Min(val, 3) : Math.Min(val, Math.Max(0, 89 - difficultyLevel)));
		value.SuccessCount = SaturatingAdd(value.SuccessCount, val);
		value.ConsecutiveFailCount = 0;
		int difficultyLevel2 = GetDifficultyLevel(text, player);
		FishingLog.Log($"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 请求增长: +{levelGain} | 实际增长: +{val} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
		SaveData(player);
		return val;
	}

	public static void RecordFailure(string fishId, Farmer player, bool keepLevel = false)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", (LogLevel)4);
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
		{
			if (!data.FishStatistics.TryGetValue(text, out var value))
			{
				value = new FishStats();
				data.FishStatistics[text] = value;
				FishingLog.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
			}
			int difficultyLevel = GetDifficultyLevel(text, player);
			if (!keepLevel)
			{
				value.FailCount = SaturatingAdd(value.FailCount, 1);
			}
			value.ConsecutiveFailCount = SaturatingAdd(value.ConsecutiveFailCount, 1);
			int difficultyLevel2 = GetDifficultyLevel(text, player);
			FishingLog.Log($"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 连续失败次数: {value.ConsecutiveFailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}" + (keepLevel ? " | 挑战鱼饵不掉等级" : ""), (LogLevel)2);
			SaveData(player);
		}
	}

	public static int GetOrCreateChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null || player == null)
		{
			return 0;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		string key = text + "|" + difficultyLevel;
		if (data.ChallengePatternSeeds.TryGetValue(key, out var value))
		{
			return value;
		}
		int num = new Random(Guid.NewGuid().GetHashCode()).Next();
		data.ChallengePatternSeeds[key] = num;
		SaveData(player);
		FishingLog.Log($"[DifficultyManager] 挑战背板种子生成 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 等级: {difficultyLevel} | 种子: {num}", (LogLevel)2);
		return num;
	}

	public static void ClearChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data != null && player != null)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			string key = text + "|" + difficultyLevel;
			if (data.ChallengePatternSeeds.Remove(key))
			{
				SaveData(player);
				FishingLog.Log($"[DifficultyManager] 挑战背板种子清除 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 等级: {difficultyLevel}", (LogLevel)2);
			}
		}
	}

	public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", (LogLevel)4);
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text) && adjustedDifficulty >= 120f && !data.CollectionStars.Contains(text))
		{
			data.CollectionStars.Add(text);
			FishingLog.Log($"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 调整后难度: {adjustedDifficulty:F1} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
			SaveData(player);
		}
	}

	public static int AddCollectionStarsForTesting(Farmer player, int count)
	{
		if (player == null || count <= 0)
		{
			return 0;
		}
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string item in BuildStarPool())
		{
			if (num >= count)
			{
				break;
			}
			string text = SpecialFishHelper.NormalizeItemId(item);
			if (!string.IsNullOrEmpty(text) && !data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				num++;
			}
		}
		if (num > 0)
		{
			SaveData(player);
		}
		return num;
	}

	private static List<string> BuildStarPool()
	{
		List<string> list = new List<string>(CountableFishIds.Count);
		foreach (string countableFishId in CountableFishIds)
		{
			list.Add(countableFishId);
		}
		return list;
	}

	public static bool IsCountableFish(string fishId)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text))
		{
			return CountableFishIds.Contains(text);
		}
		return false;
	}

	public static int GetCountableCrownCount(Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string collectionStar in data.CollectionStars)
		{
			if (IsCountableFish(collectionStar))
			{
				num++;
			}
		}
		return num;
	}

	public static List<string> GetCountableStarredFish(Farmer player)
	{
		List<string> list = new List<string>();
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			return list;
		}
		foreach (string collectionStar in data.CollectionStars)
		{
			if (IsCountableFish(collectionStar))
			{
				list.Add(collectionStar);
			}
		}
		return list;
	}

	public static double GetAssistRank(string fishId, Farmer player, List<string> countableStarred)
	{
		if (countableStarred == null || countableStarred.Count == 0)
		{
			return 0.5;
		}
		int num = int.MaxValue;
		int num2 = int.MinValue;
		foreach (string item in countableStarred)
		{
			int difficultyLevel = GetDifficultyLevel(item, player);
			if (difficultyLevel < num)
			{
				num = difficultyLevel;
			}
			if (difficultyLevel > num2)
			{
				num2 = difficultyLevel;
			}
		}
		if (num2 <= num)
		{
			return 0.5;
		}
		int difficultyLevel2 = GetDifficultyLevel(fishId, player);
		return Math.Max(0.0, Math.Min(1.0, (double)(difficultyLevel2 - num) / (double)(num2 - num)));
	}

	public static float GetAlpha(Farmer player)
	{
		return GetAlphaFromCrowns(GetCountableCrownCount(player));
	}

	public static float GetAlphaFromCrowns(int countableCrowns)
	{
		int num = Math.Max(0, countableCrowns);
		if (num >= 61)
		{
			return 1f;
		}
		if (num <= 0)
		{
			return 0f;
		}
		if (num <= 5)
		{
			return 0.02f * (float)num / 5f;
		}
		if (num <= 10)
		{
			return 0.02f + 0.05f * (float)(num - 5) / 5f;
		}
		if (num <= 20)
		{
			return 0.07f + 0.13f * (float)(num - 10) / 10f;
		}
		if (num <= 30)
		{
			return 0.2f + 0.15f * (float)(num - 20) / 10f;
		}
		return 0.35f + 0.65f * (float)(num - 30) / 31f;
	}

	public static void RecordLegendaryCatch(string fishId, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录传奇鱼皇冠", (LogLevel)4);
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text) && SpecialFishHelper.IsLegendaryFish(text) && !data.CollectionStars.Contains(text))
		{
			data.CollectionStars.Add(text);
			FishingLog.Log($"[DifficultyManager] ★ 传奇鱼一次钓获皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
			SaveData(player);
		}
	}

	public static void BackfillLegendaryCrowns(Farmer player)
	{
		if (player == null || !Context.IsWorldReady || player.fishCaught == null)
		{
			return;
		}
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			return;
		}
		bool flag = false;
		int[] array = default(int[]);
		foreach (string legendaryFishId in SpecialFishHelper.GetLegendaryFishIds())
		{
			if (((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)player.fishCaught).TryGetValue(legendaryFishId, ref array) && array != null && array.Length != 0 && array[0] > 0 && data.CollectionStars.Add(legendaryFishId))
			{
				flag = true;
				FishingLog.Log($"[DifficultyManager] ★ 传奇鱼回填皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {legendaryFishId} | 累计钓获: {array[0]}", (LogLevel)3);
			}
		}
		if (flag)
		{
			SaveData(player);
		}
	}

	public static bool HasCollectionStar(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data != null && !string.IsNullOrEmpty(text))
		{
			return data.CollectionStars.Contains(text);
		}
		return false;
	}

	public static bool HasChallengeCrown(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data != null && !string.IsNullOrEmpty(text))
		{
			return data.ChallengeCrowns.Contains(text);
		}
		return false;
	}

	public static void RecordChallengeCrown(string fishId, Farmer player, bool atLevel100 = false)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
		{
			return;
		}
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录挑战皇冠", (LogLevel)4);
			return;
		}
		bool flag = data.ChallengeCrowns.Add(text);
		bool flag2 = atLevel100 && data.Level100FlowCrowns.Add(text);
		if (flag || flag2)
		{
			FishingLog.Log($"[DifficultyManager] ★ 挑战皇冠记录 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 挑战皇冠数: {data.ChallengeCrowns.Count} | 100级流动皇冠: {data.Level100FlowCrowns.Count}", (LogLevel)2);
			SaveData(player);
		}
	}

	public static bool HasLevel100FlowCrown(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data != null && !string.IsNullOrEmpty(text))
		{
			return data.Level100FlowCrowns.Contains(text);
		}
		return false;
	}

	public static void SetChallengeCrown(string fishId, bool value, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
		{
			return;
		}
		FishDifficultyData data = GetData(player);
		if (data != null)
		{
			if (value)
			{
				data.ChallengeCrowns.Add(text);
			}
			else
			{
				data.ChallengeCrowns.Remove(text);
			}
			FishingLog.Log($"[DifficultyManager] 已设置挑战皇冠 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 值: {value}", (LogLevel)2);
			SaveData(player);
		}
	}

	public static void SetDifficultyLevel(string fishId, int targetLevel, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
		{
			targetLevel = Math.Max(-10, Math.Min(GetMaxDifficultyLevel(text), targetLevel));
			if (!data.FishStatistics.TryGetValue(text, out var value))
			{
				value = new FishStats();
				data.FishStatistics[text] = value;
			}
			value.SuccessCount = targetLevel + value.FailCount;
			FishingLog.Log($"[DifficultyManager] 已设置等级 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 目标等级: {targetLevel} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount}", (LogLevel)2);
			SaveData(player);
		}
	}

	public static int GetConsecutiveFailCount(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
		{
			return 0;
		}
		return Math.Max(0, value.ConsecutiveFailCount);
	}

	public static FishStats GetFishStats(string fishId, Farmer player)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDifficultyData data = GetData(player);
		if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
		{
			return null;
		}
		return value;
	}

	public static Dictionary<string, FishStats> GetAllFishStats(Farmer player)
	{
		return GetData(player)?.FishStatistics ?? new Dictionary<string, FishStats>();
	}

	public static void ClearAllData(Farmer player)
	{
		if (player == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家为null", (LogLevel)4);
			return;
		}
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
			return;
		}
		int count = data.FishStatistics.Count;
		int count2 = data.CollectionStars.Count;
		int count3 = data.ChallengeCrowns.Count;
		int count4 = data.Level100FlowCrowns.Count;
		data.FishStatistics.Clear();
		data.CollectionStars.Clear();
		data.ChallengeCrowns.Clear();
		data.Level100FlowCrowns.Clear();
		BackfillLegendaryCrowns(player);
		bool value = false;
		if (Context.IsMainPlayer)
		{
			try
			{
				ModEntry.ModHelper.Data.WriteSaveData<FishDifficultyData>("FishDifficultyData", (FishDifficultyData)null);
				value = true;
			}
			catch (Exception ex)
			{
				FishingLog.Log("[DifficultyManager] 删除旧存档级数据失败: " + ex.Message, (LogLevel)4);
			}
		}
		SaveData(player);
		FishingLog.Log($"[DifficultyManager] 已清空所有数据 | 玩家: {player.UniqueMultiplayerID} | 统计: {count} | 星标: {count2} | 挑战皇冠: {count3} | 100级流动皇冠: {count4} | 旧存档级键删除: {value}", (LogLevel)3);
	}

	public static void RecordHighDifficulty(string fishId, Farmer player)
	{
		FishDifficultyData data = GetData(player);
		if (data == null)
		{
			FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
		{
			if (!data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				FishingLog.Log($"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
				SaveData(player);
			}
			else
			{
				FishingLog.Log("[DifficultyManager] 鱼 " + text + " 已有收藏星标", (LogLevel)2);
			}
		}
	}

	public static int GetCollectionStarCount(Farmer player)
	{
		return GetData(player)?.CollectionStars.Count ?? 0;
	}

	private static FishDifficultyData GetData(Farmer player)
	{
		if (player == null)
		{
			return null;
		}
		if (!_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
		{
			LoadData(player);
			_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out value);
		}
		return value;
	}

	private static FishDifficultyData ReadFromModData(Farmer player)
	{
		string text = default(string);
		if (((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Character)player).modData).TryGetValue("FishingExpanded/FishDifficultyData", ref text) && !string.IsNullOrWhiteSpace(text))
		{
			try
			{
				return JsonSerializer.Deserialize<FishDifficultyData>(text, JsonOptions);
			}
			catch (JsonException ex)
			{
				FishingLog.Log($"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {player.UniqueMultiplayerID} | 错误: {ex.Message}", (LogLevel)4);
			}
		}
		return null;
	}

	private static void NormalizeData(FishDifficultyData data)
	{
		Dictionary<string, FishStats> dictionary = new Dictionary<string, FishStats>();
		foreach (KeyValuePair<string, FishStats> item in data.FishStatistics ?? new Dictionary<string, FishStats>())
		{
			string text = SpecialFishHelper.NormalizeItemId(item.Key);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text) && item.Value != null)
			{
				if (!dictionary.TryGetValue(text, out var value))
				{
					dictionary[text] = item.Value;
					continue;
				}
				value.SuccessCount = SaturatingAdd(value.SuccessCount, item.Value.SuccessCount);
				value.FailCount = SaturatingAdd(value.FailCount, item.Value.FailCount);
			}
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string item2 in data.CollectionStars ?? new HashSet<string>())
		{
			string text2 = SpecialFishHelper.NormalizeItemId(item2);
			if (!string.IsNullOrEmpty(text2))
			{
				hashSet.Add(text2);
			}
		}
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (string item3 in data.ChallengeCrowns ?? new HashSet<string>())
		{
			string text3 = SpecialFishHelper.NormalizeItemId(item3);
			if (!string.IsNullOrEmpty(text3) && !SpecialFishHelper.IsLegendaryFish(text3))
			{
				hashSet2.Add(text3);
			}
		}
		HashSet<string> hashSet3 = new HashSet<string>();
		foreach (string item4 in data.Level100FlowCrowns ?? new HashSet<string>())
		{
			string text4 = SpecialFishHelper.NormalizeItemId(item4);
			if (!string.IsNullOrEmpty(text4) && !SpecialFishHelper.IsLegendaryFish(text4))
			{
				hashSet3.Add(text4);
			}
		}
		data.FishStatistics = dictionary;
		data.CollectionStars = hashSet;
		data.ChallengeCrowns = hashSet2;
		data.Level100FlowCrowns = hashSet3;
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		foreach (KeyValuePair<string, int> item5 in data.ChallengePatternSeeds ?? new Dictionary<string, int>())
		{
			string[] array = item5.Key.Split('|');
			if (array.Length == 2)
			{
				string text5 = SpecialFishHelper.NormalizeItemId(array[0]);
				if (!string.IsNullOrEmpty(text5) && int.TryParse(array[1], out var result))
				{
					dictionary2[text5 + "|" + result] = item5.Value;
				}
			}
		}
		data.ChallengePatternSeeds = dictionary2;
	}

	private static int SaturatingAdd(int left, int right)
	{
		long num = (long)left + (long)right;
		if (num <= int.MaxValue)
		{
			if (num >= int.MinValue)
			{
				return (int)num;
			}
			return int.MinValue;
		}
		return int.MaxValue;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
