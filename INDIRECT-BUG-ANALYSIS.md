# 代码审查 - 间接Bug风险分析

## 已发现的主要问题

### 🔴 问题1: Transpiler视觉缩放失效 (严重)
**日志**: `[20:08:58 WARN] [Transpiler] ⚠ 未找到 Item.drawInMenu 调用，视觉缩放可能无效`

**根本原因**:
- Farmer.draw()方法中可能没有直接调用Item.drawInMenu()
- 或者调用方式不是callvirt而是call
- 当前Transpiler只匹配callvirt指令

**修复**:
- 扩展匹配：同时搜索Call和Callvirt指令
- 增强诊断：输出所有方法调用统计
- 不限定方法签名：匹配所有名为drawInMenu的调用

**状态**: ✅ 已修复并部署v0.4.3-debug

---

## 间接Bug风险清单

### 🟡 风险1: DifficultyLevel计算可能溢出
**位置**: `Data/FishDifficultyData.cs:28`
```csharp
public int DifficultyLevel => SuccessCount - FailCount;
```

**风险分析**:
- SuccessCount和FailCount是int类型
- 极端情况下SuccessCount可能达到int.MaxValue
- FailCount同样可能很大
- 相减可能导致整数溢出

**触发条件**:
- 玩家钓同一种鱼数百万次
- 或者通过控制台命令设置极大值

**影响**:
- DifficultyLevel变为负数（溢出）
- 破坏难度系统逻辑
- 可能导致倍数计算异常

**修复建议**:
```csharp
public int DifficultyLevel => 
    Math.Max(-10, Math.Min(100, SuccessCount - FailCount));
```

---

### 🟡 风险2: 缓存未在多人模式下隔离
**位置**: `Patches/FarmerPatches.cs:23-24`
```csharp
private static string _cachedFishId = null;
private static float _cachedScale = 1.0f;
```

**风险分析**:
- 缓存是静态字段，所有玩家共享
- 多人模式下玩家A的鱼ID会覆盖玩家B的缓存
- 玩家B可能获得错误的缩放值

**触发条件**:
- 多人模式下两个玩家同时持有不同的超大鱼
- 缓存会被频繁覆盖

**影响**:
- 玩家看到错误的鱼缩放
- 性能优化失效（缓存频繁失效）

**修复建议**:
```csharp
private static Dictionary<long, (string fishId, float scale)> _playerCache = new();

// 使用玩家ID作为键
if (_playerCache.TryGetValue(farmer.UniqueMultiplayerID, out var cached) && 
    cached.fishId == fishId)
{
    return cached.scale;
}
```

---

### 🟢 风险3: GiantFishManager的ActiveGiantFish可能无限增长
**位置**: `Services/GiantFishManager.cs:9`
```csharp
public Dictionary<string, (int multiplier, int fishSize)> ActiveGiantFish { get; set; }
```

**风险分析**:
- 钓到超大鱼时添加到字典
- 只有进入FarmHouse时才清空
- 玩家可能长时间不进FarmHouse

**触发条件**:
- 玩家在野外连续钓鱼
- 从不进入FarmHouse
- 钓到多种不同的超大鱼

**影响**:
- 内存占用持续增长
- 字典可能包含数百种过期鱼

**修复建议**:
```csharp
// 在每日开始时也清空
public static void OnDayStarted()
{
    _displayData.ActiveGiantFish.Clear();
    _displayData.ResetDailyTriggers();
    // ...
}
```

---

### 🟢 风险4: NPCDialogueGenerator数组访问越界
**位置**: `Utils/NPCDialogueGenerator.cs:116`
```csharp
int index = Game1.random.Next(PraiseTemplates.Length);
return string.Format(PraiseTemplates[index], fishName, fishSize);
```

**风险分析**:
- PraiseTemplates是110个元素的数组
- Game1.random.Next(110)返回[0, 109]
- 理论上安全，但string.Format可能抛出异常

**触发条件**:
- fishName或fishSize为null
- 或者模板字符串格式错误

**影响**:
- NPC对话崩溃
- 整个气泡系统失效

**修复建议**:
```csharp
try
{
    string template = PraiseTemplates[index];
    return string.Format(template, 
        fishName ?? "未知鱼类", 
        fishSize);
}
catch (FormatException ex)
{
    ModEntry.ModMonitor.Log($"[NPCDialogue] 模板格式错误: {ex.Message}", LogLevel.Error);
    return $"哇！这条{fishName ?? "鱼"}真大！";
}
```

---

### 🟡 风险5: CollectionsPagePatches缓存同步问题
**位置**: `Patches/CollectionsPagePatches.cs:18-20`
```csharp
private static int _cachedTab = -1;
private static int _cachedPage = -1;
private static List<(string fishId, Vector2 position)> _cachedStars = new();
```

**风险分析**:
- 缓存在页面切换时重建
- 但item.bounds位置可能在窗口大小改变时变化
- 缓存的Vector2位置可能过期

**触发条件**:
- 玩家改变游戏窗口大小
- 或者改变UI缩放
- 星标位置错位

**影响**:
- 星标绘制在错误位置
- 可能绘制在屏幕外

**修复建议**:
```csharp
// 在窗口大小改变时清空缓存
public static void OnWindowSizeChanged()
{
    InvalidateCache();
}
```

---

### 🟢 风险6: BobberBarPatches实例数据竞态条件
**位置**: `Patches/BobberBarPatches.cs:149-152`
```csharp
if (___fadeOut)
{
    CleanupInstance(__instance);
}
```

**风险分析**:
- fadeOut检查和CleanupInstance调用不是原子操作
- 多线程环境下可能重复清理
- 或者在清理过程中实例被再次使用

**触发条件**:
- 极端快速钓鱼
- 或者多人模式下并发钓鱼

**影响**:
- 可能丢失失败记录
- 或者重复记录

**修复建议**:
```csharp
private static object _cleanupLock = new object();

if (___fadeOut)
{
    lock (_cleanupLock)
    {
        if (_instanceData.ContainsKey(__instance))
        {
            CleanupInstance(__instance);
        }
    }
}
```

---

## 总结

**高优先级修复** (🔴):
1. Transpiler视觉缩放失效 - ✅ 已修复

**中优先级修复** (🟡):
1. DifficultyLevel溢出风险
2. 多人模式缓存隔离
3. CollectionsPagePatches缓存同步

**低优先级监控** (🟢):
1. ActiveGiantFish内存增长
2. NPCDialogue异常处理
3. BobberBar竞态条件

**建议测试**:
- 极端值测试（数百万次钓鱼）
- 多人模式并发测试
- 窗口大小改变测试
- 长时间运行测试
