# BATCH-029：尺寸数字按等级线性化 + 连续失败史诗提示 + 成功额外等级 + 加速度 30% 档 + 收藏页皇冠

**创建**：2026-08-06
**类型**：设计需求落地（用户确认 5 项修改并授权“完成所有代码修改并且部署”；涉及原生 `BobberBar` 构造边界、`Farmer.caughtFish` 结算、集合页绘制，执行完整调用链与 R0/R1/R2）
**状态**：R0 ✅ / R1 ✅ / R2 ✅；Release Rebuild ✅（0 警告 0 错误）；**首次部署被真实游戏启动推翻（反证 #1）→ 根因已证实并修复 → 修复版已部署**（2026-08-07 07:22）；真实验收待用户执行
**当前唯一根因/任务**：按用户定稿的 5 项修改实现并部署：① 尺寸数字正等级每级 +10%、负等级每级 -5%（与数量倍数解耦）；② 同一鱼种连续失败 ≥2 次且本次调整后难度 ≥150 → 史诗失败提示；③ 每次成功额外 `round(调整后难度/50)` 等级；④ 调整后难度 >100 时加速度增幅按等级档位降为 30%（等级 ≥89 保持 100%）；⑤ 收藏页星标改为皇冠（原生 `Characters\Farmer\hats` 的 Infinity Crown 贴图，源矩形 (20,800,20,20)）。
**唯一下一步**：等待用户启动游戏真实验收（五项各自独立标记）。

## 玩家需求（原话要点）

1. “鱼的尺寸数字上（不是视觉），改为每一个正的等级增加10%的大小，每一个负的等级减少5%的大小。”
2. “玩家连续两次挑战失败难度150以上的鱼，失败之后弹出的提示改为，类似于：收集更多徽章（鱼类收集品页的皇冠），获得更多加成，再挑战史诗级强者吧。”
3. “每次钓鱼成功，难度等级，额外增加难度除以50的值，四舍五入到整数。比如难度500，则额外加10难度等级。减少肝度。”
4. “超过100的难度系数，对于小游戏中鱼加速度，降为现在的30%，比如190难度系数对加速度的加成应该是30%。89级以上难度等级的鱼不降为30%，依然是100%加成。”
5. “鱼的收集品页面的星星改为皇冠。”

## 实测推翻与修复（反证 #1，2026-08-07 06:56 真实游戏启动）

- **现象**：修复前的 BATCH-029 部署版（DLL SHA-256 `293F6DC0...`）在游戏启动时 `FishingExpanded 初始化失败: HarmonyException: Patching exception in method ... BobberBar::update`，内层 `InvalidProgramException: Common Language Runtime detected an invalid program`；Mod 全部补丁未生效（日志无任何钓鱼活动）。证据日志已封存 `RuntimeEvidence\20260807-BATCH029-INIT-FAIL\SMAPI-latest.txt`（SHA-256 `85194191...`）。
- **根因（已证实，非猜测）**：`GetAccelerationBoost` 签名从 `(float)` 改为 `(BobberBar, float)`，但注入序列未同步调整。原序列 `[Ldarg_0, Ldfld difficulty, Call, Mul]` 中 `Ldfld` 会**消费**注入的 `this`，调用前栈顶实际为 `[value, difficulty]`，与 `(BobberBar, float)` 需要的 `[instance, difficulty]` 类型不匹配；Harmony/MonoMod 在补丁应用时 `RuntimeHelpers.PrepareMethod` 强制 JIT，CLR 直接抛 `InvalidProgramException`。旧版 `(float)` 签名与该序列匹配（实测通过），因此 BATCH-028 未暴露。
- **修复**：注入序列改为 `[Ldarg_0, Dup, Ldfld difficulty, Call GetAccelerationBoost(BobberBar,float), Mul]`（`Dup` 复制一份 `this` 供 `Ldfld` 消费，保留实例给 call），`i += 5`。
- **验证（静态测试台 `_analysis\batch029-transpiler-check`，加载真实游戏 DLL 与真实 Mod DLL，对真实 `BobberBar.update` 应用该 Transpiler 并强制 JIT）**：旧版 BATCH-028 备份 DLL = PASS（对照）；修复前部署版 = FAIL（与游戏日志一致的 `InvalidProgramException`，精确复现）；修复版 = PASS。测试台 `--patchall` 模式在 `FishingRod.draw` 出现 NRE，旧版对照同样出现 → 属测试台缺少 SMAPI Mod 上下文的伪影，非真实 bug（旧版在真实游戏 22:30 正常完成该注入）。
- **修复版已部署**（DLL SHA-256 `26DEE6F2...`，源/目标一致；备份 `DeploymentBackups\FishingExpanded-20260807-072236-pre-BATCH029-fix1` 含被推翻版 `293F6DC0...`）；验收仍按五项独立标记。

