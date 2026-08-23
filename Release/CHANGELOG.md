# Fishing Expanded — Changelog

> Bilingual: English full first, then Chinese full.
> 中英双语：先英文全文，后中文全文。

---

## ENGLISH
## 1.0.1

### Fixes
- **Generic Mod Config Menu integration restored**: the config menu failed to appear because the mod-provided API interface did not match the actual API (`AddNumberOption` missing its `fieldId` parameter); registration is fixed and the mod shows up in GMCM again.
- **Non-fish catches**: seaweed/trash hooked without winning the small level-up roll no longer display the "next challenge" line; winning the roll or reaching the level-8 cap still does.

### Improvements
- **Config tooltips wrap correctly**: long anchor lists inside slider tooltips are word-wrapped with the same font and width GMCM uses, instead of being drawn off-screen; the three income sliders were renamed to **Extra catch income / Extra experience income / No-minigame extra catch income**, and the catch & XP tooltips now list key reputation anchors with live multipliers.

### Balance Rework
- **Lowered quantity curve anchors**: -10/0 = 1, 8 = 2, 16 = 3, 32 = 4, 56 = 7, 100 = 25 fish per catch (probabilistic carry-over between anchors unchanged).
- **XP curve reworked**: anchor nodes ×1 at 0, ×2 at 10, ×3 at 30, ×4 at 50, ×5 at 75, capped at ×8 at level 100; interpolation is floored.
- **Bait bonuses reworked** (both computed on the normal catch): wild bait (level >0, native double) grants **+10% with a +1 floor**; challenge bait within 5:00 (adjusted >100) grants **+20% with a +2 floor**. The native extra fish is no longer granted inside the mod's scope; overtime settles at the normal catch before star-loss penalties.

## 1.0.0

> First stable release. Gameplay details follow `GAME-DESIGN.md` / `Wiki-for-Developers.md`; this list covers player-visible changes.

### New Content / Balance Rework

- **New quantity curve with a slower early game**: fish per catch now follows player-tuned anchors — reputation -10/0 = 1, 8 = 2, 16 = 4, 32 = 8, 56 = 24, 100 = 100 (replaces the old level × 1 line). Between anchors only the fractional remainder is settled probabilistically on each catch (level 4 expectation 1.5 ≈ a 50% chance of 2 fish); integer-expectation levels are constant, never random.
- **Reworked bait bonuses**: wild bait (level >0, native double) now grants **+10% catch with a +1 floor**, and challenge bait (adjusted difficulty >100, within 5:00) grants **+20% with a +2 floor** — both computed on the normal catch, replacing the native extra fish (BATCH-082).
- **Income scaling sliders (GMCM ×3)**: Quantity income / XP income / No-minigame quantity income. Default 100%, adjustable 10%–300%. Quantity applies after every bonus and discount; XP after base recalculation and the level multiplier; the daily-limit zero catch still takes priority. Quantity sliders show the live anchor list in their tooltip.
- **Training Rod rebalance**: while holding the Training Rod, real-fish reputation above 4 stops growing entirely for that catch (no save writes at all), and a dedicated hint replaces the rank-up tip.
- **Trash / algae / crab pots rework**: these no-minigame items use their own curve (0 = 1, 8 = 8) and each haul requests +1 level with only a 5% chance to be granted; a missed roll can still pop a light consolation hint (20%).

### Compatibility

- **Walk of Life coexistence**: when DaLion.Professions is installed, FishingExpanded takes over minigame balance — WoL fishing-profession perks (bigger green bar from Deluxe Bait, catch-bar slowdown, instant full-pond catches) no longer alter the battle. A one-time notice appears on save load; everything else from both mods keeps working. Without WoL installed this layer is fully dormant (zero behavioral difference).


## 0.5.11

> Current release candidate. Gameplay details follow `GAME-DESIGN.md` / `Wiki-for-Developers.md`; this list covers player-visible changes.

### New Content / Mechanic Polish

- **Personalized villager praise**: 51 characters (35 villagers, 10 special/story NPCs, and 6 animals: pet dog/cat, horse, trash bear, raccoon, parrot) each have 10 unique giant-fish praise lines written around their own background and personality. When you show off a giant fish, there is a 60% chance the NPC uses their personal lines and a 40% chance the classic generic pool; NPCs without a personal pool (children, mod-added NPCs) always use the generic pool.
- Personalized lines focus on natural fish-name reactions; only a few science-minded characters mention the exact size (units still follow the current language).

### Other

