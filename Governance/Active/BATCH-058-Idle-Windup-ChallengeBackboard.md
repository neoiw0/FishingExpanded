# FishingExpanded 根因批次：BATCH-058 停战休息 + 跳鱼前摇 + 挑战鱼饵背板

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260815-01 |
| 当前活动类别 | CAT-05（力竭节点打断短语 + 跳鱼间隔动态分档，BATCH-059A 修订） |
| 整合候选状态 | CAT-05 实施中（058T/058S 已部署冻结待验收） |
| 当前权威活动卡 | 本卡（BATCH-058） |
| 本轮用户问题范围 | 用户 2026-08-15 确认 B+C 方案：①力竭节点打断录播（短语立即退出，按已降难度重新录制）；②跳鱼间隔按当前有效难度动态分档（瞬移结算冷却时重算） |
| 本轮纳入 Case | FE-059A-1 力竭节点打断短语；FE-059A-2 跳鱼间隔动态分档 |
| 共享第一处分歧与所有权链证据 | 短语状态机唯一所有者=`BobberBarPatches.HandlePhrasePrefix/Postfix`（InstanceData.PhraseMode）；力竭唯一写入者=Update_Prefix 力竭块（EffectiveDifficulty 每帧重算）；跳鱼间隔唯一写入者=构造（521 行）→ 改为瞬移结算冷却时按 EffectiveDifficulty 重算；无第二所有者 |
| 排队或暂不纳入 Case | BATCH-051~057 待验收；FE-058-5 全随机开关、FE-058-6 前摇 70°（已部署冻结待验收，本类别不改） |
| 本轮准入证据 | 用户 2026-08-15 明确确认 B+C 方案（修改玩法；静态推演已给出：录播回放期间难度下降被轨迹冻结掩盖的窗口期） |
| 现有实现复核 | 源码+安装 DLL（ED6D7745...，BATCH-058T 反编译一致）：力竭计时/难度重算在录播期间照常（614-616/668-674 行）；Playing 期间 HandlePhrasePrefix 冻结运动写回放轨迹（1809-1814 行）；短语退出仅两条路（1/3 切换 1785-1797、<150 退出 1768-1777）；跳鱼间隔构造时按开局难度定档（521 行 GetJumpInterval 1396-1404） |
| 允许修改范围 | `Source\Patches\BobberBarPatches.cs`（力竭节点块追加短语打断、跳鱼瞬移结算冷却处重算间隔、相关注释）、`Source\ModEntry.cs`（fish_selftest 新增 GetJumpInterval 分档锚点）；文档：GAME-DESIGN.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | FE-058-5/6（已部署冻结）；存档结构；config.json；i18n；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | ①非挑战鱼饵下每到达力竭节点（1/3/5/7/9/12/15 分钟）：短语状态机复位为 Free（清样本与全部短语标志，同步 1785-1797 切换块清理项），鱼立即按已降难度原生运动，仍 ≥150 则下次跳鱼按新难度重录；挑战鱼饵（难度不衰减）不打断；②跳鱼瞬移结算冷却时 `JumpIntervalSeconds = GetJumpInterval(EffectiveDifficulty)` 再赋冷却，档位跟随力竭下降（<150 仍整体停止） |
| 可观测性决定 | 复用现有"力竭节点"日志（每节点 1 条，由 NextExhaustionNodeIndex 防重），短语实际被打断时追加"短语打断"标记；无新增日志通道、无每帧输出 |
| 自动化验收决定 | `fish_selftest` 新增 `GetJumpInterval` 分档锚点（150/250→8、251/350→6、351/450→5、451/550→4、551→3）；B 的打断行为无纯函数入口，走真实钓鱼路线：170 级鱼长战到 1 分钟节点，日志出现"力竭节点...短语打断"，鱼动作立即变为原生（按已降难度），下次跳鱼重录短语；挑战鱼饵下不打断；存档影响=无 |
| 当前类别阶段 | R0 完成（修复已构建并部署） |
| 当前工作树 | 完整修改（BATCH-033~058 混合未提交基线 + 本类别修改；HEAD c3bc3b4） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `6B79AE60DB69C784CB8520774880ADD7579BEF941C354757B877E23A20EED884`（BATCH-059A；反编译核验 `InterruptPhrase`/节点日志"短语打断"标记/瞬移结算处 `GetJumpInterval(EffectiveDifficulty)` 已编译、挑战鱼饵豁免条件正确） |
| 唯一下一步 | 玩家启动游戏真实验收：①170 级鱼长战——1 分钟力竭节点提示出现且日志含"短语打断（重录）"，鱼动作立即变原生、下次跳鱼按新难度重录；挑战鱼饵下不打断；②长战跳频随难度变慢（9 分钟后跳鱼/短语整体停止）；`fish_selftest` 出现"跳鱼间隔"6 条 PASS |
| 已消费动作 | `Build:ROUND-20260815-01:6B79AE60...`；`Deploy:ROUND-20260815-01:6B79AE60...`；历史 `Build/Deploy:ROUND-20260814-05:ED6D7745...`（BATCH-058T）、`ROUND-20260814-04:CEDFEBE6...`（BATCH-058S）、`ROUND-20260814-03:75356293...`（BATCH-058R）、`ROUND-20260814-02:6ADD17EE...`（BATCH-058Q）见下方修订段 |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-058-1/2/3 | 实施中 | 根因修复 | 背板种子生成/清除日志 | 真实钓鱼 + 挑战鱼饵重试 |
| CAT-02 | FE-058-4 提示坐标系 | 已验收（用户 2026-08-14 确认显示正确） | 已关闭 | TipDraw（含 uiViewportW） | 真实钓鱼目视 |
| CAT-03 | FE-058-5 全随机开关 | 已部署（冻结，待真实验收） | 真实验收 | 种子生成/清除日志（全随机下不出现） | GMCM 开关 + 真实钓鱼 |
| CAT-04 | FE-058-6 前摇旋转 70° | 已部署（冻结，待真实验收） | 根因修复（用户设计修订） | 无新增（fish_selftest 曲线核验） | fish_selftest + 真实钓鱼目视 |
| CAT-05 | FE-059A-1/2 力竭打断短语 + 跳鱼间隔动态分档 | 已部署（冻结，待真实验收） | 根因修复（用户设计修订） | 复用"力竭节点"日志（追加"短语打断"标记） | fish_selftest + 真实钓鱼长战 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-058-1 停战休息 | 设计已确认 | 真实验收 |
| FE-058-2 跳鱼前摇 | 设计已确认 | 真实验收 |
| FE-058-3 背板/不掉等级 | 设计已确认 | 真实验收 |
| FE-058-4 提示坐标系错配 | 已验收（用户确认显示正确） | 已关闭 |
| FE-058-5 全随机模式开关 | 已部署 | 真实验收 |
| FE-058-6 前摇旋转峰值角 70° | 已部署 | 真实验收 |
| FE-059A-1 力竭节点打断短语 | 已部署 | 真实验收 |
| FE-059A-2 跳鱼间隔动态分档 | 已部署 | 真实验收 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 当前安装=`DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`（BATCH-060 统一候选+058T-文案 i18n，2026-08-15 11:48 部署；备份 `DeploymentBackups\FishingExpanded-20260815-114839-pre-BATCH060-058T`=6B79AE60...；源/目标哈希一致；i18n×2 同步含"鱼竿手感增强"文案；config.json 未触碰；部署时游戏运行中，重启生效）；历史=`6B79AE60...`（BATCH-059A，09:50）、`ED6D7745...`（BATCH-058T 前摇 70°，兄弟窗口）、`CEDFEBE6...`（BATCH-058S）、`75356293...`（BATCH-058R）、`6ADD17EE...`（BATCH-058Q，用户实测验收通过） |
<!-- CURRENT-STATE-END -->

