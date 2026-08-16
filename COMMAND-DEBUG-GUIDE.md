# 控制台命令未注册问题排查指南

## 问题现象
```
fish_giant 128 30
[SMAPI] Unknown command 'fish_giant'; type 'help' for a list of available commands.
```

## 已验证项
✅ DLL已正确部署（哈希值匹配：fb493a4fcb359d8ba8e18ef3f6b3e312）
✅ 命令注册代码已存在（ModEntry.cs:97-99）
✅ 编译成功（0警告0错误）

## 可能原因

### 原因1：游戏未重启加载新DLL
**现象**：修改MOD后未完全退出游戏
**解决**：
1. 完全退出星露谷（不是返回主菜单）
2. 关闭SMAPI窗口
3. 重新启动 StardewModdingAPI.exe
4. 加载存档后输入 `help` 验证是否有 fish_* 命令

### 原因2：Entry()中发生异常
**现象**：初始化代码抛出异常，导致RegisterConsoleCommands()未执行
**排查**：查看SMAPI控制台中是否有以下日志：

**正常情况应看到**：
```
[FishingExpanded] FishingExpanded 正在初始化...
[FishingExpanded] Harmony Patches 注册成功
[FishingExpanded] 控制台命令注册完成 (输入 help 查看所有命令)
[FishingExpanded] FishingExpanded 初始化完成
```

**异常情况会看到**：
```
[FishingExpanded] 初始化失败: <异常信息>
```

**如果看到异常**：
1. 复制完整异常栈追踪
2. 检查是否缺少依赖DLL
3. 验证Harmony版本兼容性

### 原因3：SMAPI版本过旧
**现象**：Helper.ConsoleCommands.Add() API不存在
**排查**：
```bash
# 在SMAPI控制台输入
version
```

**要求**：SMAPI >= 4.0.0

**解决**：升级SMAPI到最新版本

### 原因4：MOD未正确加载
**现象**：manifest.json配置错误或依赖缺失
**排查**：在SMAPI启动日志中查找：
```
Skipped mods
------------
   These mods could not be added to your game.

   - FishingExpanded <原因>
```

**可能原因**：
- MinimumApiVersion设置过高（当前：4.0.0）
- UniqueID冲突
- DLL损坏或编译目标框架不匹配

**解决**：
1. 检查 `D:\GGGGG\K1515\Mods\FishingExpanded\manifest.json`
2. 确认 MinimumApiVersion 与实际SMAPI版本匹配
3. 删除MOD文件夹重新部署

## 立即排查步骤

### 步骤1：查看SMAPI启动日志
在SMAPI控制台窗口向上滚动，找到MOD加载阶段，查找：
```
Loading mods...
   Loaded FishingExpanded 0.3.0 by neoiw | <描述>
```

如果看到 "Loaded"，继续查找初始化日志：
```
[FishingExpanded] FishingExpanded 正在初始化...
[FishingExpanded] 控制台命令注册完成
```

### 步骤2：输入help命令验证
```
help
```

向下滚动查找是否有 `fish_setlevel` 等命令。

### 步骤3：测试最小命令
如果上述都正常，但命令仍不可用，尝试输入：
```
help fish
```
查看是否有任何以 "fish" 开头的命令。

### 步骤4：检查命令注册时机
命令在 `Entry()` 中注册，但此时存档未加载。
尝试：
1. 启动游戏（主菜单）
2. 输入 `help` 查看命令是否已注册
3. 如果主菜单就有命令，说明注册成功
4. 如果加载存档后命令消失，说明有其他问题

## 应急解决方案

如果始终无法注册命令，可以手动测试功能：

### 手动测试难度系统
```bash
# 1. 去海边钓鱼
# 2. 观察SMAPI日志：
[DifficultyManager] 钓鱼成功记录 | 鱼ID: 128 | 等级变化: 0 → 1
[HUDNotifier] 成功提示显示 | 称号: 精英

# 3. 连续钓多条同种鱼观察倍数变化
```

### 手动测试视觉缩放
编辑 `FarmerPatches.cs:166` 临时返回固定缩放值：
```csharp
public static float GetItemDrawScale(Farmer farmer)
{
    return 5.0f; // 临时固定5倍缩放测试
}
```
重新编译部署后，任何手持物品都会放大5倍。

### 手动测试NPC反应
修改 `GiantFishManager.cs` 降低触发阈值：
```csharp
// 从 multiplier > 15 改为 multiplier > 1
if (multiplier > 1)
{
    RecordGiantFish(fishId, multiplier, totalQuantity);
}
```
这样钓到任何鱼都会触发NPC反应。

## 日志收集请求

如果问题依然存在，请提供以下信息：

1. **SMAPI完整启动日志**（从启动到加载存档完成）
   - 特别是 "Loading mods" 部分
   - 特别是 "[FishingExpanded]" 开头的所有行

2. **help命令完整输出**

3. **SMAPI版本信息**（输入 `version` 的结果）

4. **manifest.json内容**
   ```bash
   cat D:\GGGGG\K1515\Mods\FishingExpanded\manifest.json
   ```

5. **是否有其他MOD**（可能存在命令冲突）

## 下一步行动

**立即尝试**：
1. 完全退出游戏和SMAPI
2. 重新启动 `StardewModdingAPI.exe`
3. 加载存档
4. 输入 `help` 查看命令列表
5. 如果仍无命令，提供上述日志信息

**如果重启后命令出现**：
- 问题已解决，继续测试
- 记录：MOD更新后需要完全重启游戏

**如果重启后命令仍不存在**：
- 查看SMAPI启动日志中的 [FishingExpanded] 行
- 检查是否有 ERROR 或异常信息
- 提供日志以便进一步诊断

---

**最可能的原因**：游戏未完全重启。请先尝试完全退出并重新启动！
