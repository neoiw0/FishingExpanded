# Fishing Expanded — Nexus Release Guide (Bilingual)

> Target: publish `FishingExpanded 1.0.0` to Nexus Mods, ID `50595`.
> Bilingual: English full first, then Chinese full. Follow the steps in order and tick each check item.

---

# ENGLISH

## 0. Pre-flight Checklist

- [ ] In `Source\manifest.json`:
  - `UniqueID` = `neoiw.FishingExpanded`
  - `UpdateKeys` = `["Nexus:50595"]`
  - `Version` = `1.0.0`
- [ ] `LICENSE` (GPL-3.0) exists at the project root.
- [ ] Player wiki and page copy are ready (`Release\NEXUS-PAGE-CONTENT.md`, `Release\PLAYER-WIKI-NEXUS-BBCODE.txt`, `Release\PLAYER-WIKI-NEXUS.html`).
- [ ] At least one smoke test on a real save (recommended: normal fishing, high-difficulty fish, challenge bait, collection crowns, save reload, and co-op/split-screen if possible).

> Note: a successful build is not the same as a real test. The last step before publishing is to download your own upload, install it into a clean `Mods` folder, and launch it once.

## 1. Rebuild Release

In PowerShell, go to `D:\GGGGG\FishingExpanded\Source` and run:

```powershell
dotnet restore '.\FishingExpanded.csproj' --force --configfile "$env:APPDATA\NuGet\NuGet.Config"
dotnet build '.\FishingExpanded.csproj' -c Release --no-restore -t:Rebuild
```

Output directory:

```
D:\GGGGG\FishingExpanded\Source\bin\Release\net6.0
```

Check:

- [ ] `FishingExpanded.dll` exists
- [ ] `manifest.json` is the updated version (UniqueID/UpdateKeys correct)
- [ ] `i18n\default.json` and `i18n\zh.json` exist

## 2. Create the Release Zip

Use this exact structure (the outer folder MUST be named `FishingExpanded`):

```
FishingExpanded-1.0.0.zip
└── FishingExpanded/
    ├── FishingExpanded.dll
    ├── manifest.json
    ├── i18n/
    │   ├── default.json
    │   └── zh.json
    ├── LICENSE
    └── README.txt
```

Do NOT package `.deps.json`, `.pdb`, source code, or `config.json`.

`Release\README.txt` and the root `LICENSE` are already prepared. After zipping, extract to a temporary folder and verify the structure.

## 3. Log In to Nexus Mods

