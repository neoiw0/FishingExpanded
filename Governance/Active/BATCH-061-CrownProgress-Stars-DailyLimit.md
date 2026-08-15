# FishingExpanded 根因批次：BATCH-061 皇冠进度文案 + 非鱼类星星 + 每日收获限额

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260815-08 |
| 当前活动类别 | CAT-04（用户 2026-08-15 指令：非鱼类/其他 Mod 鱼图鉴改回早期金星画法 `Game1.mouseCursors (346,392,8,8)`；皇冠仅可计数 61 原生鱼） |
| 整合候选状态 | 064l 已构建/已部署（`FCC6622006BF948A576CD727B1D707726A91C28721BAC3B12BC9CA113B37A0B8`，2026-08-15 18:30，备份 pre-BATCH064l=AD0665B2...） |
| 当前权威活动卡 | 本卡（BATCH-061，扩展覆盖 BATCH-062/063/064 星星素材迭代） |
| 反证计数（星星素材链） | 素材审美迭代：062"像鱼"、063"条状"、064c"下面缺一点"；根因相关试错：064f/g/h（原生半透明黑=白矩形）→ 064i（Color.White 覆盖=暴露物品本色图标）→ 064j（图标透明化）→ **用户实测：八级绿藻图标消失但"矩形还在"** → 新证据：矩形不是原生图标绘制；候选=我们星星纹理（13x10 区域 128/130 不透明=缺角矩形形状）或其他绘制 → 加侦测 |
| 根因状态 | **部分证实，部分未证实**：已证实原生对 fishCaught 无记录条目画 Black*0.2f 半透明白（trash_item 不记录）；**未证实**：绿藻（153，Fish 数据，能触发小游戏）图标透明化后矩形仍在——矩形可能不是原生 item.draw 绘制，而是我们的星星纹理本身（13x10 区域 128/130 不透明，缺左上/左下角，与用户描述"缺角的矩形"吻合）或原生其他绘制（Utility.drawWithShadow 阴影已证实随 color 透明，排除） |
| 本轮用户问题范围 | 用户 2026-08-15 确认：①图鉴皇冠行改"鱼竿手感增强(10/61)"；②非鱼类达到等级上限 8 级获得星星，描述行"无手感增强"；③有小游戏的鱼保持皇冠；④每日收获限额 333；⑤数量倍数=难度等级×1；⑥星星素材迭代；⑦**用户实测反馈：测试八级绿藻——绿藻图标消失但星星周围矩形仍在；未到 8 级的不可触发小游戏物品不受影响；用户建议"同时放置很多侦测代码"**。**用户最新指令（2026-08-15）**：非鱼类与"其他模组的鱼"达到等级后不给皇冠而给星星；当前星星效果不如早期图鉴星星，明确要求找回早期画法——已定位 = `0020e30` 提交（BATCH-029 改皇冠前）：`Game1.mouseCursors (346,392,8,8)`，图标左上角 `+3/+3` 的 20×20 `Color.Gold` |
| 本轮纳入 Case | FE-061-1 皇冠行进度；FE-061-2 非鱼类星星；FE-061-3 每日收获限额 333；FE-062-1 数量=等级×1；FE-062-2 星星贴图；FE-062-3 buff 暂停静态核对（通过）；FE-064-1 星星素材定稿（诊断中）；FE-064-2 星星画法回退早期金星；FE-064-3 其他 Mod 鱼改星 |
| 共享第一处分歧与所有权链证据 | 皇冠/星标唯一存储=`DifficultyManager.CollectionStars`；数量唯一结算边界=`FishingRodPatches.CreateFish_Postfix`；经验唯一结算边界=`FarmerFishingLevelPatches.GainExperience_Prefix`；等级/星标唯一写入者=`DifficultyManager.RecordSuccess`；图鉴展示=`CollectionsPagePatches`（Prefix 透明化 + Postfix 画星星，唯一绘制者）；**已排除**：Utility.drawWithShadow 阴影随 color.A 透明（IL 601611-601619 证实 Black*intensity*(A/255)）；**待区分**：星星纹理内容（padding 是否全透明）vs 原生其他绘制 vs 其他 Mod/UI |
| 排队或暂不纳入 Case | BATCH-059A/060 待真实验收（本卡不重复修改） |
| 本轮准入证据 | 用户 2026-08-15 逐条确认 + 澄清；064e 实测（贴图 704x2256）；064f/g/h/i/j 目视反证链；**064j 实测（新证据）**：八级绿藻图标透明化后"矩形还在"，未到 8 级条目不受影响；用户建议加侦测代码 |
| 现有实现复核 | 结算链已读；**064l 最终实现**：Draw_Postfix 对非可计数条目画早期金星（`Game1.mouseCursors (346,392,8,8)`，20×20 `Color.Gold`），可计数条目画皇冠；CreateDescription 按 `IsCountableFish` 分行（皇冠"鱼竿手感增强(N/61)"、星星"无手感增强"）；非鱼类图标透明化与 064k 诊断代码已按用户指令全部退回 |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`、`Source\Patches\FishingRodPatches.cs`、`Source\Patches\FarmerFishingLevelPatches.cs`、`Source\Patches\CollectionsPagePatches.cs`、`Source\Services\HUDNotifier.cs`、`Source\ModEntry.cs`（DayStarted/self-test/dumpstars 诊断）、`Source\i18n\default.json`、`zh.json`；文档：GAME-DESIGN.md、WIKI.md、TESTING-GUIDE.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 61 可计数池；鱼王豁免；助战概率；存档结构；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | ①`collections.crownControl` 带 `{{crownCount}}/{{crownTarget}}`（可计数皇冠鱼）；②非鱼类与其他 Mod 鱼"无手感增强"+早期金星（`Game1.mouseCursors (346,392,8,8)`，20×20 `Color.Gold`）；③每日收获限额内存计数 + CreateFish_Postfix 置 Stack=0 + gainExperience 前缀 howMuch=0 + `hud.dailyLimit` 每类每天首次；DayStarted 重置 |
| 可观测性决定 | 064l 已删除全部临时图鉴诊断（`[diag-item]`/`[diag-star]`/透明化）；`fish_dumpstars` 与 dump 目录仍保留待验收后清理；无逐帧输出 |
| 自动化验收决定 | `fish_selftest` 增：限额判定、计数超限、Reset 清空；真实钓鱼各场景；星星：图鉴非鱼类目视 + 诊断日志区分矩形来源 |
| 当前类别阶段 | CAT-01 已部署（BATCH-061，5EBE103C 12:39）；CAT-02 已部署（BATCH-062，B9FFB04F 13:22）；CAT-03：064e 诊断 → 064f/g/h 试错 → 064i 覆盖 → 064j 透明化 → 064k 诊断版；**CAT-04（当前）：星星回退早期金星 + 其他 Mod 鱼改星（AD0665B2 17:51 部署）→ 064l 退回透明化与诊断（FCC66220 18:30 部署）** |
| 当前工作树 | 完整修改（BATCH-033~060 混合未提交基线 + 本类别修改） |
| 本轮统一构建 | CAT-01：`5EBE103C...`；CAT-02：`B9FFB04F...`；CAT-03：064e=`C2A477D1...`、064f=`04775699...`、064g=`9FF3E143...`、064h=`83923C22...`、064i=`1325E957...`、064j=`ED5712F3...`、064k=`CB2FB4AB...`（诊断版）；**CAT-04：`AD0665B25E328466223A9D3C52AE4BFD68D200C361C43969DFF0E0938E5AD479`（星星回退早期金星+Mod 鱼改星，0 警告 0 错误，2026-08-15 17:51 已部署）** |
| 唯一下一步 | 用户重启游戏 → 打开图鉴 fish tab → 验证：非鱼类/其他 Mod 鱼显示早期小金星（20×20 金色）、描述"无手感增强"；可计数 61 原生鱼仍显示皇冠与"鱼竿手感增强(N/61)" |
| 已消费动作 | 064e 部署+实测；064f/g/h 试错；064i 部署+目视；064j 部署+用户实测（绿藻图标消失矩形仍在，新证据）；Build/Deploy:ROUND-20260815-07:CB2FB4AB...（064k 诊断，17:50）；**Build/Deploy:ROUND-20260815-08:AD0665B2...（CAT-04，17:51，备份 `DeploymentBackups\FishingExpanded-20260815-175137-pre-BATCH064-2`，源/目标 DLL+i18n 哈希一致，config.json 未触碰）**；**064l 退回（主线程，用户指令"非鱼类图标透明化是自作主张，全部退回"）：删除 Draw_Prefix 透明化 + 064k 诊断代码，保留 Codex 星星改动 (346,392,8,8)，Build/Deploy:ROUND-20260815-08:FCC6622006BF948A576CD727B1D707726A91C28721BAC3B12BC9CA113B37A0B8（18:30，备份 pre-BATCH064l=AD0665B2...）** |
| 重复执行授权 | 无 |
| 本轮用户问题范围 | 用户 2026-08-15 确认：①图鉴皇冠行改"鱼竿手感增强(10/61)"（已有可计数皇冠/61）；②非鱼类（垃圾/藻类等）达到等级上限 8 级获得星星（规则同皇冠：流动金色/×1.2 照抄，非鱼类达不到 95/100 天然不触发；星星不计入 α/助战），描述行显示"无手感增强"；③有小游戏的鱼（含 Mod 鱼）保持皇冠；④每日收获限额：仅原版 61 可钓真鱼不限，其他（每种 Mod 鱼、每种非鱼类）按物品单独每天最多 333 个 + 333 对应经验；达到上限后仍可钓（小游戏/难度等级/星星照常），数量=0 且经验=0；左下角每类每天首次超限提示一次，文案精准且符合游戏场景；⑤BATCH-062：数量倍数=难度等级×1（100 级=100 条）；⑥星星素材多次迭代（(144,16) 像鱼 → (280,188,94,84) 条状 → (276,183,102,106) 四周扩 → (276,183,102,105) 只扩左右上，用户确认"看到星星"），最终用户建议用"图鉴成就标签页的星星" |
| 本轮纳入 Case | FE-061-1 皇冠行进度；FE-061-2 非鱼类星星；FE-061-3 每日收获限额 333；FE-062-1 数量=等级×1；FE-062-2 星星贴图换图鉴同源金色星；FE-062-3 buff 暂停静态核对（通过）；FE-064-1 星星素材版本感知定稿 |
| 共享第一处分歧与所有权链证据 | 皇冠/星标唯一存储=`DifficultyManager.CollectionStars`（可计数=∩61 池，非鱼/Mod 鱼入池不影响）；数量唯一结算边界=`FishingRodPatches.CreateFish_Postfix`；经验唯一结算边界=`FarmerFishingLevelPatches.GainExperience_Prefix`（`TryBeginExperienceAdjustment`）；等级/星标唯一写入者=`DifficultyManager.RecordSuccess`；图鉴展示=`CollectionsPagePatches`；原版 61 池=CountableFishIds（Fish-data-extracted 非 trap 非藻类条目，恰为"原生触发钓鱼小游戏的鱼"白名单）；**贴图版本新事实（BATCH-064e 实测）**：用户环境 mouseCursors=704x2256（uiScale 1.35<2），原生成就 tab 图标 (656,80,16,16) 在该版有效（含星星+棕底 UI）；程序像素分析标定星星本体=(659,83,13,10)（13x10，框内仅 2 个顶角缺口棕底像素）；1024 高分版 (656,80) 区域早期验证为全空，不适用 |
| 排队或暂不纳入 Case | BATCH-059A/060 待真实验收（本卡不重复修改） |
| 本轮准入证据 | 用户 2026-08-15 逐条确认 + 澄清（333 按物品单独、超限可钓但数量经验为 0、经验随数量停、左下角提示、星星图标优先原生素材）；BATCH-064e 实测：fish_dumpstars 日志（704x2256/viewport 1707x960/uiScale 1.35）+ 用户确认 tab-achieve-656-80.png"里面有星星，但是包围在 UI 里" |
| 现有实现复核 | 结算链已读：pullFishFromWater Prefix（PendingFishData）→ CreateFish_Postfix（数量）→ caughtFish Postfix（等级/星标/巨型鱼）→ gainExperience Prefix（经验倍数）；藻类/垃圾=IsNonFish（GAME-DESIGN §6.2 确认，BATCH-031 核对一致）；成就 tab 原生坐标=`_analysis\CollectionsPage.full.cs:126`=(656,80,16,16)（低分版有效；1024 高分版该区域全空——高分版成就 tab 坐标未标定，当前保持大金星分支） |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`、`Source\Patches\FishingRodPatches.cs`、`Source\Patches\FarmerFishingLevelPatches.cs`、`Source\Patches\CollectionsPagePatches.cs`、`Source\Services\HUDNotifier.cs`、`Source\ModEntry.cs`（DayStarted/self-test/dumpstars 诊断）、`Source\i18n\default.json`、`zh.json`；文档：GAME-DESIGN.md、WIKI.md、TESTING-GUIDE.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 61 可计数池；鱼王豁免；助战概率；存档结构（限额为内存态，DayStarted 重置，零存档字段）；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | ①`collections.crownControl` 带 `{{crownCount}}/{{crownTarget}}`（皇冠鱼，含 Mod 鱼）；②非鱼类 `collections.noCrownControl`"无手感增强"+星星图标（BATCH-064e 定稿：低分版成就 tab 星星本体 (659,83,13,10)，20px；高分版保持大金星 (276,183,102,105)），获得条件=RecordSuccess 后非鱼且 newLevel≥8（幂等）；③`IsDailyHarvestLimited`（!CountableFishIds）→ 内存计数 `ConsumeDailyHarvest`（玩家×物品）→ CreateFish_Postfix 超限置 Stack=0 + `HarvestLimited` 标志 → gainExperience 前缀 howMuch=0；`MarkDailyLimitNotified` 每类每天首次提示 `hud.dailyLimit`；DayStarted 重置 |
| 可观测性决定 | 限额超限 1 条 Info 日志（含当日计数）；星星获得 1 条 Info；提示每类每天首次；`fish_dumpstars` 一次性诊断（贴图尺寸日志 1 条 + 5 个 PNG 导出，仅手动触发）；无逐帧输出 |
| 自动化验收决定 | `fish_selftest` 增：限额判定（原版 false/Mod 鱼 true/非鱼类 true）、计数超限（333→334 超限）、Reset 清空；真实钓鱼：非鱼类升到 8 级图鉴出现星星+"无手感增强"；皇冠鱼显示"鱼竿手感增强(N/61)"；Mod 鱼/非鱼类第 334 个数量=0 经验=0+左下角提示一次；原版鱼无限制；次日重置。存档影响=无（内存态）。星星素材：fish_dumpstars 日志贴图宽度 + 玩家目视成就 tab 星星 |
| 当前类别阶段 | CAT-01 已部署（BATCH-061，5EBE103C 12:39）；CAT-02 已部署（BATCH-062，B9FFB04F 13:22）；CAT-03 部署链 062b/063/064/064c/064d 全部已部署并目视迭代；064e 诊断版（C2A477D1，15:53）已部署并实测（贴图 704x2256）；064f（04775699，星星=低分版 (659,83,13,10)）16:13 部署后目视：**大小和位置对，但边缘一圈有脏色**；064g（9FF3E143，运行时抠图：星星色系保留、棕底/外框置透明）16:25 部署后目视：**星星外有一圈"矩形半透明白色"**（根因=星星顶满纹理边界+LinearClamp 采样重复边缘浅金色）；064h（83923C22，星星四周 +2px 透明 padding 居中）16:45 已部署待目视 |
| 当前工作树 | 完整修改（BATCH-033~060 混合未提交基线 + 本类别修改） |
| 本轮统一构建 | CAT-01：`5EBE103C...`（BATCH-061，12:39 已部署）；CAT-02：`B9FFB04F...`（BATCH-062，13:22 已部署）；CAT-03 系列：062b=`E5FA3110...`、063=`AE9EAA02...`、064=`5D7CE434...`、064c=`C3EBD532...`、064d=`482CE44C...`（目视版）、064e 诊断=`C2A477D130DD1B02691F9AC8A7051CC5AAF20F068AB6E36E1009FC4DC4861732`（已部署并实测）、064f=`047756996BBCF093AAA4C389B49DD409EF85DD111E0253FE38FB2B467EB38D3C`（16:13 部署，目视：边缘脏色）、064g=`9FF3E143CE2A8EA0CD73D2D3834631D6960143B9E834EF04B56D78EA0E1C3E85`（16:25 部署，目视：矩形白边）、064h=`83923C229FE6FD25A2BB27A85CC2917AE0A976E743AF4D80F6F27E0FB605B5BE`（padding 去白边，0 警告 0 错误，**16:45 已部署**） |
| 唯一下一步 | 用户重启游戏 → 图鉴非鱼类（升到 8 级）目视星星：应显示纯金色星星，无棕底、无白边 |
| 已消费动作 | `Deploy:ROUND-20260815-06:C2A477D1...`（064e 诊断版，15:53）；`Runtime:ROUND-20260815-06:fish_dumpstars:C2A477D1...`（实测：贴图 704x2256/viewport 1707x960/uiScale 1.35；tab-achieve-656-80.png 用户确认含星星被 UI 包围）；`Build:ROUND-20260815-07:04775699...`（064f）；`Deploy:ROUND-20260815-07:04775699...`（064f，16:13）；`Runtime:ROUND-20260815-07:目视:04775699...`（用户：大小位置对、边缘一圈脏色）；`Build:ROUND-20260815-07:9FF3E143...`（064g）；`Deploy:ROUND-20260815-07:9FF3E143...`（064g，16:25）；`Runtime:ROUND-20260815-07:目视:9FF3E143...`（用户：星星外一圈"矩形半透明白色"）；`Build:ROUND-20260815-07:83923C22...`（064h）；`Deploy:ROUND-20260815-07:83923C22...`（064h，16:45，源/目标哈希一致，备份 `DeploymentBackups\FishingExpanded-20260815-1645-pre-BATCH064h`=9FF3E143...，config.json 未触碰） |
| 重复执行授权 | 无 |

