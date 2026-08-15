# FishingExpanded 根因批次：BATCH-040 日志风暴治理与日志开关

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260811-01 |
| 当前活动类别 | CAT-01（日志治理） |
| 整合候选状态 | 已统一构建 + 已部署（DLL 70B9092E...，含 BATCH-039 + CAT-01 + FE-040-4 + FE-040-5；Deploy:ROUND-20260811-01:70B9092E... 已消费）；真实验收待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-040）；BATCH-039 冻结排队 |
| 本轮用户问题范围 | ① 排查日志风暴问题，除非必要不允许每帧日志；② config（config.json + GMCM）提供关闭日志的选项；③ 允许有意为之的有限每帧输出（如限频兜底），禁止疏忽性每帧日志；④ 完成后直接部署 |
| 本轮纳入 Case | FE-040-1 蓄力槽保护 Debug 每帧刷屏；FE-040-2 每帧路径异常兜底无限频；FE-040-3 日志主开关（config.json + GMCM）；FE-040-4 分屏绘制验收诊断每帧刷屏（退休整条 DIAG-FISH-VISUAL）；FE-040-5 GMCM 接口签名与安装版 1.16.0 不兼容（Register 缺 titleScreenOnly） |
| 共享第一处分歧与所有权链证据 | 三者共享同一所有权链：日志写入者=Mod 自身 `Monitor` 直呼；第一处分歧=`BobberBarPatches.Update_Prefix` 在蓄力槽保护生效期间每帧计算新倍率，`|old-combined|>0.01f` 随蓄力进度变化几乎每帧成立，Debug 日志每帧输出；每帧路径 catch 兜底同样逐帧 Log |
| 排队或暂不纳入 Case | BATCH-039（代码侧结束，Release Rebuild 61076076 已通过，待真实游戏验收）冻结排队，与 CAT-01 统一构建部署 |
| 合并/拆分决定及依据 | 合并为一个日志治理类别：全部修改处于同一日志写入者与同一轮用户要求；FE-040-5 为 FE-040-3 已部署修复的首次反证（GMCM 注册运行时失败），重开 Case 并同轮修复，FE-040-3 反证计数 0→1 |
| 本轮准入证据 | 2026-08-11 用户指令（日志风暴排查+日志开关+直接部署）；源码 185 处 `.Log(` 全量盘点；安装 DLL `E7F8E8D3...`（BATCH-038 部署版）反编译核对确认 `Update_Prefix` 蓄力槽保护 Debug 与源码一致；SMAPI-latest.txt（2026-08-11 09:06）确认当前会话 FishingExpanded 自身前缀条数极少、风暴前缀 `[DIAG-RUNTIME SNAPSHOT]` 归属 GCE（LEISURE-09，每 10 游戏分钟）；SMAPI-latest.txt（2026-08-11 09:57:41，33857 行）实测 12976 条 `[DIAG-FISH-VISUAL]`（Game1.drawPlayerHeldObject 6486 + Object.drawWhenHeld 6474，09:46-09:48 分屏窗口）——分屏 screen_0/screen_1 交替绘制同一玩家，诊断 state 含 isLocal=True/False 每帧翻转，状态去重（stage|identity）失效 → 每帧 2 条；GetDrawScale 仅 15 条（state 无 isLocal）印证；该诊断为 BATCH-024/026 验收诊断，验收早已完成（BATCH-038 已部署验收），应退休；SMAPI-latest.txt（2026-08-11 16:24:01）部署后实测报错 `Tried to map a mod-provided API to interface ...IGenericModConfigMenuApi, which is not compatible with the actual mod API`——技术细节指明 `Register` 无法映射；安装 GMCM 1.16.0 反编译契约确认真实 `Register(IManifest, Action, Action, bool titleScreenOnly)`、`AddBoolOption(..., string fieldId)`，本 Mod 接口签名缺参（FE-040-5） |
| 现有实现复核 | 唯一日志写入者=SMAPI `Monitor`；185 处调用分布：ModEntry 84、DifficultyManager 25、FishingRodPatches 22、BobberBarPatches 21、HUDNotifier 13、ObjectPatches 7、GiantFishManager 5、其余 8；22 处 Debug、99 处 Info、46 处 Error、19 处 Warn；无 `Trace`；`ObjectPatches.LogVisualDiagnostic` 曾以状态变化门控防每帧，但分屏 isLocal 翻转使去重失效（FE-040-4 已退休整条诊断） |
| 允许修改范围 | `Source\ModConfig.cs`（新）、`Source\Services\FishingLog.cs`（新）、`Source\Services\IGenericModConfigMenuApi.cs`（新）、`Source\ModEntry.cs`、`Source\Patches\BobberBarPatches.cs`、`FishingRodPatches.cs`、`ObjectPatches.cs`、`Game1Patches.cs`、`CollectionsPagePatches.cs`、`FarmerFishingLevelPatches.cs`、`NPCPatches.cs`、`Services\DifficultyManager.cs`、`HUDNotifier.cs`、`GiantFishManager.cs`、`Utils\ChallengeDialogueGenerator.cs`、`NPCDialogueGenerator.cs`、`i18n\default.json`、`i18n\zh.json`；文档：BUG-LEDGER.md、TESTING.md、本卡 |
| 冻结 Case/禁止范围 | 难度/星标/经验/品质/数量/尺寸结算所有权；Transpiler 注入点与 IL 栈序；鱼王豁免边界；HUD/图鉴/NPC 展示事实；部署目录 config.json（当前不存在，不创建不覆盖）；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 消除疏忽性每帧日志；每帧路径兜底统一限频（有意为之的有限输出）；新增 config.EnableLogging 主开关（config.json 自动读写 + GMCM 可选菜单）；退休 `[DIAG-FISH-VISUAL]` 整条验收诊断（Game1Patches 类、LogVisualDiagnostic/状态缓存、3 处调用点、2 处清理调用），保留全部生产视觉缩放逻辑；GMCM 接口签名对齐安装版 1.16.0（Register 补 titleScreenOnly、AddBoolOption 补 fieldId） |
| 可观测性决定 | 蓄力槽保护改为“生效/解除”状态转换单发（每段保护周期 ≤2 条 Debug）；8 处每帧/高频路径 catch 兜底改 `FishingLog.LogRateLimited`（同键 30 秒 ≤1 条，上限 64 键超限清空）；其余事件驱动日志保持单发；全部日志经 `FishingLog.Log` 受 `EnableLogging` 门控；`[DIAG-FISH-VISUAL]` 诊断整条删除（验收已完成，非必要输出）；ObjectPatches catch 前缀改中性 `[ObjectPatches]` 并保持限频 |
| 自动化验收决定 | 复用 `fish_selftest` 增加只读自测项（日志门与 config 一致、限频缓存有界）；存档影响=无（日志不写存档/玩家状态）；真实钓鱼小游戏为生产入口，集中测试阶段核对保护段日志 ≤2 条、无逐帧刷屏；`EnableLogging=false` 时 0 条 Mod 日志；GMCM 菜单出现日志开关且切换即时生效（FE-040-5 验收项） |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成，Release Rebuild ✅）；已统一部署（含 FE-040-5 纠错重部署） |
| 当前工作树 | 完整修改（BATCH-039 未提交 + CAT-01 未提交；HEAD `c3bc3b4`；新文件 ModConfig/FishingLog/IGenericModConfigMenuApi） |
| 本轮统一构建 | Release Rebuild ✅ 0 警告 0 错误；DLL SHA256 `70B9092E85FEA225F49A890AAB5FFAB1D05ED61577568BC38808F63D84958004`（含 BATCH-039 + CAT-01 + FE-040-4 退休 + FE-040-5 接口修复；反编译确认 IGenericModConfigMenuApi 已含 titleScreenOnly/fieldId，Game1Patches/LogVisualDiagnostic 已不存在）；前候选 `B0A517FD...` 备份于 DeploymentBackups\FishingExpanded-20260811-162854-pre-GMCM-fix |
| 唯一下一步 | 真实游戏集中测试（小游戏保护段 ≤2 条、每帧兜底 ≤1 条/30 秒、分屏手持绘制 0 条 [DIAG-FISH-VISUAL]、GMCM 关闭后 0 条、config.json 默认开启） |
| 已消费动作 | `Build:ROUND-20260811-01:31BAF74970B232260C18B799D87FA55DB618989F910CBDC58F63C6B8B203569B`（前候选，含治理修改）；`Build:ROUND-20260811-01:B0A517FD5E78B117EC9DC6EE56C761293E668FD43F79BAA4FDC68697253DE1AD`（终候选，含 FE-040-4 退休）；`Deploy:ROUND-20260811-01:B0A517FD5E78B117EC9DC6EE56C761293E668FD43F79BAA4FDC68697253DE1AD`；`Build:ROUND-20260811-01:70B9092E85FEA225F49A890AAB5FFAB1D05ED61577568BC38808F63D84958004`（FE-040-5 修复）；`Deploy:ROUND-20260811-01:70B9092E85FEA225F49A890AAB5FFAB1D05ED61577568BC38808F63D84958004` |
| 重复执行授权 | 用户报告部署后 GMCM 报错并要求修复；原因=IGenericModConfigMenuApi 接口签名与安装 GMCM 1.16.0 不匹配（Register 缺 titleScreenOnly），同轮重建重部署 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-040-1/2/3/4 | 代码侧结束 | 根因修复 | `FishingLog.LogRateLimited`（30s/键、64 键上限）；蓄力槽保护生效/解除单发 | `fish_selftest`（日志门/限频缓存只读项）；GMCM 开关；config.json `EnableLogging` |
| CAT-02 | BATCH-039 FE-039-1~4 | 代码侧结束（冻结排队） | 等待真实游戏验收 | 已有 BATCH-039 日志（每实例/每失败 ≤1 条） | `fish_persisttest <30\|60>`、`fish_selftest` |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-040-1 蓄力槽保护每帧 Debug 刷屏 | R2完成 | 真实游戏验收 |
| FE-040-2 每帧路径兜底无限频 | R2完成 | 真实游戏验收 |
| FE-040-3 日志主开关（config.json+GMCM） | R2完成 | 真实游戏验收（反证 1：GMCM 接口映射，已修复） |
| FE-040-4 分屏绘制验收诊断每帧刷屏 | R2完成 | 真实游戏验收（分屏 0 条 DIAG-FISH-VISUAL） |
| FE-040-5 GMCM 接口签名不兼容 | R2完成 | 启动无 ERROR；GMCM 菜单出现日志开关 |

