using System;
using UnityEngine;

namespace FFrameWork.Live
{
	// 矢印サイズの変化をホールド操作として扱うボリュームロングノート
	public class VolumeLongNote : LongNote
	{
		// 直前の有効なボリューム入力を継続入力として許可する猶予時間
		private const float ArrowVolumeHoldGraceSeconds = 0.5f;

		// 矢印サイズ変更演出が開始済みかどうか
		private bool isSizeChangeStarted;

		// 矢印サイズ変更演出が終了済みかどうか
		private bool isSizeChangeFinished;

		// 目標となる矢印サイズ段階
		private int arrowSizeStageTargetIndex;

		// 最後に有効なボリューム入力を受けた楽曲時間
		private float lastValidArrowVolumeInputTime = float.NegativeInfinity;

		// 最後に有効だった矢印サイズ変化量
		private float lastValidArrowVolumeDelta;

		// 猶予中にキャッシュ済みサイズ変化量を使っているかどうか
		private bool isUsingCachedArrowVolumeDelta;

		// サイズ変更開始時に目標段階を通知するコールバック
		public Action<int> OnBeginArrowSizeStageTarget { private get; set; }

		// サイズ変更中の進行度を通知するコールバック
		public Action<float> OnUpdateArrowSizeProgress { private get; set; }

		// サイズ変更終了時に成功可否を通知するコールバック
		public Action<bool> OnFinishArrowSizeChange { private get; set; }

		// ボリュームロングノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		// direction: 正解として扱うサイズ変化方向
		public VolumeLongNote(
			MusicScoreInfo info,
			int id,
			int laneStart,
			int laneEnd,
			LiveBundleBuildData bundleBuildData,
			float speedRatio,
			NoteLineType lineType,
			NoteDirection direction)
			: base(info, id, laneStart, laneEnd, NoteCategory.VolumeFlickLong, bundleBuildData, speedRatio, lineType)
		{
			Direction = direction;
		}

		// ボリュームロングの進行、子ノート、サイズ変更演出を更新する
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

			if (IsHoldJudgmentActive)
			{
				UpdateArrowSizeProgress(currentFrameInfo.time);
			}
			if (childNote != null && childNote.State == NoteState.Done)
			{
				FinishArrowSizeChange(true);
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
				FinishArrowSizeChange(false);
				State = NoteState.Done;
				return;
			}
			if (!IsWithinArrowVolumeHoldGrace(currentFrameInfo.time) && IsHoldInputTimedOut())
			{
				DeactivateHold();
			}
		}

		// 矢印サイズ変化を使ってホールド状態を更新し、判定を進める
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		// sizeDelta: 今回の矢印サイズ変化量
		public bool TryUpdateByArrowVolume(ArrowJudgmentFrame frame, float musicTime, float sizeDelta)
		{
			float effectiveSizeDelta;
			return TryUpdateByArrowVolume(frame, musicTime, sizeDelta, out effectiveSizeDelta);
		}

		// 矢印サイズ変化を使ってホールド状態を更新し、有効な変化量も返す
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		// sizeDelta: 今回の矢印サイズ変化量
		// effectiveSizeDelta: 判定に実際に使用したサイズ変化量
		public bool TryUpdateByArrowVolume(ArrowJudgmentFrame frame, float musicTime, float sizeDelta, out float effectiveSizeDelta)
		{
			if (!RefreshArrowVolumeHoldState(frame, musicTime, sizeDelta, out effectiveSizeDelta))
			{
				return false;
			}

			float lane = (LaneStartF + LaneEndF) * 0.5f;
			LiveInput input = new LiveInput
			{
				inputId = -3000,
				phase = LiveInputPhase.Moved,
				musicTime = musicTime,
				delta = new Vector2(0f, effectiveSizeDelta),
				lane = lane,
				source = LiveInputSource.ArrowVolume
			};

			isUsingCachedArrowVolumeDelta = !Mathf.Approximately(sizeDelta, effectiveSizeDelta);
			try
			{
				Judgment(ref input, lane);
			}
			finally
			{
				isUsingCachedArrowVolumeDelta = false;
			}
			return State == NoteState.Done;
		}

