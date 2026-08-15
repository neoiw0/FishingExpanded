# FishingExpanded 根因批次：BATCH-065 蟹笼收获纳入难度等级（按水藻类非鱼规则）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260815-09 |
| 当前活动类别 | CAT-01（用户 2026-08-15 指令：蟹笼里面拿到的物品也加难度等级，蟹笼里的按水藻这类处理） |
| 整合候选状态 | 已统一构建（待部署授权） |
| 当前权威活动卡 | 本卡（BATCH-065） |
| 本轮用户问题范围 | 用户 2026-08-15 提问确认：①蟹笼收获全部物品（含龙虾/螃蟹等 Category=-4 的原生"鱼"）一律按水藻类非鱼规则（上限 8 级）；②每次收获等级 +1；③应用数量倍数（等级越高一次收得越多）；④左下角提示只在等级提升时显示（含首次满 8 级"你已是帝王"），平时收获不刷屏 |
| 本轮纳入 Case | FE-065-1 蟹笼收获记录难度等级（+1/次，上限 8，星星，升级提示）；FE-065-2 蟹笼收获应用数量倍数；FE-065-3 蟹笼鱼归入非鱼判定（IsNonFishItem/GetMaxDifficultyLevel/HUD 封顶文案/星星） |
| 共享第一处分歧与所有权链证据 | 蟹笼收获唯一原生入口=`CrabPot.checkForAction`（tileIndexToShow==714 分支，`_analysis\crabpot\StardewValley.Objects.CrabPot.decompiled.cs:270-343`）；物品入包=`who.addItemToInventoryBool`（失败 return false 收获取消）；图鉴记录=`who.caughtFish`（仅 Data/Fish 有记录的蟹笼鱼；垃圾/藻类不调用）；经验=`who.gainExperience(1,5)`（原生固定 5 点）；等级/星标唯一写入者=`DifficultyManager.RecordSuccess`；成功提示=`HUDNotifier.ShowSuccessNotification`；现有 caughtFish Postfix 依赖 FishingRodPatches 待处理事实（蟹笼无待处理事实→天然跳过，无重复结算） |
| 排队或暂不纳入 Case | BATCH-059A/060/061/062/064l 待真实验收（本卡不重复修改）；蟹笼收获不实施每日收获限额（BATCH-061 的 333/天：蟹笼每笼每天 1 次（书《Crabbing》25%×2），全图笼子数量有自然上限，远达不到 333；且钓鱼路径限额靠 CreateFish_Postfix 置 Stack=0，蟹笼路径 addItemToInventoryBool 对 0 堆叠物品的行为不可控，无实际收益，故不实现，若用户要求再扩展） |
| 合并/拆分决定及依据 | 单类别（一个用户指令、一个权威原生入口、一个结算边界），不拆分 |
| 本轮准入证据 | 用户 2026-08-15 明确修改要求 + 4 项设计口径逐条确认；原生 `CrabPot.checkForAction` 当前安装 DLL 反编译（收获路径唯一入口、入包顺序、caughtFish/gainExperience 调用点）；`_analysis\Fish-data-extracted.txt`（10 种 trap 鱼 372/715-723）；Objects.xnb 解包（蟹笼鱼 Category=-4、藻类 Category=0、垃圾 Category=-20——`_analysis\xnb-tool`）；SMAPI-latest.txt 运行时日志（绿藻/垃圾 非鱼类=True、狗鱼=False，佐证 Category 判定现状） |
| 现有实现复核 | `DifficultyManager.IsNonFishItem`（Category≠-4 → 蟹笼鱼 Category=-4 会被当普通鱼/上限 100，需显式归入非鱼）；`RecordSuccess`（非鱼 newLevel≥8 → CollectionStars 星星，幂等）；`GetMaxDifficultyLevel`（IsNonFishItem → 8）；`HUDNotifier.ShowSuccessNotification`（isNonFish 用 Category 判定，需统一到 IsNonFishItem）；`CollectionsPagePatches`（星星/皇冠按 IsCountableFish，蟹笼鱼不在 61 池 → 自动画金星+"无手感增强"，无需改）；`caughtFish Postfix` 防重（TryGetPending 无蟹笼条目 → 天然不重复结算）；经验倍数路径（gainExperience Prefix 依赖钓鱼待处理事实，蟹笼无 → 保持原生 5 点） |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`（IsNonFishItem/新增 IsCrabPotFish）、`Source\Services\HUDNotifier.cs`（isNonFish 判定统一）、新增 `Source\Patches\CrabPotPatches.cs`、`Source\ModEntry.cs`（ReturnedToTitle 清理 + fish_selftest 蟹笼断言）；文档：GAME-DESIGN.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 61 可计数池；鱼王豁免；助战；挑战鱼饵；存档结构；GMCM；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | ①`CrabPot.checkForAction` 收获边界：Prefix 应用数量倍数（`GetQuantityMultiplier(level)`，8 级=×8，在原生 Book×2 之前叠加）+ 记录待结算（玩家→物品/旧等级）；Postfix 在 `__result==true` 且物品已入包后 `RecordSuccess(itemId, +1)`，`newLevel>oldLevel` 时 `ShowSuccessNotification`（含 8 级封顶帝王文案）；②`DifficultyManager.IsNonFishItem` 增加蟹笼鱼判定（Data/Fish 带 trap 标签 → 非鱼）：上限 8、8 级星星、HUD 封顶文案、图鉴"无手感增强"全部自动生效 |
| 可观测性决定 | 每次蟹笼收获结算 1 条 Info 日志（`[CrabPot] 蟹笼收获记录`，含玩家/物品/等级变化/实际增长）；前缀异常 Error；无逐帧输出；不新增诊断标签（复用 FishingLog） |
| 自动化验收决定 | `fish_selftest` 增 4 条只读断言：蟹笼鱼非鱼判定（715→true、723→true）、普通鱼非鱼判定（128→false）、蟹笼鱼上限=8、数量倍数 8 级=8；真实场景：蟹笼收获 → 等级+1、升到 8 级给星标+帝王提示、8 级每次收获 ×8（书×2 时 ×16）。存档影响=有（难度等级写入 modData，与钓鱼难度同一数据边界 `FishDifficultyData`，同一玩家存档路径，无需新沙箱） |
| 当前类别阶段 | R0/R1/R2 完成；代码侧结束 |
| 当前工作树 | 完整修改（历史混合未提交基线 + 本类别修改） |
| 本轮统一构建 | `833A0812A16D73670AD9BBE9A6162A17FF656B7CE542500A56AAC5209072E2D0`（Release Rebuild，0 警告 0 错误，2026-08-15；反编译核验通过：CrabPotPatches Prefix/Postfix、IsCrabPotFish、IsNonFishItem 统一、GetMaxDifficultyLevel=IsNonFishItem、selftest 5 条蟹笼断言、HUDNotifier 统一判定，证据 `_analysis\batch065-verify`） |
| 唯一下一步 | 用户授权部署 → 部署核验 → 玩家真实验收（fish_selftest 蟹笼 5 条 + 真实蟹笼收获） |
| 已消费动作 | 无 |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单线程功能批次，主线程已掌握全部调用链；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-065-1/2/3 | 实施中 | 根因修复（新功能） | `[CrabPot]` 收获日志（每收获 1 条） | 真实蟹笼收获 + fish_selftest |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-065-1 蟹笼收获等级/星星/提示 | 设计已确认 | R0 实施 + 构建 + 真实验收 |
| FE-065-2 蟹笼收获数量倍数 | 设计已确认 | R0 实施 + 构建 + 真实验收 |
| FE-065-3 蟹笼鱼归入非鱼判定 | 设计已确认 | R0 实施 + 构建 + 真实验收 |

> 机制断言：本批为新功能（用户确认设计），非 Bug 根因修复；竞争解释表不适用（无互斥假设需区分），准入证据=用户确认 + 原生调用链取证。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---|---:|---|---|---|
| 1 | CAT-01 / FE-065-1/2/3 | fish_selftest 蟹笼断言 + 真实蟹笼收获 | 通过 / 未执行 | 等级+1、8 级星标+帝王提示、×8 数量 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | |

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-065-1 | 蟹笼收获不参与难度等级 | 0 | 无（新功能） | RC-01 | 设计已确认 | 收获后 fish_info 等级+1 |
| FE-065-2 | 蟹笼收获数量不随等级增加 | 0 | 无（新功能） | RC-01 | 设计已确认 | 8 级收获 8 个 |
| FE-065-3 | 蟹笼鱼（龙虾等）图鉴按普通鱼上限 | 0 | 无（新功能） | RC-01 | 设计已确认 | 8 级封顶+星星+帝王提示 |

## RC-01 根因卡

- 根因状态：不适用（新功能，用户确认设计，无 Bug 根因）
- 原生调用链（已完成，满足"完成调用链再进入 R0"）：
  - 当前安装 DLL 版本/哈希：`D:\GGGGG\K1515\Stardew Valley.dll`，2024-12-21 04:13，6268416 字节
  - Expanded 接管入口：`CrabPot.checkForAction`（Harmony Prefix + Postfix，收获唯一权威点）
  - 原生上游入口和状态字段：`tileIndexToShow==714`（有货）、`heldObject.Value`（NetRef 产物）、`bait.Value`、`readyForHarvest`、书《Crabbing》25%×2（`Book_Crabbing` 统计）
  - 原生提交方法：`who.addItemToInventoryBool(value)` 成功 → `who.caughtFish(...)`（仅 Data/Fish 有记录的蟹笼鱼）→ `who.gainExperience(1, 5)`；背包满 → 恢复 heldObject + return false（收获取消，等级不结算）
  - 后续回调、同步和生命周期：heldObject 是 NetRef（联机同步）；盖子动画 `updateWhenCurrentLocation`；DayUpdate 生成次日产物；`attemptRemoval`/`performRemoveAction` 不收获
  - Harmony 拦截点：Prefix（倍数+待结算登记）、Postfix（成功结算）；与现有 `Farmer.caughtFish` Postfix 防重隔离（无 FishingRod 待处理事实 → 跳过）
  - 尚未读取或仍不确定的环节：无（收获路径完整取证）
  - 旧版只读参考及可接受差异：无

## 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 蟹笼收获结算 | 操作玩家（`who.IsLocalPlayer` 才结算；双人同屏=当前屏幕玩家） | `DifficultyManager.RecordSuccess` | 按玩家 UniqueMultiplayerID 键控待结算单槽 | 无广播（等级入 modData 随存档同步） | 返回标题清空待结算（ClearPending） | fish_selftest 只读断言 |
| 数量倍数 | 操作玩家 | Prefix 改 heldObject.Stack（原生随后 Book×2 叠加） | 原生 NetRef heldObject 同步 | 无 | 无 | 真实收获 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 蟹笼待结算（玩家→物品/旧等级） | `CrabPotPatches` Prefix | `CrabPotPatches` | 同 Postfix（一次性） | 每玩家 1 槽，每次收获周期内存在 | Postfix 结算/取消即移除 | 标题 ReturnedToTitle 清空；远程玩家不记录 |

## R0

1. `DifficultyManager`：新增 `IsCrabPotFish(string)`（`DataLoader.Fish(Game1.content)` 含该 rawId 且值含 `"trap"`，与原生 DayUpdate 判定一致；try-catch 兜底）；`IsNonFishItem` = Category≠-4 或蟹笼鱼（蟹笼鱼 Category=-4 显式归入非鱼）。
2. `HUDNotifier.ShowSuccessNotification`：isNonFish 判定统一改调 `DifficultyManager.IsNonFishItem`（消除第二处 Category 判定）。
3. 新增 `Source\Patches\CrabPotPatches.cs`：
   - `checkForAction` Prefix：`tileIndexToShow==714 && !justCheckingForActivity && who.IsLocalPlayer && heldObject!=null` → `held.Stack *= GetQuantityMultiplier(level)`（防溢出；原生 Book×2 在其后叠加）→ 待结算 `{ItemId, OldLevel}` 按玩家 ID 存单槽。
   - `checkForAction` Postfix：`__result==false` 或非本地玩家 → 清待结算；`__result==true` 且有待结算 → `RecordSuccess(itemId, +1, who)`；`newLevel > oldLevel` → `ShowSuccessNotification(itemId, newLevel, oldLevel)`（首升 1 级与首次满 8 级各提示一次，平时不提示）；Info 日志。
   - `ClearPending()` 静态清理。
4. `ModEntry`：`OnReturnedToTitle` 调 `CrabPotPatches.ClearPending()`；`fish_selftest` 增 4 条蟹笼只读断言。
5. 文档：GAME-DESIGN §6.2 增蟹笼规则；术语表/§1.2 数量倍数公式同步 BATCH-062（难度等级×1）。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `HUDNotifier` 内独立 Category 判定（`SpecialFishHelper.IsNonFish(itemData.Category)`） | 替换为 `DifficultyManager.IsNonFishItem(fishId)` 统一调用 | 否 | 无（单一判定所有者=DifficultyManager） |

- 修改前写入者数量：等级结算=RecordSuccess（1）；数量倍数=钓鱼 CreateFish 边界（1）+ 新增蟹笼边界（1，不同原生入口，独立生命周期）
- 修改后写入者数量：等级=1；数量=2 个原生入口各 1（钓鱼/蟹笼路径互不重叠，不构成双写同一状态）
- 运行时代码新增：约 90 行（CrabPotPatches + IsCrabPotFish + 判定统一）；净增长理由：新原生入口（蟹笼收获）需要自己的边界；复用现有 RecordSuccess/HUDNotifier/GetQuantityMultiplier，不新增第二套等级状态。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人：蟹笼收获龙虾 | 等级+1；升级/满 8 级提示；8 级每次 ×8（书×2=×16） | 每次收获刷屏提示；等级超过 8 | 待构建后验证 |
| 蟹笼垃圾（168-172）收获 | 同样记录等级（Category=-20 已非鱼，走同一边界） | 被钓鱼路径重复结算 | 边界唯一 |
| 蟹笼鱼图鉴 | 8 级封顶；星星（金星+"无手感增强"）；挑战等级行 | 按普通鱼上限 100；显示皇冠 | IsNonFishItem/IsCountableFish |
| 背包满时收获 | 收获取消（红字），等级不结算、倍数不丢失 | 等级+1 但没拿到物品 | Postfix 判 __result |
| 海草/绿藻/白藻（Category=0 非鱼） | 不受影响（无 trap → IsCrabPotFish=false，Category 判定已覆盖） | 被误判为普通鱼 | Category=0 ≠ -4 |
| 钓鱼路径 | 完全不变（trap 鱼不可钓；caughtFish Postfix 防重跳过蟹笼） | 蟹笼收获触发钓鱼结算 | TryGetPending |
| 双人同屏/联机 | 只结算操作玩家（who.IsLocalPlayer）；等级按玩家隔离 | 副屏/远程玩家被串写 | 键控 |
| 换日/标题 | 标题清空待结算；DayStarted 无残留 | 残留导致下次误结算 | ClearPending |
| 性能最坏情况 | 每收获 1 条 Info 日志；DataLoader.Fish 字典查找仅对 Category=-4 物品 | 逐帧输出 | 触发频率=点击 |
| 已验收相邻回归 | BATCH-061 限额、BATCH-060 数量/品质、图鉴星星不受影响 | 蟹笼路径触碰 CreateFish 边界 | 不修改该边界 |

## 构建、部署与集中测试

- 构建结果：待执行（0 警告 0 错误目标）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（待用户授权）
- 部署状态：未部署
- 本次单局路线：`fish_selftest`（蟹笼断言 4 条 PASS）→ 真实蟹笼收获（等级/提示/倍数/星星）
- 日志/截图/存档证据：待执行
- 每个 Case 的实际结果：待执行

## 收尾与归档

- 已完成：设计确认、原生调用链取证、方案设计
- 当前不确定性：无（静态契约可完整证明；真实验收待用户）
- 下一条准确操作：R0 代码实施
- 总账与测试路线是否已覆盖更新：实施后同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收后
