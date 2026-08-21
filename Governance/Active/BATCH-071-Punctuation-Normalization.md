# FishingExpanded 活动卡：BATCH-071 文案标点归一化

> 纯文案/显示字符串修改，不改变玩法、存档、config.json 或运行时行为。按根 `AGENTS.md` 记录免除理由。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260816-04` |
| 当前活动类别 | CAT-01（文案标点归一化） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-071） |
| 本轮用户问题范围 | 用户要求：所有文案中的括号改为英文括号；英文文案中的全角标点改为对应英文符号 |
| 本轮纳入 Case | FE-071-1 全角括号 `（）` 改为半角 `()`（所有面向用户文案）；FE-071-2 英文文案中的宽破折号 `—`/`\u2014` 改为英文连字符 `-` |
| 共享第一处分歧与所有权链证据 | 不适用（纯文案，无运行时根因；唯一所有者=各显示/翻译字符串所在文件） |
| 排队或暂不纳入 Case | BATCH-069/BATCH-070 已部署待集中测试；本轮不触碰其运行逻辑，仅与统一候选一起部署 |
| 合并/拆分决定及依据 | 同属“文案标点”单一显示类别，合并实施；按中/英文案分别验收 |
| 本轮准入证据 | 用户明确指令“所有文案中的括号改为英文的括号”“其他的全角的也发给我看一下，我来判定”“英文里面全角都改为对应的英文符号” |
| 现有实现复核 | 已扫描 `Source/i18n/*.json` 与源码中用户可见字符串；全角括号位于 zh.json 与若干 C# 字符串；英文宽破折号位于 default.json（含 `\u2014`）与 VanillaTips 英文文案 |
| 允许修改范围 | `Source/i18n/zh.json`、`Source/i18n/default.json`、`Source/ModEntry.cs`、`Source/Patches/BobberBarPatches.cs`、`Source/Patches/FishingRodPatches.cs`、`Source/Services/GiantFishManager.cs`、`Source/Services/HUDNotifier.cs`、`Source/Services/VanillaTipsIntegration.cs`、`BUG-LEDGER.md`、本卡 |
| 冻结 Case/禁止范围 | 其余机制全部冻结；不修改玩法/存档/config.json、不启动游戏、不触碰 VanillaTips 模组文件 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 中文文案全角括号 `（）` → `()`；英文文案宽破折号 `—`/`\u2014` → `-`（按上下文保留空格） |
| 可观测性决定 | 免除（纯文案/显示字符串；无运行时行为变化，不新增诊断） |
| 自动化验收决定 | 免除（纯文案；构建通过 + JSON 解析 + 源/目标哈希一致即可；不进入运行验收） |
| 当前类别阶段 | 代码侧结束（纯文本修改，已完成扫描与替换） |
| 当前工作树 | 部分修改（含 BATCH-069/BATCH-070 已部署待验收源码 + 本批文案修改） |
| 本轮统一构建 | `2367A9AE01AFC54E64A554523F8A6BF5BC756727BDAC2E6EFFFC12E61A4F4BA6`（Release Rebuild，0 警告 0 错误） |
| 唯一下一步 | 用户启动游戏查看中英文案标点显示；BATCH-069/BATCH-070 与 BATCH-071 一并验收 |
| 已消费动作 | `Build:ROUND-20260816-04:2367A9AE01AFC54E64A554523F8A6BF5BC756727BDAC2E6EFFFC12E61A4F4BA6`；`Deploy:ROUND-20260816-04:2367A9AE01AFC54E64A554523F8A6BF5BC756727BDAC2E6EFFFC12E61A4F4BA6` |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单线程纯文案修改，主线程已掌握全部改动；`NotBeneficial`，理由=改动集中且需与既有 BATCH-069/070 统一核验，不拆分） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-071-1、FE-071-2 | 代码侧结束 | 纯文案替换 | 无新增 | 无（显示文案目视即可） |
| CAT-02 | BATCH-069 / BATCH-070（既有） | 已部署待集中测试 | 根因修复 | 复用既有日志/UI | SMAPI 启动日志 + 游戏内目视 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-071-1 | 代码侧结束 | 统一构建/部署 |
| FE-071-2 | 代码侧结束 | 统一构建/部署 |

> 机制断言：只改变显示文本字符，不改变任何游戏机制、存档、config、多人状态或绘制逻辑。

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `2367A9AE01AFC54E64A554523F8A6BF5BC756727BDAC2E6EFFFC12E61A4F4BA6`（已部署，2026-08-17 13:40；备份 `DeploymentBackups\FishingExpanded-20260817-134030-pre-BATCH071`；源/目标哈希一致；config.json 未触碰） |
| 部署前安装 | `88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`（BATCH-070 修复版） |
| 部署文件与目标 | `D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`；`i18n\default.json`；`i18n\zh.json` |
| config.json | 不触碰 |
<!-- CURRENT-STATE-END -->

## 改动摘要

- `Source/i18n/zh.json`：全角括号 `（）` → 半角 `()`。
- `Source/i18n/default.json`：所有 `—` / `\u2014` → `-`（按上下文使用 ` - `，与现有英文文案风格一致）。
- `Source/ModEntry.cs`、`Source/Patches/BobberBarPatches.cs`、`Source/Patches/FishingRodPatches.cs`、`Source/Services/GiantFishManager.cs`、`Source/Services/HUDNotifier.cs`、`Source/Services/VanillaTipsIntegration.cs`：C# 中用户可见字符串的全角括号 `（）` → `()`；VanillaTips 英文文案的 `—` → `-`。
- 排除项：代码注释、XML 文档、中文文案中的其他全角标点（`，。！？：；、“”…【】～` 等）尚未转换，待用户另行判定。