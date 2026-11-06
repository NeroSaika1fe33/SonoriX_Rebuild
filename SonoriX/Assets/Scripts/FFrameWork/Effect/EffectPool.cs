using System;
using System.Collections.Generic;
using FFrameWork;
using UnityEngine;

namespace FFrameWork.Live
{
    // ParticleSystemController を事前生成して再利用するエフェクトプール
    public class EffectPool : MonoBehaviour
    {
        // プール生成に使う元プレハブ
        private GameObject prefab;

        // パーティクル更新登録先のライブコントローラー
        private BaseLiveController liveController;

        // 生成直後のインスタンスへ追加設定を適用する処理
        private Action<GameObject> configureInstance;

        // 再利用対象のエフェクト一覧
        private List<ParticleSystemController> pool;

        // 同一フレーム内で同じインスタンスを再利用しないための記録
        private Dictionary<ParticleSystemController, int> lastSpawnFrameByController;

        // 次に探索を始めるプール位置
        private int spawnIndex;

        // プレハブとプール数を指定してエフェクトプールを構築する
        public void Setup(GameObject prefab, int poolCount, BaseLiveController liveController = null, Action<GameObject> configureInstance = null)
        {
            ClearChildren();
            this.prefab = prefab;
            this.liveController = liveController;
            this.configureInstance = configureInstance;

            if (prefab == null || poolCount <= 0)
            {
                pool = null;
                return;
            }

            pool = new List<ParticleSystemController>(poolCount);
            lastSpawnFrameByController = new Dictionary<ParticleSystemController, int>(poolCount);
            for (int i = 0; i < poolCount; i++)
            {
                CreateInstance();
            }
            spawnIndex = 0;

            // Original warms the first pooled instance once, which also primes particle renderers/material state.
            pool[0]?.Play();
            pool[0]?.Stop();
        }

        // 再生可能なエフェクトを取得し、足りなければ追加生成する
        public ParticleSystemController Spawn()
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            int startIndex = spawnIndex;
            for (int i = 0; i < pool.Count; i++)
            {
                int index = (startIndex + i) % pool.Count;
                ParticleSystemController candidate = pool[index];
                if (candidate == null || IsBusy(candidate))
                {
                    continue;
                }

                spawnIndex = (index + 1) % pool.Count;
                MarkSpawned(candidate);
                return candidate;
            }

            ParticleSystemController created = CreateInstance();
            spawnIndex = pool.Count == 0 ? 0 : spawnIndex % pool.Count;
            MarkSpawned(created);
            return created;
        }

        // プレハブから 1 件分のエフェクトインスタンスを生成する
        private ParticleSystemController CreateInstance()
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, transform, false);
            instance.name = prefab.name;
            configureInstance?.Invoke(instance);

            ParticleSystemController controller = instance.GetComponent<ParticleSystemController>() ?? instance.AddComponent<ParticleSystemController>();
            controller.RegisterToLiveController(liveController);
            controller.Stop();
            pool?.Add(controller);
            return controller;
        }

        // 再生中または同一フレームで使用済みなら使用中として扱う
        private bool IsBusy(ParticleSystemController controller)
        {
            if (controller == null)
            {
                return true;
            }
            return controller.IsPlaying ||
                (lastSpawnFrameByController != null &&
                 lastSpawnFrameByController.TryGetValue(controller, out int frame) &&
                 frame == Time.frameCount);
        }

        // 使用したフレームを記録する
        private void MarkSpawned(ParticleSystemController controller)
        {
            if (controller == null)
            {
                return;
            }
            if (lastSpawnFrameByController == null)
            {
                lastSpawnFrameByController = new Dictionary<ParticleSystemController, int>();
            }
            lastSpawnFrameByController[controller] = Time.frameCount;
        }

        // 既存の子オブジェクトを破棄してプールを作り直せる状態にする
        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
