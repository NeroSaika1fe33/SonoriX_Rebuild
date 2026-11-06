namespace SonoriX.Domain.Charts
{
    //谱面编辑坐标
    public readonly struct ChartPosition
    {
        public int Bar { get; }
        public float Tick { get; }

        public ChartPosition(int bar, float tick)
        {
            Bar = bar;
            Tick = tick;
        }
    }
}