# 用自己的音游复习 C# 与 Unity

本篇是学习材料，不是已掌握能力清单。代码小题是独立练习，不会加入游戏运行代码。每次练习先自己预测，再运行或对照，最后用自己的话解释。

## 1. class 与 struct：先学复制语义

工程里的 `LiveInput`、`MusicScoreInfo`、`ArrowJudgmentFrame` 是结构体；`NoteBase`、`MusicScore`、`InputManager` 是类。

| 问题 | 值类型 struct | 引用类型 class |
| --- | --- | --- |
| 默认赋值 | 复制值 | 复制对象引用 |
| 修改副本中的数值字段 | 原值不变 | 两个引用可能看到同一对象的变化 |
| 适用例子 | 一个输入样本、一份时间信息 | 有身份与持续生命周期的 Note、输入管理器 |

**不要背成“struct 永远在栈，class 永远在堆”。**结构体可以放在数组或对象字段里，也可以发生装箱；关键先理解复制与引用。如果 struct 字段里有一个数组，复制 struct 也只复制这个数组引用，两个副本仍指向同一个数组。[Microsoft 值类型文档](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-types)

工程例子：

```csharp
LiveInput laneInput = input;
laneInput.inputId = 1000 + lane;
```

它为每个逻辑轨道复制一份输入数值，调整这份的 ID 不会直接改原 input。但是 `ArrowJudgmentFrame` 中的 `float[] SizeStages` 不会因为复制 frame 就变成新数组。

小题：

```csharp
var original = new Sample { Time = 1f, Values = new[] { 10 } };
var copy = original;
copy.Time = 2f;
copy.Values[0] = 20;
// original.Time 和 original.Values[0] 各是什么？

struct Sample
{
    public float Time;
    public int[] Values;
}
```

答案：1 与 20。数值字段复制了，数组引用被共享。

## 2. ref、out、in 与返回值

在项目中搜索：

```csharp
TryGetFixedLaneInput(int index, float musicTime, out LiveInput input)
Judgment(ref LiveInput input, float lane)
```

- `out`：由方法写入调用者的变量，常见于 `TryGet...`；方法通过 bool 表示是否成功。
- `ref`：传入变量本身的引用，方法可读取并修改它。被传的值必须先初始化。
- `in`：按只读引用传递，是否有收益取决于大小、调用方式等，不是看到 struct 就必须用。
- 普通参数：对 struct 默认复制值，对 class 默认复制对象引用；普通 class 参数仍可修改那个对象，但不能借普通赋值替换调用者变量本身。

**项目使用 ref 不表示每个被调用方法都一定修改 input。**需要检查具体方法；不要把“可能修改”和“实际修改”混为一谈。

练习：把一个只修改局部 struct 的方法分别改成返回新值与 ref 两版，说明调用者得到什么。选择 API 时先考虑可读性和副作用，再考虑经测量的复制成本。

## 3. 继承、virtual、override 与接口

`NormalNote : NoteBase` 继承共同状态与字段，重写 `IsJudgment`、`Judgment` 等行为。`virtual` 提供可重写实现，`override` 选择子类实现；通过 `NoteBase` 引用调用虚方法时，仍根据实际对象类型分派。

`SoloLiveController : LiveSceneController` 当前是空包装。它保留原组件类型，核心行为在基类里。**打开空类看到没代码，不能判断它没有功能。**

`INote` 为显示层提供共同读取契约。`IMusicTimer` 把控制器对计时器的依赖收窄到 Reset、Execute 和 PlaybackTime 等操作。这种接口便于替换实现，但前提是两种实现真的遵守同一语义。

接口不自动消除 Unity 依赖，也不自动让代码可测试。还需要能注入时间、输入和反馈，控制副作用。

面试小题：“添加一种特殊 Tap，要改哪里？”

先问新规则是什么。如果只变外观，可能只需 Prefab/View 配置；若变输入条件，涉及具体 Note、来源过滤、解析映射；若新增谱面语法，还要改 Converter。不要为一个颜色需求增加一整套继承层。

## 4. Action、Func、event 与调用链

当前 Note 不直接拿着所有 UI，而是通过回调通知外部。

