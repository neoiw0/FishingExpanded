# FishingExpanded 根因批次：BATCH-067 难度等级挂钩固定钓鱼等级 + 成功保底 +1 + 挑战掉星折扣口径回归

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260816-01（与 BATCH-066 并行；统一构建将合并两批源码，BATCH-066 未部署待授权） |
| 当前活动类别 | CAT-01（难度等级增长挂钩固定钓鱼等级 + 成功保底 +1）；CAT-02（挑战掉星折扣口径回归设计） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-067）；BATCH-066 卡继续为节日批次权威 |
| 本轮用户问题范围 | ①难度等级增长与**固定钓鱼等级**挂钩：系数 = max(等级,1)×0.1（1 级 ×0.1、10 级 ×1.0=现有速度；0 级钳 0.1）；只读 `Farmer.fishingLevel` 字段，不含食物/饮料 buff 与助战临时等级；②成功钓起请求增益保底 +1 级；③挑战鱼饵掉星折扣回归 GAME-DESIGN §7.5 语义（对**最终数量**打折）+ 兜底 ≥1 |
| 本轮纳入 Case | FE-067-1 固定钓鱼等级系数；FE-067-2 成功保底 +1；FE-067-3 挑战掉星折扣口径回归 |
| 共享第一处分歧与所有权链证据 | CAT-01 写入点 = `FishingRodPatches.CaughtFish_Postfix`（Farmer.caughtFish Postfix，成功结算唯一边界，调 `DifficultyManager.RecordSuccess` 统一封顶）；CAT-02 写入点 = `FishingRodPatches.CreateFish_Postfix`（数量转换唯一边界）。原生契约：`Farmer.fishingLevel` 为 `public readonly NetInt`（`_analysis\StardewValley.Farmer.decompiled.cs:592-593`），`FishingLevel` 属性 = `fishingLevel.Value + buffs.FishingLevel`（1612 行）→ 固定等级必须用字段；挑战鱼饵原生 `challengeBaitFishes = 3`（`_analysis\StardewValley.BobberBar.decompiled.cs:240-242`）、成功 `numCaught = challengeBaitFishes`（354-357）→ 掉星路径 nativeStack 恒 = 3，`round(3×0.4)=1` 永不归零；`CreateFish()` 在 `numberOfFishCaught > 1` 时 `item.Stack = numberOfFishCaught`（`_analysis\StardewValley.FishingRod.decompiled.cs:2403-2418`） |
| 排队或暂不纳入 Case | BATCH-066（节日开关，同工作树并行，未部署待授权，统一构建合并）；BATCH-064 白色半透明矩形方案 A/B 待用户裁决；BATCH-059A/060/061/062/064l/065 待真实验收（本批不重复修改） |
| 合并/拆分决定及依据 | 两个类别不同写入边界（CaughtFish_Postfix vs CreateFish_Postfix）、不同第一处分歧 → 不合并；同一用户指令一轮确认、同一轮统一构建 |
| 本轮准入证据 | 用户 ask_user_question 逐条确认（①系数=固定钓鱼等级×0.1、10 级=现有速度，保留雪球 `round(调整后难度/50)` 并整体乘系数；②蟹笼不吃系数；③成功保底 +1；④0 级钳 0.1、沿用 `Math.Round`、失败−1/89+ 上限/挑战不掉级/非鱼上限 8 不动；⑤掉星折扣回归设计并兜底 ≥1）；源码复核（CaughtFish_Postfix 715-727、CreateFish_Postfix 468-487）；原生契约取证（Farmer.fishingLevel 字段、BobberBar challengeBaitFishes=3、CreateFish Stack） |
| 现有实现复核 | `CaughtFish_Postfix`（FishingRodPatches.cs:715-727）：`baseLevelGain`（脱杆 0/1/2/3+ → 10/5/2/1）+ `difficultyGain`（round(调整后难度/50)）→ RecordSuccess 统一封顶（下一称号区间上限、88→89、89+ 每次 +6、鱼种上限 100/8）；`CreateFish_Postfix`（468-487）：万能 +10 与挑战 5 分钟 ×1.5（ceil）互斥 → `round(nativeStack × 掉星乘数)`（作用于原生数量，0 星=0.4）→ ×等级倍数；`GetQuantityMultiplier`（0/负=1）；蟹笼 `RecordSuccess(1)` 固定（CrabPotPatches.cs:104） |
| 允许修改范围 | `Source\Patches\FishingRodPatches.cs`（CaughtFish_Postfix 增益公式 + CreateFish_Postfix 掉星顺序 + 日志字段）、`Source\Utils\DifficultyCalculator.cs`（新增纯函数 `GetFishingLevelGainFactor`/`GetRequestedLevelGain`/`ApplyChallengeStarMultiplier`）、`Source\ModEntry.cs`（fish_selftest 只读断言）；文档：GAME-DESIGN.md、TESTING-GUIDE.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | `DifficultyManager.RecordSuccess` 封顶逻辑与存档结构（FishDifficultyData）；蟹笼 +1（CrabPotPatches）；鱼王豁免；非鱼上限 8；挑战鱼饵失败不掉级；89+ 每次 +6；config.json；部署目录；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | CAT-01：`请求增益 = max(1, round((脱杆基数 + round(调整后难度/50)) × max(固定钓鱼等级,1)×0.1))`；CAT-02：掉星折扣移到乘完等级倍数后：`finalStack = max(1, round(finalStack × 掉星乘数))` |
| 可观测性决定 | 复用现有日志：`CaughtFish_Postfix` "难度等级更新"（含脱杆/基础增长/额外难度增益/实际增长；本批追加"钓鱼等级系数"与"请求增长"字段，每成功 1 条）；`CreateFish_Postfix` "数量转化完成"（含最终数量，每成功 1 条）；修复效果由既有日志直接可见，无新增标签、无新增频率 |
| 自动化验收决定 | 复用现有 `fish_selftest` 只读断言：新增 8 条——系数锚点（0→0.1、1→0.1、5→0.5、10→1.0）、保底（round(2×0.1)=0→1、round(10×0.1)=1）、掉星（round(30×0.4)=12 设计值、round(3×0.4)=1 兜底、×1.0 不变）。存档影响=无（不新增字段、不改存档结构）；真实验收走生产入口（真实钓鱼小游戏） |
| 当前类别阶段 | R0 实施完成；R1 完成；R2 推演完成（见下） |
| 当前工作树 | 部分修改（作者 neoiw 未提交基线含 BATCH-066 + 本类别修改；同文件叠加无法拆分，按 AGENTS.md 记录为同文件混合修改，仅按明确路径暂存） |
| 本轮统一构建 | `CDCCA63FA36E7C940DC8732EA71F719F3D113328E2BB0DFBE8E871EA74385993`（Release Rebuild，0 警告 0 错误，2026-08-16；与 BATCH-066 合并统一构建；反编译核验通过：三个纯函数（1276/1281/1288 行）、CreateFish_Postfix 掉星顺序（×倍数后 5900-5907）、CaughtFish_Postfix 读 `fishingLevel` NetInt 字段 + 系数/保底（6128-6131）、selftest 11 条断言（1041-1055），证据 `_analysis\batch067-verify`） |
| 唯一下一步 | 集中测试（用户明确"仅部署，先不测试"——测试时机由用户决定；含 fish_selftest 11 条 + 真实钓鱼两档对比 + 挑战 0 星） |
| 已消费动作 | `Deploy:ROUND-20260816-01:CDCCA63FA36E7C940DC8732EA71F719F3D113328E2BB0DFBE8E871EA74385993`（2026-08-16 07:45 前后，用户授权"仅部署，先不测试"） |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（两个类别均为主线程已掌握的单文件边界修改；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-067-1/2 | 实施中 | 根因修复（新玩法，用户确认） | 复用"难度等级更新"日志（每成功 1 条） | fish_selftest 4 条只读断言 + 真实钓鱼 |
| CAT-02 | FE-067-3 | 实施中 | 根因修复（已部署行为口径回归设计文本） | 复用"数量转化完成"日志（每成功 1 条） | fish_selftest 3 条只读断言 + 真实挑战鱼饵钓鱼 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-067-1 固定钓鱼等级系数 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-067-2 成功保底 +1 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-067-3 挑战掉星折扣口径回归 | 设计完成 | R0 实施 + 构建 + 真实验收 |

