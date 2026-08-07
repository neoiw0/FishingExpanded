# FishingExpanded 任务总账

## 接管后的当前状态（2026-08-05）

本文件是当前批次和验收状态的权威总账；历史批次正文保留为证据，不覆盖本节和顶部汇总表。维护入口见 `MAINTENANCE-INDEX.md`。

### 用户最终确认的设计门禁

1. 0级也应用全局20%/1%蓄力槽保护。
2. Mod数量倍数不改变钓鱼动画数量；数量进入背包或原生 `ItemGrabMenu` 溢出路径时才转换。原生特殊鱼饵的多鱼机制保留。
3. 完美/脱杆奖励先按 `+10/+5/+2/+1` 计算，再限制在当前称号区间，最终等级不能跨区间。
4. 所有同时生效的蓄力槽减速规则取最强效果，即适用倍率中的最小值。
5. 五只鱼王除特殊提示外不参与难度、数量、品质、奖励、技能加成、称号、视觉、NPC和20%/1%全局蓄力槽保护。
6. 鱼王 ID 按原生物品 ID 归一化，`163`、`(O)163` 和 `(o)163` 视为同一只鱼王。
7. 高难度星标和 `+0.5` 钓鱼条长度额外加成只在成功钓起后获得；失败或仅进入小游戏不获得。
8. 难度、星标和隐藏加成按玩家独立保存；在线联机主机和每位农场客都必须安装本 Mod。
9. 非鱼类达到8级后，后续成功的实际难度收益为 `+0`。
10. 已有图鉴星标的鱼进入小游戏时只显示一次随机挑战宣言：称号尊敬词每级3种、毅然决然词50种、应战说法20种；不修改任何状态。
11. 小游戏出现时，非鱼王按 `难度等级 / 10 > 当前有效钓鱼等级` 显示升级建议；若同时大于当前钓鱼等级两倍，在建议开头增加“不可能的高难度挑战”提示。
12. 视觉放大只作用于真鱼：从水里飞出的飞行动画（中心锚点保持飞行轨迹）、落地后立在玩家身边的真鱼（底边中点锚点）和手持鱼（底边中点锚点）；小游戏鱼标与结算面板示意图保持原生大小（2026-08-06 用户确认）。
13. 图鉴“钓鱼条长度额外加成”只在对应鱼已获得收藏星标时显示 +0.5，按鱼种独立展示，不显示全局累计加成（2026-08-06 用户确认措辞）。

### 当前维护门禁

