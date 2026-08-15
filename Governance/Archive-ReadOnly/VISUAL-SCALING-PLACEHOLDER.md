# 占位实现说明

## 超大鱼视觉缩放的技术限制

### 当前实现（占位）
`FarmerPatches.cs` 中的 `Draw_Postfix` 方法只能在原版绘制**完成后**执行，此时物品已经绘制到屏幕上，无法再修改其尺寸。当前实现仅记录日志：

```csharp
[HarmonyPostfix]
public static void Draw_Postfix(Farmer __instance, SpriteBatch b)
{
    // 只能在这里读取状态，无法修改已经绘制的内容
    float visualScale = GiantFishManager.GetFishVisualScale(fishId);
    // 记录日志，但实际绘制已经完成
}
```

### 为什么需要 Transpiler

原版玩家举起物品的绘制逻辑在 `Farmer.draw()` 方法内部，关键代码类似：

```csharp
// 原版伪代码
void draw(SpriteBatch b) {
    // ... 绘制玩家身体 ...
    if (mostRecentlyGrabbedItem != null) {
        mostRecentlyGrabbedItem.drawInMenu(b, position, scale: 4f); // 固定缩放4倍
    }
}
```

要修改这个 `scale: 4f` 参数，需要：
1. **Transpiler**：在编译时修改 IL 指令，将 `ldc.r4 4.0` 替换为动态计算的值
2. **或者完全替换 draw 方法**（Prefix返回false），但这会影响所有其他Mod兼容性

### 正确的实现方案

```csharp
[HarmonyTranspiler]
[HarmonyPatch(typeof(Farmer), "draw")]
static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 查找 ldc.r4 4.0（举起物品的缩放值）
    // 替换为调用 GiantFishManager.GetFishVisualScale() * 4f
    // 需要深入理解 IL 指令和堆栈操作
}
```

### 当前功能状态

**仍然正常工作的部分**：
- ✅ 超大鱼的倍数计算（数量×倍数）
- ✅ NPC 5格内冒泡反应
- ✅ NPC 对话替换
- ✅ 进入 FarmHouse 重置标记
- ✅ 每日重置触发记录

**不工作的部分**：
- ❌ 玩家举起超大鱼时，视觉尺寸不会×(倍数^1/3)放大
- ❌ 玩家从背包重新选中超大鱼时，不会显示放大效果

### 为什么暂时保留占位实现

1. **核心功能优先**：超大鱼的数量倍数、NPC反应等核心机制已经完整实现
2. **复杂度评估**：Transpiler 需要对 IL 代码深入理解，容易引入新Bug
3. **测试优先**：先验证其他功能正常，再逐步优化视觉效果
4. **保留接口**：GiantFishManager.GetFishVisualScale() 接口已准备好，Transpiler实现时只需调用

### 后续优化路径

**选项1：Transpiler（推荐）**
- 精确修改 IL 指令
- 兼容性最好
- 实现难度：高

**选项2：完全替换 draw 方法（Prefix）**
- 复制原版所有绘制逻辑
- 可能与其他Mod冲突
- 实现难度：中

**选项3：等待 SMAPI 或游戏更新**
- 如果未来有更简单的API支持物品绘制缩放
- 实现难度：无，但需要等待

## 结论

当前的"占位实现"是有意为之的技术决策，优先保证核心功能完整性和稳定性。视觉缩放是锦上添花，不影响游戏玩法体验。