> 机制断言：日志只读，不改变任何游戏行为；开关与限频均不触碰存档/玩家/原生状态，可静态证明。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01/FE-040-1 | 真实钓鱼小游戏触发蓄力槽保护（20%/1%） | 保护段日志 ≤2 条（生效/解除），无逐帧刷屏 | 日志只读 |
| 2 | CAT-01/FE-040-2 | 真实钓鱼小游戏 + 手持鱼绘制 | 异常兜底同键 30 秒 ≤1 条 | 日志只读 |
| 3 | CAT-01/FE-040-3 | GMCM 菜单关闭日志 → 钓鱼与命令 | 除命令回显外 Mod 日志 0 条；config.json 生成且默认 true | 日志只读 |
| 4 | CAT-01/FE-040-3 | `fish_selftest` | 日志门一致 + 限频缓存 ≤64 | 日志只读 |
| 5 | CAT-01/FE-040-4 | 分屏窗口开启手持鱼绘制 | 日志 0 条 [DIAG-FISH-VISUAL] | 日志只读 |
| 6 | CAT-02 | BATCH-039 验收路线（fish_persisttest 等） | 按 FE-039-1~4 各自判据 | 已冻结 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `70B9092E85FEA225F49A890AAB5FFAB1D05ED61577568BC38808F63D84958004`（2026-08-11 16:29 部署，目标文件哈希已复核一致；前候选 B0A517FD... 已备份） |
<!-- CURRENT-STATE-END -->
## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-040-1 | 钓鱼小游戏蓄力槽保护期间 SMAPI 日志每帧刷屏 `[BobberBar] 蓄力槽保护触发` | 0 | 无 | RC-040 | R2完成 | 一局小游戏内保护段日志 ≤2 条 |
| FE-040-2 | 每帧路径（update/draw/手持绘制）异常时 Error/Warn 每帧刷屏 | 0 | 无 | RC-040 | R2完成 | 同键 30 秒 ≤1 条 |
| FE-040-3 | 玩家无法关闭 Mod 日志 | 1 | 2026-08-11 16:24 部署后 SMAPI 报 GMCM API 映射错误（FE-040-5，同轮已修复重部署） | RC-040 | R2完成 | GMCM 关闭后 0 条 Mod 日志 |
| FE-040-4 | 分屏双窗口手持鱼绘制时 SMAPI 日志 [DIAG-FISH-VISUAL] 每帧刷屏（实测 12976 条/09:46-09:48） | 0 | 无 | RC-040B | R2完成 | 分屏绘制 0 条 [DIAG-FISH-VISUAL] |
| FE-040-5 | 部署后 GMCM 菜单注册失败（SMAPI 报 IGenericModConfigMenuApi 映射错误） | 1 | 2026-08-11 16:24 实测日志（首次部署后） | RC-040C | R2完成 | 启动无 ERROR；GMCM 菜单出现日志开关 |

