using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading;
using FishingExpanded.Data;
using FishingExpanded.Patches;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Constants;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Tools;
using xTile.Dimensions;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("FishingExpanded")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.5.10.0")]
[assembly: AssemblyInformationalVersion("0.5.10+d0a1c9eebc1e42d99d2aa339aacf3a3a55ed15cb")]
[assembly: AssemblyProduct("FishingExpanded")]
[assembly: AssemblyTitle("FishingExpanded")]
[assembly: AssemblyVersion("0.5.10.0")]
[module: RefSafetyRules(11)]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public readonly int Version;

		public RefSafetyRulesAttribute(int P_0)
		{
			Version = P_0;
		}
	}
}
namespace FishingExpanded
{
	public class ModConfig
	{
		public bool EnableLogging { get; set; } = true;

		public bool EnableRandomFishBehavior { get; set; }

		public bool EnableFestivalFishingMods { get; set; }
	}
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
			CrabPotPatches.ClearPending();
			GiantFishManager.ResetForSave();
			FishingLog.Log("已清除 FishingExpanded 当前玩家缓存", (LogLevel)1);
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			FishingLog.Log($"=== FishingExpanded 新的一天开始 (Day {Game1.dayOfMonth}) ===", (LogLevel)2);
			GiantFishManager.OnDayStarted();
			DifficultyManager.ResetDailyHarvest();
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
			VanillaTipsIntegration.TryRegister(((Mod)this).Helper, ((Mod)this).Monitor);
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
				api.AddBoolOption(((Mod)this).ModManifest, () => Config.EnableRandomFishBehavior, delegate(bool value)
				{
					Config.EnableRandomFishBehavior = value;
				}, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.randomFishBehavior.name")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.randomFishBehavior.tooltip")));
				api.AddBoolOption(((Mod)this).ModManifest, () => Config.EnableFestivalFishingMods, delegate(bool value)
				{
					Config.EnableFestivalFishingMods = value;
				}, () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.festivalFishing.name")), () => Translation.op_Implicit(((Mod)this).Helper.Translation.Get("config.festivalFishing.tooltip")));
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
			((Mod)this).Helper.ConsoleCommands.Add("fish_giveitem", "给当前/指定玩家物品（测试）| 用法: fish_giveitem [玩家序号] <物品ID> [品质0|1|2|4] [数量]（例: fish_giveitem 265 4 10 = 10 个铱星海泡布丁）", (Action<string, string[]>)OnCommandGiveItem);
			((Mod)this).Helper.ConsoleCommands.Add("fish_dumpstars", "导出星星候选贴图区域 PNG 到 Mods\\FishingExpanded\\dump（识图选素材用）| 用法: fish_dumpstars", (Action<string, string[]>)OnCommandDumpStars);
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
			FishingLog.Log($"品质门槛: +{DifficultyCalculator.GetQualityTier(difficultyLevel)} (0=普通/1=银/2=金/4=铱, BATCH-060)", (LogLevel)2);
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
				FishingLog.Log("用法: fish_giant [玩家序号] <鱼ID> <难度等级>（BATCH-060：巨型鱼按难度等级 ≥8 触发）", (LogLevel)2);
				FishingLog.Log("例如: fish_giant 128 20 或 fish_giant 2 128 20", (LogLevel)2);
				return;
			}
			string text = args[offset];
			if (!int.TryParse(args[offset + 1], out var result) || result <= 0)
			{
				FishingLog.Log("难度等级必须是正整数！", (LogLevel)4);
				return;
			}
			GiantFishManager.RecordGiantFish(val, text, result, result * 20);
			FishingLog.Log($"✓ 已记录巨型鱼 {text} (难度等级={result}) | 玩家: {((Character)val).Name}，等待触发NPC反应", (LogLevel)2);
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

		private void OnCommandGiveItem(string command, string[] args)
		{
			int offset = 0;
			Farmer val = TryParseTargetPlayer(args, ref offset) ?? Game1.player;
			if (val == null || args.Length < offset + 1)
			{
				FishingLog.Log("用法: fish_giveitem [玩家序号] <物品ID> [品质0|1|2|4] [数量]", (LogLevel)2);
				FishingLog.Log("例如: fish_giveitem 265 4 10  (10 个铱星海泡布丁)；fish_giveitem 2 265 4 1  (给副机 1 个铱星海泡布丁)", (LogLevel)2);
				return;
			}
			string itemId = args[offset];
			string text = SpecialFishHelper.NormalizeItemId(itemId);
			int result = 0;
			int result2 = 1;
			if (args.Length > offset + 1)
			{
				int.TryParse(args[offset + 1], out result);
			}
			if (args.Length > offset + 2)
			{
				int.TryParse(args[offset + 2], out result2);
			}
			try
			{
				Item val2 = ItemRegistry.Create(text, Math.Max(1, result2), 0, false);
				Object val3 = (Object)(object)((val2 is Object) ? val2 : null);
				if (val3 != null)
				{
					Object val4 = val3;
					int quality = ((result >= 4) ? 4 : (result switch
					{
						1 => 1, 
						2 => 2, 
						_ => 0, 
					}));
					((Item)val4).Quality = quality;
				}
				val.addItemByMenuIfNecessary(val2, (behaviorOnItemSelect)null, false);
				FishingLog.Log($"[CMD] fish_giveitem | 玩家: {val.UniqueMultiplayerID} | 物品: {val2.DisplayName} ({text}) | 品质: {objQuality(result)} | 数量: {result2}", (LogLevel)2);
			}
			catch (Exception ex)
			{
				FishingLog.Log("fish_giveitem 失败: " + ex.Message + "（物品 ID 无效？）", (LogLevel)4);
			}
		}

		private static string objQuality(int q)
		{
			if (q < 4)
			{
				return q switch
				{
					1 => "银", 
					2 => "金", 
					_ => "普通", 
				};
			}
			return "铱";
		}

		private void OnCommandDumpStars(string command, string[] args)
		{
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_018a: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string text = Path.Combine(((Mod)this).Helper.DirectoryPath, "dump");
				Directory.CreateDirectory(text);
				FishingLog.Log($"[dumpstars] 当前 Game1.mouseCursors = {Game1.mouseCursors.Width}x{Game1.mouseCursors.Height} | viewport = {((Rectangle)(ref Game1.viewport)).Width}x{((Rectangle)(ref Game1.viewport)).Height} | uiScale = {Game1.options.uiScale} | zoomLevel = {Game1.options.zoomLevel}", (LogLevel)2);
				DumpRegion("tab-achieve-656-80", Game1.mouseCursors, new Rectangle(656, 80, 16, 16), 16, text);
				DumpTexture("cursors-full", Game1.mouseCursors, 512, text);
				DumpRegion("cursors16-challenge-star-area", Game1.mouseCursors_1_6, new Rectangle(200, 192, 64, 64), 8, text);
				DumpRegion("hats-crown-row", Game1.content.Load<Texture2D>("Characters\\Farmer\\hats"), new Rectangle(0, 780, 96, 80), 8, text);
				DumpRegion("bigstar-280-188", Game1.mouseCursors, new Rectangle(280, 188, 94, 105), 16, text);
				FishingLog.Log("✓ 贴图导出完成 → " + text + "（共 5 个 PNG + 贴图尺寸日志）", (LogLevel)2);
			}
			catch (Exception value)
			{
				FishingLog.Log($"fish_dumpstars 失败: {value}", (LogLevel)4);
			}
		}

		private static void DumpTexture(string name, Texture2D tex, int maxWidth, string dir)
		{
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			if (tex == null || ((GraphicsResource)tex).IsDisposed)
			{
				return;
			}
			Color[] array = (Color[])(object)new Color[tex.Width * tex.Height];
			tex.GetData<Color>(array);
			float num = (float)maxWidth / (float)tex.Width;
			int num2 = Math.Max(1, (int)Math.Round((float)tex.Height * num));
			Color[] array2 = (Color[])(object)new Color[maxWidth * num2];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < maxWidth; j++)
				{
					int num3 = Math.Min(tex.Width - 1, (int)((float)j / num));
					int num4 = Math.Min(tex.Height - 1, (int)((float)i / num));
					array2[i * maxWidth + j] = array[num4 * tex.Width + num3];
				}
			}
			string path = Path.Combine(dir, name + ".png");
			SavePng(path, maxWidth, num2, array2);
		}

		private static void DumpRegion(string name, Texture2D tex, Rectangle region, int zoom, string dir)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			if (tex == null || ((GraphicsResource)tex).IsDisposed)
			{
				return;
			}
			Color[] array = (Color[])(object)new Color[tex.Width * tex.Height];
			tex.GetData<Color>(array);
			int num = region.Width * zoom;
			int num2 = region.Height * zoom;
			Color[] array2 = (Color[])(object)new Color[num * num2];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					int num3 = region.X + j / zoom;
					int num4 = region.Y + i / zoom;
					if (num3 < tex.Width && num4 < tex.Height)
					{
						array2[i * num + j] = array[num4 * tex.Width + num3];
					}
				}
			}
			string path = Path.Combine(dir, name + ".png");
			SavePng(path, num, num2, array2);
		}

		private static void SavePng(string path, int width, int height, Color[] rgba)
		{
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			FileStream fs = new FileStream(path, FileMode.Create);
			try
			{
				byte[] array = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };
				fs.Write(array, 0, array.Length);
				byte[] array2 = new byte[13];
				WriteIntBE(array2, 0, width);
				WriteIntBE(array2, 4, height);
				array2[8] = 8;
				array2[9] = 6;
				WriteChunk("IHDR", array2);
				using MemoryStream memoryStream = new MemoryStream();
				using (ZLibStream zLibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
				{
					byte[] array3 = new byte[width * 4 + 1];
					for (int i = 0; i < height; i++)
					{
						array3[0] = 0;
						for (int j = 0; j < width; j++)
						{
							Color val = rgba[i * width + j];
							int num = 1 + j * 4;
							array3[num] = ((Color)(ref val)).R;
							array3[num + 1] = ((Color)(ref val)).G;
							array3[num + 2] = ((Color)(ref val)).B;
							array3[num + 3] = ((Color)(ref val)).A;
						}
						zLibStream.Write(array3, 0, array3.Length);
					}
				}
				WriteChunk("IDAT", memoryStream.ToArray());
				WriteChunk("IEND", Array.Empty<byte>());
			}
			finally
			{
				if (fs != null)
				{
					((IDisposable)fs).Dispose();
				}
			}
			void WriteChunk(string type, byte[] payload)
			{
				byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
				fs.Write(bytes, 0, 4);
				byte[] bytes2 = Encoding.ASCII.GetBytes(type);
				fs.Write(bytes2, 0, 4);
				fs.Write(payload, 0, payload.Length);
				uint host = PngCrc32(bytes2, payload);
				byte[] bytes3 = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)host));
				fs.Write(bytes3, 0, 4);
			}
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
			uint[] array = new uint[256];
			for (uint num = 0u; num < 256; num++)
			{
				uint num2 = num;
				for (int i = 0; i < 8; i++)
				{
					num2 = (((num2 & 1) != 0) ? (0xEDB88320u ^ (num2 >> 1)) : (num2 >> 1));
				}
				array[num] = num2;
			}
			uint num3 = uint.MaxValue;
			foreach (byte b in type)
			{
				num3 = array[(num3 ^ b) & 0xFF] ^ (num3 >> 8);
			}
			foreach (byte b2 in payload)
			{
				num3 = array[(num3 ^ b2) & 0xFF] ^ (num3 >> 8);
			}
			return num3 ^ 0xFFFFFFFFu;
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
			Report("前摇旋转: 起点 0s = 0°", (double)Math.Abs(BobberBarPatches.GetJumpWindupRotationAt(0f, jumpUp: false)) < 0.001, $"实际 {BobberBarPatches.GetJumpWindupRotationAt(0f, jumpUp: false):F1}°");
			Report("前摇旋转: 0.385s = 35°", (double)Math.Abs(BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: false) - 35f) < 0.001, $"实际 {BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: false):F1}°");
			Report("前摇旋转: 0.77s 峰值 = 70°", (double)Math.Abs(BobberBarPatches.GetJumpWindupRotationAt(0.77f, jumpUp: false) - 70f) < 0.001, $"实际 {BobberBarPatches.GetJumpWindupRotationAt(0.77f, jumpUp: false):F1}°");
			Report("前摇旋转: 0.88s 转回 = 0°", (double)Math.Abs(BobberBarPatches.GetJumpWindupRotationAt(0.88f, jumpUp: false)) < 0.001, $"实际 {BobberBarPatches.GetJumpWindupRotationAt(0.88f, jumpUp: false):F1}°");
			Report("前摇旋转: 上跳负角/下跳正角", BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: true) < 0f && BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: false) > 0f, $"上跳 {BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: true):F1}° / 下跳 {BobberBarPatches.GetJumpWindupRotationAt(0.385f, jumpUp: false):F1}°");
			Report("跳鱼间隔: 150 级 = 8s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(150f) - 8f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(150f):F0}s");
			Report("跳鱼间隔: 250 级 = 8s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(250f) - 8f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(250f):F0}s");
			Report("跳鱼间隔: 251 级 = 6s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(251f) - 6f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(251f):F0}s");
			Report("跳鱼间隔: 351 级 = 5s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(351f) - 5f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(351f):F0}s");
			Report("跳鱼间隔: 451 级 = 4s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(451f) - 4f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(451f):F0}s");
			Report("跳鱼间隔: 551 级 = 3s", (double)Math.Abs(BobberBarPatches.GetJumpInterval(551f) - 3f) < 0.0001, $"实际 {BobberBarPatches.GetJumpInterval(551f):F0}s");
			bool flag5 = DifficultyManager.IsDailyHarvestLimited("(O)128");
			bool flag6 = DifficultyManager.IsDailyHarvestLimited("(O)99999");
			bool flag7 = DifficultyManager.IsDailyHarvestLimited("(O)168");
			Report("限额: 原版鱼不限", !flag5, $"128 → {flag5}");
			Report("限额: Mod 鱼限", flag6, $"99999 → {flag6}");
			Report("限额: 垃圾限", flag7, $"168 → {flag7}");
			Report("限额: 上限=333", ok: true, $"实际 {333}");
			bool flag8 = DifficultyManager.IsNonFishItem("(O)715");
			bool flag9 = DifficultyManager.IsNonFishItem("(O)723");
			bool ok3 = !DifficultyManager.IsNonFishItem("(O)128");
			bool ok4 = DifficultyManager.GetMaxDifficultyLevel("(O)715") == 8;
			bool ok5 = DifficultyCalculator.GetQuantityMultiplier(8) == 8;
			Report("蟹笼: 龙虾=非鱼(上限8)", flag8, $"715 → {flag8}");
			Report("蟹笼: 牡蛎=非鱼", flag9, $"723 → {flag9}");
			Report("蟹笼: 狗鱼仍为真鱼", ok3, $"128 → {DifficultyManager.IsNonFishItem("(O)128")}");
			Report("蟹笼: 上限=8", ok4, $"715 上限 {DifficultyManager.GetMaxDifficultyLevel("(O)715")}");
			Report("蟹笼: 数量倍数 8级=8", ok5, $"实际 {DifficultyCalculator.GetQuantityMultiplier(8)}");
			bool ok6 = !FestivalFishingService.IsFestivalFishingActive();
			bool ok7 = !(Config?.EnableFestivalFishingMods ?? false);
			bool ok8 = !FestivalFishingService.IsVanillaFestivalMode();
			Report("节日: 非节日判定=false", ok6, $"IsFestivalFishingActive={FestivalFishingService.IsFestivalFishingActive()}");
			Report("节日: 开关默认=关", ok7, $"EnableFestivalFishingMods={Config?.EnableFestivalFishingMods}");
			Report("节日: 非节日原生门=false", ok8, $"IsVanillaFestivalMode={FestivalFishingService.IsVanillaFestivalMode()}");
			float fishingLevelGainFactor = DifficultyCalculator.GetFishingLevelGainFactor(0);
			float fishingLevelGainFactor2 = DifficultyCalculator.GetFishingLevelGainFactor(1);
			float fishingLevelGainFactor3 = DifficultyCalculator.GetFishingLevelGainFactor(5);
			float fishingLevelGainFactor4 = DifficultyCalculator.GetFishingLevelGainFactor(10);
			Report("系数: 0级=0.1", (double)Math.Abs(fishingLevelGainFactor - 0.1f) < 0.0001, $"实际 {fishingLevelGainFactor:F2}");
			Report("系数: 1级=0.1", (double)Math.Abs(fishingLevelGainFactor2 - 0.1f) < 0.0001, $"实际 {fishingLevelGainFactor2:F2}");
			Report("系数: 5级=0.5", (double)Math.Abs(fishingLevelGainFactor3 - 0.5f) < 0.0001, $"实际 {fishingLevelGainFactor3:F2}");
			Report("系数: 10级=1.0", (double)Math.Abs(fishingLevelGainFactor4 - 1f) < 0.0001, $"实际 {fishingLevelGainFactor4:F2}");
			Report("保底: 1级完美=+1", DifficultyCalculator.GetRequestedLevelGain(10, 0f, 1) == 1, $"实际 {DifficultyCalculator.GetRequestedLevelGain(10, 0f, 1)}");
			Report("保底: 1级2脱杆=+1", DifficultyCalculator.GetRequestedLevelGain(2, 0f, 1) == 1, $"实际 {DifficultyCalculator.GetRequestedLevelGain(2, 0f, 1)}");
			Report("系数: 10级完美=+10", DifficultyCalculator.GetRequestedLevelGain(10, 0f, 10) == 10, $"实际 {DifficultyCalculator.GetRequestedLevelGain(10, 0f, 10)}");
			Report("系数: 10级高难500=+20", DifficultyCalculator.GetRequestedLevelGain(10, 500f, 10) == 20, $"实际 {DifficultyCalculator.GetRequestedLevelGain(10, 500f, 10)}");
			Report("掉星: 0星×倍数10=12", DifficultyCalculator.ApplyChallengeStarMultiplier(30L, 0.4f) == 12, $"实际 {DifficultyCalculator.ApplyChallengeStarMultiplier(30L, 0.4f)}");
			Report("掉星: 0星×倍数1=1(兜底)", DifficultyCalculator.ApplyChallengeStarMultiplier(3L, 0.4f) == 1, $"实际 {DifficultyCalculator.ApplyChallengeStarMultiplier(3L, 0.4f)}");
			Report("掉星: 无掉星不变", DifficultyCalculator.ApplyChallengeStarMultiplier(30L, 1f) == 30, $"实际 {DifficultyCalculator.ApplyChallengeStarMultiplier(30L, 1f)}");
			Report("经验: 原生70普通=26", DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: false, wasPerfect: false, isBossFish: false) == 26, $"实际 {DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: false, wasPerfect: false, isBossFish: false)}");
			Report("经验: 完美+140%=62", DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: false, wasPerfect: true, isBossFish: false) == 62, $"实际 {DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: false, wasPerfect: true, isBossFish: false)}");
			Report("经验: 宝箱+120%=57", DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: true, wasPerfect: false, isBossFish: false) == 57, $"实际 {DifficultyCalculator.GetNativeExperience(0, 70f, treasureCaught: true, wasPerfect: false, isBossFish: false)}");
			Report("经验: Boss×5=195", DifficultyCalculator.GetNativeExperience(0, 110f, treasureCaught: false, wasPerfect: false, isBossFish: true) == 195, $"实际 {DifficultyCalculator.GetNativeExperience(0, 110f, treasureCaught: false, wasPerfect: false, isBossFish: true)}");
			Report("经验: 品质4=38", DifficultyCalculator.GetNativeExperience(4, 70f, treasureCaught: false, wasPerfect: false, isBossFish: false) == 38, $"实际 {DifficultyCalculator.GetNativeExperience(4, 70f, treasureCaught: false, wasPerfect: false, isBossFish: false)}");
			Report("钳制: 低于原生→原生", DifficultyCalculator.GetExperienceDifficulty(35f, 70f) == 70f, $"实际 {DifficultyCalculator.GetExperienceDifficulty(35f, 70f):F0}");
			Report("钳制: 力竭80→原生110", DifficultyCalculator.GetExperienceDifficulty(80f, 110f) == 110f, $"实际 {DifficultyCalculator.GetExperienceDifficulty(80f, 110f):F0}");
			Report("钳制: 超上限→120", DifficultyCalculator.GetExperienceDifficulty(2500f, 70f) == 120f, $"实际 {DifficultyCalculator.GetExperienceDifficulty(2500f, 70f):F0}");
			Report("钳制: 中间不变", DifficultyCalculator.GetExperienceDifficulty(100f, 70f) == 100f, $"实际 {DifficultyCalculator.GetExperienceDifficulty(100f, 70f):F0}");
			Report("钳制: 边界120=120", DifficultyCalculator.GetExperienceDifficulty(120f, 70f) == 120f, $"实际 {DifficultyCalculator.GetExperienceDifficulty(120f, 70f):F0}");
			Report("倍数: 0级=1", DifficultyCalculator.GetExperienceMultiplier(0) == 1, $"实际 {DifficultyCalculator.GetExperienceMultiplier(0)}");
			Report("倍数: 负数=1", DifficultyCalculator.GetExperienceMultiplier(-5) == 1, $"实际 {DifficultyCalculator.GetExperienceMultiplier(-5)}");
			Report("倍数: 5级=3(现状)", DifficultyCalculator.GetExperienceMultiplier(5) == 3, $"实际 {DifficultyCalculator.GetExperienceMultiplier(5)}");
			Report("倍数: 10级=5(保持)", DifficultyCalculator.GetExperienceMultiplier(10) == 5, $"实际 {DifficultyCalculator.GetExperienceMultiplier(10)}");
			Report("倍数: 13级=6", DifficultyCalculator.GetExperienceMultiplier(13) == 6, $"实际 {DifficultyCalculator.GetExperienceMultiplier(13)}");
			Report("倍数: 50级=12", DifficultyCalculator.GetExperienceMultiplier(50) == 12, $"实际 {DifficultyCalculator.GetExperienceMultiplier(50)}");
			Report("倍数: 100级=20", DifficultyCalculator.GetExperienceMultiplier(100) == 20, $"实际 {DifficultyCalculator.GetExperienceMultiplier(100)}");
			FishingLog.Log($"======== 自测结果: {pass} 通过 / {fail} 失败 ========", (LogLevel)((fail == 0) ? 2 : 4));
			void Report(string name, bool flag10, string detail = "")
			{
				FishingLog.Log((flag10 ? "[PASS]" : "[FAIL]") + " " + name + ((detail.Length > 0) ? (" | " + detail) : ""), (LogLevel)(flag10 ? 2 : 4));
				if (flag10)
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
}
namespace FishingExpanded.Utils
{
	public static class ChallengeDialogueGenerator
	{
		private static readonly Dictionary<string, string> HonorificKeys = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["rank.weak"] = "hud.starChallenge.honor.weak",
			["rank.elite"] = "hud.starChallenge.honor.elite",
			["rank.knight"] = "hud.starChallenge.honor.knight",
			["rank.lord"] = "hud.starChallenge.honor.lord",
			["rank.count"] = "hud.starChallenge.honor.count",
			["rank.duke"] = "hud.starChallenge.honor.duke",
			["rank.prince"] = "hud.starChallenge.honor.prince",
			["rank.emperor"] = "hud.starChallenge.honor.emperor",
			["rank.godking"] = "hud.starChallenge.honor.godking",
			["rank.divineking"] = "hud.starChallenge.honor.divineking",
			["rank.creator"] = "hud.starChallenge.honor.creator",
			["rank.taiyi"] = "hud.starChallenge.honor.taiyi"
		};

		private static readonly string[] FallbackHonorifics = new string[3] { "可敬的", "令人敬重的", "值得敬佩的" };

		private static readonly string[] FallbackResolve = new string[1] { "毅然决然" };

		private static readonly string[] FallbackActions = new string[1] { "应战" };

