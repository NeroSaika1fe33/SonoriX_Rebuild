using UnityEngine;
using TMPro;

namespace SonoriX.Presentation
{
    //判定View
    public sealed class LiveJudgmentView : MonoBehaviour
    {
        [SerializeField] private TMP_Text judgmentText;
        [SerializeField] private TMP_Text descriptorText;

        //设置判定Text
        public void Show(string judgment, string descriptor)
        {
            if (judgmentText != null)
            {
                judgmentText.text = judgment;
            }

            if (descriptorText != null)
            {
                descriptorText.text = descriptor;
            }
        }

        //清理判定Text
        public void Clear()
        {
            if (judgmentText != null)
            {
                judgmentText.text = string.Empty;
            }

            if (descriptorText != null)
            {
                descriptorText.text = string.Empty;
            }
        }
    }
}