## BATCH-058T-文案 图鉴文案修订（2026-08-14/15 用户确认；与兄弟窗口"058T 前摇 70°"同名区分）

- 用户需求：图鉴皇冠加成"鱼竿熟练度+1"（RPG 术语+不可见数值）改为玩家可感知的手感描述；整体排查后一并修订 3 处
- 改动（i18n 中英 + 文档，无运行行为变化，纯文案免除竞争解释表）：
  1. `collections.crownControl`：鱼竿熟练度+1 → **鱼竿手感增强** / Rod Proficiency +1 → **Improved rod handling**（机制名"鱼竿熟练度 α"保留，仅图鉴展示文案）
  2. `collections.challengeRank`：挑战等级：{称号}（等级{X}）→ **挑战等级：{称号}（{X}级）** / Challenge Rank: ... (Lv X)（消除"等级"重复）
  3. `hud.fail.bottom` 中文补连词："请升级鱼竿和钓鱼等级，使用料理增加钓鱼等级，再配上陷阱/软木塞浮标等"
- 文档同步：GAME-DESIGN.md 4.4、TESTING-GUIDE.md、TESTING.md、WIKI.md（含英文段落）、BUG-LEDGER.md 门禁 19
- 部署：文案随 BATCH-060 统一候选于 2026-08-15 11:48 部署（i18n×2 同步安装目录，核验"鱼竿手感增强"已就位）；DLL 不含文案（i18n 为内容文件），运行时行为零变化
- 验收：图鉴打开任意皇冠鱼描述显示"鱼竿手感增强"、挑战等级行显示"{X}级"

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-058-1 | 90+ 鱼太随机且挂机无反馈 | 0 | 无 | RC-058 | 设计完成 | 3 秒不动→停战 |
| FE-058-2 | 跳鱼无前摇可反应窗口 | 0 | 无 | RC-058 | 设计完成 | 前摇旋转后瞬移 |
| FE-058-3 | 挑战鱼饵无法背板、失败掉级 | 0 | 无 | RC-058 | 设计完成 | 两次行为一致 |
| FE-058-4 | 提示/挑战星跑到屏幕最右侧和下侧 | 3（058K/058N/058O 锚点与侧翻） | 058P 诊断证实坐标数值正常但目测右下 | RC-058Q | 根因已证实，修复已构建 | 提示贴绿条 24px/50px |
| FE-058-6 | 前摇旋转峰值角 150°→70°（用户指令） | 0 | 无 | RC-058 | 已部署待验收 | 目视前摇转 70° 后快速转回再瞬移 |
| FE-059A-1 | 力竭节点打断录播（用户确认 B 方案） | 0 | 无 | RC-058 | 已部署待验收 | 节点日志"短语打断"+ 鱼动作随难度降 |
| FE-059A-2 | 跳鱼间隔跟随力竭降档（用户确认 C 方案） | 0 | 无 | RC-058 | 已部署待验收 | 长战跳频随难度变慢 |

