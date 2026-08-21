# FishingExpanded 根因批次：BATCH-074 巨型鱼重置 + 联机同步 + 困难模式皇冠

> 活动表只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | `ROUND-20260818-01` |
| 当前活动类别 | 无（CAT-A/B/C 全部代码侧结束） |
| 整合候选状态 | 已统一构建（待统一部署） |
| 当前权威活动卡 | 本卡 |
| 本轮用户问题范围 | ①联机/副屏钓到超大鱼，主机屏幕看不到变大/大尺寸；②进房子后再钓同种 100 级超大/超巨大鱼，NPC 冒泡赞美消失；③换日（翻天）后超大鱼展示与赞美应和进农场一样归零；④困难模式（随机无背板）下 100 级+挑战鱼饵的增大皇冠由 +20% 改为 +30% |
| 本轮纳入 Case | CAT-A（进房/换日重置）；CAT-B（困难模式皇冠 1.3 倍）；CAT-C（联机同步：超大鱼视觉 + NPC 冒泡到其他玩家屏幕，分屏+远程） |
| 共享第一处分歧与所有权链证据 | CAT-A：`GiantFishManager.OnEnterFarmHouse` 只清 `ActiveGiantFish` 未清 `NPCBubbleTriggered/NPCDialogueTriggered`；`OnDayStarted` 只清每日触发未清展示。所有权=GiantFishManager/FishDisplayData。CAT-B：`CollectionsPagePatches.Draw_Postfix` 写死 `1.2f`。所有权=CollectionsPagePatches（展示）+ModConfig（困难模式开关）。CAT-C：`GiantFishManager._displayDataByPlayer` 为进程内运行时字典，远程联机各客户端独立，主机无农场客展示事实；NPC `showTextAboveHead` 只在调用端显示。已取证 `Game1.drawPlayerHeldObject(f)` → `f.ActiveObject.drawWhenHeld(..., f)` 覆盖所有玩家手持绘制。所有权=GiantFishManager（展示事实）+ModEntry（多人消息传输）+ObjectPatches（手持绘制消费） |
| 排队或暂不纳入 Case | 无（用户确认全部实施） |
| 合并/拆分决定及依据 | 三个类别第一处分歧/所有权不同，拆 CAT-A/B/C；CAT-A/B 先做并静态验证，CAT-C 单独取证后实施 |
| 本轮准入证据 | 用户 2026-08-18 消息：4 项现象 + “以上内容先与我讨论实施方案”；随后确认：翻天=换日(DayStarted)、两种联机都要支持、皇冠仅图鉴绘制尺寸、NPC 冒泡需要同步、进房后清空赞美记录；并明确“开始，全部实施，AB做完做充分静态验证后做C，再做充分静态验证后统一部署” |
| 现有实现复核 | CAT-A：S:GiantFishManager.cs:224-242（OnEnterFarmHouse 未清赞美）、S:GiantFishManager.cs:244-263（OnDayStarted 未清展示）、S:Data/FishDisplayData.cs:17-28（ClearActiveGiantFish 只清展示）；CAT-B：S:Patches/CollectionsPagePatches.cs:158（`level100 ? 1.2f : 1f`）；CAT-C：S:Services/GiantFishManager.cs:17-39（进程内字典）、S:Patches/ObjectPatches.cs:120-140（GetDrawScale 按 owner 查）、S:Services/GiantFishManager.cs:123-160（TriggerNPCBubble 本地 showTextAboveHead）、`_analysis` 反编译 `Game1.drawPlayerHeldObject`（对任意 Farmer f 调 `drawWhenHeld`） |
| 允许修改范围 | CAT-A：S:Data/FishDisplayData.cs、S:Services/GiantFishManager.cs、GAME-DESIGN.md、WIKI.md、TESTING.md、TESTING-GUIDE.md、本卡、BUG-LEDGER.md；CAT-B：S:Patches/CollectionsPagePatches.cs、S:ModConfig.cs（如需）、GAME-DESIGN.md、WIKI.md、本卡、BUG-LEDGER.md；CAT-C：S:Services/GiantFishManager.cs、S:Services/GiantFishSyncMessages.cs（新增）、S:ModEntry.cs、S:Patches/ObjectPatches.cs（如需）、GAME-DESIGN.md、WIKI.md、TESTING.md、TESTING-GUIDE.md、本卡、BUG-LEDGER.md |
| 冻结 Case/禁止范围 | 其余机制全部冻结；不触碰难度/结算/数量/品质/经验/鱼王豁免/背板种子逻辑/助战概率/力竭/蓄力槽保护/节日/蟹笼/config 重置 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | CAT-A：进 FarmHouse 与换日都重置展示+赞美；CAT-B：困难模式皇冠 1.2→1.3；CAT-C：联机其他玩家屏幕显示超大鱼视觉与 NPC 冒泡 |
| 可观测性决定 | CAT-A/B 复用现有日志（进房清空日志/冒泡触发日志），不新增逐帧日志；CAT-C 新增低频多人消息接收日志（每类消息单发，限频），临时诊断在闭环后删除或默认关闭 |
| 自动化验收决定 | CAT-A/B 复用 `fish_selftest` 只读断言 + Release 构建 + 反编译核验；CAT-C 需要真实分屏/远程日志或多人消息静态核验，另行记录 |
| 当前类别阶段 | CAT-A/B/C：R0/R1/R2 完成（CAT-C 取证完成：`Game1.drawPlayerHeldObject` 调用链已证实；消息同步实现+静态核验完成） |
| 当前工作树 | 混合未提交基线（BATCH-069~073 等）；本批只改允许范围 |
| 本轮统一构建 | 已执行（0 警告 0 错误；DLL SHA-256 `9F2E9BDB5D7D078B034A79841FA6052024F41904E485E1B417FC35BE1839088B`；反编译核验：ResetSessionEffects/1.3f 皇冠/三条 SendMessage/OnModMessageReceived/ApplyRemote* 均编译进 DLL） |
| 唯一下一步 | 真实验收（重启游戏后按集中测试顺序逐项验收） |
| 已消费动作 | `Build:ROUND-20260818-01:9F2E9BDB5D7D078B034A79841FA6052024F41904E485E1B417FC35BE1839088B`；`Deploy:ROUND-20260818-01:9F2E9BDB5D7D078B034A79841FA6052024F41904E485E1B417FC35BE1839088B`（2026-08-18 18:49，用户授权；备份 `DeploymentBackups\FishingExpanded-20260818-184957-pre-BATCH074`；源/目标 DLL+i18n 哈希一致；**manifest 未同步**——源码 manifest 已含 `UniqueID=neoiw.FishingExpanded`，安装目录仍为 `YourName.FishingExpanded`；config.json 未触碰） |
| 重复执行授权 | 无 |
| **后续待办（用户 2026-08-18 确认）** | 用户确认后续统一使用 `neoiw`（`neoiw.FishingExpanded`）。本次已部署，**本轮不重新部署** manifest；下次部署时把安装目录 `manifest.json` 的 `UniqueID` 从 `YourName.FishingExpanded` 同步为 `neoiw.FishingExpanded`，并核对 Description/UpdateKeys 与源码一致后再执行。 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-A | 进房/换日重置展示+赞美 | 代码侧结束 | 源码缺口修复 | 复用进房/冒泡日志 | fish_giant 后进房再 fish_giant |
| CAT-B | 困难模式皇冠 1.3 倍 | 代码侧结束 | 用户确认的设计变更 | 复用图鉴绘制无日志 | 图鉴目视 |
| CAT-C | 联机超大鱼视觉+NPC冒泡同步 | 代码侧结束 | 取证+消息同步 | 新增多人消息低频日志 | 真实分屏/远程会话 |
<!-- ROUND-CATEGORY-QUEUE-END -->

