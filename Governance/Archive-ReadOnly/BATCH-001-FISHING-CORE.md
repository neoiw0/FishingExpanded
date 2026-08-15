# BATCH-001-FISHING-CORE — 钓鱼系统核心代码审计

## 批次信息
- **批次ID**: BATCH-001-FISHING-CORE
- **类型**: 技术调研（反编译审计）
- **创建时间**: 2026-08-04
- **状态**: 进行中

## 目标
反编译并审计星露谷原生钓鱼系统的核心代码，记录完整调用链，确定 Harmony Patch 的拦截点。

## 需要审计的核心类

### 1. BobberBar（钓鱼小游戏UI）
- **路径**: `StardewValley.Menus.BobberBar`
- **关键字段**:
  - `difficulty`: 鱼的难度数值
  - `distanceFromCatching`: 蓄力槽进度（0-1）
  - `fishSize`: 鱼的尺寸
  - `fishQuality`: 鱼的品质（0/1/2/4）
  - `whichFish`: 鱼的物品ID
- **关键方法**:
  - 构造函数：初始化难度、尺寸、品质
  - `update()`: 每帧更新蓄力槽和鱼的移动
  - 成功/失败判定逻辑

### 2. FishingRod（钓鱼竿工具）
- **路径**: `StardewValley.Tools.FishingRod`
- **关键方法**:
  - `pullFishFromWater()`: 钓鱼成功后将鱼放入背包
  - `doneFishing()`: 钓鱼结束清理
  - 多条鱼的物品创建和堆叠逻辑

### 3. Collections（收藏页面）
- **路径**: `StardewValley.Menus.Collections`
- **关键方法**:
  - 鱼图标绘制逻辑
  - 图标位置计算
  - UI 层级和坐标系统

### 4. NPC 对话系统
- **路径**: `StardewValley.NPC`
- **关键方法**:
  - 对话文本获取
  - 头顶气泡显示
  - 对话触发事件

### 5. 举起物品动画
- **路径**: `StardewValley.Farmer`
- **关键字段**:
  - `mostRecentlyGrabbedItem`: 最近拾取的物品
  - `itemToEat`: 正在使用的物品
- **关键方法**:
  - 举起动画播放
  - 物品绘制和缩放

## 审计证据

### BobberBar 核心逻辑（已读取）

从之前读取的代码可以确认：

**构造函数关键逻辑**（137-199行）：
```csharp
public BobberBar(string whichFish, float fishSize, bool treasure, ...)
{
    // 从 Data/Fish 读取鱼的配置
    difficulty = Convert.ToInt32(array[1]);  // 原始difficulty
    minFishSize = Convert.ToInt32(array[3]);
    maxFishSize = Convert.ToInt32(array[4]);
    
    // 计算实际尺寸
    this.fishSize = (int)((float)minFishSize + (float)(maxFishSize - minFishSize) * fishSize);
    
    // 计算品质
    fishQuality = (fishSize < 0.33) ? 0 : ((fishSize < 0.66) ? 1 : 2);
}
```

**蓄力槽更新逻辑**（523-589行）：
```csharp
if (bobberInBar)
{
    distanceFromCatching += 0.002f;  // 鱼在绿条内，蓄力槽上升
}
else
{
    // 鱼逃跑，蓄力槽下降
    distanceFromCatching -= (beginnersRod ? 0.002f : 0.003f) * distanceFromCatchPenaltyModifier;
}

// 钳制到 [0, 1]
distanceFromCatching = Math.Max(0f, Math.Min(1f, distanceFromCatching));

// 判定成功/失败
if (distanceFromCatching <= 0f)
{
    fadeOut = true;  // 失败
}
else if (distanceFromCatching >= 1f)
{
    // 成功，调用 FishingRod.pullFishFromWater
}
```

**成功后的调用**（359-361行）：
```csharp
if (distanceFromCatching > 0.9f && fishingRod != null)
{
    fishingRod.pullFishFromWater(whichFish, fishSize, fishQuality, (int)difficulty, 
                                   treasureCaught, perfect, fromFishPond, setFlagOnCatch, 
                                   bossFish, numCaught);
}
```

## 待确认的原生行为

1. ✅ **已确认**: BobberBar 构造函数接受 `whichFish`、`fishSize`、`difficulty`
2. ✅ **已确认**: 蓄力槽减少速度通过 `distanceFromCatchPenaltyModifier` 控制
3. ✅ **已确认**: `pullFishFromWater` 的多条鱼处理逻辑
4. ✅ **已确认**: NPC 头顶气泡 API 为 `NPC.doEmote(int emoteIndex)`
5. ⏳ **待确认**: Collections 菜单的鱼图标绘制具体位置
6. ⏳ **待确认**: 举起物品的动画和绘制层
7. ⏳ **待确认**: 进入 FarmHouse 的事件监听

