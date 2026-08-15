using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        // BATCH-039: 测试命令 fish_persisttest 强制下一次钓鱼小游戏按指定秒数发放持久战奖励（薄控制，构造边界消费）
        private static int? _forcePerseveranceSeconds;

        /// <summary>SMAPI Helper</summary>
        public static IModHelper ModHelper => Instance.Helper;

        /// <summary>监视器</summary>
        public static IMonitor ModMonitor => Instance.Monitor;

        /// <summary>当前配置（config.json；GMCM 修改时实时生效）</summary>
        public static ModConfig Config { get; private set; }

        // 性能优化：使用计数器替代模运算
        private int _npcCheckCounter = 0;
        private int _cleanupCounter = 0;
        private int _hudQueueCounter = 0; // BATCH-032: HUD 提示队列驱动计数器

        // BATCH-042/044: GMCM 重置两步确认（开关待命 + 原生问题对话框二次确认；无超时，必须明确选择）
        private static bool _resetPending;
        private static bool _resetSequenceStarted;
        private static int _resetArmedScreenId;
        private static long _resetArmedPlayerId; // BATCH-044: 发起重置的玩家 ID（回调身份校验）

        /// <summary>Mod入口点</summary>
        /// <param name="helper">SMAPI Helper</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = helper.ReadConfig<ModConfig>();
            FishingLog.Enabled = Config.EnableLogging;
            FishingLog.Log("FishingExpanded 正在初始化...", LogLevel.Info);

            try
            {
                // 注册 Harmony Patches
                var harmony = new Harmony(ModManifest.UniqueID);
                harmony.PatchAll();
                FishingLog.Log("Harmony Patches 注册成功", LogLevel.Debug);

                // 注册事件监听器
                helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
                helper.Events.GameLoop.Saving += OnSaving;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                helper.Events.GameLoop.DayStarted += OnDayStarted;
                helper.Events.GameLoop.GameLaunched += OnGameLaunched;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                helper.Events.Player.Warped += OnWarped;

                // 注册控制台命令
                RegisterConsoleCommands();

                FishingLog.Log("FishingExpanded 初始化完成", LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"初始化失败: {ex}", LogLevel.Error);
            }
        }

        /// <summary>存档加载后</summary>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            DifficultyManager.LoadData(Game1.player);
            // BATCH-043: 装模组前已钓到的原版传奇鱼补发皇冠（幂等；按原生 fishCaught 记录）
            DifficultyManager.BackfillLegendaryCrowns(Game1.player);
            HUDNotifier.ClearPending(); // BATCH-032: 加载存档时清空瞬时提示队列
            GiantFishManager.ResetForSave();
            FishingLog.Log("=== FishingExpanded 存档加载完成 ===", LogLevel.Info);
        }

        /// <summary>保存前</summary>
        private void OnSaving(object sender, SavingEventArgs e)
        {
            DifficultyManager.SaveAll();
            FishingLog.Log("已保存钓鱼难度数据", LogLevel.Debug);
        }

        /// <summary>返回标题时清除当前玩家缓存，避免联机换存档串写</summary>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            DifficultyManager.UnloadData();
            HUDNotifier.ClearPending(); // BATCH-032: 返回标题清空瞬时提示队列
            Patches.FishingRodPatches.ClearPending();
            Patches.CrabPotPatches.ClearPending(); // BATCH-065: 返回标题清空蟹笼待结算
            GiantFishManager.ResetForSave();
            FishingLog.Log("已清除 FishingExpanded 当前玩家缓存", LogLevel.Debug);
        }

        /// <summary>每日开始</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            FishingLog.Log($"=== FishingExpanded 新的一天开始 (Day {Game1.dayOfMonth}) ===", LogLevel.Info);
            GiantFishManager.OnDayStarted();
            DifficultyManager.ResetDailyHarvest(); // BATCH-061: 每日收获限额按游戏日重置（内存态）
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

            // BATCH-042: 重置两步确认（关闭 GMCM 菜单后触发原生问题对话框；无逐帧日志）
            TryStartResetSequence();
        }

        /// <summary>BATCH-040: 可选 GMCM 日志开关注册（未安装 GMCM 时无操作；config.json 始终生效）。
        /// BATCH-060 第 9 项：GameLaunched 时向 VanillaTips（neoiw.vanillatips）注入 7 条提示（来源权重 11；未安装则跳过）。</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Services.VanillaTipsIntegration.TryRegister(Helper, Monitor);

            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
                return;

            api.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => Helper.WriteConfig(Config));

            api.AddBoolOption(
                ModManifest,
                () => Config.EnableLogging,
                value =>
                {
                    Config.EnableLogging = value;
                    FishingLog.Enabled = value;
                },
                () => Helper.Translation.Get("config.enableLogging.name"),
                () => Helper.Translation.Get("config.enableLogging.tooltip"));

            // BATCH-058R/S: 鱼的行为全随机模式开关（默认关；开启=每局完全随机、无法背板，更难）。
            api.AddBoolOption(
                ModManifest,
                () => Config.EnableRandomFishBehavior,
                value => Config.EnableRandomFishBehavior = value,
                () => Helper.Translation.Get("config.randomFishBehavior.name"),
                () => Helper.Translation.Get("config.randomFishBehavior.tooltip"));

            RegisterGmcmResetSection(api);
        }

        /// <summary>BATCH-042: GMCM 重置区（参考 GCE GMCM 方案：章节标题 + 警告段落 + 布尔开关待命；
        /// 不使用自绘按钮，关闭菜单后由原生问题对话框做二次确认）。</summary>
        private void RegisterGmcmResetSection(IGenericModConfigMenuApi api)
        {
            api.AddSectionTitle(
                ModManifest,
                () => Helper.Translation.Get("config.reset.section"),
                () => Helper.Translation.Get("config.reset.sectionTooltip"));
            api.AddParagraph(ModManifest, () => Helper.Translation.Get("config.reset.warning1"));
            api.AddParagraph(ModManifest, () => Helper.Translation.Get("config.reset.warning2"));
            api.AddParagraph(ModManifest, () => Helper.Translation.Get("config.reset.warning3"));
            api.AddBoolOption(
                ModManifest,
                () => _resetPending,
                value =>
                {
                    _resetPending = value;
                    _resetArmedScreenId = Context.ScreenId;
                    _resetArmedPlayerId = Game1.player?.UniqueMultiplayerID ?? 0;
                    if (!value)
                    {
                        _resetSequenceStarted = false;
                        _resetArmedPlayerId = 0;
                    }
                },
                () => Helper.Translation.Get("config.reset.name"),
                () => Helper.Translation.Get("config.reset.tooltip"),
                "reset-challenge-data");
        }

        /// <summary>关闭 GMCM 菜单后，在当前玩家屏幕弹出原生问题对话框做二次确认（防误点；无超时）。</summary>
        private void TryStartResetSequence()
        {
            if (!_resetPending || _resetSequenceStarted ||
                !Context.IsWorldReady || Game1.player == null || Game1.currentLocation == null ||
                Game1.eventUp || Game1.activeClickableMenu != null ||
                Context.ScreenId != _resetArmedScreenId ||
                Game1.player.UniqueMultiplayerID != _resetArmedPlayerId)
            {
                return;
            }

            _resetSequenceStarted = true;
            Game1.currentLocation.createQuestionDialogue(
                Helper.Translation.Get("config.reset.confirmPrompt"),
                new[]
                {
                    new Response("reset_confirm", Helper.Translation.Get("config.reset.confirmYes")),
                    new Response("reset_cancel", Helper.Translation.Get("config.reset.confirmNo"))
                },
                OnResetSequenceAnswer);
        }

        private void OnResetSequenceAnswer(Farmer who, string answer)
        {
            _resetSequenceStarted = false;
            _resetPending = false;
            long answeredPlayerId = who?.UniqueMultiplayerID ?? 0;

            // BATCH-044: 只接受发起重置的玩家本人的应答（分屏/共享地点防串清）。
            if (answeredPlayerId != _resetArmedPlayerId)
            {
                FishingLog.Log(
                    $"[GMCM] 重置应答者与发起者不一致，已忽略 | 应答: {answeredPlayerId} | 发起: {_resetArmedPlayerId}",
                    LogLevel.Warn);
                _resetArmedPlayerId = 0;
                return;
            }
            _resetArmedPlayerId = 0;

            if (!answer.Equals("reset_confirm", StringComparison.OrdinalIgnoreCase))
            {
                FishingLog.Log("[GMCM] 重置已取消，未修改任何存档数据", LogLevel.Info);
                return;
            }

            DifficultyManager.ClearAllData(who);
            ShowResetHudMessage("config.reset.done");
            FishingLog.Log($"[GMCM] 钓鱼挑战数据已重置为刚安装状态 | 玩家: {who.UniqueMultiplayerID}", LogLevel.Info);
        }

        private void ShowResetHudMessage(string translationKey)
        {
            if (Game1.player == null)
                return;

            // HUDMessage 无公开颜色字段（原生以 Game1.textColor 绘制），警告/成功靠文案本身区分。
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get(translationKey)));
        }

        /// <summary>BATCH-045: 解析可选玩家序号（1=主机，2=第一个农场客/副机，以此类推；顺序=Game1.getAllFarmers()，含离线农场客）。
        /// 仅当 args[offset] 为 1..玩家数 且（allowLoneIndex 或后面仍有参数）时按序号解析并消费；
        /// 否则返回 null，调用方按默认当前玩家处理。</summary>
        private static Farmer TryParseTargetPlayer(string[] args, ref int offset, bool allowLoneIndex = false)
        {
            if (args == null || args.Length <= offset)
                return null;

            if (int.TryParse(args[offset], out int index) && index >= 1)
            {
                List<Farmer> farmers = new List<Farmer>(Game1.getAllFarmers());
                if (index <= farmers.Count && (allowLoneIndex || args.Length > offset + 1))
                {
                    offset++;
                    return farmers[index - 1];
                }
            }
            return null;
        }

        /// <summary>玩家传送</summary>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // 检查是否进入FarmHouse
            if (e.NewLocation is StardewValley.Locations.FarmHouse)
            {
                FishingLog.Log(
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
                "设置某鱼的难度等级 | 用法: fish_setlevel [玩家序号] <鱼ID> <等级>（可选序号: 1=主机, 2=副机…）",
                OnCommandSetLevel);

            Helper.ConsoleCommands.Add("fish_addsuccess",
                "增加成功次数 | 用法: fish_addsuccess [玩家序号] <鱼ID> <次数>（可选序号: 1=主机, 2=副机…）",
                OnCommandAddSuccess);

            Helper.ConsoleCommands.Add("fish_addfail",
                "增加失败次数 | 用法: fish_addfail [玩家序号] <鱼ID> <次数>（可选序号: 1=主机, 2=副机…）",
                OnCommandAddFail);

            Helper.ConsoleCommands.Add("fish_info",
                "查看鱼的详细信息 | 用法: fish_info [玩家序号] <鱼ID>（可选序号: 1=主机, 2=副机…）",
                OnCommandInfo);

            Helper.ConsoleCommands.Add("fish_list",
                "列出所有已记录的鱼 | 用法: fish_list [玩家序号]（1=主机, 2=副机…）",
                OnCommandList);

            Helper.ConsoleCommands.Add("fish_clear",
                "清空所有钓鱼数据 | 用法: fish_clear [玩家序号] confirm",
                OnCommandClear);

            Helper.ConsoleCommands.Add("fish_addstar",
                "添加收藏皇冠（测试）| 用法: fish_addstar [玩家序号] <鱼ID>",
                OnCommandAddStar);

            Helper.ConsoleCommands.Add("fish_giant",
                "模拟巨型鱼（触发NPC反应）| 用法: fish_giant [玩家序号] <鱼ID> <倍数>",
                OnCommandGiant);

            Helper.ConsoleCommands.Add("fish_bonus",
                "查看收藏皇冠与鱼竿熟练度 | 用法: fish_bonus [玩家序号]（1=主机, 2=副机…）",
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
                "批量添加收藏皇冠（完整可计数 61 鱼池，含原版传奇，提升鱼竿熟练度）| 用法: fish_addstars [玩家序号] <数量>（1=主机, 2=副机…）",
                OnCommandAddStars);

            Helper.ConsoleCommands.Add("fish_challengecrown",
                "设置挑战鱼饵流动金色皇冠标记（测试）| 用法: fish_challengecrown [玩家序号] <鱼ID> [0|1]",
                OnCommandChallengeCrown);

            Helper.ConsoleCommands.Add("fish_persisttest",
                "强制下一次钓鱼小游戏失败时按指定秒数发放持久战奖励（测试）| 用法: fish_persisttest <30|60>",
                OnCommandPerseveranceTest);

            Helper.ConsoleCommands.Add("fish_giveitem",
                "给当前/指定玩家物品（测试）| 用法: fish_giveitem [玩家序号] <物品ID> [品质0|1|2|4] [数量]（例: fish_giveitem 265 4 10 = 10 个铱星海泡布丁）",
                OnCommandGiveItem);

            Helper.ConsoleCommands.Add("fish_dumpstars",
                "导出星星候选贴图区域 PNG 到 Mods\\FishingExpanded\\dump（识图选素材用）| 用法: fish_dumpstars",
                OnCommandDumpStars);

            FishingLog.Log("控制台命令注册完成 (输入 help 查看所有命令)", LogLevel.Debug);
        }

        private void OnCommandSetLevel(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 2)
            {
                FishingLog.Log("用法: fish_setlevel [玩家序号] <鱼ID> <等级>  (1=主机, 2=副机…)", LogLevel.Info);
                FishingLog.Log("例如: fish_setlevel 128 50 或 fish_setlevel 2 128 50", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            if (!int.TryParse(args[offset + 1], out int level))
            {
                FishingLog.Log("等级必须是数字！", LogLevel.Error);
                return;
            }

            DifficultyManager.SetDifficultyLevel(fishId, level, player);
            int actualLevel = DifficultyManager.GetDifficultyLevel(fishId, player);
            FishingLog.Log($"✓ 已设置 {fishId} 的难度等级为 {actualLevel} | 玩家: {player.Name}", LogLevel.Info);
        }

        private void OnCommandAddSuccess(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 2)
            {
                FishingLog.Log("用法: fish_addsuccess [玩家序号] <鱼ID> <次数>", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            if (!int.TryParse(args[offset + 1], out int count) || count <= 0)
            {
                FishingLog.Log("次数必须是正整数！", LogLevel.Error);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                DifficultyManager.RecordSuccess(fishId, 1, player);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId, player);
            FishingLog.Log($"✓ 已为 {fishId} 增加 {count} 次成功，当前难度等级: {level} | 玩家: {player.Name}", LogLevel.Info);
        }

        private void OnCommandAddFail(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 2)
            {
                FishingLog.Log("用法: fish_addfail [玩家序号] <鱼ID> <次数>", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            if (!int.TryParse(args[offset + 1], out int count) || count <= 0)
            {
                FishingLog.Log("次数必须是正整数！", LogLevel.Error);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                DifficultyManager.RecordFailure(fishId, player);
            }

            int level = DifficultyManager.GetDifficultyLevel(fishId, player);
            FishingLog.Log($"✓ 已为 {fishId} 增加 {count} 次失败，当前难度等级: {level} | 玩家: {player.Name}", LogLevel.Info);
        }

        private void OnCommandInfo(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 1)
            {
                FishingLog.Log("用法: fish_info [玩家序号] <鱼ID>", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            var stats = DifficultyManager.GetFishStats(fishId, player);

            if (stats == null)
            {
                FishingLog.Log($"鱼 {fishId} 尚未记录任何数据 | 玩家: {player.Name}", LogLevel.Info);
                return;
            }

            int level = stats.DifficultyLevel;
            bool hasStar = DifficultyManager.HasCollectionStar(fishId, player);

            FishingLog.Log("===========================================", LogLevel.Info);
            FishingLog.Log($"鱼ID: {fishId} | 玩家: {player.Name}", LogLevel.Info);
            FishingLog.Log($"成功次数: {stats.SuccessCount}", LogLevel.Info);
            FishingLog.Log($"失败次数: {stats.FailCount}", LogLevel.Info);
            FishingLog.Log($"难度等级: {level} ({GetRankName(level)})", LogLevel.Info);
            FishingLog.Log($"难度倍数: {Utils.DifficultyCalculator.GetDifficultyMultiplier(level):F2}x", LogLevel.Info);
            FishingLog.Log($"数量倍数: {Utils.DifficultyCalculator.GetQuantityMultiplier(level)}x", LogLevel.Info);
            FishingLog.Log($"品质门槛: +{Utils.DifficultyCalculator.GetQualityTier(level)} (0=普通/1=银/2=金/4=铱, BATCH-060)", LogLevel.Info);
            FishingLog.Log($"收藏星标: {(hasStar ? "★ 已获得" : "未获得")}", LogLevel.Info);
            FishingLog.Log("===========================================", LogLevel.Info);
        }

        private void OnCommandList(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset, allowLoneIndex: true) ?? Game1.player;
            var allStats = DifficultyManager.GetAllFishStats(player);

            if (allStats.Count == 0)
            {
                FishingLog.Log($"{player.Name} 当前没有任何钓鱼记录", LogLevel.Info);
                return;
            }

            FishingLog.Log($"===== {player.Name} 已记录的鱼种（共 {allStats.Count} 种）=====", LogLevel.Info);
            foreach (var kvp in allStats)
            {
                string fishId = kvp.Key;
                var stats = kvp.Value;
                int level = stats.DifficultyLevel;
                bool hasStar = DifficultyManager.HasCollectionStar(fishId, player);
                string starMark = hasStar ? " ★" : "";

                FishingLog.Log($"  {fishId}: 等级={level} ({GetRankName(level)}), " +
                           $"成功={stats.SuccessCount}, 失败={stats.FailCount}{starMark}",
                           LogLevel.Info);
            }
            FishingLog.Log("===========================================", LogLevel.Info);
        }

        private void OnCommandClear(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 1 || !args[offset].Equals("confirm", StringComparison.OrdinalIgnoreCase))
            {
                FishingLog.Log("⚠ 此操作将清空所有钓鱼数据！", LogLevel.Warn);
                FishingLog.Log("如需确认，请输入: fish_clear confirm 或 fish_clear <玩家序号> confirm", LogLevel.Info);
                return;
            }

            DifficultyManager.ClearAllData(player);
            FishingLog.Log($"✓ 已清空 {player.Name} 的钓鱼数据", LogLevel.Info);
        }

        private void OnCommandAddStar(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 1)
            {
                FishingLog.Log("用法: fish_addstar [玩家序号] <鱼ID>", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            DifficultyManager.RecordHighDifficulty(fishId, player);
            FishingLog.Log($"✓ 已为 {fishId} 添加收藏皇冠 | 玩家: {player.Name}", LogLevel.Info);
        }

        private void OnCommandGiant(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 2)
            {
                FishingLog.Log("用法: fish_giant [玩家序号] <鱼ID> <难度等级>（BATCH-060：巨型鱼按难度等级 ≥8 触发）", LogLevel.Info);
                FishingLog.Log("例如: fish_giant 128 20 或 fish_giant 2 128 20", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            if (!int.TryParse(args[offset + 1], out int level) || level <= 0)
            {
                FishingLog.Log("难度等级必须是正整数！", LogLevel.Error);
                return;
            }

            GiantFishManager.RecordGiantFish(player, fishId, level, level * 20);
            FishingLog.Log($"✓ 已记录巨型鱼 {fishId} (难度等级={level}) | 玩家: {player.Name}，等待触发NPC反应", LogLevel.Info);
            FishingLog.Log("提示: 将鱼拿在手上并靠近NPC即可触发反应", LogLevel.Info);
        }

        /// <summary>批量添加收藏皇冠（只从可计数原生普通鱼池挑选，BATCH-034/039：Mod 鱼皇冠不计鱼竿熟练度）。</summary>
        private void OnCommandAddStars(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 1 || !int.TryParse(args[offset], out int count) || count <= 0)
            {
                FishingLog.Log("用法: fish_addstars [玩家序号] <数量>", LogLevel.Info);
                FishingLog.Log("例如: fish_addstars 20 或 fish_addstars 2 20  (给副机加20颗)", LogLevel.Info);
                return;
            }

            int added = DifficultyManager.AddCollectionStarsForTesting(player, count);
            int countable = DifficultyManager.GetCountableCrownCount(player);
            float alpha = DifficultyManager.GetAlpha(player);

            FishingLog.Log($"✓ 已为 {player.Name} 添加 {added} 颗收藏皇冠 (目标 {count})", LogLevel.Info);
            FishingLog.Log($"可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 鱼竿熟练度: {alpha:P0}", LogLevel.Info);
            double assistChance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            FishingLog.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", LogLevel.Info);
            if (added < count)
            {
                FishingLog.Log("提示: 可计数 61 鱼池皇冠已用完（56 普通 + 5 原版传奇，不含 Mod 鱼）；可先 fish_clear confirm 清空后重新添加", LogLevel.Warn);
            }
        }

        private void OnCommandBonus(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset, allowLoneIndex: true) ?? Game1.player;
            int starCount = DifficultyManager.GetCollectionStarCount(player);
            int countable = DifficultyManager.GetCountableCrownCount(player);
            float alpha = DifficultyManager.GetAlpha(player);

            FishingLog.Log("===========================================", LogLevel.Info);
            FishingLog.Log($"玩家: {player.Name}", LogLevel.Info);
            FishingLog.Log($"收藏皇冠总数: {starCount}", LogLevel.Info);
            FishingLog.Log($"可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 鱼竿熟练度: {alpha:P0}", LogLevel.Info);
            double assistChance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            FishingLog.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", LogLevel.Info);
            FishingLog.Log("===========================================", LogLevel.Info);
        }

        /// <summary>BATCH-035: 消费强制助战标志（BobberBar 构造边界调用；一次消费后清除）。</summary>
        public static bool ConsumeForceAssistFlag()
        {
            bool forced = _forceAssistNext;
            _forceAssistNext = false;
            return forced;
        }

        /// <summary>BATCH-039: 消费强制持久战秒数（BobberBar 构造边界调用；一次消费后清除；0=未强制）。</summary>
        public static float ConsumeForcePerseveranceSeconds()
        {
            int? seconds = _forcePerseveranceSeconds;
            _forcePerseveranceSeconds = null;
            return seconds ?? 0f;
        }

        /// <summary>BATCH-035: 强制下一次钓鱼小游戏触发助战（薄控制测试命令，不写存档）。</summary>
        /// <summary>BATCH-038: 设置/清除挑战鱼饵流动金色皇冠标记（测试；写存档同生产边界）。</summary>
        private void OnCommandChallengeCrown(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (args.Length < offset + 1)
            {
                FishingLog.Log("用法: fish_challengecrown [玩家序号] <鱼ID> [0|1]", LogLevel.Info);
                FishingLog.Log("例如: fish_challengecrown 144 1 或 fish_challengecrown 2 144 1", LogLevel.Info);
                return;
            }

            string fishId = args[offset];
            bool setValue = args.Length < offset + 2 || args[offset + 1] != "0";
            DifficultyManager.SetChallengeCrown(fishId, setValue, player);
            bool now = DifficultyManager.HasChallengeCrown(fishId, player);
            FishingLog.Log(
                $"✓ 已{(setValue ? "设置" : "清除")} {fishId} 的挑战鱼饵流动金色皇冠标记（当前: {now}）| 玩家: {player.Name}",
                LogLevel.Info);
            FishingLog.Log("提示: 打开图鉴鱼类页查看皇冠是否为流动金色（视觉待真实游戏确认）", LogLevel.Info);
        }

        private void OnCommandAssist(string command, string[] args)
        {
            _forceAssistNext = true;
            int countable = DifficultyManager.GetCountableCrownCount(Game1.player);
            double chance = Utils.DifficultyCalculator.GetAssistChance(countable, DifficultyManager.CountableCrownTarget);
            FishingLog.Log("✓ 下一次钓鱼小游戏将强制触发助战（测试）", LogLevel.Info);
            if (countable <= 0)
            {
                FishingLog.Log("警告: 当前无可计数皇冠，助战将无法选出助战鱼；请先用 fish_addstar / fish_addstars 添加皇冠", LogLevel.Warn);
            }
            else
            {
                FishingLog.Log($"当前可计数皇冠: {countable}/{DifficultyManager.CountableCrownTarget} | 正常助战概率: {chance:P1}", LogLevel.Info);
            }
        }

        /// <summary>BATCH-039: 强制下一次钓鱼小游戏按指定秒数发放持久战奖励（薄控制测试命令；构造边界消费；不写存档）。</summary>
        private void OnCommandPerseveranceTest(string command, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int seconds) || (seconds != 30 && seconds != 60))
            {
                FishingLog.Log("用法: fish_persisttest <30|60>", LogLevel.Info);
                FishingLog.Log("例如: fish_persisttest 60  (下一次小游戏失败时按 60 秒必发海泡布丁)", LogLevel.Info);
                return;
            }

            _forcePerseveranceSeconds = seconds;
            FishingLog.Log($"✓ 下一次钓鱼小游戏失败时将按 {seconds} 秒发放持久战奖励（30 秒=50% +3 料理；60 秒=必得海泡布丁）", LogLevel.Info);
            FishingLog.Log("提示: 鱼王小游戏豁免奖励，强制标志会被鱼王豁免消耗", LogLevel.Info);
        }

        /// <summary>BATCH-062: 给当前/指定玩家发物品（测试/调试用；addItemByMenuIfNecessary 走原生背包或溢出菜单）。</summary>
        private void OnCommandGiveItem(string command, string[] args)
        {
            int offset = 0;
            Farmer player = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
            if (player == null || args.Length < offset + 1)
            {
                FishingLog.Log("用法: fish_giveitem [玩家序号] <物品ID> [品质0|1|2|4] [数量]", LogLevel.Info);
                FishingLog.Log("例如: fish_giveitem 265 4 10  (10 个铱星海泡布丁)；fish_giveitem 2 265 4 1  (给副机 1 个铱星海泡布丁)", LogLevel.Info);
                return;
            }

            string rawId = args[offset];
            string itemId = Utils.SpecialFishHelper.NormalizeItemId(rawId);
            int quality = 0;
            int count = 1;
            if (args.Length > offset + 1)
                int.TryParse(args[offset + 1], out quality);
            if (args.Length > offset + 2)
                int.TryParse(args[offset + 2], out count);

            try
            {
                Item item = ItemRegistry.Create(itemId, Math.Max(1, count));
                if (item is StardewValley.Object obj)
                {
                    obj.Quality = quality switch
                    {
                        1 => 1,
                        2 => 2,
                        >= 4 => 4,
                        _ => 0
                    };
                }
                player.addItemByMenuIfNecessary(item);
                FishingLog.Log(
                    $"[CMD] fish_giveitem | 玩家: {player.UniqueMultiplayerID} | 物品: {item.DisplayName} ({itemId}) | 品质: {objQuality(quality)} | 数量: {count}",
                    LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"fish_giveitem 失败: {ex.Message}（物品 ID 无效？）", LogLevel.Error);
            }
        }

        private static string objQuality(int q)
        {
            return q switch { 1 => "银", 2 => "金", >= 4 => "铱", _ => "普通" };
        }

        /// <summary>BATCH-062/064: 导出星星候选贴图区域 PNG 到 Mod 目录 dump\\（供识图选素材；纹理来自当前运行实例）。
        /// BATCH-064: 追加打印当前贴图宽度（版本感知依据）与成就 tab 星星 (656,80) 区域导出。</summary>
        private void OnCommandDumpStars(string command, string[] args)
        {
            try
            {
                string dumpDir = System.IO.Path.Combine(Helper.DirectoryPath, "dump");
                Directory.CreateDirectory(dumpDir);

                FishingLog.Log(
                    $"[dumpstars] 当前 Game1.mouseCursors = {Game1.mouseCursors.Width}x{Game1.mouseCursors.Height} | " +
                    $"viewport = {Game1.viewport.Width}x{Game1.viewport.Height} | uiScale = {Game1.options.uiScale} | zoomLevel = {Game1.options.zoomLevel}",
                    LogLevel.Info);

                // 1. mouseCursors 成就 tab 星星区域 (656,80,16,16) ×16（原生成就 tab 图标坐标）
                DumpRegion("tab-achieve-656-80", Game1.mouseCursors, new Rectangle(656, 80, 16, 16), 16, dumpDir);
                // 2. mouseCursors 全图缩略（宽 512）
                DumpTexture("cursors-full", Game1.mouseCursors, 512, dumpDir);
                // 3. mouseCursors_1_6 挑战星区域 (200,192,64,64) ×8
                DumpRegion("cursors16-challenge-star-area", Game1.mouseCursors_1_6, new Rectangle(200, 192, 64, 64), 8, dumpDir);
                // 4. hats 皇冠同行区域 (0,780,96,80) ×8（Infinity Crown 在 (20,800)）
                DumpRegion("hats-crown-row", Game1.content.Load<Texture2D>("Characters\\Farmer\\hats"), new Rectangle(0, 780, 96, 80), 8, dumpDir);
                // 5. 当前运行时大金星区域 (280,188,94,105) ×16（BATCH-064 当前实现，供对照）
                DumpRegion("bigstar-280-188", Game1.mouseCursors, new Rectangle(280, 188, 94, 105), 16, dumpDir);

                FishingLog.Log($"✓ 贴图导出完成 → {dumpDir}（共 5 个 PNG + 贴图尺寸日志）", LogLevel.Info);
            }
            catch (Exception ex)
            {
                FishingLog.Log($"fish_dumpstars 失败: {ex}", LogLevel.Error);
            }
        }

        private static void DumpTexture(string name, Texture2D tex, int maxWidth, string dir)
        {
            if (tex == null || tex.IsDisposed)
                return;
            Color[] pixels = new Color[tex.Width * tex.Height];
            tex.GetData(pixels);
            float scale = (float)maxWidth / tex.Width;
            int w = maxWidth;
            int h = Math.Max(1, (int)Math.Round(tex.Height * scale));
            Color[] scaled = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = Math.Min(tex.Width - 1, (int)(x / scale));
                    int sy = Math.Min(tex.Height - 1, (int)(y / scale));
                    scaled[y * w + x] = pixels[sy * tex.Width + sx];
                }
            string path = System.IO.Path.Combine(dir, name + ".png");
            SavePng(path, w, h, scaled);
        }

        private static void DumpRegion(string name, Texture2D tex, Rectangle region, int zoom, string dir)
        {
            if (tex == null || tex.IsDisposed)
                return;
            Color[] pixels = new Color[tex.Width * tex.Height];
            tex.GetData(pixels);
            int w = region.Width * zoom;
            int h = region.Height * zoom;
            Color[] outPixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = region.X + x / zoom;
                    int sy = region.Y + y / zoom;
                    if (sx < tex.Width && sy < tex.Height)
                        outPixels[y * w + x] = pixels[sy * tex.Width + sx];
                }
            string path = System.IO.Path.Combine(dir, name + ".png");
            SavePng(path, w, h, outPixels);
        }

        /// <summary>手写 PNG 写入（RGBA、8 位、无过滤），供诊断导出使用。</summary>
        private static void SavePng(string path, int width, int height, Color[] rgba)
        {
            using FileStream fs = new FileStream(path, FileMode.Create);
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            fs.Write(signature, 0, signature.Length);

            void WriteChunk(string type, byte[] payload)
            {
                byte[] len = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(payload.Length));
                fs.Write(len, 0, 4);
                byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
                fs.Write(typeBytes, 0, 4);
                fs.Write(payload, 0, payload.Length);
                uint crc = PngCrc32(typeBytes, payload);
                byte[] crcBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)crc));
                fs.Write(crcBytes, 0, 4);
            }

            byte[] ihdr = new byte[13];
            WriteIntBE(ihdr, 0, width);
            WriteIntBE(ihdr, 4, height);
            ihdr[8] = 8; // bit depth
            ihdr[9] = 6; // color type RGBA
            WriteChunk("IHDR", ihdr);

            using MemoryStream idat = new MemoryStream();
            using (System.IO.Compression.ZLibStream z = new System.IO.Compression.ZLibStream(
                idat, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                byte[] row = new byte[width * 4 + 1];
                for (int y = 0; y < height; y++)
                {
                    row[0] = 0; // filter none
                    for (int x = 0; x < width; x++)
                    {
                        Color c = rgba[y * width + x];
                        int o = 1 + x * 4;
                        row[o] = c.R;
                        row[o + 1] = c.G;
                        row[o + 2] = c.B;
                        row[o + 3] = c.A;
                    }
                    z.Write(row, 0, row.Length);
                }
            }
            WriteChunk("IDAT", idat.ToArray());
            WriteChunk("IEND", Array.Empty<byte>());
        }

        private static void WriteIntBE(byte[] b, int offset, int value)
        {
            b[offset] = (byte)(value >> 24);
            b[offset + 1] = (byte)(value >> 16);
            b[offset + 2] = (byte)(value >> 8);
            b[offset + 3] = (byte)value;
        }

        private static uint PngCrc32(byte[] type, byte[] payload)
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            uint crc = 0xFFFFFFFF;
            foreach (byte b in type)
                crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (byte b in payload)
                crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }

        private string GetRankName(int level)
        {
            return ModHelper.Translation.Get(Utils.DifficultyCalculator.GetRankKey(level));
        }

        /// <summary>BATCH-035 自动化验收：确定性自测（只读生产函数 + 本地种子随机，不改存档、不写玩家状态）。</summary>
        private void OnCommandSelfTest(string command, string[] args)
        {
            FishingLog.Log("======== BATCH-035 自动自测 ========", LogLevel.Info);
            int pass = 0, fail = 0;
            void Report(string name, bool ok, string detail = "")
            {
                FishingLog.Log($"{(ok ? "[PASS]" : "[FAIL]")} {name}{(detail.Length > 0 ? " | " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);
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
                FishingLog.Log("[SKIP] 排位检查: 当前无皇冠鱼（fish_addstar / fish_addstars 添加后重跑）", LogLevel.Warn);
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

            // 7. BATCH-039: α 曲线锚点（纯函数只读；不写存档）
            Report("α曲线: 0 皇冠 = 0%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(0)) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(0):P1}");
            Report("α曲线: 5 皇冠 = 2%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(5) - 0.02) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(5):P1}");
            Report("α曲线: 10 皇冠 = 7%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(10) - 0.07) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(10):P1}");
            Report("α曲线: 20 皇冠 = 20%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(20) - 0.20) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(20):P1}");
            Report("α曲线: 30 皇冠 = 35%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(30) - 0.35) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(30):P1}");
            Report("α曲线: 61 皇冠 = 100%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(61) - 1.0) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(61):P1}");
            Report("α曲线: 超过 61 钳制 = 100%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(99) - 1.0) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(99):P1}");
            Report("α曲线: 15 皇冠中点 = 13.5%", Math.Abs(DifficultyManager.GetAlphaFromCrowns(15) - 0.135) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(15):P3}");
            Report("α曲线: 40 皇冠 = 35%+65%×10/31", Math.Abs(DifficultyManager.GetAlphaFromCrowns(40) - (float)(0.35 + 0.65 * 10.0 / 31.0)) < 1e-6, $"实际 {DifficultyManager.GetAlphaFromCrowns(40):P2}");

            // 8. BATCH-039: i18n 键（10 条巅峰 + 20 条持久战奖励）
            int peakKeys = 0;
            for (int i = 1; i <= 10; i++)
            {
                string v = ModHelper.Translation.Get($"hud.peak.{i}");
                if (!string.IsNullOrWhiteSpace(v) && v != $"hud.peak.{i}") peakKeys++;
            }
            Report("i18n: hud.peak.1~10", peakKeys == 10, $"{peakKeys}/10");
            int rewardKeys = 0;
            for (int i = 1; i <= 20; i++)
            {
                string v = ModHelper.Translation.Get($"hud.battleReward.{i}");
                if (!string.IsNullOrWhiteSpace(v) && v != $"hud.battleReward.{i}") rewardKeys++;
            }
            Report("i18n: hud.battleReward.1~20", rewardKeys == 20, $"{rewardKeys}/20");

            // 9. BATCH-039: 持久战测试标志生命周期（薄控制，不写存档）
            float idlePersist = ConsumeForcePerseveranceSeconds();
            _forcePerseveranceSeconds = 30;
            float consumedPersist = ConsumeForcePerseveranceSeconds();
            float clearedPersist = ConsumeForcePerseveranceSeconds();
            Report("持久战标志: 空闲0→置位30→消费30→清除0", idlePersist == 0f && consumedPersist == 30f && clearedPersist == 0f);

            // 10. BATCH-040: 日志开关与限频缓存（只读自测，不写日志缓存）
            bool cfgLogging = Config?.EnableLogging ?? true;
            Report("日志: config 开关与日志门一致", FishingLog.Enabled == cfgLogging,
                $"config={cfgLogging} 门={FishingLog.Enabled}");
            Report("日志: 限频缓存有界", FishingLog.RateLimitCacheCount() <= FishingLog.MaxRateLimitEntries,
                $"{FishingLog.RateLimitCacheCount()}/{FishingLog.MaxRateLimitEntries}");

            // 11. BATCH-053: 加速度增幅锚点曲线（纯函数只读）
            Report("增幅锚点: 0级=10%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(0) - 0.10f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(0):P0}");
            Report("增幅锚点: 50级=20%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(50) - 0.20f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(50):P0}");
            Report("增幅锚点: 70级=40%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(70) - 0.40f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(70):P0}");
            Report("增幅锚点: 80级=70%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(80) - 0.70f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(80):P0}");
            Report("增幅锚点: 90级=100%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(90) - 1.00f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(90):P0}");
            Report("增幅锚点: 100级=100%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(100) - 1.00f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(100):P0}");
            Report("增幅锚点: 88级=94%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(88) - 0.94f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(88):P0}");
            Report("增幅锚点: 96级=100%", Math.Abs(Patches.BobberBarPatches.GetAccelerationTier(96) - 1.00f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetAccelerationTier(96):P0}");

            // 12. BATCH-058T: 前摇旋转角曲线（纯函数只读；0.77s 转到 ±70°、0.11s 转回 0°）
            Report("前摇旋转: 起点 0s = 0°", Math.Abs(Patches.BobberBarPatches.GetJumpWindupRotationAt(0f, false)) < 1e-3, $"实际 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0f, false):F1}°");
            Report("前摇旋转: 0.385s = 35°", Math.Abs(Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, false) - 35f) < 1e-3, $"实际 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, false):F1}°");
            Report("前摇旋转: 0.77s 峰值 = 70°", Math.Abs(Patches.BobberBarPatches.GetJumpWindupRotationAt(0.77f, false) - 70f) < 1e-3, $"实际 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0.77f, false):F1}°");
            Report("前摇旋转: 0.88s 转回 = 0°", Math.Abs(Patches.BobberBarPatches.GetJumpWindupRotationAt(0.88f, false)) < 1e-3, $"实际 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0.88f, false):F1}°");
            Report("前摇旋转: 上跳负角/下跳正角", Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, true) < 0f && Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, false) > 0f,
                $"上跳 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, true):F1}° / 下跳 {Patches.BobberBarPatches.GetJumpWindupRotationAt(0.385f, false):F1}°");

            // 13. BATCH-059A: 跳鱼间隔分档（纯函数只读；力竭降档后跳频跟随，数值越小跳得越频繁）
            Report("跳鱼间隔: 150 级 = 8s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(150f) - 8f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(150f):F0}s");
            Report("跳鱼间隔: 250 级 = 8s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(250f) - 8f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(250f):F0}s");
            Report("跳鱼间隔: 251 级 = 6s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(251f) - 6f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(251f):F0}s");
            Report("跳鱼间隔: 351 级 = 5s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(351f) - 5f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(351f):F0}s");
            Report("跳鱼间隔: 451 级 = 4s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(451f) - 4f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(451f):F0}s");
            Report("跳鱼间隔: 551 级 = 3s", Math.Abs(Patches.BobberBarPatches.GetJumpInterval(551f) - 3f) < 1e-4, $"实际 {Patches.BobberBarPatches.GetJumpInterval(551f):F0}s");

            // BATCH-061: 每日收获限额自测（纯函数；不触碰真实计数状态）
            bool limitedVanilla = DifficultyManager.IsDailyHarvestLimited("(O)128");
            bool limitedMod = DifficultyManager.IsDailyHarvestLimited("(O)99999");
            bool limitedTrash = DifficultyManager.IsDailyHarvestLimited("(O)168");
            Report("限额: 原版鱼不限", !limitedVanilla, $"128 → {limitedVanilla}");
            Report("限额: Mod 鱼限", limitedMod, $"99999 → {limitedMod}");
            Report("限额: 垃圾限", limitedTrash, $"168 → {limitedTrash}");
            Report("限额: 上限=333", DifficultyManager.DailyHarvestLimit == 333, $"实际 {DifficultyManager.DailyHarvestLimit}");

            // BATCH-065: 蟹笼收获按水藻类非鱼规则（纯函数只读；不触碰任何状态）
            bool crabLobster = DifficultyManager.IsNonFishItem("(O)715");
            bool crabOyster = DifficultyManager.IsNonFishItem("(O)723");
            bool crabNotVanillaFish = !DifficultyManager.IsNonFishItem("(O)128");
            bool crabNonFishLevelCap = DifficultyManager.GetMaxDifficultyLevel("(O)715") == 8;
            bool crabQuantity8 = Utils.DifficultyCalculator.GetQuantityMultiplier(8) == 8;
            Report("蟹笼: 龙虾=非鱼(上限8)", crabLobster, $"715 → {crabLobster}");
            Report("蟹笼: 牡蛎=非鱼", crabOyster, $"723 → {crabOyster}");
            Report("蟹笼: 狗鱼仍为真鱼", crabNotVanillaFish, $"128 → {DifficultyManager.IsNonFishItem("(O)128")}");
            Report("蟹笼: 上限=8", crabNonFishLevelCap, $"715 上限 {DifficultyManager.GetMaxDifficultyLevel("(O)715")}");
            Report("蟹笼: 数量倍数 8级=8", crabQuantity8, $"实际 {Utils.DifficultyCalculator.GetQuantityMultiplier(8)}");

            FishingLog.Log($"======== 自测结果: {pass} 通过 / {fail} 失败 ========", fail == 0 ? LogLevel.Info : LogLevel.Error);
        }

        /// <summary>BATCH-035 自动化验收：助战观测统计（会话内内存观测；fish_assiststats [clear]）。</summary>
        private void OnCommandAssistStats(string command, string[] args)
        {
            if (args.Length > 0 && args[0] == "clear")
            {
                Patches.BobberBarPatches.ClearAssistObservations();
                FishingLog.Log("✓ 助战观测已清空", LogLevel.Info);
                return;
            }

            var obs = Patches.BobberBarPatches.GetAssistObservations();
            FishingLog.Log("======== 助战观测统计（会话内内存，上限 500）========", LogLevel.Info);
            FishingLog.Log($"观测次数: {obs.Count}/500", LogLevel.Info);
            if (obs.Count == 0)
            {
                FishingLog.Log("无观测：用 fish_assist 强制触发或正常钓鱼触发助战，触发后自动累积", LogLevel.Warn);
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

            FishingLog.Log($"平均助战等级: {totalLevel / obs.Count:F2}", LogLevel.Info);
            FishingLog.Log($"低排位组 (<0.33): n={lowN} 平均 {(lowN > 0 ? lowSum / lowN : 0):F2}（期望≈13.5）| 40级 {low40} 次, 0级 {low0} 次", LogLevel.Info);
            FishingLog.Log($"中排位组 (0.33~0.67): n={midN} 平均 {(midN > 0 ? midSum / midN : 0):F2}（期望≈20）", LogLevel.Info);
            FishingLog.Log($"高排位组 (>0.67): n={highN} 平均 {(highN > 0 ? highSum / highN : 0):F2}（期望≈26.5）| 40级 {high40} 次, 0级 {high0} 次", LogLevel.Info);
            if (lowN > 0 && highN > 0)
            {
                double p40Low = low40 / (double)lowN, p40High = high40 / (double)highN;
                double p0Low = low0 / (double)lowN, p0High = high0 / (double)highN;
                FishingLog.Log($"40级比例: 低组 {p40Low:P2} vs 高组 {p40High:P2}（期望比值≈30）", LogLevel.Info);
                FishingLog.Log($"0级比例: 低组 {p0Low:P2} vs 高组 {p0High:P2}（期望比值≈1/30）", LogLevel.Info);
                FishingLog.Log($"低组+高组平均等级和: {(lowSum / lowN) + (highSum / highN):F2}（期望≈40）", LogLevel.Info);
            }
            FishingLog.Log("提示: 观测为会话内内存（不写存档），fish_assiststats clear 可清空；样本越多越接近期望。", LogLevel.Info);
        }

        #endregion
    }
}
