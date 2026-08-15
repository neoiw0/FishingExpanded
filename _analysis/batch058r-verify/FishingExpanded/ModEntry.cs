using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FishingExpanded.Data;
using FishingExpanded.Patches;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace FishingExpanded;

public class ModEntry : Mod
{
	private static bool _forceAssistNext;

	private static int? _forcePerseveranceSeconds;

	private int _npcCheckCounter;

	private int _cleanupCounter;

	private int _hudQueueCounter;

	private static bool _resetPending;

	private static bool _resetSequenceStarted;

	private static int _resetArmedScreenId;

	private static long _resetArmedPlayerId;

	public static ModEntry Instance { get; private set; }

	public static IModHelper ModHelper => ((Mod)Instance).Helper;

	public static IMonitor ModMonitor => ((Mod)Instance).Monitor;

	public static ModConfig Config { get; private set; }

	public override void Entry(IModHelper helper)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		Instance = this;
		Config = helper.ReadConfig<ModConfig>();
		FishingLog.Enabled = Config.EnableLogging;
		FishingLog.Log("FishingExpanded 正在初始化...", (LogLevel)2);
		try
		{
			Harmony val = new Harmony(((Mod)this).ModManifest.UniqueID);
			val.PatchAll();
			FishingLog.Log("Harmony Patches 注册成功", (LogLevel)1);
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.Player.Warped += OnWarped;
			RegisterConsoleCommands();
			FishingLog.Log("FishingExpanded 初始化完成", (LogLevel)2);
		}
		catch (Exception value)
		{
			FishingLog.Log($"初始化失败: {value}", (LogLevel)4);
		}
	}

	private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
	{
		DifficultyManager.LoadData(Game1.player);
		DifficultyManager.BackfillLegendaryCrowns(Game1.player);
		HUDNotifier.ClearPending();
		GiantFishManager.ResetForSave();
		FishingLog.Log("=== FishingExpanded 存档加载完成 ===", (LogLevel)2);
	}

	private void OnSaving(object sender, SavingEventArgs e)
	{
		DifficultyManager.SaveAll();
		FishingLog.Log("已保存钓鱼难度数据", (LogLevel)1);
	}

	private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
	{
		DifficultyManager.UnloadData();
		HUDNotifier.ClearPending();
		FishingRodPatches.ClearPending();
		GiantFishManager.ResetForSave();
		FishingLog.Log("已清除 FishingExpanded 当前玩家缓存", (LogLevel)1);
	}

	private void OnDayStarted(object sender, DayStartedEventArgs e)
	{
		FishingLog.Log($"=== FishingExpanded 新的一天开始 (Day {Game1.dayOfMonth}) ===", (LogLevel)2);
		GiantFishManager.OnDayStarted();
	}

	private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
	{
		_hudQueueCounter++;
		if (_hudQueueCounter >= 15)
		{
			_hudQueueCounter = 0;
			HUDNotifier.ProcessQueue();
		}
		_npcCheckCounter++;
		if (_npcCheckCounter >= 60)
		{
			_npcCheckCounter = 0;
			GiantFishManager.CheckAndTriggerNPCReactions();
		}
		_cleanupCounter++;
		if (_cleanupCounter >= 300)
		{
			_cleanupCounter = 0;
			BobberBarPatches.PeriodicCleanup();
		}
		TryStartResetSequence();
	}

	private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
	{
		IGenericModConfigMenuApi api = ((Mod)this).Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (api != null)
		{
			api.Register(((Mod)this).ModManifest, delegate
			{
				Config = new ModConfig();
			}, delegate
			{
				((Mod)this).Helper.WriteConfig<ModConfig>(Config);
			});
			api.AddBoolOption(((Mod)this).ModManifest, () => Config.EnableLogging, delegate(bool value)
			{
				Config.EnableLogging = value;
				FishingLog.Enabled = value;
			}, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.enableLogging.name")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.enableLogging.tooltip")));
			api.AddBoolOption(((Mod)this).ModManifest, () => Config.EnableChallengeBackboard, delegate(bool value)
			{
				Config.EnableChallengeBackboard = value;
			}, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.challengeBackboard.name")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.challengeBackboard.tooltip")));
			RegisterGmcmResetSection(api);
		}
	}

	private void RegisterGmcmResetSection(IGenericModConfigMenuApi api)
	{
		api.AddSectionTitle(((Mod)this).ModManifest, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.section")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.sectionTooltip")));
		api.AddParagraph(((Mod)this).ModManifest, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.warning1")));
		api.AddParagraph(((Mod)this).ModManifest, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.warning2")));
		api.AddParagraph(((Mod)this).ModManifest, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.warning3")));
		api.AddBoolOption(((Mod)this).ModManifest, () => _resetPending, delegate(bool value)
		{
			_resetPending = value;
			_resetArmedScreenId = Context.ScreenId;
			Farmer player = Game1.player;
			_resetArmedPlayerId = ((player != null) ? player.UniqueMultiplayerID : 0);
			if (!value)
			{
				_resetSequenceStarted = false;
				_resetArmedPlayerId = 0L;
			}
		}, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.name")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.tooltip")), "reset-challenge-data");
	}

	private void TryStartResetSequence()
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		if (_resetPending && !_resetSequenceStarted && Context.IsWorldReady && Game1.player != null && Game1.currentLocation != null && !Game1.eventUp && Game1.activeClickableMenu == null && Context.ScreenId == _resetArmedScreenId && Game1.player.UniqueMultiplayerID == _resetArmedPlayerId)
		{
			_resetSequenceStarted = true;
			Game1.currentLocation.createQuestionDialogue(Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.confirmPrompt")), (Response[])(object)new Response[2]
			{
				new Response("reset_confirm", Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.confirmYes"))),
				new Response("reset_cancel", Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.reset.confirmNo")))
			}, new afterQuestionBehavior(OnResetSequenceAnswer), (NPC)null);
		}
	}

	private void OnResetSequenceAnswer(Farmer who, string answer)
	{
		_resetSequenceStarted = false;
		_resetPending = false;
		long num = ((who != null) ? who.UniqueMultiplayerID : 0);
		if (num != _resetArmedPlayerId)
		{
			FishingLog.Log($"[GMCM] 重置应答者与发起者不一致，已忽略 | 应答: {num} | 发起: {_resetArmedPlayerId}", (LogLevel)3);
			_resetArmedPlayerId = 0L;
			return;
		}
		_resetArmedPlayerId = 0L;
		if (!answer.Equals("reset_confirm", StringComparison.OrdinalIgnoreCase))
		{
			FishingLog.Log("[GMCM] 重置已取消，未修改任何存档数据", (LogLevel)2);
			return;
		}
		DifficultyManager.ClearAllData(who);
		ShowResetHudMessage("config.reset.done");
		FishingLog.Log($"[GMCM] 钓鱼挑战数据已重置为刚安装状态 | 玩家: {who.UniqueMultiplayerID}", (LogLevel)2);
	}

	private void ShowResetHudMessage(string translationKey)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		if (Game1.player != null)
		{
			Game1.addHUDMessage(new HUDMessage(Translation.op_Implicit(((Mod)this).Helper.Translation.Get(translationKey))));
		}
	}

	private static Farmer TryParseTargetPlayer(string[] args, ref int offset, bool allowLoneIndex = false)
	{
		if (args == null || args.Length <= offset)
		{
			return null;
		}
		if (int.TryParse(args[offset], out var result) && result >= 1)
		{
			List<Farmer> list = new List<Farmer>(Game1.getAllFarmers());
			if (result <= list.Count && (allowLoneIndex || args.Length > offset + 1))
			{
				offset++;
				return list[result - 1];
			}
		}
		return null;
	}

	private void OnWarped(object sender, WarpedEventArgs e)
	{
		if (e.NewLocation is FarmHouse)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
			defaultInterpolatedStringHandler.AppendLiteral("[ModEntry] 玩家进入FarmHouse: ");
			Farmer player = e.Player;
			defaultInterpolatedStringHandler.AppendFormatted((player != null) ? new long?(player.UniqueMultiplayerID) : ((long?)null));
			defaultInterpolatedStringHandler.AppendLiteral(" | 地点: ");
			defaultInterpolatedStringHandler.AppendFormatted(e.NewLocation.Name);
			FishingLog.Log(defaultInterpolatedStringHandler.ToStringAndClear(), (LogLevel)1);
			GiantFishManager.OnEnterFarmHouse(e.Player);
		}
	}

	private void RegisterConsoleCommands()
	{
		((Mod)this).Helper.ConsoleCommands.Add("fish_setlevel", "设置某鱼的难度等级 | 用法: fish_setlevel [玩家序号] <鱼ID> <等级>（可选序号: 1=主机, 2=副机…）", (Action<string, string[]>)OnCommandSetLevel);
		((Mod)this).Helper.ConsoleCommands.Add("fish_addsuccess", "增加成功次数 | 用法: fish_addsuccess [玩家序号] <鱼ID> <次数>（可选序号: 1=主机, 2=副机…）", (Action<string, string[]>)OnCommandAddSuccess);
		((Mod)this).Helper.ConsoleCommands.Add("fish_addfail", "增加失败次数 | 用法: fish_addfail [玩家序号] <鱼ID> <次数>（可选序号: 1=主机, 2=副机…）", (Action<string, string[]>)OnCommandAddFail);
		((Mod)this).Helper.ConsoleCommands.Add("fish_info", "查看鱼的详细信息 | 用法: fish_info [玩家序号] <鱼ID>（可选序号: 1=主机, 2=副机…）", (Action<string, string[]>)OnCommandInfo);
		((Mod)this).Helper.ConsoleCommands.Add("fish_list", "列出所有已记录的鱼 | 用法: fish_list [玩家序号]（1=主机, 2=副机…）", (Action<string, string[]>)OnCommandList);
		((Mod)this).Helper.ConsoleCommands.Add("fish_clear", "清空所有钓鱼数据 | 用法: fish_clear [玩家序号] confirm", (Action<string, string[]>)OnCommandClear);
		((Mod)this).Helper.ConsoleCommands.Add("fish_addstar", "添加收藏皇冠（测试）| 用法: fish_addstar [玩家序号] <鱼ID>", (Action<string, string[]>)OnCommandAddStar);
		((Mod)this).Helper.ConsoleCommands.Add("fish_giant", "模拟巨型鱼（触发NPC反应）| 用法: fish_giant [玩家序号] <鱼ID> <倍数>", (Action<string, string[]>)OnCommandGiant);
		((Mod)this).Helper.ConsoleCommands.Add("fish_bonus", "查看收藏皇冠与鱼竿熟练度 | 用法: fish_bonus [玩家序号]（1=主机, 2=副机…）", (Action<string, string[]>)OnCommandBonus);
		((Mod)this).Helper.ConsoleCommands.Add("fish_assist", "强制下一次钓鱼小游戏触发助战（测试）| 用法: fish_assist", (Action<string, string[]>)OnCommandAssist);
		((Mod)this).Helper.ConsoleCommands.Add("fish_selftest", "BATCH-035 自动自测（概率/可计数/排位/分布/i18n/强制标志）| 用法: fish_selftest", (Action<string, string[]>)OnCommandSelfTest);
		((Mod)this).Helper.ConsoleCommands.Add("fish_assiststats", "查看/清空助战观测统计（会话内内存，上限500）| 用法: fish_assiststats [clear]", (Action<string, string[]>)OnCommandAssistStats);
		((Mod)this).Helper.ConsoleCommands.Add("fish_addstars", "批量添加收藏皇冠（完整可计数 61 鱼池，含原版传奇，提升鱼竿熟练度）| 用法: fish_addstars [玩家序号] <数量>（1=主机, 2=副机…）", (Action<string, string[]>)OnCommandAddStars);
		((Mod)this).Helper.ConsoleCommands.Add("fish_challengecrown", "设置挑战鱼饵流动金色皇冠标记（测试）| 用法: fish_challengecrown [玩家序号] <鱼ID> [0|1]", (Action<string, string[]>)OnCommandChallengeCrown);
		((Mod)this).Helper.ConsoleCommands.Add("fish_persisttest", "强制下一次钓鱼小游戏失败时按指定秒数发放持久战奖励（测试）| 用法: fish_persisttest <30|60>", (Action<string, string[]>)OnCommandPerseveranceTest);
		FishingLog.Log("控制台命令注册完成 (输入 help 查看所有命令)", (LogLevel)1);
	}

	private void OnCommandSetLevel(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 2)
		{
			FishingLog.Log("用法: fish_setlevel [玩家序号] <鱼ID> <等级>  (1=主机, 2=副机…)", (LogLevel)2);
			FishingLog.Log("例如: fish_setlevel 128 50 或 fish_setlevel 2 128 50", (LogLevel)2);
			return;
		}
		string text = args[offset];
		if (!int.TryParse(args[offset + 1], out var result))
		{
			FishingLog.Log("等级必须是数字！", (LogLevel)4);
			return;
		}
		DifficultyManager.SetDifficultyLevel(text, result, val);
		int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, val);
		FishingLog.Log($"✓ 已设置 {text} 的难度等级为 {difficultyLevel} | 玩家: {((Character)val).Name}", (LogLevel)2);
	}

	private void OnCommandAddSuccess(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 2)
		{
			FishingLog.Log("用法: fish_addsuccess [玩家序号] <鱼ID> <次数>", (LogLevel)2);
			return;
		}
		string text = args[offset];
		if (!int.TryParse(args[offset + 1], out var result) || result <= 0)
		{
			FishingLog.Log("次数必须是正整数！", (LogLevel)4);
			return;
		}
		for (int i = 0; i < result; i++)
		{
			DifficultyManager.RecordSuccess(text, 1, val);
		}
		int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, val);
		FishingLog.Log($"✓ 已为 {text} 增加 {result} 次成功，当前难度等级: {difficultyLevel} | 玩家: {((Character)val).Name}", (LogLevel)2);
	}

	private void OnCommandAddFail(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 2)
		{
			FishingLog.Log("用法: fish_addfail [玩家序号] <鱼ID> <次数>", (LogLevel)2);
			return;
		}
		string text = args[offset];
		if (!int.TryParse(args[offset + 1], out var result) || result <= 0)
		{
			FishingLog.Log("次数必须是正整数！", (LogLevel)4);
			return;
		}
		for (int i = 0; i < result; i++)
		{
			DifficultyManager.RecordFailure(text, val);
		}
		int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, val);
		FishingLog.Log($"✓ 已为 {text} 增加 {result} 次失败，当前难度等级: {difficultyLevel} | 玩家: {((Character)val).Name}", (LogLevel)2);
	}

	private void OnCommandInfo(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 1)
		{
			FishingLog.Log("用法: fish_info [玩家序号] <鱼ID>", (LogLevel)2);
			return;
		}
		string text = args[offset];
		FishStats fishStats = DifficultyManager.GetFishStats(text, val);
		if (fishStats == null)
		{
			FishingLog.Log("鱼 " + text + " 尚未记录任何数据 | 玩家: " + ((Character)val).Name, (LogLevel)2);
			return;
		}
		int difficultyLevel = fishStats.DifficultyLevel;
		bool flag = DifficultyManager.HasCollectionStar(text, val);
		FishingLog.Log("===========================================", (LogLevel)2);
		FishingLog.Log("鱼ID: " + text + " | 玩家: " + ((Character)val).Name, (LogLevel)2);
		FishingLog.Log($"成功次数: {fishStats.SuccessCount}", (LogLevel)2);
		FishingLog.Log($"失败次数: {fishStats.FailCount}", (LogLevel)2);
		FishingLog.Log($"难度等级: {difficultyLevel} ({GetRankName(difficultyLevel)})", (LogLevel)2);
		FishingLog.Log($"难度倍数: {DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel):F2}x", (LogLevel)2);
		FishingLog.Log($"数量倍数: {DifficultyCalculator.GetQuantityMultiplier(difficultyLevel)}x", (LogLevel)2);
		FishingLog.Log($"品质加成: +{DifficultyCalculator.GetQualityBonus(difficultyLevel)}", (LogLevel)2);
		FishingLog.Log("收藏星标: " + (flag ? "★ 已获得" : "未获得"), (LogLevel)2);
		FishingLog.Log("===========================================", (LogLevel)2);
	}

	private void OnCommandList(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset, allowLoneIndex: true) ?? Game1.player;
		Dictionary<string, FishStats> allFishStats = DifficultyManager.GetAllFishStats(val);
		if (allFishStats.Count == 0)
		{
			FishingLog.Log(((Character)val).Name + " 当前没有任何钓鱼记录", (LogLevel)2);
			return;
		}
		FishingLog.Log($"===== {((Character)val).Name} 已记录的鱼种（共 {allFishStats.Count} 种）=====", (LogLevel)2);
		foreach (KeyValuePair<string, FishStats> item in allFishStats)
		{
			string key = item.Key;
			FishStats value = item.Value;
			int difficultyLevel = value.DifficultyLevel;
			string value2 = (DifficultyManager.HasCollectionStar(key, val) ? " ★" : "");
			FishingLog.Log($"  {key}: 等级={difficultyLevel} ({GetRankName(difficultyLevel)}), 成功={value.SuccessCount}, 失败={value.FailCount}{value2}", (LogLevel)2);
		}
		FishingLog.Log("===========================================", (LogLevel)2);
	}

	private void OnCommandClear(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 1 || !args[offset].Equals("confirm", StringComparison.OrdinalIgnoreCase))
		{
			FishingLog.Log("⚠ 此操作将清空所有钓鱼数据！", (LogLevel)3);
			FishingLog.Log("如需确认，请输入: fish_clear confirm 或 fish_clear <玩家序号> confirm", (LogLevel)2);
		}
		else
		{
			DifficultyManager.ClearAllData(val);
			FishingLog.Log("✓ 已清空 " + ((Character)val).Name + " 的钓鱼数据", (LogLevel)2);
		}
	}

	private void OnCommandAddStar(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 1)
		{
			FishingLog.Log("用法: fish_addstar [玩家序号] <鱼ID>", (LogLevel)2);
			return;
		}
		string text = args[offset];
		DifficultyManager.RecordHighDifficulty(text, val);
		FishingLog.Log("✓ 已为 " + text + " 添加收藏皇冠 | 玩家: " + ((Character)val).Name, (LogLevel)2);
	}

	private void OnCommandGiant(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 2)
		{
			FishingLog.Log("用法: fish_giant [玩家序号] <鱼ID> <倍数>", (LogLevel)2);
			FishingLog.Log("例如: fish_giant 128 20 或 fish_giant 2 128 20", (LogLevel)2);
			return;
		}
		string text = args[offset];
		if (!int.TryParse(args[offset + 1], out var result) || result <= 0)
		{
			FishingLog.Log("倍数必须是正整数！", (LogLevel)4);
			return;
		}
		GiantFishManager.RecordGiantFish(val, text, result, result * 20);
		FishingLog.Log($"✓ 已记录巨型鱼 {text} (倍数={result}) | 玩家: {((Character)val).Name}，等待触发NPC反应", (LogLevel)2);
		FishingLog.Log("提示: 将鱼拿在手上并靠近NPC即可触发反应", (LogLevel)2);
	}

	private void OnCommandAddStars(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 1 || !int.TryParse(args[offset], out var result) || result <= 0)
		{
			FishingLog.Log("用法: fish_addstars [玩家序号] <数量>", (LogLevel)2);
			FishingLog.Log("例如: fish_addstars 20 或 fish_addstars 2 20  (给副机加20颗)", (LogLevel)2);
			return;
		}
		int num = DifficultyManager.AddCollectionStarsForTesting(val, result);
		int countableCrownCount = DifficultyManager.GetCountableCrownCount(val);
		float alpha = DifficultyManager.GetAlpha(val);
		FishingLog.Log($"✓ 已为 {((Character)val).Name} 添加 {num} 颗收藏皇冠 (目标 {result})", (LogLevel)2);
		FishingLog.Log($"可计数皇冠: {countableCrownCount}/{61} | 鱼竿熟练度: {alpha:P0}", (LogLevel)2);
		double assistChance = DifficultyCalculator.GetAssistChance(countableCrownCount, 61);
		FishingLog.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", (LogLevel)2);
		if (num < result)
		{
			FishingLog.Log("提示: 可计数 61 鱼池皇冠已用完（56 普通 + 5 原版传奇，不含 Mod 鱼）；可先 fish_clear confirm 清空后重新添加", (LogLevel)3);
		}
	}

	private void OnCommandBonus(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset, allowLoneIndex: true) ?? Game1.player;
		int collectionStarCount = DifficultyManager.GetCollectionStarCount(val);
		int countableCrownCount = DifficultyManager.GetCountableCrownCount(val);
		float alpha = DifficultyManager.GetAlpha(val);
		FishingLog.Log("===========================================", (LogLevel)2);
		FishingLog.Log("玩家: " + ((Character)val).Name, (LogLevel)2);
		FishingLog.Log($"收藏皇冠总数: {collectionStarCount}", (LogLevel)2);
		FishingLog.Log($"可计数皇冠: {countableCrownCount}/{61} | 鱼竿熟练度: {alpha:P0}", (LogLevel)2);
		double assistChance = DifficultyCalculator.GetAssistChance(countableCrownCount, 61);
		FishingLog.Log($"助战概率: {assistChance:P1}（满皇冠 10% 线性；Mod 鱼皇冠不计入）", (LogLevel)2);
		FishingLog.Log("===========================================", (LogLevel)2);
	}

	public static bool ConsumeForceAssistFlag()
	{
		bool forceAssistNext = _forceAssistNext;
		_forceAssistNext = false;
		return forceAssistNext;
	}

	public static float ConsumeForcePerseveranceSeconds()
	{
		int? forcePerseveranceSeconds = _forcePerseveranceSeconds;
		_forcePerseveranceSeconds = null;
		return ((float?)forcePerseveranceSeconds) ?? 0f;
	}

	private void OnCommandChallengeCrown(string command, string[] args)
	{
		int offset = 0;
		Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
		if (args.Length < offset + 1)
		{
			FishingLog.Log("用法: fish_challengecrown [玩家序号] <鱼ID> [0|1]", (LogLevel)2);
			FishingLog.Log("例如: fish_challengecrown 144 1 或 fish_challengecrown 2 144 1", (LogLevel)2);
			return;
		}
		string text = args[offset];
		bool flag = args.Length < offset + 2 || args[offset + 1] != "0";
		DifficultyManager.SetChallengeCrown(text, flag, val);
		bool value = DifficultyManager.HasChallengeCrown(text, val);
		FishingLog.Log($"✓ 已{(flag ? "设置" : "清除")} {text} 的挑战鱼饵流动金色皇冠标记（当前: {value}）| 玩家: {((Character)val).Name}", (LogLevel)2);
		FishingLog.Log("提示: 打开图鉴鱼类页查看皇冠是否为流动金色（视觉待真实游戏确认）", (LogLevel)2);
	}

	private void OnCommandAssist(string command, string[] args)
	{
		_forceAssistNext = true;
		int countableCrownCount = DifficultyManager.GetCountableCrownCount(Game1.player);
		double assistChance = DifficultyCalculator.GetAssistChance(countableCrownCount, 61);
		FishingLog.Log("✓ 下一次钓鱼小游戏将强制触发助战（测试）", (LogLevel)2);
		if (countableCrownCount <= 0)
		{
			FishingLog.Log("警告: 当前无可计数皇冠，助战将无法选出助战鱼；请先用 fish_addstar / fish_addstars 添加皇冠", (LogLevel)3);
			return;
		}
		FishingLog.Log($"当前可计数皇冠: {countableCrownCount}/{61} | 正常助战概率: {assistChance:P1}", (LogLevel)2);
	}

	private void OnCommandPerseveranceTest(string command, string[] args)
	{
		if (args.Length < 1 || !int.TryParse(args[0], out var result) || (result != 30 && result != 60))
		{
			FishingLog.Log("用法: fish_persisttest <30|60>", (LogLevel)2);
			FishingLog.Log("例如: fish_persisttest 60  (下一次小游戏失败时按 60 秒必发海泡布丁)", (LogLevel)2);
			return;
		}
		_forcePerseveranceSeconds = result;
		FishingLog.Log($"✓ 下一次钓鱼小游戏失败时将按 {result} 秒发放持久战奖励（30 秒=50% +3 料理；60 秒=必得海泡布丁）", (LogLevel)2);
		FishingLog.Log("提示: 鱼王小游戏豁免奖励，强制标志会被鱼王豁免消耗", (LogLevel)2);
	}

	private string GetRankName(int level)
	{
		return Translation.op_Implicit(ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
	}

	private void OnCommandSelfTest(string command, string[] args)
	{
		FishingLog.Log("======== BATCH-035 自动自测 ========", (LogLevel)2);
		int pass = 0;
		int fail = 0;
		double assistChance = DifficultyCalculator.GetAssistChance(0, 61);
		double assistChance2 = DifficultyCalculator.GetAssistChance(61, 61);
		double assistChance3 = DifficultyCalculator.GetAssistChance(20, 61);
		double assistChance4 = DifficultyCalculator.GetAssistChance(99, 61);
		Report("概率: 0 皇冠 = 0%", Math.Abs(assistChance) < 1E-09, $"实际 {assistChance:P1}");
		Report("概率: 满 61 = 10%", Math.Abs(assistChance2 - 0.1) < 1E-09, $"实际 {assistChance2:P1}");
		Report("概率: 20/61 = 3.28%", Math.Abs(assistChance3 - 0.03278688524590164) < 1E-09, $"实际 {assistChance3:P2}");
		Report("概率: 超过 61 钳制 = 10%", Math.Abs(assistChance4 - 0.1) < 1E-09, $"实际 {assistChance4:P1}");
		int collectionStarCount = DifficultyManager.GetCollectionStarCount(Game1.player);
		int countableCrownCount = DifficultyManager.GetCountableCrownCount(Game1.player);
		List<string> countableStarredFish = DifficultyManager.GetCountableStarredFish(Game1.player);
		Report("可计数: 计数=列表数 且 ≤61", countableCrownCount == countableStarredFish.Count && countableCrownCount <= 61, $"{countableCrownCount}/{61}（星标总数 {collectionStarCount}）");
		bool ok = true;
		foreach (string item in countableStarredFish)
		{
			if (!DifficultyManager.IsCountableFish(item))
			{
				ok = false;
				break;
			}
		}
		Report("可计数: 列表无 Mod 鱼", ok, $"共 {countableStarredFish.Count} 条");
		if (countableStarredFish.Count > 0)
		{
			int num = int.MaxValue;
			int num2 = int.MinValue;
			foreach (string item2 in countableStarredFish)
			{
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(item2, Game1.player);
				if (difficultyLevel < num)
				{
					num = difficultyLevel;
				}
				if (difficultyLevel > num2)
				{
					num2 = difficultyLevel;
				}
			}
			bool ok2 = true;
			foreach (string item3 in countableStarredFish)
			{
				double assistRank = DifficultyManager.GetAssistRank(item3, Game1.player, countableStarredFish);
				if (assistRank < 0.0 || assistRank > 1.0)
				{
					ok2 = false;
					break;
				}
			}
			Report("排位: 全部 ∈ [0,1]", ok2, $"难度范围 {num}~{num2}，共 {countableStarredFish.Count} 条");
		}
		else
		{
			FishingLog.Log("[SKIP] 排位检查: 当前无皇冠鱼（fish_addstar / fish_addstars 添加后重跑）", (LogLevel)3);
		}
		Random random = Game1.random;
		try
		{
			Game1.random = new Random(20260809);
			double[] array = new double[3];
			double[] array2 = new double[3];
			double[] array3 = new double[3];
			for (int i = 0; i <= 2; i++)
			{
				double rank = (double)i * 0.5;
				long[] array4 = new long[41];
				for (int j = 0; j < 50000; j++)
				{
					array4[DifficultyCalculator.GetRandomAssistLevel(rank)]++;
				}
				for (int k = 0; k <= 40; k++)
				{
					array[i] += k * array4[k];
				}
				array[i] /= 50000.0;
				array2[i] = (double)array4[0] / 50000.0;
				array3[i] = (double)array4[40] / 50000.0;
			}
			Report("分布: 中位鱼均值≈20", Math.Abs(array[1] - 20.0) < 0.5, $"实际 {array[1]:F2}");
			Report("分布: 最低+最高均值=40", Math.Abs(array[0] + array[2] - 40.0) < 0.8, $"实际 {array[0]:F2}+{array[2]:F2}");
			Report("分布: 40级比值≈30", Math.Abs(array3[2] / Math.Max(array3[0], 1E-09) - 30.0) < 6.0, $"实际 {array3[2] / Math.Max(array3[0], 1E-09):F1}");
			Report("分布: 0级镜像比值≈30", Math.Abs(array2[0] / Math.Max(array2[2], 1E-09) - 30.0) < 6.0, $"实际 {array2[0] / Math.Max(array2[2], 1E-09):F1}");
		}
		finally
		{
			Game1.random = random;
		}
		int num3 = 0;
		for (int l = 1; l <= 20; l++)
		{
			string text = Translation.op_Implicit(ModHelper.Translation.Get($"hud.assist.{l}"));
			if (!string.IsNullOrWhiteSpace(text) && text != $"hud.assist.{l}")
			{
				num3++;
			}
		}
		Report("i18n: hud.assist.1~20", num3 == 20, $"{num3}/20");
		bool flag = ConsumeForceAssistFlag();
		_forceAssistNext = true;
		bool flag2 = ConsumeForceAssistFlag();
		bool flag3 = !ConsumeForceAssistFlag();
		Report("强制标志: 空闲false→置位→消费true→清除false", !flag && flag2 && flag3);
		Report("α曲线: 0 皇冠 = 0%", (double)Math.Abs(DifficultyManager.GetAlphaFromCrowns(0)) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(0):P1}");
		Report("α曲线: 5 皇冠 = 2%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(5) - 0.02) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(5):P1}");
		Report("α曲线: 10 皇冠 = 7%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(10) - 0.07) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(10):P1}");
		Report("α曲线: 20 皇冠 = 20%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(20) - 0.2) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(20):P1}");
		Report("α曲线: 30 皇冠 = 35%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(30) - 0.35) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(30):P1}");
		Report("α曲线: 61 皇冠 = 100%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(61) - 1.0) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(61):P1}");
		Report("α曲线: 超过 61 钳制 = 100%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(99) - 1.0) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(99):P1}");
		Report("α曲线: 15 皇冠中点 = 13.5%", Math.Abs((double)DifficultyManager.GetAlphaFromCrowns(15) - 0.135) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(15):P3}");
		Report("α曲线: 40 皇冠 = 35%+65%×10/31", (double)Math.Abs(DifficultyManager.GetAlphaFromCrowns(40) - 0.5596774f) < 1E-06, $"实际 {DifficultyManager.GetAlphaFromCrowns(40):P2}");
		int num4 = 0;
		for (int m = 1; m <= 10; m++)
		{
			string text2 = Translation.op_Implicit(ModHelper.Translation.Get($"hud.peak.{m}"));
			if (!string.IsNullOrWhiteSpace(text2) && text2 != $"hud.peak.{m}")
			{
				num4++;
			}
		}
		Report("i18n: hud.peak.1~10", num4 == 10, $"{num4}/10");
		int num5 = 0;
		for (int n = 1; n <= 20; n++)
		{
			string text3 = Translation.op_Implicit(ModHelper.Translation.Get($"hud.battleReward.{n}"));
			if (!string.IsNullOrWhiteSpace(text3) && text3 != $"hud.battleReward.{n}")
			{
				num5++;
			}
		}
		Report("i18n: hud.battleReward.1~20", num5 == 20, $"{num5}/20");
		float num6 = ConsumeForcePerseveranceSeconds();
		_forcePerseveranceSeconds = 30;
		float num7 = ConsumeForcePerseveranceSeconds();
		float num8 = ConsumeForcePerseveranceSeconds();
		Report("持久战标志: 空闲0→置位30→消费30→清除0", num6 == 0f && num7 == 30f && num8 == 0f);
		bool flag4 = Config?.EnableLogging ?? true;
		Report("日志: config 开关与日志门一致", FishingLog.Enabled == flag4, $"config={flag4} 门={FishingLog.Enabled}");
		Report("日志: 限频缓存有界", FishingLog.RateLimitCacheCount() <= 64, $"{FishingLog.RateLimitCacheCount()}/{64}");
		Report("增幅锚点: 0级=10%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(0) - 0.1f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(0):P0}");
		Report("增幅锚点: 50级=20%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(50) - 0.2f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(50):P0}");
		Report("增幅锚点: 70级=40%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(70) - 0.4f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(70):P0}");
		Report("增幅锚点: 80级=70%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(80) - 0.7f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(80):P0}");
		Report("增幅锚点: 90级=100%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(90) - 1f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(90):P0}");
		Report("增幅锚点: 100级=100%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(100) - 1f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(100):P0}");
		Report("增幅锚点: 88级=94%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(88) - 0.94f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(88):P0}");
		Report("增幅锚点: 96级=100%", (double)Math.Abs(BobberBarPatches.GetAccelerationTier(96) - 1f) < 0.0001, $"实际 {BobberBarPatches.GetAccelerationTier(96):P0}");
		FishingLog.Log($"======== 自测结果: {pass} 通过 / {fail} 失败 ========", (LogLevel)((fail == 0) ? 2 : 4));
		void Report(string name, bool flag5, string detail = "")
		{
			FishingLog.Log((flag5 ? "[PASS]" : "[FAIL]") + " " + name + ((detail.Length > 0) ? (" | " + detail) : ""), (LogLevel)(flag5 ? 2 : 4));
			if (flag5)
			{
				pass++;
			}
			else
			{
				fail++;
			}
		}
	}

	private void OnCommandAssistStats(string command, string[] args)
	{
		if (args.Length != 0 && args[0] == "clear")
		{
			BobberBarPatches.ClearAssistObservations();
			FishingLog.Log("✓ 助战观测已清空", (LogLevel)2);
			return;
		}
		IReadOnlyList<BobberBarPatches.AssistObservation> assistObservations = BobberBarPatches.GetAssistObservations();
		FishingLog.Log("======== 助战观测统计（会话内内存，上限 500）========", (LogLevel)2);
		FishingLog.Log($"观测次数: {assistObservations.Count}/500", (LogLevel)2);
		if (assistObservations.Count == 0)
		{
			FishingLog.Log("无观测：用 fish_assist 强制触发或正常钓鱼触发助战，触发后自动累积", (LogLevel)3);
			return;
		}
		double num = 0.0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		double num9 = 0.0;
		double num10 = 0.0;
		double num11 = 0.0;
		foreach (BobberBarPatches.AssistObservation item in assistObservations)
		{
			num += (double)item.Level;
			if (item.Rank < 0.33)
			{
				num2++;
				num9 += (double)item.Level;
				if (item.Level == 40)
				{
					num5++;
				}
				if (item.Level == 0)
				{
					num7++;
				}
			}
			else if (item.Rank > 0.67)
			{
				num4++;
				num11 += (double)item.Level;
				if (item.Level == 40)
				{
					num6++;
				}
				if (item.Level == 0)
				{
					num8++;
				}
			}
			else
			{
				num3++;
				num10 += (double)item.Level;
			}
		}
		FishingLog.Log($"平均助战等级: {num / (double)assistObservations.Count:F2}", (LogLevel)2);
		FishingLog.Log($"低排位组 (<0.33): n={num2} 平均 {((num2 > 0) ? (num9 / (double)num2) : 0.0):F2}（期望≈13.5）| 40级 {num5} 次, 0级 {num7} 次", (LogLevel)2);
		FishingLog.Log($"中排位组 (0.33~0.67): n={num3} 平均 {((num3 > 0) ? (num10 / (double)num3) : 0.0):F2}（期望≈20）", (LogLevel)2);
		FishingLog.Log($"高排位组 (>0.67): n={num4} 平均 {((num4 > 0) ? (num11 / (double)num4) : 0.0):F2}（期望≈26.5）| 40级 {num6} 次, 0级 {num8} 次", (LogLevel)2);
		if (num2 > 0 && num4 > 0)
		{
			double value = (double)num5 / (double)num2;
			double value2 = (double)num6 / (double)num4;
			double value3 = (double)num7 / (double)num2;
			double value4 = (double)num8 / (double)num4;
			FishingLog.Log($"40级比例: 低组 {value:P2} vs 高组 {value2:P2}（期望比值≈30）", (LogLevel)2);
			FishingLog.Log($"0级比例: 低组 {value3:P2} vs 高组 {value4:P2}（期望比值≈1/30）", (LogLevel)2);
			FishingLog.Log($"低组+高组平均等级和: {num9 / (double)num2 + num11 / (double)num4:F2}（期望≈40）", (LogLevel)2);
		}
		FishingLog.Log("提示: 观测为会话内内存（不写存档），fish_assiststats clear 可清空；样本越多越接近期望。", (LogLevel)2);
	}
}
