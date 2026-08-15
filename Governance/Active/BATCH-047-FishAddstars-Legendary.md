# FishingExpanded 根因批次：BATCH-047 fish_addstars 支持传奇皇冠

> 活动卡只保存结论和证据链接；长日志、构建输出和反编译材料放在证据目录。
> 顶部当前状态覆盖更新，不在文件末尾追加版本时间线。

<!-- CURRENT-STATE-BEGIN -->
## 续接状态

| 分诊字段 | 值 |
|---|---|
| 当前轮次 | ROUND-20260812-06 |
| 当前活动类别 | CAT-01（fish_addstars 支持传奇皇冠） |
| 整合候选状态 | 已部署待集中测试（单类别；Deploy:ROUND-20260812-06:DE23813F... 已消费） |
| 当前权威活动卡 | 本卡（BATCH-047） |
| 本轮用户问题范围 | 用户 2026-08-12 明确：`fish_addstars 20` 应支持传奇皇冠（可计数 61 鱼池），开始修改并部署 |
| 本轮纳入 Case | FE-047-1 `fish_addstars` 测试池最多 56/61，无法直接用命令达到满皇冠 α=100% |
| 共享第一处分歧与所有权链证据 | 测试皇冠池=`BuildStarPool()` 剔除 5 条原版传奇；第一处分歧=该剔除使命令无法覆盖完整 61 可计数鱼池 |
| 排队或暂不纳入 Case | BATCH-042~045 已构建/部署待验收；BATCH-046 满皇冠史诗文案根因已证实，方案 A/B 待用户确认（本批不替代） |
| 合并/拆分决定及依据 | 单 Case 单类别：只改测试池来源与文案 |
| 本轮准入证据 | 用户明确要求并授权部署；`DifficultyManager.BuildStarPool` 源码确认剔除 `IsLegendaryFish` |
| 现有实现复核 | `AddCollectionStarsForTesting` 遍历 `BuildStarPool()` 并跳过传奇；`CountableFishIds` 本身含 5 传奇（61 条） |
| 允许修改范围 | `Source\Services\DifficultyManager.cs`（BuildStarPool/AddCollectionStarsForTesting 注释与逻辑）；`Source\ModEntry.cs`（帮助/警告文案）；文档：WIKI.md、TESTING-GUIDE.md、BUG-LEDGER.md、本卡 |
| 冻结 Case/禁止范围 | 难度/结算/鱼王豁免；BATCH-046 方案未确认不动；部署目录 config.json；不启动游戏、不 `git add -A` |

| 字段 | 值 |
|---|---|
| 当前类别目标 | `fish_addstars <数量>` 从完整 61 可计数鱼池（56 普通 + 5 原版传奇）添加；已存在皇冠跳过；`fish_addstars 61` 可达 α=100% |
| 可观测性决定 | 无新增诊断；警告文案同步改为 61 鱼池 |
| 自动化验收决定 | 沙箱：`fish_addstars 61` → 可计数 61/61、α=100%；重复执行不再添加；玩家序号并存（`fish_addstars 2 61`） |
| 当前类别阶段 | 代码侧结束（R0/R1/R2 完成） |
| 当前工作树 | 完整修改（BATCH-033~046 混合未提交基线） |
| 本轮统一构建 | Release Rebuild ✅（0 警告 0 错误）；DLL SHA256 `DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`；反编译核验 BuildStarPool=完整 61 池、帮助/警告文案已编译 |
| 唯一下一步 | 真实集中测试（`fish_addstars 61` → 61/61 α=100%；玩家序号并存） |
| 已消费动作 | `Build:ROUND-20260812-06:DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`；`Deploy:ROUND-20260812-06:DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D` |
| 重复执行授权 | 无 |

<!-- ROUND-CATEGORY-QUEUE-BEGIN -->
| 类别 ID | 包含 Case | 当前状态 | 结束路径 | 侦测代码/日志 | SMAPI 命令或稳定 UI 入口 |
|---|---|---|---|---|---|
| CAT-01 | FE-047-1 | 代码侧结束 | 根因修复 | 命令回显可计数/α | `fish_addstars <数量>` |
<!-- ROUND-CATEGORY-QUEUE-END -->

| Case | 当前状态 | 下一门禁 |
|---|---|---|
| FE-047-1 fish_addstars 测试池缺 5 传奇 | R2 完成 | 沙箱验收（61/61 α=100%） |

| 部署事实 | 值 |
|---|---|
| DLL SHA-256 | `DE23813FE6919109117F5EEC60D242173AE213A50C3DDEBE1E69A5D5D3E5458D`（2026-08-12 13:16 部署；备份 `DeploymentBackups\FishingExpanded-20260812-131648-pre-BATCH047`=9D199BD1...；源/目标哈希一致；config.json 未触碰；i18n 未变化） |
<!-- CURRENT-STATE-END -->

## 现象索引与持续计数

| Case | 玩家可见现象 | 历次已部署修复数 | 最近反证 | 根因组 | 状态 | 最短验收 |
|---|---|---:|---|---|---|---|
| FE-047-1 | `fish_addstars 61` 只能加到 56/61，无法命令直达满皇冠 | 0 | 无 | RC-047 | 实施中 | 61/61 α=100% |

## RC-047 根因卡

- 根因状态：已证实（源码）
- 第一处分歧：`BuildStarPool` 主动剔除 5 原版传奇
- 决定性证据：`BuildStarPool` 内 `if (!IsLegendaryFish(fishId))`；`CountableFishIds` 含 5 传奇
- 竞争解释表：竞争解释豁免（理由=静态契约完整证明池来源，无互斥假设）
- 唯一所有者：`DifficultyManager.AddCollectionStarsForTesting`（测试写入）
- 最短区分动作：池改为完整 61 后沙箱验证

## R0

- 修改第一处错误决策：`BuildStarPool` 返回全部 `CountableFishIds`；`AddCollectionStarsForTesting` 不再跳过传奇
- 新权威入口：`AddCollectionStarsForTesting`（唯一测试写入者）

## R2

| 场景 | 预期 | 不能发生 |
|---|---|---|
| `fish_addstars 61` | 可计数 61/61、α=100% | 只到 56 |
| 已有部分皇冠 | 跳过已存在，补足剩余 | 重复覆盖 |
| 玩家序号 | `fish_addstars 2 61` 给副机加满 | 串改主机 |
| Mod 鱼/非鱼类 | 不进入测试池 | 误加不可计数鱼 |

## 构建、部署与集中测试

- 构建结果：未执行
- 版本/哈希：待定
- 部署文件与目标：`D:\GGGGG\K1515\Mods\FishingExpanded`（用户已授权）

## 收尾与归档

- 已完成：契约核对、批次卡、R0 设计
- 下一条准确操作：R0 实施 → Release Rebuild → 备份部署 → 回填哈希
