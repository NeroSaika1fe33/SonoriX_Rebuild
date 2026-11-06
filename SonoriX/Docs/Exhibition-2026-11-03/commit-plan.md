# 建议提交拆分与 Git 对齐

这里的“对齐”是把当前成果对应到 Git 基线及可审查的提交边界；尚未执行提交或推送。远端认证失败，不能确认最新远端，也不能声称已经同步。

## 先统一工作区与暂存区

- 当前分支为 `SideReBuild`，origin 指向 TestMusicSynth；另一个 `github` 远端指向 SonoriX。不要用当前 `main` 的上游推断展会分支目标。
- 暂存区有 39 个新增文件，工作区还包含大量变更。特别检查 3 个 AM：DefaultSongDataBase.asset、Song4 Easy.asset、SelectedSongInfoWindow.cs；单纯 commit 会使用它们的旧暂存内容。
- 6 个 AD 是 Song4 Extra/Hard/Normal 及 meta：如果删除是最终意图，应在未来提交时使索引与磁盘一致；不要让仅暂存版本重新出现。
- 所有命令限制在本 Unity 项目。不要从仓库根执行整体 `git add .`，也不要把另一个 SonoriX 项目的已暂存变更带进展会提交。
- 新脚本和 Unity 资产须连同 meta 提交；重命名/移动应尽量保留原 GUID。不能把 `.meta` 一律当生成垃圾排除。

## 必须独立说明的提交单元

| 单元 / 建议标题 | 文件范围与依赖 | 独立原因 / 验收 |
|---|---|---|
| 1. `fix(live): restore hold audio effects on every end path` | HoldNoteSystem.cs | 行为修复，独立于 UI；成功/Miss/Pass/Remove/Clear 回归 |
| 2. `fix(guide): preserve segment rendering without hold judgments` | GuideNote.cs、LongNoteMesh.cs、LongTapEffectView.cs | 三者共同修正 Guide 的状态/显示/演出，作为一组提交 |
| 3. `feat(records): persist best results and unify result status` | SongPlayRecord/Store + meta、LiveResult、LiveScore、ScoreRank、SongSelectEntry；Flow 保存入口及 ResultInfoView 规则部分 | 持久化和枚举兼容性需单独审查；一旦插入 None 必须同步所有等级映射 |
| 4. `feat(ui): show selected song records` | SelectedSongInfoWindow + meta、SelectedSongInfoWIndow.prefab + meta、选曲 Prefab 接线、Flow 初始化部分、RankD/DataLost/记录状态图片 | 依赖 3；修复 D 数组后提交。记录窗口及选曲条目的字号更改可纳入此单元 |
| 5. `feat(exhibition): play title idle presentation` | TitleIdleAnimationView + meta、Flow 待机部分、xxAxxLive 待机字段、视频资源及 meta、关联 FMOD Bank | 运行逻辑和演出依赖共同成套；先处理超大重复资源 |
| 6. `refactor(ui): extract exhibition panels into prefabs` | StartGame/InGame/Result/Tutorial 新 Prefab、已有页面 Prefab、xxAxxLive 抽取/接线；所有对应图片/材质/Shader 依赖 | 场景与新 Prefab 必须一起；不能只提交看似“减少一万行”的场景 |
| 7. `content: update exhibition song catalog and charts` | Songs/DefaultSongDataBase 与 Song1–4 资产、Charts/Trance1、C_Match、NITO，StreamingAssets 同步谱面与所有 meta | 修正重复曲目/确认隐藏难度；资产与运行副本作为同一内容批次 |
| 8. `tune(input): configure exhibition arrow response` | ArrowController + xxAxxLive 对应参数 | 手感变化需独立审查，正文写清 1.2 / 180 / 1.5 的现场配置，不误写“中央衰减” |
| 9. `chore(unity): track missing asset metadata` | 已跟踪脚本缺失的 meta，见状态全集 | 补复现性；新功能自己的 meta 应跟随所属功能，避免拆成孤立无主文件 |
| 10. `docs: record exhibition changes and maintenance debt` | 本目录；已有 Docs/Interview 另按用途独立提交 | 展会复盘与面试备考是不同交付物 |