## 已证实事实（当前安装 `D:\GGGGG\K1515\Stardew Valley.dll` SHA-256 `DFE341CA...` 未漂移；本轮 XNB 取证 2026-08-06）

- `BobberBar` 构造边界当前把 `fishSize` 乘以数量倍数（`___fishSize = (int)(___fishSize * quantityMultiplier)`）；数量倍数与尺寸数字共用同一个倍数是旧设计，本轮按用户要求解耦：尺寸数字改用等级线性倍率，数量倍数继续只用于鱼获数量。
- 成功结算唯一入口 `FarmerFishingPatches.CaughtFish_Postfix`（`Farmer.caughtFish` Postfix）：当前只按脱杆次数 `10/5/2/1` 计算等级增长，然后交给 `DifficultyManager.RecordSuccess` 统一执行称号区间封顶（`GetNextRankCeiling`）。
- 失败结算唯一入口 `BobberBarPatches.Update_Postfix`：`___fadeOut && distanceFromCatching <= 0 && !FailureRecorded` 时调用 `DifficultyManager.RecordFailure` 并显示失败 HUD。
- 加速度增幅唯一入口 `BobberBarPatches.GetAccelerationBoost(float difficulty)`（Transpiler 在唯一 `stfld bobberAcceleration` 前注入 `ldarg.0/ldfld difficulty/call/mul`，注入序列不变即可改签名）。
- 原生无 UI 皇冠贴图：全量反编译 948 个类仅 1 处 crown（帽子 `(H)LaurelWreathCrown`）；`Cursors.xnb`/`Cursors_1_6.xnb` 无皇冠图标（金色连通域扫描 + ASCII 渲染核验）。**皇冠取证结论**：`Data/hats.xnb` 解析（XNB 字符串尾随 0x02 格式）确认 `InfinityCrown`（Infinity Crown）SpriteIndex=121；`Characters\Farmer\hats.xnb`（240×880 RGBA，4 方向 80px 带布局）源矩形 `(20, 800, 20, 20)` = 金色三尖皇冠带红宝石，方向 0 副本；LaurelWreathCrown（idx 97，`(20,640)`）实际为绿色月桂花环，不适合做金色皇冠标记。
- 临时日志 `D:\GGGGG\临时\SMAPI-latest.txt`（2026-08-06 23:25 退出）：FishingExpanded 无错误；BATCH-028 实测通过记录在案（`(O)138` 等级5→155 跳8秒、`(O)145` 等级89→1338 跳3秒、称号 88=神王/89=创世神生效）。

## 第一处分歧

- 旧设计把“尺寸数字”与“数量倍数”绑在一个倍率上，且高难度下尺寸数字增长过快（100级=200×），用户要求尺寸数字按等级线性（100级=11×、-10级=0.5×）；旧失败提示不区分连续失败；成功等级不含难度折算（肝度高）；加速度增幅在高难度（≤88 级）过高；收藏页用星标而非皇冠。

## R0：实现内容（允许修改范围）

