using SonoriX.Domain;
using SonoriX.Domain.Charts;

namespace SonoriX.Runtime
{
    
    //记录Note的判定结果类型
    public enum NoteJudgment
    {
        None,
        JustPerfect,
        Perfect,
        Great,
        Good,
        Bad,
        Miss
    }

    public enum NoteJudgmentDescriptor
    {
        None,
        Late,
        Fast
    }

    //判定结果结构体
    public readonly struct JudgmentResult
    {
        public RuntimeNote Note { get; }
        public LiveInput? Input { get; }
        public NoteJudgment Judgment { get; }
        public NoteJudgmentDescriptor Descriptor { get; }
        public ChartLongPointRole PointRole { get; }
        //-1表示普通判定，非负数表示长条持续节点索引
        public int HoldPointIndex { get; }

        public bool IsHoldPoint => HoldPointIndex >= 0;

        public bool CompletesNote =>
            Judgment != NoteJudgment.None
            && (PointRole == ChartLongPointRole.End
                || (!IsHoldPoint
                    && PointRole == ChartLongPointRole.None));
        
        public JudgmentResult(
            RuntimeNote note,
            LiveInput? input,
            NoteJudgment judgment,
            NoteJudgmentDescriptor descriptor,
            ChartLongPointRole pointRole = ChartLongPointRole.None,
            int holdPointIndex = -1)
        {
            Note = note;
            Input = input;
            Judgment = judgment;
            Descriptor = descriptor;
            PointRole = pointRole;
            HoldPointIndex = holdPointIndex;
        }
        
        
    }
}
