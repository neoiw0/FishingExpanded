# BATCH-031：全量设计↔代码一致性核对（命名残留 + 失败提示优先级定稿）

**创建**：2026-08-07
**类型**：设计/代码全量核对（用户要求 “再次核对所有游戏设计，看看是否对应代码，是否有鲁棒性。明确的问题修改并部署”）
**状态**：核对 ✅ → 文档修正 ✅ → 部署一致性核验待执行（本轮无运行时代码变化）

## 本轮准入证据（用户指令 + 既有批次证据）

- 用户本轮指令：全量核对设计↔代码与鲁棒性，明确问题修改并部署。
- 复用证据：BATCH-029/030 已封存日志 `RuntimeEvidence\20260807-BATCH029-REGRESSION-01`（SHA256 已登记）；当前部署 DLL `108C3A2C...`（BATCH-030 修复版，2026-08-07 22:49，备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`）。

## 核对结论（设计↔代码一致项，逐条核对源码）

- 难度/数量/经验/品质公式：`DifficultyCalculator` 与设计 1.2 完全一致（负数 [0.5,1]、正数 [1,50]、数量/经验 [1,200]、品质 floor(level/5) → ≥3 钳到 4）。
- 尺寸数字/视觉缩放：正级 +10%/级、负级 -5%/级（`GetFishSizeMultiplier`）；视觉线性斜率 0.0136102（0级=1.0、100级≈3.7084=原25级大小），与设计 1.2/3.x 一致；BATCH-024/025/026 已实测验收。
- 锚点合同：手持/落地真鱼底边中点（`ObjectPatches`/`AdjustLandingFishPosition`）、飞行动画中心（`DoPullFishFromWater`）、小游戏鱼标与结算面板不缩放（`FishingRodPatches.Draw_Transpiler` 只缩放落地图）。
- 高难度运动公式：>100 初始目标=顶部（`bobberTargetPosition=0f`）；换目标频率三处 + dart 偏移按 150 封顶（`Update_Transpiler` 5 处 `Math.Min(d,150)` 注入）；加速度 `1 + 档位×(d-100)/100`（等级 ≥89 档位=1、≤88 档位=0.3，d≤100 返 1）。
- 高难度鱼跳机制：分档 8/6/5/4/3 秒（`GetJumpInterval`）、0.5s 延迟、1 秒检测、跳完才重新计时（`JumpCooldownSeconds` 在跳后重置）、上下 25% 区域 `≤133`/`≥399`（原生范围 [0,532]）。
- 星标/皇冠：调整后难度 ≥120 成功才获得（`RecordHighDifficulty`）、Infinity Crown 贴图绘制、图鉴按鱼独立显示 +0.5。
- 称号表：89-99 创世神、100 混沌（`GetRankKey` 与设计 2.1 表格一致）；越级限制表一致。
- 鱼王 5 只 ID 列表 + 完整豁免（`SpecialFishHelper.LegendaryFishIds`）。
- 非鱼类上限 8（`GetMaxLevelForNonFish()`）；`IsNonFish`=Category≠-4 与设计 6.2 定义一致。
- 存档兼容：`FishStats.ConsecutiveFailCount` 缺失反序列化默认 0；BATCH-030 已排除存档不兼容。
- 弱称号场景：`ChallengeDialogueGenerator` 已不嵌入 rank.weak；零以下胜利显示弱称号（BATCH-030 修复已部署）。

## 明确问题（2 处，均为文档层，无运行时代码/i18n 变化）

1. **GAME-DESIGN.md 4.6 旧措辞残留**：原 “有效等级包含已经取整的隐藏钓鱼技能加成” 与用户已确认措辞 “钓鱼条长度额外加成”（BATCH-025）不一致 → 改为 “有效等级包含已经取整的钓鱼条长度额外加成（每颗收藏星标 +0.5，合计后向下取整）”。
2. **GAME-DESIGN.md 2.2 未写明失败提示优先级**：代码 `ShowFailureNotification` 已定序（史诗 > 触底 > 负等级 > 普通），设计未记录 → 补充优先级行；证据：封存日志 21:30:28 太阳鱼 `等级: -10 | 触底: True | 史诗提示: True` 显示史诗文案。

## 鲁棒性复核（无需修改）

- `PullFishFromWater_Prefix`：`adjustedDifficulty` 优先取 `BobberBarPatches.GetAdjustedDifficulty`（小游戏实际 float），无小游戏时才回退原生 int `fishDifficulty`；日志证据（如 `调整后 133.2`）显示 float 路径正确。
- `DifficultyManager.GetData/GetDifficultyLevel` 越界按玩家 clamp；`NormalizeData` 剔除鱼王键。
- `GiantFishManager.IsFish` 只接受 Category=-4，与设计 3.1 一致。
- 跳鱼状态机无新增状态写入者；`ConditionalWeakTable` 生命周期随 BobberBar 释放。

## 修改文件

- `GAME-DESIGN.md`（2.2 优先级 + 4.6 措辞）
- `TESTING.md`（“技能加成”→新措辞 2 处；UI 与展示行补优先级语义）
- `MAINTENANCE-INDEX.md`（“技能加成”→新措辞 1 处）

## 构建/部署决定

- 本轮无运行时代码与 i18n 变化 → 不需要新构建产物、不需要重新部署。
- 部署门禁改为核验：当前安装 DLL 哈希 = 源码构建哈希（确认 BATCH-030 部署后源码未再产生运行时差异）。
- 不覆盖 `config.json`；不启动游戏（用户实测由玩家执行）。

## 委派评估

- NotBeneficial：纯文档核对与精确子串替换，无委派价值；未使用委派。

## Closeout/静态门禁

- 治理同步：`GAME-DESIGN.md`、`TESTING.md`、`MAINTENANCE-INDEX.md`、`BUG-LEDGER.md`、本卡。
- Git 基线：HEAD=`8f799da`，工作树为既有批次混改 + 本轮文档修改；禁止 `git add -A`，按明确路径核对。
- 真实验收：本轮无玩家可见行为变化，无需新验收；BATCH-029/030 既有待实测项保持不变，每现象独立标记。