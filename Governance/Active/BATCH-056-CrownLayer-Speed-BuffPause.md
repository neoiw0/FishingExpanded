# FishingExpanded 根因批次：BATCH-056 皇冠图层修复 + 手感回调 + 钓鱼 buff 暂停

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-15 |
| 当前活动类别 | CAT-01（皇冠图层 + 手感回调 + 食物 buff 暂停） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-056） |
| 本轮用户问题范围 | 用户确认：①普通皇冠完全看不见，图层应在鱼图标上面、详情信息下面（同意修复）；②α 最终速度改为 25、方向系数 ±0.2α；③提示距离采用方案 B（锚点不动）；④新增：钓鱼期间食物 buff 时间不要走；⑤新增：挑战鱼饵 5 分钟起接管原生 3 颗星，每分钟掉 1 颗、不可恢复、95 级以上豁免，每掉 1 颗鱼获 −20% |
| 本轮纳入 Case | FE-056-1 普通皇冠 layerDepth 缺失；FE-056-2 手感回调（25/±0.2）；FE-056-3 提示距离方案 B（不改，记录）；FE-056-4 钓鱼小游戏期间食物 buff 暂停；FE-056-5 原生挑战星接管（3 星/掉星/鱼获惩罚） |
| 共享第一处分歧与所有权链证据 | 皇冠：`CollectionsPagePatches.Draw_Postfix` 普通分支无 layerDepth（默认 0.0f→不可见）；手感：`ApplyBarInput.baseMaxSpeed=30×(1−0.3α)` + `GetDirectionFactor ±0.5α`；buff：`Farmer.update → BuffManager.Update → Buff.update`，钓鱼小游戏挂在 `Game1.activeClickableMenu`，食物 buff id="food"；挑战星：原生 `BobberBar.draw` 698-711 行用 `challengeBaitFishes` 画 3 颗星（实心 236,205/空心 217,205），原生 `challengeBaitFishes` 同时驱动脱杆失败与成功数量，模组需保持=3 禁用脱杆失败，显示由 Draw_Postfix 用原生贴图接管 |
| 排队或暂不纳入 Case | BATCH-052~055 待真实验收；日志刷屏滞回已部署待验收 |
| 合并/拆分决定及依据 | 四项同属展示/手感/计时边界，同轮实施；方案 B 无代码改动仅记录 |
| 本轮准入证据 | 用户确认指令；原生绘制/计时调用链（CollectionsPage.draw FrontToBack + 0.86f；Buff.update 源码；FishingRod.cs:939 activeClickableMenu=new BobberBar） |
| 现有实现复核 | 普通皇冠 `b.Draw(crownTexture, new Rectangle(...,24,24), crownSource, Color.White)`（无 layerDepth）；`ApplyBarInput` 21 基础 + `GetDirectionFactor 0.5`；`Buff.update` 无菜单暂停逻辑 |
| 允许修改范围 | `Source\Patches\CollectionsPagePatches.cs`（普通皇冠 layerDepth）、`Source\Patches\BobberBarPatches.cs`（ApplyBarInput/GetDirectionFactor）、新增 `Source\Patches\BuffPatches.cs`；文档：GAME-DESIGN.md、WIKI.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 存档结构；难度/结算；力竭/挑战鱼饵语义；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 普通皇冠改 `layerDepth+0.01f` 重载；`baseMaxSpeed = 30−5α`（α=1→25）；方向系数回 `±0.2α`；`Buff.update` Prefix 跳过食物 buff；挑战星接管：`GetChallengeStars(level, elapsed)`（≥95 恒 3；5:00→2、6:00→1、7:00→0，不恢复），`GetChallengeStarMultiplier=1−0.2×(3−stars)`（0.8/0.6/0.4），原生计数保持 3（禁用脱杆失败），Draw_Postfix 用原生空星贴图覆盖掉落位 |
| 可观测性决定 | 皇冠以真实验收画面为准；手感复用“手感系统激活”日志（方向系数 1.20/0.80、目标速度 30.0/20.0）；buff 暂停以钓鱼前后剩余时间不减少为准；挑战星掉星日志每颗 1 条（“挑战星减少 | 剩余星星: N/3 | 鱼获惩罚: -X% | 无法恢复”） |
| 自动化验收决定 | `fish_selftest` 不新增；真实钓鱼：皇冠可见、方向系数 1.20/0.80、食物 buff 暂停、挑战鱼饵 5:00/6:00/7:00 各掉 1 颗星且不恢复、≥95 级不掉星、超时成功鱼获按 0.8/0.6/0.4 结算 |
| 当前类别阶段 | R0 实施中 |
| 当前工作树 | 完整修改（BATCH-033~055 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `AFE11A4BF75BCDB6E080E5EDA03ABF3AFC152A4B7B32E3F160B57DFC3B01BEAA`；反编译核验普通皇冠 layerDepth+0.01f、`30f−5f×α`、`±0.2f×α`、BuffPatches（id=="food" && activeClickableMenu is BobberBar）已编译 |
| 唯一下一步 | 真实验收：收藏页普通皇冠可见；日志方向系数 1.20/0.80、目标速度 30.0/20.0；食物 buff 钓鱼期间不走时；挑战鱼饵 5:00/6:00/7:00 掉星且不恢复、≥95 不掉、鱼获 0.8/0.6/0.4 |
| 已消费动作 | `Build:ROUND-20260812-15:02CAE8FDE24E576D55053931D106BFE439979F84E107EF34AAA717B1B96BEE6F`；`Deploy:ROUND-20260812-15:02CAE8FDE24E576D55053931D106BFE439979F84E107EF34AAA717B1B96BEE6F` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-056-1/2/4/5 | 实施中 | 根因修复 | 手感激活日志；挑战星减少日志 | 真实钓鱼 + 收藏页画面 |
| CAT-01 | FE-056-3（方案 B） | 不改 | 记录 | 无 | 无 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-056-1 普通皇冠不可见 | 根因已证实 | 真实验收 |
| FE-056-2 手感回调 25/±0.2 | 设计已确认 | 真实验收 |
| FE-056-3 提示距离方案 B | 不改 | 无 |
| FE-056-4 钓鱼期间食物 buff 暂停 | 根因已证实 | 真实验收 |
| FE-056-5 原生挑战星接管 | 设计已确认（原生 3 星） | 真实验收（5:00 首掉星/95 豁免/鱼获惩罚） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `02CAE8FDE24E576D55053931D106BFE439979F84E107EF34AAA717B1B96BEE6F`（2026-08-12 部署；备份 `DeploymentBackups\FishingExpanded-20260812-201909-pre-BATCH056`=97E8CC75...；源/目标哈希一致；config.json 未触碰；i18n/manifest 未变化） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-056-1 | 普通皇冠完全不可见 | 0 | BATCH-048 后首次画面验收反证 | RC-056-1 | 已证实 | 收藏页普通皇冠可见 |
| FE-056-2 | α=1 背离速度过低 | 0 | BATCH-054 后实测反证 | RC-056-2 | 已证实 | 方向系数日志 1.20/0.80 |
| FE-056-4 | 长战斗中食物 buff 耗尽 | 0 | 用户要求 | RC-056-4 | 已证实 | buff 剩余时间钓鱼期间不变 |

