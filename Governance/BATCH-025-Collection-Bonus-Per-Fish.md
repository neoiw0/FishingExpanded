# BATCH-025 图鉴“钓鱼技能加成”按鱼种独立显示

**当前阶段**：R0/R1/R2 完成（2026-08-06）；与 BATCH-024 同一 DLL，候选 `B8BDAEFA...` 启动崩溃后修复为 `2B0EB0F13272E9FC7B34EB43F6ACD30D2783F7032C873AA911BCE6516DBF8003`，已部署并启动核验通过（15:40 会话）；Runtime/真实画面验收待执行。
**准入授权**：2026-08-06 用户实测反馈图鉴加成显示错误；用户确认“只有加上星星的鱼才显示 +0.5”，本卡只允许修改 `CollectionsPagePatches.cs` 图鉴描述展示路径与对应治理/测试文件。

**本轮准入证据（2026-08-06）**：用户实测：收集页面几乎所有鱼都显示“+0.5钓鱼等级”，而不是只有已收藏星标的鱼。当前源码 `CollectionsPagePatches.CreateDescription_Postfix` 原先读取全局累计 `DifficultyManager.GetFishingLevelBonus()`（星标鱼种数 × 0.5）并追加到每一条鱼描述，与用户确认的“按鱼种独立展示、仅星标鱼 +0.5”不符。

**第一处分歧（已证实）**：`CollectionsPagePatches`（展示层）在 `CreateDescription_Postfix` 显示全局累计加成，未按当前鱼 `HasCollectionStar(fishId)` 判断；这是纯展示层分歧，不涉及难度、星标写入或经验结算所有权。

**本轮观测决定**：静态合同免除不完全适用（运行时会改变图鉴文本），但该路径不新增任何低频诊断；判定依据为真实验收时打开 Collections 菜单逐鱼核对描述文本。图鉴只读展示，不改变任何状态；无需新增诊断写入者。

**本轮自动化验收决定**：复用现有 `fish_addstar <id>` 命令与真实收藏品菜单路线（TESTING-GUIDE 3.5 场景4，Collections → Fish 标签）。存档影响：命令会写入星标/难度数据，按 FISH-CLI-01 全量 `Saves` 沙箱执行。成功：已 `fish_addstar` 的鱼描述显示“钓鱼技能加成：+0.5”；未加星的鱼描述不显示该行；鱼王与等级<=0 的鱼保持原豁免（鱼王不显示、等级<=0 不显示挑战等级但加成项仍按星标独立判定）。失败：任何未星标鱼显示 +0.5、任何星标鱼不显示 +0.5、出现全局累计值（如 +2.5）。每个现象独立标记通过/失败/未执行。

**R0 消除第一处错误决策**：`CreateDescription_Postfix` 删除全局累计加成读取，改为 `if (DifficultyManager.HasCollectionStar(fishId)) additions.Add("钓鱼技能加成：+0.5")`；显示逻辑按当前鱼种独立判定。

**R1 删除/替代清单**：
- 删除图鉴描述路径对 `GetFishingLevelBonus()` 的读取（展示层不再显示全局累计）。
- 保留 `DifficultyManager.GetFishingLevelBonus()` 作为经验结算的唯一所有者（`Farmer.gainExperience` 路径继续使用），图鉴只消费 `HasCollectionStar` 的按鱼事实。
- 不新增第二个星标状态所有者；`CollectionStars` 仍由 `DifficultyManager` 按玩家 `modData` 唯一写入。

**R2 验收**：
- 场景：星标鱼显示 +0.5、未星标鱼不显示、同一鱼加星前后描述变化、多条星标鱼互不影响、鱼王豁免、等级<=0 时挑战等级隐藏但加成项独立。
- 性能：每鱼一次 `HashSet.Contains` 查找，无新增每帧遍历。
- 存档：无新增字段、无写入变化；只读展示。
- 多人：星标按玩家 `modData` 隔离，主机/农场客各自图鉴只显示自己的星标加成。
- 部署对应性：绑定 `CollectionsPage.createDescription` 签名与 `currentTab==fishTab` 边界；部署前核对当前安装 DLL。

**Build/Deploy 门禁**：当前源码 Release `0.5.10` Rebuild 已通过（0 警告/0 错误）；Deploy 待执行（目标进程/DLL 占用检查、备份、源/目标哈希、不覆盖 `config.json`）。Runtime 与真实画面验收待执行；构建/部署不代替真实验收。
**Closeout/静态门禁（2026-08-06）**：治理同步（`BUG-LEDGER.md`、`GAME-DESIGN.md`、`MAINTENANCE-INDEX.md`、`TESTING.md`、`TESTING-GUIDE.md`、`CURRENT-WORK-HANDOFF.md`、本卡）；Git 基线 HEAD=`8f799da`，仅本批文件在工作树；聚焦 diff 限于 `CollectionsPagePatches.CreateDescription_Postfix`；委派 NotBeneficial；构建 0 警告/0 错误、DLL SHA-256 `B8BDAEFA...`；部署哈希：当前安装 `36B74510...`，新候选未部署，部署门禁待执行。
