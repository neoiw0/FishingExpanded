# FishingExpanded 根因批次：BATCH-042 GMCM 清除按钮（回到刚安装状态）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-01 |
| 当前活动类别 | CAT-01（GMCM 存档重置） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-01:AD2C6ED4... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-042） |
| 本轮用户问题范围 | 玩家可能想从 0 开始挑战；要求 GMCM 提供“清除”功能，作用=回到刚安装本模组的状态；必须防误点设计，并提示清除会产生什么效果；用户 2026-08-12 补充：参考 GCE 的 GMCM 方案 |
| 本轮纳入 Case | FE-042-1 GMCM 无存档重置入口；FE-042-2 现有 `fish_clear confirm` 未清 `ChallengeCrowns`、旧存档级键 `FishDifficultyData` 只读不删 |
| 共享第一处分歧与所有权链证据 | 存档数据唯一写入者=`DifficultyManager`（`player.modData["FishingExpanded/FishDifficultyData"]` + 旧 SMAPI 存档键 `FishDifficultyData`）；第一处分歧=没有任何“重置为刚安装状态”的生产入口，且 `ClearAllData` 漏清挑战皇冠、旧键只读不删；安装 GMCM 1.16.0 反编译契约证实接口**没有** `AddButton`，真实按钮需用 `AddComplexOption` 自绘 + 本 Mod 自己判定点击 |
| 排队或暂不纳入 Case | BATCH-041（已部署 983B094A…）待真实画面验收，冻结排队；BATCH-039/040 同待验收 |
| 合并/拆分决定及依据 | 合并为一个类别：全部修改处于同一存档写入者与同一用户要求（GMCM 清除按钮）；`fish_clear` 同步为同一语义，避免两个半套清理路径 |
| 本轮准入证据 | 用户 2026-08-12 明确要求（GMCM 清除、回到刚安装状态、防误点、效果提示）并指定参考 GCE GMCM 方案；GCE 源码（`GrowingChildrenPerformanceFix/ModEntry.cs` 463-552、8570-8655）验证其模式=章节标题+警告段落+布尔开关待命+延迟序列（`createQuestionDialogue`/`NonClosableDialogueBox` 二次确认），且只用 GMCM 1.16.0 标准控件；安装 GMCM 1.16.0 反编译确认接口成员；SMAPI 官方文档确认 `WriteSaveData(key, null)` 删除存档条目；现有 `ClearAllData` 源码确认漏清 `ChallengeCrowns` |
| 现有实现复核 | `fish_clear confirm` → `DifficultyManager.ClearAllData`（只清 `FishStatistics` + `CollectionStars`，不写删除旧键）；`LegacySaveDataKey` 只有 `ReadSaveData` 无删除；GMCM 当前只注册日志开关；`OnSaving → SaveAll` 每次保存写回缓存数据 |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`、`Source\Services\IGenericModConfigMenuApi.cs`、`Source\ModEntry.cs`、`Source\i18n\default.json`、`Source\i18n\zh.json`；文档：GAME-DESIGN.md、TESTING.md、TESTING-GUIDE.md、WIKI.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 难度/星标/数量/品质/尺寸/经验/鱼王豁免等全部结算所有权；HUD/图鉴/巨型鱼展示；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | GMCM“重置挑战数据”章节（GCE 模式）：章节标题+三条警告段落+布尔开关“准备重置挑战数据（两步确认）”待命；关闭菜单后由原生问题对话框二次确认（确认重置/取消；BATCH-044 移除 5 秒超时并校验应答者=发起玩家）；确认后清当前玩家 `FishStatistics`/`CollectionStars`/`ChallengeCrowns`，删除旧存档级键 `FishDifficultyData`（仅主玩家），立即写回空数据；`fish_clear confirm` 同步为同一语义；不重置 config、不动背包/等级/原版图鉴；BATCH-043/046 起原版传奇皇冠在重置后立即按图鉴记录恢复（重置提示已同步） |
| 可观测性决定 | 无新增诊断标签；重置动作各 1 条日志（进入确认 1 条 Warn、执行完成 1 条 Info、旧键删除成功/失败各 1 条）；GMCM 按钮两态标签 + HUD 警告/成功提示（均为一次性，非逐帧） |
| 自动化验收决定 | 场景=全量 `Saves` 沙箱；触发入口=`fish_clear confirm`（确定性生产路径，与 GMCM 开关同一所有者）＋GMCM 开关→关闭菜单→问题对话框手工路线；成功判据=三集合为空、modData 键为等价刚安装的空数据、旧存档级键已删除（主玩家）、config/背包/钓鱼等级/原版图鉴不变、其他玩家数据不变；失败判据=仍有残留、误清其他玩家数据、存档损坏、仅开开关即清空（防误点失效）、确认框取消/超时后仍执行；关闭条件=验收完成 |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~041 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F`；反编译核验 AddBoolOption(fieldId=reset-challenge-data)、createQuestionDialogue(确认/取消两响应)、HUD 完成提示、ChallengeCrowns.Clear、WriteSaveData("FishDifficultyData", null) 已编译；前候选 `15A31224…`（自绘按钮版）未部署，同轮被 GCE 模式取代 |
| 唯一下一步 | 真实集中测试（全量 Saves 沙箱 + GMCM 开关→确认框→清空核验 + `fish_clear confirm` 等价核验） |
| 已消费动作 | `Build:ROUND-20260812-01:AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F`（前候选 15A31224 未部署作废）；`Deploy:ROUND-20260812-01:AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-042-1/2 | 代码侧结束 | 根因修复 | 重置动作单发日志（进入确认/完成/旧键删除） | `fish_clear confirm`；GMCM 清除按钮（两步确认） |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-042-1 GMCM 无存档重置入口 | R2 完成 | 沙箱验收（GMCM 两步确认） |
| FE-042-2 fish_clear 漏清挑战皇冠/旧键 | R2 完成 | 沙箱验收（三集合空 + 旧键删除） |

> 机制断言：存档写入行为修改（清空数据），必须走全量 `Saves` 沙箱验收；不部署不启动游戏前，只做静态+构建证据。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01/FE-042-1 | GMCM 开启开关→关闭菜单→问题对话框选“确认重置” | 仅开启开关不清空；确认框选“取消”/5 秒未完成不清空；确认后才清空 | 存档修改 |
| 2 | CAT-01/FE-042-2 | `fish_clear confirm` | 三集合全空、旧存档级键删除（主玩家）、config/背包/等级不变 | 存档修改 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F`（2026-08-12 11:26 部署；备份 `DeploymentBackups\FishingExpanded-20260812-112626-pre-BATCH042`=983B094A...；源/目标哈希一致；config.json 未触碰） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-042-1 | 玩家无法在 GMCM 里把挑战进度重置回刚安装状态 | 0 | 无 | RC-042 | 代码侧结束 | GMCM 两步确认清空 |
| FE-042-2 | `fish_clear confirm` 后挑战皇冠仍显示、旧存档级键残留 | 0 | 无 | RC-042 | 代码侧结束 | `fish_info`/`fish_bonus` 全空 |

