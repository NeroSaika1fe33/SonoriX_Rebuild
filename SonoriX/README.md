# SonoriX

Unity 音乐游戏重构工程。新架构位于 `Assets/Scripts/SonoriX/`，旧实现
`Assets/Scripts/FFrameWork/` 在迁移期间保留，两者目前都参与 Unity 编译。

## 仓库目录

在**外层 SonoriX 目录**建立一个 Git 仓库，同时管理 Unity 和音频工程：

```text
SonoriX/                         # 仓库根目录：在这里 git init
├── .git/
├── .gitignore                   # 从内层 SonoriX/.gitignore 复制
├── .gitattributes               # 在根目录执行 git lfs track 后生成
├── SonoriX/                     # Unity Hub 打开这一层
│   ├── Assets/
│   ├── Packages/
│   ├── ProjectSettings/
│   ├── .gitignore
│   ├── .gitattributes
│   └── README.md
└── SonoriX_Fmod_Project/
    ├── SonoriX.fspro
    ├── Assets/
    ├── Metadata/
    └── Build/                   # 共享源 Bank，使用 LFS
```

不要分别在两个子目录创建 `.git`。内层忽略文件无法管理旁边音频工程，
首次提交前必须将它复制到外层根目录；保留内层副本，之后修改规则时同步两份。
本说明保存在 `SonoriX/README.md`，可从仓库文件列表打开。

## 环境与运行入口

- Unity：`6000.4.5f1`，以 `ProjectSettings/ProjectVersion.txt` 为准。
- FMOD for Unity：当前使用 `2.03.14`。
- FMOD Studio：建议使用 `2.03.14`，与插件保持一致。
- Git 与 Git LFS：首次暂存资源前安装并配置。

Unity Hub 打开仓库中的 `SonoriX/` 子目录，等待 Package Manager 还原依赖。
重构测试场景为 `Assets/Scenes/SonoriX.unity`；测试入口是
`Assets/Scripts/SonoriX/LiveRuntimeTestDriver.cs`，需配置谱面、输入动作和展示引用。
当前构建设置中启用的场景仍是 `Assets/Scenes/xxAxxLive.unity`，
如需构建重构场景，请另行调整 Build Profiles 中的场景列表。

## FMOD 如何管理

本仓库采用**提交 Unity FMOD 插件及配置**的方案，替代之前忽略整个插件目录的做法。
正常克隆后应包含插件，无需每次重新手动导入。

| 内容 | 位置（相对仓库根目录） | Git 策略 |
| --- | --- | --- |
| 插件、原生库和配套 .meta | `SonoriX/Assets/Plugins/FMOD/` | 提交，排除 Cache |
| Unity FMOD 配置 | `SonoriX/Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset` | 与 .meta 一起提交 |
| 音频源工程 | `SonoriX_Fmod_Project/SonoriX.fspro`、`Metadata/`、`Assets/` | 提交，音频使用 LFS |
| Studio 源 Bank | `SonoriX_Fmod_Project/Build/` | 提交，Bank 使用 LFS |
| Unity 播放 Bank | `SonoriX/Assets/StreamingAssets/*.bank` | 提交，Bank 使用 LFS |
| Studio 本地状态 | `.cache/`、`.user/`、`.unsaved/` | 忽略 |
| 安装包 | `*.unitypackage` | 忽略，可备份到工程外 |

这里保留源 Bank 和 Unity 播放 Bank，便于克隆后运行已有内容及检查音频工程。
修改音频后需要同步提交两处 Bank。
`.fspro` 需要配套 `Metadata/` 和音频 `Assets/`，不能只备份这一个文件。
不要整体忽略 `*.asset`、`*.dll` 或资源 `*.meta`。

### 迁移后修正路径

检查时 Unity FMOD 配置仍指向旧的 `../FMOD Studio 2.03.14/Project/...`。
在 Unity **FMOD → Edit Settings** 重新选择 Studio 工程：

```text
../SonoriX_Fmod_Project/SonoriX.fspro
```

路径相对 **Unity 工程目录**，不是相对仓库根目录。
若使用 Bank 文件夹模式，选择：

```text
../SonoriX_Fmod_Project/Build
```

源工程模式通常会从 .fspro 推导 Bank 来源，以设置界面为准。
在 FMOD Studio 中打开工程，执行 **File → Build All Platforms**，
再回 Unity 刷新 Bank，检查是否仍有旧路径警告。
当前 Unity 播放 Bank 包括 `Master.bank` 和 `Master.strings.bank`。
最后保存并提交 `FMODStudioSettings.asset` 的修改。

若插件确实缺失，导入 **FMOD for Unity 2.03.14** 到 `Assets/Plugins/FMOD/`，
保留已有配置和 .meta，避免用空白配置覆盖。

