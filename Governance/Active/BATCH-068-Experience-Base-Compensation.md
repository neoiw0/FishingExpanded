# FishingExpanded 根因批次：BATCH-068 经验基数补偿（负数等级/力竭衰减不再少于原生）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260816-01（BATCH-066/067 已部署；本批为同轮追加类别，统一构建包含全部） |
| 当前活动类别 | CAT-01（经验基数补偿：负数难度等级/力竭衰减使传入难度 < 原生难度时，经验重算为原生难度水平） |
| 整合候选状态 | 逐类处理中 |
| 当前权威活动卡 | 本卡（BATCH-068） |
| 本轮用户问题范围 | ①排查"我们的系统导致玩家获得经验比原本少"——已证实两条路径（负数等级/力竭衰减），用户确认方案 A；②用户追加：经验公式难度输入上限钳制，确认 **120**；③用户追加：经验倍数曲线——"10难度能力还是乘以5，之后平滑处理，100难度等级乘以20"，确认线性插值（10→×5、100→×20；1~10 级现状不变） |
| 本轮纳入 Case | FE-068-1 负数难度等级经验减少（主路径）；FE-068-2 力竭衰减经验减少（边缘路径）；FE-068-3 经验倍数膨胀（100 级 ×50 → 线性 ×5→×20） |
| 共享第一处分歧与所有权链证据 | 原生经验公式 `doPullFishFromWater`（`_analysis\StardewValley.FishingRod.decompiled.cs:1188-1203`）：`num5 = max(1, (num2+1)×3 + num3/3)`（num2=品质、num3=难度），宝箱 +120%、完美 +140%、Boss ×5；num3 来自 BobberBar 成功回调 `pullFishFromWater(..., (int)difficulty, ...)`（`_analysis\StardewValley.BobberBar.decompiled.cs:354-361`）——而 BobberBar.difficulty 被本 Mod 构造边界乘难度倍数（`BobberBarPatches.cs:510`）且力竭节点改写为有效难度（`BobberBarPatches.cs:690-697`）；`GetExperienceMultiplier` 负数=1 不补偿（DifficultyCalculator.cs:43-47） |
| 排队或暂不纳入 Case | BATCH-064 方案 A/B 待裁决；BATCH-059A/060/061/062/064l/065/066/067 待真实验收 |
| 合并/拆分决定及依据 | 单类别（两条路径同一第一处分歧：经验基数与传入难度挂钩，同一修复点 GainExperience_Prefix），不拆分 |
| 本轮准入证据 | 用户排查要求 + 确认方案 A 与上限 120（ask_user_question）；源码取证（BobberBarPatches 构造 510/力竭 690-697、PullFishFromWater_Prefix 签名、GainExperience_Prefix、GetExperienceMultiplier）；原生契约取证（经验公式 FishingRod 1188-1203、成功回调 BobberBar 354-361、原生最高难度 Legend II=110：Fish-data-extracted.txt:71） |
| 现有实现复核 | `GetExperienceMultiplier`（DifficultyCalculator.cs:103-107）= max(1, round(level×0.5, AwayFromZero))，100 级 = ×50（BATCH-060 公式，用户确认保留至本批）；`GainExperience_Prefix`（FarmerFishingLevelPatches.cs）：HarvestLimited 置 0 → 重算分支 → `howMuch × ExperienceMultiplier`（倍数 ≤1 时 return）；`PullFishFromWater_Prefix` 原签名只收 (fishId/fishSize/fishDifficulty/numCaught/fromFishPond)，未记录品质/宝箱/完美/Boss/原生难度；`PendingFishData` 无这些字段；`InstanceData.OriginalDifficulty` 已有（构造快照，BobberBarPatches.cs:482）；难度倍数分段线性锚点（4→1.2/20→5/50→20/100→50，2026-08-10 用户确认） |
| 允许修改范围 | `Source\Patches\FishingRodPatches.cs`（PendingFishData 字段 + PullFishFromWater_Prefix 签名扩展与记录）、`Source\Patches\BobberBarPatches.cs`（GetOriginalDifficulty 只读方法）、`Source\Patches\FarmerFishingLevelPatches.cs`（GainExperience_Prefix 补偿）、`Source\Utils\DifficultyCalculator.cs`（GetNativeExperience/GetExperienceDifficulty 纯函数 + GetExperienceMultiplier 曲线）、`Source\ModEntry.cs`（selftest 断言）；文档：GAME-DESIGN.md、TESTING-GUIDE.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 经验倍数 ≤10 级公式（BATCH-060 现状）；数量/等级/皇冠结算；存档结构；每日限额；config.json；部署目录；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 经验结算校准（两类 Case）：①**经验公式难度输入钳制到 [原生难度, 120]**（用户 2026-08-16 确认上限=120，比原生最高 110 大一点）：`PassedDifficulty < NativeDifficulty`（负数等级/力竭）→ 按原生难度重算；`PassedDifficulty > 120`（正数高等级）→ 按 120 重算；中间不变。重算复刻原生公式 `GetNativeExperience(q, clamp, 宝箱, 完美, Boss)`，随后继续走经验倍数。②**经验倍数曲线**（用户 2026-08-16 确认线性）：1~10 级保持 max(1, round(level×0.5))（10 级 = ×5 现状不变）；10~100 级线性 ×5→×20（`round(5 + (level−10)×15/90)`，100 级 = ×20，原 ×50）；≥100 封顶 20；负数 = 1 |
| 可观测性决定 | 复用日志：补偿时 `[FarmerFishingExperience] 经验基数补偿`（每成功 1 条 Info，含难度等级/传入难度/原生难度/经验前后值）；无新增频率 |
| 自动化验收决定 | 复用 `fish_selftest`：新增 5 条只读断言（GetNativeExperience 复刻原生公式：70 普通=26、完美=62、宝箱=57、Boss×5=195、品质 4=38）。存档影响=无；真实验收走生产入口（真实钓鱼：负数等级鱼 + 力竭 15 分钟扩展传奇） |
| 当前类别阶段 | R0 实施完成；R1 完成；R2 推演完成；**已部署（2026-08-16 11:52）** |
| 当前工作树 | 部分修改（BATCH-066/067 已部署源码 + 本类别修改；同文件叠加无法拆分，仅按明确路径暂存） |
| 本轮统一构建 | `F5DA3733CBA06B8079C98F40F671BA2422B935CD4A45D2B6F3AE3B2E297F8895`（Release Rebuild，0 警告 0 错误，2026-08-16 二次构建，含 BATCH-066/067/068 全部源码含倍数曲线；反编译核验通过：GetExperienceMultiplier 分段曲线（1343-1357：≤10 现状/10~100 线性 5+(level−10)/6/≥100 封顶 20）、ExperienceDifficultyCap=120f（1260）、GetExperienceDifficulty/GetNativeExperience（1324/1333）、GetOriginalDifficulty（4180）、GainExperience_Prefix 重算分支+钳制+倍数衔接（5448-5462）、PendingFishData 6 字段（5511）、PullFishFromWater_Prefix 记录（5912/5935）、selftest 17 条断言（1071-1087），证据 `_analysis\batch068-verify`；前次构建 `12DD9D97...` 因倍数修改作废未部署） |
| 唯一下一步 | 待真实验收（用户此前明确"仅部署，先不测试"；测试顺序见集中测试表：fish_selftest 17 条 + 负数金枪鱼=26 + Legend II 力竭=39 + 50/100 级日志 ×12/×20） |
| 已消费动作 | `Deploy:ROUND-20260816-01:F5DA3733CBA06B8079C98F40F671BA2422B935CD4A45D2B6F3AE3B2E297F8895`（2026-08-16 11:52 部署；轮询任务 pwsh-41 在检测到游戏退出前被终止（exit 1073807364，日志停在"轮询开始"），随后确认游戏已退出、DLL 未锁定，直接部署成功；备份 `DeploymentBackups\FishingExpanded-20260816-115228-pre-BATCH068v2`（CDCCA63F... 全套），源/目标 DLL 哈希一致，i18n×2+manifest 源/目标一致，config.json 未触碰（F7F2E2FB... 游戏写入版）） |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单文件边界修改，主线程已掌握调用链；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-068-1/2 | 实施中 | 根因修复（用户确认方案 A） | `经验基数补偿` 日志（每成功 1 条） | fish_selftest 5 条只读断言 + 真实钓鱼 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-068-1 负数等级经验减少 | R0 完成 | 构建 + 真实验收 |
| FE-068-2 力竭衰减经验减少 | R0 完成 | 构建 + 真实验收 |
| FE-068-3 经验倍数膨胀（×50→线性×5~×20） | R0 完成 | 构建 + 真实验收 |

