# 展会版本变更与维护总览（2026-11-03）

这次更新已经从“演奏功能调整”扩大为完整的展会体验：开场、操作说明、选曲、速度设定、演奏、结算、成绩记录，以及无人操作时的宣传视频。UI 的数量增长暴露了职责划分、资源管理和场景装配上的欠账；TMP 迁移是其中一项，不能代替架构整理。

本次交付为审计文档。没有修改游戏代码、场景、Prefab、暂存区，也没有执行 commit、push 或同步覆盖。

## 核对范围与 Git 基线

| 项目 | 实际状态 |
|---|---|
| 当前 Unity 项目 | `My project/TestFramework`，Unity `6000.4.5f1` |
| Git 根目录 | `D:/PortfolioMaking/Zemi/TestMusicSynth`，比 Unity 项目高两级 |
| 当前分支 / HEAD | `SideReBuild` / `76799edc25cf7abbb9b3109c07631bd63699aa5c`，2026-10-23 |
| 本地上游引用 | `origin/SideReBuild` 与 HEAD 相同；只是本地缓存状态 |
| origin | `NeroSaika1fe33/TestMusicSynth` |
| 另一远端 github | `NeroSaika1fe33/SonoriX`；本地 `main` 跟踪它的 `main` |
| 在线核验 | `git ls-remote origin refs/heads/SideReBuild` 认证失败，未确认远端最新状态 |
| 历史窗口 | 2026-09-23 至 2026-10-23 的本项目提交，补充工作区截至本次审计的全部差异 |
| 实际构建入口 | `EditorBuildSettings.asset` 只启用 `Assets/Scenes/xxAxxLive.unity` |

用户所说的“几周”没有精确起点，因此从 9 月下旬的完整 UI 流程引入开始回看；工作区文件没有可靠修改时间线，不能把未提交内容全部断言为最近某一周所做。

审计前本项目共 **298 条文件状态记录**（含 `.meta`）：178 未跟踪、60 仅工作区修改、30 仅暂存新增、21 仅工作区删除、6 暂存新增后又在磁盘删除（AD）、3 暂存后继续修改（AM）。暂存区为 39 个文件的新增快照；直接提交会漏掉多数展会变更，并可能提交磁盘上已删除的文件。

文件级全集见 [审计前 Git 状态](git-status-before-audit.txt)。其中 `Docs/Interview`、`.editorconfig`、`ProjectSettings/Packages` 等属于文档或工程杂项，不应被当成展会运行功能。根目录还有另一套 `SonoriX` 项目、Build/TGS/DebugBuild 目录及日志，均不属于本次展会项目的同一提交。

## 已提交的阶段性更新

| 日期 / 提交 | 本项目中可确认的主要变化 |
|---|---|
| 09-23 `92a9cf2`、`00b39c5` | 输入、Arrow、判定、长条、音效池、FMOD 与演奏显示同步调整，场景及 Note Prefab 更新 |
| 09-24 `2f00978` | 引入完整页面流程：选曲、速度、操作指南、结算、擦屏转场及歌曲数据；扩展各类 Note、演出 Prefab。该提交还包含大量资源，不能视作单纯脚本更新 |
| 10-01 `c611f16` | SUS 转换、Long/Normal Note、输入与显示链调整；Fixed/FreeLane 长条类型整理；更新页面流程 |
| 10-02 `b45651b` | 音效控制及池管理调整；歌曲类型转移到 `Live/Data`，但 namespace 仍为 `FFrameWork.Songs`；旧平铺歌曲资产删除 |
| 10-04 `7a4fe57` | 涉及大量注释，也有实际逻辑变化：Arrow、判定、计分、数据装配、音频、Note 和 View；不能按提交标题当作纯注释 |
| 10-09 `2031782` | 将 LiveLogic 拆为 ChartPlayback、Judgment、Hold、Score、输入与 Presentation；SoloLiveController 大幅缩小；加入 ClearGauge、AutoPlay UI 与谱面构建导出 |
| 10-12 `d3e57d7` | 使用约 1263 行的 LiveSceneController 承接场景生命周期；Presentation 拆成 View/Effect Presenter，音效参数应用与 Provider 分离 |
| 10-23 `76799ed` | 谱面与运行副本、选曲输入、Note 外观、Mesh 排序、场景、FMOD Bank 与项目设置更新。标题为 update converter，但本项目该提交并没有修改 Converter.cs |

