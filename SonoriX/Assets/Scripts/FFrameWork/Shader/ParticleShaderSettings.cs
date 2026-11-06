using UnityEngine;

namespace FFrameWork.Live
{
    [ExecuteInEditMode]
    public class ParticleShaderSettings : MonoBehaviour
    {
        public enum Mode
        {
            // 加算合成で表示する
            Additive,

            // アルファブレンドで表示する
            AlphaBlend
        }

        // パーティクル用シェーダーの表示モード
        [SerializeField] private Mode mode;
    }
}