```csharp
Action<NoteBase>     // 接受 NoteBase，不返回值
Func<NoteBase, float> // 接受 NoteBase，返回 float
```

`ScoreSystem` 订阅 `ScoreLogic` 的分数、Combo、量规等事件，转发给 PresentationRoot，再更新 View。

`event` 约束外部只能订阅/退订，不能随意从外部赋值或触发；普通委托字段没有同样限制。项目里的 Note 回调属性主要通过 setter 注入，而 ScoreLogic/控制器有对外事件。

你需要能回答：

1. 谁创建发布者和订阅者？
2. 谁订阅，什么时候退订？
3. 切歌/重试会不会重复订阅？
4. 事件触发时是否可能重入当前流程、Dispose 正在使用的对象？

`+= () => Handle()` 后再 `-= () => Handle()` 通常不能当作移除原来的那个委托；需要保留原委托或使用同一个命名方法。

项目中的 `LiveViewPresenter` 使用捕获 Note 的 lambda，是一个值得实际测量分配的地方。不是所有 lambda 都会产生相同开销；静态不捕获、缓存委托与捕获局部变量有区别。

## 5. 数组、List、Dictionary 和复杂度

| 容器 | 当前项目用途 | 复习重点 |
| --- | --- | --- |
| `NoteBase[]` | 已解析的根音符数组 | 长度固定、顺序访问、数组也有内存成本 |
| `List<NoteBase>` | 本帧候选、活动长按 | Count 与 Capacity 不同；扩容可能重新分配数组 |
| `Dictionary<INote,BaseNoteView>` | 音符到显示对象映射 | 平均查找较快；不是天然按时间排序 |
| `Dictionary<int,LongNote>` | 解析中连接长按 ID | Key 的唯一性与复用规则 |

`List.Clear()` 通常保留容量以复用存储，并不把 Capacity 变成零。当前预分配 `InputTmp[128]`，每次复用候选列表，就是减少演奏中重复创建的一种做法。

大致复杂度：数组/List 按索引读取 O(1)；List.IndexOf/Contains 一般 O(n)；从中间 RemoveAt 会移动后续元素；Dictionary 查找平均 O(1)，不是任何情况下都保证常数时间。

`JudgmentSystem` 使用候选与输入之间的多层循环。改成更复杂的数据结构不一定更快；小规模候选时简单循环可能更清楚，需要测真实数据量。

练习：10,000 颗 Note 中当前只有 20 颗可能被判定，你能否避免每帧对全部做输入匹配？当前实现哪些地方已缩小候选，哪些地方仍扫未来尾部？先画示意，不急着重写。

## 6. LINQ：看执行频率和枚举行为

Converter 中的 `OrderBy/ToList` 用于加载时构建谱面；ScoreLogic 初始化时也会枚举计分节点。演奏每帧里反复排序、创建迭代器或临时列表则更值得关注。

不能只按“代码出现 LINQ”判定性能差。要问何时执行、执行多少次、输入有多大、是否分配、是否真的成为瓶颈。

`IEnumerable` 常有延迟执行：创建一个查询不等于所有计算已经完成。重复枚举可能重复计算。需要复用结果时考虑在合适阶段物化，但物化也需要内存。

## 7. enum、元组、nullable 与保护条件

项目用 enum 表达 NoteCategory、NoteState、NoteResult 和 InputScope，而不是散落字符串。

`(NoteResult, NoteResultDescription)` 元组把“等级”和“Fast/Late/Just”一起返回。等级 Perfect 与时间描述 Just 不是同一个维度，不要只拿字符串解释。

`LiveResult`、`NoteResult` 一些比较依赖 enum 数值顺序，例如 `note.Result < NoteResult.Great`。增加 enum 成员前应检查所有顺序比较，不能认为只是加一个名字。

`?.` 是空条件调用；`??` 提供空时后备值；`default(LiveScore)` 对结构体通常是各字段默认值，不是“运行时初始化过的完整成绩”。

`float? continueTime` 同时表达“是否存在一次 Continue”和“Continue 时间是多少”，比把某个合法时间强行当未设置值更清楚。

