# FishingExpanded 根因批次：BATCH-048 100 级流动皇冠 1.2 倍 + 皇冠图层优化

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-07 |
| 当前活动类别 | CAT-01（100 级流动皇冠 + 图层） |
| 整合候选状态 | 已部署待集中测试（随 BATCH-049 统一部署；Deploy:ROUND-20260812-08:19683A67...） |
| 当前权威活动卡 | 本卡（BATCH-048） |
| 本轮用户问题范围 | 用户 2026-08-12 要求：①战胜 100 级鱼获得的流动皇冠尺寸改为 1.2 倍；②皇冠图层应在鼠标和收藏页悬停详情 UI 之下 |
| 本轮纳入 Case | FE-048-1 100 级流动皇冠无 1.2 倍标记；FE-048-2 皇冠绘制于详情 UI 之后（layer 0.99f）导致覆盖 |
| 共享第一处分歧与所有权链证据 | 绘制=CollectionsPagePatches 旧 Draw_Postfix（整页后置、layer 0.99f）；第一处分歧=①ChallengeCrowns 未记录“100 级”来源；②皇冠在 Deferred 批次中晚于 drawToolTip 提交 |
| 排队或暂不纳入 Case | BATCH-042~047 已构建/部署待验收；BATCH-046 已构建待部署 |
| 合并/拆分决定及依据 | 两个改动同属图鉴皇冠展示所有权，合并单类别 |
| 本轮准入证据 | 用户明确要求；`_analysis\CollectionsPage.full.cs:820-880` 原生 draw 结构（FrontToBack 图标层 0.86/0.88 → Deferred 批次 drawToolTip）；`ClickableTextureComponent.draw(SpriteBatch,Color,float,int,int,int)` 反编译签名；`CollectionsPage.currentTab` 为 public |
| 现有实现复核 | 旧 Draw_Postfix 在整页绘制后画皇冠（layer 0.99f）；`RecordChallengeCrown` 无 100 级标记；`FishDifficultyData` 无对应字段 |
| 允许修改范围 | `Source\Data\FishDifficultyData.cs`、`Source\Services\DifficultyManager.cs`、`Source\Patches\FishingRodPatches.cs`、`Source\Patches\CollectionsPagePatches.cs`；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 难度/结算/鱼王豁免；union 阈值；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 新增 `Level100FlowCrowns` 存档字段（挑战开始时难度等级≥100 + 挑战鱼饵成功时记录）；图鉴皇冠改为跟随每个鱼图标绘制（同批 layer=图标+0.01，位于悬停详情 UI/鼠标之下）；100 级流动皇冠基础尺寸 ×1.2 |
| 可观测性决定 | 挑战皇冠日志增加“100级流动皇冠数”；无新增诊断 |
| 自动化验收决定 | 沙箱：`fish_setlevel 100` + 挑战鱼饵成功 → 图鉴皇冠 1.2 倍；悬停详情 UI 覆盖皇冠、鼠标在最上层；99 级成功不记 1.2 倍 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~047 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`；反编译核验 Level100FlowCrowns/atLevel100/layerDepth+0.01 已编译 |
| 唯一下一步 | 用户授权部署后统一集中测试（100 级 1.2 倍 + 图层目视） |
| 已消费动作 | `Build:ROUND-20260812-07:19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-048-1/2 | 代码侧结束 | 根因修复 | 挑战皇冠日志含 100 级计数 | 图鉴鱼类页目视 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-048-1 100 级流动皇冠无 1.2 倍 | R2 完成 | 目视验收 |
| FE-048-2 皇冠覆盖详情 UI | R2 完成 | 目视验收 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`（2026-08-12 14:51 随 BATCH-049 统一部署；备份 `DeploymentBackups\FishingExpanded-20260812-145100-pre-BATCH049`） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-048-1 | 100 级流动皇冠与普通流动皇冠同尺寸 | 0 | 无 | RC-048 | 实施中 | 1.2 倍目视 |
| FE-048-2 | 皇冠盖住悬停详情 UI/鼠标 | 0 | 无 | RC-048 | 实施中 | 皇冠在 UI/鼠标之下 |

## RC-048 根因卡

- 根因状态：已证实（源码 + 原生绘制契约）
- 第一处分歧：无 100 级来源标记；皇冠在整页后置绘制（Deferred 晚于 drawToolTip）
- 决定性证据：`CollectionsPage.draw` 反编译（FrontToBack 0.86/0.88 → Deferred drawToolTip）；旧 postfix layer 0.99f
- 竞争解释表：竞争解释豁免（理由=绘制契约静态完整，无互斥假设）
- 唯一所有者：`CollectionsPagePatches`（皇冠展示）；`DifficultyManager`（标记记录）
- 最短区分动作：实施后目视验收

## R0

- `FishDifficultyData.Level100FlowCrowns`（旧存档默认空）；`RecordChallengeCrown(atLevel100)`；`HasLevel100FlowCrown`
- 皇冠绘制迁移到 `ClickableTextureComponent.draw` 3 参+3 可选参重载的 Postfix：同批 layer=图标+0.01，100 级 ×1.2

## R1

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `CollectionsPage.draw` 整页后置皇冠 postfix（layer 0.99f） | 本卡 R0 | 否 | 删除 |

## R2

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 100 级挑战鱼饵成功 | 皇冠 1.2 倍流动 | 普通尺寸 |
| 99 级挑战鱼饵成功 | 普通流动尺寸 | 误记 1.2 倍 |
| 悬停鱼图标 | 详情 UI 覆盖皇冠、鼠标最上层 | 皇冠盖 UI |
| 普通皇冠 | 24px 不变、图层同规则 | 尺寸变化 |
| 旧存档 | Level100FlowCrowns 默认空 | 报错 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）

## 收尾与归档

- 已完成：契约核对、批次卡、R0 设计
- 下一条准确操作：R0 实施 → Release Rebuild → 静态核验 → 等待用户授权部署
