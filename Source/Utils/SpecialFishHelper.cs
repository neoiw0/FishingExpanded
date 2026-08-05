using System.Collections.Generic;

namespace FishingExpanded.Utils
{
    /// <summary>特殊鱼类识别工具（BATCH-014/015）</summary>
    public static class SpecialFishHelper
    {
        /// <summary>五大传奇鱼ID（不含扩展传奇鱼）</summary>
        private static readonly HashSet<string> LegendaryFishIds = new HashSet<string>
        {
            "(o)163", // 传奇鱼 Legend
            "(o)682", // 突变鲤鱼 Mutant Carp
            "(o)160", // 鮟鱇鱼 Angler
            "(o)775", // 冰川鱼 Glacierfish
            "(o)159"  // 赤红鱼 Crimsonfish
        };

        /// <summary>传奇鱼提示文案（随机选择）</summary>
        private static readonly string[] LegendaryMessages = new string[]
        {
            "嗷！孤傲的王！",
            "传说中的巨兽，终于现身！",
            "这是属于你的荣耀时刻！",
            "世间仅此一只，已入你囊中！",
            "山巅之王，俯首称臣！",
            "传说落幕，新的传奇诞生！",
            "你已成为渔夫中的传奇！",
            "王者归来，唯你独尊！",
            "这一刻，你是鱼，也是王！",
            "传说终结于此，荣耀属于你！"
        };

        /// <summary>检查是否为传奇鱼（鱼王）</summary>
        public static bool IsLegendaryFish(string fishId)
        {
            return LegendaryFishIds.Contains(fishId);
        }

        /// <summary>检查是否为非鱼类（垃圾/藻类等）</summary>
        /// <param name="itemCategory">物品Category</param>
        public static bool IsNonFish(int itemCategory)
        {
            return itemCategory != StardewValley.Object.FishCategory; // -4
        }

        /// <summary>获取非鱼类的等级上限</summary>
        public static int GetMaxLevelForNonFish()
        {
            return 8;
        }

        /// <summary>随机获取传奇鱼提示文案</summary>
        public static string GetRandomLegendaryMessage()
        {
            int index = StardewValley.Game1.random.Next(LegendaryMessages.Length);
            return LegendaryMessages[index];
        }
    }
}
