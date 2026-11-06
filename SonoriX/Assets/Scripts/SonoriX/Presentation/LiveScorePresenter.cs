using SonoriX.App;
using SonoriX.Runtime;
using UnityEngine;

namespace SonoriX.Presentation
{
    public sealed class LiveScorePresenter : MonoBehaviour
    {
        [SerializeField] private LivePresentationRoot presentationRoot;
        [SerializeField] private LiveScoreView scoreView;
        
        private LiveSceneController liveSceneController;

        private void Awake()
        {
            ResolveDependencies();

            if (scoreView != null)
            {
                scoreView.Clear();
            }
        }

        private void ResolveDependencies()
        {
            if (presentationRoot == null)
            {
                presentationRoot = GetComponentInParent<LivePresentationRoot>();
            }

            if (presentationRoot == null)
            {
                return;
            }
            liveSceneController = presentationRoot.LiveSceneController;
        }

        private void OnEnable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.ScoreUpdated += ApplyScore;
            }
        }

        private void OnDisable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.ScoreUpdated -= ApplyScore;
            }
        }
        
        private void ApplyScore(LiveScore score)
        {
            if (scoreView == null)
            {
                return;
            }

            scoreView.Show(
                score.TotalScore.ToString(),
                score.Combo.ToString());
        }
    }
}