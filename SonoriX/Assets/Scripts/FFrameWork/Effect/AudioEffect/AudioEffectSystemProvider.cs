using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // AudioEffectController が使用する FmodAudioEffectSystem を収集し、不足分を補う
    public sealed class AudioEffectSystemProvider
    {
        private readonly List<FmodAudioEffectSystem> systems = new List<FmodAudioEffectSystem>();

        // Inspector 設定、子要素、追加 Component の順で必要数のシステムを用意する
        public FmodAudioEffectSystem[] Ensure(
            Component owner,
            IEnumerable<FmodAudioEffectSystem> configuredSystems,
            int requiredCount)
        {
            systems.Clear();
            AddRange(configuredSystems);

            if (systems.Count == 0 && owner != null)
            {
                AddRange(owner.GetComponentsInChildren<FmodAudioEffectSystem>(true));
            }

            requiredCount = Mathf.Max(1, requiredCount);
            GameObject target = owner != null ? owner.gameObject : null;
            while (systems.Count < requiredCount && target != null)
            {
                systems.Add(target.AddComponent<FmodAudioEffectSystem>());
            }

            return systems.ToArray();
        }

        private void AddRange(IEnumerable<FmodAudioEffectSystem> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (FmodAudioEffectSystem system in source)
            {
                if (system == null || systems.Contains(system))
                {
                    continue;
                }

                systems.Add(system);
            }
        }
    }
}