- **Custom titles for all 12 ranks**: the new `CustomFishingTitles` section in config.json is a simple 12-slot list, weakest to strongest — fill in any slots you want to rename, leave "" for the built-in title. The legacy `CustomFishingTitle` one-line override still works; when both are set, the slot list wins.
- **Debug logging is now off by default**: a fresh install produces no debug/info log output in the SMAPI console. Warnings and errors are always logged regardless of the toggle, so bug reports still work out of the box. Turn on "Enable logging" in GMCM for full debug output when troubleshooting.
- Rank titles renamed (EN+CN): Lord → Baron, Grand Duke → Duke, Divine King → King of Gods, Dragon God King → Primordial Dragon; the hidden 100-level title's English name is now "The Primordial One".
- Manifest version bumped to 0.5.11; the installed manifest UniqueID is synced to `neoiw.FishingExpanded` on deployment (as planned in the 0.5.10 notes).


## 0.5.10

> Current release candidate. Gameplay details follow `GAME-DESIGN.md` / `Wiki-for-Developers.md`; this list covers player-visible changes.

### New Content / Mechanic Polish

- **Quantity multiplier is now level × 1**: the higher the level, the more fish per catch; level 100 = 100 fish, level 10 = 10 fish. Negative levels and level 0 stay at 1×.
- **Quality is now threshold-based**: level 10 guarantees silver, level 25 guarantees gold, level 50 guarantees iridium; no more step-by-step accumulation.
- **XP curve optimization**: XP multiplier grows linearly from ×5 at level 10 to ×20 at level 100; the XP difficulty input is clamped to `[vanilla difficulty, 120]`, so low difficulty never gives less than vanilla and high difficulty never explodes.
- **Crab pot items are now included in the leveling system**: crab pot fish and trash follow non-fish rules (cap level 8), +1 level per haul.
- **Festival fishing toggle**: off by default = SquidFest, Trout Derby, and Festival of Ice are fully vanilla; when enabled, mod rules apply and festival scores scale with the quantity multiplier.
- **High-reputation "combo phrase" backseeding**: high-reputation fish replay a fixed segment after teleporting, letting players learn patterns.
- **Challenge bait backseeding seed**: the same fish at the same level has fixed behavior under challenge bait, cleared after a successful catch.
- **Challenge star loss notification**: after 5 minutes, one star drops per minute with a clear left-bottom message showing remaining stars and catch discount.
- **Fixed fishing-level factor and success floor**: higher fishing levels level up fish faster; every successful catch grants at least +1.
- **Lots of feel and visual fixes**: rod proficiency, dual floating-tip channels, jump telegraphs, truce breaks, screen-edge flipping, and more.
- **New test/debug command `fish_next [player index] <fish ID>`**: force a specific player's next fishing minigame to a chosen fish; no save writes, one-shot.
- **Compatibility/stability fixes**: VanillaTips top-level API fix, HUD long-text wrapping at 1/3 UI width, Chinese punctuation normalization.
- **English localization improvements**: giant fish sizes use inches in English, localized animal sounds (dog/cat/horse variants).
- **Perseverance reward balance**: battles over 60 seconds now grant Seafoam Pudding at a 60% chance (not guaranteed, no stacking with the 30-second tier).
- **Crown assist upgrades**: flowing golden crowns add extra assist chance and are more likely to be selected after triggering; hard mode (random behavior) scales the level-100 enlarged crown from 1.2× to 1.3×.
- **Custom fishing title**: manually edit `config.json` to customize the visible rank title (hidden from GMCM); leave empty to use default titles.
- **Giant fish display improvements**: entering the farmhouse or starting a new day resets giant fish display and villager praise records, allowing re-triggering; split-screen/remote co-op now syncs giant fish visuals and NPC bubbles to other players' screens.

### Other

- Manifest updated: UniqueID changed to `neoiw.FishingExpanded`, UpdateKeys points to `Nexus:50595`.
- Added player wiki (`PLAYER-WIKI.md`) and Nexus-ready bilingual description files under `Release/`.

---

## EARLIER VERSIONS (Brief)

- **0.5.x early**: fish reputation system, HUD messages, catch-bar protection, crowns and rod proficiency, giant fish with NPC reactions, exhaustion, perseverance rewards, Starfruit Tea, Wild Bait bonus, and multiplayer/split-screen data isolation were all completed across the 0.5.x iterations.

---

## 中文
## 1.0.1

