namespace SonoriX.Domain.Charts
{
    //原始谱面点
    public sealed class ChartNoteSourcePoint
    {
        public int Id { get; }
        public ChartPosition Position { get; }
        public ChartLaneRange LaneRange { get; }
        public ChartNoteCategory Category { get; }
        public ChartNoteDirection Direction { get; }
        public ChartLineType LineType { get; }
        public ChartLongPointRole LongPointRole { get; }
        public int LongGroupId { get; }
        public float SpeedRatio { get; }
        
        public bool IsLongPoint => LongPointRole != ChartLongPointRole.None;
        
        public ChartNoteSourcePoint(
            int id,
            ChartPosition position,
            ChartLaneRange laneRange,
            ChartNoteCategory category,
            ChartNoteDirection direction,
            ChartLineType lineType,
            ChartLongPointRole longPointRole,
            int longGroupId,
            float speedRatio)
        {
            Id = id;
            Position = position;
            LaneRange = laneRange;
            Category = category;
            Direction = direction;
            LineType = lineType;
            LongPointRole = longPointRole;
            LongGroupId = longGroupId;
            SpeedRatio = speedRatio;
        }
    }
}