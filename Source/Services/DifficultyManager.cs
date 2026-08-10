using System;
using System.Collections.Generic;
using System.Text.Json;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;

namespace FishingExpanded.Services
{
    /// <summary>难度等级管理器（BATCH-027：按玩家隔离，支持双人同屏与联机）</summary>
    public static class DifficultyManager
    {
        private const int MinDifficultyLevel = -10;
        private const int MaxDifficultyLevel = 100;
        private const string PlayerDataKey = "FishingExpanded/FishDifficultyData";
        private const string LegacySaveDataKey = "FishDifficultyData";

        /// <summary>BATCH-034: 手感进度 α 的分母 = 原生可计数真鱼 61 条（56 普通 + 5 原版传奇）。
        /// 清单核对 `_analysis\Fish-data-extracted.txt`（含 Goby；不含 10 蟹笼与 3 藻类；扩展传奇 898–902 按普通鱼计数）。
        /// Mod 鱼皇冠只显示、不计入 α。</summary>
        public const int CountableCrownTarget = 61;

        private static readonly HashSet<string> CountableFishIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "(O)128", "(O)129", "(O)130", "(O)131", "(O)132",
            "(O)136", "(O)137", "(O)138", "(O)139", "(O)140",
            "(O)141", "(O)142", "(O)143", "(O)144", "(O)145",
            "(O)146", "(O)147", "(O)148", "(O)149", "(O)150",
            "(O)151", "(O)154", "(O)155", "(O)156", "(O)158",
            "(O)159", "(O)160", "(O)161", "(O)162", "(O)163",
            "(O)164", "(O)165", "(O)267", "(O)269", "(O)682",
            "(O)698", "(O)699", "(O)700", "(O)701", "(O)702",
            "(O)704", "(O)705", "(O)706", "(O)707", "(O)708",
            "(O)734", "(O)775", "(O)795", "(O)796", "(O)798",
            "(O)799", "(O)800", "(O)836", "(O)837", "(O)838",
            "(O)898", "(O)899", "(O)900", "(O)901", "(O)902",
            "(O)Goby"
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // BATCH-027: 每位玩家独立缓存（键=UniqueMultiplayerID）。双人同屏每位本地玩家有独立 Game1 实例，
        // Game1.player 随屏幕切换；联机各客户端只操作本机玩家。难度/星标/加成一律按玩家隔离。
        private static readonly Dictionary<long, FishDifficultyData> _dataByPlayer =
            new Dictionary<long, FishDifficultyData>();

        /// <summary>加载指定玩家的存档数据（惰性；首次访问时执行）。</summary>
        public static void LoadData(Farmer player)
        {
            if (player == null)
                return;

            FishDifficultyData data = ReadFromModData(player);

            // 只让主玩家迁移旧的存档级数据，避免把旧共享进度复制给每个联机玩家。
            if (data == null && Context.IsMainPlayer)
            {
                try
                {
                    data = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>(LegacySaveDataKey);
                    if (data != null)
                    {
                        ModEntry.ModMonitor.Log(
                            "[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据",
                            LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }

            data ??= new FishDifficultyData();
            NormalizeData(data);
            _dataByPlayer[player.UniqueMultiplayerID] = data;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 加载玩家数据完成 | 玩家: {player.UniqueMultiplayerID} | 鱼种类数: {data.FishStatistics.Count} | " +
                $"皇冠鱼种: {data.CollectionStars.Count} | 可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                LogLevel.Info);
        }

        /// <summary>保存指定玩家存档数据（每次写入立即持久化到该玩家 modData）。</summary>
        public static void SaveData(Farmer player)
        {
            if (player == null || !_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out FishDifficultyData data))
                return;

            NormalizeData(data);
            player.modData[PlayerDataKey] = JsonSerializer.Serialize(data, JsonOptions);
            ModEntry.ModMonitor.Log(
                $"已保存玩家钓鱼难度数据 | 玩家: {player.UniqueMultiplayerID}",
                LogLevel.Debug);
        }

        /// <summary>保存所有已缓存玩家（Saving 兜底；正常写入路径已即时保存）。</summary>
        public static void SaveAll()
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (_dataByPlayer.ContainsKey(farmer.UniqueMultiplayerID))
                    SaveData(farmer);
            }
        }

        /// <summary>清除所有玩家数据缓存（返回标题/换存档时）。</summary>
        public static void UnloadData()
        {
            _dataByPlayer.Clear();
        }