> 机制断言：CAT-01 为新玩法（用户确认设计），竞争解释表不适用（无互斥假设），准入证据=用户逐条确认 + 原生契约取证。CAT-02 为已部署行为（BATCH-056 兑现）与设计文本（§7.5）不一致的回归修复，竞争解释表见 RC-01。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01+02 | fish_selftest | 通过 / 未执行 | 系数/保底/掉星 8 条只读断言 |
| 2 | CAT-01 | 真实钓鱼（钓鱼等级 1 vs 10 两档） | 通过 / 未执行 | 完美钓 1 级玩家 +1、10 级玩家 +10（未封顶时） |
| 3 | CAT-01 | 真实钓鱼（0 级新档） | 通过 / 未执行 | 成功至少 +1 |
| 4 | CAT-02 | 真实挑战鱼饵拖到 0 星（倍数 ≥5 的鱼） | 通过 / 未执行 | 数量 = round(3×倍数×0.4)，至少 1 条 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `CDCCA63FA36E7C940DC8732EA71F719F3D113328E2BB0DFBE8E871EA74385993`（2026-08-16 部署，备份 `DeploymentBackups\FishingExpanded-20260816-074434-pre-BATCH067-066`=833A0812...，源/目标哈希一致，i18n×2+manifest 同步，config.json 未触碰 FCF30FE1...） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-067-1 | 难度等级增长速度与玩家钓鱼等级无关 | 0 | 无（新玩法） | RC-01 | 设计完成 | 1 级 vs 10 级完美钓等级收益对比 |
| FE-067-2 | 低钓鱼等级/多脱杆时成功可能 +0 级 | 0 | 无（新玩法） | RC-01 | 设计完成 | 0 级玩家成功至少 +1 |
| FE-067-3 | 挑战鱼饵掉星折扣先作用于原生 3 条再乘倍数，高倍数下比设计（§7.5 最终鱼获 −20%）少给最多 20% | 0 | 无（首报，源码+原生证据证实） | RC-01 | 设计完成 | 倍数 10 + 0 星 = 12 条 |

