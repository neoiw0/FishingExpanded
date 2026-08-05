# BATCH-007 R0取证完成报告

**时间**: 2024-08-04 21:00  
**阶段**: R0 → R1转换

---

## R0取证结论

### 证据链完整性：✅

1. **Farmer.draw()源码证据**（行6324-6327）
   ```csharp
   if (ActiveObject != null && IsCarrying())
   {
       Game1.drawPlayerHeldObject(this);
   }
   ```

2. **Game1.drawPlayerHeldObject()源码证据**（行15173）
   ```csharp
   f.ActiveObject.drawWhenHeld(spriteBatch, new Vector2((int)num, (int)num2), f);
   ```

3. **Object.drawWhenHeld()源码证据**（行5345）
   ```csharp
   spriteBatch.Draw(texture, objectPosition, dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex), 
       Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
   ```

### 根因确认

**BATCH-003的错误**：
- 假设：Farmer.draw()调用Item.drawInMenu()
- 实际：Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld() → SpriteBatch.Draw(scale: 4f)

**第一处分歧**：
选择Transpiler Patch Farmer.draw()寻找drawInMenu()调用时，未反编译验证假设

**唯一所有者**：
`Object.drawWhenHeld()`方法的第5345行SpriteBatch.Draw()调用，使用固定scale=4f

---

## R1实施方案

### 删除无效代码（R1要求）

1. **删除FarmerPatches.cs整个Draw_Transpiler方法**
2. **删除FarmerPatches.cs的辅助方法**：
   - FindScaleLoadInstruction()
   - IsParameterLoadInstruction()
   - GetItemDrawScale()
   - InvalidateCache()
3. **删除缓存字段**：_playerCache, _drawTranspilerLogged

### 实现正确方案

**目标**：Patch `StardewValley.Object.drawWhenHeld()`

**技术方案**：Transpiler修改IL指令
- 定位：`ldc.r4 4` （加载浮点常量4.0）
- 修改为：`ldc.r4 4` → `ldarg.0` → `call GetDrawScale` → `mul`
- 效果：scale从固定4f变为`4f * visualScale`

**代码位置**：新建`ObjectPatches.cs`

### 性能考虑
- Object.drawWhenHeld()每帧调用一次（持有物品时）
- 使用现有的多人缓存机制（_playerCache已在GiantFishManager中）
- 不影响非鱼类物品

---

## R2验证计划

1. **场景验证**：钓到倍数>15的鱼，观察手持时尺寸放大
2. **性能验证**：多人模式下无缓存冲突
3. **回归验证**：非鱼类物品绘制不受影响

---

**下一步**：删除FarmerPatches无效代码，实现ObjectPatches
