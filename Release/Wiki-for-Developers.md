# Fishing Expanded — 玩家 Wiki（资深玩家版）

> 面向资深玩家的完整机制文档。内容基于 `GAME-DESIGN.md` 与当前源码（manifest 版本 `1.0.0`，2026-08-22 对照源码全仓复核同步：BATCH-069~079——100 级流动皇冠及图鉴 ×1.2/困难模式 ×1.3 彩蛋（BATCH-048/074）、全随机模式 `EnableRandomFishBehavior` 配置表（BATCH-058R/S）、自定义称号 `CustomFishingTitle` 仅手动编辑（BATCH-073）、日志默认关闭（2026-08-22）、控制台命令补全至 18 个、跳鱼前摇旋转曲线 ±70°（BATCH-058T）、祖龙王尊敬词 5 种、存档字段含背板种子；数量曲线 BATCH-082 新锚点+概率过渡、经验倍数 BATCH-082 锚点×1→×8 向下取整、收益缩放三滑杆改名额外渔获/额外经验（BATCH-076/081）、万能+10%/挑战+20% 增产改版（BATCH-082）、无小游戏物品专属曲线与 5% 升级掷签 + 训练鱼竿声誉封顶 4（BATCH-078）、Walk of Life 同装兼容层（BATCH-079））。公式与阈值以代码实际行为为准；与设计文档不一致处已用「实现说明」标注。
>
> English readers: the complete standalone English version is **Part 2** in the second half of this document.

---

# 第一部分 · 中文版

---

## 1. 概览

- **Mod 名称**：Fishing Expanded（UniqueID：`neoiw.FishingExpanded`，见 `manifest.json`）。
- **依赖**：SMAPI 4.0.0+，Stardew Valley 1.6+；可选 Generic Mod Config Menu（提供日志/全随机模式/节日钓鱼三个开关与“重置挑战数据”两步确认章节，未安装不影响——config.json 始终生效，全部键见 §11）。
- **设置与日志**：首次运行自动生成 `config.json`（`EnableLogging` 默认 `false`——2026-08-22 起新安装无调试输出；分级门控：关闭时仅输出警告/错误，Trace/Debug/Info 受开关控制，Warn/Error/Alert 始终可见；BATCH-040 门控机制不变）。安装 GMCM 后可在游戏内菜单开启调试日志（排障用）。GMCM 菜单另提供“重置挑战数据”章节（警告说明 + 开关待命，关闭菜单后弹确认框二次确认，回到刚安装状态；BATCH-042）。
- **核心内容**：每种鱼独立的“声誉”、动态难度/数量/经验/品质/尺寸调整、蓄力槽保护、高难度运动强化与鱼跳、图鉴皇冠与鱼竿熟练度、挑战鱼饵、力竭机制、持久战奖励、皇冠助战、双通道浮动提示、超大鱼展示、NPC 反应、星之果茶掉落。
- **存档**：数据按“玩家 + 存档”独立保存（写在该玩家 `Farmer.modData`，键 `FishingExpanded/FishDifficultyData`，含收藏星标 `CollectionStars`、挑战皇冠 `ChallengeCrowns`、100 级流动皇冠 `Level100FlowCrowns`（BATCH-048）、背板种子字典 `ChallengePatternSeeds`（键 `鱼ID|声誉`，成功钓起后删除，BATCH-058）；旧存档缺失字段默认空）；换存档、联机玩家互不共享。

---

## 1.5 术语表（BATCH-060）

| 术语 | 定义 |
|---|---|
| 声誉 | 每鱼独立统计的成长值（即代码与旧文档中的“难度等级”，内部字段 `FishStats.DifficultyLevel`；范围 [-10, 100]，非鱼类 [-10, 8]），成功增减/失败 −1；驱动全部倍率与称号（§2） |
| 调整后难度 | 小游戏实际难度 = 原生 difficulty × 声誉倍数；星标（≥120）、跳鱼（≥150）、力竭/挑战鱼饵（≥100/>100）等判定均基于它（§4） |
| 有效难度 | 战斗中实时难度 = 调整后难度经力竭衰减后的值（15 分钟降到 80；挑战鱼饵下恒等于开局值）（§9.3） |
| 难度档位 | 按有效难度划分的机制档位（跳鱼间隔 150/250/350/450/550 分档等）（§4.2） |
| 数量倍数 | 锚点曲线 + 余量概率进位（BATCH-082，详见 §2.4 与 §9.2）；锚点 -10/0=1、8=2、16=3、32=4、56=7、100=25 | 100 → 25；78 → 恒 16；4 → 期望 1.5 |
| 经验倍数 | 锚点曲线 + 线性插值向下取整（BATCH-082，详见 §2.4 与 §9.2）；节点 0=×1、10=×2、30=×3、50=×4、75=×5、100=×8 封顶 | 50 → ×4；85 → ×6；100 → ×8 |
| 固定钓鱼等级系数 | 声誉增长系数 = max(固定钓鱼等级, 1)×0.1——固定钓鱼等级 = 玩家基础钓鱼等级字段（0~10，不含食物/饮料 buff 与助战临时等级）；1 级 ×0.1、10 级 ×1.0（§2.2，BATCH-067） |
| 经验难度钳制 | 经验公式的难度输入钳制在 [原生难度, 120]：低于原生难度（负数等级/力竭）按原生难度重算，高于 120 按 120 重算（§2.4，BATCH-068） |
| 可计数皇冠 | 56 条原生普通真鱼 + 5 条原版传奇鱼（共 61）的皇冠；Mod 鱼/扩展传奇皇冠只显示、不计入 α 与助战（§7.1） |
| 鱼竿熟练度 α | 可计数皇冠分段线性（0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100%），驱动小游戏手感（§7.2） |
| 挑战星 | 挑战鱼饵的原生 3 星计数；5:00 起每分钟掉 1 颗（≥95 豁免），每颗 −20% 鱼获（§8） |
| 背板种子 | 挑战鱼饵下同鱼同等级行为的固定随机种子，成功钓起后清除；全随机模式不生成（§9.5） |

---

## 2. 声誉系统

> 本部分公式与表格中的“等级”均指鱼的**声誉**（即玩家的鱼群声誉；内部字段 `FishStats.DifficultyLevel`，代码日志沿用旧名“难度等级”）。

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

### 2.3 称号表

| 声誉 | 称号 | 下一区间上限（本次增长上限） |
|---|---|---:|
| <1 | 额……稍微强点的家伙 | 3 |
| 1–3 | 精英 | 6 |
| 4–6 | 骑士 | 8 |
| 7–8 | 男爵 | 15 |
| 9–15 | 伯爵 | 22 |
| 16–22 | 公爵 | 33 |
| 23–33 | 亲王 | 45 |
| 34–45 | 帝王 | 66 |
| 46–66 | 神皇 | 88 |
| 67–88 | 众神王 | 99 |
| 89–99 | 祖龙王 | 100 |
| 100 | 太一 | 100 |

示例：等级 1（精英）+ 完美 (+10) 最多升到 6（骑士上限）；等级 7（男爵）一次最多到 15（伯爵上限）。

### 2.4 等级效果公式

| 效果 | 公式 | 端点值 |
|---|---|---|
| difficulty 倍数 | 正数分段线性：0→×1.0、4→×1.2、20→×5、50→×20、100→×50（斜率 0.05 / 0.2375 / 0.5 / 0.6 逐段递增）；负数：`0.5 + 0.5×(level+10)/10` | -10 → 0.5；0 → 1；100 → 50 |
| 获得数量倍数 | 锚点曲线（BATCH-082）：`(-10,1) (0,1) (8,2) (16,3) (32,4) (56,7) (100,25)` 线性期望插值，区间外钳端点；**仅小数余量按概率进位**（f=进位概率，每次结算掷一次 `Game1.random`），整数期望恒定不随机（例：4 级期望 1.5 → 约 50% 给 2 条；12 级期望 2.5 → 约 50% 给 3 条；40 级恒 5；78 级恒 16） | 8 → 期望 2；56 → 7；100 → 25 |
| 经验倍数 | 锚点曲线（BATCH-082）：`(0,1) (10,2) (30,3) (50,4) (75,5) (100,8)` 线性插值后**向下取整**（尾段 75→100 增速加快为有意设计）；≤0 钳 1 | 10 → 2；50 → 4；85 → 6；100 → 8 封顶 |
| 经验难度输入 | 钳制在 `[原生难度, 120]`（BATCH-068）：低于原生难度（负数等级/力竭）→ 按原生难度重算（不低于未装模组）；高于 120 → 按 120 重算（基数不爆炸）；区间内不变；重算复刻原生公式 `max(1, (品质+1)×3 + 难度/3)` + 宝箱 +120%/完美 +140%/Boss ×5，随后仍乘经验倍数 | 上限 120（= 原生最高难度 110 以上一点） |
| 品质提升 | 门槛式“提升到”：`level ≥ 50 → 铱(4)；≥ 25 → 金(2)；≥ 10 → 银(1)；否则不提升`；最终品质 = max(原品质, 门槛)（BATCH-060） | 10 → 银；25 → 金；50 → 铱 |
| 尺寸数值（fishSize） | 正数：`1 + 0.1×level`；负数：`max(0.1, 1 − 0.05×|level|)` | 100 → 11×；-10 → 0.5× |
| 视觉缩放 | `1 + level × 0.0270843`（线性，BATCH-060 与数量倍数解耦、按声誉） | 0 级 = 1.0；100 级 ≈ 3.71 |

- 品质映射：0=普通，1=银，2=金，4=铱（跳过 3）。
- **实现说明**：BATCH-060 起品质为“提升到”式门槛（10/25/50 → 银/金/铱），不再累加；50 级必铱。
- 尺寸数字在结算边界统一应用等级倍率（`pullFishFromWater`/`caughtFish`）；声誉 >0 时脱杆不再触发原生 800ms 缩水、也不降品质（BATCH-038）。
- 负数等级时数量/经验保持 1×，品质不提升。

---

## 3. 蓄力槽保护

鱼逃跑时蓄力槽下降速度被调低（倍率越小越慢、越容易救回）：

- **全局保护**（所有非鱼王声誉，含 0 与正数）：蓄力槽 ≤20% → ×0.80；≤1% → ×0.50。
- **负数难度额外保护**（-10 ~ 0 线性）：≤40% → 0.99~1.0；≤20% → 0.60~1.0；≤1% → 0.20~1.0。
- **叠加规则**：与原生/其他 Mod 的倍率取**最小值**（效果最强）。
- 鱼王完整豁免：小游戏构造时直接退出自定义路径，不应用任何保护。

---

## 4. 高难度运动机制（调整后难度 > 100）

仅非鱼王；`调整后难度 = 原版 difficulty × 难度倍数`。难度 ≤100 时原生运动公式完全不变。

### 4.1 公式修正

- **初始目标**：难度 >100 时固定为顶部（原生公式在 >100 会算出负坐标而失效）。
- **换目标概率封顶 150**：大范围 `min(难度,150)/4000`；小偏移（±50~100）`min(难度,150)/2000`；dart 型额外 `min(难度,150)/1000`，dart 偏移 `±(50~100+min(难度,150)×2)`。
- **加速度**：难度 >100 时在原生 `bobberAcceleration` 写入前乘线性增幅 `1 + 档位×(难度−100)/100`（Transpiler 在 `stfld` 前注入乘法，BATCH-029/034/053；不改原生随机分母）；档位锚点曲线：0 级 10%、50 级 20%、70 级 40%、80 级 70%、90 级 100%、100 级 100%，点间线性，≤0 钳 10%、≥90 钳 100%（难度 190：80 级 → ×1.63、≥90 级 → ×1.9）；难度 ≤100 时增幅 = 1（原生）。

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

---

## 5. HUD 提示系统

