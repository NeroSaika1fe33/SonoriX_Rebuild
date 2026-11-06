namespace FFrameWork
{
	// ハイスピード変化情報
	public class HighSpeedInfo
	{
		// 適用される小節番号
		public int BarIndex { get; private set; }

		// 小節内 Tick 位置
		public int TickOffset { get; private set; }

		// ハイスピード倍率
		public float SpeedRatio { get; private set; }

		// ハイスピード変化情報を初期化する
		public HighSpeedInfo(int barIndex, int tickOffset, float speedRatio)
		{
			BarIndex = barIndex;
			TickOffset = tickOffset;
			SpeedRatio = speedRatio;
		}
	}
}
