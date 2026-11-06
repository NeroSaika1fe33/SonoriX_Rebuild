namespace FFrameWork.Live
{
    public enum LiveResult
    {
        None = 0,
        Retire = 1,
        Failure = 2,
        Clear = 3
    }

    // 数値が大きいほど上位のクリア記録（AutoPlay は記録対象外）
    public enum LiveClearStatus
    {
        None = 0,
        TrackCrash = 1,
        TrackClear = 2,
        FullCombo = 3,
        AllPerfect = 4,
        AutoPlay = 5
    }
}
