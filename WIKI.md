# Fishing Expanded — 玩家 Wiki（资深玩家版）

> 面向资深玩家的完整机制文档。内容基于 `GAME-DESIGN.md` 与当前源码（manifest 版本 `0.5.10`，2026-08-16 更新至 BATCH-068：节日钓鱼开关（BATCH-066）、难度等级挂钩固定钓鱼等级与成功保底 +1（BATCH-067）、挑战掉星折扣作用于最终数量（BATCH-067）、经验公式难度输入钳制 [原生难度, 120] 与经验倍数 10 级 ×5 → 100 级 ×20 线性（BATCH-068）；数量倍数自 BATCH-062 起 = 等级×1）。公式与阈值以代码实际行为为准；与设计文档不一致处已用「实现说明」标注。
>
> English readers: every section has an English mirror below the Chinese text.

---

## 1. 概览 / Overview

- **Mod 名称**：Fishing Expanded（UniqueID：`YourName.FishingExpanded`，见 `manifest.json`）。
- **依赖**：SMAPI 4.0.0+，Stardew Valley 1.6+；可选 Generic Mod Config Menu（仅提供日志开关，未安装不影响）。
- **设置与日志**：首次运行自动生成 `config.json`（`EnableLogging` 默认 `true`）；安装 GMCM 后可在游戏内菜单关闭日志；关闭后 Mod 自身日志 0 条（BATCH-040）。GMCM 菜单另提供“重置挑战数据”章节（警告说明 + 开关待命，关闭菜单后弹确认框二次确认，回到刚安装状态；BATCH-042）。
- **核心内容**：每种鱼独立的“难度等级”、动态难度/数量/经验/品质/尺寸调整、蓄力槽保护、高难度运动强化与鱼跳、图鉴皇冠与鱼竿熟练度、挑战鱼饵、力竭机制、持久战奖励、皇冠助战、双通道浮动提示、超大鱼展示、NPC 反应、星之果茶掉落。
- **存档**：数据按“玩家 + 存档”独立保存（写在该玩家 `Farmer.modData`，键 `FishingExpanded/FishDifficultyData`，含皇冠集合 `CollectionStars` 与挑战皇冠 `ChallengeCrowns`；旧存档缺失字段默认空）；换存档、联机玩家互不共享。

English: Per-fish difficulty ranks with dynamic difficulty/count/XP/quality/size scaling, catch-bar protection, high-difficulty motion tuning and fish jumps, collection crowns with the rod-proficiency curve, challenge bait, exhaustion, perseverance rewards, crown assist, dual floating-tip channels, giant-fish showcase, NPC reactions, and Starfruit Tea drops. Auto-generated config.json (`EnableLogging`, default on) plus an optional GMCM toggle. The GMCM menu also provides a two-step reset section (arm toggle, then confirm in the question dialog) to reset all challenge data to a fresh-install state (BATCH-042). Data is saved per player+save in `Farmer.modData`.

---

## 1.5 术语表 / Glossary（BATCH-060）

| 术语 | 定义 |
|---|---|
| 难度等级 | 每鱼独立统计的成长值（范围 [-10, 100]，非鱼类 [-10, 8]），成功增减/失败 −1；驱动全部倍率与称号（§2） |
| 调整后难度 | 小游戏实际难度 = 原生 difficulty × 难度等级倍数；星标（≥120）、跳鱼（≥150）、力竭/挑战鱼饵（≥100/>100）等判定均基于它（§4） |
| 有效难度 | 战斗中实时难度 = 调整后难度经力竭衰减后的值（15 分钟降到 80；挑战鱼饵下恒等于开局值）（§9.3） |
| 难度档位 | 按有效难度划分的机制档位（跳鱼间隔 150/250/350/450/550 分档等）（§4.2） |
| 数量倍数 | 最终鱼获条数乘数 = 难度等级×1（0 级及以下 = 1；100 级 = 100）（§2.4，BATCH-062） |
| 固定钓鱼等级系数 | 难度等级增长系数 = max(固定钓鱼等级, 1)×0.1——固定钓鱼等级 = 玩家基础钓鱼等级字段（0~10，不含食物/饮料 buff 与助战临时等级）；1 级 ×0.1、10 级 ×1.0（§2.2，BATCH-067） |
| 经验难度钳制 | 经验公式的难度输入钳制在 [原生难度, 120]：低于原生难度（负数等级/力竭）按原生难度重算，高于 120 按 120 重算（§2.4，BATCH-068） |
| 可计数皇冠 | 56 条原生普通真鱼 + 5 条原版传奇鱼（共 61）的皇冠；Mod 鱼/扩展传奇皇冠只显示、不计入 α 与助战（§7.1） |
| 鱼竿熟练度 α | 可计数皇冠分段线性（0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100%），驱动小游戏手感（§7.2） |
| 挑战星 | 挑战鱼饵的原生 3 星计数；5:00 起每分钟掉 1 颗（≥95 豁免），每颗 −20% 鱼获（§8） |
| 背板种子 | 挑战鱼饵下同鱼同等级行为的固定随机种子，成功钓起后清除（§9.5） |

---

## 2. 难度等级系统 / Difficulty Levels

### 2.1 等级定义

- 每种鱼独立统计：`等级 = 成功次数 − 失败次数`，钳制在 `[-10, 100]`。
- 不同品质（普通/银/金/铱）算同一种鱼。
- 非鱼类（Category ≠ -4，垃圾/藻类等）上限为 **8** 级。
- 五大鱼王（传奇鱼）不参与本系统。

### 2.2 升级与降级

**成功钓起**时，先计算原始收益，再乘固定钓鱼等级系数并保底，最后应用称号区间封顶（不能一次跨越多个称号）：

| 脱杆次数 | 基础收益 |
|---|---:|
| 0 次（完美） | +10 |
| 1 次 | +5 |
| 2 次 | +2 |
| ≥3 次 | +1 |

