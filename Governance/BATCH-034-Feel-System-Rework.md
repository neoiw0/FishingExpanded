# FishingExpanded 根因批次：BATCH-034 手感系统改版（删除隐藏等级·皇冠进度·帧率解耦·跳鱼文案）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前唯一执行批次 | BATCH-034（2026-08-09 用户多轮确认设计的玩法改版批次） |
| 本轮用户问题范围 | ① 隐藏钓鱼等级加成整体删除；② 皇冠规则（Mod 鱼只显示皇冠无加成、原版 5 传奇一次给皇冠、可计数皇冠 61 颗做手感终点）；③ 取消绿条外钳制（BATCH-033 机制整体退休）；④ 手感系统（α 线性：按下一瞬间速度 30px/帧、无加速度/阻尼/惯性、撞边完全钳制）；⑤ 帧率解耦覆盖整个钓鱼小游戏；⑥ 加速度增幅 <98→10%、≥98→100%；⑦ 跳鱼瞬移文案（上跳“鱼跃”/下跳“甩尾！”各 15 条，停留 1 秒淡出） |
| 本批次纳入 Case | FE-034-1（删除隐藏等级）、FE-034-2（皇冠/α 进度）、FE-034-3（取消绿条外钳制）、FE-034-4（手感+帧率解耦）、FE-034-5（加速度阈值 98）、FE-034-6（跳鱼文案） |
| 共享第一处分歧与所有权链证据 | 各现象独立验收；FE-034-1/2 共享 DifficultyManager 存档数据所有者；FE-034-3/4/5/6 共享 BobberBar 小游戏所有者（原生 update 边界）；同批实现一次构建部署 |
| 排队或暂不纳入 Case | 无 |
| 合并/拆分决定及依据 | 六现象不共享同一处代码错误（是设计改版而非 Bug），但所有权链相邻，同一批次实现 |
| 本轮准入证据 | 用户 2026-08-09 最终确认：1.隐藏等级全都整体删除，Mod 鱼无任何加成只显示皇冠；2.只有原版 5 条属传奇鱼；3.撞边钳制线性分配可以；4.跳鱼文案只要瞬移到上/下 25% 就显示；5.其余设计确认。此前确认：α=已收集可计数皇冠数÷61、终点 30px/帧、<98→10%、鱼跳保持 |
| 现有实现复核 | 隐藏加成链：FishDifficultyData.FishingLevelBonus → DifficultyManager 四处 ×0.2 → FarmerFishingLevelPatches.FishingLevel_Getter_Postfix → 图鉴/建议文案；绿条外钳制：BobberBarPatches Prefix/Postfix（BATCH-033，含 1 次反证修正）；手感/帧率：当前无；跳鱼：BATCH-028 状态机（冷却→1s 检测→0.5s 延迟→瞬移）；加速度增幅：BATCH-029 ≤88→30%、≥89→100% |
| 允许修改范围 | Source：DifficultyManager.cs、FishDifficultyData.cs、BobberBarPatches.cs、FarmerFishingLevelPatches.cs、CollectionsPagePatches.cs、FishingRodPatches.cs（传奇一次皇冠入口）、ModEntry.cs、i18n zh/default；文档：GAME-DESIGN.md、BUG-LEDGER.md、TESTING.md、TESTING-GUIDE.md、本卡 |
| 冻结 Case/禁止范围 | 不改 WIKI.md；不覆盖部署 config.json（部署目录无该文件）；传奇鱼难度/数量/品质/称号/视觉/NPC 豁免保持不变；不启动游戏除非用户授权部署 |

