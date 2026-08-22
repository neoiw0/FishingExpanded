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

1) "CustomFishingTitles" — rename ANY (or all 12) rank titles with a simple list.
   The 12 slots go from weakest to strongest; fill in only the ones you want to
   change and leave "" for the built-in title.

     "CustomFishingTitles": [
       "Minnow",          //  0     below rank 1
       "Small Fish",      //  1     levels 1-3
       "Medium Fish",     //  2     levels 4-6
       "Big Fish",        //  3     levels 7-8
       "Huge Fish",       //  4     levels 9-15
       "Giant Fish",      //  5     levels 16-22
       "Gigantic Fish",   //  6     levels 23-33
       "Colossal Fish",   //  7     levels 34-45
       "Monster Fish",    //  8     levels 46-66
       "Leviathan",       //  9     levels 67-88
       "Mythic Fish",     // 10     levels 89-99
       "Legendary Fish"   // 11     level 100 - the hidden top rank (discover it in game!)
     ]

   The example above fills all 12 slots so you can see how the ladder reads:
   Minnow, Small Fish, Medium Fish, Big Fish, Huge Fish, Giant Fish, Gigantic Fish,
   Colossal Fish, Monster Fish, Leviathan, Mythic Fish, Legendary Fish. You do not
   have to fill them all - any slot left as "" keeps its built-in title. Keep the
   order exactly as shown; extra or missing slots are safely ignored. Display only:
   difficulty and rewards are never affected. Edit while the game is closed.

2) "CustomFishingTitle" (legacy, one line overrides ALL titles):
     "CustomFishingTitle": "It's Just a Fish"
   -> every fish shows "It's Just a Fish" at every level.
   Leave "" to keep the built-in ladder. If both are set, the list above wins.

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

1）"CustomFishingTitles"——12 个职阶称号想改哪个改哪个，就是一个简单列表。
   12 个位置按从弱到强排列；只填你想改的，留空 "" 保持内置称号：

     "CustomFishingTitles": [
       "小虾米",      //  0     不足 1 级
       "小鱼",        //  1     1-3 级
       "中鱼",        //  2     4-6 级
       "大鱼",        //  3     7-8 级
       "特大鱼",      //  4     9-15 级
       "巨鱼",        //  5     16-22 级
       "巨型鱼",      //  6     23-33 级
       "庞然巨鱼",    //  7     34-45 级
       "怪物鱼",      //  8     46-66 级
       "海怪鱼",      //  9     67-88 级
       "神话之鱼",    // 10     89-99 级
       "传说之鱼"     // 11     100 级——隐藏头衔（进游戏自己发现！）
     ]

   上例把 12 格全部填满，方便看清整条阶梯：小虾米、小鱼、中鱼、大鱼、特大鱼、巨鱼、
   巨型鱼、庞然巨鱼、怪物鱼、海怪鱼、神话之鱼、传说之鱼。不必全填——任何留空 "" 的格子
   都保持内置称号。请严格保持上面的顺序，多写或少写的位置会被安全忽略。
   只影响显示，难度与奖励完全不受影响。请在游戏关闭时编辑。

2）"CustomFishingTitle"（旧字段，一句话覆盖全部称号）：
     "CustomFishingTitle": "就是条鱼"
   → 无论钓到哪个职阶的鱼，称号都显示为“就是条鱼”。
   留空 "" 按内置阶梯显示。两个字段同时填写时，上面的列表优先。

源码与许可证：
- 开源，GPL-3.0。见 LICENSE。

N网：
- https://www.nexusmods.com/stardewvalley/mods/50595