### 修复
- **Generic Mod Config Menu 集成恢复**：此前配置菜单无法出现——模组提供的 API 接口与实际 API 不匹配（`AddNumberOption` 缺少 `fieldId` 参数），注册失败；现已修复，Fishing Expanded 重新出现在 GMCM 中。
- **非鱼类钓获**：海藻/垃圾未掷中升级机会时不再显示"下一次向……发起挑战"文案；掷中或达到 8 级封顶时照常显示。

### 改进
- **config 提示正确换行**：滑杆提示中的长锚点列表按 GMCM 同款字体与宽度自动折行，不再画出屏幕；三个收益滑杆更名为**额外渔获 / 额外经验 / 无小游戏物品额外渔获**，渔获与经验的提示现在会列出关键声誉节点及实时倍数。

### 数值调整
- **数量曲线锚点下调**：每次钓获 -10/0 = 1 条、8 = 2、16 = 3、32 = 4、56 = 7、100 = 25（锚点间的概率过渡不变）。
- **经验曲线重做**：锚点节点 0级 ×1、10级 ×2、30级 ×3、50级 ×4、75级 ×5、100级 ×8 封顶；插值后向下取整。
- **鱼饵加成改版**（均以"不用该鱼饵时的正常渔获"为基数）：万能鱼饵（等级>0 且原生双倍）增产 **10%、保底 +1 条**；挑战鱼饵（难度>100、5 分钟内成功）增产 **20%、保底 +2 条**。原生多发部分在模组区间内不再发放；超时按正常渔获结算后再计算掉星折扣。

## 1.0.0

> 首个稳定版本。机制以 `GAME-DESIGN.md` / `Wiki-for-Developers.md` 为准，这里只列玩家可感知的主要变化。

### 新内容 / 平衡重构

- **数量曲线前期放缓**：每次钓获条数改为玩家逐点定稿的锚点曲线——声誉 -10/0 = 1 条、8 = 2、16 = 4、32 = 8、56 = 24、100 = 100（替代旧"等级×1"直线）。锚点之间只对"不足 1 条被约掉的余量"按概率进位（4 级期望 1.5 ≈ 一半概率 2 条）；整数期望等级恒定不随机。
- **鱼饵加成改版（BATCH-082）**：万能鱼饵（等级>0、原生双倍）改为**增产 10%、保底 +1 条**；挑战鱼饵（调整后难度>100、5 分钟内成功）改为**增产 20%、保底 +2 条**——均以"不用该鱼饵时的正常渔获"为基数，原生的多发部分在模组区间内不再发放。
- **收益缩放（GMCM 三条滑杆）**：额外渔获 / 额外经验 / 无小游戏物品额外渔获，默认 100%，可调 10%~300%。渔获作用于全部加成与折扣之后；经验作用于基数重算与倍率之后；每日限额清零仍然最优先。滑杆提示会显示关键节点（多少级声誉=多少倍收益）并自动换行。
- **训练鱼竿再平衡**：手持训练鱼竿时，真鱼声誉高于 4 的部分本次收获完全不写入存档，并用专属提示替代当次的升级建议行。
- **垃圾/藻类/蟹笼重构**：这类无小游戏物品走专属数量曲线（0 级 = 1 个、8 级 = 8 个）；每次收获固定请求 +1 级但只有 **5%** 概率真正授予，未中有 20% 概率弹出一条轻量安慰提示。

### 兼容性

- **Walk of Life 同装兼容**：检测到 DaLion.Professions 时，FishingExpanded 接管钓鱼小游戏平衡——WoL 钓鱼职业的效果（豪华鱼饵加大绿条、蓄力槽减速、满鱼塘秒钓）不再改变战斗。进档时提示一次；双方其余内容照常可用。未装 WoL 则此层完全休眠（零行为差异）。


## 0.5.11

> 当前发布候选版本。机制以 `GAME-DESIGN.md` / `Wiki-for-Developers.md` 为准，这里只列玩家可感知的主要变化。

### 新内容 / 机制完善

- **村民个性化赞美**：51 位角色（35 位村民、10 位特殊/剧情 NPC、6 类动物——宠物狗/猫、马、垃圾熊、浣熊、鹦鹉）各有 10 条按其背景与身份定制的巨型鱼赞美台词。举起巨型鱼时，NPC 有 60% 概率说专属台词、40% 概率说经典通用文案；没有专属池的角色（孩子、Mod 追加 NPC）始终使用通用池。
- 专属台词以自然的鱼名反应为主，仅少数科学家角色提及具体尺寸（单位仍跟随当前语言）。

### 其他

