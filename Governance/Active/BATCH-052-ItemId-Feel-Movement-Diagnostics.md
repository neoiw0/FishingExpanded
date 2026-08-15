# FishingExpanded 根因批次：BATCH-052 持久战料理 ID 修正 + 手感/高难运动诊断

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-11 |
| 当前活动类别 | 无（三类均代码侧结束，统一构建已部署） |
| 整合候选状态 | 已部署待集中测试 |
| 当前权威活动卡 | 本卡（BATCH-052） |
| 本轮用户问题范围 | 用户 2026-08-12 实测反馈：①坚持 30 秒失败收到的奖励是“生鱼寿司”，应为 +3 钓鱼料理；②绿条在中间朝鱼移动时加速效果感觉没生效；③100 级往下连续失败到 96/97 级，鱼上下移动的幅度和频率明显不够。用户提醒：项目内已有 Wiki 物品数据镜像（`D:\GGGGG\codex working space\StardewWikiMirror`），可核对物品 ID |
| 本轮纳入 Case | FE-052-1 持久战 30 秒奖励物品 ID 错误；FE-052-2 绿条朝鱼加速无感；FE-052-3 96/97 级鱼运动幅度/频率不足 |
| 共享第一处分歧与所有权链证据 | FE-052-1 唯一写入者=`TryGrantPerseveranceReward` 的 `PerseverancePlusThreeFoods` 池；FE-052-2 唯一写入者=`Update_Transpiler` 注入的 `ApplyBarInput`（绿条速度输入）；FE-052-3 唯一写入者=`GetAccelerationBoost`×原生 `bobberAcceleration`（鱼运动速度），跳频=`GetJumpInterval` |
| 排队或暂不纳入 Case | BATCH-042~051 已构建/部署待验收（本卡不重复修改其行为） |
| 合并/拆分决定及依据 | 拆 3 类：第一处分歧与唯一所有者均不同；CAT-01 根因已证实，CAT-02/CAT-03 根因未证实只做诊断，禁止混入行为修改 |
| 本轮准入证据 | ①SMAPI-latest.txt:7797 `持久战奖励 | 耗时: 34s | 奖励: 生鱼寿司`（当前安装 DLL AF073AE7... 反编译池=`(O)228/(O)728/(O)730`）；②Wiki 镜像 `StardewWikiMirror\wiki\ns8\18984_MediaWiki_Tools_list-1.js.wiki`：`id="[228]"`→Maki Roll/生鱼寿司（无钓鱼加成）、`id="[242]"`→Dish O' The Sea/海之菜肴（+3）、`id="[728]"`→Fish Stew/烩鱼汤（+3）、`id="[730]"`→Lobster Bisque/龙虾浓汤（+3）；SVE 现行内容包也按该映射使用 242/730（bundles/dialogue）；③用户 2026-08-12 明确三条现象 |
| 现有实现复核 | `Source\Patches\BobberBarPatches.cs:557` 池=`{ "(O)228", "(O)728", "(O)730" }`，注释与 GAME-DESIGN/WIKI/TESTING 均误标 228=海之菜肴；`ApplyBarInput`/`GetDirectionFactor` 静态正确，Transpiler 匹配点与原生 IL 一致；`GetAccelerationBoost` 等级 <98 增幅=现增幅 10%、≥98=100%，96/97 级（调整后难度≈1900）实际增幅≈×2.8，98 级≈×19，存在 6~7 倍断层（用户 2026-08-09 曾确认档位公式，本次为新的实测反证，需用户重审） |
| 允许修改范围 | `Source\Patches\BobberBarPatches.cs`（ID 池、InstanceData 诊断标志、ApplyBarInput 激活日志、构造日志数值）；文档：GAME-DESIGN.md、WIKI.md、TESTING.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 存档结构；挑战鱼饵/鱼王路径；GMCM；部署目录 config.json；不启动游戏、不 `git add -A`；CAT-02/CAT-03 只允许诊断不允许行为改动 |