## RC-042 根因卡

- 根因状态：已证实（源码 + 安装契约）
- 第一处分歧：没有任何“重置为刚安装状态”的入口；现有清理路径漏清 `ChallengeCrowns` 且从不删除旧存档级键
- 决定性证据：`DifficultyManager.ClearAllData` 只清 `FishStatistics`/`CollectionStars`；`LegacySaveDataKey` 全仓库只有 `ReadSaveData` 无 `WriteSaveData(null)`；GMCM 1.16.0 反编译接口无 `AddButton`
- 竞争解释表：竞争解释豁免（理由=新功能批次，无“哪个根因造成玩家现象”的互斥假设；静态契约已完整证明现有清理路径覆盖范围，且不改变任何既有结算行为）
- 最后已知正常版本/行为：不适用（新功能）
- 唯一所有者：`DifficultyManager`（存档数据）；`ModEntry`（GMCM 入口与防误点状态）
- 最短区分动作：实现 `ClearAllData` 全量重置 + GMCM 两步确认按钮，沙箱验证

## RC-042 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 当前玩家重置 | 单人=唯一玩家；联机=本机玩家；分屏=当前活动屏幕 | `DifficultyManager.ClearAllData(Game1.player)` | 静态缓存按玩家 ID 键控 | modData NetField 同步到主机存档 | 标题/换存档 `UnloadData` 已清缓存 | `fish_clear confirm` + 沙箱 |
| 旧存档级键删除 | 仅主玩家执行 | `ModHelper.Data.WriteSaveData(..., null)` | 无 | 仅主机存档 | 天然幂等（不存在则跳过） | 沙箱核对键消失 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 挑战数据 | 钓鱼结算/命令/GMCM | `DifficultyManager` | HUD/图鉴/手感/助战 | 每玩家一份 JSON | 重置或卸载 | 标题 `UnloadData` |
| 防误点确认态 | GMCM 按钮 | `ModEntry` 静态字段 | 按钮绘制/点击判定 | 单实例 | 10 秒超时/执行成功 | 返回标题无需清理（超时自动失效） |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：`983B094A…`（BATCH-041 部署版）；游戏 1.6.15.24354、SMAPI 4.5.2、GMCM 1.16.0
- Expanded 接管入口：`OnSaving → DifficultyManager.SaveAll`；`fish_clear confirm`；新增 GMCM 清除按钮
- 原生上游入口和状态字段：`Farmer.modData`（NetStringDictionary，随玩家存档/联机同步）
- 原生提交方法：`SaveGame` 序列化 `Farmer.modData`
- 后续回调、同步和生命周期：SMAPI `Saving` 事件兜底写回；返回标题 `UnloadData`
- Harmony 拦截点：无新增
- 第一处分歧：缺少重置入口 + 现有清理覆盖不全
- 尚未读取或仍不确定的环节：GMCM 菜单打开时自绘按钮的屏幕坐标需真实游戏目视确认（静态契约已对齐 `ComplexModOptionWidget.Draw` 传入 `Element.Position`）
- 旧版只读参考及可接受差异：无

