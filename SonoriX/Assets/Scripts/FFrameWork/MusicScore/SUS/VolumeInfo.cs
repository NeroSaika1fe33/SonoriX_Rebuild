namespace FFrameWork
{
	// SE 音量変化情報
	public class VolumeInfo
	{
		// 適用される小節番号
		public int BarIndex { get; private set; }

		// 小節内 Tick 位置
		public int TickOffset { get; private set; }

		// SE 音量倍率
		public float Volume { get; private set; }

		// SE 音量変化情報を初期化する
		public VolumeInfo(int barIndex, int tickOffset, float volume)
		{
			BarIndex = barIndex;
			TickOffset = tickOffset;
			Volume = volume;
		}
	}
}
