namespace FFrameWork.Live
{
	public class MusicBaseEventData
	{
		// イベントが属する小節番号
		public int barIndex;

		// 小節番号を指定してイベント基礎データを作成する
		// barIndex: イベントの基準になる小節番号
		public MusicBaseEventData(int barIndex)
		{
			this.barIndex = barIndex;
		}
	}
}