## BATCH-059A 力竭节点打断短语 + 跳鱼间隔动态分档（2026-08-15 用户确认）

- 背景：静态推演证实录播回放期间力竭难度下降被轨迹冻结掩盖（170 级 5~9 分钟窗口期"提示已降、动作没降"）；跳鱼间隔构造时按开局难度定档、力竭期间不重算（260 级降到 249.6 仍按 6s 档）。用户确认 B+C 方案
- 实现 B：非挑战鱼饵下每到达力竭节点（1/3/5/7/9/12/15 分钟），短语状态机复位为 Free 并清空全部短语标志（PhraseSamples/PhraseJumpSeen/PhraseWindupStartTime/PhraseJumpText/PlaybackWindupShown/JumpWindupActive/PhraseSwitchPending，与 1/3 切换块同款清理）；鱼立即按已降难度原生运动，仍 ≥150 则下次跳鱼按新难度重录；挑战鱼饵（难度不衰减）不打断；复用"力竭节点"日志（每节点 1 条）追加"短语打断"标记
- 实现 C：跳鱼瞬移结算冷却处 `JumpIntervalSeconds = GetJumpInterval(EffectiveDifficulty)` 再赋冷却——下一跳按当前档位（8/6/5/4/3 秒），<150 仍整体停止；构造时开局定档保留
- 影响面：短语/跳鱼状态仍唯一写入者 BobberBarPatches；无新状态、无存档/i18n 变化；每帧零新增开销（打断≤7 次/局、重算=每跳 4 次浮点比较）
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `6B79AE60DB69C784CB8520774880ADD7579BEF941C354757B877E23A20EED884`；反编译核验 `InterruptPhrase`（8 项标志复位）、节点日志"短语打断（重录）"标记、瞬移结算处 `GetJumpInterval(EffectiveDifficulty)` 已编译
- 部署：2026-08-15 09:50 部署成功（备份 `DeploymentBackups\FishingExpanded-20260815-095046-pre-BATCH059A`=ED6D7745...；源/目标哈希一致；仅 DLL 变化；config.json 未触碰）；待真实验收：①力竭节点日志含"短语打断（重录）"、鱼动作随难度降；②长战跳频变慢、<150 停止；挑战鱼饵不打断

## BATCH-058T 前摇旋转峰值角 70°（2026-08-15 用户指令）

- 用户指令：前摇"改为70度再转回去"——峰值角 150°→70°，0.88s 时序不变（0.77s 转出、0.11s 转回 0°），上跳逆时针（负角）/下跳顺时针（正角）不变
- 实现：旋转曲线拆为纯函数 `GetJumpWindupRotationAt(float elapsed, bool jumpUp)`（供 fish_selftest 只读核验，同 BATCH-053 `GetAccelerationTier` 模式），`GetJumpWindupRotation(InstanceData, bool)` 委托之；`150f`→`70f` 两处；顺带修正源码 3 处过期注释（73/804/843 仍写 0.22s/40°）与 1811 行"旋转 150°"注释
- 影响面：`GetFishIconRotation`（draw Transpiler 注入）与 BATCH-059 短语回放共用同一函数，自动沿用 70°；无新状态、无新所有者、无存档/i18n 变化
- Release Rebuild ✅（0 警告 0 错误，仅 NU1900 网络类警告）；DLL SHA256 `ED6D77452876DD179704154E21C633101B6924D381C3A612FF421146F38EB9DE`；反编译核验 `GetJumpWindupRotationAt`（70f/0.77f/0.11f）已编译、委托链正确、旋转路径无 150f 残留
- 部署：2026-08-14 23:26 部署成功（备份 `DeploymentBackups\FishingExpanded-20260814-232613-pre-BATCH058T`=CEDFEBE6...；源/目标哈希一致；仅 DLL 变化；config.json 未触碰）；待真实验收：高难鱼跳目视前摇先转到 70° 再快速转回后瞬移

## BATCH-058S 开关改名+默认关（2026-08-14 用户修订）

