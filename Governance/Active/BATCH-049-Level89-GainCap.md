# FishingExpanded 根因批次：BATCH-049 89 级以上每次成功最多 +3 级

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-08 |
| 当前活动类别 | CAT-01（89 级以上成功增长上限） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-08:19683A67... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-049） |
| 本轮用户问题范围 | 用户 2026-08-12 新增规则：89 级以上，每次最多加 3 级难度等级（按含 89 级实现） |
| 本轮纳入 Case | FE-049-1 高等级成功一次可跨多级（89→100 区间内无 +3 上限） |
| 共享第一处分歧与所有权链证据 | 成功结算唯一所有者=`DifficultyManager.RecordSuccess`；第一处分歧=现逻辑只按下一称号区间封顶（`nextRankCeiling - oldLevel`），89 级时仍可 +11 |
| 排队或暂不纳入 Case | BATCH-042~048 已构建/部署/待验收 |
| 合并/拆分决定及依据 | 单 Case 单类别：同属成功结算唯一所有者 |
| 本轮准入证据 | 用户明确规则；`RecordSuccess` 源码复核（89 级 nextRankCeiling=100 → 允许 +11）；运行日志 13:01:09 97→100 实际 +3 仅为区间封顶结果 |
| 现有实现复核 | `allowedGain = min(levelGain, nextRankCeiling - oldLevel)`；无固定 +3 上限 |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`（RecordSuccess）；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 失败结算、称号区间、鱼王豁免、GMCM、部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | `oldLevel < 89` 时 `allowedGain = min(allowedGain, 89 - oldLevel)`；`oldLevel >= 89` 时 `allowedGain = min(allowedGain, 3)`（仍受 100 封顶约束；BATCH-050 修正 88 级入口规则） |
| 可观测性决定 | 复用现有“实际增长”日志；无新增诊断 |
| 自动化验收决定 | 沙箱：89→92（+3）、90→93、97→100、99→100、88 级不受 +3 限制（按原区间规则） |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~048 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`；反编译核验 `oldLevel >= 89` cap 已编译 |
| 唯一下一步 | 真实集中测试（89→92、97→100、88 不受限） |
| 已消费动作 | `Build:ROUND-20260812-08:19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`；`Deploy:ROUND-20260812-08:19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-049-1 | 代码侧结束 | 根因修复 | “实际增长”日志 | 真实钓鱼成功 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-049-1 89 级以上无 +3 上限 | R2 完成 | 沙箱验收（89→92） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`（2026-08-12 14:51 部署；备份 `DeploymentBackups\FishingExpanded-20260812-145100-pre-BATCH049`=DE23813F...；源/目标哈希一致；config.json 未触碰；i18n 已同步） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-049-1 | 89 级一次成功可加超过 3 级 | 0 | 无 | RC-049 | 实施中 | 89→92 |

## RC-049 根因卡

- 根因状态：已证实（源码）
- 第一处分歧：RecordSuccess 只按下一称号区间封顶，无固定 +3 上限
- 决定性证据：`nextRankCeiling(89)=100` → 允许 +11
- 竞争解释表：竞争解释豁免（理由=静态契约完整，无互斥假设）
- 唯一所有者：`DifficultyManager.RecordSuccess`
- 最短区分动作：加 cap 后沙箱验证

## R0

- `oldLevel >= 89` 时 `allowedGain = Math.Min(allowedGain, 3)`

## R2

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 89 级成功 | +3 → 92 | +11 |
| 90 级成功 | +3 → 93 | 跨多级 |
| 97 级成功 | +3 → 100（封顶） | 超出 100 |
| 99 级成功 | +1 → 100 | +3 越过 100 |
| 88 级成功 | +1 → 89（BATCH-050） | 跳到 99 |
| 87 级成功 | +2 → 89（BATCH-050） | 跳进 89-99 内部 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）

## 收尾与归档

- 已完成：源码复核、批次卡、R0 设计
- 下一条准确操作：R0 实施 → Release Rebuild → 静态核验 → 等待用户授权部署
