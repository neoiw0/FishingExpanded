# BATCH-035 一键自动验收（无需启动游戏）：harness 30 项 + CFG 1313 + JIT + PatchAll + i18n 同步 + 哈希
# 用法: powershell -File verify.ps1 [-Dll <path>] [-I18n <dir>]
param(
    [string]$Dll = 'D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll',
    [string]$I18n = 'D:\GGGGG\K1515\Mods\FishingExpanded\i18n',
    [string]$SourceDll = 'D:\GGGGG\FishingExpanded\Source\bin\Release\net6.0\FishingExpanded.dll'
)
$ErrorActionPreference = 'Stop'
$root = 'D:\GGGGG\FishingExpanded'
$fail = 0

Write-Host "== 1/5 助战 harness (30 项)" -ForegroundColor Cyan
dotnet run --project "$root\_analysis\batch035-assist-check\check.csproj" -- $Dll $I18n
if ($LASTEXITCODE -ne 0) { $fail++ }

Write-Host "== 2/5 CFG 类型模拟 (1313 块)" -ForegroundColor Cyan
dotnet run --project "$root\_analysis\batch034-transpiler-debug\debug.csproj" -- $Dll | Select-String 'TYPE SIM'
if ($LASTEXITCODE -ne 0) { $fail++ }

Write-Host "== 3/5 单方法 JIT" -ForegroundColor Cyan
dotnet run --project "$root\_analysis\batch029-transpiler-check\check.csproj" -- $Dll | Select-String 'PASS'
if ($LASTEXITCODE -ne 0) { $fail++ }

Write-Host "== 4/5 PatchAll" -ForegroundColor Cyan
dotnet run --project "$root\_analysis\batch029-transpiler-check\check.csproj" -- $Dll --patchall | Select-String 'PASS'
if ($LASTEXITCODE -ne 0) { $fail++ }

Write-Host "== 5/5 哈希与 i18n 同步" -ForegroundColor Cyan
$srcHash = (Get-FileHash -LiteralPath $SourceDll -Algorithm SHA256).Hash
$instHash = (Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash
Write-Host "src : $srcHash"
Write-Host "inst: $instHash"
if ($srcHash -ne $instHash) { Write-Host 'FAIL: DLL 哈希不一致' -ForegroundColor Red; $fail++ } else { Write-Host 'DLL 哈希一致' -ForegroundColor Green }
foreach ($lang in 'zh','default') {
    $a = (Get-FileHash -LiteralPath (Join-Path $I18n "$lang.json") -Algorithm SHA256).Hash
    $b = (Get-FileHash -LiteralPath "$root\Source\i18n\$lang.json" -Algorithm SHA256).Hash
    if ($a -ne $b) { Write-Host "FAIL: i18n/$lang.json 不一致" -ForegroundColor Red; $fail++ } else { Write-Host "i18n/$lang.json 一致" -ForegroundColor Green }
}

if ($fail -eq 0) { Write-Host "==== 全部通过 ====" -ForegroundColor Green; exit 0 }
Write-Host "==== $fail 项失败 ====" -ForegroundColor Red; exit 1