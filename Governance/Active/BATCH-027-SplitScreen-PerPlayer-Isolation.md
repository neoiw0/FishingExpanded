# BATCH-027：按玩家隔离重构，支持双人同屏与联机

**创建**：2026-08-06
**类型**：所有权级修复（FE-ISSUE-4）；用户明确“必须支持所有双人模式”
**状态**：R0 ✅ / R1 ✅ / R2 ✅；Release Rebuild ✅（产物 SHA256 `AC7B1408A5B2BAFF5ADCFCC9E77BBD9CB5701FDBB163F6D4047B5829BB9C0097`）；不部署（沿用用户“先不部署”授权边界，部署待用户确认）

## 玩家需求（原话要点）

- “验证一下双人同屏和联机模式是否能用，是主机客机都要装吗？显示会不会主机客机不同步”
- “修复这个问题，必须支持所有双人模式”

## 已证实事实（当前安装 `D:\GGGGG\K1515\Stardew Valley.dll` SHA-256 `DFE341CA...` 与 SMAPI 反编译核对，2026-08-06）

- 分屏机制：每位本地玩家是一个独立 `Game1` 实例（`GameRunner.gameInstances`、`Game1.instanceId`、`Context.ScreenId => Game1.game1?.instanceId`）；`GameRunner` 对每个实例执行 `LoadInstance → Update/Draw → SaveInstance`，通过 `LocalMultiplayer.StaticVarHolderType`（反射收集游戏程序集全部静态字段动态生成 holder）把 `Game1.player`、`Game1.activeClickableMenu`、`Game1.hudMessages` 等静态字段随屏幕切换。
- 含义：所有“Game1.player 时点读取”在各自屏幕切片内天然指向该屏幕玩家；Mod 自身静态缓存不参与切换 → 必须按玩家键控。
- 联机同步：`Farmer.modData` 继承自 `Character.modData`（`Character.initNetFields` 注册 `.AddField(modData, "modData")`）→ 农场客写入的 modData 会同步到主机并存进主机存档。
- 旧证据陷阱：`_analysis/StardewValley.Farmer.decompiled.cs` 缺 `modData`（旧反编译不完整），本次用当前 DLL 重新反编译 `Character` 证实。

## 第一处分歧（源码证实）

- `DifficultyManager._data` 是单例静态缓存，只在主屏幕 `SaveLoaded` 加载一次；双人同屏时副屏玩家的所有读取都命中主玩家缓存，副屏玩家的成功/失败/星标写入会改动主玩家对象并写进副屏玩家 modData（双向串写）。
- 次要同屏问题：`FarmerFishingLevelPatches.SuppressHiddenBonusForLegendaryBobber` 单静态布尔（同屏鱼王构造期串扰）；`FishingLevel` Getter 用 `__instance != Game1.player` 排除非主玩家；`GiantFishManager.CheckAndTriggerNPCReactions` 只检查 `Game1.player`。

## R0：按玩家隔离

- `DifficultyManager` 重构为 `Dictionary<long, FishDifficultyData> _dataByPlayer`（键=UniqueMultiplayerID），惰性加载（`LoadData(Farmer)`），每次写入立即 `SaveData(Farmer)` 到该玩家 modData；`SaveAll()` 作为 Saving 兜底；`UnloadData()` 清空全部。
- 所有公开方法增加 `Farmer player` 参数并显式传玩家：BobberBar 构造/失败（`Game1.player`=该屏幕钓鱼玩家）、`Farmer.caughtFish` Postfix（`__instance`）、`CreateFish`（`rod.getLastFarmerToUse()`）、图鉴（`Game1.player`）、HUD 星标宣言（传入玩家）、FishingLevel Getter（`__instance`）、控制台命令（`Game1.player`）。
- `FarmerFishingLevelPatches`：屏蔽标志改 `HashSet<long>` 按玩家；Getter 改 `!__instance.IsLocalPlayer` + 按玩家加成（分屏两位本地玩家各自生效）。
- `GiantFishManager`：`CheckAndTriggerNPCReactions` 遍历所有本机玩家（`Game1.getOnlineFarmers().Where(IsLocalPlayer)`），`TriggerNPCBubble` 增加玩家参数；`CaughtFish` 改走 4 参 `RecordGiantFish(__instance, ...)`。
- 语义不变：单人存档键、JSON 结构、等级计算、区间封顶、鱼王豁免、非鱼类上限、视觉锚点合同全部保持。

## R1：删除与收敛

- 删除单例 `_data`、`Initialize()`（惰性加载取代）、死代码 0 参 `GiantFishManager.OnEnterFarmHouse()`。
- 全源码 `rg` 复核：无旧签名残留（`GetDifficultyLevel(...)` 单参、`RecordSuccess/RecordFailure/HasCollectionStar/RecordHighDifficulty/SetDifficultyLevel/GetFishStats` 单参、`SuppressHiddenBonusForLegendaryBobber` 全部清零）。
- 无新增状态所有者：`_dataByPlayer` 仍是 DifficultyManager 唯一数据权威；BobberBar 实例数据、待处理鱼获、展示事实均沿用按玩家/实例键控。

## R2：边界推演

