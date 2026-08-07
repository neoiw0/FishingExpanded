# BATCH-030：失败只记录一次回归（FailureRecorded 丢失）+ 弱称号文案场景修正

**创建**：2026-08-07
**类型**：已部署修复被实测推翻（BATCH-029 回归）+ 用户设计变更（含存档兼容性问询）
**状态**：取证 ✅（根因已证实）→ R0 ✅ → 已部署（2026-08-07 22:49），真实验收待用户执行
**当前唯一根因/任务**：
1. BATCH-029 重写 `BobberBarPatches.Update_Postfix` 失败分支时误删 `data.FailureRecorded = true;`（唯一写入点丢失），淡出动画期间每帧重复记录失败（单次钓鱼 18~51 次），一次失败直接扣到底（-10）。
2. 设计变更：“额...稍微强一点的个体”（rank.weak）不应出现在星标挑战宣言（错误场景）；零以下胜利应显示该称号提示，当前被 BATCH-022 `newLevel <= 0` 跳过（无提示）。

## 本轮准入证据（2026-08-07，其他电脑 D:\K1515 三份日志）

- 日志 A（21:25 会话，SHA256 `0290F378...`，`RuntimeEvidence\20260807-BATCH029-REGRESSION-01\SMAPI-latest-2125-session-D-K1515.txt`）：
  - 太阳鱼 (O)145 实例 24014637：同一秒 51 次失败记录（9→8→…→-10→-10，连续失败 1→51）。
  - 虹鳟鱼 (O)138 实例 7885577：同一秒 51 次失败记录（4→3→…→-10→-10，连续失败 1→51）。
  - 错误场景 4 条（太阳鱼 -10 星标宣言）：“令人敬重的太阳鱼额...稍微强一点的个体步步为营地亮出锋芒。”（21:33:12）、“可敬的太阳鱼额...稍微强一点的个体毫不动摇地应战。”（21:38:05）、“值得敬佩的太阳鱼额...稍微强一点的个体杀气腾腾地接下挑战。”（21:38:35）、“令人敬重的太阳鱼额...稍微强一点的个体信心十足地接招。”（21:39:26）。
  - 零以下胜利无提示 4 次：“等级过低，跳过提示”（虹鳟鱼 -10/-2、太阳鱼 -10/-2）。
- 日志 B（21:44 会话，SHA256 `EE5BF6DB...`，`SMAPI-latest-2144-session-D-K1515.txt`）：狗鱼 (O)144 同秒 18 次失败记录（2→-10）；成功 -10 两次“等级过低，跳过提示”；存档加载正常（0 鱼种新档，无崩溃）。
- 日志 C（20:09 会话，SHA256 `9CAFC178...`，`SMAPI-latest-2009-session-D-K1515-no-FishingExpanded.txt`）：该机器该会话未安装 FishingExpanded（Loaded 49 mods），仅作环境对照。
- 存档兼容性结论：不是存档问题。`FishStats.ConsecutiveFailCount` 为 BATCH-029 新增字段，JSON 反序列化缺失默认 0；两份日志存档加载均正常（21:25 会话 4 鱼种/1 星标，21:44 会话 0 鱼种）；成功记录 0→3、0→2 正常，证明新老存档均兼容。

## 现有实现复核（静态合同）

- 当前源码 `Source/Patches/BobberBarPatches.cs` 失败分支（约 302-316 行）：条件含 `!data.FailureRecorded`，但分支体只设置 `data.ResultStarted = true;`，从未设置 `FailureRecorded`。
- git diff（BATCH-029 相对 BATCH-028 工作树）明确显示删除 `data.FailureRecorded = true; // 防止重复记录`。
- 旧版 DLL（`DeploymentBackups\FishingExpanded-20260807-005605-pre-BATCH029\FishingExpanded.dll`，SHA256 `E0DA4DA2...`）ilspycmd 反编译：失败分支含 `value.FailureRecorded = true;` + `value.ResultStarted = true;`。
- `FailureRecorded`：声明（32 行）、初始化 false（104 行）、判断（302 行）均在，唯一写入点丢失 → 第一处分歧 = BATCH-029 重写删除该行。
- `HUDNotifier.ShowSuccessNotification`：`newLevel <= 0` 提前 return（BATCH-022 逻辑，旧版反编译同样存在，非回归）→ 用户现在要求零以下胜利显示弱称号 = 设计变更。
- `ChallengeDialogueGenerator.Generate`：`rankKey == "rank.weak"` 时把 rank.weak 称号文本嵌入宣言模板 → 错误场景来源。
- `DifficultyCalculator.GetRankKey`：`level < 1 → "rank.weak"`（0 和负数同属弱称号带）。

