using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;
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
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Tools;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("FishingExpanded")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.5.10.0")]
[assembly: AssemblyInformationalVersion("0.5.10+8f799da51c286626a1bae0669d186f3228f23b4a")]
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
	public class ModEntry : Mod
	{
		private int _npcCheckCounter;

		private int _cleanupCounter;

		public static ModEntry Instance { get; private set; }

		public static IModHelper ModHelper => ((Mod)Instance).Helper;

		public static IMonitor ModMonitor => ((Mod)Instance).Monitor;

		public override void Entry(IModHelper helper)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			Instance = this;
			((Mod)this).Monitor.Log("FishingExpanded 正在初始化...", (LogLevel)2);
			try
			{
				Harmony val = new Harmony(((Mod)this).ModManifest.UniqueID);
				val.PatchAll();
				((Mod)this).Monitor.Log("Harmony Patches 注册成功", (LogLevel)1);
				helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
				helper.Events.GameLoop.Saving += OnSaving;
				helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
				helper.Events.GameLoop.DayStarted += OnDayStarted;
				helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
				helper.Events.Player.Warped += OnWarped;
				RegisterConsoleCommands();
				((Mod)this).Monitor.Log("FishingExpanded 初始化完成", (LogLevel)2);
			}
			catch (Exception value)
			{
				((Mod)this).Monitor.Log($"初始化失败: {value}", (LogLevel)4);
			}
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			DifficultyManager.LoadData(Game1.player);
			GiantFishManager.ResetForSave();
			ObjectPatches.ResetVisualDiagnostics();
			((Mod)this).Monitor.Log("=== FishingExpanded 存档加载完成 ===", (LogLevel)2);
		}

		private void OnSaving(object sender, SavingEventArgs e)
		{
			DifficultyManager.SaveAll();
			((Mod)this).Monitor.Log("已保存钓鱼难度数据", (LogLevel)1);
		}

		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			DifficultyManager.UnloadData();
			FishingRodPatches.ClearPending();
			GiantFishManager.ResetForSave();
			ObjectPatches.ResetVisualDiagnostics();
			((Mod)this).Monitor.Log("已清除 FishingExpanded 当前玩家缓存", (LogLevel)1);
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			((Mod)this).Monitor.Log($"=== FishingExpanded 新的一天开始 (Day {Game1.dayOfMonth}) ===", (LogLevel)2);
			GiantFishManager.OnDayStarted();
		}

		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
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
		}

		private void OnWarped(object sender, WarpedEventArgs e)
		{
			if (e.NewLocation is FarmHouse)
			{
				IMonitor monitor = ((Mod)this).Monitor;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
				defaultInterpolatedStringHandler.AppendLiteral("[ModEntry] 玩家进入FarmHouse: ");
				Farmer player = e.Player;
				defaultInterpolatedStringHandler.AppendFormatted((player != null) ? new long?(player.UniqueMultiplayerID) : ((long?)null));
				defaultInterpolatedStringHandler.AppendLiteral(" | 地点: ");
				defaultInterpolatedStringHandler.AppendFormatted(e.NewLocation.Name);
				monitor.Log(defaultInterpolatedStringHandler.ToStringAndClear(), (LogLevel)1);
				GiantFishManager.OnEnterFarmHouse(e.Player);
			}
		}

		private void RegisterConsoleCommands()
		{
			((Mod)this).Helper.ConsoleCommands.Add("fish_setlevel", "设置某鱼的难度等级 | 用法: fish_setlevel <鱼ID> <等级>", (Action<string, string[]>)OnCommandSetLevel);
			((Mod)this).Helper.ConsoleCommands.Add("fish_addsuccess", "增加成功次数 | 用法: fish_addsuccess <鱼ID> <次数>", (Action<string, string[]>)OnCommandAddSuccess);
			((Mod)this).Helper.ConsoleCommands.Add("fish_addfail", "增加失败次数 | 用法: fish_addfail <鱼ID> <次数>", (Action<string, string[]>)OnCommandAddFail);
			((Mod)this).Helper.ConsoleCommands.Add("fish_info", "查看鱼的详细信息 | 用法: fish_info <鱼ID>", (Action<string, string[]>)OnCommandInfo);
			((Mod)this).Helper.ConsoleCommands.Add("fish_list", "列出所有已记录的鱼", (Action<string, string[]>)OnCommandList);
			((Mod)this).Helper.ConsoleCommands.Add("fish_clear", "清空所有钓鱼数据 | 用法: fish_clear confirm", (Action<string, string[]>)OnCommandClear);
			((Mod)this).Helper.ConsoleCommands.Add("fish_addstar", "添加收藏星标 | 用法: fish_addstar <鱼ID>", (Action<string, string[]>)OnCommandAddStar);
			((Mod)this).Helper.ConsoleCommands.Add("fish_giant", "模拟巨型鱼（触发NPC反应）| 用法: fish_giant <鱼ID> <倍数>", (Action<string, string[]>)OnCommandGiant);
			((Mod)this).Helper.ConsoleCommands.Add("fish_bonus", "查看当前钓鱼等级加成", (Action<string, string[]>)OnCommandBonus);
			((Mod)this).Monitor.Log("控制台命令注册完成 (输入 help 查看所有命令)", (LogLevel)1);
		}

		private void OnCommandSetLevel(string command, string[] args)
		{
			if (args.Length < 2)
			{
				((Mod)this).Monitor.Log("用法: fish_setlevel <鱼ID> <等级>", (LogLevel)2);
				((Mod)this).Monitor.Log("例如: fish_setlevel 128 50  (设置河豚难度为50)", (LogLevel)2);
				return;
			}
			string text = args[0];
			if (!int.TryParse(args[1], out var result))
			{
				((Mod)this).Monitor.Log("等级必须是数字！", (LogLevel)4);
				return;
			}
			DifficultyManager.SetDifficultyLevel(text, result, Game1.player);
			int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, Game1.player);
			((Mod)this).Monitor.Log($"✓ 已设置 {text} 的难度等级为 {difficultyLevel}", (LogLevel)2);
		}

		private void OnCommandAddSuccess(string command, string[] args)
		{
			if (args.Length < 2)
			{
				((Mod)this).Monitor.Log("用法: fish_addsuccess <鱼ID> <次数>", (LogLevel)2);
				return;
			}
			string text = args[0];
			if (!int.TryParse(args[1], out var result) || result <= 0)
			{
				((Mod)this).Monitor.Log("次数必须是正整数！", (LogLevel)4);
				return;
			}
			for (int i = 0; i < result; i++)
			{
				DifficultyManager.RecordSuccess(text, 1, Game1.player);
			}
			int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, Game1.player);
			((Mod)this).Monitor.Log($"✓ 已为 {text} 增加 {result} 次成功，当前难度等级: {difficultyLevel}", (LogLevel)2);
		}

		private void OnCommandAddFail(string command, string[] args)
		{
			if (args.Length < 2)
			{
				((Mod)this).Monitor.Log("用法: fish_addfail <鱼ID> <次数>", (LogLevel)2);
				return;
			}
			string text = args[0];
			if (!int.TryParse(args[1], out var result) || result <= 0)
			{
				((Mod)this).Monitor.Log("次数必须是正整数！", (LogLevel)4);
				return;
			}
			for (int i = 0; i < result; i++)
			{
				DifficultyManager.RecordFailure(text, Game1.player);
			}
			int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, Game1.player);
			((Mod)this).Monitor.Log($"✓ 已为 {text} 增加 {result} 次失败，当前难度等级: {difficultyLevel}", (LogLevel)2);
		}

		private void OnCommandInfo(string command, string[] args)
		{
			if (args.Length < 1)
			{
				((Mod)this).Monitor.Log("用法: fish_info <鱼ID>", (LogLevel)2);
				return;
			}
			string text = args[0];
			FishStats fishStats = DifficultyManager.GetFishStats(text, Game1.player);
			if (fishStats == null)
			{
				((Mod)this).Monitor.Log("鱼 " + text + " 尚未记录任何数据", (LogLevel)2);
				return;
			}
			int difficultyLevel = fishStats.DifficultyLevel;
			bool flag = DifficultyManager.HasCollectionStar(text, Game1.player);
			((Mod)this).Monitor.Log("===========================================", (LogLevel)2);
			((Mod)this).Monitor.Log("鱼ID: " + text, (LogLevel)2);
			((Mod)this).Monitor.Log($"成功次数: {fishStats.SuccessCount}", (LogLevel)2);
			((Mod)this).Monitor.Log($"失败次数: {fishStats.FailCount}", (LogLevel)2);
			((Mod)this).Monitor.Log($"难度等级: {difficultyLevel} ({GetRankName(difficultyLevel)})", (LogLevel)2);
			((Mod)this).Monitor.Log($"难度倍数: {DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel):F2}x", (LogLevel)2);
			((Mod)this).Monitor.Log($"数量倍数: {DifficultyCalculator.GetQuantityMultiplier(difficultyLevel)}x", (LogLevel)2);
			((Mod)this).Monitor.Log($"品质加成: +{DifficultyCalculator.GetQualityBonus(difficultyLevel)}", (LogLevel)2);
			((Mod)this).Monitor.Log("收藏星标: " + (flag ? "★ 已获得" : "未获得"), (LogLevel)2);
			((Mod)this).Monitor.Log("===========================================", (LogLevel)2);
		}

		private void OnCommandList(string command, string[] args)
		{
			Dictionary<string, FishStats> allFishStats = DifficultyManager.GetAllFishStats(Game1.player);
			if (allFishStats.Count == 0)
			{
				((Mod)this).Monitor.Log("当前没有任何钓鱼记录", (LogLevel)2);
				return;
			}
			((Mod)this).Monitor.Log($"===== 已记录的鱼种（共 {allFishStats.Count} 种）=====", (LogLevel)2);
			foreach (KeyValuePair<string, FishStats> item in allFishStats)
			{
				string key = item.Key;
				FishStats value = item.Value;
				int difficultyLevel = value.DifficultyLevel;
				string value2 = (DifficultyManager.HasCollectionStar(key, Game1.player) ? " ★" : "");
				((Mod)this).Monitor.Log($"  {key}: 等级={difficultyLevel} ({GetRankName(difficultyLevel)}), 成功={value.SuccessCount}, 失败={value.FailCount}{value2}", (LogLevel)2);
			}
			((Mod)this).Monitor.Log("===========================================", (LogLevel)2);
		}

		private void OnCommandClear(string command, string[] args)
		{
			if (args.Length < 1 || args[0] != "confirm")
			{
				((Mod)this).Monitor.Log("⚠ 此操作将清空所有钓鱼数据！", (LogLevel)3);
				((Mod)this).Monitor.Log("如需确认，请输入: fish_clear confirm", (LogLevel)2);
			}
			else
			{
				DifficultyManager.ClearAllData(Game1.player);
				((Mod)this).Monitor.Log("✓ 已清空所有钓鱼数据", (LogLevel)2);
			}
		}

		private void OnCommandAddStar(string command, string[] args)
		{
			if (args.Length < 1)
			{
				((Mod)this).Monitor.Log("用法: fish_addstar <鱼ID>", (LogLevel)2);
				return;
			}
			string text = args[0];
			DifficultyManager.RecordHighDifficulty(text, Game1.player);
			((Mod)this).Monitor.Log("✓ 已为 " + text + " 添加收藏星标并增加 +0.5 钓鱼等级", (LogLevel)2);
		}

		private void OnCommandGiant(string command, string[] args)
		{
			if (args.Length < 2)
			{
				((Mod)this).Monitor.Log("用法: fish_giant <鱼ID> <倍数>", (LogLevel)2);
				((Mod)this).Monitor.Log("例如: fish_giant 128 20  (模拟20倍河豚)", (LogLevel)2);
				return;
			}
			string text = args[0];
			if (!int.TryParse(args[1], out var result) || result <= 0)
			{
				((Mod)this).Monitor.Log("倍数必须是正整数！", (LogLevel)4);
				return;
			}
			GiantFishManager.RecordGiantFish(text, result, result * 20);
			((Mod)this).Monitor.Log($"✓ 已记录巨型鱼 {text} (倍数={result})，等待触发NPC反应", (LogLevel)2);
			((Mod)this).Monitor.Log("提示: 将鱼拿在手上并靠近NPC即可触发反应", (LogLevel)2);
		}

		private void OnCommandBonus(string command, string[] args)
		{
			float fishingLevelBonus = DifficultyManager.GetFishingLevelBonus(Game1.player);
			int value = (int)Math.Floor(fishingLevelBonus);
			int collectionStarCount = DifficultyManager.GetCollectionStarCount(Game1.player);
			((Mod)this).Monitor.Log("===========================================", (LogLevel)2);
			((Mod)this).Monitor.Log($"收藏星标数量: {collectionStarCount}", (LogLevel)2);
			((Mod)this).Monitor.Log($"钓鱼等级加成: +{fishingLevelBonus:F1} (隐藏等级: +{value})", (LogLevel)2);
			((Mod)this).Monitor.Log("===========================================", (LogLevel)2);
		}

		private string GetRankName(int level)
		{
			return Translation.op_Implicit(ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
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
			["rank.divineking"] = "hud.starChallenge.honor.divineking"
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
				return Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.starChallenge.template", (object)new
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
				ModEntry.ModMonitor.Log("[ChallengeDialogue] 生成挑战宣言失败: " + ex.Message, (LogLevel)4);
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
		public static float GetDifficultyMultiplier(int level)
		{
			if (level < 0)
			{
				return Lerp(0.5f, 1f, (float)(level + 10) / 10f);
			}
			return Lerp(1f, 50f, (float)level / 100f);
		}

		public static int GetQuantityMultiplier(int level)
		{
			if (level <= 0)
			{
				return 1;
			}
			return (int)Math.Round(Lerp(1f, 200f, (float)level / 100f));
		}

		public static int GetExperienceMultiplier(int level)
		{
			return GetQuantityMultiplier(level);
		}

		public static int GetQualityBonus(int level)
		{
			if (level <= 0)
			{
				return 0;
			}
			return level / 5;
		}

		public static int ApplyQualityBonus(int baseQuality, int level)
		{
			int qualityBonus = GetQualityBonus(level);
			int num = baseQuality + qualityBonus;
			if (num >= 3)
			{
				return 4;
			}
			return Math.Min(num, 2);
		}

		public static float GetVisualScale(int quantityMultiplier)
		{
			if (quantityMultiplier <= 1)
			{
				return 1f;
			}
			return 1f + (float)(quantityMultiplier - 1) * 0.0136102f;
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
			_ = 88;
			return "rank.divineking";
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
			_ = 88;
			return 100;
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
				if (Game1.random == null)
				{
					return string.Format(PraiseTemplates[0], fishName, fishSize);
				}
				int num = Game1.random.Next(PraiseTemplates.Length);
				return string.Format(PraiseTemplates[num], fishName, fishSize);
			}
			catch (Exception ex)
			{
				ModEntry.ModMonitor.Log("[NPCDialogue] 生成对话失败 | 错误: " + ex.Message, (LogLevel)4);
				return "哇！这条" + fishName + "真大！";
			}
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
			int num = Game1.random.Next(LegendaryMessages.Length);
			return LegendaryMessages[num];
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

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		private static readonly Dictionary<long, FishDifficultyData> _dataByPlayer = new Dictionary<long, FishDifficultyData>();

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
						ModEntry.ModMonitor.Log("[DifficultyManager] 已将旧存档级鱼数据迁移到主玩家 modData；联机玩家不会共享这份旧数据", (LogLevel)3);
					}
				}
				catch (Exception ex)
				{
					ModEntry.ModMonitor.Log("[DifficultyManager] 读取旧存档级数据失败，将使用空数据 | 错误: " + ex.Message, (LogLevel)4);
				}
			}
			if (fishDifficultyData == null)
			{
				fishDifficultyData = new FishDifficultyData();
			}
			NormalizeData(fishDifficultyData);
			_dataByPlayer[player.UniqueMultiplayerID] = fishDifficultyData;
			ModEntry.ModMonitor.Log($"[DifficultyManager] 加载玩家数据完成 | 玩家: {player.UniqueMultiplayerID} | 鱼种类数: {fishDifficultyData.FishStatistics.Count} | 钓鱼等级加成: +{fishDifficultyData.FishingLevelBonus:F1} | 星标鱼种: {fishDifficultyData.CollectionStars.Count}", (LogLevel)2);
		}

		public static void SaveData(Farmer player)
		{
			if (player != null && _dataByPlayer.TryGetValue(player.UniqueMultiplayerID, out var value))
			{
				NormalizeData(value);
				((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Character)player).modData)["FishingExpanded/FishDifficultyData"] = JsonSerializer.Serialize(value, JsonOptions);
				ModEntry.ModMonitor.Log($"已保存玩家钓鱼难度数据 | 玩家: {player.UniqueMultiplayerID}", (LogLevel)1);
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
			try
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(text);
				return (dataOrErrorItem != null && SpecialFishHelper.IsNonFish(dataOrErrorItem.Category)) ? SpecialFishHelper.GetMaxLevelForNonFish() : 100;
			}
			catch
			{
				return 100;
			}
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
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录成功", (LogLevel)4);
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
				ModEntry.ModMonitor.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
			}
			int difficultyLevel = GetDifficultyLevel(text, player);
			int maxDifficultyLevel = GetMaxDifficultyLevel(text);
			int num = Math.Min(DifficultyCalculator.GetNextRankCeiling(difficultyLevel), maxDifficultyLevel);
			int num2 = Math.Max(0, Math.Min(levelGain, num - difficultyLevel));
			value.SuccessCount = SaturatingAdd(value.SuccessCount, num2);
			int difficultyLevel2 = GetDifficultyLevel(text, player);
			ModEntry.ModMonitor.Log($"[DifficultyManager] 钓鱼成功记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 请求增长: +{levelGain} | 实际增长: +{num2} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
			SaveData(player);
			return num2;
		}

		public static void RecordFailure(string fishId, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录失败", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
			{
				if (!data.FishStatistics.TryGetValue(text, out var value))
				{
					value = new FishStats();
					data.FishStatistics[text] = value;
					ModEntry.ModMonitor.Log("[DifficultyManager] 新鱼种记录创建: " + text, (LogLevel)1);
				}
				int difficultyLevel = GetDifficultyLevel(text, player);
				value.FailCount = SaturatingAdd(value.FailCount, 1);
				int difficultyLevel2 = GetDifficultyLevel(text, player);
				ModEntry.ModMonitor.Log($"[DifficultyManager] 钓鱼失败记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount} | 等级变化: {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
				SaveData(player);
			}
		}

		public static void RecordHighDifficulty(string fishId, float adjustedDifficulty, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null，无法记录高难度", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text) && adjustedDifficulty >= 120f && !data.CollectionStars.Contains(text))
			{
				data.CollectionStars.Add(text);
				data.FishingLevelBonus = (float)data.CollectionStars.Count * 0.5f;
				ModEntry.ModMonitor.Log($"[DifficultyManager] ★ 达成高难度里程碑 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 调整后难度: {adjustedDifficulty:F1} | 新增钓鱼等级加成: +0.5 | 总加成: +{data.FishingLevelBonus:F1}", (LogLevel)3);
				SaveData(player);
			}
		}

		public static float GetFishingLevelBonus(Farmer player)
		{
			return GetData(player)?.FishingLevelBonus ?? 0f;
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

		public static void SetDifficultyLevel(string fishId, int targetLevel, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"[DifficultyManager] 已设置等级 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 目标等级: {targetLevel} | 成功次数: {value.SuccessCount} | 失败次数: {value.FailCount}", (LogLevel)2);
				SaveData(player);
			}
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
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
				return;
			}
			data.FishStatistics.Clear();
			data.CollectionStars.Clear();
			data.FishingLevelBonus = 0f;
			ModEntry.ModMonitor.Log("[DifficultyManager] 已清空所有数据", (LogLevel)2);
			SaveData(player);
		}

		public static void RecordHighDifficulty(string fishId, Farmer player)
		{
			FishDifficultyData data = GetData(player);
			if (data == null)
			{
				ModEntry.ModMonitor.Log("[DifficultyManager] ERROR: 玩家数据为null", (LogLevel)4);
				return;
			}
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			if (!string.IsNullOrEmpty(text) && !SpecialFishHelper.IsLegendaryFish(text))
			{
				if (!data.CollectionStars.Contains(text))
				{
					data.CollectionStars.Add(text);
					data.FishingLevelBonus = (float)data.CollectionStars.Count * 0.5f;
					ModEntry.ModMonitor.Log($"[DifficultyManager] ★ 添加收藏星标 ★ | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 新增钓鱼等级加成: +0.5 | 总加成: +{data.FishingLevelBonus:F1}", (LogLevel)3);
					SaveData(player);
				}
				else
				{
					ModEntry.ModMonitor.Log("[DifficultyManager] 鱼 " + text + " 已有收藏星标", (LogLevel)2);
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
					ModEntry.ModMonitor.Log($"[DifficultyManager] 玩家存档数据解析失败，将使用空数据 | 玩家: {player.UniqueMultiplayerID} | 错误: {ex.Message}", (LogLevel)4);
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
				if (!string.IsNullOrEmpty(text2) && !SpecialFishHelper.IsLegendaryFish(text2))
				{
					hashSet.Add(text2);
				}
			}
			data.FishStatistics = dictionary;
			data.CollectionStars = hashSet;
			data.FishingLevelBonus = (float)hashSet.Count * 0.5f;
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

		public static void RecordGiantFish(string fishId, int multiplier, int fishSize)
		{
			RecordGiantFish(Game1.player, fishId, multiplier, fishSize);
		}

		public static void RecordGiantFish(Farmer player, string fishId, int multiplier, int fishSize)
		{
			string text = SpecialFishHelper.NormalizeItemId(fishId);
			FishDisplayData displayData = GetDisplayData(player, create: true);
			if (displayData != null && multiplier > 15 && !SpecialFishHelper.IsLegendaryFish(text) && IsFish(text))
			{
				displayData.ActiveGiantFish[text] = (multiplier, fishSize);
				ModEntry.ModMonitor.Log($"[GiantFishManager] 超大鱼记录 | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 倍数: {multiplier} | fishSize: {fishSize} | 视觉缩放: ×{DifficultyCalculator.GetVisualScale(multiplier):F2}", (LogLevel)2);
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
					ModEntry.ModMonitor.Log($"[GiantFishManager] 检测到附近NPC | 玩家: {player.UniqueMultiplayerID} | 鱼ID: {text} | 5格内NPC数量: {nearbyNPCs.Count}", (LogLevel)1);
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
			if (displayData != null)
			{
				if (!displayData.NPCBubbleTriggered.ContainsKey(((Character)npc).Name))
				{
					displayData.NPCBubbleTriggered[((Character)npc).Name] = new HashSet<string>();
				}
				if (!displayData.NPCBubbleTriggered[((Character)npc).Name].Contains(fishId))
				{
					string fishName = ItemRegistry.GetDataOrErrorItem(fishId)?.DisplayName ?? "未知鱼类";
					string text = NPCDialogueGenerator.GenerateFishPraise(fishName, fishSize);
					npc.showTextAboveHead(text, (Color?)null, 2, 3000, 0);
					displayData.NPCBubbleTriggered[((Character)npc).Name].Add(fishId);
					ModEntry.ModMonitor.Log($"[GiantFishManager] NPC冒泡触发 | NPC: {((Character)npc).Name} | 鱼ID: {fishId} | fishSize: {fishSize} | 文案: {text.Substring(0, Math.Min(30, text.Length))}...", (LogLevel)2);
				}
			}
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
				IMonitor modMonitor = ModEntry.ModMonitor;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 2);
				defaultInterpolatedStringHandler.AppendLiteral("[GiantFishManager] 进入FarmHouse | 玩家: ");
				Farmer obj = player;
				defaultInterpolatedStringHandler.AppendFormatted((obj != null) ? new long?(obj.UniqueMultiplayerID) : ((long?)null));
				defaultInterpolatedStringHandler.AppendLiteral(" | 清空超大鱼记录: ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral("种");
				modMonitor.Log(defaultInterpolatedStringHandler.ToStringAndClear(), (LogLevel)2);
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
			ModEntry.ModMonitor.Log($"[GiantFishManager] 每日重置 | 玩家数: {_displayDataByPlayer.Count} | 清空冒泡记录: {num}个NPC | 对话记录: {num2}个NPC", (LogLevel)2);
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
		public static void ShowSuccessNotification(string fishId, int newLevel, int oldLevel = -1)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected O, but got Unknown
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Expected O, but got Unknown
			if (SpecialFishHelper.IsLegendaryFish(fishId))
			{
				string randomLegendaryMessage = SpecialFishHelper.GetRandomLegendaryMessage();
				Game1.addHUDMessage(new HUDMessage(randomLegendaryMessage, 1));
				ModEntry.ModMonitor.Log("[HUDNotifier] 传奇鱼提示 | 鱼ID: " + fishId + " | 文案: " + randomLegendaryMessage, (LogLevel)2);
				return;
			}
			string fishDisplayName = GetFishDisplayName(fishId);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
			bool flag = dataOrErrorItem != null && SpecialFishHelper.IsNonFish(dataOrErrorItem.Category);
			int num = (flag ? SpecialFishHelper.GetMaxLevelForNonFish() : 100);
			string text;
			if (newLevel >= num)
			{
				text = ((!flag) ? ("你已经成为" + fishDisplayName + "中的神明，这一刻你是鱼，也是人，更是王。") : ("对于" + fishDisplayName + "而言，你已是帝王"));
			}
			else
			{
				if (newLevel <= 0)
				{
					ModEntry.ModMonitor.Log($"[HUDNotifier] 等级过低，跳过提示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {newLevel}", (LogLevel)1);
					return;
				}
				string rankKey = DifficultyCalculator.GetRankKey(newLevel);
				string rank = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
				text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.challenge.title", (object)new
				{
					fishName = fishDisplayName,
					rank = rank
				}));
			}
			Game1.addHUDMessage(new HUDMessage(text, 2));
			ModEntry.ModMonitor.Log($"[HUDNotifier] 成功提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {newLevel} | 封顶: {newLevel >= num} | 非鱼类: {flag}", (LogLevel)1);
		}

		public static void ShowStarChallengeNotification(string fishId, int difficultyLevel, Farmer player)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected O, but got Unknown
			if (!SpecialFishHelper.IsLegendaryFish(fishId) && DifficultyManager.HasCollectionStar(fishId, player))
			{
				string fishDisplayName = GetFishDisplayName(SpecialFishHelper.NormalizeItemId(fishId));
				string text = ChallengeDialogueGenerator.Generate(fishDisplayName, difficultyLevel);
				Game1.addHUDMessage(new HUDMessage(text, 2));
				ModEntry.ModMonitor.Log($"[HUDNotifier] 星标鱼挑战宣言 | 鱼: {fishDisplayName} ({fishId}) | 等级: {difficultyLevel} | 文案: {text}", (LogLevel)1);
			}
		}

		public static void ShowDifficultyRecommendation(int difficultyLevel, Farmer player)
		{
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Expected O, but got Unknown
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
					Game1.addHUDMessage(new HUDMessage(text2, 3));
					ModEntry.ModMonitor.Log($"[HUDNotifier] 钓鱼等级建议 | 玩家: {player.UniqueMultiplayerID} | 鱼等级: {difficultyLevel} | 建议等级: {text} | 当前钓鱼等级: {fishingLevel} | 不可能挑战: {num > (float)fishingLevel * 2f}", (LogLevel)1);
				}
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"钓鱼等级建议提示失败: {value}", (LogLevel)4);
			}
		}

		public static void ShowFailureNotification(string fishId, int currentLevel)
		{
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Expected O, but got Unknown
			string fishDisplayName = GetFishDisplayName(fishId);
			string text = ((currentLevel <= -10) ? $"最平庸的{fishDisplayName}（{Math.Abs(currentLevel)}）依然太难了，请升级鱼竿，钓鱼等级，使用料理增加钓鱼等级，使用陷阱/浮木渔具等" : ((currentLevel >= 0) ? Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.positive", (object)new
			{
				fishName = fishDisplayName
			})) : (Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("hud.fail.negative", (object)new
			{
				fishName = fishDisplayName
			})) + $"（{Math.Abs(currentLevel)}）")));
			Game1.addHUDMessage(new HUDMessage(text, 3));
			ModEntry.ModMonitor.Log($"[HUDNotifier] 失败提示显示 | 鱼: {fishDisplayName} ({fishId}) | 等级: {currentLevel} | 触底: {currentLevel <= -10}", (LogLevel)1);
		}

		private static string GetFishDisplayName(string fishId)
		{
			try
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(fishId);
				if (dataOrErrorItem == null || string.IsNullOrEmpty(dataOrErrorItem.DisplayName))
				{
					ModEntry.ModMonitor.Log("[HUDNotifier] 警告：无法获取鱼名称 | 鱼ID: " + fishId, (LogLevel)3);
					return "未知鱼类";
				}
				return dataOrErrorItem.DisplayName;
			}
			catch (Exception ex)
			{
				ModEntry.ModMonitor.Log("[HUDNotifier] 获取鱼名称失败 | 鱼ID: " + fishId + " | 错误: " + ex.Message, (LogLevel)4);
				return "未知鱼类";
			}
		}
	}
}
namespace FishingExpanded.Patches
{
	[HarmonyPatch(typeof(BobberBar))]
	internal class BobberBarPatches
	{
		private class InstanceData
		{
			public string FishId { get; set; }

