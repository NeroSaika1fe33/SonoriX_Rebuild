using UnityEngine;

namespace FFrameWork.Live
{
    public class ScreenEffectView : MonoBehaviour
    {
        // ダメージ時に生成する画面エフェクト Prefab
        [SerializeField] private GameObject damageEffectPrefab;

		// ダメージエフェクトの ParticleSystem 制御
		private ParticleSystemController damageEffectController;

        // ダメージエフェクトを生成し、画面比率に合わせてスケールを調整する
        // liveController: エフェクト登録先のライブコントローラー
        public void Setup(BaseLiveController liveController = null)
        {
            if (damageEffectController == null && damageEffectPrefab != null)
            {
                GameObject instance = Instantiate(damageEffectPrefab, transform, false);
                damageEffectController = instance.GetComponent<ParticleSystemController>();
                if (damageEffectController == null)
                {
                    damageEffectController = instance.AddComponent<ParticleSystemController>();
                }
                damageEffectController.RegisterToLiveController(liveController);
                damageEffectController.Stop();
            }

            float screenAspect = Screen.height == 0 ? 1.7778f : (float)Screen.width / Screen.height;
            transform.localScale = Vector3.one * Mathf.Max(1f, 1.7778f / screenAspect);
        }

        // ノーツ判定に応じて画面エフェクトを再生する
        // note: 判定済みノーツ
        public void Excute(INote note)
        {
            if (note == null || note.Result == NoteResult.Pass)
            {
                return;
            }

            if (note.Result == NoteResult.Miss || note.Result == NoteResult.Bad)
            {
                damageEffectController?.Play();
            }
        }
    }
}
