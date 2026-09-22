using System;
using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;
using FishingExpanded.Utils;

namespace FishingExpanded.Services
{
    /// <summary>BATCH-084 食物助战奖励描述（构造边界 Prefix 发放后传递给 Postfix 展示）。</summary>
    public class FoodAssistReward
    {
        /// <summary>发放的物品限定 ID（如 (O)242）</summary>
        public string ItemId { get; set; }
        /// <summary>钓鱼 buff 档位（3 或 4，用于补差比较与日志）</summary>
        public int Tier { get; set; }
        /// <summary>物品显示名（文案用；发放时解析）</summary>
        public string DisplayName { get; set; }
    }

    /// <summary>BATCH-084: 食物助战服务——奖励池解析、发放与补差 buff。
    /// 职责边界：掷骰/窗口/存档状态全部归 DifficultyManager（modData 唯一写入者）；
    /// 本服务只负责"命中之后发什么、怎么发"，以及按不变英文名经 Game1.objectData 运行期解析物品 ID
    /// （每次启动自动核验当前安装数据；解析失败剔除该物品并记日志，不硬编码待核实 ID）。</summary>
    internal static class FoodAssistService
    {
        /// <summary>REQ-A 30~60 秒档池：随机 +1 钓鱼食物（本地 wiki 镜像 2026-08-24 核实效果与时长）。</summary>
        private static readonly string[] Tier1FoodNames = { "Trout Soup", "Shrimp Cocktail", "Maple Bar" };

        /// <summary>REQ-A ≥60 秒档池：随机 +2 钓鱼食物。</summary>
        private static readonly string[] Tier2FoodNames = { "Escargot", "Fish Taco" };

        /// <summary>REQ-B 70% 档池：+3 钓鱼料理（现行代码已证实的三个 ID，BATCH-052 口径沿用）。</summary>
        private static readonly string[] RewardPlusThreeIds = { "(O)242", "(O)728", "(O)730" };

        /// <summary>REQ-B 30% 档：海泡布丁 (+4 钓鱼)。</summary>
        private const string SeafoamPuddingId = "(O)265";

        /// <summary>REQ-B 命中时 +3 料理的概率（70%），其余 30% 为海泡布丁（用户确认）。</summary>
        public const double PlusThreeChance = 0.7;

        private static Dictionary<string, string> _nameToQualifiedId;
        private static bool _resolveAttempted;

        /// <summary>池是否可用（至少一个名字解析成功）。解析失败会记一次日志。</summary>
        public static bool IsAvailable
        {
            get
            {
                EnsureResolved();
                return _resolvedTier1.Count > 0 && _resolvedTier2.Count > 0 && _rewardPlusThree.Count > 2;
            }
        }

        private static readonly List<string> _resolvedTier1 = new List<string>();
        private static readonly List<string> _resolvedTier2 = new List<string>();
        private static readonly List<string> _rewardPlusThree = new List<string>(RewardPlusThreeIds);

        /// <summary>按不变英文名从 Game1.objectData 解析物品限定 ID（每次进程只解析一次；
        /// 游戏数据在内容加载后不变）。找不到的条目剔除并记 Warn。</summary>
        private static void EnsureResolved()
        {
            if (_resolveAttempted)
                return;
            _resolveAttempted = true;

            try
            {
                _nameToQualifiedId = new Dictionary<string, string>(StringComparer.Ordinal);
                if (Game1.objectData != null)
                {
                    foreach (var kv in Game1.objectData)
                    {
                        string name = kv.Value?.Name;
                        if (!string.IsNullOrEmpty(name) && !_nameToQualifiedNameExists(name))
                            _nameToQualifiedId[name] = "(O)" + kv.Key;
                    }
                }

                ResolveInto(Tier1FoodNames, _resolvedTier1, "30秒档(+1)");
                ResolveInto(Tier2FoodNames, _resolvedTier2, "60秒档(+2)");

                FishingLog.Log(
                    $"[BATCH-084] 奖励池解析完成 | 30秒档: {string.Join("/", Describe(_resolvedTier1))} | " +
                    $"60秒档: {string.Join("/", Describe(_resolvedTier2))} | " +
                    $"+3档: {string.Join("/", _rewardPlusThree)} | 海泡布丁: {SeafoamPuddingId}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[BATCH-084] 奖励池解析失败（食物助战与持久战新奖励将不可用）: {ex.Message}", LogLevel.Error);
            }
        }

