# FishingExpanded 维护台账索引

**整理时间**：2026-08-05  
**维护目的**：把 Claude Code 生成的设计、批次、报告和交接材料分层，明确后续维护时哪些文件具有当前权威。

## 当前权威顺序

1. `AGENTS.md`：通用治理、证据门禁、唯一所有者和 Git 边界。
2. `GAME-DESIGN.md`：用户确认的最终玩法规则。
3. `BUG-LEDGER.md`：当前批次状态、累计反证、构建/部署/实测门禁。
4. `MAINTENANCE-INDEX.md`：本文件，负责维护入口和台账分层。
5. `TESTING.md` / `TESTING-GUIDE.md`：长期回归矩阵和玩家测试步骤。
6. `Governance\*.md`、`RuntimeEvidence`、`_analysis`：批次证据、运行证据和原生契约证据。

## 当前接管状态

- Fishing 代码、治理和台账已建立独立 Git 根；兄弟 `codex working space` 只提供通用治理参考，不是本项目基线。
- 用户最终确认的四条规则已经写入 `GAME-DESIGN.md`：0级全局蓄力保护、原生 `ItemGrabMenu` 溢出、完美收益先 `+10` 再限制区间、所有蓄力减速冲突取最强效果。
- 现有 v0.5.2 记录显示已构建并部署，但九项玩家验收仍未完成；构建/部署不替代真实存档测试。
- 当前版本事实仍需后续单独整理：`Source\manifest.json` 为 `0.5.10`，`FishingExpanded.csproj` 为 `0.1.0`，历史总账和交接记录为 `v0.5.2`。在版本事实统一前不得据此生成正式发布包。

## 文件分层

| 层级 | 文件/目录 | 维护规则 |
|---|---|---|
| 设计权威 | `GAME-DESIGN.md` | 只写最终玩法，不写逐批次完成状态 |
| 当前总账 | `BUG-LEDGER.md` | 新 Case、阶段、构建、部署和实测变化必须同步 |
| 接管入口 | `CURRENT-WORK-HANDOFF.md` | 只写当前维护状态、门禁和下一步，不重复历史报告 |
| 维护索引 | `MAINTENANCE-INDEX.md` | 维护本分层和未决事实 |
| 活动/证据 | `Governance\*.md` | 保留原始证据；旧状态文字不覆盖总账当前状态 |
| 历史报告 | `BUG-FIX-REPORT-*.md`、`IMPLEMENTATION-SUMMARY.md`、`INDIRECT-BUG-ANALYSIS.md`、`PERFORMANCE-OPTIMIZATION-*.md`、`TRANSPILER-DEBUG.md`、`COMMAND-DEBUG-GUIDE.md` | 只读参考，不作为当前完成证明 |
| 运行/原生证据 | `RuntimeEvidence`、`_analysis` | 保存日志和反编译证据；重新修改前核对当前安装 DLL |

## 当前验收门禁

以下事项仍必须独立标记通过、失败或未执行：

- 高难度鱼手持视觉放大，包括倍数 27 的约 3 倍缩放。
- Mod 倍数不增加钓鱼动画数量；原生特殊鱼饵多鱼机制仍保留；最终数量在背包或原生 `ItemGrabMenu` 溢出路径处理。
- 0/1/2/3+ 次脱杆分别获得原始收益 `+10/+5/+2/+1`，并且最终等级不跨称号区间。
- 0级、负数、正数和边界等级的蓄力槽保护；多个减速规则取最小倍率。
- 100级封顶、-10级提示绝对值、鱼王五种鱼的完整难度/奖励/蓄力保护豁免、非鱼类8级上限、图鉴描述增强和存档重载。

没有新的 SMAPI 日志或玩家实测前，不把上述项目标记为真实验收完成。
