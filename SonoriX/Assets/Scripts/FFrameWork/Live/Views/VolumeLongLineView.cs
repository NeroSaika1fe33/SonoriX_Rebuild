namespace FFrameWork.Live
{
	public class VolumeLongLineView : SliderLineView
	{
		// 表示対象にするボリューム方向
		private NoteDirection directionFilter = NoteDirection.Default;

		// 表示対象にするボリューム方向を設定する
		// direction: 通過させるノーツ方向
		public void SetDirectionFilter(NoteDirection direction)
		{
			directionFilter = direction;
		}

		// 方向条件に合う VolumeLongNote だけをライン管理へ追加する
		// note: 追加するロングノーツ
		public override void Add(LongNote note)
		{
			if (!(note is VolumeLongNote volumeLongNote) ||
				!IsMatchingDirection(volumeLongNote.Direction))
			{
				return;
			}

			base.Add(note);
		}

		// 指定したロングノーツをライン管理から削除する
		// note: 削除するロングノーツ
		public override void Remove(LongNote note)
		{
			if (note == null || longNotes == null)
			{
				return;
			}

			longNotes.Remove(note);
		}

		// 現在の方向フィルターに一致するかを返す
		// direction: 判定するノーツ方向
		private bool IsMatchingDirection(NoteDirection direction)
		{
			return directionFilter == NoteDirection.Default ||
				direction == directionFilter;
		}
	}
}