- `Source/Utils/DifficultyCalculator.cs`：新增纯函数 `GetFishSizeMultiplier(int level)`：正等级 `1 + 0.10×level`（100级→11）、负等级 `1 - 0.05×|level|`（-10级→0.5，防御钳 ≥0.1）、0 级 = 1。
- `Source/Data/FishDifficultyData.cs`：`FishStats` 新增 `ConsecutiveFailCount`（旧存档反序列化缺失字段默认 0，兼容安全）。
- `Source/Services/DifficultyManager.cs`：`RecordFailure` 成功后 `ConsecutiveFailCount = SaturatingAdd(...,1)` 并写入日志；`RecordSuccess` 清零；新增 `GetConsecutiveFailCount(string fishId, Farmer player)`（与 GetDifficultyLevel 同型，鱼王/无数据返回 0）。
- `Source/Patches/BobberBarPatches.cs`：
  - `Constructor_Postfix`：`___fishSize = (int)Math.Round(___fishSize * GetFishSizeMultiplier(level))`（保留 `QuantityMultiplier` 只用于数量）；构造边界新增一次性日志输出加速增幅档（`30%（等级≤88）` / `100%（等级≥89）`）。
  - `GetAccelerationBoost` 签名改为 `(BobberBar instance, float difficulty)`：d≤100 → 1；d>100 且等级 ≥89 → `1 + (d-100)/100`（100%）；等级 ≤88 → `1 + 0.3×(d-100)/100`（现增幅的 30%，例：190 → ×1.27）。无 InstanceData（鱼王）默认按 89 级档（100%），鱼王 difficulty 未被调整故恒为 1，天然豁免。Transpiler 注入序列不变。
  - `Update_Postfix` 失败分支：`RecordFailure` 后取 `GetConsecutiveFailCount`，若 `≥2 && data.AdjustedDifficulty >= 150f` → 史诗失败提示（新 i18n 键），否则原提示。
- `Source/Patches/FishingRodPatches.cs`：`CaughtFish_Postfix` 等级增长 = 脱杆基数 + `(int)Math.Round(data.AdjustedDifficulty / 50f)`（例：难度500 → +10），统一交给 `RecordSuccess` 称号区间封顶；日志增加“额外难度增益”。
- `Source/Services/HUDNotifier.cs`：`ShowFailureNotification` 增加 `isEpicChampion` 参数；史诗提示用 `HUDMessage.achievement_type` 显示新键文案。
- `Source/Patches/CollectionsPagePatches.cs`：`Draw_Postfix` 星标矩形 `(346,392,8,8)`/`Color.Gold` 改为皇冠：纹理 `Game1.content.Load<Texture2D>("Characters\\Farmer\\hats")`（静态缓存），源矩形 `(20,800,20,20)`，`Color.White`（保留金色与红宝石原色），目标矩形保持 `(bounds.X+3, bounds.Y+3, 24, 24)`。
- `Source/i18n/default.json` + `zh.json`：新增 `hud.fail.epic`（两边同步，39→40 键）。
- 治理：`GAME-DESIGN.md`、`BUG-LEDGER.md`、`TESTING.md`、`LOCAL-PITFALLS.md` 同步。

## 冻结 Case/禁止范围

- BATCH-024/025/026/027/028 未验收项、鱼王完整豁免、非鱼类 8 级上限、蓄力槽保护、脱杆次数、数量/品质/经验结算、视觉缩放公式与锚点合同、多人按玩家隔离全部冻结；不顺手修改其他文件。

## R1：删除与收敛

