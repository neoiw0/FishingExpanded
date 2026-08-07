$ev = 'D:\GGGGG\FishingExpanded\RuntimeEvidence\20260807-BATCH029-REGRESSION-01'
New-Item -ItemType Directory -Force -Path $ev | Out-Null
$files = @(
  @('D:\GGGGG\FishingExpanded\_analysis\batch029-logs-20260807\SMAPI-latest.txt','SMAPI-latest-2125-session-D-K1515.txt'),
  @('D:\GGGGG\FishingExpanded\临时\SMAPI-latest.txt','SMAPI-latest-2144-session-D-K1515.txt'),
  @('D:\GGGGG\codex working space\其他电脑日志\SMAPI-latest.txt','SMAPI-latest-2009-session-D-K1515-no-FishingExpanded.txt')
)
$lines = @()
foreach ($f in $files) {
  if (Test-Path -LiteralPath $f[0]) {
    $dest = Join-Path $ev $f[1]
    if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Force }
    Copy-Item -LiteralPath $f[0] -Destination $dest -ErrorAction Stop
    $h = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash
    $lines += ($h + '  ' + $f[1])
  }
}
Set-Content -LiteralPath (Join-Path $ev 'SHA256.txt') -Value $lines -Encoding UTF8
Get-ChildItem -LiteralPath $ev | Select-Object Length,Name
Get-Content -LiteralPath (Join-Path $ev 'SHA256.txt')
