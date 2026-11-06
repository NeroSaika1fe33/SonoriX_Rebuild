namespace SonoriX.Runtime
{
    public enum RawInputType
    {
        FixedLane,
        FreeLane,
        ArrowPosition,
        ArrowSize
    }

    public enum RawInputPhase
    {
        Began,
        Holded,
        Moved,
        Ended
    }

    public readonly struct RawInputCommand
    {
        public RawInputType Type { get; }
        public RawInputPhase Phase { get; }
        public int LaneId { get; }
        public float Value { get; }

        public RawInputCommand(RawInputType type, RawInputPhase phase, int laneId, float value)
        {
            Type = type;
            Phase = phase;
            LaneId = laneId;
            Value = value;
        }
    }
}

