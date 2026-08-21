# FishingExpanded 根因批次：BATCH-069 VanillaTips API 顶层接口修复

> 活动表只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260816-03` |
| 当前活动类别 | 无（BATCH-070 为本轮活动类别；本卡仅保存 BATCH-069 验收门禁） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | `Governance/Active/BATCH-070-Prompt-Wrap-ThirdScreen.md`（本卡为 BATCH-069 验收门禁） |
| 本轮用户问题范围 | FishingExpanded 0.5.10 已安装 VanillaTips，但 SMAPI 日志报 `Tried to map a mod-provided API to non-public interface 'FishingExpanded.Services.VanillaTipsIntegration+IVanillaTipsApi'`，随后 FishingExpanded 误判“VanillaTips 未安装”并跳过 7 条睡眠提示注入；VanillaTips 实际已加载且 GCE 通过同一 API 注入成功 |
| 本轮纳入 Case | FE-069-1 VanillaTips 提示注入失败（嵌套接口被 SMAPI 判为非 public） |
| 共享第一处分歧与所有权链证据 | `VanillaTipsIntegration.cs` 把 `IVanillaTipsApi` 声明为 `VanillaTipsIntegration` 的嵌套接口；SMAPI `ModRegistry.GetApi<T>` 要求 `Type.IsPublic`（顶级 public），嵌套接口只满足 `IsNestedPublic`，因此拒绝映射；调用链：`ModEntry.OnGameLaunched` → `VanillaTipsIntegration.TryRegister` → `helper.ModRegistry.GetApi<IVanillaTipsApi>("neoiw.vanillatips")` |
| 排队或暂不纳入 Case | 其余批次待真实验收；本批不触碰其他机制 |
| 合并/拆分决定及依据 | 单 Case 单第一处分歧（接口声明位置），不拆分 |
| 本轮准入证据 | 用户提供 SMAPI 日志（14:45:24 ERROR + DEBUG）与反编译结论；当前源码 `Source\Services\VanillaTipsIntegration.cs` 仍为嵌套接口，与日志一致 |
| 现有实现复核 | 源码确认 `public interface IVanillaTipsApi` 位于 `public static class VanillaTipsIntegration` 内部（修复前）；`TryRegister` 内 `GetApi<IVanillaTipsApi>` 使用同一类型；GCE 使用 VanillaTips 同一 API 成功说明 VanillaTips API 本身可用 |
| 允许修改范围 | `Source\Services\VanillaTipsIntegration.cs`、`BUG-LEDGER.md`、本卡；如需要可在 BATCH-060 卡补反证注记 |
| 冻结 Case/禁止范围 | 其余全部机制冻结；不触碰 VanillaTips 模组文件、不修改提示文案/权重/ID、不新增存档、config.json、不启动游戏 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 将 `IVanillaTipsApi` 移到命名空间顶层，使 SMAPI `GetApi<T>` 能成功映射；`TryRegister` 调用不变 |
| 可观测性决定 | 复用现有日志：注入成功 `已向 VanillaTips 注入 7 条提示`（Info）、未安装 `VanillaTips 未安装`（Debug）、异常 `VanillaTips 注入失败`（Warn）；不新增诊断。成功判据=日志出现注入成功；失败判据=再次出现 `non-public interface` 错误 |
| 自动化验收决定 | 场景=SMAPI 启动时 `GameLaunched` 注入；存档影响=无；触发入口=生产启动（`VanillaTips` 已安装）；成功判据=SMAPI 日志无 `non-public interface` 错误且出现 `已向 VanillaTips 注入 7 条提示`；失败判据=错误复现或日志仍显示“未安装”；关闭条件=验收完成后关闭。不新增测试命令（外部集成启动路径，现有日志足够） |
| 当前类别阶段 | R0/R1/R2 完成；代码侧结束；已部署待集中测试（随 BATCH-070 统一部署） |
| 当前工作树 | 部分修改（源码 + 治理文件；未提交） |
| 本轮统一构建 | `88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`（Release Rebuild，0 警告 0 错误；包含 BATCH-069 顶层接口修复 + BATCH-070 提示换行及 HUD 气泡高度修复） |
| 唯一下一步 | 用户启动游戏一并验收 BATCH-069 + BATCH-070（SMAPI 日志出现“已向 VanillaTips 注入 7 条提示”且无 `non-public interface` 错误；同时目视提示换行） |
| 已消费动作 | `Build:ROUND-20260816-02:E4C964EA79FE902DBF4115BEE3545DB1D1A55486472978D95C13E71A3E8551C4`；`Deploy:ROUND-20260816-02:E4C964EA79FE902DBF4115BEE3545DB1D1A55486472978D95C13E71A3E8551C4`（2026-08-16 14:58 部署；备份 `DeploymentBackups\FishingExpanded-20260816-145849-pre-BATCH069`=F5DA3733...；源/目标 DLL 哈希一致；deps.json/manifest 哈希一致未变更；config.json 未触碰）；`Build:ROUND-20260816-03:B2E496247C671434CD2FFB578B25A0FAE0B53A970A6AA1C525D1CA3A001A7422`；`Deploy:ROUND-20260816-03:B2E496247C671434CD2FFB578B25A0FAE0B53A970A6AA1C525D1CA3A001A7422`（2026-08-16 20:39 部署，含 BATCH-069+BATCH-070；备份 `DeploymentBackups\FishingExpanded-20260816-203916-pre-BATCH070`=E4C964EA...；源/目标 DLL 哈希一致；config.json 未触碰）；`Build:ROUND-20260816-03:88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`；`Deploy:ROUND-20260816-03:88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`（2026-08-16 21:14 修复部署，含 HUD 气泡高度修复；备份 `DeploymentBackups\FishingExpanded-20260816-211423-pre-BATCH070-fix`=B2E496...；源/目标 DLL 哈希一致；config.json 未触碰） |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单文件静态契约修复，主线程已掌握调用链；`NotBeneficial`） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-069-1 | 代码侧结束（已部署） | 根因修复 | 复用注入成功/未安装日志 | SMAPI 启动日志 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-069-1 | 已部署 | 真实验收通过 |

> 机制断言：根因已由 SMAPI 错误日志 + 源码声明位置证实；竞争解释表见 RC-01。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01 / FE-069-1 | 安装 VanillaTips 后启动游戏 | 通过 / 失败 / 未执行 | SMAPI 日志无 `non-public interface` 错误；出现 `已向 VanillaTips 注入 7 条提示`；VanillaTips 内可见 7 条【渔】提示 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`（已部署，2026-08-16 21:14，含 BATCH-069+BATCH-070 及 HUD 气泡高度修复；源/目标一致） |
| 部署前安装 | `B2E496247C671434CD2FFB578B25A0FAE0B53A970A6AA1C525D1CA3A001A7422`（BATCH-070 首版；备份 `DeploymentBackups\FishingExpanded-20260816-211423-pre-BATCH070-fix`） |
| 部署文件与目标 | `D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`（源/目标哈希一致）；deps.json/manifest 未变更（哈希一致）；config.json 未触碰 |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-069-1 | 已安装 VanillaTips 但 FishingExpanded 的 7 条提示未注入（日志误报未安装） | 1 | 2026-08-16 用户日志：`non-public interface '...VanillaTipsIntegration+IVanillaTipsApi'`（BATCH-060 第 9 项部署版被反证） | RC-01 | 已部署待真实验收 | SMAPI 启动日志出现注入成功 |

