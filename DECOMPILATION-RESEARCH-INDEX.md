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
| `_analysis/StardewValley.FishingRod.decompiled.cs` | `FishingRod.draw` 结算面板/真鱼/多鱼图绘制契约、`doPullFishFromWater` 飞行动画入口 | 当前 `FishingRod` DLL、绘制层 |
| `_analysis/StardewValley.BobberBar.decompiled.cs` | `BobberBar.draw` 鱼标绘制契约与蓄力槽字段 | 当前 `BobberBar` DLL、小游戏 UI |
| `_analysis/fishingrod-il-20260806/Stardew Valley.il` | `FishingRod.draw` 全量 IL（6731 字节，RVA 0x115e54）：结算面板 1870 标记、示意图 4f、落地真鱼 `(8,8)`/3f、多鱼循环与垃圾分支 | 当前安装 `Stardew Valley.dll`（DFE341CA...） |
| `_analysis/old-dll-decomp/FishingExpanded.decompiled.cs` | 旧部署 DLL `36B74510...` 全量反编译：可运行的 `AdjustFirstSettlementFishPosition(Vector2, FishingRod)` 消费式注入模式与旧 Transpiler 结构 | 部署备份 `FishingExpanded-20260806-20260806-152004-pre-B8BDAEFA` |
| `Character`（当前安装 DLL 反编译，临时文件 `%TEMP%\\Character-current.cs`） | `Farmer.modData` 继承自 `Character.modData`，`initNetFields` 注册 `.AddField(modData, "modData")` → 联机同步进主机存档 | 当前 `Character`/`Farmer` DLL、游戏版本 |
| `GameRunner` / `LocalMultiplayer`（当前安装 DLL 反编译） | 分屏每位本地玩家独立 `Game1` 实例；`GameRunner` 对每实例 Load/Save 静态字段（`LocalMultiplayer.StaticVarHolderType` 反射收集游戏程序集静态字段）；`Game1.player`/`activeClickableMenu` 随屏幕切换；SMAPI `Context.ScreenId => Game1.game1?.instanceId` | 当前安装 DLL、SMAPI 版本 |
| 陷阱：`_analysis/StardewValley.Farmer.decompiled.cs` 缺少 `modData`（旧反编译不完整，成员继承自 `Character`） | 检索成员缺失时先查基类；不能用旧文件证明字段不存在 | 当前 `Character`/`Farmer` DLL |

使用规则：当前安装证据优先于旧文件；只读单个方法不等于完成调用链；若源码、旧反编译和安装 DLL 冲突，先保存冲突证据并停止行为修改。
