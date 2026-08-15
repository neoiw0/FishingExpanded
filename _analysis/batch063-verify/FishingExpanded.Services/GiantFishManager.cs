using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Network;

namespace FishingExpanded.Services;

public static class GiantFishManager
{
	private static readonly Dictionary<long, FishDisplayData> _displayDataByPlayer = new Dictionary<long, FishDisplayData>();

	private static readonly Dictionary<(long playerId, string fishId), int> _lastLoggedNearbyCheck = new Dictionary<(long, string), int>();

	private static readonly Dictionary<(long playerId, string npcName), Dialogue[]> _originalDialogues = new Dictionary<(long, string), Dialogue[]>();

	private static FishDisplayData GetDisplayData(Farmer player, bool create)
	{
		if (player == null)
		{
			return null;
		}
		if (!_displayDataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value) && create)
		{
			value = new FishDisplayData();
			_displayDataByPlayer[player.UniqueMultiplayerID] = value;
		}
		return value;
	}

	public static void RecordGiantFish(string fishId, int level, int fishSize)
	{
		RecordGiantFish(Game1.player, fishId, level, fishSize);
	}

	public static void RecordGiantFish(Farmer player, string fishId, int level, int fishSize)
	{
		string text = SpecialFishHelper.NormalizeItemId(fishId);
		FishDisplayData displayData = GetDisplayData(player, create: true);
		if (displayData != null && level >= 8 && !SpecialFishHelper.IsLegendaryFish(text) && IsFish(text))
		{
			displayData.ActiveGiantFish[text] = (level, fishSize);
			FishingLog.Log($"[GiantFishManager] 超大鱼记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 难度等级: {level} | fishSize: {fishSize} | 视觉缩放: ×{DifficultyCalculator.GetVisualScale(level):F2}", (LogLevel)2);
		}
	}

	public unsafe static void CheckAndTriggerNPCReactions()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (!Context.IsWorldReady)
		{
			return;
		}
		Enumerator enumerator = Game1.getOnlineFarmers().GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				Farmer current = ((Enumerator)(ref enumerator)).Current;
				if (current != null && current.IsLocalPlayer)
				{
					CheckAndTriggerForPlayer(current);
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
	}

	private static void CheckAndTriggerForPlayer(Farmer player)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		FishDisplayData displayData = GetDisplayData(player, create: false);
		if (displayData == null || displayData.ActiveGiantFish.Count == 0)
		{
			return;
		}
		Object activeObject = player.ActiveObject;
		if (activeObject == null || !player.IsCarrying())
		{
			return;
		}
		string text = SpecialFishHelper.NormalizeItemId(((Item)activeObject).QualifiedItemId);
		if (!displayData.ActiveGiantFish.TryGetValue(text, out (int, int) value))
		{
			return;
		}
		int item = value.Item1;
		int item2 = value.Item2;
		List<NPC> nearbyNPCs = GetNearbyNPCs(((Character)player).Position, 320f);
		if (nearbyNPCs.Count > 0)
		{
			(long, string) key = (player.UniqueMultiplayerID, text);
			if (!_lastLoggedNearbyCheck.ContainsKey(key) || _lastLoggedNearbyCheck[key] != nearbyNPCs.Count)
			{
				FishingLog.Log($"[GiantFishManager] 检测到附近NPC | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 5格内NPC数量: {nearbyNPCs.Count}", (LogLevel)1);
				_lastLoggedNearbyCheck[key] = nearbyNPCs.Count;
			}
		}
		foreach (NPC item3 in nearbyNPCs)
		{
			TriggerNPCBubble(item3, text, item2, player);
		}
	}

	private static void TriggerNPCBubble(NPC npc, string fishId, int fishSize, Farmer player)
	{
		FishDisplayData displayData = GetDisplayData(player, create: true);
		if (displayData == null)
		{
			return;
		}
		if (!displayData.NPCBubbleTriggered.ContainsKey(((Character)npc).Name))
		{
			displayData.NPCBubbleTriggered[((Character)npc).Name] = new HashSet<string>();
		}
		if (!displayData.NPCBubbleTriggered[((Character)npc).Name].Contains(fishId))
		{
			string fishName = ItemRegistry.GetDataOrErrorItem(fishId)?.DisplayName ?? "未知鱼类";
			string text = NPCDialogueGenerator.GenerateFishPraise(fishName, fishSize);
			string animalSound = GetAnimalSound(npc);
			if (!string.IsNullOrEmpty(animalSound))
			{
				text = animalSound + "！！！（" + text + "）";
			}
			npc.showTextAboveHead(text, (Color?)null, 2, 3000, 0);
			displayData.NPCBubbleTriggered[((Character)npc).Name].Add(fishId);
			FishingLog.Log($"[GiantFishManager] NPC冒泡触发 | NPC: {((Character)npc).Name} | 鱼ID: {fishId} | fishSize: {fishSize} | 文案: {text.Substring(0, Math.Min(30, text.Length))}...", (LogLevel)2);
		}
	}

	private static string GetAnimalSound(NPC npc)
	{
		Pet val = (Pet)(object)((npc is Pet) ? npc : null);
		if (val != null)
		{
			string a = ((NetFieldBase<string, NetString>)(object)val.petType)?.Value;
			if (string.Equals(a, "Dog", StringComparison.OrdinalIgnoreCase))
			{
				return PickSound(new string[5] { "汪汪", "汪！", "汪汪汪", "嗷呜～汪", "汪~汪" });
			}
			if (string.Equals(a, "Cat", StringComparison.OrdinalIgnoreCase))
			{
				return PickSound(new string[5] { "喵喵", "喵～", "喵呜", "喵喵喵", "咪" });
			}
			return null;
		}
		if (npc is Horse)
		{
			return PickSound(new string[5] { "嘶嘶", "嘶——", "唏律律", "吁——", "嘶～" });
		}
		return null;
	}

	private static string PickSound(string[] sounds)
	{
		if (sounds == null || sounds.Length == 0)
		{
			return null;
		}
		return sounds[Game1.random.Next(sounds.Length)];
	}

	private static List<NPC> GetNearbyNPCs(Vector2 position, float radius)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (Game1.currentLocation == null)
		{
			return new List<NPC>();
		}
		List<NPC> list = new List<NPC>();
		float num = radius * radius;
		foreach (NPC character in Game1.currentLocation.characters)
		{
			float num2 = ((Character)character).Position.X - position.X;
			float num3 = ((Character)character).Position.Y - position.Y;
			float num4 = num2 * num2 + num3 * num3;
			if (num4 <= num)
			{
				list.Add(character);
			}
		}
		return list;
	}

	public static void OnEnterFarmHouse(Farmer player)
	{
		FishDisplayData displayData = GetDisplayData(player, create: false);
		int num = displayData?.ActiveGiantFish.Count ?? 0;
		displayData?.ClearActiveGiantFish();
		ClearDialogueSnapshots(player);
		foreach (var item2 in _lastLoggedNearbyCheck.Keys.Where(delegate((long playerId, string fishId) key)
		{
			long item = key.playerId;
			Farmer obj2 = player;
			return item == ((obj2 != null) ? new long?(obj2.UniqueMultiplayerID) : ((long?)null));
		}).ToList())
		{
			_lastLoggedNearbyCheck.Remove(item2);
		}
		if (num > 0)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 2);
			defaultInterpolatedStringHandler.AppendLiteral("[GiantFishManager] 进入FarmHouse | 玩家: ");
			Farmer obj = player;
			defaultInterpolatedStringHandler.AppendFormatted((obj != null) ? new long?(obj.UniqueMultiplayerID) : ((long?)null));
			defaultInterpolatedStringHandler.AppendLiteral(" | 清空超大鱼记录: ");
			defaultInterpolatedStringHandler.AppendFormatted(num);
			defaultInterpolatedStringHandler.AppendLiteral("种");
			FishingLog.Log(defaultInterpolatedStringHandler.ToStringAndClear(), (LogLevel)2);
		}
	}

	public static void OnDayStarted()
	{
		int num = 0;
		int num2 = 0;
		foreach (FishDisplayData value in _displayDataByPlayer.Values)
		{
			num += value.NPCBubbleTriggered.Count;
			num2 += value.NPCDialogueTriggered.Count;
			value.ResetDailyTriggers();
		}
		_originalDialogues.Clear();
		_lastLoggedNearbyCheck.Clear();
		FishingLog.Log($"[GiantFishManager] 每日重置 | 玩家数: {_displayDataByPlayer.Count} | 清空冒泡记录: {num}个NPC | 对话记录: {num2}个NPC", (LogLevel)2);
	}

	public static void ResetForSave()
	{
		_displayDataByPlayer.Clear();
		_lastLoggedNearbyCheck.Clear();
		_originalDialogues.Clear();
	}

	public static float GetFishVisualScale(string fishId)
	{
		return GetFishVisualScale(fishId, Game1.player);
	}

	public static float GetFishVisualScale(string fishId, Farmer player)
	{
		string key = SpecialFishHelper.NormalizeItemId(fishId);
		FishDisplayData displayData = GetDisplayData(player, create: false);
		if (displayData != null && displayData.ActiveGiantFish.TryGetValue(key, out (int, int) value))
		{
			return DifficultyCalculator.GetVisualScale(value.Item1);
		}
		return 1f;
	}

	public static string TryGetReplacementDialogue(NPC npc)
	{
		return TryGetReplacementDialogue(npc, Game1.player);
	}

	public static string TryGetReplacementDialogue(NPC npc, Farmer player)
	{
		if (npc == null || player == null || !player.IsLocalPlayer || player.ActiveObject == null || !player.IsCarrying())
		{
			return null;
		}
		string text = SpecialFishHelper.NormalizeItemId(((Item)player.ActiveObject).QualifiedItemId);
		FishDisplayData displayData = GetDisplayData(player, create: true);
		if (displayData == null || !displayData.ActiveGiantFish.TryGetValue(text, out (int, int) value))
		{
			return null;
		}
		if (!displayData.NPCDialogueTriggered.ContainsKey(((Character)npc).Name))
		{
			displayData.NPCDialogueTriggered[((Character)npc).Name] = new HashSet<string>();
		}
		if (displayData.NPCDialogueTriggered[((Character)npc).Name].Contains(text))
		{
			RestoreOriginalDialogue(npc, player);
			return null;
		}
		string fishName = ItemRegistry.GetDataOrErrorItem(text)?.DisplayName ?? "未知鱼类";
		string result = NPCDialogueGenerator.GenerateFishPraise(fishName, value.Item2);
		displayData.NPCDialogueTriggered[((Character)npc).Name].Add(text);
		_originalDialogues[(player.UniqueMultiplayerID, ((Character)npc).Name)] = npc.CurrentDialogue?.ToArray() ?? Array.Empty<Dialogue>();
		return result;
	}

	private static void RestoreOriginalDialogue(NPC npc, Farmer player)
	{
		if (npc != null && player != null && _originalDialogues.TryGetValue((player.UniqueMultiplayerID, ((Character)npc).Name), out var value))
		{
			npc.CurrentDialogue.Clear();
			for (int num = value.Length - 1; num >= 0; num--)
			{
				npc.CurrentDialogue.Push(value[num]);
			}
			_originalDialogues.Remove((player.UniqueMultiplayerID, ((Character)npc).Name));
		}
	}

	private static void ClearDialogueSnapshots(Farmer player)
	{
		if (player == null)
		{
			return;
		}
		foreach (var item in _originalDialogues.Keys.Where(((long playerId, string npcName) key) => key.playerId == player.UniqueMultiplayerID).ToList())
		{
			_originalDialogues.Remove(item);
		}
	}

	private static bool IsFish(string fishId)
	{
		try
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
			return dataOrErrorItem != null && dataOrErrorItem.Category == -4;
		}
		catch
		{
			return false;
		}
	}
}
