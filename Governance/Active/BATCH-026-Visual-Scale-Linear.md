# BATCH-026：视觉缩放改为线性（100级 = 原25级大小）

**创建**：2026-08-06
**类型**：玩家确认的设计数值调整（非 Bug；显示链路已验收通过）
**状态**：R0 ✅ / R1 ✅ / R2 ✅；Release Rebuild ✅（0警告0错误，产物 SHA256 `77A1937CC1B5726EDE00E26C6BF06572A6E8223DAF48B7B06484FD9F6EDD4C24`）；不部署（用户明确“改源码，先不部署”）

## 玩家需求（原话要点）

- “鱼过于大了，让100难度等级的鱼达到现在25难度等级的大小就可以了，线性增大，你自己算。”
- “用节省算力的方法，加加减减就行了，线性增加而已，原来算法太复杂。”
- “改源码，先不部署。”

## 第一处分歧与公式现状（已证实）

- 唯一公式所有者：`DifficultyCalculator.GetVisualScale(int quantityMultiplier)`，旧实现 `(float)Math.Pow(quantityMultiplier, 1.0 / 3.0)`（立方根）。
- 数量倍数 `GetQuantityMultiplier(level) = round(1 + 1.99×level)`：25级=51，100级=200。
- 旧缩放：25级 = 51^(1/3) ≈ 3.7084；100级 ≈ 5.8480 → “鱼过于大”。
- 调用链（BATCH-007/008/024 已建立，本轮源码复核一致）：
  - `FishingRodPatches.DoPullFishFromWater_Postfix`：飞行动画，中心锚点。
  - `FishingRodPatches.GetFishVisualScale` / `Draw_Transpiler`：落地真鱼，底边中点。
  - `GiantFishManager.GetFishVisualScale` → `ObjectPatches.GetDrawScale` / `DrawWhenHeld_Prefix`：手持鱼，底边中点。
  - `GiantFishManager.RecordGiantFish` 日志：只读展示。
  - 小游戏鱼标与结算面板示意图不再经过 `GetVisualScale`（BATCH-024 已移除）。

## R0：新公式

- `视觉缩放 = 1 + (数量倍数 - 1) × k`，`k = (51^(1/3) - 1) / 199 ≈ 0.0136102`。
- 等价按等级线性：`缩放 = 1 + 等级 × (51^(1/3) - 1) / 100 ≈ 1 + 等级 × 0.0270843`。
- 关键点：0级=1.0000；25级≈1.6805（旧3.7084）；50级≈2.3474；100级≈3.7084（旧5.8480，= 旧25级大小）。
- 负数/0级：数量倍数 ≤ 1 → 1.0（与旧行为一致）。
- 只改视觉缩放数值；不影响数量倍数、经验、品质、`fishSize` 数值、动画数量、锚点合同。

## R1：删除与收敛

- 视觉立方根 `Math.Pow` 全部移除：`DifficultyCalculator.cs`（公式体）、`GiantFishManager.cs` 日志行改复用 `GetVisualScale`。
- 全源码 `rg "Pow"` 无残留；无新增状态、字典、缓存或字段。

## R2：边界推演

| 场景 | 预期 |
|---|---|
| 等级 0 / 负数 | 缩放 1.0，原生大小 |
| 等级 25 | ≈1.6805（明显小于旧 3.7084，仍可辨识放大） |
| 等级 100 | ≈3.7084（= 旧25级大小，不再 5.85 过大） |
| 非鱼类（上限8级） | 倍数17 → 缩放≈1.2444，正常应用 |
| 鱼王 | 无待处理事实/不记录巨型鱼，保持完整豁免 |
| 多人/分屏 | 公式为纯函数，展示事实仍按玩家隔离读取，无共享状态 |
| 锚点合同 | 只改缩放数值，锚点补偿逻辑不变 |
| 性能 | 每次调用一次乘加，替代 `Math.Pow` 开方，热路径更省 |

## 可观测性决定

- 公式为纯函数，静态合同可完整证明，符合免除条件；现有三条低频视觉诊断（`FishingRod.fly` / `FishingRod.draw` / `Object.drawWhenHeld`）直接输出新缩放值，不新增诊断。

## 自动化验收决定

- 复用 `FISH-CLI-01` 全量 `Saves` 沙箱与真实原生钓鱼入口；成功：25级≈1.68倍、100级≈3.71倍、0级/负数=1.0、锚点不漂移；失败：出现旧立方根值或锚点偏移。

## 委派评估

- `NotBeneficial`：单点纯函数公式修改，无并行子任务，主线程直接完成；未委派。

## 门禁

- 构建：Release Rebuild ✅（0警告0错误；ilspycmd 反编译确认 `GetVisualScale` = `1f + (multiplier-1) × 0.0136102f`；产物 SHA256 `77A1937C...`）。
- 部署：不部署（用户明确先不部署）。
- 实测：待用户授权部署后执行，每个现象独立标记。