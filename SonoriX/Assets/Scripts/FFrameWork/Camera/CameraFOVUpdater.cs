using FFrameWork.Core;
using UnityEngine;

namespace FFrameWork
{
    public class CameraFOVUpdater : MonoBehaviour
    {
        // FOV を更新する対象 Camera
        [SerializeField] private Camera camera;

        // 前回更新した画面幅
        private int currentScreenWidth;

        // 前回更新した画面高さ
        private int currentScreenHeight;

        // 基準として保持する初期 FOV
        private float baseFieldOfView;

        // 起動時に基準 FOV を保存して画面サイズへ反映する
        private void Awake()
        {
            if (camera != null)
            {
                baseFieldOfView = camera.fieldOfView;
            }

            UpdateFOVSize();
        }

        // 有効化時に基準 FOV を補完し、画面サイズへ反映する
        private void OnEnable()
        {
            if (camera != null && baseFieldOfView <= 0f)
            {
                baseFieldOfView = camera.fieldOfView;
            }

            UpdateFOVSize();
        }

        // 画面サイズ変更を検知して FOV を更新する
        private void Update()
        {
            if (currentScreenWidth == Screen.width && currentScreenHeight == Screen.height)
            {
                return;
            }

            UpdateFOVSize();
        }

        // Inspector 編集時に Camera 参照を補完する
        private void OnValidate()
        {
            if (camera == null)
            {
                camera = GetComponent<Camera>();
            }
        }

        // 外部から FOV 更新を強制する
        public void ForceUpdate()
        {
            UpdateFOVSize();
        }

        // 現在画面のアスペクト比に合わせて Camera FOV を更新する
        private void UpdateFOVSize()
        {
            if (camera == null)
            {
                return;
            }

            currentScreenWidth = Screen.width;
            currentScreenHeight = Screen.height;

            float sourceFov = baseFieldOfView > 0f ? baseFieldOfView : camera.fieldOfView;
            camera.fieldOfView = CameraAspect.CalculateVerticalFov(sourceFov);
        }
    }
}
