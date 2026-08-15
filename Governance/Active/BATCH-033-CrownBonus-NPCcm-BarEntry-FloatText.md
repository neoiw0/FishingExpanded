# FishingExpanded 根因批次：BATCH-033 王冠加成·NPC厘米·绿条外·浮标文案

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前唯一执行批次 | BATCH-033（2026-08-08 用户一次提交 5 项确认修改；用户要求先重读全部治理文件并锁定第一因） |
| 本轮用户问题范围 | ① 尺寸进入 NPC 惊叹/收藏页；② 王冠加成 0.5→0.2；③ 难度等级≥80 且鱼在绿条外时自身随机运动不得进入绿条；④ 低难度鱼不显示挑战宣言（第一因：测试命令无门槛写星标）；⑤ 浮木→陷阱浮标/软木塞浮标 |
| 本批次纳入 Case | FE-033-1（NPC 厘米一致）、FE-033-2（加成 0.2）、FE-033-3（≥80 绿条外）、FE-033-4（低难度宣言）、FE-033-5（浮标文案） |
| 共享第一处分歧与所有权链证据 | 各自独立：展示文案（NPC/图鉴/命令）与 BobberBar 运动规则、DifficultyManager 数值分别属于不同所有者，不合并根因；同一批次实现 |
| 排队或暂不纳入 Case | WIKI.md（未跟踪文件）不修改（用户未授权 Wiki 维护） |
| 合并/拆分决定及依据 | 五现象不共享第一处分歧，各自独立验收；同批实现便于一次构建部署 |
| 本轮准入证据 | 用户 2026-08-08 明确确认：1.难度等级≥80；2.只禁止鱼自身随机运动进条、玩家移条照常可抓、跳鱼瞬移不受影响；3.无收藏皇冠不显示宣言；4.收藏页=历史最大尺寸（原生机制）；5.NPC 全部显示厘米；6.授权部署。SMAPI-latest（2026-08-08 21:21）含 fish_addstars 20（20:13）、星之果茶掉落（21:13 #9）、挑战宣言多条（狗鱼/鲷鱼/虹鳟鱼/太阳鱼）等实测证据 |
| 现有实现复核 | 尺寸链路已贯通（BobberBar 调整 fishSize→pullFishFromWater→GiantFishManager→NPC）；原生收藏页已显示“最大尺寸”（fishCaught[id][1]×2.54 厘米）；皇冠绘制与宣言共用 HasCollectionStar（静态一致）；加成加载时按星标数重算（0.2 自动生效旧存档） |
| 允许修改范围 | Source：DifficultyManager.cs、ModEntry.cs、NPCDialogueGenerator.cs、BobberBarPatches.cs、i18n zh/default；文档：GAME-DESIGN.md、BUG-LEDGER.md、TESTING.md、TESTING-GUIDE.md、本卡 |
| 冻结 Case/禁止范围 | 不改 WIKI.md；不改 BATCH-013 历史文案；不覆盖部署 config.json（部署目录无该文件） |

| 字段 | 值 |
|---|---|
| 批次目标 | 5 项用户确认修改一次实现+构建+部署 |
| 可观测性决定 | FE-033-3 复用 BobberBar 构造日志（难度等级）；钳制激活/解除按状态迁移低频 Debug 日志（不逐帧）；FE-033-1/2/5 纯展示文案免诊断（静态合同可证） |
| 自动化验收决定 | FE-033-3：复用真实 BobberBar 小游戏路线 + 现有日志标签；存档影响=无；成功判据=日志/玩家画面中等级≥80 的鱼在条外时自身运动不进入绿条（位置不前进、不贴条边施压）、玩家移条可抓、跳鱼照常；失败判据=鱼自身进条或贴条边；关闭条件=验收完成或用户撤销 |
| 当前阶段 | FE-033-3 第 1 次反证（贴条边）→ 已修正为运动取消 → 重新构建/部署 |
| 当前工作树 | 部分修改（BATCH-032 ADDSTARS 未提交 + 本轮） |
| 最后构建 | BATCH-033 Release Rebuild：26F8A54B...（2026-08-08，0 警告 0 错误） |
| 唯一下一步 | 重新构建+部署修正版 → 玩家复测绿条外规则（不贴条） |

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-033-1 NPC 厘米 | R0/R1 完成（厘米换算） | 已构建 |
| FE-033-2 加成 0.2 | R0/R1 完成（4 处 0.2+重算） | 已构建 |
| FE-033-3 ≥80 绿条外 | 第 1 次反证（部署版贴条边）→ 已修正（运动取消） | 待重新构建部署 |
| FE-033-4 低难度宣言 | 第一因已证实；R0 展示门完成 | 已构建 |
| FE-033-5 浮标文案 | R0/R1 完成（i18n×4+设计文档） | 已构建 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 486C72391D266259B3A505EDA18596FF92E2357669A84A5FA0D20B9312408F8D（FE-033-3 修正版，2026-08-08 22:46 已部署；备份 DeploymentBackups\FishingExpanded-20260808-224628-pre-BATCH033-fix1）；前一版 26F8A54B...（22:32 部署，被 FE-033-3 实测推翻，备份 ...-223246-pre-BATCH033） |
<!-- CURRENT-STATE-END -->

## 分诊证据（2026-08-08）

