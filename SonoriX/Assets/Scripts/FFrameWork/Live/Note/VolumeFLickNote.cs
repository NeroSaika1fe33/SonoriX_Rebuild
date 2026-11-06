using System;
using UnityEngine;

namespace FFrameWork.Live
{
	// 矢印サイズの上下変化で判定するボリュームフリックノート
	public class VolumeFLickNote : FlickNote
	{
		// 成功した入力で検出した矢印サイズ段階の変化方向
		private int arrowSizeStageDelta;

		// 成功時に目標となる矢印サイズ段階を通知するコールバック
		public Action<int> OnArrowSizeStageTarget { private get; set; }

		// ボリュームフリックノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: フリック距離やレーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// direction: 正解として扱うサイズ変化方向
		public VolumeFLickNote(
			MusicScoreInfo info,
			int id,
			int laneStart,
			int laneEnd,
			LiveBundleBuildData bundleBuildData,
			float speedRatio,
			NoteDirection direction)
			: base(info, id, laneStart, laneEnd, NoteCategory.VolumeFlick, bundleBuildData, speedRatio, direction)
		{
		}

		// 矢印サイズ入力がボリュームフリックの判定条件を満たすか確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			if (liveInput.source != LiveInputSource.ArrowVolume || State == NoteState.Done)
			{
				return false;
			}
			if (JudgeLaneStart > lane || JudgeLaneEnd < lane)
			{
				return false;
			}
			return IsVolumeFlickInputInJudgeWindow(ref liveInput);
		}

		// 矢印サイズ入力を使ってボリュームフリックを判定する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			return liveInput.source == LiveInputSource.ArrowVolume &&
				IsVolumeFlickInputInJudgeWindow(ref liveInput) &&
				TryJudgeFlickInput(ref liveInput, true);
		}

		// ボリュームフリック用の判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		// directionResult: サイズ変化方向が正しいかどうか
		protected override (NoteResult, NoteResultDescription) CalculateFlickResult(float musicTime, bool directionResult)
		{
			return LiveConfig.CalculateVolumeFlickNoteResult(this, musicTime, directionResult);
		}

		// フリック成功時に目標サイズ段階を通知する
		protected override void OnSuccessfulFlick()
		{
			if (arrowSizeStageDelta != 0)
			{
				OnArrowSizeStageTarget?.Invoke(GetArrowSizeStageTargetIndex());
			}
		}

		// 縦方向の矢印サイズ変化量からフリック方向を更新する
		// touchDelta: 入力の移動量
		// isUpdatedDelta: フリック距離を超えた場合に true
		// isTrueDirection: 指定方向と一致する場合に true
		protected override void UpdateDelta(Vector2 touchDelta, ref bool isUpdatedDelta, ref bool isTrueDirection)
		{
			arrowSizeStageDelta = 0;
			if (!TryGetArrowSizeStageDelta(touchDelta, out arrowSizeStageDelta))
			{
				return;
			}

			isUpdatedDelta = true;
			isTrueDirection = IsCorrectDirection(arrowSizeStageDelta);
		}

		// 入力がボリュームフリックの判定時間内か確認する
		// liveInput: 判定に使用する入力情報
		private bool IsVolumeFlickInputInJudgeWindow(ref LiveInput liveInput)
		{
			if (!TryGetArrowSizeStageDelta(liveInput.delta, out int sizeStageDelta) ||
				!IsCorrectDirection(sizeStageDelta))
			{
				return false;
			}

			return LiveConfig.IsPerfectOrMissJudgeTiming(
				GetJudgeFrameType(),
				MusicScoreInfo.time - liveInput.musicTime,
				true);
		}

		// ボリュームフリック用の判定フレーム種別を取得する
		private JudgeFrameType GetJudgeFrameType()
		{
			return JudgeFrameType.Volume_Flick;
		}

		// 矢印サイズの縦方向変化量を -1 または 1 の方向値として取得する
		// touchDelta: 入力の移動量
		// sizeStageDelta: 取得したサイズ段階の変化方向
		private bool TryGetArrowSizeStageDelta(Vector2 touchDelta, out int sizeStageDelta)
		{
			sizeStageDelta = 0;
			float deltaY = touchDelta.y;
			if (Mathf.Approximately(deltaY, 0f))
			{
				return false;
			}

			float scale = Time.deltaTime / LiveUtility.OneFrameTime;
			if (scale > 0f)
			{
				deltaY /= scale;
			}
			if (deltaY * deltaY <= flickDistance * flickDistance)
			{
				return false;
			}

			sizeStageDelta = deltaY > 0f ? 1 : -1;
			return true;
		}

		// サイズ段階の変化がノートの指定方向と一致するか確認する
		// sizeStageDelta: サイズ段階の変化方向
		private bool IsCorrectDirection(int sizeStageDelta)
		{
			return Direction == NoteDirection.Default ||
				Direction == NoteDirection.Right && sizeStageDelta > 0 ||
				Direction == NoteDirection.Left && sizeStageDelta < 0;
		}

		// 現在のノート幅から目標となる矢印サイズ段階を取得する
		private int GetArrowSizeStageTargetIndex()
		{
			float laneWidth = Mathf.Abs(LaneEndF - LaneStartF) + 1f;
			return ArrowLogic.GetStageIndexForLaneWidth(laneWidth);
		}
	}
}
