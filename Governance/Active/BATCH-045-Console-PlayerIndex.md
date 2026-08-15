# FishingExpanded 根因批次：BATCH-045 控制台多玩家目标（玩家序号）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-04 |
| 当前活动类别 | CAT-01（控制台多玩家目标） |
| 整合候选状态 | 已部署待集中测试（随 BATCH-047 统一部署；Deploy:ROUND-20260812-06:DE23813F...） |
| 当前权威活动卡 | 本卡（BATCH-045） |
| 本轮用户问题范围 | 用户 2026-08-12 要求控制台命令支持给存档其他玩家操作：`fish_addstars 20`=主机、`fish_addstars 1 20`=主机、`fish_addstars 2 20`=副机，以此类推 |
| 本轮纳入 Case | FE-045-1 数据/查询命令只作用于当前玩家，无法给副机/农场客加数据 |
| 共享第一处分歧与所有权链证据 | 命令目标选择=ModEntry 各 handler 直接用 `Game1.player`；第一处分歧=缺少可选玩家序号解析；原生 `Game1.getAllFarmers()` 契约=主机（MasterPlayer）+ 在线/离线农场客（`Enumerable.Repeat(MasterPlayer,1).Concat(getAllFarmhands())`），顺序固定 |
| 排队或暂不纳入 Case | BATCH-042/043/044 已构建/部署待验收；BATCH-039/040/041 同待验收 |
| 合并/拆分决定及依据 | 单类别：全部修改为同一命令解析助手与同一用户约定 |
| 本轮准入证据 | 用户明确语法；`_analysis\StardewValley.Game1.decompiled.cs:10955-10959` 原生契约（getAllFarmers 含离线农场客，index 0=主机） |
| 现有实现复核 | 11 个命令（setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus/addstars/challengecrown）全部使用 `Game1.player`；鱼 ID 均 ≥128，玩家序号 1..4 无歧义 |
| 允许修改范围 | `Source\ModEntry.cs`（帮助文本、`TryParseTargetPlayer`、11 个命令 handler）；文档：TESTING-GUIDE.md、WIKI.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | `DifficultyManager`/`GiantFishManager` 写入语义；结算所有权；GMCM；`fish_assist`/`fish_persisttest`/`fish_selftest`/`fish_assiststats`（当前玩家实例命令）不改；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 可选玩家序号作为首参：`[玩家序号]`（1=主机，2=第一个农场客/副机…，省略=当前玩家）；顺序=Game1.getAllFarmers()；`fish_list`/`fish_bonus` 支持单独序号 |
| 可观测性决定 | 目标玩家名写入关键回显；无新增诊断标签 |
| 自动化验收决定 | 场景=全量 `Saves` 沙箱（双人分屏/联机）；入口=`fish_addstars 2 20`、`fish_bonus 2`、`fish_info 2 128`、`fish_clear 2 confirm` 等；成功判据=只改目标玩家、回显含玩家名、无序号行为与旧版一致；失败判据=改错玩家、无序号行为变化、歧义解析 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~044 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `260D2FFF88C04755C98BC8EC17BC5541A9CF2233E51203F33B96AD704D4A3EC5`；反编译核验 `TryParseTargetPlayer` + `Game1.getAllFarmers()` + 11 个命令帮助文本/调用已编译 |
| 唯一下一步 | 用户授权部署后统一集中测试（序号命令沙箱验收） |
| 已消费动作 | `Build:ROUND-20260812-04:260D2FFF88C04755C98BC8EC17BC5541A9CF2233E51203F33B96AD704D4A3EC5` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-045-1 | 代码侧结束 | 根因修复 | 回显含玩家名 | 11 个控制台命令（可选玩家序号） |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-045-1 命令无法指定其他玩家 | R2 完成 | 沙箱验收（序号命令） |

