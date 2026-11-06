namespace FFrameWork.Live
{
	// ボリュームロング用のホールドコンボノート
	public class VolumeLongHoldCombo : LongHoldCombo
	{
		// ホールド中の補助コンボでは判定音を鳴らさない
		public override bool PlayJudgmentSound
		{
			get { return false; }
		}

		// ボリュームロング用ホールドコンボを生成する
		// info: このノートの譜面情報
		// liveBundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// laneOffset: 判定に使用するレーン補正
		public VolumeLongHoldCombo(MusicScoreInfo info, LiveBundleBuildData liveBundleBuildData, float speedRatio, float laneOffset)
			: base(info, liveBundleBuildData, speedRatio, laneOffset, NoteCategory.VolumeFlickLong)
		{
		}
	}
}
