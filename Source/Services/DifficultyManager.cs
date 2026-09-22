using System;
using System.Collections.Generic;
using System.Text.Json;
using FishingExpanded.Data;
using FishingExpanded.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using FishingExpanded.Services;

namespace FishingExpanded.Services
{
    /// <summary>难度等级管理器（BATCH-027：按玩家隔离，支持双人同屏与联机）</summary>
    public static class DifficultyManager
    {
        private const int MinDifficultyLevel = -10;
        private const int MaxDifficultyLevel = 100;
        private const string PlayerDataKey = "FishingExpanded/FishDifficultyData";
        private const string LegacySaveDataKey = "FishDifficultyData";

        /// <summary>BATCH-034/039: 鱼竿熟练度 α 的分母 = 原生可计数真鱼 61 条（56 普通 + 5 原版传奇）。
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

        // BATCH-061: 每日收获限额（内存态，DayStarted 重置，零存档字段）——仅原版 61 可钓真鱼不限；
        // 其他（每种 Mod 鱼、每种非鱼类）按物品 ID 单独每天最多 333 个 + 333 对应经验。
        public const int DailyHarvestLimit = 333;
        private static readonly Dictionary<long, Dictionary<string, int>> _dailyHarvestByPlayer =
            new Dictionary<long, Dictionary<string, int>>();
        private static readonly Dictionary<long, HashSet<string>> _dailyLimitNotifiedByPlayer =
            new Dictionary<long, HashSet<string>>();

        /// <summary>BATCH-061: 是否为限鱼类（非原版 61 可钓真鱼 = Mod 鱼与非鱼类）。</summary>
        public static bool IsDailyHarvestLimited(string fishId)
        {
            string id = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            return !CountableFishIds.Contains(id);
        }

        /// <summary>BATCH-061: 消费一次当日收获（限鱼类）：返回该物品当日累计数（调用方判断 &gt; DailyHarvestLimit）。</summary>
        public static int ConsumeDailyHarvest(string fishId, Farmer player)
        {
            if (player == null)
                return 0;
            string id = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (!_dailyHarvestByPlayer.TryGetValue(player.UniqueMultiplayerID, out var map))
            {
                map = new Dictionary<string, int>();
                _dailyHarvestByPlayer[player.UniqueMultiplayerID] = map;
            }
            map.TryGetValue(id, out int count);
            count++;
            map[id] = count;
            return count;
        }

        /// <summary>BATCH-061: 标记某物品当日已提示过限额（返回 true=首次，调用方决定是否发提示）。</summary>
        public static bool MarkDailyLimitNotified(string fishId, Farmer player)
        {
            if (player == null)
                return false;
            string id = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (!_dailyLimitNotifiedByPlayer.TryGetValue(player.UniqueMultiplayerID, out var set))
            {
                set = new HashSet<string>();
                _dailyLimitNotifiedByPlayer[player.UniqueMultiplayerID] = set;
            }
            return set.Add(id);
        }

        /// <summary>BATCH-061: 新的一天开始，清空全部玩家当日收获计数与已提示集合。</summary>
        public static void ResetDailyHarvest()
        {
            _dailyHarvestByPlayer.Clear();
            _dailyLimitNotifiedByPlayer.Clear();
        }

        // BATCH-084: 持久战失败奖励每日限次（内存态，DayStarted 重置，零存档字段）。
        // 每位玩家两档独立计数：[0]=30~60 秒档（+1 食物），[1]=≥60 秒档（+2 食物）；各每天最多 1 次。
        public const int PerseveranceDailyCapPerTier = 1;
        private static readonly Dictionary<long, int[]> _perseveranceDailyByPlayer =
            new Dictionary<long, int[]>();

        /// <summary>BATCH-084: 消费一次持久战失败奖励当日额度（tier 0=30 秒档, 1=60 秒档；两档独立）。
        /// 当日该档已达上限返回 false（调用方跳过发放）。fish_persisttest 测试强制路径不得调用本方法。</summary>
        public static bool TryConsumePerseveranceDaily(int tier, Farmer player)
        {
            if (player == null || tier < 0 || tier > 1)
                return false;
            if (!_perseveranceDailyByPlayer.TryGetValue(player.UniqueMultiplayerID, out var counts))
            {
                counts = new int[2];
                _perseveranceDailyByPlayer[player.UniqueMultiplayerID] = counts;
            }
            if (counts[tier] >= PerseveranceDailyCapPerTier)
                return false;
            counts[tier]++;
            return true;
        }