        private static bool _nameToQualifiedNameExists(string name) => _nameToQualifiedId.ContainsKey(name);

        private static void ResolveInto(string[] names, List<string> target, string label)
        {
            foreach (string name in names)
            {
                if (_nameToQualifiedId.TryGetValue(name, out string id))
                {
                    target.Add(id);
                }
                else
                {
                    FishingLog.Log($"[BATCH-084] 物品名解析失败，已从{label}池剔除: {name}", LogLevel.Warn);
                }
            }
        }

        private static string Describe(List<string> ids)
        {
            var list = new List<string>();
            foreach (string id in ids)
                list.Add($"{ItemName(id)}({id})");
            return string.Join("/", list);
        }

        private static string ItemName(string itemId)
        {
            try
            {
                return ItemRegistry.Create(itemId)?.DisplayName ?? itemId;
            }
            catch
            {
                return itemId;
            }
        }

        /// <summary>REQ-A: 从指定档位池均匀随机取一个物品 ID（池空返回 null）。</summary>
        public static string PickPerseveranceReward(int tier)
        {
            EnsureResolved();
            List<string> pool = tier == 0 ? _resolvedTier1 : tier == 1 ? _resolvedTier2 : null;
            if (pool == null || pool.Count == 0)
                return null;
            int index = Game1.random.Next(pool.Count);
            return pool[index];
        }