        /// <summary>获取某个物品的难度上限（与玩家无关的静态规则）。</summary>
        public static int GetMaxDifficultyLevel(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId))
                return MaxDifficultyLevel;

            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(normalizedFishId);
                return itemData != null && Utils.SpecialFishHelper.IsNonFish(itemData.Category)
                    ? Utils.SpecialFishHelper.GetMaxLevelForNonFish()
                    : MaxDifficultyLevel;
            }
            catch
            {
                return MaxDifficultyLevel;
            }
        }

        /// <summary>获取指定玩家某种鱼的当前难度等级（BATCH-015: 支持非鱼类上限）</summary>
        public static int GetDifficultyLevel(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return 0;

            // 钳制到范围
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            return Math.Max(MinDifficultyLevel, Math.Min(maxLevel, stats.DifficultyLevel));
        }

        /// <summary>BATCH-010: 记录指定玩家钓鱼成功（支持可变等级增长）</summary>
        public static int RecordSuccess(string fishId, int levelGain, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", StardewModdingAPI.LogLevel.Error);
                return 0;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return 0;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            int nextRankCeiling = Math.Min(DifficultyCalculator.GetNextRankCeiling(oldLevel), maxLevel);
            int allowedGain = Math.Max(0, Math.Min(levelGain, nextRankCeiling - oldLevel));
            stats.SuccessCount = SaturatingAdd(stats.SuccessCount, allowedGain); // BATCH-010: 使用可变增长
            stats.ConsecutiveFailCount = 0; // BATCH-029: 成功清零连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"请求增长: +{levelGain} | 实际增长: +{allowedGain} | 成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData(player);
            return allowedGain;
        }

        /// <summary>记录指定玩家钓鱼失败</summary>
        public static void RecordFailure(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                ModEntry.ModMonitor.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            stats.FailCount = SaturatingAdd(stats.FailCount, 1);
            stats.ConsecutiveFailCount = SaturatingAdd(stats.ConsecutiveFailCount, 1); // BATCH-029: 连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | 连续失败次数: {stats.ConsecutiveFailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData(player);
        }

        /// <summary>记录达到difficulty≥120的鱼（按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (adjustedDifficulty >= 120f)
            {
                if (!data.CollectionStars.Contains(normalizedFishId))
                {
                    data.CollectionStars.Add(normalizedFishId);

                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                        $"调整后难度: {adjustedDifficulty:F1} | 可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                        LogLevel.Warn); // Warn级别确保显眼

                    SaveData(player);
                }
            }
        }

        /// <summary>测试工具（BATCH-032/034）：批量添加收藏皇冠。只从可计数原生普通鱼池（56 条，不含 5 条原版传奇）
        /// 挑选，跳过已加星鱼；返回实际添加数量。</summary>
        public static int AddCollectionStarsForTesting(Farmer player, int count)
        {
            if (player == null || count <= 0)
                return 0;

            FishDifficultyData data = GetData(player);
            if (data == null)
                return 0;

            int added = 0;
            foreach (string fishId in BuildStarPool())
            {
                if (added >= count)
                    break;
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                if (string.IsNullOrEmpty(normalizedFishId) ||
                    Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                    data.CollectionStars.Contains(normalizedFishId))
                {
                    continue;
                }
                data.CollectionStars.Add(normalizedFishId);
                added++;
            }

            if (added > 0)
            {
                SaveData(player);
            }
            return added;
        }

        /// <summary>BATCH-034: 测试皇冠池 = 可计数原生真鱼 61 条中的普通鱼（剔除 5 条原版传奇），
        /// 与 Mod 鱼/扩展传奇无关（Mod 鱼皇冠不计入手感进度 α）。</summary>
        private static List<string> BuildStarPool()
        {
            var pool = new List<string>(CountableFishIds.Count);
            foreach (string fishId in CountableFishIds)
            {
                if (!Utils.SpecialFishHelper.IsLegendaryFish(fishId))
                    pool.Add(fishId);
            }
            return pool;
        }

        /// <summary>BATCH-034: 是否属于可计数原生真鱼（计入手感进度 α；Mod 鱼/蟹笼/藻类不计）。</summary>
        public static bool IsCountableFish(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            return !string.IsNullOrEmpty(normalizedFishId) && CountableFishIds.Contains(normalizedFishId);
        }

        /// <summary>BATCH-034: 玩家已收集的可计数皇冠数（派生自 CollectionStars ∩ 原生 61 鱼，无独立持久状态）。</summary>
        public static int GetCountableCrownCount(Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
                return 0;

            int count = 0;
            foreach (string fishId in data.CollectionStars)
            {
                if (IsCountableFish(fishId))
                    count++;
            }
            return count;
        }


        /// <summary>BATCH-035: 玩家可计数皇冠鱼的鱼 ID 列表（CollectionStars ∩ 原生 61 鱼；Mod 鱼皇冠只显示、不参与助战）。</summary>
        public static List<string> GetCountableStarredFish(Farmer player)
        {
            var result = new List<string>();
            FishDifficultyData data = GetData(player);
            if (data == null)
                return result;

            foreach (string fishId in data.CollectionStars)
            {
                if (IsCountableFish(fishId))
                    result.Add(fishId);
            }
            return result;
        }
        /// <summary>BATCH-035: 助战鱼难度排位 r∈[0,1]（0=玩家可计数皇冠鱼中最低难度，1=最高难度；
        /// 全部相同或列表为空时取 0.5=均匀）。只派生自现有 CollectionStars ∩ 原生 61 鱼的难度数据，无新持久状态。</summary>
        public static double GetAssistRank(string fishId, Farmer player, List<string> countableStarred)
        {
            if (countableStarred == null || countableStarred.Count == 0)
                return 0.5;

            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (string id in countableStarred)
            {
                int level = GetDifficultyLevel(id, player);
                if (level < min) min = level;
                if (level > max) max = level;
            }
            if (max <= min)
                return 0.5;

            int fishLevel = GetDifficultyLevel(fishId, player);
            return Math.Max(0.0, Math.Min(1.0, (fishLevel - min) / (double)(max - min)));
        }
        /// <summary>BATCH-034: 手感进度 α = 可计数皇冠数 ÷ 61，钳制到 [0,1]（0=原生手感，1=终点手感）。</summary>
        public static float GetAlpha(Farmer player)
        {
            return Math.Min(1f, GetCountableCrownCount(player) / (float)CountableCrownTarget);
        }

        /// <summary>BATCH-034: 原版 5 条传奇鱼钓到一次直接给皇冠（计入可计数皇冠；无难度门槛）。
        /// 只在成功钓起边界（PullFishFromWater 传奇分支）调用，失败不调用。</summary>
        public static void RecordLegendaryCatch(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录传奇鱼皇冠", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 传奇鱼一次钓获皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                    StardewModdingAPI.LogLevel.Warn);
                SaveData(player);
            }
        }

        /// <summary>检查指定玩家某种鱼是否有收藏星标</summary>
        public static bool HasCollectionStar(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            return data != null && !string.IsNullOrEmpty(normalizedFishId) &&
                data.CollectionStars.Contains(normalizedFishId);
        }

        /// <summary>BATCH-038: 指定玩家某种鱼是否有挑战皇冠（挑战鱼饵 + 难度等级≥95 成功；鱼王/无数据返回 false）</summary>
        public static bool HasChallengeCrown(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            return data != null && !string.IsNullOrEmpty(normalizedFishId) &&
                data.ChallengeCrowns.Contains(normalizedFishId);
        }

        /// <summary>BATCH-038: 记录挑战皇冠（挑战鱼饵生效且难度等级≥95 的成功结算边界调用一次；鱼王不参与）</summary>
        public static void RecordChallengeCrown(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录挑战皇冠", StardewModdingAPI.LogLevel.Error);
                return;
            }

            if (data.ChallengeCrowns.Add(normalizedFishId))
            {
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 挑战皇冠记录 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"挑战皇冠数: {data.ChallengeCrowns.Count}",
                    StardewModdingAPI.LogLevel.Info);
                SaveData(player);
            }
        }

        /// <summary>BATCH-038: 测试工具：设置/清除指定鱼的挑战皇冠标记（写入存档，仅测试用）。</summary>
        public static void SetChallengeCrown(string fishId, bool value, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            FishDifficultyData data = GetData(player);
            if (data == null)
                return;

            if (value) data.ChallengeCrowns.Add(normalizedFishId);
            else data.ChallengeCrowns.Remove(normalizedFishId);
            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 已设置挑战皇冠 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | 值: {value}",
                StardewModdingAPI.LogLevel.Info);
            SaveData(player);
        }

        /// <summary>直接设置指定玩家某种鱼的难度等级（用于测试）</summary>
        public static void SetDifficultyLevel(string fishId, int targetLevel, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;
            targetLevel = Math.Max(MinDifficultyLevel, Math.Min(GetMaxDifficultyLevel(normalizedFishId), targetLevel));

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
            }

            // 通过调整成功次数来达到目标等级
            // 难度等级 = 成功次数 - 失败次数
            stats.SuccessCount = targetLevel + stats.FailCount;

            ModEntry.ModMonitor.Log(
                $"[DifficultyManager] 已设置等级 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | 目标等级: {targetLevel} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount}",
                StardewModdingAPI.LogLevel.Info);

            SaveData(player);
        }

        /// <summary>BATCH-029: 获取指定玩家某种鱼的连续失败次数（鱼王/无数据返回 0）</summary>
        public static int GetConsecutiveFailCount(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return 0;

            return Math.Max(0, stats.ConsecutiveFailCount);
        }

        /// <summary>获取指定玩家某种鱼的统计数据</summary>
        public static FishStats GetFishStats(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            if (data == null || string.IsNullOrEmpty(normalizedFishId) ||
                Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) ||
                !data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
                return null;

            return stats;
        }

        /// <summary>获取指定玩家所有鱼的统计数据</summary>
        public static System.Collections.Generic.Dictionary<string, FishStats> GetAllFishStats(Farmer player)
        {
            return GetData(player)?.FishStatistics ?? new System.Collections.Generic.Dictionary<string, FishStats>();
        }

        /// <summary>清空指定玩家所有数据（用于测试）</summary>
        public static void ClearAllData(Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            data.FishStatistics.Clear();
            data.CollectionStars.Clear();

            ModEntry.ModMonitor.Log("[DifficultyManager] 已清空所有数据", StardewModdingAPI.LogLevel.Info);
            SaveData(player);
        }

        /// <summary>记录高难度鱼（强制添加星标，用于测试；按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);

                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                    StardewModdingAPI.LogLevel.Warn);

                SaveData(player);
            }
            else
            {
                ModEntry.ModMonitor.Log(
                    $"[DifficultyManager] 鱼 {normalizedFishId} 已有收藏星标",
                    LogLevel.Info);
            }
        }

        /// <summary>获取指定玩家收藏皇冠总数（含 Mod 鱼与传奇鱼）</summary>
        public static int GetCollectionStarCount(Farmer player)
        {
            return GetData(player)?.CollectionStars.Count ?? 0;
        }

        /// <summary>获取指定玩家的难度数据（惰性加载并缓存）。</summary>
        private static FishDifficultyData GetData(Farmer player)
        {
            if (player == null)
                return null;

            if (!_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var data))
            {
                LoadData(player);
                _dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out data);
            }
            return data;
        }

        private static FishDifficultyData ReadFromModData(Farmer player)
        {
            if (player.modData.TryGetValue(PlayerDataKey, out string serializedData) &&
                !string.IsNullOrWhiteSpace(serializedData))
            {
                try
                {
                    return JsonSerializer.Deserialize<FishDifficultyData>(serializedData, JsonOptions);
                }
                catch (JsonException ex)
                {
                    ModEntry.ModMonitor.Log(
                        $"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {player.UniqueMultiplayerID} | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }
            return null;
        }

        private static void NormalizeData(FishDifficultyData data)
        {
            var normalizedStats = new Dictionary<string, FishStats>();
            foreach (var entry in data.FishStatistics ?? new Dictionary<string, FishStats>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(entry.Key);
                if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId) || entry.Value == null)
                    continue;

                if (!normalizedStats.TryGetValue(normalizedFishId, out var existing))
                {
                    normalizedStats[normalizedFishId] = entry.Value;
                    continue;
                }

                existing.SuccessCount = SaturatingAdd(existing.SuccessCount, entry.Value.SuccessCount);
                existing.FailCount = SaturatingAdd(existing.FailCount, entry.Value.FailCount);
            }

            var normalizedStars = new HashSet<string>();
            foreach (string fishId in data.CollectionStars ?? new HashSet<string>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                // BATCH-034: 传奇鱼皇冠合法（一次钓获），不再排除
                if (!string.IsNullOrEmpty(normalizedFishId))
                    normalizedStars.Add(normalizedFishId);
            }

            var normalizedChallengeCrowns = new HashSet<string>();
            foreach (string fishId in data.ChallengeCrowns ?? new HashSet<string>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                if (!string.IsNullOrEmpty(normalizedFishId) && !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                    normalizedChallengeCrowns.Add(normalizedFishId);
            }

            data.FishStatistics = normalizedStats;
            data.CollectionStars = normalizedStars;
            data.ChallengeCrowns = normalizedChallengeCrowns;
        }

        private static int SaturatingAdd(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : total < int.MinValue ? int.MinValue : (int)total;
        }
    }
}

