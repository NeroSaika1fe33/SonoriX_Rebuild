namespace FFrameWork.Live
{
	// 矢印位置の追従でホールドするスライダーロングノート
	public class SliderLongNote : LongNote
	{
		// スライダーロングノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		public SliderLongNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, NoteCategory.SliderLong, bundleBuildData, speedRatio, lineType)
		{
		}

		// スライダーロングノートの進行、子ノート、タイムアウトを更新する
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示・判定に使用する時間オフセット
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
			ExecuteSubNotes(currentFrameInfo);
			UpdateExecutingLane(currentFrameInfo);

			if (childNote != null && childNote.State == NoteState.Done)
			{
				State = NoteState.Done;
				return;
			}
			if (childNote != null && currentFrameInfo.time > childNote.MusicScoreInfo.time + LiveConfig.LiveMasterData.JudgeTimeAfter)
			{
				if (Result == NoteResult.None)
				{
					JudgeInfo = (NoteResult.Miss, NoteResultDescription.None);
					onDamage?.Invoke(this);
					onUpdateCombo?.Invoke(this);
					onJudgment?.Invoke(this);
				}
				State = NoteState.Done;
				return;
			}
			if (IsHoldInputActive && lastInputFrame + LiveConfig.LiveMasterData.MissMesh < UnityEngine.Time.frameCount)
			{
				State = NoteState.InputRelease;
			}
		}

		// 矢印フレームの位置を使ってホールド状態を更新し、判定を進める
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		public bool TryUpdateByArrow(ArrowJudgmentFrame frame, float musicTime)
		{
			if (!RefreshArrowHoldState(frame, musicTime))
			{
				return false;
			}

			float lane = (LaneStartF + LaneEndF) * 0.5f;
			LiveInput input = new LiveInput
			{
				inputId = -2000,
				musicTime = musicTime,
				lane = lane,
				source = LiveInputSource.Arrow
			};

			Judgment(ref input, lane);
			return State == NoteState.Done;
		}

		// 矢印フレームがノートと重なっているかを確認し、ホールド状態だけを更新する
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		public bool RefreshArrowHoldState(ArrowJudgmentFrame frame, float musicTime)
		{
			if (State == NoteState.Wait || State == NoteState.Done)
			{
				return false;
			}
			if (musicTime < MusicScoreInfo.time || !IsOverlappingCurrentLane(frame))
			{
				DeactivateHold();
				return false;
			}

			ActivateHold();
			return true;
		}

		// スライダーロング判定用の仮想入力を作成する
		// musicTime: 入力として扱う楽曲時間
		private LiveInput CreateArrowHoldInput(float musicTime)
		{
			float lane = (LaneStartF + LaneEndF) * 0.5f;
			return new LiveInput
			{
				inputId = -2000,
				musicTime = musicTime,
				lane = lane,
				source = LiveInputSource.Arrow
			};
		}

		// 仮想入力または矢印入力を使ってスライダーロングを判定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Wait || State == NoteState.Done)
			{
				return false;
			}

			if (Progress < 1f)
			{
				return false;
			}

			if (Result == NoteResult.None)
			{
				(NoteResult, NoteResultDescription) result = (NoteResult.JustPerfect, NoteResultDescription.None);
				JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
			}

			ActivateHold();
			State = State == NoteState.InputBegan || State == NoteState.Input ? NoteState.Input : NoteState.InputBegan;
			lastInputFrame = UnityEngine.Time.frameCount;
			OffsetJudgeTime = 0f;
			SubNoteJudgment(ref liveInput, lane);
			return true;
		}

		// スライダーロングが入力判定可能な状態か確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			return State != NoteState.Wait && State != NoteState.Done;
		}

		// スライダーロングの開始判定結果を返す
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return NoteResult.JustPerfect;
		}

		// 矢印フレームが現在のノートレーンと重なっているか確認する
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		private bool IsOverlappingCurrentLane(ArrowJudgmentFrame frame)
		{
			float noteCenterX = GetLaneWorldX((LaneStartF + LaneEndF) * 0.5f);
			float noteHalfWidth = GetNoteHalfWidth();
			float noteLeft = noteCenterX - noteHalfWidth;
			float noteRight = noteCenterX + noteHalfWidth;
			float frameLeft = frame.Position.CurrentX - frame.Size.CurrentSize;
			float frameRight = frame.Position.CurrentX + frame.Size.CurrentSize;
			return frameLeft <= noteRight && frameRight >= noteLeft;
		}

		// 現在のノート幅をワールド座標上の半幅として取得する
		private float GetNoteHalfWidth()
		{
			float laneSpan = UnityEngine.Mathf.Abs(LaneEndF - LaneStartF);
			return (laneSpan + 1f) * LiveConfig.widthX;
		}

		// レーン番号を判定位置のワールド X 座標へ変換する
		// lane: 変換対象のレーン位置
		private static float GetLaneWorldX(float lane)
		{
			UnityEngine.Vector2[] positions = LiveConfig.JudgmentPositions;
			if (positions == null || positions.Length == 0)
			{
				return 0f;
			}

			float clampedLane = UnityEngine.Mathf.Clamp(lane, 0f, positions.Length - 1);
			int start = UnityEngine.Mathf.FloorToInt(clampedLane);
			int end = UnityEngine.Mathf.CeilToInt(clampedLane);
			float t = UnityEngine.Mathf.Clamp01(clampedLane - start);
			return UnityEngine.Mathf.Lerp(positions[start].x, positions[end].x, t);
		}
	}
}
