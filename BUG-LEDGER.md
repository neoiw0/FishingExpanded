# FishingExpanded 任务总账

## 接管后的当前状态（2026-08-05）

本文件是当前批次和验收状态的权威总账；历史批次正文保留为证据，不覆盖本节和顶部汇总表。维护入口见 `MAINTENANCE-INDEX.md`。

### 用户最终确认的设计门禁

1. 0级也应用全局20%/1%蓄力槽保护。
2. Mod数量倍数不改变钓鱼动画数量；数量进入背包或原生 `ItemGrabMenu` 溢出路径时才转换。原生特殊鱼饵的多鱼机制保留。
3. 完美/脱杆奖励先按 `+10/+5/+2/+1` 计算，再限制在当前称号区间，最终等级不能跨区间。
4. 所有同时生效的蓄力槽减速规则取最强效果，即适用倍率中的最小值。
5. 五只鱼王除特殊提示外不参与难度、数量、品质、奖励、技能加成、称号、视觉、NPC和20%/1%全局蓄力槽保护。
6. 鱼王 ID 按原生物品 ID 归一化，`163`、`(O)163` 和 `(o)163` 视为同一只鱼王。
7. 高难度星标和 `+0.5` 钓鱼技能加成只在成功钓起后获得；失败或仅进入小游戏不获得。
8. 难度、星标和隐藏加成按玩家独立保存；在线联机主机和每位农场客都必须安装本 Mod。
9. 非鱼类达到8级后，后续成功的实际难度收益为 `+0`。
10. 已有图鉴星标的鱼进入小游戏时只显示一次随机挑战宣言：称号尊敬词每级3种、毅然决然词50种、应战说法20种；不修改任何状态。
11. 小游戏出现时，非鱼王按 `难度等级 / 10 > 当前有效钓鱼等级` 显示升级建议；若同时大于当前钓鱼等级两倍，在建议开头增加“不可能的高难度挑战”提示。

### 当前维护门禁

- v0.5.2 的历史记录包含构建和部署事实，但十项玩家验收仍未完成；每个现象必须独立标记通过、失败或未执行。
- 当前优先验证：视觉放大、原生 `CreateFish` 数量转换/ItemGrabMenu 溢出、单次结算、脱杆收益与区间封顶、0级全局蓄力保护、经验倍率、鱼王完整豁免、成功后星标/开局宣言、钓鱼等级建议和多人隔离。
- `Source\manifest.json`、`FishingExpanded.csproj` 和历史台账的版本号目前不一致；在版本事实统一前不得生成正式发布包。

## 活动批次

| 批次ID | 根因 | 状态 | 创建时间 | R0 | R1 | R2 | 构建 | 部署 | 实测 |
|--------|------|------|----------|----|----|----|----|------|------|
| BATCH-001 | 初始实现 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-002 | 控制台命令 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | N/A | ✅ | ✅ | 待测 |
| BATCH-003 | Transpiler视觉缩放 | ❌ 误判 | 2024-08-04 | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ |
| BATCH-004 | 低端机器性能优化 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-005 | Bug修复与稳定性 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-006 | Transpiler诊断与间接bug | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-007 | Transpiler根因重新定位 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-008 | ObjectPatches栈序列错误 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-009 | 钓鱼动画显示多条鱼 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-010 | 脱杆次数影响等级增长 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-011 | 鱼图鉴显示增强（初次评估） | ⏹️ 推迟 | 2024-08-04 | ✅ | ⏹️ | ⏹️ | N/A | N/A | N/A |
| BATCH-012 | 100级封顶特殊提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-013 | -10级底部提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-014 | 传奇鱼豁免所有规则 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-015 | 非鱼类8级上限 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-016 | 鱼图鉴显示增强（重新实施） | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-024 | 多人数据与鱼获生命周期修正 | ✅ 源码构建完成，待实测 | 2026-08-05 | ✅ | ✅ | ⏳ | ✅ | ⏳ | ⏳ |

---

## BATCH-024：多人数据与鱼获生命周期修正 ✅ 源码构建完成，待实测

**根因**：当前实现把数据写入存档级 `ReadSaveData`，并在 `Farmer.caughtFish` 时扫描背包；原生鱼获物品此时尚未创建，无法可靠覆盖堆叠和 `ItemGrabMenu` 溢出路径。