		public static string Generate(string fishName, int level)
		{
			try
			{
				string rankKey = DifficultyCalculator.GetRankKey(level);
				string rank = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
				string value;
				string translationKey = (HonorificKeys.TryGetValue(rankKey, out value) ? value : "hud.starChallenge.honor.weak");
				string honorific = Pick(LoadOptions(translationKey, FallbackHonorifics));
				string resolve = Pick(LoadOptions("hud.starChallenge.resolve", FallbackResolve));
				string action = Pick(LoadOptions("hud.starChallenge.action", FallbackActions));
				string text = ((rankKey == "rank.weak") ? "hud.starChallenge.template.weak" : "hud.starChallenge.template");
				return Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text, (object)new
				{
					honorific = honorific,
					fishName = (string.IsNullOrWhiteSpace(fishName) ? "未知鱼类" : fishName),
					rank = rank,
					resolve = resolve,
					action = action
				}));
			}
			catch (Exception ex)
			{
				FishingLog.Log("[ChallengeDialogue] 生成挑战宣言失败: " + ex.Message, (LogLevel)4);
				return "可敬的" + (fishName ?? "未知鱼类") + "毅然决然应战。";
			}
		}

		private static string[] LoadOptions(string translationKey, string[] fallback)
		{
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(translationKey));
			if (string.IsNullOrWhiteSpace(text) || text == translationKey)
			{
				return fallback;
			}
			string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 0)
			{
				return array;
			}
			return fallback;
		}

		private static string Pick(string[] options)
		{
			if (options == null || options.Length == 0)
			{
				return string.Empty;
			}
			if (Game1.random == null)
			{
				return options[0];
			}
			return options[Game1.random.Next(options.Length)];
		}
	}
	public static class DifficultyCalculator
	{
		public const float ExperienceDifficultyCap = 120f;

		public static readonly (float Minute, float Percent)[] ExhaustionNodes = new(float, float)[7]
		{
			(1f, 0.01f),
			(3f, 0.03f),
			(5f, 0.1f),
			(7f, 0.2f),
			(9f, 0.35f),
			(12f, 0.5f),
			(15f, 1f)
		};

		public static float GetDifficultyMultiplier(int level)
		{
			if (level < 0)
			{
				return Lerp(0.5f, 1f, (float)(level + 10) / 10f);
			}
			if (level >= 50)
			{
				return Lerp(20f, 50f, (float)(level - 50) / 50f);
			}
			if (level >= 20)
			{
				return Lerp(5f, 20f, (float)(level - 20) / 30f);
			}
			if (level >= 4)
			{
				return Lerp(1.2f, 5f, (float)(level - 4) / 16f);
			}
			return Lerp(1f, 1.2f, (float)level / 4f);
		}

		public static int GetQuantityMultiplier(int level)
		{
			if (level <= 0)
			{
				return 1;
			}
			return level;
		}

		public static float GetFishingLevelGainFactor(int baseFishingLevel)
		{
			return (float)Math.Max(1, baseFishingLevel) / 10f;
		}

		public static int GetRequestedLevelGain(int baseLevelGain, float adjustedDifficulty, int baseFishingLevel)
		{
			int num = (int)Math.Round(adjustedDifficulty / 50f);
			float fishingLevelGainFactor = GetFishingLevelGainFactor(baseFishingLevel);
			return Math.Max(1, (int)Math.Round((float)(baseLevelGain + num) * fishingLevelGainFactor));
		}

		public static long ApplyChallengeStarMultiplier(long finalStack, float starMultiplier)
		{
			if (starMultiplier >= 1f)
			{
				return finalStack;
			}
			return Math.Max(1L, (long)Math.Round((float)finalStack * starMultiplier));
		}

		public static float GetExperienceDifficulty(float passedDifficulty, float nativeDifficulty)
		{
			if (nativeDifficulty > 0f)
			{
				return Math.Max(nativeDifficulty, Math.Min(passedDifficulty, 120f));
			}
			return Math.Min(passedDifficulty, 120f);
		}

		public static int GetNativeExperience(int fishQuality, float experienceDifficulty, bool treasureCaught, bool wasPerfect, bool isBossFish)
		{
			int num = Math.Max(1, (fishQuality + 1) * 3 + (int)experienceDifficulty / 3);
			if (treasureCaught)
			{
				num += (int)((float)num * 1.2f);
			}
			if (wasPerfect)
			{
				num += (int)((float)num * 1.4f);
			}
			if (isBossFish)
			{
				num *= 5;
			}
			return num;
		}

		public static int GetExperienceMultiplier(int level)
		{
			if (level <= 0)
			{
				return 1;
			}
			if (level <= 10)
			{
				return (int)Math.Max(1.0, Math.Round((double)level * 0.5, MidpointRounding.AwayFromZero));
			}
			if (level >= 100)
			{
				return 20;
			}
			return (int)Math.Round(5f + (float)(level - 10) * (1f / 6f), MidpointRounding.AwayFromZero);
		}

		public static int GetQualityTier(int level)
		{
			if (level >= 50)
			{
				return 4;
			}
			if (level >= 25)
			{
				return 2;
			}
			if (level >= 10)
			{
				return 1;
			}
			return 0;
		}

		public static int ApplyQualityBonus(int baseQuality, int level)
		{
			return Math.Max(baseQuality, GetQualityTier(level));
		}

		public static float GetFishSizeMultiplier(int level)
		{
			if (level > 0)
			{
				return 1f + 0.1f * (float)level;
			}
			if (level < 0)
			{
				return Math.Max(0.1f, 1f - 0.05f * (float)Math.Abs(level));
			}
			return 1f;
		}

		public static float GetVisualScale(int level)
		{
			if (level <= 0)
			{
				return 1f;
			}
			return Math.Min(3.70843f, 1f + (float)level * 0.0270843f);
		}

		public static double GetAssistChance(int countableCrownCount, int crownTarget)
		{
			if (countableCrownCount <= 0 || crownTarget <= 0)
			{
				return 0.0;
			}
			return 0.1 * (double)Math.Min(countableCrownCount, crownTarget) / (double)crownTarget;
		}

		public static int GetRandomAssistLevel(double rank)
		{
			double num = Math.Max(0.0, Math.Min(1.0, rank));
			double num2 = -0.9354838709677419 + 1.8709677419354838 * num;
			double num3 = 0.0;
			for (int i = 0; i <= 40; i++)
			{
				num3 += 1.0 + num2 * (double)(i - 20) / 20.0;
			}
			double num4 = Game1.random.NextDouble() * num3;
			double num5 = 0.0;
			for (int j = 0; j <= 40; j++)
			{
				num5 += 1.0 + num2 * (double)(j - 20) / 20.0;
				if (num4 < num5)
				{
					return j;
				}
			}
			return 40;
		}

		public static float GetCatchPenaltyModifier(int level, float catchProgress)
		{
			float val = 1f;
			if (catchProgress <= 0.01f)
			{
				val = 0.5f;
			}
			else if (catchProgress <= 0.2f)
			{
				val = 0.8f;
			}
			float val2 = 1f;
			if (level < 0)
			{
				if (catchProgress <= 0.01f)
				{
					val2 = Lerp(0.2f, 1f, (float)(level + 10) / 10f);
				}
				else if (catchProgress <= 0.2f)
				{
					val2 = Lerp(0.6f, 1f, (float)(level + 10) / 10f);
				}
				else if (catchProgress <= 0.4f)
				{
					val2 = Lerp(0.99f, 1f, (float)(level + 10) / 10f);
				}
			}
			return Math.Min(val, val2);
		}

		public static float GetEscapeFailBonusModifier(int level, int consecutiveFails, float catchProgress)
		{
			float catchPenaltyModifier = GetCatchPenaltyModifier(level, catchProgress);
			float catchPenaltyModifier2 = GetCatchPenaltyModifier(-10, catchProgress);
			float t = Math.Clamp((float)consecutiveFails / 5f, 0f, 1f);
			return Lerp(catchPenaltyModifier, catchPenaltyModifier2, t);
		}

		public static float GetExhaustionPercent(float elapsedSeconds)
		{
			if (elapsedSeconds <= 0f)
			{
				return 0f;
			}
			float num = elapsedSeconds / 60f;
			for (int i = 0; i < ExhaustionNodes.Length; i++)
			{
				if (num <= ExhaustionNodes[i].Minute)
				{
					if (i == 0)
					{
						return Lerp(0f, ExhaustionNodes[i].Percent, num / ExhaustionNodes[i].Minute);
					}
					return Lerp(ExhaustionNodes[i - 1].Percent, ExhaustionNodes[i].Percent, (num - ExhaustionNodes[i - 1].Minute) / (ExhaustionNodes[i].Minute - ExhaustionNodes[i - 1].Minute));
				}
			}
			return 1f;
		}

		public static float GetExhaustedDifficulty(float originalAdjustedDifficulty, float elapsedSeconds)
		{
			return originalAdjustedDifficulty + (80f - originalAdjustedDifficulty) * GetExhaustionPercent(elapsedSeconds);
		}

		private static float Lerp(float a, float b, float t)
		{
			return a + (b - a) * Math.Max(0f, Math.Min(1f, t));
		}

		public static string GetRankKey(int level)
		{
			if (level < 1)
			{
				return "rank.weak";
			}
			if (level <= 3)
			{
				return "rank.elite";
			}
			if (level <= 6)
			{
				return "rank.knight";
			}
			if (level <= 8)
			{
				return "rank.lord";
			}
			if (level <= 15)
			{
				return "rank.count";
			}
			if (level <= 22)
			{
				return "rank.duke";
			}
			if (level <= 33)
			{
				return "rank.prince";
			}
			if (level <= 45)
			{
				return "rank.emperor";
			}
			if (level <= 66)
			{
				return "rank.godking";
			}
			if (level <= 88)
			{
				return "rank.divineking";
			}
			if (level <= 99)
			{
				return "rank.creator";
			}
			return "rank.taiyi";
		}

		public static int GetNextRankCeiling(int currentLevel)
		{
			if (currentLevel < 1)
			{
				return 3;
			}
			if (currentLevel <= 3)
			{
				return 6;
			}
			if (currentLevel <= 6)
			{
				return 8;
			}
			if (currentLevel <= 8)
			{
				return 15;
			}
			if (currentLevel <= 15)
			{
				return 22;
			}
			if (currentLevel <= 22)
			{
				return 33;
			}
			if (currentLevel <= 33)
			{
				return 45;
			}
			if (currentLevel <= 45)
			{
				return 66;
			}
			if (currentLevel <= 66)
			{
				return 88;
			}
			if (currentLevel <= 88)
			{
				return 99;
			}
			_ = 99;
			return 100;
		}

		public static bool TryGetStarfruitTeaDrop(int difficultyLevel)
		{
			if (difficultyLevel < 50)
			{
				return false;
			}
			double num = (double)difficultyLevel / 400.0;
			return Game1.random.NextDouble() < num;
		}
	}
	public static class NPCDialogueGenerator
	{
		private static readonly string[] PraiseTemplates = new string[100]
		{
			"天哪！这条{0}足足有{1}厘米长！你是怎么钓到的？", "我从没见过这么大的{0}！{1}厘米...真是不可思议！", "哇哦！{1}厘米的{0}？你一定是钓鱼大师！", "这条{0}太壮观了！{1}厘米...我都不敢相信自己的眼睛！", "你钓到了{1}厘米的{0}？这简直是传说级别的！", "看看这条{0}！{1}厘米...它可以进博物馆了！", "我的天！{1}厘米的{0}...你打破记录了吧？", "这条{0}有{1}厘米长...我这辈子都没见过这么大的鱼！", "哇...{1}厘米的{0}！你是用什么饵钓到的？", "这条{0}简直是怪兽级别！{1}厘米...太震撼了！",
			"你居然钓到了{1}厘米的{0}？你一定有魔法！", "我能摸摸这条{0}吗？{1}厘米...真是美丽的生物！", "这条{0}的{1}厘米尺寸...足够喂饱全镇了！", "哇哦！{1}厘米...这条{0}可以参加比赛了！", "你的钓鱼技术真是了不起！{1}厘米的{0}啊！", "这条{0}有{1}厘米...它一定是这片水域的霸主！", "我发誓，{1}厘米的{0}...这是我见过最大的鱼！", "你是怎么把{1}厘米的{0}拉上来的？太厉害了！", "这条{0}简直是奇迹！{1}厘米...令人敬畏！", "哇！{1}厘米的{0}...你该把它挂在墙上！",
			"我的老天...{1}厘米的{0}？你一定是钓鱼之神！", "这条{0}有{1}厘米...它看起来像是从深海来的！", "你钓到的这条{0}...{1}厘米...简直是艺术品！", "哇哦！{1}厘米...这条{0}一定力大无穷！", "这条{0}的{1}厘米尺寸...真是罕见的收获！", "我能拍张照吗？{1}厘米的{0}...太壮观了！", "你是怎么和{1}厘米的{0}搏斗的？太精彩了！", "这条{0}有{1}厘米...它一定活了很多年！", "哇！{1}厘米的{0}...这是渔夫的梦想！", "我从没想过{0}能长到{1}厘米...太神奇了！",
			"这条{0}简直是巨人！{1}厘米...令人难以置信！", "你钓到的{0}有{1}厘米...这是传奇！", "哇哦！{1}厘米...这条{0}可以上报纸了！", "这条{0}的{1}厘米尺寸...真是令人震惊！", "我打赌没人钓到过比{1}厘米更大的{0}！", "你是用什么鱼竿钓到{1}厘米的{0}的？太强了！", "这条{0}有{1}厘米...它一定是王者！", "哇！{1}厘米的{0}...你的运气真好！", "这条{0}简直是巨兽！{1}厘米...太不可思议了！", "你钓到了{1}厘米的{0}...这需要多大的耐心！",
			"我的天哪...{1}厘米的{0}？这是真的吗？", "这条{0}有{1}厘米长...我都想学钓鱼了！", "哇哦！{1}厘米...这条{0}可以写进历史！", "你是怎么发现{1}厘米的{0}的？太幸运了！", "这条{0}的{1}厘米尺寸...真是完美的标本！", "我能看看这条{0}吗？{1}厘米...太美了！", "你钓到的{0}有{1}厘米...这是大师级的！", "哇！{1}厘米的{0}...它一定很难对付！", "这条{0}简直是传说！{1}厘米...令人惊叹！", "你是怎么把{1}厘米的{0}制服的？太勇敢了！",
			"这条{0}有{1}厘米...我都想为它鼓掌了！", "哇哦！{1}厘米...这条{0}是水中之王！", "你钓到的这条{0}...{1}厘米...真是奇迹！", "我发誓，{1}厘米的{0}...这是钓鱼史上的壮举！", "这条{0}的{1}厘米尺寸...真是令人兴奋！", "我能摸摸这条{0}吗？{1}厘米...它好漂亮！", "你是怎么钓到{1}厘米的{0}的？教教我！", "这条{0}有{1}厘米...它一定是传奇生物！", "哇！{1}厘米的{0}...你该开个钓鱼学校！", "这条{0}简直是杰作！{1}厘米...太完美了！",
			"你钓到了{1}厘米的{0}...这需要多少技巧！", "我的天...{1}厘米的{0}？你是英雄！", "这条{0}有{1}厘米长...我都想和你一起钓鱼了！", "哇哦！{1}厘米...这条{0}可以载入史册！", "你是怎么找到{1}厘米的{0}的？太神奇了！", "这条{0}的{1}厘米尺寸...真是稀世珍宝！", "我能拍照留念吗？{1}厘米的{0}...太壮观了！", "你钓到的{0}有{1}厘米...这是顶级水平！", "哇！{1}厘米的{0}...它肯定力大如牛！", "这条{0}简直是奇观！{1}厘米...令人敬畏！",
			"你是怎么和{1}厘米的{0}较量的？太精彩了！", "这条{0}有{1}厘米...我都想把它画下来了！", "哇哦！{1}厘米...这条{0}是渔夫的荣耀！", "你钓到的这条{0}...{1}厘米...真是天赐良机！", "我从没见过{0}能长到{1}厘米...太惊人了！", "这条{0}简直是巨龙！{1}厘米...令人震撼！", "你钓到了{1}厘米的{0}...这是史诗级的！", "哇！{1}厘米的{0}...你该上电视了！", "这条{0}的{1}厘米尺寸...真是难以想象！", "我打赌这是有史以来最大的{0}！{1}厘米啊！",
			"你是用什么秘密方法钓到{1}厘米的{0}的？", "这条{0}有{1}厘米...它一定是神话！", "哇哦！{1}厘米...这条{0}可以改变历史！", "你钓到的{0}...{1}厘米...这是钓鱼界的奇迹！", "我的天哪...{1}厘米的{0}？我要告诉所有人！", "这条{0}有{1}厘米长...你真是钓鱼天才！", "哇！{1}厘米的{0}...它一定经历了无数风浪！", "这条{0}简直是王者！{1}厘米...太威武了！", "你是怎么征服{1}厘米的{0}的？太了不起了！", "这条{0}有{1}厘米...我都想为你写首歌了！",
			"哇哦！{1}厘米...这条{0}是大自然的杰作！", "你钓到的这条{0}...{1}厘米...真是无与伦比！", "我发誓，{1}厘米的{0}...这是我一生中最震撼的时刻！", "这条{0}的{1}厘米尺寸...真是令人激动不已！", "我能抱抱这条{0}吗？{1}厘米...它太可爱了！", "你是怎么钓到{1}厘米的{0}的？你是大师！", "这条{0}有{1}厘米...它一定是海洋之神的宠儿！", "哇！{1}厘米的{0}...你该写本钓鱼秘籍！", "这条{0}简直是艺术！{1}厘米...太优雅了！", "你钓到了{1}厘米的{0}...这需要多少勇气！"
		};

		public static string GenerateFishPraise(string fishName, int fishSize)
		{
			try
			{
				string[] array = LoadPraiseTemplates();
				int num = (int)Math.Round((double)fishSize * 2.54);
				if (Game1.random == null)
				{
					return string.Format(array[0], fishName, num);
				}
				int num2 = Game1.random.Next(array.Length);
				return string.Format(array[num2], fishName, num);
			}
			catch (Exception ex)
			{
				FishingLog.Log("[NPCDialogue] 生成对话失败 | 错误: " + ex.Message, (LogLevel)4);
				return "哇！这条" + fishName + "真大！";
			}
		}

		private static string[] LoadPraiseTemplates()
		{
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("npc.praise.templates"));
			if (string.IsNullOrWhiteSpace(text) || text == "npc.praise.templates")
			{
				return PraiseTemplates;
			}
			string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 0)
			{
				return array;
			}
			return PraiseTemplates;
		}
	}
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
}
namespace FishingExpanded.Services
{
	public static class DifficultyManager
	{
		private const int MinDifficultyLevel = -10;

		private const int MaxDifficultyLevel = 100;

		private const string PlayerDataKey = "FishingExpanded/FishDifficultyData";

		private const string LegacySaveDataKey = "FishDifficultyData";

		public const int CountableCrownTarget = 61;

		private static readonly HashSet<string> CountableFishIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"(O)128", "(O)129", "(O)130", "(O)131", "(O)132", "(O)136", "(O)137", "(O)138", "(O)139", "(O)140",
			"(O)141", "(O)142", "(O)143", "(O)144", "(O)145", "(O)146", "(O)147", "(O)148", "(O)149", "(O)150",
			"(O)151", "(O)154", "(O)155", "(O)156", "(O)158", "(O)159", "(O)160", "(O)161", "(O)162", "(O)163",
			"(O)164", "(O)165", "(O)267", "(O)269", "(O)682", "(O)698", "(O)699", "(O)700", "(O)701", "(O)702",
			"(O)704", "(O)705", "(O)706", "(O)707", "(O)708", "(O)734", "(O)775", "(O)795", "(O)796", "(O)798",
			"(O)799", "(O)800", "(O)836", "(O)837", "(O)838", "(O)898", "(O)899", "(O)900", "(O)901", "(O)902",
			"(O)Goby"
		};

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		private static readonly Dictionary<long, FishDifficultyData> _dataByPlayer = new Dictionary<long, FishDifficultyData>();

		public const int DailyHarvestLimit = 333;

		private static readonly Dictionary<long, Dictionary<string, int>> _dailyHarvestByPlayer = new Dictionary<long, Dictionary<string, int>>();

		private static readonly Dictionary<long, HashSet<string>> _dailyLimitNotifiedByPlayer = new Dictionary<long, HashSet<string>>();

		public static bool IsDailyHarvestLimited(string fishId)
		{
			string item = SpecialFishHelper.NormalizeItemId(fishId);
			return !CountableFishIds.Contains(item);
		}

		public static int ConsumeDailyHarvest(string fishId, Farmer player)
		{
			if (player == null)
			{
				return 0;
			}
			string key = SpecialFishHelper.NormalizeItemId(fishId);
			if (!_dailyHarvestByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
			{
				value = new Dictionary<string, int>();
				_dailyHarvestByPlayer[player.UniqueMultiplayerID] = value;
			}
			value.TryGetValue(key, out var value2);
			return value[key] = value2 + 1;
		}

		public static bool MarkDailyLimitNotified(string fishId, Farmer player)
		{
			if (player == null)
			{
				return false;
			}
			string item = SpecialFishHelper.NormalizeItemId(fishId);
			if (!_dailyLimitNotifiedByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
			{
				value = new HashSet<string>();
				_dailyLimitNotifiedByPlayer[player.UniqueMultiplayerID] = value;
			}
			return value.Add(item);
		}

		public static void ResetDailyHarvest()
		{
			_dailyHarvestByPlayer.Clear();
			_dailyLimitNotifiedByPlayer.Clear();
		}

		public static bool IsNonFishItem(string fishId)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(text);
			if (dataOrErrorItem == null)
			{
				return false;
			}
			if (SpecialFishHelper.IsNonFish(dataOrErrorItem.Category))
			{
				return true;
			}
			return IsCrabPotFish(text);
		}

		public static bool IsCrabPotFish(string fishId)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			try
			{
				string key = (text.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? text.Substring(3) : text);
				string value;
				return DataLoader.Fish(Game1.content).TryGetValue(key, out value) && value.Contains("trap", StringComparison.Ordinal);
			}
			catch (Exception ex)
			{
				FishingLog.Log("[DifficultyManager] 蟹笼鱼判定失败: " + ex.Message, (LogLevel)1);
				return false;
			}
		}

		public static void LoadData(Farmer player)
		{
			if (player == null)
			{
				return;
			}
			FishDifficultyData fishDifficultyData = ReadFromModData(player);
			if (fishDifficultyData == null && Context.IsMainPlayer)
			{
				try
				{
					fishDifficultyData = ModEntry.ModHelper.Data.ReadSaveData<FishDifficultyData>("FishDifficultyData");
					if (fishDifficultyData != null)
					{
						FishingLog.Log("[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据", (LogLevel)3);
					}
				}
				catch (Exception ex)
				{
					FishingLog.Log("[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: " + ex.Message, (LogLevel)4);
				}
			}
			if (fishDifficultyData == null)
			{
				fishDifficultyData = new FishDifficultyData();
			}
			NormalizeData(fishDifficultyData);
			_dataByPlayer[player.UniqueMultiplayerID] = fishDifficultyData;
			FishingLog.Log($"[DifficultyManager] 加载玩家数据完成 | 玩家: {player.UniqueMultiplayerID} | 鱼种类数: {fishDifficultyData.FishStatistics.Count} | 皇冠鱼种: {fishDifficultyData.CollectionStars.Count} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)2);
		}

		public static void SaveData(Farmer player)
		{
			if (player != null && _dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
			{
				NormalizeData(value);
				((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Character)player).modData)["FishingExpanded/FishDifficultyData"] = JsonSerializer.Serialize(value, JsonOptions);
				FishingLog.Log($"已保存玩家钓鱼难度数据 | 玩家: {player.UniqueMultiplayerID}", (LogLevel)1);
			}
		}

		public unsafe static void SaveAll()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			Enumerator enumerator = Game1.getOnlineFarmers().GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					Farmer current = ((Enumerator)(ref enumerator)).Current;
					if (_dataByPlayer.ContainsKey(current.UniqueMultiplayerID))
					{
						SaveData(current);
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
		}

		public static void UnloadData()
		{
			_dataByPlayer.Clear();
		}

		public static int GetMaxDifficultyLevel(string fishId)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (string.IsNullOrEmpty(text))
			{
				return 100;
			}
			if (!IsNonFishItem(text))
			{
				return 100;
			}
			return SpecialFishHelper.GetMaxLevelForNonFish();
		}

