using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace FFrameWork.Live
{
    // FMOD Studio のグローバルパラメータを再生状態つきで扱う
    public class FmodAudioEffectSystem : MonoBehaviour
    {
        // このエフェクト枠が使用中か
        public bool IsPlaying { get; private set; }

        // 最後に設定した FMOD パラメータ値
        private readonly Dictionary<string, float> currentValues = new Dictionary<string, float>();

        // エフェクト枠を使用中にする
        public void Play() => IsPlaying = true;
        
        // エフェクト枠を未使用にする
        public void Stop() => IsPlaying = false;

        // FMOD の名前付きパラメータへ値を反映する
        public bool SetParameter(string parameterName, float value)
        {
            if (string.IsNullOrEmpty(parameterName)) return false;

            CacheParameter(parameterName, value);
            FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
            return result == FMOD.RESULT.OK;
        }

        // 外部で設定済みの値をキャッシュへ反映する
        public void CacheParameter(string parameterName, float value)
        {
            if (string.IsNullOrEmpty(parameterName)) return;

            currentValues[parameterName] = value;
        }

        // 現在値を取得し、未設定なら通常値を返す
        public float GetCurrentValue(string parameterName, float fallbackNormal)
        {
            return currentValues.TryGetValue(parameterName, out var v) ? v : fallbackNormal;
        }
    }
}
