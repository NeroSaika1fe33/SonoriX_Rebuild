namespace FFrameWork.Live
{
	// ロングノートの途中点を表す、判定なしの中継ノート
	public class LongRelayNote : NoteBase
	{
		// ロングノート線の描画タイプ
		private readonly NoteLineType lineType;

		// ロングノート線の描画タイプ
		public override NoteLineType LineType
		{
			get { return lineType; }
		}

		// 中継ノートは入力判定対象外
		public override bool HasJudgment
		{
			get { return false; }
		}

		// ロングノートの中継ノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		public LongRelayNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
			this.lineType = lineType;
		}

		// 中継ノートの進行状態を更新し、時刻到達後は通過扱いにする
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			this.offsetTime = offsetTime;
			if (State == NoteState.Done)
			{
				return;
			}
			if (State == NoteState.Wait)
			{
				State = NoteState.Playing;
			}

			Progress = CalcProgress(currentFrameInfo, offsetTime);
			if (MusicScoreInfo.time <= currentFrameInfo.time)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Done;
			}
		}

		// 中継ノートを通過扱いで強制終了する
		public override void Terminate()
		{
			if (State != NoteState.Done)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Done;
			}
		}

		// オート再生時に中継ノートを通過扱いにする
		// currentFrameInfo: 現在フレームの楽曲情報
		public override bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State == NoteState.Done || MusicScoreInfo.time > currentFrameInfo.time)
			{
				return false;
			}

			OffsetJudgeTime = 0f;
			Progress = 1f;
			JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
			State = NoteState.Done;
			return true;
		}

		// 中継ノートは個別の出現処理を行わない
		protected override void OnSpawnNote()
		{
		}

		// 中継ノートは個別の終了処理を行わない
		protected override void OnUnSpawnNote()
		{
		}
	}
}
