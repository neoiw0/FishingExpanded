# BATCH-007: Transpiler根因重新定位

**创建时间**: 2024-08-04 20:40  
**累计修复次数**: 1次（BATCH-003误判为已完成）  
**当前状态**: 🟡 R1完成，待R2验证

---

## 玩家可见现象

视觉缩放功能完全不工作 - 钓到倍数>15的鱼时，手持鱼的显示尺寸没有放大

---

## 证据链

### 证据1: BATCH-003错误假设
- **假设**: Farmer.draw()调用Item.drawInMenu()绘制手持物品
- **实施**: Transpiler搜索drawInMenu调用并修改scale参数
- **结果**: BATCH-003标记为"✅ 已完成"但从未验证实际效果

### 证据2: v0.4.3日志反证
```
[20:36:13 DEBUG] [Transpiler] 开始分析 Farmer.draw() | 总指令数: 494
[20:36:13 DEBUG] [Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0
[20:36:13 WARN] [Transpiler] ⚠ 未找到 Item.drawInMenu 调用，视觉缩放可能无效
```

**明确反证**: Farmer.draw()根本不调用Item.drawInMenu()

### 证据3: Transpiler增强诊断无效
- BATCH-006增加了详细的IL分析诊断
- 诊断结果证实了方案根本性错误
- 不是"找不到"的问题，而是"不存在"的问题

---

## 根因诊断

### 错误的假设
Farmer.draw()通过Item.drawInMenu()绘制手持物品

### 真实情况（待验证）
Farmer.draw()可能：
1. 直接调用SpriteBatch.Draw()绘制物品纹理
2. 使用Item.draw()而不是drawInMenu()
3. 通过其他辅助方法绘制
4. 完全不在Farmer.draw()中绘制（可能在其他地方）

### 第一处分歧
选择Patch Farmer.draw()的Transpiler方案时，未验证Farmer.draw()是否真的调用了目标方法

---

## R0: 重新取证 ✅ 已完成

### 关键证据
1. **Farmer.draw() → Game1.drawPlayerHeldObject()**  
   - Farmer.draw():6324行：`if (ActiveObject != null && IsCarrying()) { Game1.drawPlayerHeldObject(this); }`
   
2. **Game1.drawPlayerHeldObject() → Object.drawWhenHeld()**  
   - Game1.cs:15173行：`f.ActiveObject.drawWhenHeld(spriteBatch, new Vector2(...), f);`
   
3. **Object.drawWhenHeld() 是唯一所有者**  
   - Object.cs:5345行：`spriteBatch.Draw(texture, objectPosition, ..., 4f, ...);`
   - **固定scale=4f** 是需要修改的唯一目标

### 结论
- Farmer.draw() 从不调用 Item.drawInMenu()
- BATCH-003的Transpiler搜索了不存在的调用链
- 正确方案：Patch Object.drawWhenHeld() 的 scale 参数

---

## R1: 删除旧代码，实现正确方案 ✅ 已完成

### 删除清单（符合R1删除优先审计）
1. ❌ FarmerPatches.Draw_Transpiler - 搜索不存在的drawInMenu调用
2. ❌ FarmerPatches.FindScaleLoadInstruction - 辅助方法
3. ❌ FarmerPatches.IsParameterLoadInstruction - 辅助方法
4. ❌ FarmerPatches.GetItemDrawScale - 错误的缩放计算
5. ❌ FarmerPatches.InvalidateCache - 缓存管理
6. ❌ FarmerPatches._playerCache - 多人缓存字典
7. ❌ FarmerPatches._drawTranspilerLogged - 日志标志
8. ❌ GiantFishManager.OnEnterFarmHouse() 中对 InvalidateCache() 的调用

### 新实现（唯一所有者）
✅ **ObjectPatches.DrawWhenHeld_Transpiler**
- 目标：Object.drawWhenHeld()
- 搜索：`ldc.r4 4` （加载固定scale=4f）
- 插入：`ldarg.0 → call GetDrawScale → mul` （4f * visualScale）
- 调用链：Object实例 → GiantFishManager.GetFishVisualScale()

✅ **ObjectPatches.GetDrawScale()**
- 只处理鱼类物品（Category == -4）
- 使用立方根公式计算visualScale
- 低频诊断：只记录 visualScale > 1.1f 的情况

---

## R2: 验证与测试（当前阶段）

### 必需验证
1. **基础功能** - 钓到倍数>15的鱼，手持时是否视觉放大
2. **立方根缩放** - 倍数27应显示为3倍大小（27^(1/3)=3）
3. **非鱼类物品** - 手持其他物品不应受影响
4. **多人模式** - 各玩家独立缩放，互不干扰
5. **性能** - 低频日志，无每帧计算

### 停止条件
实测确认视觉缩放正常工作，或发现新问题需重新取证

---

## 构建状态
✅ v0.5.0 构建成功（2024-08-04 21:16）

## 部署状态
✅ 已部署到 D:\GGGGG\K1515\Mods\FishingExpanded（2024-08-04 21:17）

## 实测状态
⏳ 待玩家验收

---

## 修复次数统计

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 视觉缩放不工作 | 1 | BATCH-003(误判) |

---

**下一步**: 反编译Farmer.draw()取证
