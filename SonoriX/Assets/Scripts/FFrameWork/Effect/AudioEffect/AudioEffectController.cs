using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // ライブ中の操作に応じて FMOD エフェクトパラメータを制御する
    public class AudioEffectController : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logDebugInfo;

        [Header("FMOD Effect Configurations")]
        [SerializeField] private List<FmodEffectGroup> effectGroups = new List<FmodEffectGroup>();
        [SerializeField] private FmodAudioEffectSystem[] audioEffectSystems;

        // エフェクト種別から Inspector 設定グループを引く辞書
        private readonly Dictionary<AudioEffectType, List<FmodEffectGroup>> groupMap = new Dictionary<AudioEffectType, List<FmodEffectGroup>>();
        
        // FMOD エフェクト枠の使用状況を管理するプール
        private readonly AudioEffectPool audioEffectPool = new AudioEffectPool();
        // FMOD エフェクト枠を収集し、不足分を補う
        private readonly AudioEffectSystemProvider systemProvider = new AudioEffectSystemProvider();
        // FMOD パラメータの反映先を選択して値を適用する
        private readonly AudioEffectParameterApplier parameterApplier = new AudioEffectParameterApplier();

        // ボリュームロングで現在使用中のフィルター種別
        private AudioEffectType activeVolumeLongEffectType = AudioEffectType.Free;

        // いずれかの FMOD エフェクト枠が使用中か
        public bool IsPlaying => audioEffectSystems != null && System.Array.Exists(audioEffectSystems, s => s != null && s.IsPlaying);

        private void Awake() => Setup();

        // Controller 参照と FMOD エフェクトプールを初期化する
        public void Setup(BaseLiveController controller = null)
        {
            parameterApplier.SetLiveController(controller != null ? controller : GetComponentInParent<BaseLiveController>());
            audioEffectSystems = systemProvider.Ensure(this, audioEffectSystems, GetRequiredPoolSize());

            audioEffectPool.Setup(audioEffectSystems);
            InitGroupMap();
        }

        // Inspector のエフェクト設定を種別ごとに引ける形へまとめる
        private void InitGroupMap()
        {
            groupMap.Clear();
            if (effectGroups == null) return;

            // Group effect configurations by effect type.
            foreach (var group in effectGroups)
            {
                if (group == null) continue;

                if (!groupMap.TryGetValue(group.effectType, out var list))
                {
                    list = new List<FmodEffectGroup>();
                    groupMap[group.effectType] = list;
                }
                list.Add(group);
            }
        }

        // ==========================================
        // ==========================================

        // 指定エフェクトを開始し、Switch パラメータを有効値へ切り替える
        public bool BeginEffect(AudioEffectType effectType)
        {
            if (!groupMap.TryGetValue(effectType, out var groups)) return false;

            FmodAudioEffectSystem system = audioEffectPool.Acquire(effectType);
            if (system == null) return false;

            bool success = true;

            foreach (var group in groups)
            {
                if (group.parameterBindings == null) continue;
                foreach (var binding in group.parameterBindings)
                {
                    if (binding.updateType == ParameterUpdateType.Switch)
                    {
                        success &= parameterApplier.SetValue(system, binding.parameterName, binding.activeValue);
                    }
                }
            }
            return success;
        }

        // 指定エフェクトを更新し、Linear パラメータを入力量に応じて動かす
        public bool UpdateEffect(AudioEffectType effectType, float deltaValue)
        {
            if (!groupMap.TryGetValue(effectType, out var groups)) return false;

            FmodAudioEffectSystem system = audioEffectPool.GetActive(effectType) ?? audioEffectPool.Acquire(effectType);
            if (system == null) return false;

            bool success = true;

            foreach (var group in groups)
            {
                if (group.parameterBindings == null) continue;
                foreach (var binding in group.parameterBindings)
                {
                    if (binding.updateType == ParameterUpdateType.Switch)
                    {
                        success &= parameterApplier.SetValue(system, binding.parameterName, binding.activeValue);
                    }
                    else if (binding.updateType == ParameterUpdateType.Linear)
                    {
                        float current = system.GetCurrentValue(binding.parameterName, binding.normalValue);
                        float next = Mathf.Clamp(current + deltaValue * binding.sensitivity, binding.minValue, binding.maxValue);
                        success &= parameterApplier.SetValue(system, binding.parameterName, next);
                    }
                }
            }
            return success;
        }

        // 指定エフェクトを通常値へ戻してプールから解放する
        public bool EndEffect(AudioEffectType effectType)
        {
            if (!groupMap.TryGetValue(effectType, out var groups)) return false;

            FmodAudioEffectSystem system = audioEffectPool.GetActive(effectType) ?? audioEffectPool.Acquire(effectType);
            if (system == null) return false;

            bool success = true;

            foreach (var group in groups)
            {
                if (group.parameterBindings == null) continue;
                foreach (var binding in group.parameterBindings)
                {
                    success &= parameterApplier.SetValue(system, binding.parameterName, binding.normalValue);
                }
            }
            audioEffectPool.Release(effectType);
            return success;
        }

        // ==========================================
        // ==========================================

        #region Slider Long Effects (Wobble)

        // スライダーロング用のワブルエフェクトを開始する
        public bool BeginSliderLongEffect() => BeginEffect(AudioEffectType.Wobble);
        // スライダーロング用のワブルエフェクトを更新する
        public bool UpdateSliderLongEffect() => UpdateEffect(AudioEffectType.Wobble, 1f);
        // スライダーロング用のワブルエフェクトを終了する
        public bool EndSliderLongEffect() => EndEffect(AudioEffectType.Wobble);

        #endregion

        #region Free Lane Long Effects (Flanger)

        // フリーレーンロング用のフランジャーを開始する
        public bool BeginFreeLaneLongEffect() => BeginEffect(AudioEffectType.FlangerMix);
        
        // フリーレーンロング用のフランジャーを更新する
        public bool UpdateFreeLaneLongEffect() => UpdateEffect(AudioEffectType.FlangerMix, 1f);
        
        // フリーレーンロング用のフランジャーを終了する
        public bool EndFreeLaneLongEffect() => EndEffect(AudioEffectType.FlangerMix);

        #endregion

        #region Volume Long Effects (Lowpass / Highpass)

        // 入力方向に応じてローパスまたはハイパスを開始する
        public bool BeginVolumeLongEffect(float deltaY)
        {
            if (Mathf.Approximately(deltaY, 0f)) return false;

            AudioEffectType effectType = deltaY < 0f ? AudioEffectType.LowpassCutoff : AudioEffectType.HighpassCutoff;
            if (activeVolumeLongEffectType != AudioEffectType.Free && activeVolumeLongEffectType != effectType)
            {
                EndEffect(activeVolumeLongEffectType);
            }

            activeVolumeLongEffectType = effectType;
            return BeginEffect(effectType);
        }

        // 入力方向に応じてローパスまたはハイパスのカットオフを更新する
        public bool UpdateVolumeLongEffect(float deltaY)
        {
            if (Mathf.Approximately(deltaY, 0f)) return false;

            AudioEffectType effectType = deltaY < 0f ? AudioEffectType.LowpassCutoff : AudioEffectType.HighpassCutoff;

            if (activeVolumeLongEffectType != AudioEffectType.Free && activeVolumeLongEffectType != effectType)
            {
                EndEffect(activeVolumeLongEffectType);
            }

            activeVolumeLongEffectType = effectType;
            return UpdateEffect(effectType, Mathf.Abs(deltaY));
        }

        // ボリュームロング用フィルターを終了する
        public void EndVolumeLongEffect()
        {
            if (activeVolumeLongEffectType != AudioEffectType.Free)
            {
                EndEffect(activeVolumeLongEffectType);
                activeVolumeLongEffectType = AudioEffectType.Free;
                return;
            }

            EndEffect(AudioEffectType.LowpassCutoff);
            EndEffect(AudioEffectType.HighpassCutoff);
        }

        #endregion

        // すべてのエフェクトパラメータを通常値へ戻して停止する
        public void StopAll()
        {
            if (effectGroups == null) return;

            FmodAudioEffectSystem system = audioEffectPool.GetAny();
            if (system == null) return;

            foreach (var group in effectGroups)
            {
                if (group == null || group.parameterBindings == null) continue;
                foreach (var binding in group.parameterBindings)
                {
                    parameterApplier.SetValue(system, binding.parameterName, binding.normalValue);
                }
            }
            audioEffectPool.StopAll();
            activeVolumeLongEffectType = AudioEffectType.Free;
        }

        // 同時に使う可能性があるエフェクト種別数から必要プール数を計算する
        private int GetRequiredPoolSize()
        {
            if (effectGroups == null || effectGroups.Count == 0)
            {
                return 1;
            }

            HashSet<AudioEffectType> effectTypes = new HashSet<AudioEffectType>();
            for (int i = 0; i < effectGroups.Count; i++)
            {
                if (effectGroups[i] != null)
                {
                    effectTypes.Add(effectGroups[i].effectType);
                }
            }

            return Mathf.Max(1, effectTypes.Count);
        }
    }
}