| 场景 | 预期 |
|---|---|
| 单人 | 与旧行为完全一致（主玩家数据、惰性加载） |
| 双人同屏-副屏玩家钓鱼 | 小游戏难度/失败/成功/星标/加成读各自玩家数据，写入各自 modData |
| 双人同屏-副屏玩家图鉴 | Collections 随屏幕实例显示该屏幕玩家数据（原生即按 Game1.player） |
| 双人同屏-鱼王 | 屏蔽标志按玩家隔离，副屏玩家不受主屏鱼王影响 |
| 双人同屏-NPC反应 | 每位本地玩家各自检查自己举起的鱼 |
| 联机主机+农场客 | 各客户端只操作本机玩家；农场客 modData 经原生同步进主机存档 |
| 保存/重载/返回标题 | `SaveAll`/`UnloadData` 幂等；切换存档不残留 |
| 性能 | 每次操作一次字典查找；NPC 检查快速路径先跳过无展示事实的玩家 |

## 可观测性决定

- 新增低频日志已含玩家 ID（成功/失败/星标/加载均带 `玩家: {id}`），不新增逐帧输出；无新增诊断状态。

## 自动化验收决定

- 复用 `fish_*` 控制台与 `FISH-CLI-01` 全量 `Saves` 沙箱；单人回归：setlevel/addsuccess/addfail/addstar/info/bonus 数值与旧版一致。
- 双人同屏/联机为真实运行验收：需要实际分屏/两台机器会话，每个现象（副屏难度隔离、副屏星标隔离、图鉴按屏显示、鱼王屏蔽隔离、NPC 反应）独立标记通过/失败/未执行；本轮未执行。

## 委派评估

- `NotBeneficial`：本轮为源码级重构 + 本机构建/反编译核验，主线程直接完成；没有可外包的并行取证面，且未形成 provider 路由证据闭环需求（未使用任何委派执行器）。

## 临时诊断清理条件

- `Source\Patches\Game1Patches.cs`（`Game1.drawPlayerHeldObject` 前缀）与 `ObjectPatches._visualDiagnosticStates/LogVisualDiagnostic`（状态变化去重、存档加载/卸载时 `ResetVisualDiagnostics` 清空）属 BATCH-024 视觉链路临时诊断；BATCH-026 线性缩放部署并完成真实画面验收后，按治理删除或转为有证据支持的长期低频健康信号，不允许跨存档残留。

## 文案 i18n 化与中英文核对（2026-08-06 用户要求）

- 用户确认：图鉴“钓鱼技能加成”改为“钓鱼条长度额外加成”（`collections.fishingBonus`）。
- 玩家可见文案全部迁入 i18n：图鉴两行（`collections.challengeRank`/`collections.fishingBonus`）、封顶/触底 HUD（`hud.success.nonFishCap`/`hud.success.fishCap`/`hud.fail.bottom`）、鱼王 10 条（`hud.legendary.messages`）、NPC 100 条（`npc.praise.templates`）；代码保留中文 fallback（i18n 缺失时）。
- 核对：`default.json` 与 `zh.json` 键集合一致（35 键）；鱼王 10/10、NPC 100/100、尊敬词 3/称号、毅然 50、应战 20 中英数量一致；JSON 解析有效。
- 新产物：DLL SHA256 `14AB59607AFB7CC872D2B68BDA09D2C3568061A31C2E9AE73264DC1DD95FF0F3`（0 警告 0 错误，反编译确认 6 个新键调用已编译）；i18n `default.json` `9BD1F867...`、`zh.json` `BF0F6E51...`。
- 部署：用户已授权；目标 DLL 仍被运行中的 `StardewModdingAPI`（PID 27372，16:45 启动）锁定，等待用户关闭游戏后执行备份/替换/哈希核验（当前安装 `2B0EB0F1...`，备份 `DeploymentBackups\FishingExpanded-20260806-172140-pre-BATCH026027`）。

## Closeout 静态门禁（2026-08-06 重核）
## Closeout 静态门禁（2026-08-06 重核）

- 治理同步 ✅：本卡、`BUG-LEDGER.md`、`GAME-DESIGN.md`、`TESTING.md`、`MAINTENANCE-INDEX.md`、`DECOMPILATION-RESEARCH-INDEX.md` 已同步；`LOCAL-PITFALLS.md` 记录本机补丁工具限制。
- Git 基线 ✅：HEAD `8f799da`，工作树无暂存（未使用 `git add -A`）；聚焦 diff 覆盖 BATCH-024/025/026/027 源码与治理文件，无来源不明文件混入。
- 构建 ✅：Release Rebuild 0 警告 0 错误；产物 SHA256 `AC7B1408A5B2BAFF5ADCFCC9E77BBD9CB5701FDBB163F6D4047B5829BB9C0097`（与 BATCH-026 合并产物，重核一致）。
- 部署：不部署（用户未授权）；未启动游戏。
- 逻辑检查点：未创建提交（用户未授权提交；工作树为多批次混合来源，需用户确认后按明确路径拆分暂存）。

## 门禁

- 构建：Release Rebuild ✅（0警告0错误；ilspycmd 反编译确认 `_dataByPlayer`、`RecordSuccess(fishId, levelGain, Farmer)`、`SetLegendarySuppression`、`CheckAndTriggerForPlayer` 已编译）。
- 部署：用户 2026-08-06 明确授权部署（含 BATCH-026/027 合并 DLL）；已通过进程检查，发现 `StardewModdingAPI` 运行中（PID 27372，16:45 启动，游戏 1.6.15 + 103 mods），DLL 被锁定，等待用户关闭游戏后执行备份/替换/哈希核验。
- 实测：待用户授权部署后执行；双人模式每现象独立标记。