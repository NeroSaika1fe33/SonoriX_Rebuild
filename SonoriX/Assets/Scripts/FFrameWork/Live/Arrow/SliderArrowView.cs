using UnityEngine;

namespace FFrameWork.Live
{
	[DisallowMultipleComponent]
	public class SliderArrowView : MonoBehaviour
	{
		// 矢印本体として拡縮する表示オブジェクト
		[SerializeField] private GameObject ArrowView;

		// 矢印ビューの初期化入口
		public void Setup()
		{
		}

		// 位置とサイズをまとめて矢印ビューへ反映する
		// xPos: 表示するワールド X 座標
		// size: 表示する横幅
		public void Apply(float xPos, float size)
		{
			Vector3 currentPosition = GetCurrentPosition();
			UpdateViewPosition(xPos, currentPosition.y, currentPosition.z);
			UpdateSpriteSize(size);
		}

		// 矢印ビューの座標を更新する
		// xPos: 更新後の X 座標
		// yPos: 更新後の Y 座標
		// zPos: 更新後の Z 座標
		public void UpdateViewPosition(float xPos, float yPos, float zPos)
		{
			transform.position = new Vector3(xPos, yPos, zPos);
		}

		// 現在の矢印ビュー座標を取得する
		public Vector3 GetCurrentPosition()
		{
			return transform.position;
		}

		// 矢印本体の横幅を更新する
		// size: 適用する横幅
		public void UpdateSpriteSize(float size)
		{
			Transform target = ArrowView != null ? ArrowView.transform : transform;
			Vector3 scale = target.localScale;
			scale.x = Mathf.Max(0.01f, size);
			target.localScale = scale;
		}
	}
}
