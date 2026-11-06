using System.Collections;
using UnityEngine;

namespace FFrameWork.Live
{
    public class CountdownView : MonoBehaviour
    {
        // カウントダウン表示に使う子オブジェクト配列
        private GameObject[] countdownParticleArray;

        // 実行中のカウントダウン Coroutine
        private IEnumerator coroutine;

        // カウントダウン Animator の初期再生ステート
        private static readonly int DefaultStateHash = Animator.StringToHash("clip_live_countdown_1");

        // 子オブジェクトをカウントダウン要素として収集し、初期状態では非表示にする
        public void Setup()
        {
            countdownParticleArray = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                countdownParticleArray[i] = child;
                child.SetActive(false);
            }
        }

        // カウントダウンを先頭から開始する
        public void StartCountdown()
        {
            StopCountdown();
            coroutine = Countdown();
            StartCoroutine(coroutine);
        }

        // 実行中のカウントダウンを停止して表示をリセットする
        public void StopCountdown()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
            ResetCountdown();
        }

        // 3、2、1 相当の表示を 1 秒間隔で再生する
        private IEnumerator Countdown()
        {
            for (int i = 2; i >= 0; i--)
            {
                if (countdownParticleArray != null && i < countdownParticleArray.Length && countdownParticleArray[i] != null)
                {
                    PlayCountdownObject(countdownParticleArray[i]);
                }
                yield return new WaitForSeconds(1f);
            }

            ResetCountdown();
            coroutine = null;
        }

        // すべてのカウントダウン要素を非表示に戻す
        private void ResetCountdown()
        {
            if (countdownParticleArray == null)
            {
                return;
            }

            for (int i = 0; i < countdownParticleArray.Length; i++)
            {
                if (countdownParticleArray[i] != null)
                {
                    countdownParticleArray[i].SetActive(false);
                }
            }
        }

        // 指定したカウントダウン要素の Animator と ParticleSystem を先頭から再生する
        // countdownObject: 再生対象のカウントダウン要素
        private static void PlayCountdownObject(GameObject countdownObject)
        {
            countdownObject.SetActive(true);

            Animator[] animators = countdownObject.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(DefaultStateHash, 0, 0f);
            }

            ParticleSystem[] particles = countdownObject.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Clear(true);
                particle.Play(true);
            }
        }
    }
}