			public int DifficultyLevel { get; set; }

			public float OriginalDifficulty { get; set; }

			public float AdjustedDifficulty { get; set; }

			public float NativeCatchPenaltyModifier { get; set; } = 1f;

			public float LastAppliedCatchPenaltyModifier { get; set; } = 1f;

			public int QuantityMultiplier { get; set; } = 1;

			public long PlayerId { get; set; } = -1L;

			public Farmer Owner { get; set; }

			public bool FailureRecorded { get; set; }

			public bool ResultStarted { get; set; }

			public int MissCount { get; set; }

			public bool WasBobberInBar { get; set; }
		}

		private static readonly ConditionalWeakTable<BobberBar, InstanceData> _instanceData = new ConditionalWeakTable<BobberBar, InstanceData>();

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		[HarmonyPrefix]
		public static void Constructor_Prefix(string whichFish)
		{
			FarmerFishingLevelPatches.SetLegendarySuppression(Game1.player, SpecialFishHelper.IsLegendaryFish(whichFish));
		}

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		[HarmonyPostfix]
		public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, bool ___bobberInBar)
		{
			try
			{
				string text = SpecialFishHelper.NormalizeItemId(whichFish);
				if (SpecialFishHelper.IsLegendaryFish(text))
				{
					ModEntry.ModMonitor.Log($"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {text} | 保持原始difficulty: {___difficulty:F1}", (LogLevel)2);
					return;
				}
				float num = ___difficulty;
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, Game1.player);
				InstanceData obj = new InstanceData
				{
					FishId = text,
					DifficultyLevel = difficultyLevel,
					OriginalDifficulty = num,
					NativeCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier,
					LastAppliedCatchPenaltyModifier = ___distanceFromCatchPenaltyModifier
				};
				Farmer player = Game1.player;
				obj.PlayerId = ((player != null) ? player.UniqueMultiplayerID : (-1));
				obj.Owner = Game1.player;
				obj.FailureRecorded = false;
				obj.MissCount = 0;
				obj.WasBobberInBar = ___bobberInBar;
				InstanceData instanceData = obj;
				_instanceData.Add(__instance, instanceData);
				float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
				___difficulty *= difficultyMultiplier;
				int num2 = (instanceData.QuantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel));
				___fishSize *= num2;
				___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);
				instanceData.AdjustedDifficulty = ___difficulty;
				HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
				HUDNotifier.ShowStarChallengeNotification(text, difficultyLevel, Game1.player);
				ModEntry.ModMonitor.Log($"[BobberBar] 钓鱼小游戏开始 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 原始difficulty: {num:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | 数量倍数: {num2} | fishSize: {___fishSize} | 品质: {___fishQuality}", (LogLevel)2);
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {value}", (LogLevel)4);
			}
			finally
			{
				FarmerFishingLevelPatches.SetLegendarySuppression(Game1.player, suppressed: false);
			}
		}

		[HarmonyPatch("update")]
		[HarmonyPrefix]
		public static void Update_Prefix(BobberBar __instance, ref float ___distanceFromCatchPenaltyModifier, float ___distanceFromCatching, bool ___bobberInBar)
		{
			try
			{
				if (_instanceData.TryGetValue(__instance, out var value))
				{
					if (value.WasBobberInBar && !___bobberInBar)
					{
						value.MissCount++;
					}
					value.WasBobberInBar = ___bobberInBar;
					float num = ___distanceFromCatchPenaltyModifier;
					if (Math.Abs(num - value.LastAppliedCatchPenaltyModifier) > 0.0001f)
					{
						value.NativeCatchPenaltyModifier = num;
					}
					float catchPenaltyModifier = DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, ___distanceFromCatching);
					float num2 = (value.LastAppliedCatchPenaltyModifier = (___distanceFromCatchPenaltyModifier = Math.Min(value.NativeCatchPenaltyModifier, catchPenaltyModifier)));
					if (Math.Abs(num - num2) > 0.01f)
					{
						ModEntry.ModMonitor.Log($"[BobberBar] 蓄力槽保护触发 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num:F2} → {num2:F2}", (LogLevel)1);
					}
				}
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"BobberBar.update Prefix 失败: {value2}", (LogLevel)4);
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

		[HarmonyPatch("update")]
		[HarmonyPostfix]
		public static void Update_Postfix(BobberBar __instance, float ___distanceFromCatching, bool ___fadeOut)
		{
			try
			{
				if (_instanceData.TryGetValue(__instance, out var value))
				{
					if (___fadeOut && ___distanceFromCatching <= 0f && !value.FailureRecorded)
					{
						ModEntry.ModMonitor.Log($"[BobberBar] 钓鱼失败 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 蓄力槽耗尽: {___distanceFromCatching:F3}", (LogLevel)2);
						DifficultyManager.RecordFailure(value.FishId, value.Owner ?? Game1.player);
						int difficultyLevel = DifficultyManager.GetDifficultyLevel(value.FishId, value.Owner ?? Game1.player);
						HUDNotifier.ShowFailureNotification(value.FishId, difficultyLevel);
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
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"BobberBar.update Postfix 失败: {value2}", (LogLevel)4);
			}
		}

		private static void CleanupInstance(BobberBar instance)
		{
			if (_instanceData.Remove(instance))
			{
				ModEntry.ModMonitor.Log($"[BobberBar] 清理实例数据 | 实例: {((object)instance).GetHashCode()}", (LogLevel)1);
			}
		}

		public static void PeriodicCleanup()
		{
		}
	}
	[HarmonyPatch(typeof(CollectionsPage))]
	internal class CollectionsPagePatches
	{
		[HarmonyPatch("createDescription")]
		[HarmonyPostfix]
		public static void CreateDescription_Postfix(CollectionsPage __instance, string id, ref string __result, int ___currentTab)
		{
			try
			{
				if (___currentTab == 1 && !string.IsNullOrEmpty(id) && !SpecialFishHelper.IsLegendaryFish(id))
				{
					string fishId = SpecialFishHelper.NormalizeItemId(id);
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
					string rankKey = DifficultyCalculator.GetRankKey(difficultyLevel);
					string value = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
					List<string> list = new List<string>();
					if (difficultyLevel > 0)
					{
						list.Add($"挑战等级：{value}（等级{difficultyLevel}）");
					}
					if (DifficultyManager.HasCollectionStar(fishId, Game1.player))
					{
						list.Add("钓鱼技能加成：+0.5");
					}
					if (list.Count > 0)
					{
						__result = __result + Environment.NewLine + string.Join(" | ", list);
					}
				}
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"[CollectionsPage] createDescription Postfix 失败: {value2}", (LogLevel)4);
			}
		}

		[HarmonyPatch("draw")]
		[HarmonyPostfix]
		public static void Draw_Postfix(SpriteBatch b, int ___currentTab, int ___currentPage, Dictionary<int, List<List<ClickableTextureComponent>>> ___collections)
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (___currentTab != 1 || !___collections.TryGetValue(___currentTab, out var value) || ___currentPage < 0 || ___currentPage >= value.Count)
				{
					return;
				}
				foreach (ClickableTextureComponent item in value[___currentPage])
				{
					string[] array = ((ClickableComponent)item).name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (array.Length != 0 && DifficultyManager.HasCollectionStar(array[0], Game1.player))
					{
						b.Draw(Game1.mouseCursors, new Rectangle(((ClickableComponent)item).bounds.X + 3, ((ClickableComponent)item).bounds.Y + 3, 20, 20), (Rectangle?)new Rectangle(346, 392, 8, 8), Color.Gold);
					}
				}
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"[CollectionsPage] 星标绘制失败: {value2}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(Farmer))]
	internal class FarmerFishingLevelPatches
	{
		private static readonly HashSet<long> _legendarySuppressedPlayers = new HashSet<long>();

		private static readonly Dictionary<long, float> _lastLoggedBonus = new Dictionary<long, float>();

		internal static void SetLegendarySuppression(Farmer player, bool suppressed)
		{
			if (player != null)
			{
				if (suppressed)
				{
					_legendarySuppressedPlayers.Add(player.UniqueMultiplayerID);
				}
				else
				{
					_legendarySuppressedPlayers.Remove(player.UniqueMultiplayerID);
				}
			}
		}

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		[HarmonyPostfix]
		public static void FishingLevel_Getter_Postfix(Farmer __instance, ref int __result)
		{
			try
			{
				if (__instance == null || !__instance.IsLocalPlayer || _legendarySuppressedPlayers.Contains(__instance.UniqueMultiplayerID))
				{
					return;
				}
				float fishingLevelBonus = DifficultyManager.GetFishingLevelBonus(__instance);
				if (fishingLevelBonus > 0f)
				{
					int value = __result;
					int num = (int)Math.Floor(fishingLevelBonus);
					__result += num;
					if (!_lastLoggedBonus.TryGetValue(__instance.UniqueMultiplayerID, out var value2) || Math.Abs(fishingLevelBonus - value2) > 0.01f)
					{
						_lastLoggedBonus[__instance.UniqueMultiplayerID] = fishingLevelBonus;
						ModEntry.ModMonitor.Log($"[FarmerFishingLevel] 钓鱼等级加成变化 | 原始等级: {value} | 隐藏加成: +{fishingLevelBonus:F1} (整数部分:{num}) | 最终等级: {__result}", (LogLevel)1);
					}
				}
			}
			catch (Exception value3)
			{
				ModEntry.ModMonitor.Log($"FishingLevel Getter Patch 失败: {value3}", (LogLevel)4);
			}
		}

		[HarmonyPatch("gainExperience")]
		[HarmonyPrefix]
		public static void GainExperience_Prefix(Farmer __instance, int which, ref int howMuch)
		{
			try
			{
				if (which == 1 && howMuch > 0 && __instance.IsLocalPlayer && FishingRodPatches.TryBeginExperienceAdjustment(__instance, out var data))
				{
					int experienceMultiplier = data.ExperienceMultiplier;
					if (experienceMultiplier > 1)
					{
						long num = (long)howMuch * (long)experienceMultiplier;
						howMuch = (int)Math.Min(2147483647L, num);
						ModEntry.ModMonitor.Log($"[FarmerFishingExperience] 经验倍率 | 难度等级: {data.DifficultyLevel} | 倍率: ×{experienceMultiplier} | 经验: {num / experienceMultiplier} → {howMuch}", (LogLevel)1);
					}
				}
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"Fishing experience patch 失败: {value}", (LogLevel)4);
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

			public bool SuccessRecorded { get; set; }

			public bool ExperienceAdjusted { get; set; }

			public int CreateFishCalls { get; set; }

			public bool AllowAdditionalCreateFish { get; set; }
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
				string text2 = SpecialFishHelper.NormalizeItemId(text);
				if (!TryGetPending(val, text2, out var data))
				{
					return 1f;
				}
				float visualScale = DifficultyCalculator.GetVisualScale(data.Multiplier);
				ObjectPatches.LogVisualDiagnostic("FishingRod.draw", $"{val.UniqueMultiplayerID}:{text2}", $"anchor=center | visualScale={visualScale:F3} | stateHit={visualScale > 1.001f}");
				return visualScale;
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"[FishingRodPatches] 结算视觉缩放读取失败: {value}", (LogLevel)3);
				return 1f;
			}
		}

		public static Vector2 AdjustLandingFishPosition(Vector2 position, FishingRod rod)
		{
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
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
				ModEntry.ModMonitor.Log($"[FishingRodPatches] 落地鱼图底边锚点补偿失败: {value}", (LogLevel)3);
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
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
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
				float visualScale = DifficultyCalculator.GetVisualScale(data.Multiplier);
				if (visualScale <= 1.001f)
				{
					return;
				}
				Rectangle caughtItemSourceRect = GetCaughtItemSourceRect(__instance);
				string caughtItemTextureName = GetCaughtItemTextureName(__instance);
				Vector2 val2 = default(Vector2);
				((Vector2)(ref val2))..ctor((float)caughtItemSourceRect.Width * 4f * (1f - visualScale) / 2f, (float)caughtItemSourceRect.Height * 4f * (1f - visualScale) / 2f);
				int num = 0;
				foreach (TemporaryAnimatedSprite animation in __instance.animations)
				{
					if (!(animation.textureName != caughtItemTextureName) && !(animation.sourceRect != caughtItemSourceRect))
					{
						animation.scale *= visualScale;
						animation.position += val2;
						num++;
					}
				}
				ObjectPatches.LogVisualDiagnostic("FishingRod.fly", $"{val.UniqueMultiplayerID}:{text2}", $"anchor=center | visualScale={visualScale:F3} | sprites={num} | stateHit={num > 0}");
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"[FishingRodPatches] 飞行动画视觉缩放失败: {value}", (LogLevel)3);
			}
		}

		[HarmonyPatch("draw")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b7: Expected O, but got Unknown
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Expected O, but got Unknown
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01db: Expected O, but got Unknown
			//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fc: Expected O, but got Unknown
			//IL_0208: Unknown result type (might be due to invalid IL or missing references)
			//IL_0212: Expected O, but got Unknown
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo methodInfo = AccessTools.Method(typeof(FishingRodPatches), "GetFishVisualScale", (Type[])null, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(FishingRodPatches), "AdjustLandingFishPosition", (Type[])null, (Type[])null);
			int num = list.FindIndex((CodeInstruction code) => IsLdcI4(code, 1870));
			if (num < 0)
			{
				ModEntry.ModMonitor.Log("[FishingRodPatches] 未找到结算面板源矩形，落地真鱼缩放未注入", (LogLevel)3);
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
			ModEntry.ModMonitor.Log($"[FishingRodPatches] 落地真鱼缩放注入 | 缩放: {num7} | 底边补偿: {num8}", (LogLevel)((num7 >= 1 && num8 >= 1) ? 2 : 3));
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
		public static void PullFishFromWater_Prefix(FishingRod __instance, string fishId, int fishSize, int fishDifficulty, int numCaught, bool fromFishPond)
		{
			try
			{
				Farmer lastFarmerToUse = ((Tool)__instance).getLastFarmerToUse();
				if (lastFarmerToUse == null || !lastFarmerToUse.IsLocalPlayer || fromFishPond)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(fishId);
				if (SpecialFishHelper.IsLegendaryFish(text))
				{
					ModEntry.ModMonitor.Log("[FishingRod] 传奇鱼（鱼王）豁免规则 | 鱼ID: " + text, (LogLevel)2);
					HUDNotifier.ShowSuccessNotification(fishId, 0);
					return;
				}
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, lastFarmerToUse);
				int quantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel);
				int num = 0;
				float adjustedDifficulty = fishDifficulty;
				IClickableMenu activeClickableMenu = Game1.activeClickableMenu;
				BobberBar val = (BobberBar)(object)((activeClickableMenu is BobberBar) ? activeClickableMenu : null);
				if (val != null)
				{
					num = BobberBarPatches.GetMissCount(val);
					float adjustedDifficulty2 = BobberBarPatches.GetAdjustedDifficulty(val);
					if (adjustedDifficulty2 > 0f)
					{
						adjustedDifficulty = adjustedDifficulty2;
					}
				}
				_pendingFish[GetPendingKey(lastFarmerToUse, text)] = new PendingFishData
				{
					OriginalNum = numCaught,
					Multiplier = quantityMultiplier,
					DifficultyLevel = difficultyLevel,
					ExperienceMultiplier = DifficultyCalculator.GetExperienceMultiplier(difficultyLevel),
					MissCount = num,
					AdjustedDifficulty = adjustedDifficulty,
					FishSize = fishSize
				};
				ModEntry.ModMonitor.Log($"[FishingRod] 钓鱼成功（动画阶段）| 玩家: {lastFarmerToUse.UniqueMultiplayerID} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 数量倍数: {quantityMultiplier} | 脱杆次数: {num} | 动画显示: {numCaught}条", (LogLevel)2);
			}
			catch (Exception value)
			{
				ModEntry.ModMonitor.Log($"pullFishFromWater Prefix 失败: {value}", (LogLevel)4);
			}
		}

		[HarmonyPatch("CreateFish")]
		[HarmonyPostfix]
		public static void CreateFish_Postfix(FishingRod __instance, ref Item __result)
		{
			try
			{
				Farmer lastFarmerToUse = ((Tool)__instance).getLastFarmerToUse();
				if (lastFarmerToUse == null || !lastFarmerToUse.IsLocalPlayer || __result == null)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(__result.QualifiedItemId);
				string pendingKey = GetPendingKey(lastFarmerToUse, text);
				if (_pendingFish.TryGetValue(pendingKey, out var value))
				{
					bool flag = value.CreateFishCalls == 0 || value.AllowAdditionalCreateFish;
					value.CreateFishCalls++;
					value.AllowAdditionalCreateFish = false;
					if (flag && value.Multiplier > 1)
					{
						int num = Math.Max(1, __result.Stack);
						long val = (long)num * (long)value.Multiplier;
						__result.Stack = (int)Math.Min(2147483647L, val);
						ModEntry.ModMonitor.Log($"[FishingRod] 数量转化完成 | 玩家: {lastFarmerToUse.UniqueMultiplayerID} | 鱼ID: {text} | 原生堆叠: {num} → 最终: {__result.Stack} (×{value.Multiplier})", (LogLevel)2);
					}
				}
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"FishingRod.CreateFish Postfix 失败: {value2}", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"FishingRod.doneHoldingFish Postfix 失败: {value}", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"FishingRod.openTreasureMenuEndFunction Prefix 失败: {value}", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"FishingRod.openTreasureMenuEndFunction Postfix 失败: {value}", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"FishingRod.justGotDerbyTagEndFunction Prefix 失败: {value}", (LogLevel)4);
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
				ModEntry.ModMonitor.Log($"FishingRod.justGotDerbyTagEndFunction Postfix 失败: {value}", (LogLevel)4);
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
		[HarmonyPostfix]
		public static void CaughtFish_Postfix(Farmer __instance, string itemId, int size, bool from_fish_pond, int numberCaught)
		{
			try
			{
				if (from_fish_pond || !__instance.IsLocalPlayer)
				{
					return;
				}
				string text = SpecialFishHelper.NormalizeItemId(itemId);
				if (FishingRodPatches.TryGetPending(__instance, text, out var data) && !data.SuccessRecorded)
				{
					data.SuccessRecorded = true;
					int num = data.MissCount switch
					{
						0 => 10, 
						1 => 5, 
						2 => 2, 
						_ => 1, 
					};
					int difficultyLevel = DifficultyManager.GetDifficultyLevel(text, __instance);
					int value = DifficultyManager.RecordSuccess(text, num, __instance);
					int difficultyLevel2 = DifficultyManager.GetDifficultyLevel(text, __instance);
					DifficultyManager.RecordHighDifficulty(text, data.AdjustedDifficulty, __instance);
					if (data.Multiplier > 15)
					{
						int fishSize = ((data.FishSize > 0) ? data.FishSize : (Math.Max(1, data.OriginalNum) * data.Multiplier));
						GiantFishManager.RecordGiantFish(__instance, text, data.Multiplier, fishSize);
					}
					ModEntry.ModMonitor.Log($"[Farmer] 难度等级更新 | 鱼ID: {text} | 脱杆: {data.MissCount}次 | 基础增长: +{num} | 实际增长: +{value} | {difficultyLevel} → {difficultyLevel2}", (LogLevel)2);
					HUDNotifier.ShowSuccessNotification(text, difficultyLevel2, difficultyLevel);
				}
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"caughtFish Postfix 失败: {value2}", (LogLevel)4);
			}
		}
	}
	[HarmonyPatch(typeof(Game1))]
	internal static class Game1Patches
	{
		[HarmonyPatch("drawPlayerHeldObject")]
		[HarmonyPrefix]
		public static void DrawPlayerHeldObject_Prefix(Farmer f)
		{
			try
			{
				Object val = ((f != null) ? f.ActiveObject : null);
				string value = ((val != null) ? ((Item)val).QualifiedItemId : null) ?? "<null>";
				long num = ((f != null) ? f.UniqueMultiplayerID : (-1));
				int? num2 = ((val != null) ? new int?(((Item)val).Category) : ((int?)null));
				bool value2 = num2 == -4;
				string identity = num.ToString();
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 7);
				defaultInterpolatedStringHandler.AppendLiteral("item=");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(" | category=");
				defaultInterpolatedStringHandler.AppendFormatted(num2?.ToString() ?? "<null>");
				defaultInterpolatedStringHandler.AppendLiteral(" | ");
				defaultInterpolatedStringHandler.AppendLiteral("isFishCategory=");
				defaultInterpolatedStringHandler.AppendFormatted(value2);
				defaultInterpolatedStringHandler.AppendLiteral(" | activeObject=");
				defaultInterpolatedStringHandler.AppendFormatted(val != null);
				defaultInterpolatedStringHandler.AppendLiteral(" | ");
				defaultInterpolatedStringHandler.AppendLiteral("isCarrying=");
				defaultInterpolatedStringHandler.AppendFormatted(f != null && f.IsCarrying());
				defaultInterpolatedStringHandler.AppendLiteral(" | isLocal=");
				defaultInterpolatedStringHandler.AppendFormatted(f != null && f.IsLocalPlayer);
				defaultInterpolatedStringHandler.AppendLiteral(" | ");
				defaultInterpolatedStringHandler.AppendLiteral("location=");
				object obj;
				if (f == null)
				{
					obj = null;
				}
				else
				{
					GameLocation currentLocation = ((Character)f).currentLocation;
					obj = ((currentLocation != null) ? currentLocation.Name : null);
				}
				if (obj == null)
				{
					obj = "<null>";
				}
				defaultInterpolatedStringHandler.AppendFormatted((string?)obj);
				ObjectPatches.LogVisualDiagnostic("Game1.drawPlayerHeldObject", identity, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			catch (Exception value3)
			{
				ModEntry.ModMonitor.Log($"[DIAG-FISH-VISUAL] Game1入口诊断失败: {value3}", (LogLevel)3);
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
				ModEntry.ModMonitor.Log($"NPC.checkAction 巨型鱼对话替换失败: {value}", (LogLevel)4);
				return true;
			}
		}
	}
	[HarmonyPatch(typeof(Object))]
	internal class ObjectPatches
	{
		private static bool _transpilerLogged = false;

		private static readonly Dictionary<string, string> _visualDiagnosticStates = new Dictionary<string, string>();

		internal static void ResetVisualDiagnostics()
		{
			_visualDiagnosticStates.Clear();
		}

		internal static void LogVisualDiagnostic(string stage, string identity, string state)
		{
			string key = stage + "|" + identity;
			if (!_visualDiagnosticStates.TryGetValue(key, out var value) || !string.Equals(value, state, StringComparison.Ordinal))
			{
				_visualDiagnosticStates[key] = state;
				ModEntry.ModMonitor.Log($"[DIAG-FISH-VISUAL] stage={stage} | id={identity} | {state}", (LogLevel)1);
			}
		}

		[HarmonyPatch("drawWhenHeld")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawWhenHeld_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = instructions.ToList();
			ModEntry.ModMonitor.Log($"[ObjectPatches] 开始分析 Object.drawWhenHeld() | 总指令数: {codes.Count}", (LogLevel)1);
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
						ModEntry.ModMonitor.Log($"[ObjectPatches] ✓ 成功插入视觉缩放逻辑到 Object.drawWhenHeld() | 位置: IL_{i:X4}", (LogLevel)2);
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
				ModEntry.ModMonitor.Log("[ObjectPatches] ⚠ 未找到 ldc.r4 4 指令，视觉缩放可能无效", (LogLevel)3);
				yield break;
			}
			ModEntry.ModMonitor.Log($"[ObjectPatches] ✓ 共修改了 {patchCount} 处 scale 参数", (LogLevel)2);
		}

		[HarmonyPatch("drawWhenHeld")]
		[HarmonyPrefix]
		public static void DrawWhenHeld_Prefix(Object __instance, Farmer f, ref Vector2 objectPosition)
		{
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string text = ((__instance != null) ? ((Item)__instance).QualifiedItemId : null) ?? "<null>";
				long value = ((f != null) ? f.UniqueMultiplayerID : (-1));
				int? num = ((__instance != null) ? new int?(((Item)__instance).Category) : ((int?)null));
				bool flag = num == -4;
				bool value2 = f != null && f.ActiveObject == __instance;
				LogVisualDiagnostic("Object.drawWhenHeld", $"{value}:{text}", $"category={num?.ToString() ?? "<null>"} | isFishCategory={flag} | isActiveObject={value2} | isCarrying={f != null && f.IsCarrying()} | isLocal={f != null && f.IsLocalPlayer}");
				if (__instance != null && f != null && flag)
				{
					float fishVisualScale = GiantFishManager.GetFishVisualScale(text, f);
					if (!(fishVisualScale <= 1.001f))
					{
						ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(text);
						Rectangle sourceRect = dataOrErrorItem.GetSourceRect(0, (int?)((Item)__instance).ParentSheetIndex);
						objectPosition += new Vector2((float)sourceRect.Width * 4f * (1f - fishVisualScale) / 2f, (float)sourceRect.Height * 4f * (1f - fishVisualScale));
						LogVisualDiagnostic("HeldAnchor", $"{value}:{text}", $"anchor=bottom-center | visualScale={fishVisualScale:F3} | source={sourceRect.Width}x{sourceRect.Height}");
					}
				}
			}
			catch (Exception value3)
			{
				ModEntry.ModMonitor.Log($"[DIAG-FISH-VISUAL] drawWhenHeld入口诊断失败: {value3}", (LogLevel)3);
			}
		}

		public static float GetDrawScale(Object obj, Farmer owner)
		{
			try
			{
				string text = ((obj != null) ? ((Item)obj).QualifiedItemId : null) ?? "<null>";
				int? num = ((obj != null) ? new int?(((Item)obj).Category) : ((int?)null));
				bool flag = num == -4;
				float num2 = 1f;
				if (obj != null && flag)
				{
					num2 = GiantFishManager.GetFishVisualScale(text, owner);
				}
				long value = ((owner != null) ? owner.UniqueMultiplayerID : (-1));
				LogVisualDiagnostic("GetDrawScale", $"{value}:{text}", $"category={num?.ToString() ?? "<null>"} | isFishCategory={flag} | visualScale={num2:F3} | stateHit={num2 > 1.001f}");
				return num2;
			}
			catch (Exception value2)
			{
				ModEntry.ModMonitor.Log($"[ObjectPatches] GetDrawScale 失败: {value2}", (LogLevel)4);
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

		public float FishingLevelBonus { get; set; }

		public HashSet<string> CollectionStars { get; set; } = new HashSet<string>();
	}
	public class FishStats
	{
		public int SuccessCount { get; set; }

		public int FailCount { get; set; }

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
		public Dictionary<string, (int multiplier, int fishSize)> ActiveGiantFish { get; set; } = new Dictionary<string, (int, int)>();

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
