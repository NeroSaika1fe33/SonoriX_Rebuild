using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FFrameWork.Live
{
    public class ClearGaugeView : MonoBehaviour
    {
        [SerializeField] private Image gaugeFillImage;
        [SerializeField] private RectTransform clearMarker;
        [FormerlySerializedAs("lifeText")]
        [SerializeField] private Text clearGaugeText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color clearColor = new Color(0.2f, 1f, 0.35f, 1f);

        private int maxClearGauge = 100;
        private int clearGaugeThreshold = 70;
        private int currentClearGauge;
        private RectTransform gaugeFillRect;

        public void Setup(
            Action onPause = null,
            int maxClearGauge = -1,
            int warningClearGauge = -1,
            int clearGaugeThreshold = -1)
        {
            this.maxClearGauge = maxClearGauge > 0 ? maxClearGauge : 100;
            this.clearGaugeThreshold = clearGaugeThreshold > 0
                ? clearGaugeThreshold
                : Mathf.FloorToInt(this.maxClearGauge * 0.7f);

            CacheGaugeFillRect();
            SetupClearMarker();
            SetupPauseButton(onPause);
            Execute(0);
        }

        public void Clear()
        {
            Execute(0);
        }

        public void Execute(int clearGauge)
        {
            currentClearGauge = Mathf.Clamp(clearGauge, 0, maxClearGauge);
            float rate = maxClearGauge > 0 ? Mathf.Clamp01((float)currentClearGauge / maxClearGauge) : 0f;

            if (gaugeFillImage != null)
            {
                gaugeFillImage.color = currentClearGauge >= clearGaugeThreshold ? clearColor : normalColor;
            }
            SetGaugeFillRate(rate);

            if (clearGaugeText != null)
            {
                clearGaugeText.text = currentClearGauge.ToString() + " %";
            }
        }

        private void CacheGaugeFillRect()
        {
            if (gaugeFillImage == null)
            {
                return;
            }

            gaugeFillRect = gaugeFillImage.rectTransform;
            gaugeFillRect.anchorMin = new Vector2(0f, gaugeFillRect.anchorMin.y);
            gaugeFillRect.anchorMax = new Vector2(0f, gaugeFillRect.anchorMax.y);
            gaugeFillRect.pivot = new Vector2(0f, gaugeFillRect.pivot.y);
        }

        private void SetGaugeFillRate(float rate)
        {
            if (gaugeFillRect == null)
            {
                CacheGaugeFillRect();
            }
            if (gaugeFillRect == null || !(gaugeFillRect.parent is RectTransform parentRect))
            {
                return;
            }

            float width = parentRect.rect.width * Mathf.Clamp01(rate);
            Vector2 sizeDelta = gaugeFillRect.sizeDelta;
            sizeDelta.x = width;
            gaugeFillRect.sizeDelta = sizeDelta;
        }

        private void SetupClearMarker()
        {
            if (clearMarker == null)
            {
                return;
            }

            float rate = Mathf.Clamp01((float)clearGaugeThreshold / Mathf.Max(1, maxClearGauge));
            Vector2 anchorMin = clearMarker.anchorMin;
            Vector2 anchorMax = clearMarker.anchorMax;
            anchorMin.x = rate;
            anchorMax.x = rate;
            clearMarker.anchorMin = anchorMin;
            clearMarker.anchorMax = anchorMax;

            Vector2 position = clearMarker.anchoredPosition;
            position.x = 0f;
            clearMarker.anchoredPosition = position;
        }

        private void SetupPauseButton(Action onPause)
        {
            if (pauseButton == null)
            {
                return;
            }

            pauseButton.onClick.RemoveAllListeners();
            pauseButton.interactable = onPause != null;
            if (onPause != null)
            {
                pauseButton.onClick.AddListener(() => onPause());
            }
        }
    }
}
