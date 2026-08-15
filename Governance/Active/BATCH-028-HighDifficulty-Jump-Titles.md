# BATCH-028：高难度运动公式修正 + 高难度鱼跳机制 + 称号表更新

**创建**：2026-08-06
**类型**：设计需求落地（用户已确认设计，授权开始实现；涉及原生 `BobberBar.update`，执行完整调用链与 R0/R1/R2）
**状态**：R0 ✅ / R1 ✅ / R2 ✅；Release Rebuild ✅（0 警告 0 错误）；**已部署**（2026-08-06 19:55，用户授权）；启动与真实验收待用户执行
**当前唯一根因/任务**：按用户定稿设计实现高难度运动公式修正（初始目标/换目标频率/加速度增幅）+ 高难度鱼跳机制 + 称号 89-99 创世神/100 混沌；Transpiler 注入点已用当前安装 IL 证据程序化核验（5 注入 + 3 安全跳过 + 唯一 stfld 点）。
**唯一下一步**：等待用户启动游戏真实验收（每档难度独立验收：100/120/150/200/300/500/2000）。

## 玩家需求（原话要点）

- “难度大于100时候，初始目标为顶部”
- “难度大于150的时候，换目标的频率按照150难度计算”（确认：三处都按 150；dart 也需要封顶，2026-08-06 用户确认）
- “难度大于100的时候，加速度分母 = 随机(10,30)，高于100的部分直接增幅到加速度上（线性，无上限）”
- 高难度鱼跳机制：150~250 每8秒、251~350 每6秒、351~450 每5秒、451~550 每4秒、551+ 每3秒；鱼在上下 25% 区域 0.5 秒后瞬移到对侧 25% 随机位置；不在 25% 则每 1 秒检测；延迟内游走仍照跳；上次跳完成才能重新计时；仅非鱼王
- 称号：89-99 创世神、100 混沌（67-88 仍神王）；越级限制：神王→99、创世神→100、混沌→100
- “先写入游戏设计本，不要动代码” → 2026-08-06 设计已写入 `GAME-DESIGN.md`（高难度鱼跳机制/运动公式修正/原生代码对齐小节）并获用户确认，本轮开始实现

## 已证实事实（当前安装 `D:\GGGGG\K1515\Stardew Valley.dll` SHA-256 `DFE341CA...` 未漂移；IL 证据 `_analysis\bobberbar-il-20260806\Stardew Valley.il`，2026-08-06 从当前 DLL 提取）

- 原生行为类型：`mixed=0、dart=1、smooth=2、sinker=3、floater=4`；`Data/Fish` 行为字段决定 motionType（构造 IL）。
- 每帧三处换目标概率（update IL）：大目标 `difficulty×(motionType!=2?1:20)/4000`（IL_023e 读取 difficulty）；小偏移 `difficulty/2000`（IL_03bb）；dart 额外 `difficulty/1000`（IL_0422）；dart 偏移 `±(50~100+difficulty×2)`（IL_044d/IL_0465 读取 difficulty）。
- 加速度：`(target-pos)/(random(10,30) + (100 - min(100,difficulty)))`（IL_0373 读取 difficulty）；**d>100 时原生分母已恒为 random(10,30)**，无需修改分母，只缺“高于100部分线性增幅”。
- 初始目标：构造末尾 `bobberTargetPosition=(100-difficulty)/100×548`，d>100 时为负数（无目标/冲向顶部）；`Reposition()` 只调整菜单位置，不重置目标。
- `BobberBar.update(GameTime)` 方法内 `ldfld difficulty` 共 6 处，可按下一条指令唯一区分：ldfld motionType（大目标）、ldc.r4 2000（小偏移）、ldc.r4 1000（dart 概率）、conv.i4+ldc.i4.2（dart 偏移×2）、call Math.Min（加速度分母，不注入）。`stfld bobberAcceleration` 仅 1 处。

## 第一处分歧