| 字段 | 值 |
|---|---|
| 当前类别目标 | CAT-01：`PerseverancePlusThreeFoods` 由 `(O)228` 改为 `(O)242`（海之菜肴），同步 4 份文档；CAT-02：`ApplyBarInput` 每实例首帧输出激活诊断（α/方向系数/目标速度）；CAT-03：构造日志追加实际加速增幅倍数与跳鱼间隔 |
| 可观测性决定 | CAT-01 复用现有“持久战奖励”日志（发放物品名即为判据）；CAT-02 新增“手感系统激活”每实例 1 条（InstanceData 标志防重、随实例清理）；CAT-03 复用“钓鱼小游戏开始”构造日志，追加 `加速度增幅: ×N.NN` 与 `跳鱼间隔: Ns`（每实例 1 条） |
| 自动化验收决定 | CAT-01：`fish_persisttest 30` 沙箱，成功=背包出现 海之菜肴/烩鱼汤/龙虾浓汤 之一且日志物品名正确，失败=出现生鱼寿司/无加成料理；CAT-02：真实钓鱼日志出现“手感系统激活”，否则判定 Transpiler 未注入；CAT-03：`fish_setlevel` 到 96/97/98 各开一局，日志比对 `加速度增幅` 与 98 级断层数值，用户确认目标手感 |
| 当前类别阶段 | CAT-01 代码侧结束（修复+文档）；CAT-02/CAT-03 侦测实施完成（待真实验收） |
| 当前工作树 | 完整修改（BATCH-033~051 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `FEB9BAC5929DCF6C78DCF5D2AFFE8F8F0D9CC4688BA520D883B387324EA2D1F5`；反编译核验池=`(O)242/(O)728/(O)730`、BarInputDiagnosticLogged/手感系统激活/加速度增幅均已编译 |
| 唯一下一步 | 真实集中测试：①`fish_persisttest 30` 出 242/728/730；②真实钓鱼 α=100% 查“手感系统激活”日志；③`fish_setlevel` 96/97/98 各一局查增幅数值并交用户档位决策 |
| 已消费动作 | `Build:ROUND-20260812-11:FEB9BAC5929DCF6C78DCF5D2AFFE8F8F0D9CC4688BA520D883B387324EA2D1F5`；`Deploy:ROUND-20260812-11:FEB9BAC5929DCF6C78DCF5D2AFFE8F8F0D9CC4688BA520D883B387324EA2D1F5` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-052-1 | 代码侧结束 | 根因修复 | 复用“持久战奖励”发放日志 | `fish_persisttest 30` |
| CAT-02 | FE-052-2 | 机制已证实激活（等待用户体感确认） | 诊断候选→设计确认 | “手感系统激活”每实例 1 条 | 真实钓鱼（α>0） |
| CAT-03 | FE-052-3 | 根因已证实、设计已确认（实施移交 BATCH-053） | 根因修复 | 构造日志 `加速度增幅/跳鱼间隔` | `fish_setlevel` 96/97/98 各一局 |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-052-1 持久战 30 秒奖励物品 ID 错误 | 根因已证实 | 实施 + R2 + 统一构建部署 |
| FE-052-2 绿条朝鱼加速无感 | 根因未证实 | 真实验收“手感系统激活”日志 |
| FE-052-3 96/97 级鱼运动幅度/频率不足 | 根因未证实 | 真实验收增幅数值 + 用户档位决策 |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `FEB9BAC5929DCF6C78DCF5D2AFFE8F8F0D9CC4688BA520D883B387324EA2D1F5`（2026-08-12 16:13 部署；备份 `DeploymentBackups\FishingExpanded-20260812-161354-pre-BATCH052`=AF073AE7...；源/目标哈希一致；config.json 未触碰；i18n/manifest 未变化） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-052-1 | 30 秒失败奖励是生鱼寿司而非 +3 料理 | 0 | 无 | RC-052-1 | 根因已证实 | `fish_persisttest 30` 出 242/728/730 之一 |
| FE-052-2 | 绿条中间朝鱼移动加速无感 | 0 | 无 | RC-052-2 | 根因未证实 | 日志出现“手感系统激活” |
| FE-052-3 | 96/97 级鱼上下移动幅度/频率不足 | 0 | 无 | RC-052-3 | 根因未证实 | 构造日志 96/97 vs 98 增幅数值 |

## RC-052-1 根因卡（已证实）