- 历史 v0.5.2 记录包含旧构建/部署事实；当前源码 `0.5.10`。2026-08-06 用户实测反馈两项：图鉴加成显示错误（BATCH-025）、小游戏鱼标与结算面板示意图不应放大（BATCH-024 R0 范围修正）；当前安装已替换为修复候选 `2B0EB0F1...`（候选 `B8BDAEFA...` 部署后 coreclr 启动崩溃，二分定位到位置补偿注入的 `Add` 指令，修复为消费式 `AdjustLandingFishPosition`，详见 BATCH-024 卡“启动崩溃与二分定位”）；部署启动核验通过；2026-08-06 真实画面验收通过（钓鱼过程所有尺寸相关现象、图鉴逐鱼加成显示），日志封存 `RuntimeEvidence\20260806-RUNTIME-ACCEPT-01`（SHA256 7B231460...）；游戏 DLL `DFE341CA...` 未漂移；每个现象必须独立标记通过、失败或未执行。
- 当前优先验证：BATCH-026 线性缩放数值（0/25/50/100级）、视觉放大、原生 `CreateFish` 数量转换/ItemGrabMenu 溢出、单次结算、脱杆收益与区间封顶、0级全局蓄力保护、经验倍率、鱼王完整豁免、成功后星标/开局宣言、钓鱼等级建议和多人隔离。
- BATCH-029（2026-08-06 用户定稿 5 项修改并授权“完成所有代码修改并且部署”）：尺寸数字按等级线性（正 +10%/级、负 -5%/级，与数量倍数解耦）、连续失败 ≥2 次且调整后难度 ≥150 的史诗失败提示、成功额外 `round(调整后难度/50)` 等级、加速度增幅 30% 档（等级 ≥89 保持 100%）、收藏页星标改皇冠（Infinity Crown 贴图 (20,800,20,20)）；首次部署被启动推翻（Transpiler IL 非法）→ 修复版已部署（`26DEE6F2...`），真实验收待用户执行，五项各自独立标记。
- `Source\manifest.json` 与 `Source\FishingExpanded.csproj` 当前均为 `0.5.10`；旧批次版本号只作历史证据，不阻塞本轮部署。
- BATCH-030（2026-08-07 实测回归 + 设计变更，见 `Governance\BATCH-030-FailureRecorded-RankWeakPrompt.md`）：BATCH-029 部署版被实测推翻——失败分支误删 `FailureRecorded=true`，一次失败被重复结算 18~51 次直接扣到底（虹鳟鱼/太阳鱼/狗鱼，其他电脑 D:\K1515 三份日志已封存 `RuntimeEvidence\20260807-BATCH029-REGRESSION-01`）；“稍微强一点的个体”不应出现在星标挑战宣言、零以下胜利应显示该弱称号提示（原 ≤0 跳过）；已排除存档不兼容（新字段缺失默认 0，两份存档加载正常）。修复版已构建并部署（DLL `108C3A2C...`，备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`），真实验收待用户执行，每现象独立标记。
- BATCH-031（2026-08-07 全量设计↔代码核对，见 `Governance\BATCH-031-Design-Consistency-Check.md`）：全部规则与源码逐条一致；明确问题 2 处均为文档层——GAME-DESIGN 4.6 旧措辞 “隐藏钓鱼技能加成”改 “钓鱼条长度额外加成”、2.2 补失败提示优先级（史诗 > 触底 > 负等级 > 普通，与 `ShowFailureNotification` 一致）；同步 TESTING/MAINTENANCE-INDEX 措辞；无运行时代码/i18n 变化，部署核验为哈希一致性确认。
- BATCH-032（2026-08-08 用户确认新增，见 `Governance\BATCH-032-HUDQueue-StarfruitTea.md`）：HUD 提示 FIFO 排队（全部四类+掉落消息排队、同屏同时一条、上限 5 丢最旧、按玩家隔离、原生消息不参与、回标题清空）；星之果茶掉落（难度等级 ≥50 成功钓起后概率 = 等级/4%，例 80 级 20%；掉落 1 瓶 `(O)StardropTea`，走 `CaughtFish_Postfix` 同一防重边界 + 原生溢出菜单；掉落消息 15 条中英文案随机并排队；鱼王/非鱼类豁免）。实现+构建完成（0 警告 0 错误），部署待用户指令，验收项每现象独立标记。

### 2026-08-06 实测反馈分诊

| Case | 玩家可见现象 | 唯一所有者 | 第一处分歧（已证实） | 批次 |
|---|---|---|---|---|
| FE-ISSUE-1 | 图鉴几乎所有鱼都显示“+0.5钓鱼等级” | `CollectionsPagePatches`（展示层） | `CreateDescription_Postfix` 显示全局累计加成 `GetFishingLevelBonus()` 而非每鱼星标 | BATCH-025（实测通过） |
| FE-ISSUE-2 | 小游戏鱼标与结算面板示意图被放大；只有从水里钓起举起的真鱼应放大 | `BobberBarPatches.draw`、`FishingRodPatches.draw`（绘制层） | BATCH-024 R0 按旧设计接入三条绘制路径缩放；用户确认小游戏鱼标与结算面板不缩放 | BATCH-024（实测通过） |
| FE-ISSUE-3 | 鱼视觉尺寸过大：100级≈5.85倍；要求100级=现25级大小且线性增大 | `DifficultyCalculator.GetVisualScale`（唯一公式所有者） | 立方根曲线增长过快；改为线性 1+0.0270843×等级 | BATCH-026（活动） |
| FE-ISSUE-4 | 双人同屏（本地分屏）：副屏玩家的难度/星标/加成/巨型鱼展示会读写主玩家数据（源码证实，未实测） | `DifficultyManager` 单例静态缓存不随分屏切换；BobberBar/CaughtFish/RecordGiantFish 记录入口 | Mod 静态缓存改为按玩家 UniqueMultiplayerID 键控隔离，全部数据路径显式传玩家 | BATCH-027（活动） |

- 两现象第一处分歧与唯一所有者不同，不合并；每个现象独立验收。
- 顺序：两个批次各自独立完成 R0/R1/R2 与 Release Rebuild；2026-08-06 真实画面验收均通过（每现象独立标记），日志封存 `RuntimeEvidence\20260806-RUNTIME-ACCEPT-01`。

### 当前活动 Case（BATCH-024，2026-08-06 更新）

- **现象**：用户实测小游戏鱼标被放大、结算面板内鱼名/尺寸示意图被放大；用户确认只有“胜利后从水里钓起、玩家举起的真鱼”才放大。
- **已证实（当前安装 DLL `DFE341CA...` 反编译核对）**：`BobberBar.draw` 鱼标原生 `(10,10)` 中心原点、`scale=2f`；`FishingRod.draw` 结算面板示意图（第一处鱼图 `Vector2.Zero`、`4f`）与真鱼图（第二处/原生多鱼图 `(8,8)`、`3f`）；`doPullFishFromWater` 飞行动画为原生 `TemporaryAnimatedSprite`，保持原生大小与数量。
- **第一处分歧**：BATCH-024 R0 把缩放接入小游戏鱼标与结算面板示意图，超出用户确认的“真鱼放大”范围。
- **本轮准入**：允许修改 `BobberBarPatches.cs`（删除小游戏鱼标缩放）、`FishingRodPatches.cs`（删除结算面板第一处鱼图缩放与位置补偿，保留第二处/多鱼真鱼缩放）以及对应设计、台账和测试文件；冻结：`ObjectPatches.cs` 手持缩放、`GiantFishManager` 展示事实、鱼获/数量/难度所有权不改。
- **可观测性决定**：删除 `stage=BobberBar.draw` 诊断（路径移除）；新增 `stage=FishingRod.fly` 同键低频记录；保留 `FishingRod.draw`（落地真鱼，底边中点）与手持路径记录；不逐帧输出。
- **自动化验收决定**：复用 `FISH-CLI-01` 全量 `Saves` 沙箱与真实原生钓鱼入口（鱼ID 142、倍数27）；成功：小游戏鱼标恒为原生 2f、结算面板与示意图不缩放、飞行动画约 3.00 倍且中心轨迹不漂移、落地真鱼约 3.00 倍且底边中点不漂移、手持底边中点；失败：小游戏或面板图被放大、飞鱼或落地鱼未放大、锚点偏移、日志刷屏。
- **状态**：R0/R1/R2 完成；候选 `B8BDAEFA...` 启动崩溃已二分定位并修复为 `2B0EB0F1...`，已部署、启动核验通过；2026-08-06 真实画面验收通过（钓鱼过程所有尺寸相关现象）。

### 当前活动 Case（BATCH-025，2026-08-06 更新）

- **现象**：用户实测收集页面几乎所有鱼都显示“+0.5钓鱼等级”，而不是只有已收藏星标的鱼。
- **已证实（源码核对）**：`CollectionsPagePatches.CreateDescription_Postfix` 原先显示全局累计 `GetFishingLevelBonus()`（星标鱼种数 × 0.5）到每一条鱼；`DifficultyManager.HasCollectionStar(fishId)` 已有按鱼判定能力。
- **第一处分歧**：图鉴展示层读取全局累计加成，未按当前鱼种星标独立展示。
- **本轮准入**：允许修改 `CollectionsPagePatches.cs` 描述追加路径及对应治理/测试文件；冻结：难度/星标写入所有权、经验结算、鱼王豁免边界不改。
- **可观测性决定**：图鉴路径不新增诊断；以真实验收打开 Collections 菜单逐鱼核对描述文本为判定依据。
- **自动化验收决定**：复用 `fish_addstar <id>` 与真实收藏品菜单路线（TESTING-GUIDE 3.5 场景4），全量 `Saves` 沙箱；成功：星标鱼显示 +0.5、未星标鱼不显示、鱼王豁免、无全局累计值；失败：任何未星标鱼显示加成、星标鱼不显示、出现全局累计值。
- **状态**：R0/R1/R2 完成；与 BATCH-024 同一 DLL（修复候选 `2B0EB0F1...`）已部署、启动核验通过；2026-08-06 真实画面验收通过（收集页逐鱼加成显示正常）。

### 当前活动 Case（BATCH-026，2026-08-06 更新）

- **现象**：玩家实测确认显示链路正常，但设计数值上鱼过大：要求 100 级鱼的视觉大小 = 现在 25 级的大小，改为线性增大，并用简单加减计算替代立方根。
- **已证实（源码核对）**：唯一公式所有者 `DifficultyCalculator.GetVisualScale`，旧实现 `Math.Pow(倍数, 1/3)`；25级倍数51→旧缩放≈3.7084，100级倍数200→旧缩放≈5.8480；调用点：飞行动画、落地真鱼、手持鱼（`FishingRodPatches`/`GiantFishManager`/`ObjectPatches`），小游戏鱼标与结算面板不经过该公式。
- **R0（新公式）**：`缩放 = 1 + (数量倍数-1) × 0.0136102`，即 0级=1.0、25级≈1.6805、100级≈3.7084（= 旧25级大小）；等价每级线性 +0.0270843。
- **本轮准入**：允许修改 `DifficultyCalculator.cs`（GetVisualScale 公式）、`GiantFishManager.cs`（日志复用公式）及对应设计/台账/测试文件；冻结：数量倍数、经验、品质、`fishSize` 数值、动画数量、锚点合同、鱼王豁免不改；不部署（用户明确先不部署）。
- **可观测性决定**：公式为纯函数，静态合同可完整证明，符合免除条件；现有三条低频视觉诊断直接输出新值，不新增诊断。
- **自动化验收决定**：复用 `FISH-CLI-01` 全量 `Saves` 沙箱与真实原生钓鱼入口；成功：25级≈1.68倍、100级≈3.71倍、0级/负数=1.0、锚点不漂移；失败：出现旧立方根值或锚点偏移。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0警告0错误，产物 SHA256 `77A1937CC1B5726EDE00E26C6BF06572A6E8223DAF48B7B06484FD9F6EDD4C24`，反编译确认线性公式已编译）；不部署。

### 当前活动 Case（BATCH-027，2026-08-06 更新）

- **现象**：双人同屏副屏玩家的难度/星标/加成/巨型鱼展示会读写主玩家数据；用户要求必须支持所有双人模式。
- **已证实（当前安装 DLL 反编译 + 源码核对）**：分屏每位本地玩家独立 Game1 实例，`Game1.player`/菜单/HUD 静态字段随屏幕切换（`LocalMultiplayer` 静态变量 holder）；Mod 自身静态缓存不参与切换 → `DifficultyManager._data` 单例缓存只在主屏加载，副屏读写串到主玩家。`Farmer.modData` 继承自 `Character.modData` 且注册进 NetFields → 联机农场客数据同步进主机存档。
- **R0**：`DifficultyManager` 按玩家键控惰性缓存，全部方法显式传玩家；FishingLevel 加成与鱼王屏蔽按玩家；NPC 反应遍历所有本机玩家。
- **本轮准入**：允许修改 `DifficultyManager.cs`、`BobberBarPatches.cs`、`FishingRodPatches.cs`（CaughtFish/CreateFish 传玩家）、`FarmerFishingLevelPatches.cs`、`CollectionsPagePatches.cs`、`HUDNotifier.cs`、`GiantFishManager.cs`、`ModEntry.cs` 及治理/测试文件；冻结：存档键/JSON 结构、等级公式、区间封顶、鱼王豁免、锚点合同、数量/经验/品质规则不改。
- **可观测性决定**：成功/失败/星标/加载日志已带玩家 ID；不新增逐帧诊断。
- **自动化验收决定**：`fish_*` 命令 + FISH-CLI-01 沙箱做单人回归；双人同屏/联机真实运行验收待执行，每现象独立标记。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（产物 SHA256 `AC7B1408...`，与 BATCH-026 同一 DLL）；不部署；双人实测待执行。
- **委派评估**：`NotBeneficial`（源码重构 + 本机构建/反编译核验由主线程完成，未使用委派执行器）。
- **Closeout**：HEAD `8f799da` 无暂存（未用 `git add -A`）；Release Rebuild ✅ 重核 0 警告 0 错误，产物 SHA256 `AC7B1408...` 与卡一致；未创建提交（未授权、多批次混合来源）。
- **设计-代码审计（2026-08-06 用户要求全量核对）**：`GAME-DESIGN.md` 全部规则与源码逐条匹配（公式、称号表、越级限制、蓄力槽保护、锚点合同、多人隔离、文案池数量）；发现并修复 `TESTING-GUIDE.md` 旧公式残留（5级难度 1.25x→3.45x、80级数量 161x→160x、负/零级称号、附录称号表错位、27倍视觉缩放 3.00→线性 1.354、旧日志示例），源码无改动。
- **部署状态（2026-08-06）**：用户明确授权部署；进程检查发现 `StardewModdingAPI` 运行中（PID 27372，16:45 启动，DLL 被锁定），等待用户关闭游戏后执行备份/替换/哈希核验；部署目标 `D:\GGGGG\K1515\Mods\FishingExpanded`，当前安装 DLL `2B0EB0F1...`。
- **文案措辞与中英文核对（2026-08-06 用户要求）**：图鉴“钓鱼技能加成”改为“钓鱼条长度额外加成”；玩家可见文案（图鉴两行、封顶/触底、鱼王 10 条、NPC 100 条）全部 i18n 化；`default.json`/`zh.json` 键集合一致（35 键），数量匹配；新 DLL `14AB5960...`（0 警告 0 错误）待部署。
### 当前活动 Case（BATCH-029，2026-08-06）

- **现象/需求**：用户定稿 5 项修改：① 尺寸数字（非视觉）正等级每级 +10%、负等级每级 -5%；② 同一鱼种连续失败 ≥2 次且调整后难度 ≥150 → 失败提示改为“收集更多徽章（鱼类收集品页的皇冠），获得更多加成，再挑战史诗级强者吧”；③ 每次成功额外 `round(调整后难度/50)` 等级（例：难度500 → +10）；④ 调整后难度 >100 时加速度增幅降为现增幅的 30%（等级 ≥89 保持 100%）；⑤ 收藏页星标改皇冠。
- **已证实**：尺寸数字当前与数量倍数共用一个倍率（`BobberBar` 构造边界 `fishSize × quantityMultiplier`）；失败提示无连续失败概念；成功结算仅按脱杆 10/5/2/1；`GetAccelerationBoost` 为唯一增幅入口——首次部署后被真实启动推翻（反证 #1）：签名改 `(BobberBar, float)` 后注入序列必须同步加 `Dup`（`Ldfld` 消费注入的 this），否则 `PrepareMethod` JIT 抛 `InvalidProgramException`，详见卡片“实测推翻与修复”；原生无 UI 皇冠，XNB 取证确认 Infinity Crown 帽子贴图 `(20,800,20,20)` 为金色皇冠（LaurelWreathCrown 实为绿色月桂花环）。
- **状态**：R0/R1/R2 完成（见 `Governance/BATCH-029-Size-Levels-EpicFail-Crown.md`）；Release Rebuild ✅；首次部署（`293F6DC0...`）被真实游戏启动推翻（BobberBar.update Transpiler IL 非法）→ 根因已证实、修复并二次部署（`26DEE6F2...`，2026-08-07 07:22）；真实验收待用户执行（五项各自独立标记通过/失败/未执行）。

- **BATCH-028 高难度机制与称号（2026-08-06 用户定稿设计并授权实现）**：高难度运动公式修正（>100 初始目标顶部、>150 换目标频率三处+dart 偏移按 150 封顶、加速度线性增幅无上限）+ 高难度鱼跳机制（150+ 按难度分档 8/6/5/4/3 秒、上下 25% 检测、0.5s 延迟瞬移对侧、冷却后重新计时、鱼王豁免）+ 称号 89-99 创世神/100 混沌（越级限制 神王→99、创世神→100、混沌→100）。Transpiler 注入点已用当前安装 IL 证据核验（5 注入+3 跳过+唯一增幅点）；`GAME-DESIGN.md` 同步更新（含原生代码对齐小节）；Release Rebuild ✅ 0 警告 0 错误；**已部署**（DLL `E0DA4DA2...`，2026-08-06 19:55，备份 `DeploymentBackups\FishingExpanded-20260806-195542-pre-BATCH024-028`）；启动与真实验收待用户执行。
- **部署（2026-08-06 19:55，用户授权“全部代码写完就部署”）**：BATCH-024/025/026/027/028 与文案 i18n 全部合入最新构建 `E0DA4DA2...` 一次部署（DLL/manifest/i18n×2；deps.json 哈希不变未动；部署目录无 config.json 未涉及）；备份 `DeploymentBackups\FishingExpanded-20260806-195542-pre-BATCH024-028`；目标哈希与源一致核验通过；未启动游戏（启动与验收由用户执行）。


### 当前活动 Case（BATCH-031，2026-08-07 更新）
### 当前活动 Case（BATCH-032，2026-08-08 更新）

- **任务**：HUD 提示排队 + 星之果茶掉落（用户确认设计）。
- **已证实（契约取证）**：原生 `Game1.hudMessages` 每帧移除过期消息、同文案合并刷新 3500ms；星之果茶物品 ID `(O)StardropTea`；`Farmer.addItemByMenuIfNecessary` 满包走原生溢出菜单；成功结算唯一边界 `CaughtFish_Postfix`（`SuccessRecorded` 防重）。
- **R0**：`HUDNotifier` 统一入队（队列上限 5 丢最旧、按玩家隔离、活动文案跟踪）；掉落挂在 `CaughtFish_Postfix` 结算边界（同一防重，不建第二发放路径）；概率纯函数 `DifficultyCalculator.TryGetStarfruitTeaDrop`。
- **R1**：无被替代的旧路径；5 处 `addHUDMessage` 全部改为 `EnqueueMessage`，无第二写入者。
- **R2**：排队串行显示、上限丢最旧、双人同屏隔离、掉落概率（50 级 12.5%/80 级 20%/100 级 25%）、背包满溢出菜单、鱼王/非鱼类豁免、性能（驱动 4 次/秒 + 上限 5）、存档无新字段。
- **状态**：设计+实现+构建完成（0 警告 0 错误，DLL `??` 待部署核验）；部署待用户指令；验收项：① 多提示串行不叠加 ② 上限 5 丢最旧 ③ 双人同屏各自排队 ④ 50+ 级概率掉落 ⑤ 15 条文案随机 ⑥ 背包满溢出 ⑦ 鱼王/非鱼类不掉落。

- **任务**：用户要求 “再次核对所有游戏设计，看看是否对应代码，是否有鲁棒性。明确的问题修改并部署”。
- **已证实（源码逐条核对）**：难度/数量/经验/品质公式、尺寸数字与视觉线性缩放、锚点合同、高难度运动公式（>100 初始顶部、>150 三处换目标+dart 按 150 封顶、加速度 30%/100% 档）、跳鱼分档 8/6/5/4/3 秒与 0.5s 延迟/1s 检测/跳完重计时、星标皇冠与 ≥120 门槛、称号 89-99 创世神/100 混沌、鱼王豁免与 5 鱼 ID、非鱼类上限 8、存档兼容、弱称号场景，全部与设计一致。
- **明确问题（文档层）**：① GAME-DESIGN 4.6 “隐藏钓鱼技能加成”旧措辞 → “钓鱼条长度额外加成”（BATCH-025 用户确认措辞）；② GAME-DESIGN 2.2 补失败提示优先级：史诗 > 触底 > 负等级 > 普通（代码 `ShowFailureNotification` 已定序，日志 21:30 证据：-10 且史诗条件成立显示史诗）。
- **鲁棒性复核**：`PullFishFromWater_Prefix` float/int 回退合理、`DifficultyManager` clamp、`GiantFishManager` Category=-4，均无需修改。
- **状态**：核对完成；文档修改完成；无运行时代码/i18n 变化 → 不重建不重部署，核验已部署 DLL 哈希与源码一致性；BATCH-029/030 既有待实测项不受影响。
## 活动批次

| 批次ID | 根因 | 状态 | 创建时间 | R0 | R1 | R2 | 构建 | 部署 | 实测 |
|--------|------|------|----------|----|----|----|----|------|------|
| BATCH-001 | 初始实现 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-002 | 控制台命令 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | N/A | ✅ | ✅ | 待测 |
| BATCH-003 | Transpiler视觉缩放 | ❌ 误判 | 2024-08-04 | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ |
| BATCH-004 | 低端机器性能优化 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-005 | Bug修复与稳定性 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-006 | Transpiler诊断与间接bug | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-007 | Transpiler根因重新定位 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-008 | ObjectPatches栈序列错误 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-009 | 钓鱼动画显示多条鱼 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-010 | 脱杆次数影响等级增长 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-011 | 鱼图鉴显示增强（初次评估） | ⏹️ 推迟 | 2024-08-04 | ✅ | ⏹️ | ⏹️ | N/A | N/A | N/A |
| BATCH-012 | 100级封顶特殊提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-013 | -10级底部提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-014 | 传奇鱼豁免所有规则 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-015 | 非鱼类8级上限 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-016 | 鱼图鉴显示增强（重新实施） | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-024 | 多人数据与鱼获生命周期修正、视觉路径范围修正（真鱼放大） | ✅ 实测通过（2026-08-06） | 2026-08-05 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| BATCH-025 | 图鉴“钓鱼技能加成”按鱼种独立显示（仅星标鱼 +0.5） | ✅ 实测通过（2026-08-06） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| BATCH-026 | 视觉缩放改线性（100级=原25级大小≈3.7084） | ✅ 已部署（E0DA4DA2 随 BATCH-028 一起） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-027 | 按玩家隔离，支持双人同屏与联机 | ✅ 已部署（E0DA4DA2 随 BATCH-028 一起） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-028 | 高难度运动公式修正 + 高难度鱼跳机制 + 称号 89-99 创世神/100 混沌 | ✅ 已部署 | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-029 | 尺寸数字等级线性化 + 连续失败史诗提示 + 成功额外等级 + 加速度 30% 档 + 收藏页皇冠 | ✅ 已部署 | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-030 | 失败只记录一次回归（FailureRecorded）+ 弱称号文案场景修正 | ✅ 已部署 | 2026-08-07 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-031 | 全量设计↔代码核对（命名残留 + 失败提示优先级定稿，纯文档） | ✅ 文档修正 | 2026-08-07 | ✅ | ✅ | ✅ | N/A | 核验 | 不涉及 |
| BATCH-032 | HUD 提示排队（FIFO 上限5）+ 星之果茶掉落（≥50级 概率=等级/4%） | ⏳ 已构建待部署 | 2026-08-08 | ✅ | ✅ | ✅ | ✅ | ⏳ | ⏳ |

---

## BATCH-024：多人数据与鱼获生命周期修正（启动修复待重新部署）

**新增准入证据（2026-08-05 19:07）**：用户提供的 SMAPI 日志显示上一份部署产物在 `Harmony.PatchAll` 阶段将 `CaughtFish_Postfix` 作为 `FishingRod` 类级 Patch 处理，查找不存在的 `FishingRod.caughtFish`，导致 FishingExpanded 初始化失败。当前源码已将该 Postfix 移到独立的 `[HarmonyPatch(typeof(Farmer))] FarmerFishingPatches`，编译产物反编译确认目标为 `Farmer.caughtFish`。

**当前门禁**：源码 Release Rebuild 已通过（0 警告、0 错误）；修复 DLL 尚未替换到目标安装，部署和启动日志核验待执行。允许修改范围仍限于 `Source/Patches/FishingRodPatches.cs` 与本批次状态台账。

**根因**：当前实现把数据写入存档级 `ReadSaveData`，并在 `Farmer.caughtFish` 时扫描背包；原生鱼获物品此时尚未创建，无法可靠覆盖堆叠和 `ItemGrabMenu` 溢出路径。

**R1 实施**：
- 使用玩家自己的 `Farmer.modData` 保存难度、星标和隐藏加成；仅主玩家迁移旧共享数据。
- 规范化原始/限定鱼 ID，统一鱼王识别和图鉴/命令数据键。
- 移除 `caughtFish` 背包扫描，改在原生 `FishingRod.CreateFish` 返回物品时转换数量。
- 将星标写入从 `BobberBar` 构造阶段移到成功捕获后的 `caughtFish` Postfix。
- 待处理鱼获按玩家 ID + 鱼 ID 隔离，返回标题时清理。
- 修正小游戏初始绿条外状态不计为脱杆；星标阈值在成功入口使用原生难度参数兜底。
- 在原生钓鱼经验入口应用经验倍率；鱼王 BobberBar 构造时屏蔽隐藏钓鱼等级读取，保持完整豁免。
- 加入星标鱼开局挑战宣言的展示与本地化随机池，不增加第二个状态所有者。
- 加入小游戏出现时的钓鱼等级建议；使用 `Farmer.FishingLevel` 的有效整数等级，只读比较，不写入玩家或鱼数据。

**R0/R1 状态**：R0 ✅；R1 ✅（当前源码静态检查与 Release Rebuild 已通过；上一候选部署因启动日志暴露 Patch 归属错误而作废）
**当前构建**：✅ `Source\FishingExpanded.csproj` Release Rebuild，0警告，0错误；版本 `0.5.10`；修复产物 SHA-256 待部署后登记
**上一候选部署**：已作废；目标曾与候选输出哈希一致，但用户启动日志证明该候选仍含旧的 `FishingRodPatches::CaughtFish_Postfix` 结构；旧备份见 `DeploymentBackups\FishingExpanded-20260805-183148`
**当前部署**：⏳ 待替换明确的 DLL、依赖、manifest 和 `i18n`，保留 `config.json` 不变
**实测**：待多人和原生溢出验收

## BATCH-016: 鱼图鉴显示增强（重新实施） ✅

**根因**：BATCH-011初次评估复杂度过高，用户纠正"仅仅改几个字而已"

**累计修复次数**：1次（初次实施）

**证据链**：
1. 反编译CollectionsPage.full.cs确认createDescription(string id)返回简单字符串
2. 用户反馈BATCH-011复杂度判断错误
3. 确认只需Postfix追加文本即可

**实现内容**：
- **R0取证**：反编译CollectionsPage，定位createDescription方法
- **R1实现**：CollectionsPagePatches.CreateDescription_Postfix
  - 检查currentTab==4（鱼类tab）
  - 豁免传奇鱼（BATCH-014规则）
  - 追加"挑战等级：{称号}（等级{level}） | 钓鱼技能加成：+{bonus}"
- **R1删除**：移除DifficultyManager中对已废弃CollectionsPagePatches.InvalidateCache()的调用

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\（2026-08-05 01:10）
**实测**：⏳ 待玩家验收图鉴显示功能

---

## BATCH-015: 非鱼类8级上限 ✅

**根因**：新需求 - 垃圾、藻类等非鱼物品难度上限8级

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：定义IsNonFish()检查Category != -4
- **R1实现**：
  - SpecialFishHelper.IsNonFish() - 基于Category判断
  - DifficultyManager.GetDifficultyLevel() - 应用maxLevel=8
  - HUDNotifier.ShowSuccessNotification() - 特殊文案"对于{物品名}而言，你已是帝王"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收非鱼类上限功能

---

## BATCH-014: 传奇鱼豁免所有规则 ✅

**根因**：新需求 - 5种传奇鱼（鱼王等）不受任何自定义规则影响

**累计修复次数**：1次（初次实施）

**证据链**：
1. 用户明确指定5种传奇鱼ID：163/682/160/775/159
2. 要求10种随机消息，不使用难度系统、等级加成

**实现内容**：
- **R0设计**：创建SpecialFishHelper统一管理特殊鱼规则
- **R1实现**：
  - SpecialFishHelper.IsLegendaryFish() - 硬编码5种鱼王ID
  - SpecialFishHelper.GetRandomLegendaryMessage() - 10种随机消息
  - BobberBarPatches.Constructor_Postfix - 最优先检查豁免
  - FishingRodPatches.Prefix - 最优先检查豁免
  - CollectionsPagePatches.CreateDescription_Postfix - 豁免图鉴显示

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收传奇鱼豁免功能

---

## BATCH-013: -10级底部提示 ✅

**根因**：新需求 - 难度-10时提供装备升级建议

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowFailureNotification()检测currentLevel<=-10
- **特殊文案**："最平庸的{鱼名}依然太难了，请升级鱼竿，钓鱼等级，使用料理增加钓鱼等级，使用陷阱/浮木渔具等"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收底部提示功能

---

## BATCH-012: 100级封顶特殊提示 ✅

**根因**：新需求 - 达到100级后显示神明提示

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowSuccessNotification()检测newLevel>=100
- **特殊文案**："你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收封顶提示功能

---

## BATCH-011: 鱼图鉴显示增强（初次评估） ⏹️

**根因**：新需求 - 图鉴显示挑战等级和技能加成

**初次评估**：复杂度高，推迟到v0.6.0

**用户反馈**：评估错误，"仅仅改几个字而已"

**后续**：由BATCH-016重新实施

---

## BATCH-010: 脱杆次数影响等级增长 ✅

**根因**：新需求 - 完美钓鱼（0次脱杆）应获得更高奖励

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：BobberBar追踪bobberInBar状态变化计数
- **R1实现**：
  - BobberBarPatches.Update_Prefix - 追踪!___bobberInBar计数PerfectCount
  - BobberBarPatches.GetPerfectCount() - 公开查询接口
  - DifficultyManager.RecordSuccess() - 支持可变levelGain参数
  - FarmerFishingPatches.CaughtFish_Postfix - 根据脱杆次数调用RecordSuccess
- **奖励规则**：0次脱杆→+10，1次→+5，2次→+2，3次及以上→+1

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收脱杆奖励功能

---

## BATCH-009: 钓鱼动画显示多条鱼 ✅

**根因**：FishingRod.pullFishFromWater的numCaught参数同时控制动画和最终数量

**累计修复次数**：1次（初次修复）

**证据链**：
1. v0.5.0测试显示钓到大鱼时立即显示多条鱼飞向玩家
2. pullFishFromWater()的numCaught参数控制for循环生成多个临时精灵
3. 需要分离动画数量（固定1）和最终背包数量（倍数影响）

**实现内容**：
- **R0取证**：反编译FishingRod.pullFishFromWater()和Farmer.caughtFish()
- **R1重构**：
  - FishingRodPatches.Prefix - 仅记录数据到_pendingFish，不修改numCaught
  - FarmerFishingPatches（新类）- Postfix on Farmer.caughtFish()修改Stack属性
  - 字典访问权限从private改为internal static支持跨类访问
- **第一处分歧**：pullFishFromWater()的numCaught参数，保持=1避免动画重复

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收动画功能

---

## BATCH-008: ObjectPatches栈序列错误 ✅

**根因**：BATCH-007 Transpiler成功插入指令但GetDrawScale从未被调用

**累计修复次数**：2次（BATCH-007根本方案错误，BATCH-008修复栈序列）

**证据链**：
1. v0.5.0日志显示Transpiler匹配到ldc.r4 4但无"应用视觉缩放"记录
2. 代码审查发现yield return codes[i]在插入指令之后
3. IL栈错误：新指令压入后才有4f，导致乘法操作数不足

**实现内容**：
- **R0取证**：IL指令执行顺序分析
- **R1修复**：调整yield return顺序
  ```csharp
  // 错误：yield return codes[i]; 在最后
  // 正确：yield return codes[i]; 在if块内最先执行
  if (codes[i].opcode == OpCodes.Ldc_R4 && ...) {
      yield return codes[i];  // 先压入4f
      // 再插入GetDrawScale和Mul
  }
  ```

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-007: Transpiler根因重新定位 ✅

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
- 反编译3个关键方法
- 定位唯一所有者：Object.drawWhenHeld()的scale=4f参数
- 证据保存到_analysis目录

**R1状态**：✅ 已完成
- 删除FarmerPatches全部无效代码
- 实现ObjectPatches正确Patch Object.drawWhenHeld()
- v0.5.0构建成功（2024-08-04 21:16）
- 部署到D:\GGGGG\K1515\Mods\FishingExpanded（2024-08-04 21:17）

**R2状态**：⏳ 待验证
- 基础功能：钓到倍数>15的鱼，手持时是否视觉放大
- 立方根缩放：倍数27应显示为3倍大小
- 非鱼类物品不受影响
- 多人模式独立缩放
- 性能：低频日志，无每帧计算

**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\ 
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-006: Transpiler诊断与间接bug ✅

**根因**：日志显示Transpiler未找到目标调用，间接bug影响稳定性

**发现的问题**：
1. **Transpiler诊断不足** - 无法判断IL匹配失败的原因
2. **整数溢出风险** - DifficultyLevel计算可能溢出
3. **多人缓存冲突** - FarmerPatches的静态缓存在多人模式下可能冲突

**实现内容**：
- 增强Transpiler诊断：记录总指令数、方法调用数、目标调用数
- DifficultyLevel使用long运算避免溢出
- FarmerPatches缓存改为Dictionary<long, ...>按玩家ID隔离

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.3
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待验证（注：v0.5.0已删除FarmerPatches缓存）

---

## BATCH-005: Bug修复与稳定性 ✅

**根因**：代码审查发现5个潜在bug影响稳定性

**发现的Bug清单**：
1. **Bug #1 (严重)**: BobberBarPatches.PeriodicCleanup空引用检查逻辑错误
2. **Bug #2 (中等)**: HUDNotifier.GetFishDisplayName缺少空检查
3. **Bug #3 (轻微)**: NPCDialogueGenerator.GenerateFishPraise缺少Game1.random空检查
4. **Bug #4 (中等)**: CollectionsPagePatches反射调用缺少错误处理
5. **Bug #5 (轻微)**: FarmerPatches Transpiler搜索范围可能不足

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证稳定性提升

---

## BATCH-004: 低端机器性能优化 ✅

**根因**：每帧运行的代码过多，需要支持低端机器

**识别的性能热点**：
1. CollectionsPagePatches.draw_Postfix - 收藏页面每帧遍历所有物品
2. FarmerPatches.GetItemDrawScale - 每次Farmer.draw()都重新计算
3. GiantFishManager.CheckAndTriggerNPCReactions - 每30帧LINQ查询所有NPC
4. BobberBarPatches Update钩子 - 每帧字典查找
5. ModEntry.OnUpdateTicked - 每帧模运算检查

**实现内容**：
- CollectionsPagePatches: 缓存机制，仅在页面切换时重建星标列表
- FarmerPatches: 结果缓存（注：v0.5.0已删除）
- GiantFishManager: 快速路径检查，平方距离替代开方，直接遍历替代LINQ
- ModEntry: NPC检查间隔从30帧增加到60帧

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.4.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证性能提升

---

## BATCH-003: Transpiler视觉缩放 ❌ 误判

**根因**：原Postfix实现无法修改已执行的绘制，需要Transpiler修改IL指令

**错误假设**：Farmer.draw()调用Item.drawInMenu()绘制手持物品

**实际情况**：
- 调用链是：Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
- Farmer.draw()从不调用Item.drawInMenu()
- 搜索了不存在的方法调用

**后果**：
- v0.3.0-v0.4.3的Transpiler从未生效
- 视觉缩放功能完全不工作
- 被误标记为"✅ 已完成"

**修复**：由BATCH-007重新实现正确方案

**R0状态**：❌ 假设错误
**R1状态**：❌ 删除了不存在的代码路径
**R2状态**：❌ 从未验证实际效果
**构建**：✅ v0.3.0（编译通过但功能无效）
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：❌ 视觉缩放从未工作

---

## BATCH-002: 控制台命令系统 ✅

**根因**：手动钓鱼测试各难度等级效率低下，需要快速测试工具

**实现内容**：
- 9个控制台命令（setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus）
- DifficultyManager新增公开方法支持控制台操作
- 创建12KB测试指南文档（TESTING-GUIDE.md）

**R0状态**：✅ 已完成
**构建**：✅ v0.2.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证命令功能

---

## BATCH-001: 钓鱼难度系统初始实现 ✅

**根因**：新功能需求 - 实现渐进式难度钓鱼系统

**实现内容**：
1. 难度追踪系统（成功/失败计数，-10到100等级）
2. 线性倍数计算（难度/数量/品质/保护）
3. HUD通知与9级称号系统
4. 巨型鱼NPC反应（气泡+100随机对话）
5. 收藏品星标显示
6. 隐藏钓鱼等级加成（每星标+0.5）
7. 完整低频诊断日志

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.1.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内完整验证

---

## 当前任务优先级

1. **【紧急】游戏内测试v0.5.2** - 验证9个新功能（5个bug修复+4个新需求）
2. **视觉验证** - 钓到倍数>15的鱼，手持时应该明显放大
3. **传奇鱼验证** - 钓到鱼王显示随机消息，不受难度系统影响
4. **图鉴验证** - Collections菜单查看鱼类描述，底部显示挑战等级和技能加成

---

## 修复历史记录

**累计修复次数统计**：

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 视觉缩放不工作 | 2 | BATCH-003(误判) → BATCH-007(正确方案) → BATCH-008(栈序列修复) |
| 钓鱼动画显示多条鱼 | 1 | BATCH-009(初次修复) |

**治理规则**：同一玩家可见现象的修复次数跨会话、批次、版本和名称累计，不能通过重新分组清零。

---

## 长期追踪项

- **【关键】v0.5.2所有功能需要游戏内验证** - 9个新功能待实测
- **【关键】图鉴显示需要打开Collections菜单验证** - BATCH-016
- 传奇鱼豁免需要实际钓到鱼王验证 - BATCH-014
- 非鱼类上限需要钓到垃圾/藻类验证 - BATCH-015

---

**最后更新**：2026-08-05 01:12
**当前版本**：v0.5.2
**总批次数**：16个（14个完成，1个推迟，1个误判）
**待实测项**：v0.5.2所有9个新功能
