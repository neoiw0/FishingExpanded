using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FishingExpanded.Services;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace FishingExpanded.Patches;

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