## BATCH-062 修订（2026-08-15 用户指令）

- FE-062-1 数量倍数：`GetQuantityMultiplier` = 难度等级×1（0/负=1 保底，100 级=100；覆盖 BATCH-060 的 round(level×0.5)）；`GetExperienceMultiplier` 与数量**解耦**，保持 BATCH-060 公式 max(1, round(level×0.5))（用户只改数量；若经验也要改请告知）
- FE-062-2 星星贴图：非鱼类星星从 mouseCursors_1_6 (236,205,19,19)（目视像鱼）改为 **Game1.mouseCursors (144,16,16,16) 图鉴成就页同款金色星星**（`_analysis\CollectionsPage.full.cs` 成就图标同源），24/16 缩放；"第二页祝尼魔 6 个星星"位置未能离线确认，成就星为图鉴内可见的金色星，若位置不符请指正
- FE-062-3 buff 暂停静态核对（通过）：原生 `Buff.update` 231-236 行 `!Game1.shouldTimePass()` 兜底；钓鱼小游戏期间时间照常流逝→原生会扣时，`BuffPatches.Update_Prefix`（`id=="food" && activeClickableMenu is BobberBar`）正确拦截；退出小游戏自动恢复；饮料/其他 buff 不受影响；无需修改
- 构建：Release Rebuild ✅（0 警告 0 错误）；DLL `B9FFB04FA4AD368BE16F8E6A044DB4411CDAEE99215771C2E4E03B1023A32293`；反编译核验通过；未部署（DLL 被运行中游戏锁定）