## 反证计数口径

- 按玩家可见现象“钓鱼失败只扣 1 级”计：BATCH-029 部署版（`26DEE6F2...`）首次被实测推翻 = 反证 #1 → 重开原 Case（本卡），第一处分歧已由 git diff + 旧 DLL 反编译 + 两份运行日志三方证实。
- 按 BATCH-029 批次整体计：Transpiler 启动失败（反证 #1，属部署/初始化缺陷，启动即暴露、无游戏内玩家现象）+ 本次失败循环（反证 #2）。本卡如实记录批次口径，但按现象口径执行（根因唯一、修复为恢复一行被误删代码 + 用户明确设计变更，不机械升级门禁）。

## 允许修改文件/符号（首次编辑前准入锁）

- `Source/Patches/BobberBarPatches.cs`：失败分支恢复 `data.FailureRecorded = true;`。
- `Source/Services/HUDNotifier.cs`：移除 `newLevel <= 0` 跳过（<1 弱称号显示提示）。
- `Source/Utils/ChallengeDialogueGenerator.cs`：weak 模板不含 rank token（新 i18n 键）。
- `Source/i18n/zh.json` + `default.json`：新增 `hud.starChallenge.template.weak`。
- `GAME-DESIGN.md`、`BUG-LEDGER.md`、本卡。
- 冻结：BATCH-024~029 其余未验收项、鱼王豁免、非鱼类 8 级上限、蓄力槽保护、视觉锚点、多人隔离、结算封顶等一律不修改。

## R0 实施

1. `BobberBarPatches.Update_Postfix` 失败分支：`RecordFailure` + 失败提示后恢复 `data.FailureRecorded = true;`（与 `ResultStarted` 并列）。
2. `HUDNotifier.ShowSuccessNotification`：删除 `newLevel <= 0` 提前 return；任何未封顶成功都显示“下一次你将向{鱼名}中的{称号}发起挑战”（<1 → 额...稍微强一点的个体）。
3. `ChallengeDialogueGenerator.Generate`：`rank.weak` 使用 `hud.starChallenge.template.weak`（无 rank token），其他称号不变。
4. i18n 双语新增 weak 模板键。
5. 设计本 2.1/4.5 同步新规则。

## R1：删除与收敛

- 删除路径：`ShowSuccessNotification` 的“等级过低，跳过提示”分支整体删除（无第二个写入者）。
- 无新增持久状态、字典或缓存；`FailureRecorded` 仍是唯一失败防重写入者；宣言仍由 `ChallengeDialogueGenerator` 唯一生成。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 真实失败一次（任意鱼） | 日志只出现 1 条“钓鱼失败记录”，等级 -1 | 同秒多条失败记录、一次扣到底 |
| 失败后淡出动画期间 | 无重复 HUD 失败提示（史诗/普通均只一次） | 每帧刷提示 |
| 成功（等级 -10/-2/0） | 显示“下一次你将向{鱼名}中的额...稍微强一点的个体发起挑战” | “等级过低，跳过提示”、无提示 |
| 星标鱼等级 <1 进入小游戏 | 宣言不含“稍微强一点的个体”，如“可敬的太阳鱼毅然决然应战。” | 宣言嵌入弱称号文本 |
| 星标鱼等级 ≥1 进入小游戏 | 宣言含正常称号（伯爵等），行为不变 | 宣言结构改变 |
| 连续失败史诗提示 | 修复后连续失败计数按真实失败递增（≥2 且难度 ≥150 触发） | 单次失败即计数 +18~51 |
| 旧存档 | 无 `ConsecutiveFailCount` 字段默认 0，加载正常 | 加载崩溃/垃圾值 |
| 鱼王 | 不参与（无 InstanceData/豁免路径不变） | 鱼王失败被记录或宣言出现 |