- 额外收益：`+ round(调整后难度 / 50)`（调整后难度 500 → +10），与基础收益**叠加**。
- **固定钓鱼等级系数（BATCH-067）**：`请求增益 = max(1, round((基础收益 + 额外收益) × max(固定钓鱼等级, 1) × 0.1))`——固定钓鱼等级 = 玩家基础钓鱼等级（`Farmer.fishingLevel` 字段，0~10，**不含**食物/饮料 buff 与助战临时等级）；1 级 ×0.1、10 级 ×1.0（现有增长速度 = 满级玩家速度）、0 级钳 0.1；钓鱼等级越高等级加得越快。
- **成功保底（BATCH-067）**：乘系数并四舍五入后不足 1 时按 **+1** 计算——成功钓起至少 +1 级（已封顶的鱼仍为 +0，不违反封顶）。
- 称号区间封顶（见 2.3）、89 级以上每次成功最多 +6（BATCH-060）、非鱼类上限 8 级、挑战鱼饵失败不掉级（BATCH-058）均不受系数影响。
- **蟹笼（BATCH-065）**：每次收获固定 +1 等级（不吃固定钓鱼等级系数）；经验保持原生固定 5 点。

**失败**：等级 −1，同时累计“连续失败次数”（成功后清零，用于史诗失败提示）。（BATCH-058：挑战鱼饵失败不掉等级，连续失败计数照常 +1。）

English: Level = success − failure, clamped to [-10,100] ([-10,8] for non-fish). A successful catch grants base gain by escapes (perfect 0 escapes → +10, 1 → +5, 2 → +2, 3+ → +1) plus `round(adjustedDifficulty/50)`, then the total is multiplied by the fixed-fishing-level factor `max(base fishing level, 1) × 0.1` (level 1 → ×0.1, level 10 → ×1.0; buffs excluded), floored at +1 per successful catch, and capped at the next rank ceiling. Failure −1 and increments the consecutive-failure counter. Crab pots grant a fixed +1 level per haul without the factor.

### 2.3 称号表 / Rank Table

| 难度等级 | 称号 | 下一区间上限（本次增长上限） |
|---|---|---:|
| <1 | 额...稍微强一点的个体 | 3 |
| 1–3 | 精英 | 6 |
| 4–6 | 骑士 | 8 |
| 7–8 | 领主 | 15 |
| 9–15 | 伯爵 | 22 |
| 16–22 | 大公 | 33 |
| 23–33 | 亲王 | 45 |
| 34–45 | 帝王 | 66 |
| 46–66 | 神皇 | 88 |
| 67–88 | 神王 | 99 |
| 89–99 | 龙神王 | 100 |
| 100 | 太一 | 100 |

示例：等级 1（精英）+ 完美 (+10) 最多升到 6（骑士上限）；等级 7（领主）一次最多到 15（伯爵上限）。

### 2.4 等级效果公式 / Effects by Level

| 效果 | 公式 | 端点值 |
|---|---|---|
| difficulty 倍数 | 正数分段线性：0→×1.0、4→×1.2、20→×5、50→×20、100→×50（斜率 0.05 / 0.2375 / 0.5 / 0.6 逐段递增）；负数：`0.5 + 0.5×(level+10)/10` | -10 → 0.5；0 → 1；100 → 50 |
| 获得数量倍数 | `level ≤ 0 ? 1 : level`（BATCH-062，与经验倍数解耦） | 100 → 100；8 → 8；50 → 50 |
| 经验倍数 | 1~10 级：`max(1, round(level × 0.5))`（10 级 = ×5，BATCH-060 公式保持）；10~100 级：线性 `round(5 + (level−10)×15/90)`（BATCH-068：10 级 ×5 → 100 级 ×20）；≥100 封顶 20 | 10 → 5；50 → 12；100 → 20 |
| 经验难度输入 | 钳制在 `[原生难度, 120]`（BATCH-068）：低于原生难度（负数等级/力竭）→ 按原生难度重算（不低于未装模组）；高于 120 → 按 120 重算（基数不爆炸）；区间内不变；重算复刻原生公式 `max(1, (品质+1)×3 + 难度/3)` + 宝箱 +120%/完美 +140%/Boss ×5，随后仍乘经验倍数 | 上限 120（= 原生最高难度 110 以上一点） |
| 品质提升 | 门槛式“提升到”：`level ≥ 50 → 铱(4)；≥ 25 → 金(2)；≥ 10 → 银(1)；否则不提升`；最终品质 = max(原品质, 门槛)（BATCH-060） | 10 → 银；25 → 金；50 → 铱 |
| 尺寸数值（fishSize） | 正数：`1 + 0.1×level`；负数：`max(0.1, 1 − 0.05×|level|)` | 100 → 11×；-10 → 0.5× |
| 视觉缩放 | `1 + level × 0.0270843`（线性，BATCH-060 与数量倍数解耦、按难度等级） | 0 级 = 1.0；100 级 ≈ 3.71 |

- 品质映射：0=普通，1=银，2=金，4=铱（跳过 3）。
- **实现说明**：BATCH-060 起品质为“提升到”式门槛（10/25/50 → 银/金/铱），不再累加；50 级必铱。
- 尺寸数字在结算边界统一应用等级倍率（`pullFishFromWater`/`caughtFish`）；难度等级 >0 时脱杆不再触发原生 800ms 缩水、也不降品质（BATCH-038）。
- 负数等级时数量/经验保持 1×，品质不提升。

English: All effects scale with the level. Difficulty multiplier is piecewise-linear (anchors 0→×1.0, 4→×1.2, 20→×5, 50→×20, 100→×50, slopes increasing); quantity multiplier = level (0 or below = 1, BATCH-062); XP multiplier = max(1, round(level×0.5)) up to level 10 (×5 at 10), then linear ×5→×20 through level 100 (BATCH-068), and the XP formula's difficulty input is clamped to [native difficulty, 120] (negative-level/exhaustion recalc at native difficulty, >120 recalc at 120); quality is a "raise-to" threshold (10→silver, 25→gold, 50→iridium, final = max(base, threshold)); fishSize ×0.5→×11; held-item visual scale = 1 + level×0.0270843 up to ≈3.71 (decoupled from quantity, BATCH-060). At level >0 the vanilla escape-based size shrink and quality penalty are disabled.

---

## 3. 蓄力槽保护 / Catch-Bar Protection

