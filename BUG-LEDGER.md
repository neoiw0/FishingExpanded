# FishingExpanded 任务总账

## 接管后的当前状态（2026-08-05）

本文件是当前批次和验收状态的权威总账；历史批次正文保留为证据，不覆盖本节和顶部汇总表。维护入口见 `MAINTENANCE-INDEX.md`。

### 用户最终确认的设计门禁

1. 0级也应用全局20%/1%蓄力槽保护。
2. Mod数量倍数不改变钓鱼动画数量；数量进入背包或原生 `ItemGrabMenu` 溢出路径时才转换。原生特殊鱼饵的多鱼机制保留。
3. 完美/脱杆奖励先按 `+10/+5/+2/+1` 计算，再限制在当前称号区间，最终等级不能跨区间。
4. 所有同时生效的蓄力槽减速规则取最强效果，即适用倍率中的最小值。
5. 五只鱼王除特殊提示外不参与难度、数量、品质、奖励、技能加成、称号、视觉、NPC和20%/1%全局蓄力槽保护。
6. 鱼王 ID 按原生物品 ID 归一化，`163`、`(O)163` 和 `(o)163` 视为同一只鱼王。
7. 高难度星标只在成功钓起后获得（调整后难度 ≥120）；失败或仅进入小游戏不获得。钓鱼条长度额外加成（0.5→0.2）已整体删除（2026-08-09 用户确认，BATCH-034），图鉴不再显示该行。
8. 难度、星标和隐藏加成按玩家独立保存；在线联机主机和每位农场客都必须安装本 Mod。
9. 非鱼类达到8级后，后续成功的实际难度收益为 `+0`。
10. 已有图鉴星标的鱼进入小游戏时只显示一次随机挑战宣言：称号尊敬词每级3种、毅然决然词50种、应战说法20种；不修改任何状态。难度等级<1（0和负数）的鱼即使拥有星标也不显示宣言（2026-08-08 用户确认，BATCH-033）。
11. 小游戏出现时，非鱼王按 `难度等级 / 10 > 当前有效钓鱼等级` 显示升级建议；若同时大于当前钓鱼等级两倍，在建议开头增加“不可能的高难度挑战”提示。
12. 视觉放大只作用于真鱼：从水里飞出的飞行动画（中心锚点保持飞行轨迹）、落地后立在玩家身边的真鱼（底边中点锚点）和手持鱼（底边中点锚点）；小游戏鱼标与结算面板示意图保持原生大小（2026-08-06 用户确认）。
13. ~~图鉴“钓鱼条长度额外加成”~~（BATCH-034 删除：隐藏等级加成整体退休，2026-08-09 用户确认）。

14. 隐藏等级加成整体删除；Mod 鱼皇冠只显示、不计入鱼竿熟练度；原版 5 条传奇鱼（159/160/163/682/775）钓到一次直接给皇冠，扩展传奇 898–902 按普通鱼规则（2026-08-09，BATCH-034）。
15. 鱼竿熟练度 α = 可计数皇冠数分段线性曲线（0→0%、5→2%、10→7%、20→20%、30→35%、61→100%，点间线性、超 61 钳 100%，2026-08-10 用户确认，BATCH-039 覆盖 BATCH-034 的 0→61 全程线性）；56 普通 + 5 原版传奇可计数，Mod 鱼不计；α=0 原生手感、α=1 终点手感（按下/松开瞬间定速 30px/帧、无加速度/条内阻尼/惯性、撞边完全钳制）；绿条外钳制机制取消（BATCH-034）。
16. 帧率解耦覆盖整个钓鱼小游戏（绿条、鱼、平滑、概率），60fps 行为与原生一致；加速度增幅档位锚点曲线（2026-08-12 用户定稿，BATCH-053 覆盖 BATCH-029/034 的 10%/98 阈值）：0 级 10%、50 级 20%、70 级 40%、80 级 70%、90 级 100%、100 级 100%，点间线性，≤0 钳 10%、≥90 钳 100%；公式 `增幅 = 1 + 档位×(调整后难度−100)/100`，难度 ≤100 恒 1（2026-08-09 BATCH-034 曾定 <98→10%、≥98→100%，因 96/97 与 98+ 存在 6~7 倍断层被本次实测反证替换）。
17. 跳鱼瞬移时显示贴鱼文案：上跳（目标 0~133）“鱼跃”类 15 条随机、下跳（目标 399~532）“甩尾！”类 15 条随机，5 秒内上移 30px 线性淡出（2026-08-09，BATCH-034；2026-08-10 BATCH-039 统一 5 秒）。
18. 皇冠助战（BATCH-035，2026-08-09 用户确认+多次修正）：非鱼王小游戏构造时判定一次；总概率 = 10% × (可计数皇冠数/61) 线性（Mod 鱼皇冠不计）；命中后**均匀随机**选助战鱼（所有皇冠鱼概率相同）；临时钓鱼等级 = 0~40 随机且**随被选中鱼难度排位线性倾斜**（w(L)=1+s(L-20)/20，s=-29/31+58r/31）：最高难度鱼 P(40)≈4.72%、P(0)≈0.16%、平均 26.55，最低难度鱼镜像（P(0)≈4.72%、P(40)≈0.16%、平均 13.45），40 级概率最高鱼=最低鱼 30 倍，最低+最高两条鱼平均恰 20，中位鱼均匀，最低鱼也可能抽到 40 级；绿条高度只加 临时等级×8px（保留原生训练竿/浮标计算），不写玩家等级与存档；绿条旁 20 条随机文案（荣耀的【鱼种类】【鱼职阶】前来护驾等，1 秒停留+0.5 秒淡出）；鱼王豁免；尺寸口径保持原生（英语英寸、其他语言厘米）。
19. 图鉴皇冠加成显示（BATCH-036，2026-08-09 用户确认；2026-08-10 BATCH-039 改名；2026-08-14 BATCH-058T 文案改“鱼竿手感增强”）：有皇冠的鱼在描述中追加“鱼竿手感增强”（i18n collections.crownControl；中英双语；英文 Improved rod handling；机制名“鱼竿熟练度 α”保留）；非鱼王同时保留“挑战等级”行（格式“挑战等级：{称号}（{X}级）”），鱼王豁免挑战等级但皇冠行照常显示；纯展示，不新增状态与存档字段。
20. 难度倍数曲线（BATCH-037，2026-08-10 用户确认，含 50 级锚点修正）：非鱼王正数区间改为分段线性，锚点 0级×1.0、4级×1.2、20级×5、50级×20、100级×50，每段斜率递增（越往后每级加得越多）；负数区间 [-10,0] 保持 0.5~1.0 线性不变；数量/经验/品质/尺寸公式不受影响；鱼王不乘倍数。

21. 力竭机制（BATCH-038，2026-08-10 用户确认）：调整后难度 ≥100 的非鱼王小游戏参与；节点 1/3/5/7/9/12/15 分钟 → 1%/3%/10%/20%/35%/50%/100%，节点间线性（含 0→1 分钟段）；15 分钟难度降到 80 后保持；公式 = 调整后难度 + (80-调整后难度)×百分比；难度随节点下降时所有难度档位整体下降，但奖励以开局快照计算不下降；每个节点在“鱼其他提示”通道显示 10 套随机诙谐文案（i18n hud.exhaust.{1|3|5|7|9|12|15}.{1..10}）；计时只在小游戏进行中累计。
22. 提示双通道（BATCH-038；2026-08-10 BATCH-039 统一 5 秒淡出）：鱼动作提示（鱼跃/甩尾）与鱼其他提示（助战/力竭/30 秒巅峰提示等）各为独立浮动提示通道；起点固定、显示 5 秒、5 秒内上移 30px 并线性淡出、可同时多条、每通道上限 8 丢最旧；其他提示锚定钓鱼条左侧（x+44）；纯显示不入 FIFO 队列；助战文案由“1 秒停留+0.5 秒淡出”改为浮动提示通道（BATCH-035 文案池沿用）。
23. 脱杆惩罚取消（BATCH-038）：难度等级 >0 时脱杆不再影响鱼的尺寸（原生 800ms 缩水计时器持续重置）与品质（按原生完美规则提升，≥2→铱、≥1→金）；等级收益照旧按脱杆 10/5/2/1 + round(调整后难度/50) 结算。
24. 尺寸修复（BATCH-038）：100 级狗鱼（O)144）只有 30 多厘米的根因是原生 BobberBar 脱杆 800ms 缩水累计 + 尺寸数字未乘等级倍率；难度等级 >0 禁用缩水，尺寸数字在 pullFishFromWater 结算边界统一乘倍率（正 +10%/级、负 -5%/级、0 级不乘），doPullFishFromWater Postfix 写回 fishSize，Farmer.caughtFish Prefix 同步收藏记录尺寸。
25. 万能鱼饵加成（BATCH-038）：难度等级 >0 且原生本应给两条鱼（numCaught≥2，非挑战鱼饵）时，最终数量在原基础上多给 10 条（2→12），再乘等级数量倍数。
26. 挑战鱼饵改版（BATCH-038，用户明确“5 分钟超时只取消给鱼数量的加成”）：调整后难度 >100 时生效；5 分钟内成功 → 数量 ×1.5（ceil）再乘等级倍数；超时只取消该加成，等级/普通皇冠/流动金色皇冠/品质/尺寸照常（例：30 分钟钓到 98 级仍给流动金皇冠）；挑战鱼饵下难度不随时间衰减、无其他鱼助战、原生“3 次脱杆失败”禁用；节点提示仍显示并追加 10 条随机文案（i18n hud.exhaust.append.{1..10}）。
27. 流动金色皇冠（BATCH-038）：难度等级 ≥95 的鱼在挑战鱼饵生效时成功（不区分是否超时）→ 该鱼皇冠升级为流动效果金色（Collections 页金/白呼吸 + 1±0.05 缩放脉冲）；新增存档字段 ChallengeCrowns（旧存档缺失默认空）；测试命令 fish_challengecrown。
28. 手感优化 + 太一/人鱼合一（BATCH-038）：α=1 定速最大值 30→21（-30%，30×(1-0.3α) 随 α 线性），保留无加速/阻尼/惯性、撞边完全钳制，叠加方向系数朝鱼 ×(1+0.2α)/背离 ×(1-0.2α)（α=1 → ×1.2/×0.8）；100 级称号混沌→太一（rank.taiyi）；满皇冠（α≥1）且史诗条件成立时失败提示改为“人鱼合一”（hud.fail.union），不再提示收集皇冠。

29. 持久战奖励与巅峰提示（BATCH-039，2026-08-10 用户确认；BATCH-052 修正物品 ID）：所有非鱼王小游戏战斗秒数统一累计；战斗到 30 秒整在“鱼其他提示”通道单发一次巅峰提示（10 套随机，i18n hud.peak.1~10）；失败且战斗 ≥60 秒必得海泡布丁 1 个（(O)265，钓鱼 +4）；30~60 秒失败 50% 概率随机 +3 料理（海之菜肴 (O)242 / 烩鱼汤 (O)728 / 龙虾浓汤 (O)730；原误标 228=生鱼寿司无加成，2026-08-12 Wiki 镜像核验修正）；60 秒不叠加 30 秒抽奖；奖励走失败单发边界（FailureRecorded 同分支）+ 原生溢出菜单，提示 20 条随机含鱼名+职阶（i18n hud.battleReward.1~20，FIFO 队列）；鱼王天然豁免；测试命令 fish_persisttest <30|60>。

30. 小游戏浮动提示间距（BATCH-041，2026-08-11 用户确认，比最初版本更好）：鱼动作提示在绿条右侧 3 个绿条宽处（左对齐，锚点 `xPositionOnScreen+208`）；鱼其他提示（助战/力竭/巅峰）在绿条左侧 3 个绿条宽处（右对齐，锚点 `xPositionOnScreen-44`）；绿条原生几何=左缘 `xPositionOnScreen+64`、宽 36px；起点固定、5 秒内上移 30px 线性淡出保持不变。

31. GMCM 清除功能（BATCH-042，2026-08-12 用户确认，参考 GCE GMCM 方案）：GMCM 新增“重置挑战数据”章节（警告段落 + 开关“准备重置挑战数据（两步确认）”待命），关闭菜单后由原生问题对话框二次确认（确认重置/取消；BATCH-044 移除 5 秒超时），确认后回到刚安装状态（清当前玩家难度/成功失败/连续失败/收藏星标/挑战皇冠，删除旧存档级键 `FishDifficultyData`）；不重置 config、不影响背包/钓鱼等级/原版图鉴/其他玩家；`fish_clear confirm` 与开关同一语义。

32. 传奇鱼皇冠回填（BATCH-043，2026-08-12 用户确认）：SaveLoaded 时扫描本地玩家 `farmer.fishCaught` 中 5 条原版传奇（`(O)159/160/163/682/775`，value[0]>0），给对应玩家补发皇冠并写回；幂等（已有皇冠跳过、无变化不写）；鱼塘不计（原生 fishCaught 不含鱼塘）；重置后立即按图鉴记录回填传奇皇冠（BATCH-046，普通星标不恢复）。

33. 重置确认加固（BATCH-044，2026-08-12 用户确认“修复，去掉超时”）：移除 5 秒超时（确认框保持打开直到明确选择）；记录发起重置的玩家 ID，回调使用回答者 `who` 并校验一致，不一致忽略（防分屏/共享地点串清）。

34. 控制台多玩家目标（BATCH-045，2026-08-12 用户确认）：数据/查询命令支持可选玩家序号首参——`fish_addstars 20`=主机、`fish_addstars 1 20`=主机、`fish_addstars 2 20`=副机，以此类推（顺序=`Game1.getAllFarmers()`，含离线农场客；省略=当前玩家）；覆盖 setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus/addstars/challengecrown 共 11 个命令；`fish_list`/`fish_bonus` 支持单独序号；`fish_assist`/`fish_persisttest`/`fish_selftest`/`fish_assiststats` 保持当前玩家语义。

35. `fish_addstars` 支持传奇皇冠（BATCH-047，2026-08-12 用户确认并授权部署）：测试皇冠池从“56 普通”扩为完整 61 可计数鱼池（56 普通 + 5 原版传奇）；`fish_addstars 61` 可达 α=100%；已存在皇冠跳过；玩家序号约定不变。

36. 重置后立即回填传奇皇冠（BATCH-046 方案 A，2026-08-12 用户确认）：`ClearAllData` 清空三集合后立即调用 `BackfillLegendaryCrowns(player)` 再统一写回；重置后同会话内传奇皇冠即恢复，满皇冠史诗失败正常显示“人鱼合一”；普通星标/挑战皇冠不恢复。

37. 100 级流动皇冠 1.2 倍 + 皇冠图层（BATCH-048，2026-08-12 用户确认）：新增存档字段 `Level100FlowCrowns`（挑战开始时难度等级≥100 + 挑战鱼饵成功时记录，旧存档默认空）；图鉴皇冠改为跟随鱼图标同批绘制（layer=图标+0.01），位于悬停详情 UI 与鼠标之下；100 级流动皇冠基础尺寸 ×1.2。

38. 89 级以上每次成功最多 +3 级（BATCH-049，2026-08-12 用户确认；BATCH-050 修正入口）：`RecordSuccess` 在旧等级 ≥89（含 89）时 `allowedGain = min(allowedGain, 3)`，仍受 100 封顶；88 级及以下最多只能升到 89（超高难挑战区间入口，不得跳过）。

39. 89 级超高难入口修正（BATCH-050，2026-08-12 用户修正）：`oldLevel < 89` 时 `allowedGain = min(allowedGain, max(0, 89 - oldLevel))`——88→89（+1）、87→89（+2）；89 级以上保持每次最多 +3。

