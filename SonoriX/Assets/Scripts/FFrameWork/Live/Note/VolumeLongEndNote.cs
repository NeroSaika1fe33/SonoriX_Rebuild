namespace FFrameWork.Live
{
	// ボリュームロングノートの終端を表すノート
	public class VolumeLongEndNote : NoteBase
	{
		// ロングノート線の描画タイプ
		private readonly NoteLineType lineType;

		// ロングノート線の描画タイプ
		public override NoteLineType LineType
		{
			get { return lineType; }
		}

		// ボリュームロングノートの終端を生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		// direction: ボリューム変化の方向
		public VolumeLongEndNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear, NoteDirection direction = NoteDirection.Default)
			: base(info, id, laneStart, laneEnd, NoteCategory.VolumeFlickLong, bundleBuildData, speedRatio)
		{
			this.lineType = lineType;
			Direction = direction;
		}

		// ボリューム終端の進行とホールド継続状態を更新する
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

			JudgeInfo = (NoteResult.Perfect, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// 親ロングノート終了時にボリューム終端を強制的に確定する
		public override void Terminate()
		{
			if (State == NoteState.Done)
			{
				return;
			}

			JudgeInfo = parentNote == null || parentNote.IsHoldResultSuccessful
				? (NoteResult.Perfect, NoteResultDescription.None)
				: (NoteResult.Miss, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// オート再生時にボリューム終端を自動判定する
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