		// 矢印サイズホールドの有効状態だけを更新する
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		// sizeDelta: 今回の矢印サイズ変化量
		public bool RefreshArrowVolumeHoldState(ArrowJudgmentFrame frame, float musicTime, float sizeDelta)
		{
			float effectiveSizeDelta;
			return RefreshArrowVolumeHoldState(frame, musicTime, sizeDelta, out effectiveSizeDelta);
		}

		// 矢印サイズホールドの有効状態を更新し、有効な変化量も返す
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		// musicTime: 現在の楽曲時間
		// sizeDelta: 今回の矢印サイズ変化量
		// effectiveSizeDelta: 判定に実際に使用したサイズ変化量
		public bool RefreshArrowVolumeHoldState(ArrowJudgmentFrame frame, float musicTime, float sizeDelta, out float effectiveSizeDelta)
		{
			effectiveSizeDelta = 0f;
			if (State == NoteState.Wait || State == NoteState.Done)
			{
				return false;
			}
			if (!CanAcceptArrowVolumeHold(musicTime) || !IsArrowFrameOverlappingCurrentLane(frame))
			{
				DeactivateHold();
				return false;
			}

			if (!TryGetArrowSizeStageDelta(sizeDelta, out _))
			{
				if (IsHoldJudgmentActive &&
					IsWithinArrowVolumeHoldGrace(musicTime) &&
					!Mathf.Approximately(lastValidArrowVolumeDelta, 0f))
				{
					effectiveSizeDelta = lastValidArrowVolumeDelta;
					ActivateHold();
					return true;
				}

				DeactivateHold();
				return false;
			}

			lastValidArrowVolumeInputTime = musicTime;
			lastValidArrowVolumeDelta = sizeDelta;
			effectiveSizeDelta = sizeDelta;
			ActivateHold();
			return true;
		}

		// 矢印サイズ入力を使ってボリュームロングを判定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Wait || State == NoteState.Done)
			{
				return false;
			}
			if (liveInput.source != LiveInputSource.ArrowVolume ||
				!TryGetArrowSizeStageDelta(liveInput.delta.y, out int sizeStageDelta) ||
				!IsVolumeFlickLongJudgeTiming(liveInput.musicTime))
			{
				return false;
			}

			arrowSizeStageTargetIndex = GetArrowSizeStageTargetIndex();
			if (!isUsingCachedArrowVolumeDelta)
			{
				lastValidArrowVolumeInputTime = liveInput.musicTime;
				lastValidArrowVolumeDelta = liveInput.delta.y;
			}
			if (Result == NoteResult.None)
			{
				(NoteResult, NoteResultDescription) result = LiveConfig.CalculateVolumeFlickLongNoteResult(this, liveInput.musicTime, true);
				if (result.Item1 == NoteResult.None)
				{
					if (liveInput.musicTime < MusicScoreInfo.time)
					{
						return false;
					}

					result = (NoteResult.Miss, NoteResultDescription.None);
				}

				JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
				BeginArrowSizeChange();
			}

