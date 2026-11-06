namespace FFrameWork.Live
{
    public interface IMusicTimer
    {
        // 現在の再生時刻ミリ秒
        int PlaybackTime { get; }

        // タイマーの基準時刻をリセットする
        // playbackTimeMs: 初期再生時刻ミリ秒
        // unityTime: リセット時の Unity 時刻
        void Reset(int playbackTimeMs, float unityTime);

        // 現在時刻を更新する
        // unityTime: 現在の Unity 時刻
        void Execute(float unityTime);
    }
}