| 字段 | 值 |
|---|---|
| 批次目标 | 6 项用户确认修改一次实现+构建（部署待用户授权） |
| 可观测性决定 | 构造日志增加 α（手感进度）；跳鱼日志增加文案方向；Transpiler 助手方法不逐帧日志（纯数学换算）；删除隐藏等级为静态合同可证免诊断；帧率解耦 60fps 行为不变由公式静态证明 |
| 自动化验收决定 | 复用真实 BobberBar 小游戏路线 + fish_setlevel/fish_addstars 控制台；存档影响=新增 CollectionStars 传奇条目（可计数皇冠数派生自 CollectionStars∩原生61鱼集合，无新持久字段）；成功判据=60fps 下 α=0 与原版一致、α=1 按下一瞬间 30px/帧且撞边停住、跳鱼出现文案；失败判据=栈序注入非法崩溃、α=0 行为漂移、文案不出现；关闭条件=验收完成或用户撤销 |
| 当前阶段 | R0/R1/R2 完成，构建通过，已部署；真实验收待用户执行（六 Case 独立标记） |
| 当前工作树 | 部分修改（BATCH-032/033 未提交 + 本轮；来源已核对） |
| 最后构建 | 2026-08-09 Release 构建通过（0 警告 0 错误） |
| 唯一下一步 | 实机游玩验收：用户按 TESTING-GUIDE 3.10 实测（`fish_addstars`/`fish_bonus` 可在 SMAPI 控制台输入，我读日志核验）；六 Case 独立标记 |

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-034-1 删除隐藏等级 | R0 ✅ R1 ✅（rg 无残留） | 部署+实测 |
| FE-034-2 皇冠/α 进度 | R0 ✅ R1 ✅（α 派生自 CollectionStars，无新持久字段） | 部署+实测 |
| FE-034-3 取消绿条外钳制 | R0 ✅（BATCH-033 机制整体退休） | 部署+实测 |
| FE-034-4 手感+帧率解耦 | R0 ✅（Transpiler 双验证通过） | 部署+实测 |
| FE-034-5 加速度阈值 98 | R0 ✅ | 部署+实测 |
| FE-034-6 跳鱼文案 | R0 ✅（瞬移即显示，方向=目标区） | 部署+实测 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | A06974D99983A2B25D9B45BCEF52F2CBBE6FD1ED3827808FBC50BA4DE73FCE19（2026-08-09 Release） |
| 部署状态 | 已部署 2026-08-09 10:10（备份 `DeploymentBackups\FishingExpanded-20260809-101014-pre-BATCH034`；目标 DLL/i18n 源-目标哈希核验一致；目标目录无 config.json） |
<!-- CURRENT-STATE-END -->

## 设计确认（2026-08-09 用户逐条确认）

1. **隐藏等级整体删除**：FishingLevelBonus 字段、FarmerFishingLevelPatches.FishingLevel_Getter_Postfix、图鉴“钓鱼条长度额外加成：+0.2”文案、4.6 建议中的加成表述全部删除。其他 Mod 鱼没有任何加成，只显示皇冠。
2. **只有原版 5 条属传奇鱼**：159/160/163/682/775；扩展传奇 898–902 按普通鱼规则。传奇鱼钓到一次直接给皇冠（无难度门槛），计入手感进度 α。
3. **α = 玩家已收集可计数皇冠数 ÷ 61**（56 原生普通真鱼 + 5 原版传奇；`_analysis\Fish-data-extracted.txt` 已核 61 条真鱼清单）。
4. **终点手感（α=1）**：按下瞬间速度=30px/帧、松开瞬间=30px/帧（无加速度/条内阻尼/惯性）；撞边完全钳制（不反弹、速度归零）；从 0 皇冠到 61 皇冠线性分配（含撞边钳制行为）。
5. **帧率解耦覆盖整个钓鱼小游戏**：绿条运动、鱼运动、平滑、随机概率 delta-time 基准，60fps 行为与现在完全一致。
6. **加速度增幅**：难度>100 时等级 <98 → 10%、≥98 → 100%（原 ≤88→30%、≥89→100%）。
7. **跳鱼文案**：高难度跳机制保持；每次瞬移显示文案——上跳（目标 0~133）“鱼跃”类 15 条随机、下跳（目标 399~532）“甩尾！”类 15 条随机；停留 1 秒后淡出；贴鱼绘制（小游戏内鱼旁边）。
8. **取消绿条外钳制**（BATCH-033 机制整体删除，含该机制 1 次反证记录一并归档为历史）。

## R0 实施与验证记录（2026-08-09）

