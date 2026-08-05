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

        /// <summary>SMAPI Helper</summary>
        public static IModHelper ModHelper => Instance.Helper;

        /// <summary>监视器</summary>
        public static IMonitor ModMonitor => Instance.Monitor;

        // 性能优化：使用计数器替代模运算
        private int _npcCheckCounter = 0;
        private int _cleanupCounter = 0;

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
            DifficultyManager.Initialize();
            Monitor.Log("=== FishingExpanded 存档加载完成 ===", LogLevel.Info);
        }

        /// <summary>保存前</summary>
        private void OnSaving(object sender, SavingEventArgs e)
        {
            DifficultyManager.SaveData();
            Monitor.Log("已保存钓鱼难度数据", LogLevel.Debug);
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
                Monitor.Log($"[ModEntry] 玩家进入FarmHouse: {e.NewLocation.Name}", LogLevel.Debug);
                GiantFishManager.OnEnterFarmHouse();
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
                "添加收藏星标 | 用法: fish_addstar <鱼ID>",
                OnCommandAddStar);

            Helper.ConsoleCommands.Add("fish_giant",
                "模拟巨型鱼（触发NPC反应）| 用法: fish_giant <鱼ID> <倍数>",
                OnCommandGiant);

            Helper.ConsoleCommands.Add("fish_bonus",
                "查看当前钓鱼等级加成",
                OnCommandBonus);

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

            DifficultyManager.SetDifficultyLevel(fishId, level);
            int actualLevel = DifficultyManager.GetDifficultyLevel(fishId);
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
                DifficultyManager.RecordSuccess(fishId);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId);
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
                DifficultyManager.RecordFailure(fishId);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId);
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
            var stats = DifficultyManager.GetFishStats(fishId);

            if (stats == null)
            {
                Monitor.Log($"鱼 {fishId} 尚未记录任何数据", LogLevel.Info);
                return;
            }

            int level = stats.DifficultyLevel;
            bool hasStar = DifficultyManager.HasCollectionStar(fishId);

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
            var allStats = DifficultyManager.GetAllFishStats();

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
                bool hasStar = DifficultyManager.HasCollectionStar(fishId);
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

            DifficultyManager.ClearAllData();
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
            DifficultyManager.RecordHighDifficulty(fishId);
            Monitor.Log($"✓ 已为 {fishId} 添加收藏星标并增加 +0.5 钓鱼等级", LogLevel.Info);
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

        private void OnCommandBonus(string command, string[] args)
        {
            float bonus = DifficultyManager.GetFishingLevelBonus();
            int hiddenLevels = (int)Math.Floor(bonus);
            int starCount = DifficultyManager.GetCollectionStarCount();

            Monitor.Log("===========================================", LogLevel.Info);
            Monitor.Log($"收藏星标数量: {starCount}", LogLevel.Info);
            Monitor.Log($"钓鱼等级加成: +{bonus:F1} (隐藏等级: +{hiddenLevels})", LogLevel.Info);
            Monitor.Log("===========================================", LogLevel.Info);
        }

        private string GetRankName(int level)
        {
            if (level < -8) return "额...稍微强一点的个体";
            if (level < -5) return "精英";
            if (level < -2) return "骑士";
            if (level < 0) return "领主";
            if (level < 10) return "伯爵";
            if (level < 30) return "大公";
            if (level < 50) return "亲王";
            if (level < 70) return "帝王";
            if (level < 90) return "神皇";
            return "神王";
        }

        #endregion
    }
}
