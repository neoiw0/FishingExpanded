using System;
using HarmonyLib;
using StardewValley;

namespace FishingExpanded.Patches
{
    /// <summary>Farmer Patch（已废弃 - BATCH-007 证明Farmer.draw不调用Item.drawInMenu）</summary>
    /// <remarks>
    /// R1删除理由：BATCH-007 证据显示 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
    /// 手持物品缩放的唯一所有者是 Object.drawWhenHeld()，已在 ObjectPatches.cs 实现正确Patch
    /// 本类保留作为历史记录，所有方法已删除
    /// </remarks>
    [HarmonyPatch(typeof(Farmer))]
    internal class FarmerPatches
    {
        // BATCH-007 R1: 所有代码已删除
        // - Draw_Transpiler: 搜索不存在的 Item.drawInMenu() 调用
        // - FindScaleLoadInstruction: 辅助方法
        // - IsParameterLoadInstruction: 辅助方法
        // - GetItemDrawScale: 缩放计算方法
        // - InvalidateCache: 缓存清理方法
        // - _drawTranspilerLogged: 日志标志
        // - _playerCache: 多人缓存字典
        //
        // 正确实现见: ObjectPatches.cs
    }
}