- 用户反馈：058R 开关名"挑战鱼饵背板模式"不好；应叫"鱼的行为全随机模式（更难）"；默认关
- 实现：`ModConfig.EnableChallengeBackboard`（默认 true）整体替换为 `EnableRandomFishBehavior`（默认 false，反编译核验无旧字段残留）；构造边界 `HasChallengeBait && !EnableRandomFishBehavior`；GMCM 键 `config.randomFishBehavior.name/tooltip`（中英）
- config.json 迁移：旧字段 `EnableChallengeBackboard: true` 由 SMAPI 启动自动忽略并补新字段默认 false——语义与旧 true 一致（都是背板模式），行为不变，无需手动迁移
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `CEDFEBE6B5F79946C12F3C6D9994057C1F6175E52D677184300D75833A5692B9`；反编译核验 `EnableRandomFishBehavior`/构造条件/GMCM 已编译、旧字段无残留
- 部署：2026-08-14 18:27（备份 `DeploymentBackups\FishingExpanded-20260814-182728-pre-BATCH058S`=75356293...；源/目标哈希一致；i18n×2 同步；config.json 未触碰）；待真实验收

## BATCH-058R 背板模式开关（2026-08-14 用户指令）

- 用户需求：部分玩家喜欢完全随机，不喜欢可背板；背板模式作为默认，config/GMCM 允许进入全随机模式
- 实现：`ModConfig.EnableChallengeBackboard=true`（默认=背板）；GMCM 布尔开关（日志开关之后、重置区之前）；BobberBarPatches 构造边界 `HasChallengeBait && EnableChallengeBackboard` 才生成种子/PatternRandom——全随机模式下 `PatternRandom=null`，Update_Prefix/Postfix 的随机源替换/恢复天然跳过，整帧随机保持原生；挑战鱼饵其余规则（不掉等级/掉星/5 分钟）不受影响；存档 `ChallengePatternSeeds` 保留（全随机下不新增，切回背板继续用旧种子）
- i18n：`config.challengeBackboard.name/tooltip`（中英）；旧 config.json 缺失字段由 SMAPI 自动补默认 true，不破坏用户设置
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `75356293960D47A9640733CBA2B86CEC285E94D2D7B05AFB7A019B232E015F72`；反编译核验 `EnableChallengeBackboard`（默认 true）/构造条件/GMCM AddBoolOption 已编译；未部署

## BATCH-058Q 根因卡（2026-08-14，提示位置反证 #3 后定位成功）

- 根因状态：已证实（原生渲染链反编译 + 用户设置 + 日志/目测对照）
- 第一处分歧：`BobberBar.draw` 开头 `StartWorldDrawInUI` 切到世界 render target（screen buffer，viewport 物理像素系），结尾 `EndWorldDrawInUI` 恢复 UI render target（uiScreen，uiViewport 逻辑系，`_analysis\StardewValley.BobberBar.decompiled.cs:729`、`StardewValley.Game1.decompiled.cs:11871-11898`）；我们的 `Draw_Postfix` 在 EndWorldDrawInUI 之后执行 → 提示几何（世界系数值）被当作 uiViewport 系绘制。uiScreen 最终以 `options.uiScale` 拉伸（Game1.decompiled.cs:14410），用户窗口 2560x1440、zoomLevel=1、uiScale=-1（自动=2）→ uiViewport=1280x720 → 提示 x=1176 显示在屏幕 2352（超右缘）、y=907 显示在 1814（超底缘）——即"字体在屏幕最右侧和下侧"
- 证据位置：`_analysis\StardewValley.BobberBar.decompiled.cs:729`、`_analysis\StardewValley.Game1.decompiled.cs:11871-11898`、`14407-14416`、`startup_preferences`（zoomLevel=1/uiScale=-1）、SMAPI-latest.txt 16:08 会话（TipDraw 世界系坐标正常但用户目测右下）
- 为什么 058K/058N/058O 都"算对了却不对"：诊断只记录世界系坐标数值（贴 bar），未发现绘制时 SpriteBatch 已切到 UI 系；BATCH-041 验收通过是因为当时 uiScale=1（k=1，两系重合），窗口变大后 uiScale 自动=2 才暴露
- 唯一所有者：`BobberBarPatches.DrawTip`（浮动提示唯一绘制入口）+ `Draw_Postfix` 挑战星覆盖
- 竞争解释表（058O 起）：DrawTip 屏幕钳制推右缘（058O 侧翻未命中排除）、bar 本身在右侧（日志 barX=1052 排除）、分屏坐标系（GCE 日志 split=False 排除）、外部 Mod（当前安装 DLL 反编译一致排除）、**UI/世界 render target 坐标系错配（本卡证实）**
- 修复：`k = uiViewport.Width / viewport.Width`；DrawTip 全部几何（锚点/侧翻/钳制/y/阴影/描边/字体 scale）换算到 UI 系，钳制边界改用 `Game1.uiViewport.Width`；AddTip 分块宽度同换算；挑战星覆盖位置与缩放 `×k`（原生挑战星在世界系 2f 缩放，UI 系需 2f×k 才显示同尺寸）。纯数学换算，不触碰 render target/SpriteBatch 状态
- 高级复核门禁：诊断取证（渲染链反编译+用户设置+日志对照）、所有权审计（DrawTip/挑战星唯一绘制者）、方案设计（k 换算）已完成并绑定新证据，经主控复核通过后实施
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `6ADD17EE936B490F7DD5FE60F66DA7FCC593F9C00F324BC0272A9CE3DE7C0B01`；反编译核验 `uiViewport` 换算/`starK`/`uiViewportW` 已编译；未部署