        /// <summary>REQ-B: 小游戏构造边界（原生计算绿条高度之前）调用——消费当日命中并应用补差 buff。
        /// force=true 时为 fish_foodassist 测试强制：不消费存档状态、不占窗口额度。
        /// 物品本体不在此处入包（避免背包满时的溢出菜单与原生菜单赋值竞争），由调用方在首个 update Tick
        /// 经 addItemByMenuIfNecessary 发放——与本模组持久战奖励既定发放路径同一语义。
        /// 返回奖励描述供后续发放与展示；null=未触发（未命中/无额度/池不可用）。</summary>
        public static FoodAssistReward TryGrantAtConstruction(Farmer player, bool force)
        {
            try
            {
                if (player == null || !player.IsLocalPlayer || !Context.IsWorldReady)
                    return null;
                if (!IsAvailable)
                    return null;
                // 测试强制路径不读/不写存档掷骰状态；正常路径必须成功消费当日命中
                if (!force && !DifficultyManager.TryConsumeDailyFoodAssist(player))
                    return null;

                // 70% +3 料理 / 30% 海泡布丁
                bool plusThree = Game1.random.NextDouble() < PlusThreeChance;
                string itemId = plusThree
                    ? _rewardPlusThree[Game1.random.Next(_rewardPlusThree.Count)]
                    : SeafoamPuddingId;
                int tier = plusThree ? 3 : 4;

                // 补差 buff（范围 A）：仅当当前食物钓鱼 buff 低于奖励档位时应用该食物的完整原生食物 buff 组；
                // 应用走 applyBuff 原生语义（id="food" 自动替换旧食物 buff；§7.6.1 小游戏暂停机制自然生效）
                string buffResult = "无需(已有同级或更强)";
                Item item = ItemRegistry.Create(itemId);
                try
                {
                    int currentFoodFishing = GetCurrentFoodFishingBuff(player);
                    if (currentFoodFishing < tier)
                    {
                        int appliedCount = ApplyNativeFoodBuffs(item, player);
                        buffResult = appliedCount > 0 ? $"已应用({currentFoodFishing}→{tier})" : "无原生食物buff";
                    }
                }
                catch (Exception ex)
                {
                    buffResult = "应用失败";
                    FishingLog.Log($"[BATCH-084] 补差 buff 应用失败: {ex}", LogLevel.Warn);
                }

                var reward = new FoodAssistReward { ItemId = itemId, Tier = tier, DisplayName = item?.DisplayName ?? itemId };
                FishingLog.Log(
                    $"[BATCH-084] 食物助战命中(物品待入包) | 玩家: {player.UniqueMultiplayerID} | 物品: {reward.DisplayName}({itemId}) | " +
                    $"档位: +{tier} | 补差buff: {buffResult}{(force ? " | 测试强制(不计额度)" : "")}",
                    LogLevel.Info);
                return reward;
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[BATCH-084] 食物助战发放失败: {ex}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>当前玩家食物类(id="food")钓鱼 buff 合计（不含饮料/装备/助战临时等级）。</summary>
        private static int GetCurrentFoodFishingBuff(Farmer player)
        {
            var applied = player.buffs?.AppliedBuffs;
            if (applied != null && applied.TryGetValue("food", out Buff foodBuff) && foodBuff?.effects != null)
                return (int)System.Math.Round(foodBuff.effects.FishingLevel.Value);
            return 0;
        }

        /// <summary>按原生物品数据创建并应用食物 buff 组（与食用同一来源 GetFoodOrDrinkBuffs；
        /// 普通品质 durationMultiplier=1，时长由 Data/Objects 原生定义换算）。</summary>
        private static int ApplyNativeFoodBuffs(Item item, Farmer player)
        {
            if (!(item is StardewValley.Object obj))
                return 0;
            int count = 0;
            foreach (Buff buff in obj.GetFoodOrDrinkBuffs())
            {
                player.applyBuff(buff);
                count++;
            }
            return count;
        }

        /// <summary>进日处理（ModEntry.OnDayStarted 调用）：窗口维护 + 掷骰 + 写档。</summary>
        public static void OnDayStarted(Farmer player)
        {
            DifficultyManager.RollDailyFoodAssist(player);
        }

        /// <summary>REQ-B 食物助战文案（10 条随机；i18n hud.foodassist.1~10）。
        /// BATCH-084A 用户口径（2026-08-24）：{{fishName}}=从玩家已获得皇冠的鱼（GetCountableStarredFish，
        /// 与皇冠助战同一列表）中随机一种的显示名；无皇冠鱼时回退纯食物短文案。</summary>
        public static string PickFoodAssistText(Farmer player, string foodDisplayName)
        {
            try
            {
                string fishName = PickCrownedFishName(player);
                if (string.IsNullOrWhiteSpace(fishName))
                    return $"{foodDisplayName}！";
                int index = Game1.random.Next(1, 11);
                string key = $"hud.foodassist.{index}";
                string text = ModEntry.ModHelper.Translation.Get(key, new { food = foodDisplayName, fishName });
                return string.IsNullOrWhiteSpace(text) || text == key
                    ? $"{foodDisplayName}！"
                    : text;
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[BATCH-084] 食物助战文案生成失败: {ex.Message}", LogLevel.Warn);
                return foodDisplayName ?? string.Empty;
            }
        }

        /// <summary>从玩家已获皇冠的可计数鱼中随机取一种显示名；空列表返回 null。</summary>
        private static string PickCrownedFishName(Farmer player)
        {
            List<string> crowned = DifficultyManager.GetCountableStarredFish(player);
            if (crowned == null || crowned.Count == 0)
                return null;
            string fishId = crowned[Game1.random.Next(crowned.Count)];
            var itemData = StardewValley.ItemRegistry.GetDataOrErrorItem(fishId);
            string name = itemData?.DisplayName;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
