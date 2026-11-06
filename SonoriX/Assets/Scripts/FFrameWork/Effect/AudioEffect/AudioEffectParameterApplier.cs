namespace FFrameWork.Live
{
    // FMOD パラメータの反映先を選択して値を適用する
    public sealed class AudioEffectParameterApplier
    {
        private BaseLiveController liveController;

        // ライブイベントへ直接パラメータを設定できる Controller を設定する
        public void SetLiveController(BaseLiveController liveController)
        {
            this.liveController = liveController;
        }

        // ライブイベント経由を優先し、失敗時は FMOD グローバルパラメータへ直接反映する
        public bool SetValue(FmodAudioEffectSystem system, string parameterName, float value)
        {
            if (system == null)
            {
                return false;
            }

            if (liveController != null && liveController.TrySetMusicEventParameter(parameterName, value))
            {
                system.CacheParameter(parameterName, value);
                return true;
            }

            return system.SetParameter(parameterName, value);
        }
    }
}