## 方案设计（BATCH-064 高级复核门禁，2026-08-15 完成）

### 旧行为链（用户所见）
1. 非鱼类（垃圾/藻类）钓起 → 原生 `Farmer.caughtFish`（Farmer.decompiled.cs:2997）因 `trash_item` 标签**不记录 fishCaught**
2. 打开图鉴 fish tab → 原生 `CollectionsPage.draw`（full.cs:841）对 fishCaught 无记录条目画 `Color.Black * 0.2f`（半透明黑，未收集剪影）
3. 半透明黑图标叠加在图鉴浅色背景上 → 用户看到"白色半透明矩形"
4. 我们的星星（任何素材版本）画在该条目左上角 → 用户看到"星星 + 周围一圈白色半透明"

### 失败尝试（写入者=CollectionsPagePatches.Draw_Postfix）
| 版本 | 修改 | 用户反馈 | 结论 |
|---|---|---|---|
| 064f | 星星源=成就 tab (659,83,13,10) | 大小位置对，边缘脏 | 星星素材本身 OK |
| 064g | 抠图（金色系保留其余透明） | 矩形半透明白色 | 与素材无关 |
| 064h | +2px 透明 padding | 还是一圈白色半透明 | 与素材无关 |

### 新行为链（目标）
1. 非鱼类条目在原生绘制时**不显示半透明白色矩形**——原生画图标本色（Color.White）
2. 星星保持当前素材（064h 抠图+padding，用户已确认星星本身"大小和位置都对"）

