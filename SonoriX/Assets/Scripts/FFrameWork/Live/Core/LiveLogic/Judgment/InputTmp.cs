using System.Collections.Generic;

namespace FFrameWork.Live
{
    internal sealed class InputTmp
    {
        public LiveInput liveInput;
        public float Lane;
        public readonly List<NoteBase> CandidateNotes = new List<NoteBase>(4);

        public InputTmp()
        {
            liveInput = default;
        }
    }
}