- **FE-034-1**：删除 `FishDifficultyData.FishingLevelBonus`、`FarmerFishingLevelPatches` Getter 注入与传奇抑制（补 `using System`）、`CollectionsPagePatches` 加成行、`DifficultyManager` 四处 ×0.2、i18n `fishingBonus`；`rg` 残留检查通过。
- **FE-034-2**：`DifficultyManager.CountableCrownTarget=61`（56 普通 + 5 原版传奇，含 Goby 与扩展传奇 898–902；清单核对 `_analysis\Fish-data-extracted.txt`）；`GetCountableCrownCount` = `CollectionStars ∩ 61 集合`（Mod 鱼皇冠只显示不计 α，无新持久字段）；`RecordLegendaryCatch` 仅 `PullFishFromWater_Prefix` 成功传奇分支调用（失败不经过）；`NormalizeData` 不再排除传奇星标。
- **FE-034-3**：BATCH-033 绿条外钳制 Prefix/Postfix 相关逻辑整体删除（不再有“进入绿条则移动不生效”/退回逻辑）。
- **FE-034-4/6**：`BobberBarPatches` Transpiler 注入 8 类点：概率 ×3、漂移 ×2、`GetAccelerationBoost`（dup 保留实例）、`GetSmoothFactor`、`ApplyFishPosition`、`ApplyBarInput`、`ApplyBarPosition`、`ApplyBounce` ×2；跳鱼文案 = 每次瞬移完成时按目标区（≤133 上跳/≥399 下跳）从 i18n 各 15 条随机，贴鱼绘制 1s 停留 + 0.5s 淡出；`BobberBar.draw` Postfix 纯显示。
- **FE-034-5**：`GetAccelerationBoost` 等级 ≥98 → 100%、<98 → 10%（d≤100 原生不变）。
- **Transpiler IL 排障（InvalidProgramException）**：两处注入栈序错误已修复——① `ApplyBarInput`：原生 `this.bobberBarSpeed += num5` 在 add 处栈为 `[inst(stfld), speed, num5]`，原“ldarg.0+call”把实例压到参数之上导致 JIT 失败；改为整体替换语句 `ldarg.0; dup; dup; ldfld; ldloc; call`（三实例引用分别供 ldfld/call/stfld）。② `ApplyBounce`：div 处栈为 `[inst, bounced]`，原“ldarg.0+call”同样错位；改为整体替换 `(0f-speed)*2f/3f` 段（两处反弹共用模式）。
- **验证**：`_analysis\batch034-transpiler-debug`（CFG 分支感知类型模拟，含标签栈深一致性、实例调用 this、int32/bool 合并，1313 块通过）+ `_analysis\batch029-transpiler-check --patchall`（全部补丁应用 + JIT 编译通过；harness 增加 DispatchProxy IMonitor 桩解决无 SMAPI 宿主下 Transpiler 日志 NRE）双验证通过；构建 0 警告 0 错误。
- **R2 推演**：α=0 时 60fps 各公式恒等于原生（`GetFrameScale` 返回 1）；α=1 终点 30px/帧、撞边完全钳制（`ApplyBounce` ×(1-α) 归零）；帧率解耦 `1-(1-p)^k` 统计一致；多人按玩家隔离（α 按玩家皇冠）；存档无新字段（α 派生）；性能无逐帧日志；跳鱼文案纯显示无状态写入。

## Closeout（逻辑检查点，2026-08-09）

