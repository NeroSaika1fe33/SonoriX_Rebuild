using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    public class TapEffectView : MonoBehaviour
    {
        [Serializable]
        public class EffectPrefab
        {
            // エフェクト参照名
            public string name;

            // 生成するエフェクト Prefab
            public GameObject prefab;

            // この Prefab 用に確保するプール数
            public int poolCount = 16;
        }

        // 固定レーン数
        private const int FixedLaneCount = 4;

        // 固定レーン押下エフェクトの X 座標
        private static readonly float[] FixedLaneXs = { -135f, -45f, 45f, 135f };

        [Header("Lane Press Settings")]
        // ノーツエフェクト種別ごとの Prefab 設定
        [SerializeField] private EffectPrefab[] effectPrefabs;

        // 通常タップ用のフォールバック Prefab
        [SerializeField] private GameObject fallbackSinglePrefab;

        // フリック用のフォールバック Prefab
        [SerializeField] private GameObject fallbackFlickPrefab;

        // ループ用のフォールバック Prefab
        [SerializeField] private GameObject fallbackLoopPrefab;

        [Header("Fade & Timing Settings")]
        [SerializeField] private float fixedLaneTapEffectMinDisplaySeconds = 0.12f;
        [SerializeField] private float fixedLaneTapEffectFadeOutSeconds = 0.18f;
        [SerializeField] private int defaultNoteEffectPoolCount = 32;

        [Header("Transform Settings")]
        [SerializeField] private bool forceLocalParticleAlignment = true;
        [SerializeField] private bool forceLocalSimulationSpace = true;

        private const string UseFlipMatrixPropertyName = "_UseFlipMatrix";
        private const string NormalAuraEffectName = "fx_note_normal_aura";
        private const string NormalGenEffectName = "fx_note_normal_gen";
        private const string FlickAuraEffectName = "fx_note_flick_aura";
        private const string FlickGenEffectName = "fx_note_flick_gen";
        private const string AuraChildPath = "root/aura";
        private const string BaseChildPath = "root/aura/base";

        private ParticleSystemController[] fixedLaneEffects;
        private float[] fixedLaneStartedTimes;
        private Coroutine[] stopCoroutines;
        private Dictionary<GameObject, EffectPool> noteEffectPools;
        private Dictionary<GameObject, int> noteEffectPoolCounts;
        private Dictionary<Transform, Vector3> noteEffectInitialScales;
        private Transform noteEffectPoolRoot;
        private BaseLiveController liveController;
        private ILiveSoundPlayer soundPlayer;

        public void Setup(
            BaseLiveController liveBaseController,
            EffectPrefab[] effectPrefabs = null,
            GameObject fallbackSinglePrefab = null,
            GameObject fallbackFlickPrefab = null,
            GameObject fallbackLoopPrefab = null,
            ILiveSoundPlayer liveSoundPlayer = null,
            Transform unpickedEffectAnchor = null)
        {
            if (effectPrefabs != null) this.effectPrefabs = effectPrefabs;
            if (fallbackSinglePrefab != null) this.fallbackSinglePrefab = fallbackSinglePrefab;
            if (fallbackFlickPrefab != null) this.fallbackFlickPrefab = fallbackFlickPrefab;
            if (fallbackLoopPrefab != null) this.fallbackLoopPrefab = fallbackLoopPrefab;

            this.liveController = liveBaseController;
            this.soundPlayer = liveSoundPlayer;
            fixedLaneStartedTimes = new float[FixedLaneCount];
            stopCoroutines = new Coroutine[FixedLaneCount];
            noteEffectPools = new Dictionary<GameObject, EffectPool>();
            noteEffectPoolCounts = BuildNoteEffectPoolCounts();
            noteEffectInitialScales = new Dictionary<Transform, Vector3>();
            SetupFixedLaneEffects(liveBaseController, unpickedEffectAnchor);
        }

        public void Clear()
        {
            CancelAllStopCoroutines();
            if (fixedLaneEffects != null)
            {
                foreach (var effect in fixedLaneEffects) effect?.Stop();
            }
            StopNoteEffectPools();
        }

        // 固定レーン入力に対して押下中エフェクトを開始または停止する
        // lane: 入力されたレーン番号
        // input: 入力フェーズ情報
        public void Unpicked(int lane, ref LiveInput input)
        {
            if (input.source != LiveInputSource.FixedLane) return;

            int index = GetFixedLaneIndex(lane);
            if (index < 0 || index >= FixedLaneCount || fixedLaneEffects == null) return;

            ParticleSystemController effect = fixedLaneEffects[index];
            if (effect == null) return;

            if (input.phase == LiveInputPhase.Began)
            {
                CancelStopCoroutine(index);
                fixedLaneStartedTimes[index] = Time.time;

                effect.ConfigureStableHold(3600f);
                effect.Play();
                PlaySE(LiveSoundDefine.SE_LIVE_TAP);
            }
            else if (input.phase == LiveInputPhase.Ended || input.phase == LiveInputPhase.Canceled)
            {
                StopFixedLaneTapEffectAt(index);
            }
        }

        // ノーツ判定に対応する単発エフェクトと SE を再生する
        // note: 判定済みノーツ
        // checkPlayedHaptic: 互換用のハプティック再生確認関数
		public void Excute(INote note, Func<bool> checkPlayedHaptic)
		{
			if (note == null || note.Result == NoteResult.None || note.Result == NoteResult.Miss || note.Result == NoteResult.Pass) return;
			if (note is LongHoldCombo) return;

            Vector3 spawnPos = GetNoteCenterPosition(note);

            GameObject prefab = GetNoteParticlePrefab(note.Category);
            if (prefab != null)
            {
                ParticleSystemController effect = SpawnNoteEffect(prefab);
                if (effect != null)
                {
                    effect.transform.localPosition = spawnPos;
                    ApplyNoteEffectWidth(effect.transform, note);
                    effect.Play();
                }
            }

            PlayNoteSE(note);
        }

        private void StopFixedLaneTapEffectAt(int index)
        {
            float elapsed = Time.time - fixedLaneStartedTimes[index];
            float remainingMinTime = fixedLaneTapEffectMinDisplaySeconds - elapsed;

            if (remainingMinTime > 0f)
            {
                CancelStopCoroutine(index);
                stopCoroutines[index] = StartCoroutine(DeferredStopCoroutine(index, remainingMinTime));
                return;
            }

            if (fixedLaneTapEffectFadeOutSeconds > 0f)
            {
                fixedLaneEffects[index]?.FadeOutAndStop(fixedLaneTapEffectFadeOutSeconds);
            }
            else
            {
                fixedLaneEffects[index]?.Stop();
            }
        }

        private IEnumerator DeferredStopCoroutine(int index, float delay)
        {
            yield return new WaitForSeconds(delay);
            stopCoroutines[index] = null;

            if (fixedLaneTapEffectFadeOutSeconds > 0f)
            {
                fixedLaneEffects[index]?.FadeOutAndStop(fixedLaneTapEffectFadeOutSeconds);
            }
            else
            {
                fixedLaneEffects[index]?.Stop();
            }
        }

        private void CancelStopCoroutine(int index)
        {
            if (stopCoroutines != null && stopCoroutines[index] != null)
            {
                StopCoroutine(stopCoroutines[index]);
                stopCoroutines[index] = null;
            }
        }

        private void CancelAllStopCoroutines()
        {
            for (int i = 0; i < FixedLaneCount; i++) CancelStopCoroutine(i);
        }

        private void SetupFixedLaneEffects(BaseLiveController controller, Transform anchor)
        {
            GameObject prefab = GetPrefabByName("fx_lane_tap") ?? fallbackSinglePrefab;
            fixedLaneEffects = new ParticleSystemController[FixedLaneCount];
            if (prefab == null) return;

            Transform root = transform.Find("FixedLaneRoot");
            if (root == null)
            {
                root = new GameObject("FixedLaneRoot").transform;
                root.SetParent(transform, false);
            }

            for (int i = 0; i < FixedLaneCount; i++)
            {
                GameObject instance = Instantiate(prefab, root, false);
                instance.name = $"FixedLaneEffect_{i}";

                Vector3 pos = new Vector3(FixedLaneXs[i], 0f, 0f);
                if (anchor != null)
                {
                    Vector3 anchorPos = transform.InverseTransformPoint(anchor.position);
                    pos.y = anchorPos.y;
                    pos.z = anchorPos.z;
                }
                instance.transform.localPosition = pos;
                ConfigureParticleSystems(instance);
                ConfigureParticleRenderers(instance);

                ParticleSystemController pController = instance.GetComponent<ParticleSystemController>() ?? instance.AddComponent<ParticleSystemController>();
                pController.ConfigureStableHold(3600f);
                pController.RegisterToLiveController(controller);
                pController.Stop();
                fixedLaneEffects[i] = pController;
            }
        }

        private GameObject GetNoteParticlePrefab(NoteCategory category)
        {
            if (category == NoteCategory.SliderFlick)
            {
                return GetPrefabByName(NormalAuraEffectName) ?? GetPrefabByName(NormalGenEffectName) ?? fallbackSinglePrefab;
            }

            return GetPrefabByName(NormalAuraEffectName) ?? GetPrefabByName(NormalGenEffectName) ?? fallbackSinglePrefab;
        }

        private GameObject GetPrefabByName(string effectName)
        {
            if (effectPrefabs == null) return null;
            for (int i = 0; i < effectPrefabs.Length; i++)
            {
                if (effectPrefabs[i] != null && effectPrefabs[i].name == effectName) return effectPrefabs[i].prefab;
            }
            return null;
        }

        private Dictionary<GameObject, int> BuildNoteEffectPoolCounts()
        {
            Dictionary<GameObject, int> counts = new Dictionary<GameObject, int>();
            for (int i = 0; effectPrefabs != null && i < effectPrefabs.Length; i++)
            {
                EffectPrefab effectPrefab = effectPrefabs[i];
                if (effectPrefab == null || effectPrefab.prefab == null)
                {
                    continue;
                }

                counts[effectPrefab.prefab] = Mathf.Max(1, effectPrefab.poolCount);
            }
            return counts;
        }

        private ParticleSystemController SpawnNoteEffect(GameObject prefab)
        {
            EffectPool pool = GetOrCreateNoteEffectPool(prefab);
            return pool != null ? pool.Spawn() : null;
        }

        private EffectPool GetOrCreateNoteEffectPool(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (noteEffectPools == null)
            {
                noteEffectPools = new Dictionary<GameObject, EffectPool>();
            }
            if (noteEffectInitialScales == null)
            {
                noteEffectInitialScales = new Dictionary<Transform, Vector3>();
            }

            if (noteEffectPools.TryGetValue(prefab, out EffectPool existingPool))
            {
                return existingPool;
            }

            Transform root = GetOrCreateNoteEffectPoolRoot();
            GameObject poolObject = new GameObject($"{prefab.name}_Pool");
            poolObject.transform.SetParent(root, false);

            EffectPool pool = poolObject.AddComponent<EffectPool>();
            pool.Setup(prefab, GetNoteEffectPoolCount(prefab), liveController, ConfigureNoteEffectInstance);
            noteEffectPools[prefab] = pool;
            return pool;
        }

        private void ConfigureNoteEffectInstance(GameObject instance)
        {
            ConfigureParticleSystems(instance);
            ConfigureParticleRenderers(instance);
        }

        private Transform GetOrCreateNoteEffectPoolRoot()
        {
            if (noteEffectPoolRoot != null)
            {
                return noteEffectPoolRoot;
            }

            Transform root = transform.Find("NoteEffectPoolRoot");
            if (root == null)
            {
                root = new GameObject("NoteEffectPoolRoot").transform;
                root.SetParent(transform, false);
            }

            noteEffectPoolRoot = root;
            return noteEffectPoolRoot;
        }

        private int GetNoteEffectPoolCount(GameObject prefab)
        {
            if (prefab != null && noteEffectPoolCounts != null && noteEffectPoolCounts.TryGetValue(prefab, out int count))
            {
                return Mathf.Max(1, count);
            }
            return Mathf.Max(1, defaultNoteEffectPoolCount);
        }

        private void StopNoteEffectPools()
        {
            ParticleSystemController[] controllers = noteEffectPoolRoot != null
                ? noteEffectPoolRoot.GetComponentsInChildren<ParticleSystemController>(true)
                : null;

            for (int i = 0; controllers != null && i < controllers.Length; i++)
            {
                controllers[i]?.Stop();
            }
        }

        private void PlayNoteSE(INote note)
        {
            NoteBase noteBase = note as NoteBase;
            if (noteBase != null && !noteBase.PlayJudgmentSound)
            {
                return;
            }
            if (note.Category == NoteCategory.SliderLong)
            {
                return;
            }

            string seName = LiveSoundDefine.SE_LIVE_PERFECT;
            if (note.Category == NoteCategory.SliderFlick) seName = LiveSoundDefine.SE_LIVE_FLICK;
            else if (note.Result == NoteResult.Great) seName = LiveSoundDefine.SE_LIVE_GREAT;

            PlaySE(seName);
        }

        private void PlaySE(string seName)
        {
            if (string.IsNullOrEmpty(seName) || soundPlayer == null) return;
            soundPlayer.PlayIngameSEOneShot(seName, 1f);
        }

        private static int GetFixedLaneIndex(int lane)
        {
            if (lane >= 4 && lane <= 5) return 0;
            if (lane >= 6 && lane <= 7) return 1;
            if (lane >= 8 && lane <= 9) return 2;
            if (lane >= 10 && lane <= 11) return 3;
            return -1;
        }

        private static Vector3 GetNoteCenterPosition(INote note)
        {
            float lane = (note.LaneStartF + note.LaneEndF) * 0.5f;
            Vector2[] positions = LiveConfig.JudgmentPositions;
            if (positions == null || positions.Length == 0)
            {
                return new Vector3(LiveUtility.CalcLaneTransformX(lane), LiveConfig.JudgmentPositionY, 0f);
            }

            float clampedLane = Mathf.Clamp(lane, 0f, positions.Length - 1);
            int start = Mathf.FloorToInt(clampedLane);
            int end = Mathf.CeilToInt(clampedLane);
            float t = Mathf.Clamp01(clampedLane - start);
            Vector2 position = Vector2.Lerp(positions[start], positions[end], t);
            return new Vector3(position.x, position.y, 0f);
        }

        private void ApplyNoteEffectWidth(Transform effectRoot, INote note)
        {
            if (effectRoot == null || note == null)
            {
                return;
            }

            float width = Mathf.Max(1f, note.LaneEndF - note.LaneStartF + 1f);
            ApplyChildWidth(effectRoot.Find(AuraChildPath), width);
            ApplyChildWidth(effectRoot.Find(BaseChildPath), width);
        }

        private void ApplyChildWidth(Transform target, float width)
        {
            if (target == null)
            {
                return;
            }

            Vector3 initialScale = GetInitialScale(target);
            target.localScale = new Vector3(initialScale.x * width, initialScale.y, initialScale.z);
        }

        private Vector3 GetInitialScale(Transform target)
        {
            if (noteEffectInitialScales == null)
            {
                noteEffectInitialScales = new Dictionary<Transform, Vector3>();
            }

            if (!noteEffectInitialScales.TryGetValue(target, out Vector3 initialScale))
            {
                initialScale = target.localScale;
                noteEffectInitialScales[target] = initialScale;
            }
            return initialScale;
        }

        private void ConfigureParticleSystems(GameObject instance)
        {
            if (instance == null || !forceLocalSimulationSpace) return;

            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem == null) continue;

                ParticleSystem.MainModule main = particleSystem.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        private void ConfigureParticleRenderers(GameObject instance)
        {
            if (instance == null) return;

            ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; renderers != null && i < renderers.Length; i++)
            {
                ParticleSystemRenderer renderer = renderers[i];
                if (renderer == null) continue;

                if (forceLocalParticleAlignment)
                {
                    renderer.alignment = ParticleSystemRenderSpace.Local;
                }

                Material[] materials = renderer.materials;
                for (int j = 0; materials != null && j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material != null && material.HasProperty(UseFlipMatrixPropertyName))
                    {
                        material.SetFloat(UseFlipMatrixPropertyName, 0f);
                    }
                }
                renderer.materials = materials;
            }
        }
    }
}