- **12 个职阶称号全部可自定义**：config.json 新增 `CustomFishingTitles` 段——一个 12 格简单列表，从弱到强排列，想改哪格填哪格，留空保持内置称号。旧字段 `CustomFishingTitle`（一句话覆盖全部）继续可用；两者同时填写时列表优先。
- **调试日志默认关闭**：全新安装不再向 SMAPI 控制台输出调试/信息日志。警告与错误不受开关影响、始终保留，报障开箱即用。排障时可在 GMCM 打开“Enable logging”查看完整调试日志。
- 称号改名（中英）：领主→男爵、大公→公爵、神王→众神王、龙神王→祖龙王；100 级隐藏头衔英文名改为“The Primordial One”。
- Manifest 版本号升至 0.5.11；部署时安装目录 UniqueID 同步为 `neoiw.FishingExpanded`（兑现 0.5.10 说明中的计划）。


## 0.5.10

> 当前发布候选版本。机制以 `GAME-DESIGN.md` / `Wiki-for-Developers.md` 为准，这里只列玩家可感知的主要变化。

### 新内容 / 机制完善

- **数量倍率改为 等级×1**：等级越高一次钓得越多，100 级 = 100 条；10 级 = 10 条。负等级和 0 级保持 1 倍。
- **品质改为门槛提升**：10 级保底银星、25 级保底金星、50 级保底铱星；不再逐级累加。
- **经验曲线优化**：经验倍数 10 级 ×5 → 100 级 ×20 线性增长；经验公式的难度输入钳制在 `[原生难度, 120]`，低难度不会比原版少，高难度也不会数值爆炸。
- **蟹笼物品纳入升级系统**：蟹笼鱼与蟹笼垃圾按非鱼类规则升级（上限 8 级），收获一次 +1 级。
- **节日钓鱼开关**：默认关闭 = 鱿鱼节、鳟鱼大赛、冰雪节完全原版；开启后模组规则生效，且节日分数按数量倍数提升。
- **高声誉鱼“招式短语”**：高声誉的鱼在瞬移后会循环播放一段固定轨迹，玩家可以背板。
- **挑战鱼饵背板种子**：同鱼同等级在挑战鱼饵下行为固定，钓起后清除。
- **挑战星掉星提示**：超出 5 分钟后每分钟掉一颗星，并在左下角提示剩余星数和鱼获折扣。
- **固定钓鱼等级系数与成功保底**：高钓鱼等级升级更快；成功至少 +1 级。
- **大量手感与视觉修复**：鱼竿熟练度、双通道浮动提示、跳鱼前摇、停战休息、屏幕边缘翻转等。
- **新增测试/调试命令 `fish_next [玩家序号] <鱼ID>`**：强制指定玩家的下一次钓鱼小游戏为指定鱼；适合测试和自定挑战，不写存档、一次消费。
- **兼容性/稳定性修复**：VanillaTips 顶层 API 接口修复、HUD 长文案按 UI 屏宽 1/3 折行、中文标点规范化。
- **英文适配增强**：英文模式下巨型鱼尺寸改用英寸（in.）、动物叫声使用英文拟声词（狗/猫/马各多种随机）。
- **持久战奖励平衡**：战斗 ≥60 秒的海泡布丁改为 60% 概率发放（不再必得，也不叠加 30 秒档）。
- **皇冠助战强化**：流动金色皇冠会额外提升助战概率，且触发后金冠鱼更容易被选中；困难模式（随机无背板）下 100 级增大金冠尺寸加成 1.2 → 1.3 倍。
- **自定义钓鱼称号**：可在 `config.json` 手动自定义称号（GMCM 隐藏），留空则继续使用默认称号。
- **巨型鱼展示优化**：进入农舍或换日后，巨型鱼展示与村民赞美记录都会重置，可再次触发；联机/分屏下其他玩家屏幕也能看到超大鱼视觉与 NPC 冒泡。

### 其他

- 更新 Manifest：UniqueID 改为 `neoiw.FishingExpanded`，UpdateKeys 指向 `Nexus:50595`。
- 新增玩家版 Wiki（`PLAYER-WIKI.md`）以及 `Release/` 下的 N 网双语发布文件。

---

## 更早版本（简略）

- **0.5.x 早期**：鱼群声誉系统、HUD 提示、蓄力槽保护、皇冠与鱼竿熟练度、巨型鱼与 NPC 反应、力竭机制、持久战奖励、星之果茶、万能鱼饵加成、多人/分屏数据隔离等核心系统均在 0.5.x 迭代中完成。