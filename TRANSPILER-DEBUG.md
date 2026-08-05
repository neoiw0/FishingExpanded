# Transpiler失效诊断报告

**问题**: 视觉缩放Transpiler完全失效
**日志**: `[20:08:58 WARN] [Transpiler] ⚠ 未找到 Item.drawInMenu 调用，视觉缩放可能无效`

## 可能的原因

### 原因1: Farmer.draw()不直接调用Item.drawInMenu()
- Farmer.draw()可能使用其他方法绘制手持物品
- 可能通过Item.draw()或其他绘制方法
- 需要反编译确认实际调用链

### 原因2: Harmony Patch目标错误
- 当前Patch: `[HarmonyPatch("draw", new Type[] { typeof(SpriteBatch) })]`
- 可能需要Patch不同的重载
- Farmer类可能有多个draw方法

### 原因3: 方法签名查找不准确
- 当前查找8参数版本的drawInMenu
- 可能需要查找其他参数数量的重载
- 或者需要查找call而不是callvirt

## 修复方案

### 方案A: 改用Postfix直接修改CurrentItem的Scale
优点:
- 不依赖IL指令匹配
- 实现简单可靠
- 容易调试

缺点:
- 无法修改已经执行的绘制
- 需要在绘制前设置scale

### 方案B: Patch Item.drawInMenu()方法本身
优点:
- 直接在绘制方法内修改
- 不依赖Farmer.draw()的实现

缺点:
- 影响所有Item绘制，不仅手持物品
- 需要精确判断上下文

### 方案C: 查找Farmer.draw()中所有绘制调用
优点:
- 更广泛的匹配
- 不依赖特定方法名

缺点:
- 可能误匹配其他绘制

## 推荐方案: A + 诊断增强

1. 立即实现方案A作为fallback（保证功能可用）
2. 增强Transpiler诊断，输出更多信息
3. 反编译Farmer.draw()确认实际实现
