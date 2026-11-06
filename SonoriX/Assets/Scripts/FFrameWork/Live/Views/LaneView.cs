using UnityEngine;
using UnityEngine.Serialization;

namespace FFrameWork.Live
{
    public class LaneView : MonoBehaviour
    {
        // レーン面のベース表示
        [SerializeField] private SpriteRenderer subLaneBack;
        [SerializeField] private SpriteRenderer mainLaneBack;

        // レーン境界線の表示
        [SerializeField] private SpriteRenderer laneLine;

        //矢印のジャッジレーン
        [SerializeField] private SpriteRenderer arrowBackLane;

        // 判定ラインの表示
        [SerializeField] private SpriteRenderer defaultJudgeLine;

        // ライブ設定に合わせてレーン透明度と表示状態を初期化する
        // liveSetting: レーン透明度を含むライブ設定
        public void Setup(LiveSettingData liveSetting)
        {
            float alpha = liveSetting != null ? liveSetting.LaneTransparent : 1f;
            Color color = Color.white;
            color.a = alpha;
            if (subLaneBack != null)
            {
                subLaneBack.enabled = alpha > 0f;
            }            
            if (mainLaneBack != null)
            {
                mainLaneBack.enabled = alpha > 0f;
            }
            if (laneLine != null) laneLine.enabled = true;
            if (arrowBackLane != null) arrowBackLane.enabled = true;
            if (defaultJudgeLine != null) defaultJudgeLine.enabled = true;
        }
    }
}
