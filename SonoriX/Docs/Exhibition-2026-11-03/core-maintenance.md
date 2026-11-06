# 本体变更与维护总览

## 当前运行链

```text
SongSelectDataBase / SongSelectEntry
  → SongCatalog / LiveBootFactory / LiveBundleBuildData
  → SUS 读取与 Converter / MusicScore
  → LiveSceneController：场景生命周期、音乐时间、启动/暂停/恢复/结束
  → LiveLogic：按时间推进并协调子系统
      ├─ ChartPlaybackSystem：时间与 Note 调度
      ├─ LiveInputCollector / InputManager：真实输入与 Auto 输入
      ├─ JudgmentSystem：候选输入与判定
      ├─ HoldNoteSystem：持续输入与长条音效生命周期
      ├─ ScoreSystem / ScoreLogic：计分、Combo、Gauge
      └─ PresentationRoot → View / Effect Presenter → Pool / FMOD
  → LiveEnded → 当前 UI Flow 保存成绩、显示结果
```

这条链是已有拆分的方向；LiveLogic 仍持有 Unity/Arrow/输入和表现依赖，尚不是完全独立的领域模型。另一个 `../../SonoriX` 项目拥有不同命名与实现，不应把那边的 Runtime/Parser 更新混写成此项目已完成的重构。

## 当前本体改动的实际含义

| 改动 | 解决的问题 | 建议回归 |
|---|---|---|
| HoldNoteSystem.Clear/Remove 也结束已追踪音效 | 清空、池回收时避免留下长条参数 | 结束、重开、切歌、退出 |
| Miss/Pass 的 EndNote 也恢复音效 | 音效不能只在成功终点归位 | Free/Fixed/Slider/Volume 的成功、漏尾和 Miss |
| Done 不再启动长条音效，Done 即触发结束 | 起止在同帧或已完成对象避免重新开启效果 | 极短条、低帧率、Auto |
| beginEffect 前就加入追踪集合 | 即使只应用了一部分参数，仍能在结束时尝试恢复 | Begin 失败、事件缺失、复合参数 |
| GuideNote 到起点直接进入 Input，持续到子终点 | 非判定 Guide 不应等待玩家输入/超时再显示 | 多段 Guide、跳转、末尾结束 |
| LongNoteMesh 对 Guide 保留当前区段 | 避免经过中继后替换为上段父对象造成起点固定 | 连续转折、不同宽度 Guide |
| LongTapEffect 只接收 HasJudgment 的 LongNote | 显示用 Guide 不分配 Hold 演出 | Guide/真实长条混合谱 |
| LiveScore 集中 GetResultRank/GetClearStatus | UI 与存档使用共同等级与 AP/FC/Clear 判断 | 零判定、全 Perfect、含 Great/Good/Miss、Auto |
| ScoreRank 插入 None=0 | 明确“未游玩”，同时改变旧枚举所有数值 | 序列化整数、数组下标、历史数据 |
| 新 JSON RecordStore | 曲目配置与玩家成绩分离，Auto 排除、分别保存最好成绩 | 写失败、重启、空/坏 JSON、fallback |
| Arrow 中央倍率 | 使控制手感可随横坐标变化 | 中心/边缘、连续移动、实际控制器；以场景值为准 |

## 应保留和继续维护的边界

| 范围 | 现状与维护重点 |
|---|---|
| 场景生命周期 | LiveSceneController 约 1263 行，仍集中预览数据、FMOD、协程、时间、RenderTexture、输入和结果；适合后续按音频时钟/会话/场景 View 装配分拆 |
| 逻辑推进 | 已拆系统应继续由 LiveLogic 调度；不要把 Note 特例再次堆回总循环；判定、计分、特效启动/结束的先后顺序要保留 |
| 输入与 Auto | InputManager 存在拥有/借用关系；Clear/Dispose 时只释放自己创建的对象。现场设备输入归一化与 UI 输入属于不同使用阶段，需要统一权限切换 |
| 音频时间 | 保留 FMOD 同步时间与谱面时间/显示偏移区分；首播、暂停恢复、切歌及视频 BGM 交接需要一起验证 |
| 特效池与音效参数 | 生命周期应成对，结束操作可重复调用；不能只释放画面对象而遗留音频状态 |
| 计分与等级 | 目前同时有 ScoreGaugeCalculator、LiveScore.GetResultRank、ScoreLogic 更新 rank；阈值虽然共享部分常量，最大分数来源与调用时机仍需核对 |
| 资产与运行数据 | SongSelectEntry 是配置资产，RecordStore 是玩家状态；当前配置对象直接访问 DefaultStore，下一步应由应用层注入成绩服务 |
| 谱面来源 | Assets/Charts 为源，StreamingAssets 为运行副本；Builder 构建前会导出并修改 Entry。新增谱面要成对检查，避免构建隐式改动资产而未复审 |

