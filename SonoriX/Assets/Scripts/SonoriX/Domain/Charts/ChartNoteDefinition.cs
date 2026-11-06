using System.Collections.Generic;

namespace SonoriX.Domain.Charts
{
    //从谱面拿出来的Note信息
    public sealed class ChartNoteDefinition
    {
        //NoteId
        public int Id { get; }
        //对应谱面时间
        public float Time { get; }
        //Note结束时间，一般只有长条型Note持有
        public float EndTime { get; }
        //记录每个Note位置的Lane信息
        public ChartLaneRange LaneRange { get; }
        //基本Note类型
        public ChartNoteCategory Category { get; }
        //Note方向
        public ChartNoteDirection Direction { get; }
        //长条型Note的链接方式
        public ChartLineType LineType { get; }
        //速度倍率
        public float SpeedRatio { get; }
        //是否持续，一般只有长条形Note持有
        public bool HasDuration => EndTime > Time;
        //根据持续计分生成，不属于长条路径链接点
        public IReadOnlyList<float> HoldTimes { get; }
        
        //长条路径
        public IReadOnlyList<ChartLongPointDefinition> LongPoints { get; }

        public ChartNoteDefinition(
            int id,
            float time,
            float endTime,
            ChartLaneRange laneRange,
            ChartNoteCategory category,
            ChartNoteDirection direction,
            ChartLineType lineType,
            float speedRatio,
            IReadOnlyList<ChartLongPointDefinition> longPoints = null,
            IReadOnlyList<float> holdTimes = null)
        {
            Id = id;
            Time = time;
            EndTime = endTime;
            LaneRange = laneRange;
            Category = category;
            Direction = direction;
            LineType = lineType;
            SpeedRatio = speedRatio;
            LongPoints = longPoints ?? System.Array.Empty<ChartLongPointDefinition>();
            HoldTimes = holdTimes ?? System.Array.Empty<float>();
        }
    }
}