## RC-040 根因卡

- 根因状态：已证实（源码 + 安装 DLL 反编译 + 运行日志归属核对）
- 第一处分歧：`BobberBarPatches.Update_Prefix` 在蓄力槽保护生效期间每帧计算新倍率，`Math.Abs(oldModifier - combinedModifier) > 0.01f` 随蓄力进度变化几乎每帧成立 → Debug 日志每帧输出；注释声称“避免每帧输出”但判定条件未按状态转换设计，属于疏忽性每帧日志。
- 决定性证据：
  1. 源码 `BobberBarPatches.cs` 391~398 行：保护倍率随 `___distanceFromCatching` 每帧变化，差值判定每帧命中；
  2. 安装 DLL `E7F8E8D3...`（BATCH-038 部署版，2026-08-10 21:10 部署验收）反编译确认同一日志点（Update_Prefix 内 Debug）；
  3. `SMAPI-latest.txt`（2026-08-11 09:06）FishingExpanded 自身前缀仅数条（会话未钓鱼），风暴前缀 `[DIAG-RUNTIME SNAPSHOT]`/`[DIAG-TIMEFLOW-02]` 归属 GCE（LEISURE-09/TIMEFLOW-02 案例），每 10 游戏分钟一次，非每帧。
- 竞争解释表（至少 2 个互斥假设；外部假设来自其他 Mod 与安装漂移）：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态（仍成立/已排除） |
|---|---|---|---|
| H1: FishingExpanded 自身每帧日志（蓄力槽保护 Debug） | 保护生效期间每帧输出 `[BobberBar] 蓄力槽保护触发` | 源码+安装 DLL 反编译均确认该 Log 位于 `BobberBar.update` Prefix 每帧路径且判定随距离变化 | 已证实（风暴源） |
| H2: 风暴来自其他 Mod（如 GCE 运行时快照） | 高频前缀与 FishingExpanded 无归属关系 | SMAPI 日志前缀 `[DIAG-RUNTIME SNAPSHOT] case=LEISURE-09` 属 GCE 案例；时间戳 840/830/820... 为每 10 游戏分钟，非每帧；FishingExpanded 前缀条数极少 | 已排除（归属 GCE，频率非每帧；不属本 Mod 修复范围） |
| H3: 安装 DLL 与源码漂移（安装版含源码没有的每帧日志） | 安装 DLL 反编译出现源码不存在的 Log 点 | 安装 DLL `E7F8E8D3...` 反编译日志点与源码一致（蓄力槽保护 Debug 两者均存在） | 已排除（无漂移，风暴源在源码中） |

