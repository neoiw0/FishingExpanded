# BATCH-024 多人数据与鱼获生命周期修正

**当前阶段**：R0/R1/R2 完成（2026-08-06 视觉路径范围修正）；候选 `B8BDAEFA...` 启动崩溃（coreclr AV），经二分定位修复为 `2B0EB0F13272E9FC7B34EB43F6ACD30D2783F7032C873AA911BCE6516DBF8003`；已部署并启动核验通过（15:40 会话 `初始化完成`、Transpiler 命中 `缩放: 3 | 底边补偿: 3`）；Runtime/真实画面验收待执行。
**准入授权**：用户确认必须支持多人，并授权部署、启动游戏和实机验收；2026-08-06 用户再次实测并确认视觉范围。

**本轮准入证据（2026-08-06）**：当前安装 `D:\GGGGG\K1515\Stardew Valley.dll`（SHA-256 `DFE341CAFD91565B675365AC69F49C7A6DA47A24F8399A7DB0D3AB52F79EB32B`）重新反编译核对：`BobberBar.draw` 鱼标 `(10,10)` 中心原点、`scale=2f`；`FishingRod.draw` 结算面板（`mouseCursors` 31,1870）与面板内示意图（第一处鱼图 `Vector2.Zero`、4f）；落地真鱼（第二处/原生多鱼图 `(8,8)`、3f，位置 `(0,-56)`，点击后 `doneHoldingFish` 收包）；`doPullFishFromWater` 飞行动画为原生 `TemporaryAnimatedSprite`（`textureName`=物品纹理、`scale=4f`）。2026-08-06 11:41 本机 SMAPI 日志证明上一部署把缩放接入 `BobberBar.draw`、结算面板 4f 与落地真鱼 3f（`[FishingRodPatches] 结算视觉锚点注入 | 中心位置补偿: 1 | 首图缩放: 1 | 中心鱼图缩放: 3`），飞行动画无任何接入记录。该日志实测值：`stage=BobberBar.draw ... visualScale=1.913/2.080/2.351 stateHit=True`（小游戏鱼标被放大）、`stage=FishingRod.draw ... visualScale=1.913/2.351 stateHit=True`（落地真鱼被放大）、无任何 `stage=FishingRod.fly` 记录（飞行动画未放大）。2026-08-06 用户补充确认：显示鱼尺寸的结算页面出现时，同步立在玩家身体旁边、点击一次收进背包的鱼（`FishingRod.draw` 第二处/多鱼 3f 图，位置 `(0,-56)`）才是需要放大的真鱼。用户确认：小游戏鱼标不缩放、结算面板示意图不缩放、飞行动画放大（中心锚点保持轨迹）、落地真鱼放大（底边中点锚点）、手持鱼放大（底边中点锚点）。

**本轮观测决定**：删除 `stage=BobberBar.draw` 诊断（路径移除）；新增 `stage=FishingRod.fly` 同键低频记录（按玩家/鱼 ID/命中数键控）；保留 `FishingRod.draw`（落地真鱼，`anchor=bottom-center`）与手持路径记录；不逐帧输出，返回标题或存档加载时清理。视觉修复只改变绘制缩放和位置，不改变鱼获、数量、难度或状态所有权。

**本轮自动化验收决定**：沿用 `FISH-CLI-01` 的全量 `Saves` 沙箱和现有 `fish_*` 命令；手持场景使用 `fish_giant 142 27` 和实际鲤鱼，验证底边中点；小游戏/结算场景使用鱼 ID 142 的真实原生钓鱼入口。成功条件：小游戏鱼标恒为原生 2f；结算面板与示意图不缩放；`stage=FishingRod.fly` 命中且视觉缩放约为 3.00、飞行中心轨迹不漂移；落地真鱼约 3.00 倍且底边中点不漂移；手持底边中点不漂移；无重复动画数量、无鱼王误放大、无日志刷屏。失败条件：小游戏或面板图被放大、飞鱼或落地鱼未放大、锚点偏移、Mod新增多鱼图或日志刷屏。静态/构建/部署不能代替该结果。诊断证据闭环后删除临时诊断，或转为有证据支持的低频健康信号。

**新增启动证据**：用户提供的 SMAPI 日志显示上一候选部署在 `Harmony.PatchAll` 阶段失败，错误 Patch 方法为 `FishingRodPatches::CaughtFish_Postfix`，原因是类级 `FishingRod` 目标下不存在 `caughtFish`。该日志证明上一候选部署不能作为启动通过证据。

