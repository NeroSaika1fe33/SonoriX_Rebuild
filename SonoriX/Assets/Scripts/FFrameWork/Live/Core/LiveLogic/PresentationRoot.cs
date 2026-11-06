namespace FFrameWork.Live
{
    internal sealed class PresentationRoot
    {
        private readonly LiveViewPresenter viewPresenter = new LiveViewPresenter();
        private readonly LiveEffectPresenter effectPresenter = new LiveEffectPresenter();

        public void SetLiveViews(LiveViewBase[] liveViews)
        {
            viewPresenter.SetLiveViews(liveViews);
        }

        public void SetAudioEffectController(AudioEffectController controller)
        {
            effectPresenter.SetAudioEffectController(controller);
        }

        public void CreateNotePool(MusicScore musicScore, float noteDisplayTimeOffset)
        {
            viewPresenter.CreateNotePool(musicScore, noteDisplayTimeOffset);
        }

        public void ChangeScoreCalculator(ScoreGaugeCalculator calculator)
        {
            viewPresenter.ChangeScoreCalculator(calculator);
        }

        public void SetupScore(LiveScore score)
        {
            viewPresenter.SetupScore(score);
        }

        public void UpdateScore(LiveScore score, int addScore)
        {
            viewPresenter.UpdateScore(score, addScore);
        }

        public void UpdateClearGauge(int clearGauge)
        {
            viewPresenter.UpdateClearGauge(clearGauge);
        }

        public void UpdateCombo(LiveScore score)
        {
            viewPresenter.UpdateCombo(score);
        }

        public void SpawnNote(INote note)
        {
            viewPresenter.SpawnNote(note);
        }

        public void UnspawnNote(INote note)
        {
            viewPresenter.UnspawnNote(note);
        }

        public void JudgmentNote(INote note)
        {
            viewPresenter.JudgmentNote(note);
        }

        public void Unpicked(int lane, ref LiveInput input)
        {
            viewPresenter.Unpicked(lane, ref input);
        }

        public void StopAllAudio()
        {
            effectPresenter.StopAllAudio();
        }

        public bool BeginSliderLongEffect()
        {
            return effectPresenter.BeginSliderLongEffect();
        }

        public void UpdateSliderLongEffect()
        {
            effectPresenter.UpdateSliderLongEffect();
        }

        public void EndSliderLongEffect()
        {
            effectPresenter.EndSliderLongEffect();
        }

        public bool BeginFreeLaneLongEffect()
        {
            return effectPresenter.BeginFreeLaneLongEffect();
        }

        public void UpdateFreeLaneLongEffect()
        {
            effectPresenter.UpdateFreeLaneLongEffect();
        }

        public void EndFreeLaneLongEffect()
        {
            effectPresenter.EndFreeLaneLongEffect();
        }

        public bool BeginVolumeLongEffect(float direction)
        {
            return effectPresenter.BeginVolumeLongEffect(direction);
        }

        public void UpdateVolumeLongEffect(float sizeDelta)
        {
            effectPresenter.UpdateVolumeLongEffect(sizeDelta);
        }

        public void EndVolumeLongEffect()
        {
            effectPresenter.EndVolumeLongEffect();
        }
    }
}