- 最后已知正常版本/行为：BATCH-038 前各版本同样存在该每帧日志点（无“正常”日志行为基准）；本次以治理目标为准。
- 过去失败方案及为何失败：无（首轮治理）。
- 唯一所有者：Mod 自身 SMAPI `Monitor`（新权威入口 `FishingLog`）。
- 最短区分动作：已完成（源码盘点 + 安装 DLL 反编译 + 日志前缀归属核对）。
- 诊断标签/触发窗口：`[BobberBar] 蓄力槽保护生效/解除`（状态转换单发）；`FishingLog.LogRateLimited` 键见 R0。
- 诊断是否只读且不改变行为：是（日志路径纯只读；开关只影响输出）。
- 诊断构建/部署与哈希：`70B9092E...`（含治理修改 + FE-040-4 退休 + FE-040-5 接口修复的统一候选，已部署）。
- 等待玩家返回的日志及位置：`C:\Users\braveturtle\AppData\Roaming\StardewValley\ErrorLogs\SMAPI-latest.txt`（集中测试后封存）。
- 日志返回后的成功/失败判据：保护段 ≤2 条且无逐帧；`EnableLogging=false` 时 0 条；失败=仍逐帧输出或开关无效。
- 反证记录：无。

## RC-040B 根因卡（FE-040-4 分屏绘制验收诊断）

