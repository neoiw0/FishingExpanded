using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using FishingExpanded.Services;

namespace FishingExpanded
{
    /// <summary>Mod入口类</summary>
    public class ModEntry : Mod
    {
        /// <summary>Mod唯一实例</summary>
        public static ModEntry Instance { get; private set; }

        // BATCH-035: 测试命令 fish_assist 强制下一次钓鱼小游戏触发助战（薄控制，消费后清除）
        private static bool _forceAssistNext;

        /// <summary>SMAPI Helper</summary>
        public static IModHelper ModHelper => Instance.Helper;

        /// <summary>监视器</summary>
        public static IMonitor ModMonitor => Instance.Monitor;

        // 性能优化：使用计数器替代模运算
        private int _npcCheckCounter = 0;
        private int _cleanupCounter = 0;
        private int _hudQueueCounter = 0; // BATCH-032: HUD 提示队列驱动计数器

        /// <summary>Mod入口点</summary>
        /// <param name="helper">SMAPI Helper</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Monitor.Log("FishingExpanded 正在初始化...", LogLevel.Info);

            try
            {
                // 注册 Harmony Patches
                var harmony = new Harmony(ModManifest.UniqueID);
                harmony.PatchAll();
                Monitor.Log("Harmony Patches 注册成功", LogLevel.Debug);

                // 注册事件监听器
                helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
                helper.Events.GameLoop.Saving += OnSaving;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                helper.Events.GameLoop.DayStarted += OnDayStarted;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                helper.Events.Player.Warped += OnWarped;

                // 注册控制台命令
                RegisterConsoleCommands();

                Monitor.Log("FishingExpanded 初始化完成", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"初始化失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>存档加载后</summary>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            DifficultyManager.LoadData(Game1.player);
            HUDNotifier.ClearPending(); // BATCH-032: 加载存档时清空瞬时提示队列
            GiantFishManager.ResetForSave();
            Patches.ObjectPatches.ResetVisualDiagnostics();
            Monitor.Log("=== FishingExpanded 存档加载完成 ===", LogLevel.Info);
        }

        /// <summary>保存前</summary>
        private void OnSaving(object sender, SavingEventArgs e)
        {
            DifficultyManager.SaveAll();
            Monitor.Log("已保存钓鱼难度数据", LogLevel.Debug);
        }

        /// <summary>返回标题时清除当前玩家缓存，避免联机换存档串写</summary>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            DifficultyManager.UnloadData();
            HUDNotifier.ClearPending(); // BATCH-032: 返回标题清空瞬时提示队列
            Patches.FishingRodPatches.ClearPending();
            GiantFishManager.ResetForSave();
            Patches.ObjectPatches.ResetVisualDiagnostics();
            Monitor.Log("已清除 FishingExpanded 当前玩家缓存", LogLevel.Debug);
        }

        /// <summary>每日开始</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Monitor.Log($"=== FishingExpanded 新的一天开始 (Day {Game1.dayOfMonth}) ===", LogLevel.Info);
            GiantFishManager.OnDayStarted();
        }

        /// <summary>每帧更新（性能优化：使用计数器，增加检查间隔）</summary>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // BATCH-032: 每 15 tick（约 0.25 秒）驱动 HUD 提示队列（前一条消失后显示下一条）
            _hudQueueCounter++;
            if (_hudQueueCounter >= 15)
            {
                _hudQueueCounter = 0;
                HUDNotifier.ProcessQueue();
            }

            // 性能优化：NPC检查从每30帧改为每60帧（从每秒2次降为1次）
            _npcCheckCounter++;
            if (_npcCheckCounter >= 60)
            {
                _npcCheckCounter = 0;
                GiantFishManager.CheckAndTriggerNPCReactions();
            }

