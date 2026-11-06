namespace FFrameWork.Live
{
	// 固定レーン用ロングノートの開始ノート
	public class FixedLongNote : LongNote
	{
		// 固定レーンロングノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		public FixedLongNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio)
			: base(info, id, laneStart, laneEnd, NoteCategory.FixedLong, bundleBuildData, speedRatio, NoteLineType.Linear)
		{
		}
	}
}