- 根因状态：已证实（运行日志归属核对 + 源码状态去重键分析）
- 第一处分歧：BATCH-024/026 时代的验收诊断 `ObjectPatches.LogVisualDiagnostic` 以 `stage|identity` 为键做状态去重防每帧；分屏模式下 `Game1.drawPlayerHeldObject`/`Object.drawWhenHeld` 由 screen_0/screen_1 交替绘制同一玩家，state 字符串含 `isLocal=True/False` 每帧翻转 → 去重键相同但 state 不同，每帧输出 2 条。`GetDrawScale` 的 state 不含 isLocal，仅 15 条，印证键设计本身有效、isLocal 字段是翻转源。
- 决定性证据：SMAPI-latest.txt（2026-08-11 09:57:41，33857 行）中 `[DIAG-FISH-VISUAL]` 共 12976 条：Game1.drawPlayerHeldObject 6486 + Object.drawWhenHeld 6474，集中在 09:46-09:48 分屏窗口；同会话 GetDrawScale 仅 15 条。
- 竞争解释：
  | 假设 | 预测可观察量 | 排除证据/判据 | 状态 |
  |---|---|---|---|
  | H1: 分屏 isLocal 翻转使去重失效 | 仅分屏窗口大量输出、state 交替 isLocal=True/False | 日志计数与分屏时间段吻合；GetDrawScale（无 isLocal）不风暴 | 已证实 |
  | H2: 单屏下也每帧输出 | 非分屏会话同样大量 DIAG-FISH-VISUAL | 09:06 会话（单屏）FishingExpanded 前缀极少 | 已排除 |
- 处置：该诊断为验收诊断，验收早已完成（BATCH-038 已部署验收），按真实证据闭环后临时诊断必须删除的原则退休整条路径（Game1Patches 类删除；LogVisualDiagnostic/_visualDiagnosticStates/ResetVisualDiagnostics 与 3 处调用、2 处清理调用删除）；生产视觉缩放逻辑（Transpiler/GetDrawScale/Prefix 位置补偿/飞行动画缩放）全部保留。
- 反证记录：无。

## RC-040C 根因卡（FE-040-5 GMCM 接口映射）

- 根因状态：已证实（部署后实测日志 + 安装版 GMCM 反编译契约核对）
- 第一处分歧：`FishingExpanded.Services.IGenericModConfigMenuApi` 声明 `Register(IManifest, Action, Action)`（3 参），安装版 GMCM 1.16.0 真实接口为 `Register(IManifest, Action, Action, bool titleScreenOnly)`（4 参，可选参仍计入签名）；Pintail 无法把 3 参代理方法映射到 4 参目标方法，SMAPI 抛 `Tried to map a mod-provided API...` 并放弃整份代理 → GMCM 菜单不注册。
- 决定性证据：SMAPI-latest.txt（2026-08-11 16:24:01）ERROR 技术细节 `The IGenericModConfigMenuApi interface defines method Register which does not exist in the API`；安装 `D:\GGGGG\K1515\Mods\GenericModConfigMenu`（manifest Version 1.16.0）反编译 `GenericModConfigMenu.IGenericModConfigMenuApi` 确认 Register 4 参、AddBoolOption 6 参（含 fieldId）。
- 竞争解释：
  | 假设 | 预测可观察量 | 排除证据/判据 | 状态 |
  |---|---|---|---|
  | H1: 接口签名缺参导致 Pintail 映射失败 | 仅 Register 报错，其余 Mod 正常 | 报错技术细节明确指向 Register；接口反编译 3 参 vs 目标 4 参 | 已证实 |
  | H2: GMCM 未安装或版本过旧 | GetApi 返回 null，无报错 | 日志确认访问到 GenericModConfigMenu.Framework.Api（1.16.0） | 已排除 |