## 当前未完成提交的展会变化

| 模块 | 已观察到的变化 | 提交时必须连带 |
|---|---|---|
| 标题与无人操作展示 | 新增 `TitleIdleAnimationView`；流程控制器接入等待、播放、输入退出、恢复菜单 BGM；视频首帧后启动独立 FMOD 音轨 | 脚本及 meta、流程改动、场景视频引用、视频资源、对应 Bank |
| 选曲成绩窗口 | 新增 `SelectedSongInfoWindow` 和对应 Prefab；显示封面、歌曲、难度、最高分、最佳 Rank/Clear | 窗口、选曲 Prefab、成绩模型/存储、统一结算规则 |
| 成绩记录 | `SongPlayRecord` 与 `SongPlayRecordStore`；按曲 ID/难度存 JSON；分别保留最佳分数/等级/通关；Auto 不写成绩 | `SongSelectEntry`、`LiveScore`、`LiveResult`、`ScoreRank`、结果保存入口与 UI |
| 结算 | 使用统一 Rank/Clear 计算；补 D 图片字段；缺 Rank 图时生成 Legacy Text 兜底 | `ResultInfoView`、ResultPanel、RankD 及状态图片 |
| 选曲表现 | 各字段独立字号；难度数值不再加 Lv.；默认 PlayLevel 改为 0 | `SongSelectMenuItemView`、对应 Prefab 和歌曲资产；其他页面仍有 Lv. 文案，需统一设计 |
| 操作说明 | 每次重新启用从第 1 页开始；指南图片替换及扩展，场景含第 6/7 页引用 | 指南/确认 Prefab、OperationGuide1–7、脚本、场景覆盖值 |
| 页面资产化 | 新增 StartGame/InGame/Result/Tutorial/SelectedSongInfoWIndow Prefab；选曲及已有设置/指南 Prefab 更新 | 不可只提交场景，新增 Prefab 和所有依赖必须一并纳入 |
| UI 与音符美术 | 大批标题、菜单、结果、Note、轮盘与判定图片替换；新增窗口、难度槽、Lane、背景等；旧教程图片删除 | 图片及 meta、引用它们的 Prefab/材质、删除验证 |
| 材质/背景 | 新增 CyberGlitch 背景 Shader、Back 材质、PressAnyButto 淡入材质；Title_Fade 最小透明度 0.05→0.7、速度 0.5→0.3 | Shader、材质、纹理、meta 与绑定一起检查 |
| 本体修正 | 长条音效的 Miss/Pass/Done/Remove/Clear 清理；Guide 连续显示、分段 Mesh 与演出分配修正 | 见本体维护文档；宜独立于美术提交 |
| Arrow 手感 | 增加按横坐标插值的中央倍率 | 代码与实际场景参数共同记录；当前两者默认值/注释不同 |
| 歌曲/谱面 | 歌曲资产重建为 Song1–4 文件夹，新增 C_Match、NITO 源谱及运行副本；Trance1 三难度源谱和运行副本更新，Extra 运行副本同步 | 数据库、歌曲资产、源谱、StreamingAssets、封面/FMOD 引用及 meta |
| 音频 | Master.bank 与 Master.strings.bank 更新，FMOD 编辑器缓存随之变化 | Bank 成对核对；缓存单独审查，不把缓存当音频源工程 |

此外有多份已跟踪脚本的 `.meta` 尚未跟踪，例如 LiveInputCollector、Presenters、AudioEffect Provider、ClearGauge、AutoPlaySwitch、长条 EndNote 与 MeshSortingOrder。这属于复现工程所需的 Unity 标识补全，不能因“脚本已提交”而忽略。

## 场景内的变化

`xxAxxLive.unity` 的大量 YAML 删除主要来自把内联 UI 抽成 Prefab。当前场景可见的 Prefab 来源包括速度设定、Arrow 判定、Tutorial、MusicVisualizer、操作指南/确认、SelectSong、转场、InGame、StartGame 和 Result。仅统计场景自身的 Text 会误判 UI 已经迁移完成。

