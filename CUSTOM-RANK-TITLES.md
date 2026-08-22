# Custom Rank Titles (config.json) / 自定义职阶称号（config.json）

Every species keeps its own opinion of you, and that opinion is shown as a **rank title**. You can rename any — or all — of the 12 titles.

每个种群都会偷偷给你记一笔评价，评价会显示为**职阶称号**。这 12 个称号中的任何一个——或全部——都可以改成你自己想要的文字。

---

## English

### How to edit

1. Launch the game once so `FishingExpanded/config.json` is created inside your Mods folder.
2. Close the game.
3. Open `config.json` and find `"CustomFishingTitles"`.
4. It is a simple 12-slot list, ordered from weakest to strongest:

```json
"CustomFishingTitles": [
  "Minnow",          //  0     below level 1
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
```

### Rules

- Fill in only the slots you want to change. Any slot left as `""` keeps its built-in title.
- Keep the order exactly as shown. Extra or missing slots are safely ignored.
- Display only — difficulty, rewards and progression are never affected.
- Edit while the game is closed.

### Legacy one-line override

The older `"CustomFishingTitle"` field still works: set it to any text and **every** fish shows that title at every level.

```json
"CustomFishingTitle": "It's Just a Fish"
```

Leave it as `""` to keep the built-in ladder. If both settings are filled in, the 12-slot list above wins.

---

## 中文

### 编辑方法

1. 先启动一次游戏，让 Mods 目录下生成 `FishingExpanded/config.json`。
2. 关闭游戏。
3. 打开 `config.json`，找到 `"CustomFishingTitles"`。
4. 它就是一个简单的 12 格列表，按从弱到强排列：

```json
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
```

### 规则

- 只填你想改的格子，留空 `""` 的格子保持内置称号。
- 请严格保持上面的顺序，多写或少写的位置会被安全忽略。
- 只影响显示——难度、奖励和进度完全不受影响。
- 请在游戏关闭时编辑。

### 旧字段：一句话覆盖全部称号

旧字段 `"CustomFishingTitle"` 仍然可用：填入任意文字后，**所有**等级的鱼都显示这一个称号。

```json
"CustomFishingTitle": "就是条鱼"
```

留空 `""` 则按内置阶梯显示。两个字段同时填写时，上面的 12 格列表优先。
