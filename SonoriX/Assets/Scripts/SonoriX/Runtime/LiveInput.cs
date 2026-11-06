namespace SonoriX.Runtime
{
    //用于记录Input类型
    public enum LiveInputType
    {
        FixedLane,
        FreeLane,
        Arrow
    }
    
    public enum LiveInputPhase
    {
        Began,
        Holded,
        Moved,
        Ended
    }
    
    //Input抽象数据结构体
    public readonly struct LiveInput
    {
        //这次Input的类型
        public LiveInputType Type { get; }
        //这次Input的音乐时间（MusicTime）
        public float Time { get; }
        //这次Input的Lane位置
        public int LaneID { get; }
        //输入阶段信息
        public LiveInputPhase Phase { get; }
        
        //Free输入使用Lane单位，不是屏幕像素
        public float CenterLane { get; }
        public float Width { get; }
        
        //对Free输入，Moved表示按住期间的位置变化
        public bool IsPressed =>
            Phase == LiveInputPhase.Began
            || Phase == LiveInputPhase.Holded
            || Phase == LiveInputPhase.Moved;
        
        public LiveInput(
            LiveInputType type,
            float time,
            int laneID,
            LiveInputPhase phase,
            float centerLane = 0f,
            float width = 0f)
        {
            Type = type;
            Time = time;
            LaneID = laneID;
            Phase = phase;
            CenterLane = centerLane;
            Width = width;
        }
        
        //工具函数，生产Input数据类型Tap,暂时只用于Tap类型
        public static LiveInput FixedLane(float time, int laneID, LiveInputPhase phase)
        {
            return new LiveInput(LiveInputType.FixedLane, time, laneID, phase);
        }

        public static LiveInput FreeLane(float time, float centerLane, float width, LiveInputPhase phase)
        {
            return new LiveInput(
                LiveInputType.FreeLane,
                time,
                -1,
                phase,
                centerLane,
                width);
        }

        public static LiveInput Arrow(
            float time,
            float centerLane,
            float width)
        {
            return new LiveInput(
                LiveInputType.Arrow,
                time,
                -1,
                LiveInputPhase.Moved,
                centerLane,
                width);
        }
    }
}