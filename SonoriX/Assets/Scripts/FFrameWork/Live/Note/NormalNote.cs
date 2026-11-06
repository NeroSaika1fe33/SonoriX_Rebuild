namespace FFrameWork.Live
{
	// 通常タップノートの判定を行うクラス
	public class NormalNote : NoteBase
	{
		// 通常ノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		public NormalNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
		}

		// 入力が通常ノートの判定条件を満たすか確認する
		// liveinput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveinput, float lane)
		{
			if (State == NoteState.Done || JudgeLaneStart > lane || JudgeLaneEnd < lane)
			{
				return false;
			}

			JudgeFrameType frameType = LiveUtility.GetINoteToJudgeFrameType(this);
			if (!LiveUtility.IsJudgmentTiming(frameType, MusicScoreInfo.time - liveinput.musicTime))
			{
				return false;
			}
			return liveinput.phase == LiveInputPhase.Began;
		}

		// 入力を使って通常ノートの判定結果を確定する
		// liveinput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveinput, float lane)
		{
			if (State == NoteState.Done)
			{
				return false;
			}
			if (liveinput.phase != LiveInputPhase.Began)
			{
				return false;
			}

			(NoteResult, NoteResultDescription) result = LiveConfig.CalculateDefaultNoteResult(this, liveinput.musicTime);
			JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
			State = NoteState.Done;
			return true;
		}

		// 指定された楽曲時間で通常ノートの判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return LiveConfig.CalculateDefaultNoteResult(this, musicTime).Item1;
		}
	}
}