> 机制断言：本批为用户确认的行为修正（方案 A），根因已由原生契约 + 源码双重证实；竞争解释表见 RC-01。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01 | fish_selftest | 通过 / 未执行 | 经验重算 5 条 + 钳制 5 条 + 倍数 7 条断言 |
| 2 | CAT-01 / FE-068-1 | 真实钓鱼（难度等级 -10 的鱼，如金枪鱼 70） | 通过 / 未执行 | 日志 `经验基数重算`；经验 = 原生水平（26/完美 62），且 `fish_info` 无等级变化异常 |
| 3 | CAT-01 / FE-068-2 | 真实挑战 Legend II（原生 110）等级 0，非挑战鱼饵拖 15 分钟成功 | 通过 / 未执行 | 日志 `经验基数重算` 传入难度 80 < 原生 110；经验 = 39（原生水平） |
| 4 | CAT-01 / FE-068-3 | 真实钓鱼（金枪鱼难度等级 50 与 100） | 通过 / 未执行 | 日志 `经验倍率` ×12 与 ×20（非旧 ×25/×50） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `F5DA3733CBA06B8079C98F40F671BA2422B935CD4A45D2B6F3AE3B2E297F8895`（已部署，2026-08-16 11:52，源/目标一致） |
| 部署前安装 | `CDCCA63FA36E7C940DC8732EA71F719F3D113328E2BB0DFBE8E871EA74385993`（BATCH-066+067；备份 `DeploymentBackups\FishingExpanded-20260816-115228-pre-BATCH068v2`；另一备份 `...-080128-pre-BATCH068` 同版） |
| 部署文件与目标 | `D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`（源/目标哈希一致）；i18n×2+manifest 源/目标一致（default=AEDE48.../zh=FECE56.../manifest=8404ED...）；config.json 未触碰（F7F2E2FB...，游戏已自动补 `EnableFestivalFishingMods=false`） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-068-1 | 负数难度等级的鱼钓起经验少于未装模组（-10 级约少一半） | 0 | 无（首报，源码+原生证据证实） | RC-01 | R0 完成 | 等级 -10 金枪鱼经验=26 |
| FE-068-2 | 扩展传奇拖过力竭节点（15 分钟）后经验少于未装模组 | 0 | 无（首报，同上） | RC-01 | R0 完成 | Legend II 等级 0 拖 15 分钟经验=39 |

