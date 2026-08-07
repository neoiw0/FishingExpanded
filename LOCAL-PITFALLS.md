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
- 复杂内联 PowerShell 命令（含嵌套单引号、数组字面量或较长中文）经 shell 工具包装后可能被改写，表现为命令被拒（`rejected: blocked by policy`）或静默失效；先写入临时 `.ps1` 文件再执行，避免逐条调试内联引用。
- 文件修改使用聚焦 patch；超长方法修改后立即查看前后文，不能只依赖编译成功。
- 递归 Remove-Item -Recurse 等清理命令可能被执行策略直接拒绝（命令被拦，不是语法错误）；优先使用不存在的新目录名避免删除，确需删除时先解析并核对目标在预期工作区内。

## 补丁与编辑

- `apply_patch`（Codex 桌面 shim，指向 WindowsApps 的 codex.exe）从本机 shell 调用始终返回 "Access is denied"，不可用。
- `git apply` 在本机对所有补丁（含纯 ASCII、行内容逐字节匹配）始终返回 "patch does not apply"，不可用；不要花时间排查。
- 工作区文件编辑使用 PowerShell 精确子串替换：显式 `[System.Text.Encoding]::UTF8` 读写（避免 GBK 乱码）、用单引号字符串（避免反引号转义）、保留原行尾，修改后立即 `git diff` 核对。

## Harmony 与 IL 注入

- Transpiler 注入 IL 时禁止对 MonoGame `Vector2` 结构体直接使用 `OpCodes.Add`；coreclr JIT 会以 `0xc0000005` 访问冲突崩溃（崩溃点紧跟 Transpiler 日志，启动 2/2 复现）。应使用“消费式”方法（如 `AdjustLandingFishPosition(Vector2 position, FishingRod rod)`：吞掉栈上已有向量并返回调整后结果），只注入 `Ldarg_0 + Call`，不再注入 `Add`。
- 对 float32 注入 `Mul`（`ldc.r4 3 * GetFishVisualScale`）经验证安全；结构与旧 DLL 可运行模式一致时才合入。
- 候选 DLL 启动崩溃时先做对照（临时装回旧 DLL 同一方式启动），再用二分构建定位（禁用整块→禁用部分→单指令级），每一步保存 SMAPI 日志与事件日志证据。

## XNB 与贴图取证

- Codex 桌面环境 `view_image` 返回 [Unsupported Image] 不可用；图标/贴图识别使用 PIL ASCII 渲染（金色 `#`、亮白 `+`、暗色 `.`/`:`、透明空格）或精确 RGB 打印。
- 本机 XNB（K1515 内容）为 LZX 压缩；自建解压工具反射 MonoGame `LzxDecoder.Decompress` 可用，但解压流**不含** `XNB` 头，直接从类型清单开始（首字节 = 类型数 7-bit）。LZX chunk 头：普通 = 2 字节输入长度（解压 0x8000）；`b0==0xFF` 时为 5 字节扩展头。
- 纹理 XNB payload 头：`format u32`（0=RGBA）+ 10 字节 + `width/height/mipCount/dataSize` 各 u32 + 像素数据（偏移随清单长度变化，用 size 反推校验）。
- Stardew 1.6 `Data/*.xnb` 字典字符串每条尾随一个 `0x02` 字节（自定义管线）；解析 `Dictionary<string,string>` 时读 [7-bit 长度+字节] 后必须再跳 1 字节，否则立即错位。
- `Characters\Farmer\hats.xnb`：240×880 RGBA；每 80px 带 = 12 顶帽子 × 4 个方向副本（`y = (idx*20/240)*80 + direction*20`，x = `(idx*20)%240`）；`Data/hats.xnb` 的 SpriteIndex 在字段 6，缺省用数值键。

## 构建、取证与部署

- 项目文件是 `Source\FishingExpanded.csproj`；项目根无参数构建可能找不到项目或使用错误 cwd。
- `Source\bin` 和 `Source\obj` 是生成物，不是当前源码证据；构建前后分别核对时间、文件清单和哈希。
- 反编译必须优先使用当前安装 DLL；`_analysis` 中旧文件不能单独证明当前运行时契约。
- `manifest.json` 来自 `Source\manifest.json`，i18n 来自 `Source\i18n`；部署文件不能机械拼接同一源目录。
- 构建、部署和游戏内实测分开记录；部署成功不证明 Patch 运行，日志加载成功也不证明玩家现象已修复。
- 部署前执行目标进程/DLL 占用检查，保留备份并核对源/目标哈希；没有用户明确授权不启动游戏或部署。
- PowerShell 数组字面量 `@('a','b'+'c')`：逗号优先级低于 `+` 且左操作数为数组时 `+` 做**数组拼接**，结果 4 元素 `['a','a',"`n",'c']`，`$arr[0]==$arr[1]` 导致 `Replace` 静默无操作。文件写入类脚本改用显式变量（`$old=...; $new=...`）并逐字节复核。
- Harmony Transpiler 注入：`Ldarg_0; Ldfld field` 中 `Ldfld` 会**消费**刚推入的 `this`；修改被注入方法签名（如 `(float)` → `(BobberBar,float)`）必须重新推演栈序（需要实例时用 `Dup` 或补一次 `Ldarg_0`）。Harmony/MonoMod 在补丁应用时经 `RuntimeHelpers.PrepareMethod` 强制 JIT，参数栈类型不匹配会当场抛 `InvalidProgramException: Common Language Runtime detected an invalid program`，表现为“Mod 初始化失败”且整包补丁不生效。
- 验证 Transpiler 是否合法可写独立测试台（`_analysis\batch029-transpiler-check`）：加载真实游戏 DLL + 待测 Mod DLL，`Harmony.Patch(original, null,null,transpiler,null)` 后强制 JIT；但 `--patchall` 模式因测试台缺少 SMAPI Mod 上下文（`ModEntry.ModMonitor` 为 null）会在 `FishingRod.draw` 等注入处 NRE——必须用已知正常旧版 DLL 做对照排除环境伪影。
