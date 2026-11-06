namespace SonoriX.Domain.Charts
{
    //Note所具备的用于记录位置的Lane编号
    public readonly struct ChartLaneRange
    {
        //Note的起点Lane编号
        public int StartLane { get; }
        //Note的终点编号
        public int EndLane { get; }

        public ChartLaneRange(int startLane, int endLane)
        {
            if (endLane < startLane)
            {
                throw new System.ArgumentException(
                    "右侧轨道编号不能小于左侧轨道编号。");
            }
            
            StartLane = startLane;
            EndLane = endLane;
        }
    }
}