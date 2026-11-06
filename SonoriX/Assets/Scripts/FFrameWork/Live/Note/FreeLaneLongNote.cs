using UnityEngine;

namespace FFrameWork.Live
{
	// フリーレーン入力でホールドするロングノート
	public class FreeLaneLongNote : LongNote
	{
		// フリーレーンロングノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		public FreeLaneLongNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, NoteCategory.FreeLaneLong, bundleBuildData, speedRatio, lineType)
		{
		}

		// フリーレーン入力の状態を更新し、必要ならホールド判定を実行する
		// frame: フリーレーン入力を表す判定フレーム
		// musicTime: 現在の楽曲時間
		// isPressed: フリーレーンが押下中かどうか
		public bool TryUpdateFreeLaneHold(ArrowJudgmentFrame frame, float musicTime, bool isPressed)
		{
			if (!RefreshFreeLaneHoldState(frame, musicTime, isPressed))
			{
				return false;
			}

			float lane = (LaneStartF + LaneEndF) * 0.5f;
			LiveInput input = CreateFreeLaneHoldInput(musicTime, lane);
			return Judgment(ref input, lane);
		}

		// フリーレーンホールドの有効状態だけを更新する
		// frame: フリーレーン入力を表す判定フレーム
		// musicTime: 現在の楽曲時間
		// isPressed: フリーレーンが押下中かどうか
		public bool RefreshFreeLaneHoldState(ArrowJudgmentFrame frame, float musicTime, bool isPressed)
		{
			string reason;
			if (!CanActivateFreeLaneHold(frame, musicTime, isPressed, out reason))
			{
				DeactivateHold();
				return false;
			}

			ActivateHold();
			return true;
		}

		// フリーレーンホールドを有効化できる状態か確認する
		// frame: フリーレーン入力を表す判定フレーム
		// musicTime: 現在の楽曲時間
		// isPressed: フリーレーンが押下中かどうか
		// reason: 有効化できない場合の理由
		public bool CanActivateFreeLaneHold(ArrowJudgmentFrame frame, float musicTime, bool isPressed, out string reason)
		{
			if (State == NoteState.Wait)
			{
				reason = "note state is Wait";
				return false;
			}
			if (State == NoteState.Done)
			{
				reason = "note state is Done";
				return false;
			}
			if (musicTime < MusicScoreInfo.time)
			{
				reason = "music time is before note start";
				return false;
			}
			if (!isPressed)
			{
				reason = "FreeLaneInput is not pressed";
				return false;
			}
			if (!IsOverlappingCurrentLane(frame))
			{
				reason = "arrow frame does not overlap note lane";
				return false;
			}

			reason = null;
			return true;
		}

		// フリーレーンホールド用の仮想入力を作成する
		// musicTime: 入力として扱う楽曲時間
		// lane: 入力として扱うレーン位置
		private LiveInput CreateFreeLaneHoldInput(float musicTime, float lane)
		{
			return new LiveInput
			{
				inputId = -1000,
				phase = LiveInputPhase.Moved,
				musicTime = musicTime,
				lane = lane,
				laneStart = JudgedLaneStart,
				laneEnd = JudgedLaneEnd,
				width = Mathf.Max(1f, JudgedLaneEnd - JudgedLaneStart + 1f),
				source = LiveInputSource.FreeLane
			};
		}

		// 入力フレームが現在のノートレーンと重なっているか確認する
		// frame: フリーレーン入力を表す判定フレーム
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
			float laneSpan = Mathf.Abs(LaneEndF - LaneStartF);
			return (laneSpan + 1f) * LiveConfig.widthX;
		}

		// レーン番号を判定位置のワールド X 座標へ変換する
		// lane: 変換対象のレーン位置
		private static float GetLaneWorldX(float lane)
		{
			Vector2[] positions = LiveConfig.JudgmentPositions;
			if (positions == null || positions.Length == 0)
			{
				return 0f;
			}

			float clampedLane = Mathf.Clamp(lane, 0f, positions.Length - 1);
			int start = Mathf.FloorToInt(clampedLane);
			int end = Mathf.CeilToInt(clampedLane);
			float t = Mathf.Clamp01(clampedLane - start);
			return Mathf.Lerp(positions[start].x, positions[end].x, t);
		}
	}
}
