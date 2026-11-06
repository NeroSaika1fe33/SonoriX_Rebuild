using SonoriX;
using SonoriX.App;
using SonoriX.Domain.Charts;
using SonoriX.Infrastructure.Charts;
using SonoriX.Infrastructure.Input;
using SonoriX.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SonoriX.Presentation;
using UnityEngine.Serialization;

[RequireComponent(typeof(LiveSceneController))]
public class LiveRuntimeTestDriver : MonoBehaviour
{
    [Header("启动")]
    [SerializeField] private bool autoBoot = false;
    
    [Header("FreeLaneInput")] [SerializeField]
    private bool enableFreeTestInput = true;
    [SerializeField] private LivePresentationRoot presentationRoot;
    [SerializeField] private RectTransform freeInputView;
    
    //内部lane8-11的中心，与谱面的L9，宽度4对应
    [SerializeField] private float freeStartCenter = 9.5f;

    [SerializeField, Range(1f, 32f)] private float freeInputWidth = 4f;
    [SerializeField, Min(0f)] private float freeMoveSpeed = 8f;
    
    [SerializeField] private LiveSceneController liveSceneController;
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private float startMusicTime;
    [SerializeField] private TextAsset chartText;

    [SerializeField] private InputActionReference lane0Action;
    [SerializeField] private InputActionReference lane1Action;
    [SerializeField] private InputActionReference lane2Action;
    [SerializeField] private InputActionReference lane3Action;

    [SerializeField] private float appearWindow = 2f;

    private InputActionRawInputReader rawInputReader;
    [SerializeField] private bool enableArrowTestInput = true;

    private void Awake()
    {
        if (liveSceneController == null)
        {
            liveSceneController = GetComponent<LiveSceneController>();
        }
    }

    private void Start()
    {
        if (autoBoot)
        {
            BootTestRuntime();
        }
    }

    [ContextMenu("Restart Test Runtime")]
    private void Restart()
    {
        rawInputReader?.Disable();
        BootTestRuntime();
    }

    private void BootTestRuntime()
    {
        if (liveSceneController == null)
        {
            Debug.LogError("LiveRuntimeTestDriver requires a LiveSceneController.");
            return;
        }

        LiveBootRequest request = CreateBootRequest();

        rawInputReader?.Enable();
        liveSceneController.Boot(request);

        Debug.Log($"LiveRuntime test booted. Notes:{request.Chart.Notes.Count} StartTime:{request.StartMusicTime:0.000}");
    }

    private LiveBootRequest CreateBootRequest()
    {
        return new LiveBootRequest(
            BuildChartDefinition(),
            JudgmentRules.Default,
            BuildInputReader(),
            new ManualLiveClock(playbackSpeed),
            startMusicTime,
            appearWindow
            );
    }

    private ChartDefinition BuildChartDefinition()
    {
        if (chartText == null)
        {
            throw new System.InvalidOperationException(
                "请在 LiveRuntimeTestDriver 的 Chart Text 中指定谱面文本。");
        }

        ChartSourceDefinition source =
            new SnxChartParser().Parse(chartText.text);

        ChartDefinition chart =
            new ChartSourceCompiler().Compile(source);

        foreach (ChartNoteDefinition note in chart.Notes)
        {
            if (note.HoldTimes.Count == 0)
            {
                continue;
            }

            Debug.Log(
                $"Long:{note.Id} " +
                $"Start:{note.Time:0.000} " +
                $"End:{note.EndTime:0.000} " +
                $"HoldTimes:[{string.Join(", ", note.HoldTimes)}]");
        }

        return chart;
    }

    private ILiveInputReader BuildInputReader()
    {
        rawInputReader = new InputActionRawInputReader(
            lane0Action != null ? lane0Action.action : null,
            lane1Action != null ? lane1Action.action : null,
            lane2Action != null ? lane2Action.action : null,
            lane3Action != null ? lane3Action.action : null);

        ILiveInputReader fixedReader =
            new LiveInputCollector(rawInputReader);

        return new FreeTestInputReader(
            fixedReader,
            this);
    }

    private void OnDisable()
    {
        rawInputReader?.Disable();
        liveSceneController?.Stop();
    }
    private sealed class FreeTestInputReader : ILiveInputReader
{
    private readonly ILiveInputReader fixedReader;
    private readonly LiveRuntimeTestDriver owner;

    private float centerLane;
    private bool wasPressed;

    public FreeTestInputReader(
        ILiveInputReader fixedReader,
        LiveRuntimeTestDriver owner)
    {
        this.fixedReader = fixedReader;
        this.owner = owner;

        centerLane = owner.freeStartCenter;
    }

    public IReadOnlyList<LiveInput> Read(float musicTime)
    {
        List<LiveInput> inputs =
            new List<LiveInput>(fixedReader.Read(musicTime));

        if (!owner.enableFreeTestInput
            && !owner.enableArrowTestInput)
        {
            wasPressed = false;

            if (owner.freeInputView != null)
            {
                owner.freeInputView.gameObject.SetActive(false);
            }

            return inputs;
        }

        Keyboard keyboard = Keyboard.current;

        float direction = 0f;

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed)
            {
                direction -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                direction += 1f;
            }
        }

        float width = Mathf.Clamp(
            owner.freeInputWidth, 1f, 32f);

        centerLane += direction
            * owner.freeMoveSpeed
            * Time.deltaTime;

        // 操作区域整体保持在内部 Lane 0～31 的边界内。
        float halfWidth = width * 0.5f;

        centerLane = Mathf.Clamp(
            centerLane,
            -0.5f + halfWidth,
            31.5f - halfWidth);

        bool isPressed =
            keyboard != null
            && keyboard.spaceKey.isPressed;

        if (owner.enableFreeTestInput)
        {
            if (isPressed)
            {
                LiveInputPhase phase = wasPressed
                    ? LiveInputPhase.Holded
                    : LiveInputPhase.Began;

                inputs.Add(LiveInput.FreeLane(
                    musicTime,
                    centerLane,
                    width,
                    phase));
            }
            else if (wasPressed)
            {
                inputs.Add(LiveInput.FreeLane(
                    musicTime,
                    centerLane,
                    width,
                    LiveInputPhase.Ended));
            }
        }

        wasPressed = owner.enableFreeTestInput && isPressed;
        
        if (owner.enableArrowTestInput)
        {
            inputs.Add(LiveInput.Arrow(
                musicTime,
                centerLane,
                width));
        }

        UpdateView(width);

        return inputs;
    }

    private void UpdateView(float width)
    {
        if (owner.freeInputView == null
            || owner.presentationRoot == null)
        {
            return;
        }

        LiveNoteLayout layout =
            owner.presentationRoot.LiveNoteLayout;

        if (layout == null)
        {
            return;
        }

        owner.freeInputView.anchoredPosition =
            layout.CalculateLaneJudgePosition(centerLane);

        owner.freeInputView.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            layout.CalculateLaneWidth(width));

        owner.freeInputView.gameObject.SetActive(true);
    }
}
}