- 原生公式在 d>100 时退化（初始目标为负、换目标概率随难度线性暴涨、dart 偏移随难度无限增大），使高难度反而容易捕获；按用户定稿设计修正运动公式并新增强制鱼跳。

## R0：实现内容（允许修改范围）

- `Source/Patches/BobberBarPatches.cs`：InstanceData 新增鱼跳状态；Constructor_Postfix 加 `ref bobberTargetPosition`（d>100 → 顶部 0）；Update_Prefix 加 ref 位置/速度字段并实现鱼跳状态机；新增 Update_Transpiler（5 处 `ldfld difficulty` 后注入 `ldc.r4 150 + Math.Min(float,float)`；`stfld bobberAcceleration` 前注入 `ldarg.0/ldfld difficulty/call GetAccelerationBoost/mul`）；新增 `GetAccelerationBoost`（d>100 → `1+(d-100)/100`，无上限；否则 1）。
- `Source/Utils/DifficultyCalculator.cs`：GetRankKey 89-99 → `rank.creator`、100 → `rank.chaos`；GetNextRankCeiling 神王(67-88)→99、创世神(89-99)→100、混沌(100)→100。
- `Source/Utils/ChallengeDialogueGenerator.cs`：HonorificKeys 增补 creator/chaos。
- `Source/i18n/default.json`、`Source/i18n/zh.json`：新增 `rank.creator`、`rank.chaos`、`hud.starChallenge.honor.creator`、`hud.starChallenge.honor.chaos`（每称号3种尊敬词）。
- 治理：`GAME-DESIGN.md` 已更新（含原生代码对齐小节）；本轮同步 `BUG-LEDGER.md`、`TESTING.md`（新增高难度维度）、本活动卡。

## 冻结 Case/禁止范围

- BATCH-024/025/026/027 未验收项、鱼王豁免、非鱼类 8 级上限、蓄力槽保护、脱杆次数、数量/品质/经验结算、视觉锚点合同、多人按玩家隔离全部冻结；不顺手修改其他文件。

## R1：删除与收敛

- 被替代项：无原生路径被删除；三处换目标概率与 dart 偏移仍在原生 `update` 内计算，Transpiler 只把其中的 `difficulty` 封顶为 `min(d,150)`（原生概率块的唯一写入者仍是原生方法，本 Mod 未新增第二写入者）。
- 旧状态：无新增持久状态；鱼跳计时器是每实例临时状态，随 BobberBar 生命周期由 `ConditionalWeakTable` 释放；难度/星标/加成仍归 `DifficultyManager` 唯一权威。
- 写入者数量：运动参数（位置/速度/目标）唯一写入者=原生 `update`，本 Mod 按设计在 Prefix 边界瞬移/设初始目标（与既有构造边界调整同模型）；鱼跳状态机唯一写入者=本 Mod 实例数据。
- 运行时代码净增长：`BobberBarPatches.cs` 270 → 446 行（含 Transpiler 与状态机），理由：原生逻辑不可改写，必须经 Transpiler/边界注入；无旧代码删除项（纯新增功能批次）。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 主机单人 d≤100 | 运动公式与原生完全一致（min 注入等于原值、增幅=1、初始目标不变） | 任何 >100 行为出现在 ≤100 鱼上 |
| 主机单人 d>100 | 初始目标=顶部；加速度 ×(1+(d-100)/100) | 初始目标为负/无目标 |
| 主机单人 d 150~250/251~350/351~450/451~550/551+ | 鱼跳间隔 8/6/5/4/3 秒；上下 25% 检测；0.5s 延迟瞬移到对侧 25% 随机位置；冷却从跳完成起算 | 间隔不符、跳过 25% 区域、延迟期取消跳 |
| 换目标频率封顶 | d>150 时三处概率与 dart 偏移均按 150 计算 | 概率随难度继续上涨、偏移超过 ±400 |
| 鱼王 | 无 InstanceData，天然豁免（不跳、不修正、不增幅） | 鱼王触发鱼跳或公式修正 |
| 双人同屏/联机 | 每实例独立状态机，难度按玩家隔离（BATCH-027 沿用） | 副屏/客机鱼跳串用主玩家数据 |
| 成功回调 | `pullFishFromWater` 仍收到完整调整后难度（986606 处不注入） | 结算难度被 150 封顶 |
| 存档/换日/标题 | 无新增持久状态，`UnloadData`/清理路径不变 | 残留计时器 |
| 性能 | 鱼跳每帧 O(1)；瞬移日志 ≤每 3 秒一条 | 逐帧日志、无界状态 |
| 已验收相邻回归 | 蓄力槽保护、脱杆次数、数量/品质/经验、图鉴/称号、BATCH-027 玩家隔离 | 上述行为被本批改动 |

