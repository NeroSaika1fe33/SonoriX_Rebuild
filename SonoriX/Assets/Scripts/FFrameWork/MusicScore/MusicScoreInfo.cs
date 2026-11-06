namespace FFrameWork.Live
{
	public struct MusicScoreInfo
	{
		// 現在の小節番号
		public int bar;

		// 小節内の進行率
		public float barProgress;

		// 楽曲開始からの秒数
		public float time;

		// 現在の BPM
		public float bpm;

		// 1 小節あたりの拍数
		public float timeSignature;

		// ハイスピード倍率
		public float speedRatio;

		// SE 音量倍率
		public float seVolume;

		// 譜面時間情報をまとめて初期化する
		public MusicScoreInfo(int bar, float barProgress, float time, float bpm, float timeSignature, float speedRatio, float seVolume)
		{
			this.bar = bar;
			this.barProgress = barProgress;
			this.time = time;
			this.bpm = bpm;
			this.timeSignature = timeSignature;
			this.speedRatio = speedRatio;
			this.seVolume = seVolume;
		}
	}
}
