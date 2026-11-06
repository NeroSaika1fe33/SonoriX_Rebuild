using UnityEngine;

namespace FFrameWork.Live
{
    public class ArrowPointerView : MonoBehaviour
    { 
        // ポインター表示に使う SpriteRenderer
        [SerializeField] SpriteRenderer _spriteRenderer;

        // 参照不足ログを一度だけ出すためのフラグ
        private bool hasLoggedMissingReference;

        // 現在のポインター X 座標
        public float CurrentX
        {
            get { return GetTargetTransform().position.x; }
        }

        // 起動時に SpriteRenderer 参照を解決する
        private void Awake()
        {
            Setup();
        }

        // Inspector 未設定時は自身または子から SpriteRenderer を探す
        public void Setup()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        // ポインターを指定 X 座標へ移動する
        // x: 移動先のワールド X 座標
        public bool ApplyPosition(float x)
        {
            if (_spriteRenderer == null)
            {
                Setup();
            }

            if (_spriteRenderer == null)
            {
                LogMissingReference();
                return false;
            }

            Transform target = GetTargetTransform();
            Vector3 position = target.position;
            position.x = x;
            target.position = position;
            return true;
        }

        // SpriteRenderer があればその Transform、なければ自身の Transform を返す
        private Transform GetTargetTransform()
        {
            return _spriteRenderer != null ? _spriteRenderer.transform : transform;
        }

        // SpriteRenderer 参照が不足していることを一度だけ警告する
        private void LogMissingReference()
        {
            if (hasLoggedMissingReference)
            {
                return;
            }

            hasLoggedMissingReference = true;
            Debug.LogWarning($"{nameof(ArrowPointerView)} requires a SpriteRenderer.", this);
        }
    }
}