## RC-01 根因卡

- 根因状态：已证实（原生契约 + 源码取证）
- 第一处分歧：原生经验公式的难度输入被本 Mod 调小——①构造边界 `___difficulty *= GetDifficultyMultiplier(level)`（负数 0.5~0.95，BobberBarPatches.cs:510）；②力竭节点 `___difficulty = data.EffectiveDifficulty`（非挑战鱼饵降到 80，690-697）；成功回调（BobberBar 354-361）把该字段传入经验公式 → 经验基数变小；`GetExperienceMultiplier` 负数=1 不补偿。
- 决定性证据：`_analysis\StardewValley.FishingRod.decompiled.cs:1188-1203`（经验公式含 num3/3）；`_analysis\StardewValley.BobberBar.decompiled.cs:354-361`（成功回调传 (int)difficulty）；`BobberBarPatches.cs:510/690-697`（两处 difficulty 写入）；`_analysis\Fish-data-extracted.txt:69-73`（扩展传奇原生难度 95/85/110/80/100，>80 可被力竭压到原生以下）。
- 竞争解释表：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态（仍成立/已排除） |
|---|---|---|---|
| A：传入难度被调小 → 原生经验公式算少 | 负数/力竭后经验 = f(调整后或有效难度) < f(原生难度) | 510/690-697 行直接证实字段写入；354-361 证实传入 | 已证实（根因） |
| B：经验倍数为负或 0 导致减少 | 经验 ×0 或负 | `GetExperienceMultiplier`：level≤0 → 1，永不 <1（DifficultyCalculator.cs:43-47） | 已排除 |
| C（外部）：原生经验公式基于其他输入（尺寸/数量） | 调整难度不影响经验 | 1188-1203 公式只含 num2(品质)/num3(难度)/宝箱/完美/Boss | 已排除 |
| D：每日收获限额误触发 | 正常鱼经验=0 | HarvestLimited 仅 Mod 鱼/非鱼类超 333/天（BATCH-061 设计内） | 已排除（设计内） |