		public static int GetDifficultyLevel(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
			{
				return 0;
			}
			int maxDifficultyLevel = GetMaxDifficultyLevel(text);
			return Math.Max(-10, Math.Min(maxDifficultyLevel, value.DifficultyLevel));
		}

		public static int RecordSuccess(string fishId, int levelGain, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", (LogLevel)4);
				return 0;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
			{
				return 0;
			}
			if (!data.FishStatistics.TryGetValue(text, out var value))
			{
				value = new FishStats();
				data.FishStatistics[text] = value;
				FishingLog.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
			}
			int difficultyLevel = GetDifficultyLevel(text, player);
			int maxDifficultyLevel = GetMaxDifficultyLevel(text);
			int num = Math.Min(DifficultyCalculator.GetNextRankCeiling(difficultyLevel), maxDifficultyLevel);
			int val = Math.Max(0, Math.Min(levelGain, num - difficultyLevel));
			val = ((difficultyLevel >= 89) ? Math.Min(val, 6) : Math.Min(val, Math.Max(0, 89 - difficultyLevel)));
			value.SuccessCount = SaturatingAdd(value.SuccessCount, val);
			value.ConsecutiveFailCount = 0;
			int difficultyLevel2 = GetDifficultyLevel(text, player);
			if (difficultyLevel2 >= SpecialFishHelper.GetMaxLevelForNonFish() && IsNonFishItem(text) && !data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				FishingLog.Log($"[DifficultyManager] 非鱼类星星获得 | 玩家: {player.UniqueMultiplayerID} | 物品: {text} | 等级: {difficultyLevel2}", (LogLevel)2);
			}
			FishingLog.Log($"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 请求增长: +{levelGain} | 实际增长: +{val} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
			SaveData(player);
			return val;
		}

		public static void RecordFailure(string fishId, Farmer player, bool keepLevel = false)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
			{
				if (!data.FishStatistics.TryGetValue(text, out var value))
				{
					value = new FishStats();
					data.FishStatistics[text] = value;
					FishingLog.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
				}
				int difficultyLevel = GetDifficultyLevel(text, player);
				if (!keepLevel)
				{
					value.FailCount = SaturatingAdd(value.FailCount, 1);
				}
				value.ConsecutiveFailCount = SaturatingAdd(value.ConsecutiveFailCount, 1);
				int difficultyLevel2 = GetDifficultyLevel(text, player);
				FishingLog.Log($"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 连续失败次数: {value.ConsecutiveFailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}" + (keepLevel ? " | 挑战鱼饵不掉等级" : ""), (LogLevel)2);
				SaveData(player);
			}
		}

		public static int GetOrCreateChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null || player == null)
			{
				return 0;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			string key = text + "|" + difficultyLevel;
			if (data.ChallengePatternSeeds.TryGetValue(key, out var value))
			{
				return value;
			}
			int num = new Random(Guid.NewGuid().GetHashCode()).Next();
			data.ChallengePatternSeeds[key] = num;
			SaveData(player);
			FishingLog.Log($"[DifficultyManager] 挑战背板种子生成 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 等级: {difficultyLevel} | 种子: {num}", (LogLevel)2);
			return num;
		}

		public static void ClearChallengePatternSeed(string fishId, int difficultyLevel, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data != null && player != null)
			{
				string text = SpecialFishHelper.NormalizeItemId(fishId);
				string key = text + "|" + difficultyLevel;
				if (data.ChallengePatternSeeds.Remove(key))
				{
					SaveData(player);
					FishingLog.Log($"[DifficultyManager] 挑战背板种子清除 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 等级: {difficultyLevel}", (LogLevel)2);
				}
			}
		}

		public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text) && adjustedDifficulty >= 120f && !data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				FishingLog.Log($"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 调整后难度: {adjustedDifficulty:F1} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
				SaveData(player);
			}
		}

		public static int AddCollectionStarsForTesting(Farmer player, int count)
		{
			if (player == null || count <= 0)
			{
				return 0;
			}
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				return 0;
			}
			int num = 0;
			foreach (string item in BuildStarPool())
			{
				if (num >= count)
				{
					break;
				}
				string text = SpecialFishHelper.NormalizeItemId(item);
				if (!string.IsNullOrEmpty(text) && !data.CollectionStars.Contains(text))
				{
					data.CollectionStars.Add(text);
					num++;
				}
			}
			if (num > 0)
			{
				SaveData(player);
			}
			return num;
		}

		private static List<string> BuildStarPool()
		{
			List<string> list = new List<string>(CountableFishIds.Count);
			foreach (string countableFishId in CountableFishIds)
			{
				list.Add(countableFishId);
			}
			return list;
		}

		public static bool IsCountableFish(string fishId)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text))
			{
				return CountableFishIds.Contains(text);
			}
			return false;
		}

		public static int GetCountableCrownCount(Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				return 0;
			}
			int num = 0;
			foreach (string collectionStar in data.CollectionStars)
			{
				if (IsCountableFish(collectionStar))
				{
					num++;
				}
			}
			return num;
		}

		public static List<string> GetCountableStarredFish(Farmer player)
		{
			List<string> list = new List<string>();
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				return list;
			}
			foreach (string collectionStar in data.CollectionStars)
			{
				if (IsCountableFish(collectionStar))
				{
					list.Add(collectionStar);
				}
			}
			return list;
		}

		public static double GetAssistRank(string fishId, Farmer player, List<string> countableStarred)
		{
			if (countableStarred == null || countableStarred.Count == 0)
			{
				return 0.5;
			}
			int num = int.MaxValue;
			int num2 = int.MinValue;
			foreach (string item in countableStarred)
			{
				int difficultyLevel = GetDifficultyLevel(item, player);
				if (difficultyLevel < num)
				{
					num = difficultyLevel;
				}
				if (difficultyLevel > num2)
				{
					num2 = difficultyLevel;
				}
			}
			if (num2 <= num)
			{
				return 0.5;
			}
			int difficultyLevel2 = GetDifficultyLevel(fishId, player);
			return Math.Max(0.0, Math.Min(1.0, (double)(difficultyLevel2 - num) / (double)(num2 - num)));
		}

		public static float GetAlpha(Farmer player)
		{
			return GetAlphaFromCrowns(GetCountableCrownCount(player));
		}

		public static float GetAlphaFromCrowns(int countableCrowns)
		{
			int num = Math.Max(0, countableCrowns);
			if (num >= 61)
			{
				return 1f;
			}
			if (num <= 0)
			{
				return 0f;
			}
			if (num <= 5)
			{
				return 0.02f * (float)num / 5f;
			}
			if (num <= 10)
			{
				return 0.02f + 0.05f * (float)(num - 5) / 5f;
			}
			if (num <= 20)
			{
				return 0.07f + 0.13f * (float)(num - 10) / 10f;
			}
			if (num <= 30)
			{
				return 0.2f + 0.15f * (float)(num - 20) / 10f;
			}
			return 0.35f + 0.65f * (float)(num - 30) / 31f;
		}

		public static void RecordLegendaryCatch(string fishId, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录传奇鱼皇冠", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && SpecialFishHelper.IsLegendaryFish(text) && !data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				FishingLog.Log($"[DifficultyManager] ★ 传奇鱼一次钓获皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
				SaveData(player);
			}
		}

		public static void BackfillLegendaryCrowns(Farmer player)
		{
			if (player == null || !Context.IsWorldReady || player.fishCaught == null)
			{
				return;
			}
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				return;
			}
			bool flag = false;
			int[] array = default(int[]);
			foreach (string legendaryFishId in SpecialFishHelper.GetLegendaryFishIds())
			{
				if (((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)player.fishCaught).TryGetValue(legendaryFishId, ref array) && array != null && array.Length != 0 && array[0] > 0 && data.CollectionStars.Add(legendaryFishId))
				{
					flag = true;
					FishingLog.Log($"[DifficultyManager] ★ 传奇鱼回填皇冠 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {legendaryFishId} | 累计钓获: {array[0]}", (LogLevel)3);
				}
			}
			if (flag)
			{
				SaveData(player);
			}
		}

		public static bool HasCollectionStar(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data != null && !string.IsNullOrEmpty(text))
			{
				return data.CollectionStars.Contains(text);
			}
			return false;
		}

		public static bool HasChallengeCrown(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data != null && !string.IsNullOrEmpty(text))
			{
				return data.ChallengeCrowns.Contains(text);
			}
			return false;
		}

		public static void RecordChallengeCrown(string fishId, Farmer player, bool atLevel100 = false)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
			{
				return;
			}
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录挑战皇冠", (LogLevel)4);
				return;
			}
			bool flag = data.ChallengeCrowns.Add(text);
			bool flag2 = atLevel100 && data.Level100FlowCrowns.Add(text);
			if (flag || flag2)
			{
				FishingLog.Log($"[DifficultyManager] ★ 挑战皇冠记录 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 挑战皇冠数: {data.ChallengeCrowns.Count} | 100级流动皇冠: {data.Level100FlowCrowns.Count}", (LogLevel)2);
				SaveData(player);
			}
		}

		public static bool HasLevel100FlowCrown(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data != null && !string.IsNullOrEmpty(text))
			{
				return data.Level100FlowCrowns.Contains(text);
			}
			return false;
		}

		public static void SetChallengeCrown(string fishId, bool value, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text))
			{
				return;
			}
			FishDifficultyData data = GetData(player);
			if (data != null)
			{
				if (value)
				{
					data.ChallengeCrowns.Add(text);
				}
				else
				{
					data.ChallengeCrowns.Remove(text);
				}
				FishingLog.Log($"[DifficultyManager] 已设置挑战皇冠 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 值: {value}", (LogLevel)2);
				SaveData(player);
			}
		}

		public static void SetDifficultyLevel(string fishId, int targetLevel, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
			{
				targetLevel = Math.Max(-10, Math.Min(GetMaxDifficultyLevel(text), targetLevel));
				if (!data.FishStatistics.TryGetValue(text, out var value))
				{
					value = new FishStats();
					data.FishStatistics[text] = value;
				}
				value.SuccessCount = targetLevel + value.FailCount;
				FishingLog.Log($"[DifficultyManager] 已设置等级 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 目标等级: {targetLevel} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount}", (LogLevel)2);
				SaveData(player);
			}
		}

		public static int GetConsecutiveFailCount(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
			{
				return 0;
			}
			return Math.Max(0, value.ConsecutiveFailCount);
		}

		public static FishStats GetFishStats(string fishId, Farmer player)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDifficultyData data = GetData(player);
			if (data == null || string.IsNullOrEmpty(text) || SpecialFishHelper.IsLegendaryFish(text) || !data.FishStatistics.TryGetValue(text, out var value))
			{
				return null;
			}
			return value;
		}

		public static Dictionary<string, FishStats> GetAllFishStats(Farmer player)
		{
			return GetData(player)?.FishStatistics ?? new Dictionary<string, FishStats>();
		}

		public static void ClearAllData(Farmer player)
		{
			if (player == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家为null", (LogLevel)4);
				return;
			}
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
				return;
			}
			int count = data.FishStatistics.Count;
			int count2 = data.CollectionStars.Count;
			int count3 = data.ChallengeCrowns.Count;
			int count4 = data.Level100FlowCrowns.Count;
			data.FishStatistics.Clear();
			data.CollectionStars.Clear();
			data.ChallengeCrowns.Clear();
			data.Level100FlowCrowns.Clear();
			BackfillLegendaryCrowns(player);
			bool value = false;
			if (Context.IsMainPlayer)
			{
				try
				{
					ModEntry.ModHelper.Data.WriteSaveData<FishDifficultyData>("FishDifficultyData", (FishDifficultyData)null);
					value = true;
				}
				catch (Exception ex)
				{
					FishingLog.Log("[DifficultyManager] 删除旧存档级数据失败: " + ex.Message, (LogLevel)4);
				}
			}
			SaveData(player);
			FishingLog.Log($"[DifficultyManager] 已清空所有数据 | 玩家: {player.UniqueMultiplayerID} | 统计: {count} | 星标: {count2} | 挑战皇冠: {count3} | 100级流动皇冠: {count4} | 旧存档级键删除: {value}", (LogLevel)3);
		}

		public static void RecordHighDifficulty(string fishId, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				FishingLog.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
			{
				if (!data.CollectionStars.Contains(text))
				{
					data.CollectionStars.Add(text);
					FishingLog.Log($"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 可计数皇冠: {GetCountableCrownCount(player)}/{61}", (LogLevel)3);
					SaveData(player);
				}
				else
				{
					FishingLog.Log("[DifficultyManager] 鱼 " + text + " 已有收藏星标", (LogLevel)2);
				}
			}
		}

		public static int GetCollectionStarCount(Farmer player)
		{
			return GetData(player)?.CollectionStars.Count ?? 0;
		}

		private static FishDifficultyData GetData(Farmer player)
		{
			if (player == null)
			{
				return null;
			}
			if (!_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
			{
				LoadData(player);
				_dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out value);
			}
			return value;
		}

		private static FishDifficultyData ReadFromModData(Farmer player)
		{
			string text = default(string);
			if (((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Character)player).modData).TryGetValue("FishingExpanded/FishDifficultyData", ref text) && !string.IsNullOrWhiteSpace(text))
			{
				try
				{
					return JsonSerializer.Deserialize<FishDifficultyData>(text, JsonOptions);
				}
				catch (JsonException ex)
				{
					FishingLog.Log($"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {player.UniqueMultiplayerID} | 错误: {ex.Message}", (LogLevel)4);
				}
			}
			return null;
		}

		private static void NormalizeData(FishDifficultyData data)
		{
			Dictionary<string, FishStats> dictionary = new Dictionary<string, FishStats>();
			foreach (KeyValuePair<string, FishStats> item in data.FishStatistics ?? new Dictionary<string, FishStats>())
			{
				string text = SpecialFishHelper.NormalizeItemId(item.Key);
				if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text) && item.Value != null)
				{
					if (!dictionary.TryGetValue(text, out var value))
					{
						dictionary[text] = item.Value;
						continue;
					}
					value.SuccessCount = SaturatingAdd(value.SuccessCount, item.Value.SuccessCount);
					value.FailCount = SaturatingAdd(value.FailCount, item.Value.FailCount);
				}
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string item2 in data.CollectionStars ?? new HashSet<string>())
			{
				string text2 = SpecialFishHelper.NormalizeItemId(item2);
				if (!string.IsNullOrEmpty(text2))
				{
					hashSet.Add(text2);
				}
			}
			HashSet<string> hashSet2 = new HashSet<string>();
			foreach (string item3 in data.ChallengeCrowns ?? new HashSet<string>())
			{
				string text3 = SpecialFishHelper.NormalizeItemId(item3);
				if (!string.IsNullOrEmpty(text3) && !SpecialFishHelper.IsLegendaryFish(text3))
				{
					hashSet2.Add(text3);
				}
			}
			HashSet<string> hashSet3 = new HashSet<string>();
			foreach (string item4 in data.Level100FlowCrowns ?? new HashSet<string>())
			{
				string text4 = SpecialFishHelper.NormalizeItemId(item4);
				if (!string.IsNullOrEmpty(text4) && !SpecialFishHelper.IsLegendaryFish(text4))
				{
					hashSet3.Add(text4);
				}
			}
			data.FishStatistics = dictionary;
			data.CollectionStars = hashSet;
			data.ChallengeCrowns = hashSet2;
			data.Level100FlowCrowns = hashSet3;
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			foreach (KeyValuePair<string, int> item5 in data.ChallengePatternSeeds ?? new Dictionary<string, int>())
			{
				string[] array = item5.Key.Split('|');
				if (array.Length == 2)
				{
					string text5 = SpecialFishHelper.NormalizeItemId(array[0]);
					if (!string.IsNullOrEmpty(text5) && int.TryParse(array[1], out var result))
					{
						dictionary2[text5 + "|" + result] = item5.Value;
					}
				}
			}
			data.ChallengePatternSeeds = dictionary2;
		}

		private static int SaturatingAdd(int left, int right)
		{
			long num = (long)left + (long)right;
			if (num <= int.MaxValue)
			{
				if (num >= int.MinValue)
				{
					return (int)num;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
	}
	internal static class FestivalFishingService
	{
		internal static bool IsSquidFest()
		{
			return Utility.IsPassiveFestivalDay("SquidFest");
		}

		internal static bool IsTroutDerby()
		{
			return Utility.IsPassiveFestivalDay("TroutDerby");
		}

		internal static bool IsIceFestival()
		{
			if (!Game1.isFestival())
			{
				return false;
			}
			GameLocation currentLocation = Game1.currentLocation;
			if (currentLocation == null)
			{
				return false;
			}
			Event currentEvent = currentLocation.currentEvent;
			return ((currentEvent != null) ? new bool?(currentEvent.isSpecificFestival("winter8")) : ((bool?)null)) == true;
		}

		internal static bool IsFestivalFishingActive()
		{
			if (!IsSquidFest() && !IsTroutDerby())
			{
				return IsIceFestival();
			}
			return true;
		}

		internal static bool IsVanillaFestivalMode()
		{
			if (IsFestivalFishingActive())
			{
				return !ModEntry.Config.EnableFestivalFishingMods;
			}
			return false;
		}
	}
	public static class FishingLog
	{
		public const int MaxRateLimitEntries = 64;

		private static readonly Dictionary<string, long> _lastLoggedByKey = new Dictionary<string, long>();

		public static bool Enabled { get; set; } = true;

		public static void Log(string message, LogLevel level)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			if (Enabled && ModEntry.ModMonitor != null)
			{
				ModEntry.ModMonitor.Log(message, level);
			}
		}

		public static void LogRateLimited(string key, string message, LogLevel level, double intervalSeconds = 30.0)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			if (!Enabled || ModEntry.ModMonitor == null)
			{
				return;
			}
			long tickCount = Environment.TickCount64;
			if (!_lastLoggedByKey.TryGetValue(key, out var value) || tickCount - value >= (long)(intervalSeconds * 1000.0))
			{
				if (_lastLoggedByKey.Count >= 64)
				{
					_lastLoggedByKey.Clear();
				}
				_lastLoggedByKey[key] = tickCount;
				ModEntry.ModMonitor.Log(message, level);
			}
		}

		public static int RateLimitCacheCount()
		{
			return _lastLoggedByKey.Count;
		}
	}
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
	public static class HUDNotifier
	{
		private static readonly Dictionary<long, Queue<HUDMessage>> PendingByPlayer = new Dictionary<long, Queue<HUDMessage>>();

		private static readonly Dictionary<long, string> ActiveMessageText = new Dictionary<long, string>();

		private static void EnqueueMessage(Farmer player, HUDMessage message)
		{
			if (player == null || message == null)
			{
				return;
			}
			long uniqueMultiplayerID = player.UniqueMultiplayerID;
			if (!ActiveMessageText.ContainsKey(uniqueMultiplayerID))
			{
				Game1.addHUDMessage(message);
				ActiveMessageText[uniqueMultiplayerID] = message.message;
				return;
			}
			if (!PendingByPlayer.TryGetValue(uniqueMultiplayerID, out var value))
			{
				value = new Queue<HUDMessage>();
				PendingByPlayer[uniqueMultiplayerID] = value;
			}
			if (value.Count >= 5)
			{
				value.Dequeue();
			}
			value.Enqueue(message);
		}

		public static void ProcessQueue()
		{
			try
			{
				if (Game1.player == null || Game1.hudMessages == null)
				{
					return;
				}
				long uniqueMultiplayerID = Game1.player.UniqueMultiplayerID;
				if (ActiveMessageText.TryGetValue(uniqueMultiplayerID, out var activeText) && !string.IsNullOrEmpty(activeText))
				{
					if (Game1.hudMessages.Any((HUDMessage m) => m != null && m.message != null && m.message == activeText))
					{
						return;
					}
					ActiveMessageText.Remove(uniqueMultiplayerID);
				}
				if (PendingByPlayer.TryGetValue(uniqueMultiplayerID, out var value) && value.Count != 0)
				{
					HUDMessage val = value.Dequeue();
					Game1.addHUDMessage(val);
					ActiveMessageText[uniqueMultiplayerID] = val.message;
				}
			}
			catch (Exception value2)
			{
				FishingLog.Log($"HUD 提示队列驱动失败: {value2}", (LogLevel)4);
			}
		}

		public static void ClearPending()
		{
			PendingByPlayer.Clear();
			ActiveMessageText.Clear();
		}

		public static void ShowSuccessNotification(string fishId, int newLevel, int oldLevel = -1)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected O, but got Unknown
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Expected O, but got Unknown
			if (SpecialFishHelper.IsLegendaryFish(fishId))
			{
				string randomLegendaryMessage = SpecialFishHelper.GetRandomLegendaryMessage();
				EnqueueMessage(Game1.player, new HUDMessage(randomLegendaryMessage, 1));
				FishingLog.Log("[HUDNotifier] 传奇鱼提示 | 鱼ID: " + fishId + " | 文案: " + randomLegendaryMessage, (LogLevel)2);
				return;
			}
			string fishDisplayName = GetFishDisplayName(fishId);
			bool flag = DifficultyManager.IsNonFishItem(fishId);
			int num = (flag ? SpecialFishHelper.GetMaxLevelForNonFish() : 100);
			string text;
			if (newLevel >= num)
			{
				text = ((!flag) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.success.fishCap", (object)new
				{
					fishName = fishDisplayName
				})) : Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.success.nonFishCap", (object)new
				{
					fishName = fishDisplayName
				})));
			}
			else
			{
				string rankKey = DifficultyCalculator.GetRankKey(newLevel);
				string rank = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
				text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.title", (object)new
				{
					fishName = fishDisplayName,
					rank = rank
				}));
			}
			EnqueueMessage(Game1.player, new HUDMessage(text, 2));
			FishingLog.Log($"[HUDNotifier] 成功提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {newLevel} | 封顶: {newLevel >= num} | 非鱼类: {flag}", (LogLevel)1);
		}

		public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			if (!SpecialFishHelper.IsLegendaryFish(fishId) && DifficultyManager.HasCollectionStar(fishId, player) && difficultyLevel >= 1)
			{
				string fishDisplayName = GetFishDisplayName(SpecialFishHelper.NormalizeItemId(fishId));
				string text = ChallengeDialogueGenerator.Generate(fishDisplayName, difficultyLevel);
				EnqueueMessage(player, new HUDMessage(text, 2));
				FishingLog.Log($"[HUDNotifier] 星标鱼挑战宣言 | 鱼: {fishDisplayName} ({fishId}) | 等级: {difficultyLevel} | 文案: {text}", (LogLevel)1);
			}
		}

		public static void ShowDifficultyRecommendation(int difficultyLevel, Farmer player)
		{
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Expected O, but got Unknown
			try
			{
				if (player == null || difficultyLevel <= 0)
				{
					return;
				}
				float num = (float)difficultyLevel / 10f;
				int fishingLevel = player.FishingLevel;
				if (!(num <= (float)fishingLevel))
				{
					string text = num.ToString("0.##", CultureInfo.InvariantCulture);
					string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.recommendation", (object)new
					{
						recommendedLevel = text
					}));
					if (num > (float)fishingLevel * 2f)
					{
						text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.impossible")) + text2;
					}
					EnqueueMessage(player, new HUDMessage(text2, 3));
					FishingLog.Log($"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | 鱼等级: {difficultyLevel} | 建议等级: {text} | 当前钓鱼等级: {fishingLevel} | 不可能挑战: {num > (float)fishingLevel * 2f}", (LogLevel)1);
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"钓鱼等级建议提示失败: {value}", (LogLevel)4);
			}
		}

		public static void ShowFailureNotification(string fishId, int currentLevel, bool isEpicChampion = false)
		{
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Expected O, but got Unknown
			string fishDisplayName = GetFishDisplayName(fishId);
			string text;
			if (!isEpicChampion)
			{
				text = ((currentLevel <= -10) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.bottom", (object)new
				{
					fishName = fishDisplayName,
					absLevel = Math.Abs(currentLevel)
				})) : ((currentLevel >= 0) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.positive", (object)new
				{
					fishName = fishDisplayName
				})) : (Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.negative", (object)new
				{
					fishName = fishDisplayName
				})) + $"（{Math.Abs(currentLevel)}）")));
			}
			else
			{
				bool flag = DifficultyManager.GetAlpha(Game1.player) >= 1f;
				text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(flag ? "hud.fail.union" : "hud.fail.epic"));
			}
			EnqueueMessage(Game1.player, new HUDMessage(text, 3));
			FishingLog.Log($"[HUDNotifier] 失败提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {currentLevel} | 触底: {currentLevel <= -10} | 史诗提示: {isEpicChampion}", (LogLevel)1);
		}

		public static void ShowStarfruitTeaNotification(string fishId)
		{
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			try
			{
				string fishDisplayName = GetFishDisplayName(fishId);
				int value = Game1.random.Next(1, 16);
				string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get($"hud.starfruitTea.{value}", (object)new
				{
					fishName = fishDisplayName
				}));
				EnqueueMessage(Game1.player, new HUDMessage(text, 2));
				FishingLog.Log($"[HUDNotifier] 星之果茶掉落提示 | 鱼: {fishDisplayName} ({fishId}) | 文案: #{value}", (LogLevel)1);
			}
			catch (Exception value2)
			{
				FishingLog.Log($"星之果茶掉落提示失败: {value2}", (LogLevel)4);
			}
		}

		public static void ShowPerseveranceRewardNotification(string fishId, int level)
		{
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Expected O, but got Unknown
			try
			{
				string fishDisplayName = GetFishDisplayName(fishId);
				string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
				int value = Game1.random.Next(1, 21);
				string text2 = $"hud.battleReward.{value}";
				string text3 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text2, (object)new
				{
					fishName = fishDisplayName,
					rankName = text
				}));
				if (string.IsNullOrWhiteSpace(text3) || text3 == text2)
				{
					text3 = fishDisplayName + text + "希望下次能更尽兴些";
				}
				EnqueueMessage(Game1.player, new HUDMessage(text3, 2));
				FishingLog.Log($"[HUDNotifier] 持久战奖励提示 | 鱼: {fishDisplayName} ({fishId}) | 文案: #{value}", (LogLevel)1);
			}
			catch (Exception value2)
			{
				FishingLog.Log($"持久战奖励提示失败: {value2}", (LogLevel)4);
			}
		}

		public static void ShowChallengeStarLoss(int stars)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			try
			{
				int num = (3 - stars) * 20;
				string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.starLoss", (object)new
				{
					stars = stars,
					percent = num
				}));
				EnqueueMessage(Game1.player, new HUDMessage(text, 3));
				FishingLog.Log($"[HUDNotifier] 挑战星掉落提示 | 剩余星: {stars}/3 | 鱼获减少: -{num}%", (LogLevel)2);
			}
			catch (Exception value)
			{
				FishingLog.Log($"挑战星掉落提示失败: {value}", (LogLevel)4);
			}
		}

		public static void ShowDailyLimitReached(string fishId)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Expected O, but got Unknown
			try
			{
				string fishDisplayName = GetFishDisplayName(fishId);
				string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.dailyLimit", (object)new
				{
					fishName = fishDisplayName
				}));
				EnqueueMessage(Game1.player, new HUDMessage(text, 2));
				FishingLog.Log($"[HUDNotifier] 每日收获限额提示 | 鱼: {fishDisplayName} ({fishId})", (LogLevel)1);
			}
			catch (Exception value)
			{
				FishingLog.Log($"每日收获限额提示失败: {value}", (LogLevel)4);
			}
		}

		private static string GetFishDisplayName(string fishId)
		{
			try
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
				if (dataOrErrorItem == null || string.IsNullOrEmpty(dataOrErrorItem.DisplayName))
				{
					FishingLog.Log("[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: " + fishId, (LogLevel)3);
					return "未知鱼类";
				}
				return dataOrErrorItem.DisplayName;
			}
			catch (Exception ex)
			{
				FishingLog.Log("[HUDNotifier] 获取鱼名称失败 | 鱼ID: " + fishId + " | 错误: " + ex.Message, (LogLevel)4);
				return "未知鱼类";
			}
		}
	}
	public interface IGenericModConfigMenuApi
	{
		void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

		void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

		void AddParagraph(IManifest mod, Func<string> text);

		void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
	}
	public static class VanillaTipsIntegration
	{
		public interface IVanillaTipsApi
		{
			void RegisterTips(string sourceId, float weight, string[] ids, string[] categories, string[] zh, string[] en);

			void RemoveTips(string sourceId, params string[] tipIds);
		}

		public const float SourceWeight = 11f;

		private static readonly string[] Ids = new string[7] { "fe-exhaust", "fe-tea", "fe-giant", "fe-assist", "fe-assist-rank", "fe-alpha", "fe-challenge-bait" };

		private static readonly string[] Categories = new string[7] { "general", "general", "general", "general", "general", "general", "general" };

		private static readonly string[] ZhTexts = new string[7] { "【渔】时间站在渔夫这边：再倔的鱼，也熬不过一刻钟。", "【渔】五十级以后，运气好时能闻到茶香。", "【渔】八级以上的鱼，值得举起来走遍全村——趁还没回家。", "【渔】你钓过的老朋友，偶尔会游来帮忙，把绿条悄悄变长。", "【渔】最不服输的那条朋友，帮起忙来也最卖力。", "【渔】传说集齐六十一顶皇冠的人，手会像水一样顺。", "【渔】上了挑战鱼饵，帮手们就不会来了。" };

		private static readonly string[] EnTexts = new string[7] { "Fisher: Time favors the angler — even the most stubborn fish gives in within a quarter of an hour.", "Fisher: Past level 50, luck may bring the scent of tea to your haul.", "Fisher: Fish at level 8 or above deserve a parade through town — while you still can, before heading home.", "Fisher: Old friends you've caught sometimes swim by to help, quietly lengthening your catch bar.", "Fisher: The friend who never yields helps the hardest.", "Fisher: They say the one who gathers sixty-one crowns wields the rod as smoothly as water.", "Fisher: With challenge bait on, no helpers will come." };

		public static void TryRegister(IModHelper helper, IMonitor monitor)
		{
			try
			{
				IVanillaTipsApi api = helper.ModRegistry.GetApi<IVanillaTipsApi>("neoiw.vanillatips");
				if (api == null)
				{
					monitor.Log("[FishingExpanded] VanillaTips 未安装，跳过提示注入。", (LogLevel)1);
					return;
				}
				api.RegisterTips("YourName.FishingExpanded", 11f, Ids, Categories, ZhTexts, EnTexts);
				monitor.Log($"[FishingExpanded] 已向 VanillaTips 注入 {Ids.Length} 条提示（来源权重 {11f:0.##}）。", (LogLevel)2);
			}
			catch (Exception value)
			{
				monitor.Log($"[FishingExpanded] VanillaTips 注入失败（不影响本模组）：{value}", (LogLevel)3);
			}
		}
	}
}
namespace FishingExpanded.Patches
{
	[HarmonyPatch(typeof(BobberBar))]
	internal class BobberBarPatches
	{
		private enum PhraseMode
		{
			Free,
			WaitingMiddle,
			Recording,
			Playing
		}

