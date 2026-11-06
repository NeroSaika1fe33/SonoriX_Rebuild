using FFrameWork.Live;
	
namespace FFrameWork
{
	// ロングノーツ上の点が持つ役割
	public enum LongPointRole
	{
		// 自動判別する
		Auto = 0,

		// ロングノーツ開始点
		Start = 1,

		// ロングノーツ中継点
		Relay = 2,

		// ロングノーツ終点
		End = 3
	}

	// SUS から読み取ったノーツ情報
	public class NoteInfo
	{
		// 小節番号
		public int Bar { get; private set; }

		// 小節内の進行率
		public float BarProgress { get; private set; }

		// ノーツの左端レーン
		public int Lane { get; private set; }

		// ロングノーツを接続するための ID
		public int LongNo { get; private set; }

		// ノーツ種別
		public NoteCategory Category { get; private set; }
		
		// フリックやボリューム操作の方向
		public NoteDirection Direction { get; private set; }

		// ライン形状
		public NoteLineType LineType { get; private set; }

		// ノーツ幅
		public int Width { get; private set; }

		// ロングノーツ上の役割
		public LongPointRole LongPointRole { get; private set; }

		// ノーツ固有の速度倍率
		public float SpeedRatio { get; set; }

		// ノーツ情報を初期化する
		public NoteInfo(int bar, float barProgress, int lane, int width, NoteCategory category, NoteDirection direction, NoteLineType lineType, float speedRatio, int longNo = -1, LongPointRole longPointRole = LongPointRole.Auto)
		{
			Bar = bar;
			BarProgress = barProgress;
			Lane = lane;
			LongNo = longNo;
			Category = category;
			LineType = lineType;
			Width = width;
			Direction = direction;
			SpeedRatio = speedRatio;
			LongPointRole = longPointRole;
		}

		// 同じ位置にある追加情報で既存ノーツ情報を更新する
		public void Update(NoteCategory category, NoteDirection direction, NoteLineType lineType, float speedRatio, int longNo = -1, LongPointRole longPointRole = LongPointRole.Auto)
		{
			if (direction != NoteDirection.Default)
			{
				Direction = direction;
			}
			if (lineType != NoteLineType.Linear)
			{
				LineType = lineType;
			}
			if (!1f.Equals(speedRatio))
			{
				SpeedRatio = speedRatio;
			}
			if (longNo != -1)
			{
				LongNo = longNo;
			}
			if (longPointRole != LongPointRole.Auto)
			{
				LongPointRole = longPointRole;
			}
		}
	}
}