- 根因状态：已证实
- 第一处分歧：奖励池把 `(O)228` 当成海之菜肴；228 实际是生鱼寿司（Maki Roll，无钓鱼加成），海之菜肴真实 ID 是 242
- 决定性证据：Wiki 镜像 `StardewWikiMirror\wiki\ns8\18984_MediaWiki_Tools_list-1.js.wiki` 逐条抽取 `id="[228]"`→`Maki Roll/生鱼寿司`、`id="[242]"`→`Dish O' The Sea/海之菜肴`、`id="[728]"`→`Fish Stew/烩鱼汤`、`id="[730]"`→`Lobster Bisque/龙虾浓汤`；当前安装日志 34s 奖励显示“生鱼寿司”，与 228 运行时行为一致；SVE 内容包按同一映射使用 242/730
- 竞争解释表：假设 A=代码 ID 填错（Wiki/运行时一致）仍成立；假设 B=其他 Mod 运行时覆盖 228/242/728/730 映射（已排除：Wiki 为原版映射、SVE/CP 搜索未发现对这三个 ID 的 Data/Objects 覆盖、当前安装日志 228 产出生鱼寿司与原版一致）
- 唯一所有者：`BobberBarPatches.PerseverancePlusThreeFoods` + `TryGrantPerseveranceReward`
- 最短区分动作：改 `(O)228`→`(O)242` 后 `fish_persisttest 30` 验证

## RC-052-2 根因卡（未证实，诊断候选）

- 根因状态：未证实
- 第一处分歧候选：①Transpiler 注入点未命中（α 手感整段未生效）；②注入已生效但方向系数感知不足/数值太小；③`GetDirectionFactor` 计算与用户预期不符
- 决定性证据（当前）：Harmony `MethodBodyReader` 反编译确认运行时 `CodeInstruction.operand` 在生成路径为 `LocalBuilder`、读取路径为 `LocalVariableInfo`，`IsLocalIndex` 两种类型都需覆盖（现源码只覆盖 `LocalBuilder`）；原生 IL `ldarg.0; dup; ldfld bobberBarSpeed; ldloc.s 4; add; stfld bobberBarSpeed` 与 Transpiler 匹配点一致
- 竞争解释表：

| 假设 | 预测可观察量 | 排除证据/判据 | 状态 |
|---|---|---|---|
| 注入未命中 | 日志永不出现“手感系统激活” | 新增每实例 1 条日志 | 待验证 |
| 注入命中但方向系数无感 | 日志出现激活且 α=100%、方向系数 1.2/0.8，但玩家仍无感 | 激活日志 + 用户复测 | 待验证 |
| 方向系数计算错误 | 日志方向系数恒 1.0 或符号反 | 激活日志数值 | 待验证 |

- 唯一所有者：`Update_Transpiler`/`ApplyBarInput`（绿条速度唯一输入点）
- 最短区分动作：部署后真实钓鱼一局，查“手感系统激活”日志

## RC-052-3 根因卡（未证实，诊断候选）

- 根因状态：未证实
- 第一处分歧候选：①等级 <98 加速度增幅仅 10%（96/97 级实际≈×2.8），与 ≥98 级 100%（≈×19）形成 6~7 倍断层，玩家从 100 级掉到 96/97 感觉鱼变“懒”；②跳鱼机制/力竭难度与用户预期不符；③BATCH-051 逃逸减速被误感知为鱼运动变慢（该机制只改 `distanceFromCatchPenaltyModifier`，不直接改鱼位运动）
- 决定性证据（当前）：`GetAccelerationBoost(level<98)=1+0.1×(d-100)/100`，96/97 级 d≈1900 时≈×2.8；`level≥98`≈×19；跳鱼间隔 96/97 仍为 3 秒档；BATCH-051 只写蓄力槽惩罚倍率，不改 `bobberPosition/bobberSpeed`
- 唯一所有者：`GetAccelerationBoost`（加速度）与 `GetJumpInterval`（跳频）
- 最短区分动作：构造日志输出 96/97/98 实际增幅倍数，由用户确认目标手感后决定是否重审档位公式
- 设计确认（2026-08-12 用户定稿）：档位锚点曲线 0→10%、50→20%、70→40%、80→70%、90→100%、100→100%，点间线性；实施见 `Governance\Active\BATCH-053-Acceleration-Anchors.md`

## R0（CAT-01 已证实修复）

- `PerseverancePlusThreeFoods`：`(O)228` → `(O)242`
- 注释与 GAME-DESIGN.md / WIKI.md / TESTING.md / BUG-LEDGER.md 同步修正
- CAT-02 诊断：`InstanceData.BarInputDiagnosticLogged` + `ApplyBarInput` 每实例首帧日志（只读、限每实例 1 条）
- CAT-03 诊断：构造日志追加 `加速度增幅: ×N.NN` 与 `跳鱼间隔: Ns`