		private class FloatingTip
		{
			public const float Lifetime = 5f;

			public const float RisePixels = 30f;

			public string Text { get; set; }

			public float StartX { get; set; }

			public float StartY { get; set; }

			public float Age { get; set; }

			public bool Centered { get; set; }

			public bool RightAligned { get; set; }

			public bool IsActionTip { get; set; }

			public float PendingDelay { get; set; }

			public long Id { get; set; }

			public bool FlipLogged { get; set; }

			public bool DrawLogged { get; set; }

			public float LifetimeOverride { get; set; } = -1f;

			public float DisplayLifetime
			{
				get
				{
					if (!(LifetimeOverride > 0f))
					{
						return 5f;
					}
					return LifetimeOverride;
				}
			}

			public float Alpha => Math.Max(0f, 1f - Age / DisplayLifetime);

			public float YOffset => 30f * (Age / DisplayLifetime);
		}

		private class InstanceData
		{
			public string FishId { get; set; }

			public int DifficultyLevel { get; set; }

			public float OriginalDifficulty { get; set; }

			public float AdjustedDifficulty { get; set; }

			public float NativeCatchPenaltyModifier { get; set; } = 1f;

			public float LastAppliedCatchPenaltyModifier { get; set; } = 1f;

			public bool ProtectionEngaged { get; set; }

			public bool EscapeBonusEngaged { get; set; }

			public bool BarInputDiagnosticLogged { get; set; }

			public int LastChallengeStarsLogged { get; set; } = 3;

			public float IdleSeconds { get; set; }

			public bool IdlePending { get; set; }

			public bool IsIdle { get; set; }

			public bool IdleTipShown { get; set; }

			public float LastBarPos { get; set; }

			public float IdleFrozenPosition { get; set; }

			public bool JumpWindupActive { get; set; }

			public float JumpWindupSeconds { get; set; }

			public float JumpWindupStartPosition { get; set; }

			public int PatternSeed { get; set; }

			public Random PatternRandom { get; set; }

			public Random SavedGameRandom { get; set; }

			public PhraseMode PhraseMode { get; set; }

			public List<(float Time, float Position)> PhraseSamples { get; } = new List<(float, float)>();

			public float PhraseRecordTime { get; set; }

			public bool PhraseJumpSeen { get; set; }

			public float PhraseWindupStartTime { get; set; } = -1f;

			public bool PhraseJumpIsUp { get; set; }

			public string PhraseJumpText { get; set; }

			public float PhraseDuration { get; set; }

			public float PhrasePlayTime { get; set; }

			public bool PlaybackWindupShown { get; set; }

			public float NextPhraseThreshold { get; set; } = 1f / 3f;

			public bool PhraseSwitchPending { get; set; }

			public int QuantityMultiplier { get; set; } = 1;

			public long PlayerId { get; set; } = -1L;

			public Farmer Owner { get; set; }

			public bool FailureRecorded { get; set; }

			public bool ResultStarted { get; set; }

			public int MissCount { get; set; }

			public bool WasBobberInBar { get; set; }

			public float Alpha { get; set; }

			public List<FloatingTip> ActionTips { get; } = new List<FloatingTip>();

			public List<FloatingTip> OtherTips { get; } = new List<FloatingTip>();

			public int AssistLevel { get; set; }

			public string AssistFishId { get; set; }

			public string BaitId { get; set; }

			public bool HasChallengeBait { get; set; }

			public float BattleElapsedSeconds { get; set; }

			public int NextExhaustionNodeIndex { get; set; }

			public float EffectiveDifficulty { get; set; }

			public bool PeakTipShown { get; set; }

			public float ForcedPerseveranceSeconds { get; set; }

			public float JumpIntervalSeconds { get; set; }

			public float JumpCooldownSeconds { get; set; }

			public float JumpDetectionSeconds { get; set; }

			public bool JumpPending { get; set; }

			public float JumpPendingSeconds { get; set; }

			public float JumpPendingTarget { get; set; }
		}

		public readonly struct AssistObservation
		{
			public string FishId { get; }

			public double Rank { get; }

			public int Level { get; }

			public AssistObservation(string fishId, double rank, int level)
			{
				FishId = fishId;
				Rank = rank;
				Level = level;
			}
		}

		private static readonly ConditionalWeakTable<BobberBar, InstanceData> _instanceData = new ConditionalWeakTable<BobberBar, InstanceData>();

		private static long _nextTipId;

		private const float BarLeftX = 64f;

		private const float BarWidth = 36f;

		private const float OtherTipBarGapPixels = 50f;

		private const float ActionTipBarGapPixels = 24f;

		private const float TipMaxWidthPixels = 420f;

		private const int TipMaxLines = 3;

		private const float OtherTipAnchorX = 14f;

		private const float ActionTipAnchorX = 124f;

		private const int MaxFloatingTips = 8;

		private const double PerseveranceChance = 0.5;

		private const string PerseveranceSeaFoamPudding = "(O)265";

		private static readonly string[] PerseverancePlusThreeFoods = new string[3] { "(O)242", "(O)728", "(O)730" };

		private static readonly List<AssistObservation> AssistObservations = new List<AssistObservation>();

		private const int AssistObservationCap = 500;

		private static void AgeTips(List<FloatingTip> tips, float dt)
		{
			for (int num = tips.Count - 1; num >= 0; num--)
			{
				if (tips[num].PendingDelay > 0f)
				{
					tips[num].PendingDelay = Math.Max(0f, tips[num].PendingDelay - dt);
				}
				else
				{
					tips[num].Age += dt;
					if (tips[num].Age >= tips[num].DisplayLifetime)
					{
						tips.RemoveAt(num);
					}
				}
			}
		}

		private static void AddTip(List<FloatingTip> tips, string text, float x, float y, bool centered, bool rightAligned = false, bool actionTip = false, float lifetimeOverride = -1f)
		{
			float num = ((((Rectangle)(ref Game1.viewport)).Width > 0) ? ((float)((Rectangle)(ref Game1.uiViewport)).Width / (float)((Rectangle)(ref Game1.viewport)).Width) : 1f);
			if (num <= 0f || float.IsNaN(num) || float.IsInfinity(num))
			{
				num = 1f;
			}
			float num2 = x * num;
			float maxWidth = Math.Min(420f * num, rightAligned ? Math.Max(120f * num, num2 - 16f * num) : Math.Max(120f * num, (float)((Rectangle)(ref Game1.uiViewport)).Width - num2 - 16f * num));
			List<string> list = SplitTipChunks(Game1.dialogueFont, text, maxWidth, 1f, 3);
			for (int i = 0; i < list.Count; i++)
			{
				string text2 = list[i];
				if (i < list.Count - 1)
				{
					text2 += "…";
				}
				if (tips.Count >= 8)
				{
					tips.RemoveAt(0);
				}
				tips.Add(new FloatingTip
				{
					Id = Interlocked.Increment(ref _nextTipId),
					Text = text2,
					StartX = x,
					StartY = y,
					Centered = centered,
					RightAligned = rightAligned,
					IsActionTip = actionTip,
					PendingDelay = (float)i * (((lifetimeOverride > 0f) ? lifetimeOverride : 5f) + 0.2f),
					LifetimeOverride = lifetimeOverride
				});
			}
		}

		private static List<string> SplitTipChunks(SpriteFont font, string text, float maxWidth, float scale, int maxLinesPerChunk)
		{
			List<string> list = new List<string>();
			if (string.IsNullOrEmpty(text))
			{
				list.Add(text ?? string.Empty);
				return list;
			}
			List<string> list2 = WrapTipText(font, text, maxWidth, scale);
			for (int i = 0; i < list2.Count; i += maxLinesPerChunk)
			{
				int count = Math.Min(maxLinesPerChunk, list2.Count - i);
				list.Add(string.Join(" ", list2.GetRange(i, count)));
			}
			if (list.Count == 0)
			{
				list.Add(text);
			}
			return list;
		}

		private static void DrawTip(SpriteBatch b, FloatingTip tip)
		{
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0572: Unknown result type (might be due to invalid IL or missing references)
			//IL_0582: Unknown result type (might be due to invalid IL or missing references)
			//IL_0587: Unknown result type (might be due to invalid IL or missing references)
			//IL_058c: Unknown result type (might be due to invalid IL or missing references)
			//IL_059d: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0683: Unknown result type (might be due to invalid IL or missing references)
			//IL_0685: Unknown result type (might be due to invalid IL or missing references)
			//IL_0690: Unknown result type (might be due to invalid IL or missing references)
			//IL_069a: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_060b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0610: Unknown result type (might be due to invalid IL or missing references)
			//IL_0627: Unknown result type (might be due to invalid IL or missing references)
			//IL_0629: Unknown result type (might be due to invalid IL or missing references)
			//IL_0637: Unknown result type (might be due to invalid IL or missing references)
			//IL_0639: Unknown result type (might be due to invalid IL or missing references)
			//IL_063b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0640: Unknown result type (might be due to invalid IL or missing references)
			//IL_064e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0658: Unknown result type (might be due to invalid IL or missing references)
			//IL_0620: Unknown result type (might be due to invalid IL or missing references)
			//IL_0625: Unknown result type (might be due to invalid IL or missing references)
			if (tip.PendingDelay > 0f || string.IsNullOrEmpty(tip.Text))
			{
				return;
			}
			float num = ((((Rectangle)(ref Game1.viewport)).Width > 0) ? ((float)((Rectangle)(ref Game1.uiViewport)).Width / (float)((Rectangle)(ref Game1.viewport)).Width) : 1f);
			if (num <= 0f || float.IsNaN(num) || float.IsInfinity(num))
			{
				num = 1f;
			}
			float num2 = 1f * num;
			float num3 = 8f * num;
			Color val = (tip.IsActionTip ? Color.RoyalBlue : Color.DarkRed);
			Color val2 = Color.Lerp(val, Color.White, 0.85f);
			SpriteFont dialogueFont = Game1.dialogueFont;
			float num4 = tip.StartX * num;
			float num5 = Math.Min(420f * num, tip.RightAligned ? Math.Max(120f * num, num4 - num3 * 2f) : Math.Max(120f * num, (float)((Rectangle)(ref Game1.uiViewport)).Width - num4 - num3 * 2f));
			List<string> list = WrapTipText(dialogueFont, tip.Text, num5, num2);
			if (list.Count > 3)
			{
				while (list.Count > 3)
				{
					list.RemoveAt(list.Count - 1);
				}
				string text = list[2];
				while (dialogueFont.MeasureString(text + "…").X * num2 > num5 && text.Length > 0)
				{
					text = text.Substring(0, text.Length - 1);
				}
				list[2] = text + "…";
			}
			float num6 = (float)dialogueFont.LineSpacing * num2;
			float num7 = (float)list.Count * num6;
			float num8 = 0f;
			foreach (string item in list)
			{
				num8 = Math.Max(num8, dialogueFont.MeasureString(item).X * num2);
			}
			float num9;
			float num10;
			if (tip.RightAligned)
			{
				num9 = num4 + 50f * num;
				num10 = num9 + 36f * num;
			}
			else if (!tip.Centered)
			{
				num10 = num4 - 24f * num;
				num9 = num10 - 36f * num;
			}
			else
			{
				num9 = num4 - 64f * num;
				num10 = num9 + 36f * num;
			}
			bool flag = false;
			float val3;
			if (tip.RightAligned)
			{
				if (num4 - num3 < num8)
				{
					val3 = num10 + 24f * num;
					flag = true;
				}
				else
				{
					val3 = num4 - num8;
				}
			}
			else if (!tip.Centered)
			{
				if (num4 + num8 > (float)((Rectangle)(ref Game1.uiViewport)).Width - num3)
				{
					val3 = num9 - 50f * num - num8;
					flag = true;
				}
				else
				{
					val3 = num4;
				}
			}
			else
			{
				val3 = num4 - num8 / 2f;
			}
			val3 = Math.Max(num3, Math.Min(val3, (float)((Rectangle)(ref Game1.uiViewport)).Width - num3 - num8));
			if (flag && !tip.FlipLogged)
			{
				tip.FlipLogged = true;
				FishingLog.LogRateLimited("TipSideFlip:" + tip.Id, $"[BobberBar] 提示侧翻 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {num9 - 64f * num:F0} | StartX: {tip.StartX:F0} | maxWidth: {num8:F0} | finalX: {val3:F0} | viewportW: {((Rectangle)(ref Game1.viewport)).Width}", (LogLevel)2);
			}
			if (!tip.DrawLogged)
			{
				tip.DrawLogged = true;
				FishingLog.LogRateLimited("TipDraw:" + tip.Id, $"[BobberBar] 提示坐标 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {num9 - 64f * num:F0} | startX: {tip.StartX:F0} | maxWidth: {num8:F0} | finalX: {val3:F0} | viewportW: {((Rectangle)(ref Game1.viewport)).Width} | uiViewportW: {((Rectangle)(ref Game1.uiViewport)).Width} | flipped: {flag}", (LogLevel)2);
			}
			float num11 = (tip.Centered ? tip.StartY : (tip.StartY - num7 / 2f)) * num + tip.YOffset * num;
			Vector2 val4 = default(Vector2);
			for (int i = 0; i < list.Count; i++)
			{
				((Vector2)(ref val4))..ctor(val3, num11 + (float)i * num6);
				b.DrawString(dialogueFont, list[i], val4 + new Vector2(2f * num, 2f * num), Color.Black * (tip.Alpha * 0.65f), 0f, Vector2.Zero, num2, (SpriteEffects)0, 0f);
				for (int j = 0; j < 4; j++)
				{
					Vector2 val5 = (Vector2)(j switch
					{
						0 => new Vector2(1f * num, 0f), 
						1 => new Vector2(-1f * num, 0f), 
						2 => new Vector2(0f, 1f * num), 
						_ => new Vector2(0f, -1f * num), 
					});
					b.DrawString(dialogueFont, list[i], val4 + val5, val2 * (tip.Alpha * 0.85f), 0f, Vector2.Zero, num2, (SpriteEffects)0, 0f);
				}
				b.DrawString(dialogueFont, list[i], val4, Color.White * tip.Alpha, 0f, Vector2.Zero, num2, (SpriteEffects)0, 0f);
			}
		}

