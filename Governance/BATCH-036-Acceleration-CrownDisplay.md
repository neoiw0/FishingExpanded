# FishingExpanded 根因批次：BATCH-036 图鉴皇冠加成显示（加速度档位公式还原核验）

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前唯一执行批次 | BATCH-036（2026-08-09 用户新一轮指令：难度手感 + 图鉴皇冠显示） |
| 本轮用户问题范围 | ① "4 级小丑鱼（SVE Clownfish）远难于鱼王，检查公式；疑似加速度问题"：要求 200 难度时加速度为原版最高加速度的 110%，额外 10% 算法 = 10%×(200-100)/100；② "有皇冠的鱼只显示挑战等级、未显示其提供的加成"：要求显示"钓鱼控制力+1" |
| 本批次纳入 Case | FE-036-1（加速度增幅统一 10% 线性）、FE-036-2（图鉴皇冠加成显示） |
| 共享第一处分歧与所有权链证据 | 两现象所有权链独立（BobberBar 小游戏参数 vs CollectionsPage 展示），按用户同轮指令合并为一个实现批次；Case 各自独立门禁与验收标记 |
| 本轮准入证据 | 用户 2026-08-09 指令原话（见上）；当前安装 DLL D:\GGGGG\K1515\Stardew Valley.dll SHA-256 DFE341CA... 2026-08-09 复核未漂移；已部署 Mod DLL CB0A2B69...（BATCH-035 fix4）反编译确认旧公式 1 + ratio×(d-100)/100（等级<98→0.1、≥98→1.0） |
| 现有实现复核 | 原生 BobberBar.update（当前 DLL 反编译 _analysis\StardewValley.BobberBar.decompiled.cs 行 392）：bobberAcceleration = (target-pos) / (random(10,30) + (100 - min(100,difficulty)))，d>100 时原生分母已恒为 random(10,30)（即原版最高加速度档）；GetAccelerationBoost（BATCH-028/029/034）在 stfld bobberAcceleration 前乘增幅；图鉴 CollectionsPagePatches.CreateDescription_Postfix 只追加挑战等级（level>0），无皇冠加成行 |
| 允许修改范围 | Source：BobberBarPatches.cs（GetAccelerationBoost + 构造日志档位文案）、CollectionsPagePatches.cs、i18n/zh.json、i18n/default.json（collections.crownControl）；文档：GAME-DESIGN.md、BUG-LEDGER.md、TESTING.md、TESTING-GUIDE.md、本卡 |
| 冻结 Case/禁止范围 | 不动原生分母/换目标频率/dart 偏移/跳鱼机制/手感系统/帧率解耦（BATCH-028/034 已验收设计冻结）；不启动游戏、不部署（用户本轮未授权部署）；不覆盖部署目录 config.json |

| 字段 | 值 |
|---|---|
| 批次目标 | 皇冠显示已实现；加速度公式按用户确认还原为 98 阈值档位；一次构建核验（部署与实机验收待用户授权） |
| 可观测性决定 | 公式为静态合同可证（纯函数，无新增运行时诊断）；构造日志档位文案同步更新（一次性日志，非逐帧） |
| 自动化验收决定 | 复用真实 BobberBar 小游戏路线 + 控制台 fish_setlevel（难度等级）+ 构造日志核对调整后难度与增幅；图鉴用 Collections 页实测；存档影响=无（纯展示与纯公式，无新持久字段）；成功判据=难度 200 时增幅恰 ×1.10（反编译/日志）、皇冠鱼图鉴出现"钓鱼控制力+1"、非皇冠鱼不出现；失败判据=Transpiler 栈序变更、文案出现在非皇冠鱼、98 档位分支残留 |
| 当前阶段 | FE-036-1 已按用户指示还原（98 阈值公式正确，代码已还原，构建待执行）；FE-036-2 皇冠显示已实现；均未部署 |
| 当前工作树 | 部分修改（BATCH-032~035 未提交 + 本轮；来源已核对） |
| 唯一下一步 | Release 构建 + 反编译核验（还原后的公式 + 皇冠显示）→ 等待用户授权部署与实机验收 |

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-036-1 加速度档位公式 | 🚫 已还原（用户 2026-08-09 晚确认旧 98 阈值公式正确，BATCH-036 临时统一改动作废） | 构建核验 + 部署授权 + 实测 |
| FE-036-2 图鉴皇冠加成显示 | R0 ✅ R1 ✅（i18n 双语言同步） | 构建 + 部署授权 + 实测 |

## 取证与契约证据（2026-08-09）

### FE-036-1 加速度公式
- 原生公式（当前 DLL DFE341CA... 反编译，_analysis\StardewValley.BobberBar.decompiled.cs 行 392）：
  bobberAcceleration = (bobberTargetPosition - bobberPosition) / (random(10,30) + (100 - min(100,difficulty)))
