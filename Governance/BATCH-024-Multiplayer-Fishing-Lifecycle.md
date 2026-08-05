# BATCH-024 多人数据与鱼获生命周期修正

**当前阶段**：R1 完成，待 R2 真实验收（2026-08-05）
**准入授权**：用户确认必须支持多人，并授权部署、启动游戏和实机验收。

## R0 证据与第一处分歧

- 现有 `DifficultyManager` 使用存档级 `ReadSaveData("FishDifficultyData")`，无法满足按玩家隔离。
- 当前安装的原生链路是 `FishingRod.pullFishFromWater()` → 鱼获动画 → `Farmer.caughtFish()`（记录捕获统计）→ `FishingRod.doneHoldingFish()` → 私有 `CreateFish()` → `addItemToInventoryBool()` 或原生 `ItemGrabMenu`。
- 旧实现试图在 `Farmer.caughtFish` Postfix 扫描背包修改数量；该时点原生鱼获物品尚未创建，无法可靠覆盖背包堆叠和溢出菜单。
- 鱼王识别表使用小写 `(o)`，调用链可能传入未限定或大写 `(O)` ID，静态识别不闭环。
- 高难度星标原先在 `BobberBar` 构造时写入，失败离开小游戏也可能获得星标；设计要求成功钓起后才获得。

## 用户确认合同

- 五只鱼王只显示特殊提示，不参与难度、数量、品质、奖励、技能、称号、视觉、NPC和蓄力保护。
- 鱼王 ID 统一兼容 `163`、`(O)163`、`(o)163` 等等价形式。
- 高难度星标和 `+0.5` 技能加成只在成功钓起后写入；鱼王永不获得。
- 没有技能加成时图鉴隐藏“钓鱼技能加成”一项；等级小于等于0的成功不显示挑战提示。
- 非鱼类到8级后后续成功不再增加难度。
- 难度、星标和隐藏加成按玩家独立保存；在线联机双方都安装本 Mod。
- 已有星标鱼进入小游戏时显示一次左下角挑战宣言；称号尊敬词每级3种，毅然决然词50种，应战说法20种，宣言不写状态。
- 小游戏出现时，非鱼王使用 `难度等级 / 10` 与当前有效整数钓鱼等级比较；超过当前等级显示建议，超过当前等级两倍时在建议开头显示“不可能的高难度挑战”；鱼王跳过。
- 非鱼王经验按当前小游戏难度倍率结算；隐藏技能加成无半级时向下取整；鱼王不使用隐藏技能加成或经验倍率。

## R1 允许修改的边界

- `Source/Services/DifficultyManager.cs`、`Source/Utils/SpecialFishHelper.cs`、`Source/Data/FishDifficultyData.cs`
- `Source/Patches/BobberBarPatches.cs`、`Source/Patches/FishingRodPatches.cs`、`Source/ModEntry.cs`
- `Source/Patches/FarmerFishingLevelPatches.cs`、`Source/Utils/ChallengeDialogueGenerator.cs`、`Source/Services/HUDNotifier.cs`、`Source/i18n/*.json`
- 对应 `GAME-DESIGN.md`、`BUG-LEDGER.md`、`MAINTENANCE-INDEX.md`、`TESTING.md`、`TESTING-GUIDE.md`

## R1 删除/替代清单

- 删除存档级鱼数据作为当前写入者，改用当前 `Farmer.modData`；仅主玩家迁移旧共享数据。
- 删除 `Farmer.caughtFish` Postfix 的背包扫描和堆叠改写。
- 数量转换唯一落点改为原生 `FishingRod.CreateFish` 返回物品边界。
- 删除 `BobberBar` 构造阶段的高难度星标写入。
- 待处理鱼获按玩家 ID + 规范化鱼 ID 隔离，并在返回标题时清理。
- 修正小游戏初始绿条外状态的脱杆计数；成功入口以原生 `fishDifficulty` 作为星标阈值兜底。
- 在 `Farmer.gainExperience(1, ...)` 的原生写入边界应用经验倍率；鱼王 BobberBar 构造期间屏蔽隐藏钓鱼等级。
- 星标挑战宣言从 BobberBar 构造后只读取星标和当前等级，由 HUD 统一展示。
- 等级建议在 BobberBar 构造后由 HUD 只读展示，不建立第二份难度或钓鱼等级状态。

## R2 验收

- 单人：正常鱼成功/失败、0级保护、非鱼类8级封顶、星标只在成功后获得。
- 多人：主玩家与农场客各自设置/钓鱼，等级、星标和技能加成不互相读取；双方安装 Mod 后无重复结算。
- 鱼王：五种 ID 形式均只显示特殊提示，完全不进入自定义难度和奖励路径。
- 数量：普通背包、已有同类堆叠、原生多鱼鱼饵和满背包 `ItemGrabMenu` 均只转换最终物品数量，动画仍由原生数量控制。
- 星标宣言：已有星标进入小游戏时出现一次；验证称号词池每级3种、毅然决然50种、应战20种，普通鱼与鱼王边界分别成立。
- 经验：等级0经验倍率为1，正数等级按设计倍率增加；鱼王经验与隐藏技能等级不受 Mod 影响。
- 等级建议：使用难度等级 1、10、50、100 与不同玩家等级验证小数比较、两倍“不可能”前缀和鱼王豁免。