练习：读 `ScoreLogic.ApplyClearGauge`，解释 `continueTime.HasValue` 和 `continueTime.Value` 为什么分开使用。

## 8. Unity 对象、组件和序列化

GameObject 是场景实体，Component 提供行为或数据，Transform 表示空间关系。`MonoBehaviour` 是 Unity 能管理生命周期的组件类型；普通 C# 对象不会被 Unity 自动调用 Update。

当前 `LiveLogic`、`JudgmentSystem`、`ScoreLogic` 是普通对象；`LiveSceneController`、`ArrowController`、`NotePool` 等是组件。普通类方法叫 OnUpdate，也必须有人主动调用才会执行。

`[SerializeField] private float sensitivity` 表示 Inspector 可保存该字段。**源码初始化值只是初始值来源之一，已保存场景/Prefab 值可能覆盖它。**当前中心灵敏度的 0.8 与场景 1.5 就是例子。

Prefab 是可复用对象模板；场景或 Prefab 实例有自己的覆盖。`.meta` 的 GUID 用于资源引用，随便删除或重建可能使引用丢失。保留脚本文件名不意味着 GUID 仍相同。

ScriptableObject 适合共享配置资产。但 Play Mode 中修改它的数据是否会保留，以及多个使用者是否共享状态，要结合使用方式确认；不应把运行状态随意写回共享配置。

## 9. 生命周期和执行顺序

| 回调 | 主要用途 | 本项目学习点 |
| --- | --- | --- |
| Awake | 对象初始化 | 不依赖其他对象 Start 已完成 |
| OnEnable | 激活后的启用处理 | 适合匹配启用周期的订阅，但要防重复 |
| Start | 首次更新前的初始化 | ArrowController 在这里准备逻辑和 View |
| Update | 每个渲染帧处理 | 主演奏时间、输入与视图推进 |
| FixedUpdate | 固定步长处理 | 常用于物理；不是本项目判定精度的自动保证 |
| LateUpdate | Update 后处理 | 常用于相机/跟随；不等于与音频同步 |
| OnDisable | 禁用时处理 | 与启用订阅对称 |
| OnDestroy | 销毁时清理 | 事件、输入、FMOD、RenderTexture 等生命周期 |