        /// <summary>BATCH-084: 该玩家指定档位当日已发次数（供日志展示；tier 0=30 秒档, 1=60 秒档）。</summary>
        public static int GetPerseveranceDailyCount(int tier, Farmer player)
        {
            if (player == null || tier < 0 || tier > 1)
                return 0;
            return _perseveranceDailyByPlayer.TryGetValue(player.UniqueMultiplayerID, out var counts)
                ? counts[tier]
                : 0;
        }

        /// <summary>BATCH-084: 新的一天开始，清空持久战失败奖励当日计数。</summary>
        public static void ResetDailyPerseverance()
        {
            _perseveranceDailyByPlayer.Clear();
        }

        /// <summary>BATCH-084: 进日掷骰食物助战（本地玩家；掷骰结果立即写入存档防读档重抽）。
        /// 步骤：①窗口维护（窗口戳不同 → 清零已用量）；②按 p=(可计数皇冠÷61)×0.5 掷骰写状态；
        /// ③单条日志。由 FoodAssistService.OnDayStarted 调用（本管理器仍是 modData 唯一写入者）。</summary>
        public static void RollDailyFoodAssist(Farmer player)
        {
            if (player == null || !Context.IsWorldReady)
                return;

            FishDifficultyData data = GetData(player);
            if (data == null)
                return;

            // 窗口维护：周一开启窗口 1，周四开启窗口 2；跨窗口（含读档跨日）自动清零
            int stamp = DifficultyCalculator.GetFoodAssistWindowStamp(Game1.Date.TotalDays, Game1.Date.DayOfWeek);
            if (data.FoodAssistWindowStamp != stamp)
            {
                data.FoodAssistWindowStamp = stamp;
                data.FoodAssistWindowUsed = 0;
            }

            double chance = DifficultyCalculator.GetFoodAssistDailyChance(
                GetCountableCrownCount(player), CountableCrownTarget);
            bool hit = chance > 0.0 && Game1.random.NextDouble() < chance;
            data.FoodAssistDailyState = hit ? 2 : 1;
            SaveData(player);

            FishingLog.Log(
                $"[BATCH-084] 食物助战日掷骰 | 玩家: {player.UniqueMultiplayerID} | 可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget} | " +
                $"p={chance:P1} | 结果: {(hit ? "命中" : "未命中")} | 本窗口已用: {data.FoodAssistWindowUsed}/{DifficultyCalculator.FoodAssistWindowQuota}",
                LogLevel.Info);
        }

        /// <summary>BATCH-084: 消费当日食物助战命中（小游戏构造边界调用一次；仅本地玩家）。
        /// 条件：当日状态=2（命中未消费）且当前窗口有剩余额度。成功 → 状态置 3、窗口用量 +1 并立即存档。</summary>
        public static bool TryConsumeDailyFoodAssist(Farmer player)
        {
            if (player == null || !Context.IsWorldReady)
                return false;

            FishDifficultyData data = GetData(player);
            if (data == null)
                return false;

            // 读档/跨日兜底：状态有效但窗口戳过期时先维护（正常路径已在进日处理）
            int stamp = DifficultyCalculator.GetFoodAssistWindowStamp(Game1.Date.TotalDays, Game1.Date.DayOfWeek);
            if (data.FoodAssistWindowStamp != stamp)
            {
                data.FoodAssistWindowStamp = stamp;
                data.FoodAssistWindowUsed = 0;
            }

            if (data.FoodAssistDailyState != 2)
                return false;
            if (data.FoodAssistWindowUsed >= DifficultyCalculator.FoodAssistWindowQuota)
            {
                FishingLog.Log(
                    $"[BATCH-084] 食物助战命中被窗口上限拦截 | 玩家: {player.UniqueMultiplayerID} | 本窗口已用: {data.FoodAssistWindowUsed}/{DifficultyCalculator.FoodAssistWindowQuota}",
                    LogLevel.Info);
                return false;
            }

            data.FoodAssistDailyState = 3;
            data.FoodAssistWindowUsed++;
            SaveData(player);
            return true;
        }


