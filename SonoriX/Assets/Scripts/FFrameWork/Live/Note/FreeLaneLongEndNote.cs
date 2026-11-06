namespace FFrameWork.Live
{
	// フリーレーンロングノートの終端判定を扱うノート
	public class FreeLaneLongEndNote : NoteBase
	{
		// フリーレーンロングノートの終端を生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		public FreeLaneLongEndNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio)
			: this(info, id, laneStart, laneEnd, NoteCategory.FreeLaneLong, bundleBuildData, speedRatio)
		{
		}

		// 派生クラス用にカテゴリを指定して終端ノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: 親ロングノートに合わせるノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		protected FreeLaneLongEndNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
		}

		// 終端が Done になったとき、親ロングノートも終了させる状態
		public override NoteState State
		{
			get { return base.State; }
			protected set
			{
				if (value == NoteState.Done && State != NoteState.Done && parentNote != null)
				{
					parentNote.ForceTerminate();
				}
				base.State = value;
			}
		}

		// 親ロングノートを設定し、カテゴリを親に合わせる
		// note: 親として扱うロングノート
		public override void SetParentNote(LongNote note)
		{
			base.SetParentNote(note);
			Category = parentNote != null ? parentNote.Category : Category;
		}

		// 入力がロングノート終端の判定条件を満たすか確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Done || parentNote == null || JudgeLaneStart > lane || JudgeLaneEnd < lane)
			{
				return false;
			}

			JudgeFrameType frameType = LiveUtility.GetINoteToJudgeFrameType(this);
			if (!LiveUtility.IsJudgmentTiming(frameType, MusicScoreInfo.time - liveInput.musicTime))
			{
				return false;
			}

			return IsLongEndInputPhase(ref liveInput);
		}

		// ロングノート終端の判定結果を確定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Done || !IsLongEndInputPhase(ref liveInput))
			{
				return false;
			}

			(NoteResult, NoteResultDescription) result = IsHoldThroughLongEnd(ref liveInput)
				? (NoteResult.JustPerfect, NoteResultDescription.None)
				: LiveConfig.CalculateLongEndNoteResult(this, liveInput.musicTime);
			JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
			State = NoteState.Done;
			return true;
		}

		// 指定された楽曲時間で終端ノートの判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return LiveConfig.CalculateLongEndNoteResult(this, musicTime).Item1;
		}

		// 入力フェーズが終了またはキャンセルか確認する
		// phase: 確認対象の入力フェーズ
		private static bool IsEndedOrCanceled(LiveInputPhase phase)
		{
			return phase == LiveInputPhase.Ended || phase == LiveInputPhase.Canceled;
		}

		// ロングノート終端として受け付ける入力フェーズか確認する
		// liveInput: 判定に使用する入力情報
		private bool IsLongEndInputPhase(ref LiveInput liveInput)
		{
			return IsEndedOrCanceled(liveInput.phase) || IsHoldThroughLongEnd(ref liveInput);
		}

		// 終端時刻までホールドが継続しているか確認する
		// liveInput: 判定に使用する入力情報
		private bool IsHoldThroughLongEnd(ref LiveInput liveInput)
		{
			return liveInput.phase == LiveInputPhase.Moved &&
				parentNote != null &&
				parentNote.IsHoldJudgmentActive &&
				liveInput.musicTime >= MusicScoreInfo.time;
		}
	}
}
