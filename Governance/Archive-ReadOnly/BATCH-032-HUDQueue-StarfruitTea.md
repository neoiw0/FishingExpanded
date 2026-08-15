# BATCH-032：HUD 提示排队 + 星之果茶掉落

**创建**：2026-08-08
**类型**：用户确认的玩法/UX 新增（非 Bug）
**状态**：设计 ✅ → 实现 ✅ → 构建 ✅（0 警告 0 错误）→ 部署待用户指令

## 准入证据与设计确认

- 用户 2026-08-08 确认：排队功能第 1 点（上限 5 丢最旧）可以、第 2 点（成功/失败/等级建议/星标宣言全部排队）全部排队。
- 用户新增：难度等级 ≥50 的鱼成功钓起后，按 `1/4 × 难度等级%`（即 等级/400）概率掉落 1 瓶星之果茶（例：80 级 → 20%）；掉落时额外显示左下角消息，15 条文案随机、文风幽默符合游戏场景（用户补充：从 5 条扩到 15 条）。
- 设计写入 `GAME-DESIGN.md`：2.3 HUD 提示排队、5.4 星之果茶掉落。

## 调用链与契约取证

- 原生 HUD 契约（`_analysis\StardewValley.Game1.decompiled.cs`）：`Game1.hudMessages` 为 `List<HUDMessage>`；每帧 `RemoveAll(m => m.update(time))` 移除过期消息；`addHUDMessage` 对同 whatType 同文案的消息合并刷新 timeLeft=3500f，否则追加。
- 星之果茶物品 ID：`(O)StardropTea`（游戏 DLL 字符串证据）。
- 发放 API：`Farmer.addItemByMenuIfNecessary(Item)`（反编译确认：有空位直接入包，满走原生 ItemGrabMenu 溢出）。
- 成功结算唯一边界：`FarmerFishingPatches.CaughtFish_Postfix`（`data.SuccessRecorded` 防重，BATCH-030 修复后唯一发放边界）→ 掉落挂在此处，不建立第二发放路径。

## 实现

- `HUDNotifier.cs`：新增 FIFO 队列（上限 5 满丢最旧、按玩家隔离）、活动文案跟踪（原生 hudMessages 消失后出队下一条）、`ProcessQueue`（ModEntry 每 15 tick 驱动）、`ClearPending`（回标题/切存档清空）、`ShowStarfruitTeaNotification`（15 条随机）；原 5 处 `addHUDMessage` 统一改为 `EnqueueMessage`。
- `ModEntry.cs`：UpdateTicked 加 0.25s 驱动；SaveLoaded/ReturnedToTitle 清空队列。
- `DifficultyCalculator.cs`：`TryGetStarfruitTeaDrop(level)`（≥50 时概率 = 等级/400）。
- `FishingRodPatches.cs`：CaughtFish_Postfix 在成功结算后 roll 掉落 → `addItemByMenuIfNecessary` 发放 + 掉落提示入队。
- i18n：`hud.starfruitTea.1`~`.15` 中英 15 条，56 键一致，JSON 校验通过。

## 所有权与边界

- 唯一写入者：HUD 展示 = `HUDNotifier`（队列与活动记录唯一所有者）；掉落 = `CaughtFish_Postfix` 结算边界（与等级/星标同一防重）。
- 鱼王豁免（无 pending 数据天然不进入结算边界）；非鱼类上限 8 达不到 50 级天然不参与。
- 多人/双人同屏：队列按玩家 `UniqueMultiplayerID` 隔离；掉落只对本地玩家（`IsLocalPlayer` 既有边界）。
- 原生消息不参与排队、不受影响。

## R2 场景推演

- 排队：小游戏开始（建议+宣言两条）→ 建议先显示 3.5s 后宣言显示；连续失败快速触发多条 → 串行显示不叠加；队列满 5 → 丢最旧保最新；双人同屏各屏幕独立；回标题后无残留。
- 掉落：50 级 12.5%、80 级 20%、100 级 25%；失败不掉；鱼王不掉；非鱼类不掉；背包满 → 原生溢出菜单；消息与其他提示排队。
- 性能：驱动 4 次/秒，`List.Any` 字符串匹配，无每帧遍历；队列上限 5 防无界。
- 存档：无新增存档字段；掉落物走原生背包/溢出。
- 部署对应性：无 Harmony 新补丁，仅修改既有边界内代码；部署前核对当前安装 DLL。

## 测试工具增强（2026-08-08，用户测试中要求）

- 新增命令 `fish_addstars <数量>`：一次给当前玩家批量添加收藏星标（每颗 +0.5 钓鱼条长度额外加成）。
- 自动从当前安装 `Data/Fish` 挑选普通鱼（跳过 5 只鱼王与已加星鱼），失败回退内置测试池（128~158）；返回实际添加数，鱼池不足时提示。
- 唯一写入者不变：`DifficultyManager.AddCollectionStarsForTesting` → `SaveData(player)`，与星标/加成既有所有权一致；图鉴皇冠、`fish_bonus`、挑战宣言同步反映。
- 部署：DLL `9AE5B858...`（2026-08-08 19:43，备份 `DeploymentBackups\FishingExpanded-20260808-194306-pre-ADDSTARS`）；i18n/manifest 未变；`TESTING-GUIDE.md` 命令总览已更新。
- 检查点：本轮未提交（等用户测试验收后与后续修复统一提交，避免反复提交）。
## Closeout/静态门禁

- 治理同步：`GAME-DESIGN.md`、`BUG-LEDGER.md`、`TESTING.md`、本卡。
- Git 基线：HEAD=`8f799da`；工作树为既有批次混改 + 本轮修改；禁止 `git add -A`。
- 构建：Release Rebuild 0 警告 0 错误。
- 委派：NotBeneficial（局部实现，无委派价值）。
- 真实验收（用户执行后每现象独立标记）：① 多条提示串行显示不叠加；② 队列上限 5 丢最旧；③ 双人同屏各自排队；④ 50+ 级成功钓鱼概率掉落星之果茶（80 级约 20%）；⑤ 掉落消息 15 条随机中文显示；⑥ 背包满走原生溢出菜单；⑦ 鱼王/非鱼类不掉落。