        /// <summary>BATCH-078: 训练鱼竿有效声誉上限（临时有效值；不改写存档中超过上限的声誉）。</summary>
        public const int TrainingRodLevelCap = 4;

        /// <summary>BATCH-078: 无小游戏物品（非鱼类）每次收获的升级概率（原 100% 固定 +1 级改为概率制）。</summary>
        public const double NonFishLevelUpChance = 0.05;

        /// <summary>BATCH-078: 无小游戏物品升级掷签未中时，弹出轻量提示的概率（文案池 20 条通用文案）。
        /// BATCH-085（2026-09-22 用户定稿）：0.20 → 0.10；且该提示还须通过 `HUDNotifier` 的忙时闸门
        /// （该玩家屏幕上已有本模组提示或队列非空时直接不弹）才会真正显示。</summary>
        public const double NonFishLevelMissHintChance = 0.10;

        /// <summary>BATCH-078: 训练鱼竿下本次成功的等级增益是否会被封顶拦截
        /// （真实声誉 >4 时恒为 true；否则当"不加训练竿限制会升过 4"时为 true）。供调用方替换建议行提示。</summary>
        public static bool WouldTrainingCapTrigger(int realOldLevel, int requestedGain, bool isNonFishItem)
        {
            if (isNonFishItem || requestedGain <= 0)
                return false;
            int effectiveOld = Math.Min(realOldLevel, TrainingRodLevelCap);
            if (realOldLevel > TrainingRodLevelCap)
                return true;
            int uncappedAllowed = Math.Min(requestedGain, Math.Max(0,
                Math.Min(DifficultyCalculator.GetNextRankCeiling(realOldLevel), MaxDifficultyLevel) - realOldLevel));
            return effectiveOld + uncappedAllowed > TrainingRodLevelCap;
        }

        /// <summary>BATCH-061: 是否为非鱼类（物品类别 ≠ 鱼类别 -4；垃圾/藻类等；GAME-DESIGN §6.2）。
        /// BATCH-065: 蟹笼鱼（Data/Fish 带 trap 标签、Category=-4）按水藻类非鱼规则一并归入（上限 8、8 级星星、
        /// HUD 封顶文案、图鉴"无手感增强"全部自动生效）。</summary>
        public static bool IsNonFishItem(string fishId)
        {
            string id = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            var itemData = StardewValley.ItemRegistry.GetDataOrErrorItem(id);
            if (itemData == null)
                return false;
            if (Utils.SpecialFishHelper.IsNonFish(itemData.Category))
                return true;
            return IsCrabPotFish(id);
        }

