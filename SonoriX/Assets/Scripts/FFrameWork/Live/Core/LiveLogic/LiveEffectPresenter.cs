namespace FFrameWork.Live
{
    // LiveLogic から音声エフェクト制御への呼び出しを集約する
    internal sealed class LiveEffectPresenter
    {
        private AudioEffectController audioEffectController;

        public void SetAudioEffectController(AudioEffectController controller)
        {
            audioEffectController = controller;
        }

        public void StopAllAudio()
        {
            audioEffectController?.StopAll();
        }

        public bool BeginSliderLongEffect()
        {
            return audioEffectController != null && audioEffectController.BeginSliderLongEffect();
        }

        public void UpdateSliderLongEffect()
        {
            audioEffectController?.UpdateSliderLongEffect();
        }

        public void EndSliderLongEffect()
        {
            audioEffectController?.EndSliderLongEffect();
        }

        public bool BeginFreeLaneLongEffect()
        {
            return audioEffectController != null && audioEffectController.BeginFreeLaneLongEffect();
        }

        public void UpdateFreeLaneLongEffect()
        {
            audioEffectController?.UpdateFreeLaneLongEffect();
        }

        public void EndFreeLaneLongEffect()
        {
            audioEffectController?.EndFreeLaneLongEffect();
        }

        public bool BeginVolumeLongEffect(float direction)
        {
            return audioEffectController != null && audioEffectController.BeginVolumeLongEffect(direction);
        }

        public void UpdateVolumeLongEffect(float sizeDelta)
        {
            audioEffectController?.UpdateVolumeLongEffect(sizeDelta);
        }

        public void EndVolumeLongEffect()
        {
            audioEffectController?.EndVolumeLongEffect();
        }
    }
}