鱼逃跑时蓄力槽下降速度被调低（倍率越小越慢、越容易救回）：

- **全局保护**（所有非鱼王难度等级，含 0 与正数）：蓄力槽 ≤20% → ×0.80；≤1% → ×0.50。
- **负数难度额外保护**（-10 ~ 0 线性）：≤40% → 0.99~1.0；≤20% → 0.60~1.0；≤1% → 0.20~1.0。
- **叠加规则**：与原生/其他 Mod 的倍率取**最小值**（效果最强）。
- 鱼王完整豁免：小游戏构造时直接退出自定义路径，不应用任何保护。

English: Drain-speed multiplier is lowered near the bottom (global: 0.80 at ≤20%, 0.50 at ≤1%) with extra negative-level protection (down to 0.20 at -10). All applicable modifiers combine by taking the minimum. Legendary fish are fully exempt.

---

## 4. 高难度运动机制 / High-Difficulty Motion (adjusted difficulty > 100)

仅非鱼王；`调整后难度 = 原版 difficulty × 难度倍数`。难度 ≤100 时原生运动公式完全不变。

### 4.1 公式修正

- **初始目标**：难度 >100 时固定为顶部（原生公式在 >100 会算出负坐标而失效）。
- **换目标概率封顶 150**：大范围 `min(难度,150)/4000`；小偏移（±50~100）`min(难度,150)/2000`；dart 型额外 `min(难度,150)/1000`，dart 偏移 `±(50~100+min(难度,150)×2)`。
- **加速度**：难度 >100 时加速度分母固定 `随机(10,30)`，增幅 = `1 + 档位×(难度−100)/100`；档位锚点曲线：0 级 10%、50 级 20%、70 级 40%、80 级 70%、90 级 100%、100 级 100%，点间线性，≤0 钳 10%、≥90 钳 100%（难度 190：80 级 → ×1.63、≥90 级 → ×1.9）；难度 ≤100 时增幅 = 1（原生）。

### 4.2 高难度鱼跳（调整后难度 ≥ 150）

| 调整后难度 | 触发间隔 |
|---|---:|
| 150–250 | 每 8 秒 |
| 251–350 | 每 6 秒 |
| 351–450 | 每 5 秒 |
| 451–550 | 每 4 秒 |
| ≥551 | 每 3 秒 |

- 每次跳跃完成后才开始下一次计时；冷却结束后每 1 秒检测一次鱼位置。
- 鱼在下 25%（位置 ≥399）→ 0.5 秒后瞬移到上 25%（0~133 随机）；鱼在上 25%（≤133）→ 瞬移到下 25%（399~532 随机）。
- 0.5 秒延迟期间鱼游出区域仍照跳；跳跃是瞬间位移（速度归零），不改变尺寸/数量/品质/结算。
- 每次瞬移在“鱼动作提示”通道显示贴鱼文案（上跳=“鱼跃”类、下跳=“甩尾！”类，各 15 条随机），见 5.6。

English: For non-legendary minigames with adjusted difficulty >100 the initial target is forced to the top, retarget probabilities are capped at 150, and acceleration scales by an anchor curve (level 0→10%, 50→20%, 70→40%, 80→70%, 90→100%, 100→100%, linear between; ≤0 clamps 10%, ≥90 clamps 100%). At ≥150 the fish also hard-teleports between the top/bottom quarters on a difficulty-based cooldown (8s down to 3s), with a jump action tip on each teleport.

---

## 5. HUD 提示系统 / HUD Messages

本 Mod 左下角消息走 **FIFO 队列**：同一时间只显示一条，前一条淡出后显示下一条；每玩家队列上限 5 条（满时丢最旧）；返回标题/换存档清空。覆盖：成功、失败、等级建议、皇冠挑战宣言、星之果茶、持久战奖励消息。**小游戏内的浮动提示（动作/其他）不走队列**，在钓鱼条两侧显示（见 5.6）。

### 5.1 成功提示

- 普通鱼未封顶：`下一次你将向{鱼名}中的{称号}发起挑战`（0 级/负数胜利也显示，用“额...稍微强一点的个体”）。
- 鱼达到 100 级（含封顶后再成功）：`你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。`
- 非鱼类达到 8 级：`对于{物品名}而言，你已是帝王`。
- 鱼王：随机 10 条独特文案（如“嗷！孤傲的王！”）。

### 5.2 失败提示（优先级从高到低）

1. **史诗提示**：同鱼种连续失败 ≥2 次 **且** 本次调整后难度 ≥150 → 未满皇冠时：`收集更多徽章（鱼类收集品页的皇冠），获得更多加成，再挑战史诗级强者吧`；**满皇冠**（鱼竿熟练度 α≥1）时替换为：`你需要进入人鱼合一的状态，你必须是鱼，才能赢下这场挑战。`（BATCH-038）
2. **触底**：等级 -10 → `最平庸的{鱼名}（10）依然太难了……`（含钓鱼等级/料理/鱼饵建议）。
3. **负等级**：`你对{鱼名}的了解加深了（{绝对值}）`。
4. **普通**：`还是挑战{鱼名}中更平庸的个体吧`。

### 5.3 等级建议（小游戏出现时）

- 当 `难度等级/10 > 当前钓鱼等级`：显示 `建议达到{X}以上钓鱼等级再挑战……`（鱼王豁免）。
- 当 `难度等级/10 > 当前钓鱼等级×2`：开头追加 `这是不可能的高难度挑战！`。

### 5.4 皇冠挑战宣言

- 进入小游戏时若该鱼已有图鉴皇冠**且难度等级 ≥1**（0 和负数不显示，BATCH-033）：显示 `{尊敬词}{鱼名}{等级称号}{毅然决然词}{应战说法}。`（每称号 3 种尊敬词、50 种毅然决然词、20 种应战说法，独立随机）。

### 5.5 星之果茶掉落（等级 ≥ 50 的普通鱼）

- 概率 = `难度等级/4`%（即 等级/400）：50 级 → 12.5%，80 级 → 20%，100 级 → 25%。
- 掉落 1 瓶 `(O)StardropTea`，走成功结算唯一边界（不重复发放）；背包满时走原生溢出菜单。
- 附带 15 条随机幽默消息之一。非鱼类上限 8 天然不参与；鱼王豁免；仅本地玩家判定。

