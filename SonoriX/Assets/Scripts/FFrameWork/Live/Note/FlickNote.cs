using UnityEngine;

namespace FFrameWork.Live
{
	// フリック入力の方向とタイミングを判定する基底ノート
	public class FlickNote : NoteBase
	{
		// 最後に入力を受け取ったフレーム番号
		protected int lastInputFrame;

		// 入力 ID ごとのフリック更新有無と方向判定結果
		private readonly (int, bool, bool)[] inputResults;

		// フリックとして認める最小移動距離
		protected readonly float flickDistance;

		// フリックノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: フリック距離やレーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// direction: 正解として扱うフリック方向
		public FlickNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio, NoteDirection direction)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
			inputResults = new (int, bool, bool)[8];
			flickDistance = LiveUtility.ScreenDpiToInch(bundleBuildData != null ? bundleBuildData.FlickDistance : LiveBundleBuildData.DefaultFlickDistance);
			Direction = direction;
		}

		// Done になったとき、親ロングノートも強制終了する状態
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

		// フリック入力中にフレームが進んだ場合、保持している入力結果で判定を確定する
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示・判定に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			base.Excute(currentFrameInfo, offsetTime);
			if (State == NoteState.Input && lastInputFrame + 1 < UnityEngine.Time.frameCount && CalcProgress(currentFrameInfo, offsetTime) >= 1f)
			{
				JudgmentFlick(currentFrameInfo.time);
			}
		}

		// 保存済みの入力差分からフリック判定を確定する
		// musicTime: 判定に使用する楽曲時間
		protected bool JudgmentFlick(float musicTime)
		{
			bool hasInput = false;
			bool hasDirection = false;
			bool allDirections = true;
			for (int i = 0; i < inputResults.Length; i++)
			{
				(int id, bool updated, bool trueDirection) = inputResults[i];
				if (id == 0 || !updated)
				{
					continue;
				}
				hasInput = true;
				hasDirection |= trueDirection;
				allDirections &= trueDirection;
			}
			if (!hasInput)
			{
				return false;
			}

			bool directionResult = allDirections || hasDirection;
			(NoteResult, NoteResultDescription) result = CalculateFlickResult(musicTime, directionResult);
			if (result.Item1 == NoteResult.None)
			{
				return false;
			}

			JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
			if (directionResult && IsSuccessfulFlickResult(JudgeInfo))
			{
				OnSuccessfulFlick();
			}
			State = NoteState.Done;
			return true;
		}

		// 入力がフリック判定の対象になるか確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Done)
			{
				return false;
			}
			if (IsLaneCheck(liveInput))
			{
				if (JudgeLaneStart > lane || JudgeLaneEnd < lane)
				{
					return false;
				}
				if (!LiveUtility.IsJudgmentTiming(LiveUtility.GetINoteToJudgeFrameType(this), MusicScoreInfo.time - liveInput.musicTime))
				{
					return false;
				}
			}
			return parentNote != null ||
				State != NoteState.Playing ||
				liveInput.phase == LiveInputPhase.Began ||
				liveInput.source == LiveInputSource.Arrow;
		}

		// 入力差分を使ってフリック判定を進める
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			return TryJudgeFlickInput(ref liveInput, liveInput.source == LiveInputSource.Arrow);
		}

		// public override bool IsJudgment(ref LiveTouch touch, float lane)
		// {
		// 	if (State == NoteState.Done)
		// 	{
		// 		return false;
		// 	}
		// 	if (IsLaneCheck(touch))
		// 	{
		// 		if (JudgeLaneStart > lane || JudgeLaneEnd < lane)
		// 		{
		// 			return false;
		// 		}
		// 		if (!LiveUtility.IsJudgmentTiming(LiveUtility.GetINoteToJudgeFrameType(this), MusicScoreInfo.time - touch.musicTime))
		// 		{
		// 			return false;
		// 		}
		// 	}
		// 	return parentNote != null || State != NoteState.Playing || touch.phase == UnityEngine.InputSystem.TouchPhase.Began;
		// }

		// public override bool Judgment(ref LiveTouch touch, float lane)
		// {
		// 	bool isUpdatedDelta = false;
		// 	bool isTrueDirection = false;
		// 	if (State == NoteState.Done)
		// 	{
		// 		return false;
		// 	}
		// 	if (State == NoteState.Playing)
		// 	{
		// 		State = NoteState.Input;
		// 	}
		// 	if (State != NoteState.Input)
		// 	{
		// 		return false;
		// 	}
		//
		// 	if (parentNote == null)
		// 	{
		// 		if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
		// 		{
		// 			AddInputResult(ref touch, ref isUpdatedDelta, ref isTrueDirection);
		// 		}
		// 		else if (HasInputResult(touch.touchId))
		// 		{
		// 			UpdateDelta(touch.delta, ref isUpdatedDelta, ref isTrueDirection);
		// 			if (isUpdatedDelta)
		// 			{
		// 				AddInputResult(ref touch, ref isUpdatedDelta, ref isTrueDirection);
		// 			}
		// 		}
		// 		return isUpdatedDelta ? JudgmentFlick(touch.musicTime) : false;
		// 	}
		//
		// 	UpdateDelta(touch.delta, ref isUpdatedDelta, ref isTrueDirection);
		// 	if (isUpdatedDelta)
		// 	{
		// 		AddInputResult(ref touch, ref isUpdatedDelta, ref isTrueDirection);
		// 	}
		// 	lastInputFrame = Time.frameCount;
		// 	if (MusicScoreInfo.time - touch.musicTime <= -LiveUtility.HalfFrameTime && isTrueDirection)
		// 	{
		// 		return JudgmentFlick(touch.musicTime);
		// 	}
		// 	return false;
		// }

		// 指定された楽曲時間でロング始点相当の判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return LiveConfig.CalculateLongStartNoteResult(this, musicTime).Item1;
		}

		// private bool IsLaneCheck(LiveTouch touch)
		// {
		// 	if (State != NoteState.Input)
		// 	{
		// 		return true;
		// 	}
		// 	for (int i = 0; i < inputResults.Length; i++)
		// 	{
		// 		if (inputResults[i].Item1 != 0 && inputResults[i].Item1 == touch.touchId)
		// 		{
		// 			return false;
		// 		}
		// 	}
		// 	return true;
		// }

		// private void AddInputResult(ref LiveTouch touch, ref bool isUpdatedDelta, ref bool isTrueDirection)
		// {
		// 	for (int i = 0; i < inputResults.Length; i++)
		// 	{
		// 		if (inputResults[i].Item1 == 0 || inputResults[i].Item1 == touch.touchId)
		// 		{
		// 			inputResults[i] = (touch.touchId, isUpdatedDelta, isTrueDirection);
		// 			return;
		// 		}
		// 	}
		// }

		// 入力差分を記録し、必要ならフリック判定を確定する
		// liveInput: 判定に使用する入力情報
		// allowStartWithoutBegan: Began 以外の入力から判定開始を許可するかどうか
		protected bool TryJudgeFlickInput(ref LiveInput liveInput, bool allowStartWithoutBegan)
		{
			bool isUpdatedDelta = false;
			bool isTrueDirection = false;
			if (State == NoteState.Done)
			{
				return false;
			}
			if (State == NoteState.Playing)
			{
				if (!allowStartWithoutBegan && liveInput.phase != LiveInputPhase.Began)
				{
					return false;
				}
				State = NoteState.Input;
			}
			if (State != NoteState.Input)
			{
				return false;
			}

			UpdateDelta(liveInput.delta, ref isUpdatedDelta, ref isTrueDirection);
			if (isUpdatedDelta)
			{
				AddInputResult(liveInput.inputId, isUpdatedDelta, isTrueDirection);
			}
			lastInputFrame = Time.frameCount;
			return isUpdatedDelta ? JudgmentFlick(liveInput.musicTime) : false;
		}

		// フリック専用の判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		// directionResult: フリック方向が正しいかどうか
		protected virtual (NoteResult, NoteResultDescription) CalculateFlickResult(float musicTime, bool directionResult)
		{
			return parentNote != null
				? LiveConfig.CalculateLongEndFlickNoteResult(this, musicTime, directionResult)
				: LiveConfig.CalculateFlickNoteResult(this, musicTime, directionResult);
		}

		// フリック成功時の追加処理派生クラスで必要に応じて上書きする
		protected virtual void OnSuccessfulFlick()
		{
		}

		// フリック成功扱いになる判定結果か確認する
		// result: 判定結果と詳細情報
		protected static bool IsSuccessfulFlickResult((NoteResult, NoteResultDescription) result)
		{
			return result.Item1 >= NoteResult.Good;
		}

		// 入力差分からフリック距離と方向の成否を更新する
		// touchDelta: 入力の移動量
		// isUpdatedDelta: フリック距離を超えた場合に true
		// isTrueDirection: 指定方向と一致する場合に true
		protected virtual void UpdateDelta(Vector2 touchDelta, ref bool isUpdatedDelta, ref bool isTrueDirection)
		{
			if (touchDelta == LiveUtility.Vector2Zero)
			{
				return;
			}

			float scale = Time.deltaTime / LiveUtility.OneFrameTime;
			if (scale > 0f)
			{
				touchDelta /= scale;
			}
			if (touchDelta.sqrMagnitude <= flickDistance * flickDistance)
			{
				isUpdatedDelta = false;
				isTrueDirection = false;
				return;
			}

			isUpdatedDelta = true;
			Vector2 expected = Vector2.up;
			if (Direction == NoteDirection.Left)
			{
				expected = LiveUtility.Vector2Left;
			}
			else if (Direction == NoteDirection.Right)
			{
				expected = LiveUtility.Vector2Right;
			}
			isTrueDirection = Direction == NoteDirection.Default || Vector2.Dot(expected.normalized, touchDelta.normalized) >= 0f;
		}

		// 入力 ID ごとのフリック判定途中結果を保存する
		// inputId: 入力を識別する ID
		// isUpdatedDelta: フリック距離を超えたかどうか
		// isTrueDirection: 指定方向と一致したかどうか
		private void AddInputResult(int inputId, bool isUpdatedDelta, bool isTrueDirection)
		{
			for (int i = 0; i < inputResults.Length; i++)
			{
				if (inputResults[i].Item1 == 0 || inputResults[i].Item1 == inputId)
				{
					inputResults[i] = (inputId, isUpdatedDelta, isTrueDirection);
					return;
				}
			}
		}

		// 現在の入力に対してレーン範囲チェックが必要か確認する
		// liveInput: 判定に使用する入力情報
		private bool IsLaneCheck(LiveInput liveInput)
		{
			if (State != NoteState.Input)
			{
				return true;
			}
			return !HasInputResult(liveInput.inputId);
		}

		// 指定した入力 ID の判定途中結果を保持しているか確認する
		// inputId: 確認対象の入力 ID
		private bool HasInputResult(int inputId)
		{
			for (int i = 0; i < inputResults.Length; i++)
			{
				if (inputResults[i].Item1 != 0 && inputResults[i].Item1 == inputId)
				{
					return true;
				}
			}
			return false;
		}
	}
}
