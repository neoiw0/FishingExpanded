# FishingExpanded 当前工作交接

**最后更新**: 2026-08-06（视觉范围修正 + 图鉴加成修正，R0/R1/R2/构建完成）
**当前状态**: BATCH-024 视觉路径范围修正（小游戏鱼标/结算面板不缩放，飞行动画中心锚点，落地真鱼与手持底边中点锚点）与 BATCH-025 图鉴“钓鱼技能加成”按鱼种独立显示（仅星标鱼 +0.5）均已完成 R0/R1/R2；候选 `B8BDAEFA...` 启动崩溃（coreclr AV，二分定位到位置补偿注入 `Add` 指令），已修复为 `2B0EB0F1...`（消费式 `AdjustLandingFishPosition`）并部署、启动核验通过（15:40 会话）；Runtime/真实画面验收未完成。权威状态读取 `BUG-LEDGER.md`、`MAINTENANCE-INDEX.md`、`Governance/Archive-ReadOnly/BATCH-024-Multiplayer-Fishing-Lifecycle.md`、`Governance/Archive-ReadOnly/BATCH-025-Collection-Bonus-Per-Fish.md`。

> 下一步：部署门禁（目标进程/DLL 占用检查、备份、源/目标哈希、不覆盖 `config.json`）；真实游戏验收（每个现象独立标记通过/失败/未执行）：小游戏鱼标保持原生 2f、结算面板与示意图保持原生 4f、飞行动画约 3.00 倍中心轨迹不漂移、立在玩家身边点击收包的落地真鱼约 3.00 倍且底边中点不漂移、手持底边中点不漂移、图鉴仅星标鱼显示 +0.5。工具约束：本机 `apply_patch` 与 `git apply` 不可用，编辑使用 PowerShell 精确子串替换，详见 `LOCAL-PITFALLS.md`。

---

## v0.5.2 完整功能清单

### 第一阶段：v0.5.1修复（5个问题）

#### BATCH-008: ObjectPatches Transpiler栈序列错误 ✅

### BATCH-008: ObjectPatches Transpiler栈序列错误 ✅
- **问题1**：视觉缩放失败（GetDrawScale从未被调用）
- **根因**：yield return顺序错误，ldc.r4 4在乘法之后执行
- **修复**：调整IL指令顺序，先压入4f再插入乘法逻辑

### BATCH-009: 钓鱼动画显示多条鱼 ✅
- **问题2**：钓到大鱼时直接显示多条鱼飞向玩家
- **根因**：FishingRodPatches.Prefix修改numCaught参数影响动画
- **修复**：
  - Prefix只记录数据，不修改numCaught
  - 新增Farmer.caughtFish的Postfix修改最终进入背包的数量
  - 数量转化延迟到物品真正进背包时

### BATCH-010: 脱杆次数影响等级增长 ✅
- **问题3**：新规则 - 0次脱杆+10，1次+5，2次+2，3次及以上+1
- **实现**：
  - BobberBar追踪脱杆次数（鱼不在绿条内时计数）
  - DifficultyManager.RecordSuccess()支持可变等级增长
  - Farmer.caughtFish的Postfix根据脱杆次数调用RecordSuccess

#### BATCH-011: 鱼图鉴显示（初次评估） ⏹️
- **问题4**：Collections菜单显示"挑战等级：伯爵（等级12） | 钓鱼技能加成：+2.5"
- **初始状态**：设计已更新到GAME-DESIGN.md，代码实现推迟到v0.6.0
- **用户反馈**：复杂度评估错误，"仅仅改几个字而已"
- **最终实现**：由BATCH-016重新实现

### BATCH-012: 100级封顶特殊提示 ✅
- **问题5**：封顶后显示"你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。"
- **实现**：HUDNotifier.ShowSuccessNotification()检测newLevel>=100并显示特殊文案

### 第二阶段：v0.5.2新需求（4个功能）

#### BATCH-013: -10级底部提示 ✅
- **新需求**：难度-10时特殊提示装备建议
- **实现**：HUDNotifier检测currentLevel<=-10，显示"最平庸的{鱼名}依然太难了，请升级鱼竿..."

#### BATCH-014: 传奇鱼豁免所有规则 ✅
- **新需求**：5种传奇鱼（鱼王等）不受任何自定义规则影响
- **实现**：
  - SpecialFishHelper.IsLegendaryFish()识别5种鱼王
  - BobberBarPatches和FishingRodPatches最优先检查豁免
  - 显示10种随机消息："嗷！孤傲的王！"等

#### BATCH-015: 非鱼类8级上限 ✅
- **新需求**：垃圾、藻类等非鱼物品上限8级
- **实现**：
  - SpecialFishHelper.IsNonFish()检查Category != -4
  - DifficultyManager.GetDifficultyLevel()应用maxLevel=8
  - HUDNotifier特殊文案："对于{物品名}而言，你已是帝王"