本 Mod 左下角消息走 **FIFO 队列**：同一时间只显示一条，前一条淡出后显示下一条；每玩家队列上限 5 条（满时丢最旧）；返回标题/换存档清空。覆盖：成功、失败、等级建议、皇冠挑战宣言、星之果茶、持久战奖励消息。**小游戏内的浮动提示（动作/其他）不走队列**，在钓鱼条两侧显示（见 5.6）。另：GameLaunched 时向 VanillaTips（`neoiw.vanillatips`）注入 7 条钓鱼提示（来源权重 11；未安装该 Mod 则跳过——BATCH-060 第 9 项）。

### 5.1 成功提示

- 普通鱼未封顶：`下一次你将向{鱼名}中的{称号}发起挑战`（0 级/负数胜利也显示，用“额……稍微强点的家伙”）。
- 鱼达到 100 级（含封顶后再成功）：`你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。`
- 非鱼类达到 8 级：`对于{物品名}而言，你已是帝王`。
- 鱼王：随机 10 条独特文案（如“嗷！孤傲的王！”）。

### 5.2 失败提示（优先级从高到低）

1. **史诗提示**：同鱼种连续失败 ≥2 次 **且** 本次调整后难度 ≥150 → 未满皇冠时：`收集更多徽章（鱼类收集品页的皇冠），获得更多加成，再挑战史诗级强者吧`；**满皇冠**（鱼竿熟练度 α≥1）时替换为：`你需要进入人鱼合一的状态，你必须是鱼，才能赢下这场挑战。`（BATCH-038）
2. **触底**：等级 -10 → `最平庸的{鱼名}（10）依然太难了……`（含钓鱼等级/料理/鱼饵建议）。
3. **负等级**：`你对{鱼名}的了解加深了（{绝对值}）`。
4. **普通**：`还是挑战{鱼名}中更平庸的个体吧`。

### 5.3 等级建议（小游戏出现时）

- 当 `声誉/10 > 当前钓鱼等级`：显示 `建议达到{X}以上钓鱼等级再挑战……`（鱼王豁免）。
- 当 `声誉/10 > 当前钓鱼等级×2`：开头追加 `这是不可能的高难度挑战！`。

### 5.4 皇冠挑战宣言

- 进入小游戏时若该鱼已有图鉴皇冠**且声誉 ≥1**（0 和负数不显示，BATCH-033）：显示 `{尊敬词}{鱼名}{等级称号}{毅然决然词}{应战说法}。`（尊敬词每称号 3 种、**祖龙王 5 种**；毅然决然词 50 种、应战说法 20 种，三组独立随机）。

### 5.5 星之果茶掉落（等级 ≥ 50 的普通鱼）

- 概率 = `声誉/4`%（即 等级/400）：50 级 → 12.5%，80 级 → 20%，100 级 → 25%。
- 掉落 1 瓶 `(O)StardropTea`，走成功结算唯一边界（不重复发放）；背包满时走原生溢出菜单。
- 附带 15 条随机幽默消息之一。非鱼类上限 8 天然不参与；鱼王豁免；仅本地玩家判定。

### 5.6 小游戏浮动提示（双通道，BATCH-038/039/041）

- **鱼动作提示**（跳鱼“鱼跃/甩尾”类）：显示在**绿条右侧紧贴处**（左对齐；绿条原生几何=左缘 `xPositionOnScreen+64`、宽 36px，即锚点 `xPositionOnScreen+124`，文字左缘距绿条右缘固定 24px），起点固定在触发瞬间（鱼落地位置上方 30px）。
- **鱼其他提示**（助战/力竭节点/30 秒巅峰提示等）：显示在**绿条左侧紧贴处**（右对齐；锚点 `xPositionOnScreen+14`，文字右缘距绿条左缘固定 50px，绿条中部高度）。
- **屏幕边缘侧翻**：当绿条靠近屏幕边缘、原定一侧放不下文字时，提示自动翻到绿条另一侧（动作提示翻到左侧=右缘距绿条左缘 50px；其他提示翻到右侧=左缘距绿条右缘 24px），避免被屏幕钳制推到最右缘；保留 8px 屏幕边距兜底。
- 两通道：起点固定、默认显示 5 秒、5 秒内慢慢上移 30px 并线性淡出、可同时显示多条、每通道上限 8 条（超限丢最旧）；**助战文案例外 15 秒（BATCH-060）**；纯显示，不写任何游戏状态。

---

## 6. 超大鱼与 NPC 反应

### 6.1 触发条件（三个同时满足）

1. 鱼被选中且**举起**（仅切换到物品栏不算；手柄同理）；
2. 该次钓获鱼的**声誉 ≥ 8**（BATCH-060：巨型鱼与数量倍数解耦、直接按声誉判定；原“数量倍数 >15”等价于等级 ≥8）；
3. 自钓到该鱼后**尚未进入过 FarmHouse 室内**。

### 6.2 效果

- **视觉缩放**：手持鱼、从水里飞向玩家的飞行动画、举起结算的真鱼都会放大（底边/中心锚点保持原生位置）；小游戏内的鱼标与结算面板示意图保持原生大小。
- **NPC 冒泡**：玩家 5 格（320 像素）内 NPC 头顶显示随机赞美文案——通用池（100 条，含鱼名+尺寸）或该 NPC 专属池（51 组角色各 10 条、按背景与身份定制；触发时 60% 专属 / 40% 通用，未覆盖 NPC 恒用通用池，BATCH-075）；尺寸口径英文=原生英寸+in.、其他语言=厘米（`fishSize×2.54` 取整，BATCH-033/073）；每个 NPC 在同一超大鱼会话内每种鱼最多 1 次（进入 FarmHouse 或换日后重置，BATCH-074）。动物 NPC（宠物 Pet 按 petType=狗/猫、马 Horse）先叫一声再把赞美放括号里，如“汪汪！！！（这条狗鱼竟然有388cm简直是奇迹）”；每种动物 5 套叫声随机（BATCH-058）。
- **主动对话替换**：与 NPC 对话首次替换为赞美（同文案池，同样适用专属池 60/40 分流），第二次恢复原对话；每 NPC 在同一超大鱼会话内每种鱼最多 1 次（BATCH-074）。
- **会话重置**：进入 FarmHouse 或换日（凌晨）时，超大鱼展示与 NPC 冒泡/对话触发记录一起归零；下次钓到新的超大鱼重新计算（BATCH-074）。
- 鱼王与垃圾/藻类等非鱼类不产生超大鱼展示（当前实现仅 Category = -4 的真鱼登记）。

---

## 7. 图鉴、皇冠与鱼竿熟练度

### 7.1 皇冠达成条件

- **成功钓起**该鱼，且本次小游戏的**调整后难度 ≥ 120**（例如原版 difficulty 3 × 50 倍 = 150）；仅进入小游戏或失败不获得；鱼王豁免。
- 原版 5 条传奇鱼（159/160/163/682/775）：**钓到一次直接给皇冠**（无难度门槛，BATCH-034）。
- 扩展传奇 898–902 属于可计数 61 池：按普通鱼规则（调整后难度 ≥120）给皇冠；**其他 Mod 鱼与不可触发小游戏的条目达到条件后给星星（早期 mouseCursors 金星画法），不计入鱼竿熟练度 α**（2026-08-15 用户指令）。

### 7.2 鱼竿熟练度 α（皇冠的永久作用，BATCH-034/039）

- α = 可计数皇冠数分段线性曲线：0→0%、5→2%、10→7%、20→20%、30→35%、61→100%；点间线性，超过 61 钳制 100%。
- 可计数皇冠 = 56 条原生普通真鱼 + 5 条原版传奇鱼（共 61）；Mod 鱼/扩展传奇不计。
- α 影响钓鱼手感：α=0 完全原生；α=1 终点手感——按下/松开瞬间直接定速、无加速度/条内阻尼/惯性、撞边完全钳制（不反弹、速度归零）；定速最大值 `30−5α`（α=1 → 25px/帧）；叠加方向系数：绿条中间朝鱼移动 ×(1+0.2α)、相背离 ×(1−0.2α)（α=1 → ×1.2/×0.8，BATCH-056 回调）。
- 隐藏钓鱼等级加成已整体删除（原每皇冠 +0.2 退休，BATCH-034）；帧率解耦覆盖整个小游戏（60fps 行为与原生一致）。

### 7.3 挑战皇冠（流光溢彩，BATCH-038/048）

- 声誉 **≥95** 的鱼在**挑战鱼饵**生效时成功钓起（无论是否超过 5 分钟时限，超时只按掉星扣数量）→ 该鱼皇冠升级为**流动效果的金色皇冠**（图鉴页金/白呼吸 + 1±0.05 缩放脉冲）。
- **100 级流动皇冠（BATCH-048）**：挑战开始时声誉 ≥100 的鱼再用挑战鱼饵成功，额外记入 `Level100FlowCrowns`——图鉴中基础尺寸 ×1.2；**困难模式（全随机无背板，`EnableRandomFishBehavior=true`）下 ×1.3（BATCH-074 彩蛋，仅图鉴绘制，助战权重与存档判定不变）**。
- 判定取挑战开始时的声誉快照，在成功结算唯一边界记录一次；鱼王豁免。
- 独立存档字段 `ChallengeCrowns` / `Level100FlowCrowns`，旧存档缺失默认空；测试命令 `fish_challengecrown [玩家序号] <鱼ID> [0|1]`。

### 7.4 皇冠助战（BATCH-035）

- 进入非鱼王小游戏时判定一次：总概率 = `10% × (可计数皇冠数/61)` 线性（无皇冠=0%、满皇冠=10%）**+ 每条可计数流动金冠鱼 +0.1% + 每条可计数增大流动金冠鱼再 +0.05%**（加成不设上限、由可计数 61 条自然封顶——BATCH-073）。
- 命中后从玩家可计数皇冠鱼中按权重选一条：普通皇冠鱼权重 1，流动金冠鱼 +0.1%，增大流动金冠鱼再 +0.05%（BATCH-073 加权选鱼；近似均匀，金冠鱼略更容易出场）。
- 临时钓鱼等级 = 0~40，随被选中鱼的难度排位线性倾斜（最高难度鱼更容易给高等级助战；平均 26.55 / 13.45）。
- 效果：本次小游戏绿条高度 +`临时等级×8px`；**不写玩家钓鱼等级、不入存档**，不影响经验/数量/品质/难度。
- 显示 20 条随机助战文案（“荣耀的【鱼种】【职阶】前来护驾！”等，进“鱼其他提示”通道）；**助战文案显示 15 秒**（15 秒内上移 30px 线性淡出，BATCH-060，仅助战延长，其余提示 5 秒）；挑战鱼饵生效时不触发助战；鱼王豁免。

### 7.5 图鉴显示

- 鱼类页：可计数 61 原生鱼图标左上角绘制金色皇冠（Infinity Crown 贴图）；有挑战皇冠的鱼以流动金色呼吸绘制（其中 100 级流动皇冠基础尺寸 ×1.2、困难模式 ×1.3——BATCH-048/074，仅图鉴显示）；非鱼类与其他 Mod 鱼绘制早期金星（`Game1.mouseCursors (346,392,8,8)`，20×20 `Color.Gold`）。
- 描述新增行：`挑战等级：{称号}（{X}级）`（等级 ≤0 不显示；鱼王豁免挑战等级行）。
- 有皇冠的可计数鱼追加显示：`鱼竿手感增强`（i18n `collections.crownControl`；BATCH-058T 文案，机制名“鱼竿熟练度 α”保留）；星星条目显示 `无手感增强`（i18n `collections.noCrownControl`）；纯展示，不新增状态。

---

## 8. 物品处理与鱼饵