## RC-058 根因卡

- 根因状态：已证实（用户设计 + 原生调用链）
- 第一处分歧：挂机无状态机；跳鱼 0.5s 延迟后直接瞬移；挑战鱼饵随机源逐帧消耗且失败扣等级
- 唯一所有者：`BobberBarPatches`（挂机/前摇/随机源）、`DifficultyManager`（等级/种子）

## R0

- InstanceData：IdleSeconds/IdlePending/IsIdle/IdleTipShown/LastBarPos、JumpWindupActive/Seconds、PatternSeed/PatternRandom/SavedGameRandom
- Prefix：换随机源、待命→停战（出绿条外 5px）、冻结鱼、战斗计时暂停、跳鱼 0.22s 前摇
- Postfix：恢复随机源、绿条位移检测（>0.5px 恢复）、`RecordFailure(keepLevel: HasChallengeBait)`
- Draw：停战摇头摆尾（sin 摆动 0.09rad）+ 前摇旋转叠加（layer 0.885）
- DifficultyManager：`RecordFailure(keepLevel)`、种子 GetOrCreate/Clear、NormalizeData 归一化种子字典
- FishingRodPatches：成功钓起清除种子
- i18n：`hud.idle.1~10`（中英各 10 条）

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 跳鱼 0.5s 延迟后直接瞬移 | 替换为 0.5s 延迟+0.22s 前摇 | 否 | 无 |
| 挑战鱼饵失败无条件扣级 | 替换为 keepLevel 分支 | 否 | 连续失败计数保留 |

- 修改前写入者数量：1；修改后：1

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 绿条 3 秒不动 | 待命；鱼下次出绿条外 5px 停 | 立即冻结/绿条内停 | 待部署后验证 |
| 停战期间 | 摇头摆尾、蓄力槽不掉、战斗暂停 | 掉条/继续计时 | 待部署后验证 |
| 恢复操作 | 立即恢复运动 | 卡死 | 待部署后验证 |
| 鱼跃/甩尾 | 0.5s 延迟后 0.22s 前摇旋转再瞬移 | 无前摇直接瞬移 | 待部署后验证 |
| 普通 dart | 无前摇 | 也旋转 | 只在跳鱼状态机内 |
| 挑战鱼饵同鱼同等级 | 两次行为一致（钓起前） | 每次不同 | 待部署后验证 |
| 挑战鱼饵失败 | 不掉等级、连续失败+1 | 等级下降 | 待部署后验证 |
| 钓起后 | 种子清除，下次重新随机 | 永久固定 | 待部署后验证 |
| 非挑战鱼饵 | 随机源照常、失败照常扣级 | 被背板/免扣影响 | 条件限定 |
| 鱼王 | 天然豁免 | 参与 | 无 InstanceData |
| 多人/分屏 | 按玩家+存档独立种子 | 串玩家 | modData 键控 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `2433A200D5A2895DBB29030770DD60886606535A2B7780E9F0837C1043B22661`（manifest 仍 0.5.10）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（默认部署授权）
- 部署状态：✅ 2433A200（09:21）→ F5C9CF01（10:04）→ 8A14ADFD（20:11，原生鱼图标旋转 + 前摇 0.88s）

## 反证 #1（2026-08-13 用户实测）

- 现象：停战时鱼仍有小幅度上下颤动；摇头摆尾周期太快（0.45s）；0.22s 前摇期间鱼未绝对静止
- 根因（已证实）：原生 `motionType 3/4` 每帧给 `floaterSinkerAcceleration` ±0.01 并在本帧参与位置积分，Prefix 清零无法阻止（表现为鱼整体上下颤动）；摇头摆尾周期 0.45s 过快
- 修复：停战/前摇冻结位置存入 `IdleFrozenPosition/JumpWindupStartPosition`，Update_Postfix 每帧把 `bobberPosition` 回正（鱼整体绝对静止）；摇头摆尾保持旋转设计（±0.09rad），周期改为一秒一次（期间仅旋转，位置不动）
- 过程修正：曾把摇头摆尾误改为左右平移（自作主张），用户澄清后已还原为旋转设计（fix2）

## 反证 #2（2026-08-13 用户实测）

