# SonoriX

基于 Unity 与 FMOD 的音乐游戏项目，包含谱面读取、轨道输入、音符判定、计分和演出展示。目前正在重构旧实现，旧版与新版暂时并存，新版尚未完整替代旧版。

## 当前阶段与代码结构

| 命名空间 | 代码目录 | 定位 |
| --- | --- | --- |
| `FFrameWork.*` | `Assets/Scripts/FFrameWork/` | 旧版实现，保留现有玩法与演出流程，仍会维护和修复 |
| `SonoriX.*` | `Assets/Scripts/SonoriX/` | 重构中的新架构，将数据与规则（Domain）、运行逻辑（Runtime）、输入与谱面解析（Infrastructure）、场景调度（App）和展示（Presentation）分开 |

两套代码目前都参与 Unity 编译。目录中的少量脚本尚未统一命名空间，例如新版测试入口 `LiveRuntimeTestDriver` 位于全局命名空间。

- **旧版场景**：`Assets/Scenes/xxAxxLive.unity`，也是当前构建配置启用的场景。
- **重构测试场景**：`Assets/Scenes/SonoriX.unity`，通过 `LiveRuntimeTestDriver` 加载谱面、模拟输入并启动运行逻辑；目前使用 `ManualLiveClock`，属于开发测试入口。

## 克隆与打开

仓库同时包含两个工程，请保留它们的相对位置：

```text
仓库根目录/
├── SonoriX/                     # Unity 工程
└── SonoriX_Fmod_Project/         # FMOD Studio 源工程
    ├── SonoriX.fspro
    ├── Metadata/
    ├── Assets/
    └── Build/
```

1. 安装 **Unity 6000.4.5f1**（以 `ProjectSettings/ProjectVersion.txt` 为准）。
2. 安装 [Git LFS](https://git-lfs.com/)，再克隆并下载音频、视频和 Bank：

   ```powershell
   git lfs install
   git clone https://github.com/NeroSaika1fe33/SonoriX_Rebuild.git
   Set-Location SonoriX_Rebuild
   git lfs pull
   ```

3. 用 Unity Hub 打开内层 `SonoriX/`，等待依赖还原和资源导入，再打开所需场景。

## FMOD 注意事项

**Unity 插件和 FMOD Studio 编辑器是两个独立部分。** 仓库已包含 **FMOD for Unity 2.03.14** 插件、配置、音频源工程及 Bank；插件用于 Unity 播放音频，编辑音频工程还需在每台电脑上单独安装 [FMOD Studio 2.03.14](https://www.fmod.com/download)。

- 编辑音频：在 FMOD Studio 中打开 `SonoriX_Fmod_Project/SonoriX.fspro`。必须同时保留 `Metadata/` 和音频 `Assets/`；Bank 不能替代可编辑的源工程。
- Unity 关联：在 **FMOD → Edit Settings** 检查工程路径为 `../SonoriX_Fmod_Project/SonoriX.fspro`；若使用 Bank 文件夹模式，选择 `../SonoriX_Fmod_Project/Build`。这些路径相对 Unity 工程目录。
- 若 Unity 无法自动启动 Studio，先确认本机已安装并配置软件关联，也可以直接从 Studio 打开 `.fspro`。
- 更新音频：在 Studio 执行 **File → Build All Platforms**，回 Unity 刷新 Bank，再一起提交源工程和更新的 Bank（含 Unity `Assets/StreamingAssets/` 中的播放 Bank）。
- 克隆后若音频缺失，先检查 `git lfs pull` 是否成功；若插件确实缺失，再导入同版本插件，保留已有配置和 `.meta`。

## 日常提交

旧场景维护与新版重构按功能分别 commit，在 Rider 中选择本次相关文件，并带上依赖的脚本、Prefab 或资源即可，无需仅为分开提交而创建分支。需要长期保留稳定版、同时推进未完成重构时，再采用 `main` 与 `refactor/sonorix` 分支。

Unity 和 FMOD 共用外层仓库；切换分支会同时切换两套工程。音频、视频、Bank 和已配置的原生库使用 Git LFS，保留仓库根目录的 `.gitattributes`；资源 `.meta` 要与资源一起提交，缓存和软件安装目录不提交。