### 5.6 小游戏浮动提示（双通道，BATCH-038/039/041）

- **鱼动作提示**（跳鱼“鱼跃/甩尾”类）：显示在**绿条右侧紧贴处**（左对齐；绿条原生几何=左缘 `xPositionOnScreen+64`、宽 36px，即锚点 `xPositionOnScreen+124`，文字左缘距绿条右缘固定 24px），起点固定在触发瞬间（鱼落地位置上方 30px）。
- **鱼其他提示**（助战/力竭节点/30 秒巅峰提示等）：显示在**绿条左侧紧贴处**（右对齐；锚点 `xPositionOnScreen+14`，文字右缘距绿条左缘固定 50px，绿条中部高度）。
- **屏幕边缘侧翻**：当绿条靠近屏幕边缘、原定一侧放不下文字时，提示自动翻到绿条另一侧（动作提示翻到左侧=右缘距绿条左缘 50px；其他提示翻到右侧=左缘距绿条右缘 24px），避免被屏幕钳制推到最右缘；保留 8px 屏幕边距兜底。
- 两通道：起点固定、默认显示 5 秒、5 秒内慢慢上移 30px 并线性淡出、可同时显示多条、每通道上限 8 条（超限丢最旧）；**助战文案例外 15 秒（BATCH-060）**；纯显示，不写任何游戏状态。

English: All corner messages are FIFO-queued per player (1 visible, cap 5). Success shows the next-rank title (weak title below level 1) or cap/legendary lines; failure hints follow priority epic (with the "union" variant at full crowns α≥1) > bottom(-10) > negative > normal. Entering a minigame may show a fishing-level recommendation (impossible warning at 2×) and a challenge declaration for crowned species at level ≥1. Non-legendary fish at level ≥50 have a level/400 Starfruit Tea drop chance on success. In-minigame floating tips use two fixed-anchor channels: action tips 3 bar-widths right of the green bar, other tips 3 bar-widths left, both rising 30px over 5s (cap 8 each).

---

## 6. 超大鱼与 NPC 反应 / Giant Fish & NPC Reactions

### 6.1 触发条件（三个同时满足）

1. 鱼被选中且**举起**（仅切换到物品栏不算；手柄同理）；
2. 该次钓获鱼的**难度等级 ≥ 8**（BATCH-060：巨型鱼与数量倍数解耦、直接按难度等级判定；原“数量倍数 >15”等价于等级 ≥8）；
3. 自钓到该鱼后**尚未进入过 FarmHouse 室内**。

### 6.2 效果

- **视觉缩放**：手持鱼、从水里飞向玩家的飞行动画、举起结算的真鱼都会放大（底边/中心锚点保持原生位置）；小游戏内的鱼标与结算面板示意图保持原生大小。
- **NPC 冒泡**：玩家 5 格（320 像素）内 NPC 头顶显示随机赞美文案（100 条，含鱼名 + 具体尺寸，单位统一为厘米：`fishSize×2.54` 取整）；每个 NPC 每天每种鱼最多 1 次。动物 NPC（宠物 Pet 按 petType=狗/猫、马 Horse）先叫一声再把赞美放括号里，如“汪汪！！！（这条狗鱼竟然有388cm简直是奇迹）”；每种动物 5 套叫声随机（BATCH-058）。
- **主动对话替换**：与 NPC 对话首次替换为赞美（同文案池），第二次恢复原对话；每 NPC 每天每种鱼最多 1 次。
- **每日重置**：凌晨清空冒泡/对话触发记录；**进入 FarmHouse 永久清除**当前手持超大鱼的放大效果（直到下次再钓到新的超大鱼）。
- 鱼王与垃圾/藻类等非鱼类不产生超大鱼展示（当前实现仅 Category = -4 的真鱼登记）。

English: A fish shows as giant when caught with a quantity multiplier >15 (level ≥8 → ×17), is being held up, and the player hasn't entered the FarmHouse since catching it. Giant fish get visual scaling, one NPC bubble per NPC/species/day within 5 tiles (size in cm), and one dialogue replacement per NPC/species/day. Entering the FarmHouse permanently clears the current giant display.

---

## 7. 图鉴、皇冠与鱼竿熟练度 / Collections, Crowns & Rod Proficiency

### 7.1 皇冠达成条件

- **成功钓起**该鱼，且本次小游戏的**调整后难度 ≥ 120**（例如原版 difficulty 3 × 50 倍 = 150）；仅进入小游戏或失败不获得；鱼王豁免。
- 原版 5 条传奇鱼（159/160/163/682/775）：**钓到一次直接给皇冠**（无难度门槛，BATCH-034）。
- 扩展传奇 898–902 属于可计数 61 池：按普通鱼规则（调整后难度 ≥120）给皇冠；**其他 Mod 鱼与不可触发小游戏的条目达到条件后给星星（早期 mouseCursors 金星画法），不计入鱼竿熟练度 α**（2026-08-15 用户指令）。

### 7.2 鱼竿熟练度 α（皇冠的永久作用，BATCH-034/039）

- α = 可计数皇冠数分段线性曲线：0→0%、5→2%、10→7%、20→20%、30→35%、61→100%；点间线性，超过 61 钳制 100%。
- 可计数皇冠 = 56 条原生普通真鱼 + 5 条原版传奇鱼（共 61）；Mod 鱼/扩展传奇不计。
- α 影响钓鱼手感：α=0 完全原生；α=1 终点手感——按下/松开瞬间直接定速、无加速度/条内阻尼/惯性、撞边完全钳制（不反弹、速度归零）；定速最大值 `30−5α`（α=1 → 25px/帧）；叠加方向系数：绿条中间朝鱼移动 ×(1+0.2α)、相背离 ×(1−0.2α)（α=1 → ×1.2/×0.8，BATCH-056 回调）。
- 隐藏钓鱼等级加成已整体删除（原每皇冠 +0.2 退休，BATCH-034）；帧率解耦覆盖整个小游戏（60fps 行为与原生一致）。