- 处置：接口补全 `Register(..., bool titleScreenOnly = false)`、`AddBoolOption(..., string fieldId = null)`，与安装版契约一致后重建重部署（70B9092E...）；GMCM 菜单注册恢复。
- 反证记录：FE-040-3 反证计数 0→1（本次为 FE-040-3 已部署修复的首次运行时失败）。

## RC-040 多人/分屏影响矩阵

> 运行器无联机/分屏能力时，本矩阵是多人验证的替代证据；日志路径无影响。

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 日志输出 | 全部 | SMAPI Monitor（经 FishingLog） | 静态 bool + 静态限频字典 | 无（无广播） | 限频字典不跨会话，无清理需求 | 静态核对 |
| config.EnableLogging | 全部 | SMAPI config.json | 静态 bool | 无 | 无 | fish_selftest |

无影响（理由：日志与配置均不读写玩家/存档/原生状态，无网络同步）。

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 限频缓存（key→tick） | LogRateLimited | FishingLog | 同函数 | 上限 64 键、每键 30 秒 1 条 | 超上限整体清空 | 无跨会话需求（键为低频异常路径） |
| EnableLogging | config.json/GMCM | 玩家（config） | FishingLog | 1 个 bool | 无 | 无 |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`B0A517FD5E78B117EC9DC6EE56C761293E668FD43F79BAA4FDC68697253DE1AD`（2026-08-11 10:18 部署，含 BATCH-039 + CAT-01；反编译核对）。
- Expanded 接管入口：`BobberBar.update` Prefix/Postfix、`BobberBar.draw` Postfix、`Object.drawWhenHeld`（Transpiler+Prefix）、`FishingRod.draw`（Transpiler 注入的 GetFishVisualScale/AdjustLandingFishPosition）。
- 原生上游入口和状态字段：`BobberBar.distanceFromCatching/distanceFromCatchPenaltyModifier` 每帧更新；`Object.drawWhenHeld`/`Game1.drawPlayerHeldObject` 每帧绘制。
- 原生提交方法：不涉及（日志只读，不改变原生提交）。
- 后续回调、同步和生命周期：不涉及。
- Harmony 拦截点：上述 4 处（`Game1.drawPlayerHeldObject` Prefix 随 FE-040-4 退休；本次只改日志调用，不动 IL 注入逻辑）。
- 第一处分歧：见 RC-040。
- 尚未读取或仍不确定的环节：无（本次不改原生契约）。
- 旧版只读参考及可接受差异：安装 DLL 反编译仅用于核对日志点归属，不作为新契约。

## R0