同一回调在不同组件间的相对执行顺序不能凭脚本文件排列推断。要么查执行顺序配置，要么由明确的入口按顺序调用。[Unity 执行顺序文档](https://docs.unity3d.com/6000.0/Documentation/Manual/execution-order.html)

## 10. Time、deltaTime、绝对时间与暂停

三种计算要区分：

```text
每秒移动速度 → speed × deltaTime
已经收到的位移差分 → 按差分累加，通常不再乘 deltaTime
音符在歌曲中的位置 → 从统一歌曲时间与目标时间计算
```

用每帧 `position += fallingSpeed × deltaTime` 做画面也能移动，但若需要与歌曲严格关联，从绝对歌曲时间重算更容易处理丢帧、重试和跳转。这样并不能消除输入采样误差。

`Time.timeScale` 影响 scaled 时间，不保证所有第三方音频和所有业务状态一起暂停。当前工程主要用 isPaused、音乐 setPaused 和通知 View，而不是全局 scale 暂停。

`Time.time` 是 float，长时间运行的精度与来源要留意；但没有测量就不要把所有偏差都归咎于 float。先查基准、单位和偏移的正负号。

## 11. 内存、GC 和对象池：避免口诀

托管分配、原生资源、GPU/音频内存是不同层面。

- 创建普通 class 实例、字符串拼接、装箱、列表扩容等可能产生托管分配。
- `new Vector3(...)` 创建值类型值，不意味着每次都在托管堆分配。
- `Instantiate/Destroy` 涉及 Unity 对象生命周期，成本不只有 C# GC。
- `new Material`、`new Mesh` 与 RenderTexture 有原生资源，不能只等托管对象失去引用。
- 对象池用复用换取更稳定的创建成本，但也可能占用过多常驻内存。

音游应尽量避免演奏热路径不必要的分配；“任何地方严禁分配”并不是可执行的性能策略。加载阶段必要的分配和持续循环里的分配应分别看待。

正确的优化过程：先固定复现条件，用 Profiler 看 CPU 与 GC.Alloc，定位调用栈，改一处，再用同样条件对比。不能只凭“用了对象池”就声称零 GC。

本项目可练习检查的目标：量规日志、捕获 Note 的 lambda、候选列表容量增长、Enum.GetValues、池不足扩容，以及长条 Mesh/Material 清理。它们是检查目标，尚无本轮测量数字。

面试报告应包括设备、构建方式、谱面、帧率设置、采样时长及改前改后数据。平均 FPS 可能掩盖偶发长帧；音游还应关心帧时间分布和输入误差。

## 12. Coroutine、Task 与 UniTask

当前 `LiveStart`、音乐准备和 Resume 倒计时主要用 Coroutine。

Coroutine 可以分多帧执行，但不是自动开启后台线程。`yield return null` 让出直到后续帧；`WaitForSeconds` 受 scaled 时间及帧边界影响，不是采样精度音频定时器。

`async/await` 表达等待过程，不等于并行或新线程。异步任务的异常、取消、对象销毁后的回调都需要处理。[Microsoft 异步编程文档](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)

UniTask 针对 Unity 提供更轻量的异步集成，但外围字符串、闭包、第三方 API 等仍可分配，不能承诺整个业务路径必然零 GC。学习时重点掌握取消令牌、异常传播与生命周期，而不是只把 Coroutine 改写成 await。[UniTask 官方仓库](https://github.com/Cysharp/UniTask)

练习目标：做独立的加载页面，在退出页面时取消等待，失败时展示可理解状态，确保旧请求完成后不会覆盖新选曲。完成后才能写“做过带取消的异步加载样例”。

## 13. 事件与 UniRx 的关系

当前工程用 C# 事件连接计分与显示。UniRx 是可观察流/订阅的另一套组织方式，并不是音游的必需条件。

先用普通事件理解“发布、订阅、变换、结束、解除订阅”，再阅读公司实际使用版本的官方资料。不要为了匹配 JD 把全部事件贸然换成响应式框架。

独立练习可以是：选曲变化后更新标题/封面，连续切歌时丢弃旧加载结果，退出页面后不再响应。关键是解释数据流与释放，而不是 API 名称记忆。

## 14. Resources、StreamingAssets、AssetBundle 与 Addressables

| 名称 | 先理解的角色 | 与当前工程的关系 |
| --- | --- | --- |
| Inspector 引用 | 显式配置资源关系 | 场景和 ScriptableObject 广泛使用 |
| Resources | 通过特定路径加载打包资源 | 工程有相关资源路径与配置入口 |
| StreamingAssets | 将附带文件放入构建，访问方式因平台而异 | 当前外部谱面和 FMOD bank |
| AssetBundle | Unity 资源构建/分发单元 | 字段名含 Bundle 不证明实际构建/下载了 Bundle |
| Addressables | 通过地址与句柄组织加载、依赖和释放 | 当前未见集成，属于补习项 |

Addressables 基础练习应包含：加载一张歌曲封面、保存句柄、切歌时处理旧结果、退出页面时释放、展示加载失败。加载与释放需要成对管理；引用计数归零也不等于你立刻观察到所有内存都归还。[Addressables 内存管理文档](https://docs.unity.cn/Packages/com.unity.addressables@2.3/manual/MemoryManagement.html)

不要在尚未理解资源所有权前，把本项目所有读取路径一次性换成 Addressables。

## 15. 数学与渲染的最低知识

你至少需要解释：

- 归一化 `t`：把某段范围换成 0～1，便于插值。
- `Lerp(a,b,t)`：在两个值间插值；是否 Clamp 看具体 API。
- `Clamp`：把值限制在范围内，不表示数据原本合理。
- 坐标：屏幕/世界/局部坐标不同，不能直接相比较。
- 区间相交：`leftA <= rightB && rightA >= leftB`。
- 网格：顶点位置 + 三角索引决定形状；UV 决定采样纹理的位置；颜色和参数影响外观。
- 透明渲染：混合、排序、深度与重复覆盖像素的成本。

练习：画两个区间，分别画出不相交、贴边、部分重叠、完全覆盖，预测箭头框与音符范围判断应返回什么。

不用一开始背完整 Shader 语法；先能解释 CPU 传了什么，以及 Shader 对这些数据做了什么。

## 16. 游戏开发的验证方式

| 验证层 | 适合的问题 | 示例 |
| --- | --- | --- |
| 纯逻辑 | 可确定输入输出的规则 | BPM 分段计算、阈值边界、物理输入分配 |
| 编辑器集成 | Unity 组件和生命周期 | Prefab 引用、事件退订、池重置 |
| 构建/设备 | 平台和资源差异 | Android 文件读取、音频延迟、后台恢复 |
| 玩家体验 | 可玩性和反馈 | 操作舒适度、信息可读性、难度曲线 |

Auto 播放适合看谱面和基础播放流程，但不能覆盖手动操作延迟、丢事件、多触点或真实设备反馈。

测试判定边界时，先定义单位与闭区间规则。把目标时间设为 5 秒，输入设在阈值内、边界上、边界外，分别列预期。浮点边界可能有表示误差，要设计有意义的容差或选取可控表示；不要把任意 epsilon 当成正确性证明。

## 17. 不看资料自测

先在纸上答，再看参考。

1. `MusicScore` 与 `ScoreLogic` 分别是什么？
2. 逻辑 Note 与显示 GameObject 为什么要分开？
3. `k4` 是否就是内部轨道 4？
4. 目标 5.000，输入 4.980，当前 LiveConfig 的 offset 正负及 Fast/Late？
5. 120 FPS 会把当前普通 Tap 的 Perfect 窗口减半吗？
6. 为什么“音乐时间准确”仍不能保证输入判定准确？
7. 一个自由按键覆盖三个逻辑轨道，如何避免重复命中？
8. 为什么一直按住不能自动打中所有普通 Tap？
9. 长按和 Tap 的状态有什么区别？
10. `ArrowJudgmentFrame` 复制后 SizeStages 是否独立？
11. 有 NotePool 能否证明演奏时没有 Instantiate 或 GC？
12. Coroutine 是否就是后台线程？
13. 源码倍率 0.8，场景 1.5，实际生效应查哪里？
14. Unity 能切 Android，是否等于已经具备手游移植经验？

参考答案：

1. 前者是谱面时间/音符容器，后者计算成绩。
2. 规则与呈现分开，也便于复用显示对象和替换外观。
3. 不是；当前映射为固定键索引 3，对应内部起始轨道 10。
4. `+0.020`，Fast。
5. 不会；当前换算常量是固定 1/60 秒。
6. 当前帧轮询会引入采样/消费延迟，还存在设备和输出链路差异。
7. 按同一物理来源分组，筛选一个候选；真实和弦规则还需设计。
8. 普通 Tap 要求 `Began`，持续输入是 `Moved`。
9. 长按要维护持续有效、断开、容错、中继与末端等条件。
10. 数组引用仍共享；数值字段是复制。
11. 不能；当前池可扩容，其他热路径也可能分配。
12. 不是，通常是在 Unity 主线程分帧恢复执行。
13. 先看当前实例的序列化覆盖，再看是否被运行时代码改写。
14. 不是；应有具体适配、构建和设备验证经历。

## 18. 三个建议独立完成的小练习

**练习 A：纯 C# Tap 判定器。**输入目标时间、输入时间、轨道和输入阶段；输出等级及 Fast/Late。先写出 10 个输入输出样例，再实现。重点是条件、单位、边界与副作用。

**练习 B：不依赖游戏的输入分配。**三条逻辑输入来自同一个物理按键，两颗 Note 同时可能命中；写出你的候选筛选规则。另加两颗不同固定键的同时输入，确保两者身份独立。

**练习 C：歌曲加载的小状态机。**用文字或少量代码表达 Idle → Loading → Ready → Playing → Paused → Finished，以及失败与取消。解释在哪个状态拥有音乐实例和资源句柄，何时释放。

做完后把“我预计什么、实际什么、为什么”的记录带到面试，比只说“我学习能力很强”更具体。
