namespace FFrameWork
{
	// ライブ中に発生するイベント情報
	public class EventInfo
	{
		// イベントが発生する小節番号
		public int Bar { get; private set; }

		// 小節内のイベント発生位置
		public float BarProgress { get; private set; }

		// イベント種別
		public EventType EventType { get; private set; }

		// イベント情報を初期化する
		public EventInfo(int bar, float barProgress, EventType eventType)
		{
			Bar = bar;
			BarProgress = barProgress;
			EventType = eventType;
		}
	}
}