		private static List<string> WrapTipText(SpriteFont font, string text, float maxWidth, float scale)
		{
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			List<string> list = new List<string>();
			if (string.IsNullOrEmpty(text))
			{
				list.Add(text ?? string.Empty);
				return list;
			}
			string text2 = "";
			string[] array = text.Split(' ');
			foreach (string text3 in array)
			{
				if (text3.Length == 0)
				{
					continue;
				}
				string text4 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
				if (font.MeasureString(text4).X * scale <= maxWidth || text2.Length == 0)
				{
					text2 = text4;
					continue;
				}
				if (text2.Length > 0)
				{
					list.Add(text2);
					text2 = "";
				}
				text2 = text3;
				while (text2.Length > 0 && font.MeasureString(text2).X * scale > maxWidth)
				{
					int num = text2.Length;
					while (num > 1 && font.MeasureString(text2.Substring(0, num)).X * scale > maxWidth)
					{
						num--;
					}
					if (num < 1)
					{
						num = 1;
					}
					list.Add(text2.Substring(0, num));
					text2 = text2.Substring(num);
				}
			}
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
			if (list.Count == 0)
			{
				list.Add(text);
			}
			return list;
		}

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		[HarmonyPostfix]
		public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberTargetPosition, bool ___bobberInBar, ref int ___bobberBarHeight, string baitID, float ___bobberBarPos, int ___xPositionOnScreen, int ___yPositionOnScreen)
		{
			try
			{
				float num = ModEntry.ConsumeForcePerseveranceSeconds();
				if (FestivalFishingService.IsVanillaFestivalMode())
				{
					FishingLog.Log("[节日] 原生模式跳过 BobberBar 注入 | 鱼ID: " + SpecialFishHelper.NormalizeItemId(whichFish) + " | 开关关闭（节日完全原生）", (LogLevel)2);
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(whichFish);
				if (SpecialFishHelper.IsLegendaryFish(text))
				{
					FishingLog.Log($"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {text} | 保持原始difficulty: {___difficulty:F1}", (LogLevel)2);
					if (num > 0f)
					{
						FishingLog.Log($"[BobberBar] 持久战测试标志被鱼王豁免消耗 | 强制秒数: {num:F0}s", (LogLevel)2);
					}
					return;
				}
				float num2 = ___difficulty;
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, Game1.player);
				InstanceData obj = new InstanceData
				{
					FishId = text,
					DifficultyLevel = difficultyLevel,
					OriginalDifficulty = num2,
					NativeCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
					LastAppliedCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier
				};
				Farmer player = Game1.player;
				obj.PlayerId = ((player != null) ? player.UniqueMultiplayerID : (-1));
				obj.Owner = Game1.player;
				obj.FailureRecorded = false;
				obj.MissCount = 0;
				obj.WasBobberInBar = ___bobberInBar;
				obj.Alpha = DifficultyManager.GetAlpha(Game1.player);
				obj.BaitId = baitID;
				obj.HasChallengeBait = baitID == "(O)ChallengeBait";
				obj.EffectiveDifficulty = ___difficulty;
				obj.ForcedPerseveranceSeconds = num;
				InstanceData instanceData = obj;
				_instanceData.Add(__instance, instanceData);
				if (instanceData.HasChallengeBait && !ModEntry.Config.EnableRandomFishBehavior)
				{
					instanceData.PatternSeed = DifficultyManager.GetOrCreateChallengePatternSeed(text, difficultyLevel, Game1.player);
					instanceData.PatternRandom = new Random(instanceData.PatternSeed);
				}
				float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
				___difficulty *= difficultyMultiplier;
				int value = (instanceData.QuantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel));
				int value2 = ___fishSize;
				___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);
				instanceData.AdjustedDifficulty = ___difficulty;
				instanceData.EffectiveDifficulty = ___difficulty;
				float tipX = (float)___xPositionOnScreen + 14f;
				float tipY = (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f;
				TryTriggerAssist(Game1.player, instanceData, ref ___bobberBarHeight, tipX, tipY);
				if (instanceData.AdjustedDifficulty > 100f)
				{
					___bobberTargetPosition = 0f;
				}
				instanceData.JumpIntervalSeconds = GetJumpInterval(instanceData.AdjustedDifficulty);
				float accelerationBoost = GetAccelerationBoost(__instance, ___difficulty);
				HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
				HUDNotifier.ShowStarChallengeNotification(text, difficultyLevel, Game1.player);
				FishingLog.Log($"[BobberBar] 钓鱼小游戏开始 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 原始difficulty: {num2:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | 数量倍数: {value} | fishSize(原生): {value2} (结算×{DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel):F2}) | 品质: {___fishQuality} | 加速增幅档: {GetAccelerationTier(difficultyLevel):P0} | 加速度增幅: ×{accelerationBoost:F2} | 跳鱼间隔: {instanceData.JumpIntervalSeconds:F0}s | 鱼竿熟练度α: {instanceData.Alpha:P0} | 力竭: {instanceData.AdjustedDifficulty:F0}{((instanceData.AdjustedDifficulty >= 100f) ? "（参与）" : "（不参与）")} | 挑战鱼饵: {instanceData.HasChallengeBait} | barX: {___xPositionOnScreen} | barY: {___yPositionOnScreen} | viewportW: {((Rectangle)(ref Game1.viewport)).Width}" + ((num > 0f) ? $" | 持久战测试强制: {num:F0}s" : ""), (LogLevel)2);
			}
			catch (Exception value3)
			{
				FishingLog.Log($"BobberBar 构造函数 Patch 失败: {value3}", (LogLevel)4);
			}
		}

		[HarmonyPatch("update")]
		[HarmonyPrefix]
		public static void Update_Prefix(BobberBar __instance, ref float ___difficulty, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberPosition, ref float ___bobberTargetPosition, ref float ___bobberSpeed, ref float ___floaterSinkerAcceleration, float ___distanceFromCatching, bool ___bobberInBar, ref int ___fishSizeReductionTimer, ref int ___challengeBaitFishes, int ___xPositionOnScreen, int ___yPositionOnScreen, float ___bobberBarPos, int ___bobberBarHeight)
		{
			try
			{
				if (!_instanceData.TryGetValue(__instance, out var value))
				{
					return;
				}
				if (value.PatternRandom != null)
				{
					value.SavedGameRandom = Game1.random;
					Game1.random = value.PatternRandom;
				}
				float num = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
				if (num <= 0f)
				{
					num = 1f / 60f;
				}
				AgeTips(value.ActionTips, num);
				AgeTips(value.OtherTips, num);
				if (value.IdlePending && !value.IsIdle)
				{
					float num2 = ___bobberBarPos - 32f;
					float num3 = num2 + (float)___bobberBarHeight;
					if (___bobberPosition - 16f > num3 + 5f || ___bobberPosition + 12f < num2 - 5f)
					{
						value.IsIdle = true;
						value.IdlePending = false;
						value.IdleFrozenPosition = ___bobberPosition;
						if (!value.IdleTipShown)
						{
							value.IdleTipShown = true;
							AddTip(value.OtherTips, PickIdleText(), (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
						}
					}
				}
				if (!value.ResultStarted && !value.IsIdle)
				{
					value.BattleElapsedSeconds += num;
					if (!value.PeakTipShown && value.BattleElapsedSeconds >= 30f)
					{
						value.PeakTipShown = true;
						string text = PickPeakText(value.DifficultyLevel);
						AddTip(value.OtherTips, text, (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
						FishingLog.Log($"[BobberBar] 持久战巅峰提示 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 耗时: {value.BattleElapsedSeconds:F0}s | 文案: {text}", (LogLevel)2);
					}
					if (value.AdjustedDifficulty >= 100f)
					{
						while (value.NextExhaustionNodeIndex < DifficultyCalculator.ExhaustionNodes.Length && value.BattleElapsedSeconds >= DifficultyCalculator.ExhaustionNodes[value.NextExhaustionNodeIndex].Minute * 60f)
						{
							(float, float) tuple = DifficultyCalculator.ExhaustionNodes[value.NextExhaustionNodeIndex];
							value.NextExhaustionNodeIndex++;
							string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(value.DifficultyLevel)));
							int value2 = Game1.random.Next(1, 11);
							string text3 = $"hud.exhaust.{tuple.Item1:0}.{value2}";
							string text4 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text3, (object)new
							{
								rankName = text2
							}));
							if (string.IsNullOrWhiteSpace(text4) || text4 == text3)
							{
								text4 = $"[{text2}]体力见底（{tuple.Item1:0}分钟）";
							}
							if (value.HasChallengeBait)
							{
								int value3 = Game1.random.Next(1, 11);
								string text5 = $"hud.exhaust.append.{value3}";
								string text6 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text5));
								if (string.IsNullOrWhiteSpace(text6) || text6 == text5)
								{
									text6 = "但这场对决，它还想继续";
								}
								text4 += text6;
							}
							AddTip(value.OtherTips, text4, (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
							bool flag = !value.HasChallengeBait && value.PhraseMode != PhraseMode.Free;
							if (flag)
							{
								InterruptPhrase(value);
							}
							FishingLog.Log($"[BobberBar] 力竭节点 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 节点: {tuple.Item1:0}分钟 ({tuple.Item2:P0}) | 耗时: {value.BattleElapsedSeconds:F0}s | 挑战鱼饵: {value.HasChallengeBait} | 文案: {text4}" + (flag ? " | 短语打断（重录）" : ""), (LogLevel)2);
						}
						value.EffectiveDifficulty = (value.HasChallengeBait ? value.AdjustedDifficulty : DifficultyCalculator.GetExhaustedDifficulty(value.AdjustedDifficulty, value.BattleElapsedSeconds));
						if (Math.Abs(___difficulty - value.EffectiveDifficulty) > 0.001f)
						{
							___difficulty = value.EffectiveDifficulty;
						}
					}
					else
					{
						value.EffectiveDifficulty = value.AdjustedDifficulty;
					}
				}
				else
				{
					value.EffectiveDifficulty = value.AdjustedDifficulty;
				}
				if (value.IsIdle)
				{
					___bobberSpeed = 0f;
					___bobberTargetPosition = ___bobberPosition;
					___floaterSinkerAcceleration = 0f;
				}
				if (value.DifficultyLevel > 0)
				{
					___fishSizeReductionTimer = 800;
				}
				if (value.HasChallengeBait && value.AdjustedDifficulty > 100f)
				{
					int challengeStars = GetChallengeStars(value.DifficultyLevel, value.BattleElapsedSeconds);
					if (challengeStars < value.LastChallengeStarsLogged)
					{
						value.LastChallengeStarsLogged = challengeStars;
						FishingLog.Log($"[BobberBar] 挑战星减少 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 剩余星星: {challengeStars}/3 | 鱼获惩罚: -{20 * (3 - challengeStars)}% | 无法恢复", (LogLevel)2);
						HUDNotifier.ShowChallengeStarLoss(challengeStars);
					}
					___challengeBaitFishes = 3;
				}
				if (value.WasBobberInBar && !___bobberInBar)
				{
					value.MissCount++;
				}
				value.WasBobberInBar = ___bobberInBar;
				float num4 = ___distanceFromCatchPenaltyModifier;
				if (Math.Abs(num4 - value.LastAppliedCatchPenaltyModifier) > 0.0001f)
				{
					value.NativeCatchPenaltyModifier = num4;
				}
				float catchProgress = (value.ProtectionEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
				float num5 = DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, catchProgress);
				bool flag2 = false;
				int num6 = 0;
				if (value.AdjustedDifficulty > 100f)
				{
					num6 = DifficultyManager.GetConsecutiveFailCount(value.FishId, value.Owner ?? Game1.player);
					if (num6 > 0)
					{
						float catchProgress2 = (value.EscapeBonusEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
						num5 = DifficultyCalculator.GetEscapeFailBonusModifier(value.DifficultyLevel, num6, catchProgress2);
						flag2 = num5 < DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, catchProgress2) - 0.0001f;
					}
				}
				if (flag2 && !value.EscapeBonusEngaged)
				{
					value.EscapeBonusEngaged = true;
					FishingLog.Log($"[BobberBar] 逃逸减速加成生效 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 连续失败: {num6} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num5:F2}", (LogLevel)1);
				}
				else if (!flag2 && value.EscapeBonusEngaged)
				{
					value.EscapeBonusEngaged = false;
					FishingLog.Log($"[BobberBar] 逃逸减速加成解除 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 减速倍率: {num5:F2}", (LogLevel)1);
				}
				float num7 = Math.Min(value.NativeCatchPenaltyModifier, num5);
				if (value.IsIdle)
				{
					num7 = 0f;
				}
				___distanceFromCatchPenaltyModifier = num7;
				value.LastAppliedCatchPenaltyModifier = num7;
				bool flag3 = num7 < value.NativeCatchPenaltyModifier - 0.0001f;
				if (flag3 && !value.ProtectionEngaged)
				{
					value.ProtectionEngaged = true;
					FishingLog.Log($"[BobberBar] 蓄力槽保护生效 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num4:F2} → {num7:F2}", (LogLevel)1);
				}
				else if (!flag3 && value.ProtectionEngaged)
				{
					value.ProtectionEngaged = false;
					FishingLog.Log($"[BobberBar] 蓄力槽保护解除 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num7:F2} → {num4:F2}", (LogLevel)1);
				}
				if ((value.EffectiveDifficulty >= 150f || value.JumpPending) && !value.ResultStarted && !value.IsIdle && value.PhraseMode != PhraseMode.Playing)
				{
					if (value.JumpPending)
					{
						if (value.JumpWindupActive)
						{
							value.JumpWindupSeconds -= num;
							___bobberSpeed = 0f;
							___bobberTargetPosition = ___bobberPosition;
							___floaterSinkerAcceleration = 0f;
							if (value.JumpWindupSeconds <= 0f)
							{
								___bobberPosition = value.JumpPendingTarget;
								___bobberTargetPosition = value.JumpPendingTarget;
								___bobberSpeed = 0f;
								value.JumpPending = false;
								value.JumpWindupActive = false;
								value.JumpIntervalSeconds = GetJumpInterval(value.EffectiveDifficulty);
								value.JumpCooldownSeconds = value.JumpIntervalSeconds;
								value.JumpDetectionSeconds = 0f;
								if (value.EffectiveDifficulty >= 150f)
								{
									if (value.PhraseMode == PhraseMode.Free)
									{
										value.PhraseMode = PhraseMode.WaitingMiddle;
									}
									else if (value.PhraseMode == PhraseMode.Recording)
									{
										value.PhraseJumpSeen = true;
									}
								}
							}
						}
						else
						{
							value.JumpPendingSeconds -= num;
							if (value.JumpPendingSeconds <= 0f)
							{
								value.JumpWindupActive = true;
								value.JumpWindupSeconds = 0.88f;
								value.JumpWindupStartPosition = ___bobberPosition;
								if (value.PhraseMode == PhraseMode.Recording)
								{
									value.PhraseWindupStartTime = value.PhraseRecordTime;
								}
								___bobberSpeed = 0f;
								___bobberTargetPosition = ___bobberPosition;
								___floaterSinkerAcceleration = 0f;
							}
						}
					}
					else if (value.JumpCooldownSeconds > 0f)
					{
						value.JumpCooldownSeconds -= num;
						if (value.JumpCooldownSeconds < 0f)
						{
							value.JumpCooldownSeconds = 0f;
						}
					}
					else
					{
						value.JumpDetectionSeconds -= num;
						if (value.JumpDetectionSeconds <= 0f)
						{
							value.JumpDetectionSeconds = 1f;
							float num8 = ___bobberPosition;
							if (num8 >= 399f)
							{
								value.JumpPending = true;
								value.JumpPendingSeconds = 0.5f;
								value.JumpPendingTarget = Game1.random.Next(0, 134);
							}
							else if (num8 <= 133f)
							{
								value.JumpPending = true;
								value.JumpPendingSeconds = 0.5f;
								value.JumpPendingTarget = Game1.random.Next(399, 533);
							}
							if (value.JumpPending)
							{
								bool flag4 = value.JumpPendingTarget <= 133f;
								string text7 = PickJumpText(flag4);
								AddTip(value.ActionTips, text7, (float)___xPositionOnScreen + 124f, (float)___yPositionOnScreen + 36f + ___bobberPosition - 30f, centered: false, rightAligned: false, actionTip: true);
								if (value.PhraseMode == PhraseMode.Recording)
								{
									value.PhraseJumpIsUp = flag4;
									value.PhraseJumpText = text7;
								}
								FishingLog.Log($"[BobberBar] 高难度鱼跳 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 难度: {value.EffectiveDifficulty:F0} | 跳至: {value.JumpPendingTarget:F0} | 文案: {(flag4 ? "上跳(鱼跃)" : "下跳(甩尾)")}: {text7}", (LogLevel)2);
							}
						}
					}
				}
				HandlePhrasePrefix(__instance, value, ref ___bobberPosition, ref ___bobberSpeed, ref ___bobberTargetPosition, ref ___floaterSinkerAcceleration, num);
			}
			catch (Exception value4)
			{
				FishingLog.LogRateLimited("BobberBar.Update_Prefix", $"BobberBar.update Prefix 失败: {value4}", (LogLevel)4);
			}
		}

		public static int GetMissCount(BobberBar instance)
		{
			if (_instanceData.TryGetValue(instance, out var value))
			{
				return value.MissCount;
			}
			return 0;
		}

		public static float GetAdjustedDifficulty(BobberBar instance)
		{
			if (!_instanceData.TryGetValue(instance, out var value))
			{
				return 0f;
			}
			return value.AdjustedDifficulty;
		}

		public static float GetOriginalDifficulty(BobberBar instance)
		{
			if (!_instanceData.TryGetValue(instance, out var value))
			{
				return 0f;
			}
			return value.OriginalDifficulty;
		}

		public static bool HasChallengeBait(BobberBar instance)
		{
			if (_instanceData.TryGetValue(instance, out var value))
			{
				return value.HasChallengeBait;
			}
			return false;
		}

		public static float GetElapsedSeconds(BobberBar instance)
		{
			if (!_instanceData.TryGetValue(instance, out var value))
			{
				return 0f;
			}
			return value.BattleElapsedSeconds;
		}

		private static void TryGrantPerseveranceReward(InstanceData data)
		{
			try
			{
				if (data == null || (data.Owner == null && Game1.player == null))
				{
					return;
				}
				float forcedPerseveranceSeconds = data.ForcedPerseveranceSeconds;
				float num = ((forcedPerseveranceSeconds > 0f) ? forcedPerseveranceSeconds : data.BattleElapsedSeconds);
				string text;
				if (num >= 60f)
				{
					text = "(O)265";
				}
				else
				{
					if (!(num >= 30f) || !(Game1.random.NextDouble() < 0.5))
					{
						return;
					}
					text = PerseverancePlusThreeFoods[Game1.random.Next(PerseverancePlusThreeFoods.Length)];
				}
				Farmer val = data.Owner ?? Game1.player;
				Item val2 = ItemRegistry.Create(text, 1, 0, false);
				val.addItemByMenuIfNecessary(val2, (behaviorOnItemSelect)null, false);
				HUDNotifier.ShowPerseveranceRewardNotification(data.FishId, data.DifficultyLevel);
				FishingLog.Log($"[BobberBar] 持久战奖励 | 实例: {data.GetHashCode()} | 鱼ID: {data.FishId} | 耗时: {num:F0}s | 奖励: {val2.DisplayName} | 玩家: {val.UniqueMultiplayerID}" + ((forcedPerseveranceSeconds > 0f) ? " | 测试强制" : ""), (LogLevel)2);
			}
			catch (Exception value)
			{
				FishingLog.Log($"BobberBar 持久战奖励失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("update")]
		[HarmonyPostfix]
		public static void Update_Postfix(BobberBar __instance, float ___distanceFromCatching, bool ___fadeOut, float ___bobberBarPos, ref float ___bobberPosition)
		{
			try
			{
				if (!_instanceData.TryGetValue(__instance, out var value))
				{
					return;
				}
				if (value.PatternRandom != null && value.SavedGameRandom != null)
				{
					Game1.random = value.SavedGameRandom;
					value.SavedGameRandom = null;
				}
				float num = Math.Abs(___bobberBarPos - value.LastBarPos);
				value.LastBarPos = ___bobberBarPos;
				float num2 = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
				if (num2 <= 0f)
				{
					num2 = 1f / 60f;
				}
				if (num > 0.5f)
				{
					value.IdleSeconds = 0f;
					value.IdlePending = false;
					if (value.IsIdle)
					{
						value.IsIdle = false;
					}
				}
				else if (!value.IsIdle && !value.IdlePending)
				{
					value.IdleSeconds += num2;
					if (value.IdleSeconds >= 3f)
					{
						value.IdlePending = true;
					}
				}
				if (value.IsIdle)
				{
					___bobberPosition = value.IdleFrozenPosition;
				}
				else if (value.JumpWindupActive && value.PhraseMode != PhraseMode.Playing)
				{
					___bobberPosition = value.JumpWindupStartPosition;
				}
				HandlePhrasePostfix(value, ___bobberPosition, ___distanceFromCatching);
				if (___fadeOut && ___distanceFromCatching <= 0f && !value.FailureRecorded)
				{
					FishingLog.Log($"[BobberBar] 钓鱼失败 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 蓄力槽耗尽: {___distanceFromCatching:F3}", (LogLevel)2);
					DifficultyManager.RecordFailure(value.FishId, value.Owner ?? Game1.player, value.HasChallengeBait);
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(value.FishId, value.Owner ?? Game1.player);
					bool isEpicChampion = value.AdjustedDifficulty >= 150f && DifficultyManager.GetConsecutiveFailCount(value.FishId, value.Owner ?? Game1.player) >= 2;
					HUDNotifier.ShowFailureNotification(value.FishId, difficultyLevel, isEpicChampion);
					TryGrantPerseveranceReward(value);
					value.FailureRecorded = true;
					value.ResultStarted = true;
				}
				if (___fadeOut && ___distanceFromCatching >= 1f)
				{
					value.ResultStarted = true;
				}
				if (value.ResultStarted && !___fadeOut)
				{
					CleanupInstance(__instance);
				}
			}
			catch (Exception value2)
			{
				FishingLog.LogRateLimited("BobberBar.Update_Postfix", $"BobberBar.update Postfix 失败: {value2}", (LogLevel)4);
			}
		}

		private static void CleanupInstance(BobberBar instance)
		{
			if (_instanceData.Remove(instance))
			{
				FishingLog.Log($"[BobberBar] 清理实例数据 | 实例: {((object)instance).GetHashCode()}", (LogLevel)1);
			}
		}

		public static void PeriodicCleanup()
		{
		}

		[HarmonyPatch("update")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Expected O, but got Unknown
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d6: Expected O, but got Unknown
			//IL_0351: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Expected O, but got Unknown
			//IL_035f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0365: Expected O, but got Unknown
			//IL_036d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0373: Expected O, but got Unknown
			//IL_037c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0382: Expected O, but got Unknown
			//IL_038a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0390: Expected O, but got Unknown
			//IL_040f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0419: Expected O, but got Unknown
			//IL_0499: Unknown result type (might be due to invalid IL or missing references)
			//IL_049f: Expected O, but got Unknown
			//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ad: Expected O, but got Unknown
			//IL_058d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0597: Expected O, but got Unknown
			//IL_0525: Unknown result type (might be due to invalid IL or missing references)
			//IL_052b: Expected O, but got Unknown
			//IL_0533: Unknown result type (might be due to invalid IL or missing references)
			//IL_0539: Expected O, but got Unknown
			//IL_072e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0738: Expected O, but got Unknown
			//IL_069b: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a1: Expected O, but got Unknown
			//IL_06a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_06af: Expected O, but got Unknown
			//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_06bd: Expected O, but got Unknown
			//IL_06c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_06cc: Expected O, but got Unknown
			//IL_06da: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e0: Expected O, but got Unknown
			//IL_08a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a9: Expected O, but got Unknown
			//IL_08b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b7: Expected O, but got Unknown
			//IL_08bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c5: Expected O, but got Unknown
			//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_08d4: Expected O, but got Unknown
			//IL_08dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e2: Expected O, but got Unknown
			//IL_08f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_08f9: Expected O, but got Unknown
			//IL_0901: Unknown result type (might be due to invalid IL or missing references)
			//IL_0907: Expected O, but got Unknown
			//IL_0918: Unknown result type (might be due to invalid IL or missing references)
			//IL_091e: Expected O, but got Unknown
			//IL_0926: Unknown result type (might be due to invalid IL or missing references)
			//IL_092c: Expected O, but got Unknown
			//IL_0936: Unknown result type (might be due to invalid IL or missing references)
			//IL_093c: Expected O, but got Unknown
			List<CodeInstruction> list = instructions.ToList();
			FieldInfo fieldInfo = AccessTools.Field(typeof(BobberBar), "difficulty");
			FieldInfo objB = AccessTools.Field(typeof(BobberBar), "motionType");
			FieldInfo objB2 = AccessTools.Field(typeof(BobberBar), "bobberAcceleration");
			FieldInfo fieldInfo2 = AccessTools.Field(typeof(BobberBar), "bobberSpeed");
			FieldInfo objB3 = AccessTools.Field(typeof(BobberBar), "bobberPosition");
			FieldInfo fieldInfo3 = AccessTools.Field(typeof(BobberBar), "bobberBarSpeed");
			FieldInfo objB4 = AccessTools.Field(typeof(BobberBar), "bobberBarPos");
			FieldInfo objB5 = AccessTools.Field(typeof(BobberBar), "floaterSinkerAcceleration");
			MethodInfo method = typeof(Math).GetMethod("Min", new Type[2]
			{
				typeof(float),
				typeof(float)
			});
			MethodInfo method2 = typeof(BobberBarPatches).GetMethod("GetAccelerationBoost", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method3 = typeof(BobberBarPatches).GetMethod("GetFrameScale", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method4 = typeof(BobberBarPatches).GetMethod("GetSmoothFactor", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method5 = typeof(BobberBarPatches).GetMethod("ScaleProbability", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method6 = typeof(BobberBarPatches).GetMethod("ApplyBarInput", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method7 = typeof(BobberBarPatches).GetMethod("ApplyBarPosition", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method8 = typeof(BobberBarPatches).GetMethod("ApplyFishPosition", BindingFlags.Static | BindingFlags.Public);
			MethodInfo method9 = typeof(BobberBarPatches).GetMethod("ApplyBounce", BindingFlags.Static | BindingFlags.Public);
			CodeInstruction[] array = (CodeInstruction[])(object)new CodeInstruction[2]
			{
				new CodeInstruction(OpCodes.Ldc_R4, (object)150f),
				new CodeInstruction(OpCodes.Call, (object)method)
			};
			for (int i = 0; i < list.Count; i++)
			{
				CodeInstruction val = list[i];
				CodeInstruction val2 = ((i + 1 < list.Count) ? list[i + 1] : null);
				CodeInstruction val3 = ((i + 2 < list.Count) ? list[i + 2] : null);
				if (val.opcode == OpCodes.Ldfld && object.Equals(val.operand, fieldInfo))
				{
					if ((val2 != null && val2.opcode == OpCodes.Ldfld && object.Equals(val2.operand, objB)) || (val2 != null && val2.opcode == OpCodes.Ldc_R4 && IsFloat(val2.operand, 2000f)) || (val2 != null && val2.opcode == OpCodes.Ldc_R4 && IsFloat(val2.operand, 1000f)) || (val2 != null && val2.opcode == OpCodes.Conv_I4 && val3 != null && val3.opcode == OpCodes.Ldc_I4_2))
					{
						list.InsertRange(i + 1, array);
						i += array.Length;
					}
				}
				else if (val.opcode == OpCodes.Stfld && object.Equals(val.operand, objB2) && i > 0)
				{
					list.InsertRange(i, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[5]
					{
						new CodeInstruction(OpCodes.Ldarg_0, (object)null),
						new CodeInstruction(OpCodes.Dup, (object)null),
						new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo),
						new CodeInstruction(OpCodes.Call, (object)method2),
						new CodeInstruction(OpCodes.Mul, (object)null)
					});
					i += 5;
				}
				else if (val.opcode == OpCodes.Ldc_R4 && (IsFloat(val.operand, 4000f) || IsFloat(val.operand, 2000f) || IsFloat(val.operand, 1000f)) && val2 != null && val2.opcode == OpCodes.Div)
				{
					list.Insert(i + 2, new CodeInstruction(OpCodes.Call, (object)method5));
					i++;
				}
				else if (val.opcode == OpCodes.Ldc_R4 && IsFloat(val.operand, 0.01f) && i > 0 && list[i - 1].opcode == OpCodes.Ldfld && object.Equals(list[i - 1].operand, objB5))
				{
					list.InsertRange(i + 1, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
					{
						new CodeInstruction(OpCodes.Call, (object)method3),
						new CodeInstruction(OpCodes.Mul, (object)null)
					});
					i += 2;
				}
				else if (val.opcode == OpCodes.Ldc_R4 && IsFloat(val.operand, 5f) && val2 != null && val2.opcode == OpCodes.Div && val3 != null && val3.opcode == OpCodes.Add)
				{
					list.InsertRange(i + 2, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
					{
						new CodeInstruction(OpCodes.Call, (object)method4),
						new CodeInstruction(OpCodes.Mul, (object)null)
					});
					i += 2;
				}
				else if (val.opcode == OpCodes.Add && val2 != null && val2.opcode == OpCodes.Stfld && object.Equals(val2.operand, objB3))
				{
					list[i] = new CodeInstruction(OpCodes.Call, (object)method8);
				}
				else if (val.opcode == OpCodes.Add && i > 3 && IsLocalIndex(list[i - 1], 4) && val2 != null && val2.opcode == OpCodes.Stfld && object.Equals(val2.operand, fieldInfo3) && list[i - 2].opcode == OpCodes.Ldfld && object.Equals(list[i - 2].operand, fieldInfo3) && list[i - 3].opcode == OpCodes.Ldarg_0 && list[i - 4].opcode == OpCodes.Ldarg_0)
				{
					CodeInstruction val4 = list[i - 1];
					list.RemoveRange(i - 4, 5);
					list.InsertRange(i - 4, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[6]
					{
						new CodeInstruction(OpCodes.Ldarg_0, (object)null),
						new CodeInstruction(OpCodes.Dup, (object)null),
						new CodeInstruction(OpCodes.Dup, (object)null),
						new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo3),
						val4,
						new CodeInstruction(OpCodes.Call, (object)method6)
					});
				}
				else if (val.opcode == OpCodes.Add && val2 != null && val2.opcode == OpCodes.Stfld && object.Equals(val2.operand, objB4))
				{
					list[i] = new CodeInstruction(OpCodes.Call, (object)method7);
				}
				else if (val.opcode == OpCodes.Div && i > 7 && list[i - 1].opcode == OpCodes.Ldc_R4 && IsFloat(list[i - 1].operand, 3f) && list[i - 2].opcode == OpCodes.Mul && list[i - 3].opcode == OpCodes.Ldc_R4 && IsFloat(list[i - 3].operand, 2f) && list[i - 4].opcode == OpCodes.Neg && list[i - 5].opcode == OpCodes.Ldfld && object.Equals(list[i - 5].operand, fieldInfo3) && list[i - 6].opcode == OpCodes.Ldarg_0 && list[i - 7].opcode == OpCodes.Ldarg_0)
				{
					list.RemoveRange(i - 7, 8);
					list.InsertRange(i - 7, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[10]
					{
						new CodeInstruction(OpCodes.Ldarg_0, (object)null),
						new CodeInstruction(OpCodes.Dup, (object)null),
						new CodeInstruction(OpCodes.Dup, (object)null),
						new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo3),
						new CodeInstruction(OpCodes.Neg, (object)null),
						new CodeInstruction(OpCodes.Ldc_R4, (object)2f),
						new CodeInstruction(OpCodes.Mul, (object)null),
						new CodeInstruction(OpCodes.Ldc_R4, (object)3f),
						new CodeInstruction(OpCodes.Div, (object)null),
						new CodeInstruction(OpCodes.Call, (object)method9)
					});
				}
			}
			return list;
		}

		[HarmonyPatch("draw")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_0386: Unknown result type (might be due to invalid IL or missing references)
			//IL_038c: Expected O, but got Unknown
			//IL_0395: Unknown result type (might be due to invalid IL or missing references)
			//IL_039b: Expected O, but got Unknown
			//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c3: Expected O, but got Unknown
			//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d1: Expected O, but got Unknown
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo objB = AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[9]
			{
				typeof(Texture2D),
				typeof(Vector2),
				typeof(Rectangle?),
				typeof(Color),
				typeof(float),
				typeof(Vector2),
				typeof(float),
				typeof(SpriteEffects),
				typeof(float)
			}, (Type[])null);
			MethodInfo method = typeof(BobberBarPatches).GetMethod("GetFishIconRotation", BindingFlags.Static | BindingFlags.Public);
			MethodInfo objB2 = AccessTools.PropertyGetter(typeof(Color), "White");
			MethodInfo method2 = typeof(BobberBarPatches).GetMethod("GetFishIconColor", BindingFlags.Static | BindingFlags.Public);
			ConstructorInfo constructor = typeof(Vector2).GetConstructor(new Type[2]
			{
				typeof(float),
				typeof(float)
			});
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].opcode != OpCodes.Callvirt || !object.Equals(list[i].operand, objB) || i < 8 || !(list[i - 1].opcode == OpCodes.Ldc_R4) || !IsFloat(list[i - 1].operand, 0.88f) || !(list[i - 2].opcode == OpCodes.Ldc_I4_0) || !(list[i - 3].opcode == OpCodes.Ldc_R4) || !IsFloat(list[i - 3].operand, 2f) || !(list[i - 4].opcode == OpCodes.Newobj) || !object.Equals(list[i - 4].operand, constructor) || !(list[i - 5].opcode == OpCodes.Ldc_R4) || !IsFloat(list[i - 5].operand, 10f) || !(list[i - 6].opcode == OpCodes.Ldc_R4) || !IsFloat(list[i - 6].operand, 10f) || !(list[i - 7].opcode == OpCodes.Ldc_R4) || !IsFloat(list[i - 7].operand, 0f) || !(list[i - 8].opcode == OpCodes.Call) || !object.Equals(list[i - 8].operand, objB2))
				{
					continue;
				}
				bool flag = false;
				for (int num = i - 9; num >= Math.Max(0, i - 35); num--)
				{
					if (list[num].opcode == OpCodes.Ldc_I4 && object.Equals(list[num].operand, 1840))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					list.RemoveAt(i - 8);
					list.InsertRange(i - 8, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
					{
						new CodeInstruction(OpCodes.Ldarg_0, (object)null),
						new CodeInstruction(OpCodes.Call, (object)method2)
					});
					list.RemoveAt(i - 6);
					list.InsertRange(i - 6, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
					{
						new CodeInstruction(OpCodes.Ldarg_0, (object)null),
						new CodeInstruction(OpCodes.Call, (object)method)
					});
					i += 2;
				}
			}
			return list;
		}

		private static bool IsLocalIndex(CodeInstruction code, int index)
		{
			if (code.opcode != OpCodes.Ldloc && code.opcode != OpCodes.Ldloc_S)
			{
				return false;
			}
			if (code.operand is LocalBuilder localBuilder)
			{
				return localBuilder.LocalIndex == index;
			}
			return false;
		}

		private static bool IsFloat(object operand, float value)
		{
			if (operand is float num)
			{
				return Math.Abs(num - value) < 0.001f;
			}
			return false;
		}

		public static float GetAccelerationBoost(BobberBar instance, float difficulty)
		{
			if (FestivalFishingService.IsVanillaFestivalMode())
			{
				return 1f;
			}
			if (difficulty <= 100f)
			{
				return 1f;
			}
			InstanceData value;
			int level = (_instanceData.TryGetValue(instance, out value) ? value.DifficultyLevel : 100);
			float accelerationTier = GetAccelerationTier(level);
			return 1f + accelerationTier * (difficulty - 100f) / 100f;
		}

		public static float GetAccelerationTier(int level)
		{
			if (level <= 0)
			{
				return 0.1f;
			}
			if (level >= 90)
			{
				return 1f;
			}
			if (level < 50)
			{
				return 0.1f + 0.1f * (float)level / 50f;
			}
			if (level < 70)
			{
				return 0.2f + 0.2f * (float)(level - 50) / 20f;
			}
			if (level < 80)
			{
				return 0.4f + 0.3f * (float)(level - 70) / 10f;
			}
			return 0.7f + 0.3f * (float)(level - 80) / 10f;
		}

		public static float GetJumpInterval(float adjustedDifficulty)
		{
			if (adjustedDifficulty >= 551f)
			{
				return 3f;
			}
			if (adjustedDifficulty >= 451f)
			{
				return 4f;
			}
			if (adjustedDifficulty >= 351f)
			{
				return 5f;
			}
			if (adjustedDifficulty >= 251f)
			{
				return 6f;
			}
			return 8f;
		}

		public static float GetAlpha(BobberBar instance)
		{
			if (!_instanceData.TryGetValue(instance, out var value))
			{
				return 0f;
			}
			return value.Alpha;
		}

		public static int GetChallengeStars(int difficultyLevel, float elapsedSeconds)
		{
			if (difficultyLevel >= 95)
			{
				return 3;
			}
			float num = elapsedSeconds - 300f;
			if (num < 0f)
			{
				return 3;
			}
			return Math.Max(0, 3 - ((int)Math.Floor(num / 60f) + 1));
		}

		public static float GetChallengeStarMultiplier(int stars)
		{
			return 1f - 0.2f * (float)(3 - stars);
		}

		public static float GetFrameScale()
		{
			if (FestivalFishingService.IsVanillaFestivalMode())
			{
				return 1f;
			}
			float num = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (num <= 0f)
			{
				return 1f;
			}
			float num2 = num * 60f;
			if (!(Math.Abs(num2 - 1f) < 0.0001f))
			{
				return num2;
			}
			return 1f;
		}

		public static float GetSmoothFactor()
		{
			float frameScale = GetFrameScale();
			if (Math.Abs(frameScale - 1f) < 0.0001f)
			{
				return 0.2f;
			}
			return (float)(1.0 - Math.Pow(0.8, frameScale));
		}

		public static float ScaleProbability(float probability)
		{
			float frameScale = GetFrameScale();
			if (Math.Abs(frameScale - 1f) < 0.0001f)
			{
				return probability;
			}
			double num = Math.Max(0.0, Math.Min(1.0, probability));
			return (float)(1.0 - Math.Pow(1.0 - num, frameScale));
		}

		public static float ApplyBarInput(BobberBar instance, float speed, float num5)
		{
			float frameScale = GetFrameScale();
			float alpha = GetAlpha(instance);
			if (alpha <= 0f)
			{
				return speed + num5 * frameScale;
			}
			float num6 = 30f - 5f * alpha;
			float directionFactor = GetDirectionFactor(instance, num5, alpha);
			float num7 = ((num5 < 0f) ? (0f - num6) : num6) * directionFactor;
			if (_instanceData.TryGetValue(instance, out var value) && !value.BarInputDiagnosticLogged)
			{
				value.BarInputDiagnosticLogged = true;
				FishingLog.Log($"[BobberBar] 手感系统激活 | 实例: {((object)instance).GetHashCode()} | α: {alpha:P0} | 方向系数: {directionFactor:F2} | 目标速度: {num7:F1}px/帧 | 原生分量: {speed + num5 * frameScale:F1}", (LogLevel)2);
			}
			return (1f - alpha) * (speed + num5 * frameScale) + alpha * num7;
		}

		private static float GetDirectionFactor(BobberBar instance, float num5, float alpha)
		{
			if (num5 == 0f || instance == null)
			{
				return 1f;
			}
			float num6 = instance.bobberBarPos + (float)instance.bobberBarHeight / 2f;
			if (!((num5 < 0f) ? (num6 > instance.bobberPosition) : (num6 < instance.bobberPosition)))
			{
				return 1f - 0.2f * alpha;
			}
			return 1f + 0.2f * alpha;
		}

		public static float ApplyBarPosition(float pos, float speed)
		{
			return pos + speed * GetFrameScale();
		}

		public static float ApplyFishPosition(float pos, float delta)
		{
			return pos + delta * GetFrameScale();
		}

		public static float ApplyBounce(BobberBar instance, float bounced)
		{
			return bounced * (1f - GetAlpha(instance));
		}

		internal static void RecordAssistObservation(string fishId, double rank, int level)
		{
			if (AssistObservations.Count >= 500)
			{
				AssistObservations.RemoveAt(0);
			}
			AssistObservations.Add(new AssistObservation(fishId, rank, level));
		}

		internal static IReadOnlyList<AssistObservation> GetAssistObservations()
		{
			return AssistObservations;
		}

		internal static void ClearAssistObservations()
		{
			AssistObservations.Clear();
		}

		private static void TryTriggerAssist(Farmer player, InstanceData data, ref int bobberBarHeight, float tipX, float tipY)
		{
			try
			{
				if (player == null || !player.IsLocalPlayer)
				{
					return;
				}
				bool flag = ModEntry.ConsumeForceAssistFlag();
				if (data.HasChallengeBait)
				{
					return;
				}
				double assistChance = DifficultyCalculator.GetAssistChance(DifficultyManager.GetCountableCrownCount(player), 61);
				if (flag || (!(assistChance <= 0.0) && !(Game1.random.NextDouble() >= assistChance)))
				{
					List<string> countableStarredFish = DifficultyManager.GetCountableStarredFish(player);
					if (countableStarredFish.Count != 0)
					{
						string text = countableStarredFish[Game1.random.Next(countableStarredFish.Count)];
						double assistRank = DifficultyManager.GetAssistRank(text, player, countableStarredFish);
						int randomAssistLevel = DifficultyCalculator.GetRandomAssistLevel(assistRank);
						RecordAssistObservation(text, assistRank, randomAssistLevel);
						int value = bobberBarHeight;
						bobberBarHeight += randomAssistLevel * 8;
						data.AssistLevel = randomAssistLevel;
						data.AssistFishId = text;
						string text2 = PickAssistText(text, DifficultyManager.GetDifficultyLevel(text, player));
						AddTip(data.OtherTips, text2, tipX, tipY, centered: false, rightAligned: true, actionTip: false, 15f);
						FishingLog.Log($"[BobberBar] 助战触发 | 助战鱼: {text} | 难度: {DifficultyManager.GetDifficultyLevel(text, player)} | 排位: {assistRank:P0} | 临时钓鱼等级: +{randomAssistLevel} | 绿条高度: {value} → {bobberBarHeight} | 总概率: {assistChance:P1}{(flag ? " | 测试强制触发" : "")} | 文案: {text2}", (LogLevel)2);
					}
				}
			}
			catch (Exception value2)
			{
				FishingLog.Log($"BobberBar 助战判定失败: {value2}", (LogLevel)4);
			}
		}

		private static string PickAssistText(string fishId, int level)
		{
			try
			{
				Item obj = ItemRegistry.Create(fishId, 1, 0, false);
				string text = ((obj != null) ? obj.DisplayName : null) ?? fishId;
				string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
				int value = Game1.random.Next(1, 21);
				string text3 = $"hud.assist.{value}";
				string text4 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text3, (object)new
				{
					fishName = text,
					rankName = text2
				}));
				return (string.IsNullOrWhiteSpace(text4) || text4 == text3) ? ("荣耀的" + text + text2 + "前来护驾！") : text4;
			}
			catch (Exception ex)
			{
				FishingLog.Log("[BobberBar] 助战文案生成失败: " + ex.Message, (LogLevel)4);
				return "皇冠鱼前来护驾！";
			}
		}

		private static string PickJumpText(bool jumpUp)
		{
			try
			{
				int value = Game1.random.Next(1, 16);
				string text = (jumpUp ? $"hud.jump.up.{value}" : $"hud.jump.down.{value}");
				string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text));
				return (!string.IsNullOrWhiteSpace(text2) && !(text2 == text)) ? text2 : (jumpUp ? "鱼跃！" : "甩尾！");
			}
			catch (Exception ex)
			{
				FishingLog.Log("[BobberBar] 跳鱼文案生成失败: " + ex.Message, (LogLevel)4);
				return jumpUp ? "鱼跃！" : "甩尾！";
			}
		}

		private static string PickPeakText(int level)
		{
			try
			{
				string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
				int value = Game1.random.Next(1, 11);
				string text2 = $"hud.peak.{value}";
				string text3 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text2, (object)new
				{
					rankName = text
				}));
				return (string.IsNullOrWhiteSpace(text3) || text3 == text2) ? ("[" + text + "]的力气达到巅峰") : text3;
			}
			catch (Exception ex)
			{
				FishingLog.Log("[BobberBar] 巅峰文案生成失败: " + ex.Message, (LogLevel)4);
				return "[职阶]的力气达到巅峰";
			}
		}

		[HarmonyPatch("draw")]
		[HarmonyPostfix]
		public static void Draw_Postfix(BobberBar __instance, SpriteBatch b)
		{
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			//IL_014a: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!_instanceData.TryGetValue(__instance, out var value))
				{
					return;
				}
				if (value.HasChallengeBait && value.AdjustedDifficulty > 100f)
				{
					float num = ((((Rectangle)(ref Game1.viewport)).Width > 0) ? ((float)((Rectangle)(ref Game1.uiViewport)).Width / (float)((Rectangle)(ref Game1.viewport)).Width) : 1f);
					if (num <= 0f || float.IsNaN(num) || float.IsInfinity(num))
					{
						num = 1f;
					}
					int challengeStars = GetChallengeStars(value.DifficultyLevel, value.BattleElapsedSeconds);
					if (challengeStars < 3)
					{
						int num2 = (((float)((IClickableMenu)__instance).xPositionOnScreen > (float)((Rectangle)(ref Game1.viewport)).Width * 0.75f) ? (((IClickableMenu)__instance).xPositionOnScreen - 80) : (((IClickableMenu)__instance).xPositionOnScreen + 216));
						int num3 = (__instance.bobbers.Contains("(O)SonarBobber") ? (((IClickableMenu)__instance).yPositionOnScreen + 136) : (((IClickableMenu)__instance).yPositionOnScreen + 40));
						Rectangle value2 = default(Rectangle);
						((Rectangle)(ref value2))..ctor(217, 205, 19, 19);
						for (int i = challengeStars; i < 3; i++)
						{
							b.Draw(Game1.mouseCursors_1_6, new Vector2((float)(num2 - 12), (float)(num3 + i * 40)) * num + __instance.everythingShake * num, (Rectangle?)value2, Color.White, 0f, Vector2.Zero, 2f * num, (SpriteEffects)0, 0.89f);
						}
					}
				}
				foreach (FloatingTip actionTip in value.ActionTips)
				{
					DrawTip(b, actionTip);
				}
				foreach (FloatingTip otherTip in value.OtherTips)
				{
					DrawTip(b, otherTip);
				}
			}
			catch (Exception value3)
			{
				FishingLog.LogRateLimited("BobberBar.Draw_Postfix", $"BobberBar 小游戏文案绘制失败: {value3}", (LogLevel)4);
			}
		}

		public static float GetJumpWindupRotationAt(float elapsed, bool jumpUp)
		{
			float num = ((elapsed <= 0.77f) ? (70f * (elapsed / 0.77f)) : (70f * (1f - (elapsed - 0.77f) / 0.11f)));
			return (jumpUp ? (-1f) : 1f) * num;
		}

		private static float GetJumpWindupRotation(InstanceData data, bool jumpUp)
		{
			float elapsed = 0.88f - Math.Max(0f, data.JumpWindupSeconds);
			return GetJumpWindupRotationAt(elapsed, jumpUp);
		}

		public static float GetFishIconRotation(BobberBar instance)
		{
			if (!_instanceData.TryGetValue(instance, out var value))
			{
				return 0f;
			}
			if (value.IsIdle)
			{
				double totalMilliseconds = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				return (float)(Math.Sin(totalMilliseconds / 1000.0 * Math.PI * 2.0) * 0.09);
			}
			if (value.JumpWindupActive)
			{
				bool jumpUp = value.JumpPendingTarget <= 133f;
				return MathHelper.ToRadians(GetJumpWindupRotation(value, jumpUp));
			}
			return 0f;
		}

		public static Color GetFishIconColor(BobberBar instance)
		{
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			if (_instanceData.TryGetValue(instance, out var value) && value.JumpWindupActive)
			{
				double totalMilliseconds = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				float num = (float)(0.5 + 0.5 * Math.Sin(totalMilliseconds / 180.0 * Math.PI * 2.0));
				return Color.Lerp(Color.Red, Color.White, 0.2f + 0.3f * num);
			}
			return Color.White;
		}

		private static void InterruptPhrase(InstanceData data)
		{
			data.PhraseSwitchPending = false;
			data.PhraseMode = PhraseMode.Free;
			data.PhraseSamples.Clear();
			data.PhraseJumpSeen = false;
			data.PhraseWindupStartTime = -1f;
			data.PhraseJumpText = null;
			data.PlaybackWindupShown = false;
			data.JumpWindupActive = false;
		}

		private static void HandlePhrasePrefix(BobberBar instance, InstanceData data, ref float position, ref float speed, ref float target, ref float floaterSinker, float dt)
		{
			if (data.ResultStarted || data.IsIdle)
			{
				return;
			}
			if (data.EffectiveDifficulty < 150f)
			{
				if (data.PhraseMode != PhraseMode.Free)
				{
					data.PhraseMode = PhraseMode.Free;
					data.PhraseSamples.Clear();
					data.JumpWindupActive = false;
				}
			}
			else
			{
				if (data.PhraseMode != PhraseMode.Playing)
				{
					return;
				}
				data.PhrasePlayTime += dt;
				if (data.PhrasePlayTime >= data.PhraseDuration)
				{
					if (data.PhraseSwitchPending)
					{
						data.PhraseSwitchPending = false;
						data.PhraseMode = PhraseMode.Free;
						data.PhraseSamples.Clear();
						data.PhraseJumpSeen = false;
						data.PhraseWindupStartTime = -1f;
						data.PhraseJumpText = null;
						data.PlaybackWindupShown = false;
						data.JumpWindupActive = false;
						data.NextPhraseThreshold = Math.Min(1f, data.NextPhraseThreshold + 1f / 3f);
					}
					else
					{
						data.PhrasePlayTime -= data.PhraseDuration;
						data.PlaybackWindupShown = false;
						data.JumpWindupActive = false;
					}
				}
				if (data.PhraseMode != PhraseMode.Playing)
				{
					return;
				}
				float num = (position = InterpolatePhrase(data, data.PhrasePlayTime));
				speed = 0f;
				target = num;
				floaterSinker = 0f;
				if (!data.PlaybackWindupShown && data.PhraseWindupStartTime >= 0f && data.PhrasePlayTime >= data.PhraseWindupStartTime)
				{
					data.PlaybackWindupShown = true;
					data.JumpWindupActive = true;
					data.JumpWindupSeconds = 0.88f;
					data.JumpPendingTarget = (data.PhraseJumpIsUp ? 0f : 500f);
					if (!string.IsNullOrEmpty(data.PhraseJumpText))
					{
						AddTip(data.ActionTips, data.PhraseJumpText, (float)((IClickableMenu)instance).xPositionOnScreen + 124f, (float)((IClickableMenu)instance).yPositionOnScreen + 36f + num - 30f, centered: false, rightAligned: false, actionTip: true);
					}
				}
				if (data.JumpWindupActive)
				{
					data.JumpWindupSeconds -= dt;
					if (data.JumpWindupSeconds <= 0f)
					{
						data.JumpWindupActive = false;
					}
				}
			}
		}

		private static void HandlePhrasePostfix(InstanceData data, float position, float catchProgress)
		{
			if (data.ResultStarted || data.IsIdle || data.EffectiveDifficulty < 150f)
			{
				return;
			}
			bool flag = position >= 256f && position <= 276f;
			if (data.PhraseMode == PhraseMode.WaitingMiddle && flag)
			{
				data.PhraseMode = PhraseMode.Recording;
				data.PhraseRecordTime = 0f;
				data.PhraseSamples.Clear();
				data.PhraseSamples.Add((0f, position));
				data.PhraseJumpSeen = false;
				data.PhraseWindupStartTime = -1f;
				data.PhraseJumpText = null;
			}
			else if (data.PhraseMode == PhraseMode.Recording)
			{
				float num = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
				if (num <= 0f)
				{
					num = 1f / 60f;
				}
				data.PhraseRecordTime += num;
				data.PhraseSamples.Add((data.PhraseRecordTime, position));
				if (data.PhraseJumpSeen && flag)
				{
					data.PhraseDuration = data.PhraseRecordTime;
					data.PhraseMode = PhraseMode.Playing;
					data.PhrasePlayTime = 0f;
					data.PlaybackWindupShown = false;
				}
			}
			if (data.PhraseMode == PhraseMode.Playing && data.NextPhraseThreshold < 1f && catchProgress >= data.NextPhraseThreshold)
			{
				data.PhraseSwitchPending = true;
			}
		}

		private static float InterpolatePhrase(InstanceData data, float t)
		{
			List<(float, float)> phraseSamples = data.PhraseSamples;
			if (phraseSamples.Count == 0)
			{
				return 0f;
			}
			if (t <= phraseSamples[0].Item1)
			{
				return phraseSamples[0].Item2;
			}
			for (int i = 0; i < phraseSamples.Count - 1; i++)
			{
				float item = phraseSamples[i].Item1;
				float item2 = phraseSamples[i + 1].Item1;
				if (t >= item && t <= item2)
				{
					float num = ((item2 > item) ? ((t - item) / (item2 - item)) : 0f);
					return phraseSamples[i].Item2 + (phraseSamples[i + 1].Item2 - phraseSamples[i].Item2) * num;
				}
			}
			return phraseSamples[phraseSamples.Count - 1].Item2;
		}

		private static string PickIdleText()
		{
			try
			{
				int value = Game1.random.Next(1, 11);
				string text = $"hud.idle.{value}";
				string text2 = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(text));
				return (string.IsNullOrWhiteSpace(text2) || text2 == text) ? "它停下来歇口气" : text2;
			}
			catch (Exception ex)
			{
				FishingLog.Log("[BobberBar] 停战文案生成失败: " + ex.Message, (LogLevel)4);
				return "它停下来歇口气";
			}
		}
	}
	[HarmonyPatch(typeof(Buff))]
	internal class BuffPatches
	{
		[HarmonyPatch("update")]
		[HarmonyPrefix]
		public static bool Update_Prefix(Buff __instance)
		{
			if (__instance != null && __instance.id == "food" && Game1.activeClickableMenu is BobberBar)
			{
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(CollectionsPage))]
	internal class CollectionsPagePatches
	{
		private static Texture2D _crownTexture;

		private static bool _collectionsDrawing;

		private static int _collectionsTab = -1;

		[HarmonyPatch("createDescription")]
		[HarmonyPostfix]
		public static void CreateDescription_Postfix(CollectionsPage __instance, string id, ref string __result, int ___currentTab)
		{
			try
			{
				if (___currentTab != 1 || string.IsNullOrEmpty(id))
				{
					return;
				}
				string fishId = SpecialFishHelper.NormalizeItemId(id);
				bool flag = DifficultyManager.HasCollectionStar(fishId, Game1.player);
				List<string> list = new List<string>();
				if (!SpecialFishHelper.IsLegendaryFish(id))
				{
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
					if (difficultyLevel > 0)
					{
						string rankKey = DifficultyCalculator.GetRankKey(difficultyLevel);
						string rankName = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
						list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.challengeRank", (object)new
						{
							rankName = rankName,
							level = difficultyLevel
						})));
					}
				}
				if (flag)
				{
					if (!DifficultyManager.IsCountableFish(fishId))
					{
						list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.noCrownControl")));
					}
					else
					{
						list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.crownControl", (object)new
						{
							crownCount = DifficultyManager.GetCountableCrownCount(Game1.player),
							crownTarget = 61
						})));
					}
				}
				if (list.Count > 0)
				{
					__result = __result + Environment.NewLine + string.Join(" | ", list);
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"[CollectionsPage] createDescription Postfix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("draw")]
		[HarmonyPrefix]
		public static void CollectionsDraw_Prefix(CollectionsPage __instance)
		{
			_collectionsDrawing = true;
			_collectionsTab = __instance.currentTab;
		}

		[HarmonyPatch("draw")]
		[HarmonyPostfix]
		public static void CollectionsDraw_Postfix()
		{
			_collectionsDrawing = false;
			_collectionsTab = -1;
		}

		private static Texture2D GetCrownTexture()
		{
			if (_crownTexture == null || ((GraphicsResource)_crownTexture).IsDisposed)
			{
				_crownTexture = Game1.content.Load<Texture2D>("Characters\\Farmer\\hats");
			}
			return _crownTexture;
		}

		[HarmonyPatch(typeof(ClickableTextureComponent), "draw", new Type[]
		{
			typeof(SpriteBatch),
			typeof(Color),
			typeof(float),
			typeof(int),
			typeof(int),
			typeof(int)
		})]
		[HarmonyPostfix]
		public static void Draw_Postfix(SpriteBatch b, ClickableTextureComponent __instance, float layerDepth)
		{
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!_collectionsDrawing || _collectionsTab != 1)
				{
					return;
				}
				string[] array = ((ClickableComponent)__instance).name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (array.Length == 0 || !DifficultyManager.HasCollectionStar(array[0], Game1.player))
				{
					return;
				}
				if (!DifficultyManager.IsCountableFish(array[0]))
				{
					b.Draw(Game1.mouseCursors, new Rectangle(((ClickableComponent)__instance).bounds.X + 3, ((ClickableComponent)__instance).bounds.Y + 3, 20, 20), (Rectangle?)new Rectangle(346, 392, 8, 8), Color.Gold, 0f, Vector2.Zero, (SpriteEffects)0, layerDepth + 0.01f);
					return;
				}
				Texture2D crownTexture = GetCrownTexture();
				Rectangle value = default(Rectangle);
				((Rectangle)(ref value))..ctor(20, 800, 20, 20);
				bool flag = DifficultyManager.HasChallengeCrown(array[0], Game1.player);
				bool flag2 = DifficultyManager.HasLevel100FlowCrown(array[0], Game1.player);
				if (flag)
				{
					double a = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0;
					float num = (float)Math.Sin(a);
					Color val = Color.Lerp(new Color(218, 165, 32), Color.White, (num + 1f) / 2f * 0.4f);
					float num2 = 24f * (flag2 ? 1.2f : 1f) * (1f + 0.05f * num);
					Vector2 val2 = default(Vector2);
					((Vector2)(ref val2))..ctor((float)(((ClickableComponent)__instance).bounds.X + 3 + 12), (float)(((ClickableComponent)__instance).bounds.Y + 3 + 12));
					b.Draw(crownTexture, val2, (Rectangle?)value, val, 0f, new Vector2(10f, 10f), num2 / 20f, (SpriteEffects)0, layerDepth + 0.01f);
				}
				else
				{
					b.Draw(crownTexture, new Vector2((float)(((ClickableComponent)__instance).bounds.X + 3), (float)(((ClickableComponent)__instance).bounds.Y + 3)), (Rectangle?)value, Color.White, 0f, Vector2.Zero, 1.2f, (SpriteEffects)0, layerDepth + 0.01f);
				}
			}
			catch (Exception value2)
			{
				FishingLog.Log($"[CollectionsPage] 皇冠绘制失败: {value2}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(CrabPot))]
	internal static class CrabPotPatches
	{
		private sealed class PendingCrabPotHarvest
		{
			public string ItemId;

			public int OldLevel;
		}

		private static readonly Dictionary<long, PendingCrabPotHarvest> _pendingHarvest = new Dictionary<long, PendingCrabPotHarvest>();

		[HarmonyPatch("checkForAction")]
		[HarmonyPrefix]
		public static void CheckForAction_Prefix(CrabPot __instance, Farmer who, bool justCheckingForActivity)
		{
			try
			{
				if (justCheckingForActivity || who == null || !who.IsLocalPlayer || __instance.tileIndexToShow != 714)
				{
					return;
				}
				Object val = ((NetFieldBase<Object, NetRef<Object>>)(object)((Object)__instance).heldObject)?.Value;
				if (val != null)
				{
					string text = SpecialFishHelper.NormalizeItemId(((Item)val).QualifiedItemId);
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, who);
					int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
					if (quantityMultiplier > 1 && ((Item)val).Stack > 0 && ((Item)val).Stack <= int.MaxValue / quantityMultiplier)
					{
						((Item)val).Stack = ((Item)val).Stack * quantityMultiplier;
					}
					_pendingHarvest[who.UniqueMultiplayerID] = new PendingCrabPotHarvest
					{
						ItemId = text,
						OldLevel = difficultyLevel
					};
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"[CrabPot] 收获前缀失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("checkForAction")]
		[HarmonyPostfix]
		public static void CheckForAction_Postfix(CrabPot __instance, Farmer who, bool justCheckingForActivity, bool __result)
		{
			try
			{
				if (who == null)
				{
					return;
				}
				PendingCrabPotHarvest value;
				if (!__result || !who.IsLocalPlayer)
				{
					_pendingHarvest.Remove(who.UniqueMultiplayerID);
				}
				else if (_pendingHarvest.Remove(who.UniqueMultiplayerID, out value))
				{
					int oldLevel = value.OldLevel;
					int value2 = DifficultyManager.RecordSuccess(value.ItemId, 1, who);
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(value.ItemId, who);
					if (difficultyLevel > oldLevel)
					{
						HUDNotifier.ShowSuccessNotification(value.ItemId, difficultyLevel, oldLevel);
					}
					FishingLog.Log($"[CrabPot] 蟹笼收获记录 | 玩家: {who.UniqueMultiplayerID} | 物品: {value.ItemId} | 等级: {oldLevel} → {difficultyLevel} | 实际增长: +{value2}", (LogLevel)2);
				}
			}
			catch (Exception value3)
			{
				FishingLog.Log($"[CrabPot] 收获后缀失败: {value3}", (LogLevel)4);
			}
		}

		public static void ClearPending()
		{
			_pendingHarvest.Clear();
		}
	}
	[HarmonyPatch(typeof(Farmer))]
	internal class FarmerFishingLevelPatches
	{
		[HarmonyPatch("gainExperience")]
		[HarmonyPrefix]
		public static void GainExperience_Prefix(Farmer __instance, int which, ref int howMuch)
		{
			try
			{
				if (FestivalFishingService.IsVanillaFestivalMode() || which != 1 || howMuch <= 0 || !__instance.IsLocalPlayer || !FishingRodPatches.TryBeginExperienceAdjustment(__instance, out var data))
				{
					return;
				}
				if (data.HarvestLimited)
				{
					howMuch = 0;
					return;
				}
				if (data.NativeDifficulty > 0f && data.PassedDifficulty > 0f && (data.PassedDifficulty < data.NativeDifficulty || data.PassedDifficulty > 120f))
				{
					float experienceDifficulty = DifficultyCalculator.GetExperienceDifficulty(data.PassedDifficulty, data.NativeDifficulty);
					int nativeExperience = DifficultyCalculator.GetNativeExperience(data.FishQuality, experienceDifficulty, data.TreasureCaught, data.WasPerfect, data.IsBossFish);
					FishingLog.Log($"[FarmerFishingExperience] 经验基数重算 | 难度等级: {data.DifficultyLevel} | 传入难度: {data.PassedDifficulty:F0} | 原生难度: {data.NativeDifficulty:F0} | 钳制后: {experienceDifficulty:F0} | 经验: {howMuch} → {nativeExperience}", (LogLevel)2);
					howMuch = nativeExperience;
				}
				int experienceMultiplier = data.ExperienceMultiplier;
				if (experienceMultiplier > 1)
				{
					long num = (long)howMuch * (long)experienceMultiplier;
					howMuch = (int)Math.Min(2147483647L, num);
					FishingLog.Log($"[FarmerFishingExperience] 经验倍率 | 难度等级: {data.DifficultyLevel} | 倍率: ×{experienceMultiplier} | 经验: {num / experienceMultiplier} → {howMuch}", (LogLevel)1);
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"Fishing experience patch 失败: {value}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(Farmer))]
	internal class FarmerPatches
	{
	}
	[HarmonyPatch(typeof(FishingRod))]
	internal class FishingRodPatches
	{
		internal sealed class PendingFishData
		{
			public int OriginalNum { get; set; }

			public int Multiplier { get; set; }

			public int DifficultyLevel { get; set; }

			public int ExperienceMultiplier { get; set; }

			public int MissCount { get; set; }

			public float AdjustedDifficulty { get; set; }

			public int FishSize { get; set; }

			public bool WildBaitBonus { get; set; }

			public bool ChallengeBonusActive { get; set; }

			public float ChallengeStarMultiplier { get; set; } = 1f;

			public bool HasChallengeBait { get; set; }

			public bool SuccessRecorded { get; set; }

			public bool ExperienceAdjusted { get; set; }

			public int CreateFishCalls { get; set; }

			public bool AllowAdditionalCreateFish { get; set; }

			public bool HarvestLimited { get; set; }

			public float NativeDifficulty { get; set; }

			public float PassedDifficulty { get; set; }

			public int FishQuality { get; set; }

			public bool TreasureCaught { get; set; }

			public bool WasPerfect { get; set; }

			public bool IsBossFish { get; set; }
		}

		internal static readonly Dictionary<string, PendingFishData> _pendingFish = new Dictionary<string, PendingFishData>();

		public static float GetFishVisualScale(FishingRod rod)
		{
			try
			{
				Farmer val = ((rod != null) ? ((Tool)rod).getLastFarmerToUse() : null);
				object obj;
				if (rod == null)
				{
					obj = null;
				}
				else
				{
					ItemMetadata whichFish = rod.whichFish;
					obj = ((whichFish != null) ? whichFish.QualifiedItemId : null);
				}
				string text = (string)obj;
				if (val == null || !val.IsLocalPlayer || string.IsNullOrEmpty(text))
				{
					return 1f;
				}
				string fishId = SpecialFishHelper.NormalizeItemId(text);
				if (!TryGetPending(val, fishId, out var data))
				{
					return 1f;
				}
				return DifficultyCalculator.GetVisualScale(data.DifficultyLevel);
			}
			catch (Exception value)
			{
				FishingLog.LogRateLimited("FishingRodPatches.GetFishVisualScale", $"[FishingRodPatches] 结算视觉缩放读取失败: {value}", (LogLevel)3);
				return 1f;
			}
		}

		public static Vector2 AdjustLandingFishPosition(Vector2 position, FishingRod rod)
		{
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			float fishVisualScale = GetFishVisualScale(rod);
			if (fishVisualScale <= 1.001f || rod?.whichFish == null)
			{
				return position;
			}
			try
			{
				Rectangle caughtItemSourceRect = GetCaughtItemSourceRect(rod);
				return position + new Vector2(0f, (float)caughtItemSourceRect.Height * 3f * (1f - fishVisualScale) / 2f);
			}
			catch (Exception value)
			{
				FishingLog.LogRateLimited("FishingRodPatches.AdjustLandingFishPosition", $"[FishingRodPatches] 落地鱼图底边锚点补偿失败: {value}", (LogLevel)3);
				return position;
			}
		}

		private static Rectangle GetCaughtItemSourceRect(FishingRod rod)
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if (rod.whichFish.TypeIdentifier == "(O)")
			{
				return rod.whichFish.GetParsedOrErrorData().GetSourceRect(0, (int?)null);
			}
			return new Rectangle(228, 408, 16, 16);
		}

		private static string GetCaughtItemTextureName(FishingRod rod)
		{
			if (rod.whichFish.TypeIdentifier == "(O)")
			{
				return rod.whichFish.GetParsedOrErrorData().TextureName;
			}
			return "LooseSprites\\Cursors";
		}

		[HarmonyPatch("doPullFishFromWater")]
		[HarmonyPostfix]
		public static void DoPullFishFromWater_Postfix(FishingRod __instance)
		{
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Farmer val = ((__instance != null) ? ((Tool)__instance).getLastFarmerToUse() : null);
				object obj;
				if (__instance == null)
				{
					obj = null;
				}
				else
				{
					ItemMetadata whichFish = __instance.whichFish;
					obj = ((whichFish != null) ? whichFish.QualifiedItemId : null);
				}
				string text = (string)obj;
				if (val == null || !val.IsLocalPlayer || string.IsNullOrEmpty(text))
				{
					return;
				}
				string fishId = SpecialFishHelper.NormalizeItemId(text);
				if (!TryGetPending(val, fishId, out var data))
				{
					return;
				}
				float visualScale = DifficultyCalculator.GetVisualScale(data.DifficultyLevel);
				if (visualScale <= 1.001f)
				{
					return;
				}
				Rectangle caughtItemSourceRect = GetCaughtItemSourceRect(__instance);
				string caughtItemTextureName = GetCaughtItemTextureName(__instance);
				Vector2 val2 = default(Vector2);
				((Vector2)(ref val2))..ctor((float)caughtItemSourceRect.Width * 4f * (1f - visualScale) / 2f, (float)caughtItemSourceRect.Height * 4f * (1f - visualScale) / 2f);
				foreach (TemporaryAnimatedSprite animation in __instance.animations)
				{
					if (!(animation.textureName != caughtItemTextureName) && !(animation.sourceRect != caughtItemSourceRect))
					{
						animation.scale *= visualScale;
						animation.position += val2;
					}
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"[FishingRodPatches] 飞行动画视觉缩放失败: {value}", (LogLevel)3);
			}
		}

		[HarmonyPatch("doPullFishFromWater")]
		[HarmonyPostfix]
		public static void DoPullFishFromWater_SizeQuality_Postfix(FishingRod __instance)
		{
			try
			{
				Farmer val = ((__instance != null) ? ((Tool)__instance).getLastFarmerToUse() : null);
				object obj;
				if (__instance == null)
				{
					obj = null;
				}
				else
				{
					ItemMetadata whichFish = __instance.whichFish;
					obj = ((whichFish != null) ? whichFish.QualifiedItemId : null);
				}
				string text = (string)obj;
				if (val == null || !val.IsLocalPlayer || string.IsNullOrEmpty(text))
				{
					return;
				}
				string text2 = SpecialFishHelper.NormalizeItemId(text);
				if (!TryGetPending(val, text2, out var data))
				{
					return;
				}
				if (data.DifficultyLevel != 0 && data.FishSize > 0)
				{
					__instance.fishSize = data.FishSize;
				}
				if (data.DifficultyLevel > 0)
				{
					if (__instance.fishQuality >= 2)
					{
						__instance.fishQuality = 4;
					}
					else if (__instance.fishQuality >= 1)
					{
						__instance.fishQuality = 2;
					}
				}
				FishingLog.Log($"[FishingRod] 结算尺寸/品质应用 | 玩家: {val.UniqueMultiplayerID} | 鱼ID: {text2} | 等级: {data.DifficultyLevel} | fishSize: {__instance.fishSize} | fishQuality: {__instance.fishQuality}", (LogLevel)1);
			}
			catch (Exception value)
			{
				FishingLog.Log($"[FishingRodPatches] 结算尺寸/品质应用失败: {value}", (LogLevel)3);
			}
		}

		[HarmonyPatch("draw")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b2: Expected O, but got Unknown
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Expected O, but got Unknown
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d6: Expected O, but got Unknown
			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Expected O, but got Unknown
			//IL_0203: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Expected O, but got Unknown
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo methodInfo = AccessTools.Method(typeof(FishingRodPatches), "GetFishVisualScale", (Type[])null, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(FishingRodPatches), "AdjustLandingFishPosition", (Type[])null, (Type[])null);
			int num = list.FindIndex((CodeInstruction code) => IsLdcI4(code, 1870));
			if (num < 0)
			{
				FishingLog.Log("[FishingRodPatches] 未找到结算面板源矩形，落地真鱼缩放未注入", (LogLevel)3);
				return list;
			}
			int num2 = -1;
			for (int num3 = num; num3 < list.Count; num3++)
			{
				if (IsSpriteBatchDrawCall(list[num3]))
				{
					num2 = num3;
					break;
				}
			}
			HashSet<int> hashSet = new HashSet<int>();
			for (int num4 = Math.Max(0, num2 + 1); num4 < list.Count; num4++)
			{
				if (!IsLdcR4(list[num4], 3f))
				{
					continue;
				}
				for (int num5 = num4 + 1; num5 < Math.Min(list.Count, num4 + 12); num5++)
				{
					if (!(list[num5].opcode != OpCodes.Call) || !(list[num5].opcode != OpCodes.Callvirt))
					{
						if (IsSpriteBatchDrawCall(list[num5]))
						{
							hashSet.Add(num4);
						}
						break;
					}
				}
			}
			List<CodeInstruction> list2 = new List<CodeInstruction>(list.Count + 24);
			int num6 = -1;
			int num7 = 0;
			int num8 = 0;
			for (int num9 = 0; num9 < list.Count; num9++)
			{
				CodeInstruction val = list[num9];
				list2.Add(val);
				if (num9 > num2 && IsGame1GlobalToLocalCall(val))
				{
					num6 = list2.Count - 1;
				}
				if (hashSet.Contains(num9))
				{
					list2.Add(new CodeInstruction(OpCodes.Ldarg_0, (object)null));
					list2.Add(new CodeInstruction(OpCodes.Call, (object)methodInfo));
					list2.Add(new CodeInstruction(OpCodes.Mul, (object)null));
					num7++;
					if (num6 >= 0)
					{
						list2.Insert(num6 + 1, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
						list2.Insert(num6 + 2, new CodeInstruction(OpCodes.Call, (object)methodInfo2));
						num8++;
						num6 = -1;
					}
				}
			}
			FishingLog.Log($"[FishingRodPatches] 落地真鱼缩放注入 | 缩放: {num7} | 底边补偿: {num8}", (LogLevel)((num7 >= 1 && num8 >= 1) ? 2 : 3));
			return list2;
		}

		private static bool IsLdcI4(CodeInstruction code, int expected)
		{
			if (code.opcode == OpCodes.Ldc_I4 && code.operand is int num)
			{
				return num == expected;
			}
			OpCode opcode = code.opcode;
			if (opcode == OpCodes.Ldc_I4_M1)
			{
				return expected == -1;
			}
			OpCode opCode = opcode;
			if (opCode == OpCodes.Ldc_I4_0)
			{
				return expected == 0;
			}
			OpCode opCode2 = opcode;
			if (opCode2 == OpCodes.Ldc_I4_1)
			{
				return expected == 1;
			}
			OpCode opCode3 = opcode;
			if (opCode3 == OpCodes.Ldc_I4_2)
			{
				return expected == 2;
			}
			OpCode opCode4 = opcode;
			if (opCode4 == OpCodes.Ldc_I4_3)
			{
				return expected == 3;
			}
			OpCode opCode5 = opcode;
			if (opCode5 == OpCodes.Ldc_I4_4)
			{
				return expected == 4;
			}
			OpCode opCode6 = opcode;
			if (opCode6 == OpCodes.Ldc_I4_5)
			{
				return expected == 5;
			}
			OpCode opCode7 = opcode;
			if (opCode7 == OpCodes.Ldc_I4_6)
			{
				return expected == 6;
			}
			OpCode opCode8 = opcode;
			if (opCode8 == OpCodes.Ldc_I4_7)
			{
				return expected == 7;
			}
			OpCode opCode9 = opcode;
			if (opCode9 == OpCodes.Ldc_I4_8)
			{
				return expected == 8;
			}
			return false;
		}

		private static bool IsLdcR4(CodeInstruction code, float expected)
		{
			if (code.opcode == OpCodes.Ldc_R4 && code.operand is float num)
			{
				return Math.Abs(num - expected) < 0.001f;
			}
			return false;
		}

		private static bool IsGame1GlobalToLocalCall(CodeInstruction code)
		{
			if (code.opcode == OpCodes.Call && code.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(Game1))
			{
				return methodInfo.Name == "GlobalToLocal";
			}
			return false;
		}

		private static bool IsSpriteBatchDrawCall(CodeInstruction code)
		{
			if ((code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(SpriteBatch))
			{
				return methodInfo.Name == "Draw";
			}
			return false;
		}

		[HarmonyPatch("pullFishFromWater")]
		[HarmonyPrefix]
		public static void PullFishFromWater_Prefix(FishingRod __instance, string fishId, int fishSize, int fishQuality, int fishDifficulty, bool treasureCaught, bool wasPerfect, bool fromFishPond, string setFlagOnCatch, bool isBossFish, int numCaught)
		{
			try
			{
				if (FestivalFishingService.IsVanillaFestivalMode())
				{
					FishingLog.Log("[节日] 原生模式跳过 pending 记录 | 鱼ID: " + SpecialFishHelper.NormalizeItemId(fishId) + " | 开关关闭（节日完全原生）", (LogLevel)2);
					return;
				}
				Farmer lastFarmerToUse = ((Tool)__instance).getLastFarmerToUse();
				if (lastFarmerToUse == null || !lastFarmerToUse.IsLocalPlayer || fromFishPond)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(fishId);
				if (SpecialFishHelper.IsLegendaryFish(text))
				{
					FishingLog.Log("[FishingRod] 传奇鱼（鱼王）豁免规则 | 鱼ID: " + text, (LogLevel)2);
					DifficultyManager.RecordLegendaryCatch(text, lastFarmerToUse);
					HUDNotifier.ShowSuccessNotification(fishId, 0);
					return;
				}
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, lastFarmerToUse);
				int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
				int num = 0;
				float num2 = fishDifficulty;
				bool flag = false;
				float num3 = 0f;
				float nativeDifficulty = 0f;
				IClickableMenu activeClickableMenu = Game1.activeClickableMenu;
				BobberBar val = (BobberBar)(object)((activeClickableMenu is BobberBar) ? activeClickableMenu : null);
				if (val != null)
				{
					num = BobberBarPatches.GetMissCount(val);
					float adjustedDifficulty = BobberBarPatches.GetAdjustedDifficulty(val);
					if (adjustedDifficulty > 0f)
					{
						num2 = adjustedDifficulty;
					}
					flag = BobberBarPatches.HasChallengeBait(val);
					num3 = BobberBarPatches.GetElapsedSeconds(val);
					nativeDifficulty = BobberBarPatches.GetOriginalDifficulty(val);
				}
				bool flag2 = difficultyLevel > 0 && numCaught >= 2 && !flag;
				bool flag3 = flag && num2 > 100f && num3 < 300f;
				float challengeStarMultiplier = 1f;
				if (flag && num2 > 100f && difficultyLevel < 95 && num3 >= 300f)
				{
					challengeStarMultiplier = BobberBarPatches.GetChallengeStarMultiplier(BobberBarPatches.GetChallengeStars(difficultyLevel, num3));
				}
				int num4 = ((fishSize > 0 && difficultyLevel != 0) ? Math.Max(1, (int)Math.Round((float)fishSize * DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel))) : fishSize);
				_pendingFish[GetPendingKey(lastFarmerToUse, text)] = new PendingFishData
				{
					OriginalNum = numCaught,
					Multiplier = quantityMultiplier,
					DifficultyLevel = difficultyLevel,
					ExperienceMultiplier = DifficultyCalculator.GetExperienceMultiplier(difficultyLevel),
					MissCount = num,
					AdjustedDifficulty = num2,
					FishSize = num4,
					WildBaitBonus = flag2,
					ChallengeBonusActive = flag3,
					ChallengeStarMultiplier = challengeStarMultiplier,
					HasChallengeBait = flag,
					NativeDifficulty = nativeDifficulty,
					PassedDifficulty = fishDifficulty,
					FishQuality = fishQuality,
					TreasureCaught = treasureCaught,
					WasPerfect = wasPerfect,
					IsBossFish = isBossFish
				};
				FishingLog.Log($"[FishingRod] 钓鱼成功（动画阶段）| 玩家: {lastFarmerToUse.UniqueMultiplayerID} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 数量倍数: {quantityMultiplier} | 脱杆次数: {num} | 动画显示: {numCaught}条 | 尺寸(原生→结算): {fishSize} → {num4} | 挑战鱼饵: {flag} | 万能加成: {flag2} | 挑战加成(5分钟): {flag3} | 耗时: {num3:F0}s", (LogLevel)2);
			}
			catch (Exception value)
			{
				FishingLog.Log($"pullFishFromWater Prefix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("CreateFish")]
		[HarmonyPostfix]
		public static void CreateFish_Postfix(FishingRod __instance, ref Item __result)
		{
			try
			{
				if (FestivalFishingService.IsVanillaFestivalMode())
				{
					return;
				}
				Farmer lastFarmerToUse = ((Tool)__instance).getLastFarmerToUse();
				if (lastFarmerToUse == null || !lastFarmerToUse.IsLocalPlayer || __result == null)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(__result.QualifiedItemId);
				string pendingKey = GetPendingKey(lastFarmerToUse, text);
				if (!_pendingFish.TryGetValue(pendingKey, out var value))
				{
					return;
				}
				bool flag = value.CreateFishCalls == 0 || value.AllowAdditionalCreateFish;
				value.CreateFishCalls++;
				value.AllowAdditionalCreateFish = false;
				if (!flag)
				{
					return;
				}
				if (value.WildBaitBonus || value.ChallengeBonusActive || value.ChallengeStarMultiplier < 1f || value.Multiplier > 1)
				{
					int num = Math.Max(1, __result.Stack);
					long num2 = num;
					if (value.WildBaitBonus)
					{
						num2 = num + 10;
					}
					else if (value.ChallengeBonusActive)
					{
						num2 = (long)Math.Ceiling((double)num * 1.5);
					}
					if (value.Multiplier > 1)
					{
						num2 *= value.Multiplier;
					}
					if (value.ChallengeStarMultiplier < 1f)
					{
						num2 = DifficultyCalculator.ApplyChallengeStarMultiplier(num2, value.ChallengeStarMultiplier);
					}
					__result.Stack = (int)Math.Min(2147483647L, num2);
					FishingLog.Log($"[FishingRod] 数量转化完成 | 玩家: {lastFarmerToUse.UniqueMultiplayerID} | 鱼ID: {text} | 原生堆叠: {num} → 最终: {__result.Stack} | 万能加成: {value.WildBaitBonus} | 挑战加成: {value.ChallengeBonusActive} | 等级倍数: ×{value.Multiplier}", (LogLevel)2);
				}
				if (!DifficultyManager.IsDailyHarvestLimited(text))
				{
					return;
				}
				int num3 = DifficultyManager.ConsumeDailyHarvest(text, lastFarmerToUse);
				if (num3 > 333)
				{
					value.HarvestLimited = true;
					__result.Stack = 0;
					if (DifficultyManager.MarkDailyLimitNotified(text, lastFarmerToUse))
					{
						HUDNotifier.ShowDailyLimitReached(text);
					}
					FishingLog.Log($"[FishingRod] 每日收获限额 | 玩家: {lastFarmerToUse.UniqueMultiplayerID} | 鱼ID: {text} | 当日: {num3 - 1}/{333} → 本次数量清零", (LogLevel)2);
				}
			}
			catch (Exception value2)
			{
				FishingLog.Log($"FishingRod.CreateFish Postfix 失败: {value2}", (LogLevel)4);
			}
		}

		internal static bool TryGetPending(Farmer owner, string fishId, out PendingFishData data)
		{
			return _pendingFish.TryGetValue(GetPendingKey(owner, fishId), out data);
		}

		internal static bool TryBeginExperienceAdjustment(Farmer owner, out PendingFishData data)
		{
			data = null;
			if (owner == null)
			{
				return false;
			}
			string value = owner.UniqueMultiplayerID + ":";
			foreach (KeyValuePair<string, PendingFishData> item in _pendingFish)
			{
				if (item.Key.StartsWith(value, StringComparison.Ordinal) && !item.Value.ExperienceAdjusted)
				{
					item.Value.ExperienceAdjusted = true;
					data = item.Value;
					return true;
				}
			}
			return false;
		}

		internal static void ClearPending()
		{
			_pendingFish.Clear();
		}

		internal static void RemovePending(Farmer owner, string fishId)
		{
			if (owner != null && !string.IsNullOrEmpty(fishId))
			{
				_pendingFish.Remove(GetPendingKey(owner, fishId));
			}
		}

		private static void ClearPending(FishingRod rod)
		{
			Farmer lastFarmerToUse = ((Tool)rod).getLastFarmerToUse();
			ItemMetadata whichFish = rod.whichFish;
			string text = ((whichFish != null) ? whichFish.QualifiedItemId : null);
			if (lastFarmerToUse != null && !string.IsNullOrEmpty(text))
			{
				_pendingFish.Remove(GetPendingKey(lastFarmerToUse, text));
			}
		}

		private static void PrepareAdditionalFishCreation(FishingRod rod, int remainingFish)
		{
			Farmer lastFarmerToUse = ((Tool)rod).getLastFarmerToUse();
			ItemMetadata whichFish = rod.whichFish;
			string text = ((whichFish != null) ? whichFish.QualifiedItemId : null);
			if (lastFarmerToUse != null && !string.IsNullOrEmpty(text) && _pendingFish.TryGetValue(GetPendingKey(lastFarmerToUse, text), out var value))
			{
				value.AllowAdditionalCreateFish = remainingFish == 1;
			}
		}

		[HarmonyPatch("doneHoldingFish")]
		[HarmonyPostfix]
		public static void DoneHoldingFish_Postfix(FishingRod __instance, bool ___treasureCaught, bool ___gotTroutDerbyTag)
		{
			try
			{
				if (!___treasureCaught && !___gotTroutDerbyTag)
				{
					ClearPending(__instance);
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"FishingRod.doneHoldingFish Postfix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("openTreasureMenuEndFunction")]
		[HarmonyPrefix]
		public static void OpenTreasureMenuEndFunction_Prefix(FishingRod __instance, int remainingFish)
		{
			try
			{
				PrepareAdditionalFishCreation(__instance, remainingFish);
			}
			catch (Exception value)
			{
				FishingLog.Log($"FishingRod.openTreasureMenuEndFunction Prefix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("openTreasureMenuEndFunction")]
		[HarmonyPostfix]
		public static void OpenTreasureMenuEndFunction_Postfix(FishingRod __instance, int remainingFish)
		{
			try
			{
				ClearPending(__instance);
			}
			catch (Exception value)
			{
				FishingLog.Log($"FishingRod.openTreasureMenuEndFunction Postfix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("justGotDerbyTagEndFunction")]
		[HarmonyPrefix]
		public static void JustGotDerbyTagEndFunction_Prefix(FishingRod __instance, int remainingFish)
		{
			try
			{
				PrepareAdditionalFishCreation(__instance, remainingFish);
			}
			catch (Exception value)
			{
				FishingLog.Log($"FishingRod.justGotDerbyTagEndFunction Prefix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("justGotDerbyTagEndFunction")]
		[HarmonyPostfix]
		public static void JustGotDerbyTagEndFunction_Postfix(FishingRod __instance, int remainingFish)
		{
			try
			{
				ClearPending(__instance);
			}
			catch (Exception value)
			{
				FishingLog.Log($"FishingRod.justGotDerbyTagEndFunction Postfix 失败: {value}", (LogLevel)4);
			}
		}

		private static string GetPendingKey(Farmer owner, string fishId)
		{
			return $"{owner.UniqueMultiplayerID}:{SpecialFishHelper.NormalizeItemId(fishId)}";
		}
	}
	[HarmonyPatch(typeof(Farmer))]
	internal class FarmerFishingPatches
	{
		[HarmonyPatch("caughtFish")]
		[HarmonyPrefix]
		public static void CaughtFish_Prefix(Farmer __instance, string itemId, ref int size, bool from_fish_pond)
		{
			try
			{
				if (!FestivalFishingService.IsVanillaFestivalMode() && !from_fish_pond && __instance.IsLocalPlayer && size > 0)
				{
					string text = SpecialFishHelper.NormalizeItemId(itemId);
					if (FishingRodPatches.TryGetPending(__instance, text, out var data) && data.FishSize > 0 && data.DifficultyLevel != 0 && data.FishSize != size)
					{
						FishingLog.Log($"[Farmer] 收藏尺寸使用结算值 | 鱼ID: {text} | {size} → {data.FishSize}", (LogLevel)1);
						size = data.FishSize;
					}
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"caughtFish Prefix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("caughtFish")]
		[HarmonyPostfix]
		public static void CaughtFish_Postfix(Farmer __instance, string itemId, int size, bool from_fish_pond, int numberCaught)
		{
			try
			{
				if (FestivalFishingService.IsVanillaFestivalMode() || from_fish_pond || !__instance.IsLocalPlayer)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(itemId);
				if (!FishingRodPatches.TryGetPending(__instance, text, out var data) || data.SuccessRecorded)
				{
					return;
				}
				data.SuccessRecorded = true;
				if (FestivalFishingService.IsSquidFest() && text == "(O)151" && data.Multiplier > 1)
				{
					int num = numberCaught * (data.Multiplier - 1);
					if (num > 0)
					{
						Game1.stats.Increment(StatKeys.SquidFestScore(Game1.dayOfMonth, Game1.year), num);
						FishingLog.Log($"[节日] 鱿鱼节分数补差 | 玩家: {__instance.UniqueMultiplayerID} | 数量倍数: {data.Multiplier} | 原生数量: {numberCaught} | 补分: +{num}", (LogLevel)2);
					}
				}
				int num2 = data.MissCount switch
				{
					0 => 10, 
					1 => 5, 
					2 => 2, 
					_ => 1, 
				};
				int value = ((NetFieldBase<int, NetInt>)(object)__instance.fishingLevel).Value;
				int requestedLevelGain = DifficultyCalculator.GetRequestedLevelGain(num2, data.AdjustedDifficulty, value);
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, __instance);
				int value2 = DifficultyManager.RecordSuccess(text, requestedLevelGain, __instance);
				int difficultyLevel2 = DifficultyManager.GetDifficultyLevel(text, __instance);
				DifficultyManager.RecordHighDifficulty(text, data.AdjustedDifficulty, __instance);
				if (data.DifficultyLevel >= 95 && data.HasChallengeBait)
				{
					DifficultyManager.RecordChallengeCrown(text, __instance, data.DifficultyLevel >= 100);
				}
				if (data.HasChallengeBait)
				{
					DifficultyManager.ClearChallengePatternSeed(text, data.DifficultyLevel, __instance);
				}
				if (data.DifficultyLevel >= 8)
				{
					int fishSize = ((data.FishSize > 0) ? data.FishSize : Math.Max(20, data.OriginalNum * 10));
					GiantFishManager.RecordGiantFish(__instance, text, data.DifficultyLevel, fishSize);
				}
				FishingLog.Log($"[Farmer] 难度等级更新 | 鱼ID: {text} | 脱杆: {data.MissCount}次 | 基础增长: +{num2} | 额外难度增益: +{(int)Math.Round(data.AdjustedDifficulty / 50f)} | 钓鱼等级系数: ×{DifficultyCalculator.GetFishingLevelGainFactor(value):F1} | 请求增长: +{requestedLevelGain} | 实际增长: +{value2} | {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
				HUDNotifier.ShowSuccessNotification(text, difficultyLevel2, difficultyLevel);
				if (difficultyLevel >= 50 && DifficultyCalculator.TryGetStarfruitTeaDrop(difficultyLevel))
				{
					Item val = ItemRegistry.Create("(O)StardropTea", 1, 0, false);
					__instance.addItemByMenuIfNecessary(val, (behaviorOnItemSelect)null, false);
					HUDNotifier.ShowStarfruitTeaNotification(text);
					FishingLog.Log($"[Farmer] 星之果茶掉落 | 鱼ID: {text} | 难度等级: {difficultyLevel} | 概率: {(double)difficultyLevel / 400.0:P1}", (LogLevel)2);
				}
			}
			catch (Exception value3)
			{
				FishingLog.Log($"caughtFish Postfix 失败: {value3}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(Event))]
	internal static class EventFestivalPatches
	{
		[HarmonyPatch("caughtFish")]
		[HarmonyPostfix]
		public static void CaughtFish_Postfix(Event __instance, string itemId, int size, Farmer who)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (who == null || !who.IsLocalPlayer || !__instance.isSpecificFestival("winter8") || !ModEntry.Config.EnableFestivalFishingMods || size <= 0 || ((Character)who).TilePoint.X >= 79 || ((Character)who).TilePoint.Y >= 43)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(itemId);
				if (FishingRodPatches.TryGetPending(who, text, out var data))
				{
					int num = data.Multiplier - 1;
					if (num > 0)
					{
						who.festivalScore += num;
						FishingLog.Log($"[节日] 冰雪节分数补差 | 玩家: {who.UniqueMultiplayerID} | 鱼ID: {text} | 数量倍数: {data.Multiplier} | 补分: +{num} | 总分: {who.festivalScore}", (LogLevel)2);
					}
					FishingRodPatches.RemovePending(who, text);
				}
			}
			catch (Exception value)
			{
				FishingLog.Log($"Event.caughtFish Postfix 失败: {value}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(NPC))]
	internal static class NPCPatches
	{
		[HarmonyPatch("checkAction")]
		[HarmonyPrefix]
		public static bool CheckAction_Prefix(NPC __instance, Farmer who, GameLocation l, ref bool __result)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
			try
			{
				if (who == null || !who.IsLocalPlayer)
				{
					return true;
				}
				string text = GiantFishManager.TryGetReplacementDialogue(__instance, who);
				if (string.IsNullOrEmpty(text))
				{
					return true;
				}
				__instance.CurrentDialogue.Clear();
				__instance.CurrentDialogue.Push(new Dialogue(__instance, "FishingExpanded.GiantFishPraise", text));
				Game1.drawDialogue(__instance);
				__result = true;
				return false;
			}
			catch (Exception value)
			{
				FishingLog.Log($"NPC.checkAction 巨型鱼对话替换失败: {value}", (LogLevel)4);
				return true;
			}
		}
	}
	[HarmonyPatch(typeof(Object))]
	internal class ObjectPatches
	{
		private static bool _transpilerLogged;

		[HarmonyPatch("drawWhenHeld")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawWhenHeld_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = instructions.ToList();
			FishingLog.Log($"[ObjectPatches] 开始分析 Object.drawWhenHeld() | 总指令数: {codes.Count}", (LogLevel)1);
			MethodInfo getScaleMethod = AccessTools.Method(typeof(ObjectPatches), "GetDrawScale", (Type[])null, (Type[])null);
			int patchCount = 0;
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float num && Math.Abs(num - 4f) < 0.01f)
				{
					yield return codes[i];
					yield return new CodeInstruction(OpCodes.Ldarg_0, (object)null);
					yield return new CodeInstruction(OpCodes.Ldarg_3, (object)null);
					yield return new CodeInstruction(OpCodes.Call, (object)getScaleMethod);
					yield return new CodeInstruction(OpCodes.Mul, (object)null);
					patchCount++;
					if (!_transpilerLogged)
					{
						FishingLog.Log($"[ObjectPatches] ✓ 成功插入视觉缩放逻辑到 Object.drawWhenHeld() | 位置: IL_{i:X4}", (LogLevel)2);
						_transpilerLogged = true;
					}
				}
				else
				{
					yield return codes[i];
				}
			}
			if (patchCount == 0)
			{
				FishingLog.Log("[ObjectPatches] ⚠ 未找到 ldc.r4 4 指令，视觉缩放可能无效", (LogLevel)3);
				yield break;
			}
			FishingLog.Log($"[ObjectPatches] ✓ 共修改了 {patchCount} 处 scale 参数", (LogLevel)2);
		}

		[HarmonyPatch("drawWhenHeld")]
		[HarmonyPrefix]
		public static void DrawWhenHeld_Prefix(Object __instance, Farmer f, ref Vector2 objectPosition)
		{
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string text = ((__instance != null) ? ((Item)__instance).QualifiedItemId : null) ?? "<null>";
				bool flag = ((__instance != null) ? new int?(((Item)__instance).Category) : ((int?)null)) == -4;
				if (__instance != null && f != null && flag)
				{
					float fishVisualScale = GiantFishManager.GetFishVisualScale(text, f);
					if (!(fishVisualScale <= 1.001f))
					{
						ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(text);
						Rectangle sourceRect = dataOrErrorItem.GetSourceRect(0, (int?)((Item)__instance).ParentSheetIndex);
						objectPosition += new Vector2((float)sourceRect.Width * 4f * (1f - fishVisualScale) / 2f, (float)sourceRect.Height * 4f * (1f - fishVisualScale));
					}
				}
			}
			catch (Exception value)
			{
				FishingLog.LogRateLimited("ObjectPatches.DrawWhenHeld_Prefix", $"[ObjectPatches] drawWhenHeld 处理失败: {value}", (LogLevel)3);
			}
		}

		public static float GetDrawScale(Object obj, Farmer owner)
		{
			try
			{
				string fishId = ((obj != null) ? ((Item)obj).QualifiedItemId : null) ?? "<null>";
				bool flag = ((obj != null) ? new int?(((Item)obj).Category) : ((int?)null)) == -4;
				float result = 1f;
				if (obj != null && flag)
				{
					result = GiantFishManager.GetFishVisualScale(fishId, owner);
				}
				return result;
			}
			catch (Exception value)
			{
				FishingLog.LogRateLimited("ObjectPatches.GetDrawScale", $"[ObjectPatches] GetDrawScale 失败: {value}", (LogLevel)4);
				return 1f;
			}
		}
	}
}
namespace FishingExpanded.Data
{
	public class FishDifficultyData
	{
		public Dictionary<string, FishStats> FishStatistics { get; set; } = new Dictionary<string, FishStats>();

		public HashSet<string> CollectionStars { get; set; } = new HashSet<string>();

		public HashSet<string> ChallengeCrowns { get; set; } = new HashSet<string>();

		public HashSet<string> Level100FlowCrowns { get; set; } = new HashSet<string>();

		public Dictionary<string, int> ChallengePatternSeeds { get; set; } = new Dictionary<string, int>();
	}
	public class FishStats
	{
		public int SuccessCount { get; set; }

		public int FailCount { get; set; }

		public int ConsecutiveFailCount { get; set; }

		public int DifficultyLevel
		{
			get
			{
				try
				{
					long val = (long)SuccessCount - (long)FailCount;
					return (int)Math.Max(-10L, Math.Min(100L, val));
				}
				catch
				{
					return 0;
				}
			}
		}
	}
	public class FishDisplayData
	{
		public Dictionary<string, (int level, int fishSize)> ActiveGiantFish { get; set; } = new Dictionary<string, (int, int)>();

		public Dictionary<string, HashSet<string>> NPCBubbleTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

		public Dictionary<string, HashSet<string>> NPCDialogueTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

		public void ResetDailyTriggers()
		{
			NPCBubbleTriggered.Clear();
			NPCDialogueTriggered.Clear();
		}

		public void ClearActiveGiantFish()
		{
			ActiveGiantFish.Clear();
		}
	}
}
