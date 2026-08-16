# FishingExpanded 根因批次：BATCH-066 节日钓鱼开关（鱿鱼节/鳟鱼大赛/冰雪节）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260816-01 |
| 当前活动类别 | CAT-01（用户 2026-08-16 指令：允许玩家设置钓鱼节日是否使用模组；默认关闭=节日完全原生；开启后分数按数量倍数翻倍） |
| 整合候选状态 | 逐类处理中 |
| 当前权威活动卡 | 本卡（BATCH-066） |
| 本轮用户问题范围 | ①覆盖范围=鱿鱼节+鳟鱼大赛（用户先确认），追问后补入冰雪节（冬 8 冰湖钓鱼比赛，用户提示存在，已证实第三个钓鱼节日）；②默认关闭（当前行为=节日全用模组，将反转）；③关闭语义=完全原生（无难度等级、无数量倍数、无经验倍数、无品质加成、无模组 HUD/结算）；④开启后分数公式=原生数量×数量倍数（鱿鱼节难度 10 → 一次给 10 个鱿鱼的分）；⑤冰雪节开启后比赛分数×数量倍数（festivalScore 每次成功 +数量倍数 分） |
| 本轮纳入 Case | FE-066-1 节日判定与开关（GMCM + config）；FE-066-2 被动节日（鱿鱼节/鳟鱼大赛）完全原生门；FE-066-3 冰雪节（winter8）完全原生门 + pending 残留修复；FE-066-4 鱿鱼节分数补差（SquidFestScore = numberCaught × 数量倍数）；FE-066-5 冰雪节分数补差（festivalScore += 数量倍数） |
| 共享第一处分歧与所有权链证据 | 三个节日两条结算路径：①被动节日（SquidFest/TroutDerby，`Utility.IsPassiveFestivalDay`，`Game1.netWorldState.Value.ActivePassiveFestivals.Contains`，`_analysis\festival-tmp\StardewValley.Utility.decompiled.cs:5557`）走完整钓鱼链：BobberBar 创建（`startMinigameEndFunction` 无 isFestival 检查，939 行 `new BobberBar`）→ `pullFishFromWater` → `Farmer.caughtFish`（鱿鱼节分数=原生 `SquidFestScore(Game1.dayOfMonth, Game1.year) += numberCaught`，仅 `(O)151`，`_analysis\festival-tmp\farmer\StardewValley.Farmer.decompiled.cs:3019-3022`）→ `gainExperience`（`doPullFishFromWater` 1188 行 `!Game1.isFestival()` 才给经验，被动节日 isFestival=false → 给经验）；②事件型节日（冰雪节 winter8）：`Game1.isFestival()`=true → `playerCaughtFishEndFunction` 1102-1112 分支走 `Event.caughtFish`（不走 `Farmer.caughtFish`），`fishCaught=false` 鱼不进背包；`Event.caughtFish` winter8 分支（`_analysis\festival-tmp\event\StardewValley.Event.decompiled.cs:13159-13171`）：`size>0 && TilePoint<79,43` → `festivalScore++`（每次成功 +1 分，不看鱼种/数量）；`Event.isSpecificFestival`=`isFestival && id=="festival_winter8"`（11735-11742）；经验被 `!Game1.isFestival()` 原生跳过；`doPullFishFromWater` 1188 行经验分支同样跳过。冰雪节无 BobberBar 例外：`startMinigameEndFunction` 891-940 无条件创建 BobberBar → 模组 BobberBarPatches 会命中（当前行为下冰雪节小游戏被模组改难度=用户反感的点）。TroutDerby tag（`doneHoldingFish` 2310 行 `0.33×numberOfFishCaught`）不走 CreateFish，数量倍数不影响 tag 数（原生语义保留） |
| 排队或暂不纳入 Case | BATCH-059A/060/061/062/064l/065 待真实验收（本卡不重复修改）；BATCH-064 白色半透明矩形方案 A/B 待用户裁决（独立类别，排队）；本期不扩展夜市潜艇钓鱼（冬 15-17，`Game1.currentMinigame is FishingGame` 非 FishingRod 链，模组不介入，见 RC-01 排他性说明） |
| 合并/拆分决定及依据 | 单类别（一个用户指令、统一开关、同一原生钓鱼链边界），不拆分 |
| 本轮准入证据 | 用户 2026-08-16 明确修改要求 + 5 项设计口径逐条确认（ask_user_question：覆盖范围三节日/默认关/完全原生/分数×倍数/冰雪节纳入）；当前安装 DLL 反编译：`FishingRod.decompiled.cs`（startMinigameEndFunction 939 无条件 BobberBar、playerCaughtFishEndFunction 1102 节日分支、doneHoldingFish 2310 TroutDerby、pullFishFromWater 1137、doPullFishFromWater 1188 经验分支）、`Event.decompiled.cs`（caughtFish 13143、winter8 13159、isSpecificFestival 11735）、`Farmer.decompiled.cs`（caughtFish 2993、SquidFestScore 3019）、`Utility.decompiled.cs`（IsPassiveFestivalDay 5557/GetDayOfPassiveFestival 5571）、`Game1.decompiled.cs`（isFestival 12061）、`StatKeys.decompiled.cs`（SquidFestScore 191-193 `$"SquidFestScore_{day}_{year}"`）——均在 `_analysis\festival-tmp\` |
| 现有实现复核 | `BobberBarPatches.Constructor_Postfix`（419-548）：注册 InstanceData + 改 difficulty/fishQuality/bobberTargetPosition/bobberBarHeight——节日原生门=开头直接 return（不注册实例、不改任何 ref）→ Transpiler 注入的静态方法 `_instanceData.TryGetValue` 全部失败回落原生（GetAlpha=0→ApplyBarInput `speed+num5×k` 原生路径、ApplyBounce 原生、GetFrameScale 60fps=1）；`Update_Prefix/Postfix`、`Draw_Postfix` 均 `_instanceData.TryGetValue` 门→未注册自动跳过；`Update_Transpiler` 内联 150f 难度封顶（BATCH-028）对原生 difficulty<150 无影响（鱿鱼/鳟鱼原生难度均 <150）→ 原生门无需动 Transpiler；`FishingRodPatches.PullFishFromWater_Prefix`（330-426）无条件记录 PendingFishData——冰雪节会记录但无人消费（残留，当前已部署版本的潜在 bug：下一杆/下一次经验结算可能错误消费）；`CreateFish_Postfix`（429-503）数量倍数转换——冰雪节 `doneHoldingFish` 不调用（fishCaught=false）所以 CreateFish 不调用，数量倍数天然不生效；`CaughtFish_Prefix/Postfix`（662-776）——被动节日会命中（需门），冰雪节不命中（结算走 Event）；`FarmerFishingLevelPatches.GainExperience_Prefix`——被动节日命中（需门），冰雪节原生跳过；`ModConfig.cs` 现有 EnableLogging/EnableRandomFishBehavior 两字段 + GMCM 注册模式（ModEntry.OnGameLaunched 152-185） |
| 允许修改范围 | `Source\ModConfig.cs`（+EnableFestivalFishingMods）、`Source\ModEntry.cs`（GMCM 注册）、新增 `Source\Services\FestivalFishingService.cs`、`Source\Patches\BobberBarPatches.cs`（Constructor_Postfix 节日门）、`Source\Patches\FishingRodPatches.cs`（PullFishFromWater_Prefix/CreateFish_Postfix/CaughtFish_Prefix+Postfix 节日门 + 新增 Event.caughtFish Postfix 冰雪节补分 + SquidFest 补差）、`Source\Patches\FarmerFishingLevelPatches.cs`（GainExperience_Prefix 节日门）、`Source\i18n\default.json` + `zh.json`（config.festivalFishing.*）；文档：GAME-DESIGN.md、TESTING-GUIDE.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 61 可计数池；鱼王豁免；助战；挑战鱼饵；每日限额；存档结构；GMCM 重置区；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 开关 `EnableFestivalFishingMods`（默认 false）：①关闭=三个节日钓鱼链全部原生（BobberBar 不注入难度/品质/手感/提示、不记录 pending、不乘数量、不乘经验、不记录难度等级、无模组 HUD）；②开启=模组全链照常 + 鱿鱼节分数 `SquidFestScore += numberCaught × (Multiplier−1)` 补差（原生已 +numberCaught）+ 冰雪节 `festivalScore += (Multiplier−1)` 补差（原生已 +1） |
| 可观测性决定 | 复用 FishingLog：节日原生门每实例/每成功 1 条 Info（`[节日] 原生模式跳过 <边界>`，频率=钓鱼事件本身）；鱿鱼节补差 1 条 Info（`[Farmer] 鱿鱼节分数补差`）；冰雪节补差 1 条 Info（`[Event] 冰雪节分数补差`）；无逐帧输出；开关切换由 SMAPI config 读写天然可观测 |
| 自动化验收决定 | 复用现有 `fish_selftest`：新增 3 条只读断言（`FestivalFishingService.IsFestivalFishingActive` 非节日=false；开关默认=false；`GetDayOfPassiveFestival` 非节日=-1）；节日状态无法用现有命令伪造（原生 `ActivePassiveFestivals` 网络状态），真实节日分数验证走生产入口（玩家实测：冬 8 冰雪节/冬 12-13 鱿鱼节/夏 20-21 鳟鱼大赛）。存档影响=有（SquidFestScore 为原生 stats 键，模组仅增量；festivalScore 为 Farmer 内存字段不入档）→ 真实验收时使用统一全量 `Saves` 沙箱并事后恢复 |
| 当前类别阶段 | 取证完成；设计完成；R0 实施完成（含统一 Release 构建） |
| 当前工作树 | 部分修改（作者 neoiw 未提交基线 + 本类别修改） |
| 本轮统一构建 | `6F681270C170EC6230C7536A5088EFFB0E1DC981970D8B2E9B1F487CBC1B2283`（Release Rebuild，0 警告 0 错误，2026-08-16；反编译核验通过：FestivalFishingService 全方法、BobberBarPatches 构造门+GetAccelerationBoost/GetFrameScale 门、FishingRodPatches pending 门/CreateFish 门/RemovePending、FarmerFishingPatches CaughtFish 双门+SquidFest 补差、EventFestivalPatches 冰雪节补差、FarmerFishingLevelPatches 经验门、selftest 3 条节日断言，证据 `_analysis\batch066-verify`） |
| 唯一下一步 | R2 静态推演 → 文档同步（GAME-DESIGN/TESTING-GUIDE/TESTING）→ 用户授权部署 |
| 已消费动作 | 无 |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单类别功能批次，主线程已掌握全部调用链与文件；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-066-1/2/3/4/5 | 实施中 | 根因修复（新功能） | `[节日]` 原生门日志（每事件 1 条） | fish_selftest 3 条只读断言 + 真实节日实测 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-066-1 节日判定与开关 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-066-2 被动节日完全原生门 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-066-3 冰雪节完全原生门 + pending 残留修复 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-066-4 鱿鱼节分数补差 | 设计完成 | R0 实施 + 构建 + 真实验收 |
| FE-066-5 冰雪节分数补差 | 设计完成 | R0 实施 + 构建 + 真实验收 |

> 机制断言：本批为新功能（用户确认设计），非 Bug 根因修复；竞争解释表不适用（无互斥假设需区分），准入证据=用户确认 + 原生调用链取证。排他性说明：夜市潜艇钓鱼（冬 15-17）为 `FishingGame` 小游戏（`Game1.currentMinigame`），不走 FishingRod/BobberBar/Farmer.caughtFish 链，模组任何 Patch 均不介入，无需纳入开关（证据：`Event.caughtFish` 13149 `Game1.currentMinigame is FishingGame && isSpecificFestival("fall16")` 分支仅集市钓鱼游戏；`FishingRod` 无夜市路径）。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---|---:|---|---|---|
| 1 | CAT-01 / FE-066-1 | fish_selftest | 通过 / 未执行 | 非节日判定 false、开关默认 false |
| 2 | CAT-01 / FE-066-2/4 | 真实鱿鱼节（冬 12-13）钓鱼 | 通过 / 未执行 | 关=完全原生（无难度注入、无倍数、分数原生）；开=分数=numberCaught×倍数（难度 10 → +10 分） |
| 3 | CAT-01 / FE-066-3/5 | 真实冰雪节（冬 8）钓鱼 | 通过 / 未执行 | 关=完全原生 + 无 pending 残留（下一杆正常）；开=每次成功 festivalScore +数量倍数 分 |
| 4 | CAT-01 / FE-066-2 | 真实鳟鱼大赛（夏 20-21）钓鱼 | 通过 / 未执行 | 关=完全原生；开=模组照常（tag 概率原生 33%×numCaught 不变） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-066-1 | 节日钓鱼无法选择是否用模组 | 0 | 无（新功能） | RC-01 | 设计完成 | GMCM 出现开关且默认关 |
| FE-066-2 | 鱿鱼节/鳟鱼大赛被模组难度/倍数介入 | 0 | 无（新功能） | RC-01 | 设计完成 | 关=小游戏原生难度 |
| FE-066-3 | 冰雪节小游戏被模组注入难度；pending 残留 | 0 | 无（新功能+潜伏缺陷） | RC-01 | 设计完成 | 关=原生；下一杆无错误结算 |
| FE-066-4 | 鱿鱼节分数不随难度等级翻倍 | 0 | 无（新功能） | RC-01 | 设计完成 | 难度 10 一次 +10 分 |
| FE-066-5 | 冰雪节分数不随难度等级翻倍 | 0 | 无（新功能） | RC-01 | 设计完成 | 开启后每次成功 +倍数 分 |

## RC-01 根因卡

- 根因状态：不适用（新功能，用户确认设计；FE-066-3 附带修复一个潜伏缺陷：冰雪节钓鱼写入 PendingFishData 但 `Farmer.caughtFish` 不调用导致无人消费——证据 `FishingRod.decompiled.cs:1102-1112` 节日分支 + `FishingRodPatches.cs:399` 无条件记录）
- 原生调用链（已完成，满足"完成调用链再进入 R0"）：
  - 当前安装 DLL 版本/哈希：`D:\GGGGG\K1515\Stardew Valley.dll`，2024-12-21 04:13，6268416 字节（1.6.15 build 24354）
  - Expanded 接管入口：`BobberBar` 构造（被动+冰雪节共同入口）、`FishingRod.pullFishFromWater`（记录 pending）、`Farmer.caughtFish` Prefix/Postfix（被动节日结算）、`Farmer.gainExperience` Prefix（被动节日经验）、新增 `Event.caughtFish` Postfix（冰雪节补分）
  - 原生上游入口和状态字段：`FishingRod.startMinigameEndFunction`（BobberBar 创建，无条件）；`Game1.isFestival()`（事件型节日）；`Utility.IsPassiveFestivalDay(string)`（被动节日，`ActivePassiveFestivals`）；`Game1.currentLocation.currentEvent.isSpecificFestival("winter8")`（冰雪节判定）
  - 原生提交方法：被动节日=`Farmer.caughtFish`（收藏+SquidFestScore）+ `gainExperience`；冰雪节=`Event.caughtFish`（仅 festivalScore++，鱼不进背包）
  - 后续回调、同步和生命周期：`playerCaughtFishEndFunction` 1102 节日分支 `fishCaught=false; doneFishing(farmer)`；`doPullFishFromWater` 1188 经验分支 `!Game1.isFestival()` 跳过；pending 字典按玩家+鱼ID键控，`ClearPending` 标题清理
  - Harmony 拦截点：见"Expanded 接管入口"
  - 第一处分歧：无（新功能开关，全部边界为新增门 + 新增补差，不修改既有非节日行为）
  - 尚未读取或仍不确定的环节：鱿鱼/鳟鱼原生难度数值（Data/Fish）——R2 时核验原生 difficulty<150（150 封顶 Transpiler 不影响原生门）；夜市（冬 15-17）确认无 FishingRod 链
  - 旧版只读参考及可接受差异：无

## 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 节日原生门判定 | 各玩家本地（`Game1.player` 判定） | `FestivalFishingService`（只读） | 只读 Utility/Game1 状态 | 无 | 无 | fish_selftest 3 条 |
| 鱿鱼节分数补差 | 本地玩家（CaughtFish_Postfix `__instance.IsLocalPlayer` 已门） | `Game1.stats.Increment`（原生统计所有者） | 补差增量 | 原生 stats 同步 | 原生机制 | 真实节日 |
| 冰雪节分数补差 | 本地玩家（`who.IsLocalPlayer` 门） | 原生 `Event.caughtFish`（festivalScore 唯一写入者，Postfix 补差仍经同一字段） | 内存字段 | 无广播（festivalScore 原生本地） | 节日结束自然清零（原生） | 真实节日 |
| pending 消费（冰雪节） | 本地玩家 | `FishingRodPatches`（唯一所有者） | 字典键控 | 无 | `ClearPending` 标题清理 + 本次消费式移除 | 真实节日 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| PendingFishData | `PullFishFromWater_Prefix`（原生门关闭时跳过） | `FishingRodPatches` | `CaughtFish_Postfix`（被动）/ 新增 `Event.caughtFish Postfix`（冰雪节，消费即移除） | 每玩家每鱼 1 槽 | 消费即移除 | 标题 `ClearPending`；远程玩家不记录 |
| 节日开关 | 用户 config/GMCM | `ModConfig`（唯一） | 各 Patch 只读 | 1 | — | SMAPI 自动读写 |

## R0

1. `ModConfig.cs`：新增 `public bool EnableFestivalFishingMods { get; set; } = false;`（默认关=当前行为反转，用户明确要求）。
2. 新增 `Source\Services\FestivalFishingService.cs`：
   - `IsSquidFest()`=`Utility.IsPassiveFestivalDay("SquidFest")`；`IsTroutDerby()`=`Utility.IsPassiveFestivalDay("TroutDerby")`；`IsIceFestival()`=`Game1.isFestival() && Game1.currentLocation?.currentEvent?.isSpecificFestival("winter8")==true`；`IsFestivalFishingActive()`=三者或；`IsVanillaFestivalMode()`=`IsFestivalFishingActive() && !ModEntry.Config.EnableFestivalFishingMods`。
3. `BobberBarPatches.Constructor_Postfix` 开头（force 消费后、鱼王豁免前）：`if (FestivalFishingService.IsVanillaFestivalMode()) { 日志; return; }`——不注册实例、不改任何 ref → Update/Draw 全部实例门自动跳过、Transpiler 静态方法回落原生（GetAlpha=0、GetFrameScale 60fps=1、内联 150 封顶对原生 difficulty 无影响）。
4. `FishingRodPatches.PullFishFromWater_Prefix` 开头：`if (IsVanillaFestivalMode()) return;`（不记录 pending → 冰雪节无残留，被动节日无倍数/无结算数据）。
5. `FishingRodPatches.CreateFish_Postfix` 开头：`if (IsVanillaFestivalMode()) return;`（数量倍数/每日限额在节日关闭时不生效）。
6. `FarmerFishingPatches.CaughtFish_Prefix/Postfix` 开头：`if (IsVanillaFestivalMode()) return;`（收藏尺寸、等级结算、皇冠、巨型鱼、星之果茶、HUD 全部跳过）。
7. `FarmerFishingLevelPatches.GainExperience_Prefix` 开头：`if (IsVanillaFestivalMode()) return;`（经验倍数在被动节日关闭时不生效；冰雪节原生就无经验）。
8. 鱿鱼节补差（CaughtFish_Postfix 内，SuccessRecorded 后）：`if (FestivalFishingService.IsSquidFest() && fishId=="(O)151" && data.Multiplier>1) Game1.stats.Increment(StatKeys.SquidFestScore(Game1.dayOfMonth, Game1.year), numberCaught * (data.Multiplier - 1));`（原生已 +numberCaught，总分=numberCaught×倍数）。
9. 新增 `Event.caughtFish` Postfix（冰雪节补分 + pending 消费）：`[HarmonyPatch(typeof(Event))]`，`who.IsLocalPlayer && __instance.isSpecificFestival("winter8") && size>0 && TilePoint<79,43` 且 `TryGetPending(who, fishId, out data)` → `who.festivalScore += (data.Multiplier - 1)`（>0 时）+ 消费式移除该 pending + 1 条 Info 日志。
10. `ModEntry.OnGameLaunched`：GMCM `AddBoolOption`（config.festivalFishing.name/tooltip）。
11. i18n default.json + zh.json：`config.festivalFishing.*` 双语。
12. `fish_selftest`：新增 3 条只读断言（非节日 IsFestivalFishingActive=false；开关默认=false；非节日 GetDayOfPassiveFestival=-1）。
13. 文档：GAME-DESIGN.md 新增节日规则小节；TESTING-GUIDE.md/TESTING.md 补节日场景。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| （无旧字段删除）冰雪节 pending 残留路径 | 消费式移除替代"记录后无人消费" | 否（新消费点接管） | 无 |

- 修改前写入者数量：pending 写入=1（PullFishFromWater_Prefix）；festivalScore 写入=1（原生 Event.caughtFish）
- 修改后写入者数量：pending=1（消费者新增 Event Postfix）；festivalScore=1（原生，Postfix 补差经同一字段，无第二状态）
- 运行时代码新增：约 120 行（服务 + 6 处节日门 + 2 处补差 + GMCM/i18n）；净增长理由：新功能开关需要统一判定服务与各边界门；补差复用原生统计/字段所有者，不新增第二套状态。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人：鱿鱼节关 | 小游戏原生难度、数量 1 条、经验原生、分数原生（numberCaught） | 难度注入/倍数/经验倍率/模组 HUD | 待构建后验证 |
| 鱿鱼节开（难度 10） | 分数=1×10=10 分（原生 +1 补 +9）；数量 10 条 | 分数重复计算（原生+补差恰好总数） | 补差公式 |
| 鳟鱼大赛关 | 完全原生 | 倍数/tag 概率变化 | 门 |
| 鳟鱼大赛开 | 模组照常；tag 概率原生 33%×numCaught | tag 数量被倍数放大（tag 不走 CreateFish） | 原生链路 |
| 冰雪节关 | 小游戏原生、鱼不进背包、分数 +1/次、下一杆无错误结算 | pending 残留污染下一杆 | 门 + 消费式移除 |
| 冰雪节开 | 小游戏模组难度；每次成功 festivalScore +数量倍数 分 | 双倍补差（原生 +1 后 Postfix 补 multiplier−1） | 补差公式 |
| 夜市潜艇钓鱼（冬 15-17） | 原生不受影响（FishingGame 小游戏，无 FishingRod 链） | 被节日门误伤 | 原生链路（无 BobberBar） |
| 普通钓鱼（非节日） | 完全不变（门=false） | 节日判定误触发 | IsPassiveFestivalDay=-1 非节日 |
| 非鱼类/鱼王 | 豁免规则照常（与节日无关） | 节日门改变鱼王行为 | 鱼王在门之后检查 |
| 双人同屏/联机 | 各玩家本地判定；分数补差只操作本地玩家 | 副屏/远程被串写 | IsLocalPlayer 门 |
| 换日/标题 | 节日结束判定自然为 false；pending 标题清理 | 跨日残留 | ClearPending |
| 性能最坏情况 | 门判定=2 次字典 Contains + 1 次引用判空，仅钓鱼事件时调用 | 逐帧输出 | 频率=钓鱼事件 |
| 已验收相邻回归 | BATCH-061 限额、BATCH-060 数量/品质、BATCH-058S 随机模式不受影响 | 节日门触碰非节日路径 | 门=false 时零行为变化 |

## 构建、部署与集中测试

- 构建结果：待执行（0 警告 0 错误目标）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（待用户授权）
- 部署状态：未部署
- 本次单局路线：`fish_selftest`（含新增 3 条断言）→ 真实鱿鱼节/冰雪节/鳟鱼大赛（依用户可达日期）
- 日志/截图/存档证据：待执行
- 每个 Case 的实际结果：待执行

## 收尾与归档

- 已完成：设计确认（5 项）、三个节日原生调用链取证、方案设计
- 当前不确定性：无（静态契约可完整证明；真实验收待用户）
- 下一条准确操作：R0 代码实施
- 总账与测试路线是否已覆盖更新：实施后同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收后
