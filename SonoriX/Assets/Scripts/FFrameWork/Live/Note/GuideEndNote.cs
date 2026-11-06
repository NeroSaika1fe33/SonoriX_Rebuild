namespace FFrameWork.Live
{
	// ガイドノートの終端を表す表示専用ノート
	public class GuideEndNote : NoteBase
	{
		// ガイドラインの描画タイプ
		private readonly NoteLineType lineType;

		// ガイドラインの描画タイプ
		public override NoteLineType LineType
		{
			get { return lineType; }
		}

		// ガイド終端は入力判定対象外
		public override bool HasJudgment
		{
			get { return false; }
		}

		// ガイド終端ノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ガイドラインの描画タイプ
		public GuideEndNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
			this.lineType = lineType;
		}

		// 親ロングノートを設定し、ガイド用カテゴリへ固定する
		// note: 親として扱うロングノート
		public override void SetParentNote(LongNote note)
		{
			parentNote = note;
			Category = NoteCategory.GuideLong;
		}

		// ガイド終端ノートの進行状態を更新する
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			this.offsetTime = offsetTime;
			if (State == NoteState.Done)
			{
				return;
			}
			if (State == NoteState.Last)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Done;
				return;
			}
			if (State == NoteState.Wait)
			{
				State = NoteState.Playing;
			}

			Progress = CalcProgress(currentFrameInfo, offsetTime);
			if (MusicScoreInfo.time <= currentFrameInfo.time)
			{
				State = NoteState.Last;
			}
		}

		// ガイド終端を強制的に終了状態へ進める
		public override void Terminate()
		{
			if (State != NoteState.Done)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Done;
			}
		}

		// オート再生時にガイド終端を通過扱いにする
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

		// ガイド終端は個別の出現処理を行わない
		protected override void OnSpawnNote()
		{
		}

		// ガイド終端は個別の終了処理を行わない
		protected override void OnUnSpawnNote()
		{
		}
	}
}