            // 每5秒清理一次BobberBar实例数据（防止内存泄漏）
            _cleanupCounter++;
            if (_cleanupCounter >= 300)
            {
                _cleanupCounter = 0;
                Patches.BobberBarPatches.PeriodicCleanup();
            }
        }

        /// <summary>玩家传送</summary>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // 检查是否进入FarmHouse
            if (e.NewLocation is StardewValley.Locations.FarmHouse)
            {
                Monitor.Log(
                    $"[ModEntry] 玩家进入FarmHouse: {e.Player?.UniqueMultiplayerID} | 地点: {e.NewLocation.Name}",
                    LogLevel.Debug);
                GiantFishManager.OnEnterFarmHouse(e.Player);
            }
        }

        #region 控制台命令

        /// <summary>注册控制台命令</summary>
        private void RegisterConsoleCommands()
        {
            Helper.ConsoleCommands.Add("fish_setlevel",
                "设置某鱼的难度等级 | 用法: fish_setlevel <鱼ID> <等级>",
                OnCommandSetLevel);

            Helper.ConsoleCommands.Add("fish_addsuccess",
                "增加成功次数 | 用法: fish_addsuccess <鱼ID> <次数>",
                OnCommandAddSuccess);

            Helper.ConsoleCommands.Add("fish_addfail",
                "增加失败次数 | 用法: fish_addfail <鱼ID> <次数>",
                OnCommandAddFail);

            Helper.ConsoleCommands.Add("fish_info",
                "查看鱼的详细信息 | 用法: fish_info <鱼ID>",
                OnCommandInfo);

            Helper.ConsoleCommands.Add("fish_list",
                "列出所有已记录的鱼",
                OnCommandList);

            Helper.ConsoleCommands.Add("fish_clear",
                "清空所有钓鱼数据 | 用法: fish_clear confirm",
                OnCommandClear);

            Helper.ConsoleCommands.Add("fish_addstar",
                "添加收藏皇冠（测试）| 用法: fish_addstar <鱼ID>",
                OnCommandAddStar);

            Helper.ConsoleCommands.Add("fish_giant",
                "模拟巨型鱼（触发NPC反应）| 用法: fish_giant <鱼ID> <倍数>",
                OnCommandGiant);

            Helper.ConsoleCommands.Add("fish_bonus",
                "查看收藏皇冠与手感进度 | 用法: fish_bonus",
                OnCommandBonus);

                        Helper.ConsoleCommands.Add("fish_assist",
                "强制下一次钓鱼小游戏触发助战（测试）| 用法: fish_assist",
                OnCommandAssist);

            Helper.ConsoleCommands.Add("fish_selftest",
                "BATCH-035 自动自测（概率/可计数/排位/分布/i18n/强制标志）| 用法: fish_selftest",
                OnCommandSelfTest);

            Helper.ConsoleCommands.Add("fish_assiststats",
                "查看/清空助战观测统计（会话内内存，上限500）| 用法: fish_assiststats [clear]",
                OnCommandAssistStats);

Helper.ConsoleCommands.Add("fish_addstars",
                "批量添加收藏皇冠（可计数原生普通鱼池，提升手感进度）| 用法: fish_addstars <数量>",
                OnCommandAddStars);

            Helper.ConsoleCommands.Add("fish_challengecrown",
                "设置挑战鱼饵流动金色皇冠标记（测试）| 用法: fish_challengecrown <鱼ID> [0|1]",
                OnCommandChallengeCrown);

            Monitor.Log("控制台命令注册完成 (输入 help 查看所有命令)", LogLevel.Debug);
        }

        private void OnCommandSetLevel(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("用法: fish_setlevel <鱼ID> <等级>", LogLevel.Info);
                Monitor.Log("例如: fish_setlevel 128 50  (设置河豚难度为50)", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            if (!int.TryParse(args[1], out int level))
            {
                Monitor.Log("等级必须是数字！", LogLevel.Error);
                return;
            }

            DifficultyManager.SetDifficultyLevel(fishId, level, Game1.player);
            int actualLevel = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
            Monitor.Log($"✓ 已设置 {fishId} 的难度等级为 {actualLevel}", LogLevel.Info);
        }

        private void OnCommandAddSuccess(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("用法: fish_addsuccess <鱼ID> <次数>", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            if (!int.TryParse(args[1], out int count) || count <= 0)
            {
                Monitor.Log("次数必须是正整数！", LogLevel.Error);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                DifficultyManager.RecordSuccess(fishId, 1, Game1.player);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
            Monitor.Log($"✓ 已为 {fishId} 增加 {count} 次成功，当前难度等级: {level}", LogLevel.Info);
        }

        private void OnCommandAddFail(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("用法: fish_addfail <鱼ID> <次数>", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            if (!int.TryParse(args[1], out int count) || count <= 0)
            {
                Monitor.Log("次数必须是正整数！", LogLevel.Error);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                DifficultyManager.RecordFailure(fishId, Game1.player);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
            Monitor.Log($"✓ 已为 {fishId} 增加 {count} 次失败，当前难度等级: {level}", LogLevel.Info);
        }

        private void OnCommandInfo(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Monitor.Log("用法: fish_info <鱼ID>", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            var stats = DifficultyManager.GetFishStats(fishId, Game1.player);

            if (stats == null)
            {
                Monitor.Log($"鱼 {fishId} 尚未记录任何数据", LogLevel.Info);
                return;
            }

            int level = stats.DifficultyLevel;
            bool hasStar = DifficultyManager.HasCollectionStar(fishId, Game1.player);

            Monitor.Log("===========================================", LogLevel.Info);
            Monitor.Log($"鱼ID: {fishId}", LogLevel.Info);
            Monitor.Log($"成功次数: {stats.SuccessCount}", LogLevel.Info);
            Monitor.Log($"失败次数: {stats.FailCount}", LogLevel.Info);
            Monitor.Log($"难度等级: {level} ({GetRankName(level)})", LogLevel.Info);
            Monitor.Log($"难度倍数: {Utils.DifficultyCalculator.GetDifficultyMultiplier(level):F2}x", LogLevel.Info);
            Monitor.Log($"数量倍数: {Utils.DifficultyCalculator.GetQuantityMultiplier(level)}x", LogLevel.Info);
            Monitor.Log($"品质加成: +{Utils.DifficultyCalculator.GetQualityBonus(level)}", LogLevel.Info);
            Monitor.Log($"收藏星标: {(hasStar ? "★ 已获得" : "未获得")}", LogLevel.Info);
            Monitor.Log("===========================================", LogLevel.Info);
        }

        private void OnCommandList(string command, string[] args)
        {
            var allStats = DifficultyManager.GetAllFishStats(Game1.player);

            if (allStats.Count == 0)
            {
                Monitor.Log("当前没有任何钓鱼记录", LogLevel.Info);
                return;
            }

            Monitor.Log($"===== 已记录的鱼种（共 {allStats.Count} 种）=====", LogLevel.Info);
            foreach (var kvp in allStats)
            {
                string fishId = kvp.Key;
                var stats = kvp.Value;
                int level = stats.DifficultyLevel;
                bool hasStar = DifficultyManager.HasCollectionStar(fishId, Game1.player);
                string starMark = hasStar ? " ★" : "";

                Monitor.Log($"  {fishId}: 等级={level} ({GetRankName(level)}), " +
                           $"成功={stats.SuccessCount}, 失败={stats.FailCount}{starMark}",
                           LogLevel.Info);
            }
            Monitor.Log("===========================================", LogLevel.Info);
        }

        private void OnCommandClear(string command, string[] args)
        {
            if (args.Length < 1 || args[0] != "confirm")
            {
                Monitor.Log("⚠ 此操作将清空所有钓鱼数据！", LogLevel.Warn);
                Monitor.Log("如需确认，请输入: fish_clear confirm", LogLevel.Info);
                return;
            }

            DifficultyManager.ClearAllData(Game1.player);
            Monitor.Log("✓ 已清空所有钓鱼数据", LogLevel.Info);
        }

        private void OnCommandAddStar(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Monitor.Log("用法: fish_addstar <鱼ID>", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            DifficultyManager.RecordHighDifficulty(fishId, Game1.player);
            Monitor.Log($"✓ 已为 {fishId} 添加收藏皇冠", LogLevel.Info);
        }

        private void OnCommandGiant(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("用法: fish_giant <鱼ID> <倍数>", LogLevel.Info);
                Monitor.Log("例如: fish_giant 128 20  (模拟20倍河豚)", LogLevel.Info);
                return;
            }

            string fishId = args[0];
            if (!int.TryParse(args[1], out int multiplier) || multiplier <= 0)
            {
                Monitor.Log("倍数必须是正整数！", LogLevel.Error);
                return;
            }

            GiantFishManager.RecordGiantFish(fishId, multiplier, multiplier * 20);
            Monitor.Log($"✓ 已记录巨型鱼 {fishId} (倍数={multiplier})，等待触发NPC反应", LogLevel.Info);
            Monitor.Log("提示: 将鱼拿在手上并靠近NPC即可触发反应", LogLevel.Info);
        }

        /// <summary>批量添加收藏皇冠（只从可计数原生普通鱼池挑选，BATCH-034：Mod 鱼皇冠不计手感进度）。</summary>
        private void OnCommandAddStars(string command, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int count) || count <= 0)
            {
                Monitor.Log("用法: fish_addstars <数量>", LogLevel.Info);
                Monitor.Log("例如: fish_addstars 20  (一次加20颗可计数皇冠)", LogLevel.Info);
                return;
            }

            int added = DifficultyManager.AddCollectionStarsForTesting(Game1.player, count);
            int countable = DifficultyManager.GetCountableCrownCount(Game1.player);
            float alpha = DifficultyManager.GetAlpha(Game1.player);

            Monitor.Log($"✓ 已添加 {added} 颗收藏皇冠 (目标 {count})", LogLevel.Info);
            Monitor.Log($"可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 手感进度: {alpha:P0}", LogLevel.Info);
            double assistChance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            Monitor.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", LogLevel.Info);
            if (added < count)
            {
                Monitor.Log("提示: 可计数原生普通鱼池皇冠已用完（56 条，不含传奇与 Mod 鱼）；可先 fish_clear confirm 清空后重新添加", LogLevel.Warn);
            }
        }

        private void OnCommandBonus(string command, string[] args)
        {
            int starCount = DifficultyManager.GetCollectionStarCount(Game1.player);
            int countable = DifficultyManager.GetCountableCrownCount(Game1.player);
            float alpha = DifficultyManager.GetAlpha(Game1.player);

            Monitor.Log("===========================================", LogLevel.Info);
            Monitor.Log($"收藏皇冠总数: {starCount}", LogLevel.Info);
            Monitor.Log($"可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 手感进度: {alpha:P0}", LogLevel.Info);
            double assistChance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            Monitor.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", LogLevel.Info);
            Monitor.Log("===========================================", LogLevel.Info);
        }

        /// <summary>BATCH-035: 消费强制助战标志（BobberBar 构造边界调用；一次消费后清除）。</summary>
        public static bool ConsumeForceAssistFlag()
        {
            bool forced = _forceAssistNext;
            _forceAssistNext = false;
            return forced;
        }

        /// <summary>BATCH-035: 强制下一次钓鱼小游戏触发助战（薄控制测试命令，不写存档）。</summary>
        /// <summary>BATCH-038: 设置/清除挑战鱼饵流动金色皇冠标记（测试；写存档同生产边界）。</summary>
        private void OnCommandChallengeCrown(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Monitor.Log("用法: fish_challengecrown <鱼ID> [0|1]", LogLevel.Info);
                Monitor.Log("例如: fish_challengecrown 144 1  (设置狗鱼为流动金色皇冠)", LogLevel.Info);
                return;
            }

            bool setValue = args.Length < 2 || args[1] != "0";
            string fishId = args[0];
            DifficultyManager.SetChallengeCrown(fishId, setValue, Game1.player);
            bool now = DifficultyManager.HasChallengeCrown(fishId, Game1.player);
            Monitor.Log(
                $"✓ 已{(setValue ? "设置" : "清除")} {fishId} 的挑战鱼饵流动金色皇冠标记（当前: {now}）",
                LogLevel.Info);
            Monitor.Log("提示: 打开图鉴鱼类页查看皇冠是否为流动金色（视觉待真实游戏确认）", LogLevel.Info);
        }

        private void OnCommandAssist(string command, string[] args)
        {
            _forceAssistNext = true;
            int countable = DifficultyManager.GetCountableCrownCount(Game1.player);
            double chance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            Monitor.Log("✓ 下一次钓鱼小游戏将强制触发助战（测试）", LogLevel.Info);
            if (countable <= 0)
            {
                Monitor.Log("警告: 当前无可计数皇冠，助战将无法选出助战鱼；请先用 fish_addstar / fish_addstars 添加皇冠", LogLevel.Warn);
            }
            else
            {
                Monitor.Log($"当前可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 正常助战概率: {chance:P1}", LogLevel.Info);
            }
        }

        private string GetRankName(int level)
        {
            return ModHelper.Translation.Get(Utils.DifficultyCalculator.GetRankKey(level));
        }

        /// <summary>BATCH-035 自动化验收：确定性自测（只读生产函数 + 本地种子随机，不改存档、不写玩家状态）。</summary>
        private void OnCommandSelfTest(string command, string[] args)
        {
            Monitor.Log("======== BATCH-035 自动自测 ========", LogLevel.Info);
            int pass = 0, fail = 0;
            void Report(string name, bool ok, string detail = "")
            {
                Monitor.Log($"{(ok ? "[PASS]" : "[FAIL]")} {name}{(detail.Length > 0 ? " | " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);
                if (ok) pass++; else fail++;
            }

            // 1. 概率数学（纯函数）
            double c0 = Utils.DifficultyCalculator.GetAssistChance(0, DifficultyManager.CountableCrownTarget);
            double cFull = Utils.DifficultyCalculator.GetAssistChance(61, DifficultyManager.CountableCrownTarget);
            double cMid = Utils.DifficultyCalculator.GetAssistChance(20, DifficultyManager.CountableCrownTarget);
            double cOver = Utils.DifficultyCalculator.GetAssistChance(99, DifficultyManager.CountableCrownTarget);
            Report("概率: 0 皇冠 = 0%", Math.Abs(c0) < 1e-9, $"实际 {c0:P1}");
            Report("概率: 满 61 = 10%", Math.Abs(cFull - 0.10) < 1e-9, $"实际 {cFull:P1}");
            Report("概率: 20/61 = 3.28%", Math.Abs(cMid - 0.10 * 20.0 / 61.0) < 1e-9, $"实际 {cMid:P2}");
            Report("概率: 超过 61 钳制 = 10%", Math.Abs(cOver - 0.10) < 1e-9, $"实际 {cOver:P1}");

            // 2. 可计数皇冠（只读）
            int starCount = DifficultyManager.GetCollectionStarCount(Game1.player);
            int countable = DifficultyManager.GetCountableCrownCount(Game1.player);
            var starred = DifficultyManager.GetCountableStarredFish(Game1.player);
            Report("可计数: 计数=列表数 且 ≤61", countable == starred.Count && countable <= DifficultyManager.CountableCrownTarget,
                $"{countable}/{DifficultyManager.CountableCrownTarget}（星标总数 {starCount}）");
            bool allCountable = true;
            foreach (string id in starred)
            {
                if (!DifficultyManager.IsCountableFish(id)) { allCountable = false; break; }
            }
            Report("可计数: 列表无 Mod 鱼", allCountable, $"共 {starred.Count} 条");

            // 3. 排位（当前玩家皇冠鱼，只读）
            if (starred.Count > 0)
            {
                int minD = int.MaxValue, maxD = int.MinValue;
                foreach (string id in starred)
                {
                    int lvl = DifficultyManager.GetDifficultyLevel(id, Game1.player);
                    if (lvl < minD) minD = lvl;
                    if (lvl > maxD) maxD = lvl;
                }
                bool ranksOk = true;
                foreach (string id in starred)
                {
                    double r = DifficultyManager.GetAssistRank(id, Game1.player, starred);
                    if (r < 0.0 || r > 1.0) { ranksOk = false; break; }
                }
                Report("排位: 全部 ∈ [0,1]", ranksOk, $"难度范围 {minD}~{maxD}，共 {starred.Count} 条");
            }
            else
            {
                Monitor.Log("[SKIP] 排位检查: 当前无皇冠鱼（fish_addstar / fish_addstars 添加后重跑）", LogLevel.Warn);
            }

            // 4. 等级分布统计（本地种子随机，结束后恢复全局随机）
            Random saved = Game1.random;
            try
            {
                Game1.random = new Random(20260809);
                const int N = 50000;
                double[] mean = new double[3], p0 = new double[3], p40 = new double[3];
                for (int r = 0; r <= 2; r++)
                {
                    double rank = r * 0.5;
                    long[] hist = new long[41];
                    for (int i = 0; i < N; i++)
                        hist[Utils.DifficultyCalculator.GetRandomAssistLevel(rank)]++;
                    for (int L = 0; L <= 40; L++) mean[r] += L * hist[L];
                    mean[r] /= N;
                    p0[r] = hist[0] / (double)N;
                    p40[r] = hist[40] / (double)N;
                }
                Report("分布: 中位鱼均值≈20", Math.Abs(mean[1] - 20.0) < 0.5, $"实际 {mean[1]:F2}");
                Report("分布: 最低+最高均值=40", Math.Abs(mean[0] + mean[2] - 40.0) < 0.8, $"实际 {mean[0]:F2}+{mean[2]:F2}");
                Report("分布: 40级比值≈30", Math.Abs(p40[2] / Math.Max(p40[0], 1e-9) - 30.0) < 6.0, $"实际 {p40[2] / Math.Max(p40[0], 1e-9):F1}");
                Report("分布: 0级镜像比值≈30", Math.Abs(p0[0] / Math.Max(p0[2], 1e-9) - 30.0) < 6.0, $"实际 {p0[0] / Math.Max(p0[2], 1e-9):F1}");
            }
            finally
            {
                Game1.random = saved;
            }

            // 5. i18n 键（20 条助战文案）
            int assistKeys = 0;
            for (int i = 1; i <= 20; i++)
            {
                string v = ModHelper.Translation.Get($"hud.assist.{i}");
                if (!string.IsNullOrWhiteSpace(v) && v != $"hud.assist.{i}") assistKeys++;
            }
            Report("i18n: hud.assist.1~20", assistKeys == 20, $"{assistKeys}/20");

            // 6. 强制标志生命周期（薄控制，不写存档）
            bool idle = ConsumeForceAssistFlag();
            _forceAssistNext = true;
            bool consumed = ConsumeForceAssistFlag();
            bool cleared = !ConsumeForceAssistFlag();
            Report("强制标志: 空闲false→置位→消费true→清除false", !idle && consumed && cleared);

            Monitor.Log($"======== 自测结果: {pass} 通过 / {fail} 失败 ========", fail == 0 ? LogLevel.Info : LogLevel.Error);
        }

        /// <summary>BATCH-035 自动化验收：助战观测统计（会话内内存观测；fish_assiststats [clear]）。</summary>
        private void OnCommandAssistStats(string command, string[] args)
        {
            if (args.Length > 0 && args[0] == "clear")
            {
                Patches.BobberBarPatches.ClearAssistObservations();
                Monitor.Log("✓ 助战观测已清空", LogLevel.Info);
                return;
            }

            var obs = Patches.BobberBarPatches.GetAssistObservations();
            Monitor.Log("======== 助战观测统计（会话内内存，上限 500）========", LogLevel.Info);
            Monitor.Log($"观测次数: {obs.Count}/500", LogLevel.Info);
            if (obs.Count == 0)
            {
                Monitor.Log("无观测：用 fish_assist 强制触发或正常钓鱼触发助战，触发后自动累积", LogLevel.Warn);
                return;
            }

            double totalLevel = 0;
            int lowN = 0, midN = 0, highN = 0, low40 = 0, high40 = 0, low0 = 0, high0 = 0;
            double lowSum = 0, midSum = 0, highSum = 0;
            foreach (var o in obs)
            {
                totalLevel += o.Level;
                if (o.Rank < 0.33) { lowN++; lowSum += o.Level; if (o.Level == 40) low40++; if (o.Level == 0) low0++; }
                else if (o.Rank > 0.67) { highN++; highSum += o.Level; if (o.Level == 40) high40++; if (o.Level == 0) high0++; }
                else { midN++; midSum += o.Level; }
            }

            Monitor.Log($"平均助战等级: {totalLevel / obs.Count:F2}", LogLevel.Info);
            Monitor.Log($"低排位组 (<0.33): n={lowN} 平均 {(lowN > 0 ? lowSum / lowN : 0):F2}（期望≈13.5）| 40级 {low40} 次, 0级 {low0} 次", LogLevel.Info);
            Monitor.Log($"中排位组 (0.33~0.67): n={midN} 平均 {(midN > 0 ? midSum / midN : 0):F2}（期望≈20）", LogLevel.Info);
            Monitor.Log($"高排位组 (>0.67): n={highN} 平均 {(highN > 0 ? highSum / highN : 0):F2}（期望≈26.5）| 40级 {high40} 次, 0级 {high0} 次", LogLevel.Info);
            if (lowN > 0 && highN > 0)
            {
                double p40Low = low40 / (double)lowN, p40High = high40 / (double)highN;
                double p0Low = low0 / (double)lowN, p0High = high0 / (double)highN;
                Monitor.Log($"40级比例: 低组 {p40Low:P2} vs 高组 {p40High:P2}（期望比值≈30）", LogLevel.Info);
                Monitor.Log($"0级比例: 低组 {p0Low:P2} vs 高组 {p0High:P2}（期望比值≈1/30）", LogLevel.Info);
                Monitor.Log($"低组+高组平均等级和: {(lowSum / lowN) + (highSum / highN):F2}（期望≈40）", LogLevel.Info);
            }
            Monitor.Log("提示: 观测为会话内内存（不写存档），fish_assiststats clear 可清空；样本越多越接近期望。", LogLevel.Info);
        }

        #endregion
    }
}

