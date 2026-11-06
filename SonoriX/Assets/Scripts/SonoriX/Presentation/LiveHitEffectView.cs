using TMPro;
using UnityEngine;

namespace SonoriX.Presentation
{
    public sealed class LiveHitEffectView  : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text effectText;

        private void Awake()
        {
            if(rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        public void Show(Vector2 position, string text)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
            }

            if (effectText != null)
            {
                effectText.text = text;
                effectText.enabled = true;
            }
        }

        public void Hide()
        {
            if (effectText != null)
            {
                effectText.enabled = false;
            }
        }
    }
}