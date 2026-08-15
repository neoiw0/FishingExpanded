# FishingExpanded - Claude/兼容入口

本文件仅用于用户直接在 Claude Desktop 中进行独立的轻量工作。根 `AGENTS.md` 是本项目唯一治理规则；开始任何操作前必须完整读取并遵守，本文不复制第二套规则。用户当轮明确指令优先于这里的默认范围。

## 项目信息

- **项目名称**：FishingExpanded
- **项目类型**：独立 Stardew Valley Mod
- **项目根**：`D:\GGGGG\FishingExpanded`
- **源码路径**：`D:\GGGGG\FishingExpanded\Source`
- **游戏路径**：`D:\GGGGG\K1515`
- **Mod 安装路径**：`D:\GGGGG\K1515\Mods\FishingExpanded`

## Fishing 专属权威文件

- `GAME-DESIGN.md`：用户确认的难度、数量、品质、传奇鱼、非鱼类和视觉规则。
- `BUG-LEDGER.md`：Fishing 批次索引、累计反证、阶段和实测门禁。
- `MAINTENANCE-INDEX.md`：当前维护入口、台账分层、版本事实和接管门禁。
- `Governance\Active\*.md`（当前活动卡）与 `Governance\Archive-ReadOnly\*.md`（历史档案）：对应批次的取证、根因、删除清单和验证记录；先由总账定位后再读取。
- `TESTING-GUIDE.md`：控制台命令和玩家场景步骤；长期回归维度以 `TESTING.md` 为准。
- `CURRENT-WORK-HANDOFF.md`：交接背景材料，默认只读；除非用户明确要求，不维护该文件。

## 项目特有规则

1. 接管 `BobberBar`、`FishingRod`、`Farmer.caughtFish`、`CollectionsPage`、NPC 对话或手持物绘制前，完整记录原生入口、参数/字段、状态更新顺序、成功/失败回调、物品数量/品质提交时机和显示生命周期。
2. 难度、收藏星标和钓鱼等级加成必须通过现有 `DifficultyManager`/`FishDifficultyData` 存档边界管理。不要从旧文档推断存档键名，修改前以当前源码和 Save API 为准。
3. 线性插值的具体参数、封顶/触底边界和测试样例以 `GAME-DESIGN.md` 为准；任何偏离都必须先记录用户确认或新的原生证据。
4. `fishSize`、鱼获堆叠数量和手持绘制缩放是不同状态。修改其中一项时必须验证另外两项不会被重复写入或错误联动。
5. 传奇鱼豁免、非鱼类八级上限、脱杆奖励、鱼图鉴文本、NPC 反应和控制台命令均属于 Fishing 专属合同；不得用 GCE 或其他 Mod 的行为规则覆盖它们。
6. 本 Mod 不依赖战斗系统；其主要风险集中在钓鱼小游戏、鱼获提交、存档数据、Collections UI、HUD、NPC 反应和手持物绘制，不得把其他 Mod 的战斗或家庭状态所有权模型机械套用到本项目。

## 默认工作范围

- 所有面向用户的文字使用中文。
- 默认只处理用户明确限定的文案、翻译和说明文字，并保持其余玩法、代码语义、文件结构和既有修改不变。
- 若任务涉及 Bug 行为、根因裁决、架构或所有权、治理状态、Git 暂存/提交、正式部署、存档或 `config.json`、启动游戏或真实验收，应停止扩大范围并提醒用户交由 Codex 主控。
- 修改前后核对 `git status` 和实际 diff；不得吸收、回退或重写来源不明的既有修改。
- 完成后只报告实际修改文件和已执行的验证，不得把构建、部署或真实验收互相替代。