			ActivateHold();
			State = IsHoldInputState(State) ? NoteState.Input : NoteState.InputBegan;
			lastInputFrame = Time.frameCount;
			OffsetJudgeTime = 0f;
			SubNoteJudgment(ref liveInput, lane);
			UpdateArrowSizeProgress(liveInput.musicTime);
			return true;
		}

		// 入力がボリュームロングの判定対象か確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			return State != NoteState.Wait &&
				State != NoteState.Done &&
				liveInput.source == LiveInputSource.ArrowVolume &&
				TryGetArrowSizeStageDelta(liveInput.delta.y, out _);
		}

		// 指定された楽曲時間でボリュームロングの判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return LiveConfig.CalculateVolumeFlickLongNoteResult(this, musicTime, true).Item1;
		}

		// 親または終端から強制終了されたときにサイズ変更演出も終了する
		public override void ForceTerminate()
		{
			FinishArrowSizeChange(IsChildSuccessful());
			base.ForceTerminate();
		}

		// 矢印サイズ変更演出を開始する
		private void BeginArrowSizeChange()
		{
			if (isSizeChangeStarted)
			{
				return;
			}

			isSizeChangeStarted = true;
			OnBeginArrowSizeStageTarget?.Invoke(arrowSizeStageTargetIndex);
		}

		// 子ノートまでの進行度として矢印サイズ変更の進行度を通知する
		// musicTime: 現在の楽曲時間
		private void UpdateArrowSizeProgress(float musicTime)
		{
			if (!isSizeChangeStarted || isSizeChangeFinished || childNote == null)
			{
				return;
			}

			float progress = LiveConfig.GetSliderLongLineParentProgress(musicTime, this, childNote);
			OnUpdateArrowSizeProgress?.Invoke(progress);
		}

		// 矢印サイズ変更演出を終了する
		// success: サイズ変更を成功扱いにするかどうか
		private void FinishArrowSizeChange(bool success)
		{
			if (!isSizeChangeStarted || isSizeChangeFinished)
			{
				return;
			}

			isSizeChangeFinished = true;
			OnFinishArrowSizeChange?.Invoke(success);
		}

		// 矢印サイズ変化量を -1 または 1 の方向値として取得する
		// sizeDelta: 矢印サイズの変化量
		// sizeStageDelta: 取得したサイズ段階の変化方向
		private bool TryGetArrowSizeStageDelta(float sizeDelta, out int sizeStageDelta)
		{
			sizeStageDelta = 0;
			if (Mathf.Approximately(sizeDelta, 0f))
			{
				return false;
			}

			sizeStageDelta = sizeDelta > 0f ? 1 : -1;
			return IsCorrectDirection(sizeStageDelta);
		}

		// サイズ段階の変化がノートの指定方向と一致するか確認する
		// sizeStageDelta: サイズ段階の変化方向
		private bool IsCorrectDirection(int sizeStageDelta)
		{
			return Direction == NoteDirection.Default ||
				Direction == NoteDirection.Right && sizeStageDelta > 0 ||
				Direction == NoteDirection.Left && sizeStageDelta < 0;
		}

		// ボリュームロングの開始判定時間内か確認する
		// musicTime: 判定に使用する楽曲時間
		private bool IsVolumeFlickLongJudgeTiming(float musicTime)
		{
			return Result != NoteResult.None ||
				LiveConfig.IsPerfectOrMissJudgeTiming(
					GetJudgeFrameType(),
					MusicScoreInfo.time - musicTime,
					true);
		}

		// 指定時刻の矢印サイズホールドを受け付けられるか確認する
		// musicTime: 判定に使用する楽曲時間
		private bool CanAcceptArrowVolumeHold(float musicTime)
		{
			return IsVolumeFlickLongJudgeTiming(musicTime) ||
				musicTime >= MusicScoreInfo.time;
		}

		// このノートに対応する判定フレーム種別を取得する
		private JudgeFrameType GetJudgeFrameType()
		{
			return LiveUtility.GetINoteToJudgeFrameType(this);
		}

		// 終端ノートまたは自身の幅から目標の矢印サイズ段階を取得する
		private int GetArrowSizeStageTargetIndex()
		{
			NoteBase targetNote = childNote != null ? childNote : this;
			float laneWidth = Mathf.Abs(targetNote.LaneEndF - targetNote.LaneStartF) + 1f;
			return ArrowLogic.GetStageIndexForLaneWidth(laneWidth);
		}

		// 最後の有効入力からホールド猶予時間内か確認する
		// musicTime: 現在の楽曲時間
		private bool IsWithinArrowVolumeHoldGrace(float musicTime)
		{
			return musicTime - lastValidArrowVolumeInputTime <= ArrowVolumeHoldGraceSeconds;
		}

	}
}
