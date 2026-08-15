using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Utils;

public static class SpecialFishHelper
{
	private static readonly HashSet<string> LegendaryFishIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "(O)163", "(O)682", "(O)160", "(O)775", "(O)159" };

	private static readonly string[] LegendaryMessages = new string[10] { "嗷！孤傲的王！", "传说中的巨兽，终于现身！", "这是属于你的荣耀时刻！", "世间仅此一只，已入你囊中！", "山巅之王，俯首称臣！", "传说落幕，新的传奇诞生！", "你已成为渔夫中的传奇！", "王者归来，唯你独尊！", "这一刻，你是鱼，也是王！", "传说终结于此，荣耀属于你！" };

	public static bool IsLegendaryFish(string fishId)
	{
		return LegendaryFishIds.Contains(NormalizeItemId(fishId));
	}

	public static IEnumerable<string> GetLegendaryFishIds()
	{
		return LegendaryFishIds;
	}

	public static string NormalizeItemId(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return itemId;
		}
		itemId = itemId.Trim();
		if (itemId.StartsWith("(o)", StringComparison.OrdinalIgnoreCase))
		{
			return "(O)" + itemId.Substring(3);
		}
		if (itemId.StartsWith("("))
		{
			return itemId;
		}
		return "(O)" + itemId;
	}

	public static bool IsNonFish(int itemCategory)
	{
		return itemCategory != -4;
	}

	public static int GetMaxLevelForNonFish()
	{
		return 8;
	}

	public static string GetRandomLegendaryMessage()
	{
		string[] array = LoadLegendaryMessages();
		int num = Game1.random.Next(array.Length);
		return array[num];
	}

	private static string[] LoadLegendaryMessages()
	{
		string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.legendary.messages"));
		if (string.IsNullOrWhiteSpace(text) || text == "hud.legendary.messages")
		{
			return LegendaryMessages;
		}
		string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			return array;
		}
		return LegendaryMessages;
	}
}
