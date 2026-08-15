# FishingExpanded 根因批次：BATCH-050 89 级超高难入口修正（88 及以下最多升到 89）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-09 |
| 当前活动类别 | CAT-01（89 级超高难入口） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-09:13D6752E... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-050） |
| 本轮用户问题范围 | 用户 2026-08-12 修正 BATCH-049：88 级仍可 +11 到 99 不对，最多只能加到 89；89 级以上属于超高难挑战区间，只能 3 级一次增加 |
| 本轮纳入 Case | FE-050-1 88 级（及 67-88 区间）可跳过 89 直接进入 89-99 内部 |
| 共享第一处分歧与所有权链证据 | 成功结算唯一所有者=`DifficultyManager.RecordSuccess`；第一处分歧=BATCH-049 只对 `oldLevel>=89` 加 +3 上限，88 级仍按下一称号区间封顶到 99 |
| 排队或暂不纳入 Case | BATCH-042~049 已构建/部署待验收 |
| 合并/拆分决定及依据 | 单 Case 单类别：同属成功结算唯一所有者，BATCH-049 规则修正 |
| 本轮准入证据 | 用户明确修正；`RecordSuccess` 源码复核（BATCH-049 后 88 级 allowedGain=min(请求,11)） |
| 现有实现复核 | BATCH-049 分支 `oldLevel>=89 → min(...,3)`；`oldLevel<89` 未处理入口 |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`（RecordSuccess）；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、BATCH-049 卡、本卡 |
| 冻结 Case/禁止范围 | 失败结算、100 封顶、鱼王豁免、GMCM、部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | `oldLevel < 89` 时 `allowedGain = min(allowedGain, max(0, 89 - oldLevel))`（最多进入 89）；`oldLevel >= 89` 保持 `min(...,3)` |
| 可观测性决定 | 复用现有“实际增长”日志；无新增诊断 |
| 自动化验收决定 | 沙箱：88→89（+1）、87→89（+2）、80→89（+9 上限）、89→92、97→100、99→100 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~049 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C`；源码核验 `oldLevel<89 → min(allowedGain, 89-oldLevel)` 分支已编译 |
| 唯一下一步 | 真实集中测试（88→89、87→89、80→89、89→92） |
| 已消费动作 | `Build:ROUND-20260812-09:13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C`；`Deploy:ROUND-20260812-09:13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-050-1 | 代码侧结束 | 根因修复 | “实际增长”日志 | 真实钓鱼成功 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-050-1 88 级可跳过 89 | R2 完成 | 沙箱验收（88→89） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C`（2026-08-12 15:08 部署；备份 `DeploymentBackups\FishingExpanded-20260812-150831-pre-BATCH050`=19683A67...；源/目标哈希一致；config.json 未触碰；i18n 未变化） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-050-1 | 88 级一次成功可跳到 99 | 1（BATCH-049 已部署，本次为规则修正反证） | 2026-08-12 用户修正 | RC-050 | 实施中 | 88→89 |

## RC-050 根因卡

- 根因状态：已证实（源码 + 用户规则）
- 第一处分歧：BATCH-049 未约束 89 入口，`oldLevel<89` 仍按下一称号区间封顶
- 决定性证据：`RecordSuccess` BATCH-049 后分支；用户明确“88 最多加到 89”
- 竞争解释表：竞争解释豁免（理由=用户规则 + 静态契约完整）
- 唯一所有者：`DifficultyManager.RecordSuccess`
- 最短区分动作：加入口 cap 后沙箱验证

## R0

- `oldLevel < 89`：`allowedGain = min(allowedGain, max(0, 89 - oldLevel))`

## R2

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 88 级成功 | +1 → 89 | +11 到 99 |
| 87 级成功 | +2 → 89 | 跳进 89-99 |
| 80 级成功 | 最多 +9 → 89 | 跳到 99 |
| 89 级成功 | +3 → 92 | 超过 +3 |
| 99 级成功 | +1 → 100 | 越过 100 |
| 低等级 | 原区间规则不受影响（如 8→15） | 被 89 入口误限 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）

## 收尾与归档

- 已完成：规则确认、批次卡、R0 设计
- 下一条准确操作：R0 实施 → Release Rebuild → 静态核验 → 等待用户授权部署
