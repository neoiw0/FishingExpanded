# FishingExpanded 根因批次：BATCH-073 英文单位·动物叫声·60秒布丁·金冠助战·自定义称号

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260818-01` |
| 当前活动类别 | CAT-073 |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡 |
| 本轮用户问题范围 | 用户 2026-08-18 一次提交 5 项确认修改：①英文模式所有 cm 改 in.，NPC 冒泡/对话都要；②英文模式动物叫声使用英文拟声词且保持多种随机，只修头顶冒泡；③战斗 ≥60 秒海泡布丁概率 60%（40% 无奖励，不叠加 30 秒档）；④流动金色皇冠鱼：总助战概率每条 +0.1%，增大（100 级 1.2 倍）流动金冠鱼再 +0.05%，且触发后金冠鱼更容易被选中；⑤钓鱼称号允许玩家手动改 config.json 自定义，隐藏于 GMCM，空值继续用 i18n 称号 |
| 本轮纳入 Case | FE-073-1 英文英寸、FE-073-2 英文动物叫声、FE-073-3 60秒布丁 60%、FE-073-4 金冠助战加成与加权选鱼、FE-073-5 自定义钓鱼称号 |
| 共享第一处分歧与所有权链证据 | 各项独立：展示文案/动物叫声=GiantFishManager/NPCDialogueGenerator；持久战奖励=BobberBarPatches；助战概率与选鱼=DifficultyCalculator+BobberBarPatches+DifficultyManager；自定义称号=ModConfig+各 rankName 展示点。不共享第一分歧，但同批实现一次构建 |
| 排队或暂不纳入 Case | WIKI.md、PLAYER-WIKI.md 默认只读；除非用户明确要求不维护 |
| 合并/拆分决定及依据 | 5 项均为用户明确新功能/改动，独立验收；同批统一构建部署 |
| 本轮准入证据 | 用户逐条确认：1 按建议；2 按建议且叫声要多样、只修冒泡；3 按建议；4 选 B 且触发后金冠鱼也更容易被选中、不设 100% 上限、增大=Level100FlowCrowns；5 自定义称号代替现有称号、仅手动改 config.json、空值用 i18n |
| 现有实现复核 | 见各 Case 下方；已核对 NPCDialogueGenerator、GiantFishManager、BobberBarPatches、DifficultyCalculator、DifficultyManager、ModConfig、HUDNotifier、ChallengeDialogueGenerator、CollectionsPagePatches |
| 允许修改范围 | Source：ModConfig.cs、ModEntry.cs、NPCDialogueGenerator.cs、GiantFishManager.cs、BobberBarPatches.cs、DifficultyCalculator.cs、DifficultyManager.cs、HUDNotifier.cs、ChallengeDialogueGenerator.cs、CollectionsPagePatches.cs、i18n/default.json、i18n/zh.json；文档：GAME-DESIGN.md、BUG-LEDGER.md、TESTING.md、TESTING-GUIDE.md、本卡 |
| 冻结 Case/禁止范围 | 不改 WIKI.md/PLAYER-WIKI.md；不覆盖部署 config.json；不启动游戏除非用户授权部署；不改动已有治理规则 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 一次性完成 5 项源码+i18n+文档修改并通过 Release 构建 |
| 可观测性决定 | FE-073-1/2/5 纯展示/文案修改免诊断（静态合同可证）；FE-073-3/4 复用现有一次性日志（持久战奖励日志、助战触发日志），不新增逐帧/高频诊断；fish_selftest 增加只读断言 |
| 自动化验收决定 | fish_selftest 增加：60 秒布丁概率数学、助战概率含金冠加成、自定义称号回退；存档影响=无新增存档字段（仅 config.json 新字段默认空）；触发入口=SMAPI 控制台命令；成功判据=断言通过；失败判据=断言失败；关闭条件=验收完成或用户撤销 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 部分修改（BATCH-060~072 未提交 + 本轮） |
| 本轮统一构建 | DLL 34929524F2EA5458FD7762C29157FECB7F9BE4074FC91DE3BAE26560E62981F1（0 警告 0 错误） |
| 唯一下一步 | 集中测试（fish_selftest + 实测英文/称号/布丁/助战） |
| 已消费动作 | `Deploy:ROUND-20260818-01:34929524F2EA5458FD7762C29157FECB7F9BE4074FC91DE3BAE26560E62981F1` |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 未委派；主线程直接实施（涉及多文件交互，交由主控保持一致） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-073 | FE-073-1~5 | 代码侧结束 | 功能实现 | 无新增高频日志 | fish_selftest 只读断言 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-073-1 英文英寸 | 已构建 | R0 完成 |
| FE-073-2 英文动物叫声 | 已构建 | R0 完成 |
| FE-073-3 60秒布丁 60% | 已构建 | R0 完成 |
| FE-073-4 金冠助战加成与加权选鱼 | 已构建 | R0 完成 |
| FE-073-5 自定义钓鱼称号 | 已构建 | R0 完成 |

> 机制断言：这 5 项全部属于用户确认的新功能/文本修改，非 Bug 根因，竞争解释表按功能设计记录，不适用根因假设流程；运行时行为修改集中在持久战概率与助战概率/选鱼，其余为纯展示文案。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-073 / FE-073-3、FE-073-4 | fish_selftest | 通过 / 失败 / 未执行 | 60s 布丁 60%、助战概率公式 |
| 2 | CAT-073 / FE-073-1~2、FE-073-5 | 游戏内英文/中文实测 | 冒泡/对话单位、动物叫声、自定义称号显示 | 通过 / 失败 / 未执行 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 34929524F2EA5458FD7762C29157FECB7F9BE4074FC91DE3BAE26560E62981F1（2026-08-18 15:15 部署；备份 DeploymentBackups\\FishingExpanded-20260818-151519-pre-BATCH073） | |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|---|
| FE-073-1 | 英文 NPC 冒泡/对话显示 cm | 0 | 无 | 展示 | 已确认 | 英文下看 NPC 冒泡/对话为 in. |
| FE-073-2 | 英文动物叫声不显示 | 0 | 无 | 展示 | 已确认 | 英文下狗/猫/马冒泡有英文叫声 |
| FE-073-3 | ≥60 秒布丁 100% | 0 | 无 | 奖励 | 已确认 | fish_selftest 断言 |
| FE-073-4 | 金冠鱼无助战加成 | 0 | 无 | 助战 | 已确认 | fish_selftest 断言 |
| FE-073-5 | 称号不可自定义 | 0 | 无 | 配置/展示 | 已确认 | config.json 设值后游戏内称号改变 |

反证累计 ≥2 的 Case 停止行为补丁，先完成诊断取证、所有权审计和方案设计（高级复核门禁）并绑定新证据后再实施；第三次标记 `architecture-review`。

## 设计要点

### FE-073-1 英文英寸
- `NPCDialogueGenerator.GenerateFishPraise` 按 `LocalizedContentManager.CurrentLanguageCode == LanguageCode.en` 分流：英文传 `fishSize`，其他语言传 `fishSizeCm`。
- `default.json` 的 `npc.praise.templates` 全部 `cm` 改为 `in.`；`zh.json` 保持厘米。
- 冒泡与主动对话共用该生成器，因此两处同时生效。

### FE-073-2 英文动物叫声
- `GiantFishManager.GetAnimalSound` 改为按当前语言返回对应叫声池：
  - 英文：狗 `Woof!|Woof woof!|Arf!|Ruff!|Yip!|Bow-wow!`，猫 `Meow!|Mrow!|Meow meow!|Mew!|Mrrow!`，马 `Neigh!|Whinny!|Nicker!|Snort!|Neigh-heigh!`。
  - 中文：保留现有 5 套中文叫声。
- 只修头顶冒泡；动物没有主动对话。
- 英文冒泡格式使用 `!!!`，中文保留 `！！！`。

### FE-073-3 60 秒布丁 60%
- `TryGrantPerseveranceReward`：`elapsed >= 60f` 时，以 60% 概率发海泡布丁；未命中不发任何持久战奖励。
- 不再进入 30 秒档。
- 保持 `fish_persisttest <30|60>` 测试命令可强制验证。

### FE-073-4 金冠助战加成与加权选鱼
- 总助战概率 = 基础 `10% × 可计数皇冠数/61` + `0.1% × 可计数流动金冠鱼数` + `0.05% × 可计数增大流动金冠鱼数`。
- 流动金冠鱼 = `ChallengeCrowns`；增大流动金冠鱼 = `Level100FlowCrowns`（同时属于流动金冠，因此合计 +0.15%）。
- 不设 100% 上限；由 61 条上限自然限制。
- 触发后选鱼改为加权随机：普通皇冠鱼权重 1，流动金冠鱼权重额外 +0.1%，增大流动金冠鱼权重再额外 +0.05%（即普通 1 / 流动 1.001 / 增大 1.0015，归一化后选择）。
- `fish_bonus` 与 `fish_selftest` 同步更新展示/断言。

### FE-073-5 自定义钓鱼称号
- `ModConfig` 新增 `CustomFishingTitle`，默认空字符串；不注册到 GMCM。
- 新增集中显示称号方法：空/纯空白时返回 `Translation.Get(rankKey)`，否则返回自定义称号。
- 替换所有显示 rankName 的位置：HUD 成功提示、挑战宣言、图鉴挑战等级、助战/持久战奖励/巅峰/力竭提示、控制台日志。
- 内部等级、越级限制、尊敬词仍按原 rankKey 逻辑，只替换可见称号文本。

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 自定义称号 | config.json | SMAPI/ModConfig | 各展示点 | 单值 | 无 | 无 |
| 助战概率/选鱼 | 派生自 CollectionStars/ChallengeCrowns/Level100FlowCrowns | 只读 | BobberBarPatches | 每次构造一次 | 无 | 无 |
| 持久战奖励 | BobberBarPatches 失败单发 | BobberBarPatches | 原生溢出菜单/HUD | 每次失败最多一次 | 无 | 无 |

## 构建、部署与集中测试

- 构建结果：
- 版本/哈希：
- 部署文件与目标：
- 本次单局路线：
- 日志/截图/存档证据：
- 每个 Case 的实际结果：

## 收尾与归档

- 已完成：
- 当前不确定性：
- 下一条准确操作：
- 总账与测试路线是否已覆盖更新：
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：