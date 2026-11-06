# UI 架构复盘与维护方案

审计基线和范围见 [总览](README.md)。以下为基于源码及 Unity YAML 的架构评估，尚未执行重构或字体迁移。

## 已经做对的部分

页面已有明确流程状态和独立 View，使用 SelectedSongChanged 通知部分信息展示；擦屏转场有专用 Controller，演奏数据处理也已部分拆离。此次 StartGame/InGame/Result 等 Prefab 抽取，让页面不必全部保存在大场景里，是继续维护的基础。

但“一个页面一个脚本、一个 Prefab”还不足以建立边界。当前问题是控制、装配、数据与表现仍互相穿透。

## 当前规模和职责

| 脚本 | 行数 | 当前职责 / 债务 |
|---|---:|---|
| LivePanelFlowController | 1257 | 8 种状态、页面开关、输入、转场、BGM/SE、布局、默认面板创建、选曲与 Auto、结果保存、待机展示接线 |
| SongSelectMenuController | 628 | 歌曲分组/难度、键鼠输入、选择和滚动、界面实例与音效 |
| SongSelectMenuItemView | 549 | 数据展示之外，还自动创建槽位/Text、施加布局/字号、处理 RawImage/Image/Sprite 缓存 |
| NoteSpeedSettingsPanel | 496 | 设置、输入、预览、布局与默认 UI 构建 |
| OperationGuidePanel / Prompt | 402 / 239 | 翻页、输入、查找引用、默认 UI、流程调用 |
| ResultInfoView | 281 | 结算显示、状态图片选择，运行时创建 Rank Text 兜底 |
| SelectedSongInfoWindow | 267 | 成绩展示、选曲订阅、查找/创建槽位、布局、动态封面 Sprite 生命周期 |
| TitleIdleAnimationView | 294 | 视频/Animator/FMOD/RenderTexture/超时/资源清理，以及创建默认 UI |

行数是定位线索，真正需要改变的是职责的拥有者，而非为了缩短文件机械拆方法。

## Text/TMP 的真实现状

并非全部都是 Legacy Text。UI 目录内 7 个脚本仍直接依赖 `UnityEngine.UI.Text`；AutoPlaySwitch 和 ScrollingText 已依赖 TMP。Live/Views 的 ScoreView 等也存在 Legacy Text，需要纳入同一迁移清单。

Prefab 中直接序列化的 Legacy Text 数量：

| Prefab | Text | TMP UGUI |
|---|---:|---:|
| NoteSpeedSettingPanel | 3 | 0 |
| OperationGuidePanel | 3 | 0 |
| ResultPanel | 14 | 0 |
| SelectedSongInfoWIndow | 7 | 0 |
| SongSelectMenuItem | 7 | 0 |
| TutorialPanel | 7 | 0 |

合计 41 个直接序列化的 Legacy Text；这是上述 6 个 Prefab 的组件数，不是运行时实例总数。选曲条目会重复实例化，嵌套 Prefab 与代码创建的 Text 也不能直接累计为同一统计。

正式场景自身为 0 Text / 1 TMP UGUI，未跟踪 SonoriX 场景为 21 Text / 1 TMP UGUI，均不展开 Prefab。因此不能只扫描 `.unity` 或只替换 `using` 判断迁移完成。

### 架构疏忽在哪里

1. **文字标准没有在第一批页面时确定。** 两套文本系统、图片文字、代码生成文字同时扩张，字体、间距、长标题、省略与滚动各自实现。后续成本来自各页重复决策，不只是 Text 组件本身。
2. **运行时兜底变成了第二套 UI 来源。** Prefab 中的布局与 `CreateDefault`、EnsureSlot、OnValidate/ApplyLayout 并存；美术改完 Inspector 后，代码可能再次覆盖字号/位置。切换 TMP 时，旧工厂仍会不断生成新的 Text。
3. **流程控制器逐步吸收所有横向需求。** 待机、BGM、分辨率、成绩保存都能“暂时放进去”，最终每新增一页都要修改多个旧页面的关闭逻辑及输入限制。
4. **View 通过流程控制器跨层拿数据。** SongSelectEntry 的 PlayRecord 属性调用静态存储；显示曲目信息可能间接触发文件读取，UI 无法清楚表达“加载失败”和“没有记录”的区别。
5. **依赖没有统一装配入口。** Inspector 引用、FindAnyObjectByType、按子物体名字查找、Resources 字符串路径和动态 Instantiate 混用。`SelectedSongInfoWIndow` 的大小写拼写也已进入加载路径，不能孤立改文件名。
6. **输入权限分散。** Flow、各 Panel、选曲和 AutoPlay 各自处理输入，部分直接查询 Keyboard/Mouse；转场锁只在部分入口统一。需要以状态决定当前输入归属，避免同一次按键作用到前后两页。
7. **配置与业务状态混放。** 每个槽位单独字号解决局部问题，却没有共同 Theme；同时页面承担 AutoPlay、曲目选择和存档触发，导致视觉调整也要理解业务生命周期。
8. **重复显示组件的边界没有定义。** SelectedSongInfoView 与新 Window 都处理封面/标题/难度；它们可以是不同布局，但应该共享数据快照及格式化规则，不应各自实现存档、Sprite 缓存或字体策略。

