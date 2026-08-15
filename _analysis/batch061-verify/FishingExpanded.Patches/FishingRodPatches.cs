using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingExpanded.Patches;

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
			((Vector2)(ref val2))._002Ector((float)caughtItemSourceRect.Width * 4f * (1f - visualScale) / 2f, (float)caughtItemSourceRect.Height * 4f * (1f - visualScale) / 2f);
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
				HasChallengeBait = flag
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
				if (value.ChallengeStarMultiplier < 1f)
				{
					num2 = (long)Math.Round((float)num * value.ChallengeStarMultiplier);
				}
				if (value.Multiplier > 1)
				{
					num2 *= value.Multiplier;
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
