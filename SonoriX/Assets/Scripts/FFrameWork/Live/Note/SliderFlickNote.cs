using UnityEngine;

namespace FFrameWork.Live
{
	// 矢印位置の横方向変化で判定するスライダーフリックノート
	public class SliderFlickNote : FlickNote
	{
		// スライダーフリックノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: フリック距離やレーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// direction: 正解として扱うフリック方向
		public SliderFlickNote(
			MusicScoreInfo info,
			int id,
			int laneStart,
			int laneEnd,
			NoteCategory category,
			LiveBundleBuildData bundleBuildData,
			float speedRatio,
			NoteDirection direction)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio, direction)
		{
		}

		// 矢印入力がスライダーフリックの判定条件を満たすか確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			if (liveInput.source != LiveInputSource.Arrow || State == NoteState.Done)
			{
				return false;
			}
			if (JudgeLaneStart > lane || JudgeLaneEnd < lane)
			{
				return false;
			}
			return IsSliderFlickInputInJudgeWindow(ref liveInput);
		}

		// 矢印入力を使ってスライダーフリックを判定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			return liveInput.source == LiveInputSource.Arrow &&
				IsSliderFlickInputInJudgeWindow(ref liveInput) &&
				TryJudgeFlickInput(ref liveInput, true);
		}

		// スライダーフリック用の判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		// directionResult: フリック方向が正しいかどうか
		protected override (NoteResult, NoteResultDescription) CalculateFlickResult(float musicTime, bool directionResult)
		{
			return LiveConfig.CalculateSliderFlickNoteResult(this, musicTime, directionResult);
		}

		// 横方向の矢印移動量からフリック方向を更新する
		// touchDelta: 入力の移動量
		// isUpdatedDelta: フリック距離を超えた場合に true
		// isTrueDirection: 指定方向と一致する場合に true
		protected override void UpdateDelta(Vector2 touchDelta, ref bool isUpdatedDelta, ref bool isTrueDirection)
		{
			if (!TryGetArrowPositionDirectionDelta(touchDelta, out int positionDirectionDelta))
			{
				return;
			}

			isUpdatedDelta = true;
			isTrueDirection = IsCorrectDirection(positionDirectionDelta);
		}

		// 入力がスライダーフリックの判定時間内か確認する
		// liveInput: 判定に使用する入力情報
		private bool IsSliderFlickInputInJudgeWindow(ref LiveInput liveInput)
		{
			if (!TryGetArrowPositionDirectionDelta(liveInput.delta, out int positionDirectionDelta) ||
				!IsCorrectDirection(positionDirectionDelta))
			{
				return false;
			}

			return LiveConfig.IsPerfectOrMissJudgeTiming(
				GetJudgeFrameType(),
				MusicScoreInfo.time - liveInput.musicTime,
				true);
		}

		// このノートに対応する判定フレーム種別を取得する
		private JudgeFrameType GetJudgeFrameType()
		{
			return LiveUtility.GetINoteToJudgeFrameType(this);
		}

		// 矢印の横方向移動量を -1 または 1 の方向値として取得する
		// touchDelta: 入力の移動量
		// positionDirectionDelta: 取得した横方向の変化方向
		private bool TryGetArrowPositionDirectionDelta(Vector2 touchDelta, out int positionDirectionDelta)
		{
			positionDirectionDelta = 0;
			float deltaX = touchDelta.x;
			if (Mathf.Approximately(deltaX, 0f))
			{
				return false;
			}

			float scale = Time.deltaTime / LiveUtility.OneFrameTime;
			if (scale > 0f)
			{
				deltaX /= scale;
			}
			if (deltaX * deltaX <= flickDistance * flickDistance)
			{
				return false;
			}

			positionDirectionDelta = deltaX > 0f ? 1 : -1;
			return true;
		}

		// 横方向の変化がノートの指定方向と一致するか確認する
		// positionDirectionDelta: 横方向の変化方向
		private bool IsCorrectDirection(int positionDirectionDelta)
		{
			return Direction == NoteDirection.Default ||
				Direction == NoteDirection.Right && positionDirectionDelta > 0 ||
				Direction == NoteDirection.Left && positionDirectionDelta < 0;
		}
	}
}