- **Git 基线**：HEAD `cbe3ea2`（BATCH-024~032 部署检查点）；工作树混合来源——BATCH-032/033 未提交修改（历史会话）+ 本轮 BATCH-034 修改 + 未跟踪治理卡/分析工具。未创建 git 提交：用户未授权提交，且按治理禁止 `git add -A` 吸收混合来源；需提交时按明确路径拆分。
- **构建/部署**：Release 0 警告 0 错误，DLL `A06974D99983A2B25D9B45BCEF52F2CBBE6FD1ED3827808FBC50BA4DE73FCE19`；已部署 2026-08-09 10:10（备份 `DeploymentBackups\FishingExpanded-20260809-101014-pre-BATCH034`，源-目标哈希一致）。
- **部署后审计**：对已部署 DLL 重跑 CFG 类型模拟（1313 块 TYPE SIM OK）、`--patchall`（PASS）、单方法 JIT（PASS）；ilspycmd 反编译抽查——`GetAlpha`/`ApplyBarInput`/`ApplyBounce`/`RecordLegendaryCatch`/`CountableCrownTarget=61` 存在，`FishingLevelBonus`/`GetFishingLevelBonus`/`fishingBonus`/`IsInGreenBar` 零残留，两处注入序列（`Ldarg_0; Dup; Dup; ...`）与源码一致；部署 i18n 85+85 键、跳鱼文案 30 条齐备。
- **真实运行加载核验（2026-08-09 10:16，用户授权启动）**：SMAPI 启动真实加载通过——`FishingExpanded 初始化完成`、`Harmony Patches 注册成功`、落地真鱼缩放注入（缩放3/补偿3）、Object.drawWhenHeld 注入、`存档加载完成`、`可计数皇冠: 3/61`（新 GetCountableCrownCount/α 代码在真实存档上运行）；无 InvalidProgramException、无补丁失败；日志 `SMAPI-latest.txt` 加载检查点存档 `RuntimeEvidence\20260809-BATCH034-RUNTIME-ACCEPT-01\SMAPI-load-checkpoint.txt`；Saves 沙箱备份同目录。`SymbolsNotMatchingException` 来自 Custom Furniture 旧 PDB（无关）。玩法现象验收待用户实机执行。
- **检查点含义**：代码+构建+部署为一个逻辑检查点；真实游戏验收（六 Case 独立标记）待用户执行，不以此代替。

## 取证与契约证据

- 当前安装 DLL：`D:\GGGGG\K1515\Stardew Valley.dll` SHA256 `DFE341CA...`（总账核验未漂移）。
- 原生 `BobberBar.update` 反编译：`_analysis\StardewValley.BobberBar.decompiled.cs`（行 384–470）；IL 全量：`_analysis\bobberbar-il-20260806\Stardew Valley.il`（update 方法 986387–987909）。
- 注入点核对（IL 偏移）：概率 4000/2000/1000（IL_0258/03c5/042c）、漂移 ±0.01（IL_02f2/0310）、平滑 /5（IL_0399）、鱼位置积分（IL_04af–04b0）、绿条速度输入（IL_071b）、绿条位置积分（IL_072e）、底部反弹（IL_0766–0772+陷阱浮标因子）、顶部反弹（IL_07f0–07fc）、bobberAcceleration stfld（IL_0380，BATCH-028/029 既有注入点）。
- 成功链路：BobberBar.update → FishingRod.pullFishFromWater → PullFishFromWater_Prefix（传奇鱼分支当前直接 return）→ Farmer.caughtFish → CaughtFish_Postfix（需 pending 数据）。
- 存档：Farmer.modData["FishingExpanded/FishDifficultyData"] System.Text.Json；删除 FishingLevelBonus 字段后旧存档多余键被 Json 忽略，无需迁移。
- α 计数口径：CollectionStars ∩ 原生 61 鱼集合派生，不新增持久字段（单一写入者=CollectionStars 维持）。

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| CollectionStars | DifficultyManager（RecordHighDifficulty/RecordLegendaryCatch/测试命令） | 同上 | 图鉴皇冠、宣言、α 计数 | 存档级 | 无 | 按玩家 modData 同步 |
| BobberBar InstanceData.Alpha | BobberBarPatches 构造边界 | 只读 | Transpiler 助手方法 | 小游戏生命周期 | 小游戏结束 | ConditionalWeakTable 自动释放 |
| BobberBar InstanceData.JumpText/Timer | BobberBarPatches 跳鱼瞬移边界 | 只读 | Draw_Postfix | 小游戏生命周期 | 淡出完成 | 同上 |

## R0

- 修改第一处错误决策（设计改版，非 Bug）：
  1. FE-034-1：删除 FishingLevelBonus 字段与 Getter Postfix 注入、图鉴加成行、i18n 键。
  2. FE-034-2：DifficultyManager 新增原生 61 鱼集合、GetAlpha/GetCountableCrownCount、RecordLegendaryCatch（PullFishFromWater_Prefix 传奇分支调用，成功一次一发）；NormalizeData 不再排除传奇鱼星标、不再重算加成。
  3. FE-034-3：删除 BobberBarPatches 绿条外钳制全部状态与判定（含 IsInGreenBar 镜像）。
  4. FE-034-4：Update_Transpiler 扩展注入（输入响应线性混合、撞边钳制线性、帧率解耦）；InstanceData.Alpha；助手方法。
  5. FE-034-5：GetAccelerationBoost 阈值 <98→0.1、≥98→1（无实例数据默认 98 保持传奇全额增幅）。
  6. FE-034-6：跳鱼瞬移边界设置 JumpText/JumpTextTimer；新增 BobberBar.draw Postfix 绘制（鱼图标上方，1s 停留+0.5s 淡出）；i18n 上下跳各 15 条（中英）。