- 动画阶段始终显示原生数量的鱼（本 Mod 不伪造动画条数）。
- 数量倍数在原生 `CreateFish` 创建最终物品后一次性应用：`最终堆叠 = 原生堆叠 × 等级数量倍数`；BATCH-077 起等级数量倍数为"锚点期望 + 余量概率进位"，**每次结算掷一次**（同一次渔获内所有消费点共用同一个掷签值）；背包与溢出菜单（`ItemGrabMenu`）共用同一个物品，不建立第二套溢出逻辑，不因倍数丢失鱼获。
- **万能鱼饵加成**（BATCH-038/082）：声誉 >0 且原生本应给两条鱼（`numCaught ≥ 2`，非挑战鱼饵）时，改为**增产 10%、保底 +1 条**——最终 = max(⌈N×1.1⌉, N+1)，N=正常渔获（1 条 × 等级数量倍数）；原生第 2 条在模组区间内不再发放。声誉 0 时不吃加成、保留原生双倍。
- **挑战鱼饵改版**（调整后难度 >100 时生效，BATCH-038/082）：
  - **5 分钟（300 秒）内成功** → **增产 20%、保底 +2 条**：最终 = max(⌈N×1.2⌉, N+2)（原生 `challengeBaitFishes=3` 多发的 2 条在模组区间内不再发放）；调整后难度 ≤100 为原生行为区间（3 条照旧）；
  - **超时（BATCH-082 一致化）**：数量加成取消且原生多发同样被替代——按正常渔获 N 结算（若保留原生 3 条会"故意拖超时反拿 3 倍"）；
  - **超时掉星（BATCH-058）**：5:00 起每分钟掉 1 颗原生挑战星（5:00→2、6:00→1、7:00→0，不可恢复；声誉 ≥95 豁免），每掉 1 颗最终鱼获 −20%（0.8/0.6/0.4，作用于最终数量，四舍五入并兜底 ≥1 条——BATCH-067 口径）；等级收益、普通皇冠、流动金色皇冠、品质、尺寸全部照常（例：30 分钟钓到 98 级鱼仍给流动金皇冠）；**掉星瞬间左下角 FIFO 提示剩余星数与鱼获减少百分比（BATCH-060，小游戏期间可见）**；
  - **难度不衰减**（力竭衰减不适用，有效难度恒等于开局值）；
  - **无其他鱼助战**；**原生“3 次脱杆失败”规则禁用**（可继续挑战到成功）。
- 堆叠按原版规则（同种同品质可堆叠，上限 999）。

---

## 9. 特殊鱼类与持久战

### 9.1 鱼王（五大传奇鱼，完整豁免）

传奇鱼 (163)、突变鲤鱼 (682)、鮟鱇鱼 (160)、冰川鱼 (775)、赤红鱼 (159)。

- 不记录等级、不应用难度/数量/品质/经验倍数、无挑战称号、无视觉缩放、无 NPC 反应、无蓄力槽保护、无等级建议、无助战、无力竭。
- **钓到一次直接给皇冠**并计入鱼竿熟练度 α（BATCH-034）。
- 鱼王 ID 按原生物品 ID 归一化（`163`/`(O)163`/`(o)163` 视为同一只）；扩展传奇 898–902 按普通鱼规则。
- 钓到后随机显示 10 条独特文案之一。

### 9.2 非鱼类（垃圾/藻类等，Category ≠ -4）

- 等级范围 [-10, 8]；达到 8 级后成功不再增加等级（底层统计保持 8）。
- **升级改概率制（BATCH-078）**：每次收获固定请求 +1 级，但仅 **5%** 概率真正授予（`NonFishLevelUpChance`）；未中仍记一次成功事件（连续失败清零、星星检查照常；`SuccessCount` 沿用"累计增长量"口径不加），并有 **20%** 概率弹出一条通用轻提示（i18n `hud.trashMiss.{1..20}`）。钓竿直取与蟹笼两条路径同规则。
- **数量走专属锚点曲线（BATCH-078）**：`(0,1) (8,8)` 线性期望插值 + 同款余量概率进位（负级钳 1；样例：4 级期望 4.5 → 45% 概率 5 个）；另有独立额外渔获滑杆 `NoMinigameQuantityIncomePercent`（见 §设置）。
- 品质按等级 8 计算（达不到 10 级门槛，不提升）。
- 8 级封顶后再成功显示 `对于{物品名}而言，你已是帝王`。
- **实现说明**：设计文档规定非鱼类封顶后仍参与视觉缩放/NPC/星标，但当前代码的巨型鱼登记限定 Category = -4 的真鱼，非鱼类不产生超大鱼展示；其原始难度极低，实际上也几乎不可能达到皇冠阈值。

### 9.2.1 训练鱼竿（BATCH-078）

- 判定口径与原生一致：当前工具 `QualifiedItemId == "(T)TrainingRod"`。
- 仅作用于**真鱼**（非鱼类不受影响）：本次捕获的有效声誉钳制到 **4**（`TrainingRodLevelCap`），在结算源头生效——数量/经验/尺寸/皇冠/巨型鱼/星之果茶全部下游自动 ≤4。
- 真实声誉已 >4 时：本次成功**完全不写档**（连 RecordSuccess 都不调）；若不加限制将升过 4 时，以 `hud.trainingCap.{1..30}` 随机文案替代当次升级建议行。存档中的真实声誉永不改写。

### 9.2.2 收益缩放（BATCH-076，GMCM 三滑杆）

| 滑杆 | config 字段 | 范围 | 应用点 |
|---|---|---|---|
| 额外渔获（BATCH-081 前显示名"条数收益"） | `QuantityIncomePercent` | [10,300] 默认 100 | 万能/挑战/等级倍数/掉星折扣**全部完成后**整体缩放最终条数（向上取整、≥1 条）；蟹笼在倍数后、书×2 前 |
| 额外经验（BATCH-081 前显示名"经验收益"） | `ExperienceIncomePercent` | [10,300] 默认 100 | 基数重算+倍率之后整体缩放（向上取整）；限额清零优先 |
| 无小游戏物品额外渔获（BATCH-081 前显示名"无小游戏物品条数收益"） | `NoMinigameQuantityIncomePercent` | [10,300] 默认 100 | 专属曲线结果之后 |

节日原生模式三者均不生效；GMCM tooltip 动态显示实际节点阵列（`FormatQuantityAnchors` / BATCH-081 新增 `FormatExperienceAnchors`，倍数实时取 `GetExperienceMultiplier`）；三条收益滑杆的 tooltip 由 `ModEntry.WrapTooltip` 按 GMCM 同款字体(`Game1.dialogueFont`)/宽度(800px)自行折行——GMCM 对含 `\n` 的文本不做自动折行（反编译 `SpecificModConfigMenu.draw`）。

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
- **跳鱼前摇**：鱼跃/甩尾在原有 0.5 秒延迟后追加 0.88 秒前摇（鱼图标 0.77 秒线性转到 **±70°**、0.11 秒快速转回——BATCH-058T 曲线，`fish_selftest` 自检 0.385s=35°、0.77s=70°），随后瞬移；上跳逆时针（负角）、下跳顺时针（正角），前摇期间鱼图标发红光呼吸脉动。鱼行动提示在跳鱼判定、0.5 秒延迟开始时即显示（比前摇再早 0.5 秒）。
- **挑战鱼饵背板**：同一种鱼、同一声誉在挑战鱼饵下的行为（目标序列、dart、跳鱼时机）由固定种子决定，钓起前每次一致；失败不掉等级但连续失败计数照常；成功钓起后种子清除，下次重新随机。

### 9.6 高难鱼招式短语（BATCH-059）

- 调整后难度 ≥150 的非鱼王鱼：一次瞬移后鱼第一次到达中线（266±10，只判到达）为短语起点，下一次瞬移后再到中线为终点；这一段（含那次瞬移）反复循环播放，跳鱼前摇与行动提示一并重放。
- 蓄力进度每净涨 1/3（绝对值）换一段新短语；玩家可以背下短段落来应对，而不是面对 20 分钟随机运动。
- 非挑战鱼饵下，力竭节点到达时打断录播（短语立即退出，鱼按已降难度的原生运动；仍满足条件则下次跳鱼按新难度重录）；挑战鱼饵难度不衰减、不打断（BATCH-059A）。

---

## 10. 多人 / 联机 / 双人同屏

- 在线联机：主机与每位农场客**都必须安装**本 Mod；每位玩家只读写自己的 `modData`，数据随各自存档。
- 本地分屏：每位本地玩家独立实例，难度/星标/加成/展示/提示队列全部按 `UniqueMultiplayerID` 隔离。
- 掉落（星之果茶/持久战奖励）、结算、展示只对本地玩家生效。

---

## 11. 配置项与节日钓鱼开关（BATCH-066）

全部配置键一览（未装 GMCM 时手动编辑 `config.json`；下列键在 GMCM 均有开关，`CustomFishingTitle` 仅手动编辑）：

| 键 | 默认 | 说明 |
|---|---|---|
| `EnableLogging` | false | 调试/信息日志总开关（2026-08-22 起）；Warn/Error/Alert 始终输出 |
| `EnableRandomFishBehavior` | false | 鱼的行为全随机模式（BATCH-058R/S）：true = 每局行为完全随机、无背板种子、更难；100 级流动皇冠图鉴 ×1.3（BATCH-074） |
| `EnableFestivalFishingMods` | false | 节日钓鱼应用模组规则（BATCH-066，见下表） |
| `QuantityIncomePercent` | 100 | 额外渔获缩放（BATCH-076，BATCH-081 改显示名）：[10,300]，GMCM 滑杆；tooltip 显示实际锚点阵列 |
| `ExperienceIncomePercent` | 100 | 额外经验缩放（BATCH-076，BATCH-081 改显示名）：[10,300]，GMCM 滑杆 |
| `NoMinigameQuantityIncomePercent` | 100 | 无小游戏物品额外渔获缩放（BATCH-078，BATCH-081 改显示名）：[10,300]，GMCM 滑杆；独立于主曲线 |
| `CustomFishingTitle` | 空 | 自定义钓鱼称号（BATCH-073）：非空替代所有玩家可见称号；仅手动编辑 config.json，不注册 GMCM |

### 节日钓鱼开关（BATCH-066）

`config.json` 键 `EnableFestivalFishingMods`（GMCM“节日钓鱼应用模组规则”，默认 **false**），覆盖三个钓鱼节日：

| 开关 | 行为 |
|---|---|
| **false（默认）= 完全原生** | 鱿鱼节（SquidFest，冬 12–13 沙滩）/ 鳟鱼大赛（TroutDerby，夏 20–21 森林）/ 冰雪节（Festival of Ice，冬 8 森林）：无声誉注入、无数量倍数、无经验倍数、无品质加成、无模组结算/HUD/提示；分数与概率全原生（鱿鱼节每次成功 +原生数量 分；冰雪节每次成功 +1 分；鳟鱼大赛 tag 概率 33%×原生数量） |
| **true = 模组规则照常** | 声誉/倍数/品质全部生效，且节日分数按数量倍数翻倍：鱿鱼节每次成功 +`原生数量×倍数` 分；冰雪节每次成功 +数量倍数 分（原生 +1，模组补 倍数−1；例：难度 10 → 一次 +10 分） |

- 冰雪节为事件型节日：鱼不进背包、无经验，结算走原生 `Event.caughtFish`（模组 Postfix 补分并消费式清理待处理事实）。
- 开关关闭时所有模组边界自动回落原生（BobberBar 构造/结算/经验五处边界统一判定），不残留状态。

---

## 12. 控制台命令（调试/测试）