## 首次建仓库与 Git LFS

以下命令在 Rider 的 PowerShell Terminal 执行。
本次文件更新本身不会初始化仓库或上传文件。

```powershell
Set-Location 'D:\PortfolioMaking\Zemi\SonoriX'
Copy-Item -LiteralPath '.\SonoriX\.gitignore' -Destination '.\.gitignore'
git init -b main
git rev-parse --show-toplevel
```

最后一条应输出外层 `D:/PortfolioMaking/Zemi/SonoriX`。

### 首次 git add 之前配置 LFS

```powershell
git lfs version
git lfs install --local
git lfs track '*.bank' '*.wav' '*.mp3' '*.ogg' '*.flac' '*.aif' '*.aiff' '*.mp4' '*.mov' '*.webm'
git lfs track '*.dll' '*.so' '*.dylib' '*.a' '*.lib'
```

若第一条命令不可用，安装 [Git LFS](https://git-lfs.com/)，重启 Rider 后重试。
`track` 在当前**仓库根目录**生成 .gitattributes，必须提交。
扩展名规则覆盖两个子目录，不会匹配 .meta。

代码、Unity 场景、Prefab、.asset、.meta、FMOD Metadata XML 和 .fspro 保持普通 Git 管理。
内层现有 .gitattributes 保存了 FMOD macOS bundle 文本的 LF 规则，保留即可，
它不能替代根目录覆盖两个工程的 LFS 配置。
不要将整个 *.bundle 目录作为 LFS 文件；上述规则已覆盖常见原生库。

其他超大二进制资源可以按实际路径单独添加规则：

```powershell
git lfs track 'SonoriX/Assets/path/to/large-binary-file'
```

然后暂存并检查：

```powershell
git add .
git status --short
git lfs ls-files
git check-ignore -v 'SonoriX/Library/example' 'SonoriX_Fmod_Project/.cache/example'
git check-attr filter -- 'SonoriX/Assets/Features/Video/DemoAnimation.mp4'
```

确认缓存没有被暂存，插件、FMOD 设置、.fspro、Metadata、源音频与 Bank 均在提交范围。
最后一条对该视频应显示 `filter: lfs`；LFS 列表显示实际匹配的二进制文件。
GitHub 普通 Git 拒绝超过 100 MiB 的文件，大型视频等必须在首次提交前进入 LFS。
LFS 存储及带宽受托管平台账户配额限制。

若已暂存但尚未提交，配置 LFS 后执行 `git add --renormalize .`，
再用 `git lfs ls-files` 检查。
已提交的大文件历史不会因新增规则自动迁移，需要另行规划历史迁移，
不要直接对已共享历史执行重写命令。

## Rider 提交和推送

1. 打开内层 Unity 工程或 SonoriX.sln。
2. **Settings → Version Control → Directory Mappings** 添加外层
   `D:\PortfolioMaking\Zemi\SonoriX`，选择 **Git**，移除失效的旧路径映射。
3. 在 Commit 窗口检查两个子目录均有变更。若旁边音频目录未显示，
   使用外层根目录的 `git status` 和 `git add .` 检查、暂存。
4. 完成 FMOD 路径修正并验证 Unity 场景运行后提交：

   ```powershell
   git commit -m 'chore: establish SonoriX refactoring baseline'
   ```

5. 在 GitHub 创建**空仓库**，不要预先添加 README、License 或 .gitignore。
   在 **Git → Manage Remotes** 添加 origin 和新仓库 URL，
   或在外层根目录执行（替换 URL）：

   ```powershell
   git remote add origin https://github.com/YOUR_ACCOUNT/YOUR_REPOSITORY.git
   git push -u origin main
   ```

以后正常使用 Rider Commit / Push，Git LFS hook 会上传 LFS 对象。
FMOD Studio 更新音频后先构建 Bank、回 Unity 刷新，再一起提交源工程与 Bank。

## 其他电脑克隆

安装 Git LFS 后：

```powershell
git lfs install
git clone https://github.com/YOUR_ACCOUNT/YOUR_REPOSITORY.git
Set-Location YOUR_REPOSITORY
git lfs pull
```

Unity Hub 打开克隆目录内的 SonoriX/，FMOD Studio 打开旁边的
SonoriX_Fmod_Project/SonoriX.fspro。保持相对目录布局一致。

## 参考

- [FMOD 2.03 Unity 用户指南](https://www.fmod.com/docs/2.03/unity/user-guide.html)
- [FMOD 官方缓存与版本控制说明](https://qa.fmod.com/t/git-and-fmod/22503)
- [Rider Git 目录映射](https://www.jetbrains.com/help/rider/Settings_Version_Control_Directory_Mappings.html)
- [GitHub：配置 Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/configuring-git-large-file-storage)
- [GitHub：大文件限制](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)