### 修复方案（二选一，待用户确认）
- **方案 A（推荐，纯展示层）**：在 `Draw_Postfix` 非鱼类分支内，先用 `Color.White` 重画一次 `__instance.texture/sourceRect`（覆盖原生半透明黑图标，layerDepth+0.005f），再画星星（+0.01f）。不改存档、不改原生对象、不新增状态；写入者仍唯一（展示层）。
- **方案 B（改条目状态）**：进入图鉴时临时把非鱼类有星星条目的 `name` 中 drawShadow 位改为 true，绘制后恢复——改动原生对象字段，副作用面更大，不推荐。

### 所有权审计
- 图标底色绘制：原生 `CollectionsPage.draw`（唯一）；我们的修复=在其后覆盖绘制，展示层职责归属 CollectionsPagePatches ✓
- 星星绘制：`CollectionsPagePatches.Draw_Postfix`（唯一）✓
- 不写存档（fishCaught/CollectionStars 均不动）✓

### 删除计划
- 修复生效并验收后：删除 `fish_dumpstars` 临时诊断命令与 dump 目录；`GetAchievementStarTexture` 抠图+padding 保留（星星素材定稿）

### 反证澄清
- 星星素材链反证 1-3（062/063/064c）为素材审美迭代（用户目视裁判，正常设计过程）
- 反证 4-6（064f/g/h）为**同一根因**（原生半透明白色矩形），已证实与素材无关——不重复计入星星素材反证；本 Case 的已部署修复被推翻计数按"星星素材"=3 次（062/063/064c），064f/g/h 属同一未证实根因的试错，根因证实后按新方案一次性修复

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-061-1/2/3 | 实施中 | 根因修复 | 限额超限/星星获得日志 | 真实钓鱼 + fish_selftest |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-061-1 皇冠行进度 | 设计已确认 | 实施+真实验收 |
| FE-061-2 非鱼类星星 | 设计已确认 | 实施+真实验收 |
| FE-061-3 每日限额 | 设计已确认 | 实施+真实验收 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `5EBE103C6E2CE3F6FF93DC7D32D1BEA01F65B84B5AF6FA8787D178E8D6B2597D`（2026-08-15 12:39 部署；备份 `DeploymentBackups\FishingExpanded-20260815-123938-pre-BATCH061`=DDC3AE79...；源/目标哈希一致；i18n×2 同步；config.json 未触碰） |
<!-- CURRENT-STATE-END -->