| 命令 | 作用 | 示例 |
|---|---|---|
| `fish_setlevel <鱼ID> <等级>` | 直接设置声誉 | `fish_setlevel 128 50` |
| `fish_setlevel [玩家序号] <鱼ID> <等级>` | 直接设置声誉 | `fish_setlevel 128 50` / `fish_setlevel 2 128 50` |
| `fish_addsuccess [玩家序号] <鱼ID> <次数>` | 增加成功次数 | `fish_addsuccess 128 10` / `fish_addsuccess 2 128 10` |
| `fish_addfail [玩家序号] <鱼ID> <次数>` | 增加失败次数 | `fish_addfail 128 5` / `fish_addfail 2 128 5` |
| `fish_info [玩家序号] <鱼ID>` | 查看该鱼详细数据（等级/倍数/皇冠等） | `fish_info 128` / `fish_info 2 128` |
| `fish_list [玩家序号]` | 列出全部已记录鱼种 | `fish_list` / `fish_list 2` |
| `fish_clear [玩家序号] confirm` | 清空全部钓鱼挑战数据（需 confirm；含挑战皇冠与旧存档级键） | `fish_clear confirm` / `fish_clear 2 confirm` |
| `fish_addstar [玩家序号] <鱼ID>` | 强制添加收藏皇冠（测试） | `fish_addstar 128` / `fish_addstar 2 128` |
| `fish_addstars [玩家序号] <数量>` | 批量添加可计数皇冠（完整 61 鱼池：56 普通 + 5 原版传奇，提升 α） | `fish_addstars 20` / `fish_addstars 2 20` |
| `fish_challengecrown [玩家序号] <鱼ID> [0\|1]` | 设置挑战鱼饵流动金色皇冠标记（测试） | `fish_challengecrown 144 1` / `fish_challengecrown 2 144 1` |
| `fish_giant [玩家序号] <鱼ID> <声誉>` | 模拟巨型鱼触发 NPC 反应（BATCH-060：按声誉 ≥8 判定） | `fish_giant 128 20` / `fish_giant 2 128 20` |
| `fish_bonus [玩家序号]` | 查看收藏皇冠数与鱼竿熟练度 α | `fish_bonus` / `fish_bonus 2` |
| `fish_assist` | 强制下一次小游戏触发助战（测试） | `fish_assist` |
| `fish_assiststats [clear]` | 查看/清空助战观测统计（会话内，上限 500） | `fish_assiststats clear` |
| `fish_persisttest <30\|60>` | 强制下一次小游戏按指定秒数判定持久战奖励（测试） | `fish_persisttest 60` |
| `fish_next [玩家序号] <鱼ID>` | 强制下一次钓鱼小游戏为指定鱼（测试；支持玩家序号，BATCH-072） | `fish_next 151` / `fish_next 2 151` |
| `fish_giveitem [玩家序号] <物品ID> [品质0\|1\|2\|4] [数量]` | 给当前/指定玩家物品（测试） | `fish_giveitem 265 4 10` = 10 个铱星海泡布丁 |
| `fish_dumpstars` | 导出星星候选贴图区域 PNG 到 `Mods\FishingExpanded\dump`（识图选素材用） | `fish_dumpstars` |
| `fish_selftest` | 自动自测（难度/可计数皇冠/助战分布/限频日志/经验钳制与倍数曲线等只读项） | `fish_selftest` |

### 调试案例速查（场景 → 命令序列 → 预期）

以下案例可直接在 SMAPI 控制台执行；`128`/`131`/`144` 等鱼 ID 换成你存档中存在的任意原生鱼 ID。

**案例 1：设定声誉并核对称号与倍数**
```
fish_setlevel 128 80
fish_info 128
```
预期：声誉 80 → 称号「众神王」（67–88 区间）；数量倍数 ×80；经验倍数 ×17；难度倍数 ×38（锚点曲线：50 级后每级 +0.6，见 §2.4）。

**案例 2：边界钳制与负声誉**
```
fish_setlevel 129 -10
fish_setlevel 146 100
fish_info 129
fish_info 146
```
预期：精确钳制在 -10 / 100；负声誉时数量/经验保持 ×1、品质不提升、蓄力槽享额外保护（§3）；100 显示称号「太一」。

**案例 3：图鉴皇冠（调整后难度 ≥120）**
```
fish_setlevel 131 50
# 选一条原生 difficulty ≥6 的鱼（调整后 = 原生×20 = 120），真实成功钓起一次：
fish_bonus
```
预期：可计数皇冠 +1、α 沿曲线上涨；图鉴该鱼出现金色皇冠。Mod 鱼/不可触发小游戏的条目只给早期金星、不计 α（§7.1）。

**案例 4：强制下一条鱼 + 流动皇冠标记（测试直写）**
```
fish_next 144              # 下一次小游戏强制为鱼 144（会话态，不写存档，用后失效）
fish_challengecrown 144 1  # 直接写入流动金冠标记
fish_challengecrown 144 0  # 取消标记
```
预期：标记即时生效/清除，图鉴该鱼皇冠呈流动金色。真实获取路径见 §7.3：挑战开始时声誉 ≥95 且挑战鱼饵成功得流动金冠；≥100 额外记录增大版（困难模式图鉴 ×1.3）。

**案例 5：持久战奖励判定**
```
fish_persisttest 60
# 找任意非鱼王钓一次并故意失败；再用 fish_persisttest 30 重复一次
```
预期：60 秒档失败必得海泡布丁 ×1（`(O)265`，日志带「测试强制」）；30 秒档失败 50% 得 +3 钓鱼料理之一；两档不叠加；鱼王豁免（日志提示标志被消耗）。

**案例 6：双人/分屏隔离验证**
```
# 在主机执行：
fish_addstars 2 20         # 给副机加 20 颗可计数皇冠
fish_setlevel 2 151 100    # 设副机鱼 151 的声誉
fish_next 2 151            # 强制副机下一次小游戏为 151
fish_bonus                 # 主机自己的数据应不变
fish_bonus 2               # 核对副机数据
```
预期：全部按 `UniqueMultiplayerID` 隔离，主机与副机互不影响（BATCH-045 玩家序号规则）。

---

## 13. 资深玩家速查

| 目标 | 条件 / 数值 |
|---|---|
| 巨物展示（放大 + NPC 反应） | 声誉 ≥8（BATCH-060）；举起并 5 格内有 NPC；钓后未进 FarmHouse |
| 图鉴皇冠 | 成功钓起且本次调整后难度 ≥120（原版 5 传奇一次钓获直接给） |
| 鱼竿熟练度 α | 可计数皇冠分段线性：0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100% |
| 流光溢彩皇冠 | 声誉 ≥95 + 挑战鱼饵生效时成功（不区分是否超时）；挑战开始时 100 级鱼 ×1.2（困难模式随机无背板 ×1.3，BATCH-074）；皇冠位于详情 UI/鼠标之下 |
| 皇冠助战 | 基础概率 10%×(皇冠/61)＋每条流动金冠 +0.1%＋每条增大金冠再 +0.05%（BATCH-073），加权选鱼；临时 +0~40 钓鱼等级（仅绿条高度）；助战文案显示 15 秒（BATCH-060） |
| 铱星品质起点 | 门槛式：10 级→银、25 级→金、**50 级→铱**（最终品质 = max(原品质, 门槛)，BATCH-060） |
| 鱼跳 / 史诗失败提示 | 调整后难度 ≥150（加速度档位锚点曲线，90 级起满档） |
| 运动公式修正 / 挑战鱼饵 / 力竭 | 调整后难度 >100（力竭 ≥100） |
| 挑战鱼饵数量 | 5 分钟内成功：增产 20%、保底 +2 条（max(⌈N×1.2⌉, N+2)，N=正常渔获；原生多发不再发放，BATCH-082）；超时按正常渔获结算并掉星 −20%/颗（0.8/0.6/0.4，≥95 豁免不掉星，掉星有左下角提示） |
| 万能鱼饵 | 等级 >0 且原生两条：增产 10%、保底 +1 条（max(⌈N×1.1⌉, N+1)；原生第 2 条不再发放；0 级保留原生双倍，BATCH-082） |
| 星之果茶 | 声誉 ≥50，概率 等级/400（每次成功独立判定） |
| 持久战奖励 | 失败 ≥60 秒 → 海泡布丁；30~60 秒 → 50% +3 料理；30 秒巅峰提示 |
| 蓄力槽保护 | ≤20% → ×0.80，≤1% → ×0.50（全局，非鱼王） |
| 鱼王豁免 | 五大传奇鱼：全部系统豁免，一次钓获直接给皇冠 |
| 非鱼类上限 | 等级 8（数量 8 倍封顶，BATCH-062；品质不提升） |
| 一次成功最大收益 | (完美 +10 + round(调整后难度/50)) × 固定钓鱼等级系数 max(等级,1)×0.1，保底 +1，按下一称号上限封顶（BATCH-067） |
| 经验倍数 | 锚点节点 0=×1、10=×2、30=×3、50=×4、75=×5、100=×8 封顶；插值后向下取整（BATCH-082；20级=×2、85级=×6） |
| 经验难度钳制 | 经验公式难度输入 ∈ [原生难度, 120]：负数等级/力竭按原生难度重算（不低于未装模组），超 120 按 120（BATCH-068） |
| 蟹笼 | 每次收获 +1 等级（固定，不吃系数）；经验原生 5 点（BATCH-065） |
| 节日钓鱼 | `EnableFestivalFishingMods` 默认 false = 三个钓鱼节日完全原生；true = 模组规则 + 节日分数按数量倍数翻倍（BATCH-066） |
| 全随机模式 | `EnableRandomFishBehavior` 默认 false = 挑战鱼饵背板（同鱼同等级固定行为）；true = 每局完全随机、无法背板、更难；困难模式下 100 级流动皇冠图鉴 ×1.3（BATCH-058R/S、074） |
| 89 级增长上限 | 88 及以下最多升到 89（超高难入口）；89 级以上每次成功最多 +6 级（100 封顶，BATCH-060 由 +3 上调） |
| 连续失败逃跑减速 | 调整后难度>100（挑战鱼饵同样参与，95 级以上也吃）：连续失败 0→5 次，低条区逃跑减速线性增强到 -10 级等效（≤20% ×0.60、≤1% ×0.20）；成功清零；鱼王豁免 |
| 小游戏浮动提示 | 动作提示=左缘距绿条右缘 24px（左对齐，锚点 x+124）；其他提示=右缘距绿条左缘 50px（右对齐，锚点 x+14，距绿条绝对像素写死）；绿条靠屏幕边缘时自动翻到另一侧（BATCH-058O）；字体=星露谷 dialogueFont + 右下黑阴影 + 非常淡的蓝/红描边（向白色混合 85%）；长文本按最大 420px 折行、每段最多 3 行，超长内容拆成续集提示排队显示（BATCH-058C/058G/058H/058K/058L/058M/058N/058O） |
| 日志开关 | `config.json` `EnableLogging`（2026-08-22 起默认 false；Warn/Error 始终输出）或 GMCM 菜单 |
| 清除挑战进度 | GMCM“重置挑战数据”章节（开关待命 + 确认框两步）或 `fish_clear confirm`；回到刚安装状态 |
| 传奇皇冠回填 | 每次加载存档按图鉴记录自动补发 5 条原版传奇皇冠（装模组前已钓到也补；重置后立即补回） |
| 存档位置 | `Farmer.modData["FishingExpanded/FishDifficultyData"]`（每玩家） |

### 策略要点（面向想冲 100 级/全皇冠的玩家）

- **完美钓获性价比最高**：0 脱杆 +10，还附带 `round(难度/50)` 额外收益；但最终增长受固定钓鱼等级系数缩放——满级（10 级）才是全速，低等级钓鱼时增长较慢，想快速冲级先升玩家钓鱼等级。
- **冲级节奏**：越级限制意味着每个称号区间都要多次成功；先冲高基础难度的鱼（如狗鱼 60、海参类）能更快到达 120 皇冠阈值——`等级 ≈ (120/原版难度 − 1)` 对应的分段倍数可查 2.4 锚点（原版难度 60 只需约 8 级即达标）。
- **皇冠 → 手感**：皇冠不再加隐藏钓鱼等级，而是提高鱼竿熟练度 α（0→100%），高 α 下绿条按下即定速、无惯性、撞边钳制，操作更跟手；满 61 可计数皇冠后小游戏体验最顺。
- **全皇冠顺序**：56 条原生普通真鱼 + 5 条传奇鱼；Mod 鱼皇冠只显示不计 α，别先刷 Mod 鱼。
- **挑战鱼饵的正确用法**：只对调整后难度 >100 的鱼生效，且目标是把该鱼声誉刷到 ≥95——成功后皇冠升级流光溢彩；超时只损失数量，皇冠照拿，可以慢慢磨。
- **力竭是双刃剑**：高难度鱼拖过 15 分钟会降到 80 难度（更易钓），但奖励按开局快照结算，不掉收益；持久战失败还有海泡布丁/料理保底。
- **星之果茶**是等级 50 后的稳定副产：100 级鱼每 4 次成功期望 1 瓶。
- **巨型鱼展示**是限时“战利品”：进家门即失效，想给村民看鱼就钓到后立刻举着逛。
- 触底（-10）后再失败会得到完整升级建议，提示你该换鱼竿/鱼饵/料理。