40. 连续失败逃跑减速（BATCH-051，2026-08-12 用户确认；BATCH-055 加阈值滞回；BATCH-057 修订挑战鱼饵参与）：调整后难度 >100 的鱼按鱼种累计连续失败；蓄力槽低区逃跑减速从原倍率向“-10 级等效倍率”线性插值（0→5 次，5 次=完全等效 -10 级：≤20%×0.60、≤1%×0.20）；成功清零；鱼王豁免（挑战鱼饵同样参与，95 级以上也吃，BATCH-057 用户指令）；不改存档。BATCH-055：1%/20%/40% 阈值边界加 0.5% 死区（未生效用进度+0.5%、已生效用进度−0.5% 计算），消除生效/解除日志逐帧横跳刷屏。
41. 持久战料理 ID 修正 + 手感/高难运动诊断（BATCH-052，2026-08-12）：30~60 秒 +3 料理池 `(O)228` 修正为 `(O)242`（Wiki 镜像核验：228=生鱼寿司/Maki Roll 无钓鱼加成，242=海之菜肴/Dish O' The Sea +3；728/730 不变）；新增“手感系统激活”每实例 1 条诊断（验证 `ApplyBarInput` Transpiler 注入是否运行时命中）；构造日志追加实际加速度增幅倍数与跳鱼间隔（验证 96/97 与 ≥98 级约 ×2.8 vs ×19 断层）；手感与 96/97 运动根因未证实，仅诊断不改行为。
42. 加速度增幅锚点曲线（BATCH-053，2026-08-12 用户定稿并授权部署）：档位锚点 0→10%、50→20%、70→40%、80→70%、90→100%、100→100%，点间线性；≤0 钳 10%、≥90 钳 100%；`GetAccelerationTier` 纯函数 + `GetAccelerationBoost` 接入；`fish_selftest` 增锚点自测；构造日志 `加速增幅档` 改按曲线百分比输出；目的=消除 96/97（×2.8）与 98+（×19）的断层。
43. 提示字体/颜色 + 手感方向系数增强（BATCH-054，2026-08-12 用户指令）：小游戏浮动提示两通道字体放大 30%（缩放 1.3）；鱼行动提示=白字宝蓝色边（RoyalBlue）、其他提示=白字深红色边（DarkRed），8 向描边；手感方向系数 0.2→0.5（α=1 时朝鱼 ×1.5、背离 ×0.5，原 ×1.2/×0.8）。
44. 阈值滞回修复日志刷屏（BATCH-055，2026-08-12 用户确认）：蓄力槽保护与逃逸减速加成的阈值比较（1%/20%/40%）加 0.5% 死区——未生效时用 `进度+0.005`、已生效时用 `进度−0.005` 计算倍率，状态标志形成滞回；消除长战斗中“生效/解除”成对逐帧刷屏（本次会话 496+139 条日志证据）。
45. 皇冠图层修复 + 手感回调 + 钓鱼食物 buff 暂停 + 原生挑战星接管（BATCH-056，2026-08-12 用户确认）：普通皇冠补 `layerDepth+0.01f`（原无 layerDepth 默认 0.0f 被压在收藏页底层不可见）；α=1 基础定速 `30−5α=25`、方向系数回调 `±0.2α`（朝 ×1.2=30、离 ×0.8=20）；钓鱼小游戏（`Game1.activeClickableMenu is BobberBar`）期间 `id=="food"` 的 buff 不走时（Prefix 跳过 `Buff.update`）；原生挑战星 3 星接管——5:00 起每分钟掉 1 颗（5:00→2、6:00→1、7:00→0，不可恢复，难度等级 ≥95 豁免），每掉 1 颗鱼获 −20%（0.8/0.6/0.4，替换超时直接取消 ×1.5），原生计数保持 3 禁用脱杆失败，显示用原生空星贴图覆盖（Draw_Postfix）。
46. 停战休息 + 跳鱼前摇 + 挑战鱼饵背板（BATCH-058，2026-08-13 用户确认）：①绿条 3 秒不动 → 鱼下一次出绿条外 5px 停下并缓慢摇头摆尾（旋转、一秒一次），期间蓄力槽不掉、战斗计时暂停，10 套休战文案（hud.idle.1~10）；②鱼跃/甩尾在 0.5s 延迟后追加 0.88s 前摇（0.77s 旋转 70°+0.11s 转回，上跳逆时针/下跳顺时针，BATCH-058F 曾为 150°、BATCH-058T 改 70°）再瞬移（原生鱼图标旋转注入，不新增第二条鱼）；③挑战鱼饵失败不掉等级（连续失败照常 +1），同鱼同等级行为由固定种子决定（钓起前一致），成功钓起后清除种子重新随机；种子存 `FishDifficultyData.ChallengePatternSeeds`（键=鱼ID|等级）。
47. 称号改名与文档一致性修正（BATCH-058J，2026-08-14 用户指令）：89-99 级称号由“创世神”改为“龙神王”（i18n rank.creator 中英同步）；文档层一致性：手感正文统一为 α=1 定速 25px/帧（30−5α）、加速增幅英文镜像改为锚点曲线、挑战鱼饵超时规则中文与英文镜像改为掉星 −20% 体系、品质表 100 级改为“铱星（15 级起封顶）”。
48. 高难鱼招式短语（BATCH-059，2026-08-14 用户确认）：调整后难度 ≥150 的非鱼王鱼——瞬移后第一次到达中线（266±10，只判到达不判穿越、不设超时）为短语起点，下一次瞬移后再到中线为终点；该段轨迹（含瞬移与前摇视觉）循环回放，冻结原生运动；蓄力进度每净涨 1/3（绝对值 1/3、2/3、1，只前进）切换到新短语；蓄力槽/逃逸减速/脱杆/挑战星/力竭照常，只接管鱼位置与跳鱼时机。
49. 超长提示拆续集（BATCH-058L/M，2026-08-14 用户确认）：提示最多 3 行；超长内容拆成“第一段 + 续集”，续集在上一段 5 秒结束后 +0.2 秒排队显示（`PendingDelay`），非结尾段加“…”，不丢内容；`SplitTipChunks` 依赖既有 `WrapTipText`。BATCH-058L 已部署（DLL `02A3CB1D17ADA3DC9C846BF5908498BB4619661C50191C1729A12C110C88AF31`，14:58）；BATCH-058M 已构建并部署（DLL `18AD4D02D4C8CD4BBE681AA7D94EC3F67C74FC72DAB3CEA7DE3F3E2C137158AD`，0 警告 0 错误，2026-08-14 15:31；备份 `DeploymentBackups\FishingExpanded-20260814-152859-pre-BATCH058M`=02A3CB1D...；源/目标哈希一致；config.json 未触碰）。
50. 提示锚点用户定稿（BATCH-058N，2026-08-14 用户定稿）：鱼动作提示左缘距绿条右缘固定 24px、左对齐（锚点 x+124）；鱼其他提示右缘距绿条左缘固定 50px、右对齐（锚点 x+14）；上下位置不变（动作=触发时鱼上方 30px，其他=绿条中部）。已部署（DLL `E0A1A104825395531AD12AADEF7ED0037F50019250DCCAAD745333C2BCE11D56`，0 警告 0 错误，15:48；备份 `DeploymentBackups\FishingExpanded-20260814-154826-pre-BATCH058N`=18AD4D02...；源/目标哈希一致；config.json 未触碰）；待真实验收。
51. 提示屏幕边缘钳制根因与侧翻（BATCH-058O，2026-08-14 用户反证）：原生钓鱼条可停靠 `[0, viewport.Width−96]` 任意位置；动作提示锚点=绿条右缘+24px，钓鱼条靠右时 `StartX` 可超屏，`DrawTip` 的 `Math.Min(x, viewport.Width−8−maxWidth)` 把文字推到屏幕最右缘——这就是“提示非常靠右”的独立原因；原生鱼图标遇到同场景会翻到绿条左侧，我们缺这步。修复：原定一侧放不下时翻到绿条另一侧（动作→左侧右缘距绿条左缘 50px；其他→右侧左缘距绿条右缘 24px），保留 8px 屏幕边距兜底；新增一次性诊断 `TipSideFlip:<TipId>`（每提示最多 1 条，记录类型/barX/StartX/maxWidth/finalX/viewportW）。已构建部署（DLL `E2F7FBB7904A2FF48099D117DFBFB1FAB70479DBA0D11844048FC4456B43EED4`，0 警告 0 错误，15:53；备份 `DeploymentBackups\FishingExpanded-20260814-155340-pre-BATCH058O`=E0A1A104...；config.json 未触碰）；待真实验收。
52. 提示位置第二次反证与坐标诊断（BATCH-058P，2026-08-14）：BATCH-058O 实测（SMAPI 日志 15:55:57 实例 41313174，两次高难度鱼跳）无任何 `TipSideFlip`，侧翻分支未命中，钳制假设不足以解释“非常靠右”；已部署修复被推翻次数=2，按治理停止行为补丁，仅增加诊断——`钓鱼小游戏开始` 记录 barX/barY/viewportW（每实例 1 条），`DrawTip` 每条提示最多 1 条 `TipDraw:<Id>`（类型/barX/startX/maxWidth/finalX/viewportW/flipped）；按 2026-08-14 治理更新登记高级复核门禁（原 xhigh 机制废除）。诊断版已部署（DLL `912D7CCE3CD9F099E2513A34C323D7CE6257651AC91D66CAA983E11FF75525F8`，0 警告 0 错误，16:01；备份 `DeploymentBackups\FishingExpanded-20260814-160143-pre-BATCH058P`=E2F7FBB7...；config.json 未触碰）；等待玩家复现并返回日志+截图。
53. **提示位置根因证实与修复（BATCH-058Q，2026-08-14）**：玩家反馈“字体在屏幕最右侧和下侧”后取证——原生 `BobberBar.draw` 开头 `StartWorldDrawInUI` 切世界 render target（screen buffer/viewport 物理像素系）、结尾 `EndWorldDrawInUI` 恢复 UI render target（uiScreen/uiViewport 逻辑系），`Draw_Postfix` 在 UI 系绘制但提示几何是世界系；uiScreen 以 `options.uiScale` 拉伸（Game1.decompiled.cs:14410）；用户设置 zoomLevel=1/uiScale=-1（自动）→ 2560x1440 窗口 uiScale=2、uiViewport=1280x720 → 提示 x=1176 显示在屏幕 2352（超右缘）、y=907 显示在 1814（超底缘）。修复：`k=uiViewport/viewport`，DrawTip 全部几何+字体 scale+钳制（钳制边界改用 `Game1.uiViewport.Width`）、AddTip 分块宽度、挑战星覆盖位置/缩放全部换算到 UI 系；BATCH-041 验收通过是因为当时 uiScale=1（两系重合），窗口变大后暴露。高级复核门禁（取证/所有权审计/方案设计）完成并经主控复核。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `6ADD17EE936B490F7DD5FE60F66DA7FCC593F9C00F324BC0272A9CE3DE7C0B01`，反编译核验 `uiViewport` 换算/`starK`/`uiViewportW` 已编译）；2026-08-14 16:29 已部署（备份 `DeploymentBackups\FishingExpanded-20260814-162900-pre-BATCH058Q`=912D7CCE...，源/目标哈希一致，config.json 未触碰）；**用户实测验收通过（提示显示正确）**。
54. **背板模式开关（BATCH-058R，2026-08-14 用户指令）**：部分玩家喜欢完全随机；背板模式作为默认，config/GMCM 允许进入全随机。实现：`ModConfig.EnableChallengeBackboard=true`（默认=背板）；GMCM 布尔开关；构造边界 `HasChallengeBait && EnableChallengeBackboard` 才生成种子/PatternRandom；`ChallengePatternSeeds` 存档保留。Release Rebuild ✅（DLL `75356293...`）；18:14 已部署；**已被 BATCH-058S 改名替换**。
55. **全随机模式开关改名+默认关（BATCH-058S，2026-08-14 用户修订）**：开关改名"鱼的行为全随机模式（更难）"、默认关（默认保持背板）。`EnableChallengeBackboard` 整体替换为 `EnableRandomFishBehavior=false`（默认=背板；true=全随机更难，反编译核验无旧字段残留）；构造条件 `HasChallengeBait && !EnableRandomFishBehavior`；GMCM 键 `config.randomFishBehavior.name/tooltip`（中英）；旧 config.json 字段由 SMAPI 启动自动忽略并补新默认，行为不变。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `CEDFEBE6B5F79946C12F3C6D9994057C1F6175E52D677184300D75833A5692B9`，反编译核验通过）；2026-08-14 18:27 已部署（备份 `DeploymentBackups\FishingExpanded-20260814-182728-pre-BATCH058S`=75356293...，源/目标哈希一致，i18n×2 同步，config.json 未触碰）；待真实验收。
56. **前摇旋转峰值角 150°→70°（BATCH-058T，2026-08-15 用户指令）**：前摇改为"先转到 70° 再很快转回去"——0.88s 时序不变（0.77s 线性转到 ±70°、0.11s 快速转回 0°），上跳逆时针/下跳顺时针、红光脉动不变；旋转曲线拆为纯函数 `GetJumpWindupRotationAt`（供 `fish_selftest` 只读核验，BATCH-059 短语回放同一注入自动沿用）；顺带修正源码 3 处过期注释（0.22s/40°）。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `ED6D77452876DD179704154E21C633101B6924D381C3A612FF421146F38EB9DE`，反编译核验 70f 已编译）；2026-08-14 23:26 已部署（备份 `DeploymentBackups\FishingExpanded-20260814-232613-pre-BATCH058T`=CEDFEBE6...，源/目标哈希一致，config.json 未触碰）；待真实验收。
57. **力竭打断录播 + 跳鱼间隔动态分档（BATCH-059A，2026-08-15 用户确认 B+C 方案）**：①非挑战鱼饵下每到达力竭节点（1/3/5/7/9/12/15 分钟）短语状态机复位 Free 并清空全部短语标志（`InterruptPhrase`，蓄力换段进度只前进），鱼立即按已降难度原生运动，仍 ≥150 则下次跳鱼按新难度重录；挑战鱼饵（难度不衰减）不打断；复用"力竭节点"日志追加"短语打断"标记。②跳鱼间隔改为瞬移结算冷却时按当前有效难度重算（`GetJumpInterval(EffectiveDifficulty)`），力竭降档后下一跳变慢；<150 仍由跳鱼门槛整体停止。`fish_selftest` 增 `GetJumpInterval` 分档锚点（150/250→8、251→6、351→5、451→4、551→3）。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `6B79AE60DB69C784CB8520774880ADD7579BEF941C354757B877E23A20EED884`，反编译核验通过）；2026-08-15 09:50 已部署（备份 `DeploymentBackups\FishingExpanded-20260815-095046-pre-BATCH059A`=ED6D7745...，源/目标哈希一致，config.json 未触碰）；待真实验收。
58. **设计调整 7 项（BATCH-060，2026-08-15 用户逐条确认，见 `Governance\Active\BATCH-060-Design-Adjustments.md`）**：①89+ 每次成功最多 +6（原 +3；BATCH-049/050 入口规则保留：88 及以下最多升 89；89→95→100 两次成功）；②助战文案显示 15 秒（仅助战，其余提示 5 秒；`FloatingTip.LifetimeOverride`）；③经验倍数 = max(1, round(level×0.5))（10 级后经验流入原生精通系统）；④品质改门槛式"提升到"：10 银/25 金/50 铱，最终 = max(原品质, 门槛)（`GetQualityBonus` 退休 → `GetQualityTier`；非鱼 8 级无品质提升）；⑤GAME-DESIGN.md 与 WIKI.md 均新增术语表（WIKI 头注 BATCH-041 过期声明修正为 BATCH-060）；⑥挑战鱼饵掉星左下角 FIFO 提示（`HUDNotifier.ShowChallengeStarLoss`，i18n `hud.starLoss` 中英）；⑦数量倍数 = max(1, round(level×0.5))（100 级 ×50；万能 12×50=600、挑战 5 分钟 5×50=250 均 <999，A16 钳制丢鱼消失）；⑧巨型鱼连锁：门槛改难度等级 ≥8、视觉缩放改按等级（`1+level×0.0270843`，100 级 ≈3.7084 端点不变）、`fish_giant` 参数改等级、`FishDisplayData` 元组 multiplier→level。R0/R1/R2 完成；代码+文档（GAME-DESIGN/WIKI/TESTING-GUIDE）已改；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `DDC3AE79DD09D1997AA22C762172B66478184DCD0D745972AA84684973B5A62E`，反编译核验：89+ Math.Min(gain,6)、数量/经验 ×0.5 AwayFromZero、GetQualityTier(10/25/50)、缩放 0.0270843f、助战 LifetimeOverride 15f、ShowChallengeStarLoss/hud.starLoss、巨型鱼 level>=8 均编译进 DLL）；**2026-08-15 11:48 已部署**（用户授权；备份 `DeploymentBackups\FishingExpanded-20260815-114839-pre-BATCH060-058T`=6B79AE60...，源/目标哈希一致，i18n×2 同步含 058T-文案，config.json 未触碰；部署时游戏运行中，重启生效）；真实验收按集中测试顺序 6 项执行。
59. **图鉴文案修订 3 处（BATCH-058T-文案，2026-08-14/15 用户确认；与兄弟窗口"058T 前摇 70°"同名区分）**：①`collections.crownControl` 鱼竿熟练度+1→**鱼竿手感增强**/Rod Proficiency +1→**Improved rod handling**（机制名"鱼竿熟练度 α"保留，仅图鉴展示文案；消除 RPG 术语与不可见数值"+1"）；②`collections.challengeRank` 挑战等级：{称号}（等级{X}）→**（{X}级）**/Challenge Rank: ... (Lv X)（消除"等级"重复）；③`hud.fail.bottom` 中文补连词（"请升级鱼竿和钓鱼等级……再配上陷阱/软木塞浮标等"）。纯 i18n+文档修改（GAME-DESIGN 4.4/TESTING-GUIDE/TESTING/WIKI/本总账门禁 19），无运行行为变化；随 BATCH-060 统一候选部署（i18n×2 已同步安装目录并核验）；待真实验收（图鉴皇冠鱼显示"鱼竿手感增强"）。
60. **皇冠进度文案 + 非鱼类星星 + 每日收获限额（BATCH-061，2026-08-15 用户确认，见 `Governance\Active\BATCH-061-CrownProgress-Stars-DailyLimit.md`）**：①图鉴皇冠行改"鱼竿手感增强({{crownCount}}/{{crownTarget}})"（Mod 鱼皇冠也显示同一全局进度；非鱼类显示 `collections.noCrownControl`"无手感增强"）；②非鱼类（垃圾/藻类，IsNonFish 判定）难度等级达到上限 8 级 → `CollectionStars` 获星星（幂等；不计入 61 可计数/α/助战；图鉴图标画 mouseCursors_1_6 (236,205,19,19) 实心星，24/19 缩放；流动金色/×1.2 规则同皇冠但非鱼类达不到 95/100 天然不触发）；③每日收获限额：仅原版 61 可钓真鱼（CountableFishIds）不限；其他（每种 Mod 鱼、每种非鱼类）按物品 ID 单独每天最多 333 个+333 对应经验——CreateFish_Postfix 消费计数（>333 → Stack=0 + `HarvestLimited` 标志 + 每类每天首次 `hud.dailyLimit` 左下角提示），GainExperience_Prefix 读标志经验置 0；小游戏/难度等级/星星照常；内存态 DayStarted 重置，零存档字段。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `5EBE103C6E2CE3F6FF93DC7D32D1BEA01F65B84B5AF6FA8787D178E8D6B2597D`，反编译核验全符号编译进 DLL）；**2026-08-15 12:39 已部署**（备份 `DeploymentBackups\FishingExpanded-20260815-123938-pre-BATCH061`=DDC3AE79...，源/目标哈希一致，i18n×2 同步，config.json 未触碰；部署时游戏运行中，重启生效）；待真实验收（fish_selftest 限额 4 条 PASS + 真实钓鱼场景）。
61. **数量倍数改等级×1 + 星星素材迭代 + 停战计时核对（BATCH-062/063/064，2026-08-15 用户指令）**：①数量倍数 `GetQuantityMultiplier` = 难度等级×1（0/负=1 保底，100 级=100 条），经验 `GetExperienceMultiplier` 解耦保持 max(1, round(level×0.5))；②停战计时核对（通过，无需修改）：`!data.IsIdle` 期间 BattleElapsedSeconds 暂停，3 秒不动不计入战斗时间；③星星素材多轮迭代（用户目视裁判）：BATCH-062 mouseCursors (144,16,16,16)（"像鱼"）→ 063 (280,188,94,84)（"条状扁扁的"）→ 064 版本感知 → 064c (276,183,102,106)（"最接近，下面缺一点"）→ 064d (276,183,102,105) 高分/(196,131,57,57) 经典，20px（用户"看到星星了"）；④用户建议"用图鉴成就标签页的星星"→ 064e 诊断版实测（贴图 704x2256/uiScale 1.35）确认低分版成就 tab (656,80,16,16) 有效，程序标定星星本体 (659,83,13,10)；064f 部署后目视"边缘一圈脏东西"→ 064g 抠图后"矩形半透明白色"→ 064h padding 后"周围还是一圈白色半透明"。**根因已证实（2026-08-15 源码取证）**："白色半透明矩形"不是星星素材问题（064f/g/h 三次修改均无效），而是原生绘制——`CollectionsPage.draw`（full.cs:841）对 fishCaught 无记录的条目画 `Color.Black*0.2f` 半透明黑（图鉴浅色背景上呈白）；垃圾（trash_item）钓起不记录 fishCaught（`Farmer.caughtFish` decompiled.cs:2997 `!HasBaseTag("trash_item")`），故非鱼类条目永远 drawShadow=false。**方案设计完成（高级复核门禁）**：方案 A（推荐）=Draw_Postfix 非鱼类分支先用 Color.White 重画条目图标（layerDepth+0.005f）覆盖原生半透明白色，再画星星（+0.01f）；方案 B=临时改 name.drawShadow 位（不推荐）。待用户确认方案后实施。部署链：062 `B9FFB04F...` 13:22、062b `E5FA3110...` 14:23、063 `AE9EAA02...` 14:58、064 `5D7CE434...` 15:10、064c `C3EBD532...` 15:12、064d `482CE44C...` 15:34、064e `C2A477D1...` 15:53、064f `04775699...` 16:13、064g `9FF3E143...` 16:25、064h `83923C22...` 16:45（当前安装；均源/目标哈希一致、config.json 未触碰）；另有 `fish_giveitem [玩家序号] <物品ID> [品质0|1|2|4] [数量]` 命令（例：fish_giveitem 265 4 10 = 10 个铱星海泡布丁）。
62. **非鱼类/其他 Mod 鱼图鉴改星 + 星星画法回退早期金星（BATCH-064-2/3 + 064l，2026-08-15 用户指令）**：不触发钓鱼小游戏的条目与其他 Mod 鱼达到条件后不再画皇冠，改为早期图鉴金星——`Game1.mouseCursors` 源矩形 (346,392,8,8)，图标左上角 +3/+3 的 20×20 `Color.Gold`（`0020e30` 提交中 BATCH-029 改皇冠前的原画法）；皇冠仅保留给可计数 61 原生鱼（含原版 5 传奇）；星标条目描述显示"无手感增强"（`collections.noCrownControl`），皇冠条目显示"鱼竿手感增强(N/61)"；064l（用户指令"非鱼类图标透明化是自作主张，全部退回"）删除 Draw_Prefix 透明化与 064k 诊断代码，保留原生图标；最终部署 DLL `FCC6622006BF948A576CD727B1D707726A91C28721BAC3B12BC9CA113B37A0B8`（18:30，备份 pre-BATCH064l=AD0665B2...）。已同步 GAME-DESIGN/WIKI/TESTING/TESTING-GUIDE/BUG-LEDGER/活动卡。
63. **蟹笼收获纳入难度等级（BATCH-065，2026-08-15 用户指令，见 `Governance\Active\BATCH-065-CrabPot-Difficulty.md`）**：蟹笼里面拿到的物品也加难度等级，一律按水藻类（非鱼）规则处理——①每次收获 = 一次成功，等级 +1（上限 8，8 级给图鉴星星 + "无手感增强"，封顶"你已是帝王"）；②收获应用数量倍数（BATCH-062 公式=难度等级×1，8 级=×8，与原生书《Crabbing》25%×2 叠加=×16）；③左下角提示只在等级实际提升时显示（平时不刷屏）；④蟹笼鱼（Data/Fish 带 `trap` 标签、Category=-4：372/715-723 等）经 `DifficultyManager.IsCrabPotFish` 显式归入非鱼判定（IsNonFishItem/GetMaxDifficultyLevel/HUD 封顶文案统一单一所有者）；⑤新 Patch `CrabPotPatches`（`CrabPot.checkForAction` 收获唯一权威点：Prefix 应用倍数+登记待结算，Postfix 入包成功才 `RecordSuccess`+1；背包满收获取消不结算）；⑥经验保持原生固定 5 点、不实施每日限额（每笼每天 1 次，天然远低于 333，理由见活动卡）；⑦顺带同步 GAME-DESIGN 数量倍数公式到 BATCH-062（等级×1）与经验解耦措辞。fish_selftest 增 5 条蟹笼只读断言。Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `833A0812A16D73670AD9BBE9A6162A17FF656B7CE542500A56AAC5209072E2D0`，反编译核验全符号编译进 DLL，证据 `_analysis\batch065-verify`）；**未部署**（待用户授权）；待真实验收（fish_selftest 蟹笼 5 条 + 真实蟹笼收获：等级+1、8 级星标+帝王提示、×8 数量）。

