using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(BobberBar))]
internal class BobberBarPatches
{
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

		public float Alpha => Math.Max(0f, 1f - Age / 5f);

		public float YOffset => 30f * (Age / 5f);
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

	private const float BarLeftX = 64f;

	private const float BarWidth = 36f;

	private const float TipGapBarWidths = 3f;

	private const float OtherTipAnchorX = -44f;

	private const float ActionTipAnchorX = 208f;

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
			tips[num].Age += dt;
			if (tips[num].Age >= 5f)
			{
				tips.RemoveAt(num);
			}
		}
	}

	private static void AddTip(List<FloatingTip> tips, string text, float x, float y, bool centered, bool rightAligned = false, bool actionTip = false)
	{
		if (tips.Count >= 8)
		{
			tips.RemoveAt(0);
		}
		tips.Add(new FloatingTip
		{
			Text = text,
			StartX = x,
			StartY = y,
			Centered = centered,
			RightAligned = rightAligned,
			IsActionTip = actionTip
		});
	}

	private static void DrawTip(SpriteBatch b, FloatingTip tip)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(tip.Text))
		{
			return;
		}
		Color val = (tip.IsActionTip ? Color.RoyalBlue : Color.DarkRed);
		Vector2 val2 = Game1.smallFont.MeasureString(tip.Text) * 1.3f;
		float num = (tip.RightAligned ? (tip.StartX - val2.X) : (tip.Centered ? (tip.StartX - val2.X / 2f) : tip.StartX));
		float num2 = (tip.Centered ? (tip.StartY + tip.YOffset) : (tip.StartY - val2.Y / 2f + tip.YOffset));
		Vector2 val3 = default(Vector2);
		((Vector2)(ref val3))..ctor(num, num2);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i != 0 || j != 0)
				{
					b.DrawString(Game1.smallFont, tip.Text, val3 + new Vector2((float)i, (float)j), val * tip.Alpha, 0f, Vector2.Zero, 1.3f, (SpriteEffects)0, 0f);
				}
			}
		}
		b.DrawString(Game1.smallFont, tip.Text, val3, Color.White * tip.Alpha, 0f, Vector2.Zero, 1.3f, (SpriteEffects)0, 0f);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberTargetPosition, bool ___bobberInBar, ref int ___bobberBarHeight, string baitID, float ___bobberBarPos, int ___xPositionOnScreen, int ___yPositionOnScreen)
	{
		try
		{
			float num = ModEntry.ConsumeForcePerseveranceSeconds();
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
			float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
			___difficulty *= difficultyMultiplier;
			int value = (instanceData.QuantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel));
			int value2 = ___fishSize;
			___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);
			instanceData.AdjustedDifficulty = ___difficulty;
			instanceData.EffectiveDifficulty = ___difficulty;
			float tipX = (float)___xPositionOnScreen + -44f;
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
			FishingLog.Log($"[BobberBar] 钓鱼小游戏开始 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 原始difficulty: {num2:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | 数量倍数: {value} | fishSize(原生): {value2} (结算×{DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel):F2}) | 品质: {___fishQuality} | 加速增幅档: {GetAccelerationTier(difficultyLevel):P0} | 加速度增幅: ×{accelerationBoost:F2} | 跳鱼间隔: {instanceData.JumpIntervalSeconds:F0}s | 鱼竿熟练度α: {instanceData.Alpha:P0} | 力竭: {instanceData.AdjustedDifficulty:F0}{((instanceData.AdjustedDifficulty >= 100f) ? "（参与）" : "（不参与）")} | 挑战鱼饵: {instanceData.HasChallengeBait}" + ((num > 0f) ? $" | 持久战测试强制: {num:F0}s" : ""), (LogLevel)2);
		}
		catch (Exception value3)
		{
			FishingLog.Log($"BobberBar 构造函数 Patch 失败: {value3}", (LogLevel)4);
		}
	}

	[HarmonyPatch("update")]
	[HarmonyPrefix]
	public static void Update_Prefix(BobberBar __instance, ref float ___difficulty, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberPosition, ref float ___bobberTargetPosition, ref float ___bobberSpeed, float ___distanceFromCatching, bool ___bobberInBar, ref int ___fishSizeReductionTimer, ref int ___challengeBaitFishes, int ___xPositionOnScreen, int ___yPositionOnScreen, float ___bobberBarPos, int ___bobberBarHeight)
	{
		try
		{
			if (!_instanceData.TryGetValue(__instance, out var value))
			{
				return;
			}
			float num = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			AgeTips(value.ActionTips, num);
			AgeTips(value.OtherTips, num);
			if (!value.ResultStarted)
			{
				value.BattleElapsedSeconds += num;
				if (!value.PeakTipShown && value.BattleElapsedSeconds >= 30f)
				{
					value.PeakTipShown = true;
					string text = PickPeakText(value.DifficultyLevel);
					AddTip(value.OtherTips, text, (float)___xPositionOnScreen + -44f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
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
						AddTip(value.OtherTips, text4, (float)___xPositionOnScreen + -44f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
						FishingLog.Log($"[BobberBar] 力竭节点 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 节点: {tuple.Item1:0}分钟 ({tuple.Item2:P0}) | 耗时: {value.BattleElapsedSeconds:F0}s | 挑战鱼饵: {value.HasChallengeBait} | 文案: {text4}", (LogLevel)2);
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
			if (value.DifficultyLevel > 0)
			{
				___fishSizeReductionTimer = 800;
			}
			if (value.HasChallengeBait && value.AdjustedDifficulty > 100f)
			{
				___challengeBaitFishes = 3;
			}
			if (value.WasBobberInBar && !___bobberInBar)
			{
				value.MissCount++;
			}
			value.WasBobberInBar = ___bobberInBar;
			float num2 = ___distanceFromCatchPenaltyModifier;
			if (Math.Abs(num2 - value.LastAppliedCatchPenaltyModifier) > 0.0001f)
			{
				value.NativeCatchPenaltyModifier = num2;
			}
			float catchProgress = (value.ProtectionEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
			float num3 = DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, catchProgress);
			bool flag = false;
			int num4 = 0;
			if (!value.HasChallengeBait && value.AdjustedDifficulty > 100f)
			{
				num4 = DifficultyManager.GetConsecutiveFailCount(value.FishId, value.Owner ?? Game1.player);
				if (num4 > 0)
				{
					float catchProgress2 = (value.EscapeBonusEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
					num3 = DifficultyCalculator.GetEscapeFailBonusModifier(value.DifficultyLevel, num4, catchProgress2);
					flag = num3 < DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, catchProgress2) - 0.0001f;
				}
			}
			if (flag && !value.EscapeBonusEngaged)
			{
				value.EscapeBonusEngaged = true;
				FishingLog.Log($"[BobberBar] 逃逸减速加成生效 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 连续失败: {num4} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num3:F2}", (LogLevel)1);
			}
			else if (!flag && value.EscapeBonusEngaged)
			{
				value.EscapeBonusEngaged = false;
				FishingLog.Log($"[BobberBar] 逃逸减速加成解除 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 减速倍率: {num3:F2}", (LogLevel)1);
			}
			float num5 = (value.LastAppliedCatchPenaltyModifier = (___distanceFromCatchPenaltyModifier = Math.Min(value.NativeCatchPenaltyModifier, num3)));
			bool flag2 = num5 < value.NativeCatchPenaltyModifier - 0.0001f;
			if (flag2 && !value.ProtectionEngaged)
			{
				value.ProtectionEngaged = true;
				FishingLog.Log($"[BobberBar] 蓄力槽保护生效 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num2:F2} → {num5:F2}", (LogLevel)1);
			}
			else if (!flag2 && value.ProtectionEngaged)
			{
				value.ProtectionEngaged = false;
				FishingLog.Log($"[BobberBar] 蓄力槽保护解除 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num5:F2} → {num2:F2}", (LogLevel)1);
			}
			if ((!(value.EffectiveDifficulty >= 150f) && !value.JumpPending) || value.ResultStarted)
			{
				return;
			}
			if (value.JumpPending)
			{
				value.JumpPendingSeconds -= num;
				if (value.JumpPendingSeconds <= 0f)
				{
					___bobberPosition = value.JumpPendingTarget;
					___bobberTargetPosition = value.JumpPendingTarget;
					___bobberSpeed = 0f;
					value.JumpPending = false;
					value.JumpCooldownSeconds = value.JumpIntervalSeconds;
					value.JumpDetectionSeconds = 0f;
					bool flag3 = value.JumpPendingTarget <= 133f;
					string text7 = PickJumpText(flag3);
					AddTip(value.ActionTips, text7, (float)___xPositionOnScreen + 208f, (float)___yPositionOnScreen + 36f + value.JumpPendingTarget - 30f, centered: false, rightAligned: false, actionTip: true);
					FishingLog.Log($"[BobberBar] 高难度鱼跳 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 难度: {value.EffectiveDifficulty:F0} | 跳至: {value.JumpPendingTarget:F0} | 文案: {(flag3 ? "上跳(鱼跃)" : "下跳(甩尾)")}: {text7}", (LogLevel)2);
				}
				return;
			}
			if (value.JumpCooldownSeconds > 0f)
			{
				value.JumpCooldownSeconds -= num;
				if (value.JumpCooldownSeconds < 0f)
				{
					value.JumpCooldownSeconds = 0f;
				}
				return;
			}
			value.JumpDetectionSeconds -= num;
			if (value.JumpDetectionSeconds <= 0f)
			{
				value.JumpDetectionSeconds = 1f;
				float num7 = ___bobberPosition;
				if (num7 >= 399f)
				{
					value.JumpPending = true;
					value.JumpPendingSeconds = 0.5f;
					value.JumpPendingTarget = Game1.random.Next(0, 134);
				}
				else if (num7 <= 133f)
				{
					value.JumpPending = true;
					value.JumpPendingSeconds = 0.5f;
					value.JumpPendingTarget = Game1.random.Next(399, 533);
				}
			}
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
	public static void Update_Postfix(BobberBar __instance, float ___distanceFromCatching, bool ___fadeOut)
	{
		try
		{
			if (_instanceData.TryGetValue(__instance, out var value))
			{
				if (___fadeOut && ___distanceFromCatching <= 0f && !value.FailureRecorded)
				{
					FishingLog.Log($"[BobberBar] 钓鱼失败 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 蓄力槽耗尽: {___distanceFromCatching:F3}", (LogLevel)2);
					DifficultyManager.RecordFailure(value.FishId, value.Owner ?? Game1.player);
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

	public static float GetFrameScale()
	{
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
		float num6 = 30f * (1f - 0.3f * alpha);
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
			return 1f - 0.5f * alpha;
		}
		return 1f + 0.5f * alpha;
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
					AddTip(data.OtherTips, text2, tipX, tipY, centered: false, rightAligned: true);
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
		try
		{
			if (!_instanceData.TryGetValue(__instance, out var value))
			{
				return;
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
		catch (Exception value2)
		{
			FishingLog.LogRateLimited("BobberBar.Draw_Postfix", $"BobberBar 小游戏文案绘制失败: {value2}", (LogLevel)4);
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
