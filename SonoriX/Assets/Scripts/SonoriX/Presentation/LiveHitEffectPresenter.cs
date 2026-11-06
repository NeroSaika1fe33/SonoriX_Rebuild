using SonoriX.App;
using SonoriX.Runtime;
using UnityEngine;

namespace SonoriX.Presentation
{
    public sealed class LiveHitEffectPresenter : MonoBehaviour
    {
        [SerializeField] private LivePresentationRoot presentationRoot;
        [SerializeField] private LiveHitEffectView effectView;
        [SerializeField] private float visibleSeconds = 0.25f;

        private LiveSceneController liveSceneController;
        private LiveNoteLayout noteLayout;
        private float hideAt;
        private bool isShowing;

        private void Awake()
        {
            ResolveDependencies();

            if (effectView != null)
            {
                effectView.Hide();
            }
        }

        private void OnEnable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.JudgmentProduced += ShowEffect;
            }
        }

        private void OnDisable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.JudgmentProduced -= ShowEffect;
            }
        }

        private void Update()
        {
            if (!isShowing)
            {
                return;
            }

            if (Time.time < hideAt)
            {
                return;
            }

            if (effectView != null)
            {
                effectView.Hide();
            }

            isShowing = false;
        }

        private void ShowEffect(JudgmentResult result)
        {
            if (effectView == null || noteLayout == null)
            {
                return;
            }

            Vector2 position = noteLayout.CalculateJudgeLinePosition(result.Note);
            effectView.Show(position, result.Judgment.ToString());
            hideAt = Time.time + visibleSeconds;
            isShowing = true;
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
            noteLayout = presentationRoot.LiveNoteLayout;
        }
    }
}