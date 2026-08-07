$ErrorActionPreference = 'Stop'
$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$src = 'D:\GGGGG\FishingExpanded\Source\bin\Release\net6.0'
$tgt = 'D:\GGGGG\K1515\Mods\FishingExpanded'
$bak = "D:\GGGGG\FishingExpanded\DeploymentBackups\FishingExpanded-$ts-pre-BATCH030"
New-Item -ItemType Directory -Force -Path $bak | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bak 'i18n') | Out-Null

# 1) 备份当前安装文件
Copy-Item -LiteralPath (Join-Path $tgt 'FishingExpanded.dll') -Destination (Join-Path $bak 'FishingExpanded.dll') -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $tgt 'manifest.json') -Destination (Join-Path $bak 'manifest.json') -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $tgt 'i18n\zh.json') -Destination (Join-Path $bak 'i18n\zh.json') -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $tgt 'i18n\default.json') -Destination (Join-Path $bak 'i18n\default.json') -ErrorAction Stop

# 2) 替换部署文件
Copy-Item -LiteralPath (Join-Path $src 'FishingExpanded.dll') -Destination (Join-Path $tgt 'FishingExpanded.dll') -Force -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $src 'manifest.json') -Destination (Join-Path $tgt 'manifest.json') -Force -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $src 'i18n\zh.json') -Destination (Join-Path $tgt 'i18n\zh.json') -Force -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $src 'i18n\default.json') -Destination (Join-Path $tgt 'i18n\default.json') -Force -ErrorAction Stop

# 3) 哈希核对
$pairs = @(
  @((Join-Path $src 'FishingExpanded.dll'), (Join-Path $tgt 'FishingExpanded.dll')),
  @((Join-Path $src 'manifest.json'), (Join-Path $tgt 'manifest.json')),
  @((Join-Path $src 'i18n\zh.json'), (Join-Path $tgt 'i18n\zh.json')),
  @((Join-Path $src 'i18n\default.json'), (Join-Path $tgt 'i18n\default.json'))
)
$allOk = $true
foreach ($pr in $pairs) {
  $h1 = (Get-FileHash -LiteralPath $pr[0] -Algorithm SHA256).Hash
  $h2 = (Get-FileHash -LiteralPath $pr[1] -Algorithm SHA256).Hash
  $ok = $h1 -eq $h2
  if (-not $ok) { $allOk = $false }
  Write-Output "$ok  $([System.IO.Path]::GetFileName($pr[1]))  $h1"
}
Write-Output "BACKUP_DIR: $bak"
Write-Output ("ALL_MATCH: " + $allOk)
