# FishingExpanded 委派与执行器政策

本文件只在准备、复核或收尾外部模型委派时读取。只有用户明确改变委派策略、供应商边界或成本决策时才更新；普通 Case 不维护本文件。

## 主控和资格

- Codex 是项目规划、根因裁决、治理状态和最终验证的唯一主控；受委派执行器不会因被调用而取得主控权。
- 委派必须通过已加载且验证可用的 `codex-with-cc` 流程，并在项目共享委派根（当前 checkout 的 `git rev-parse --git-common-dir` 加 `gce-delegation`）使用显式 provider profile 启动。委派资格按当前 Case、证据状态和阶段动态复核，不因曾经派发而永久锁定。
- 除保留给 Codex 的事项外，可委派任务默认委派；`NotBeneficial` 必须附对比证据（主线程耗时 vs 委派耗时/成本）。不设机械委派比例，也不为提高比例制造任务。
- 本框架支持最多 10 个主线程同时运行（同一模型 `deepseek-v4-flash`、`reasoning_effort=max`，经 opencode zen 的 `/v1/responses`）。每个主线程独立使用自己的 WorkflowId 与 SessionKey；会话租约和文件锁负责并发串行化，不得绕过锁或复用其他线程的 SessionKey。
- 并行派发必须显式传 `-AllowParallel` 并带 `-Scope`：框架默认以全局 `delegate.lock` 单运行互斥，未传该参数时并发运行会被拒绝（`Another delegate_to_claude run is still active. Use -AllowParallel to bypass`）；`-AllowParallel` 仅限独立只读任务或 `-Scope` 明确不相交的可写任务，并行批次后回到串行复核。
- 若主线程已掌握充分源码、日志与调用链证据并完成根因、所有权或架构裁决，直接进入实施与验证，不得再委派同范围根因审计。

## 模型路由

默认执行器是 `opencode-flash`：opencode zen 的 `deepseek-v4-flash`，与 10 个主线程同模型。执行器走 Anthropic 原生格式（`https://opencode.ai/zen/go/v1/messages`），凭证为 `ANTHROPIC_API_KEY`（由本机环境变量 `OPENCODE_API_KEY` 映射，密钥不进项目文件）。识图类子任务使用 `opencode-vision`（`qwen3.8-max`，`thinking=disabled`，2026-08-11 实测 6–19 秒返回、不编造截断内容；备选 `kimi-k3`）。`thinking=disabled` 经 profile 的 `requestParams` 随请求下发；若未来开启思考，`max_tokens` 必须 ≥ 8000（6000 实测思考吃光额度、正文为空）。

| 任务 | 默认 profile | 说明 |
|---|---|---|
| 文件清单、限定搜索、日志时间线、明确差异列表、格式或文案检查 | `opencode-flash` | 机械取证 |
| 需要工具的源码搜索、调用者枚举、R1 清理审计、diff 初审 | `opencode-flash` | 工具型取证 |
| 复杂只读调用链、反编译证据和多人/存档生命周期枚举 | `opencode-flash` | 根因与架构裁决仍由 Codex 完成 |
| 第一处分歧、入口、失败合同和修改范围均已确定的机械实现 | `opencode-flash` | 所有权、协议、存档或生命周期不清楚时不得委派实现 |
| 已把全部事实放入提示的整理、规范化或摘要 | `opencode-flash` | 禁止把“未找到”写成“不存在” |
| 复现、回归比较与稳定回退 | `opencode-flash` | 仅在固定快照下执行 |
| 截图/图像识别、画面元素描述 | `opencode-vision` | `qwen3.8-max`（`thinking=disabled`，`max_tokens` 500 即可）；备选 `kimi-k3`；图片块经 `/v1/messages` 直传，不走 Claude Code stdin |

- DeepSeek 的唯一合法渠道是 opencode zen（`opencode-flash` / `opencode-vision`）。禁止使用 `api.deepseek.com` 直连、CC Switch 的 DeepSeek 路由，以及 bundled `delegate_to_openai_compatible_report.*` 内置的 DeepSeek/OpenAI-compatible 入口。
- `opus5`（fox/CC Switch）保留在 profile 配置中供历史工件核对，不再作为默认路由；只有用户明确指定时才使用。
- profile 名必须映射到当前可验证的模型 ID，不能用任务名或别名伪造路由。2026-08-01 以前的委派没有 v2 sidecar 或供应商记录，统一视为“路由未验证”，不得用于 provider 选择或成本比较。

## 缓存政策（方案 D）

- 实测基线（2026-08-10，`deepseek-v4-flash` 经 `/v1/messages`）：同一 system 前缀第二次调用 `cache_read_input_tokens=2944 / input_tokens=2`，命中率约 99.9%；图片块同样可缓存（1990/2021）。
- 我们的 worker 提示词以用户消息注入（stdin），不追加到 system 前缀；缓存主要取决于 Claude Code 自带 system+tools 前缀的稳定性。
- 要求：角色模板保持静态，提示词开头不放时间戳/随机 ID/run 级变量；升级或更换 `claude` CLI 版本前必须做一次缓存命中对照；不得使用 `--append-system-prompt` 注入易变内容。

## 连接和有界合同

