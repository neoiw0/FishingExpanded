# FishingExpanded 反编译研究索引

本文件只索引本项目已保存的反编译证据，不把索引本身当作当前原生契约。重新修改前，优先用当前安装 DLL 重新验证目标方法和版本。

| 证据 | 用途 | 复核边界 |
|---|---|---|
| `_analysis/StardewValley.Farmer.decompiled.cs` | `Farmer.draw`、手持物入口和玩家鱼获调用关系 | 当前 `Farmer` DLL、游戏版本 |
| `_analysis/StardewValley.Game1.decompiled.cs` | `Game1.drawPlayerHeldObject` 等绘制中转 | 当前 `Game1` DLL、绘制层 |
| `_analysis/StardewValley.Object.decompiled.cs` | `Object.drawWhenHeld` 和物品绘制参数 | 当前 `Object` DLL、IL 栈序 |
| `_analysis/StardewValley.Item.decompiled.cs` | 物品类型、数量和显示契约线索 | 当前 `Item` DLL、实际调用者 |
| `_analysis/CollectionsPage.full.cs` | 图鉴描述生成入口 | 当前 `CollectionsPage` DLL、鱼类 tab |
| `_analysis/CollectionsPage.draw.decompiled.cs` | 图标和绘制坐标线索 | 当前 Collections 绘制路径 |
| `_analysis/Object.drawWhenHeld.IL.txt` | 手持物 Transpiler 的 IL 观察记录 | 当前方法指令、Harmony 转换结果 |

使用规则：当前安装证据优先于旧文件；只读单个方法不等于完成调用链；若源码、旧反编译和安装 DLL 冲突，先保存冲突证据并停止行为修改。
