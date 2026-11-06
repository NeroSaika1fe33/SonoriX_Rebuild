namespace FFrameWork
{
	// BPM と小節位置を保持する
	public class BpmInfo
	{
		// BPM 値
		public float bpm;

		// BPM が適用される小節番号
		public int bar;

		// 小節内の進行率
		public float barProgress;

		// BPM 情報を初期化する
		public BpmInfo(float bpm, int bar, float barProgress)
		{
			this.bpm = bpm;
			this.bar = bar;
			this.barProgress = barProgress;
		}
	}
}
