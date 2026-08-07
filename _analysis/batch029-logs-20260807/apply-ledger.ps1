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
$p = 'D:\GGGGG\FishingExpanded\BUG-LEDGER.md'
$old1 = '- `Source\manifest.json` 与 `Source\FishingExpanded.csproj` 当前均为 `0.5.10`；旧批次版本号只作历史证据，不阻塞本轮部署。'
$new1 = $old1 + $nl + '- BATCH-030（2026-08-07 实测回归 + 设计变更，见 `Governance\BATCH-030-FailureRecorded-RankWeakPrompt.md`）：BATCH-029 部署版被实测推翻——失败分支误删 `FailureRecorded=true`，一次失败被重复结算 18~51 次直接扣到底（虹鳟鱼/太阳鱼/狗鱼，其他电脑 D:\K1515 三份日志已封存 `RuntimeEvidence\20260807-BATCH029-REGRESSION-01`）；“稍微强一点的个体”不应出现在星标挑战宣言、零以下胜利应显示该弱称号提示（原 ≤0 跳过）；已排除存档不兼容（新字段缺失默认 0，两份存档加载正常）。修复版已构建并部署，真实验收待用户执行，每现象独立标记。'
Edit-File $p $old1 $new1
$old2 = '| BATCH-029 | 尺寸数字等级线性化 + 连续失败史诗提示 + 成功额外等级 + 加速度 30% 档 + 收藏页皇冠 | ✅ 已部署 | 2026-08-06 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |'
$new2 = $old2 + $nl + '| BATCH-030 | 失败只记录一次回归（FailureRecorded）+ 弱称号文案场景修正 | ✅ 已部署 | 2026-08-07 | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |'
Edit-File $p $old2 $new2
Write-Output 'LEDGER DONE'