## 关键发现

### pullFishFromWater 多条鱼处理（FishingRod.cs）

**关键逻辑**：
```csharp
// 1. 通过网络事件传递参数
pullFishFromWaterEvent.Fire(delegate(BinaryWriter writer) {
    writer.Write(numCaught);  // 鱼的数量
});

// 2. 实际处理在 doPullFishFromWater
numberOfFishCaught = argReader.ReadInt32();  // 读取数量

// 3. 如果 numCaught > 1，创建多个飞行动画
if (numberOfFishCaught > 1) {
    for (int i = 1; i < numberOfFishCaught; i++) {
        // 创建额外的 TemporaryAnimatedSprite
        // 每个延迟 (i-1) * 100ms
        delayBeforeAnimationStart = (i - 1) * 100
    }
}

// 4. 最终调用 playerCaughtFishEndFunction
// 该函数调用 farmer.caughtFish(whichFish, fishSize, fromFishPond, numberOfFishCaught)
```

**重要发现**：
- `numberOfFishCaught` 参数控制动画数量和实际获得的鱼数量
- 原版 `Farmer.caughtFish` 方法会根据 `numberOfFishCaught` 创建多个物品实例
- **溢出处理**：由 `Farmer.addItemToInventoryBool` 处理，满了会返回false但不会掉地上，而是直接丢失（需要进一步确认）

### NPC 气泡系统

**API 入口**：
```csharp
NPC.doEmote(int emoteIndex)
```

**常用表情索引**：
- 12: 爱心
- 16: 音乐符号
- 20: 星星
- 28: 惊讶
- 32: 问号

**自定义文案**：
- NPC 气泡不支持自定义文本，只能显示预定义的表情图标
- **需要改用对话气泡**：`NPC.showTextAboveHead(string text)` 或修改 `NPC.CurrentDialogue`

### 钓鱼成功后的流程

```
BobberBar.update()
  └─ distanceFromCatching >= 1f
      └─ FishingRod.pullFishFromWater(whichFish, fishSize, fishQuality, ...)
          └─ [网络事件] doPullFishFromWater
              └─ playerCaughtFishEndFunction
                  └─ Farmer.caughtFish(fishId, fishSize, fromFishPond, numberOfFishCaught)
                      └─ 创建物品并加入背包
```

### Collections 页面鱼图标绘制

**待确认位置**：
- 需要查找 `CollectionsPage.draw()` 方法中的鱼图标绘制代码
- 确定坐标计算方式，以便在左上角叠加星星

## Harmony Patch 拦截点（修订）

### 1. 难度调整
- **Target**: `BobberBar` 构造函数
- **Type**: Postfix
- **作用**: 修改 `difficulty`、`fishSize`、`fishQuality` 字段

### 2. 蓄力槽保护
- **Target**: `BobberBar.update()`
- **Type**: Transpiler 或 Prefix
- **作用**: 动态修改 `distanceFromCatchPenaltyModifier`

### 3. 钓鱼结果
- **Target**: `FishingRod.pullFishFromWater()`
- **Type**: Prefix
- **作用**: 修改 `numCaught` 参数，记录成功次数

### 4. 钓鱼失败
- **Target**: `BobberBar.update()` 中 `distanceFromCatching <= 0f` 分支
- **Type**: Postfix
- **作用**: 记录失败次数，显示 HUD 提示

### 5. 多条鱼数量修改
- **Target**: `FishingRod.pullFishFromWater()`
- **Type**: Prefix（修改 `numCaught` 参数）
- **作用**: 根据难度等级倍数计算实际获得数量

### 6. NPC 反应
- **Target**: `Farmer.Update()` 或自定义 Tick 事件
- **Type**: 检测工具栏选中物品
- **作用**: 触发 `NPC.showTextAboveHead()` 显示赞美文案

### 7. Collections 星标
- **Target**: `CollectionsPage.draw()`
- **Type**: Postfix
- **作用**: 在鱼图标左上角绘制星星

## 批次状态

- **进度**: 反编译审计完成 80%
- **下一步**: 
  1. 实施难度等级统计与存档系统（Task #3）
  2. 实施 difficulty 动态调整（Task #4）
  3. 创建 Harmony Patch 框架
