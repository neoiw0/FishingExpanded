using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using xTile.Dimensions;

namespace FishingExpanded.Patches;

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
			((Vector2)(ref val4))._002Ector(val3, num11 + (float)i * num6);
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
					((Rectangle)(ref value2))._002Ector(217, 205, 19, 19);
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