- 代码型或工具型外部委派通过 `Invoke-ProviderDelegate.py` 的明确 provider profile 启动；report-only 委派通过 `Invoke-ProviderReport.py` 启动。共享根是同一 Fishing Git 项目的唯一机器本地设置，不得在每个 worktree 的 `.codex` 中复制。每个 profile 必须显式声明 `networkMode`；`direct` 清除代理并设置全局直连，`inherit` 才允许继承当前网络。未知模式、未知 profile 或端点不匹配必须停止，不得静默回退到 VPN、其他端点或其他模型。
- 每个 profile 必须显式锁定 `ANTHROPIC_MODEL`、三个 `ANTHROPIC_DEFAULT_*_MODEL` 和 `CLAUDE_CODE_SUBAGENT_MODEL`。启动器使用独立 `CLAUDE_CONFIG_DIR`，不得让 Claude Desktop 的当前 provider/model 设置覆盖 worker；项目 `.claude` 规则仍只作为项目权限与网络补充，不能取得模型所有权。
- provider session key 使用 `routing-v2` 命名空间，与旧启动器产生的 Claude session pool 完全隔离；不得恢复 2026-08-01 修复前的 provider 会话。
- 启动前必须只读请求 profile 的模型列表接口（opencode 为 `/v1/models`），并确认主模型、默认模型和内部子代理模型全部在返回列表内；不在列表中的 ID 立即停止。模型列表只证明供应商宣称支持，不证明本次调用实际计费路由。
- 任务名、TaskId、`-Model` 文本和 provider sidecar 都不证明最终路由。每次运行必须同时满足：v2 sidecar 配置证据、响应中的本地模型匹配、以及供应商官网或 usage API 对该次运行的模型记录。最后一项缺失时状态为“等待 provider receipt”，不得接受路由、成本或低价模型结论。
- 用户反馈的供应商记录写入 Git 忽略的 `.codex/codex_with_cc/provider-receipts.local.json`，按 RunId 保存 `verified / mismatch / not-found`、观察模型和时间。`mismatch` 或 `not-found` 必须停止该 profile 的继续分配并重新定位第一处分歧。
- 每次委派只使用当轮消息内的有界合同，写明 Case、权威证据与精确入口、能在旧代码上失败的合同、停止条件、目标、允许读写范围、禁止事项、验证命令和返回内容。
- 可写实施委派前还须记录允许范围的当前 Git diff 与已有验证基线。执行器未提供对应基线证据时不得把失败归为既有问题；未找到只能报告未解决，禁止推成不存在，禁止无区分力的重复搜索或构建。
- 可委派日志提取、限定代码搜索、写入者枚举、已确认方案的白名单实现、静态契约、构建和 diff 初审。
- 需要主动使用工具搜索源码、反编译证据或核对真实文件与行号的任务，必须使用具备工具能力的 Claude Code CLI researcher/reviewer，不得使用 report-only runner。
- report-only 任务统一使用 `Invoke-ProviderReport.py`，并显式传入 `-ProviderProfile opencode-flash`。该启动器以 Anthropic Messages 协议提交报告；它不会执行 shell 命令或修改项目文件。
- bundled `delegate_to_openai_compatible_report.*` 属于旧版 DeepSeek/OpenAI-compatible 入口，禁止用于本项目任何委派；不得用它绕过 `opencode-flash` 默认规则。

## 复核、返工和收回

- 同一 Case 或文件同时只能有一个修改者。执行器结果均为候选成果；Codex 必须复核实际 diff、契约和验证结果后，才能推进阶段或对外宣布完成。
- 执行成果若与权威证据矛盾、遗漏 TaskFile 明确入口、发生范围漂移、把“未找到”写成“不存在”，或未经基线证明便把失败归为既有问题，必须重新评估委派资格。
- 仅当根因与方案未变化、缺陷明确属于局部机械修正时，允许一次有界返工；否则由 Codex 收回或重新拆成更小子项。不得为了维持委派形式而重复调用执行器。
- 对 Git 忽略、本地使用且可恢复的辅助工具，以及根因、第一处分歧、唯一所有者和修改入口均已证实且不改变状态、协议或所有权的局部机械修正，不机械套用完整多执行器门禁。
- 已有具备工具能力的审查接受后，由主线程直接完成聚焦契约、实际 diff、构建及适用的部署核验；不得仅为规范化报告状态、重复证明相同事实或凑足样本继续委派。

## 保留给 Codex 的事项

根因裁决、架构所有权决定、治理文件与状态更新、Git 暂存/提交、正式部署、存档或配置修改、启动游戏和真实验收结论由 Codex 保留。

## 成本收尾

每个具有实现工作的委派工作流形成主线程最终接受、收回或阻塞结论后，主线程必须更新项目本地成本决策。若 `.codex/codex_with_cc/cost-summary.local.json` 的 `comparison.reportRequired=true`，须在当前收尾中向用户提交费用、首次通过率、返工次数和耗时比较，并记录已报告。

成本归因只有在 v2 本地模型证据和 provider receipt 都匹配时成立。opencode zen 网关当前在响应中返回 `cost=0` 且尚无供应商定价表或账单，`opencode-zen` 的成本归因保持 `blocked-pending-provider-receipt`，不得按 0 计费宣称免费，也不得用旧 sidecar 或缺失 receipt 的运行推断为其他供应商。Codex Sol 子线程的真实编排 token 当前不在 Claude worker 产物中，必须单列为未知成本。

不得为凑足样本增加不必要委派，也不得降低权威证据、验证或验收要求。