- 无新增持久集合或缓存：`ConsecutiveFailCount` 并入既有 `FishStats` 持久对象（`Farmer.modData` 序列化，旧存档缺失字段默认 0）；加速度增幅仍走唯一 `GetAccelerationBoost` 入口（签名变化，Transpiler 注入序列不变）；史诗提示只替换失败提示文案，不新增第二个状态所有者（连续计数唯一写入者 = `DifficultyManager.RecordFailure/RecordSuccess`）。
- 旧行为删除：`fishSize × 数量倍数` 在构造边界的唯一写入点被替换为 `fishSize × 等级尺寸倍率`；数量倍数不再参与尺寸数字。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 尺寸数字：等级 0 / +1 / +10 / +100 / -5 / -10 | 1.0 / 1.1 / 2.0 / 11.0 / 0.75 / 0.5 倍（四舍五入） | 尺寸数字按数量倍数增长（100级 200×） |
| 尺寸数字与数量解耦 | 数量倍数照常（100级 200 条），尺寸数字 = 等级线性 | 数量或动画数量受尺寸倍率影响 |
| 连续失败提示：同鱼连续失败第 2 次起且调整后难度 ≥150 | 史诗提示“收集更多徽章（鱼类收集品页的皇冠）…” | 第 1 次失败、难度 <150、鱼王出现史诗提示 |
| 连续失败清零：成功后再次失败 | 恢复普通失败提示 | 成功后计数不清零 |
| 成功额外等级 | 调整后难度 500 → +10；与脱杆 10/5/2/1 叠加后由称号区间封顶 | 额外等级绕过区间封顶 |
| 加速度档：调整后难度 190、等级 ≤88 | 增幅 = 1 + 0.3×0.9 = ×1.27（现 +90% 的 30%） | 190 仍 ×1.9 |
| 加速度档：调整后难度 190、等级 ≥89（如 89 级） | 增幅 = 1 + 0.9 = ×1.9（100%） | 89+ 被降到 30% |
| 加速度档：d≤100 | 恒 1（原生不变） | 任何档位逻辑影响 ≤100 |
| 鱼王 | 无 InstanceData，不参与任何新规则 | 鱼王尺寸/加速度/提示被修改 |
| 收藏页皇冠 | 星标位置显示金色皇冠（Infinity Crown 贴图），鱼王无星标故不显示 | 显示旧星标、贴图缺失、崩溃 |
| 双人同屏/联机 | 连续计数/尺寸/加速档按玩家与实例隔离（BATCH-027 沿用） | 副屏/客机串用主玩家数据 |
| 旧存档兼容 | 无 `ConsecutiveFailCount` 字段反序列化为 0 | 加载崩溃或计数为垃圾值 |
| 性能 | 皇冠贴图静态缓存一次；构造期一次性日志 | 每帧加载贴图或逐帧日志 |

## 可观测性决定

- 构造边界一次性日志：`加速增幅档 30%（等级≤88）/ 100%（等级≥89）`，每实例一条，不逐帧。
- 成功结算日志追加 `额外难度增益: +X`。
- 失败记录日志追加 `连续失败次数: N`。
- 纯函数（尺寸倍率、加速度档位）静态合同可完整证明，符合免除条件。

## 自动化验收决定

- 复用 `fish_setlevel <鱼ID> <等级>` + `fish_info` + `FISH-CLI-01` 全量 `Saves` 沙箱 + 真实原生钓鱼入口：
  1. 尺寸数字：`fish_info (O)145` 各等级核对（0/10/100/-10），或钓起后结算面板尺寸数字。
  2. 连续失败：`(O)145` 设等级 89（调整后难度 ≈1338 ≥150），真钓失败两次，第二次出史诗提示；成功后再失败恢复普通提示。
  3. 额外等级：难度约 500（等级约 34 → 30×(1+0.49×34)≈530 → +11），看日志 `实际增长` 含额外增益。
  4. 加速档：构造期一次性日志核对 30%/100% 档；等级 88 vs 89 各验一次。
  5. 皇冠：打开收藏品页面目视金色皇冠。
- 每项独立记录通过/失败/未执行；真实验收由用户执行（本轮不启动游戏）。

## 构建与部署事实

