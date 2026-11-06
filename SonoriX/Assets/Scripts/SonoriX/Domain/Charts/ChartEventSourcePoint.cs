namespace SonoriX.Domain.Charts
{
    //原始谱面事件
    public sealed class ChartEventSourcePoint
    {
        public ChartPosition Position { get; }
        public ChartEventType  Type { get; }
        
        //BPM,BPB,SPEED
        public float? NumericValue { get; }
        //SEC,BACK
        public int? PresetId { get; }
        
        public ChartEventSourcePoint(
            ChartPosition position,
            ChartEventType type,
            float? numericValue = null,
            int? presetId = null)
        {
            Position = position;
            Type = type;
            NumericValue = numericValue;
            PresetId = presetId;
        }
    }
}