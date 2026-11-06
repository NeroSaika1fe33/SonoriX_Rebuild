using UnityEngine;
using TMPro;

namespace SonoriX.Presentation
{
    public sealed class LiveScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;

        public void Show(string score, string combo)
        {
            if (scoreText != null)
            {
                scoreText.text = score;
            }

            if (comboText != null)
            {
                comboText.text = combo;
            }
        }

        public void Clear()
        {
            if (scoreText != null)
            {
                scoreText.text = string.Empty;
            }

            if (comboText != null)
            {
                comboText.text = string.Empty;
            }
        }
    }
}