# FishingExpanded 当前本机环境

本文件只保存当前机器的路径、工具和可重复命令；不记录 Git 提交、某次构建、某次部署、委派运行或历史故障。

## 路径

| 用途 | 当前值 |
|---|---|
| 工作区/项目根 | `D:\GGGGG\FishingExpanded` |
| 权威源码 | `D:\GGGGG\FishingExpanded\Source` |
| 项目文件 | `Source\FishingExpanded.csproj` |
| 构建输出 | `Source\bin\Release\net6.0` |
| 游戏/SMAPI | `D:\GGGGG\K1515` |
| FishingExpanded 安装目录 | `D:\GGGGG\K1515\Mods\FishingExpanded` |
| SMAPI 最新日志 | `C:\Users\braveturtle\AppData\Roaming\StardewValley\ErrorLogs\SMAPI-latest.txt` |
| 运行证据 | `RuntimeEvidence` |
| 反编译证据 | `_analysis` |
| 部署备份 | `DeploymentBackups` |

## 工具

| 工具 | 当前入口 |
|---|---|
| .NET | `C:\Program Files\dotnet\dotnet.exe`，当前可解析 `dotnet` |
| Ripgrep | 当前可解析 `rg` |
| Git | 当前可解析 `git`；本项目使用根目录自己的 `.git` |
| ilspycmd | `C:\Users\braveturtle\.dotnet\tools\ilspycmd.exe` |

## 构建

在 `Source` 目录运行，构建前先确认当前项目树和依赖状态：

```powershell
dotnet restore '.\FishingExpanded.csproj' --force --configfile "$env:APPDATA\NuGet\NuGet.Config"
dotnet build '.\FishingExpanded.csproj' -c Release --no-restore -t:Rebuild
```

`GamePath` 当前写在 `FishingExpanded.csproj` 中；其他安装必须显式传入新的 `GamePath`。不要在项目根无参数执行 `dotnet build`。`Source\manifest.json` 和 `Source\i18n` 是发布文件来源，不能从旧的 `bin` 目录反向恢复源码。

## 日志与部署

- 新游戏启动可能覆盖 `SMAPI-latest.txt`；有价值日志先复制到 `RuntimeEvidence`，目标同名时先拒绝覆盖并核对哈希。
- 部署目录的 `config.json` 属于用户安装状态；没有明确授权时不修改、不覆盖。
- 版本文字可能同时出现在源码 manifest、构建输出和项目文档中；发布判断必须核对实际待部署文件和哈希，不能只看文档版本。
- 本项目与 `D:\GGGGG\codex working space` 是两个独立 Git 根；不得跨仓库引用提交、分支或工作树状态。
