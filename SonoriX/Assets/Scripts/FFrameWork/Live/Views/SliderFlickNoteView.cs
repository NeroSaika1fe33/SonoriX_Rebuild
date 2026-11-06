namespace FFrameWork.Live
{
	// スライダーフリックノーツの向きを反映するビュー
	public class SliderFlickNoteView : DefaultNoteView
	{
		// ノーツ生成時にフリック方向の反転表示を適用する
		// note: 表示対象のノーツ
		// posZ: ノーツ表示に使う Z 座標
		public override void Spawn(INote note, float posZ)
		{
			base.Spawn(note, posZ);
			ApplyDirectionFlip();
		}

		// ノーツ方向に合わせて SpriteRenderer の左右反転を設定する
		private void ApplyDirectionFlip()
		{
			if (note == null || spriteRenderer == null)
			{
				return;
			}

			spriteRenderer.flipX = note.Direction == NoteDirection.Left;
		}
	}
}