## R0

1. DifficultyManager：`IsNonFishItem(id)`（ItemRegistry Category≠-4）；`DailyHarvestLimit=333` + 内存 `_dailyHarvestByPlayer`/`_dailyLimitNotifiedByPlayer`（玩家→物品）；`IsDailyHarvestLimited`/`ConsumeDailyHarvest`/`MarkDailyLimitNotified`/`ResetDailyHarvest`；`RecordSuccess` 内非鱼 newLevel≥8 → CollectionStars.Add（幂等）
2. FishingRodPatches：`PendingFishData.HarvestLimited`；CreateFish_Postfix 尾段——限鱼类 Consume 计数，>333 → Stack=0 + 标志 + 首次提示
3. FarmerFishingLevelPatches：GainExperience_Prefix 中 `pendingFish.HarvestLimited` → howMuch=0
4. CollectionsPagePatches：皇冠行带 `{{crownCount}}/{{crownTarget}}`；非鱼类分支 `collections.noCrownControl`；Draw 非鱼类画星星（Game1.mouseCursors_1_6 (236,205,19,19)，24/19 缩放，layer 同皇冠）
5. HUDNotifier：`ShowDailyLimitReached(fishId)`（FIFO 队列，`hud.dailyLimit`）
6. ModEntry：DayStarted → `ResetDailyHarvest()`；fish_selftest 增限额/星星纯函数自测
7. i18n 中英：crownControl 带参、noCrownControl、dailyLimit

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 皇冠行无进度旧文案 | 替换为带参模板 | 否 | 无 |
| 非鱼类无标记（空着） | 替换为星星+无手感增强行 | 否 | 无 |
| 非原版鱼无限量 | 替换为 333/天限额 | 否 | 原版 61 池保持不限 |

