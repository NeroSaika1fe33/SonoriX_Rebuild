namespace SonoriX.Domain.Charts
{
    //保存整体轨道信息
    public class ChartLaneLayout
    {
        public const int LaneCount = 32;

        public const int FixedKeyCount = 4;
        public const int FixedKeyWidth = 4;
        
        //固定4key整体覆盖内部Lane8-23
        public const int FixedFrameStartLane = 8;
        public const int FixedFrameEndLane = 23;
        public const int FixedEndLaneExclusive =
            FixedFrameStartLane + FixedKeyCount * FixedKeyWidth;

        //输入编号0-3
        public static ChartLaneRange GetFixedKeyRange(int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= FixedKeyCount)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(keyIndex),
                    "固定键输入编号必须在 0～3 之间。");
            }
            
            int startLane = FixedFrameStartLane + keyIndex* FixedKeyWidth;

            return new ChartLaneRange(
                startLane,
                startLane + FixedKeyWidth - 1);
        }
        
        //判断谱面范围是否覆盖这个固定键
        public static bool ContainsFixedKey(
            ChartLaneRange noteRange, int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= FixedKeyCount)
            {
                return false;
            }
            
            ChartLaneRange keyRange = GetFixedKeyRange(keyIndex);

            return noteRange.StartLane <= keyRange.StartLane
                   && noteRange.EndLane >= keyRange.EndLane;
        }
    }
}