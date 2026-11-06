using UnityEngine;

namespace CP
{
    public static class CameraAspectUtility
    {
        // 基準アスペクトの垂直 FOV を現在アスペクト用の垂直 FOV へ変換する
        // verticalFOV: 基準の垂直 FOV
        // targetAspect: 基準アスペクト比
        // currentAspect: 変換先アスペクト比
        public static float CalculateFOV(float verticalFOV, float targetAspect, float currentAspect)
        {
            float horizontalFOV = CalculateVertical2HorizontalFOV(verticalFOV, targetAspect);
            return CalculateHorizontal2VerticalFOV(horizontalFOV, currentAspect);
        }

        // 垂直 FOV から水平 FOV を計算する
        // verticalFOV: 垂直 FOV
        // aspect: 対象アスペクト比
        public static float CalculateVertical2HorizontalFOV(float verticalFOV, float aspect)
        {
            return Mathf.Atan(Mathf.Tan(verticalFOV * 0.5f * Mathf.Deg2Rad) * aspect) * 2f * Mathf.Rad2Deg;
        }

        // 水平 FOV から垂直 FOV を計算する
        // horizontalFOV: 水平 FOV
        // aspect: 対象アスペクト比
        public static float CalculateHorizontal2VerticalFOV(float horizontalFOV, float aspect)
        {
            return Mathf.Atan(Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad) / aspect) * 2f * Mathf.Rad2Deg;
        }

        // FOV 差によって発生する垂直位置オフセットを計算する
        // baseFov: 基準 FOV
        // targetFov: 変換後 FOV
        // dist: カメラから対象までの距離
        public static float CalculateVerticalPositionOffset(float baseFov, float targetFov, float dist)
        {
            return -(Mathf.Tan(targetFov * 0.5f * Mathf.Deg2Rad) - Mathf.Tan(baseFov * 0.5f * Mathf.Deg2Rad)) * dist;
        }
    }
}
