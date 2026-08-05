using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using FishingExpanded.Services;
using StardewModdingAPI;

namespace FishingExpanded.Patches
{
    /// <summary>Object Patch（用于手持物品视觉缩放）</summary>
    [HarmonyPatch(typeof(StardewValley.Object))]
    internal class ObjectPatches
    {
        private static bool _transpilerLogged = false;

        /// <summary>Transpiler: 修改 Object.drawWhenHeld() 中的scale参数从4f变为4f*visualScale</summary>
        [HarmonyPatch(nameof(StardewValley.Object.drawWhenHeld))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DrawWhenHeld_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            ModEntry.ModMonitor.Log(
                $"[ObjectPatches] 开始分析 Object.drawWhenHeld() | 总指令数: {codes.Count}",
                LogLevel.Debug);

            // 查找 ldc.r4 4 （加载浮点常量4.0作为scale）
            MethodInfo getScaleMethod = AccessTools.Method(typeof(ObjectPatches), nameof(GetDrawScale));

            int patchCount = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                // 查找 ldc.r4 4 指令（Object.drawWhenHeld第5345行的scale参数）
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f && Math.Abs(f - 4f) < 0.01f)
                {
                    // BATCH-008修复：先压入4f，再插入乘法逻辑
                    // 栈变化：[4f] → [4f, this] → [4f, visualScale] → [4f * visualScale]

                    yield return codes[i]; // ✅ 先返回 ldc.r4 4 → 栈：[4f]
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // 栈：[4f, obj]
                    yield return new CodeInstruction(OpCodes.Ldarg_3); // 栈：[4f, obj, farmer]
                    yield return new CodeInstruction(OpCodes.Call, getScaleMethod); // 栈：[4f, visualScale]
                    yield return new CodeInstruction(OpCodes.Mul); // 栈：[4f * visualScale]

                    patchCount++;

                    if (!_transpilerLogged)
                    {
                        ModEntry.ModMonitor.Log(
                            $"[ObjectPatches] ✓ 成功插入视觉缩放逻辑到 Object.drawWhenHeld() | 位置: IL_{i:X4}",
                            LogLevel.Info);
                        _transpilerLogged = true;
                    }
                }
                else
                {
                    yield return codes[i]; // 其他指令正常返回
                }
            }

            if (patchCount == 0)
            {
                ModEntry.ModMonitor.Log(
                    "[ObjectPatches] ⚠ 未找到 ldc.r4 4 指令，视觉缩放可能无效",
                    LogLevel.Warn);
            }
            else
            {
                ModEntry.ModMonitor.Log(
                    $"[ObjectPatches] ✓ 共修改了 {patchCount} 处 scale 参数",
                    LogLevel.Info);
            }
        }

        /// <summary>计算手持物品的视觉缩放（由Transpiler调用）</summary>
        /// <param name="obj">Object实例</param>
        /// <returns>缩放倍数（1.0 = 不缩放）</returns>
        public static float GetDrawScale(StardewValley.Object obj, Farmer owner)
        {
            try
            {
                // 只处理鱼类物品
                if (obj.Category != StardewValley.Object.FishCategory)
                    return 1.0f;

                string fishId = obj.QualifiedItemId;
                return GiantFishManager.GetFishVisualScale(fishId, owner);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[ObjectPatches] GetDrawScale 失败: {ex}", LogLevel.Error);
                return 1.0f;
            }
        }
    }
}