## RC-056 根因卡

- 根因状态：已证实（源码 + 原生调用链）
- 第一处分歧：普通皇冠 Draw 重载无 layerDepth（默认 0.0f）在 FrontToBack 批次被压在底层；α=1 基础定速 21 且方向系数 0.5 使背离仅 10.5；Buff.update 在 `Game1.shouldTimePass()` 为真时逐帧扣时，钓鱼小游戏不暂停
- 决定性证据：`CollectionsPage.draw` 835-841 行（FrontToBack + 0.86f）；`Buff.update` 229-244 行；`Object.cs:5235`（food/drink id）；`FishingRod.cs:939`（activeClickableMenu=new BobberBar）
- 唯一所有者：`CollectionsPagePatches.Draw_Postfix` / `BobberBarPatches.ApplyBarInput` / `Buff.update`

### FE-056-1 反证 #1（2026-08-12 部署后仍不可见）

- 现象：BATCH-056 部署后普通/流动皇冠仍不可见；诊断日志（限频“皇冠绘制触发(任意组件)”）在打开收藏页时完全未出现
- 根因（已证实）：收藏页是 `GameMenu` 的子页面，`Game1.activeClickableMenu` 是 `GameMenu` 而非 `CollectionsPage`；BATCH-048 起组件后置的菜单门 `Game1.activeClickableMenu is CollectionsPage` 永远不成立，皇冠从未绘制（并非层级问题）
- 修复：移除菜单门，改挂 `CollectionsPage.draw` 前后置标志（`_collectionsDrawing/_collectionsTab`）限定绘制窗口；临时诊断已删除
- 部署：`2A86D412...`（2026-08-12 22:15；备份 pre-BATCH056-crownfix=4AF36431...）

