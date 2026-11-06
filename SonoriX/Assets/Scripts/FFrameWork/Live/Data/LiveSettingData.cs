namespace FFrameWork.Live
{
    // ライブ中のプレイヤー設定
    public class LiveSettingData
    {
        // レーン表示の透明度
        public float LaneTransparent { get; set; } = 1f;

        // All Perfect 演出を使用するかどうか
        public bool UseAllPerfectEffect { get; set; } = true;

        // 振動を使用するかどうか
        public bool UseVibration { get; set; } = false;

        // 120FPS 動作を使用するかどうか
        public bool Use120FPS { get; set; } = true;

        // VSync を使用するかどうか
        public bool? UseVSync { get; set; } = true;

        // ノート速度
        public float NoteSpeed { get; set; } = LivePlayerSettings.NoteSpeed;

        // タイミング調整値
        public float TimingAdjustData { get; set; }

        // 画面の明るさ
        public float Brightness { get; set; } = 1f;
    }
}