        /// <summary>BATCH-065: 是否为蟹笼鱼（原生物品 Category=-4 但 Data/Fish 中带 "trap" 标签，
        /// 即只能从蟹笼获得的"鱼"：372/715-723 等）。判定口径与原生 `CrabPot.DayUpdate` 的
        /// `item.Value.Contains("trap")` 一致；其他 Mod 扩展的蟹笼鱼同样覆盖。</summary>
        public static bool IsCrabPotFish(string fishId)
        {
            string id = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            try
            {
                string rawId = id.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? id.Substring(3) : id;
                return DataLoader.Fish(Game1.content)
                    .TryGetValue(rawId, out string fishData) &&
                    fishData.Contains("trap", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"[DifficultyManager] 蟹笼鱼判定失败: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

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
                        FishingLog.Log(
                            "[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据",
                            LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    FishingLog.Log(
                        $"[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: {ex.Message}",
                        LogLevel.Error);
                }
            }

            data ??= new FishDifficultyData();
            NormalizeData(data);
            _dataByPlayer[player.UniqueMultiplayerID] = data;

            FishingLog.Log(
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
            FishingLog.Log(
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

        /// <summary>获取某个物品的难度上限（与玩家无关的静态规则）。
        /// BATCH-065: 蟹笼鱼经 IsNonFishItem 归入非鱼 → 上限 8。</summary>
        public static int GetMaxDifficultyLevel(string fishId)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId))
                return MaxDifficultyLevel;

            return IsNonFishItem(normalizedFishId)
                ? Utils.SpecialFishHelper.GetMaxLevelForNonFish()
                : MaxDifficultyLevel;
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

        /// <summary>BATCH-010: 记录指定玩家钓鱼成功（支持可变等级增长）。
        /// BATCH-078: grantLevel=false 时仍记一次成功事件（连续失败清零、星星检查照常），但等级不增长——
        /// 供无小游戏物品 5% 升级掷签未中路径使用，保持"成功统计唯一写入者"在本管理器。
        /// 注意 SuccessCount 沿用 BATCH-010"累计增长量"口径（+=allowedGain）：未中路径 allowedGain=0，
        /// 故计数不增加；这是既有口径而非本批引入的行为。</summary>
        public static int RecordSuccess(string fishId, int levelGain, Farmer player, bool grantLevel = true)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", StardewModdingAPI.LogLevel.Error);
                return 0;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return 0;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                FishingLog.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            int maxLevel = GetMaxDifficultyLevel(normalizedFishId);
            int nextRankCeiling = Math.Min(DifficultyCalculator.GetNextRankCeiling(oldLevel), maxLevel);
            int allowedGain = Math.Max(0, Math.Min(levelGain, nextRankCeiling - oldLevel));
            // BATCH-049/050: 89 级为“超高难挑战区间”入口——88 及以下最多只能升到 89；
            // BATCH-060（2026-08-15 用户确认）: 89 级以上（含 89）每次成功最多 +6 级（仍受 100 封顶约束）。
            if (oldLevel < 89)
                allowedGain = Math.Min(allowedGain, Math.Max(0, 89 - oldLevel));
            else
                allowedGain = Math.Min(allowedGain, 6);
            // BATCH-078: 掷签未中路径——照常记成功但不授予等级。
            if (!grantLevel)
                allowedGain = 0;
            stats.SuccessCount = SaturatingAdd(stats.SuccessCount, allowedGain); // BATCH-010: 使用可变增长
            stats.ConsecutiveFailCount = 0; // BATCH-029: 成功清零连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            // BATCH-061: 非鱼类（垃圾/藻类等）难度等级达到上限 8 级 → 获得星星（幂等；星星计入 CollectionStars
            // 但不进入 61 可计数池，不影响 α/助战；展示由 CollectionsPage 按类别画星星+"无手感增强"）。
            if (newLevel >= Utils.SpecialFishHelper.GetMaxLevelForNonFish() &&
                IsNonFishItem(normalizedFishId) &&
                !data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);
                FishingLog.Log(
                    $"[DifficultyManager] 非鱼类星星获得 | 玩家: {player.UniqueMultiplayerID} | 物品: {normalizedFishId} | 等级: {newLevel}",
                    LogLevel.Info);
            }

            FishingLog.Log(
                $"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"请求增长: +{levelGain} | 实际增长: +{allowedGain} | 成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}",
                LogLevel.Info);

            SaveData(player);
            return allowedGain;
        }

