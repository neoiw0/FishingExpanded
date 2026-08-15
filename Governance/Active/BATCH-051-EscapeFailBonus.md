# FishingExpanded 根因批次：BATCH-051 非挑战鱼饵连续失败逃跑减速

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-16（BATCH-057 修订轮） |
| 当前活动类别 | CAT-01（连续失败逃跑减速） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-10:AF073AE7... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-051） |
| 本轮用户问题范围 | 用户 2026-08-12 提出：非挑战鱼饵、调整后难度 >100 的鱼，按鱼种分别累计连续失败；失败越多，鱼快逃跑时（钓鱼条低）减速越多；第 5 次失败给到“相当于 -10 级”的逃跑速度减慢，平滑加成；成功清零。用户澄清：口径复用现有“-10 级/普通鱼在钓鱼条太少时减少更慢”的蓄力槽保护机制。BATCH-057（2026-08-12 用户指令）：挑战鱼饵同样参与（95 级以上也吃），否则太难 |
| 本轮纳入 Case | FE-051-1 高难鱼连续失败无递增逃跑减速 |
| 共享第一处分歧与所有权链证据 | 逃跑减速唯一所有者=`BobberBarPatches` 每帧写 `___distanceFromCatchPenaltyModifier`；第一处分歧=现只有全局 20%/1% 与负等级保护，无按连续失败的递增减速；现有契约=`DifficultyCalculator.GetCatchPenaltyModifier(level, progress)`（-10 级：≤1%→0.20、≤20%→0.60、≤40%→0.99） |
| 排队或暂不纳入 Case | BATCH-042~050 已构建/部署待验收 |
| 合并/拆分决定及依据 | 单 Case 单类别：同属蓄力槽/逃跑减速唯一管线 |
| 本轮准入证据 | 用户明确机制与口径；`GetCatchPenaltyModifier` 源码复核；`ConsecutiveFailCount` 按玩家+鱼种已有且成功清零 |
| 现有实现复核 | `Update_Prefix` 每帧 `newModifier=GetCatchPenaltyModifier(level, distance)` → `min(native, new)`；`GetConsecutiveFailCount(fishId, player)` 公开可用 |
| 允许修改范围 | `Source\Utils\DifficultyCalculator.cs`（纯函数）、`Source\Patches\BobberBarPatches.cs`（接入+状态转换日志）；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 存档结构；鱼王豁免；挑战鱼饵路径；力竭/手感 α；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | `GetEscapeFailBonusModifier(level, consecutiveFails, catchProgress) = Lerp(GetCatchPenaltyModifier(level, p), GetCatchPenaltyModifier(-10, p), clamp(N/5,0,1))`；`AdjustedDifficulty > 100f` 时应用（挑战鱼饵同样参与，BATCH-057）；状态转换单发日志 |
| 可观测性决定 | 新增“逃逸减速加成生效/解除”状态转换日志（每段 ≤2 条），复用保护日志风格 |
| 自动化验收决定 | 沙箱：调整后难度>100、连续失败 0/1/3/5 → 蓄力 ≤20% 时倍率分别 ≈0.80/0.75/0.65/0.60；≤1% 时 ≈0.50/0.44/0.32/0.20；鱼王不受影响；挑战鱼饵连续失败 5 次后低条减速生效（BATCH-057）；成功清零 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~050 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `4EF947D52B7C67357BDDB981661AD9A3CD10963EFA8E6CE3384BDBF535D480D0`；反编译核验逃逸减速条件仅剩 `AdjustedDifficulty > 100f`（挑战鱼饵参与）+ 滞回保留 |
| 唯一下一步 | 真实集中测试（0/1/3/5 次倍率、>40% 无效果、挑战鱼饵连续失败 5 次低条减速生效、鱼王豁免、成功清零） |
| 已消费动作 | `Build:ROUND-20260812-16:4EF947D52B7C67357BDDB981661AD9A3CD10963EFA8E6CE3384BDBF535D480D0`；`Deploy:ROUND-20260812-16:4EF947D52B7C67357BDDB981661AD9A3CD10963EFA8E6CE3384BDBF535D480D0` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-051-1 | 代码侧结束 | 根因修复 | 逃逸减速状态转换日志 | 真实钓鱼失败连续重试 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-051-1 高难鱼连续失败无递增逃跑减速 | R2 完成 | 沙箱验收（倍率与豁免） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `4EF947D52B7C67357BDDB981661AD9A3CD10963EFA8E6CE3384BDBF535D480D0`（2026-08-12 22:22 修订部署；备份 `DeploymentBackups\FishingExpanded-20260812-222219-pre-BATCH057`=2A86D412...；源/目标哈希一致；config.json 未触碰；i18n 未变化） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-051-1 | 高难鱼失败多次后逃跑速度无变化 | 0 | 无 | RC-051 | 实施中 | 5 次失败倍率=-10 级等效 |