### 7.3 挑战皇冠（流光溢彩，BATCH-038）

- 难度等级 **≥95** 的鱼在**挑战鱼饵**生效时成功钓起（无论是否超过 5 分钟时限，超时只按掉星扣数量）→ 该鱼皇冠升级为**流动效果的金色皇冠**（图鉴页金/白呼吸 + 1±0.05 缩放脉冲）。
- 独立存档字段 `ChallengeCrowns`，旧存档缺失默认空；测试命令 `fish_challengecrown <鱼ID> [0|1]`。

### 7.4 皇冠助战（BATCH-035）

- 进入非鱼王小游戏时判定一次：总概率 = `10% × (可计数皇冠数/61)` 线性（无皇冠=0%、满皇冠=10%）。
- 命中后从玩家可计数皇冠鱼中**均匀随机**选一条助战鱼（所有皇冠鱼概率相同）。
- 临时钓鱼等级 = 0~40，随被选中鱼的难度排位线性倾斜（最高难度鱼更容易给高等级助战；平均 26.55 / 13.45）。
- 效果：本次小游戏绿条高度 +`临时等级×8px`；**不写玩家钓鱼等级、不入存档**，不影响经验/数量/品质/难度。
- 显示 20 条随机助战文案（“荣耀的【鱼种】【职阶】前来护驾！”等，进“鱼其他提示”通道）；**助战文案显示 15 秒**（15 秒内上移 30px 线性淡出，BATCH-060，仅助战延长，其余提示 5 秒）；挑战鱼饵生效时不触发助战；鱼王豁免。

### 7.5 图鉴显示

- 鱼类页：可计数 61 原生鱼图标左上角绘制金色皇冠（Infinity Crown 贴图）；有挑战皇冠的鱼以流动金色呼吸绘制；非鱼类与其他 Mod 鱼绘制早期金星（`Game1.mouseCursors (346,392,8,8)`，20×20 `Color.Gold`）。
- 描述新增行：`挑战等级：{称号}（{X}级）`（等级 ≤0 不显示；鱼王豁免挑战等级行）。
- 有皇冠的可计数鱼追加显示：`鱼竿手感增强`（i18n `collections.crownControl`；BATCH-058T 文案，机制名“鱼竿熟练度 α”保留）；星星条目显示 `无手感增强`（i18n `collections.noCrownControl`）；纯展示，不新增状态。

English: A permanent crown is earned by successfully catching a countable native fish with adjusted difficulty ≥120 (legendaries get one on their first catch). Other mod fish and non-minigame entries earn a classic gold star instead (display-only, not counted toward α). Crowns feed the rod-proficiency curve α (0/5/10/20/30/61 → 0/2/7/20/35/100%, capped at 61 countable crowns) which smooths the catch-bar feel (instant speed, no inertia, edge clamping, −30% max speed and directional bias at α=1). Catching a fish at level ≥95 with challenge bait upgrades its crown to a flowing golden one. Crowned fish can assist (10% × crowns/61 chance, temporary +0~40 fishing level on bar height only), and the collection page shows the crown plus an "improved rod handling" line, while starred entries show "no rod handling bonus".

---

## 8. 物品处理与鱼饵 / Item Handling & Baits

- 动画阶段始终显示原生数量的鱼（本 Mod 不伪造动画条数）。
- 数量倍数在原生 `CreateFish` 创建最终物品后一次性应用：`最终堆叠 = 原生堆叠 × 等级数量倍数`；背包与溢出菜单（`ItemGrabMenu`）共用同一个物品，不建立第二套溢出逻辑，不因倍数丢失鱼获。
- **万能鱼饵加成**（BATCH-038）：难度等级 >0 且原生本应给两条鱼（`numCaught ≥ 2`，非挑战鱼饵）时，原生堆叠 **+10 条**（原生 2 条 → 12 条），再乘等级数量倍数。
- **挑战鱼饵改版**（调整后难度 >100 时生效，BATCH-038）：
  - **5 分钟（300 秒）内成功** → 原生堆叠 `×1.5`（向上取整）再乘等级数量倍数；
  - **超时掉星（BATCH-058）**：5:00 起每分钟掉 1 颗原生挑战星（5:00→2、6:00→1、7:00→0，不可恢复；难度等级 ≥95 豁免），每掉 1 颗最终鱼获 −20%（0.8/0.6/0.4，**作用于乘完等级倍数后的最终数量**，四舍五入并兜底 ≥1 条——BATCH-067 回归设计口径；原生挑战鱼饵必给 3 条，恒 ≥1 条不归零）；等级收益、普通皇冠、流动金色皇冠、品质、尺寸全部照常（例：30 分钟钓到 98 级鱼仍给流动金皇冠）；**掉星瞬间左下角 FIFO 提示剩余星数与鱼获减少百分比（BATCH-060，小游戏期间可见）**；
  - **难度不衰减**（力竭衰减不适用，有效难度恒等于开局值）；
  - **无其他鱼助战**；**原生“3 次脱杆失败”规则禁用**（可继续挑战到成功）。
- 堆叠按原版规则（同种同品质可堆叠，上限 999）。

English: The quantity multiplier is applied once at item creation (final stack = native stack × multiplier), shared by the inventory and the vanilla overflow menu. Wild bait adds +10 to a native 2-fish catch at level >0. Challenge bait (adjusted difficulty >100) multiplies the native stack by 1.5 (ceiling) within 5 minutes; after 5 minutes one native challenge star drops per minute (5:00→2, 6:00→1, 7:00→0, unrecoverable; exempt at level ≥95) and each lost star reduces the final catch by 20% (0.8/0.6/0.4). Level/crowns/quality/size still apply, difficulty never decays, no assist, and the native 3-escape failure rule is disabled.

---

## 9. 特殊鱼类与持久战 / Special Fish & Perseverance

### 9.1 鱼王（五大传奇鱼，完整豁免）

传奇鱼 (163)、突变鲤鱼 (682)、鮟鱇鱼 (160)、冰川鱼 (775)、赤红鱼 (159)。

