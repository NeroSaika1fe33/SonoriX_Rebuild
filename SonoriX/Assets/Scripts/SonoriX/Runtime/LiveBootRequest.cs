using SonoriX.Domain;
using SonoriX.Domain.Charts;

namespace SonoriX.Runtime
{
    public sealed class LiveBootRequest
    {
        //谱面定义
        public ChartDefinition Chart { get; }
        //判定规则
        public JudgmentRules Rules { get; }
        //读取输入
        public ILiveInputReader InputReader { get; }
        //音乐时钟
        public ILiveClock Clock { get; }
        //可显示时间窗口
        public float AppearWindow { get; }
        //开始音乐时间
        public float StartMusicTime { get; }

        public LiveBootRequest(
            ChartDefinition chart,
            JudgmentRules rules,
            ILiveInputReader inputReader,
            ILiveClock clock,
            float startMusicTime,
            float appearWindow
            )
        {
            Chart = chart;
            Rules = rules;
            InputReader = inputReader;
            Clock = clock;
            StartMusicTime = startMusicTime;
            AppearWindow = appearWindow;
        }
    }
}
