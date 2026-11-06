namespace FFrameWork.Live
{
	// ロングノート継続中に発生するホールドコンボ用ノート
	public class LongHoldCombo : NoteBase
	{
		// 親ロングノートの判定幅に追加するレーン補正値
		private readonly float laneOffset;

		// ホールドコンボ用のレーン補正値
		public float LaneOffset
		{
			get { return laneOffset; }
		}

		// ホールドコンボ用ノートを生成する
		// info: このノートの譜面情報
		// liveBundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// laneOffset: 親ロングノートに対するレーン判定補正
		// category: ホールドコンボとして扱うノート種別
		public LongHoldCombo(MusicScoreInfo info, LiveBundleBuildData liveBundleBuildData, float speedRatio, float laneOffset, NoteCategory category = NoteCategory.FreeLaneLong)
			: base(info, 0, 0, 0, category, liveBundleBuildData, speedRatio)
		{
			this.laneOffset = laneOffset;
		}

		// ホールド状態を確認し、継続コンボの判定を進める
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示・判定に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
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

		// 入力がホールドコンボの判定範囲内か確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			return State == NoteState.Last &&
				parentNote != null &&
				parentNote.JudgedLaneStart - laneOffset <= lane &&
				parentNote.JudgedLaneEnd + laneOffset >= lane;
		}

		// ホールドコンボの判定結果を確定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Last)
			{
				JudgeInfo = (NoteResult.JustPerfect, NoteResultDescription.None);
				State = NoteState.Done;
			}
			return true;
		}

		// 親ロングノート終了時にホールドコンボを強制的に確定する
		public override void Terminate()
		{
			if (State == NoteState.Done)
			{
				return;
			}

			JudgeInfo = IsChildSuccessful()
				? (NoteResult.JustPerfect, NoteResultDescription.None)
				: (NoteResult.Miss, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// ホールドコンボは個別表示を生成しない
		protected override void OnSpawnNote()
		{
		}

		// ホールドコンボ終了時の判定、スコア、コンボ、ダメージ処理を呼ぶ
		protected override void OnUnSpawnNote()
		{
			onJudgment?.Invoke(this);
			onUpdateScore?.Invoke(this);
			onUpdateCombo?.Invoke(this);
			onDamage?.Invoke(this);
		}

		// オートプレイ用にホールドコンボを自動判定する
		// currentFrameInfo: 現在フレームの楽曲情報
		public override bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State == NoteState.Done || MusicScoreInfo.time > currentFrameInfo.time)
			{
				return false;
			}

			JudgeInfo = (NoteResult.Perfect, NoteResultDescription.None);
			State = NoteState.Done;
			return true;
		}

		// 親ロングノートのホールド入力を待つ必要があるか確認する
		private bool ShouldWaitForInput()
		{
			return parentNote == null || !parentNote.IsHoldJudgmentActive;
		}

		// 親ロングノート側でホールド成功状態が維持されているか確認する
		private bool IsChildSuccessful()
		{
			return parentNote != null &&
				(parentNote.IsHoldResultSuccessful || parentNote.IsHoldJudgmentActive);
		}
	}
}