### FE-033-1 尺寸进入其他系统
- 链路已贯通：`BobberBar` 构造 Postfix 调整 `___fishSize`（今日日志：110/121）→ 原生 `pullFishFromWater` 传调整后值（原生契约 line 361）→ `PullFishFromWater_Prefix` 捕获 → `GiantFishManager.RecordGiantFish`（日志 fishSize 111/79/109/74）→ NPC 冒泡/对话。
- 真实不一致：手持鱼旁原生尺寸文本 = `fishSize×2.54` 厘米（`FishingRod.draw`），NPC 文案直接写 `fishSize`“厘米”→ 数字对不上。用户确认：NPC 全部显示厘米（×2.54 取整）。
- 收藏页：原生 `CollectionsPage.createDescription` Fish 分支已显示“最大尺寸”（`farmer.fishCaught[id][1]`，中文 ×2.54 厘米）；该值由原生 `caughtFish` 记录=本 Mod 调整后尺寸 → 无需新增代码，验收时核对。用户确认按“历史最大尺寸”理解。
- R0：`NPCDialogueGenerator.GenerateFishPraise` 统一换算厘米（唯一展示所有者，冒泡与对话共用）。

### FE-033-2 王冠加成 0.5→0.2
- `DifficultyManager`：225/267/424/509 行 `×0.5f`→`×0.2f`；230/428 日志；238 注释。
- 加载重算（509 行）→ 旧存档自动生效，无需迁移。
- 图鉴/命令/测试文档文案同步。

### FE-033-3 难度等级≥80 绿条外规则
- 原生契约（当前安装 DLL DFE341CA... 重新反编译核对）：`bobberBarPos`/`bobberBarHeight` 字段；条顶=pos-32；条内判定 `pos+12<=top+height && pos-16>=top`（含底部兜底）；鱼位置 `bobberPosition += bobberSpeed + floaterSinkerAcceleration`；目标生成 5 处。
- **第 1 次反证（2026-08-08 用户实测）**：用户此前已显式否决“每帧退回”并确认“加判定，只要会进入绿条就移动不生效”（本轮消息原文），但首版实现仍做成“贴条边钳制”（钉回 prevBarTop-12 / prevBarBottom+16，每帧钉在条边=用户否决的每帧退回变体），鱼一直贴着绿条施压——实现未遵循用户显式指令，记录为流程教训。修正=判定命中（本帧前在条外+位移会进入条）时：位置回到本帧开始处（运动不生效）+ 取消朝向条的目标与速度（不持续施压）+ bobberInBar 同步置 false（防误计脱杆）+ 状态迁移低频 Debug 日志（钳制激活/解除）。修正版 DLL 486C7239... 已部署（2026-08-08 22:46）。
- R0：只在 BobberBarPatches 现有 Prefix/Postfix 内实现，不新增 Transpiler：
  - Prefix：等级≥80 时记录本帧前鱼位置/条边界/条内状态；高难度鱼跳瞬移置位 `JumpAppliedThisFrame`。
  - Postfix：等级≥80 且本帧前在条外且非跳鱼帧，鱼自身位移越过条边界（位移量>0.5 区分玩家移条）→ 位置钉在条外同侧边缘（上侧 top+15、下侧 bottom-11），该段运动不生效。
  - 条内（含玩家移条覆盖）不受影响；跳鱼瞬移不参与；鱼王无 InstanceData 天然豁免；非鱼类上限 8 天然不参与。

### FE-033-4 低难度宣言（第一因已证实，2026-08-08 用户要求核对代码）
- 用户断言难度 -10~0 本来就不可能有星标皇冠 → 代码证实：真实钓获路径 RecordHighDifficulty(fishId, adjustedDifficulty, player) 要求 adjustedDifficulty>=120；狗鱼 (O)144 原生 difficulty=60，难度等级 -10~0 时调整后 =30~60，永远达不到 120 → 真实路径不可能给狗鱼新授星标。用户判断成立。
- 第一因：星标写入路径未统一执行 >=120 才授予的门槛。BATCH-032 测试命令 fish_addstar / fish_addstars（RecordHighDifficulty(fishId, player) 2 参重载 / AddCollectionStarsForTesting）无难度检查即可写星标；用户 20:13 运行 fish_addstars 20 直接给狗鱼/鲷鱼/太阳鱼/虹鳟鱼等写入星标（日志 5670 行），20:23 起狗鱼以等级 0、-1…-10 反复触发宣言（日志 8185~21117 行）。
- 展示链：挑战宣言唯一门槛=HasCollectionStar（BobberBarPatches 构造 Postfix → HUDNotifier.ShowStarChallengeNotification），忠实消费非法星标状态；皇冠绘制同样以 HasCollectionStar 为准（用户确认皇冠绘制无问题，不改）。
- 修复（用户确认新规则）：宣言必须同时要求当前难度等级>=1（弱称号区间不显示）→ HUDNotifier.ShowStarChallengeNotification 增加 difficultyLevel<1 提前返回（已改）。
- 备注：测试命令产生的星标仍留在存档（图鉴皇冠会继续画）；干净验收可用 fish_clear confirm 清除。

### FE-033-5 浮标文案
- zh.json 2 处（陷阱鱼饵/浮木鱼饵、陷阱/浮木渔具）；default.json 2 处（Trap Bait/Driftwood Bait×2）；GAME-DESIGN 3 处。统一改为“陷阱浮标/软木塞浮标”（英文 Trap Bobber/Cork Bobber）。
<!-- CURRENT-STATE-END-DUP -->