> 机制断言：测试/管理命令行为修改（写存档数据，但写入者仍为 DifficultyManager），必须走全量 `Saves` 沙箱验收。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01/FE-045-1 | `fish_addstars 20` / `fish_addstars 1 20` / `fish_addstars 2 20` | 前两者=主机，第三者=副机；`fish_bonus 2` 复核 | 存档修改 |
| 2 | CAT-01/FE-045-1 | `fish_clear 2 confirm`、`fish_info 2 128` | 只清副机、只读副机；主机不受影响 | 存档修改 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | 本卡候选 `260D2FFF88C04755C98BC8EC17BC5541A9CF2233E51203F33B96AD704D4A3EC5`；已随 BATCH-047 统一部署 `DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`（2026-08-12 13:16，备份 `DeploymentBackups\FishingExpanded-20260812-131648-pre-BATCH047`） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-045-1 | 控制台只能给当前玩家加数据 | 0 | 无 | RC-045 | 实施中 | 副机序号生效 |

## RC-045 根因卡

- 根因状态：已证实（源码 + 原生契约）
- 第一处分歧：命令 handler 全部硬编码 `Game1.player`，无玩家选择器
- 决定性证据：ModEntry 11 个 handler 逐条复核；`Game1.getAllFarmers()` 原生实现（主机+在线/离线农场客）
- 竞争解释表：竞争解释豁免（理由=新功能要求，无互斥根因）
- 唯一所有者：`ModEntry`（命令解析）；数据写入仍为 `DifficultyManager`
- 最短区分动作：实现玩家序号解析并在沙箱验收

## RC-045 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 多玩家命令 | 主机控制台 | `DifficultyManager`（目标玩家参数） | 无 | modData NetField | 标题清理 | 沙箱 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 挑战数据 | 命令/钓鱼/GMCM | `DifficultyManager` | HUD/图鉴/α | 每玩家 | 重置/卸载 | 标题 `UnloadData` |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`9D199BD1…`（BATCH-043 部署版；BATCH-044 未部署）
- Expanded 接管入口：SMAPI 控制台命令 → `TryParseTargetPlayer` → 目标玩家参数
- 原生上游入口和状态字段：`Game1.getAllFarmers()`（主机 + 在线/离线农场客）
- 原生提交方法：`Farmer.modData`（目标玩家）
- 后续回调、同步和生命周期：modData NetField 同步；离线农场客上线后其客户端可能覆盖主机测试写入（本机分屏不受影响）
- Harmony 拦截点：无新增
- 第一处分歧：无玩家选择器
- 尚未读取或仍不确定的环节：无

## R0

- 修改第一处错误决策：新增 `TryParseTargetPlayer`（可选首参序号，1=主机），11 个命令接入
- 新权威入口：`TryParseTargetPlayer`（唯一解析助手）
- R1 待删除旧路径：无（向后兼容：无序号=当前玩家）

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 无 | 向后兼容新增可选参数 | - | - |

- 修改前写入者数量：1（`DifficultyManager`）
- 修改后写入者数量：1（`DifficultyManager`）

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 单人 | 无序号=当前玩家；`fish_addstars 1 20`=主机 | 行为回退 | 静态待构建 |
| 双人分屏 | `2`=副机，只改副机 | 串改主机 | 静态待构建 |
| 四人联机 | 序号 1..4 对应 getAllFarmers 顺序 | 序号越界误写 | 静态待构建 |
| 离线农场客 | 序号可指向离线农场客并写入其 modData | 找不到目标报错 | 静态待构建 |
| 歧义边界 | 鱼 ID≥128 不与序号冲突；`fish_clear 2 confirm` 解析正确 | 把鱼 ID 当序号 | 静态待构建 |
| 单序号命令 | `fish_list 2`/`fish_bonus 2` 生效 | 被当作计数 | 静态待构建 |
| 不适用命令 | assist/persisttest/selftest/assiststats 保持当前玩家语义 | 误加序号解析 | 静态待构建 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `260D2FFF88C04755C98BC8EC17BC5541A9CF2233E51203F33B96AD704D4A3EC5`（manifest 版本仍为 0.5.10）
- 部署文件与目标：未部署（需用户授权）
- 本次单局路线：全量 `Saves` 沙箱 + 序号命令组合
- 日志/截图/存档证据：待集中测试封存

## 收尾与归档

- 已完成：契约核对、批次卡、R0 设计
- 当前不确定性：离线农场客上线覆盖行为需实测确认（文档已注明）
- 下一条准确操作：R0 代码实现 → Release Rebuild → 静态核验 → 等待用户授权部署/实测
- 总账与测试路线是否已覆盖更新：实施中同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收完成后可归档