## R1：旧路径退休

| 被替代项 | 删除/截断证据 | 是否仍有调用者 | 保留理由/退出条件 |
|---|---|---|---|
| `(O)228` 错误条目 | 本次替换 | 否（池内唯一引用） | 文档同步替换 |
| 无（CAT-02/CAT-03 仅新增诊断，不替代路径） | 不适用 | 不适用 | 验收通过后按观测决定删除/转长期低频 |

- 修改前写入者数量：1（奖励池）；修改后：1
- 运行时代码净增长：约 15 行（诊断标志+两处日志），理由=根因未证实的类别按契约只允许有界侦测

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 | 静态/运行结果 |
|---|---|---|---|
| `fish_persisttest 30` 失败 | 获得 242/728/730 之一（+3 钓鱼） | 获得 228 生鱼寿司 | 待部署后验证 |
| 60 秒池 | 海泡布丁不变 | 被 30 秒池影响 | 代码未触碰 265 |
| 鱼王/挑战鱼饵 | 不变 | 受池修改影响 | 池只影响 30~60 秒失败分支 |
| 真实钓鱼 α=100% | 日志出现“手感系统激活”且方向系数 1.2/0.8 | 日志缺失或每帧刷屏 | 待部署后验证 |
| 96/97/98 各一局 | 构造日志增幅 ≈×2.8/×2.8/×19 | 日志缺失 | 待部署后验证 |
| 多人与分屏 | 诊断按实例 Owner 输出，不写存档 | 串玩家/刷屏 | 日志绑定 InstanceData |
| 性能最坏情况 | 每实例 ≤1 条新日志 | 逐帧输出 | 标志防重 |

## 构建、部署与集中测试

- 构建结果：Release Rebuild ✅（0 警告 0 错误）
- 版本/哈希：DLL SHA256 `FEB9BAC5929DCF6C78DCF5D2AFFE8F8F0D9CC4688BA520D883B387324EA2D1F5`（manifest 仍 0.5.10）
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（默认部署授权）
- 本次单局路线：`fish_persisttest 30` → 30 秒池物品名；真实钓鱼 α=100% → 手感激活日志；`fish_setlevel` 96/97/98 → 增幅数值
- 日志/截图/存档证据：待用户集中测试收集
- 每个 Case 的实际结果：待执行（判据见上表）

## 运行时证据（2026-08-12 16:35 会话）

- `RuntimeEvidence\20260812-BATCH052-053-LOGCHECK-01\SMAPI-latest.txt`（SHA256 `C19A62BD4AA92C6F557A4914A349E88BECA9C4E08E0321143B4DB6CDB4F0D9B0`）
- 16:35:16 `[BobberBar] 手感系统激活 | α: 100 % | 方向系数: 1.20 | 目标速度: 25.2px/帧 | 原生分量: 0.2` → **FE-052-2 注入未命中假设已排除**：Transpiler 运行时确认命中，方向系数与目标速度正确；剩余竞争解释=体感/数值感知（α=1 本身无加速度过程，方向系数 1.2 只是定速倍率）
- 16:35:16 `钓鱼小游戏开始 | 难度等级: 100 | 调整后: 3000.0 | 加速增幅档: 100 % | 加速度增幅: ×30.00` → FE-053-1 100 级曲线生效
- 16:45:41 `持久战奖励 | 耗时: 624s | 奖励: 海泡布丁` → ≥60 秒池正常；30 秒池（242/728/730）本会话未测
- 本会话未执行：`fish_selftest`、96/97/98 三档对比、`fish_persisttest 30`

## 收尾与归档

- 已完成：根因 1 证实（Wiki 镜像）；根因 2/3 竞争解释表与诊断设计
- 当前不确定性：CAT-02 注入是否运行时命中；CAT-03 是否需重审加速度档位公式
- 下一条准确操作：实施 CAT-01 修复 + CAT-02/CAT-03 诊断 → 统一构建部署
- 总账与测试路线是否已覆盖更新：本卡 + BUG-LEDGER 同步中
- 关闭或被替代后是否可移入 `Governance/Archive-ReadOnly`：否（待真实验收）