- Release Rebuild（2026-08-07 00:55）：0 警告 0 错误；ilspycmd 反编译核验关键符号（`GetFishSizeMultiplier`、`GetConsecutiveFailCount`、`GetAccelerationBoost(BobberBar,float)`、`GetCrownTexture`、`hud.fail.epic`、结算 `round(AdjustedDifficulty/50)`）。
- 构建产物 SHA-256：`FishingExpanded.dll`（修复版）`26DEE6F265301217338F9BB9B4E8425999B8319B7A31353532F81AD5BEBD81F7`；`manifest.json` `B906DCF8E2BC07D44F74A630B1B58BC550CD07E99849DAF96776E6853C2A1CEB`；`i18n\default.json` `CA25BDC83F30A2F64E93E1CAF73A718195F1AA270A02D40CA0D7D8A23F4618FA`；`i18n\zh.json` `A41A57F24B259F9E464122823DEA59EA07D69881D9E25818661455ABCBAD0AB2`。
- 首次部署（2026-08-07 00:56，用户授权）：备份 `DeploymentBackups\FishingExpanded-20260807-005605-pre-BATCH029`；替换 DLL/manifest/i18n×2；源/目标 SHA-256 一致；被真实游戏启动推翻（见上节）。
- 修复版部署（2026-08-07 07:22）：备份被推翻版至 `DeploymentBackups\FishingExpanded-20260807-072236-pre-BATCH029-fix1`；替换 `FishingExpanded.dll`（`293F6DC0...` → `26DEE6F2...`）；源/目标 SHA-256 一致；manifest/i18n 未变化；部署目录无 `config.json`（未涉及）。
- 进程门禁：Stardew Valley.exe 未运行（游戏已退出）；存在一个提权孤儿 SMAPI 控制台 PID 10556（22:44 启动、无窗口、非本 shell 可关闭），独占打开测试确认其不锁定任何部署文件，未影响部署；如需清理可手动结束该进程。

## Closeout 门禁（2026-08-07）

- 治理同步：本卡 + `BUG-LEDGER.md`（活动块/门禁/表格行）+ `GAME-DESIGN.md`（尺寸数字公式、额外等级收益、加速度档位、史诗提示、皇冠共 5 处）+ `TESTING.md`（3 行矩阵）+ `LOCAL-PITFALLS.md`（XNB 取证章节）已同步。
- Git 基线：HEAD `8f799da`；工作树为 BATCH-024~029 混改未提交基线（不 `git add -A`，未提交——用户未要求提交）；本轮只改 BATCH-029 范围内的源码/治理/构建/部署文件。
- 聚焦 diff：`DifficultyCalculator.cs`（新增 `GetFishSizeMultiplier`）、`FishDifficultyData.cs`（`ConsecutiveFailCount`）、`DifficultyManager.cs`（成功清零/失败累加/`GetConsecutiveFailCount`）、`BobberBarPatches.cs`（fishSize 线性倍率、加速档双档、史诗失败分支、恢复被误删的 `GetJumpInterval` 头）、`FishingRodPatches.cs`（`round(AdjustedDifficulty/50)` 额外等级）、`HUDNotifier.cs`（史诗参数）、`CollectionsPagePatches.cs`（皇冠绘制+静态缓存）、`i18n`×2（`hud.fail.epic`）、`manifest.json`（描述 BATCH-028/029）。
- 委派评估：不委派（Luna 当前模型禁止委派）。
- 构建结果：Release Rebuild 0 警告 0 错误（首次与修复版各一次）；ilspycmd 反编译核验关键符号齐全；Transpiler 修复经测试台对真实游戏 DLL 验证（旧版 PASS / 被推翻版 FAIL / 修复版 PASS）。
- 部署哈希：修复版源/目标 4 文件 SHA-256 逐一一致；备份 `DeploymentBackups\FishingExpanded-20260807-005605-pre-BATCH029`（部署前基线）与 `FishingExpanded-20260807-072236-pre-BATCH029-fix1`（被推翻版）。
- 进程门禁：Stardew Valley.exe 未运行；PID 10556 已确认为 0 线程/0 句柄僵尸进程（tasklist 状态 Unknown、CPU 0），不运行代码、不锁定文件，不影响游戏启动；用户已确认可忽略。

## 委派评估

- 不委派（Luna 当前模型禁止委派；且原生契约取证/所有权裁决属主线程职责）。