- **经验流向**：钓鱼等级 10 级封顶后经验全部流入原生精通系统；高声誉下经验倍率可观（100 级 ×20 + 基数钳制 120），刷精通点很快，但经验基数不会因调整后难度无限膨胀（BATCH-068）。

> 注：本 Wiki 为静态文档，仅在收到明确指令时更新（2026-08-22 对照当前源码全仓复核，同步至 manifest 1.0.0 / BATCH-079）。

---

## 14. 与其他 Mod 同装（Walk of Life 兼容层，BATCH-079）

- **触发条件**：检测到 `DaLion.Professions`（Walk of Life - Rebirth ≥1.4.2）安装时启用；未安装则整层休眠，对既有玩家零行为差异。
- **规则排序（经验）**：本 Mod 的 `gainExperience` 前缀优先级 810 > WoL 替换前缀 800——限额清零/基数重算/倍率先裁决，WoL 把调整后的经验记入其声望账本；WoL 关闭技能重置/声望等级时其前缀放行原生，同样成立。
- **规则排序（蟹笼）**：本 Mod 收获前缀 810 先行——数量缩放+登记先完成，WoL 的特殊捕获（Luremaster 真鱼/海藻/宝藏）与 Core 漏斗分支继承乘后堆叠；我方后缀照常结算升星。
- **小游戏参数隔离**：Entry 时按 owner 动态反注册 `BobberBar` 构造/update 上 DaLion.Professions 的全部补丁（豪华饵绿条 +12、Aquarist 蓄力减速、尊贵 Aquarist 满塘秒钓），每次进档幂等复核防动态重挂；日志标签 `[WoL-COMPAT]`，进档一次性 HUD 提示。
- **不接管的内容**：WoL 的咬钩提速、Angler 记忆鱼钩、价格加成、非钓鱼职业树、声望/精通系统等全部保留。

---

# Part 2 · English Version

---

## 1. Overview

- **Mod**: Fishing Expanded (UniqueID: `neoiw.FishingExpanded`, see `manifest.json`).
- **Dependencies**: SMAPI 4.0.0+, Stardew Valley 1.6+; optional Generic Mod Config Menu — exposes three toggles (debug logging, full-random fish behavior, festival fishing rules) plus a two-step "reset challenge data" section; without GMCM, `config.json` always applies (all keys in §11).
- **Settings & logging**: `config.json` is auto-generated on first run (`EnableLogging` default `false` since 2026-08-22 — fresh installs produce no debug output; level gating: only warnings/errors when off, Trace/Debug/Info behind the toggle; Warn/Error/Alert are always visible). The GMCM menu can re-enable logging for troubleshooting and provides the reset section (arm toggle, then a native confirmation dialog after closing the menu; BATCH-042).
- **Core content**: per-fish reputation, dynamic difficulty/count/XP/quality/size scaling, catch-bar protection, high-difficulty motion tuning and fish jumps, collection crowns with rod proficiency, challenge bait, exhaustion, perseverance rewards, crown assist, dual floating-tip channels, giant-fish showcase, NPC reactions, Starfruit Tea drops.
- **Save data**: per player+save in that player's `Farmer.modData`, key `FishingExpanded/FishDifficultyData`, containing collection stars `CollectionStars`, challenge crowns `ChallengeCrowns`, level-100 flowing crowns `Level100FlowCrowns` (BATCH-048) and pattern seeds `ChallengePatternSeeds` (keyed `fishId|level`, deleted on successful landing; BATCH-058); fields missing from old saves default to empty. Saves and farmhands never share data.

---

## 1.5 Glossary (BATCH-060)

| Term | Definition |
|---|---|
| Reputation | Per-fish growth stat (internally `FishStats.DifficultyLevel`, shown as "difficulty level" in code logs; range [−10, 100], non-fish [−10, 8]); success/failure ±1; drives all multipliers and ranks (§2) |
| Adjusted difficulty | Actual minigame difficulty = native difficulty × level multiplier; star crowns (≥120), fish jumps (≥150), exhaustion/challenge bait (≥100/>100) key off it (§4) |
| Effective difficulty | Real-time fight difficulty = adjusted difficulty after exhaustion decay (down to 80 at 15 min; constant under challenge bait) (§9.3) |
| Difficulty tiers | Mechanic bands by effective difficulty (jump intervals 150/250/350/450/550 etc.) (§4.2) |
| Quantity multiplier | Anchor curve + fractional roll (BATCH-082, see §2.4 & §9.2); anchors -10/0=1, 8=2, 16=3, 32=4, 56=7, 100=25 | 100 → 25; 78 → constant 16; 4 → expectation 1.5 |
| XP multiplier | Anchor curve (BATCH-082): `(0,1) (10,2) (30,3) (50,4) (75,5) (100,8)` linear interpolation floored; tail 75→100 ramps faster by design; ≤0 clamps to 1 | 10→2; 50→4; 85→6; 100→8 capped |
| Fixed fishing-level factor | Level-gain factor = max(base fishing level, 1)×0.1 — base field only (0–10, no food/drink buffs or assist temp levels); ×0.1 at 1, ×1.0 at 10 (§2.2, BATCH-067) |
| XP difficulty clamp | XP formula's difficulty input clamped to [native difficulty, 120]: below → recompute at native; above → recompute at 120 (§2.4, BATCH-068) |
| Countable crowns | Crowns of the 56 native regular fish + 5 vanilla legendaries (= 61); mod-fish/extended-legendary crowns display only and don't count toward α or assist (§7.1) |
| Rod proficiency α | Piecewise-linear curve over countable crowns (0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100%), drives minigame feel (§7.2) |
| Challenge stars | Native 3-star counter under challenge bait; one lost per minute from 5:00 (exempt ≥95), each −20% of the final catch (§8) |
| Pattern seed | Fixed random seed for challenge-bait behavior of the same fish+level; cleared on landing; not generated in full-random mode (§9.5) |

---

## 2. Reputation

> In Part 2, `level` in formulas and shorthand text means the fish's **reputation** (internally `FishStats.DifficultyLevel`).

### 2.1 Level Definition

- Per fish: `level = successes − failures`, clamped to `[−10, 100]`.
- Different qualities count as the same fish.
- Non-fish items (Category ≠ -4, trash/weeds) cap at **8**.
- The five legendary fish take no part.

### 2.2 Gains and Losses

On success, base gain by escapes:

| Escapes | Base gain |
|---|---:|
| 0 (perfect) | +10 |
| 1 | +5 |
| 2 | +2 |
| ≥3 | +1 |

- Extra gain: `+ round(adjusted difficulty / 50)` (adjusted 500 → +10), stacked on top.
- **Fixed fishing-level factor (BATCH-067)**: `requested = max(1, round((base + extra) × max(base fishing level, 1) × 0.1))` — base field `Farmer.fishingLevel` (0–10, excludes food/drink buffs and assist temp levels); ×0.1 at level 1, ×1.0 at level 10 (= current speed), 0 clamps to 0.1; higher fishing level → faster growth.
- **Floor (BATCH-067)**: after factor/rounding anything below 1 becomes **+1** — every successful catch gains at least one level (capped fish stay +0, caps respected).
- Rank-ceiling caps (§2.3), max +6 above level 89 (BATCH-060), non-fish cap 8, challenge-bait failure keeps level (BATCH-058): none scaled by the factor.
- **Crab pots (BATCH-065)**: fixed +1 per haul (factor not applied); XP stays the native flat 5.

Failure: level −1 and consecutive-failures +1 (cleared on success; feeds epic failure hints). BATCH-058: challenge-bait failure doesn't drop level but the counter still increments.

### 2.3 Rank Table

| Reputation | Rank | Next-ceiling cap (max growth this catch) |
|---|---|---:|
| <1 | Uhh... slightly stronger one | 3 |
| 1–3 | Elite | 6 |
| 4–6 | Knight | 8 |
| 7–8 | Baron | 15 |
| 9–15 | Count | 22 |
| 16–22 | Duke | 33 |
| 23–33 | Prince | 45 |
| 34–45 | Emperor | 66 |
| 46–66 | God Emperor | 88 |
| 67–88 | King of Gods | 99 |
| 89–99 | Primordial Dragon | 100 |
| 100 | The Primordial One | 100 |

(Rank names follow i18n keys `rank.*`.) Example: level 1 (Elite) + perfect (+10) caps at 6 (Knight ceiling); level 7 (Baron) caps at 15 (Count ceiling).

### 2.4 Effects by Level

| Effect | Formula | Endpoints |
|---|---|---|
| difficulty multiplier | positive piecewise-linear anchors 0→×1.0, 4→×1.2, 20→×5, 50→×20, 100→×50 (slopes 0.05 / 0.2375 / 0.5 / 0.6, increasing); negative `0.5 + 0.5×(level+10)/10` | −10→0.5; 0→1; 100→50 |
| quantity multiplier | anchor curve (BATCH-082): `(-10,1) (0,1) (8,2) (16,3) (32,4) (56,7) (100,25)` linear expectation + fractional roll; anchors and integer-expectation levels constant | 8→2; 56→7; 100→25; 78→constant 16 |
| XP multiplier | anchor curve (BATCH-082): `(0,1) (10,2) (30,3) (50,4) (75,5) (100,8)` linear interpolation floored (tail 75→100 ramps faster by design); ≤0 clamps to 1 | 10→2; 50→4; 85→6; 100→8 capped |
| XP difficulty input | clamped `[native difficulty, 120]` (BATCH-068): below native (negative level/exhaustion) → recompute at native (never below un-modded); above 120 → recompute at 120; recompute replicates vanilla `max(1,(quality+1)×3+difficulty/3)` + treasure +120% / perfect +140% / boss ×5, then still multiplied by the XP multiplier | cap 120 |
| quality raise-to | `level ≥ 50 → iridium(4); ≥25 → gold(2); ≥10 → silver(1)`; final = max(base, threshold) (BATCH-060) | 10 silver; 25 gold; 50 iridium |
| fishSize value | positive `1 + 0.1×level`; negative `max(0.1, 1 − 0.05×|level|)` | 100→11×; −10→0.5× |
| visual scale | `1 + level×0.0270843` (linear, decoupled from quantity, BATCH-060) | 0 → 1.0; 100 ≈ 3.71 |

- Quality codes: 0=normal, 1=silver, 2=gold, 4=iridium (3 skipped).
- **Implementation note**: since BATCH-060 quality is "raise-to" thresholds (10/25/50 → silver/gold/iridium), not cumulative; level 50 is always iridium.
- Size values apply the multiplier once at the settlement boundary (`pullFishFromWater`/`caughtFish`); at level >0 escapes no longer trigger the vanilla 800ms shrink or quality loss (BATCH-038).
- Negative levels: quantity/XP stay ×1, quality never raised.

---

## 3. Catch-Bar Protection

The bar drains more slowly while the fish escapes (smaller multiplier = slower = easier to save):