- 正式场景：标题待机开启，等待 60 秒；播放器组件引用为空，由代码创建；视频指向 `DemoAnimation.mp4`；FMOD 为 `event:/Music/BGM/DemoAnimationBGM`，音量 0.8，播放时长 0 表示等视频结束，暂停菜单 BGM。
- 选曲记录窗口：已指定 Prefab，`createDefaultSelectedSongInfoWindow=0`，依赖选曲 Prefab 内的实际装配。
- Arrow：场景 `sensitivity=1.2`、阈值 180、中央倍率 **1.5**。代码默认 3 / 180 / 0.8。因此当前场景中心灵敏度为 1.8，外侧为 1.2，是中央增强，不能用代码注释描述为衰减。
- 判定音量：Perfect/Great/Good 从 0.4→0.7；Flick 从 0.4→1.5。
- 正式场景仍开启 `enableDebugPanelSwitch=1`；构建前确认是否允许展会观众触发强制切页。
- Fixed/FreeLane Note Prefab 存在倾斜/尺寸调整；如 FixedLong 的 Y 缩放 30→20、X 旋转 0→-4 度。VolumeLong 的 Y 缩放 70→50、Sprite 高度 2→1。RotatingWheels 两张图片 alpha 1→0.8。
- `Title.unity` 被删除；`SonoriX.unity`、`TestSonoriX.unity` 为未跟踪新增，但未进入 Build Settings。必须把“展会入口”和“实验场景”分开判断。

字段级补充见 [场景与歌曲数据快照](scene-and-data-snapshot.md)。快照是按稳定 fileID 比较组件，不等于完整场景 diff；Prefab 抽取导致的数量下降不等于内容消失。

## 展会前优先处理的具体事项

1. **资源提交完整性**：关键新 Prefab、待机脚本、视频、歌曲和 meta 未纳管，当前 HEAD 无法复现工作区展示版本。
2. **视频重复与体积**：`DemoAnimation.mp3`、`.mp4` 均为 371,927,582 字节，SHA-256 完全相同；两者文件头都是 ISO BMFF/MP4（`ftypisom`）。`.mp3` 不是独立导出的 MP3 音轨。确认引用后移除误副本，并在提交前安排视频的 LFS 或其他大文件分发方式；不要只换扩展名。
3. **歌曲目录重复**：DefaultSongDataBase 有 12 条引用、11 个不同 GUID，C_Match Easy 被重复引用；Song1 Extra 资产存在但不在该数据库。前者确认是否误配置，后者确认是否有意隐藏。
4. **Rank D 图缺失**：记录窗口数组目前是 DataLost/S/A/B/C，仅 5 项；新枚举 D=5，访问时返回 null 并退回文字，因此 D 无法显示专用图片。该窗口 S–C 并未整体错位，它已预留 None 占位；`ScoreView` 其他数组仍需核查。
5. **材质 GUID 语义变动**：Title_Fade_Mat 原 GUID `f577c676...` 现在属于新增 PressAnyButto_Fade_Mat；StartGamePanel 和 SonoriX 场景仍引用这个 GUID。因此不是简单“GUID 丢失”，而是旧引用会解析为另一份材质，需要确认视觉意图。
6. **遗留预览引用**：正式场景 `previewSusScore` 的 `ece92f11...` 在 Assets/本地 PackageCache 内未解析，且在 HEAD 场景也存在，属于遗留问题。默认 `startOnAwake=0` 不代表所有调试/预览路径都安全，需要检查其使用路径。
7. **成绩数据随包污染**：StreamingAssets/PlayRecords 已有本地 JSON。展会版本应明确空记录启动还是预置成绩；不能把个人游玩结果随手加入提交。

12 份当前歌曲资产的 SUS 源文件与指向的 StreamingAssets 文件逐字节一致，这是已完成的静态验证。未在本次审计中启动 Unity、执行构建或做现场设备测试；“没有未解析的主要 Prefab GUID”也不等于无 Missing Component 或 fileID 接线问题。

## 后续阅读与执行顺序

- [UI 架构复盘与 TMP 迁移](ui-architecture.md)：当前职责问题、目标边界、逐页迁移方式。
- [本体维护总览](core-maintenance.md)：运行链、近期本体修改、维护风险与回归范围。
- [提交拆分清单](commit-plan.md)：必须独立提交、需要一起提交，以及暂缓/排除的项目。

展会前先补资源完整性、修正配置与完成回归；稳定后冻结可复现版本。展后按页面迁移 TMP，并逐步把保存、音频、输入和布局从页面流程中移出。