## RC-051 根因卡

- 根因状态：已证实（源码 + 用户规则）
- 第一处分歧：蓄力槽保护管线无“按连续失败递增”的减速来源
- 决定性证据：`GetCatchPenaltyModifier` 仅全局+负等级分支；`Update_Prefix` 未读 `GetConsecutiveFailCount`
- 竞争解释表：竞争解释豁免（理由=新机制，无互斥根因）
- 唯一所有者：`BobberBarPatches.Update_Prefix`（写入 `___distanceFromCatchPenaltyModifier`）
- 最短区分动作：实现后沙箱验证倍率

## R0

- `DifficultyCalculator.GetEscapeFailBonusModifier(level, consecutiveFails, catchProgress)` 纯函数
- `Update_Prefix`：`!HasChallengeBait && AdjustedDifficulty > 100f` 且 `GetConsecutiveFailCount>0` 时用该函数替换 `newModifier`
- `InstanceData.EscapeBonusEngaged` + 生效/解除单发日志

## R2

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 连续失败 5 次、蓄力 ≤20% | 倍率 ≈0.60（-10 级等效） | 仍 0.80 |
| 连续失败 5 次、蓄力 ≤1% | 倍率 ≈0.20 | 仍 0.50 |
| 失败 0 次 | 原倍率不变 | 无端减速 |
| 1/3 次 | 线性插值（≈0.75/0.65、0.44/0.32） | 阶梯突变 |
| 蓄力 >40% | 无效果（-10 级目标=1.0） | 高条减速 |
| 挑战鱼饵 | 同样参与（连续失败 5 次低条减速到 -10 级等效；95 级以上也吃，BATCH-057） | 不参与 |
| 鱼王 | 天然豁免（无 InstanceData） | 参与 |
| 成功清零 | 下一次小游戏回到 0 次 | 跨鱼种清零/保留 |
| 力竭共存 | 减速只作用于距离惩罚倍率，不动 EffectiveDifficulty | 覆盖力竭 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `AF073AE7E940A183071565172A769E02E70959453A647D51B5D8FDCF7E6461CA`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）

## BATCH-057 修订（2026-08-12 用户指令）

- 代码：`Update_Prefix` 逃逸减速条件由 `!data.HasChallengeBait && data.AdjustedDifficulty > 100f` 改为 `data.AdjustedDifficulty > 100f`（挑战鱼饵同样参与；鱼王天然豁免不变）
- 文档：GAME-DESIGN.md §10、WIKI.md 汇总表、TESTING.md 回归行同步删除“挑战鱼饵豁免”，并新增验收“挑战鱼饵连续失败 5 次后低条减速生效”
- 部署：见上方构建/部署事实（本卡顶部状态以 BUG-LEDGER 与最新部署为准）

## 收尾与归档

- 已完成：口径确认、批次卡、R0 设计
- 下一条准确操作：R0 实施 → Release Rebuild → 静态核验 → 等待用户授权部署