## R0

- 修改第一处错误决策：`ClearAllData` 改为全量重置（三集合 + 旧存档级键 + 立即写回）；GMCM 按 GCE 方案增加“重置挑战数据”章节（警告段落 + 开关待命 + 原生问题对话框二次确认）
- 新权威入口：GMCM 开关序列与 `fish_clear confirm` 共用 `DifficultyManager.ClearAllData`（唯一写入者）
- R1 待删除旧路径：无旧路径，只修正 `ClearAllData` 覆盖范围

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `fish_clear` 只清两集合的半套语义 | 本卡 R0 改为全量重置 | 是（测试/玩家命令） | 保留命令，语义升级为与 GMCM 按钮一致 |

- 修改前写入者数量：1（`DifficultyManager`）
- 修改后写入者数量：1（`DifficultyManager`）
- 运行时代码新增/删除：新增 GMCM 按钮绘制/点击判定/防误点状态；`ClearAllData` 扩展
- 净增长理由：新 UI 功能必须有绘制与点击判定；数据重置仍复用唯一所有者，不新增第二份状态

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 开关待命→确认框确认后全部挑战数据为空；旧键删除；config/背包/等级不变 | 仅开开关即清空；误删原版图鉴 | 静态通过（构建反编译核验） |
| 本地主屏/副屏 | 序列只响应当前活动屏幕（`_resetArmedScreenId`）；重置只作用于该屏玩家 | 另一屏玩家被重置 | 静态通过（构建反编译核验） |
| 远程与四人混合 | 农场客重置只清自己；主机重置只清主机；旧键仅主机删 | 清到其他玩家 | 静态通过（构建反编译核验） |
| 普通鱼/非鱼类/鱼王 | 重置后所有鱼难度 0、无星标/挑战皇冠 | 鱼王或特殊对象残留 | 静态通过（构建反编译核验） |
| 数量、动画与 ItemGrabMenu | 不涉及 | 重置影响物品/动画 | 静态待构建 |
| 难度结算/HUD/图鉴/手持展示 | 重置后图鉴皇冠与 α 立即回到 0 起点 | 旧缓存继续显示 | 静态待构建 |
| 换日/重载/标题/断线 | 保存后重载仍为空；标题返回后无残留确认态 | 超时后仍可误确认 | 静态待构建 |
| 性能最坏情况 | 按钮绘制每帧一次矩形+文字；点击判定每帧一次矩形包含测试 | 逐帧日志/无界缓存 | 静态待构建 |
| 已验收相邻回归 | 日志开关、HUD 队列、存档隔离不受影响 | GMCM 注册报错 | 静态待构建 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F`（manifest 版本仍为 0.5.10，本轮不改版本号）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（DLL + i18n；config.json 不触碰）
- 本次单局路线：全量 `Saves` 沙箱 + GMCM 按钮两步确认 + `fish_clear confirm`
- 日志/截图/存档证据：待集中测试封存
- 每个 Case 的实际结果：待执行

## 收尾与归档

- 已完成：契约核对、批次卡、R0 设计
- 当前不确定性：GMCM 自绘按钮点击坐标需真机目视确认；沙箱验收未执行
- 下一条准确操作：R0 代码实现 → Release Rebuild → 静态核验 → 等待用户授权部署/实测
- 总账与测试路线是否已覆盖更新：实施中同步
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：验收完成后可归档
