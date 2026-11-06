using System;
using UnityEngine;
using System.Collections.Generic;

namespace FFrameWork.Live
{       
    // FMOD パラメータの更新方式
    public enum ParameterUpdateType
    {
        // 入力量に応じて連続的に増減する
        Linear,
        // 開始時と終了時に値を切り替える
        Switch
    }
    
    // FMOD エフェクトの Inspector 設定データ
    public class FmodEffectData
    {
        [Serializable]
        // 1 つの FMOD パラメータに対する値設定
        public class FmodParameterBinding
        {
            [Header("FMOD Automation Parameter")]
            public string parameterName;
            public ParameterUpdateType updateType;

            [Header("Values")] 
            public float normalValue;
            public float activeValue;
            public float minValue;
            public float maxValue;
            
            [Header("Sensitivity")]
            public float sensitivity;
        }
        
    }

    [Serializable]
    // 同じ AudioEffectType でまとめて動かす FMOD パラメータ群
    public class FmodEffectGroup
    {
        // このグループが反応するエフェクト種別
        public AudioEffectType effectType;
        // 操作対象の FMOD パラメータ一覧
        public List<FmodEffectData.FmodParameterBinding> parameterBindings;
    }
}