## R0

- 普通皇冠：`b.Draw(crownTexture, new Vector2(bounds.X+3, bounds.Y+3), crownSource, Color.White, 0f, Vector2.Zero, 24f/20f, SpriteEffects.None, layerDepth+0.01f)`
- `ApplyBarInput`：`baseMaxSpeed = 30f − 5f×α`（α=1→25）
- `GetDirectionFactor`：`1f ± 0.2f×α`
- 新增 `BuffPatches.Update_Prefix`：`__instance.id=="food" && Game1.activeClickableMenu is BobberBar` → return false

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 普通皇冠无 layerDepth 重载 | 替换为带 layerDepth 重载 | 否 | 无 |
| `30×(1−0.3α)` 与 `±0.5α` | 替换为 `30−5α` 与 `±0.2α` | 否 | 文档同步 |

- 修改前写入者数量：1；修改后：1

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 收藏页普通皇冠 | 鱼图标上方可见、详情/鼠标下方 | 不可见/盖住详情 | 待部署后验证 |
| 流动皇冠 | 保持 1.2 倍与呼吸，层级不变 | 被普通分支改动影响 | 分支独立 |
| α=1 朝鱼 | 25×1.2=30px/帧 | 31.5 | 日志验证 |
| α=1 背离 | 25×0.8=20px/帧 | 10.5 | 日志验证 |
| 钓鱼中食物 buff | 剩余时间不减少 | 继续扣时 | 真实验收 |
| 钓鱼中饮料/其他 buff | 照常走时（本次范围仅食物） | 被暂停 | 条件只匹配 id="food" |
| 非钓鱼状态 | buff 正常走时 | 暂停 | 条件限定菜单 |
| 多人/分屏 | 按实例/屏幕独立 | 串玩家 | Game1 实例各自判定 |
| 存档 | 无新字段 | 写档 | 纯运行时 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `02CAE8FDE24E576D55053931D106BFE439979F84E107EF34AAA717B1B96BEE6F`（manifest 仍 0.5.10）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（默认部署授权）
- 部署状态：✅ 已部署（2026-08-12；备份 pre-BATCH056=97E8CC75...；源/目标哈希一致；config.json 未动）
- 本次单局路线：吃 +3 料理 → 钓一局长战斗 → 查 buff 剩余时间与手感日志 → 打开收藏页查皇冠
- 日志/截图/存档证据：待收集
- 每个 Case 的实际结果：待执行

## 收尾与归档

- 已完成：三项确认 + buff 新需求取证
- 当前不确定性：部署待游戏关闭；挑战星首掉星时刻采用 5:00（若用户要 6:00 首掉改一处常量即可）、超时鱼获惩罚替换“直接取消 ×1.5”（按用户“每颗 −20%”实施）
- 下一条准确操作：用户关闭游戏 → 备份部署 → 真实验收
- 总账与测试路线是否已覆盖更新：本卡 + BUG-LEDGER 同步中
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：否（待真实验收）