1. Open https://www.nexusmods.com
2. Log in.
3. Go to **Mods → Upload** (or https://www.nexusmods.com/users/myaccount?tab=mod+files to upload to an existing mod).
4. If ID `50595` is an assigned page, go to its admin page. For a brand-new mod, follow the on-screen creation flow.

## 4. Fill In the Mod Page

Open `Release\NEXUS-PAGE-CONTENT.md` and copy by table:
(Player wiki is already converted to a paste-ready details format: `Release\PLAYER-WIKI-NEXUS-BBCODE.txt`, with `Release\PLAYER-WIKI-NEXUS.html` as backup.)

- **Mod Name**: `Fishing Expanded`
- **Summary**: English or Chinese short summary (English recommended)
- **Category**: Gameplay Tweaks or Fishing
- **Description**: Paste the full bilingual player wiki (English half + Chinese half) from the BBCode/HTML files
- **Tags**: Fishing, Expansion, Gameplay, Difficulty, Crowns, Challenge, Open Source, Vanilla-Friendly, Co-op, Quality of Life

## 5. Upload the File

Upload `FishingExpanded-1.0.0.zip` and fill in:

| Field | Value |
|---|---|
| Mod version | 1.0.0 |
| Game version | 1.6 (or 1.6+ if required) |
| SMAPI version | 4.0.0+ (if present) |
| File name | `FishingExpanded-1.0.0.zip` |
| Changelog | Paste the 1.0.0 section from `Release\CHANGELOG.md` |

## 6. Upload Images / Videos

Follow the screenshot checklist in `NEXUS-PAGE-CONTENT.md`:

- [ ] Cover image (high-difficulty minigame battle)
- [ ] Collection crowns + challenge level
- [ ] Giant fish + NPC bubble
- [ ] Challenge bait / fish jump telegraph
- [ ] Big catch or Starfruit Tea
- [ ] Optional short GIF/video

## 7. Additional Info

- **Requirements / Dependencies**: add SMAPI as a requirement; add Generic Mod Config Menu as optional.
- **Permissions**: choose open-source options consistent with GPL-3.0 (modify/share/redistribute, derivatives under the same license, credit required).
- **Source code**: https://github.com/neoiw0/FishingExpanded
- **Credits**: author `neoiw`; Stardew Valley / ConcernedApe; the SMAPI team; and everyone who tested.

## 8. Publish

1. Save the page, then click **Publish Mod**.
2. After publishing, open your page logged OUT to check title, summary, description rendering, images, and the download button.
3. Download your uploaded zip, install into a clean `Mods` folder, and launch with SMAPI once. Confirm:
   - SMAPI log shows `Fishing Expanded 1.0.0` loaded
   - No red errors
   - `UpdateKeys` reports no problem
4. If anything is wrong, hide the version or mark it as outdated on Nexus; do not leave a broken file.

## 9. Post-Publish Checklist

- [ ] Page is public
- [ ] File is uploaded and downloadable
- [ ] Update check correctly points to `Nexus:50595`
- [ ] Source repository is public and linked
- [ ] License is correct (GPL-3.0)
- [ ] Player wiki / documentation has an entry (Description or source repo README)

## 10. FAQ

- **I don't have a source repository yet.** "Contagious open source" means source must be available. Create a GitHub/Gitee repo, push the current project, then add the URL to Nexus and the README.
- **The ID 50595 doesn't match my new page?** Use the ID actually assigned by Nexus; then correct `UpdateKeys` in `manifest.json`.
- **I want a different license.** Replace the root `LICENSE`, update `Release\README.txt`, `NEXUS-PAGE-CONTENT.md`, and the Nexus Permissions, then recreate the zip.
- **What about the old UniqueID?** The new release uses `neoiw.FishingExpanded`. If old installs exist, mention in the Description that existing users can simply overwrite.

---

# 中文

## 0. 发布前核对

- [ ] `Source\manifest.json` 已更新：
  - `UniqueID` = `neoiw.FishingExpanded`
  - `UpdateKeys` = `["Nexus:50595"]`
  - `Version` = `1.0.0`
- [ ] 项目根目录已有 `LICENSE`（GPL-3.0）。
- [ ] 玩家 Wiki 和页面文案已准备好（`Release\NEXUS-PAGE-CONTENT.md`、`Release\PLAYER-WIKI-NEXUS-BBCODE.txt`、`Release\PLAYER-WIKI-NEXUS.html`）。
- [ ] 已在真实存档做过至少一次冒烟测试（建议：普通钓鱼、高难鱼、挑战鱼饵、图鉴皇冠、存档重载、双人/联机如果条件允许）。

> 注意：构建成功 ≠ 已实测。发布前最后一步是亲自下载自己上传的包，装到干净 `Mods` 目录启动一次。

## 1. 重新构建 Release

在 PowerShell 中进入 `D:\GGGGG\FishingExpanded\Source`，运行：

```powershell
dotnet restore '.\FishingExpanded.csproj' --force --configfile "$env:APPDATA\NuGet\NuGet.Config"
dotnet build '.\FishingExpanded.csproj' -c Release --no-restore -t:Rebuild
```

输出目录：

```
D:\GGGGG\FishingExpanded\Source\bin\Release\net6.0
```

核对：

- [ ] `FishingExpanded.dll` 存在
- [ ] `manifest.json` 是更新后的版本（UniqueID/UpdateKeys 正确）
- [ ] `i18n\default.json` 和 `i18n\zh.json` 存在

## 2. 制作发布 Zip

请使用以下精确结构（外层文件夹名必须是 `FishingExpanded`）：

```
FishingExpanded-1.0.0.zip
└── FishingExpanded/
    ├── FishingExpanded.dll
    ├── manifest.json
    ├── i18n/
    │   ├── default.json
    │   └── zh.json
    ├── LICENSE
    └── README.txt
```

不需要打包 `.deps.json`、`.pdb`、源码或 `config.json`。

我们已经准备了 `Release\README.txt` 和项目根 `LICENSE`。制作 zip 后，请自行解压到临时目录验证结构。

## 3. 登录 Nexus Mods

1. 打开 https://www.nexusmods.com
2. 登录你的账号。
3. 进入 **Mods → Upload**（或通过 https://www.nexusmods.com/users/myaccount?tab=mod+files 上传到已有 Mod）。
4. 如果 ID `50595` 是已分配的 Mod 页面，直接进该页面后台；如果是全新发布，按页面提示创建 Mod。

## 4. 填写 Mod 页面信息

打开 `Release\NEXUS-PAGE-CONTENT.md`，按表格复制：
（玩家版 Wiki 已转成可直接粘贴的详情页格式，见 `Release\PLAYER-WIKI-NEXUS-BBCODE.txt`，备选 `Release\PLAYER-WIKI-NEXUS.html`。）

- **Mod 名称**：`Fishing Expanded`
- **摘要**：英文或中文短摘要（建议英文）
- **分类**：Gameplay Tweaks 或 Fishing
- **描述**：从 BBCode/HTML 文件复制完整双语玩家 Wiki（先英文半段，后中文半段）
- **标签**：Fishing、Expansion、Gameplay、Difficulty、Crowns、Challenge、Open Source、Vanilla-Friendly、Co-op、Quality of Life

## 5. 上传文件

上传 `FishingExpanded-1.0.0.zip`，填写：

| 字段 | 值 |
|---|---|
| Mod version | 1.0.0 |
| Game version | 1.6（或按要求填 1.6+） |
| SMAPI version | 4.0.0+（若该字段存在） |
| File name | `FishingExpanded-1.0.0.zip` |
| Changelog | 粘贴 `Release\CHANGELOG.md` 中 1.0.0 的内容 |

## 6. 上传图片 / 视频

按 `NEXUS-PAGE-CONTENT.md` 第 7 节截图清单：

- [ ] 封面图（高难度钓鱼小游戏战斗）
- [ ] 图鉴皇冠 + 挑战等级
- [ ] 巨型鱼 + NPC 冒泡
- [ ] 挑战鱼饵 / 鱼跃前摇
- [ ] 大量鱼获或星之果茶
- [ ] 可选短视频/动图

## 7. 填写附加信息

- **Requirements / Dependencies**：添加 SMAPI 作为 Requirement；可选添加 Generic Mod Config Menu。
- **Permissions**：选择与 GPL-3.0 一致的开源授权（允许修改、分享、再分发，衍生作品同许可；注明来源）。
- **Source code**：https://github.com/neoiw0/FishingExpanded
- **Credit / Thanks**：作者 `neoiw`；星露谷/ConcernedApe；SMAPI 团队；以及所有参与测试的玩家。

## 8. 发布

1. 保存页面后，点击 **Publish Mod**。
2. 发布后，用“游客/未登录”方式打开你的 Mod 页面，检查：
   - 标题、摘要、描述渲染正常
   - 图片正常显示
   - 下载按钮可用
3. 下载你上传的 zip，解压到干净 `Mods` 目录，用 SMAPI 启动一次，确认：
   - SMAPI 日志显示 `Fishing Expanded 1.0.0` 加载成功
   - 没有红色 error
   - `UpdateKeys` 未报错
4. 如果发现问题，先在 N 网隐藏版本或标记为旧版，不要留着坏包。

## 9. 发布后清单

- [ ] 页面已公开
- [ ] 文件已上传且可下载
- [ ] 更新检查正确指向 `Nexus:50595`
- [ ] 源码仓库已公开并写入页面
- [ ] 许可证正确（GPL-3.0）
- [ ] 玩家版 Wiki / 说明文档有入口（可在 Description 或 Source 仓库 README 放链接）

## 10. 常见问题

- **我没有源码仓库怎么办？** “传染性开源”意味着需要提供源码。请先创建 GitHub/Gitee 等仓库，把当前项目源码推上去，拿到 URL 后填进 N 网和 README。
- **ID 50595 和我创建的新页面不一致？** 以 N 网实际分配/已有页面为准；确认后把 `manifest.json` 的 `UpdateKeys` 改成真实 ID。
- **我想换许可证？** 替换项目根 `LICENSE`，并同步修改 `Release\README.txt`、`NEXUS-PAGE-CONTENT.md` 和 N 网 Permissions；然后重新打 zip。
- **旧 UniqueID 会怎样？** 新发布使用 `neoiw.FishingExpanded`；如果之前有旧安装，建议在 Description 里注明“旧版用户直接覆盖安装即可”。