这些是逻辑单元，不应机械地“按完整文件分 10 次 add”。`LivePanelFlowController.cs` 同时包含 3/4/5，`xxAxxLive.unity` 同时包含 5/6/8；应按 hunk 拆分并验证每个中间版本。如果无法安全拆分 Unity YAML 或产生缺失资源，应合并相关 UI/场景提交，并在提交正文列出包含的功能。**任何时点的提交都应具备完整脚本与资源依赖。**

当前场景中的 BGM/SE 与视频共用 Master.bank，因此音频不能盲目按缓存 diff 拆成几次。可以把 Bank 作为相关功能之前的音频依赖提交，再接入场景；必须同时审查 Master.strings.bank 的事件映射。

## 应单独审查或暂缓的变化

| 文件/目录 | 处理建议 |
|---|---|
| Assets/Features/Video/DemoAnimation.mp3 与 .mp4 | 两份 SHA-256 相同，均为 MP4 内容。先确认有效视频及 FMOD 音源，去掉误副本；视频按大文件策略单独处理，不能混入普通美术提交 |
| Assets/Features/Material/Title_Fade_Mat.mat.meta 与 PressAnyButto_Fade_Mat.mat.meta | 原 Title GUID 被另一个材质使用；单独检查所有旧引用后与材质绑定提交，不能按纯数值调参忽略 |
| Assets/Scenes/Title.unity 删除、SonoriX/TestSonoriX 新增 | 正式场景抽取以 xxAxxLive 为准；确认实验场景保留意图后单独提交，验证其中未解析引用 |
| 旧 Tutorial1–5、Intro1、Note、旧 JPG 删除及废弃目录 meta | 清理提交独立于新增功能；先确认全项目引用，不能只检查 Build 场景 |
| Assets/StreamingAssets/PlayRecords | 实际游玩 JSON 默认不纳入功能提交；若需要预置数据，作为明确的展会种子数据提交并说明 |
| Assets/StreamingAssets/Charts/Test | 测试内容单独提交或排除，避免默认随正式 StreamingAssets 打包 |
| Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset | 工具缓存，与 Bank 产物分开审查；当前虽然被 ignore 规则覆盖，已跟踪文件仍会显示修改 |
| `.idea` 删除、`.editorconfig`、ProjectSettings/Packages | 工程/IDE 配置单元；明确内容后提交，不混入玩法修复 |
| 根 `.gitignore` | 仓库级修改位于本工作区之外，另行维护；现有根路径规则未覆盖 `My project/Build_10_*` 等嵌套目录 |
| ../Build_*、../TGS_*、DebugBuild、TestBuild 内二进制、日志 | 本次源码提交排除；已跟踪的旧产物需要另做仓库清理，新增 ignore 不会自动取消跟踪 |
| ../../SonoriX 的全部源码/资源/配置 | 另一项目的重构，必须独立审查/提交，不能借本次展会总结代为同步 |

## 完成对齐的验收条件

1. 确认 origin/SideReBuild 在线状态并重新比较本地 HEAD；当前认证失败项解决前不宣称与远端一致。
2. 每批只包含计划文件，核查 cached diff；AM/AD 已处理，另一项目的暂存内容未混入。
3. 正式入口在干净 checkout 中可加载，资源 GUID/fileID 和歌曲路径完整，超大媒体有可取得的分发方式。
4. 演奏与页面回归、记录保存、60 秒待机与重复播放在正式包/现场设备通过；标记测试包使用的提交 SHA。
5. 展会冻结点使用明确版本标签或发布记录，再将后续 TMP/架构重构安排到独立分支。

文件级依据：[Git 状态全集](git-status-before-audit.txt)、[场景/数据快照](scene-and-data-snapshot.md)。两份快照记录审计时状态，不会因后续操作自动更新。