- 修改前写入者数量：数量=CreateFish_Postfix、经验=GainExperience_Prefix、等级=RecordSuccess、星标=DifficultyManager；修改后：1/1/1/1

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 原版鱼钓获 | 不限数量/经验 | 被限额 | 池判定 |
| Mod 鱼第 334 个 | 数量 0、经验 0、提示一次 | 背包+1/经验+1/重复提示 | 待构建后验证 |
| 非鱼类第 334 个 | 同上（垃圾/藻类按物品单独） | 拖累其他物品计数 | 键=物品 ID |
| 限额后继续钓 | 小游戏/难度等级/星星照常 | 结算/等级被跳过 | 拦截点只在数量/经验 |
| 非鱼类升到 8 级 | 图鉴星星+"无手感增强" | 显示皇冠/手感增强行 | 类别分支 |
| 皇冠鱼 | "鱼竿手感增强(10/61)" 随皇冠数变化 | 固定数字 | 参数化 |
| 次日 | 计数清零可再获 333 | 跨天累计 | DayStarted 重置 |
| 多人/分屏 | 按玩家 ID 隔离 | 串玩家 | 键控 |
| 鱼塘/蟹笼 | 不参与（原生路径外） | 被限额 | fromFishPond 跳过 |
| 鱼王 | 天然豁免 | 参与 | 传奇走独立边界 |

## 构建、部署与集中测试

- 构建结果：待执行（0 警告 0 错误目标）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（默认部署授权）
- 部署状态：待执行
