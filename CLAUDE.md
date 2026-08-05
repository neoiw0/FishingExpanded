# FishingExpanded - Claude/兼容入口

根 `AGENTS.md` 是本项目唯一核心治理规则。本文件只保存 FishingExpanded 特有的增量约束和路径说明，不复制第二套通用规则；用户当轮明确指令优先于这里的默认范围。

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
- `Governance\*.md`：对应批次的取证、根因、删除清单和验证记录；先由总账定位后再读取。
- `TESTING-GUIDE.md`：控制台命令和玩家场景步骤；长期回归维度以 `TESTING.md` 为准。
- `CURRENT-WORK-HANDOFF.md`：交接背景材料，默认只读；除非用户明确要求，不维护该文件。

## 项目特有规则

1. 接管 `BobberBar`、`FishingRod`、`Farmer.caughtFish`、`CollectionsPage`、NPC 对话或手持物绘制前，完整记录原生入口、参数/字段、状态更新顺序、成功/失败回调、物品数量/品质提交时机和显示生命周期。
2. 难度、收藏星标和钓鱼等级加成必须通过现有 `DifficultyManager`/`FishDifficultyData` 存档边界管理。不要从旧文档推断存档键名，修改前以当前源码和 Save API 为准。
3. 线性插值的具体参数、封顶/触底边界和测试样例以 `GAME-DESIGN.md` 为准；任何偏离都必须先记录用户确认或新的原生证据。
4. `fishSize`、鱼获堆叠数量和手持绘制缩放是不同状态。修改其中一项时必须验证另外两项不会被重复写入或错误联动。
5. 传奇鱼豁免、非鱼类八级上限、脱杆奖励、鱼图鉴文本、NPC 反应和控制台命令均属于 Fishing 专属合同；不得用 GCE 或其他 Mod 的行为规则覆盖它们。
6. 本 Mod 不依赖战斗系统；其主要风险集中在钓鱼小游戏、鱼获提交、存档数据、Collections UI、HUD、NPC 反应和手持物绘制，不得把其他 Mod 的战斗或家庭状态所有权模型机械套用到本项目。

## 默认边界

- 面向用户的说明默认使用中文；代码、SMAPI 原生日志和 API 名称保持原文。
- 构建成功只证明静态产物可生成；部署成功只证明文件替换完成；两者都不能代替玩家存档实测。
- 不自动启动游戏，不修改受保护安装，不覆盖部署目录的 `config.json`，不把旧 Claude 路径或旧交接时间线当作当前环境事实。
