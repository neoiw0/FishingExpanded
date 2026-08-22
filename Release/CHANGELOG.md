# Fishing Expanded — Changelog

> Bilingual: English full first, then Chinese full.
> 中英双语：先英文全文，后中文全文。

---

## ENGLISH
## 0.5.11

> Current release candidate. Gameplay details follow `GAME-DESIGN.md` / `WIKI.md`; this list covers player-visible changes.

### New Content / Mechanic Polish

- **Personalized villager praise**: 51 characters (35 villagers, 10 special/story NPCs, and 6 animals: pet dog/cat, horse, trash bear, raccoon, parrot) each have 10 unique giant-fish praise lines written around their own background and personality. When you show off a giant fish, there is a 60% chance the NPC uses their personal lines and a 40% chance the classic generic pool; NPCs without a personal pool (children, mod-added NPCs) always use the generic pool.
- Personalized lines focus on natural fish-name reactions; only a few science-minded characters mention the exact size (units still follow the current language).

### Other

- Manifest version bumped to 0.5.11; the installed manifest UniqueID is synced to `neoiw.FishingExpanded` on deployment (as planned in the 0.5.10 notes).


## 0.5.10

> Current release candidate. Gameplay details follow `GAME-DESIGN.md` / `WIKI.md`; this list covers player-visible changes.

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
## 0.5.11

> 当前发布候选版本。机制以 `GAME-DESIGN.md` / `WIKI.md` 为准，这里只列玩家可感知的主要变化。

### 新内容 / 机制完善

- **村民个性化赞美**：51 位角色（35 位村民、10 位特殊/剧情 NPC、6 类动物——宠物狗/猫、马、垃圾熊、浣熊、鹦鹉）各有 10 条按其背景与身份定制的巨型鱼赞美台词。举起巨型鱼时，NPC 有 60% 概率说专属台词、40% 概率说经典通用文案；没有专属池的角色（孩子、Mod 追加 NPC）始终使用通用池。
- 专属台词以自然的鱼名反应为主，仅少数科学家角色提及具体尺寸（单位仍跟随当前语言）。

### 其他

- Manifest 版本号升至 0.5.11；部署时安装目录 UniqueID 同步为 `neoiw.FishingExpanded`（兑现 0.5.10 说明中的计划）。


## 0.5.10

> 当前发布候选版本。机制以 `GAME-DESIGN.md` / `WIKI.md` 为准，这里只列玩家可感知的主要变化。

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