#### BATCH-016: 图鉴页面显示增强 ✅
- **用户纠正**：BATCH-011复杂度判断错误
- **实现**：
  - CollectionsPagePatches.CreateDescription_Postfix
  - 追加"挑战等级：{称号}（等级{level}） | 钓鱼技能加成：+{bonus}"
  - 仅在鱼类tab生效，传奇鱼豁免

---

## 构建状态

**历史记录称 v0.5.2 构建成功**（2026-08-05 01:10）：
- 0警告 0错误
- 输出：D:\GGGGG\FishingExpanded\Source\bin\Release\net6.0\

---

## 部署状态

**✅ 已部署**（2026-08-05 01:10）：
- FishingExpanded.dll → D:\GGGGG\K1515\Mods\FishingExpanded\
- 部署命令执行成功
- 游戏进程已关闭，无DLL锁定

---

## 准确的下一步

**待用户验收**：
1. 用户启动游戏测试v0.5.2
2. 验收9个功能（见下方清单）
3. 读取SMAPI日志确认所有Patch正确应用
4. 反馈实测结果或新问题

---

## 验收清单（v0.5.2）

### 第一阶段功能（5个bug修复）

**BATCH-008（视觉缩放）**：
- [ ] 钓到倍数>15的鱼，手持时视觉放大
- [ ] SMAPI日志有`[ObjectPatches] 应用视觉缩放`记录
- [ ] 倍数27的鱼约3倍大小

**BATCH-009（钓鱼动画）**：
- [ ] 钓到倍数100的鱼，动画只显示1条
- [ ] 背包收到100条鱼
- [ ] 难度等级只增长1次

**BATCH-010（脱杆次数）**：
- [ ] 0次脱杆（完美）→ 等级+10
- [ ] 1次脱杆 → 等级+5
- [ ] 2次脱杆 → 等级+2
- [ ] 3次及以上 → 等级+1

**BATCH-012（封顶提示）**：
- [ ] 达到100级的鱼显示"你已经成为{鱼名}中的神明..."
- [ ] 未封顶鱼仍显示正常挑战提示

### 第二阶段功能（4个新需求）

**BATCH-013（底部提示）**：
- [ ] 难度-10时显示"最平庸的{鱼名}依然太难了，请升级鱼竿..."

**BATCH-014（传奇鱼豁免）**：
- [ ] 钓到鱼王（Legend/MutantCarp/Angler/Glacierfish/Crimsonfish）显示随机消息
- [ ] 传奇鱼不受难度系统、等级加成影响
- [ ] SMAPI日志有"传奇鱼（鱼王）豁免规则"记录

**BATCH-015（非鱼上限）**：
- [ ] 垃圾/藻类等达到8级后显示"对于{物品名}而言，你已是帝王"
- [ ] 非鱼类不继续增加难度

**BATCH-016（图鉴显示）**：
- [ ] 打开图鉴（Collections菜单）→ 鱼类tab
- [ ] 查看任意普通鱼描述，底部显示"挑战等级：{称号}（等级{level}） | 钓鱼技能加成：+{bonus}"
- [ ] 传奇鱼（鱼王）不显示额外信息

---

## 关键文件路径

| 文件 | 路径 |
|---|---|
| v0.5.2 DLL（已部署） | D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll |
| 新增Utils类 | Source/Utils/SpecialFishHelper.cs |
| 新增Patches | Source/Patches/CollectionsPagePatches.cs |
| 游戏设计（已更新） | GAME-DESIGN.md |
| 当前总账 | BUG-LEDGER.md |
| 维护台账索引 | MAINTENANCE-INDEX.md |

---

## 总账同步状态

✅ 已由接管整理完成：BATCH-008 到 BATCH-016 的当前索引和待测门禁以 `BUG-LEDGER.md` 为准。旧批次正文中的“待同步”或旧阶段文字只作为历史证据，不再作为当前行动指令。

## 接管后的最终规则

- 0级应用全局20%/1%蓄力槽保护。
- 背包溢出使用原生 `ItemGrabMenu`。
- 完美钓鱼原始收益为 `+10`，但最终等级不能跨称号区间；其他脱杆收益为 `+5/+2/+1`。
- 蓄力槽规则冲突时取最强效果，即适用倍率中的最小值。
- 五只鱼王除特殊提示外不接受难度、数量、品质、奖励、技能、称号、视觉、NPC和全局蓄力槽修改。

---

**下一位接手者行动**：
1. 先读 `AGENTS.md`、`MAINTENANCE-INDEX.md`、`BUG-LEDGER.md` 和 `GAME-DESIGN.md`
2. 统一 manifest、csproj 和发布台账的版本事实
3. 按 `TESTING-GUIDE.md` 执行一次游戏内验收，并为每个现象保存独立 SMAPI 证据
4. 若出现新失败，按 `FISHING-BATCH-REPAIR-SOP.md` 新开唯一活动批次，不直接改旧历史报告
