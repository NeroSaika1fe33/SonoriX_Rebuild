using SonoriX.App;
using UnityEngine;

namespace SonoriX.Presentation
{
    public sealed class LivePresentationRoot : MonoBehaviour
    {
        [SerializeField] private LiveSceneController liveSceneController;
        [SerializeField] private LiveNoteLayout liveNoteLayout = new LiveNoteLayout();
        
        public LiveSceneController LiveSceneController => liveSceneController;
        public LiveNoteLayout LiveNoteLayout => liveNoteLayout;

        private void Awake()
        {
            if (liveSceneController == null)
            {
                liveSceneController = GetComponentInParent<LiveSceneController>();
            }
        }
    }
}