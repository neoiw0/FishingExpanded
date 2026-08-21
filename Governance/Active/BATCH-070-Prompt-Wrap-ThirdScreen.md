# FishingExpanded 根因批次：BATCH-070 提示按 1/3 屏宽换行

> 活动表只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260817-01` |
| 当前活动类别 | CAT-01（提示按 UI 视口 1/3 宽换行；HUD 气泡高度反证修复） |
| 整合候选状态 | 已部署待集中测试（当前安装 `05554B169E0D...`） |
| 当前权威活动卡 | 本卡（BATCH-070） |
| 本轮用户问题范围 | 用户反证：左下角 HUD 文本≥3 行时仍略超出气泡；询问每行增高是否不足 |
| 本轮纳入 Case | FE-070-1 BobberBar 浮动提示超过 1/3 屏换行；FE-070-2 HUDNotifier/ModEntry HUD 消息超过 1/3 屏换行 |
| 共享第一处分歧与所有权链证据 | 两条提示显示路径独立：浮动提示由 `BobberBarPatches.WrapTipText` 自绘，HUD 消息经 `Game1.addHUDMessage` 原生绘制；共同规则=显示宽度上限按 `Game1.uiViewport.Width / 3` |
| 排队或暂不纳入 Case | BATCH-069 已部署待真实验收，本轮统一部署后由用户一并验收；不触碰 VanillaTips 注入提示 |
| 合并/拆分决定及依据 | 同属“本 Mod 提示显示宽度”单一规则，但所有者/绘制路径不同，分两个 Case 独立验收；代码同一轮实施 |
| 本轮准入证据 | 用户明确指令“各种提示在英文下可能太长，所有都执行超过1/3个屏幕要换行”并要求部署；用户已确认范围=仅本 Mod 自绘提示、口径=UI 视口宽度/3 |
| 现有实现复核 | `BobberBarPatches.cs` 已有 `WrapTipText` 和固定 `TipMaxWidthPixels=420f`；`HUDNotifier.cs`/`ModEntry.cs` 直接 `new HUDMessage`，原生 HUDMessage 不按 1/3 屏换行（已反编译 `StardewValley.HUDMessage.draw` 确认背景高度固定 112、文本单行绘制） |
| 允许修改范围 | `Source/Patches/BobberBarPatches.cs`、`Source/Services/HUDNotifier.cs`、`Source/Services/WrappingHUDMessage.cs`（新增）、`Source/ModEntry.cs`、`GAME-DESIGN.md`、`BUG-LEDGER.md`、本卡 |
| 冻结 Case/禁止范围 | 其余全部机制冻结；不触碰 VanillaTips 注入、不修改 i18n 文案、不新增存档/config.json、不启动游戏 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | 所有本 Mod 自绘提示（BobberBar 浮动提示 + HUDNotifier/ModEntry HUD 消息）在文本宽度超过 `Game1.uiViewport.Width / 3` 时换行；HUD 消息背景随行数增高 |
| 可观测性决定 | 纯显示修改，复用现有绘制/日志；不新增诊断。成功判据=游戏内英文长提示不超过 1/3 屏宽且可读；失败判据=仍超宽或 HUD 背景不匹配 |
| 自动化验收决定 | 场景=真实游戏内英文/中文长提示显示；存档影响=无；触发入口=生产 UI（钓鱼浮动提示、左下角 HUD 通知）；成功判据=视觉换行、无异常；失败判据=文字溢出或绘制异常；关闭条件=验收完成。无确定性现有 SMAPI 命令可覆盖 HUD 绘制，按纯 UI 展示验收 |
| 当前类别阶段 | 代码侧结束（已部署待集中测试） |
| 当前工作树 | 部分修改（含 BATCH-069/BATCH-070/BATCH-071 未提交源码与治理 + 本反证修复） |
| 本轮统一构建 | `05554B169E0D77E649028B9A54931F06439EC2B85C7777CB70905E9F3ED448F7`（Release Rebuild，0 警告 0 错误；改用 `SpriteFont.MeasureString(wrapped).Y` 计算真实文本高度） |
| 唯一下一步 | 用户启动游戏验收左下角 HUD ≥3 行气泡完整包裹文字；BATCH-069/BATCH-070/BATCH-071 一并集中测试 |
| 已消费动作 | 既往动作不变；`Build:ROUND-20260817-01:05554B169E0D77E649028B9A54931F06439EC2B85C7777CB70905E9F3ED448F7`；`Deploy:ROUND-20260817-01:05554B169E0D77E649028B9A54931F06439EC2B85C7777CB70905E9F3ED448F7`（2026-08-17 22:15 部署，备份 `DeploymentBackups\FishingExpanded-20260817-221555-pre-BATCH070-heightfix2`=2367A9AE...；源/目标哈希一致；config.json 未触碰） |
| 重复执行授权 | 无 |
| 本批可委派任务/委派记录 | 无（单主线程静态 UI 修改，已掌握全部调用点；`NotBeneficial`，理由=改动集中且需与既有未提交 BATCH-069 一起核验，不拆分） |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-070-1、FE-070-2 | 已部署待集中测试 | 根因修复（用户规则实现 + 高度反证修复） | 无新增；复用现有日志 | 真实游戏 UI 目视 |
| CAT-02 | BATCH-069 FE-069-1 | 代码侧结束（已部署，待一并验收） | 根因修复 | 复用注入成功/未安装日志 | SMAPI 启动日志 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-070-1 | 已部署 | 真实验收通过/失败/未执行 |
| FE-070-2 | 已部署 | 真实验收通过/失败/未执行 |
| FE-069-1 | 已部署 | 真实验收通过/失败/未执行 |

> 机制断言：纯显示宽度规则，不改变任何玩法/存档/多人状态。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-01 / FE-070-1 | 钓鱼小游戏触发英文长浮动提示（助战/力竭/星之果茶等） | 通过 / 失败 / 未执行 | 提示在 1/3 屏宽内换行，不超屏 |
| 2 | CAT-01 / FE-070-2 | 触发英文长左下角 HUD 通知（成功/失败/星之果茶/持久战等） | 通过 / 失败 / 未执行 | HUD 文字在 1/3 屏宽内换行，背景高度随行数适配 |
| 3 | CAT-02 / FE-069-1 | 安装 VanillaTips 启动游戏 | 通过 / 失败 / 未执行 | SMAPI 日志无 `non-public interface` 且出现注入成功 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `05554B169E0D77E649028B9A54931F06439EC2B85C7777CB70905E9F3ED448F7`（已部署，2026-08-17 22:15；改用 `SpriteFont.MeasureString(wrapped).Y` 计算真实文本高度；备份 `DeploymentBackups\FishingExpanded-20260817-221555-pre-BATCH070-heightfix2`=2367A9AE...；源/目标哈希一致；config.json 未触碰） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---:|---|---|---|---|
| FE-070-1 | 英文长浮动提示超过 1/3 屏宽 | 0 | 无 | RC-01 | 实施中 | 游戏内目视 |
| FE-070-2 | 英文长 HUD 通知超过 1/3 屏宽 | 1 | 有（2026-08-17：≥3行文本仍略超出气泡） | RC-01 | 实施中 | 游戏内目视 |

## RC-01 根因卡

- 根因状态：已证实（用户规则 + 现有代码复核）
- 第一处分歧：显示宽度上限未按 1/3 屏宽统一；浮动提示固定 420px，HUD 消息原生单行不换行。
- 决定性证据：`BobberBarPatches.cs` `TipMaxWidthPixels=420f`；`StardewValley.HUDMessage.draw` 反编译显示 `Game1.smallFont.MeasureString(message).X` 单行背景、`num2=112` 固定高度。
- 2026-08-17 反证补充：气泡高度原先按 `lines.Length * LineSpacing` 估算；MonoGame `SpriteFont.MeasureString` 的 Y 实为“换行间距之和 + 本行字形裁剪最大高度”，中文字形高度大于 LineSpacing 时会低估，导致≥3 行文本略超出气泡。已改用 `Game1.smallFont.MeasureString(wrapped).Y`。
- 竞争解释表：竞争解释豁免（理由=这是用户确认的显示规则修改，不是 Bug 根因排查；不涉及外部 Mod/原生行为竞争，唯一待改点是本 Mod 自绘提示的宽度上限/高度计算）。
- 唯一所有者：BobberBar 浮动提示=`BobberBarPatches.DrawTip/AddTip`；HUD 消息=`HUDNotifier`/`ModEntry.ShowResetHudMessage`。
- 最短区分动作：实现后游戏内用英文长文案目视。

## RC-01 多人/分屏影响矩阵

| 写入路径/行为 | 主机/客户端/副屏 | 权威写入者 | 实例/进程静态 | 消息/广播/同步 | 断线/换日/标题清理 | 自动化覆盖 |
|---|---|---|---|---|---|---|
| 浮动提示绘制 | 各屏本地实例 | BobberBarPatches（显示） | 每实例 | 无 | 小游戏结束自动清理 | 无（纯 UI） |
| HUD 通知绘制 | 各玩家本地队列 | HUDNotifier / 原生 HUDMessage | 按玩家字典 | 无 | ReturnedToTitle 清空 | 无（纯 UI） |

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 换日/标题/分屏/远程清理 |
|---|---|---|---|---|---|---|
| 浮动提示行宽 | BobberBarPatches | BobberBarPatches | 绘制 | 每提示一次 | 提示消失 | 小游戏结束 |
| HUD 消息文本 | HUDNotifier/ModEntry | HUDNotifier/ModEntry | 原生 HUD 绘制 | 队列上限 5 | 消息淡出 | ReturnedToTitle 清空 |

## 原生完整调用链

- 当前安装 DLL 版本/哈希：部署前以 BATCH-069 当前安装 `E4C964EA...` 为准；构建后更新。
- Expanded 接管入口：`BobberBarPatches.Draw_Postfix`（浮动提示）、`Game1.addHUDMessage`（HUD 通知）。
- 原生上游入口和状态字段：`HUDMessage.draw`、`Game1.smallFont`、`Game1.uiViewport`。
- 原生提交方法：无（纯显示）。
- 后续回调、同步和生命周期：无。
- Harmony 拦截点：BobberBarPatches.Draw_Postfix；HUD 消息不新增 Harmony。
- 第一处分歧：见 RC-01。
- 尚未读取或仍不确定的环节：无。
- 旧版只读参考及可接受差异：BATCH-058G 固定 420px 是旧规则，被本次用户规则替换。

## R0

- 修改第一处错误决策：将浮动提示固定 420px 改为 `Game1.uiViewport.Width / 3`；为 HUD 消息新增 `WrappingHUDMessage` 子类，按同一宽度换行并动态增高背景。
- 新权威入口：`BobberBarPatches.GetTipMaxWidth()`（或等价内联）；`WrappingHUDMessage`。
- R1 待删除旧路径：删除 `TipMaxWidthPixels` 常量；HUD 消息不再直接 `new HUDMessage` 用于本 Mod 长文案。

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `TipMaxWidthPixels=420f` | 改为动态 1/3 屏宽 | 无（替换全部 2 处） | 无 |
| HUDNotifier/ModEntry 直接 `new HUDMessage` | 改为 `WrappingHUDMessage` | 本 Mod 长提示调用点全部替换 | 无；原生 HUDMessage 仍用于非本 Mod 消息 |

- 修改前写入者数量：显示规则写入者=BobberBarPatches/HUDNotifier/ModEntry 各 1；修改后=同。
- 修改后写入者数量：同。
- 运行时代码新增/删除：新增 1 个显示子类；删除 1 个常量。
- 净增长理由：HUD 原生消息需要动态换行/增高背景，必须新增显示子类；不新增状态所有者。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| 主机单人 | 长英文提示在 1/3 屏宽内换行 | 文字超屏/背景不匹配 | 待构建后游戏内验证 |
| 本地主屏/副屏 | 各屏按各自 uiViewport 宽度计算 | 串用他屏宽度 | 每屏独立绘制 |
| 远程与四人混合 | HUD 队列按玩家独立，换行只影响显示 | 不新增同步/广播 | 无状态写入 |
| 普通鱼/非鱼类/鱼王 | 提示文案按同一规则换行 | 影响玩法/结算 | 纯显示 |
| 数量、动画与 ItemGrabMenu | 不受影响 | 无 | 无相关改动 |
| 难度结算/HUD/图鉴/手持展示 | 仅 HUD/浮动提示显示变化 | 不影响存档/难度 | 无状态写入 |
| 换日/重载/标题/断线 | 队列/提示清理照旧 | 残留换行状态 | 无新增状态 |
| 性能最坏情况 | 每提示仅一次测量/换行；HUD 每帧绘制按行数线性 | 逐帧创建对象 | 在 draw 内只做一次 parseText 后复用 wrapped 字符串 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误；DLL SHA256 `88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`）
- 版本/哈希：`88DB2F7B32AEBB85D8B8C6DEC4DE6A9DB702453B5E0B8A21D5728ABBA3AAD552`
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll`（2026-08-16 21:14 部署，备份 `DeploymentBackups\FishingExpanded-20260816-211423-pre-BATCH070-fix`）
- 本次单局路线：用户启动游戏一起验收 BATCH-069 + BATCH-070
- 日志/截图/存档证据：待收集
- 每个 Case 的实际结果：未执行

## 收尾与归档

- 已完成：活动卡建立
- 当前不确定性：构建/部署后待用户目视验收
- 下一条准确操作：完成源码修改 → Release Rebuild → 部署 → 同步治理
- 总账与测试路线是否已覆盖更新：总账待同步；TESTING.md 无长期回归维度变化
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：真实验收通过并用户确认后归档