- 不记录等级、不应用难度/数量/品质/经验倍数、无挑战称号、无视觉缩放、无 NPC 反应、无蓄力槽保护、无等级建议、无助战、无力竭。
- **钓到一次直接给皇冠**并计入鱼竿熟练度 α（BATCH-034）。
- 鱼王 ID 按原生物品 ID 归一化（`163`/`(O)163`/`(o)163` 视为同一只）；扩展传奇 898–902 按普通鱼规则。
- 钓到后随机显示 10 条独特文案之一。

### 9.2 非鱼类（垃圾/藻类等，Category ≠ -4）

- 等级范围 [-10, 8]；达到 8 级后成功不再增加等级（底层统计保持 8）。
- 数量/难度/品质倍数仍按等级 8 计算（数量倍数 = 等级×1 = **8 倍**，BATCH-062；品质 8 级达不到 10 级门槛，不提升）。
- 8 级封顶后再成功显示 `对于{物品名}而言，你已是帝王`。
- **实现说明**：设计文档规定非鱼类封顶后仍参与视觉缩放/NPC/星标，但当前代码的巨型鱼登记限定 Category = -4 的真鱼，非鱼类不产生超大鱼展示；其原始难度极低，实际上也几乎不可能达到皇冠阈值。

### 9.3 力竭机制（调整后难度 ≥100 的非鱼王，BATCH-038）

- 节点与百分比：1/3/5/7/9/12/15 分钟 → 1%/3%/10%/20%/35%/50%/100%，节点间线性（含 0→1 分钟段）。
- 公式：`有效难度 = 调整后难度 + (80 − 调整后难度) × 百分比`；15 分钟（100%）时难度降到 80，之后保持 80。
- **奖励不下降**：数量/等级/皇冠结算以开局调整后难度快照为准；**经验基数同样不下降（BATCH-068）**——经验公式难度输入钳制在 [原生难度, 120]，有效难度低于原生难度时按原生难度重算，经验不低于未装模组。
- 挑战鱼饵生效时难度**不衰减**（有效难度恒等于开局值），但节点提示仍显示并追加一句随机文案。
- 每个节点在“鱼其他提示”通道显示一条随机诙谐文案（i18n `hud.exhaust.{1|3|5|7|9|12|15}.{1..10}`）。

### 9.4 持久战奖励（BATCH-039）

- 所有非鱼王小游戏的战斗秒数统一累计（暂停/菜单不计时；鱼王天然豁免）。
- **30 秒整**：在“鱼其他提示”通道单发一次巅峰提示（10 套随机文案，如“[职阶]的力气达到巅峰”）。
- **失败且战斗 ≥60 秒**：必得海泡布丁 1 个（`(O)265`，钓鱼 +4）。
- **30 秒 ≤ 战斗 < 60 秒**：50% 概率随机获得 +3 钓鱼料理之一（海之菜肴 `(O)242` / 烩鱼汤 `(O)728` / 龙虾浓汤 `(O)730`；BATCH-052 修正：228 实为生鱼寿司，海之菜肴=242）。
- 60 秒不叠加 30 秒的抽奖；奖励进背包（背包满走原生溢出菜单），提示 20 条随机诙谐文案入 FIFO 队列。
- 测试命令：`fish_persisttest <30|60>`。
- 钓鱼小游戏期间食物 buff（`id="food"`）计时暂停，退出后恢复走时（BATCH-056；饮料与其他 buff 照常）。

### 9.5 停战休息、跳鱼前摇与挑战鱼饵背板（BATCH-058）

- **停战休息**：绿条连续 3 秒不动 → 鱼在下一次出绿条外超过 5 像素时停下，缓慢摇头摆尾；期间蓄力槽不掉、战斗计时暂停；重新操作绿条恢复。触发时其他提示通道随机显示 10 套休战文案之一。
- **跳鱼前摇**：鱼跃/甩尾在原有 0.5 秒延迟后追加 0.88 秒前摇（鱼图标 0.77 秒旋转 150°、0.11 秒快速转回），随后瞬移；上跳逆时针、下跳顺时针，前摇期间鱼图标发红光呼吸脉动。鱼行动提示在跳鱼判定、0.5 秒延迟开始时即显示（比前摇再早 0.5 秒）。
- **挑战鱼饵背板**：同一种鱼、同一难度等级在挑战鱼饵下的行为（目标序列、dart、跳鱼时机）由固定种子决定，钓起前每次一致；失败不掉等级但连续失败计数照常；成功钓起后种子清除，下次重新随机。

### 9.6 高难鱼招式短语（BATCH-059）

- 调整后难度 ≥150 的非鱼王鱼：一次瞬移后鱼第一次到达中线（266±10，只判到达）为短语起点，下一次瞬移后再到中线为终点；这一段（含那次瞬移）反复循环播放，跳鱼前摇与行动提示一并重放。
- 蓄力进度每净涨 1/3（绝对值）换一段新短语；玩家可以背下短段落来应对，而不是面对 20 分钟随机运动。

English: The five vanilla legendaries are fully exempt from every system, grant their crown on the first catch (counted into α), and show one of 10 unique catch lines. Non-fish items cap at level 8 (multipliers still apply at 8), but giant-fish display only registers real fish-category items. Non-legendary minigames at adjusted difficulty ≥100 suffer exhaustion (nodes 1/3/5/7/9/12/15 min → 1/3/10/20/35/50/100%, floor 80 after 15 min) while rewards use the opening snapshot; challenge bait keeps difficulty constant. Battles past 30s get a peak tip; failing at ≥60s grants a Seafoam Pudding, 30–60s grants a 50% chance of a +3 cooking dish (no stacking).

---

## 10. 多人 / 联机 / 双人同屏

- 在线联机：主机与每位农场客**都必须安装**本 Mod；每位玩家只读写自己的 `modData`，数据随各自存档。
- 本地分屏：每位本地玩家独立实例，难度/星标/加成/展示/提示队列全部按 `UniqueMultiplayerID` 隔离。
- 掉落（星之果茶/持久战奖励）、结算、展示只对本地玩家生效。

English: Host and every farmhand must install the mod. All state is isolated per player by `UniqueMultiplayerID` (supports online co-op and local split-screen); drops and displays only apply to the local player.

---

## 11. 节日钓鱼开关 / Festival Fishing (BATCH-066)