## RC-01 根因卡

- 根因状态：已证实
- 第一处分歧：`IVanillaTipsApi` 被声明为 `VanillaTipsIntegration` 的嵌套接口；SMAPI `GetApi<T>` 只接受顶级 public 接口，因此映射失败。
- 决定性证据：
  - SMAPI 日志：`Tried to map a mod-provided API to non-public interface 'FishingExpanded.Services.VanillaTipsIntegration+IVanillaTipsApi'; must be a public interface.`
  - 源码：修复前 `public interface IVanillaTipsApi` 位于 `public static class VanillaTipsIntegration` 内部。
  - 对照：GCE 通过 VanillaTips 同一 API 成功注入 46 条提示，说明 API 本身可用、问题在 FishingExpanded 的接口声明位置。
- 竞争解释表：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态（仍成立/已排除） |
|---|---|---|---|
| VanillaTips 未安装 | SMAPI 日志显示 VanillaTips 模组未加载；所有 GetApi 都返回 null | 用户明确 VanillaTips 已正常加载；GCE 通过同一 API 注入成功 | 已排除 |
| 接口方法签名与 VanillaTips 实际 API 不匹配 | GetApi 成功但调用时抛 MissingMethod/签名异常 | 日志在 GetApi 阶段即报 `non-public interface`，尚未进入调用；方法签名与 GCE 用法一致 | 已排除 |
| 嵌套接口导致 SMAPI 判定为非 public | GetApi 报 `non-public interface`，类型名为 `...VanillaTipsIntegration+IVanillaTipsApi` | 日志类型名含 `+` 表示嵌套类型；SMAPI 要求 `Type.IsPublic` | 仍成立（根因） |

- 最后已知正常版本/行为：BATCH-060 第 9 项设计目标（注入 7 条提示）从未在已部署版生效；无“正常”版本。
- 过去失败方案及为何失败：无此前修复；本次为部署版首次反证。
- 唯一所有者：`Source\Services\VanillaTipsIntegration.cs`（接口声明 + TryRegister）。
- 最短区分动作：把接口移到命名空间顶层后重新构建，启动游戏查看 SMAPI 日志。
- 诊断标签/触发窗口：无需新增诊断；现有 `VanillaTips 未安装`/`已向 VanillaTips 注入` 日志即判据。
- 诊断是否只读且不改变行为：不适用（无新增诊断）。
- 诊断构建/部署与哈希：不适用。
- 等待玩家返回的日志及位置：不适用（根因已证实，待构建部署后启动日志）。
- 日志返回后的成功/失败判据：成功=出现 `已向 VanillaTips 注入 7 条提示` 且无 `non-public interface`；失败=错误复现。
- 反证记录：BATCH-060 第 9 项部署版被本日志反证（累计 1 次）；原根因=未考虑 SMAPI 对顶级 public 接口的要求。

