# FishingExpanded 当前本机踩坑

只保留仍会重复影响工作的机器和工具事实；单次会话、旧插件版本和已解决事件不追加。

## 路径与搜索

- 每条命令把 cwd 与相对路径成对核对；在 `Source` cwd 使用 `ModEntry.cs`，项目根才使用 `Source\ModEntry.cs`。
- Windows PowerShell 不展开 `Source/*.cs`；使用 `rg pattern Source -g '*.cs'`。
- `rg` 无匹配返回 1，不等于命令或路径失败；需要区分退出码和错误输出。
- `rg --` 会结束选项解析；`-g` 必须放在 `--` 前。
- PowerShell 数组不要直接作为 `rg` 的一个位置参数；多文件搜索使用 `foreach`。
- 复杂固定文本优先使用 `rg -F`；长绝对路径从已验证根路径派生。

## PowerShell 与文件

- PowerShell 5.1 处理中文文件时显式使用 UTF-8；控制台乱码不等于文件损坏。
- `Copy-Item` 的缺失源可能是非终止错误；关键复制使用 `-ErrorAction Stop` 并核对目标。
- 保存日志前先拒绝同名目标；不要依赖 Windows PowerShell 5.1 不支持的 `Copy-Item -NoClobber` 行为。
- 不把 `Format-Table` 结果交给后续文本筛选；先处理原始对象。
- `Select-String -InputObject $lines` 会把数组当作一个对象；搜索文件时使用 `-LiteralPath`。
- 文件修改使用聚焦 patch；超长方法修改后立即查看前后文，不能只依赖编译成功。

## 构建、取证与部署

- 项目文件是 `Source\FishingExpanded.csproj`；项目根无参数构建可能找不到项目或使用错误 cwd。
- `Source\bin` 和 `Source\obj` 是生成物，不是当前源码证据；构建前后分别核对时间、文件清单和哈希。
- 反编译必须优先使用当前安装 DLL；`_analysis` 中旧文件不能单独证明当前运行时契约。
- `manifest.json` 来自 `Source\manifest.json`，i18n 来自 `Source\i18n`；部署文件不能机械拼接同一源目录。
- 构建、部署和游戏内实测分开记录；部署成功不证明 Patch 运行，日志加载成功也不证明玩家现象已修复。
- 部署前执行目标进程/DLL 占用检查，保留备份并核对源/目标哈希；没有用户明确授权不启动游戏或部署。
