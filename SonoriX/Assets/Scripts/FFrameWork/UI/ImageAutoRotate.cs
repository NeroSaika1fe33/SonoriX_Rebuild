using UnityEngine;

// UI の回転
namespace FFrameWork
{
    // 毎フレーム Z 軸回転を加える簡易 UI アニメーション
    public class ImageAutoRotate : MonoBehaviour
    {
        // 1 秒あたりの回転角度
        [SerializeField] private float speed = 90f;

        // 指定速度で RectTransform または Transform を回転させる
        private void Update()
        {
            transform.Rotate(0f, 0f, speed * Time.deltaTime);
        }
    }
}
