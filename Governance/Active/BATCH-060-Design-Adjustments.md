# FishingExpanded 根因批次：BATCH-060 设计调整（用户确认 7 项）

> 活动表只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260815-01` |
| 当前活动类别 | CAT-01（用户确认的 7 项设计调整） |
| 整合候选状态 | 已部署（2026-08-15 11:48，用户授权；含 BATCH-058T 文案统一候选） |
| 当前权威活动卡 | 本卡 |
| 本轮用户问题范围 | 7 项设计调整（2026-08-15 用户逐条确认；Q1–Q9 澄清后拍板）：①89+ 每次成功最多 +6；②助战提示 15 秒（仅助战文案）；③经验倍数 = max(1, round(level×0.5))；④品质 50 级提升到铱（门槛式，0~50 由实现规划为 10 银/25 金/50 铱）；⑤WIKI 与 GAME-DESIGN 均加术语表；⑥挑战鱼饵掉星左下角提示；⑦数量倍数 = max(1, round(level×0.5))；⑧巨型鱼改按难度等级 ≥8 判定（不再与数量倍数挂钩） |
| 本轮纳入 Case | CAT-01（7 项调整 + 巨型鱼连锁） |
| 共享第一处分歧与所有权链证据 | 无 Bug 根因；全部为用户确认的设计变更。所有权：数量/经验/品质/缩放=DifficultyCalculator；等级上限=DifficultyManager；巨型鱼展示=GiantFishManager/FishDisplayData；结算数量=FishingRodPatches；助战/掉星提示=BobberBarPatches+HUDNotifier；i18n=zh/default.json |
| 排队或暂不纳入 Case | 无 |
| 合并/拆分决定及依据 | 7 项同属"用户一次授权的设计调整"（同一轮次、同一批准来源），合并为一个类别 CAT-01 实施，一次统一构建 |
| 本轮准入证据 | 用户 2026-08-15 消息：草案 7 条 + Q1–Q9 逐条澄清 + "有问题的，都改过来"（明确实施授权）；含"游戏原生就有精通为溢出等级的出口"（经验 10 级后流入精通，D3 空洞论断修正依据） |
| 现有实现复核 | 数量/经验倍数 S:DifficultyCalculator.cs:31-49（原 ×1..×200）；品质 S:DifficultyCalculator.cs:51-77（原 floor(level/5) 累加钳铱）；视觉缩放 S:DifficultyCalculator.cs:91-101（原按数量倍数）；89+ 上限 S:DifficultyManager.cs:180-185（原 +3）；巨型鱼门槛 S:GiantFishManager.cs:42-61 + S:FishingRodPatches.cs:722-728（原数量倍数 >15）；助战提示 S:BobberBarPatches.cs:1591（原 5 秒）；掉星 S:BobberBarPatches.cs:704-712（原仅日志+贴图）；鱼王豁免/非鱼 8 级/结算唯一边界均不受影响 |
| 允许修改范围 | S:DifficultyCalculator.cs、S:DifficultyManager.cs、S:GiantFishManager.cs、S:FishDisplayData.cs、S:FishingRodPatches.cs、S:BobberBarPatches.cs、S:HUDNotifier.cs、S:ModEntry.cs、S:i18n/zh.json、S:i18n/default.json、GAME-DESIGN.md、WIKI.md、TESTING-GUIDE.md、本卡、BUG-LEDGER.md |
| 冻结 Case/禁止范围 | 其余全部机制冻结；不触碰蓄力槽保护族、力竭/停战/背板/短语逻辑、多人/分屏边界、config 重置 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 7 项设计调整按清单实施完毕并构建验证 |
| 可观测性决定 | 复用现有日志（数量转化 FishingRodPatches:469-473、难度等级更新、助战触发、掉星 BobberBar:707-711 已有）；新增 HUDNotifier 掉星提示日志 1 条（Info 级，掉星时单发）；无逐帧日志 |
| 自动化验收决定 | 场景=数值与展示矩阵；存档影响=无（数量/经验/品质为运行时派生，FishDifficultyData 结构不变）；触发入口=fish_setlevel 设定等级后实测数量/品质/经验/缩放 + fish_giant <等级> 验证巨型鱼门槛（≥8 触发、<8 不触发）+ fish_selftest 只读自测（不新增）；成功判据=数值与文档公式一致；失败判据=偏离公式或旧路径残留；关闭条件=验收完成后关闭 |
| 当前类别阶段 | R0/R1/R2 完成；代码侧结束（构建通过，待部署） |
| 当前工作树 | 完整修改（代码+文档已改，构建通过） |
| 本轮统一构建 | 已执行（0 警告 0 错误；DLL SHA-256 `DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`；反编译核验：89+ 分支 Math.Min(gain,6)、数量/经验 ×0.5 AwayFromZero、GetQualityTier(10/25/50)、品质 max、缩放 0.0270843f、助战 LifetimeOverride 15f、ShowChallengeStarLoss/hud.starLoss、巨型鱼 level>=8 均编译进 DLL） |
| 唯一下一步 | 玩家重启游戏后真实验收（当前会话仍为旧 DLL，重启生效）；按集中测试顺序 6 项逐项验收 |
| 已消费动作 | `Build:ROUND-20260815-01:DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`；`Deploy:ROUND-20260815-01:DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`（2026-08-15 11:48，用户授权；备份 `DeploymentBackups\FishingExpanded-20260815-114839-pre-BATCH060-058T`=6B79AE60...；源/目标哈希一致；i18n×2 同步含 BATCH-058T 文案；config.json 未触碰） |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 全部改动已由主控直接完成（改前已读全部目标文件；NotBeneficial：无独立可并行子任务） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | 7 项设计调整 | 实施中（代码+文档完成） | 用户确认的设计变更（无根因） | 复用现有日志；掉星提示新增 1 条 Info 日志 | fish_setlevel/fish_giant/fish_info/fish_selftest |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| CAT-01 | 设计完成（代码+文档完成） | Release 构建通过 |

> 机制断言：本批为用户确认的设计变更，无 Bug 根因竞争解释；竞争解释表豁免（理由=全部条款由用户逐条确认并澄清，不存在互斥假设；变更边界=已确认条款替换，旧路径 R1 删除）。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01 数量/经验倍数 | `fish_setlevel <鱼> 100` 后钓获 | 100 级 → ×50；0/负等级 → ×1 | 通过/失败/未执行 |
| 2 | CAT-01 品质门槛 | `fish_setlevel` 10/25/50 后钓获 | 银/金/铱（max(原品质,门槛)） | 通过/失败/未执行 |
| 3 | CAT-01 89+ 上限 | `fish_setlevel <鱼> 89` 后完美钓获 | 89→95→100，每次 ≤+6 | 通过/失败/未执行 |
| 4 | CAT-01 巨型鱼门槛 | `fish_giant <鱼> 8` 与 `fish_giant <鱼> 7` | 8 级触发 NPC、7 级不触发；视觉缩放按等级 | 通过/失败/未执行 |
| 5 | CAT-01 助战 15 秒 | `fish_assist` 强制触发 | 助战文案 15 秒淡出（其余 5 秒） | 通过/失败/未执行 |
| 6 | CAT-01 掉星提示 | 挑战鱼饵拖过 5:00（90–94 级） | 左下角提示剩余星与 −20%；≥95 无提示 | 通过/失败/未执行 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 候选 v1：`DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`（0 警告 0 错误）；2026-08-15 11:48 已部署（备份 `DeploymentBackups\FishingExpanded-20260815-114839-pre-BATCH060-058T`=6B79AE60...；源/目标哈希一致；i18n×2 同步含 BATCH-058T 文案；config.json 未触碰）；部署时游戏运行中（文件可覆盖，当前会话仍为旧 DLL，重启生效）。**候选 v2（追加第 9 项 VanillaTips 注入）：`B5CC284E655C661F4C8D6F6D8430F795AE2796B7FA5D10BC5C32FF29A95A25BE`（0 警告 0 错误，反编译核验 TryRegister/RegisterTips("YourName.FishingExpanded", 11f)/7 ids 均编译进 DLL）**；v1 已部署不含注入代码，v2 需重新部署授权 |
<!-- CURRENT-STATE-END -->

## 变更清单（用户确认）

1. **89+ 每次成功最多 +6 级**（原 +3，BATCH-049/050）：`DifficultyManager.RecordSuccess` 89+ 分支 3→6；88 及以下最多升到 89 的入口规则保留。89→95→100 需 2 次成功。
2. **助战提示 15 秒**（仅助战文案，其余提示 5 秒）：`FloatingTip` 新增 `LifetimeOverride`/`DisplayLifetime`，`AddTip` 加 `lifetimeOverride` 参数，助战调用传 15f；上移/淡出按 15 秒线性。
3. **经验倍数 = max(1, round(level×0.5))**（原 ×1..×200）：`GetExperienceMultiplier` 复用新数量倍数公式；10 级后经验流入原生精通系统（用户指正，D3"经验空洞"论断修正依据）。
4. **品质门槛式"提升到"**：10 级→银、25 级→金、50 级→铱；最终品质 = max(原品质, 门槛)；`GetQualityBonus` 退休 → `GetQualityTier`；非鱼 8 级无品质提升。
5. **术语表**：GAME-DESIGN.md 新增"术语表"章节（难度等级/调整后难度/有效难度/难度档位/数量倍数/可计数皇冠/鱼竿熟练度 α/挑战星/背板种子）；WIKI.md 新增 §1.5 术语表；WIKI 头注更新至 BATCH-060（原声明 BATCH-041 过期）。
6. **挑战鱼饵掉星左下角提示**：`HUDNotifier.ShowChallengeStarLoss`（FIFO 队列，小游戏期间可见），i18n `hud.starLoss` 中英双语；掉星防重点（LastChallengeStarsLogged）单发；≥95 豁免不掉星天然不触发。
7. **数量倍数 = max(1, round(level×0.5))**（原 ×1..×200）：`GetQuantityMultiplier` 新公式；连锁——巨型鱼门槛与视觉缩放解耦（见 8）；万能鱼饵 12×50=600、挑战 5 分钟 5×50=250 均 <999，A16 钳制丢鱼问题消失。
8. **巨型鱼按难度等级 ≥8**（连锁）：`RecordGiantFish` 参数 multiplier→level、门槛 >15→≥8；`GetVisualScale` 输入改等级（`1 + level×0.0270843`，100 级 ≈3.7084 端点不变）；`FishDisplayData.ActiveGiantFish` 元组 multiplier→level；`fish_giant <鱼ID> <等级>` 语义更新（TESTING-GUIDE/WIKI 同步）。
9. **VanillaTips 提示注入（2026-08-15 用户指令，来源权重 11）**：新增 `S:VanillaTipsIntegration.cs`——`GameLaunched` 时经 `neoiw.vanillatips` API 以来源 `YourName.FishingExpanded`、来源级权重 11 注册 7 条钓鱼机制提示（【渔】前缀、中英双语、分类 general；机制：力竭/星之果茶/巨型鱼/助战/助战排位/α 终点/挑战鱼饵无助战）。未装 VanillaTips 时 `GetApi` 返回 null 静默跳过（无硬依赖）；不修改 VanillaTips 任何文件（该模组由并行窗口开发）。权重语义：来源级 0~50，首次注册值为默认、GMCM 可调。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 7 项按公式生效 | 旧 ×200/15 级铱/+3/倍数门槛残留 | 待构建后核验 |
| 本地主屏/副屏 | 数据按玩家隔离不变 | 副屏读写主屏数据 | 未触及该路径 |
| 远程与四人混合 | 数量/品质/经验按实例 Owner 结算 | 跨玩家发放 | 未触及该路径 |
| 普通鱼/非鱼类/鱼王 | 非鱼 8 级 ×4 倍、无品质提升；鱼王完整豁免（不经数量/品质边界） | 非鱼/鱼王被新公式影响 | 鱼王在 RecordSuccess/品质前已 return（S:FishingRodPatches.cs:348） |
| 数量、动画与 ItemGrabMenu | 动画仍原生条数；数量倍数只在 CreateFish 后应用 | 二次乘倍、动画数量变化 | 未触及动画路径 |
| 难度结算/HUD/图鉴/手持展示 | 手持缩放按等级（巨型鱼 ≥8）；图鉴行不变 | 缩放与数量倍数重新挂钩 | GetVisualScale 调用点 4 处已全量改为等级 |
| 换日/重载/标题/断线 | 巨型鱼展示与 NPC 每日重置不变 | 新字段/存档结构变化 | FishDifficultyData 结构未变 |
| 性能最坏情况 | 无新增逐帧日志/计算 | 每帧新增分配 | 新增仅掉星单发提示 |
| 已验收相邻回归 | 助战/掉星/力竭/停战/短语共用提示通道不回归 | 其他提示时长被误改 | 仅助战调用传 15f |

## 构建、部署与集中测试

- 构建结果：待执行
- 版本/哈希：
- 部署文件与目标：
- 本次单局路线：
- 日志/截图/存档证据：
- 每个 Case 的实际结果：

## 收尾与归档

- 已完成：7 项代码 + GAME-DESIGN.md/WIKI.md/TESTING-GUIDE.md 文档同步；评审文档待加实施注记
- 当前不确定性：真实验收待用户（未授权部署）
- 下一条准确操作：Release 构建并核验 DLL 哈希
- 总账与测试路线是否已覆盖更新：总账条目 58 已添加（见 BUG-LEDGER.md）；TESTING.md 长期回归维度：视觉缩放行已与新公式一致，无需改
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收通过并用户确认后归档
