using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // AudioEffectType ごとに FmodAudioEffectSystem を貸し出す小さなプール
    public class AudioEffectPool
    {
        // プール内の FMOD エフェクト枠
        private readonly List<Entry> entries = new List<Entry>();

        // 1 つの FMOD エフェクト枠の使用状態
        private class Entry
        {
            // 実際にパラメータを反映するシステム
            public FmodAudioEffectSystem System;
            // 現在この枠を使用しているエフェクト種別
            public AudioEffectType Owner;
            // 使用中かどうか
            public bool InUse;
        }

        // プールに登録されている枠数
        public int Capacity => entries.Count;
        // 現在使用中の枠数
        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].InUse)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // 初期システム一覧からプールを作り直す
        public void Setup(IEnumerable<FmodAudioEffectSystem> initialSystems)
        {
            entries.Clear();
            if (initialSystems != null)
            {
                foreach (FmodAudioEffectSystem system in initialSystems)
                {
                    if (system == null || Contains(system))
                    {
                        continue;
                    }

                    entries.Add(new Entry
                    {
                        System = system,
                        Owner = AudioEffectType.Free,
                        InUse = false
                    });
                }
            }
        }

        // 指定エフェクト用のシステムを取得し、未使用枠を使用中にする
        public FmodAudioEffectSystem Acquire(AudioEffectType owner)
        {
            FmodAudioEffectSystem activeSystem = GetActive(owner);
            if (activeSystem != null)
            {
                return activeSystem;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.System == null || entry.InUse)
                {
                    continue;
                }

                entry.Owner = owner;
                entry.InUse = true;
                entry.System.Play();
                return entry.System;
            }

            return null;
        }

        // 指定エフェクトで使用中のシステムを取得する
        public FmodAudioEffectSystem GetActive(AudioEffectType owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.System != null && entry.InUse && entry.Owner == owner)
                {
                    return entry.System;
                }
            }

            return null;
        }

        // 登録済みの任意のシステムを取得する
        public FmodAudioEffectSystem GetAny()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].System != null)
                {
                    return entries[i].System;
                }
            }

            return null;
        }

        // 互換用に任意のシステムを返す
        public FmodAudioEffectSystem GetSystem()
        {
            return GetAny();
        }

        // 指定エフェクトが使用している枠を停止して解放する
        public void Release(AudioEffectType owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.System == null || !entry.InUse || entry.Owner != owner)
                {
                    continue;
                }

                entry.System.Stop();
                entry.Owner = AudioEffectType.Free;
                entry.InUse = false;
                return;
            }
        }

        // すべての枠を停止して未使用へ戻す
        public void StopAll()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.System != null)
                {
                    entry.System.Stop();
                }

                entry.Owner = AudioEffectType.Free;
                entry.InUse = false;
            }
        }

        // 同じシステムがすでに登録済みかを返す
        private bool Contains(FmodAudioEffectSystem system)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].System == system)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
