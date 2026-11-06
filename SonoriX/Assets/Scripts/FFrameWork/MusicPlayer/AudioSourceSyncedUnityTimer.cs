using UnityEngine;

namespace FFrameWork.Live
{
    public sealed class AudioSourceSyncedUnityTimer : IMusicTimer
    {
        // AudioSource 時刻と Unity 時刻の許容差分
        private const int MaximumDiffMs = 62;

        // Unity 時刻ベースの補間を開始する再生時刻
        private const int StartTimeForUnityBasedUpdateMs = 100;

        // 秒からミリ秒へ変換する倍率
        private const float MillisecondsPerSecond = 1000f;

        // 同期元の AudioSource
        private readonly AudioSource audioSource;

        // 現在の再生時刻ミリ秒
        private int playbackTimeMs = -1;

        // Unity 時刻補間の基準になる AudioSource 時刻
        private int referenceAudioTimeMs = -1;

        // Unity 時刻補間の基準になる Unity 時刻
        private float referenceUnityTime = -1f;

        // AudioSource 側の時刻更新待ち状態か
        private bool isWaitingForAudioTimeUpdate;

        // 現在の再生時刻ミリ秒
        public int PlaybackTime
        {
            get { return playbackTimeMs; }
        }

        // 同期元 AudioSource を指定してタイマーを作成する
        // audioSource: 再生時刻の参照元
        public AudioSourceSyncedUnityTimer(AudioSource audioSource)
        {
            this.audioSource = audioSource;
        }

        // 再生時刻と Unity 時刻の基準をリセットする
        // playbackTimeMs: 初期再生時刻ミリ秒
        // unityTime: リセット時の Unity 時刻
        public void Reset(int playbackTimeMs, float unityTime)
        {
            this.playbackTimeMs = playbackTimeMs;
            referenceAudioTimeMs = playbackTimeMs >= StartTimeForUnityBasedUpdateMs ? playbackTimeMs : -1;
            referenceUnityTime = playbackTimeMs >= StartTimeForUnityBasedUpdateMs ? unityTime : -1f;
            isWaitingForAudioTimeUpdate = false;
        }

        // AudioSource 時刻と Unity 時刻補間を使って再生時刻を更新する
        // unityTime: 現在の Unity 時刻
        public void Execute(float unityTime)
        {
            int audioTimeMs = GetAudioTimeMs();
            if (audioTimeMs <= StartTimeForUnityBasedUpdateMs - 1)
            {
                playbackTimeMs = audioTimeMs;
                return;
            }

            if (referenceAudioTimeMs < 0)
            {
                referenceAudioTimeMs = audioTimeMs;
                referenceUnityTime = unityTime;
            }

            if (isWaitingForAudioTimeUpdate)
            {
                int previousPlaybackTimeMs = playbackTimeMs;
                referenceAudioTimeMs = audioTimeMs;
                referenceUnityTime = unityTime;
                if (audioTimeMs > previousPlaybackTimeMs)
                {
                    playbackTimeMs = audioTimeMs;
                    isWaitingForAudioTimeUpdate = false;
                }
                return;
            }

            int unityBasedTimeMs = referenceAudioTimeMs + (int)((unityTime - referenceUnityTime) * MillisecondsPerSecond);
            if (unityBasedTimeMs - audioTimeMs > MaximumDiffMs)
            {
                playbackTimeMs = audioTimeMs + MaximumDiffMs;
                isWaitingForAudioTimeUpdate = true;
            }
            else if (audioTimeMs - unityBasedTimeMs > MaximumDiffMs)
            {
                playbackTimeMs = audioTimeMs;
                referenceAudioTimeMs = audioTimeMs;
                referenceUnityTime = unityTime;
            }
            else
            {
                playbackTimeMs = unityBasedTimeMs;
            }
        }

        // AudioSource から現在時刻をミリ秒で取得する
        private int GetAudioTimeMs()
        {
            if (audioSource == null || audioSource.clip == null)
            {
                return playbackTimeMs;
            }

            int sampleTimeMs = audioSource.timeSamples > 0
                ? Mathf.FloorToInt((float)audioSource.timeSamples / audioSource.clip.frequency * MillisecondsPerSecond)
                : Mathf.FloorToInt(audioSource.time * MillisecondsPerSecond);
            return Mathf.Max(sampleTimeMs, 0);
        }
    }
}
