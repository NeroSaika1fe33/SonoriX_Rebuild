using System.Collections.Generic;
using SonoriX.Domain;
using SonoriX.Domain.Charts;

namespace SonoriX.Runtime
{
    public readonly struct ChartFrame
    {
        //可以进入判定区间并尝试配对Input的Note
        public IReadOnlyList<RuntimeNote> JudgeableNotes { get; }
        //因超过判定时间而判定Miss的判定Note
        public IReadOnlyList<JudgmentResult> MissedResults { get; }
        //当前谱面是否完成
        public bool IsFinished { get; }
        //当前音乐时间
        public float MusicTime { get; }
        //可见Note
        public IReadOnlyList<RuntimeNote> VisibleNotes { get; }
            
        public ChartFrame(
            IReadOnlyList<RuntimeNote> judgeableNotes,
            IReadOnlyList<JudgmentResult> missedResults,
            bool isFinished,
            float musicTime,
            IReadOnlyList<RuntimeNote> visibleNotes
            )
        {
            JudgeableNotes = judgeableNotes;
            MissedResults = missedResults;
            IsFinished = isFinished;
            MusicTime = musicTime;
            VisibleNotes = visibleNotes;
        }
    }
    
    //判定超时Note为Miss，并筛选出可见和可判定的Note，记录已完成判定的Note
    public class ChartPlaybackSystem
    {
        private readonly ChartData chartData;
        private readonly HashSet<int> completeNoteIds = new HashSet<int>();
        private readonly JudgmentRules rules;
        private readonly float appearWindow;

        public ChartPlaybackSystem(ChartData chartData, JudgmentRules rules,float appearWindow)
        {
            this.chartData = chartData;
            this.rules = rules;
            this.appearWindow = appearWindow;
        }

        public ChartFrame Update(float musicTime)
        {
            List<RuntimeNote> judgeableNotes = new List<RuntimeNote>();
            List<JudgmentResult> missedResults = new List<JudgmentResult>();
            List<RuntimeNote> visibleNotes = new List<RuntimeNote>();
            //判定这一帧里的所有note，分别将他们放入超时Miss判定和可见和可被判定分类
            foreach (RuntimeNote note in chartData.Notes)
            {
                if (completeNoteIds.Contains(note.Id))
                {
                    continue;
                }
                
                float judgeTime = musicTime - note.Time;

                //超时Note
                if (note is LongNote longNote)
                {
                    //起点超时直接算起点，长条继续存在
                    if (longNote.Category != ChartNoteCategory.SliderLong
                        && !longNote.StartJudged
                        && judgeTime > rules.MaxWindow)
                    {
                        longNote.MarkStartJudged();

                        missedResults.Add(new JudgmentResult(
                            longNote,
                            null,
                            NoteJudgment.Miss,
                            NoteJudgmentDescriptor.Late,
                            ChartLongPointRole.Start));
                    }
                }
                else if(judgeTime > rules.MaxWindow)
                {
                    missedResults.Add(new JudgmentResult(
                        note,
                        null,
                        NoteJudgment.Miss,
                        NoteJudgmentDescriptor.Late));
                    
                    completeNoteIds.Add(note.Id);
                    continue;
                }
                
                //可见Note
                if (judgeTime >= -appearWindow)
                {
                    visibleNotes.Add(note);
                }
                
                //可判定Note
                if (judgeTime >= -rules.MaxWindow)
                {
                    judgeableNotes.Add(note);
                }
            }
            
            bool isFinished = completeNoteIds.Count >= chartData.Notes.Count;
            return new ChartFrame(judgeableNotes, missedResults, isFinished, musicTime, visibleNotes);
        }

        public void CompleteJudgedNotes(IReadOnlyList<JudgmentResult> results)
        {
            foreach (JudgmentResult result in results)
            {
                if (result.CompletesNote)
                {
                    completeNoteIds.Add(result.Note.Id);
                }
            }
        }
    }
}