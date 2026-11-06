using System;

namespace FFrameWork.Live
{
    internal sealed class ScoreSystem
    {
        private readonly LiveBundleBuildData liveBundleBuildData;
        private readonly PresentationRoot presentationRoot;
        private ScoreLogic scoreLogic;

        public ScoreSystem(LiveBundleBuildData liveBundleBuildData, PresentationRoot presentationRoot)
        {
            this.liveBundleBuildData = liveBundleBuildData;
            this.presentationRoot = presentationRoot;
        }

        public LiveScore Score => scoreLogic?.Score ?? default;
        public bool IsAllPerfectCombo => scoreLogic != null && scoreLogic.IsAllPerfectCombo;
        public bool IsPerfectCombo => scoreLogic != null && scoreLogic.IsPerfectCombo;
        public bool IsClearGauge => scoreLogic != null && scoreLogic.IsClearGauge;

        public void Setup(LiveBootDataBase bootData, NoteBase[] notes)
        {
            SetScoreLogic(new ScoreLogic(liveBundleBuildData), bootData, notes);
        }

        public void SetScoreLogic(ScoreLogic scoreLogic, LiveBootDataBase bootData, NoteBase[] notes)
        {
            DetachScoreLogicEvents(this.scoreLogic);
            this.scoreLogic = scoreLogic;
            AttachScoreLogicEvents(this.scoreLogic);
            this.scoreLogic?.Setup(bootData, notes, Array.Empty<EventBase>());
        }

        public void Dispose()
        {
            DetachScoreLogicEvents(scoreLogic);
            scoreLogic = null;
        }

        public void Continue(float time)
        {
            scoreLogic?.Continue(time);
        }

        public void ConfigurePlaybackNote(NoteBase note)
        {
            if (note == null)
            {
                return;
            }

            note.OnUpdateScore = noteInfo => scoreLogic?.CalculateScore(noteInfo);
            note.OnUpdateCombo = noteInfo => scoreLogic?.UpdateCombo(noteInfo);
            note.OnDamage = noteInfo => scoreLogic?.Damage(noteInfo);
            note.OnUpdateResult = resultTuple => scoreLogic != null ? scoreLogic.UpdateResult(resultTuple) : resultTuple;
        }

        public void UpdateNoteResult(NoteBase note)
        {
            scoreLogic?.UpdateNoteResult(note);
        }

        private void AttachScoreLogicEvents(ScoreLogic logic)
        {
            if (logic == null)
            {
                return;
            }

            logic.OnScoreCalculatorChanged += HandleScoreCalculatorChanged;
            logic.OnScoreSetup += HandleScoreSetup;
            logic.OnScoreUpdated += HandleScoreUpdated;
            logic.OnClearGaugeUpdated += HandleClearGaugeUpdated;
            logic.OnComboUpdated += HandleComboUpdated;
            logic.OnNoteJudged += HandleScoreNoteJudged;
        }

        private void DetachScoreLogicEvents(ScoreLogic logic)
        {
            if (logic == null)
            {
                return;
            }

            logic.OnScoreCalculatorChanged -= HandleScoreCalculatorChanged;
            logic.OnScoreSetup -= HandleScoreSetup;
            logic.OnScoreUpdated -= HandleScoreUpdated;
            logic.OnClearGaugeUpdated -= HandleClearGaugeUpdated;
            logic.OnComboUpdated -= HandleComboUpdated;
            logic.OnNoteJudged -= HandleScoreNoteJudged;
        }

        private void HandleScoreCalculatorChanged(ScoreGaugeCalculator calculator)
        {
            presentationRoot.ChangeScoreCalculator(calculator);
        }

        private void HandleScoreSetup(LiveScore score)
        {
            presentationRoot.SetupScore(score);
        }

        private void HandleScoreUpdated(LiveScore score, int addScore)
        {
            presentationRoot.UpdateScore(score, addScore);
        }

        private void HandleClearGaugeUpdated(int clearGauge)
        {
            presentationRoot.UpdateClearGauge(clearGauge);
        }

        private void HandleComboUpdated(LiveScore score)
        {
            presentationRoot.UpdateCombo(score);
        }

        private void HandleScoreNoteJudged(INote note)
        {
            presentationRoot.JudgmentNote(note);
        }
    }
}