### 当前维护门禁

- 历史 v0.5.2 记录包含旧构建/部署事实；当前源码 `0.5.10`。2026-08-06 用户实测反馈两项：图鉴加成显示错误（BATCH-025）、小游戏鱼标与结算面板示意图不应放大（BATCH-024 R0 范围修正）；当前安装已替换为修复候选 `2B0EB0F1...`（候选 `B8BDAEFA...` 部署后 coreclr 启动崩溃，二分定位到位置补偿注入的 `Add` 指令，修复为消费式 `AdjustLandingFishPosition`，详见 BATCH-024 卡“启动崩溃与二分定位”）；部署启动核验通过；2026-08-06 真实画面验收通过（钓鱼过程所有尺寸相关现象、图鉴逐鱼加成显示），日志封存 `RuntimeEvidence\20260806-RUNTIME-ACCEPT-01`（SHA256 7B231460...）；游戏 DLL `DFE341CA...` 未漂移；每个现象必须独立标记通过、失败或未执行。
- 当前优先验证：BATCH-026 线性缩放数值（0/25/50/100级）、视觉放大、原生 `CreateFish` 数量转换/ItemGrabMenu 溢出、单次结算、脱杆收益与区间封顶、0级全局蓄力保护、经验倍率、鱼王完整豁免、成功后星标/开局宣言、钓鱼等级建议和多人隔离。
- BATCH-029（2026-08-06 用户定稿 5 项修改并授权“完成所有代码修改并且部署”）：尺寸数字按等级线性（正 +10%/级、负 -5%/级，与数量倍数解耦）、连续失败 ≥2 次且调整后难度 ≥150 的史诗失败提示、成功额外 `round(调整后难度/50)` 等级、加速度增幅 30% 档（等级 ≥89 保持 100%）、收藏页星标改皇冠（Infinity Crown 贴图 (20,800,20,20)）；首次部署被启动推翻（Transpiler IL 非法）→ 修复版已部署（`26DEE6F2...`），真实验收待用户执行，五项各自独立标记。
- `Source\manifest.json` 与 `Source\FishingExpanded.csproj` 当前均为 `0.5.10`；旧批次版本号只作历史证据，不阻塞本轮部署。
- BATCH-030（2026-08-07 实测回归 + 设计变更，见 `Governance\Active\BATCH-030-FailureRecorded-RankWeakPrompt.md`）：BATCH-029 部署版被实测推翻——失败分支误删 `FailureRecorded=true`，一次失败被重复结算 18~51 次直接扣到底（虹鳟鱼/太阳鱼/狗鱼，其他电脑 D:\K1515 三份日志已封存 `RuntimeEvidence\20260807-BATCH029-REGRESSION-01`）；“稍微强一点的个体”不应出现在星标挑战宣言、零以下胜利应显示该弱称号提示（原 ≤0 跳过）；已排除存档不兼容（新字段缺失默认 0，两份存档加载正常）。修复版已构建并部署（DLL `108C3A2C...`，备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`），真实验收待用户执行，每现象独立标记。
- BATCH-031（2026-08-07 全量设计↔代码核对，见 `Governance\Archive-ReadOnly\BATCH-031-Design-Consistency-Check.md`）：全部规则与源码逐条一致；明确问题 2 处均为文档层——GAME-DESIGN 4.6 旧措辞 “隐藏钓鱼技能加成”改 “钓鱼条长度额外加成”、2.2 补失败提示优先级（史诗 > 触底 > 负等级 > 普通，与 `ShowFailureNotification` 一致）；同步 TESTING/MAINTENANCE-INDEX 措辞；无运行时代码/i18n 变化，部署核验为哈希一致性确认。
- BATCH-032（2026-08-08 用户确认新增，见 `Governance\Archive-ReadOnly\BATCH-032-HUDQueue-StarfruitTea.md`）：HUD 提示 FIFO 排队（全部四类+掉落消息排队、同屏同时一条、上限 5 丢最旧、按玩家隔离、原生消息不参与、回标题清空）；星之果茶掉落（难度等级 ≥50 成功钓起后概率 = 等级/4%，例 80 级 20%；掉落 1 瓶 `(O)StardropTea`，走 `CaughtFish_Postfix` 同一防重边界 + 原生溢出菜单；掉落消息 15 条中英文案随机并排队；鱼王/非鱼类豁免）。实现+构建完成（0 警告 0 错误），部署待用户指令，验收项每现象独立标记。
- BATCH-038（2026-08-10 用户多轮确认的钓鱼改版，见 `Governance\Archive-ReadOnly\BATCH-038-Exhaust-Tips-ChallengeBait.md`）：力竭机制（难度≥100 非鱼王，节点 1/3/5/7/9/12/15 分钟 1/3/10/20/35/50/100% 线性，15 分钟难度降到 80，奖励按开局快照不衰减；每节点 10 套诙谐文案进“鱼其他提示”）；提示双通道（动作/其他各 2 秒上移 30px 淡出、起点固定、上限 8）；脱杆惩罚取消（难度等级>0 尺寸+品质，等级照旧 10/5/2/1）；尺寸修复（狗鱼 100 级根因=原生 800ms 缩水，结算边界统一乘倍率并同步收藏记录）；万能鱼饵 +10（等级>0 且原生两条）；挑战鱼饵改版（难度>100，5 分钟 ×1.5 数量，超时只取消数量加成，难度不衰减、无助战、原生 3 次脱杆禁用、节点提示+10 条追加文案）；流动金色皇冠（≥95 级挑战鱼饵成功，新增 ChallengeCrowns 存档字段）；手感 α 优化（定速 -30% + 方向系数 ×1.2/×0.8 线性）；100 级太一 + 满皇冠人鱼合一史诗文案。实现+构建完成（0 警告 0 错误，DLL `9D0F1DF8...`，harness 26/26 + Transpiler TYPE SIM OK），2026-08-10 晚已部署并验收（DLL `E7F8E8D3...`，见 RuntimeEvidence\20260810-BATCH038-DEPLOY-ACCEPT-01，总账该行状态以部署事实为准），验收项每现象独立标记。
- BATCH-040（2026-08-11 用户要求，见 `Governance\Active\BATCH-040-Logging-Storm-Config.md`）：日志风暴治理——蓄力槽保护每帧 Debug 刷屏改“生效/解除”状态转换单发（每段 ≤2 条）；9 处每帧/高频路径兜底统一 `FishingLog.LogRateLimited`（同键 30 秒 ≤1 条、上限 64 键超限清空）；新增 `ModConfig.EnableLogging`（默认 true，config.json 自动读写）+ 可选 GMCM 开关；185 处直接 `Monitor` 调用全量迁移 `FishingLog.Log`；`fish_selftest` 增加日志门/限频缓存只读自测；实测 SMAPI-latest.txt（2026-08-11 09:57）12976 条 `[DIAG-FISH-VISUAL]` 分屏风暴（分屏 isLocal 翻转使状态去重失效），退休整条验收诊断（Game1Patches 类 + LogVisualDiagnostic/状态缓存 + 3 调用点 + 2 清理调用，FE-040-4）。R0/R1/R2 完成，Release Rebuild ✅（0 警告 0 错误，DLL `B0A517FD...` 含 BATCH-039 统一候选）；2026-08-11 10:18 已部署（Deploy:ROUND-20260811-01:B0A517FD...，备份 DeploymentBackups\FishingExpanded-20260811-101811-pre-BATCH040，源/目标哈希一致，未动 config.json）；2026-08-11 16:24 部署后实测发现 GMCM 接口映射报错（FE-040-5，Register 缺 titleScreenOnly），修复后重建重部署（Deploy:ROUND-20260811-01:70B9092E...，备份 DeploymentBackups\FishingExpanded-20260811-162854-pre-GMCM-fix）；真实验收待集中测试。

### 2026-08-06 实测反馈分诊

| Case | 玩家可见现象 | 唯一所有者 | 第一处分歧（已证实） | 批次 |
|---|---|---|---|---|
| FE-ISSUE-1 | 图鉴几乎所有鱼都显示“+0.5钓鱼等级” | `CollectionsPagePatches`（展示层） | `CreateDescription_Postfix` 显示全局累计加成 `GetFishingLevelBonus()` 而非每鱼星标 | BATCH-025（实测通过） |
| FE-ISSUE-2 | 小游戏鱼标与结算面板示意图被放大；只有从水里钓起举起的真鱼应放大 | `BobberBarPatches.draw`、`FishingRodPatches.draw`（绘制层） | BATCH-024 R0 按旧设计接入三条绘制路径缩放；用户确认小游戏鱼标与结算面板不缩放 | BATCH-024（实测通过） |
| FE-ISSUE-3 | 鱼视觉尺寸过大：100级≈5.85倍；要求100级=现25级大小且线性增大 | `DifficultyCalculator.GetVisualScale`（唯一公式所有者） | 立方根曲线增长过快；改为线性 1+0.0270843×等级 | BATCH-026（活动） |
| FE-ISSUE-4 | 双人同屏（本地分屏）：副屏玩家的难度/星标/加成/巨型鱼展示会读写主玩家数据（源码证实，未实测） | `DifficultyManager` 单例静态缓存不随分屏切换；BobberBar/CaughtFish/RecordGiantFish 记录入口 | Mod 静态缓存改为按玩家 UniqueMultiplayerID 键控隔离，全部数据路径显式传玩家 | BATCH-027（活动） |

- 两现象第一处分歧与唯一所有者不同，不合并；每个现象独立验收。
- 顺序：两个批次各自独立完成 R0/R1/R2 与 Release Rebuild；2026-08-06 真实画面验收均通过（每现象独立标记），日志封存 `RuntimeEvidence\20260806-RUNTIME-ACCEPT-01`。

### 当前活动 Case（BATCH-038，2026-08-10 更新）

- **任务**：用户多轮确认的钓鱼改版（详见 `Governance\Archive-ReadOnly\BATCH-038-Exhaust-Tips-ChallengeBait.md`）：①难度≥100 鱼力竭机制（节点 1/3/5/7/9/12/15 分钟 → 1/3/10/20/35/50/100%，线性，15 分钟难度降到 80，所有难度同时下降、奖励不下降；每节点“鱼其他提示”10 套诙谐文案）；②动作提示改 2 秒固定起点上移淡出；③助战等改“鱼其他提示”在钓鱼条另一侧；④修 100 级狗鱼尺寸；⑤脱杆惩罚取消=尺寸+品质（等级照旧）；⑥万能鱼饵 +10（难度等级>0 且原生两条）；⑦挑战鱼饵 5 分钟 +50%、超时只取消数量加成、难度不衰减、无助战、节点提示+10 条追加文案；⑧≥95 级挑战鱼饵成功给流动金色皇冠；⑨手感 α 优化（定速 -30%、朝鱼 ×1.2/背离 ×0.8）；⑩100 级称号混沌→太一；⑪满皇冠史诗失败改“人鱼合一”。
- **已证实（契约取证）**：原生 `BobberBar.update` 脱杆缩水 `fishSizeReductionTimer`（`_analysis/StardewValley.BobberBar.decompiled.cs:553-557`）；`doPullFishFromWater` 完美品质规则 `num2>=2→4、num2>=1→2`（1172-1176）；挑战鱼饵原生 `challengeBaitFishes=3`、成功 `numCaught=challengeBaitFishes`、脱杆减到 0 失败；狗鱼 (O)144 本安装数据 min15/max60（`_analysis/Fish-data-extracted.txt:14`）；日志 4440 次脱杆后尺寸 15 → 38cm。
- **R0**：力竭纯函数（节点表/线性/80 目标）＋BobberBar Update 接入；FloatingTip 双通道（2 秒/30px/线性淡出/上限 8）；脱杆缩水禁用与品质完美化；结算尺寸统一与收藏同步；万能/挑战数量加成；挑战皇冠存档；流动皇冠绘制；手感定速与方向系数；太一/人鱼合一 i18n。
- **R1**：旧贴鱼跟随提示整体替换；助战文案并入其他提示通道；`rank.chaos` 删除；数量/品质/尺寸各一个权威边界，无残留双写。
- **R2**：已推演（力竭各节点/挑战共存、超时语义、脱杆边界、提示通道上限、α 手感、存档兼容、多人隔离、无逐帧日志）。
- **状态**：R0/R1/R2 完成；Release 构建通过（0 警告 0 错误，DLL `9D0F1DF871B8559A0248109F9FD5A2C1FC1F13B99EFDC2B81CED528EF03EDE1C`，134656 字节，2026-08-10 18:19；数学 harness 26/26；Transpiler TYPE SIM OK 1313 块）；**未部署**；真实加载与玩法实测待用户授权。
- **委派评估**：`Prohibited`（用户 2026-08-09 明确不再允许委派；未使用执行器）。
- **Closeout**：Git 基线 `cbe3ea2` 无暂存（未用 `git add -A`）；工作树含 BATCH-032~038 混合来源，未创建提交（未授权）。

### 当前活动 Case（BATCH-039，2026-08-10 更新）

- **任务**：用户一次提交 5 项玩法改版并逐项确认（详见 `Governance\Active\BATCH-039-Perseverance-Rewards-ProficiencyCurve.md`）：①持久战奖励（30 秒失败 50% 概率 +3 料理、60 秒失败必得海泡布丁，奖励提示 20 条含鱼名+职阶诙谐文案随机，FIFO 队列）；②绿条旁浮动提示统一 5 秒淡出；③30 秒整“鱼其他提示”单发巅峰提示（10 条随机）；④“钓鱼控制力”改名“鱼竿熟练度”（图鉴 i18n collections.crownControl 中英双语）；⑤α 曲线改皇冠数分段线性（0/5/10/20/30/61 → 0/2/7/20/35/100%，点间线性，超 61 钳 100%）。
- **已确认（用户 2026-08-10）**：30 秒池=海之菜肴/烩鱼汤/龙虾浓汤（不含海泡布丁）；60 秒只发海泡布丁不叠加抽奖；机制适用所有非鱼王鱼。
- **R0**：BobberBar 战斗计时扩为全量实例累计（力竭衰减仍限 ≥100）；失败单发边界发放奖励（`FailureRecorded` 同分支、`addItemByMenuIfNecessary`、单发防重）；30 秒巅峰提示单发；`FloatingTip.Lifetime` 2f→5f；`GetAlpha` 分段线性 + 纯函数 `GetAlphaFromCrowns`；i18n 中英 30 条新文案 + 改名；薄控制命令 `fish_persisttest <30|60>`（构造边界消费，鱼王豁免丢弃）。
- **R1**：`ExhaustionElapsedSeconds` 全量改名 `BattleElapsedSeconds`（无残留调用）；2 秒/1 秒+0.5 秒文档残留清理；旧“钓鱼控制力/手感进度”措辞替换（历史治理卡不改写）。
- **R2**：已推演（30/60 秒边界、成功不发放、鱼王豁免、难度 <100 照常计时、挑战鱼饵 300 秒语义不变、多人/分屏按实例 Owner、背包满溢出、α 锚点与中点、无逐帧日志）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `610760761F7C342194FF4FA47DEABDB1FF90AB838AA07BA2FDACB861F0D37E74`，反编译确认编译进 DLL）；**未部署**（未授权）；真实加载与玩法实测待用户授权。
- **委派评估**：`NotBeneficial`（单线程功能批次，主线程已掌握全部调用链，无外部执行器）。

### 当前活动 Case（BATCH-040，2026-08-11 更新）

- **任务**：用户要求排查日志风暴问题——除非必要不允许每帧日志（有意为之的有限每帧输出可保留，禁止疏忽性每帧日志），并提供 config（config.json + GMCM）关闭日志的选项；完成后直接部署（详见 `Governance\Active\BATCH-040-Logging-Storm-Config.md`）。
- **已证实**：源码 185 处 `.Log(` 全量盘点 + 安装 DLL `E7F8E8D3...`（BATCH-038 部署版）反编译核对——`BobberBarPatches.Update_Prefix`（小游戏每帧）中“蓄力槽保护触发”Debug 日志在保护生效期间随蓄力进度变化几乎每帧输出（`|old-combined|>0.01f` 每帧命中），为疏忽性每帧日志；每帧路径（update/draw/手持绘制）catch 兜底 Error/Warn 同样存在逐帧刷屏风险；`SMAPI-latest.txt` 中高频 `[DIAG-RUNTIME SNAPSHOT]` 前缀归属 GCE（LEISURE-09，每 10 游戏分钟），排除其他 Mod 归属。
- **R0**：蓄力槽保护日志改“生效/解除”状态转换单发（每段保护周期 ≤2 条）；9 处每帧/高频路径兜底统一 `FishingLog.LogRateLimited`（同键 30 秒 ≤1 条、上限 64 键超限清空）；新增 `FishingLog` 统一日志入口 + `ModConfig.EnableLogging`（默认 true，config.json 自动读写）+ 可选 GMCM 开关；185 处直接 `Monitor` 调用全量迁移 `FishingLog.Log`；`fish_selftest` 增加日志门/限频缓存只读自测。
- **R1**：直接 `Monitor.Log`/`ModEntry.ModMonitor.Log` 调用全量退休（残留检查仅命中 FishingLog 内部）；蓄力槽“触发”每帧日志与每帧 catch 直接输出退役。
- **R2**：已推演（保护段 ≤2 条、限频 30 秒/键、开关关闭后 0 条、GMCM 未安装不崩、config.json 缺失自动默认、多人/分屏无串写、限频缓存有界、性能最坏情况无字符串拼接）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `70B9092E85FEA225F49A890AAB5FFAB1D05ED61577568BC38808F63D84958004`，反编译确认 FishingLog/ModConfig/LogRateLimited/ProtectionEngaged 已编译、IGenericModConfigMenuApi 签名对齐 GMCM 1.16.0、Game1Patches/LogVisualDiagnostic 已移除；含 BATCH-039 统一候选）；2026-08-11 10:18 首部署（B0A517FD...）+ 16:24 实测发现 GMCM 报错后 16:29 纠错重部署（70B9092E...），均备份+源/目标哈希一致，未覆盖 config.json；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（日志治理为机械全量迁移 + 已证实根因，主线程直接完成，无外部执行器）。

### 当前活动 Case（BATCH-041，2026-08-11 更新）

- **任务**：用户实测小游戏浮动提示距离绿条太远（几个版本前距离正好），明确新规则：鱼动作提示在绿条右侧 3 个绿条宽处、鱼其他提示在绿条左侧 3 个绿条宽处，比最初版本更好（详见 `Governance\Active\BATCH-041-Tip-Anchors-Gap.md`）。
- **皇冠门槛确认（2026-08-12）**：用户明确保持 ≥120（“皇冠120”），不做 >100 修改；副屏小嘴鲈鱼此前无皇冠的解释成立（从 0 级升到 19 级的所有成功杆调整后难度均 <120，需在 ≥17 级成功钓起一杆后授予），该现象关闭。
- **已证实（反编译对比）**：BATCH-037（6C56968D）助战提示在绿条右侧 x+108（右缘+8px）、动作提示贴鱼 x+82 居中；BATCH-038（E7F8E8D3）起其他提示移到左侧 x+44（左缘-20px）、动作提示仍 x+82 居中，与当前 70B9092E 锚点一致；BATCH-039 仅把寿命 2s→5s，“起点固定”提示与上下移动绿条的视觉距离被拉大；原生绿条左缘 x+64、宽 36px。
- **R0**：锚点改为按绿条几何推导——其他提示 `OtherTipAnchorX=64-3×36=-44`（右对齐，文字右缘距绿条左缘 108px）、动作提示 `ActionTipAnchorX=64+36+3×36=208`（左对齐，文字左缘距绿条右缘 108px）；`FloatingTip` 增加 `RightAligned`，`DrawTip` 支持右对齐；4 处 `AddTip`（助战/力竭/巅峰→其他通道右对齐；跳鱼→动作通道新锚点左对齐）全部更新。
- **R1**：旧锚点常量 44f/82f 无残留；提示状态仍唯一写入者 BobberBarPatches 浮动提示通道。
- **R2**：已推演（长文案不遮鱼标、分屏各屏独立、多通道叠加右/左对齐、提示随实例释放、无逐帧日志、文案池/寿命/淡出不变）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `983B094AA9DF2BECD8B7E762C4DB6F7982C25D61AAFF4E49ACA4C99908EFF905`，反编译核验 -44/208/RightAligned 已编译）；2026-08-11 18:20 部署（备份 `DeploymentBackups\FishingExpanded-20260811-1820-pre-BATCH041`=70B9092E...，源/目标哈希一致，config.json 未触碰）；真实画面验收待集中测试（目视间距=3 绿条宽）。
- **委派评估**：`NotBeneficial`（单文件纯显示锚点调整，主线程已掌握调用链，无外部执行器）。

### 当前活动 Case（BATCH-042，2026-08-12 更新）

- **任务**：用户要求 GMCM 提供“清除”功能，作用=回到刚安装本模组的状态（玩家可从 0 开始挑战）；必须防误点设计并提示效果；用户补充参考 GCE 的 GMCM 方案（详见 `Governance\Active\BATCH-042-GMCM-Reset-Button.md`）。
- **已证实（源码 + 安装契约）**：存档唯一写入者=`DifficultyManager`（`Farmer.modData["FishingExpanded/FishDifficultyData"]` + 旧 SMAPI 存档键 `FishDifficultyData`）；现有 `fish_clear confirm` 只清 `FishStatistics`/`CollectionStars`，漏清 `ChallengeCrowns`，旧键全仓库只有 `ReadSaveData` 无删除；安装 GMCM 1.16.0 反编译契约无 `AddButton`；GCE 方案=章节+警告段落+布尔开关待命+原生问题对话框二次确认（`GrowingChildrenPerformanceFix/ModEntry.cs` 8570-8655）；SMAPI 官方文档确认 `WriteSaveData(key, null)` 删除条目。
- **R0**：`ClearAllData` 改全量重置（三集合 + 旧存档级键仅主玩家 + 立即写回）；GMCM 按 GCE 模式实现“重置挑战数据”章节（开关待命 + 问题对话框二次确认 + 5 秒自动回退）；`fish_clear confirm` 与开关同一语义。
- **R1**：无旧路径，只修正清理覆盖范围，写入者仍唯一。
- **R2**：已推演（单人/分屏/联机按当前玩家隔离、旧键仅主机删、超时回退、config/背包/等级/原版图鉴不动、无逐帧日志、沙箱验收）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `AD2C6ED40FE3553B7684ACD5F4B9459D727A5D44CE2B0537D116ED8F3F05E96F`，反编译核验 createQuestionDialogue/AddBoolOption(fieldId=reset-challenge-data)/ChallengeCrowns.Clear/WriteSaveData(null)；前候选 15A31224 自绘按钮版未部署作废）；**已部署 2026-08-12 11:26**（备份 `DeploymentBackups\FishingExpanded-20260812-112626-pre-BATCH042`=983B094A...，源/目标哈希一致，i18n 已同步，config.json 未触碰）；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单所有者存档清理 + 自绘按钮，主线程直接完成）。

### 当前活动 Case（BATCH-043，2026-08-12 更新）

- **任务**：用户确认 SaveLoaded 时扫描 `farmer.fishCaught` 中 5 个原版传奇 ID，给对应玩家补发皇冠并写回（装模组前已钓到也补）（详见 `Governance\Active\BATCH-043-LegendaryCrown-Backfill.md`）。
- **已证实（原生契约）**：`Farmer.fishCaught` 键=限定 ID（`(O)163`）、`value[0]`=累计钓获数、鱼塘不写入（`Farmer.caughtFish` 反编译 2995-3016）；`RecordLegendaryCatch` 只在实时钓获边界调用，全源码无 fishCaught 扫描。
- **R0**：`DifficultyManager.BackfillLegendaryCrowns`（幂等扫描本地玩家 fishCaught 5 传奇 ID → CollectionStars → 有变化才写回）；`OnSaveLoaded` 在 LoadData 后调用。
- **R1**：无旧路径，皇冠写入者仍唯一。
- **R2**：已推演（单人/分屏/联机各补各、鱼塘天然不计、重置后下次加载补回传奇皇冠、幂等、每次加载 5 次 TryGetValue）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `9D199BD1B81730D421052559C4271CE89B24EC623CA42847AEA7DFAFCAF7D608`，反编译核验 OnSaveLoaded→BackfillLegendaryCrowns、fishCaught.TryGetValue(限定ID)+value[0]>0、CollectionStars.Add 已编译）；**已部署 2026-08-12 12:21**（备份 `DeploymentBackups\FishingExpanded-20260812-122114-pre-BATCH043`=AD2C6ED4...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单所有者小迁移逻辑，主线程直接完成）。

### 当前活动 Case（BATCH-044，2026-08-12 更新）

- **任务**：用户实测“主机清理后副机似乎也归零”，取证后确认模组只清主机；但发现 5 秒超时未真正取消确认框、回调未校验发起玩家。用户明确“修复，去掉超时”（详见 `Governance\Active\BATCH-044-Reset-Confirm-Hardening.md`）。
- **已证实（日志 + 原生契约）**：SMAPI-latest.txt 12:35:17“自动取消”后 12:35:19 仍执行清空（仅主机 1419976689139663055）；12:40 副机 623430766237409787 从 8 级继续钓到 15 级证明未被清；`GameLocation.answerDialogue` 反编译=`afterQuestion(Game1.player, answer)`，`afterQuestion` 为地点实例字段。
- **R0**：移除 5 秒自愈超时与 `_resetSequenceStartTick`；记录 `_resetArmedPlayerId`；`OnResetSequenceAnswer` 用回答者 `who` 并校验，不一致忽略。
- **R1**：删除“自动取消”自愈分支与对应日志。
- **R2**：已推演（确认框保持打开、取消不执行、分屏他屏应答忽略、单人只清自己、标题换存档无残留）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `BDCE071FA936C5974DA22F00894C4F9224D6A9E5323DB6D3AF8E794DCA68A1F7`，反编译核验 `_resetArmedPlayerId` 校验、`ClearAllData(who)`、无超时/自动取消残留）；**已随 BATCH-047 统一部署（2026-08-12 13:16，DLL DE23813F...，备份 `DeploymentBackups\FishingExpanded-20260812-131648-pre-BATCH047`）**；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单文件小范围交互修复，主线程直接完成）。

### 当前活动 Case（BATCH-045，2026-08-12 更新）

- **任务**：用户要求控制台命令支持给存档其他玩家操作：`fish_addstars 20`=主机、`fish_addstars 1 20`=主机、`fish_addstars 2 20`=副机，以此类推（详见 `Governance\Active\BATCH-045-Console-PlayerIndex.md`）。
- **已证实（原生契约）**：`Game1.getAllFarmers()`=主机（MasterPlayer）+ 在线/离线农场客（`Enumerable.Repeat(MasterPlayer,1).Concat(getAllFarmhands())`，`Game1.decompiled.cs:10955-10959`）；11 个命令原全部硬编码 `Game1.player`。
- **R0**：新增 `TryParseTargetPlayer`（可选首参序号，1=主机，省略=当前玩家），接入 setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus/addstars/challengecrown；`fish_list`/`fish_bonus` 支持单独序号。
- **R1**：无旧路径，向后兼容。
- **R2**：已推演（单人/双人分屏/四人联机/离线农场客、鱼 ID≥128 无歧义、`fish_clear 2 confirm`、单序号命令、不适用命令保持原语义）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `260D2FFF88C04755C98BC8EC17BC5541A9CF2233E51203F33B96AD704D4A3EC5`，反编译核验 `TryParseTargetPlayer`+`Game1.getAllFarmers()`+11 命令接入）；**已随 BATCH-047 统一部署（2026-08-12 13:16，DLL DE23813F...）**；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单文件机械解析改造，主线程直接完成）。

### 当前活动 Case（BATCH-046，2026-08-12 更新）

- **现象**：满皇冠时史诗级鱼失败仍提示“收集更多皇冠”，未显示“人鱼合一”（详见 `Governance\Active\BATCH-046-Epic-FullCrown-Message.md`）。
- **已证实（运行日志）**：12:33 加载回填 5 传奇皇冠 → 12:35 重置清空（含传奇）→ 12:48 `可计数皇冠: 56/61 | 鱼竿熟练度: 90 %` → 13:00:29 `史诗提示: True` 仍走 `hud.fail.epic`；BATCH-030 重复结算未复发（每次失败仅 -1）。
- **根因**：`ClearAllData` 清掉传奇皇冠后，`BackfillLegendaryCrowns` 只在 SaveLoaded 执行，同会话内 α=90%，`GetAlpha>=1f` 不成立。
- **待确认方案**：A=重置后立即回填（推荐）；B=重置不清传奇皇冠；C=下调 union 阈值（不推荐）。
- **状态**：根因已证实；用户 2026-08-12 确认方案 A（重置后立即回填）；R0/R1/R2 完成；Release Rebuild ✅（DLL SHA256 `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3` 含 046+048+049）；**已部署 2026-08-12 14:51**（备份 `DeploymentBackups\FishingExpanded-20260812-145100-pre-BATCH049`=DE23813F...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单所有者小改动，主线程直接完成）。

### 当前活动 Case（BATCH-047，2026-08-12 更新）

- **任务**：用户明确 `fish_addstars 20` 应支持传奇皇冠（可计数 61 鱼池），并要求开始修改并部署（详见 `Governance\Active\BATCH-047-FishAddstars-Legendary.md`）。
- **已证实（源码）**：`BuildStarPool` 剔除 5 条原版传奇，`fish_addstars 61` 最多 56/61、α=90%。
- **R0**：测试池改为完整 `CountableFishIds`（61），`AddCollectionStarsForTesting` 不再跳过传奇；帮助/警告文案同步。
- **R2**：61 满、已存在跳过、玩家序号并存、Mod 鱼不入池。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`，反编译核验 BuildStarPool=完整 61 池）；**已部署 2026-08-12 13:16**（备份 `DeploymentBackups\FishingExpanded-20260812-131648-pre-BATCH047`=9D199BD1...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单文件小改动，主线程直接完成）。

### 当前活动 Case（BATCH-048，2026-08-12 更新）

- **任务**：用户要求 ①战胜 100 级鱼获得的流动皇冠尺寸 ×1.2；②皇冠图层位于鼠标与收藏页悬停详情 UI 之下（详见 `Governance\Active\BATCH-048-Level100Crown-Layer.md`）。
- **R0**：新增 `Level100FlowCrowns` 存档字段（挑战开始时难度等级≥100 记录）；皇冠绘制迁移到 `ClickableTextureComponent.draw` Postfix（同批 layer=图标+0.01），100 级 ×1.2；删除整页后置 layer 0.99f 旧 postfix。
- **R2**：100/99 级边界、悬停 UI/鼠标层级、旧存档兼容、普通皇冠不变。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（DLL `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`）；**已部署 2026-08-12 14:51**（备份 `DeploymentBackups\FishingExpanded-20260812-145100-pre-BATCH049`）；真实验收待集中测试。

### 当前活动 Case（BATCH-049，2026-08-12 更新）

- **任务**：用户新增规则：89 级以上（含 89）每次成功最多 +3 级难度等级（详见 `Governance\Active\BATCH-049-Level89-GainCap.md`）。
- **R0**：`RecordSuccess` 在 `oldLevel >= 89` 时 `allowedGain = min(allowedGain, 3)`。
- **R2**：89→92、90→93、97→100、99→100、88→89、87→89（BATCH-050 修正入口）。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（DLL `19683A6723E9EC07B8ABCC5A26059F3C90BE877C4DDEBEFD02F1B91BFD1BADA3`）；**已部署 2026-08-12 14:51**（备份 `DeploymentBackups\FishingExpanded-20260812-145100-pre-BATCH049`）；真实验收待集中测试。

### 当前活动 Case（BATCH-050，2026-08-12 更新）

- **任务**：用户修正 BATCH-049——88 级最多只能加到 89，89 级以上属于超高难挑战区间、每次最多 +3（详见 `Governance\Active\BATCH-050-Level89-EntryCap.md`）。
- **R0**：`RecordSuccess` 增加 `oldLevel < 89` 分支：`allowedGain = min(allowedGain, max(0, 89 - oldLevel))`。
- **R2**：88→89、87→89、80→89、89→92、97→100、99→100、低等级原区间不受影响。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `13D6752E0B26E52BDA24CC8B430E6B7C3ADCB9A3FB4EB5AC3D13E485F7AD276C`）；**已部署 2026-08-12 15:08**（备份 `DeploymentBackups\FishingExpanded-20260812-150831-pre-BATCH050`=19683A67...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试。
- **委派评估**：`NotBeneficial`（单文件小改动，主线程直接完成）。

### 当前活动 Case（BATCH-051，2026-08-12 更新）

- **任务**：用户新增机制——调整后难度 >100 的鱼按鱼种累计连续失败，鱼快逃跑时递增减速，第 5 次失败等效 -10 级，成功清零；口径复用现有蓄力槽保护（详见 `Governance\Active\BATCH-051-EscapeFailBonus.md`）。BATCH-057（2026-08-12 用户指令）：挑战鱼饵同样参与（95 级以上也吃），否则太难。
- **R0**：`DifficultyCalculator.GetEscapeFailBonusModifier(level, fails, progress)` 纯函数；`Update_Prefix` 接入（非挑战鱼饵且 AdjustedDifficulty>100 时替换 newModifier）；`EscapeBonusEngaged` 状态转换单发日志。
- **R2**：0/1/3/5 次线性倍率、>40% 无效果、挑战鱼饵/鱼王豁免、成功清零、力竭共存。
- **状态**：R0/R1/R2 完成；BATCH-057 修订完成（条件改为仅 `AdjustedDifficulty > 100f`）；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `4EF947D52B7C67357BDDB981661AD9A3CD10963EFA8E6CE3384BDBF535D480D0`，反编译核验）；**已部署 2026-08-12 22:22**（备份 `DeploymentBackups\FishingExpanded-20260812-222219-pre-BATCH057`=2A86D412...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试（含挑战鱼饵连续失败 5 次低条减速生效）。
- **委派评估**：`NotBeneficial`（单管线小改动，主线程直接完成）。

### 当前活动 Case（BATCH-052，2026-08-12 更新）

- **任务**：①30 秒持久战奖励实收“生鱼寿司”，应为 +3 料理（根因已证实：奖励池 `(O)228` 实为生鱼寿司/Maki Roll 无加成，海之菜肴真实 ID=242）；②绿条中间朝鱼移动加速无感（根因未证实，新增“手感系统激活”每实例 1 条诊断验证 Transpiler 注入）；③100 级掉到 96/97 级后鱼上下移动幅度/频率不足（根因未证实，疑似 `<98` 加速度档实际≈×2.8 vs `≥98`≈×19 断层；构造日志输出实际数值供用户决策）。证据：SMAPI-latest.txt:7797（34s→生鱼寿司）+ `D:\GGGGG\codex working space\StardewWikiMirror` 物品编号工具（228/242/728/730 映射）（详见 `Governance\Active\BATCH-052-ItemId-Feel-Movement-Diagnostics.md`）。
- **R0**：`PerseverancePlusThreeFoods` 228→242；`InstanceData.BarInputDiagnosticLogged` + `ApplyBarInput` 每实例首帧日志；构造日志追加实际增幅倍数/跳鱼间隔。
- **R2**：30 秒池出 242/728/730、60 秒池不变、鱼王/挑战豁免、诊断每实例 ≤1 条、多人按实例隔离、不写存档。
- **状态**：根因 1 已证实并修复；根因 2 机制已证实激活（2026-08-12 16:35 日志：`手感系统激活 | α: 100% | 方向系数: 1.20 | 目标速度: 25.2px/帧`，注入未命中假设排除，剩余为体感/数值）；根因 3 设计已确认并移交 BATCH-053（100 级 `加速度增幅: ×30.00` 已生效）；Release Rebuild ✅（DLL `FEB9BAC5...`）已部署；真实验收待集中测试（30 秒池物品、96/97 三档对比、fish_selftest）。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-053，2026-08-12 更新）

- **任务**：用户定稿加速度增幅锚点曲线——0 级 10%、50 级 20%、70 级 40%、80 级 70%、90 级 100%、100 级 100%，中间平滑处理，改完部署（详见 `Governance\Active\BATCH-053-Acceleration-Anchors.md`）。
- **R0**：新增 `GetAccelerationTier(int)` 锚点线性纯函数；`GetAccelerationBoost` 改调该函数；构造日志 `加速增幅档` 输出百分比；`fish_selftest` 增 8 项锚点自测。
- **R2**：锚点与中间值、96/97 与 98 无断层、难度≤100 恒 1、鱼王/力竭/挑战鱼饵豁免语义、多人按实例、无存档字段。
- **状态**：根因已证实（97→98 断层）；设计已确认；R0/R1/R2 完成；Release Rebuild ✅（DLL `5B00CD518C05333D7E5BD3D7C265154F810134822E731D381999EC151B9D1226`，反编译核验 GetAccelerationTier 六锚点分支）；**已部署 2026-08-12 16:27**（备份 `DeploymentBackups\FishingExpanded-20260812-162733-pre-BATCH053`=FEB9BAC5...，源/目标哈希一致，config.json 未触碰）；运行时证据：16:35:16 100 级 `加速增幅档: 100% | 加速度增幅: ×30.00` 已生效（`RuntimeEvidence\20260812-BATCH052-053-LOGCHECK-01`，SHA256 C19A62BD...）；真实验收待集中测试（fish_selftest 锚点 + 96/97/98 构造日志）。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-054，2026-08-12 更新）

- **任务**：用户指令——①小游戏浮动提示字体再大 30%；②鱼行动提示白字宝蓝边、其他提示白字深红边；③手感系统增强到 50%/150%（朝鱼 ×1.5、背离 ×0.5）（详见 `Governance\Active\BATCH-054-TipFont-Colors-FeelBoost.md`）。
- **R0**：`FloatingTip.IsActionTip` + `AddTip(actionTip)`；`DrawTip` 缩放 1.3 + 8 向描边（RoyalBlue/DarkRed）+ 白字；`GetDirectionFactor` 0.2→0.5。
- **R2**：α=0 原生不变、α=1 朝鱼 31.5px/帧/背离 10.5px/帧、两通道颜色互不混淆、多人按实例、无存档字段。
- **状态**：设计完成；R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `CD3836DEE731D6193291A9D03695D66742C7F35E9142321AD03FF3884C1B2269`，反编译核验 IsActionTip/RoyalBlue/DarkRed/1.3f/0.5f）；**已部署 2026-08-12 17:23**（备份 `DeploymentBackups\FishingExpanded-20260812-172356-pre-BATCH054`=5B00CD51...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试（提示颜色/字号画面 + 方向系数日志 1.50/0.50）。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-055，2026-08-12 更新）

- **任务**：用户确认加阈值滞回，修复长战斗日志刷屏（详见 `Governance\Active\BATCH-055-Hysteresis-LogStorm.md`）。
- **R0**：`Hysteresis=0.005f`；保护/逃逸各按自身 `Engaged` 状态取 `progress±ε` 计算倍率与状态比较；标志更新顺序不变。
- **R2**：40%/20%/1% 边界徘徊日志收敛、单向只各 1 条、死区保持上一状态、挑战鱼饵/鱼王豁免、多人按实例、无存档字段。
- **状态**：根因已证实（日志证据）；R0/R1/R2 完成；Release Rebuild ✅（0 警告 0 错误，DLL SHA256 `97E8CC75E914B2D0A7F42DBA83F540556167684A895831966D7161F8E8D86A38`，反编译核验 ±0.005 滞回）；**已部署 2026-08-12 17:28**（备份 `DeploymentBackups\FishingExpanded-20260812-172847-pre-BATCH055`=CD3836DE...，源/目标哈希一致，config.json 未触碰）；真实验收待集中测试（40% 边界徘徊 30 秒日志 ≤2 条）。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-056，2026-08-12 更新）

- **任务**：①普通皇冠完全不可见（根因=Draw 无 layerDepth 默认 0.0f 被压底层），修复为 `layerDepth+0.01f`；②α 最终速度改 25（`30−5α`）、方向系数回 ±0.2α；③提示距离方案 B（不改）；④钓鱼小游戏期间食物 buff 不走时（`Buff.update` Prefix，`id=="food" && Game1.activeClickableMenu is BobberBar`）；⑤原生挑战星接管（3 星，5:00 起每分钟掉 1 颗、不可恢复、≥95 豁免、每颗鱼获 −20%）。
- **R0**：普通皇冠带 layerDepth 重载；`ApplyBarInput.baseMaxSpeed=30−5α`；`GetDirectionFactor ±0.2α`；新增 `BuffPatches.Update_Prefix`；`GetChallengeStars/GetChallengeStarMultiplier` + Draw_Postfix 原生空星覆盖 + 结算惩罚。
- **R2**：皇冠层级（鱼图标上/详情下）、α=1 朝 30/离 20、食物 buff 钓鱼期间不变/钓鱼外正常、饮料与其他 buff 照常、挑战星 5:00/6:00/7:00 各掉 1 颗且不恢复、≥95 不掉、鱼获 0.8/0.6/0.4、多人按实例、无存档字段。
- **状态**：设计已确认；R0/R1/R2 完成；Release Rebuild ✅（DLL SHA256 `2A86D4129B98613C280265D5FCC7337FB6BB2F28F3B63A1862B0CE065886E905`，反编译核验）；**已部署 2026-08-12 22:15**（备份 `DeploymentBackups\FishingExpanded-20260812-221505-pre-BATCH056-crownfix`=4AF36431...，源/目标哈希一致，config.json 未触碰）；FE-056-1 反证 #1 根因已证实（收藏页 activeClickableMenu=GameMenu，原菜单门永不成立→皇冠从未绘制），已修复为 `CollectionsPage.draw` 前后置标志并部署；真实验收待集中测试（皇冠可见性/层级、手感、食物 buff、挑战星）。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-058，2026-08-13 更新）

- **任务**：①停战休息（绿条 3 秒不动→鱼出绿条外 5px 停下摇头摆尾，期间不掉蓄力槽、战斗暂停，10 套休战文案）；②鱼跃/甩尾前摇（0.5s 延迟后追加 0.88s 旋转前摇再瞬移，原生鱼图标旋转）；③挑战鱼饵失败不掉等级、同鱼同等级背板（钓起前固定，钓起后重新随机）（详见 `Governance\Active\BATCH-058-Idle-Windup-ChallengeBackboard.md`）。
- **R0**：InstanceData 挂机/前摇/种子字段；Prefix 换固定随机源+冻结+前摇；Postfix 恢复随机源+绿条检测+keepLevel；Draw 叠加摇头摆尾/前摇旋转；`RecordFailure(keepLevel)`；`GetOrCreate/ClearChallengePatternSeed`；i18n 10 条×2。
- **R2**：停战/恢复、前摇只限鱼跃甩尾、背板钓起前一致、失败不掉级连续失败照常、钓起后重新随机、非挑战鱼饵与鱼王不受影响、多人按玩家存档独立。
- **状态**：设计已确认；R0/R1/R2 完成；反证 #1/#2/#3 已修复并部署；BATCH-058L 已部署（DLL `02A3CB1D...`，14:58：3 行截断+省略号）；BATCH-058M 已部署（DLL `18AD4D02...`，15:31：长提示拆续集）；BATCH-058N 已部署（DLL `E0A1A104...`，15:48：用户定稿锚点）；BATCH-058O 已部署（DLL `E2F7FBB7...`，15:53：屏幕边缘侧翻）；BATCH-058P 诊断已部署（DLL `912D7CCE...`，16:01：记录每条提示坐标与 barX；备份 `DeploymentBackups\FishingExpanded-20260814-160143-pre-BATCH058P`；config.json 未触碰）；**BATCH-058Q 根因已证实**（提示/挑战星坐标世界系与 UI render target 错配，uiScale>1 时被拉伸到屏幕最右侧和下侧；证据=原生渲染链反编译+用户设置+16:08 日志对照），修复已构建（DLL `6ADD17EE...`，0 警告 0 错误，反编译核验通过），未部署。
- **委派评估**：`Prohibited`（用户明确禁止委派）。

### 当前活动 Case（BATCH-024，2026-08-06 更新）

- **现象**：用户实测小游戏鱼标被放大、结算面板内鱼名/尺寸示意图被放大；用户确认只有“胜利后从水里钓起、玩家举起的真鱼”才放大。
- **已证实（当前安装 DLL `DFE341CA...` 反编译核对）**：`BobberBar.draw` 鱼标原生 `(10,10)` 中心原点、`scale=2f`；`FishingRod.draw` 结算面板示意图（第一处鱼图 `Vector2.Zero`、`4f`）与真鱼图（第二处/原生多鱼图 `(8,8)`、`3f`）；`doPullFishFromWater` 飞行动画为原生 `TemporaryAnimatedSprite`，保持原生大小与数量。
- **第一处分歧**：BATCH-024 R0 把缩放接入小游戏鱼标与结算面板示意图，超出用户确认的“真鱼放大”范围。
- **本轮准入**：允许修改 `BobberBarPatches.cs`（删除小游戏鱼标缩放）、`FishingRodPatches.cs`（删除结算面板第一处鱼图缩放与位置补偿，保留第二处/多鱼真鱼缩放）以及对应设计、台账和测试文件；冻结：`ObjectPatches.cs` 手持缩放、`GiantFishManager` 展示事实、鱼获/数量/难度所有权不改。
- **可观测性决定**：删除 `stage=BobberBar.draw` 诊断（路径移除）；新增 `stage=FishingRod.fly` 同键低频记录；保留 `FishingRod.draw`（落地真鱼，底边中点）与手持路径记录；不逐帧输出。
- **自动化验收决定**：复用 `FISH-CLI-01` 全量 `Saves` 沙箱与真实原生钓鱼入口（鱼ID 142、倍数27）；成功：小游戏鱼标恒为原生 2f、结算面板与示意图不缩放、飞行动画约 3.00 倍且中心轨迹不漂移、落地真鱼约 3.00 倍且底边中点不漂移、手持底边中点；失败：小游戏或面板图被放大、飞鱼或落地鱼未放大、锚点偏移、日志刷屏。
- **状态**：R0/R1/R2 完成；候选 `B8BDAEFA...` 启动崩溃已二分定位并修复为 `2B0EB0F1...`，已部署、启动核验通过；2026-08-06 真实画面验收通过（钓鱼过程所有尺寸相关现象）。

### 当前活动 Case（BATCH-025，2026-08-06 更新）

- **现象**：用户实测收集页面几乎所有鱼都显示“+0.5钓鱼等级”，而不是只有已收藏星标的鱼。
- **已证实（源码核对）**：`CollectionsPagePatches.CreateDescription_Postfix` 原先显示全局累计 `GetFishingLevelBonus()`（星标鱼种数 × 0.5）到每一条鱼；`DifficultyManager.HasCollectionStar(fishId)` 已有按鱼判定能力。
- **第一处分歧**：图鉴展示层读取全局累计加成，未按当前鱼种星标独立展示。
- **本轮准入**：允许修改 `CollectionsPagePatches.cs` 描述追加路径及对应治理/测试文件；冻结：难度/星标写入所有权、经验结算、鱼王豁免边界不改。
- **可观测性决定**：图鉴路径不新增诊断；以真实验收打开 Collections 菜单逐鱼核对描述文本为判定依据。
- **自动化验收决定**：复用 `fish_addstar <id>` 与真实收藏品菜单路线（TESTING-GUIDE 3.5 场景4），全量 `Saves` 沙箱；成功：星标鱼显示 +0.5、未星标鱼不显示、鱼王豁免、无全局累计值；失败：任何未星标鱼显示加成、星标鱼不显示、出现全局累计值。
- **状态**：R0/R1/R2 完成；与 BATCH-024 同一 DLL（修复候选 `2B0EB0F1...`）已部署、启动核验通过；2026-08-06 真实画面验收通过（收集页逐鱼加成显示正常）。

### 当前活动 Case（BATCH-026，2026-08-06 更新）

- **现象**：玩家实测确认显示链路正常，但设计数值上鱼过大：要求 100 级鱼的视觉大小 = 现在 25 级的大小，改为线性增大，并用简单加减计算替代立方根。
- **已证实（源码核对）**：唯一公式所有者 `DifficultyCalculator.GetVisualScale`，旧实现 `Math.Pow(倍数, 1/3)`；25级倍数51→旧缩放≈3.7084，100级倍数200→旧缩放≈5.8480；调用点：飞行动画、落地真鱼、手持鱼（`FishingRodPatches`/`GiantFishManager`/`ObjectPatches`），小游戏鱼标与结算面板不经过该公式。
- **R0（新公式）**：`缩放 = 1 + (数量倍数-1) × 0.0136102`，即 0级=1.0、25级≈1.6805、100级≈3.7084（= 旧25级大小）；等价每级线性 +0.0270843。
- **本轮准入**：允许修改 `DifficultyCalculator.cs`（GetVisualScale 公式）、`GiantFishManager.cs`（日志复用公式）及对应设计/台账/测试文件；冻结：数量倍数、经验、品质、`fishSize` 数值、动画数量、锚点合同、鱼王豁免不改；不部署（用户明确先不部署）。
- **可观测性决定**：公式为纯函数，静态合同可完整证明，符合免除条件；现有三条低频视觉诊断直接输出新值，不新增诊断。
- **自动化验收决定**：复用 `FISH-CLI-01` 全量 `Saves` 沙箱与真实原生钓鱼入口；成功：25级≈1.68倍、100级≈3.71倍、0级/负数=1.0、锚点不漂移；失败：出现旧立方根值或锚点偏移。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（0警告0错误，产物 SHA256 `77A1937CC1B5726EDE00E26C6BF06572A6E8223DAF48B7B06484FD9F6EDD4C24`，反编译确认线性公式已编译）；不部署。

### 当前活动 Case（BATCH-027，2026-08-06 更新）

- **现象**：双人同屏副屏玩家的难度/星标/加成/巨型鱼展示会读写主玩家数据；用户要求必须支持所有双人模式。
- **已证实（当前安装 DLL 反编译 + 源码核对）**：分屏每位本地玩家独立 Game1 实例，`Game1.player`/菜单/HUD 静态字段随屏幕切换（`LocalMultiplayer` 静态变量 holder）；Mod 自身静态缓存不参与切换 → `DifficultyManager._data` 单例缓存只在主屏加载，副屏读写串到主玩家。`Farmer.modData` 继承自 `Character.modData` 且注册进 NetFields → 联机农场客数据同步进主机存档。
- **R0**：`DifficultyManager` 按玩家键控惰性缓存，全部方法显式传玩家；FishingLevel 加成与鱼王屏蔽按玩家；NPC 反应遍历所有本机玩家。
- **本轮准入**：允许修改 `DifficultyManager.cs`、`BobberBarPatches.cs`、`FishingRodPatches.cs`（CaughtFish/CreateFish 传玩家）、`FarmerFishingLevelPatches.cs`、`CollectionsPagePatches.cs`、`HUDNotifier.cs`、`GiantFishManager.cs`、`ModEntry.cs` 及治理/测试文件；冻结：存档键/JSON 结构、等级公式、区间封顶、鱼王豁免、锚点合同、数量/经验/品质规则不改。
- **可观测性决定**：成功/失败/星标/加载日志已带玩家 ID；不新增逐帧诊断。
- **自动化验收决定**：`fish_*` 命令 + FISH-CLI-01 沙箱做单人回归；双人同屏/联机真实运行验收待执行，每现象独立标记。
- **状态**：R0/R1/R2 完成；Release Rebuild ✅（产物 SHA256 `AC7B1408...`，与 BATCH-026 同一 DLL）；不部署；双人实测待执行。
- **委派评估**：`NotBeneficial`（源码重构 + 本机构建/反编译核验由主线程完成，未使用委派执行器）。
- **Closeout**：HEAD `8f799da` 无暂存（未用 `git add -A`）；Release Rebuild ✅ 重核 0 警告 0 错误，产物 SHA256 `AC7B1408...` 与卡一致；未创建提交（未授权、多批次混合来源）。
- **设计-代码审计（2026-08-06 用户要求全量核对）**：`GAME-DESIGN.md` 全部规则与源码逐条匹配（公式、称号表、越级限制、蓄力槽保护、锚点合同、多人隔离、文案池数量）；发现并修复 `TESTING-GUIDE.md` 旧公式残留（5级难度 1.25x→3.45x、80级数量 161x→160x、负/零级称号、附录称号表错位、27倍视觉缩放 3.00→线性 1.354、旧日志示例），源码无改动。
- **部署状态（2026-08-06）**：用户明确授权部署；进程检查发现 `StardewModdingAPI` 运行中（PID 27372，16:45 启动，DLL 被锁定），等待用户关闭游戏后执行备份/替换/哈希核验；部署目标 `D:\GGGGG\K1515\Mods\FishingExpanded`，当前安装 DLL `2B0EB0F1...`。
- **文案措辞与中英文核对（2026-08-06 用户要求）**：图鉴“钓鱼技能加成”改为“钓鱼条长度额外加成”；玩家可见文案（图鉴两行、封顶/触底、鱼王 10 条、NPC 100 条）全部 i18n 化；`default.json`/`zh.json` 键集合一致（35 键），数量匹配；新 DLL `14AB5960...`（0 警告 0 错误）待部署。
### 当前活动 Case（BATCH-029，2026-08-06）

- **现象/需求**：用户定稿 5 项修改：① 尺寸数字（非视觉）正等级每级 +10%、负等级每级 -5%；② 同一鱼种连续失败 ≥2 次且调整后难度 ≥150 → 失败提示改为“收集更多徽章（鱼类收集品页的皇冠），获得更多加成，再挑战史诗级强者吧”；③ 每次成功额外 `round(调整后难度/50)` 等级（例：难度500 → +10）；④ 调整后难度 >100 时加速度增幅降为现增幅的 30%（等级 ≥89 保持 100%）；⑤ 收藏页星标改皇冠。
- **已证实**：尺寸数字当前与数量倍数共用一个倍率（`BobberBar` 构造边界 `fishSize × quantityMultiplier`）；失败提示无连续失败概念；成功结算仅按脱杆 10/5/2/1；`GetAccelerationBoost` 为唯一增幅入口——首次部署后被真实启动推翻（反证 #1）：签名改 `(BobberBar, float)` 后注入序列必须同步加 `Dup`（`Ldfld` 消费注入的 this），否则 `PrepareMethod` JIT 抛 `InvalidProgramException`，详见卡片“实测推翻与修复”；原生无 UI 皇冠，XNB 取证确认 Infinity Crown 帽子贴图 `(20,800,20,20)` 为金色皇冠（LaurelWreathCrown 实为绿色月桂花环）。
- **状态**：R0/R1/R2 完成（见 `Governance/Active/BATCH-029-Size-Levels-EpicFail-Crown.md`）；Release Rebuild ✅；首次部署（`293F6DC0...`）被真实游戏启动推翻（BobberBar.update Transpiler IL 非法）→ 根因已证实、修复并二次部署（`26DEE6F2...`，2026-08-07 07:22）；真实验收待用户执行（五项各自独立标记通过/失败/未执行）。

- **BATCH-028 高难度机制与称号（2026-08-06 用户定稿设计并授权实现）**：高难度运动公式修正（>100 初始目标顶部、>150 换目标频率三处+dart 偏移按 150 封顶、加速度线性增幅无上限）+ 高难度鱼跳机制（150+ 按难度分档 8/6/5/4/3 秒、上下 25% 检测、0.5s 延迟瞬移对侧、冷却后重新计时、鱼王豁免）+ 称号 89-99 创世神/100 混沌（越级限制 神王→99、创世神→100、混沌→100）。Transpiler 注入点已用当前安装 IL 证据核验（5 注入+3 跳过+唯一增幅点）；`GAME-DESIGN.md` 同步更新（含原生代码对齐小节）；Release Rebuild ✅ 0 警告 0 错误；**已部署**（DLL `E0DA4DA2...`，2026-08-06 19:55，备份 `DeploymentBackups\FishingExpanded-20260806-195542-pre-BATCH024-028`）；启动与真实验收待用户执行。
- **部署（2026-08-06 19:55，用户授权“全部代码写完就部署”）**：BATCH-024/025/026/027/028 与文案 i18n 全部合入最新构建 `E0DA4DA2...` 一次部署（DLL/manifest/i18n×2；deps.json 哈希不变未动；部署目录无 config.json 未涉及）；备份 `DeploymentBackups\FishingExpanded-20260806-195542-pre-BATCH024-028`；目标哈希与源一致核验通过；未启动游戏（启动与验收由用户执行）。


### 当前活动 Case（BATCH-031，2026-08-07 更新）
### 当前活动 Case（BATCH-033，2026-08-08 更新）

- **任务**：用户一次提交 5 项确认修改（详见 `Governance\Active\BATCH-033-CrownBonus-NPCcm-BarEntry-FloatText.md`）：① NPC 惊叹与手持尺寸一致显示厘米（fishSize×2.54）；② 王冠加成 0.5→0.2（旧存档加载自动重算生效）；③ 难度等级 ≥80 且鱼在绿条外时，鱼自身随机运动不得进入绿条（玩家移条照常可抓、150+ 跳鱼瞬移不受影响、鱼王/非鱼类天然豁免）；④ 低难度鱼不显示挑战宣言（第一因已证实：BATCH-032 测试命令 fish_addstars 无难度门槛写星标，产生低难度鱼持有星标的非法状态，宣言忠实消费；用户新规则=宣言须同时满足难度等级≥1 且有星标）；⑤ 浮木→陷阱浮标/软木塞浮标文案。
- **第一因（FE-033-4，已证实）**：星标写入路径未统一执行 ≥120 门槛——BATCH-032 测试命令 fish_addstar/fish_addstars（RecordHighDifficulty 2 参重载 / AddCollectionStarsForTesting）可对任意低难度鱼直接写星标；真实路径有 adjustedDifficulty≥120 门槛（狗鱼原生 difficulty=60，等级 -10~0 时调整后 30~60，永远达不到 120）。日志证据：20:13 fish_addstars 20 → 20:23 起狗鱼（等级 0/-10）反复显示宣言。修复=宣言须难度等级≥1（HUDNotifier 展示门已改）。
- **已证实（契约取证）**：原生收藏页 Fish 分支已显示“最大尺寸”（`farmer.fishCaught[id][1]`×2.54 厘米，值=本 Mod 调整后尺寸）；BobberBar.update 原生契约（当前安装 DLL DFE341CA... 重新反编译）条顶=`bobberBarPos-32`、条内判定与底部兜底、位置推进 `bobberPosition += bobberSpeed + floaterSinkerAcceleration`；`DifficultyManager.LoadData` 加载时按星标数重算加成。
- **R0**：NPCDialogueGenerator 换算厘米（唯一展示所有者）；DifficultyManager/ModEntry/i18n 数值与文案 0.2；BobberBarPatches Prefix/Postfix 内实现绿条外钳制（不新增 Transpiler、不新增状态所有者）；浮标文案 i18n×4+设计文档。
- **R1**：无被替代旧路径（0.5→0.2 为同一字段重算，无第二写入者）。
- **R2**：绿条外钳制区分鱼自身位移（>0.5px）与玩家移条；跳鱼帧跳过；条内不受影响；鱼王/非鱼天然豁免；性能=无逐帧日志（状态迁移低频 Debug）；存档=无新字段；NPC 文案中英一致厘米。
- **状态**：R0/R1 完成（NPC 厘米、绿条钳制、0.2 数值与文案、浮标文案、宣言展示门全部完成）；Release Rebuild 通过（DLL 26F8A54B...，0 警告 0 错误）；R2 已推演；已部署（DLL 26F8A54B...，2026-08-08 22:32，备份 DeploymentBackups\FishingExpanded-20260808-223246-pre-BATCH033；PID 10556 经核实为僵尸进程后部署）。**FE-033-3 第 1 次反证（2026-08-08 用户实测）**：部署版把“取消运动”实现成“贴条边钳制”，鱼一直贴着绿条；根因=实现偏差（设计明确要求“只要会进入绿条就移动不生效”）。修正=判定命中时位置回到本帧开始处+取消朝向条的目标与速度+bobberInBar 同步 false（防误计脱杆）+状态迁移低频 Debug 日志；修正版 DLL 486C7239... 已部署（2026-08-08 22:46，备份 DeploymentBackups\FishingExpanded-20260808-224628-pre-BATCH033-fix1）；用户此前显式否决“每帧退回”并确认“会进条就移动不生效”，首版实现未遵循该语义，已记录为流程教训；其余 4 项仍待验收。

### 当前活动 Case（BATCH-032，2026-08-08 更新）

- **任务**：HUD 提示排队 + 星之果茶掉落（用户确认设计）。
- **已证实（契约取证）**：原生 `Game1.hudMessages` 每帧移除过期消息、同文案合并刷新 3500ms；星之果茶物品 ID `(O)StardropTea`；`Farmer.addItemByMenuIfNecessary` 满包走原生溢出菜单；成功结算唯一边界 `CaughtFish_Postfix`（`SuccessRecorded` 防重）。
- **R0**：`HUDNotifier` 统一入队（队列上限 5 丢最旧、按玩家隔离、活动文案跟踪）；掉落挂在 `CaughtFish_Postfix` 结算边界（同一防重，不建第二发放路径）；概率纯函数 `DifficultyCalculator.TryGetStarfruitTeaDrop`。
- **R1**：无被替代的旧路径；5 处 `addHUDMessage` 全部改为 `EnqueueMessage`，无第二写入者。
- **R2**：排队串行显示、上限丢最旧、双人同屏隔离、掉落概率（50 级 12.5%/80 级 20%/100 级 25%）、背包满溢出菜单、鱼王/非鱼类豁免、性能（驱动 4 次/秒 + 上限 5）、存档无新字段。
- **状态**：已构建并部署（DLL `9AE5B858...`，2026-08-08 19:43，备份 `DeploymentBackups\FishingExpanded-20260808-194306-pre-ADDSTARS`）；2026-08-08 用户实测：星之果茶掉落触发（21:13 #9）、挑战宣言多条正常（太阳鱼 100 级混沌）、`fish_addstars 20` 生效；排队/上限/分屏/满包/鱼王豁免等其余项仍待逐项验收。

- **任务**：用户要求 “再次核对所有游戏设计，看看是否对应代码，是否有鲁棒性。明确的问题修改并部署”。
- **已证实（源码逐条核对）**：难度/数量/经验/品质公式、尺寸数字与视觉线性缩放、锚点合同、高难度运动公式（>100 初始顶部、>150 三处换目标+dart 按 150 封顶、加速度 30%/100% 档）、跳鱼分档 8/6/5/4/3 秒与 0.5s 延迟/1s 检测/跳完重计时、星标皇冠与 ≥120 门槛、称号 89-99 创世神/100 混沌、鱼王豁免与 5 鱼 ID、非鱼类上限 8、存档兼容、弱称号场景，全部与设计一致。
- **明确问题（文档层）**：① GAME-DESIGN 4.6 “隐藏钓鱼技能加成”旧措辞 → “钓鱼条长度额外加成”（BATCH-025 用户确认措辞）；② GAME-DESIGN 2.2 补失败提示优先级：史诗 > 触底 > 负等级 > 普通（代码 `ShowFailureNotification` 已定序，日志 21:30 证据：-10 且史诗条件成立显示史诗）。
- **鲁棒性复核**：`PullFishFromWater_Prefix` float/int 回退合理、`DifficultyManager` clamp、`GiantFishManager` Category=-4，均无需修改。
- **状态**：核对完成；文档修改完成；无运行时代码/i18n 变化 → 不重建不重部署，核验已部署 DLL 哈希与源码一致性；BATCH-029/030 既有待实测项不受影响。
### 当前活动 Case（BATCH-034，2026-08-09 更新）

- **任务**：用户多轮确认的玩法改版（详见 `Governance\Active\BATCH-034-Feel-System-Rework.md`）：① 隐藏等级加成整体删除（Mod 鱼无任何加成只显示皇冠）；② 皇冠/手感进度 α=可计数皇冠÷61（56 普通+5 原版传奇；传奇一次钓获直接给皇冠）；③ 取消绿条外钳制（BATCH-033 机制整体退休）；④ 手感系统（终点：按下/松开瞬间定速 30px/帧、无加速度/阻尼/惯性、撞边完全钳制，0→61 线性分配）；⑤ 帧率解耦覆盖整个小游戏；⑥ 加速度增幅 <98→10%、≥98→100%；⑦ 跳鱼瞬移文案（鱼跃/甩尾各 15 条）。
- **已证实（契约取证）**：原生 `BobberBar.update`（当前安装 DLL 反编译 + IL 全量）唯一 num5 累加点 `bobberBarSpeed += num5`、两处撞边反弹 `(0f-speed)*2f/3f`、三处概率/两处漂移/平滑/两处积分点；61 鱼清单核对 `_analysis\Fish-data-extracted.txt`；原版 5 传奇 ID 159/160/163/682/775。
- **R0**：删隐藏等级链（FishDifficultyData/FarmerFishingLevelPatches/CollectionsPagePatches/DifficultyManager/i18n）；α 派生自 CollectionStars∩61 集合（无新持久字段）；`RecordLegendaryCatch` 挂成功传奇分支；Transpiler 注入 8 类点 + 跳鱼文案 draw Postfix。
- **Transpiler IL 排障**：两处注入栈序错误修复（ApplyBarInput：`ldarg.0; dup; dup; ldfld; ldloc; call`；ApplyBounce：整体替换反弹段）；CFG 类型模拟（1313 块）+ PatchAll/JIT 双验证通过（batch029 harness 增 IMonitor 桩）。
- **R1**：rg 无残留（FishingLevelBonus/GetFishingLevelBonus/IsInGreenBar/fishingBonus/GreenBar 全清）。
- **R2**：已推演（α=0 恒等、α=1 终点、帧率统计一致、多人隔离、存档无新字段、无逐帧日志）。
- **状态**：R0/R1/R2 完成；Release 构建通过（0 警告 0 错误，DLL `A06974D9...`）；**已部署 2026-08-09 10:10**（备份 `DeploymentBackups\FishingExpanded-20260809-101014-pre-BATCH034`，目标 DLL/i18n 源-目标哈希核验一致，目标目录无 config.json）；**真实加载核验通过（2026-08-09 10:16 用户授权启动**：初始化/补丁注册/存档加载/可计数皇冠 3/61 全正常，证据 `RuntimeEvidence\20260809-BATCH034-RUNTIME-ACCEPT-01\SMAPI-load-checkpoint.txt`，Saves 沙箱备份同目录）；玩法现象实测待用户实机执行，六 Case 独立标记。
- **委派评估**：`NotBeneficial`（Transpiler IL 排障需主线程类型模拟与裁决，未使用委派执行器）。

## 活动批次

| 批次ID | 根因 | 状态 | 创建时间 | R0 | R1 | R2 | 构建 | 部署 | 实测 |
|--------|------|------|----------|----|----|----|----|------|------|
| BATCH-001 | 初始实现 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-002 | 控制台命令 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | N/A | ✅ | ✅ | 待测 |
| BATCH-003 | Transpiler视觉缩放 | ❌ 误判 | 2024-08-04 | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ |
| BATCH-004 | 低端机器性能优化 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-005 | Bug修复与稳定性 | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-006 | Transpiler诊断与间接bug | ✅ 已完成 | 2024-08-04 | ✅ | N/A | ✅ | ✅ | ✅ | 待测 |
| BATCH-007 | Transpiler根因重新定位 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-008 | ObjectPatches栈序列错误 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-009 | 钓鱼动画显示多条鱼 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-010 | 脱杆次数影响等级增长 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-011 | 鱼图鉴显示增强（初次评估） | ⏹️ 推迟 | 2024-08-04 | ✅ | ⏹️ | ⏹️ | N/A | N/A | N/A |
| BATCH-012 | 100级封顶特殊提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-013 | -10级底部提示 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-014 | 传奇鱼豁免所有规则 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-015 | 非鱼类8级上限 | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-016 | 鱼图鉴显示增强（重新实施） | ✅ 已完成 | 2024-08-04 | ✅ | ✅ | ✅ | ✅ | ✅ | 待测 |
| BATCH-024 | 多人数据与鱼获生命周期修正、视觉路径范围修正（真鱼放大） | ✅ 实测通过（2026-08-06） | 2026-08-05 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| BATCH-025 | 图鉴“钓鱼技能加成”按鱼种独立显示（仅星标鱼 +0.5） | ✅ 实测通过（2026-08-06） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| BATCH-026 | 视觉缩放改线性（100级=原25级大小≈3.7084） | ✅ 已部署（E0DA4DA2 随 BATCH-028 一起） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-027 | 按玩家隔离，支持双人同屏与联机 | ✅ 已部署（E0DA4DA2 随 BATCH-028 一起） | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-028 | 高难度运动公式修正 + 高难度鱼跳机制 + 称号 89-99 创世神/100 混沌 | ✅ 已部署 | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-029 | 尺寸数字等级线性化 + 连续失败史诗提示 + 成功额外等级 + 加速度 30% 档 + 收藏页皇冠 | ✅ 已部署 | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-030 | 失败只记录一次回归（FailureRecorded）+ 弱称号文案场景修正 | ✅ 已部署 | 2026-08-07 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-031 | 全量设计↔代码核对（命名残留 + 失败提示优先级定稿，纯文档） | ✅ 文档修正 | 2026-08-07 | ✅ | ✅ | ✅ | N/A | 核验 | 不涉及 |
| BATCH-032 | HUD 提示排队（FIFO 上限5）+ 星之果茶掉落（≥50级 概率=等级/4%） | ✅ 已部署（9AE5B858...，2026-08-08 19:43） | 2026-08-08 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-033 | 王冠加成 0.2 + NPC 厘米 + 绿条外钳制 + 低难度宣言 + 浮标文案 | ✅ 已部署（486C7239 fix1，2026-08-08 22:46）；绿条外钳制由 BATCH-034 整体退休 | 2026-08-08 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-034 | 删隐藏等级 + 皇冠/α 手感 + 帧率解耦 + 98 阈值 + 跳鱼文案（绿条外钳制取消） | ✅ 已部署（A06974D9...，2026-08-09 10:10，备份 pre-BATCH034） | 2026-08-09 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-035 | 皇冠助战（临时钓鱼等级随鱼难度排位 0~40·绿条旁 20 条文案） | ✅ 已部署 fix4（CB0A2B69...，2026-08-09 14:46，备份 pre-BATCH035-fix4；harness 26/26 + CFG 1313 块 + PatchAll/JIT OK） | 2026-08-09 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-036 | 图鉴皇冠“钓鱼控制力+1”（加速度公式部分已按用户指示还原为 98 阈值档位） | 🚧 皇冠显示已实现未部署；加速度还原待构建核验 | 2026-08-09 | ✅ | ✅ | ✅ | ⏳ | ⏳ | ⏳ |
| BATCH-037 | 难度倍数曲线改分段线性（4级×1.2、20级×5、50级×20、100级×50，越往后加得越多） | ✅ 已部署（DLL 6C56968D...，2026-08-10 07:47，备份 pre-BATCH037；含 BATCH-036 皇冠显示） | 2026-08-10 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| BATCH-038 | 力竭机制 + 提示双通道 + 脱杆尺寸品质取消 + 尺寸修复 + 万能/挑战鱼饵 + 流动皇冠 + 手感优化 + 太一/人鱼合一 | 🚧 已实现+构建通过（DLL 9D0F1DF8...，2026-08-10 18:19，0 警告 0 错误，harness 26/26）；未部署，待用户授权 | 2026-08-10 | ✅ | ✅ | ✅ | ⏳ | ⏳ | ⏳ |
| BATCH-039 | 持久战奖励（30/60 秒）+ 30 秒巅峰提示 + 浮动提示 5 秒淡出 + 控制力改名鱼竿熟练度 + α 分段曲线 | 🚧 代码侧结束（R0/R1/R2 ✅，Release Rebuild ✅）；2026-08-11 用户授权随 BATCH-040 统一部署；真实验收待集中测试 | 2026-08-10 | ✅ | ✅ | ✅ | ✅ | ⏳ | ⏳ |
| BATCH-040 | 日志风暴治理（蓄力槽保护每帧刷屏清除 + 每帧兜底限频 + config/GMCM 日志开关 + 分屏 DIAG-FISH-VISUAL 诊断退休 + GMCM 接口修复） | 🚧 已部署（DLL 70B9092E...，含 BATCH-039；Deploy:ROUND-20260811-01:70B9092E...，FE-040-3 反证 1 次已同轮修复）；真实验收待集中测试 | 2026-08-11 | ✅ | ✅ | ✅ | ✅ | ⏳ | ⏳ |

---

## BATCH-024：多人数据与鱼获生命周期修正（启动修复待重新部署）

**新增准入证据（2026-08-05 19:07）**：用户提供的 SMAPI 日志显示上一份部署产物在 `Harmony.PatchAll` 阶段将 `CaughtFish_Postfix` 作为 `FishingRod` 类级 Patch 处理，查找不存在的 `FishingRod.caughtFish`，导致 FishingExpanded 初始化失败。当前源码已将该 Postfix 移到独立的 `[HarmonyPatch(typeof(Farmer))] FarmerFishingPatches`，编译产物反编译确认目标为 `Farmer.caughtFish`。

**当前门禁**：源码 Release Rebuild 已通过（0 警告、0 错误）；修复 DLL 尚未替换到目标安装，部署和启动日志核验待执行。允许修改范围仍限于 `Source/Patches/FishingRodPatches.cs` 与本批次状态台账。

**根因**：当前实现把数据写入存档级 `ReadSaveData`，并在 `Farmer.caughtFish` 时扫描背包；原生鱼获物品此时尚未创建，无法可靠覆盖堆叠和 `ItemGrabMenu` 溢出路径。

**R1 实施**：
- 使用玩家自己的 `Farmer.modData` 保存难度、星标和隐藏加成；仅主玩家迁移旧共享数据。
- 规范化原始/限定鱼 ID，统一鱼王识别和图鉴/命令数据键。
- 移除 `caughtFish` 背包扫描，改在原生 `FishingRod.CreateFish` 返回物品时转换数量。
- 将星标写入从 `BobberBar` 构造阶段移到成功捕获后的 `caughtFish` Postfix。
- 待处理鱼获按玩家 ID + 鱼 ID 隔离，返回标题时清理。
- 修正小游戏初始绿条外状态不计为脱杆；星标阈值在成功入口使用原生难度参数兜底。
- 在原生钓鱼经验入口应用经验倍率；鱼王 BobberBar 构造时屏蔽隐藏钓鱼等级读取，保持完整豁免。
- 加入星标鱼开局挑战宣言的展示与本地化随机池，不增加第二个状态所有者。
- 加入小游戏出现时的钓鱼等级建议；使用 `Farmer.FishingLevel` 的有效整数等级，只读比较，不写入玩家或鱼数据。

**R0/R1 状态**：R0 ✅；R1 ✅（当前源码静态检查与 Release Rebuild 已通过；上一候选部署因启动日志暴露 Patch 归属错误而作废）
**当前构建**：✅ `Source\FishingExpanded.csproj` Release Rebuild，0警告，0错误；版本 `0.5.10`；修复产物 SHA-256 待部署后登记
**上一候选部署**：已作废；目标曾与候选输出哈希一致，但用户启动日志证明该候选仍含旧的 `FishingRodPatches::CaughtFish_Postfix` 结构；旧备份见 `DeploymentBackups\FishingExpanded-20260805-183148`
**当前部署**：⏳ 待替换明确的 DLL、依赖、manifest 和 `i18n`，保留 `config.json` 不变
**实测**：待多人和原生溢出验收

## BATCH-016: 鱼图鉴显示增强（重新实施） ✅

**根因**：BATCH-011初次评估复杂度过高，用户纠正"仅仅改几个字而已"

**累计修复次数**：1次（初次实施）

**证据链**：
1. 反编译CollectionsPage.full.cs确认createDescription(string id)返回简单字符串
2. 用户反馈BATCH-011复杂度判断错误
3. 确认只需Postfix追加文本即可

**实现内容**：
- **R0取证**：反编译CollectionsPage，定位createDescription方法
- **R1实现**：CollectionsPagePatches.CreateDescription_Postfix
  - 检查currentTab==4（鱼类tab）
  - 豁免传奇鱼（BATCH-014规则）
  - 追加"挑战等级：{称号}（等级{level}） | 钓鱼技能加成：+{bonus}"
- **R1删除**：移除DifficultyManager中对已废弃CollectionsPagePatches.InvalidateCache()的调用

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\（2026-08-05 01:10）
**实测**：⏳ 待玩家验收图鉴显示功能

---

## BATCH-015: 非鱼类8级上限 ✅

**根因**：新需求 - 垃圾、藻类等非鱼物品难度上限8级

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：定义IsNonFish()检查Category != -4
- **R1实现**：
  - SpecialFishHelper.IsNonFish() - 基于Category判断
  - DifficultyManager.GetDifficultyLevel() - 应用maxLevel=8
  - HUDNotifier.ShowSuccessNotification() - 特殊文案"对于{物品名}而言，你已是帝王"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收非鱼类上限功能

---

## BATCH-014: 传奇鱼豁免所有规则 ✅

**根因**：新需求 - 5种传奇鱼（鱼王等）不受任何自定义规则影响

**累计修复次数**：1次（初次实施）

**证据链**：
1. 用户明确指定5种传奇鱼ID：163/682/160/775/159
2. 要求10种随机消息，不使用难度系统、等级加成

**实现内容**：
- **R0设计**：创建SpecialFishHelper统一管理特殊鱼规则
- **R1实现**：
  - SpecialFishHelper.IsLegendaryFish() - 硬编码5种鱼王ID
  - SpecialFishHelper.GetRandomLegendaryMessage() - 10种随机消息
  - BobberBarPatches.Constructor_Postfix - 最优先检查豁免
  - FishingRodPatches.Prefix - 最优先检查豁免
  - CollectionsPagePatches.CreateDescription_Postfix - 豁免图鉴显示

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收传奇鱼豁免功能

---

## BATCH-013: -10级底部提示 ✅

**根因**：新需求 - 难度-10时提供装备升级建议

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowFailureNotification()检测currentLevel<=-10
- **特殊文案**："最平庸的{鱼名}依然太难了，请升级鱼竿，钓鱼等级，使用料理增加钓鱼等级，使用陷阱/浮木渔具等"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收底部提示功能

---

## BATCH-012: 100级封顶特殊提示 ✅

**根因**：新需求 - 达到100级后显示神明提示

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R1实现**：HUDNotifier.ShowSuccessNotification()检测newLevel>=100
- **特殊文案**："你已经成为{鱼名}中的神明，这一刻你是鱼，也是人，更是王。"

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收封顶提示功能

---

## BATCH-011: 鱼图鉴显示增强（初次评估） ⏹️

**根因**：新需求 - 图鉴显示挑战等级和技能加成

**初次评估**：复杂度高，推迟到v0.6.0

**用户反馈**：评估错误，"仅仅改几个字而已"

**后续**：由BATCH-016重新实施

---

## BATCH-010: 脱杆次数影响等级增长 ✅

**根因**：新需求 - 完美钓鱼（0次脱杆）应获得更高奖励

**累计修复次数**：1次（初次实施）

**实现内容**：
- **R0设计**：BobberBar追踪bobberInBar状态变化计数
- **R1实现**：
  - BobberBarPatches.Update_Prefix - 追踪!___bobberInBar计数PerfectCount
  - BobberBarPatches.GetPerfectCount() - 公开查询接口
  - DifficultyManager.RecordSuccess() - 支持可变levelGain参数
  - FarmerFishingPatches.CaughtFish_Postfix - 根据脱杆次数调用RecordSuccess
- **奖励规则**：0次脱杆→+10，1次→+5，2次→+2，3次及以上→+1

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收脱杆奖励功能

---

## BATCH-009: 钓鱼动画显示多条鱼 ✅

**根因**：FishingRod.pullFishFromWater的numCaught参数同时控制动画和最终数量

**累计修复次数**：1次（初次修复）

**证据链**：
1. v0.5.0测试显示钓到大鱼时立即显示多条鱼飞向玩家
2. pullFishFromWater()的numCaught参数控制for循环生成多个临时精灵
3. 需要分离动画数量（固定1）和最终背包数量（倍数影响）

**实现内容**：
- **R0取证**：反编译FishingRod.pullFishFromWater()和Farmer.caughtFish()
- **R1重构**：
  - FishingRodPatches.Prefix - 仅记录数据到_pendingFish，不修改numCaught
  - FarmerFishingPatches（新类）- Postfix on Farmer.caughtFish()修改Stack属性
  - 字典访问权限从private改为internal static支持跨类访问
- **第一处分歧**：pullFishFromWater()的numCaught参数，保持=1避免动画重复

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收动画功能

---

## BATCH-008: ObjectPatches栈序列错误 ✅

**根因**：BATCH-007 Transpiler成功插入指令但GetDrawScale从未被调用

**累计修复次数**：2次（BATCH-007根本方案错误，BATCH-008修复栈序列）

**证据链**：
1. v0.5.0日志显示Transpiler匹配到ldc.r4 4但无"应用视觉缩放"记录
2. 代码审查发现yield return codes[i]在插入指令之后
3. IL栈错误：新指令压入后才有4f，导致乘法操作数不足

**实现内容**：
- **R0取证**：IL指令执行顺序分析
- **R1修复**：调整yield return顺序
  ```csharp
  // 错误：yield return codes[i]; 在最后
  // 正确：yield return codes[i]; 在if块内最先执行
  if (codes[i].opcode == OpCodes.Ldc_R4 && ...) {
      yield return codes[i];  // 先压入4f
      // 再插入GetDrawScale和Mul
  }
  ```

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-007: Transpiler根因重新定位 ✅

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待玩家验收视觉缩放功能

---

**根因**：BATCH-003假设了错误的调用链 - Farmer.draw()从不调用Item.drawInMenu()

**累计修复次数**：1次（BATCH-003误判为已完成）

**证据链**：
1. v0.4.3日志显示：`[Transpiler] IL分析完成 | 方法调用总数: 132 | drawInMenu调用: 0`
2. 反编译Farmer.draw()证实：调用链是 Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
3. Object.drawWhenHeld():5345行使用固定scale=4f，这才是唯一所有者

**实现内容**：
- **R0取证**：反编译Farmer.draw()、Game1.drawPlayerHeldObject()、Object.drawWhenHeld()
- **R1删除旧代码**：
  - ❌ FarmerPatches所有Transpiler代码（Draw_Transpiler等7个方法）
  - ❌ GiantFishManager中对FarmerPatches.InvalidateCache()的调用
- **R1新实现**：
  - ✅ ObjectPatches.DrawWhenHeld_Transpiler - 搜索并修改`ldc.r4 4`指令
  - ✅ ObjectPatches.GetDrawScale() - 计算鱼类视觉缩放

**R0状态**：✅ 已完成
- 反编译3个关键方法
- 定位唯一所有者：Object.drawWhenHeld()的scale=4f参数
- 证据保存到_analysis目录

**R1状态**：✅ 已完成
- 删除FarmerPatches全部无效代码
- 实现ObjectPatches正确Patch Object.drawWhenHeld()
- v0.5.0构建成功（2024-08-04 21:16）
- 部署到D:\GGGGG\K1515\Mods\FishingExpanded（2024-08-04 21:17）

**R2状态**：⏳ 待验证
- 基础功能：钓到倍数>15的鱼，手持时是否视觉放大
- 立方根缩放：倍数27应显示为3倍大小
- 非鱼类物品不受影响
- 多人模式独立缩放
- 性能：低频日志，无每帧计算

**构建**：✅ v0.5.0 - 0警告 0错误
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\ 
**实测**：⏳ 待玩家验收视觉缩放功能

---

## BATCH-006: Transpiler诊断与间接bug ✅

**根因**：日志显示Transpiler未找到目标调用，间接bug影响稳定性

**发现的问题**：
1. **Transpiler诊断不足** - 无法判断IL匹配失败的原因
2. **整数溢出风险** - DifficultyLevel计算可能溢出
3. **多人缓存冲突** - FarmerPatches的静态缓存在多人模式下可能冲突

**实现内容**：
- 增强Transpiler诊断：记录总指令数、方法调用数、目标调用数
- DifficultyLevel使用long运算避免溢出
- FarmerPatches缓存改为Dictionary<long, ...>按玩家ID隔离

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.3
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待验证（注：v0.5.0已删除FarmerPatches缓存）

---

## BATCH-005: Bug修复与稳定性 ✅

**根因**：代码审查发现5个潜在bug影响稳定性

**发现的Bug清单**：
1. **Bug #1 (严重)**: BobberBarPatches.PeriodicCleanup空引用检查逻辑错误
2. **Bug #2 (中等)**: HUDNotifier.GetFishDisplayName缺少空检查
3. **Bug #3 (轻微)**: NPCDialogueGenerator.GenerateFishPraise缺少Game1.random空检查
4. **Bug #4 (中等)**: CollectionsPagePatches反射调用缺少错误处理
5. **Bug #5 (轻微)**: FarmerPatches Transpiler搜索范围可能不足

**R0状态**：✅ 已完成
**R1状态**：N/A
**R2状态**：✅ 已完成
**构建**：✅ v0.4.2
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证稳定性提升

---

## BATCH-004: 低端机器性能优化 ✅

**根因**：每帧运行的代码过多，需要支持低端机器

**识别的性能热点**：
1. CollectionsPagePatches.draw_Postfix - 收藏页面每帧遍历所有物品
2. FarmerPatches.GetItemDrawScale - 每次Farmer.draw()都重新计算
3. GiantFishManager.CheckAndTriggerNPCReactions - 每30帧LINQ查询所有NPC
4. BobberBarPatches Update钩子 - 每帧字典查找
5. ModEntry.OnUpdateTicked - 每帧模运算检查

**实现内容**：
- CollectionsPagePatches: 缓存机制，仅在页面切换时重建星标列表
- FarmerPatches: 结果缓存（注：v0.5.0已删除）
- GiantFishManager: 快速路径检查，平方距离替代开方，直接遍历替代LINQ
- ModEntry: NPC检查间隔从30帧增加到60帧

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.4.1
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证性能提升

---

## BATCH-003: Transpiler视觉缩放 ❌ 误判

**根因**：原Postfix实现无法修改已执行的绘制，需要Transpiler修改IL指令

**错误假设**：Farmer.draw()调用Item.drawInMenu()绘制手持物品

**实际情况**：
- 调用链是：Farmer.draw() → Game1.drawPlayerHeldObject() → Object.drawWhenHeld()
- Farmer.draw()从不调用Item.drawInMenu()
- 搜索了不存在的方法调用

**后果**：
- v0.3.0-v0.4.3的Transpiler从未生效
- 视觉缩放功能完全不工作
- 被误标记为"✅ 已完成"

**修复**：由BATCH-007重新实现正确方案

**R0状态**：❌ 假设错误
**R1状态**：❌ 删除了不存在的代码路径
**R2状态**：❌ 从未验证实际效果
**构建**：✅ v0.3.0（编译通过但功能无效）
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：❌ 视觉缩放从未工作

---

## BATCH-002: 控制台命令系统 ✅

**根因**：手动钓鱼测试各难度等级效率低下，需要快速测试工具

**实现内容**：
- 9个控制台命令（setlevel/addsuccess/addfail/info/list/clear/addstar/giant/bonus）
- DifficultyManager新增公开方法支持控制台操作
- 创建12KB测试指南文档（TESTING-GUIDE.md）

**R0状态**：✅ 已完成
**构建**：✅ v0.2.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内验证命令功能

---

## BATCH-001: 钓鱼难度系统初始实现 ✅

**根因**：新功能需求 - 实现渐进式难度钓鱼系统

**实现内容**：
1. 难度追踪系统（成功/失败计数，-10到100等级）
2. 线性倍数计算（难度/数量/品质/保护）
3. HUD通知与9级称号系统
4. 巨型鱼NPC反应（气泡+100随机对话）
5. 收藏品星标显示
6. 隐藏钓鱼等级加成（每星标+0.5）
7. 完整低频诊断日志

**R0状态**：✅ 已完成
**R1状态**：✅ 已完成
**R2状态**：✅ 已完成
**构建**：✅ v0.1.0
**部署**：✅ D:\GGGGG\K1515\Mods\FishingExpanded\
**实测**：⏳ 待游戏内完整验证

---

## 当前任务优先级

1. **【紧急】游戏内测试v0.5.2** - 验证9个新功能（5个bug修复+4个新需求）
2. **视觉验证** - 钓到倍数>15的鱼，手持时应该明显放大
3. **传奇鱼验证** - 钓到鱼王显示随机消息，不受难度系统影响
4. **图鉴验证** - Collections菜单查看鱼类描述，底部显示挑战等级和技能加成

---

## 修复历史记录

**累计修复次数统计**：

| 现象 | 累计次数 | 批次历史 |
|---|---:|---|
| 视觉缩放不工作 | 2 | BATCH-003(误判) → BATCH-007(正确方案) → BATCH-008(栈序列修复) |
| 钓鱼动画显示多条鱼 | 1 | BATCH-009(初次修复) |

**治理规则**：同一玩家可见现象的修复次数跨会话、批次、版本和名称累计，不能通过重新分组清零。

---

## 长期追踪项

- **【关键】v0.5.2所有功能需要游戏内验证** - 9个新功能待实测
- **【关键】图鉴显示需要打开Collections菜单验证** - BATCH-016
- 传奇鱼豁免需要实际钓到鱼王验证 - BATCH-014
- 非鱼类上限需要钓到垃圾/藻类验证 - BATCH-015

---

**最后更新**：2026-08-05 01:12
**当前版本**：v0.5.2
**总批次数**：16个（14个完成，1个推迟，1个误判）
**待实测项**：v0.5.2所有9个新功能










