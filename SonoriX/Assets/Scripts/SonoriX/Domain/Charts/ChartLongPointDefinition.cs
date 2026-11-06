namespace SonoriX.Domain.Charts
{
    //编译后的长条路径点
    public sealed class ChartLongPointDefinition
    {
        public int Id { get; }
        public float Time { get; }
        public ChartLaneRange LaneRange { get; }
        public ChartLongPointRole Role { get; }
        public ChartNoteDirection Direction { get; }
        public ChartLineType LineType { get; }
        public float SpeedRatio { get; }

        public ChartLongPointDefinition(
            int Id,
            float time,
            ChartLaneRange laneRange,
            ChartLongPointRole role,
            ChartNoteDirection direction,
            ChartLineType lineType,
            float speedRatio)
        {
            this.Id = Id;
            this.Time = time;
            this.LaneRange = laneRange;
            this.Role = role;
            this.Direction = direction;
            this.LineType = lineType;
            this.SpeedRatio = speedRatio;
        }
    }
}