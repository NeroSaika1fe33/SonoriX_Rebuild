using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork.Live
{
	public class LongNoteView : BaseNoteView
	{
		// ロングノーツの進行率に合わせて表示位置とサイズを更新する
		public override void Move()
		{
			if (note == null)
			{
				return;
			}
			if (note.Progress <= 0f)
			{
				transform.localScale = Vector3.zero;
				return;
			}
			if (ShouldHideAfterStartJudgment())
			{
				transform.localScale = Vector3.zero;
				return;
			}

			float progress = CalcProgress();
			transform.localPosition = CalcNotePosition(ref progress);
			UpdateNoteSpriteSize();
			UpdateMaterialParameters(progress);
		}

		// 表示に使うロングノーツ進行率を取得する
		protected float CalcProgress()
		{
			if (note == null)
			{
				return 0f;
			}

			return Mathf.Clamp01(note.Progress);
		}

		// 始点判定後に子ノーツへ表示を譲るべきかを返す
		private bool ShouldHideAfterStartJudgment()
		{
			return note != null &&
				note.ChildNote != null &&
				note.Progress >= 1f;
		}

		// レーン補間を使ってロングノーツの表示位置を計算する
		// progress: 現在のノーツ進行率
		protected override Vector3 CalcNotePosition(ref float progress)
		{
			return CalcLaneInterpolatedNotePosition(ref progress, true);
		}

		// ロングノーツの開始レーンから終了レーンまでを含む表示幅を取得する
		protected override float GetNoteSpriteLaneWidth()
		{
			if (note == null)
			{
				return base.GetNoteSpriteLaneWidth();
			}

			return Mathf.Max(1f, note.LaneEndF - note.LaneStartF + 1f);
		}
	}
}