这不是缺少一个“大 UIManager”。继续增加全局管理器会把现有依赖转移到另一个中心，无法解决每页的归属不清。

## 建议的职责边界

```text
场景装配入口：持有各页面、服务和资源配置，显式 Initialize
  ├─ Flow / Router：状态与合法跳转、页面进入退出、转场期间输入归属
  ├─ UI 输入适配：Confirm / Cancel / Navigate / Start / Skip
  ├─ UI 音频服务：页面 BGM、选择音、待机音频交接
  ├─ 展会待机控制：计时、开始/退出请求；播放器拥有原生资源
  ├─ 选曲/设置/结算 Presenter：把数据快照交给对应 View
  └─ 成绩应用服务：监听演奏完成后保存，并通知记录变化

View：序列化引用 + Render(data) + 用户意图事件
Prefab：布局、字体样式、图片、按钮与引用
Theme/配置资产：字体、颜色、字号层级、间距与分辨率策略
RecordStore：读写、版本、缓存和失败结果，不依赖页面是否显示
```

可保留现有 FlowState 和页面，不需要展前引入复杂框架。先让新功能遵循边界，再迁移旧职责。

## TMP 迁移步骤与完成标准

1. 先在 UI 配置中定义字体资产、中文/日文/英数字 fallback、Title/Body/Caption/Number 四类样式；明确长标题的省略或滚动规则。图片文字可保留为美术资源，但动态曲名/分数统一为文本。
2. 建立统一文本创建/绑定方式；先改所有会新建 `Text` 的工厂和结果兜底，再改页面，否则旧组件还会反复生成。正式页面依赖完整 Prefab，缺引用在 Editor 校验中明确报错；默认工厂仅保留给明确的测试/原型路径。
3. 按依赖做迁移：先 SongSelectMenuItem/SelectedSongInfoWindow/SelectedSongInfoView，后速度/指南，再结算及游戏 HUD。每批同时提交 C# 字段、Prefab 组件和场景覆盖。不要只全局替换字段类型，Text 原组件的 fileID 引用不会因此自动转成 TMP 组件。
4. `TextAnchor`、Overflow、BestFit、preferredWidth、Mask/RectMask2D 行为逐项映射到 TMP；检查 ScrollingText 的 viewport 和动态刷新。FormerlySerializedAs 只能辅助字段名迁移，不能替换组件类型。
5. 同页完成 Inspector 接线后检查字形、换行、分数位数、DataLost/Not Played、长曲名、日文/中文、1920×1080 与其他展会分辨率。静态提示文字关闭 raycastTarget，交互区域交给实际按钮。
6. 每个迁移提交可单独回退，移除该页面旧工厂和旧文本组件；最后扫描源码、Prefab、场景及运行时创建路径，确认剩余 Legacy Text 都有保留理由。

## TitleIdleAnimationView 专项维护

当前实现考虑了不少现场问题：Prepare 与首帧等待分别计时、使用 unscaled time、视频禁用 Unity 音轨、首帧后启动 FMOD、错误与超时置完成、Stop 退订事件并释放纹理/FMOD、自动生成的播放器每次销毁重建。

后续应保留这些生命周期保障，并让播放器只负责 Play/Stop/Finished/Failed。默认黑底和 RawImage 创建应移到 Prefab/装配层；等待时长与场景进入退出属于展会待机控制；菜单 BGM 交接交给音频服务。

需要运行验证的边界包括：快速重复进入退出、Prepare 中输入退出、首帧超时、视频自然结束、播放器禁用、循环 Animator 且时长为 0、外部指定 VideoPlayer 的复用。独立 FMOD 音轨只在首帧对齐开始时间，并没有实现持续同步校正；长视频在目标机器上是否漂移仍需实测。

## 实施节奏

展会前：补全 Prefab 和资源提交，修复确定的配置问题，冻结主要页面布局；只做可控的单页 TMP 迁移。

展后第一阶段：集中装配与 Theme，移出成绩保存和 BGM；停止在正式页面自动修补层级。

展后第二阶段：统一 UI 输入和进入/退出契约，逐页完成 TMP，消除重复曲目信息格式化与 Sprite 缓存，补最小 Editor 引用校验。