- 现象：停战/前摇时出现“两条鱼”——一条原鱼、一条叠加旋转的鱼；用户要求绿条上下的那条原鱼自身摇头摆尾；另将 0.22s 前摇改为 0.88s（同比例：0.56s 转出 40°+0.32s 转回）
- 根因（已证实）：此前用 Draw_Postfix 叠加绘制旋转鱼，旋转后原鱼边角露出形成双鱼
- 修复：新增 `BobberBar.draw` Transpiler，把原生鱼图标 Draw 的旋转参数替换为 `GetFishIconRotation`（停战=1s 摇头摆尾、前摇=0.88s 旋转），删除叠加绘制路径；前摇时长 0.22→0.88（0.56/0.32 同比例）

## 反证 #3（2026-08-13 静态复核）

- 现象：fix3 的旋转注入实际未生效——本机 MonoGame `Color.White` 是属性 getter（`call get_White()`）而非字段（ldsfld），Transpiler 链匹配失败
- 修复：匹配改为 `AccessTools.PropertyGetter(Color.White)`（OpCodes.Call）；同时替换颜色参数为 `GetFishIconColor`——0.88s 前摇期间鱼图标发红光并呼吸脉动（Color.Lerp(Red, White, 0.2+0.3·pulse)），其余保持白色

## 提示字体细化（BATCH-058A，2026-08-13 用户选 A）

- 两通道提示：保留白字+蓝/红描边，描边改 4 向更细（α 0.85），并加星露谷风右下 2px 黑色软阴影（α 0.65）
- 部署：`A09BFB83984F498359372EB74EBFF237A8F0651660C9DDC82EA48CF350042954`（20:41；备份 pre-BATCH058-tipstyle=DEBEF7D4...）

## BATCH-058C/058D 修订（2026-08-13 用户指令）

- 前摇旋转比例：0.88s 内 0.66s 转出 40°、0.22s 转回（原 0.56/0.32）
- 提示字体：改用星露谷 `dialogueFont`（原尺寸）+ 右下 2px 黑阴影 + 细蓝/红描边（用户选 C）
- 动物 NPC 赞美：名字匹配常见动物（中英）时先叫声再括号赞美，如“汪汪！！！（这条狗鱼竟然有388cm简直是奇迹）”
- 部署：`AEA2ECED571D083C57C18E8879AC668AE264D0766631ACFC3C0940548D14A833`（21:18；备份 pre-BATCH058-mix=A09BFB83...）

## BATCH-058E 修订（2026-08-13 用户澄清）

- 动物 NPC 识别改为按类型：`StardewValley.Characters.Pet`（petType=Dog/Cat）+ `Horse`；不再按名字判断（宠物名字是玩家自定义）
- 每种动物 5 套叫声随机（狗：汪汪/汪！/汪汪汪/嗷呜～汪/汪~汪；猫：喵喵/喵～/喵呜/喵喵喵/咪；马：嘶嘶/嘶——/唏律律/吁——/嘶～）
- 部署：`606C1B3504DB4A86AB286FACAD5BA4E6CECF8C2B814BD2C3F9518073E81BB863`（21:39；备份 pre-BATCH058-sounds5=F2D30F49...）

## BATCH-058F 修订（2026-08-13 用户指令）

- 前摇旋转：0.88s 内 0.77s 转到 150°、0.11s 快速转回（原 0.66/40°、0.22/0.22）
- 部署：`0E7805F221D70912F55FA4455E6214B6147AA7A3CFC1CA793543D99CF737B1B8`（22:09；备份 pre-BATCH058-spin150=606C1B35...）

## BATCH-058G 修订（2026-08-13 用户反馈）

- 提示长文本出屏：两通道按可用宽度自动折行（英文按词、中文/超长词按字符）+ 8px 屏幕边距钳制
- 部署：`CB5F2E91293D174F2BC10E6D44CDE124A0585C61DDC347ED49E1ECC9053F2BD3`（22:21；备份 pre-BATCH058-wrap=0E7805F2...）

## BATCH-058H 修订（2026-08-14 用户指令）

- 提示描边去饱和：蓝/红向白色混合 85%，只留一点点颜色
- 鱼行动提示（蓝色）改为鱼原地停住开始前摇时即显示（不再等瞬移后），位置=鱼起跳位置上方 30px
- 部署：`5E9CCB68F4D6AA88D4C4568C87FA6A9BE93F452C7F0E813022FBDC685C12AD60`（09:35；备份 pre-BATCH058H=CB5F2E91...）

## BATCH-058I/058J 修订（2026-08-14 用户指令）

- 058I：鱼行动提示从“前摇开始时”再提前 0.5 秒——在跳鱼判定、0.5 秒延迟开始时即显示（位置=鱼当前位置上方 30px）
- 058J：89-99 称号“创世神”改名“龙神王”（i18n rank.creator 中英同步）
- 文档一致性：手感 25px/帧、加速曲线英文、挑战鱼饵掉星、品质表封顶（按用户要求全部修正）
- 部署：`DAF01BE71854C08CE0AEEDC58813451384A9D28342051E17935CB420D5132055`（11:37；备份 pre-BATCH058J=5E9CCB68...）