## RC-01 根因卡

- 根因状态：已证实（源码取证 + 原生契约取证；CAT-01 为新设计无根因）
- 第一处分歧：
  - FE-067-1/2：无第一处分歧（用户新玩法，写入点=CaughtFish_Postfix 增益计算，DifficultyManager 封顶逻辑不动）。
  - FE-067-3：`CreateFish_Postfix`（FishingRodPatches.cs:481-482）掉星乘数作用于 `nativeStack`（原生 3 条）→ `round(3×0.4)=1` → 再 ×等级倍数；设计 §7.5 要求"每掉 1 颗**最终鱼获** −20%"，即应作用于乘完等级倍数后的最终数量。高倍数下实际比设计少（倍数 10 + 0 星：实际 10 条 vs 设计 12 条；倍数 50：50 vs 60）；低倍数（1-2）无差异。
- 决定性证据：`_analysis\StardewValley.BobberBar.decompiled.cs:240-242/354-357`（挑战鱼饵 challengeBaitFishes=3、成功 numCaught=3）；`_analysis\StardewValley.FishingRod.decompiled.cs:2403-2418`（numberOfFishCaught>1 → Stack=numberOfFishCaught）；`FishingRodPatches.cs:472-485`（nativeStack=3 → round(3×0.4)=1 → ×倍数）；GAME-DESIGN §7.5 设计文本（"每掉 1 颗最终鱼获 −20%（0.8/0.6/0.4）"）。
- 竞争解释表（FE-067-3）：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态（仍成立/已排除） |
|---|---|---|---|
| A：掉星乘数作用于原生数量再乘倍数，高倍数下比设计少 | 倍数 m 时 0 星给 m 条（3m×0.4 的设计值 > m） | 源码 481-482 行直接证实运算顺序 | 已证实（根因） |
| B：挑战鱼饵成功原生只给 1 条 → 0 星可能归零 | 0 星时 0 条鱼 | BobberBar 240-242/354-357：challengeBaitFishes=3、numCaught=3；round(3×0.4)=1 永不归零 | 已排除 |
| C：数量倍数可为 0 | 低等级 0 条 | `GetQuantityMultiplier` 0/负=1 保底（DifficultyCalculator.cs:34） | 已排除 |
| D（外部）：原生 Stack 本身为 0 | CreateFish 返回 Stack=0 | `CreateFish()` Stack=numberOfFishCaught≥1（FishingRod 2403-2418） | 已排除 |