`config.json` 键 `EnableFestivalFishingMods`（GMCM“节日钓鱼应用模组规则”，默认 **false**），覆盖三个钓鱼节日：

| 开关 | 行为 |
|---|---|
| **false（默认）= 完全原生** | 鱿鱼节（SquidFest，冬 12–13 沙滩）/ 鳟鱼大赛（TroutDerby，夏 20–21 森林）/ 冰雪节（Festival of Ice，冬 8 森林）：无难度等级注入、无数量倍数、无经验倍数、无品质加成、无模组结算/HUD/提示；分数与概率全原生（鱿鱼节每次成功 +原生数量 分；冰雪节每次成功 +1 分；鳟鱼大赛 tag 概率 33%×原生数量） |
| **true = 模组规则照常** | 难度等级/倍数/品质全部生效，且节日分数按数量倍数翻倍：鱿鱼节每次成功 +`原生数量×倍数` 分；冰雪节每次成功 +数量倍数 分（原生 +1，模组补 倍数−1；例：难度 10 → 一次 +10 分） |

- 冰雪节为事件型节日：鱼不进背包、无经验，结算走原生 `Event.caughtFish`（模组 Postfix 补分并消费式清理待处理事实）。
- 开关关闭时所有模组边界自动回落原生（BobberBar 构造/结算/经验五处边界统一判定），不残留状态。

English: The `EnableFestivalFishingMods` config toggle (default off) makes the three fishing festivals (SquidFest, TroutDerby, Festival of Ice) fully vanilla: no difficulty levels, multipliers, XP or mod settlement/tips; scores and tag chances stay vanilla. When enabled, mod rules apply and festival scores scale with the quantity multiplier (SquidFest +native count×multiplier per catch; Ice Festival +multiplier points per catch). The Ice Festival is event-based: fish don't enter the inventory and no XP is granted.

---

## 12. 控制台命令 / Console Commands（调试/测试）

| 命令 | 作用 | 示例 |
|---|---|---|
| `fish_setlevel <鱼ID> <等级>` | 直接设置难度等级 | `fish_setlevel 128 50` |
| `fish_setlevel [玩家序号] <鱼ID> <等级>` | 直接设置难度等级 | `fish_setlevel 128 50` / `fish_setlevel 2 128 50` |
| `fish_addsuccess [玩家序号] <鱼ID> <次数>` | 增加成功次数 | `fish_addsuccess 128 10` / `fish_addsuccess 2 128 10` |
| `fish_addfail [玩家序号] <鱼ID> <次数>` | 增加失败次数 | `fish_addfail 128 5` / `fish_addfail 2 128 5` |
| `fish_info [玩家序号] <鱼ID>` | 查看该鱼详细数据（等级/倍数/皇冠等） | `fish_info 128` / `fish_info 2 128` |
| `fish_list [玩家序号]` | 列出全部已记录鱼种 | `fish_list` / `fish_list 2` |
| `fish_clear [玩家序号] confirm` | 清空全部钓鱼挑战数据（需 confirm；含挑战皇冠与旧存档级键） | `fish_clear confirm` / `fish_clear 2 confirm` |
| `fish_addstar [玩家序号] <鱼ID>` | 强制添加收藏皇冠（测试） | `fish_addstar 128` / `fish_addstar 2 128` |
| `fish_addstars [玩家序号] <数量>` | 批量添加可计数皇冠（完整 61 鱼池：56 普通 + 5 原版传奇，提升 α） | `fish_addstars 20` / `fish_addstars 2 20` |
| `fish_challengecrown [玩家序号] <鱼ID> [0\|1]` | 设置挑战鱼饵流动金色皇冠标记（测试） | `fish_challengecrown 144 1` / `fish_challengecrown 2 144 1` |
| `fish_giant [玩家序号] <鱼ID> <难度等级>` | 模拟巨型鱼触发 NPC 反应（BATCH-060：按难度等级 ≥8 判定） | `fish_giant 128 20` / `fish_giant 2 128 20` |
| `fish_bonus [玩家序号]` | 查看收藏皇冠数与鱼竿熟练度 α | `fish_bonus` / `fish_bonus 2` |
| `fish_assist` | 强制下一次小游戏触发助战（测试） | `fish_assist` |
| `fish_assiststats [clear]` | 查看/清空助战观测统计（会话内，上限 500） | `fish_assiststats clear` |
| `fish_persisttest <30\|60>` | 强制下一次小游戏按指定秒数判定持久战奖励（测试） | `fish_persisttest 60` |
| `fish_selftest` | 自动自测（难度/可计数皇冠/助战分布/限频日志/经验钳制与倍数曲线等只读项） | `fish_selftest` |

English: Fifteen SMAPI console commands cover level/stats editing, inspection, data clearing, crown toggles, giant-fish simulation, assist testing/statistics, perseverance-reward forcing, and a read-only self-test (including the BATCH-068 experience-clamp and multiplier-curve assertions). Data/query commands accept an optional leading player index (1=host, 2=first farmhand, etc., from `Game1.getAllFarmers()`; omitted = current player), e.g. `fish_addstars 2 20` and `fish_bonus 2` (BATCH-045).

---

## 13. 资深玩家速查 / Quick Reference

