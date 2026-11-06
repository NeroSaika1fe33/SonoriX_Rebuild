using System.Collections.Generic;

namespace FFrameWork.Live
{
	// ノートが外部へ公開する共通情報のインターフェース
	public interface INote
	{
		// 譜面上の時間や位置などの情報
		MusicScoreInfo MusicScoreInfo { get; }

		// 現在の楽曲時間と判定時間の差
		float OffsetJudgeTime { get; }
		
		// 譜面上で定義されたノート左端の初期レーン位置
		int DefaultLeftLane { get; }
		
		// 譜面上で定義されたノート右端の初期レーン位置
		int DefaultRightLane { get; }

		// 実行中に更新されるノート左端のレーン位置
		float LaneStartF { get; set; }

		// 実行中に更新されるノート右端のレーン位置
		float LaneEndF { get; set; }

		// 入力判定に使用する左端のレーン範囲
		float JudgeLaneStart { get; set; }

		// 入力判定に使用する右端のレーン範囲
		float JudgeLaneEnd { get; set; }

		// ノート出現から判定時刻までの進行度
		float Progress { get; }

		// 現在のノート状態
		NoteState State { get; }

		// 判定結果と詳細情報
		(NoteResult, NoteResultDescription) JudgeInfo { get; }

		// 判定結果
		NoteResult Result { get; }

		// ノート種別
		NoteCategory Category { get; }

		// ノート方向
		NoteDirection Direction { get; }

		// ロングノート線の描画タイプ
		NoteLineType LineType { get; }

		// 親ノート
		INote ParentNote { get; }

		// 子ノート
		INote ChildNote { get; }

		// 表示側が参照するノート一覧
		List<INote> ViewNoteList { get; set; }

		// 譜面内のノート ID
		int Id { get; }
	}
}