- 最后已知正常版本/行为：BATCH-038 引入力竭 + 构造难度倍数起即存在（2026-08-10）。
- 过去失败方案及为何失败：无（首报）。
- 唯一所有者：经验结算 = `GainExperience_Prefix`（增益边界）+ 原生 `doPullFishFromWater`（基数）；难度字段写入 = `BobberBarPatches` 构造/Update。
- 最短区分动作：补偿实现后 fish_selftest 5 条断言 + 真实负数等级钓鱼日志核对。
- 诊断标签/触发窗口：无新增诊断（复用 `经验基数补偿` 日志，每成功 1 条）。
- 诊断是否只读且不改变行为：N/A（本批为行为修改）。
- 诊断构建/部署与哈希：N/A。
- 等待玩家返回的日志及位置：N/A（代码侧结束前不等待）。
- 日志返回后的成功/失败判据：N/A。
- 反证记录：无。

## RC-01 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 经验基数补偿 | 各玩家本地结算（IsLocalPlayer 门） | GainExperience_Prefix → 原生 gainExperience | pending 按玩家键隔离 | 无新增 | ClearPending 已有 | fish_selftest 纯函数断言 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 清理 |
|---|---|---|---|---|---|---|
| pending 经验事实（原生难度/品质/标志） | PullFishFromWater_Prefix | 本 Prefix | GainExperience_Prefix | 每成功 1 次 | 消费后 ExperienceAdjusted | ClearPending |

本批**无新增持久状态、无存档字段**；只加 pending 内存字段（消费即弃）。

## 原生完整调用链

- 当前安装 DLL：`CDCCA63FA36E7C940DC8732EA71F719F3D113328E2BB0DFBE8E871EA74385993`（BATCH-066+067）
- Expanded 接管入口：`FishingRod.pullFishFromWater` Prefix（记录事实）；`Farmer.gainExperience` Prefix（经验倍数 + 本批补偿）
- 原生上游：BobberBar 成功 → `pullFishFromWater(fishId, fishSize, fishQuality, (int)difficulty, treasureCaught, wasPerfect, fromFishPond, setFlagOnCatch, isBossFish, numCaught)`（事件转发）→ `doPullFishFromWater`（1188-1203 经验公式）
- 原生提交方法：`farmer.gainExperience(1, num5)`
- 后续回调/同步：`NetEventBinary` 分发；经验在 Poll 阶段（TryBeginExperienceAdjustment 消费）
- Harmony 拦截点：PullFishFromWater_Prefix（本批扩展签名）、GainExperience_Prefix（本批加补偿）
- 第一处分歧：经验基数与"被本 Mod 调小的传入难度"挂钩
- 尚未读取或仍不确定的环节：无
- 旧版只读参考及可接受差异：BATCH-038 力竭反编译与本批无冲突