## 可观测性决定

- 鱼跳瞬移：每次跳跃一条 Info 日志（实例、难度、源区域→目标位置），频率上限每 3 秒一次，不逐帧输出。
- Transpiler：静态合同可完整证明（当前安装 IL 证据 + 构建后对产物反编译核对注入点），免除运行时诊断。

## Transpiler 注入点核验（2026-08-06，当前安装 DLL IL 证据 `_analysis/bobberbar-il-20260806/Stardew Valley.il`）

- update 内 `ldfld BobberBar::difficulty` 共 8 处，下一条指令核验：
  - 986642（大目标概率，后跟 `ldfld motionType`）→ 注入 min(150)
  - 986795（小偏移概率，后跟 `ldc.r4 2000`）→ 注入 min(150)
  - 986836（dart 概率，后跟 `ldc.r4 1000`）→ 注入 min(150)
  - 986853/986864（dart 偏移×2，后跟 `conv.i4; ldc.i4.2`）→ 注入 min(150)
  - 986606（`pullFishFromWater` 难度参数，后跟 `conv.i4; ldarg.0`）→ 不注入（结算保持完整难度）
  - 986680（num3 幅度，后跟 `ldsfld random`）/ 986767（加速度分母，后跟 `call Math.Min`）→ 不注入
- `stfld bobberAcceleration` 全方法唯一（986772）→ 增幅注入点唯一。
- 构建产物 `_analysis/batch028-final/FishingExpanded.Patches.BobberBarPatches.decompiled.cs` 反编译核对：Transpiler/GetAccelerationBoost/GetJumpInterval/鱼跳状态机均编译进 DLL。

## 自动化验收决定

- 复用 `fish_setlevel <鱼ID> <等级>` + `FISH-CLI-01` 全量 `Saves` 沙箱：等级 100/120/150/200/250/300/400/500/600/2000 各档进入小游戏，观察初始目标、换目标频率、加速度表现、鱼跳间隔/区域/0.5s 延迟/冷却；称号用 `fish_info`/图鉴核对 89-99 创世神、100 混沌、越级限制 88→99。
- 每档独立记录通过/失败/未执行；真实验收由用户执行（游戏运行验收），本轮不启动游戏。

## 构建与部署事实

- Release Rebuild（2026-08-06）：0 警告 0 错误。
- `Source/bin/Release/net6.0/FishingExpanded.dll` SHA-256 `E0DA4DA2...`
- `default.json` `93129959...`；`zh.json` `4129B961...`；`manifest.json` `EFBEE235...`；`FishingExpanded.deps.json` `D2D47D71...`（与安装一致，未替换）。
- 部署（2026-08-06 19:55）：备份 `DeploymentBackups\FishingExpanded-20260806-195542-pre-BATCH024-028`；替换 DLL/manifest/i18n×2 到 `D:\GGGGG\K1515\Mods\FishingExpanded`；目标哈希与源一致核验通过；部署目录无 `config.json`（未涉及）；游戏未启动。

## 委派评估

- 不委派（Luna 当前模型禁止委派；且 Transpiler/所有权裁决属主线程职责）。