> 机制断言：CAT-A 为源码缺口修复（展示/赞美生命周期），CAT-B 为用户确认的设计变更；CAT-C 为多人同步新增路径，实施前必须完成调用链取证与竞争解释表。

| 集中测试顺序 | 类别/场景 | 命令或 UI 路线 | 独立判据 | 机制断言 |
|---:|---|---|---|---|
| 1 | CAT-A 进房重置 | fish_giant 模拟→进 FarmHouse→fish_giant 同鱼 | 第二次仍触发冒泡 | 通过/失败/未执行 |
| 2 | CAT-A 换日重置 | fish_giant 模拟→换日→持鱼 | 展示与赞美均归零 | 通过/失败/未执行 |
| 3 | CAT-B 困难模式皇冠 | 开 EnableRandomFishBehavior→100级挑战皇冠目视 | 皇冠 1.3 倍（普通 1.2 倍） | 通过/失败/未执行 |
| 4 | CAT-C 分屏 | 副屏钓超大鱼→主屏目视副屏手持 | 主屏看到放大 | 通过/失败/未执行 |
| 5 | CAT-C 远程 | 农场客钓超大鱼→主机目视 | 主机看到放大+NPC冒泡 | 通过/失败/未执行 |
| 6 | CAT-C 私有提示 | 副屏掉星/助战 | 主机不显示副屏私有 HUD | 通过/失败/未执行 |