## 可观测性决定

- 修复后复用现有日志：失败记录每条真实失败 1 条（日志频率由 18~51 条/次降为 1 条/次，本身就是回归证据）；不加新诊断。
- 成功提示日志覆盖负等级（“成功提示显示”包含等级 -10 等）；“等级过低，跳过提示”路径删除后不再出现。
- 宣言日志保留，文案核对不含弱称号。

## 自动化验收决定

- 复用真实原生钓鱼入口 + 现有日志（不新增控制台命令）：失败一次 → 1 条失败记录；`fish_setlevel` 设 -10 后成功 → “成功提示显示”含弱称号；星标鱼 -10 宣言不含弱称号。
- 存档影响：无（不修改存档）；真实验收由用户执行。

## 构建与部署事实

- Release Rebuild（2026-08-07）：0 警告 0 错误。
- ilspycmd 反编译核验：`BobberBarPatches.Update_Postfix` 恢复 `value.FailureRecorded = true;`（反编译 241 行）；`HUDNotifier` 已无“等级过低，跳过提示”分支；`ChallengeDialogueGenerator` 含 `hud.starChallenge.template.weak` 条件。
- 产物 SHA-256：`FishingExpanded.dll` `108C3A2C05160AE9DCBCA285D2D9D13D78EFF9020CAF3792414A64A51B8B2C7B`；`manifest.json` `B906DCF8E2BC07D44F74A630B1B58BC550CD07E99849DAF96776E6853C2A1CEB`（未变）；`i18n\zh.json` `6BC92C0E71C1E9E203153E13B920684198D0091F3D2F9CF83AE04C614066BEC8`；`i18n\default.json` `1678B72CE6F31972C6606992F527A8FFB190E608B9989F98DE79688DE57C5CA1`；三份 JSON 解析通过。
- 部署（2026-08-07 22:49，用户既有部署授权延续）：备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`（含被替换 DLL `26DEE6F2...` 与旧 i18n/manifest）；4 文件源/目标 SHA-256 逐一一致；部署目录无 `config.json`（未涉及）。
- 进程门禁：Stardew Valley.exe 未运行；PID 10556 僵尸进程（0 线程/0 句柄）不锁定 DLL（独占打开验证通过）。

## Closeout 门禁（2026-08-07）

- 治理同步：本卡 + `BUG-LEDGER.md`（活动块/表格行）+ `GAME-DESIGN.md`（2.1 成功提示、4.5 宣言弱称号）+ `TESTING.md`（2 行矩阵）+ `LOCAL-PITFALLS.md`（内联命令踩坑）已同步。
- Git 基线：HEAD `8f799da`；工作树为 BATCH-024~030 混改未提交基线（不 `git add -A`，用户未要求提交）；本轮只改 BATCH-030 范围内文件。
- 聚焦 diff：`BobberBarPatches.cs`（恢复 `FailureRecorded=true`）、`HUDNotifier.cs`（删除 ≤0 跳过）、`ChallengeDialogueGenerator.cs`（weak 模板）、`i18n`×2（`hud.starChallenge.template.weak`）、`GAME-DESIGN.md`、`BUG-LEDGER.md`、`TESTING.md`、本卡。
- 委派评估：不委派（Luna 当前模型禁止委派）。
- 构建结果：Release Rebuild 0 警告 0 错误；ilspycmd 反编译核验关键符号齐全。
- 部署哈希：源/目标 4 文件一致；备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`。
- 进程门禁：游戏未运行；僵尸 PID 10556 不锁定文件。
- 检查点：本卡与总账形成逻辑检查点；不代表真实游戏验收。

## 委派评估

- 不委派（Luna 当前模型禁止委派；根因裁决与部署属主线程职责）。