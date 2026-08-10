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

		public float Alpha { get; set; }

		public string JumpText { get; set; }

		public float JumpTextTimer { get; set; }

		public string AssistText { get; set; }

		public float AssistTextTimer { get; set; }

		public int AssistLevel { get; set; }

		public string AssistFishId { get; set; }

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

	private static readonly List<AssistObservation> AssistObservations = new List<AssistObservation>();

	private const int AssistObservationCap = 500;

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	public static void Constructor_Postfix(BobberBar __instance, string whichFish, ref float ___difficulty, ref int ___fishSize, ref int ___fishQuality, ref float ___distanceFromCatchPenaltyModifier, ref float ___bobberTargetPosition, bool ___bobberInBar, ref int ___bobberBarHeight)
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
			obj.Alpha = DifficultyManager.GetAlpha(Game1.player);
			InstanceData instanceData = obj;
			_instanceData.Add(__instance, instanceData);
			float difficultyMultiplier = DifficultyCalculator.GetDifficultyMultiplier(difficultyLevel);
			___difficulty *= difficultyMultiplier;
			int value = (instanceData.QuantityMultiplier = DifficultyCalculator.GetQuantityMultiplier(difficultyLevel));
			___fishSize = (int)Math.Round((float)___fishSize * DifficultyCalculator.GetFishSizeMultiplier(difficultyLevel));
			___fishQuality = DifficultyCalculator.ApplyQualityBonus(___fishQuality, difficultyLevel);
			instanceData.AdjustedDifficulty = ___difficulty;
			TryTriggerAssist(Game1.player, instanceData, ref ___bobberBarHeight);
			if (instanceData.AdjustedDifficulty > 100f)
			{
				___bobberTargetPosition = 0f;
			}
			instanceData.JumpIntervalSeconds = GetJumpInterval(instanceData.AdjustedDifficulty);
			HUDNotifier.ShowDifficultyRecommendation(difficultyLevel, Game1.player);
			HUDNotifier.ShowStarChallengeNotification(text, difficultyLevel, Game1.player);
			ModEntry.ModMonitor.Log($"[BobberBar] 钓鱼小游戏开始 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {text} | 难度等级: {difficultyLevel} | 原始difficulty: {num:F1} | 调整后: {___difficulty:F1} (×{difficultyMultiplier:F2}) | 数量倍数: {value} | fishSize: {___fishSize} | 品质: {___fishQuality} | 加速增幅档: {((difficultyLevel >= 98) ? "100%（等级≥98）" : "10%（等级<98）")} | 手感进度α: {instanceData.Alpha:P0}", (LogLevel)2);
		}
		catch (Exception value2)
		{
			ModEntry.ModMonitor.Log($"BobberBar 构造函数 Patch 失败: {value2}", (LogLevel)4);
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
			float num = (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			if (!string.IsNullOrEmpty(value.JumpText))
			{
				value.JumpTextTimer -= num;
				if (value.JumpTextTimer <= 0f)
				{
					value.JumpText = null;
				}
			}
			if (!string.IsNullOrEmpty(value.AssistText))
			{
				value.AssistTextTimer -= num;
				if (value.AssistTextTimer <= 0f)
				{
					value.AssistText = null;
				}
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
			float catchPenaltyModifier = DifficultyCalculator.GetCatchPenaltyModifier(value.DifficultyLevel, ___distanceFromCatching);
			float num3 = (value.LastAppliedCatchPenaltyModifier = (___distanceFromCatchPenaltyModifier = Math.Min(value.NativeCatchPenaltyModifier, catchPenaltyModifier)));
			if (Math.Abs(num2 - num3) > 0.01f)
			{
				ModEntry.ModMonitor.Log($"[BobberBar] 蓄力槽保护触发 | 实例: {((object)__instance).GetHashCode()} | 等级: {value.DifficultyLevel} | 蓄力进度: {___distanceFromCatching:P0} | 减速倍率: {num2:F2} → {num3:F2}", (LogLevel)1);
			}
			if (!(value.AdjustedDifficulty >= 150f) || value.ResultStarted)
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
					bool flag = value.JumpPendingTarget <= 133f;
					value.JumpText = PickJumpText(flag);
					value.JumpTextTimer = 1.5f;
					ModEntry.ModMonitor.Log($"[BobberBar] 高难度鱼跳 | 实例: {((object)__instance).GetHashCode()} | 鱼ID: {value.FishId} | 难度: {value.AdjustedDifficulty:F0} | 跳至: {value.JumpPendingTarget:F0} | 文案: {(flag ? "上跳(鱼跃)" : "下跳(甩尾)")}: {value.JumpText}", (LogLevel)2);
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
					bool isEpicChampion = value.AdjustedDifficulty >= 150f && DifficultyManager.GetConsecutiveFailCount(value.FishId, value.Owner ?? Game1.player) >= 2;
					HUDNotifier.ShowFailureNotification(value.FishId, difficultyLevel, isEpicChampion);
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
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Expected O, but got Unknown
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Expected O, but got Unknown
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Expected O, but got Unknown
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Expected O, but got Unknown
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Expected O, but got Unknown
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Expected O, but got Unknown
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Expected O, but got Unknown
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Expected O, but got Unknown
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Expected O, but got Unknown
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Expected O, but got Unknown
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Expected O, but got Unknown
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Expected O, but got Unknown
		//IL_072f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0739: Expected O, but got Unknown
		//IL_069c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a2: Expected O, but got Unknown
		//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b0: Expected O, but got Unknown
		//IL_06b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Expected O, but got Unknown
		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cd: Expected O, but got Unknown
		//IL_06db: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Expected O, but got Unknown
		//IL_08a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08aa: Expected O, but got Unknown
		//IL_08b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b8: Expected O, but got Unknown
		//IL_08c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c6: Expected O, but got Unknown
		//IL_08cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d5: Expected O, but got Unknown
		//IL_08dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e3: Expected O, but got Unknown
		//IL_08f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Expected O, but got Unknown
		//IL_0902: Unknown result type (might be due to invalid IL or missing references)
		//IL_0908: Expected O, but got Unknown
		//IL_0919: Unknown result type (might be due to invalid IL or missing references)
		//IL_091f: Expected O, but got Unknown
		//IL_0927: Unknown result type (might be due to invalid IL or missing references)
		//IL_092d: Expected O, but got Unknown
		//IL_0937: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Expected O, but got Unknown
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
		int num = (_instanceData.TryGetValue(instance, out value) ? value.DifficultyLevel : 98);
		float num2 = ((num >= 98) ? 1f : 0.1f);
		return 1f + num2 * (difficulty - 100f) / 100f;
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
		float num6 = ((num5 < 0f) ? (-30f) : 30f);
		return (1f - alpha) * (speed + num5 * frameScale) + alpha * num6;
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

	private static void TryTriggerAssist(Farmer player, InstanceData data, ref int bobberBarHeight)
	{
		try
		{
			if (player == null || !player.IsLocalPlayer)
			{
				return;
			}
			bool flag = ModEntry.ConsumeForceAssistFlag();
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
					data.AssistText = PickAssistText(text, DifficultyManager.GetDifficultyLevel(text, player));
					data.AssistTextTimer = 1.5f;
					ModEntry.ModMonitor.Log($"[BobberBar] 助战触发 | 助战鱼: {text} | 难度: {DifficultyManager.GetDifficultyLevel(text, player)} | 排位: {assistRank:P0} | 临时钓鱼等级: +{randomAssistLevel} | 绿条高度: {value} → {bobberBarHeight} | 总概率: {assistChance:P1}{(flag ? " | 测试强制触发" : "")} | 文案: {data.AssistText}", (LogLevel)2);
				}
			}
		}
		catch (Exception value2)
		{
			ModEntry.ModMonitor.Log($"BobberBar 助战判定失败: {value2}", (LogLevel)4);
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
			ModEntry.ModMonitor.Log("[BobberBar] 助战文案生成失败: " + ex.Message, (LogLevel)4);
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
			ModEntry.ModMonitor.Log("[BobberBar] 跳鱼文案生成失败: " + ex.Message, (LogLevel)4);
			return jumpUp ? "鱼跃！" : "甩尾！";
		}
	}

	[HarmonyPatch("draw")]
	[HarmonyPostfix]
	public static void Draw_Postfix(BobberBar __instance, SpriteBatch b, int ___xPositionOnScreen, int ___yPositionOnScreen, float ___bobberPosition, float ___bobberBarPos, int ___bobberBarHeight)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (_instanceData.TryGetValue(__instance, out var value))
			{
				if (!string.IsNullOrEmpty(value.JumpText) && value.JumpTextTimer > 0f)
				{
					float num = Math.Min(1f, value.JumpTextTimer / 0.5f);
					Vector2 val = Game1.smallFont.MeasureString(value.JumpText);
					Vector2 val2 = default(Vector2);
					((Vector2)(ref val2))..ctor((float)___xPositionOnScreen + 82f - val.X / 2f, (float)___yPositionOnScreen + 36f + ___bobberPosition - 30f);
					b.DrawString(Game1.smallFont, value.JumpText, val2 + new Vector2(1f, 1f), Color.Black * (num * 0.7f));
					b.DrawString(Game1.smallFont, value.JumpText, val2, Color.White * num);
				}
				if (!string.IsNullOrEmpty(value.AssistText) && value.AssistTextTimer > 0f)
				{
					float num2 = Math.Min(1f, value.AssistTextTimer / 0.5f);
					Vector2 val3 = Game1.smallFont.MeasureString(value.AssistText);
					Vector2 val4 = default(Vector2);
					((Vector2)(ref val4))..ctor((float)___xPositionOnScreen + 108f, (float)___yPositionOnScreen + 12f + ___bobberBarPos + (float)___bobberBarHeight / 2f - val3.Y / 2f);
					b.DrawString(Game1.smallFont, value.AssistText, val4 + new Vector2(1f, 1f), Color.Black * (num2 * 0.7f));
					b.DrawString(Game1.smallFont, value.AssistText, val4, Color.White * num2);
				}
			}
		}
		catch (Exception value2)
		{
			ModEntry.ModMonitor.Log($"BobberBar 小游戏文案绘制失败: {value2}", (LogLevel)4);
		}
	}
}
