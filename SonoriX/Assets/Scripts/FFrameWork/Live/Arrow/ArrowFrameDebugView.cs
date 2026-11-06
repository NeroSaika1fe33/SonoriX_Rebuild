using UnityEngine;

namespace FFrameWork.Live
{
    [DisallowMultipleComponent]
    public class ArrowFrameDebugView : MonoBehaviour
    {
        // 判定枠として動かす表示オブジェクト
        [SerializeField] private GameObject frameView;

        // デバッグ表示の初期化入口
        public void Setup()
        {
        }

        // 位置とサイズをまとめてデバッグ表示へ反映する
        // xPos: 表示するワールド X 座標
        // size: 表示する判定幅
        public void Apply(float xPos, float size)
        {
            UpdateViewPosition(xPos);
            UpdateSpriteSize(size);
        }

        // デバッグ表示の X 座標だけを更新する
        // xPos: 表示するワールド X 座標
        public void UpdateViewPosition(float xPos)
        {
            Vector3 position = transform.position;
            position.x = xPos;
            transform.position = position;
        }

        // デバッグ表示の横幅を更新する
        // size: 表示する判定幅
        public void UpdateSpriteSize(float size)
        {
            Transform target = GetTargetTransform();
            Vector3 scale = target.localScale;
            scale.x = Mathf.Max(0.01f, size);
            target.localScale = scale;
        }

        // 専用表示オブジェクトがあればそれを、なければ自身の Transform を返す
        private Transform GetTargetTransform()
        {
            return frameView != null ? frameView.transform : transform;
        }
    }
}