<!-- CURRENT-STATE-END -->

## R0：根因/设计

### CAT-A
- `FishDisplayData` 新增 `ResetSessionEffects()`：清 `ActiveGiantFish`、`NPCBubbleTriggered`、`NPCDialogueTriggered`。
- `GiantFishManager.OnEnterFarmHouse` 调用 `ResetSessionEffects()`（保留现有对话快照/日志缓存清理）。
- `GiantFishManager.OnDayStarted` 对每个玩家调用 `ResetSessionEffects()`（展示+赞美归零）。
- GAME-DESIGN §3.2/§3.3 同步：同种鱼在同一超大鱼会话内只触发一次；进 FarmHouse 或换日后重置。

### CAT-B
- `CollectionsPagePatches.Draw_Postfix`：`level100Factor = level100 ? (ModEntry.Config.EnableRandomFishBehavior ? 1.3f : 1.2f) : 1f`。
- GAME-DESIGN §4.7 定义“增大流动金冠鱼（普通 1.2 / 困难模式 1.3）”。

### CAT-C（实施前先取证）
- 竞争解释表：
  1. 远程联机：主机无农场客运行时展示事实 → 需要多人消息同步。
  2. 分屏本地：共享字典应命中，若仍失败则为绘制路径/记录时机缺口 → 需要诊断确认。
- 调用链：`Farmer.caughtFish` Postfix → `RecordGiantFish(player,...)` → `_displayDataByPlayer`；`Object.drawWhenHeld`/`Farmer.draw` → `GetDrawScale(obj, owner)` → `GetFishVisualScale(fishId, owner)`；`CheckAndTriggerNPCReactions` → `TriggerNPCBubble` → `showTextAboveHead`。
- 待取证：其他玩家手持鱼在主机屏幕的实际绘制路径；SMAPI 消息在本地分屏是否送达；NPC 冒泡在分屏是否天然共享。
- 实现：新增 SMAPI 多人消息（`GiantFishRecord`/`GiantFishClear`/`NPCFishBubble`），发送点=记录/清空/冒泡触发，接收点=写入本地展示字典/显示冒泡；不广播私有 HUD。

## R1：删除与收敛
- CAT-A：无旧路径替代，仅扩展清空范围。
- CAT-B：无旧路径替代。
- CAT-C：若分屏本地路径无需消息则分屏走本地共享字典；远程路径新增消息传输，不新增第二个状态所有者（消息只是副本）。

## R2：场景推演
| 场景 | 预期 | 不能发生 |
|---|---|---|
| 单人进房后再钓同种超大鱼 | 再次冒泡 | 旧 fishId 抑制 |
| 换日后持旧超大鱼 | 无放大、无赞美 | 跨日残留 |
| 困难模式 100 级皇冠 | 图鉴 1.3 倍 | 普通模式误变 1.3 |
| 分屏副屏钓超大鱼 | 主屏看到放大+NPC冒泡 | 主屏无事实/私有提示广播 |
| 远程农场客钓超大鱼 | 主机看到放大+NPC冒泡 | 主机无事实 |
| 远程掉星/助战 | 仅本人屏幕提示 | 广播私有 HUD |
| 性能 | 消息低频、幂等 | 逐帧/逐 NPC 刷屏 |