- **Global protection** (all non-legendary levels incl. 0 and positive): bar ≤20% → ×0.80; ≤1% → ×0.50.
- **Negative-level extra protection** (linear across −10~0): ≤40% → 0.99~1.0; ≤20% → 0.60~1.0; ≤1% → 0.20~1.0.
- **Combining**: minimum of all modifiers from vanilla/other mods (strongest wins).
- Legendaries fully exempt: the constructor exits the custom path before any protection applies.

---

## 4. High-Difficulty Motion (adjusted difficulty > 100)

Non-legendaries only; `adjusted difficulty = vanilla difficulty × level multiplier`. At ≤100 the vanilla motion math is untouched.

### 4.1 Formula Fixes

- **Initial target**: forced to the top at >100 (the vanilla formula computes invalid negative coordinates beyond 100).
- **Retarget probability capped at 150**: large range `min(difficulty,150)/4000`; small offsets (±50~100) `/2000`; dart extra `/1000`, dart offset `±(50~100 + min(difficulty,150)×2)`.
- **Acceleration**: at >100 the value stored into vanilla `bobberAcceleration` is pre-multiplied by a linear boost `1 + tier×(difficulty−100)/100` (transpiler injects the multiply before the store — BATCH-029/034/053; the vanilla random denominator is not modified). Tier anchors: 0→10%, 50→20%, 70→40%, 80→70%, 90→100%, 100→100%, linear between; ≤0 clamps 10%, ≥90 clamps 100% (difficulty 190: tier 80 → ×1.63, ≥90 → ×1.9). At ≤100 boost = 1 (vanilla).

### 4.2 High-Difficulty Fish Jumps (adjusted difficulty ≥ 150)

| Adjusted difficulty | Interval |
|---|---:|
| 150–250 | every 8 s |
| 251–350 | every 6 s |
| 351–450 | every 5 s |
| 451–550 | every 4 s |
| ≥551 | every 3 s |

- Next timer starts only after a jump completes; after cooldown the fish position is checked once per second.
- Fish in the bottom quarter (position ≥399) teleports to the top quarter (random 0~133) after 0.5s; top quarter (≤133) → bottom (random 399~532).
- If the fish leaves the band during the 0.5s delay it still jumps; jumps are instant displacements (speed zeroed) and never change size/count/quality/settlement.
- Each teleport posts a fish-action tip ("leap"/"tail-slap" pools of 15 each) — §5.6.

---

## 5. HUD Messages

Corner messages use a **FIFO queue**: one visible at a time, next after fade-out; cap 5 per player (full → oldest dropped); cleared on returning to title/save switching. Covers success, failure, level suggestions, crown declarations, Starfruit Tea, perseverance rewards. **In-minigame floating tips bypass the queue** and render beside the bar (§5.6). Also: on GameLaunched the mod injects 7 fishing tips into VanillaTips (`neoiw.vanillatips`, source weight 11; skipped if absent — BATCH-060 item 9).

### 5.1 Success

- Regular fish below cap: next-rank challenge line (levels ≤0 also show, using the weak-tier name).
- Level 100 reached (incl. post-cap successes): godhood line ("you are now a god among {fish}…").
- Non-fish at level 8: "emperor" line for the item.
- Legendaries: one of 10 unique lines (e.g. "Ao! The proud king!").
(In-game strings come from i18n.)

### 5.2 Failure (priority high → low)

1. **Epic**: same-fish consecutive fails ≥2 **and** adjusted ≥150 → below-full-crowns: badge hint; **full crowns** (α≥1): replaced by the "union" line (BATCH-038).
2. **Bottomed out** (level −10): full advice line with gear/bait/dish hints.
3. **Negative level**: understanding-deepened line with absolute value.
4. **Normal**: fallback challenge-weaker-individuals line.

### 5.3 Fishing-Level Suggestion (when a minigame opens)

- `level/10 > current fishing level` → advise reaching X first (legendaries exempt).
- `level/10 > fishing level × 2` → prepend an impossible-challenge warning.

### 5.4 Crown Challenge Declaration

- On entering a minigame for a species already holding its collection crown **and level ≥1** (0/negative hidden, BATCH-033): `{honorific}{fish}{rank}{resolute phrase}{challenge phrase}` — honorifics 3 per rank except Primordial Dragon with 5; 50 resolute phrases; 20 challenge phrases; three independent rolls.

### 5.5 Starfruit Tea Drops (regular fish, level ≥ 50)

- Chance = `level/4`% (= level/400): 50 → 12.5%, 80 → 20%, 100 → 25%.
- Drops one `(O)StardropTea` through the single success-settlement boundary (no double grants); full inventory uses the native overflow menu.
- Accompanied by one of 15 random humorous messages. Non-fish (cap 8) naturally excluded; legendaries exempt; local player only.

### 5.6 In-Minigame Floating Tips (two channels, BATCH-038/039/041)