**R1 实施**：
- 使用玩家自己的 `Farmer.modData` 保存难度、星标和隐藏加成；仅主玩家迁移旧共享数据。
- 规范化原始/限定鱼 ID，统一鱼王识别和图鉴/命令数据键。
- 移除 `caughtFish` 背包扫描，改在原生 `FishingRod.CreateFish` 返回物品时转换数量。
- 将星标写入从 `BobberBar` 构造阶段移到成功捕获后的 `caughtFish` Postfix。
- 待处理鱼获按玩家 ID + 鱼 ID 隔离，返回标题时清理。
- 修正小游戏初始绿条外状态不计为脱杆；星标阈值在成功入口使用原生难度参数兜底。
- 在原生钓鱼经验入口应用经验倍率；鱼王 BobberBar 构造时屏蔽隐藏钓鱼等级读取，保持完整豁免。
- 加入星标鱼开局挑战宣言的展示与本地化随机池，不增加第二个状态所有者。
- 加入小游戏出现时的钓鱼等级建议；使用 `Farmer.FishingLevel` 的有效整数等级，只读比较，不写入玩家或鱼数据。

**R0/R1 状态**：R0 ✅；R1 ✅（源码静态检查与 Release Rebuild 已通过）
**构建**：✅ Release Rebuild，0警告，0错误
**实测**：待多人和原生溢出验收

## BATCH-016: 鱼图鉴显示增强（重新实施） ✅

**根因**：BATCH-011初次评估复杂度过高，用户纠正"仅仅改几个字而已"

**累计修复次数**：1次（初次实施）

**证据链**：
1. 反编译CollectionsPage.full.cs确认createDescription(string id)返回简单字符串
2. 用户反馈BATCH-011复杂度判断错误
3. 确认只需Postfix追加文本即可

**实现内容**：
- **R0取证**：反编译CollectionsPage，定位createDescription方法
- **R1实现**：CollectionsPagePatches.CreateDescription_Postfix
  - 检查currentTab==4（鱼类tab）
  - 豁免传奇鱼（BATCH-014规则）
  - 追加"挑战等级：{称号}（等级{level}） | 钓鱼技能加成：+{bonus}"
- **R1删除**：移除DifficultyManager中对已废弃CollectionsPagePatches.InvalidateCache()的调用

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\（2026-08-05 01:10）
**实测**：⏳ 待玩家验收图鉴显示功能

---

## BATCH-015: 非鱼类8级上限 ✅

**根因**：新需求 - 垃圾、藻类等非鱼物品难度上限8级

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：定义IsNonFish()检查Category != -4
- **R1实现**：
  - SpecialFishHelper.IsNonFish() - 基于Category判断
  - DifficultyManager.GetDifficultyLevel() - 应用maxLevel=8
  - HUDNotifier.ShowSuccessNotification() - 特殊文案"对于{物品名}而言，你已是帝王"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收非鱼类上限功能

---

## BATCH-014: 传奇鱼豁免所有规则 ✅

**根因**：新需求 - 5种传奇鱼（鱼王等）不受任何自定义规则影响

**累计修复次数**：1次（初次实施）

**证据链**：
1. 用户明确指定5种传奇鱼ID：163/682/160/775/159
2. 要求10种随机消息，不使用难度系统、等级加成

**实现内容**：
- **R0设计**：创建SpecialFishHelper统一管理特殊鱼规则
- **R1实现**：
  - SpecialFishHelper.IsLegendaryFish() - 硬编码5种鱼王ID
  - SpecialFishHelper.GetRandomLegendaryMessage() - 10种随机消息
  - BobberBarPatches.Constructor_Postfix - 最优先检查豁免
  - FishingRodPatches.Prefix - 最优先检查豁免
  - CollectionsPagePatches.CreateDescription_Postfix - 豁免图鉴显示

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收传奇鱼豁免功能

---

## BATCH-013: -10级底部提示 ✅

**根因**：新需求 - 难度-10时提供装备升级建议

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowFailureNotification()检测currentLevel<=-10
- **特殊文案**："最平庸的{鱼名}依然太难了，请升级鱼竿，钓鱼等级，使用料理增加钓鱼等级，使用陷阱/浮木渔具等"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收底部提示功能

---

## BATCH-012: 100级封顶特殊提示 ✅

**根因**：新需求 - 达到100级后显示神明提示

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowSuccessNotification()检测newLevel>=100
- **特殊文案**："你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收封顶提示功能

---

## BATCH-011: 鱼图鉴显示增强（初次评估） ⏹️

**根因**：新需求 - 图鉴显示挑战等级和技能加成

**初次评估**：复杂度高，推迟到v0.6.0

**用户反馈**：评估错误，"仅仅改几个字而已"

**后续**：由BATCH-016重新实施

---

