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
		for (int i = tips.Count - 1; i >= 0; i--)
		{
			if (tips[i].PendingDelay > 0f)
			{
				tips[i].PendingDelay = Math.Max(0f, tips[i].PendingDelay - dt);
			}
			else
			{
				tips[i].Age += dt;
				if (tips[i].Age >= 5f)
				{
					tips.RemoveAt(i);
				}
			}
		}
	}

	private static void AddTip(List<FloatingTip> tips, string text, float x, float y, bool centered, bool rightAligned = false, bool actionTip = false)
	{
		float availableWidth = Math.Min(420f, rightAligned ? Math.Max(120f, x - 16f) : Math.Max(120f, (float)((Rectangle)(ref Game1.viewport)).Width - x - 16f));
		List<string> chunks = SplitTipChunks(Game1.dialogueFont, text, availableWidth, 1f, 3);
		for (int i = 0; i < chunks.Count; i++)
		{
			string chunkText = chunks[i];
			if (i < chunks.Count - 1)
			{
				chunkText += "…";
			}
			if (tips.Count >= 8)
			{
				tips.RemoveAt(0);
			}
			tips.Add(new FloatingTip
			{
				Id = Interlocked.Increment(ref _nextTipId),
				Text = chunkText,
				StartX = x,
				StartY = y,
				Centered = centered,
				RightAligned = rightAligned,
				IsActionTip = actionTip,
				PendingDelay = (float)i * 5.2f
			});
		}
	}

	private static List<string> SplitTipChunks(SpriteFont font, string text, float maxWidth, float scale, int maxLinesPerChunk)
	{
		List<string> result = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			result.Add(text ?? string.Empty);
			return result;
		}
		List<string> allLines = WrapTipText(font, text, maxWidth, scale);
		for (int i = 0; i < allLines.Count; i += maxLinesPerChunk)
		{
			int count = Math.Min(maxLinesPerChunk, allLines.Count - i);
			result.Add(string.Join(" ", allLines.GetRange(i, count)));
		}
		if (result.Count == 0)
		{
			result.Add(text);
		}
		return result;
	}

	private static void DrawTip(SpriteBatch b, FloatingTip tip)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_053b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0620: Unknown result type (might be due to invalid IL or missing references)
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		if (tip.PendingDelay > 0f || string.IsNullOrEmpty(tip.Text))
		{
			return;
		}
		Color baseBorder = (tip.IsActionTip ? Color.RoyalBlue : Color.DarkRed);
		Color borderColor = Color.Lerp(baseBorder, Color.White, 0.85f);
		SpriteFont tipFont = Game1.dialogueFont;
		float availableWidth = Math.Min(420f, tip.RightAligned ? Math.Max(120f, tip.StartX - 16f) : Math.Max(120f, (float)((Rectangle)(ref Game1.viewport)).Width - tip.StartX - 16f));
		List<string> lines = WrapTipText(tipFont, tip.Text, availableWidth, 1f);
		if (lines.Count > 3)
		{
			while (lines.Count > 3)
			{
				lines.RemoveAt(lines.Count - 1);
			}
			string lastLine = lines[2];
			while (tipFont.MeasureString(lastLine + "…").X * 1f > availableWidth && lastLine.Length > 0)
			{
				lastLine = lastLine.Substring(0, lastLine.Length - 1);
			}
			lines[2] = lastLine + "…";
		}
		float lineHeight = (float)tipFont.LineSpacing * 1f;
		float totalHeight = (float)lines.Count * lineHeight;
		float maxWidth = 0f;
		foreach (string line in lines)
		{
			maxWidth = Math.Max(maxWidth, tipFont.MeasureString(line).X * 1f);
		}
		float barLeft;
		float barRight;
		if (tip.RightAligned)
		{
			barLeft = tip.StartX + 50f;
			barRight = barLeft + 36f;
		}
		else if (!tip.Centered)
		{
			barRight = tip.StartX - 24f;
			barLeft = barRight - 36f;
		}
		else
		{
			barLeft = tip.StartX - 64f;
			barRight = barLeft + 36f;
		}
		bool flipped = false;
		float x;
		if (tip.RightAligned)
		{
			if (tip.StartX - 8f < maxWidth)
			{
				x = barRight + 24f;
				flipped = true;
			}
			else
			{
				x = tip.StartX - maxWidth;
			}
		}
		else if (!tip.Centered)
		{
			if (tip.StartX + maxWidth > (float)((Rectangle)(ref Game1.viewport)).Width - 8f)
			{
				x = barLeft - 50f - maxWidth;
				flipped = true;
			}
			else
			{
				x = tip.StartX;
			}
		}
		else
		{
			x = tip.StartX - maxWidth / 2f;
		}
		x = Math.Max(8f, Math.Min(x, (float)((Rectangle)(ref Game1.viewport)).Width - 8f - maxWidth));
		if (flipped && !tip.FlipLogged)
		{
			tip.FlipLogged = true;
			FishingLog.LogRateLimited("TipSideFlip:" + tip.Id, $"[BobberBar] 提示侧翻 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {barLeft - 64f:F0} | StartX: {tip.StartX:F0} | maxWidth: {maxWidth:F0} | finalX: {x:F0} | viewportW: {((Rectangle)(ref Game1.viewport)).Width}", (LogLevel)2);
		}
		if (!tip.DrawLogged)
		{
			tip.DrawLogged = true;
			FishingLog.LogRateLimited("TipDraw:" + tip.Id, $"[BobberBar] 提示坐标 | 类型: {(tip.IsActionTip ? "动作" : "其他")} | barX: {barLeft - 64f:F0} | startX: {tip.StartX:F0} | maxWidth: {maxWidth:F0} | finalX: {x:F0} | viewportW: {((Rectangle)(ref Game1.viewport)).Width} | flipped: {flipped}", (LogLevel)2);
		}
		float y = (tip.Centered ? (tip.StartY + tip.YOffset) : (tip.StartY - totalHeight / 2f + tip.YOffset));
		Vector2 linePos = default(Vector2);
		for (int li = 0; li < lines.Count; li++)
		{
			((Vector2)(ref linePos))..ctor(x, y + (float)li * lineHeight);
			b.DrawString(tipFont, lines[li], linePos + new Vector2(2f, 2f), Color.Black * (tip.Alpha * 0.65f), 0f, Vector2.Zero, 1f, (SpriteEffects)0, 0f);
			for (int d = 0; d < 4; d++)
			{
				Vector2 dir = (Vector2)(d switch
				{
					0 => new Vector2(1f, 0f), 
					1 => new Vector2(-1f, 0f), 
					2 => new Vector2(0f, 1f), 
					_ => new Vector2(0f, -1f), 
				});
				b.DrawString(tipFont, lines[li], linePos + dir, borderColor * (tip.Alpha * 0.85f), 0f, Vector2.Zero, 1f, (SpriteEffects)0, 0f);
			}
			b.DrawString(tipFont, lines[li], linePos, Color.White * tip.Alpha, 0f, Vector2.Zero, 1f, (SpriteEffects)0, 0f);
		}
	}

	private static List<string> WrapTipText(SpriteFont font, string text, float maxWidth, float scale)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		List<string> lines = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			lines.Add(text ?? string.Empty);
			return lines;
		}
		string current = "";
		string[] array = text.Split(' ');
		foreach (string word in array)
		{
			if (word.Length == 0)
			{
				continue;
			}
			string candidate = ((current.Length == 0) ? word : (current + " " + word));
			if (font.MeasureString(candidate).X * scale <= maxWidth || current.Length == 0)
			{
				current = candidate;
				continue;
			}
			if (current.Length > 0)
			{
				lines.Add(current);
				current = "";
			}
			current = word;
			while (current.Length > 0 && font.MeasureString(current).X * scale > maxWidth)
			{
				int cut = current.Length;
				while (cut > 1 && font.MeasureString(current.Substring(0, cut)).X * scale > maxWidth)
				{
					cut--;
				}
				if (cut < 1)
				{
					cut = 1;
				}
				lines.Add(current.Substring(0, cut));
				current = current.Substring(cut);
			}
		}
		if (current.Length > 0)
		{
			lines.Add(current);
		}
		if (lines.Count == 0)
		{
			lines.Add(text);
		}
		return lines;
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberTargetPosition, bool ___bobberInBar, ref int ___bobberBarHeight, string baitID, float ___bobberBarPos, int ___xPositionOnScreen, int ___yPositionOnScreen)
	{
		try
		{
			float forcedPerseveranceSeconds = ModEntry.ConsumeForcePerseveranceSeconds();
			string normalizedFishId = SpecialFishHelper.NormalizeItemId(whichFish);
			if (SpecialFishHelper.IsLegendaryFish(normalizedFishId))
			{
				FishingLog.Log($"[BobberBar] 传奇鱼（鱼王）豁免规则 | 鱼ID: {normalizedFishId} | 保持原始difficulty: {___difficulty:F1}", (LogLevel)2);
				if (forcedPerseveranceSeconds > 0f)
				{
					FishingLog.Log($"[BobberBar] 持久战测试标志被鱼王豁免消耗 | 强制秒数: {forcedPerseveranceSeconds:F0}s", (LogLevel)2);
				}
				return;
			}
			float originalDifficulty = ___difficulty;
			int difficultyLevel = DifficultyManager.GetDifficultyLevel(normalizedFishId, Game1.player);
			InstanceData obj = new InstanceData
			{
				FishId = normalizedFishId,
				DifficultyLevel = difficultyLevel,
				OriginalDifficulty = originalDifficulty,
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
			obj.ForcedPerseveranceSeconds = forcedPerseveranceSeconds;
			InstanceData instanceData = obj;
			_instanceData.Add(__instance, instanceData);
			if (instanceData.HasChallengeBait)
			{
				instanceData.PatternSeed = DifficultyManager.GetOrCreateChallengePatternSeed(normalizedFishId, difficultyLevel, Game1.player);
				instanceData.PatternRandom = new Random(instanceData.PatternSeed);
			}
			float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
			___difficulty *= difficultyMultiplier;
			int quantityMultiplier = (instanceData.QuantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel));
			int nativeFishSize = ___fishSize;
			___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);
			instanceData.AdjustedDifficulty = ___difficulty;
			instanceData.EffectiveDifficulty = ___difficulty;
			float assistTipX = (float)___xPositionOnScreen + 14f;
			float assistTipY = (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f;
			TryTriggerAssist(Game1.player, instanceData, ref ___bobberBarHeight, assistTipX, assistTipY);
			if (instanceData.AdjustedDifficulty > 100f)
			{
				___bobberTargetPosition = 0f;
			}
			instanceData.JumpIntervalSeconds = GetJumpInterval(instanceData.AdjustedDifficulty);
			float accelerationBoost = GetAccelerationBoost(__instance, ___difficulty);
			HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
			HUDNotifier.ShowStarChallengeNotification(normalizedFishId, difficultyLevel, Game1.player);
			FishingLog.Log($"[BobberBar] 钓鱼小游戏开始 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {normalizedFishId} | 难度等级: {difficultyLevel} | 原始difficulty: {originalDifficulty:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | 数量倍数: {quantityMultiplier} | fishSize(原生): {nativeFishSize} (结算×{DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel):F2}) | 品质: {___fishQuality} | 加速增幅档: {GetAccelerationTier(difficultyLevel):P0} | 加速度增幅: ×{accelerationBoost:F2} | 跳鱼间隔: {instanceData.JumpIntervalSeconds:F0}s | 鱼竿熟练度α: {instanceData.Alpha:P0} | 力竭: {instanceData.AdjustedDifficulty:F0}{((instanceData.AdjustedDifficulty >= 100f) ? "（参与）" : "（不参与）")} | 挑战鱼饵: {instanceData.HasChallengeBait} | barX: {___xPositionOnScreen} | barY: {___yPositionOnScreen} | viewportW: {((Rectangle)(ref Game1.viewport)).Width}" + ((forcedPerseveranceSeconds > 0f) ? $" | 持久战测试强制: {forcedPerseveranceSeconds:F0}s" : ""), (LogLevel)2);
		}
		catch (Exception value)
		{
			FishingLog.Log($"BobberBar 构造函数 Patch 失败: {value}", (LogLevel)4);
		}
	}

	[HarmonyPatch("update")]
	[HarmonyPrefix]
	public static void Update_Prefix(BobberBar __instance, ref float ___difficulty, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberPosition, ref float ___bobberTargetPosition, ref float ___bobberSpeed, ref float ___floaterSinkerAcceleration, float ___distanceFromCatching, bool ___bobberInBar, ref int ___fishSizeReductionTimer, ref int ___challengeBaitFishes, int ___xPositionOnScreen, int ___yPositionOnScreen, float ___bobberBarPos, int ___bobberBarHeight)
	{
		try
		{
			if (!_instanceData.TryGetValue(__instance, out var data))
			{
				return;
			}
			if (data.PatternRandom != null)
			{
				data.SavedGameRandom = Game1.random;
				Game1.random = data.PatternRandom;
			}
			float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f)
			{
				dt = 1f / 60f;
			}
			AgeTips(data.ActionTips, dt);
			AgeTips(data.OtherTips, dt);
			if (data.IdlePending && !data.IsIdle)
			{
				float barTop = ___bobberBarPos - 32f;
				float barBottom = barTop + (float)___bobberBarHeight;
				if (___bobberPosition - 16f > barBottom + 5f || ___bobberPosition + 12f < barTop - 5f)
				{
					data.IsIdle = true;
					data.IdlePending = false;
					data.IdleFrozenPosition = ___bobberPosition;
					if (!data.IdleTipShown)
					{
						data.IdleTipShown = true;
						AddTip(data.OtherTips, PickIdleText(), (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
					}
				}
			}
			if (!data.ResultStarted && !data.IsIdle)
			{
				data.BattleElapsedSeconds += dt;
				if (!data.PeakTipShown && data.BattleElapsedSeconds >= 30f)
				{
					data.PeakTipShown = true;
					string peakText = PickPeakText(data.DifficultyLevel);
					AddTip(data.OtherTips, peakText, (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
					FishingLog.Log($"[BobberBar] 持久战巅峰提示 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 耗时: {data.BattleElapsedSeconds:F0}s | 文案: {peakText}", (LogLevel)2);
				}
				if (data.AdjustedDifficulty >= 100f)
				{
					while (data.NextExhaustionNodeIndex < DifficultyCalculator.ExhaustionNodes.Length && data.BattleElapsedSeconds >= DifficultyCalculator.ExhaustionNodes[data.NextExhaustionNodeIndex].Minute * 60f)
					{
						(float, float) node = DifficultyCalculator.ExhaustionNodes[data.NextExhaustionNodeIndex];
						data.NextExhaustionNodeIndex++;
						string rankName = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(data.DifficultyLevel)));
						int textIndex = Game1.random.Next(1, 11);
						string key = $"hud.exhaust.{node.Item1:0}.{textIndex}";
						string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(key, (object)new { rankName }));
						if (string.IsNullOrWhiteSpace(text) || text == key)
						{
							text = $"[{rankName}]体力见底（{node.Item1:0}分钟）";
						}
						if (data.HasChallengeBait)
						{
							int appendIndex = Game1.random.Next(1, 11);
							string appendKey = $"hud.exhaust.append.{appendIndex}";
							string appendText = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(appendKey));
							if (string.IsNullOrWhiteSpace(appendText) || appendText == appendKey)
							{
								appendText = "但这场对决，它还想继续";
							}
							text += appendText;
						}
						AddTip(data.OtherTips, text, (float)___xPositionOnScreen + 14f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f, centered: false, rightAligned: true);
						FishingLog.Log($"[BobberBar] 力竭节点 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 节点: {node.Item1:0}分钟 ({node.Item2:P0}) | 耗时: {data.BattleElapsedSeconds:F0}s | 挑战鱼饵: {data.HasChallengeBait} | 文案: {text}", (LogLevel)2);
					}
					data.EffectiveDifficulty = (data.HasChallengeBait ? data.AdjustedDifficulty : DifficultyCalculator.GetExhaustedDifficulty(data.AdjustedDifficulty, data.BattleElapsedSeconds));
					if (Math.Abs(___difficulty - data.EffectiveDifficulty) > 0.001f)
					{
						___difficulty = data.EffectiveDifficulty;
					}
				}
				else
				{
					data.EffectiveDifficulty = data.AdjustedDifficulty;
				}
			}
			else
			{
				data.EffectiveDifficulty = data.AdjustedDifficulty;
			}
			if (data.IsIdle)
			{
				___bobberSpeed = 0f;
				___bobberTargetPosition = ___bobberPosition;
				___floaterSinkerAcceleration = 0f;
			}
			if (data.DifficultyLevel > 0)
			{
				___fishSizeReductionTimer = 800;
			}
			if (data.HasChallengeBait && data.AdjustedDifficulty > 100f)
			{
				int targetStars = GetChallengeStars(data.DifficultyLevel, data.BattleElapsedSeconds);
				if (targetStars < data.LastChallengeStarsLogged)
				{
					data.LastChallengeStarsLogged = targetStars;
					FishingLog.Log($"[BobberBar] 挑战星减少 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 剩余星星: {targetStars}/3 | 鱼获惩罚: -{20 * (3 - targetStars)}% | 无法恢复", (LogLevel)2);
				}
				___challengeBaitFishes = 3;
			}
			if (data.WasBobberInBar && !___bobberInBar)
			{
				data.MissCount++;
			}
			data.WasBobberInBar = ___bobberInBar;
			float oldModifier = ___distanceFromCatchPenaltyModifier;
			if (Math.Abs(oldModifier - data.LastAppliedCatchPenaltyModifier) > 0.0001f)
			{
				data.NativeCatchPenaltyModifier = oldModifier;
			}
			float protectionProgress = (data.ProtectionEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
			float newModifier = DifficultyCalculator.GetCatchPenaltyModifier(data.DifficultyLevel, protectionProgress);
			bool escapeBonusActive = false;
			int consecutiveFails = 0;
			if (data.AdjustedDifficulty > 100f)
			{
				consecutiveFails = DifficultyManager.GetConsecutiveFailCount(data.FishId, data.Owner ?? Game1.player);
				if (consecutiveFails > 0)
				{
					float escapeProgress = (data.EscapeBonusEngaged ? Math.Max(0f, ___distanceFromCatching - 0.005f) : Math.Min(1f, ___distanceFromCatching + 0.005f));
					newModifier = DifficultyCalculator.GetEscapeFailBonusModifier(data.DifficultyLevel, consecutiveFails, escapeProgress);
					escapeBonusActive = newModifier < DifficultyCalculator.GetCatchPenaltyModifier(data.DifficultyLevel, escapeProgress) - 0.0001f;
				}
			}
			if (escapeBonusActive && !data.EscapeBonusEngaged)
			{
				data.EscapeBonusEngaged = true;
				FishingLog.Log($"[BobberBar] 逃逸减速加成生效 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 连续失败: {consecutiveFails} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {newModifier:F2}", (LogLevel)1);
			}
			else if (!escapeBonusActive && data.EscapeBonusEngaged)
			{
				data.EscapeBonusEngaged = false;
				FishingLog.Log($"[BobberBar] 逃逸减速加成解除 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 减速倍率: {newModifier:F2}", (LogLevel)1);
			}
			float combinedModifier = Math.Min(data.NativeCatchPenaltyModifier, newModifier);
			if (data.IsIdle)
			{
				combinedModifier = 0f;
			}
			___distanceFromCatchPenaltyModifier = combinedModifier;
			data.LastAppliedCatchPenaltyModifier = combinedModifier;
			bool protectionActive = combinedModifier < data.NativeCatchPenaltyModifier - 0.0001f;
			if (protectionActive && !data.ProtectionEngaged)
			{
				data.ProtectionEngaged = true;
				FishingLog.Log($"[BobberBar] 蓄力槽保护生效 | 实例: {((object)__instance).GetHashCode()} | 等级: {data.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {oldModifier:F2} → {combinedModifier:F2}", (LogLevel)1);
			}
			else if (!protectionActive && data.ProtectionEngaged)
			{
				data.ProtectionEngaged = false;
				FishingLog.Log($"[BobberBar] 蓄力槽保护解除 | 实例: {((object)__instance).GetHashCode()} | 等级: {data.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {combinedModifier:F2} → {oldModifier:F2}", (LogLevel)1);
			}
			if ((data.EffectiveDifficulty >= 150f || data.JumpPending) && !data.ResultStarted && !data.IsIdle && data.PhraseMode != PhraseMode.Playing)
			{
				if (data.JumpPending)
				{
					if (data.JumpWindupActive)
					{
						data.JumpWindupSeconds -= dt;
						___bobberSpeed = 0f;
						___bobberTargetPosition = ___bobberPosition;
						___floaterSinkerAcceleration = 0f;
						if (data.JumpWindupSeconds <= 0f)
						{
							___bobberPosition = data.JumpPendingTarget;
							___bobberTargetPosition = data.JumpPendingTarget;
							___bobberSpeed = 0f;
							data.JumpPending = false;
							data.JumpWindupActive = false;
							data.JumpCooldownSeconds = data.JumpIntervalSeconds;
							data.JumpDetectionSeconds = 0f;
							if (data.EffectiveDifficulty >= 150f)
							{
								if (data.PhraseMode == PhraseMode.Free)
								{
									data.PhraseMode = PhraseMode.WaitingMiddle;
								}
								else if (data.PhraseMode == PhraseMode.Recording)
								{
									data.PhraseJumpSeen = true;
								}
							}
						}
					}
					else
					{
						data.JumpPendingSeconds -= dt;
						if (data.JumpPendingSeconds <= 0f)
						{
							data.JumpWindupActive = true;
							data.JumpWindupSeconds = 0.88f;
							data.JumpWindupStartPosition = ___bobberPosition;
							if (data.PhraseMode == PhraseMode.Recording)
							{
								data.PhraseWindupStartTime = data.PhraseRecordTime;
							}
							___bobberSpeed = 0f;
							___bobberTargetPosition = ___bobberPosition;
							___floaterSinkerAcceleration = 0f;
						}
					}
				}
				else if (data.JumpCooldownSeconds > 0f)
				{
					data.JumpCooldownSeconds -= dt;
					if (data.JumpCooldownSeconds < 0f)
					{
						data.JumpCooldownSeconds = 0f;
					}
				}
				else
				{
					data.JumpDetectionSeconds -= dt;
					if (data.JumpDetectionSeconds <= 0f)
					{
						data.JumpDetectionSeconds = 1f;
						float fishPosition = ___bobberPosition;
						if (fishPosition >= 399f)
						{
							data.JumpPending = true;
							data.JumpPendingSeconds = 0.5f;
							data.JumpPendingTarget = Game1.random.Next(0, 134);
						}
						else if (fishPosition <= 133f)
						{
							data.JumpPending = true;
							data.JumpPendingSeconds = 0.5f;
							data.JumpPendingTarget = Game1.random.Next(399, 533);
						}
						if (data.JumpPending)
						{
							bool jumpUp = data.JumpPendingTarget <= 133f;
							string jumpText = PickJumpText(jumpUp);
							AddTip(data.ActionTips, jumpText, (float)___xPositionOnScreen + 124f, (float)___yPositionOnScreen + 36f + ___bobberPosition - 30f, centered: false, rightAligned: false, actionTip: true);
							if (data.PhraseMode == PhraseMode.Recording)
							{
								data.PhraseJumpIsUp = jumpUp;
								data.PhraseJumpText = jumpText;
							}
							FishingLog.Log($"[BobberBar] 高难度鱼跳 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 难度: {data.EffectiveDifficulty:F0} | 跳至: {data.JumpPendingTarget:F0} | 文案: {(jumpUp ? "上跳(鱼跃)" : "下跳(甩尾)")}: {jumpText}", (LogLevel)2);
						}
					}
				}
			}
			HandlePhrasePrefix(__instance, data, ref ___bobberPosition, ref ___bobberSpeed, ref ___bobberTargetPosition, ref ___floaterSinkerAcceleration, dt);
		}
		catch (Exception value)
		{
			FishingLog.LogRateLimited("BobberBar.Update_Prefix", $"BobberBar.update Prefix 失败: {value}", (LogLevel)4);
		}
	}

	public static int GetMissCount(BobberBar instance)
	{
		if (_instanceData.TryGetValue(instance, out var data))
		{
			return data.MissCount;
		}
		return 0;
	}

	public static float GetAdjustedDifficulty(BobberBar instance)
	{
		if (!_instanceData.TryGetValue(instance, out var data))
		{
			return 0f;
		}
		return data.AdjustedDifficulty;
	}

	public static bool HasChallengeBait(BobberBar instance)
	{
		if (_instanceData.TryGetValue(instance, out var data))
		{
			return data.HasChallengeBait;
		}
		return false;
	}

	public static float GetElapsedSeconds(BobberBar instance)
	{
		if (!_instanceData.TryGetValue(instance, out var data))
		{
			return 0f;
		}
		return data.BattleElapsedSeconds;
	}

	private static void TryGrantPerseveranceReward(InstanceData data)
	{
		try
		{
			if (data == null || (data.Owner == null && Game1.player == null))
			{
				return;
			}
			float forcedSeconds = data.ForcedPerseveranceSeconds;
			float elapsed = ((forcedSeconds > 0f) ? forcedSeconds : data.BattleElapsedSeconds);
			string itemId;
			if (elapsed >= 60f)
			{
				itemId = "(O)265";
			}
			else
			{
				if (!(elapsed >= 30f) || !(Game1.random.NextDouble() < 0.5))
				{
					return;
				}
				itemId = PerseverancePlusThreeFoods[Game1.random.Next(PerseverancePlusThreeFoods.Length)];
			}
			Farmer owner = data.Owner ?? Game1.player;
			Item item = ItemRegistry.Create(itemId, 1, 0, false);
			owner.addItemByMenuIfNecessary(item, (behaviorOnItemSelect)null, false);
			HUDNotifier.ShowPerseveranceRewardNotification(data.FishId, data.DifficultyLevel);
			FishingLog.Log($"[BobberBar] 持久战奖励 | 实例: {data.GetHashCode()} | 鱼ID: {data.FishId} | 耗时: {elapsed:F0}s | 奖励: {item.DisplayName} | 玩家: {owner.UniqueMultiplayerID}" + ((forcedSeconds > 0f) ? " | 测试强制" : ""), (LogLevel)2);
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
			if (!_instanceData.TryGetValue(__instance, out var data))
			{
				return;
			}
			if (data.PatternRandom != null && data.SavedGameRandom != null)
			{
				Game1.random = data.SavedGameRandom;
				data.SavedGameRandom = null;
			}
			float barDelta = Math.Abs(___bobberBarPos - data.LastBarPos);
			data.LastBarPos = ___bobberBarPos;
			float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f)
			{
				dt = 1f / 60f;
			}
			if (barDelta > 0.5f)
			{
				data.IdleSeconds = 0f;
				data.IdlePending = false;
				if (data.IsIdle)
				{
					data.IsIdle = false;
				}
			}
			else if (!data.IsIdle && !data.IdlePending)
			{
				data.IdleSeconds += dt;
				if (data.IdleSeconds >= 3f)
				{
					data.IdlePending = true;
				}
			}
			if (data.IsIdle)
			{
				___bobberPosition = data.IdleFrozenPosition;
			}
			else if (data.JumpWindupActive && data.PhraseMode != PhraseMode.Playing)
			{
				___bobberPosition = data.JumpWindupStartPosition;
			}
			HandlePhrasePostfix(data, ___bobberPosition, ___distanceFromCatching);
			if (___fadeOut && ___distanceFromCatching <= 0f && !data.FailureRecorded)
			{
				FishingLog.Log($"[BobberBar] 钓鱼失败 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {data.FishId} | 蓄力槽耗尽: {___distanceFromCatching:F3}", (LogLevel)2);
				DifficultyManager.RecordFailure(data.FishId, data.Owner ?? Game1.player, data.HasChallengeBait);
				int newLevel = DifficultyManager.GetDifficultyLevel(data.FishId, data.Owner ?? Game1.player);
				bool isEpicChampion = data.AdjustedDifficulty >= 150f && DifficultyManager.GetConsecutiveFailCount(data.FishId, data.Owner ?? Game1.player) >= 2;
				HUDNotifier.ShowFailureNotification(data.FishId, newLevel, isEpicChampion);
				TryGrantPerseveranceReward(data);
				data.FailureRecorded = true;
				data.ResultStarted = true;
			}
			if (___fadeOut && ___distanceFromCatching >= 1f)
			{
				data.ResultStarted = true;
			}
			if (data.ResultStarted && !___fadeOut)
			{
				CleanupInstance(__instance);
			}
		}
		catch (Exception value)
		{
			FishingLog.LogRateLimited("BobberBar.Update_Postfix", $"BobberBar.update Postfix 失败: {value}", (LogLevel)4);
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
		List<CodeInstruction> codes = instructions.ToList();
		FieldInfo difficultyField = AccessTools.Field(typeof(BobberBar), "difficulty");
		FieldInfo motionTypeField = AccessTools.Field(typeof(BobberBar), "motionType");
		FieldInfo bobberAccelerationField = AccessTools.Field(typeof(BobberBar), "bobberAcceleration");
		FieldInfo bobberSpeedField = AccessTools.Field(typeof(BobberBar), "bobberSpeed");
		FieldInfo bobberPositionField = AccessTools.Field(typeof(BobberBar), "bobberPosition");
		FieldInfo bobberBarSpeedField = AccessTools.Field(typeof(BobberBar), "bobberBarSpeed");
		FieldInfo bobberBarPosField = AccessTools.Field(typeof(BobberBar), "bobberBarPos");
		FieldInfo floaterSinkerAccelerationField = AccessTools.Field(typeof(BobberBar), "floaterSinkerAcceleration");
		MethodInfo mathMinMethod = typeof(Math).GetMethod("Min", new Type[2]
		{
			typeof(float),
			typeof(float)
		});
		MethodInfo accelerationBoostMethod = typeof(BobberBarPatches).GetMethod("GetAccelerationBoost", BindingFlags.Static | BindingFlags.Public);
		MethodInfo frameScaleMethod = typeof(BobberBarPatches).GetMethod("GetFrameScale", BindingFlags.Static | BindingFlags.Public);
		MethodInfo smoothFactorMethod = typeof(BobberBarPatches).GetMethod("GetSmoothFactor", BindingFlags.Static | BindingFlags.Public);
		MethodInfo scaleProbabilityMethod = typeof(BobberBarPatches).GetMethod("ScaleProbability", BindingFlags.Static | BindingFlags.Public);
		MethodInfo applyBarInputMethod = typeof(BobberBarPatches).GetMethod("ApplyBarInput", BindingFlags.Static | BindingFlags.Public);
		MethodInfo applyBarPositionMethod = typeof(BobberBarPatches).GetMethod("ApplyBarPosition", BindingFlags.Static | BindingFlags.Public);
		MethodInfo applyFishPositionMethod = typeof(BobberBarPatches).GetMethod("ApplyFishPosition", BindingFlags.Static | BindingFlags.Public);
		MethodInfo applyBounceMethod = typeof(BobberBarPatches).GetMethod("ApplyBounce", BindingFlags.Static | BindingFlags.Public);
		CodeInstruction[] difficultyCap = (CodeInstruction[])(object)new CodeInstruction[2]
		{
			new CodeInstruction(OpCodes.Ldc_R4, (object)150f),
			new CodeInstruction(OpCodes.Call, (object)mathMinMethod)
		};
		for (int i = 0; i < codes.Count; i++)
		{
			CodeInstruction current = codes[i];
			CodeInstruction next = ((i + 1 < codes.Count) ? codes[i + 1] : null);
			CodeInstruction nextNext = ((i + 2 < codes.Count) ? codes[i + 2] : null);
			if (current.opcode == OpCodes.Ldfld && object.Equals(current.operand, difficultyField))
			{
				if ((next != null && next.opcode == OpCodes.Ldfld && object.Equals(next.operand, motionTypeField)) || (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 2000f)) || (next != null && next.opcode == OpCodes.Ldc_R4 && IsFloat(next.operand, 1000f)) || (next != null && next.opcode == OpCodes.Conv_I4 && nextNext != null && nextNext.opcode == OpCodes.Ldc_I4_2))
				{
					codes.InsertRange(i + 1, difficultyCap);
					i += difficultyCap.Length;
				}
			}
			else if (current.opcode == OpCodes.Stfld && object.Equals(current.operand, bobberAccelerationField) && i > 0)
			{
				codes.InsertRange(i, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[5]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Dup, (object)null),
					new CodeInstruction(OpCodes.Ldfld, (object)difficultyField),
					new CodeInstruction(OpCodes.Call, (object)accelerationBoostMethod),
					new CodeInstruction(OpCodes.Mul, (object)null)
				});
				i += 5;
			}
			else if (current.opcode == OpCodes.Ldc_R4 && (IsFloat(current.operand, 4000f) || IsFloat(current.operand, 2000f) || IsFloat(current.operand, 1000f)) && next != null && next.opcode == OpCodes.Div)
			{
				codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, (object)scaleProbabilityMethod));
				i++;
			}
			else if (current.opcode == OpCodes.Ldc_R4 && IsFloat(current.operand, 0.01f) && i > 0 && codes[i - 1].opcode == OpCodes.Ldfld && object.Equals(codes[i - 1].operand, floaterSinkerAccelerationField))
			{
				codes.InsertRange(i + 1, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
				{
					new CodeInstruction(OpCodes.Call, (object)frameScaleMethod),
					new CodeInstruction(OpCodes.Mul, (object)null)
				});
				i += 2;
			}
			else if (current.opcode == OpCodes.Ldc_R4 && IsFloat(current.operand, 5f) && next != null && next.opcode == OpCodes.Div && nextNext != null && nextNext.opcode == OpCodes.Add)
			{
				codes.InsertRange(i + 2, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
				{
					new CodeInstruction(OpCodes.Call, (object)smoothFactorMethod),
					new CodeInstruction(OpCodes.Mul, (object)null)
				});
				i += 2;
			}
			else if (current.opcode == OpCodes.Add && next != null && next.opcode == OpCodes.Stfld && object.Equals(next.operand, bobberPositionField))
			{
				codes[i] = new CodeInstruction(OpCodes.Call, (object)applyFishPositionMethod);
			}
			else if (current.opcode == OpCodes.Add && i > 3 && IsLocalIndex(codes[i - 1], 4) && next != null && next.opcode == OpCodes.Stfld && object.Equals(next.operand, bobberBarSpeedField) && codes[i - 2].opcode == OpCodes.Ldfld && object.Equals(codes[i - 2].operand, bobberBarSpeedField) && codes[i - 3].opcode == OpCodes.Ldarg_0 && codes[i - 4].opcode == OpCodes.Ldarg_0)
			{
				CodeInstruction ldlocNum5 = codes[i - 1];
				codes.RemoveRange(i - 4, 5);
				codes.InsertRange(i - 4, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[6]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Dup, (object)null),
					new CodeInstruction(OpCodes.Dup, (object)null),
					new CodeInstruction(OpCodes.Ldfld, (object)bobberBarSpeedField),
					ldlocNum5,
					new CodeInstruction(OpCodes.Call, (object)applyBarInputMethod)
				});
			}
			else if (current.opcode == OpCodes.Add && next != null && next.opcode == OpCodes.Stfld && object.Equals(next.operand, bobberBarPosField))
			{
				codes[i] = new CodeInstruction(OpCodes.Call, (object)applyBarPositionMethod);
			}
			else if (current.opcode == OpCodes.Div && i > 7 && codes[i - 1].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 1].operand, 3f) && codes[i - 2].opcode == OpCodes.Mul && codes[i - 3].opcode == OpCodes.Ldc_R4 && IsFloat(codes[i - 3].operand, 2f) && codes[i - 4].opcode == OpCodes.Neg && codes[i - 5].opcode == OpCodes.Ldfld && object.Equals(codes[i - 5].operand, bobberBarSpeedField) && codes[i - 6].opcode == OpCodes.Ldarg_0 && codes[i - 7].opcode == OpCodes.Ldarg_0)
			{
				codes.RemoveRange(i - 7, 8);
				codes.InsertRange(i - 7, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[10]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Dup, (object)null),
					new CodeInstruction(OpCodes.Dup, (object)null),
					new CodeInstruction(OpCodes.Ldfld, (object)bobberBarSpeedField),
					new CodeInstruction(OpCodes.Neg, (object)null),
					new CodeInstruction(OpCodes.Ldc_R4, (object)2f),
					new CodeInstruction(OpCodes.Mul, (object)null),
					new CodeInstruction(OpCodes.Ldc_R4, (object)3f),
					new CodeInstruction(OpCodes.Div, (object)null),
					new CodeInstruction(OpCodes.Call, (object)applyBounceMethod)
				});
			}
		}
		return codes;
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
		List<CodeInstruction> codes = instructions.ToList();
		MethodInfo drawMethod = AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[9]
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
		MethodInfo rotationMethod = typeof(BobberBarPatches).GetMethod("GetFishIconRotation", BindingFlags.Static | BindingFlags.Public);
		MethodInfo whiteGetter = AccessTools.PropertyGetter(typeof(Color), "White");
		MethodInfo colorMethod = typeof(BobberBarPatches).GetMethod("GetFishIconColor", BindingFlags.Static | BindingFlags.Public);
		ConstructorInfo vector2Ctor = typeof(Vector2).GetConstructor(new Type[2]
		{
			typeof(float),
			typeof(float)
		});
		for (int i = 0; i < codes.Count; i++)
		{
			if (codes[i].opcode != OpCodes.Callvirt || !object.Equals(codes[i].operand, drawMethod) || i < 8 || !(codes[i - 1].opcode == OpCodes.Ldc_R4) || !IsFloat(codes[i - 1].operand, 0.88f) || !(codes[i - 2].opcode == OpCodes.Ldc_I4_0) || !(codes[i - 3].opcode == OpCodes.Ldc_R4) || !IsFloat(codes[i - 3].operand, 2f) || !(codes[i - 4].opcode == OpCodes.Newobj) || !object.Equals(codes[i - 4].operand, vector2Ctor) || !(codes[i - 5].opcode == OpCodes.Ldc_R4) || !IsFloat(codes[i - 5].operand, 10f) || !(codes[i - 6].opcode == OpCodes.Ldc_R4) || !IsFloat(codes[i - 6].operand, 10f) || !(codes[i - 7].opcode == OpCodes.Ldc_R4) || !IsFloat(codes[i - 7].operand, 0f) || !(codes[i - 8].opcode == OpCodes.Call) || !object.Equals(codes[i - 8].operand, whiteGetter))
			{
				continue;
			}
			bool isFish = false;
			for (int k = i - 9; k >= Math.Max(0, i - 35); k--)
			{
				if (codes[k].opcode == OpCodes.Ldc_I4 && object.Equals(codes[k].operand, 1840))
				{
					isFish = true;
					break;
				}
			}
			if (isFish)
			{
				codes.RemoveAt(i - 8);
				codes.InsertRange(i - 8, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Call, (object)colorMethod)
				});
				codes.RemoveAt(i - 6);
				codes.InsertRange(i - 6, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[2]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Call, (object)rotationMethod)
				});
				i += 2;
			}
		}
		return codes;
	}

	private static bool IsLocalIndex(CodeInstruction code, int index)
	{
		if (code.opcode != OpCodes.Ldloc && code.opcode != OpCodes.Ldloc_S)
		{
			return false;
		}
		if (code.operand is LocalBuilder local)
		{
			return local.LocalIndex == index;
		}
		return false;
	}

	private static bool IsFloat(object operand, float value)
	{
		if (operand is float f)
		{
			return Math.Abs(f - value) < 0.001f;
		}
		return false;
	}

	public static float GetAccelerationBoost(BobberBar instance, float difficulty)
	{
		if (difficulty <= 100f)
		{
			return 1f;
		}
		InstanceData data;
		int level = (_instanceData.TryGetValue(instance, out data) ? data.DifficultyLevel : 100);
		float tier = GetAccelerationTier(level);
		return 1f + tier * (difficulty - 100f) / 100f;
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
		if (!_instanceData.TryGetValue(instance, out var data))
		{
			return 0f;
		}
		return data.Alpha;
	}

	public static int GetChallengeStars(int difficultyLevel, float elapsedSeconds)
	{
		if (difficultyLevel >= 95)
		{
			return 3;
		}
		float overFiveMinutes = elapsedSeconds - 300f;
		if (overFiveMinutes < 0f)
		{
			return 3;
		}
		return Math.Max(0, 3 - ((int)Math.Floor(overFiveMinutes / 60f) + 1));
	}

	public static float GetChallengeStarMultiplier(int stars)
	{
		return 1f - 0.2f * (float)(3 - stars);
	}

	public static float GetFrameScale()
	{
		float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
		if (dt <= 0f)
		{
			return 1f;
		}
		float k = dt * 60f;
		if (!(Math.Abs(k - 1f) < 0.0001f))
		{
			return k;
		}
		return 1f;
	}

	public static float GetSmoothFactor()
	{
		float k = GetFrameScale();
		if (Math.Abs(k - 1f) < 0.0001f)
		{
			return 0.2f;
		}
		return (float)(1.0 - Math.Pow(0.8, k));
	}

	public static float ScaleProbability(float probability)
	{
		float k = GetFrameScale();
		if (Math.Abs(k - 1f) < 0.0001f)
		{
			return probability;
		}
		double p = Math.Max(0.0, Math.Min(1.0, probability));
		return (float)(1.0 - Math.Pow(1.0 - p, k));
	}

	public static float ApplyBarInput(BobberBar instance, float speed, float num5)
	{
		float k = GetFrameScale();
		float alpha = GetAlpha(instance);
		if (alpha <= 0f)
		{
			return speed + num5 * k;
		}
		float baseMaxSpeed = 30f - 5f * alpha;
		float directionFactor = GetDirectionFactor(instance, num5, alpha);
		float targetSpeed = ((num5 < 0f) ? (0f - baseMaxSpeed) : baseMaxSpeed) * directionFactor;
		if (_instanceData.TryGetValue(instance, out var data) && !data.BarInputDiagnosticLogged)
		{
			data.BarInputDiagnosticLogged = true;
			FishingLog.Log($"[BobberBar] 手感系统激活 | 实例: {((object)instance).GetHashCode()} | α: {alpha:P0} | 方向系数: {directionFactor:F2} | 目标速度: {targetSpeed:F1}px/帧 | 原生分量: {speed + num5 * k:F1}", (LogLevel)2);
		}
		return (1f - alpha) * (speed + num5 * k) + alpha * targetSpeed;
	}

	private static float GetDirectionFactor(BobberBar instance, float num5, float alpha)
	{
		if (num5 == 0f || instance == null)
		{
			return 1f;
		}
		float barCenter = instance.bobberBarPos + (float)instance.bobberBarHeight / 2f;
		if (!((num5 < 0f) ? (barCenter > instance.bobberPosition) : (barCenter < instance.bobberPosition)))
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
			bool forced = ModEntry.ConsumeForceAssistFlag();
			if (data.HasChallengeBait)
			{
				return;
			}
			double chance = DifficultyCalculator.GetAssistChance(DifficultyManager.GetCountableCrownCount(player), 61);
			if (forced || (!(chance <= 0.0) && !(Game1.random.NextDouble() >= chance)))
			{
				List<string> starredFish = DifficultyManager.GetCountableStarredFish(player);
				if (starredFish.Count != 0)
				{
					string assistFishId = starredFish[Game1.random.Next(starredFish.Count)];
					double assistRank = DifficultyManager.GetAssistRank(assistFishId, player, starredFish);
					int assistLevel = DifficultyCalculator.GetRandomAssistLevel(assistRank);
					RecordAssistObservation(assistFishId, assistRank, assistLevel);
					int oldHeight = bobberBarHeight;
					bobberBarHeight += assistLevel * 8;
					data.AssistLevel = assistLevel;
					data.AssistFishId = assistFishId;
					string assistText = PickAssistText(assistFishId, DifficultyManager.GetDifficultyLevel(assistFishId, player));
					AddTip(data.OtherTips, assistText, tipX, tipY, centered: false, rightAligned: true);
					FishingLog.Log($"[BobberBar] 助战触发 | 助战鱼: {assistFishId} | 难度: {DifficultyManager.GetDifficultyLevel(assistFishId, player)} | 排位: {assistRank:P0} | 临时钓鱼等级: +{assistLevel} | 绿条高度: {oldHeight} → {bobberBarHeight} | 总概率: {chance:P1}{(forced ? " | 测试强制触发" : "")} | 文案: {assistText}", (LogLevel)2);
				}
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"BobberBar 助战判定失败: {value}", (LogLevel)4);
		}
	}

	private static string PickAssistText(string fishId, int level)
	{
		try
		{
			Item obj = ItemRegistry.Create(fishId, 1, 0, false);
			string fishName = ((obj != null) ? obj.DisplayName : null) ?? fishId;
			string rankName = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
			int index = Game1.random.Next(1, 21);
			string key = $"hud.assist.{index}";
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(key, (object)new { fishName, rankName }));
			return (string.IsNullOrWhiteSpace(text) || text == key) ? ("荣耀的" + fishName + rankName + "前来护驾！") : text;
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
			int index = Game1.random.Next(1, 16);
			string key = (jumpUp ? $"hud.jump.up.{index}" : $"hud.jump.down.{index}");
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(key));
			return (!string.IsNullOrWhiteSpace(text) && !(text == key)) ? text : (jumpUp ? "鱼跃！" : "甩尾！");
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
			string rankName = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(DifficultyCalculator.GetRankKey(level)));
			int index = Game1.random.Next(1, 11);
			string key = $"hud.peak.{index}";
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(key, (object)new { rankName }));
			return (string.IsNullOrWhiteSpace(text) || text == key) ? ("[" + rankName + "]的力气达到巅峰") : text;
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
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!_instanceData.TryGetValue(__instance, out var data))
			{
				return;
			}
			if (data.HasChallengeBait && data.AdjustedDifficulty > 100f)
			{
				int stars = GetChallengeStars(data.DifficultyLevel, data.BattleElapsedSeconds);
				if (stars < 3)
				{
					int num2 = (((float)((IClickableMenu)__instance).xPositionOnScreen > (float)((Rectangle)(ref Game1.viewport)).Width * 0.75f) ? (((IClickableMenu)__instance).xPositionOnScreen - 80) : (((IClickableMenu)__instance).xPositionOnScreen + 216));
					int num3 = (__instance.bobbers.Contains("(O)SonarBobber") ? (((IClickableMenu)__instance).yPositionOnScreen + 136) : (((IClickableMenu)__instance).yPositionOnScreen + 40));
					Rectangle emptyStar = default(Rectangle);
					((Rectangle)(ref emptyStar))..ctor(217, 205, 19, 19);
					for (int i = stars; i < 3; i++)
					{
						b.Draw(Game1.mouseCursors_1_6, new Vector2((float)(num2 - 12), (float)(num3 + i * 40)) + __instance.everythingShake, (Rectangle?)emptyStar, Color.White, 0f, Vector2.Zero, 2f, (SpriteEffects)0, 0.89f);
					}
				}
			}
			foreach (FloatingTip tip in data.ActionTips)
			{
				DrawTip(b, tip);
			}
			foreach (FloatingTip tip2 in data.OtherTips)
			{
				DrawTip(b, tip2);
			}
		}
		catch (Exception value)
		{
			FishingLog.LogRateLimited("BobberBar.Draw_Postfix", $"BobberBar 小游戏文案绘制失败: {value}", (LogLevel)4);
		}
	}

	private static float GetJumpWindupRotation(InstanceData data, bool jumpUp)
	{
		float elapsed = 0.88f - Math.Max(0f, data.JumpWindupSeconds);
		float angle = ((!(elapsed <= 0.77f)) ? (150f * (1f - (elapsed - 0.77f) / 0.11f)) : (150f * (elapsed / 0.77f)));
		return (jumpUp ? (-1f) : 1f) * angle;
	}

	public static float GetFishIconRotation(BobberBar instance)
	{
		if (!_instanceData.TryGetValue(instance, out var data))
		{
			return 0f;
		}
		if (data.IsIdle)
		{
			double swayMs = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			return (float)(Math.Sin(swayMs / 1000.0 * Math.PI * 2.0) * 0.09);
		}
		if (data.JumpWindupActive)
		{
			bool jumpUp = data.JumpPendingTarget <= 133f;
			return MathHelper.ToRadians(GetJumpWindupRotation(data, jumpUp));
		}
		return 0f;
	}

	public static Color GetFishIconColor(BobberBar instance)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (_instanceData.TryGetValue(instance, out var data) && data.JumpWindupActive)
		{
			double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			float pulse = (float)(0.5 + 0.5 * Math.Sin(ms / 180.0 * Math.PI * 2.0));
			return Color.Lerp(Color.Red, Color.White, 0.2f + 0.3f * pulse);
		}
		return Color.White;
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
			float playPos = (position = InterpolatePhrase(data, data.PhrasePlayTime));
			speed = 0f;
			target = playPos;
			floaterSinker = 0f;
			if (!data.PlaybackWindupShown && data.PhraseWindupStartTime >= 0f && data.PhrasePlayTime >= data.PhraseWindupStartTime)
			{
				data.PlaybackWindupShown = true;
				data.JumpWindupActive = true;
				data.JumpWindupSeconds = 0.88f;
				data.JumpPendingTarget = (data.PhraseJumpIsUp ? 0f : 500f);
				if (!string.IsNullOrEmpty(data.PhraseJumpText))
				{
					AddTip(data.ActionTips, data.PhraseJumpText, (float)((IClickableMenu)instance).xPositionOnScreen + 124f, (float)((IClickableMenu)instance).yPositionOnScreen + 36f + playPos - 30f, centered: false, rightAligned: false, actionTip: true);
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
		bool inMiddle = position >= 256f && position <= 276f;
		if (data.PhraseMode == PhraseMode.WaitingMiddle && inMiddle)
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
			float dt = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f)
			{
				dt = 1f / 60f;
			}
			data.PhraseRecordTime += dt;
			data.PhraseSamples.Add((data.PhraseRecordTime, position));
			if (data.PhraseJumpSeen && inMiddle)
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
		List<(float, float)> samples = data.PhraseSamples;
		if (samples.Count == 0)
		{
			return 0f;
		}
		if (t <= samples[0].Item1)
		{
			return samples[0].Item2;
		}
		for (int i = 0; i < samples.Count - 1; i++)
		{
			float t2 = samples[i].Item1;
			float t3 = samples[i + 1].Item1;
			if (t >= t2 && t <= t3)
			{
				float frac = ((t3 > t2) ? ((t - t2) / (t3 - t2)) : 0f);
				return samples[i].Item2 + (samples[i + 1].Item2 - samples[i].Item2) * frac;
			}
		}
		return samples[samples.Count - 1].Item2;
	}

	private static string PickIdleText()
	{
		try
		{
			int index = Game1.random.Next(1, 11);
			string key = $"hud.idle.{index}";
			string text = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(key));
			return (string.IsNullOrWhiteSpace(text) || text == key) ? "它停下来歇口气" : text;
		}
		catch (Exception ex)
		{
			FishingLog.Log("[BobberBar] 停战文案生成失败: " + ex.Message, (LogLevel)4);
			return "它停下来歇口气";
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