- **Fish-action tips** (jump "leap/tail-slap"): right of the green bar, left-aligned (anchor `xPositionOnScreen+124`; text edge fixed 24px from the bar's right edge; spawn point fixed at trigger moment, 30px above the landing spot).
- **Fish-other tips** (assist/exhaustion nodes/30s peak): left of the bar, right-aligned (anchor `xPositionOnScreen+14`; text edge fixed 50px from the bar's left edge; mid-bar height).
- **Screen-edge flip**: near screen edges the tip flips to the other side of the bar automatically (8px margin fallback).
- Both channels: fixed anchors, default 5s lifetime rising 30px with linear fade, multiple concurrent, cap 8 per channel (oldest dropped); **assist lines last 15s (BATCH-060)**; display-only, writes no game state.

---

## 6. Giant Fish & NPC Reactions

### 6.1 Trigger Conditions (all three)

1. The fish is selected and **held up** (switching hotbar doesn't count; same for controllers);
2. That catch's **reputation ≥ 8** (BATCH-060: judged by it directly; old "quantity >15" equals reputation ≥8);
3. No FarmHouse interior entered since catching it.

### 6.2 Effects

- **Visual scaling**: held fish, fly-to-player animation and the held-up settlement fish all enlarge (bottom/center anchors keep vanilla positions); in-minigame markers stay native-sized.
- **NPC bubbles**: random praise overhead within 5 tiles (320px) — generic pool (100 lines, includes fish name+size) or the NPC's exclusive pool (51 characters × 10 lines each, background-tailored; 60% exclusive / 40% generic roll, uncovered NPCs always generic, BATCH-075); size units inches+in. (English) / cm (`fishSize×2.54` rounded, other languages) (BATCH-033/073); once per NPC per species per giant session (reset on entering FarmHouse or new day, BATCH-074). Animal NPCs (Pet dog/cat by petType, Horse) bark/meow/neigh first, praise in brackets — e.g. "Woof!!! (this pickerel is 388cm, a miracle)"; 5 random sound sets per animal (BATCH-058).
- **Dialogue replacement**: first talk swaps to praise (same pools, same 60/40 split), second restores vanilla; once per NPC/species/session (BATCH-074).
- **Session reset**: entering FarmHouse or a new day zeroes the giant display and all bubble/dialogue records; the next giant catch starts fresh (BATCH-074).
- Legendaries and non-fish never register as giant (current implementation registers Category = -4 real fish only).

---

## 7. Collections, Crowns & Rod Proficiency

### 7.1 Crown Conditions

- **Successfully land** the fish with this minigame's **adjusted difficulty ≥ 120** (e.g. vanilla 3 × 50 = 150); merely hooking or failing grants nothing; legendaries exempt.
- Vanilla legendaries 159/160/163/682/775: **first catch grants the crown outright** (no difficulty gate, BATCH-034).
- Extended legends 898–902 belong to the countable 61 pool: normal rules (adjusted ≥120). **Other mod fish and non-minigame entries earn a gold star** (early mouseCursors star draw) once eligible — not counted toward rod proficiency α.

### 7.2 Rod Proficiency α (permanent effect of crowns, BATCH-034/039)

- α = piecewise-linear over countable crowns: 0→0%, 5→2%, 10→7%, 20→20%, 30→35%, 61→100%; linear between points; clamps at 61.
- Countable = 56 native regular + 5 vanilla legendary (= 61); mod fish/extended legends don't count.
- Feel effects: α=0 fully vanilla; α=1 endpoint feel — instant speed set on press/release, no acceleration/bar damping/inertia, hard edge clamp (no bounce, speed zeroed); max set speed `30−5α` (α=1 → 25px/frame); directional factor toward the fish ×(1+0.2α), away ×(1−0.2α) (α=1 → ×1.2/×0.8, BATCH-056).
- Hidden fishing-level bonus fully removed (old +0.2/crown retired, BATCH-034); framerate decoupling covers the whole minigame (60fps behavior matches vanilla).

### 7.3 Challenge Crowns (flowing gold, BATCH-038/048)

- A fish at level **≥95** landed under **challenge bait** (regardless of the 5-minute limit — overtime only reduces count via star loss) → its crown upgrades to a **flowing golden crown** (gold/white breathing + 1±0.05 scale pulse on the collections page).
- **Level-100 flowing crown (BATCH-048)**: challenges started at level ≥100 that succeed under bait additionally record `Level100FlowCrowns` — drawn at ×1.2 base size; **×1.3 in hard mode** (full-random, `EnableRandomFishBehavior=true`) — a display-only easter egg (BATCH-074), no effect on assist weights or save logic.
- Judged from the level snapshot at challenge start; recorded once at the single settlement boundary; legendaries exempt.
- Dedicated save fields `ChallengeCrowns` / `Level100FlowCrowns`, missing-in-old-saves default empty; test command `fish_challengecrown [playerIdx] <fishId> [0|1]`.

### 7.4 Crown Assist (BATCH-035/073)

- Rolled once per non-legendary minigame: total chance = `10% × (countable crowns/61)` linear (0% at none, 10% at full) **+0.1% per countable flowing golden crown +0.05% more per enlarged level-100 flowing crown** (bonuses uncapped, bounded naturally by the 61 pool — BATCH-073).
- On hit the assist fish is picked by weight: normal crowned fish weight 1, flowing golden +0.001, enlarged level-100 flowing +0.0005 more (BATCH-073 weighted pick; near-uniform, golden-crown fish slightly favored).
- Temporary fishing level 0–40, tilted by the chosen fish's difficulty rank (top fish favors high assists; means 26.55 / 13.45).
- Effect: this minigame's bar height +`temp level×8px`; **never writes player level or save**, no effect on XP/count/quality/difficulty.
- Shows one of 20 assist lines in the other-tips channel; **assist lines display 15 seconds** (rise 30px, linear fade; BATCH-060 — only assist is extended, others 5s); challenge bait blocks assist entirely; legendaries exempt.

### 7.5 Collections Page Display

- Fish tab: gold crown drawn top-left over each of the 61 countable icons (Infinity Crown texture); flowing-golden breathing draw for challenge crowns (level-100 ones at ×1.2 base, ×1.3 in hard mode — BATCH-048/074, display only); mod fish/non-fish get the early gold star (`Game1.mouseCursors (346,392,8,8)`, 20×20 `Color.Gold`).
- Description adds: `Challenge rank: {rank} ({X})` (hidden at ≤0; legendaries skip the line).
- Crowned countable fish append "improved rod handling" (i18n `collections.crownControl`, BATCH-058T wording; mechanism keeps the name rod-proficiency α); starred entries show "no rod handling bonus" (`collections.noCrownControl`); pure display.

---

## 8. Item Handling & Baits

- The animation phase always shows the native fish count (no fabricated counts).
- The quantity multiplier applies once after vanilla `CreateFish` creates the final item: `final stack = native stack × level multiplier`; inventory and overflow menu (`ItemGrabMenu`) share the same item — no second overflow path, no losses.
- **Wild bait bonus** (BATCH-038/082): level >0 and a native 2-fish result (`numCaught ≥ 2`, non-challenge bait) → **+10% catch with a +1 floor**: final = max(⌈N×1.1⌉, N+1), N = the normal catch (1 × level multiplier); the native extra fish is no longer granted inside the mod's scope. At level 0 the vanilla double catch is kept.
- **Challenge bait revamp** (active when adjusted >100, BATCH-038/082):
  - **Success within 5:00 (300s)** → **+20% catch with a +2 floor**: final = max(⌈N×1.2⌉, N+2) (the native `challengeBaitFishes=3` extras are no longer granted inside the mod's scope); adjusted ≤100 stays vanilla (3 fish as before);
  - **Overtime (BATCH-082)**: the count bonus is cancelled and the native extras are likewise replaced — settles at the normal catch N (keeping 3 would reward stalling for triple);
  - **Overtime star drop (BATCH-058)**: one native star lost per minute from 5:00 (5:00→2, 6:00→1, 7:00→0, unrecoverable; exempt at level ≥95), each star −20% of the FINAL count (0.8/0.6/0.4, rounded, floored at ≥1 — BATCH-067); level gains, normal crowns, flowing crowns, quality and size all unaffected (e.g. a level-98 fish landed after 30 minutes still gets its flowing crown); **a bottom-left FIFO notice shows remaining stars and the percentage lost (BATCH-060, visible during the minigame)**;
  - **No difficulty decay** (exhaustion doesn't apply; effective difficulty stays at the opening value);
  - **No fish assist**; the **native "3 failed escapes" rule is disabled** (grind until you win).
- Stacking follows vanilla (same kind+quality stack, cap 999).

---

## 9. Special Fish & Perseverance

### 9.1 Legendary Fish (fully exempt)

Angler (159), Mutant Carp (682), Glacierfish (775), Crimsonfish (163), Legend (160).

- No level tracking, no difficulty/count/quality/XP multipliers, no ranks, no scaling, no NPC reactions, no bar protection, no suggestions, no assist, no exhaustion.
- **First catch grants the crown**, counted into α (BATCH-034).
- IDs normalize to vanilla item IDs (`163`/`(O)163`/`(o)163` identical); extended legends 898–902 follow normal-fish rules.
- One of 10 unique lines shows on capture.

### 9.2 Non-Fish Items (Category ≠ -4)

- Range [−10, 8]; successes past 8 add nothing further (underlying stats hold at 8).
- **Level-ups are now probabilistic (BATCH-078)**: each haul requests +1 level but only a **5%** chance is granted (`NonFishLevelUpChance`); a missed roll still records the success event (fail-streak cleared, star check runs; `SuccessCount` keeps its cumulative-growth semantics and adds 0) and can pop a light consolation hint at 20% (`hud.trashMiss.{1..20}`). Rod-direct trash and crab pots share the same rule.
- **Quantity uses an exclusive anchor curve (BATCH-078)**: `(0,1) (8,8)` linear expectation + the same fractional-roll smoothing (negatives clamp to 1; e.g. level 4 expectation 4.5 → 45% chance of 5); plus its own income slider `NoMinigameQuantityIncomePercent` (see §Config).
- Quality computes at level 8 (threshold 10 unreachable → never raised).
- Past-cap successes show the "emperor" line for the item.
- **Implementation note**: design docs say capped non-fish keep participating in visuals/NPC/stars, but current code registers giant displays only for Category = -4 real fish; their raw difficulty makes crown thresholds practically unreachable anyway.

### 9.2.1 Training Rod (BATCH-078)

- Detection matches vanilla: held tool `QualifiedItemId == "(T)TrainingRod"`.
- Real fish only (non-fish unaffected): effective reputation clamps to **4** (`TrainingRodLevelCap`) at the settlement source — quantity/XP/size/crowns/giant-fish/starfruit-tea all downstream automatically ≤4.
- When real reputation already exceeds 4: that success writes **nothing to the save** (RecordSuccess isn't even called); if uncapped growth would pass 4, a random `hud.trainingCap.{1..30}` line replaces the rank-up tip. The stored real reputation is never rewritten.

### 9.2.2 Income Scaling (BATCH-076, three GMCM sliders)

| Slider | config field | range | application point |
|---|---|---|---|
| Quantity income | `QuantityIncomePercent` | [10,300] default 100 | after ALL bonuses/discounts scale final stack (ceil, ≥1); crab pots before book ×2 |
| XP income | `ExperienceIncomePercent` | [10,300] default 100 | after base recalc + multiplier (ceil); daily-limit zero wins |
| No-minigame quantity income | `NoMinigameQuantityIncomePercent` | [10,300] default 100 | after the exclusive curve result |

In vanilla-festival mode none of the three apply; quantity tooltips render the live anchor list via `FormatQuantityAnchors`.

### 9.3 Exhaustion (adjusted ≥100, non-legendary, BATCH-038)

- Nodes/percentages: 1/3/5/7/9/12/15 min → 1%/3%/10%/20%/35%/50%/100%, linear between (incl. the 0→1 min segment).
- Formula: `effective = adjusted + (80 − adjusted) × percent`; at 15 min (100%) difficulty reaches 80 and stays there.
- **Rewards don't drop**: count/level/crown settle on the opening adjusted snapshot; **XP base likewise (BATCH-068)** — the XP input clamps to [native, 120], recomputing at native when effective falls below it, never worse than un-modded.
- Challenge bait: **no decay** (effective stays at opening value) but node tips still appear with an extra random quip.
- Each node posts one random witty tip (i18n `hud.exhaust.{1|3|5|7|9|12|15}.{1..10}`).

### 9.4 Perseverance Rewards (BATCH-039)

- Battle seconds accumulate across all non-legendary minigames (pause/menus excluded; legendaries inherently excluded).
- **At 30s exactly**: single peak tip in the other-tips channel (one of 10 sets).
- **Fail at ≥60s**: guaranteed Seafoam Pudding ×1 (`(O)265`, fishing +4).
- **30–60s fail**: 50% chance of one of the three +3 fishing dishes (`(O)242` / `(O)728` / `(O)730`; BATCH-052 correction: 228 is actually Maki Roll, not a fishing dish).
- The 60s reward doesn't stack the 30s roll; rewards go to inventory (overflow menu when full) with 20 random humor lines into the FIFO queue.
- Test command: `fish_persisttest <30|60>`.
- During the minigame, food buffs (`id="food"`) pause; resumes on exit (BATCH-056; drinks and others run normally).

### 9.5 Truce, Jump Wind-Up & Pattern Seeds (BATCH-058)

- **Truce**: bar idle for 3s → the fish freezes when it next leaves the bar by >5px, gently swaying; bar doesn't drain and battle time pauses during truce; input resumes everything. Ten random truce line sets exist.
- **Jump wind-up**: after the existing 0.5s delay an added 0.88s wind-up (icon rotates linearly to **±70°** over 0.77s then snaps back in 0.11s — BATCH-058T curve, self-test checks 0.385s=35°, 0.77s=70°), then the teleport; upward = counter-clockwise (negative), downward = clockwise; red glow pulses during wind-up. The action tip fires at judgment/delay start (0.5s before the wind-up).
- **Challenge pattern seed**: the same fish at the same level behaves identically (target sequence, darts, jump timing) under a fixed seed until landed; failures don't drop level but the consecutive counter increments; landing clears the seed, next encounter rerolls. **Full-random mode** (`EnableRandomFishBehavior=true`, default off) generates no seeds — every fight is completely random and unlearnable.

### 9.6 High-Difficulty Phrase Loop (BATCH-059)

- Non-legendaries ≥150: after a teleport, the first middle crossing (266±10, arrival only) starts a phrase; the segment (including that teleport) loops — teleport→middle→teleport→middle — replaying wind-up and action tips too.
- Every net progress of 1/3 switches to a fresh segment; players can memorize short segments instead of facing 20 minutes of randomness.
- Under non-challenge bait, hitting an exhaustion node interrupts recording (the loop exits immediately; the fish moves vanilla-style at reduced difficulty, re-recording on the next jump if still eligible); challenge bait never decays and is never interrupted (BATCH-059A).

---

## 10. Multiplayer / Co-op / Split-Screen

- Online: host and every farmhand **must install** the mod; each player reads/writes only their own `modData`, bound to their save.
- Local split-screen: independent instances per local player; levels/stars/bonuses/displays/queues all isolated by `UniqueMultiplayerID`.
- Drops (Starfruit Tea/perseverance), settlement and showcase affect the local player only.

---

## 11. Config Keys & Festival Fishing (BATCH-066)

All config keys (edit `config.json` manually without GMCM; the keys below all have GMCM controls, `CustomFishingTitle` is manual-only):

| Key | Default | Notes |
|---|---|---|
| `EnableLogging` | false | Master debug/info log switch (since 2026-08-22); Warn/Error/Alert always print |
| `EnableRandomFishBehavior` | false | Full-random fish behavior (BATCH-058R/S): true = every fight fully random, no pattern seeds, harder; level-100 flowing crowns draw ×1.3 in the collection page (BATCH-074) |
| `EnableFestivalFishingMods` | false | Apply mod rules to festival fishing (see table below) |
| `QuantityIncomePercent` | 100 | Quantity income scaling (BATCH-076): [10,300], GMCM slider; tooltip renders the live anchor list |
| `ExperienceIncomePercent` | 100 | XP income scaling (BATCH-076): [10,300], GMCM slider |
| `NoMinigameQuantityIncomePercent` | 100 | No-minigame quantity income scaling (BATCH-078): [10,300], GMCM slider; independent of the main curve |
| `CustomFishingTitle` | empty | Custom angler title (BATCH-073): non-empty replaces all player-visible rank titles; manual edit only, not registered in GMCM |

### Festival Fishing Toggle (BATCH-066)

`config.json` key `EnableFestivalFishingMods` (GMCM "apply mod rules to festivals", default **false**) covers three festivals:

| Toggle | Behavior |
|---|---|
| **false (default) = fully vanilla** | SquidFest (beach, Winter 12–13) / TroutDerby (forest, Summer 20–21) / Festival of Ice (forest, Winter 8): no level injection, no quantity/XP multipliers, no quality bonus, no mod settlement/HUD/tips; scores and chances stay vanilla (SquidFest +native count per catch; Ice Fest +1 per catch; TroutDerby tag chance 33%×native count) |
| **true = mod rules apply** | Levels/multipliers/quality active, festival scores scale with the quantity multiplier: SquidFest +`native count×multiplier` per catch; Ice Fest +multiplier per catch (native +1, mod adds multiplier−1; e.g. level 10 → +10 per catch) |

- Ice Fest is event-based: fish don't enter the inventory and grant no XP; settlement rides vanilla `Event.caughtFish` (mod Postfix awards points and consumes pending facts).
- With the toggle off every mod boundary falls back to vanilla uniformly (five BobberBar construct/settlement/XP boundaries), leaving no residue.

---

## 12. Console Commands (debug/testing)

| Command | Purpose | Example |
|---|---|---|
| `fish_setlevel <fishId> <level>` | Set a fish's reputation directly | `fish_setlevel 128 50` |
| `fish_setlevel [playerIdx] <fishId> <level>` | Set directly (target player) | `fish_setlevel 128 50` / `fish_setlevel 2 128 50` |
| `fish_addsuccess [playerIdx] <fishId> <count>` | Add successful catches | `fish_addsuccess 128 10` |
| `fish_addfail [playerIdx] <fishId> <count>` | Add failures | `fish_addfail 128 5` |
| `fish_info [playerIdx] <fishId>` | Inspect a fish (level/multipliers/crowns) | `fish_info 128` / `fish_info 2 128` |
| `fish_list [playerIdx]` | List all recorded species | `fish_list` / `fish_list 2` |
| `fish_clear [playerIdx] confirm` | Wipe all challenge data (confirm required; incl. crowns and legacy save keys) | `fish_clear confirm` |
| `fish_addstar [playerIdx] <fishId>` | Force-add a collection crown (testing) | `fish_addstar 128` |
| `fish_addstars [playerIdx] <count>` | Batch-add countable crowns (full 61 pool: 56 regular + 5 legends; raises α) | `fish_addstars 20` |
| `fish_challengecrown [playerIdx] <fishId> [0\|1]` | Set/clear the flowing golden crown flag (testing) | `fish_challengecrown 144 1` |
| `fish_giant [playerIdx] <fishId> <level>` | Simulate giant-fish NPC reactions (judged by level ≥8, BATCH-060) | `fish_giant 128 20` |
| `fish_bonus [playerIdx]` | Show crown count and rod proficiency α | `fish_bonus` / `fish_bonus 2` |
| `fish_assist` | Force the next minigame to trigger assist (testing) | `fish_assist` |
| `fish_assiststats [clear]` | View/clear assist observation stats (session-scoped, cap 500) | `fish_assiststats clear` |
| `fish_persisttest <30\|60>` | Force perseverance judgment at the given seconds (testing) | `fish_persisttest 60` |
| `fish_next [playerIdx] <fishId>` | Force the next minigame's fish (testing; player index supported, BATCH-072) | `fish_next 151` / `fish_next 2 151` |
| `fish_giveitem [playerIdx] <itemId> [quality 0\|1\|2\|4] [count]` | Grant an item (testing) | `fish_giveitem 265 4 10` = 10 iridium Seafoam Puddings |
| `fish_dumpstars` | Export candidate star-texture regions to `Mods\FishingExpanded\dump` (for asset picking) | `fish_dumpstars` |
| `fish_selftest` | Read-only self-test (levels/countable crowns/assist distribution/log gating/XP clamp & curves…) | `fish_selftest` |

Eighteen commands total. Data/query commands accept an optional leading player index (1=host, 2=first farmhand, …, from `Game1.getAllFarmers()`; omitted = current player, BATCH-045); assist/persisttest/selftest/dumpstars have no index.

### Debug Scenarios (scenario → command sequence → expectation)

Run these directly in the SMAPI console; replace `128`/`131`/`144` etc. with any native fish ID present in your save.

**Case 1: set reputation, check rank & multipliers**
```
fish_setlevel 128 80
fish_info 128
```
Expect: reputation 80 → rank "King of Gods" (67–88 band); quantity multiplier ×80; XP multiplier ×17; difficulty multiplier ×38 (anchor curve: +0.6 per level past 50, §2.4).

**Case 2: clamping & negative reputation**
```
fish_setlevel 129 -10
fish_setlevel 146 100
fish_info 129
fish_info 146
```
Expect: exact clamps at −10 and 100; negative reputation keeps quantity/XP ×1, never raises quality, and gets extra bar protection (§3); reputation 100 shows "The Primordial One".

**Case 3: collection crown (adjusted ≥120)**
```
fish_setlevel 131 50
# pick a fish with native difficulty ≥6 (adjusted = native×20 = 120), then really land it once:
fish_bonus
```
Expect: countable crowns +1 and α rises along the curve; the fish icon gains a gold crown. Mod fish / non-minigame entries instead get an early star that doesn't count toward α (§7.1).

**Case 4: force next fish + flowing-crown flag (direct test write)**
```
fish_next 144              # force the next minigame to fish 144 (session-scoped, not saved)
fish_challengecrown 144 1  # write the flowing golden flag directly
fish_challengecrown 144 0  # clear the flag
```
Expect: the flag applies/clears instantly; that crown renders as flowing gold. Real path see §7.3 — challenge started at reputation ≥95 landed under bait earns the flowing crown; ≥100 additionally records the enlarged version (draws ×1.3 in hard mode).

**Case 5: perseverance reward judgment**
```
fish_persisttest 60
# fail any non-legendary catch on purpose; repeat with fish_persisttest 30
```
Expect: the 60s tier guarantees Seafoam Pudding ×1 (`(O)265`, log carries "forced"); the 30s tier fails into a 50% chance of one +3 fishing dish; tiers don't stack; legendaries are exempt (log notes the flag being consumed).

**Case 6: split-screen / co-op isolation**
```
# on the host:
fish_addstars 2 20         # give farmhand 2 twenty countable crowns
fish_setlevel 2 151 100    # set farmhand 2's fish-151 reputation
fish_next 2 151            # force that farmhand's next minigame
fish_bonus                 # your own data must be unchanged
fish_bonus 2               # verify the farmhand's data
```
Expect: everything is isolated by `UniqueMultiplayerID`; host and farmhands never interfere (BATCH-045 index rules).

---

## 13. Quick Reference

| Goal | Condition / Value |
|---|---|
| Giant showcase (scaling + NPC reactions) | Reputation ≥8 (BATCH-060); held up with an NPC within 5 tiles; no FarmHouse visit since landing |
| Collection crown | Landed with this minigame's adjusted ≥120 (5 legends grant on first catch) |
| Rod proficiency α | Countable crowns piecewise-linear: 0/5/10/20/30/61 → 0%/2%/7%/20%/35%/100% |
| Flowing golden crown | Level ≥95 + landed under challenge bait (overtime irrelevant); challenges started at 100 draw ×1.2 (hard mode ×1.3, BATCH-074); crown renders beneath detail UI/cursor |
| Crown assist | Base chance 10%×(crowns/61) +0.1%/flowing +0.05%/more enlarged (BATCH-073), weighted pick; temp +0~40 fishing level (bar height only); assist lines last 15s (BATCH-060) |
| Iridium quality floor | Threshold raise-to: 10 silver / 25 gold / **50 iridium** (final = max(base, threshold), BATCH-060) |
| Fish jumps / epic failure | Adjusted ≥150 (acceleration tier curve saturates from level 90) |
| Motion fixes / challenge bait / exhaustion | Adjusted >100 (exhaustion ≥100) |
| Challenge bait counts | ≤5:00: +20% catch, +2 floor (max(⌈N×1.2⌉, N+2), N = normal catch; native extras replaced, BATCH-082); overtime: normal catch N, then −20%/lost star (0.8/0.6/0.4, ≥95 exempt from star loss, HUD notice) |
| Wild bait | Level >0 and native pair: +10% catch, +1 floor (max(⌈N×1.1⌉, N+1); vanilla double kept at level 0; BATCH-082) |
| Starfruit Tea | Reputation ≥50, chance reputation/400 (independent per success) |
| Perseverance | Fail ≥60s → Seafoam Pudding; 30–60s → 50% +3 dish; peak tip at 30s |
| Bar protection | ≤20% → ×0.80, ≤1% → ×0.50 (global, non-legendary) |
| Legendary exemption | Five legends: exempt from everything, crown on first catch |
| Non-fish cap | Level 8 (quantity 8× capped, BATCH-062; quality never raised) |
| Max single-catch gain | (perfect +10 + round(adjusted/50)) × factor max(level,1)×0.1, floored +1, capped by next-rank ceiling (BATCH-067) |
| XP multiplier | anchor nodes 0=×1, 10=×2, 30=×3, 50=×4, 75=×5, 100=×8 capped; interpolation floored (BATCH-082; level 20 = ×2, 85 = ×6) |
| XP input clamp | ∈ [native, 120]: negative/exhaustion recompute at native (never below un-modded), >120 at 120 (BATCH-068) |
| Crab pot | +1 level per haul (fixed, no factor); XP native flat 5 (BATCH-065) |
| Festival fishing | `EnableFestivalFishingMods` false = three festivals fully vanilla; true = mod rules + scores scaled by quantity multiplier (BATCH-066) |
| Full-random mode | `EnableRandomFishBehavior` false = pattern-seed backboard (same fish+level behaves identically); true = fully random per fight, harder; hard mode draws level-100 crowns ×1.3 (BATCH-058R/S, 074) |
| Reputation-89 growth cap | ≤88 caps at 89 (gateway to ultra-high); above 89 max +6/success (cap 100, BATCH-060 raised from +3) |
| Consecutive-fail escape slowdown | Adjusted >100 (challenge bait participates; level 95+ affected too): fails 0→5 ramp slowdown toward the −10-equivalent (≤20% ×0.60, ≤1% ×0.20); cleared on success; legendaries exempt |
| Floating tips | Action tips: left edge 24px from bar right edge (anchor x+124); other tips: right edge 50px from bar left edge (anchor x+14, absolute pixels); auto-flip near screen edges (BATCH-058O); SDV dialogueFont + black drop shadow + faint blue/red outline (85% toward white); wrap at 420px max, 3 lines per tip, longer texts queue as sequels (BATCH-058C/G/H/K/L/M/N/O) |
| Logging | `config.json` `EnableLogging` (default false since 2026-08-22; Warn/Error always print) or GMCM |
| Reset progress | GMCM reset section (arm toggle + confirm dialog) or `fish_clear confirm`; back to fresh-install state |
| Legendary crown backfill | On each save load the 5 legendary crowns are re-granted per bestiary records (pre-mod catches included; restored immediately after reset) |
| Save location | `Farmer.modData["FishingExpanded/FishDifficultyData"]` (per player) |

### Strategy Tips (toward level 100 / full crowns)

- **Perfect catches pay best**: 0 escapes +10 plus `round(difficulty/50)` extra — but the fixed fishing-level factor scales it down, so max your fishing level first for full speed.
- **Pacing**: rank ceilings force several successes per band; grind high-base-difficulty fish first (e.g. pike 60, sea cucumbers) to reach the 120 crown threshold faster — needed level ≈ anchors in §2.4 (vanilla 60 needs only ~8 levels).
- **Crowns → feel**: crowns raise α (0→100%); high α gives instant-speed, inertia-free, edge-clamped control; all 61 countable crowns feels best.
- **Crown order**: 56 native regular + 5 legends; mod-fish crowns display only — don't farm those first.
- **Proper challenge-bait use**: works only above adjusted 100; aim to push a fish to ≥95 then land it under bait for the flowing crown; overtime costs only count — take your time.
- **Exhaustion cuts both ways**: dragging past 15 minutes drops difficulty to 80 (easier) while rewards stay at the opening snapshot; long fights failing still net pudding/dishes.
- **Starfruit Tea** is steady side income after 50: a level-100 fish averages one bottle per 4 successes.
- **Giant showcase** is a limited-time trophy: it dies at your farmhouse door — hold it up and parade immediately.
- Failing at the bottom (−10) gives the fullest upgrade advice (rod/bait/dish).

- **XP flow**: after fishing level 10 all XP flows into mastery; high levels multiply fast (100 ×20 with base clamp 120), but the base never explodes with adjusted difficulty (BATCH-068).

> Note: This wiki is a static document updated only on explicit instruction (2026-08-22 full source re-audit, synced to manifest 1.0.0 / BATCH-079).

---

## 14. Coexisting with Other Mods (Walk of Life layer, BATCH-079)

- **Activation**: enabled only when `DaLion.Professions` (Walk of Life - Rebirth ≥1.4.2) is installed; fully dormant otherwise — zero behavioral difference for existing players.
- **Rule ordering (XP)**: this mod's `gainExperience` prefix runs at priority 810, ahead of WoL's replacement prefix (800) — daily-limit zeroing / base recalc / multiplier decide first, then WoL books the adjusted value into its prestige ledger; with WoL's skill-reset/prestige toggles off, its prefix passes through to vanilla and the ordering still holds.
- **Rule ordering (crab pots)**: this mod's harvest prefix (810) scales and registers first — WoL's special catches (Luremaster fish/algae/treasure) and the Core hopper branch inherit the scaled stack; our postfix still settles reputation.
- **Minigame parameter isolation**: at Entry, all DaLion.Professions patches on the `BobberBar` constructor and `update` are unpatched by owner (Deluxe-Bait bar +12, Aquarist slowdown, prestiged instant full-pond catches), re-verified idempotently every save load against dynamic re-patching; log tag `[WoL-COMPAT]`, plus a one-time HUD notice per session.
- **Not taken over**: WoL bite-speed perks, Angler tackle memory, price bonuses, non-fishing profession trees, prestige/mastery systems all keep working.
