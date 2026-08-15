# FishingExpanded 根因批次：BATCH-044 重置确认加固（移除超时 + 发起玩家校验）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-03 |
| 当前活动类别 | CAT-01（重置确认加固） |
| 整合候选状态 | 已部署待集中测试（随 BATCH-047 统一部署；Deploy:ROUND-20260812-06:DE23813F...） |
| 当前权威活动卡 | 本卡（BATCH-044） |
| 本轮用户问题范围 | 用户实测“主机清理后副机似乎也归零”；取证后确认模组只清主机（12:35 日志），但发现两个隐患：①5 秒超时是假的（超时日志后确认仍执行）；②回调未校验发起玩家。用户明确：修复并去掉超时 |
| 本轮纳入 Case | FE-044-1 防误点超时未真正取消已打开的确认框；FE-044-2 重置回调未校验回答者身份（分屏/共享地点串清风险） |
| 共享第一处分歧与所有权链证据 | 重置序列状态=ModEntry 静态字段；第一处分歧=自愈逻辑只清标志不清对话框，且 `OnResetSequenceAnswer` 使用 `Game1.player` 而非回答者 `who`；原生 `GameLocation.answerDialogue` 契约=`afterQuestion(Game1.player, answer)`，`afterQuestion` 存放在共享地点对象上 |
| 排队或暂不纳入 Case | BATCH-042/043 已部署待集中测试；BATCH-039/040/041 同待验收 |
| 合并/拆分决定及依据 | 两个隐患同属重置确认序列同一所有者，合并单类别 |
| 本轮准入证据 | SMAPI-latest.txt 12:35:17 `[GMCM] 重置确认对话框未完成，已自动取消` + 12:35:19 仍执行清空（玩家 1419976689139663055）；12:40 副机（623430766237409787）从 8 级继续钓到 15 级证明未被清；原生 `GameLocation.answerDialogue` 反编译确认回调签名与共享状态；用户 2026-08-12 明确“修复，去掉超时” |
| 现有实现复核 | `TryStartResetSequence` 自愈分支只置 `_resetPending=false/_resetSequenceStarted=false`，不关闭 `Game1.activeClickableMenu`；`OnResetSequenceAnswer(Farmer who, ...)` 忽略 `who`，用 `Game1.player` 调 `ClearAllData`；无发起玩家 ID 记录 |
| 允许修改范围 | `Source\ModEntry.cs`（重置状态字段、setter、TryStartResetSequence、OnResetSequenceAnswer）；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、BATCH-042 卡、本卡 |
| 冻结 Case/禁止范围 | `ClearAllData` 语义与范围（仍只清当前发起玩家）；GMCM 章节结构；难度/结算所有权；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 移除 5 秒超时与自愈日志；记录发起重置的玩家 ID（`_resetArmedPlayerId`）；`OnResetSequenceAnswer` 用回答者 `who` 并校验 `who.UniqueMultiplayerID == _resetArmedPlayerId`，不一致忽略；确认框保持打开直到明确选择确认/取消 |
| 可观测性决定 | 移除“确认对话框未完成，已自动取消”日志；新增不一致应答 Warn 日志（每异常 1 条）；其余日志不变 |
| 自动化验收决定 | 场景=全量 `Saves` 沙箱；入口=GMCM 开关→关闭菜单→确认框；成功判据=确认框长时间不选仍保持打开、取消不执行、确认只清发起玩家、分屏另一屏应答被忽略；失败判据=超时后仍执行、误清他人、确认框可被 5 秒自动关闭 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~043 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `BDCE071FA936C5974DA22F00894C4F9224D6A9E5323DB6D3AF8E794DCA68A1F7`；反编译核验 `_resetArmedPlayerId` 记录/校验、`Game1.player.UniqueMultiplayerID == _resetArmedPlayerId` 门、`OnResetSequenceAnswer` 不一致忽略、`ClearAllData(who)` 已编译；无 `_resetSequenceStartTick`/自动取消残留 |
| 唯一下一步 | 用户授权部署后统一集中测试（两步确认 + 无超时 + 分屏应答隔离） |
| 已消费动作 | `Build:ROUND-20260812-03:BDCE071FA936C5974DA22F00894C4F9224D6A9E5323DB6D3AF8E794DCA68A1F7` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-044-1/2 | 代码侧结束 | 根因修复 | 不一致应答 Warn（每异常 1 条） | GMCM 重置两步确认 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-044-1 超时未真正取消 | R2 完成 | 沙箱验收（确认框保持打开） |
| FE-044-2 回调未校验发起玩家 | R2 完成 | 沙箱验收（副机应答忽略） |