**当前 Build/Deploy 门禁**：当前源码 Release `0.5.10` Rebuild 已通过（0 警告/0 错误，DLL SHA-256 `B8BDAEFA...`）；编译产物反编译确认 `FarmerFishingPatches` 的类级目标为 `Farmer`、方法目标为 `caughtFish`，`FishingRodPatches` 不再包含该 Postfix。当前安装 DLL 实测为 `36B74510...`（2026-08-05 22:36:37，即用户 2026-08-06 实测的含旧视觉缩放版本）；早前记录的 `9D0C1C29` 是 20:42 visual-diag 部署（备份 `DeploymentBackups\FishingExpanded-20260805-visual-diag`），已被后续部署替换，不是当前安装。新候选 `B8BDAEFA` 尚未部署。部署门禁：目标进程/DLL 占用检查、备份当前 `36B74510`、源/目标哈希核对、不覆盖 `config.json`。

**上一候选部署事实（已作废）**：目标 `D:\GGGGG\K1515\Mods\FishingExpanded` 曾部署 DLL、依赖和 manifest，与 `Source\bin\Release\net6.0` 哈希一致；DLL SHA-256 为 `13B3391F52D2E4B8F5EB739C28779D36698B44A7BCB993D9BE7870780CA3FCE1`；备份见 `DeploymentBackups\FishingExpanded-20260805-183148`。用户启动日志已推翻其可启动性。

## R0 证据与第一处分歧

- 现有 `DifficultyManager` 使用存档级 `ReadSaveData("FishDifficultyData")`，无法满足按玩家隔离。
- 当前安装的原生链路是 `FishingRod.pullFishFromWater()` → 鱼获动画 → `Farmer.caughtFish()`（记录捕获统计）→ `FishingRod.doneHoldingFish()` → 私有 `CreateFish()` → `addItemToInventoryBool()` 或原生 `ItemGrabMenu`。
- 旧实现试图在 `Farmer.caughtFish` Postfix 扫描背包修改数量；该时点原生鱼获物品尚未创建，无法可靠覆盖背包堆叠和溢出菜单。
- 鱼王识别表使用小写 `(o)`，调用链可能传入未限定或大写 `(O)` ID，静态识别不闭环。
- 高难度星标原先在 `BobberBar` 构造时写入，失败离开小游戏也可能获得星标；设计要求成功钓起后才获得。

## 用户确认合同

- 五只鱼王只显示特殊提示，不参与难度、数量、品质、奖励、技能、称号、视觉、NPC和蓄力保护。
- 鱼王 ID 统一兼容 `163`、`(O)163`、`(o)163` 等等价形式。
- 高难度星标和 `+0.5` 技能加成只在成功钓起后写入；鱼王永不获得。
- 没有技能加成时图鉴隐藏“钓鱼技能加成”一项；等级小于等于0的成功不显示挑战提示。
- 非鱼类到8级后后续成功不再增加难度。
- 难度、星标和隐藏加成按玩家独立保存；在线联机双方都安装本 Mod。
- 已有星标鱼进入小游戏时显示一次左下角挑战宣言；称号尊敬词每级3种，毅然决然词50种，应战说法20种，宣言不写状态。
- 小游戏出现时，非鱼王使用 `难度等级 / 10` 与当前有效整数钓鱼等级比较；超过当前等级显示建议，超过当前等级两倍时在建议开头显示“不可能的高难度挑战”；鱼王跳过。
- 非鱼王经验按当前小游戏难度倍率结算；隐藏技能加成无半级时向下取整；鱼王不使用隐藏技能加成或经验倍率。

## R1 允许修改的边界

- `Source/Services/DifficultyManager.cs`、`Source/Utils/SpecialFishHelper.cs`、`Source/Data/FishDifficultyData.cs`
- `Source/Patches/BobberBarPatches.cs`、`Source/Patches/FishingRodPatches.cs`、`Source/ModEntry.cs`
- `Source/Patches/FarmerFishingLevelPatches.cs`、`Source/Utils/ChallengeDialogueGenerator.cs`、`Source/Services/HUDNotifier.cs`、`Source/i18n/*.json`
- `Source/Patches/ObjectPatches.cs`、`Source/Patches/Game1Patches.cs`
- 对应 `GAME-DESIGN.md`、`BUG-LEDGER.md`、`MAINTENANCE-INDEX.md`、`TESTING.md`、`TESTING-GUIDE.md`
- 本轮自动化测试工具：`Source/Tools/Validate-FishingContracts.ps1`、`Source/Tools/Invoke-FishingRuntimeScenario.ps1`、`Source/Tools/Invoke-FishingTestSuite.ps1`

## R1 删除/替代清单

