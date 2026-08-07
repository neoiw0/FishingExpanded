$ErrorActionPreference = 'Stop'

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

$nl = [string]"`n"

# 1) BobberBarPatches.cs - 恢复 FailureRecorded = true
$p1 = 'D:\GGGGG\FishingExpanded\Source\Patches\BobberBarPatches.cs'
$old1 = '                    HUDNotifier.ShowFailureNotification(data.FishId, newLevel, isEpicChampion);' + $nl + '                    data.ResultStarted = true;'
$new1 = '                    HUDNotifier.ShowFailureNotification(data.FishId, newLevel, isEpicChampion);' + $nl + '                    data.FailureRecorded = true; // BATCH-030: 防止淡出动画期间重复记录失败（BATCH-029 重写时误删）' + $nl + '                    data.ResultStarted = true;'
Edit-File $p1 $old1 $new1

# 2) HUDNotifier.cs - 移除 <=0 跳过
$p2 = 'D:\GGGGG\FishingExpanded\Source\Services\HUDNotifier.cs'
$old2 = '                // BATCH-022: 低等级时不显示提示（避免水草等第一次钓就提示）' + $nl + '                if (newLevel <= 0)' + $nl + '                {' + $nl + '                    ModEntry.ModMonitor.Log(' + $nl + '                        $"[HUDNotifier] 等级过低，跳过提示 | 鱼: {fishName} ({fishId}) | 等级: {newLevel}",' + $nl + '                        StardewModdingAPI.LogLevel.Debug);' + $nl + '                    return;' + $nl + '                }' + $nl + $nl
$new2 = '                // BATCH-030: 难度等级 <1（0 级和负数）的胜利也显示称号提示（弱称号“额...稍微强一点的个体”）；' + $nl + '                // 取代 BATCH-022 的 ≤0 跳过逻辑。' + $nl
Edit-File $p2 $old2 $new2

# 3) ChallengeDialogueGenerator.cs - weak 模板
$p3 = 'D:\GGGGG\FishingExpanded\Source\Utils\ChallengeDialogueGenerator.cs'
$old3 = '                return ModEntry.ModHelper.Translation.Get(' + $nl + '                    "hud.starChallenge.template",'
$new3 = '                // BATCH-030: 弱称号（等级 <1）只用于零以下胜利提示，不嵌入挑战宣言。' + $nl + '                string templateKey = rankKey == "rank.weak"' + $nl + '                    ? "hud.starChallenge.template.weak"' + $nl + '                    : "hud.starChallenge.template";' + $nl + '                return ModEntry.ModHelper.Translation.Get(' + $nl + '                    templateKey,'
Edit-File $p3 $old3 $new3

# 4) zh.json
$p4 = 'D:\GGGGG\FishingExpanded\Source\i18n\zh.json'
$old4 = '  "hud.starChallenge.template": "{{honorific}}{{fishName}}{{rank}}{{resolve}}{{action}}。",'
$new4 = '  "hud.starChallenge.template": "{{honorific}}{{fishName}}{{rank}}{{resolve}}{{action}}。",' + $nl + '  "hud.starChallenge.template.weak": "{{honorific}}{{fishName}}{{resolve}}{{action}}。",'
Edit-File $p4 $old4 $new4

# 5) default.json
$p5 = 'D:\GGGGG\FishingExpanded\Source\i18n\default.json'
$old5 = '  "hud.starChallenge.template": "{{honorific}} {{fishName}}, the {{rank}}, {{resolve}} {{action}}.",'
$new5 = '  "hud.starChallenge.template": "{{honorific}} {{fishName}}, the {{rank}}, {{resolve}} {{action}}.",' + $nl + '  "hud.starChallenge.template.weak": "{{honorific}} {{fishName}} {{resolve}} {{action}}.",'
Edit-File $p5 $old5 $new5

# 6) GAME-DESIGN.md 2.1
$p6 = 'D:\GGGGG\FishingExpanded\GAME-DESIGN.md'
$old6 = '- 当本次成功后的难度等级小于等于0时，不显示上述挑战称号提示。'
$new6 = '- 当本次成功后的难度等级小于1（0 级和负数）时，仍显示上述提示，称号为“额...稍微强一点的个体”（2026-08-07 用户确认：零以下胜利应显示弱称号提示，取代旧设计“小于等于0不显示”；BATCH-030）。'
Edit-File $p6 $old6 $new6

# 7) GAME-DESIGN.md 4.5
$old7 = '- 文案结构：`{尊敬词}{鱼名}{鱼的等级}{毅然决然词}{应战说法}。`'
$new7 = '- 文案结构：`{尊敬词}{鱼名}{鱼的等级}{毅然决然词}{应战说法}。`（等级 <1 的弱称号除外，见下）' + $nl + '- 等级 <1 的鱼：宣言不包含“额...稍微强一点的个体”称号文本，只显示 `{尊敬词}{鱼名}{毅然决然词}{应战说法}。`（该称号只用于零以下胜利提示；2026-08-07 用户确认，BATCH-030）。'
Edit-File $p6 $old7 $new7

Write-Output 'ALL EDITS DONE'