- 新权威入口：BobberBar 构造边界（α 快照）、原生 update Transpiler（手感/帧率唯一注入者）、PullFishFromWater_Prefix 传奇分支（传奇皇冠唯一发放者）、BobberBar.draw Postfix（跳鱼文案唯一绘制者）。
- R1 待删除旧路径：FishingLevelBonus 字段/Getter Postfix/SetLegendarySuppression/Constructor_Prefix/绿条外钳制状态与判定/i18n collections.fishingBonus/ModEntry 加成文案/测试池含 Mod 鱼逻辑。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| FishDifficultyData.FishingLevelBonus | 全局 rg 无剩余引用 | 否（删除后旧存档 Json 忽略） | 无 |
| FarmerFishingLevelPatches.FishingLevel_Getter_Postfix + 抑制集合 | 删除补丁 | 否 | 无 |
| BobberBarPatches.Constructor_Prefix（传奇抑制） | 删除 | 否 | 无 |
| BATCH-033 绿条外钳制（InstanceData 5 字段+Prefix/Postfix 判定+IsInGreenBar） | 删除 | 否 | 无 |
| i18n collections.fishingBonus | 删除键 | 否 | 无 |
| CollectionsPagePatches 加成行 | 删除 | 否 | 无 |

- 修改前写入者数量：FishingLevelBonus 1（DifficultyManager）+ 手感 0；修改后：α 派生自 CollectionStars（1 写入者不变）。
- 运行时代码新增/删除：Transpiler 助手方法净增（帧率+手感必须注入原生边界，无法复用现有所有者）；删除隐藏等级/钳制路径净减。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 α=0 | 与原版完全一致（60fps） | 速度/位置/概率漂移 | 公式静态证明（待构建） |
| 主机单人 α=1 | 按下一瞬间 ±30px/帧、撞边停住 | 反弹、加速过程 | 待构建 |
| 中间 α 线性 | 手感连续过渡 | 跳变/反向 | 待构建 |
| 普通鱼/非鱼类/鱼王 | 非鱼正常；鱼王无 InstanceData 原生豁免（含原生手感） | 鱼王被改难度/手感 | 待构建 |
| 扩展传奇 898–902 | 按普通鱼规则可皇冠可计数 | 被当传奇豁免 | 待构建 |
| 原版传奇 | 钓到一次给皇冠；小游戏豁免 | 失败给皇冠、双发 | 待构建 |
| Mod 鱼 | 皇冠显示但不计 α | 计入 α | 待构建 |
| 帧率 30/120fps | 行为速率一致 | 逐帧翻倍 | 公式静态证明 |
| 跳鱼文案 | 瞬移时上/下跳各显示对应文案，1s 后淡出 | 每帧刷屏、文案残留 | 待构建 |
| 换日/重载/标题 | 存档兼容（旧键忽略） | 加载异常 | 待构建 |
| 性能最坏情况 | 助手方法仅算术+CWT 查找 | 逐帧日志/分配 | 静态审查 |
| 已验收相邻回归 | 星标宣言、等级建议、HUD 排队、星之果茶 | 回退 | 待构建 |

## 构建、部署与集中测试

- 构建结果：待构建（Release Rebuild）。
- 版本/哈希：待构建。
- 部署：未授权，不部署。
- 每个 Case 的实际结果：待验收。

## 收尾与归档

- 已完成：设计确认、取证、GAME-DESIGN 更新。
- 当前不确定性：60fps 位级一致性（概率/平滑换算在 60fps 有浮点舍入，统计一致、位级不完全一致——设计接受统计一致）。
- 下一条准确操作：R0 代码实施。
- 总账与测试路线是否已覆盖更新：实施后更新。
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收通过后可归档。
*** End Patch




