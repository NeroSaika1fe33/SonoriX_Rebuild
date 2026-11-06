namespace SonoriX.Domain.Charts
{
    //时间线参数节点
    public sealed class ChartTimingPoint
    {
        //谱面位置
        public ChartPosition Position { get; }
        //BPM
        public float Bpm { get;}
        public float TimeSignature { get; }
        public float SpeedRatio { get; }
        public float SeVolume { get; }

        public ChartTimingPoint(
            ChartPosition position,
            float bpm,
            float timeSignature,
            float speedRatio,
            float seVolume)
        {
            Position = position;
            Bpm = bpm;
            TimeSignature = timeSignature;
            SpeedRatio = speedRatio;
            SeVolume = seVolume;
        }
    }
}