| 目标 | 条件 / 数值 |
|---|---|
| 巨物展示（放大 + NPC 反应） | 难度等级 ≥8（BATCH-060）；举起并 5 格内有 NPC；钓后未进 FarmHouse |
| 图鉴皇冠 | 成功钓起且本次调整后难度 ≥120（原版 5 传奇一次钓获直接给） |
| 鱼竿熟练度 α | 可计数皇冠分段线性：0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100% |
| 流光溢彩皇冠 | 难度等级 ≥95 + 挑战鱼饵生效时成功（不区分是否超时）；挑战开始时 100 级鱼 ×1.2；皇冠位于详情 UI/鼠标之下 |
| 皇冠助战 | 概率 10%×(皇冠/61)，临时 +0~40 钓鱼等级（仅绿条高度）；助战文案显示 15 秒（BATCH-060） |
| 铱星品质起点 | 门槛式：10 级→银、25 级→金、**50 级→铱**（最终品质 = max(原品质, 门槛)，BATCH-060） |
| 鱼跳 / 史诗失败提示 | 调整后难度 ≥150（加速度档位锚点曲线，90 级起满档） |
| 运动公式修正 / 挑战鱼饵 / 力竭 | 调整后难度 >100（力竭 ≥100） |
| 挑战鱼饵数量 | 5 分钟内成功：原生 ×1.5（ceil）再乘等级倍数；超时按掉星 −20%/颗（0.8/0.6/0.4，≥95 豁免，掉星有左下角提示） |
| 万能鱼饵 | 等级 >0 且原生两条：2 → 12 条，再乘等级倍数 |
| 星之果茶 | 难度等级 ≥50，概率 等级/400（每次成功独立判定） |
| 持久战奖励 | 失败 ≥60 秒 → 海泡布丁；30~60 秒 → 50% +3 料理；30 秒巅峰提示 |
| 蓄力槽保护 | ≤20% → ×0.80，≤1% → ×0.50（全局，非鱼王） |
| 鱼王豁免 | 五大传奇鱼：全部系统豁免，一次钓获直接给皇冠 |
| 非鱼类上限 | 等级 8（数量 8 倍封顶，BATCH-062；品质不提升） |
| 一次成功最大收益 | (完美 +10 + round(调整后难度/50)) × 固定钓鱼等级系数 max(等级,1)×0.1，保底 +1，按下一称号上限封顶（BATCH-067） |
| 经验倍数 | 1~10 级 max(1, round(level×0.5))（10 级 = ×5）；10~100 级线性 ×5→×20（50 级 ×12、100 级 ×20；BATCH-068） |
| 经验难度钳制 | 经验公式难度输入 ∈ [原生难度, 120]：负数等级/力竭按原生难度重算（不低于未装模组），超 120 按 120（BATCH-068） |
| 蟹笼 | 每次收获 +1 等级（固定，不吃系数）；经验原生 5 点（BATCH-065） |
| 节日钓鱼 | `EnableFestivalFishingMods` 默认 false = 三个钓鱼节日完全原生；true = 模组规则 + 节日分数按数量倍数翻倍（BATCH-066） |
| 89 级增长上限 | 88 及以下最多升到 89（超高难入口）；89 级以上每次成功最多 +6 级（100 封顶，BATCH-060 由 +3 上调） |
| 连续失败逃跑减速 | 调整后难度>100（挑战鱼饵同样参与，95 级以上也吃）：连续失败 0→5 次，低条区逃跑减速线性增强到 -10 级等效（≤20% ×0.60、≤1% ×0.20）；成功清零；鱼王豁免 |
| 小游戏浮动提示 | 动作提示=左缘距绿条右缘 24px（左对齐，锚点 x+124）；其他提示=右缘距绿条左缘 50px（右对齐，锚点 x+14，距绿条绝对像素写死）；绿条靠屏幕边缘时自动翻到另一侧（BATCH-058O）；字体=星露谷 dialogueFont + 右下黑阴影 + 非常淡的蓝/红描边（向白色混合 85%）；长文本按最大 420px 折行、每段最多 3 行，超长内容拆成续集提示排队显示（BATCH-058C/058G/058H/058K/058L/058M/058N/058O） |
| 日志开关 | `config.json` `EnableLogging`（默认 true）或 GMCM 菜单 |
| 清除挑战进度 | GMCM“重置挑战数据”章节（开关待命 + 确认框两步）或 `fish_clear confirm`；回到刚安装状态 |
| 传奇皇冠回填 | 每次加载存档按图鉴记录自动补发 5 条原版传奇皇冠（装模组前已钓到也补；重置后立即补回） |
| 存档位置 | `Farmer.modData["FishingExpanded/FishDifficultyData"]`（每玩家） |

### 策略要点（面向想冲 100 级/全皇冠的玩家）

- **完美钓获性价比最高**：0 脱杆 +10，还附带 `round(难度/50)` 额外收益；但最终增长受固定钓鱼等级系数缩放——满级（10 级）才是全速，低等级钓鱼时增长较慢，想快速冲级先升玩家钓鱼等级。
- **冲级节奏**：越级限制意味着每个称号区间都要多次成功；先冲高基础难度的鱼（如狗鱼 60、海参类）能更快到达 120 皇冠阈值——`等级 ≈ (120/原版难度 − 1)` 对应的分段倍数可查 2.4 锚点（原版难度 60 只需约 8 级即达标）。
- **皇冠 → 手感**：皇冠不再加隐藏钓鱼等级，而是提高鱼竿熟练度 α（0→100%），高 α 下绿条按下即定速、无惯性、撞边钳制，操作更跟手；满 61 可计数皇冠后小游戏体验最顺。
- **全皇冠顺序**：56 条原生普通真鱼 + 5 条传奇鱼；Mod 鱼皇冠只显示不计 α，别先刷 Mod 鱼。
- **挑战鱼饵的正确用法**：只对调整后难度 >100 的鱼生效，且目标是把该鱼难度等级刷到 ≥95——成功后皇冠升级流光溢彩；超时只损失数量，皇冠照拿，可以慢慢磨。
- **力竭是双刃剑**：高难度鱼拖过 15 分钟会降到 80 难度（更易钓），但奖励按开局快照结算，不掉收益；持久战失败还有海泡布丁/料理保底。
- **星之果茶**是等级 50 后的稳定副产：100 级鱼每 4 次成功期望 1 瓶。
- **巨型鱼展示**是限时“战利品”：进家门即失效，想给村民看鱼就钓到后立刻举着逛。
- 触底（-10）后再失败会得到完整升级建议，提示你该换鱼竿/鱼饵/料理。

- **经验流向**：钓鱼等级 10 级封顶后经验全部流入原生精通系统；高难度等级经验倍率可观（100 级 ×20 + 基数钳制 120），刷精通点很快，但经验基数不会因调整后难度无限膨胀（BATCH-068）。

> 注：本 Wiki 为静态文档，仅在收到明确指令时更新（2026-08-16 更新至 BATCH-068，同步 BATCH-062 数量倍数 = 等级×1）。
