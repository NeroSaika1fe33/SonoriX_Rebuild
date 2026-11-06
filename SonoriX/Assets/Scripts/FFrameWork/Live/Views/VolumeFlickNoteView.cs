using UnityEngine;

namespace FFrameWork.Live
{
	public class VolumeFlickNoteView : DefaultNoteView
	{
		// 音量ダウン方向に使う Sprite
		[SerializeField]
		private Sprite volumeDownSprite;

		// 音量アップ方向に使う Sprite
		[SerializeField]
		private Sprite volumeUpSprite;

		// 方向 Sprite が未設定の場合に戻す標準 Sprite
		private Sprite defaultSprite;

		// 標準 Sprite を保持してから基底のセットアップを行う
		// spawnPosition: ノーツ生成位置
		// judgmentPositions: 各レーンの判定位置
		public override void Setup(Vector2 spawnPosition, Vector2[] judgmentPositions)
		{
			base.Setup(spawnPosition, judgmentPositions);
			defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
		}

		// ノーツ生成時に方向 Sprite を適用する
		// note: 表示対象のノーツ
		// posZ: ノーツ表示に使う Z 座標
		public override void Spawn(INote note, float posZ)
		{
			base.Spawn(note, posZ);
			ApplyDirectionSprite();
		}

		// ノーツ方向に対応する Sprite へ差し替える
		private void ApplyDirectionSprite()
		{
			if (note == null || spriteRenderer == null)
			{
				return;
			}

			Sprite directionSprite = GetDirectionSprite(note.Direction);
			if (directionSprite != null)
			{
				spriteRenderer.sprite = directionSprite;
			}
		}

		// ノーツ方向に対応する Sprite を取得する
		// direction: ノーツ方向
		private Sprite GetDirectionSprite(NoteDirection direction)
		{
			if (direction == NoteDirection.Left)
			{
				return volumeDownSprite != null ? volumeDownSprite : defaultSprite;
			}
			if (direction == NoteDirection.Right)
			{
				return volumeUpSprite != null ? volumeUpSprite : defaultSprite;
			}
			return defaultSprite;
		}
	}
}