## BATCH-059 招式短语（2026-08-14 用户确认）

- 调整后难度 ≥150 的非鱼王鱼：短语=瞬移后第一次到中线（266±10，只判到达、不穿越、无超时）→ 下次瞬移后再到中线；该段循环回放（位置轨迹插值+冻结原生运动，跳鱼前摇旋转/红光/行动提示一并重放）；蓄力进度每净涨 1/3（绝对值、只前进）换段
- 实现：`PhraseMode` 状态机（Free/WaitingMiddle/Recording/Playing）+ `PhraseSamples` 轨迹 + Prefix 回放/Postfix 录制
- 部署：`04E96DD4FC867553BC7814078E01996FF753B8A433D6C82337D1A159691399CA`（13:27；备份 pre-BATCH059=DAF01BE7...）

## BATCH-058K 修订（2026-08-14 用户反馈）

- 提示距绿条改为写死的绝对像素 24px（动作提示左缘距绿条右缘、其他提示右缘距绿条左缘），并给文字写死最大宽度 420px 折行——避免长文案换行后一路铺到屏幕右缘
- 部署：`E0025547A62CF3EDCA482A522A8A9331B898B6767F9C68E3BA6990BE7362288B`（14:55；备份 pre-BATCH058K=04E96DD4...）

## BATCH-058L 修订（2026-08-14 用户确认）

- 提示换行加行数上限：最多 3 行；超长内容拆成“续集提示”，排到前一条结束后排队显示（058M，不丢内容）
- 本次单局路线：真实钓鱼三场景（挂机/跳鱼前摇/挑战鱼饵重试）
- 日志/截图/存档证据：待收集
- 每个 Case 的实际结果：待执行

## BATCH-058M 构建/部署状态（2026-08-14）

- 实现：`AddTip` 用 `SplitTipChunks` 把超长文案按最多 3 行拆成“第一段 + 续集”；续集 `PendingDelay = i × (5s + 0.2s)`，前一段显示完再排队出现；非结尾段加“…”，不丢内容
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `18AD4D02D4C8CD4BBE681AA7D94EC3F67C74FC72DAB3CEA7DE3F3E2C137158AD`
- 部署：2026-08-14 15:31 部署成功（源/目标哈希一致 `18AD4D02...`；备份 `DeploymentBackups\FishingExpanded-20260814-152859-pre-BATCH058M`=02A3CB1D...；config.json 未触碰）
- 待部署后验收：长提示按 3 行/段拆分，续集在上一条 5.2 秒后出现

## BATCH-058N 提示锚点用户定稿（2026-08-14）

- 用户定稿：鱼动作提示（鱼跃/甩尾）=左缘距绿条右缘固定 24px、左对齐（锚点 x+124）；鱼其他提示（助战/力竭/巅峰/休战）=右缘距绿条左缘固定 50px、右对齐（锚点 x+14）；上下位置不变（动作=触发时鱼上方 30px，其他=绿条中部）
- 竞争解释与反证：所有已保存反编译快照（batch052~batch059）锚点均为 3 绿条宽（x+208 / x−44）；BATCH-058K 起改为 24px（x+124 / x+40）；用户实测反馈“提示不在绿条周围、非常靠右”且明确“3 个绿条宽是对的”，随后给出 058N 定稿（动作 24px、其他 50px）；当前源码/构建/安装契约均为同一份，排除外部 Mod 或旧 DLL 路径；本次按用户定稿直接修正锚点
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `E0A1A104825395531AD12AADEF7ED0037F50019250DCCAAD745333C2BCE11D56`；反编译核验 `OtherTipAnchorX=14f`、`ActionTipAnchorX=124f`
- 部署：2026-08-14 15:48 部署成功（备份 `DeploymentBackups\FishingExpanded-20260814-154826-pre-BATCH058N`=18AD4D02...；config.json 未触碰）
- 待部署后验收：触发动作提示和其他提示，目视文字与绿条间距=24px / 50px，且不再偏离绿条

## BATCH-058O 屏幕边缘钳制根因（2026-08-14 用户反证）