## BATCH-010: 脱杆次数影响等级增长 ✅

**根因**：新需求 - 完美钓鱼（0次脱杆）应获得更高奖励

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：BobberBar追踪bobberInBar状态变化计数
- **R1实现**：
  - BobberBarPatches.Update_Prefix - 追踪!___bobberInBar计数PerfectCount
  - BobberBarPatches.GetPerfectCount() - 公开查询接口
  - DifficultyManager.RecordSuccess() - 支持可变levelGain参数
  - FarmerFishingPatches.CaughtFish_Postfix - 根据脱杆次数调用RecordSuccess
- **奖励规则**：0次脱杆→+10，1次→+5，2次→+2，3次及以上→+1

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收脱杆奖励功能

---

## BATCH-009: 钓鱼动画显示多条鱼 ✅

**根因**：FishingRod.pullFishFromWater的numCaught参数同时控制动画和最终数量

**累计修复次数**：1次（初次修复）

**证据链**：
1. v0.5.0测试显示钓到大鱼时立即显示多条鱼飞向玩家
2. pullFishFromWater()的numCaught参数控制for循环生成多个临时精灵
3. 需要分离动画数量（固定1）和最终背包数量（倍数影响）

**实现内容**：
- **R0取证**：反编译FishingRod.pullFishFromWater()和Farmer.caughtFish()
- **R1重构**：
  - FishingRodPatches.Prefix - 仅记录数据到_pendingFish，不修改numCaught
  - FarmerFishingPatches（新类）- Postfix on Farmer.caughtFish()修改Stack属性
  - 字典访问权限从private改为internal static支持跨类访问
- **第一处分歧**：pullFishFromWater()的numCaught参数，保持=1避免动画重复

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收动画功能

---

## BATCH-008: ObjectPatches栈序列错误 ✅

**根因**：BATCH-007 Transpiler成功插入指令但GetDrawScale从未被调用

**累计修复次数**：2次（BATCH-007根本方案错误，BATCH-008修复栈序列）

**证据链**：
1. v0.5.0日志显示Transpiler匹配到ldc.r4 4但无"应用视觉缩放"记录
2. 代码审查发现yield return codes[i]在插入指令之后
3. IL栈错误：新指令压入后才有4f，导致乘法操作数不足

**实现内容**：
- **R0取证**：IL指令执行顺序分析
- **R1修复**：调整yield return顺序
  ```csharp
  // 错误：yield return codes[i]; 在最后
  // 正确：yield return codes[i]; 在if块内最先执行
  if (codes[i].opcode == OpCodes.Ldc_R4 && ...) {
      yield return codes[i];  // 先压入4f
      // 再插入GetDrawScale和Mul
  }
  ```

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-007: Transpiler根因重新定位 ✅

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
- 反编译3个关键方法
- 定位唯一所有者：Object.drawWhenHeld()的scale=4f参数
- 证据保存到_analysis目录

**R1状态**：✅ 已完成
- 删除FarmerPatches全部无效代码
- 实现ObjectPatches正确Patch Object.drawWhenHeld()
- v0.5.0构建成功（2024-08-04 21:16）
- 部署到D:\GGGGG\K1515\Mods\FishingExpanded（2024-08-04 21:17）

**R2状态**：⏳ 待验证
- 基础功能：钓到倍数>15的鱼，手持时是否视觉放大
- 立方根缩放：倍数27应显示为3倍大小
- 非鱼类物品不受影响
- 多人模式独立缩放
- 性能：低频日志，无每帧计算

**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\ 
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-006: Transpiler诊断与间接bug ✅

**根因**：日志显示Transpiler未找到目标调用，间接bug影响稳定性

**发现的问题**：
1. **Transpiler诊断不足** - 无法判断IL匹配失败的原因
2. **整数溢出风险** - DifficultyLevel计算可能溢出
3. **多人缓存冲突** - FarmerPatches的静态缓存在多人模式下可能冲突

**实现内容**：
- 增强Transpiler诊断：记录总指令数、方法调用数、目标调用数
- DifficultyLevel使用long运算避免溢出
- FarmerPatches缓存改为Dictionary<long, ...>按玩家ID隔离

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.3
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待验证（注：v0.5.0已删除FarmerPatches缓存）

---

## BATCH-005: Bug修复与稳定性 ✅

**根因**：代码审查发现5个潜在bug影响稳定性

