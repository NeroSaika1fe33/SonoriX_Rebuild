using System.Collections.Generic;
using SonoriX.Runtime;
using UnityEngine;

namespace SonoriX.App
{
    // 不创建测试谱面、Note、具体输入设备或具体时钟，只执行 LiveBootRequest。
    public class LiveSceneController : MonoBehaviour
    {
        [SerializeField] private bool autoRun = true;

        private readonly LiveRuntimeFactory runtimeFactory = new LiveRuntimeFactory();

        private LiveRuntime runtime;
        private ILiveInputReader inputReader;
        private ILiveClock clock;
        private bool isBooted;
        private bool isFinished;
        public LiveFrame LastFrame { get; private set; }
        private LiveScore lastScore;
        private bool hasScore;

        public bool IsBooted => isBooted;
        public bool IsFinished => isFinished;
        public float CurrentMusicTime => clock != null ? clock.CurrentTime : 0f;
        
        public event System.Action<LiveFrame> FrameUpdated;
        public event System.Action<JudgmentResult> JudgmentProduced;
        public event System.Action<LiveScore> ScoreUpdated;
        public event System.Action RuntimeReset;

        public void Boot(LiveBootRequest request)
        {
            Stop();

            runtime = runtimeFactory.Create(
                request.Chart,
                request.Rules,
                request.AppearWindow);
            
            inputReader = request.InputReader;
            clock = request.Clock;
            
            clock.Start(request.StartMusicTime);

            isBooted = true;
            isFinished = false;
            lastScore = default;
            hasScore = false;
        }

        private void Update()
        {
            if (!autoRun || !isBooted || isFinished)
            {
                return;
            }

            Tick(Time.deltaTime);
        }
        
        //每帧生产出“帧信息”
        public LiveFrame Tick(float deltaTime)
        {
            if (!isBooted || runtime == null || clock == null)
            {
                return LastFrame;
            }
            
            //从deltaTime获取音乐时间
            clock.Tick(deltaTime);
            
            float musicTime = clock.CurrentTime;

            //获得当前帧的输入数组
            IReadOnlyList<LiveInput> inputs = inputReader != null
                ? inputReader.Read(musicTime)
                : System.Array.Empty<LiveInput>();

            //把Runtime往后推进一帧
            LastFrame = runtime.Tick(musicTime, inputs);
            //触发事件
            PublishFrameEvents(LastFrame);

            if (LastFrame.ChartFrame.IsFinished)
            {
                isFinished = true;
                clock.Stop();
            }
            return LastFrame;
        }

        private void PublishFrameEvents(LiveFrame frame)
        {
            //触发事件，并传入最新一帧信息
            FrameUpdated?.Invoke(frame);

            //触发事件，生成日志
            foreach (JudgmentResult result in frame.AllResults)
            {
                JudgmentProduced?.Invoke(result);
            }

            //只有在分数发生变化时才触发日志更新事件
            if (!hasScore || !frame.Score.Equals(lastScore))
            {
                lastScore = frame.Score;
                hasScore = true;
                ScoreUpdated?.Invoke(frame.Score);
            }
        }

        public void Stop()
        {
            clock?.Stop();

            runtime = null;
            inputReader = null;
            clock = null;

            isBooted = false;
            isFinished = false;

            LastFrame = default;
            lastScore = default;
            hasScore = false;

            RuntimeReset?.Invoke();
        }
    }
}

