using FMOD;
using FMOD.Studio;
using UnityEngine;

namespace FFrameWork.Live
{
    public sealed class FmodSyncedUnityTimer : IMusicTimer
    {
        // 同期元の FMOD EventInstance
        private readonly EventInstance eventInstance;

        // 現在の再生時刻ミリ秒
        private int playbackTimeMs = -1;

        // 現在の再生時刻ミリ秒
        public int PlaybackTime
        {
            get { return playbackTimeMs; }
        }

        // 同期元 FMOD EventInstance を指定してタイマーを作成する
        // eventInstance: 再生時刻の参照元
        public FmodSyncedUnityTimer(EventInstance eventInstance)
        {
            this.eventInstance = eventInstance;
        }

        // 再生時刻をリセットし、FMOD タイムライン位置も同期する
        // playbackTimeMs: 初期再生時刻ミリ秒
        // unityTime: 互換用の Unity 時刻
        public void Reset(int playbackTimeMs, float unityTime)
        {
            this.playbackTimeMs = Mathf.Max(playbackTimeMs, 0);
            if (eventInstance.isValid())
            {
                eventInstance.setTimelinePosition(this.playbackTimeMs);
            }
        }

        // FMOD タイムライン位置から再生時刻を更新する
        // unityTime: 互換用の Unity 時刻
        public void Execute(float unityTime)
        {
            playbackTimeMs = GetAudioTimeMs();
        }

        // FMOD EventInstance から現在時刻をミリ秒で取得する
        private int GetAudioTimeMs()
        {
            if (!eventInstance.isValid())
            {
                return playbackTimeMs;
            }

            RESULT result = eventInstance.getTimelinePosition(out int timelinePosition);
            return result == RESULT.OK
                ? Mathf.Max(timelinePosition, 0)
                : playbackTimeMs;
        }
    }
}
