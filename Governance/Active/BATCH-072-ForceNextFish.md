# FishingExpanded 活动卡：BATCH-072 强制下一次钓鱼鱼种测试命令

> 用户明确要求“下一次必然钓到鱿鱼”，并需要主屏/副屏玩家同样可用。新增薄控制测试命令 `fish_next`，不写存档、不改玩法默认行为。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260818-01` |
| 当前活动类别 | CAT-01（强制下一次鱼种测试命令） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-072） |
| 本轮用户问题范围 | 用户要求提供并实现“增加 20 皇冠 + 鱿鱼 100 级 + 下一次必然钓到鱿鱼”的测试功能；并补充副屏玩家同样需要这些指令 |
| 本轮纳入 Case | FE-072-1 `fish_next [玩家序号] <鱼ID>` 强制下一次钓鱼小游戏鱼种 |
| 共享第一处分歧与所有权链证据 | 不适用（新测试命令，无根因分歧）。唯一接入口=BobberBar 构造函数构造边界，原生 `BobberBar` 的 `whichFish`/`setFlagOnCatch` 参数在构造前被消费式替换；最终 `pullFishFromWater(whichFish,...)` 与 `FishingRod.whichFish` 均读取 BobberBar 字段，因此同一入口覆盖小游戏与结算 |
| 排队或暂不纳入 Case | BATCH-069/070/071 已部署待集中测试；本轮不触碰其运行逻辑 |
| 合并/拆分决定及依据 | 单类别单 Case，直接实施 |
| 本轮准入证据 | 用户 2026-08-18 明确要求“需要这个功能，开始做吧” |
| 现有实现复核 | 已核对 `FishingRod.startMinigameEndFunction`（`_analysis\StardewValley.FishingRod.decompiled.cs:891-940`）与 `BobberBar` 构造函数（`_analysis\StardewValley.BobberBar.decompiled.cs:138-151/361`）：BobberBar 构造参数 `whichFish` 同时决定小游戏、`pullFishFromWater` 与最终鱼获；原生期望未限定 ID（如 `151`），Mod 内部统一使用 `(O)151` |
| 允许修改范围 | `Source/ModEntry.cs`、`Source/Patches/BobberBarPatches.cs`、`TESTING-GUIDE.md`、`TESTING.md`、`WIKI.md`、`BUG-LEDGER.md`、本卡 |
| 冻结 Case/禁止范围 | 其余机制全部冻结；不修改存档、config.json、不部署、不启动游戏 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 新增 `fish_next [玩家序号] <鱼ID>`：为指定玩家设置下一次钓鱼小游戏固定鱼种；BobberBar 构造边界消费一次；支持主屏/副屏玩家序号 |
| 可观测性决定 | 构造消费时 1 条 Info 日志 `[BobberBar] 强制鱼种生效`；命令设置成功 1 条 Info；无逐帧/高频输出 |
| 自动化验收决定 | 复用 `fish_selftest`：新增强制鱼种标志生命周期只读自测（空闲 null → 置位 (O)151 → 消费 → 清除）；运行验收=真实钓鱼一杆，日志出现“强制鱼种生效”且最终鱼获为指定鱼。存档影响=无（会话态标志，不写存档） |
| 当前类别阶段 | 代码侧结束（已部署，待真实验收） |
| 当前工作树 | 部分修改（含 BATCH-069/070/071 未提交源码与文档 + 本批修改） |
| 本轮统一构建 | 未作为统一候选发布；单类 Release Rebuild 已通过（0 警告 0 错误），DLL SHA256 `1E65AB958AC0873A48DC86DCB967A532DAEE4D52CE96C3B5967D094ACCEDF245` |
| 唯一下一步 | 用户启动游戏真实验收：`fish_addstars 20` / `fish_setlevel 151 100` / `fish_next 151`；副屏 `fish_addstars 2 20` / `fish_setlevel 2 151 100` / `fish_next 2 151`；日志出现“强制鱼种生效”且最终鱼获为鱿鱼 |
| 已消费动作 | `Build:ROUND-20260818-01:1E65AB958AC0873A48DC86DCB967A532DAEE4D52CE96C3B5967D094ACCEDF245`；`Deploy:ROUND-20260818-01:1E65AB958AC0873A48DC86DCB967A532DAEE4D52CE96C3B5967D094ACCEDF245` |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单文件+文档小改动，主线程直接完成；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-072-1 | 代码侧结束 | 功能实现 | `[BobberBar] 强制鱼种生效`（每实例 1 条） | `fish_next [玩家序号] <鱼ID>` |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-072-1 | 代码侧结束 | 统一构建/部署授权与真实验收 |

> 机制断言：只影响用户显式使用测试命令后的下一次钓鱼小游戏；不使用命令时行为与现版本完全一致。

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `1E65AB958AC0873A48DC86DCB967A532DAEE4D52CE96C3B5967D094ACCEDF245` |
| 部署前安装 | `05554B169E0D77E649028B9A54931F06439EC2B85C7777CB70905E9F3ED448F7` |
| 部署时间 | 2026-08-18 08:49 |
| 备份 | `DeploymentBackups\FishingExpanded-20260818-084900-pre-BATCH072` |
| 部署文件与目标 | `D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`（源/目标哈希一致） |
| i18n/manifest | 未触碰（i18n 源/目标哈希一致；manifest 因存在未部署的 UniqueID 工作树改动，本次不纳入） |
| config.json | 未触碰 |
<!-- CURRENT-STATE-END -->

## 改动摘要

- `Source/ModEntry.cs`：
  - 新增 `fish_next` 命令注册与 `OnCommandForceNextFish`；
  - 新增按玩家 `UniqueMultiplayerID` 隔离的会话态字典 `_forcedNextFishByPlayer`；
  - 提供 `SetForceNextFish` / `ConsumeForceNextFish` / `ClearForceNextFish`；
  - `ReturnedToTitle` 清空强制鱼种标记；
  - `fish_selftest` 增加强制鱼种标志生命周期自测。
- `Source/Patches/BobberBarPatches.cs`：
  - 新增 BobberBar 构造函数 Prefix：在原生构造前消费强制鱼种，把 `whichFish` 替换为目标鱼原生未限定 ID，并清空原鱼临时 `setFlagOnCatch`。
- 文档：
  - `TESTING-GUIDE.md` 命令表新增 `fish_next`；
  - `WIKI.md` 控制台命令表与英文说明新增 `fish_next`；
  - `TESTING.md` 控制台多玩家目标补充 `fish_next` 支持玩家序号。