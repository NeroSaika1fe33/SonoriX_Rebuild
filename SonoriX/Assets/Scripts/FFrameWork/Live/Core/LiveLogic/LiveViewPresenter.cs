using UnityEngine;

namespace FFrameWork.Live
{
    // LiveView への表示更新を集約する
    internal sealed class LiveViewPresenter
    {
        private LiveViewBase[] liveViews;

        public void SetLiveViews(LiveViewBase[] liveViews)
        {
            this.liveViews = liveViews;
        }

        public void CreateNotePool(MusicScore musicScore, float noteDisplayTimeOffset)
        {
            if (liveViews == null)
            {
                return;
            }

            var notePoolCount = LiveUtility.CalculateMaxSimultaneousNotes(musicScore, noteDisplayTimeOffset);
            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.CreateNotePool(notePoolCount);
            }
        }

        public void ChangeScoreCalculator(ScoreGaugeCalculator calculator)
        {
            liveViews.ChangeScoreCalculator(calculator);
        }

        public void SetupScore(LiveScore score)
        {
            liveViews.SetupScore(score);
        }

        public void UpdateScore(LiveScore score, int addScore)
        {
            liveViews.UpdateScore(ref score, addScore);
        }

        public void UpdateClearGauge(int clearGauge)
        {
            liveViews.UpdateClearGauge(clearGauge);
        }

        public void UpdateCombo(LiveScore score)
        {
            liveViews.UpdateCombo(score);
        }

        public void SpawnNote(INote note)
        {
            ForEachLiveView(view => view.SpawnNote(note));
        }

        public void UnspawnNote(INote note)
        {
            ForEachLiveView(view => view.UnspawnNote(note));
        }

        public void JudgmentNote(INote note)
        {
            liveViews.JudgmentNote(note);
        }

        public void Unpicked(int lane, ref LiveInput input)
        {
            liveViews.Unpicked(lane, ref input);
        }

        private void ForEachLiveView(System.Action<LiveViewBase> action)
        {
            if (liveViews == null || action == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    action(liveViews[i]);
                }
            }
        }
    }
}