- 删除存档级鱼数据作为当前写入者，改用当前 `Farmer.modData`；仅主玩家迁移旧共享数据。
- 删除 `Farmer.caughtFish` Postfix 的背包扫描和堆叠改写。
- 数量转换唯一落点改为原生 `FishingRod.CreateFish` 返回物品边界。
- 删除 `BobberBar` 构造阶段的高难度星标写入。
- 待处理鱼获按玩家 ID + 规范化鱼 ID 隔离，并在返回标题时清理。
- 修正小游戏初始绿条外状态的脱杆计数；成功入口以原生 `fishDifficulty` 作为星标阈值兜底。
- 在 `Farmer.gainExperience(1, ...)` 的原生写入边界应用经验倍率；鱼王 BobberBar 构造期间屏蔽隐藏钓鱼等级。
- 星标挑战宣言从 BobberBar 构造后只读取星标和当前等级，由 HUD 统一展示。
- 等级建议在 BobberBar 构造后由 HUD 只读展示，不建立第二份难度或钓鱼等级状态。
- 删除 `FishingRodPatches` 对 `Farmer.addItemToInventoryBool` 的自建溢出追踪和补菜单；原生 `CreateFish` 返回物品后继续由原生背包与 `ItemGrabMenu` 提交。
- 巨型鱼展示事实按玩家 ID 隔离；手持绘制从 `drawWhenHeld(..., Farmer)` 读取对应玩家，进入 FarmHouse 只清理进入者的事实。
- 首次 NPC 巨型鱼主动对话保存原生 `CurrentDialogue`，第二次交互先恢复原生栈再交给原生 `checkAction`。
- 【2026-08-06】删除 `BobberBarPatches` 小游戏鱼标缩放（`GetFishVisualScale(BobberBar)`、`Draw_Transpiler` 及 `stage=BobberBar.draw` 诊断）。
- 【2026-08-06】删除 `FishingRodPatches.AdjustFirstSettlementFishPosition` 与结算面板 4f 缩放注入（含 `IsNullableRectangleConstructor`/`IsLoadLocal`/`GetLocalIndex` 等仅服务于面板检测的辅助方法）。
- 【2026-08-06】落地真鱼（第二处/多鱼 3f）锚点由中心改为底边中点：新增 `AdjustLandingFishPosition` 消费式位置调整（初版 `GetLandingFishPositionOffset`+注入 `Add` 指令导致 coreclr 启动崩溃，见“启动崩溃与二分定位”）。
- 【2026-08-06】新增飞行动画缩放：`DoPullFishFromWater_Postfix` 按 `textureName`+`sourceRect` 识别原生飞鱼并缩放，新增 `stage=FishingRod.fly` 诊断。

## R2 验收

- 单人：正常鱼成功/失败、0级保护、非鱼类8级封顶、星标只在成功后获得。
- 多人：主玩家与农场客各自设置/钓鱼，等级、星标和技能加成不互相读取；双方安装 Mod 后无重复结算。
- 鱼王：五种 ID 形式均只显示特殊提示，完全不进入自定义难度和奖励路径。
- 数量：普通背包、已有同类堆叠、原生多鱼鱼饵和满背包 `ItemGrabMenu` 均只转换最终物品数量，动画仍由原生数量控制。
- 星标宣言：已有星标进入小游戏时出现一次；验证称号词池每级3种、毅然决然50种、应战20种，普通鱼与鱼王边界分别成立。
- 经验：等级0经验倍率为1，正数等级按设计倍率增加；鱼王经验与隐藏技能等级不受 Mod 影响。
- 等级建议：使用难度等级 1、10、50、100 与不同玩家等级验证小数比较、两倍“不可能”前缀和鱼王豁免。

**R2 推演结论（2026-08-06 完成）**：
- 场景对应性：小游戏鱼标（原生 `2f`）、结算面板与示意图（原生 `4f`）当前源码无任何缩放路径接入；落地真鱼与原生多鱼图（`3f`）底边中点缩放；飞行动画（原生 `4f`）中心缩放；手持（`4f`）底边中点缩放。所有路径只消费 `GiantFishManager`/待处理事实的展示缩放，不反向写入难度、存档或奖励。
- 性能：缩放与锚点补偿均为绘制期常数开销；`ObjectPatches.LogVisualDiagnostic` 按 stage+玩家+鱼ID 同键去重，不逐帧输出；删除 `BobberBar` 绘制 Patch 后小游戏路径不再有 Mod 绘制开销。
- 存档：本批不新增、不改写存档字段；待处理鱼获与巨型鱼展示事实仍按玩家 ID 键控，返回标题/存档加载时清理（`ResetVisualDiagnostics`）。
- 多人：缩放只对 `IsLocalPlayer` 生效；其他玩家路径不读取本地展示事实；鱼王在任何玩家路径均豁免。
- 部署对应性：新增 `Game1Patches.cs`（仅诊断）、`FishingRodPatches` 绘制注入（`Draw_Transpiler`、`DoPullFishFromWater_Postfix`、`AdjustLandingFishPosition`）与目标安装 `FishingRod.draw`/`doPullFishFromWater` 签名绑定；部署前核对当前安装 DLL 哈希，替换后核对启动日志与诊断命中。

