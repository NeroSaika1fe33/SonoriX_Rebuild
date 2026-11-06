using System;
using System.Collections;
using UnityEngine;

namespace FFrameWork.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    // 左右のパネルで画面を覆って切り替えるワイプ遷移
    public class PanelTransitionWipeController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform leftImage;
        [SerializeField] private RectTransform rightImage;

        [Header("Animation Settings")]
        [SerializeField] private float hiddenX = 1920f;        
        [SerializeField] private float enterDuration = 0.2f;   
        [SerializeField] private float coverHoldSeconds = 0.5f;
        [SerializeField] private float exitDuration = 0.2f;    

        // 遷移中の入力ブロックに使う CanvasGroup
        private CanvasGroup canvasGroup;
        // 実行中のワイプ Coroutine
        private Coroutine transitionCoroutine;

        // 現在ワイプ再生中か
        public bool IsPlaying => transitionCoroutine != null;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            SetCoverProgress(0f); 
        }

        // 画面が覆われたタイミングでコールバックを実行する
        public void Play(Action onCovered)
        {
            if (leftImage == null || rightImage == null)
            {
                onCovered?.Invoke();
                return;
            }

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(PlayRoutine(onCovered));
        }

        // 入場、覆い保持、退場を順に再生する
        private IEnumerator PlayRoutine(Action onCovered)
        {
            canvasGroup.blocksRaycasts = true;

            yield return MoveRoutine(0f, 1f, enterDuration);

            onCovered?.Invoke();
            if (coverHoldSeconds > 0f)
            {
                yield return TimerWait(coverHoldSeconds);
            }

            yield return MoveRoutine(1f, 0f, exitDuration);

            canvasGroup.blocksRaycasts = false;
            transitionCoroutine = null;
        }

        // 0 から 1 の進捗で左右パネルを移動する
        private IEnumerator MoveRoutine(float startProgress, float endProgress, float duration)
        {
            if (duration <= 0f)
            {
                SetCoverProgress(endProgress);
                yield break;
            }

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                t = t * t * (3f - 2f * t);
                
                SetCoverProgress(Mathf.Lerp(startProgress, endProgress, t));
                yield return null;
            }

            SetCoverProgress(endProgress);
        }

        // unscaled time で指定秒数待つ
        private IEnumerator TimerWait(float seconds)
        {
            float timer = 0f;
            while (timer < seconds)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ワイプの開閉進捗を左右パネル位置へ反映する
        private void SetCoverProgress(float progress)
        {
            float currentX = Mathf.Lerp(hiddenX, 0f, progress);

            if (leftImage != null)
            {
                leftImage.anchoredPosition = new Vector2(-currentX, leftImage.anchoredPosition.y);
            }

            if (rightImage != null)
            {
                rightImage.anchoredPosition = new Vector2(currentX, rightImage.anchoredPosition.y);
            }
        }
    }
}
