using CP;
using UnityEngine;

namespace FFrameWork.Core
{
    public static class CameraAspect
    {
        // 前回計算した画面高さ
        private static int screenHeight;

        // 前回計算した画面幅
        private static int screenWidth;

        // キャッシュしている現在アスペクト比
        private static float currentAspect;

        // 基準になる 16:9 相当のアスペクト比
        public static readonly float TargetAspect = 1.7778f;

        // 現在画面のアスペクト比を取得し、画面サイズ変更時だけ再計算する
        public static float CurrentAspect
        {
            get
            {
                if (screenHeight != Screen.height || screenWidth != Screen.width)
                {
                    screenHeight = Screen.height;
                    screenWidth = Screen.width;
                    currentAspect = screenHeight == 0 ? TargetAspect : (float)screenWidth / screenHeight;
                }

                return currentAspect;
            }
        }

        // 現在画面が基準より縦長か
        public static bool IsVertical => CurrentAspect < TargetAspect;

        // 現在画面が基準より横長か
        public static bool IsHorizontal => TargetAspect < CurrentAspect;

        // 縦長画面で見切れないよう垂直 FOV を補正する
        // currentFov: 基準の垂直 FOV
        public static float CalculateVerticalFov(float currentFov)
        {
            if (IsVertical)
            {
                return CameraAspectUtility.CalculateFOV(currentFov, TargetAspect, CurrentAspect);
            }

            return currentFov;
        }

        // 補正済み垂直 FOV を基準アスペクト側へ戻す
        // currentFov: 補正対象の垂直 FOV
        public static float CalculateInvertVerticalFov(float currentFov)
        {
            if (IsVertical)
            {
                return CameraAspectUtility.CalculateFOV(currentFov, CurrentAspect, TargetAspect);
            }

            return currentFov;
        }
    }
}