## RC-01 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| VanillaTips 提示注册 | 仅启动时 GameLaunched，与玩家无关 | VanillaTipsIntegration.TryRegister | 进程级一次性注册 | 无 | 无（启动注册后不随换日清理） | SMAPI 启动日志 |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| VanillaTips 来源注册 | VanillaTipsIntegration.TryRegister | VanillaTips（外部 Mod） | VanillaTips 提示 UI | 7 条一次注册 | 游戏进程结束 | 无（外部 Mod 管理） |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：用户环境 FishingExpanded 0.5.10；本仓库当前安装哈希以 BATCH-068 部署事实为准（`F5DA3733...`，2026-08-16 11:52）。
- Expanded 接管入口：`ModEntry.OnGameLaunched` → `VanillaTipsIntegration.TryRegister`。
- 原生上游入口和状态字段：SMAPI `IModRegistry.GetApi<T>`；VanillaTips `IVanillaTipsApi`（`RegisterTips`/`RemoveTips`）。
- 原生提交方法：VanillaTips 的 `RegisterTips`。
- 后续回调、同步和生命周期：注册后由 VanillaTips 管理；无 Expanded 回调。
- Harmony 拦截点：无。
- 第一处分歧：接口声明为嵌套类型导致 SMAPI 映射失败。
- 尚未读取或仍不确定的环节：无（静态契约可完整证明）。
- 旧版只读参考及可接受差异：BATCH-060 活动卡第 9 项为原始实现说明。

## R0

- 修改第一处错误决策：将 `IVanillaTipsApi` 从 `VanillaTipsIntegration` 嵌套类中移到 `FishingExpanded.Services` 命名空间顶层。
- 新权威入口：`VanillaTipsIntegration.TryRegister` 仍调用 `helper.ModRegistry.GetApi<IVanillaTipsApi>("neoiw.vanillatips")`，接口类型变为顶级 public。
- R1 待删除旧路径：无旧接口路径；删除嵌套声明，保留同一方法签名。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| 嵌套 `VanillaTipsIntegration.IVanillaTipsApi` | 已改为顶层 `FishingExpanded.Services.IVanillaTipsApi` | 无（全仓仅 TryRegister 使用） | 无 |

- 修改前写入者数量：1（TryRegister）
- 修改后写入者数量：1（TryRegister）
- 运行时代码新增/删除：接口声明位置移动，无逻辑增删；净增长 0 行运行逻辑。
- 净增长理由：无净增长（仅移动声明 + 注释）。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 已安装 VanillaTips | GetApi 成功，注册 7 条提示 | `non-public interface` 错误 | 待构建后启动验证 |
| 未安装 VanillaTips | GetApi 返回 null，静默跳过 | 误报已注入 | 现有 null 分支不变 |
| GCE 共存 | 两模组各自通过 VanillaTips API 注册 | 相互覆盖/冲突 | 不同 sourceId，VanillaTips 管理 |
| 分屏/联机 | 启动时一次性注册，与玩家无关 | 每玩家重复注册 | 无玩家参数 |
| 换日/重载/标题 | 注册不重复触发 | 每次换日重新注册 | GameLaunched 仅启动一次 |
| 性能最坏情况 | 启动一次调用 | 每 Tick/每帧调用 | 仅 GameLaunched |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误；DLL SHA256 `E4C964EA79FE902DBF4115BEE3545DB1D1A55486472978D95C13E71A3E8551C4`）
- 版本/哈希：`E4C964EA79FE902DBF4115BEE3545DB1D1A55486472978D95C13E71A3E8551C4`
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`（2026-08-16 14:58 部署，备份 `DeploymentBackups\FishingExpanded-20260816-145849-pre-BATCH069`）
- 本次单局路线：启动游戏（VanillaTips 已安装），查看 SMAPI 日志
- 日志/截图/存档证据：待收集
- 每个 Case 的实际结果：未执行

## 收尾与归档

- 已完成：源码修改（接口移到顶层）；Release Rebuild；部署；治理同步
- 当前不确定性：真实验收待用户（启动游戏查看 SMAPI 日志）
- 下一条准确操作：玩家启动游戏后返回 SMAPI 日志，确认注入成功
- 总账与测试路线是否已覆盖更新：总账已补 BATCH-069 条目；TESTING.md 无长期回归维度变化
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：真实验收通过并用户确认后归档