## 启动崩溃与二分定位（2026-08-06 新增证据）

- **现象**：候选 `B8BDAEFA...` 部署后，游戏在 `Harmony.PatchAll` 阶段启动崩溃，连续两次复现（15:20、15:23 会话）；Windows 事件日志：`StardewModdingAPI.exe` / `coreclr.dll` / 异常 `0xc0000005`，崩溃点紧跟 `[FishingRodPatches] 落地真鱼缩放注入 | 缩放: 3 | 底边补偿: 3` 日志。
- **对照**：临时装回旧 DLL `36B74510...`（今早正常运行的版本），同一启动方式运行正常（15:30 会话 `初始化完成`）→ 排除启动环境因素，确认新候选为回归源。
- **二分结果**：
  - BISECT-1（禁用 `Draw_Transpiler`+`DoPullFishFromWater_Postfix`）：正常 → 崩溃在新绘制补丁内。
  - BISECT-2（仅恢复 `Draw_Transpiler`）：崩溃复现 → 定位到 Transpiler。
  - BISECT-3（仅禁用位置补偿注入，保留 `Ldarg_0+Call GetFishVisualScale+Mul` 缩放注入）：正常 → 定位到位置补偿注入。
- **根因**：Transpiler 在 `GlobalToLocal` 后注入 `Ldarg_0 + Call GetLandingFishPositionOffset + Add`，其中 `OpCodes.Add` 直接对 MonoGame `Vector2` 结构体执行 IL 加指令，导致 coreclr JIT 访问冲突崩溃（缩放注入的 `Mul` 只作用于 float32，验证无问题）。
- **修复**：改为消费式方法 `AdjustLandingFishPosition(Vector2 position, FishingRod rod)`（方法内完成 `position + offset` 并返回调整后位置），注入序列为 `Ldarg_0 + Call AdjustLandingFishPosition`，与旧 DLL 已验证的 `AdjustFirstSettlementFishPosition` 模式一致；不再注入 `Add`。
- **修复核验**：新候选 `2B0EB0F13272E9FC7B34EB43F6ACD30D2783F7032C873AA911BCE6516DBF8003` 部署后启动通过（15:40 会话：`FishingExpanded 初始化完成`，`落地真鱼缩放注入 | 缩放: 3 | 底边补偿: 3`），游戏进程存活、日志持续增长；启动日志归档 `RuntimeEvidence\20260806-DEPLOY-BATCH024-025\SMAPI-fixed-candidate-startup.txt`（SHA-256 `4A3FDF30...`）。

## Closeout/静态门禁（2026-08-06，无统一脚本，逐项记录）

- 治理同步：`BUG-LEDGER.md`（活动 Case、批次表、维护门禁）、`GAME-DESIGN.md`（视觉锚点合同、图鉴加成）、`MAINTENANCE-INDEX.md`、`TESTING.md`、`TESTING-GUIDE.md`、`DECOMPILATION-RESEARCH-INDEX.md`、`CURRENT-WORK-HANDOFF.md` 已同步；新增 `BATCH-025` 活动卡。
- Git 基线：HEAD=`8f799da`；工作树仅含本批源码与治理修改及新增证据文件，未暂存；不执行 `git add -A`。
- 聚焦 diff：`FishingRodPatches.cs`（`Draw_Transpiler` 只命中落地真鱼 3f+Draw、`DoPullFishFromWater_Postfix` 飞行动画、`AdjustLandingFishPosition` 底边中点）、`ObjectPatches.cs`/`Game1Patches.cs`（手持底边中点+低频诊断）、`BobberBarPatches.cs`（仅记录数量/玩家，无绘制缩放）、`CollectionsPagePatches.cs`（BATCH-025）、`ModEntry.cs`（返回标题/存档加载清理诊断状态）。
- 委派评估：NotBeneficial（本批为聚焦源码修正与治理同步，主线程直接完成）。
- 构建结果：候选 `B8BDAEFA...` Release Rebuild 0 警告/0 错误但启动崩溃；修复候选 `2B0EB0F1...` Release Rebuild 0 警告/0 错误。
- 部署哈希：候选 `B8BDAEFA...` 部署后启动崩溃（coreclr AV，证据见“启动崩溃与二分定位”）；修复候选 `2B0EB0F1...` 已部署（备份 `DeploymentBackups\FishingExpanded-20260806-20260806-152004-pre-B8BDAEFA` 与 `RuntimeEvidence\20260806-DEPLOY-BATCH024-025`），启动核验通过；`config.json` 未创建/未覆盖，`Mods.lnk` 未动。
- 门禁结论：静态与构建门禁通过；R0/R1/R2 完成；Deploy/Runtime/真实画面验收未完成，不得宣布实测通过。