- 旧 Mod 公式（已部署 CB0A2B69... 反编译确认）：d≤100 → 1；d>100 → 1 + ratio×(d-100)/100，ratio=0.1（等级<98）/1.0（≥98）。
- 玩家现象数值推演（SVE 小丑鱼 FlashShifter.StardewValleyExpandedCP_Clownfish，原生 difficulty=45、dart；数据 D:\GGGGG\K1515\Mods\[CP] Stardew Valley Expanded\code\Other\Fish.json 行 33）：
  - 难度等级 4 → 倍数 2.96 → 调整后 133.2 → 旧增幅 ×1.033、新增幅 ×1.033（该档不变）。
  - 与鱼王对比（Legend 原生 110）：两者 d>100 时分母均=random(10,30)，基础加速度几乎相同；"远难于鱼王"主要来自 d>100 时原生减速项 +(100-min(100,d)) 消失（基础加速度跳到原版最高档）、dart 型每帧 13.3% 概率 ±316~366 大偏移换目标、初始目标=顶部、换目标频率高于鱼王。该结论已记入活动卡，供用户后续决定是否进一步调整（本轮只按指令改增幅公式）。
  - 用户 2026-08-09 晚确认：旧档位公式（<98→10%、≥98→100%）正确，BATCH-036 曾临时统一 10% 线性（仅影响 ≥98 档），已按用户指示还原为原公式。
- R0（已还原）：GetAccelerationBoost 恢复等级档位公式（<98→ratio 0.1、≥98→ratio 1.0，鱼王默认 98 档）；Transpiler 注入签名 (BobberBar, float) 不变，IL 栈序零改动。
- R1：还原 ratio 档位分支、构造日志档位文案与注释；InstanceData.DifficultyLevel 继续被助战/建议等使用。

### FE-036-2 图鉴皇冠加成
- 现状：CreateDescription_Postfix 只追加挑战等级（level>0）；皇冠鱼（CollectionStars）无加成行；鱼王整体 return（BATCH-014 豁免）。
- R0：皇冠判定先行（DifficultyManager.HasCollectionStar）；非鱼王保持挑战等级；有皇冠追加 collections.crownControl（钓鱼控制力+1 / Fishing Control +1）；鱼王有皇冠也显示加成行（BATCH-034：原版 5 传奇一次钓获直接给皇冠，皇冠计入手感进度 α）。
- R1：无旧路径删除（仅新增展示行）；i18n 双语言同步。
- 语义说明：皇冠是手感进度 α（可计数皇冠÷61）与助战概率的来源；"钓鱼控制力+1"为用户确认的展示文案；Mod 鱼皇冠按门禁 14 不计入 α/助战，该行仍按用户指令对全部皇冠鱼统一显示（纯展示，不新增状态、不写存档）。

## 状态与生命周期

| 状态/事务 | 创建者 | 唯一写入者 | 消费者 | 容量/频率 | 失效条件 | 清理 |
|---|---|---|---|---|---|---|
| GetAccelerationBoost | BobberBarPatches 纯函数 | 只读 | Transpiler stfld 注入 | 每帧一次 | 无 | 无 |
| 图鉴皇冠加成行 | CollectionsPagePatches 展示 | 只读 | Collections 描述 | 打开描述时 | 无 | 无 |

- 无新持久字段、无新字典/队列/缓存；两处均为既有展示/公式边界的修改。

## R2：场景与反向测试

| 场景 | 预期 | 不能发生 |
|---|---|---|
| 非鱼王 d≤100 | 增幅=1，原生公式不变 | 任何 >100 行为 |
| 非鱼王 d=200、等级 <98 | 增幅 ×1.10（10% 增长） | 档位分支缺失 |
| 非鱼王 d=190、等级 <98 | ×1.09；等级 ≥98 → ×1.9（100% 增长） | 档位分支错误 |
| 鱼王（原生 110） | 无 InstanceData → 默认 98 档 → ×1.10 | 鱼王触发鱼跳/其他修正 |
| 等级 ≥98 鱼 | 增幅 100% 增长（200→×2.0、500→×5.0，与 BATCH-034 门禁 16 一致） | 10% 统一改动残留 |
| 图鉴：无皇冠鱼 | 只显示挑战等级（level>0） | "钓鱼控制力+1"出现 |
| 图鉴：皇冠鱼（普通/Mod/扩展传奇） | 挑战等级 + "钓鱼控制力+1" | 皇冠行缺失 |
| 图鉴：皇冠鱼王 | 只显示"钓鱼控制力+1"（无挑战等级） | 挑战等级出现在鱼王 |
| 双人同屏/联机 | 公式与展示按当前玩家数据（BATCH-027 沿用） | 串用主玩家数据 |
| 构建 | Release 0 警告 0 错误；反编译确认新公式与 i18n | 旧公式残留 |

## 委派评估（2026-08-09）

- 用户 2026-08-09 明确指令：不再允许委派、不切换思考强度（BATCH-035 卡记录，本轮继续遵守）。
- 本轮未委派；阶段门禁记录 Prohibited（用户指令）。
- 不发送 EFFORT_GATE 标记。

## 设计变更记录（2026-08-09）

- 加速度档位公式：BATCH-036 曾临时统一 10% 线性，2026-08-09 晚用户确认旧公式正确后已还原（BUG-LEDGER 门禁 16 与 GAME-DESIGN 同步还原）。
- GAME-DESIGN 4.4 图鉴显示增强：有皇冠的鱼追加"钓鱼控制力+1"行；鱼王豁免挑战等级但皇冠行照常显示。
