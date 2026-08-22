Fishing Expanded v0.5.11
========================

ENGLISH
-------
A fair, deep fishing expansion for Stardew Valley.
Every species keeps its own reputation score for you—plus rank titles, crowns, giant fish, challenge bait, and more—built to feel like vanilla Stardew.

Requirements:
- Stardew Valley 1.6+
- SMAPI 4.0.0+
- Online co-op: host and every farmhand must install this mod.

Install:
1. Install SMAPI and run Stardew Valley once.
2. Extract this archive into your `Stardew Valley/Mods` folder.
3. Make sure the folder is named `FishingExpanded`.
4. Launch the game with SMAPI. No configuration needed.

Uninstall:
- Delete the `FishingExpanded` folder. Your vanilla save is safe.

Advanced (optional, manual edit of config.json):
After the first launch you will find `FishingExpanded/config.json` inside your Mods folder.

1) "CustomFishingTitles" — rename ANY (or all 12) rank titles, one per key.
   Fill in only the ones you want to change; leave "" for the built-in title.

     "CustomFishingTitles": {
       "weak": "",            // <1
       "elite": "Ace Angler",        // 1-3   was: Elite
       "knight": "",            // 4-6   Knight
       "lord": "",            // 7-8   Baron
       "count": "",            // 9-15  Count
       "duke": "",            // 16-22 Duke
       "prince": "",            // 23-33 Prince
       "emperor": "",            // 34-45 Emperor
       "godking": "",            // 46-66 God King
       "divineking": "",            // 67-88 King of Gods
       "creator": "",            // 89-99 Primordial Dragon
       "taiyi": ""             // 100   hidden top rank (discover it in game!)
     }

   The example above makes fish at levels 1-3 display as "Ace Angler"; every other
   title stays built-in. Keys are case-insensitive; unknown keys are ignored.
   Display only — difficulty and rewards are never affected. Edit while the game is closed.

2) "CustomFishingTitle" (legacy, one line overrides ALL titles):
     "CustomFishingTitle": "Legend of the Pond"
   -> every fish shows "Legend of the Pond", from Elite to the hidden top rank.
   Leave "" to keep the built-in ladder. If both are set, the per-rank list above wins.

Source & License:
- Open source under GPL-3.0. See LICENSE.
- Source code: https://github.com/neoiw0/FishingExpanded

Nexus:
- https://www.nexusmods.com/stardewvalley/mods/50595

========================================

中文
----
一个公平、耐玩、深度融入星露谷原版的钓鱼扩展。
每个种群都会偷偷给你记一笔评价——头衔、皇冠、巨型鱼、挑战鱼饵等机制一应俱全——玩起来像原版本来就该有的样子。

需求：
- 星露谷物语 1.6+
- SMAPI 4.0.0+
- 在线联机：主机和每位农场客都必须安装本 Mod。

安装：
1. 先安装 SMAPI，并至少启动一次星露谷。
2. 把压缩包解压到 `Stardew Valley/Mods` 文件夹。
3. 确认文件夹名是 `FishingExpanded`。
4. 用 SMAPI 启动游戏。无需额外配置。

卸载：
- 删除 `FishingExpanded` 文件夹即可。原版存档安全。

高级设置（可选，手动编辑 config.json）：
首次启动后，Mods 目录下会生成 `FishingExpanded/config.json`。

1）"CustomFishingTitles"——12 个职阶称号想改哪个改哪个，一键一位。
   只填你想改的，留空 "" 保持内置称号：

     "CustomFishingTitles": {
       "weak": "",              // <1
       "elite": "钓鱼王牌",      // 1-3   原称号：精英
       "knight": "",              // 4-6   骑士
       "lord": "",              // 7-8   男爵
       "count": "",              // 9-15  伯爵
       "duke": "",              // 16-22 公爵
       "prince": "",              // 23-33 亲王
       "emperor": "",              // 34-45 帝王
       "godking": "",              // 46-66 神皇
       "divineking": "",              // 67-88 众神王
       "creator": "",              // 89-99 祖龙王
       "taiyi": ""             // 100   隐藏头衔（进游戏自己发现！）
     }

   上例效果：1-3 级的鱼显示“钓鱼王牌”，其余全部保持内置。键名忽略大小写，写错键名自动忽略。
   只影响显示——难度与奖励完全不受影响。请在游戏关闭时编辑。

2）"CustomFishingTitle"（旧字段，一句话覆盖全部称号）：
     "CustomFishingTitle": "湖畔传说"
   → 无论钓到哪个职阶的鱼，称号都显示为“湖畔传说”——从精英一路到隐藏头衔。
   留空 "" 按内置阶梯显示。两个字段同时填写时，上面的逐职阶列表优先。

源码与许可证：
- 开源，GPL-3.0。见 LICENSE。

N网：
- https://www.nexusmods.com/stardewvalley/mods/50595