- 最后已知正常版本/行为：BATCH-056 部署起（2026-08-12）即存在该口径偏差；从未出现归零。
- 过去失败方案及为何失败：无（首报）。
- 唯一所有者：`CreateFish_Postfix`（数量转换唯一边界）；等级增益唯一所有者 = `CaughtFish_Postfix` + `DifficultyManager.RecordSuccess`（封顶）。
- 最短区分动作：代码顺序修正后 fish_selftest 3 条掉星断言 + 真实挑战鱼饵 0 星钓鱼数量核对。
- 诊断标签/触发窗口：无新增诊断（复用"数量转化完成"日志）。
- 诊断是否只读且不改变行为：N/A（本批为行为修改，非诊断）。
- 诊断构建/部署与哈希：N/A。
- 等待玩家返回的日志及位置：N/A（代码侧结束前不等待）。
- 日志返回后的成功/失败判据：N/A。
- 反证记录：无。

## RC-01 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 等级增益（系数×保底） | 各玩家本地结算（IsLocalPlayer 门） | CaughtFish_Postfix → DifficultyManager | 按玩家 UniqueMultiplayerID 隔离 | 无新增 | 无新增状态 | fish_selftest 纯函数断言 |
| 系数读取（fishingLevel 字段） | 各玩家自己的 Farmer 实例字段 | 原生 Farmer.fishingLevel（Mod 只读） | 每玩家独立 NetInt | 原生同步 | 原生处理 | 无（只读原生字段） |
| 数量转换（掉星折扣） | 各玩家本地 CreateFish（IsLocalPlayer 门） | CreateFish_Postfix | 按玩家 pending 键隔离 | 无新增 | ClearPending 已有 | fish_selftest 纯函数断言 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 难度等级（成功计数） | 原生钓鱼成功 | DifficultyManager.RecordSuccess（封顶不变） | HUD/图鉴/倍数/助战 | 每成功 1 次 | 封顶/重置 | 存档内，标题/换日不清 |
| 数量（最终 Stack） | 原生 CreateFish | CreateFish_Postfix（顺序修正） | 背包/溢出菜单 | 每成功 1 次 | — | ClearPending（已有） |

本批**无新增持久状态、无新增缓存、无新增存档字段**；只调整两处既有边界的计算顺序/公式。

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`833A0812A16D73670AD9BBE9A6162A17FF656B7CE542500A56AAC5209072E2D0`（BATCH-060v2/065；BATCH-066 源码已构建 `6F681270...` 未部署，与本批合并统一构建）
- Expanded 接管入口：`Farmer.caughtFish` Postfix（难度结算）；`FishingRod.CreateFish` Postfix（数量转换）
- 原生上游入口和状态字段：BobberBar 成功 → `FishingRod.pullFishFromWater(fishId,…, numCaught)`（事件转发）→ `doPullFishFromWater`（`numberOfFishCaught = num4`）→ `Farmer.caughtFish(…, numberOfFishCaught)` → `CreateFish()`（numberOfFishCaught>1 → Stack=numberOfFishCaught）；挑战鱼饵 BobberBar 构造 `challengeBaitFishes=3`，成功 `numCaught=challengeBaitFishes`
- 原生提交方法：`FishingRod.doneFishing`/`doPullFishFromWater` → `Farmer.caughtFish`（鱼获+图鉴）→ `gainExperience(1, …)`（经验）
- 后续回调、同步和生命周期：`NetEventBinary` 分发 pullFishFromWater 事件；经验结算在 NetEventBinary.Poll 阶段（`TryBeginExperienceAdjustment` 消费 pending）；`ClearPending` 于换日/标题
- Harmony 拦截点：`CaughtFish_Prefix/Postfix`、`CreateFish_Postfix`、`GainExperience_Prefix`（本批只动前两个的计算顺序）
- 第一处分歧：FE-067-3 = CreateFish_Postfix 481-482 行运算顺序；FE-067-1/2 = 无（新设计）
- 尚未读取或仍不确定的环节：无（本批不触碰其他原生路径）
- 旧版只读参考及可接受差异：BATCH-038/056 反编译（`_analysis\batch056-bobber-decompiled.cs` 等）与本批无冲突

