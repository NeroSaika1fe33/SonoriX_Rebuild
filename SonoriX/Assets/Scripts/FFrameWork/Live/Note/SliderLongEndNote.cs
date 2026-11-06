namespace FFrameWork.Live
{
	// スライダーロングノートの終端を表すノート
	public class SliderLongEndNote : NoteBase
	{
		// スライダーロングノートの終端を生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		public SliderLongEndNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio)
			: base(info, id, laneStart, laneEnd, NoteCategory.SliderLong, bundleBuildData, speedRatio)
		{
		}

		// 親ロングノートを設定し、カテゴリをスライダーロングに固定する
		// note: 親として扱うロングノート
		public override void SetParentNote(LongNote note)
		{
			parentNote = note;
			Category = NoteCategory.SliderLong;
		}

		// スライダー終端の進行とホールド継続状態を更新する
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示・判定に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			this.offsetTime = offsetTime;
			if (State == NoteState.Done)
			{
				return;
			}
			if (State == NoteState.Last)
			{
				JudgeInfo = (NoteResult.Miss, NoteResultDescription.None);
				State = NoteState.Done;
				return;
			}
			if (State == NoteState.Wait)
			{
				State = NoteState.Playing;
			}

			Progress = CalcProgress(currentFrameInfo, offsetTime);
			if (MusicScoreInfo.time > currentFrameInfo.time)
			{
				return;
			}

			if (ShouldWaitForInput())
			{
				State = NoteState.Last;
				return;
			}

			JudgeInfo = (NoteResult.JustPerfect, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// 親ロングノート終了時にスライダー終端を強制的に確定する
		public override void Terminate()
		{
			if (State == NoteState.Done)
			{
				return;
			}

			JudgeInfo = parentNote == null || parentNote.IsHoldResultSuccessful
				? (NoteResult.JustPerfect, NoteResultDescription.None)
				: (NoteResult.Miss, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// オート再生時にスライダー終端を自動判定する
		// currentFrameInfo: 現在フレームの楽曲情報
		public override bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State == NoteState.Done || MusicScoreInfo.time > currentFrameInfo.time)
			{
				return false;
			}

			OffsetJudgeTime = 0f;
			Progress = 1f;
			JudgeInfo = (NoteResult.Perfect, NoteResultDescription.None);
			State = NoteState.Done;
			return true;
		}

		// 親ロングノートのホールド入力を待つ必要があるか確認する
		private bool ShouldWaitForInput()
		{
			return parentNote == null || !parentNote.IsHoldJudgmentActive;
		}
	}
}
