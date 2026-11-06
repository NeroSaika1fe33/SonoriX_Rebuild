namespace FFrameWork.Live
{
	// 入力判定を持たず、ガイド表示だけを行うロングノート
	public class GuideNote : LongNote
	{
		// ガイドノートは入力判定対象外
		public override bool HasJudgment
		{
			get { return false; }
		}

		// ガイドノート用の状態管理
		public override NoteState State
		{
			get { return state; }
			protected set
			{
				state = value;
				if (value == NoteState.Done)
				{
					OnUnSpawnNote();
				}
				else if (value == NoteState.Playing)
				{
					OnSpawnNote();
				}
			}
		}

		// ガイドノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ガイドラインの描画タイプ
		public GuideNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio, lineType)
		{
		}

		// ガイドノートの進行と子ノートを更新する
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
			OffsetJudgeTime = currentFrameInfo.time - MusicScoreInfo.time;
			Progress = CalcProgress(currentFrameInfo, offsetTime);
			// ガイドは入力判定を待たず、始点時刻から終端まで表示を継続する。
			if (currentFrameInfo.time >= MusicScoreInfo.time && State != NoteState.Input)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Input;
			}
			ExecuteSubNotes(currentFrameInfo);
			UpdateExecutingLane(currentFrameInfo);
			if (childNote != null && childNote.State == NoteState.Done)
			{
				JudgeInfo = (NoteResult.Pass, NoteResultDescription.None);
				State = NoteState.Done;
				return;
			}
		}

		// ガイドノートの表示開始コールバックを呼ぶ
		protected override void OnSpawnNote()
		{
			onSpawn?.Invoke(this);
		}

		// 子ノートを終了し、ガイドノートの表示解除コールバックを呼ぶ
		protected override void OnUnSpawnNote()
		{
			SubNoteTerminate();
			onUnspawn?.Invoke(this);
		}

		// ガイドノートはオート判定しない
		// currentFrameInfo: 現在フレームの楽曲情報
		public override bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			return false;
		}
	}
}
