# BATCH-009: 钓鱼时立即转化多条鱼

**创建时间**: 2024-08-04 22:25  
**累计修复次数**: 1次（首次修复）  
**当前状态**: 🔴 R0取证中

---

## 玩家可见现象

钓到大鱼时，钓鱼动画直接显示多条鱼飞向玩家，并且多次增加难度等级

---

## 证据链

### 证据1: 用户反馈
"钓到一个大鱼，会直接钓的时候就转化为多个鱼，而且多次加难度等级"

### 证据2: FishingRodPatches.cs源码审查
```csharp
[HarmonyPrefix]
public static void PullFishFromWater_Prefix(string fishId, ref int numCaught)
{
    numCaught *= quantityMultiplier; // ❌ 在Prefix中修改，影响游戏动画
    DifficultyManager.RecordSuccess(fishId); // ❌ 立即增加等级
}
```

**错误逻辑**：
1. `numCaught` 参数控制 `FishingRod.doPullFishFromWater()` 中的飞行动画数量
2. 倍数10时会创建10个 `TemporaryAnimatedSprite`
3. `RecordSuccess()` 在Prefix中调用，等级立即+1（应该等物品真正进入背包后再加）

---

## 根因诊断

### 错误的假设
BATCH-001认为修改 `pullFishFromWater()` 的 `numCaught` 参数可以控制最终数量

### 真实情况
`numCaught` 参数控制：
1. 飞行动画数量（`doPullFishFromWater` 循环创建动画）
2. 后续调用 `Farmer.caughtFish(fishId, fishSize, fromFishPond, numCaught)`

### 第一处分歧
应该使用Postfix拦截 `Farmer.caughtFish()` 或 `Farmer.addItemToInventoryBool()`，而不是修改 `pullFishFromWater` 的参数

---

## R0: 取证（当前阶段）

### 已完成证据
1. ✅ 用户反馈确认问题
2. ✅ 源码审查发现Prefix修改numCaught导致动画错误
3. ✅ 定位唯一所有者：`Farmer.caughtFish()` 或 `Farmer.addItemToInventoryBool()`

### 停止条件
✅ 根因明确：Prefix修改参数影响游戏动画逻辑

---

## R1: 修复方案

### 删除清单
- ❌ FishingRodPatches.PullFishFromWater_Prefix 中的 `numCaught *= quantityMultiplier`
- ❌ FishingRodPatches.PullFishFromWater_Prefix 中的 `DifficultyManager.RecordSuccess()`

### 新实现
**方案A（推荐）**：Patch `Farmer.caughtFish()`
- Prefix保存原始数量
- Postfix修改最终创建的Item数量
- 在Postfix中调用 `RecordSuccess()` 和HUD通知

**方案B**：Transpiler修改 `FishingRod.doPullFishFromWater()`
- 只修改传给 `Farmer.caughtFish()` 的参数
- 不修改动画循环的参数
- 复杂度高，不推荐

**选择方案A**：更简单，更符合"唯一所有者"原则

---

## R2: 待R1完成后执行

验证：
1. 钓鱼动画只显示1条鱼（即使倍数100）
2. 背包收到正确数量的鱼
3. 难度等级只增长1次
4. 原生多条鱼机制（海泡鱼饵）仍然正常工作

---

## 构建状态
⏳ 待R1完成

## 部署状态
⏳ 待R1完成

## 实测状态
❌ v0.5.0钓鱼动画显示多条鱼

---

## 修复次数统计

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 钓鱼动画显示多条鱼 | 1 | BATCH-009(修复中) |

---

**下一步**: 实现FarmerPatches.CaughtFish_Postfix修改数量