- 蓄力槽保护日志改为状态转换单发：InstanceData 新增 `ProtectionEngaged`；`combinedModifier < NativeCatchPenaltyModifier - 0.0001f` 判定保护生效，生效/解除各记 1 条 Debug（每段保护周期 ≤2 条）。
- 每帧/高频路径 catch 兜底统一 `FishingLog.LogRateLimited(key, msg, level, 30s)`：BobberBar.Update_Prefix、BobberBar.Update_Postfix、BobberBar.Draw_Postfix、BobberBar.PeriodicCleanup、FishingRodPatches.GetFishVisualScale、FishingRodPatches.AdjustLandingFishPosition、ObjectPatches.DrawWhenHeld_Prefix、ObjectPatches.GetDrawScale、Game1Patches.DrawPlayerHeldObject_Prefix。
- 新增 `FishingLog` 统一日志入口（唯一写入者）：`Log` 受 `EnableLogging` 门控；`LogRateLimited` 同键 30 秒 ≤1 条、上限 64 键超限清空。
- 新增 `ModConfig.EnableLogging`（默认 true）：SMAPI config.json 自动读写；GMCM 可选注册 bool 开关（未安装时跳过），修改实时更新 `FishingLog.Enabled`。
- 185 处直接 `Monitor.Log`/`ModEntry.ModMonitor.Log` 全量迁移到 `FishingLog.Log`。
- `fish_selftest` 增加只读自测：日志门与 config 一致、限频缓存 ≤64。
- R1 待删除旧路径：直接 Monitor 调用；蓄力槽“触发”每帧日志；每帧 catch 直接输出。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 直接 `Monitor.Log`/`ModEntry.ModMonitor.Log`（185 处） | 全量迁移 `FishingLog.Log`；残留检查 `rg "ModMonitor\.Log\(|Monitor\.Log\("` 仅命中 FishingLog 内部 | 无 | 无 |
| 蓄力槽保护“触发”每帧 Debug | 改为生效/解除状态转换 | 无 | 无 |
| 每帧 catch 直接 Error/Warn | 改为 LogRateLimited | 无 | 无 |

- 修改前写入者数量：1（Monitor，未受控）。
- 修改后写入者数量：1（FishingLog → Monitor，受控）。
- 运行时代码新增/删除：新增 FishingLog（约 55 行）+ ModConfig（约 8 行）+ GMCM 接口（约 12 行）+ GMCM 注册（约 28 行）+ 自测 6 行；删除/改写 185 处调用（等量替换）+ 蓄力槽 1 处。
- 净增长理由：日志治理需要唯一受控入口与开关；未复制任何既有所有者，限频缓存归日志所有者自身。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人小游戏保护段 | 生效/解除 ≤2 条 | 每帧 `蓄力槽保护触发` | 静态通过；运行待验收 |
| 每帧路径异常（update/draw/手持） | 同键 30 秒 ≤1 条 | Error/Warn 每帧刷屏 | 静态通过；运行待验收 |
| EnableLogging=false（config.json） | 0 条 Mod 日志 | 任何 Monitor 输出 | 静态通过；运行待验收 |
| GMCM 关闭开关 | 实时生效且写回 config.json | 开关无效或覆盖用户 config | 静态通过；运行待验收 |
| 未安装 GMCM | 正常加载，config.json 仍生效 | 空引用/加载失败 | 静态通过（GetApi null 短路） |
| config.json 缺失 | SMAPI 自动创建默认 true | 覆盖用户设置 | 静态通过（ReadConfig 默认值） |
| 本地主屏/副屏、联机 | 日志按各自实例输出，无串写 | 日志影响任何玩家状态 | 无影响（日志只读） |
| 换日/重载/标题 | 限频缓存无跨会话泄漏需求 | 缓存无界增长 | 静态通过（64 键上限+清空） |
| 性能最坏情况 | 每帧路径仅 bool 判断，无字符串拼接 | 每帧字符串格式化 | 静态通过（限频先短路再拼串） |
| 已验收相邻回归 | 难度/星标/结算/展示不受影响 | 任何行为变化 | 静态通过（仅日志层改动） |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅ 0 警告 0 错误（2026-08-11）。
- 版本/哈希：manifest 0.5.10（未升版）；DLL `31BAF749...`。
- 部署文件与目标：DLL+PDB+manifest+i18n → `D:\GGGGG\K1515\Mods\FishingExpanded`；用户已明确授权“搞定之后直接部署”；不覆盖 config.json（当前不存在）。
- 本次单局路线：真实钓鱼小游戏触发保护段核对日志量；GMCM 关闭日志核对 0 条；`fish_selftest` 日志自测项。
- 日志/截图/存档证据：集中测试后封存。
- 每个 Case 的实际结果：待标记。

## 收尾与归档

- 已完成：R0/R1/R2、Release Rebuild、反编译核对、治理同步。
- 当前不确定性：真实游戏日志量验收（未启动游戏，仅部署授权）。
- 下一条准确操作：部署后由用户进行集中测试并回传日志/截图。
- 总账与测试路线是否已覆盖更新：BUG-LEDGER.md 与本卡同步；TESTING.md 性能维度强化。
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收通过后整理归档。