        /// <summary>记录指定玩家钓鱼失败。BATCH-058：挑战鱼饵失败不掉等级（keepLevel=true 时跳过 FailCount，
        /// 连续失败计数照常 +1，保留史诗提示与逃逸减速）。</summary>
        public static void RecordFailure(string fishId, Farmer player, bool keepLevel = false)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.FishStatistics.TryGetValue(normalizedFishId, out var stats))
            {
                stats = new FishStats();
                data.FishStatistics[normalizedFishId] = stats;
                FishingLog.Log($"[DifficultyManager] 新鱼种记录创建: {normalizedFishId}", LogLevel.Debug);
            }

            int oldLevel = GetDifficultyLevel(normalizedFishId, player);
            if (!keepLevel)
                stats.FailCount = SaturatingAdd(stats.FailCount, 1);
            stats.ConsecutiveFailCount = SaturatingAdd(stats.ConsecutiveFailCount, 1); // BATCH-029: 连续失败计数
            int newLevel = GetDifficultyLevel(normalizedFishId, player);

            FishingLog.Log(
                $"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                $"成功次数: {stats.SuccessCount} | 失败次数: {stats.FailCount} | 连续失败次数: {stats.ConsecutiveFailCount} | " +
                $"等级变化: {oldLevel} → {newLevel}" + (keepLevel ? " | 挑战鱼饵不掉等级" : ""),
                LogLevel.Info);

            SaveData(player);
        }

        /// <summary>BATCH-058: 获取挑战鱼饵背板种子（同鱼同等级在钓起前固定；缺失时生成并持久化）。</summary>
        public static int GetOrCreateChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null || player == null)
                return 0;

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            string key = normalizedFishId + "|" + difficultyLevel;
            if (data.ChallengePatternSeeds.TryGetValue(key, out int existing))
                return existing;

            int seed = new Random(Guid.NewGuid().GetHashCode()).Next();
            data.ChallengePatternSeeds[key] = seed;
            SaveData(player);
            FishingLog.Log(
                $"[DifficultyManager] 挑战背板种子生成 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | 等级: {difficultyLevel} | 种子: {seed}",
                LogLevel.Info);
            return seed;
        }

        /// <summary>BATCH-058: 挑战鱼饵成功钓起后删除背板种子（下次同鱼同等级重新随机）。</summary>
        public static void ClearChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null || player == null)
                return;

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            string key = normalizedFishId + "|" + difficultyLevel;
            if (data.ChallengePatternSeeds.Remove(key))
            {
                SaveData(player);
                FishingLog.Log(
                    $"[DifficultyManager] 挑战背板种子清除 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | 等级: {difficultyLevel}",
                    LogLevel.Info);
            }
        }

        /// <summary>记录达到difficulty≥120的鱼（按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", StardewModdingAPI.LogLevel.Error);
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

                    FishingLog.Log(
                        $"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                        $"调整后难度: {adjustedDifficulty:F1} | 可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                        LogLevel.Warn); // Warn级别确保显眼

                    SaveData(player);
                }
            }
        }

        /// <summary>测试工具（BATCH-032/034；BATCH-047 支持传奇）：批量添加收藏皇冠。
        /// 从完整可计数 61 鱼池（56 普通 + 5 原版传奇）挑选，跳过已加星鱼；返回实际添加数量。</summary>
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
                if (string.IsNullOrEmpty(normalizedFishId) || data.CollectionStars.Contains(normalizedFishId))
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

        /// <summary>BATCH-034/047: 测试皇冠池 = 完整可计数 61 鱼池（56 普通 + 5 原版传奇），
        /// 不含 Mod 鱼/蟹笼/藻类（Mod 鱼皇冠不计入鱼竿熟练度 α）。</summary>
        private static List<string> BuildStarPool()
        {
            var pool = new List<string>(CountableFishIds.Count);
            foreach (string fishId in CountableFishIds)
            {
                pool.Add(fishId);
            }
            return pool;
        }

        /// <summary>BATCH-034/039: 是否属于可计数原生真鱼（计入鱼竿熟练度 α；Mod 鱼/蟹笼/藻类不计）。</summary>
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

        /// <summary>BATCH-073: 玩家可计数皇冠鱼中拥有流动金色皇冠（挑战鱼饵 ≥95 级成功）的条数。</summary>
        public static int GetCountableFlowCrownCount(Farmer player)
        {
            int count = 0;
            foreach (string fishId in GetCountableStarredFish(player))
            {
                if (HasChallengeCrown(fishId, player))
                    count++;
            }
            return count;
        }

        /// <summary>BATCH-073: 玩家可计数皇冠鱼中拥有增大流动金色皇冠（100 级挑战成功，图鉴 1.2 倍）的条数。
        /// 这些鱼同时属于流动金色皇冠，因此在总概率加成中另外 +0.05%。</summary>
        public static int GetCountableLevel100FlowCrownCount(Farmer player)
        {
            int count = 0;
            foreach (string fishId in GetCountableStarredFish(player))
            {
                if (HasLevel100FlowCrown(fishId, player))
                    count++;
            }
            return count;
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
        /// <summary>BATCH-034/039: 鱼竿熟练度 α（皇冠数分段线性曲线，钳制到 [0,1]；0 皇冠=原生手感，61=终点手感）。</summary>
        public static float GetAlpha(Farmer player)
        {
            return GetAlphaFromCrowns(GetCountableCrownCount(player));
        }

        /// <summary>BATCH-039: α 曲线锚点（2026-08-10 用户确认）：0→0%、5→2%、10→7%、20→20%、30→35%、61→100%，
        /// 点位之间线性，超过 61 钳制 100%。独立纯函数供 fish_selftest 只读核验。</summary>
        public static float GetAlphaFromCrowns(int countableCrowns)
        {
            int c = Math.Max(0, countableCrowns);
            if (c >= CountableCrownTarget)
                return 1f;
            if (c <= 0)
                return 0f;
            if (c <= 5)
                return 0.02f * c / 5f;
            if (c <= 10)
                return 0.02f + 0.05f * (c - 5) / 5f;
            if (c <= 20)
                return 0.07f + 0.13f * (c - 10) / 10f;
            if (c <= 30)
                return 0.20f + 0.15f * (c - 20) / 10f;
            return 0.35f + 0.65f * (c - 30) / (CountableCrownTarget - 30f);
        }

        /// <summary>BATCH-034: 原版 5 条传奇鱼钓到一次直接给皇冠（计入可计数皇冠；无难度门槛）。
        /// 只在成功钓起边界（PullFishFromWater 传奇分支）调用，失败不调用。</summary>
        public static void RecordLegendaryCatch(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录传奇鱼皇冠", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);
                FishingLog.Log(
                    $"[DifficultyManager] ★ 传奇鱼一次钓获皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                    StardewModdingAPI.LogLevel.Warn);
                SaveData(player);
            }
        }

        /// <summary>BATCH-043: 装模组前已钓到的原版 5 条传奇鱼按“钓到一次直接给皇冠”补发。
        /// 只扫描本地玩家自己的 `Farmer.fishCaught`（键=限定 ID 如 (O)163，value[0]=累计钓获数>0）；
        /// 原生 fishCaught 不记录鱼塘钓获，天然与实时钓获规则一致。幂等：已有皇冠跳过，无变化不写。
        /// 由 OnSaveLoaded 在 LoadData 之后调用。</summary>
        public static void BackfillLegendaryCrowns(Farmer player)
        {
            if (player == null || !Context.IsWorldReady || player.fishCaught == null)
                return;

            FishDifficultyData data = GetData(player);
            if (data == null)
                return;

            bool changed = false;
            foreach (string legendaryId in Utils.SpecialFishHelper.GetLegendaryFishIds())
            {
                if (player.fishCaught.TryGetValue(legendaryId, out int[] counts) &&
                    counts != null && counts.Length > 0 && counts[0] > 0 &&
                    data.CollectionStars.Add(legendaryId))
                {
                    changed = true;
                    FishingLog.Log(
                        $"[DifficultyManager] ★ 传奇鱼回填皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {legendaryId} | 累计钓获: {counts[0]}",
                        LogLevel.Warn);
                }
            }

            if (changed)
                SaveData(player);
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
        public static void RecordChallengeCrown(string fishId, Farmer player, bool atLevel100 = false)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录挑战皇冠", StardewModdingAPI.LogLevel.Error);
                return;
            }

            bool added = data.ChallengeCrowns.Add(normalizedFishId);
            bool level100Added = atLevel100 && data.Level100FlowCrowns.Add(normalizedFishId);
            if (added || level100Added)
            {
                FishingLog.Log(
                    $"[DifficultyManager] ★ 挑战皇冠记录 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"挑战皇冠数: {data.ChallengeCrowns.Count} | 100级流动皇冠: {data.Level100FlowCrowns.Count}",
                    StardewModdingAPI.LogLevel.Info);
                SaveData(player);
            }
        }

        /// <summary>BATCH-048: 指定玩家某种鱼是否有 100 级流动金色皇冠（战胜 100 级鱼 + 挑战鱼饵成功）。</summary>
        public static bool HasLevel100FlowCrown(string fishId, Farmer player)
        {
            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            FishDifficultyData data = GetData(player);
            return data != null && !string.IsNullOrEmpty(normalizedFishId) &&
                data.Level100FlowCrowns.Contains(normalizedFishId);
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
            FishingLog.Log(
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
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
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

            FishingLog.Log(
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

        /// <summary>清空指定玩家所有挑战数据（GMCM 清除按钮与 fish_clear 共用；回到刚安装状态）。
        /// 清空难度统计/收藏星标/挑战皇冠；删除旧存档级键（仅主玩家）；立即写回空数据；
        /// BATCH-046：清空后立即按原生图鉴记录回填原版传奇皇冠（同会话生效，避免满皇冠状态失效）。</summary>
        public static void ClearAllData(Farmer player)
        {
            if (player == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家为null", LogLevel.Error);
                return;
            }

            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", LogLevel.Error);
                return;
            }

            int statsCount = data.FishStatistics.Count;
            int starCount = data.CollectionStars.Count;
            int crownCount = data.ChallengeCrowns.Count;
            int level100Count = data.Level100FlowCrowns.Count;

            data.FishStatistics.Clear();
            data.CollectionStars.Clear();
            data.ChallengeCrowns.Clear();
            data.Level100FlowCrowns.Clear();

            // BATCH-084: 食物助战状态一并清零（"回到刚安装状态"语义；当日掷骰作废，次日重新掷骰）
            data.FoodAssistDailyState = 0;
            data.FoodAssistWindowStamp = -1;
            data.FoodAssistWindowUsed = 0;

            // BATCH-046（方案 A，用户 2026-08-12 确认）：重置后立即回填原版 5 条传奇皇冠。
            BackfillLegendaryCrowns(player);

            bool legacyDeleted = false;
            if (Context.IsMainPlayer)
            {
                try
                {
                    // SMAPI 契约：WriteSaveData(key, null) 删除存档条目（官方文档确认）。
                    ModEntry.ModHelper.Data.WriteSaveData<FishDifficultyData>(LegacySaveDataKey, null);
                    legacyDeleted = true;
                }
                catch (Exception ex)
                {
                    FishingLog.Log($"[DifficultyManager] 删除旧存档级数据失败: {ex.Message}", LogLevel.Error);
                }
            }

            SaveData(player);

            FishingLog.Log(
                $"[DifficultyManager] 已清空所有数据 | 玩家: {player.UniqueMultiplayerID} | " +
                $"统计: {statsCount} | 星标: {starCount} | 挑战皇冠: {crownCount} | 100级流动皇冠: {level100Count} | 旧存档级键删除: {legacyDeleted}",
                LogLevel.Warn);
        }

        /// <summary>记录高难度鱼（强制添加星标，用于测试；按玩家）</summary>
        public static void RecordHighDifficulty(string fishId, Farmer player)
        {
            FishDifficultyData data = GetData(player);
            if (data == null)
            {
                FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", StardewModdingAPI.LogLevel.Error);
                return;
            }

            string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
            if (string.IsNullOrEmpty(normalizedFishId) || Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                return;

            if (!data.CollectionStars.Contains(normalizedFishId))
            {
                data.CollectionStars.Add(normalizedFishId);

                FishingLog.Log(
                    $"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {normalizedFishId} | " +
                    $"可计数皇冠: {GetCountableCrownCount(player)}/{CountableCrownTarget}",
                    StardewModdingAPI.LogLevel.Warn);

                SaveData(player);
            }
            else
            {
                FishingLog.Log(
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
                    FishingLog.Log(
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

            var normalizedLevel100 = new HashSet<string>();
            foreach (string fishId in data.Level100FlowCrowns ?? new HashSet<string>())
            {
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(fishId);
                if (!string.IsNullOrEmpty(normalizedFishId) && !Utils.SpecialFishHelper.IsLegendaryFish(normalizedFishId))
                    normalizedLevel100.Add(normalizedFishId);
            }

            data.FishStatistics = normalizedStats;
            data.CollectionStars = normalizedStars;
            data.ChallengeCrowns = normalizedChallengeCrowns;
            data.Level100FlowCrowns = normalizedLevel100;

            var normalizedSeeds = new Dictionary<string, int>();
            foreach (var kv in data.ChallengePatternSeeds ?? new Dictionary<string, int>())
            {
                string[] keyParts = kv.Key.Split('|');
                if (keyParts.Length != 2)
                    continue;
                string normalizedFishId = Utils.SpecialFishHelper.NormalizeItemId(keyParts[0]);
                if (!string.IsNullOrEmpty(normalizedFishId) && int.TryParse(keyParts[1], out int level))
                    normalizedSeeds[normalizedFishId + "|" + level] = kv.Value;
            }
            data.ChallengePatternSeeds = normalizedSeeds;
        }

        private static int SaturatingAdd(int left, int right)
        {
            long total = (long)left + right;
            return total > int.MaxValue ? int.MaxValue : total < int.MinValue ? int.MinValue : (int)total;
        }
    }
}