## R0

- 修改第一处错误决策：
  - CAT-01：CaughtFish_Postfix 增益计算接入纯函数 `GetRequestedLevelGain(baseGain, adjustedDifficulty, fishingLevel)` = `max(1, round((base + round(adj/50)) × max(level,1)×0.1))`；难度等级字段读 `__instance.fishingLevel.Value`（固定等级，不含 buff）
  - CAT-02：CreateFish_Postfix 掉星折扣移到 `×等级倍数` 之后，经纯函数 `ApplyChallengeStarMultiplier(finalStack, star)` = `max(1, round(finalStack × star))`
- 新权威入口：不变（仍为两处既有 Postfix；新增三个纯函数入 DifficultyCalculator）
- R1 待删除旧路径：无新增旧路径（仅替换两处内联公式）

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| CaughtFish_Postfix 内联 `baseLevelGain + difficultyGain` 直传 RecordSuccess | 本批替换为 GetRequestedLevelGain 调用 | 无（唯一调用点被替换） | 不保留 |
| CreateFish_Postfix 内联 `round(nativeStack × star)` 先打折 | 本批替换为先乘倍数后打折 | 无（唯一调用点被替换） | 不保留 |

- 修改前写入者数量：2（CaughtFish_Postfix 等级、CreateFish_Postfix 数量）→ 修改后：2（不变，仍是唯一边界）
- 运行时代码新增/删除：净增约 15 行（3 个纯函数 + 2 处调用改写 + 日志字段）
- 净增长理由：纯函数使公式可被 fish_selftest 只读核验（治理自动化验收要求），不新增状态或所有者

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 钓鱼等级 1：完美 +1；10 级：完美 +10（未封顶）；2 脱杆 1 级：保底 +1 | 成功 +0（未封顶时） | 纯函数断言 + 日志字段 |
| 本地主屏/副屏 | 各屏幕按自己 fishingLevel 字段算系数 | 串用另一玩家等级 | 按玩家隔离（IsLocalPlayer + 每玩家字段） |
| 远程与四人混合 | 各客户端本地结算 | 跨玩家写入 | 同上 |
| 普通鱼/非鱼类/鱼王 | 非鱼上限 8 内保底生效；鱼王豁免路径不变；蟹笼固定 +1 不吃系数 | 蟹笼被系数改变 | CrabPotPatches 未改 |
| 数量、动画与 ItemGrabMenu | 掉星折扣在最终数量上（倍数 10+0 星=12）；动画数量不变；溢出菜单不变 | 0 条鱼（round(3×0.4)=1 兜底） | 纯函数断言 |
| 难度结算/HUD/图鉴/手持展示 | 等级结算日志含系数与请求增长；HUD 称号不变 | 封顶逻辑被绕过 | RecordSuccess 未改 |
| 换日/重载/标题/断线 | 无新状态可泄漏 | — | 无新增状态 |
| 性能最坏情况 | 每成功 1 次多 2 次浮点乘除 | 逐帧/高频输出 | 无新增日志频率 |
| 已验收相邻回归 | BATCH-062 数量倍数、BATCH-056 掉星体系其余分支（5 分钟内 ×1.5 不变） | 5 分钟内数量变化 | ×1.5 分支未动 |

## 构建、部署与集中测试

- 构建结果：待执行（与 BATCH-066 合并统一 Release 构建）
- 版本/哈希：
- 部署文件与目标：
- 本次单局路线：
- 日志/截图/存档证据：
- 每个 Case 的实际结果：

## 收尾与归档

- 已完成：
- 当前不确定性：
- 下一条准确操作：
- 总账与测试路线是否已覆盖更新：
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：