- 玩家现象：即使按 3 绿条宽/24px/50px 锚点，提示仍会跑到屏幕最右缘，远离绿条；锚点距离本身解释不了该现象
- 反证记录：BATCH-058N 已部署但用户确认“肯定还有独立原因”→ 提示位置现象已部署修复被推翻次数=1，按 SOP 重开并先取证
- 第一处分歧（静态证实）：原生 `BobberBar` 构造把 `xPositionOnScreen` 允许在 `[0, viewport.Width−96]` 任意位置（反编译 252~285 行）；动作提示锚点=绿条右缘+24px，当钓鱼条停在屏幕右侧时 `StartX` 可超过 `viewport.Width`；`DrawTip` 用 `Math.Min(x, viewport.Width−8−maxWidth)` 钳制，把文字左缘直接推到屏幕右缘，导致提示不在绿条周围
- 原生对照：原生鱼图标在同一场景会翻转——`xPositionOnScreen > viewport.Width×0.75f` 时画到 `xPositionOnScreen−80`（反编译 693 行），我们未做侧翻
- 竞争解释表：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态（仍成立/已排除） |
|---|---|---|---|
| DrawTip 屏幕钳制把动作提示推到右缘 | 只有钓鱼条停靠屏幕右侧时，动作提示跑到最右缘/与绿条重叠；其他提示仍在绿条左侧 | 原生 bar 可达 `viewport.Width−96`；`StartX=barRight+24` 可超屏；钳制公式确定；待运行时日志确认 barX | 仍成立（主假设） |
| 钓鱼条本身就在屏幕右侧，提示只是跟着在右侧 | bar 和提示都在右侧且相邻 | 提示被钳到 `viewport.Width−8−maxWidth` 时与绿条重叠/偏离；需日志 barX+finalX 区分 | 仍成立（需日志排除） |
| 分屏 viewport 宽与 xPositionOnScreen 坐标系不一致 | 只在副屏出现 | 待日志记录 viewport 宽与 barX；无副屏日志前不排除 | 仍成立（待排除） |
| 外部 Mod/原生 HUD 在右侧绘制 | 提示与 Mod 无关也出现 | 提示文字为 Mod i18n 文案且走唯一 DrawTip 通道；当前安装 DLL 反编译与源码一致 | 已排除（静态） |

- 唯一所有者：`BobberBarPatches.DrawTip`（唯一浮动提示绘制入口）
- 修复设计：DrawTip 按文字实际宽度判断——原定一侧放不下时翻到绿条另一侧（动作提示翻到左侧=右缘距绿条左缘 50px；其他提示翻到右侧=左缘距绿条右缘 24px），与原生鱼图标翻转同思路；仍保留最终屏幕钳制兜底
- 可观测性决定：新增一次性诊断（每条提示最多 1 条）`TipSideFlip:<TipId>`，记录类型、barX、StartX、maxWidth、finalX、viewport 宽；仅侧翻发生时记录，限频每键 30 秒 1 条、无循环输出
- 自动化验收决定：真实钓鱼移动角色到地图右缘使 bar 停靠屏幕右侧，触发动作提示 → 应翻到绿条左侧且不重叠；日志出现 `TipSideFlip` 且 finalX≈barLeft−50−maxWidth；移动角色到左缘触发其他提示 → 翻到绿条右侧；存档影响=无（纯显示）
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `E2F7FBB7904A2FF48099D117DFBFB1FAB70479DBA0D11844048FC4456B43EED4`；反编译核验 `OtherTipAnchorX=14f`、`ActionTipAnchorX=124f`、`TipSideFlip` 诊断已编译
- 部署：2026-08-14 15:53 部署成功（备份 `DeploymentBackups\FishingExpanded-20260814-155340-pre-BATCH058O`=E0A1A104...；config.json 未触碰）

## BATCH-058P 坐标诊断（2026-08-14，第二次反证）

- 反证记录：BATCH-058O 已部署并实测（SMAPI 日志 15:55:57 起，实例 41313174，太阳鱼 100 级，两次高难度鱼跳），但日志中**没有任何 `TipSideFlip`** → 侧翻分支未命中，钳制假设不足以解释“非常靠右” → 已部署修复被推翻次数=2
- 强制处理：停止局部行为补丁；只做诊断取证、所有权审计和方案设计；先过高级复核门禁再实施（2026-08-14 治理更新：废除 xhigh/EFFORT_GATE 机制，不再登记 xhigh-required）
- 新增诊断（纯日志，不改行为）：`钓鱼小游戏开始` 追加 `barX/barY/viewportW`（每实例 1 条）；`DrawTip` 每条提示最多 1 条 `TipDraw:<Id>`，记录类型、barX、startX、maxWidth、finalX、viewportW、flipped
- 区分判据：若 `barX` 接近 `viewportW−96` 且 `finalX≈startX` → 钓鱼条本身在右侧、提示其实贴着绿条，需截图复核；若 `finalX≈viewportW−8−maxWidth` 且 `flipped=false` → 钳制路径仍被触发但侧翻条件未命中，需查坐标换算；若 `finalX` 与计算不符 → 绘制批/变换层问题
- Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `912D7CCE3CD9F099E2513A34C323D7CE6257651AC91D66CAA983E11FF75525F8`；反编译核验 `TipDraw`、`barX/barY/viewportW` 已编译
- 部署：2026-08-14 16:01 部署成功（备份 `DeploymentBackups\FishingExpanded-20260814-160143-pre-BATCH058P`=E2F7FBB7...；config.json 未触碰）

## 收尾与归档

- 已完成：设计确认（用户逐条答复）
- 当前不确定性：无
- 下一条准确操作：R0 实施 → 构建部署
- 总账与测试路线是否已覆盖更新：本卡 + BUG-LEDGER 同步中
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：否（待真实验收）
