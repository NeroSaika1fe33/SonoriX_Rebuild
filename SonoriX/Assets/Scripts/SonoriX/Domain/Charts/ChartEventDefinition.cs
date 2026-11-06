namespace SonoriX.Domain.Charts
{
    // 编译后的播放事件，时间单位为秒。
    public sealed class ChartEventDefinition
    {
        public float Time { get; }
        public ChartEventType Type { get; }
        public int? PresetId { get; }

        public ChartEventDefinition(
            float time,
            ChartEventType type,
            int? presetId = null)
        {
            Time = time;
            Type = type;
            PresetId = presetId;
        }
    }
}