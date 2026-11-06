using SonoriX.App;
using SonoriX.Runtime;
using UnityEngine;

namespace SonoriX.Log
{
    //日志
    public sealed class LiveFrameLogger : MonoBehaviour
    {
        [SerializeField] private LiveSceneController liveSceneController;

        private void Awake()
        {
            if (liveSceneController == null)
            {
                liveSceneController = GetComponent<LiveSceneController>();
            }
        }

        private void OnEnable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.JudgmentProduced += LogJudgment;
                liveSceneController.ScoreUpdated += LogScore;
            }
        }

        private void OnDisable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.JudgmentProduced -= LogJudgment;
                liveSceneController.ScoreUpdated -= LogScore;
            }
        }
        
        private void LogJudgment(JudgmentResult result)
        {
            string point = result.IsHoldPoint
                ? $"Hold[{result.HoldPointIndex}]"
                : result.PointRole.ToString();

            Debug.Log(
                $"Note:{result.Note.Id} " +
                $"Point:{point} " +
                $"Result:{result.Judgment} " +
                $"Complete:{result.CompletesNote}");
        }

        private void LogScore(LiveScore score)
        {
            Debug.Log($"Score:{score.TotalScore} Combo:{score.Combo}");
        }
    }
}