**发现的Bug清单**：
1. **Bug #1 (严重)**: BobberBarPatches.PeriodicCleanup空引用检查逻辑错误
2. **Bug #2 (中等)**: HUDNotifier.GetFishDisplayName缺少空检查
3. **Bug #3 (轻微)**: NPCDialogueGenerator.GenerateFishPraise缺少Game1.random空检查
4. **Bug #4 (中等)**: CollectionsPagePatches反射调用缺少错误处理
5. **Bug #5 (轻微)**: FarmerPatches Transpiler搜索范围可能不足

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证稳定性提升

---

## BATCH-004: 低端机器性能优化 ✅

**根因**：每帧运行的代码过多，需要支持低端机器

**识别的性能热点**：
1. CollectionsPagePatches.draw_Postfix - 收藏页面每帧遍历所有物品
2. FarmerPatches.GetItemDrawScale - 每次Farmer.draw()都重新计算
3. GiantFishManager.CheckAndTriggerNPCReactions - 每30帧LINQ查询所有NPC
4. BobberBarPatches Update钩子 - 每帧字典查找
5. ModEntry.OnUpdateTicked - 每帧模运算检查

**实现内容**：
- CollectionsPagePatches: 缓存机制，仅在页面切换时重建星标列表
- FarmerPatches: 结果缓存（注：v0.5.0已删除）
- GiantFishManager: 快速路径检查，平方距离替代开方，直接遍历替代LINQ
- ModEntry: NPC检查间隔从30帧增加到60帧

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.4.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证性能提升

---

## BATCH-003: Transpiler视觉缩放 ❌ 误判

**根因**：原Postfix实现无法修改已执行的绘制，需要Transpiler修改IL指令

**错误假设**：Farmer.draw()调用Item.drawInMenu()绘制手持物品

**实际情况**：
- 调用链是：Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
- Farmer.draw()从不调用Item.drawInMenu()
- 搜索了不存在的方法调用

**后果**：
- v0.3.0-v0.4.3的Transpiler从未生效
- 视觉缩放功能完全不工作
- 被误标记为"✅ 已完成"

**修复**：由BATCH-007重新实现正确方案

**R0状态**：❌ 假设错误
**R1状态**：❌ 删除了不存在的代码路径
**R2状态**：❌ 从未验证实际效果
**构建**：✅ v0.3.0（编译通过但功能无效）
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：❌ 视觉缩放从未工作

---

## BATCH-002: 控制台命令系统 ✅

**根因**：手动钓鱼测试各难度等级效率低下，需要快速测试工具

**实现内容**：
- 9个控制台命令（setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus）
- DifficultyManager新增公开方法支持控制台操作
- 创建12KB测试指南文档（TESTING-GUIDE.md）

**R0状态**：✅ 已完成
**构建**：✅ v0.2.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证命令功能

---

## BATCH-001: 钓鱼难度系统初始实现 ✅

**根因**：新功能需求 - 实现渐进式难度钓鱼系统

**实现内容**：
1. 难度追踪系统（成功/失败计数，-10到100等级）
2. 线性倍数计算（难度/数量/品质/保护）
3. HUD通知与9级称号系统
4. 巨型鱼NPC反应（气泡+100随机对话）
5. 收藏品星标显示
6. 隐藏钓鱼等级加成（每星标+0.5）
7. 完整低频诊断日志

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.1.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内完整验证

---

## 当前任务优先级

1. **【紧急】游戏内测试v0.5.2** - 验证9个新功能（5个bug修复+4个新需求）
2. **视觉验证** - 钓到倍数>15的鱼，手持时应该明显放大
3. **传奇鱼验证** - 钓到鱼王显示随机消息，不受难度系统影响
4. **图鉴验证** - Collections菜单查看鱼类描述，底部显示挑战等级和技能加成

---

## 修复历史记录

**累计修复次数统计**：

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 视觉缩放不工作 | 2 | BATCH-003(误判) → BATCH-007(正确方案) → BATCH-008(栈序列修复) |
| 钓鱼动画显示多条鱼 | 1 | BATCH-009(初次修复) |

**治理规则**：同一玩家可见现象的修复次数跨会话、批次、版本和名称累计，不能通过重新分组清零。

---

## 长期追踪项

- **【关键】v0.5.2所有功能需要游戏内验证** - 9个新功能待实测
- **【关键】图鉴显示需要打开Collections菜单验证** - BATCH-016
- 传奇鱼豁免需要实际钓到鱼王验证 - BATCH-014
- 非鱼类上限需要钓到垃圾/藻类验证 - BATCH-015

---

**最后更新**：2026-08-05 01:12
**当前版本**：v0.5.2
**总批次数**：16个（14个完成，1个推迟，1个误判）
**待实测项**：v0.5.2所有9个新功能
