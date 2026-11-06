namespace FFrameWork
{
	// 小節ごとの拍子情報
	public class TimeSignatureInfo
	{
		// 1 小節あたりの拍数
		public float TimeSignature;

		// この拍子が始まる小節番号
		public int StartBarIndex;

		// 拍子情報を初期化する
		public TimeSignatureInfo(float timeSignature, int startBarIndex)
		{
			TimeSignature = timeSignature;
			StartBarIndex = startBarIndex;
		}
	}
}
