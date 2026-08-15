# FishingExpanded 根因批次：BATCH-043 传奇鱼皇冠回填（装模组前已钓到）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-02 |
| 当前活动类别 | CAT-01（传奇鱼皇冠回填） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-02:9D199BD1... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-043） |
| 本轮用户问题范围 | 用户 2026-08-12 确认：装模组前已钓到的原版 5 条传奇鱼，应在 SaveLoaded 时扫描 `farmer.fishCaught` 补发皇冠并写回 |
| 本轮纳入 Case | FE-043-1 装模组前已钓传奇鱼无皇冠、α 无法计数（无回填路径） |
| 共享第一处分歧与所有权链证据 | 皇冠唯一写入者=`DifficultyManager`（CollectionStars）；第一处分歧=`RecordLegendaryCatch` 只在实时钓获边界（`PullFishFromWater_Prefix`）调用，加载存档无任何回填；原生 `Farmer.caughtFish` 反编译契约：`fishCaught` 键=限定 ID（如 `(O)163`）、`value[0]`=累计钓获数、鱼塘不计入（`!from_fish_pond` 才写入） |
| 排队或暂不纳入 Case | BATCH-042（已部署 AD2C6ED4…）待真实集中测试；BATCH-039/040/041 同待验收 |
| 合并/拆分决定及依据 | 单 Case 单类别：全部修改处于同一皇冠写入者与同一用户要求 |
| 本轮准入证据 | 用户 2026-08-12 明确要求；`_analysis\StardewValley.Farmer.decompiled.cs:2995-3016` 原生契约（限定 ID 键、value[0] 累计、鱼塘排除）；`CollectionsPage.full.cs:792` 读键格式 `"(O)"+id` 印证 |
| 现有实现复核 | `RecordLegendaryCatch` 只在 `PullFishFromWater_Prefix`（FishingRodPatches.cs:354）实时调用；`OnSaveLoaded` 只做 `LoadData`/清 HUD/清展示，无 fishCaught 扫描；`NormalizeData` 不生成新皇冠 |
| 允许修改范围 | `Source\Utils\SpecialFishHelper.cs`（暴露 5 传奇 ID）、`Source\Services\DifficultyManager.cs`（回填方法）、`Source\ModEntry.cs`（SaveLoaded 调用）、`Source\i18n\default.json`/`zh.json`（重置提示补充说明）；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、BATCH-042 卡、本卡 |
| 冻结 Case/禁止范围 | 难度/数量/品质/尺寸/经验/鱼王豁免等结算所有权；BATCH-042 重置按钮其他语义；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | SaveLoaded 后扫描本地玩家 `farmer.fishCaught` 中 5 个原版传奇 ID（`(O)159/160/163/682/775`），`value[0]>0` 即补 `CollectionStars` 并写回；幂等（已有皇冠跳过，无变化不写） |
| 可观测性决定 | 回填每玩家每次加载最多 5 次 `TryGetValue`；补发时每个 ID 各 1 条 Warn 日志；无新增逐帧诊断 |
| 自动化验收决定 | 场景=全量 `Saves` 沙箱；入口=存档加载（构造含 5 传奇 `fishCaught` 记录、清空 CollectionStars 的沙箱存档）；成功判据=重载后 5 皇冠出现、α 计数增加、`fish_bonus` 正确、无重复写、其他玩家不串；失败判据=未补发、误补给无记录玩家、鱼塘记录被计入、每次加载反复写 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~042 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608`；反编译核验 OnSaveLoaded→BackfillLegendaryCrowns、fishCaught.TryGetValue(限定ID)+value[0]>0、CollectionStars.Add、回填日志已编译 |
| 唯一下一步 | 真实集中测试（沙箱重载补发 + 幂等 + 重置交互） |
| 已消费动作 | `Build:ROUND-20260812-02:9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608`；`Deploy:ROUND-20260812-02:9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-043-1 | 代码侧结束 | 根因修复 | 回填补发每 ID 1 条 Warn | 存档加载（SaveLoaded） |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-043-1 装前已钓传奇无皇冠 | R2 完成 | 沙箱验收（重载补发） |

