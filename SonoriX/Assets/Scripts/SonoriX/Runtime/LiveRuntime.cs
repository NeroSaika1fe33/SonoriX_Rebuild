using System.Collections.Generic;

namespace SonoriX.Runtime
{
    public readonly struct LiveFrame
    {
        //本帧内的谱面
        public ChartFrame ChartFrame { get; }
        //按照input生产判定结果的list
        public IReadOnlyList<JudgmentResult> InputResults { get; }
        //全部的判定结果的list
        public IReadOnlyList<JudgmentResult> AllResults { get; }
        //本帧内的分数，combo，计数
        public LiveScore Score { get; }
            
        public LiveFrame(
            ChartFrame chartFrame,
            IReadOnlyList<JudgmentResult> inputResults,
            IReadOnlyList<JudgmentResult> allResults,
            LiveScore score
            )
        {
            ChartFrame = chartFrame;
            InputResults = inputResults;
            AllResults = allResults;
            Score = score;
        }
    }
    
    public class LiveRuntime
    {
        //谱面进程系统
        private readonly ChartPlaybackSystem chartPlaybackSystem;
        //判定系统
        private readonly JudgmentSystem judgmentSystem;
        //分数系统
        private readonly ScoreSystem scoreSystem;
        
        public LiveRuntime(
            ChartPlaybackSystem chartPlaybackSystem,
            JudgmentSystem judgmentSystem,
            ScoreSystem scoreSystem
                )
        {
            this.chartPlaybackSystem = chartPlaybackSystem;
            this.judgmentSystem = judgmentSystem;
            this.scoreSystem = scoreSystem;
        }
        
        /// <summary>
        /// 推进谱面状态，执行输入判定，并返回这一帧产生的判定结果。
        /// </summary>
        /// <param name="musicTime"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public LiveFrame Tick(float musicTime, IReadOnlyList<LiveInput> inputs)
        {
            ChartFrame chartFrame = chartPlaybackSystem.Update(musicTime);
            
            IReadOnlyList<JudgmentResult> inputResults =judgmentSystem.Evaluate(musicTime,chartFrame.JudgeableNotes, inputs);
            
            chartPlaybackSystem.CompleteJudgedNotes(inputResults);

            List<JudgmentResult> allResults = new List<JudgmentResult>();
            
            allResults.AddRange(chartFrame.MissedResults);
            allResults.AddRange(inputResults);
            
            //获取分数
            scoreSystem.ApplyResults(allResults);
            
            return new LiveFrame(chartFrame, inputResults, allResults,scoreSystem.CurrentScore);
        }
    }
}