> 机制断言：确认交互行为修改（无存档写入变化），静态+构建验证后仍需真机两步确认验收。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01/FE-044-1 | 开关→关菜单→确认框不选 10 秒以上 | 确认框仍打开；选择“确认重置”仍按确认执行；“自动取消”日志不存在 | 交互行为 |
| 2 | CAT-01/FE-044-2 | 分屏下主机发起，副机尝试应答 | 副机应答被忽略并记 Warn；只清主机 | 交互行为 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 本卡候选 `BDCE071FA936C5974DA22F00894C4F9224D6A9E5323DB6D3AF8E794DCA68A1F7`；已随 BATCH-047 统一部署 `DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`（2026-08-12 13:16，备份 `DeploymentBackups\FishingExpanded-20260812-131648-pre-BATCH047`） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-044-1 | 确认框 5 秒后看似取消，实际点确认仍清空 | 0 | 无 | RC-044 | 实施中 | 无“自动取消”日志且确认框保持 |
| FE-044-2 | 分屏下存在串清其他玩家风险（本次未发生） | 0 | 无 | RC-044 | 实施中 | 副机应答被忽略 |

## RC-044 根因卡

- 根因状态：已证实（日志 + 原生契约）
- 第一处分歧：自愈逻辑只清标志不清对话框（12:35:17 日志后 12:35:19 仍执行）；回调用 `Game1.player` 且无发起玩家校验
- 决定性证据：SMAPI-latest.txt 12:35:17/19 两条日志；`GameLocation.answerDialogue` 反编译 `afterQuestion(Game1.player, answer.responseKey)`；`afterQuestion` 为地点实例字段（分屏同地点时共享）
- 竞争解释表：竞争解释豁免（理由=静态+运行日志已闭环，无互斥根因）
- 唯一所有者：`ModEntry` 重置序列状态；数据写入仍为 `DifficultyManager.ClearAllData`
- 最短区分动作：移除超时 + 发起玩家校验，真机两步验收

## RC-044 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 确认应答 | 发起玩家所在屏 | `ModEntry.OnResetSequenceAnswer` | 静态（含发起玩家 ID） | 无广播 | 标题 `UnloadData` 后状态清空 | 沙箱/分屏实测 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 重置待命/确认 | GMCM 开关 | `ModEntry` 静态 | 序列/回调 | 单实例 | 选择确认/取消 | 标题无需清理（无超时） |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`9D199BD1…`（BATCH-043 部署版）
- Expanded 接管入口：GMCM 开关 → `TryStartResetSequence` → `createQuestionDialogue` → 原生 `answerDialogue` → `OnResetSequenceAnswer`
- 原生上游入口和状态字段：`GameLocation.afterQuestion`（共享地点实例字段）
- 原生提交方法：`answerDialogue` 调 `afterQuestion(Game1.player, answer.responseKey)` 后置 null
- 后续回调、同步和生命周期：回调后无残留（原生置 null）
- Harmony 拦截点：无新增
- 第一处分歧：见 RC 卡
- 尚未读取或仍不确定的环节：无

## R0

- 修改第一处错误决策：移除 5 秒自愈超时；记录 `_resetArmedPlayerId`；回调用 `who` 并校验
- 新权威入口：`OnResetSequenceAnswer`（唯一应答入口，校验发起玩家）
- R1 待删除旧路径：`_resetSequenceStartTick` 字段与“自动取消”自愈分支

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `_resetSequenceStartTick` 与 5 秒自愈 | 本卡 R0 | 否 | 删除 |

- 修改前写入者数量：1（`DifficultyManager`）
- 修改后写入者数量：1（`DifficultyManager`）

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 确认框长时间不选 | 保持打开，选择后按所选执行 | 5 秒自动取消/自动执行 | 静态待构建 |
| 取消路径 | 选择取消后不清空、状态复位 | 取消后仍清空 | 静态待构建 |
| 分屏他屏应答 | 副机应答被忽略并 Warn；只清主机 | 串清副机 | 静态待构建 |
| 单人确认 | 主机确认只清主机 | 影响其他存档/玩家 | 静态待构建 |
| 标题/换存档 | 状态复位，无残留 | 跨存档串状态 | 静态待构建 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `BDCE071FA936C5974DA22F00894C4F9224D6A9E5323DB6D3AF8E794DCA68A1F7`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）
- 本次单局路线：全量 `Saves` 沙箱 + 两步确认 + 分屏应答测试
- 日志/截图/存档证据：待集中测试封存

## 收尾与归档

- 已完成：取证、批次卡、R0 设计
- 当前不确定性：分屏真实应答路径需真机验证
- 下一条准确操作：R0 代码实现 → Release Rebuild → 静态核验 → 等待用户授权部署/实测
- 总账与测试路线是否已覆盖更新：实施中同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收完成后可归档
