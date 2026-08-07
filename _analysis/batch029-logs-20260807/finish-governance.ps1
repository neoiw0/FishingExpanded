$ErrorActionPreference = 'Stop'
$nl = [string]"`n"
function Edit-File([string]$path, [string]$old, [string]$new) {
  $bytes = [System.IO.File]::ReadAllBytes($path)
  $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
  $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
  if (-not $text.Contains($old)) { throw "PATTERN NOT FOUND in $path" }
  $text = $text.Replace($old, $new)
  if ($hasBom) { [System.IO.File]::WriteAllText($path, $text, [System.Text.Encoding]::UTF8) }
  else { [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false))) }
  Write-Output "EDITED $path"
}

# 1) TESTING.md 两行矩阵
$p = 'D:\GGGGG\FishingExpanded\TESTING.md'
$old1 = '| 钓鱼生命周期 | 抛竿、小游戏更新、成功/失败判定、回调、鱼获提交和难度结算顺序符合当前原生调用链 |'
$new1 = $old1 + ' 失败判定只结算一次（BATCH-030 恢复 `FailureRecorded` 防重，单次失败不得重复扣级）'
Edit-File $p $old1 $new1
$old2 = '同鱼种连续失败 ≥2 次且调整后难度 ≥150 时失败提示为史诗文案（成功清零，BATCH-029） |'
$new2 = '同鱼种连续失败 ≥2 次且调整后难度 ≥150 时失败提示为史诗文案（成功清零，BATCH-029）；难度等级 <1（0 和负数）的胜利显示弱称号“额...稍微强一点的个体”提示，该弱称号不嵌入星标挑战宣言（BATCH-030） |'
Edit-File $p $old2 $new2

# 2) LOCAL-PITFALLS 新增踩坑
$p2 = 'D:\GGGGG\FishingExpanded\LOCAL-PITFALLS.md'
$old3 = '- PowerShell 数组字面量 `@(''a'',''b''+''c'')`：'
if (-not (Get-Content -Raw -LiteralPath $p2).Contains('- 复杂内联 PowerShell 命令')) {
  $anchor = '- 文件修改使用聚焦 patch；超长方法修改后立即查看前后文，不能只依赖编译成功。'
  $new3 = '- 复杂内联 PowerShell 命令（含嵌套单引号、数组字面量或较长中文）经 shell 工具包装后可能被改写，表现为命令被拒（`rejected: blocked by policy`）或静默失效；先写入临时 `.ps1` 文件再执行，避免逐条调试内联引用。' + $nl + $anchor
  Edit-File $p2 $anchor $new3
}

# 3) 活动卡状态与部署事实
$p3 = 'D:\GGGGG\FishingExpanded\Governance\BATCH-030-FailureRecorded-RankWeakPrompt.md'
$old4 = '**状态**：取证 ✅（根因已证实）→ R0 ✅ → 构建/部署完成，真实验收待用户执行'
$new4 = '**状态**：取证 ✅（根因已证实）→ R0 ✅ → 已部署（2026-08-07 22:49），真实验收待用户执行'
Edit-File $p3 $old4 $new4
$old5 = '## 构建与部署事实

（部署后填写哈希）

## Closeout 门禁（2026-08-07）

（收尾填写：治理同步、Git 基线、聚焦 diff、委派评估、构建结果、部署哈希、进程门禁）'
$new5 = '## 构建与部署事实

- Release Rebuild（2026-08-07）：0 警告 0 错误。
- ilspycmd 反编译核验：`BobberBarPatches.Update_Postfix` 恢复 `value.FailureRecorded = true;`（反编译 241 行）；`HUDNotifier` 已无“等级过低，跳过提示”分支；`ChallengeDialogueGenerator` 含 `hud.starChallenge.template.weak` 条件。
- 产物 SHA-256：`FishingExpanded.dll` `108C3A2C05160AE9DCBCA285D2D9D13D78EFF9020CAF3792414A64A51B8B2C7B`；`manifest.json` `B906DCF8E2BC07D44F74A630B1B58BC550CD07E99849DAF96776E6853C2A1CEB`（未变）；`i18n\zh.json` `6BC92C0E71C1E9E203153E13B920684198D0091F3D2F9CF83AE04C614066BEC8`；`i18n\default.json` `1678B72CE6F31972C6606992F527A8FFB190E608B9989F98DE79688DE57C5CA1`；三份 JSON 解析通过。
- 部署（2026-08-07 22:49，用户既有部署授权延续）：备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`（含被替换 DLL `26DEE6F2...` 与旧 i18n/manifest）；4 文件源/目标 SHA-256 逐一一致；部署目录无 `config.json`（未涉及）。
- 进程门禁：Stardew Valley.exe 未运行；PID 10556 僵尸进程（0 线程/0 句柄）不锁定 DLL（独占打开验证通过）。

## Closeout 门禁（2026-08-07）

- 治理同步：本卡 + `BUG-LEDGER.md`（活动块/表格行）+ `GAME-DESIGN.md`（2.1 成功提示、4.5 宣言弱称号）+ `TESTING.md`（2 行矩阵）+ `LOCAL-PITFALLS.md`（内联命令踩坑）已同步。
- Git 基线：HEAD `8f799da`；工作树为 BATCH-024~030 混改未提交基线（不 `git add -A`，用户未要求提交）；本轮只改 BATCH-030 范围内文件。
- 聚焦 diff：`BobberBarPatches.cs`（恢复 `FailureRecorded=true`）、`HUDNotifier.cs`（删除 ≤0 跳过）、`ChallengeDialogueGenerator.cs`（weak 模板）、`i18n`×2（`hud.starChallenge.template.weak`）、`GAME-DESIGN.md`、`BUG-LEDGER.md`、`TESTING.md`、本卡。
- 委派评估：不委派（Luna 当前模型禁止委派）。
- 构建结果：Release Rebuild 0 警告 0 错误；ilspycmd 反编译核验关键符号齐全。
- 部署哈希：源/目标 4 文件一致；备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`。
- 进程门禁：游戏未运行；僵尸 PID 10556 不锁定文件。
- 检查点：本卡与总账形成逻辑检查点；不代表真实游戏验收。'
Edit-File $p3 $old5 $new5

# 4) BUG-LEDGER 哈希
$p4 = 'D:\GGGGG\FishingExpanded\BUG-LEDGER.md'
$old6 = '修复版已构建并部署，真实验收待用户执行，每现象独立标记。'
$new6 = '修复版已构建并部署（DLL `108C3A2C...`，备份 `DeploymentBackups\FishingExpanded-20260807-224903-pre-BATCH030`），真实验收待用户执行，每现象独立标记。'
Edit-File $p4 $old6 $new6

Write-Output 'GOVERNANCE DONE'
