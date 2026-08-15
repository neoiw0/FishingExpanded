# BATCH-008: ObjectPatches Transpiler栈序列错误

**创建时间**: 2024-08-04 22:15  
**累计修复次数**: 2次（BATCH-003误判 → BATCH-007重新实现 → BATCH-008修复栈错误）  
**当前状态**: 🔴 R0取证中

---

## 玩家可见现象

视觉缩放功能仍然不工作 - BATCH-007部署的v0.5.0中，Transpiler成功插入但GetDrawScale从未被调用

---

## 证据链

### 证据1: SMAPI日志显示Transpiler成功但功能失效
```
[ObjectPatches] ✓ 成功插入视觉缩放逻辑到 Object.drawWhenHeld() | 位置: IL_0027
[ObjectPatches] ✓ 共修改了 1 处 scale 参数
[GiantFishManager] 超大鱼记录 | 鱼ID: 145 | 倍数: 17 | 视觉缩放: ×2.57
```
**关键反证**：完全没有 `[ObjectPatches] 应用视觉缩放` 日志，说明GetDrawScale()从未被调用

### 证据2: ObjectPatches.cs源码审查发现栈序列错误

```csharp
if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f && Math.Abs(f - 4f) < 0.01f)
{
    // ❌ 错误：先插入3条指令，后yield return codes[i]
    yield return new CodeInstruction(OpCodes.Ldarg_0);
    yield return new CodeInstruction(OpCodes.Call, getScaleMethod);
    yield return new CodeInstruction(OpCodes.Mul);
    patchCount++;
}
yield return codes[i]; // ldc.r4 4 被放到了最后
```

**栈效果分析**：
- 原始IL：`... ldc.r4 4 ...`（压入4f到栈顶）
- 错误插入后：`... ldarg.0 call mul ldc.r4 4 ...`
- 执行结果：
  1. ldarg.0 → 栈：`[obj]`
  2. call GetDrawScale → 栈：`[visualScale]`（假设1.0）
  3. mul → **栈下溢出错误**（只有1个值无法乘法）或与栈底的其他值错误相乘
  4. ldc.r4 4 → 栈：`[错误结果, 4f]`（4f放到了错误位置）

**正确应该是**：
```csharp
yield return codes[i]; // 先压入 ldc.r4 4 → 栈：[4f]
// 然后插入：
yield return new CodeInstruction(OpCodes.Ldarg_0);  // 栈：[4f, obj]
yield return new CodeInstruction(OpCodes.Call, getScaleMethod); // 栈：[4f, visualScale]
yield return new CodeInstruction(OpCodes.Mul); // 栈：[4f * visualScale]
```

---

## 根因诊断

### 错误的假设
BATCH-007认为找到`ldc.r4 4`后直接插入乘法逻辑即可

### 真实情况
IL Transpiler必须严格遵守栈机器的顺序：
1. `ldc.r4 4` 必须先执行（压入4f）
2. 然后才能执行乘法逻辑（读取4f并乘以visualScale）
3. 插入新指令前必须先 `yield return` 原指令

### 第一处分歧
BATCH-007实现Transpiler时，未验证插入指令的栈效果，直接按"找到→插入→返回原指令"的错误顺序编写

---

## R0: 取证（当前阶段）

### 已完成证据
1. ✅ SMAPI日志证实GetDrawScale从未被调用
2. ✅ 源码审查发现yield return顺序错误
3. ✅ 栈效果分析证实mul指令会栈下溢或错误计算

### 停止条件
✅ 根因明确：yield return顺序错误导致栈序列混乱

---

## R1: 修复方案

### 删除清单
无需删除代码，只需调整yield return顺序

### 修复方案
**ObjectPatches.DrawWhenHeld_Transpiler**：
```csharp
if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f && Math.Abs(f - 4f) < 0.01f)
{
    yield return codes[i]; // ✅ 先返回 ldc.r4 4
    
    // 然后插入乘法逻辑
    yield return new CodeInstruction(OpCodes.Ldarg_0);
    yield return new CodeInstruction(OpCodes.Call, getScaleMethod);
    yield return new CodeInstruction(OpCodes.Mul);
    
    patchCount++;
}
else
{
    yield return codes[i]; // 其他指令正常返回
}
```

**注意**：必须在if分支内也yield return，否则会跳过ldc.r4 4指令

---

## R2: 待R1完成后执行

场景、性能、多人验证

---

## 构建状态
⏳ 待R1完成

## 部署状态
⏳ 待R1完成

## 实测状态
❌ v0.5.0视觉缩放仍然无效

---

## 修复次数统计

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 视觉缩放不工作 | 2 | BATCH-003(误判) → BATCH-007(Transpiler插入成功但栈错误) → BATCH-008(修复中) |

---

**下一步**: 修复ObjectPatches.cs的yield return顺序