## R0

- 修改第一处错误决策（两处）：①`GainExperience_Prefix` 在 `PassedDifficulty < NativeDifficulty || PassedDifficulty > 120` 时重算 `howMuch = GetNativeExperience(q, GetExperienceDifficulty(passed, native), 宝箱, 完美, Boss)`（复刻原生公式 + 钳制），再走既有经验倍数逻辑；②`GetExperienceMultiplier` 改为分段：1~10 级旧公式（10 级 = ×5），10~100 级线性 `round(5 + (level−10)×15/90, AwayFromZero)`（100 级 = ×20），≥100 封顶 20，负数 = 1。
- 新权威入口：不变（GainExperience_Prefix + GetExperienceMultiplier）；新增纯函数 `GetExperienceDifficulty`（钳制 [原生,120]）+ `GetNativeExperience`（复刻原生公式）+ 常量 `ExperienceDifficultyCap=120`（DifficultyCalculator）。
- R1 待删除旧路径：无（重算是新增分支，不替代既有逻辑；倍数公式原地修改）。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 无（新增分支） | — | — | — |

- 修改前写入者数量：1（GainExperience_Prefix）→ 修改后：1（不变，仍唯一边界）
- 运行时代码新增/删除：净增约 35 行（1 纯函数 + 6 个 pending 字段 + Prefix 记录 + 补偿分支 + 日志 + 断言）
- 净增长理由：补偿需精确复刻原生公式（治理可观测性/可测性要求），不新增状态所有者

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 负数等级：经验 = f(原生难度)；力竭后有效难度 < 原生：经验 = f(原生)；调整后难度 >120：经验 = f(120)；倍数 10 级 ×5、100 级 ×20 线性 | 经验 < f(原生)；经验基数无上限爆炸；100 级倍数 = ×50 | 纯函数断言 + 日志 |
| 本地主屏/副屏 | 各玩家本地结算 | 串玩家 | IsLocalPlayer 门 |
| 远程与四人混合 | 同左 | 跨玩家写入 | 同上 |
| 普通鱼/非鱼类/鱼王 | 鱼王豁免路径不变（不走 pending）；非鱼上限 8 内负数同样补偿 | 鱼王经验被改 | 鱼王分支提前 return |
| 调整后难度 ∈ [原生, 120] | 现状完全不变（基数=f(调整后)×倍数） | 中间区间经验变化 | 重算条件不成立 |
| 力竭但有效难度仍 ≥ 原生（≤120） | 现状不变 | 补偿误触发 | 条件不成立 |
| 每日限额 | 经验=0（设计） | 补偿覆盖限额 | HarvestLimited 分支在补偿前 return |
| 挑战鱼饵 | 难度不衰减 → 传入=开局调整后；>120 时按 120 重算 | 挑战鱼饵经验无上限 | 690-692 挑战不衰减 |
| 倍数曲线 1~10 级 | 1,1,2,2,3,3,4,4,5,5 现状逐级不变 | 低等级经验变化 | level ≤ 10 走旧公式 |
| 倍数曲线 11~99 | 线性 5+(level−10)/6 取整（13 级=6、50 级=12） | 跳变/断崖 | 纯函数断言 |
| 倍数 ≥100 | 封顶 20（等级上限 100，防御性） | 超 20 | level ≥ 100 返回 20 |
| 换日/重载/标题/断线 | 无新状态泄漏 | — | pending 消费即弃 |
| 性能最坏情况 | 每成功 1 次多一次比较 + 至多一次重算 | 逐帧输出 | 无新增频率 |
| 已验收相邻回归 | BATCH-060/062 经验倍数（≤10 级）、BATCH-067 等级增益/掉星 | 倍数逻辑被绕过 | 重算后继续走倍数分支 |

## 构建、部署与集中测试

- 构建结果：待执行
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
