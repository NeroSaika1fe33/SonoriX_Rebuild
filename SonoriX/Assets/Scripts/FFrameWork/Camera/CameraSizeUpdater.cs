using UnityEngine;

namespace FFrameWork
{
    public class CameraSizeUpdater : MonoBehaviour
    {
        private enum BaseType
        {
            // 幅と高さの両方を基準に調整する
            Both = 0,

            // 幅基準で OrthographicSize を調整する
            Width = 1,

            // 高さ基準で OrthographicSize を固定する
            Height = 2
        }

        // 基準画面幅
        [SerializeField] private float baseWidth = 1920f;

        // 基準画面高さ
        [SerializeField] private float baseHeight = 1080f;

        // 1 Unity Unit あたりのピクセル数
        [SerializeField] private float pixelPerUnit = 108f;

        // OrthographicSize の計算基準
        [SerializeField] private BaseType baseType = BaseType.Both;

        // サイズ更新対象の Camera
        [SerializeField] private Camera camera;

        // 前回更新した画面幅
        private int currentScreenWidth;

        // 前回更新した画面高さ
        private int currentScreenHeight;

        // 現在の OrthographicSize
        public float OrthographicSize => camera != null ? camera.orthographicSize : 0f;

        // 基準画面幅
        public float BaseWidth => baseWidth;

        // 基準画面高さ
        public float BaseHeight => baseHeight;

        // 基準高さから計算した OrthographicSize
        public float BaseOrthographicSize => pixelPerUnit == 0f ? 0f : baseHeight / pixelPerUnit * 0.5f;

        // 起動時に OrthographicSize を画面サイズへ合わせる
        private void Awake()
        {
            UpdateOrthographicSize();
        }

        // 有効化時に OrthographicSize を画面サイズへ合わせる
        private void OnEnable()
        {
            UpdateOrthographicSize();
        }

        // 画面サイズ変更を検知して OrthographicSize を更新する
        private void Update()
        {
            if (currentScreenWidth == Screen.width && currentScreenHeight == Screen.height)
            {
                return;
            }

            UpdateOrthographicSize();
        }

        // Inspector 編集時に Camera 参照を補完する
        private void OnValidate()
        {
            if (camera == null)
            {
                camera = GetComponentInChildren<Camera>();
            }
        }

        // 外部から OrthographicSize 更新を強制する
        public void ForceUpdate()
        {
            UpdateOrthographicSize();
        }

        // 基準解像度と現在画面比率から OrthographicSize を計算する
        private void UpdateOrthographicSize()
        {
            if (camera == null || pixelPerUnit == 0f || baseWidth == 0f || Screen.width == 0)
            {
                return;
            }

            currentScreenWidth = Screen.width;
            currentScreenHeight = Screen.height;

            float orthographicSize = BaseOrthographicSize;

            if (baseType != BaseType.Height)
            {
                float screenRatio = (float)Screen.height / Screen.width;
                float baseRatio = baseHeight / baseWidth;

                if (baseRatio <= screenRatio || baseType == BaseType.Width)
                {
                    orthographicSize *= screenRatio / baseRatio;
                }
            }

            camera.orthographicSize = orthographicSize;
        }
    }
}
