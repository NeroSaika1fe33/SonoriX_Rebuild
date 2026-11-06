using SonoriX.App;
using SonoriX.Runtime;
using UnityEngine;

namespace SonoriX.Presentation
{
    //判定显示
    public sealed class LiveJudgmentPresenter : MonoBehaviour
    {
        [SerializeField] private LivePresentationRoot presentationRoot;
        [SerializeField] private LiveJudgmentView judgmentView;
        
        private LiveSceneController liveSceneController;

        private void Awake()
        {
            ResolveDependencies();

            if (judgmentView != null)
            {
                judgmentView.Clear();
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
                liveSceneController.JudgmentProduced += ApplyJudgment;
            }
        }

        private void OnDisable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.JudgmentProduced -= ApplyJudgment;
            }
        }

        private void ApplyJudgment(JudgmentResult result)
        {
            if (judgmentView == null)
            {
                return;
            }
            
            judgmentView.Show(
                result.Judgment.ToString(),
                result.Descriptor.ToString()
                );
        }
    }
}