## 存档与等级的兼容性

新存档按枚举名字保存 Rank/Clear，因此不直接依赖新枚举整数，但其他 Unity 序列化字段和 `rankSprites[(int)rank]` 仍会受 S=0→1、D=4→5 影响。`SelectedSongInfoWindow` 的数组已经有 None 占位，只缺 D；`ScoreView` 仍是直接按整数取图，未来配置数组时必须遵循同一映射或改为显式字段映射。

成绩保存当前由 LivePanelFlowController.OnLiveEnded 触发：只有 Clear/Failure 进入保存，Auto 进一步由 Store 排除。这个位置把“保存是否发生”耦合到 UI 是否订阅演奏事件，建议移动到演奏应用服务，再由 UI 监听结果。

Store 优先写 StreamingAssets/PlayRecords，失败后写 persistentDataPath；带内存缓存、临时文件替换和待保存重试，退出时 Flush。需要明确展会记录策略：重启保留、每位观众清空、或只提供空模板。目前空主 JSON 被视为主动清空，不恢复备份；损坏主文件与 fallback 的行为也应单独确认，不能仅因为存在 `.bak` 就认为会自动恢复。

潜在维护问题：读取 `GetLastWriteTimeUtc` 的异常路径不都位于 TryRead 的捕获内；缓存命中后外部手改 JSON 不会即时刷新。它们是静态审计发现的边界风险，尚未复现为现场故障。

## 歌曲与工程资产

- 当前磁盘有 Song1 四难度、Song2 四难度、Song3 三难度、Song4 Easy，共 12 份歌曲资产；数据库只有 11 个不同 GUID，并重复 Song3 Easy。
- Song4 其他难度为 AD 状态：暂存内容存在、磁盘已删除。默认 commit 会把这些难度重新放入提交快照，须先统一目录意图。
- 源谱和运行副本当前 12 对全部逐字节一致；构建前 Builder 按 Title 生成路径，因此修改 Title 可能改变导出位置，旧路径不会自动成为受控迁移。
- 已跟踪代码有多份 meta 尚未入库；这是工程可复现性问题，优先于纯代码美化。
- Build Settings 的正式入口、实验场景、删除的 Title 场景应在版本说明中明确，避免用实验场景结果判断正式包。

## 建议的最小回归范围

1. 完整用户路径：标题→说明确认→全部说明页→选曲/难度→速度设定→手动演奏→结算→返回选曲/标题，再重复一次。
2. 逐类 Note：Fixed、FreeLane、Slider、Volume、Guide 及长条中继；成功/漏起点/漏终点/中途松开；留意特效与 FMOD 参数是否归位。
3. 成绩：零判定、D–S、Crash/Clear/FC/AP、Auto 不覆盖、同曲不同难度隔离、重启读取、不可写目录与坏 JSON。
4. 生命周期：演奏中退出/切歌、暂停恢复、Auto 切换限制、反复进入结果；确认时间、输入和对象池均重置。
5. 展会专用：标题等 60 秒后播放、准备中退出、自然结束、连续多轮循环、音画偏移、视频缺失/出错；验证正式分辨率与控制器。

本次仅做静态核对；以上为待执行回归范围，没有以源码推断代替运行通过记录。优先为判定/成绩/音效生命周期建立有输入输出意义的测试，再推进会改变调用顺序的重构。