> 机制断言：存档写入行为修改（补写 CollectionStars），必须走全量 `Saves` 沙箱验收；不部署不启动游戏前只做静态+构建证据。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01/FE-043-1 | 沙箱存档含 5 传奇 fishCaught + 空 CollectionStars → 重载 | 5 皇冠自动补发、`fish_bonus` 可计数皇冠 +5、无重复写 | 存档修改 |
| 2 | CAT-01/FE-043-1 | 重置按钮清空后重载 | 传奇皇冠按图鉴记录自动补回（交互语义），普通星标不恢复 | 存档修改 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608`（2026-08-12 12:21 部署；备份 `DeploymentBackups\FishingExpanded-20260812-122114-pre-BATCH043`=AD2C6ED4...；源/目标哈希一致；config.json 未触碰） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-043-1 | 装模组前已钓的传奇鱼没有皇冠，α 拿不到对应手感 | 0 | 无 | RC-043 | 实施中 | 重载后 5 传奇皇冠出现 |

## RC-043 根因卡

- 根因状态：已证实（源码 + 原生契约）
- 第一处分歧：皇冠只在实时钓获边界发放，加载存档时没有任何基于原生图鉴记录的补发路径
- 决定性证据：`RecordLegendaryCatch` 调用点唯一（FishingRodPatches.cs:354）；全源码无 `fishCaught`/`collectedFish` 扫描；原生 `Farmer.caughtFish` 只把非鱼塘钓获写入 `fishCaught`（限定 ID 键、value[0] 累计）
- 竞争解释表：竞争解释豁免（理由=新功能/迁移要求，无“玩家现象由哪个根因造成”的互斥假设；静态契约已完整证明缺失回填路径）
- 唯一所有者：`DifficultyManager`（CollectionStars）
- 最短区分动作：实现 SaveLoaded 回填并沙箱验证

## RC-043 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 回填皇冠 | 每玩家各自本地执行（Game1.player） | `DifficultyManager.BackfillLegendaryCrowns` | 按玩家缓存 | modData NetField 同步 | 标题 `UnloadData` 后下次加载重扫（幂等） | 沙箱重载 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 传奇皇冠 | 实时钓获/回填 | `DifficultyManager` | α/图鉴/助战 | 每玩家 ≤5 个 | 重置（下次加载按图鉴补回） | 标题清理缓存 |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`AD2C6ED4…`（BATCH-042 部署版）；游戏 1.6.15.24354、SMAPI 4.5.2
- Expanded 接管入口：`OnSaveLoaded → LoadData`（新增回填调用）
- 原生上游入口和状态字段：`Farmer.fishCaught`（`NetStringIntArrayDictionary`，限定 ID 键，value[0]=累计钓获数，value[1]=最大尺寸）
- 原生提交方法：`Farmer.caughtFish`（非鱼塘才写入）
- 后续回调、同步和生命周期：`CollectionsPage` 读 `fishCaught`（限定 ID）；modData 同步
- Harmony 拦截点：无新增
- 第一处分歧：无回填路径
- 尚未读取或仍不确定的环节：无（沙箱真实验收待执行）

## R0

- 修改第一处错误决策：SaveLoaded 后按 `fishCaught` 幂等补发 5 原版传奇皇冠并写回
- 新权威入口：`DifficultyManager.BackfillLegendaryCrowns`（唯一写入者）
- R1 待删除旧路径：无

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 无 | 新功能，无替代路径 | - | - |

- 修改前写入者数量：1（`DifficultyManager`）
- 修改后写入者数量：1（`DifficultyManager`）

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 装前已钓 5 传奇 → 重载补 5 皇冠、α 增加 | 误给未钓玩家 | 静态待构建 |
| 本地主屏/副屏 | 各自只补本屏玩家 | 串到另一屏 | 静态待构建 |
| 远程与四人混合 | 农场客各自补自己并同步 | 主机代写他人 | 静态待构建 |
| 鱼塘记录 | 原生 fishCaught 不含鱼塘，天然不计 | 鱼塘计入 | 静态待构建 |
| 重置交互 | 重置清空后立即回填传奇皇冠（BATCH-046，同会话生效）；普通星标不恢复 | 每次加载重复写/刷日志 | 静态待构建 |
| 性能最坏情况 | 每次加载 5 次 TryGetValue，无变化不写 | 每帧扫描 | 静态待构建 |
| 已验收相邻回归 | 实时钓获、BATCH-042 重置、鱼王豁免不受影响 | 行为回退 | 静态待构建 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）
- 本次单局路线：全量 `Saves` 沙箱 + 重载核验 + 重置交互核验
- 日志/截图/存档证据：待集中测试封存
- 每个 Case 的实际结果：待执行

## 收尾与归档

- 已完成：契约核对、批次卡、R0 设计
- 当前不确定性：重置后自动补回是否为用户期望（当前按用户字面指令=每次加载扫描补发；如需“只补一次”需一次性标记方案）
- 下一条准确操作：R0 代码实现 → Release Rebuild → 静态核验 → 等待用户授权部署/实测
- 总账与测试路线是否已覆盖更新：实施中同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收完成后可归档
