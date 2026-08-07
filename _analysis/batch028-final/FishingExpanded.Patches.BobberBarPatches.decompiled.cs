using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches;

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

		public float JumpIntervalSeconds { get; set; }

		public float JumpCooldownSeconds { get; set; }

		public float JumpDetectionSeconds { get; set; }

		public bool JumpPending { get; set; }

		public float JumpPendingSeconds { get; set; }

		public float JumpPendingTarget { get; set; }
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
	public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberTargetPosition, bool ___bobberInBar)
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
			if (instanceData.AdjustedDifficulty > 100f)
			{
				___bobberTargetPosition = 0f;
			}
			instanceData.JumpIntervalSeconds = GetJumpInterval(instanceData.AdjustedDifficulty);
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
	public static void Update_Prefix(BobberBar __instance, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberPosition, ref float ___bobberTargetPosition, ref float ___bobberSpeed, float ___distanceFromCatching, bool ___bobberInBar)
	{
		try
		{
			if (!_instanceData.TryGetValue(__instance, out var value))
			{
				return;
			}
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
			if (!(value.AdjustedDifficulty >= 150f) || value.ResultStarted)
			{
				return;
			}
			float num4 = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (num4 <= 0f)
			{
				num4 = 1f / 60f;
			}
			if (value.JumpPending)
			{
				value.JumpPendingSeconds -= num4;
				if (value.JumpPendingSeconds <= 0f)
				{
					___bobberPosition = value.JumpPendingTarget;
					___bobberTargetPosition = value.JumpPendingTarget;
					___bobberSpeed = 0f;
					value.JumpPending = false;
					value.JumpCooldownSeconds = value.JumpIntervalSeconds;
					value.JumpDetectionSeconds = 0f;
					ModEntry.ModMonitor.Log($"[BobberBar] 高难度鱼跳 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 难度: {value.AdjustedDifficulty:F0} | 跳至: {value.JumpPendingTarget:F0}", (LogLevel)2);
				}
				return;
			}
			if (value.JumpCooldownSeconds > 0f)
			{
				value.JumpCooldownSeconds -= num4;
				if (value.JumpCooldownSeconds < 0f)
				{
					value.JumpCooldownSeconds = 0f;
				}
				return;
			}
			value.JumpDetectionSeconds -= num4;
			if (value.JumpDetectionSeconds <= 0f)
			{
				value.JumpDetectionSeconds = 1f;
				float num5 = ___bobberPosition;
				if (num5 >= 399f)
				{
					value.JumpPending = true;
					value.JumpPendingSeconds = 0.5f;
					value.JumpPendingTarget = Game1.random.Next(0, 134);
				}
				else if (num5 <= 133f)
				{
					value.JumpPending = true;
					value.JumpPendingSeconds = 0.5f;
					value.JumpPendingTarget = Game1.random.Next(399, 533);
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

	[HarmonyPatch("update")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Expected O, but got Unknown
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Expected O, but got Unknown
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Expected O, but got Unknown
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		FieldInfo fieldInfo = AccessTools.Field(typeof(BobberBar), "difficulty");
		FieldInfo objB = AccessTools.Field(typeof(BobberBar), "motionType");
		FieldInfo objB2 = AccessTools.Field(typeof(BobberBar), "bobberAcceleration");
		MethodInfo method = typeof(Math).GetMethod("Min", new Type[2]
		{
			typeof(float),
			typeof(float)
		});
		MethodInfo method2 = typeof(BobberBarPatches).GetMethod("GetAccelerationBoost", BindingFlags.Static | BindingFlags.Public);
		CodeInstruction[] array = (CodeInstruction[])(object)new CodeInstruction[2]
		{
			new CodeInstruction(OpCodes.Ldc_R4, (object)150f),
			new CodeInstruction(OpCodes.Call, (object)method)
		};
		for (int i = 0; i < list.Count; i++)
		{
			CodeInstruction val = list[i];
			if (val.opcode == OpCodes.Ldfld && object.Equals(val.operand, fieldInfo))
			{
				CodeInstruction val2 = ((i + 1 < list.Count) ? list[i + 1] : null);
				CodeInstruction val3 = ((i + 2 < list.Count) ? list[i + 2] : null);
				if ((val2 != null && val2.opcode == OpCodes.Ldfld && object.Equals(val2.operand, objB)) || (val2 != null && val2.opcode == OpCodes.Ldc_R4 && IsFloat(val2.operand, 2000f)) || (val2 != null && val2.opcode == OpCodes.Ldc_R4 && IsFloat(val2.operand, 1000f)) || (val2 != null && val2.opcode == OpCodes.Conv_I4 && val3 != null && val3.opcode == OpCodes.Ldc_I4_2))
				{
					list.InsertRange(i + 1, array);
					i += array.Length;
				}
			}
			else if (val.opcode == OpCodes.Stfld && object.Equals(val.operand, objB2) && i > 0)
			{
				list.InsertRange(i, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[4]
				{
					new CodeInstruction(OpCodes.Ldarg_0, (object)null),
					new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo),
					new CodeInstruction(OpCodes.Call, (object)method2),
					new CodeInstruction(OpCodes.Mul, (object)null)
				});
				i += 4;
			}
		}
		return list;
	}

	private static bool IsFloat(object operand, float value)
	{
		if (operand is float num)
		{
			return Math.Abs(num - value) < 0.001f;
		}
		return false;
	}

	public static float GetAccelerationBoost(float difficulty)
	{
		if (!(difficulty > 100f))
		{
			return 1f;
		}
		return 1f + (difficulty - 100f) / 100f;
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
}
