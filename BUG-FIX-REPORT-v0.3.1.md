# FishingExpanded v0.3.1 - 代码审查与Bug修复报告

**审查时间**：2024-08-04 18:50  
**修复版本**：v0.3.1

---

## ✅ 已修复的Bug

### Bug #1：数量二次乘法 ⚠️ **严重**

**位置**：`FishingRodPatches.cs:42`

**问题描述**：
```csharp
numCaught *= quantityMultiplier;  // 第28行：已经乘过一次
// ...
GiantFishManager.RecordGiantFish(fishId, quantityMultiplier, numCaught * quantityMultiplier); // 又乘一次！
```

**实际影响**：
- 如果倍数是20，numCaught变成20
- 传给GiantFishManager的fishSize是 `20 × 20 = 400` ❌
- 应该是 `20` ✓

**修复方案**：
```csharp
// numCaught 已经是修改后的最终值，不需要再乘一次
GiantFishManager.RecordGiantFish(fishId, quantityMultiplier, numCaught);
```

**影响范围**：
- 巨型鱼记录的fishSize错误（视觉缩放会过度放大）
- NPC对话中的鱼尺寸数字错误

---

### Bug #2：空引用风险（2处） ⚠️

**位置**：`GiantFishManager.cs:86, 169`

**问题描述**：
```csharp
string fishName = ItemRegistry.GetDataOrErrorItem(fishId).DisplayName; // 可能为null
```

**修复方案**：
```csharp
var itemData = ItemRegistry.GetDataOrErrorItem(fishId);
string fishName = itemData?.DisplayName ?? "未知鱼类";
```

**影响范围**：
- 无效fishId导致NullReferenceException崩溃
- 低概率（仅在MOD冲突或数据损坏时）

---

### Bug #3：字符串解析风险 ⚠️

**位置**：`CollectionsPagePatches.cs:51-54`

**问题描述**：
```csharp
string[] itemData = item.name.Split(' ');
if (itemData.Length == 0) continue; // 没有检查item.name是否为null
string fishId = itemData[0]; // itemData[0]可能为空字符串
```

**修复方案**：
```csharp
if (string.IsNullOrEmpty(item.name)) continue;
string[] itemData = item.name.Split(' ');
if (itemData.Length == 0 || string.IsNullOrEmpty(itemData[0])) continue;
string fishId = itemData[0];
```

**影响范围**：
- 收藏品页面可能因异常数据崩溃
- 低概率（仅在MOD冲突时）

---

## ⚠️ 已识别但未修复的问题

### 问题 #4：多人游戏静态字段覆盖 ⚠️ 中等

**位置**：`BobberBarPatches.cs:14-16`

**问题描述**：
```csharp
private static string _currentFishId;
private static int _currentDifficultyLevel;
private static float _originalDifficulty;
```

**影响场景**：
- 分屏多人或多个玩家**同时钓鱼**时
- 静态字段会被后启动的钓鱼事件覆盖
- 导致记录错乱（玩家A的失败可能记到玩家B的鱼上）

**为何未修复**：
- 需要重构为 `Dictionary<BobberBar, FishData>` 实例字典
- 影响范围较大，需要修改3个Patch方法
- 当前大多数玩家是单人游戏，实际触发概率低

**计划修复**：下个版本（v0.4.0）重构为实例化管理

**临时缓解**：
- 单人游戏不受影响
- 分屏多人只要不**同时**钓鱼就不会触发
- 即使触发，最多导致统计数据偏差，不会崩溃

---

### 问题 #5：Transpiler参数查找逻辑 ⚠️ 需验证

**位置**：`FarmerPatches.cs:120-150` 的 `FindScaleLoadInstruction`

**潜在问题**：
- 从`callvirt drawInMenu`往前查找第3个参数
- IL栈上参数顺序是**反向压栈**（从右往左）
- 当前逻辑假设往前数第3个就是scale参数

**为何未修复**：
- 需要实际运行游戏查看日志验证
- 如果Transpiler成功插入日志出现，说明逻辑正确
- 如果视觉缩放不生效，需要调整OpCodes匹配

**验证方法**：
1. 启动游戏，查看是否有 `[Transpiler] ✓ 成功插入视觉缩放逻辑`
2. 使用 `fish_giant 128 30` 命令
3. 拿鱼在手观察是否放大约3倍

**如果验证失败**：
- 添加详细IL指令日志
- 反编译`Farmer.draw()`查看实际IL序列
- 调整`FindScaleLoadInstruction`逻辑

---

### 问题 #6：缺少边界检查 ⚠️ 低风险

**位置**：多个Patches

**问题描述**：
- 没有验证 `Game1.player` 是否为null
- 没有验证 `Game1.currentLocation` 是否为null
- 某些边缘情况（加载中、切换场景）可能崩溃

**为何未修复**：
- 所有Patches已经包裹在try-catch中
- 异常会被捕获并记录，不会导致游戏崩溃
- 实际触发概率极低

**建议**：长期优化时添加防御性检查

---

## 📊 代码审查统计

| 类别 | 数量 |
|------|------|
| 已修复的严重bug | 1 |
| 已修复的空引用风险 | 2处 |
| 已修复的解析风险 | 1 |
| 已识别待修复问题 | 3 |
| 总审查文件数 | 13个.cs文件 |
| 代码行数 | ~1500行 |

---

## 🔧 修复后的测试重点

### 必须验证的功能：

1. **巨型鱼记录**
   ```bash
   fish_giant 128 20
   # 验证日志中 fishSize = 20（不是 400）
   ```

2. **NPC对话**
   ```bash
   fish_giant 149 30
   # 拿鱼靠近NPC，验证对话中的尺寸是 30（不是 900）
   ```

3. **视觉缩放**
   ```bash
   fish_giant 128 50
   # 拿鱼在手，验证是否放大约 3.7 倍（50^(1/3)）
   ```

4. **收藏品页面**
   - 添加星标后打开Collections页面
   - 验证不会因空字符串崩溃

5. **命令系统**
   ```bash
   fish_setlevel 128 50
   fish_info 128
   # 验证命令已注册并正常工作
   ```

---

## 📝 版本更新日志

**v0.3.1 (2024-08-04 18:50)**
- 🐛 修复：数量二次乘法导致fishSize错误
- 🐛 修复：ItemRegistry空引用风险（2处）
- 🐛 修复：收藏品页面字符串解析风险
- 🐛 修复：manifest.json缺少EntryDll字段
- 📝 识别：多人游戏静态字段覆盖问题（待v0.4.0修复）
- 📝 识别：Transpiler参数查找逻辑待验证

**v0.3.0 (2024-08-04 17:45)**
- ✨ 新增：Transpiler视觉缩放实现
- ✨ 新增：9个控制台测试命令
- ✨ 新增：完整测试指南文档

**v0.2.1 (2024-08-04 17:19)**
- ✨ 初始实现：所有核心功能

---

## 🎯 下一步行动

1. **立即**：重启游戏测试修复后的版本
2. **验证**：巨型鱼fishSize数值是否正确
3. **验证**：Transpiler是否成功插入（查看日志）
4. **验证**：视觉缩放是否生效
5. **反馈**：如有问题提供完整日志

---

**当前状态**：v0.3.1已编译并部署到游戏目录，等待游戏测试验证。
