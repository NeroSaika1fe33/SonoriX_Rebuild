using System;

public enum EventState
{
    // 発火待ちのイベント状態
    Wait = 0,

    // すでに発火済みのイベント状態
    Done = 1
}

namespace FFrameWork.Live
{
    public class EventBase
    {
        // イベントを発火させる基準となる譜面時間情報
        public MusicScoreInfo MusicScoreInfo { get; protected set; }

        // 現在のイベント処理状態
        public EventState State { get; protected set; }

        // 発火予定時刻から実際に処理された時刻までの差分
        public float OffsetJudgeTime { get; protected set; }

        // 譜面時間情報を保持してイベントを発火待ち状態で初期化する
        // info: イベントの基準になる譜面時間情報
        public EventBase(MusicScoreInfo info)
        {
            MusicScoreInfo = info;
            State = EventState.Wait;
        }

        // 現在の譜面時間が基準時間に到達したら一度だけコールバックを呼ぶ
        // currentFrameInfo: 現在フレームの譜面時間情報
        // resultCallback: イベント発火時に呼び出す通知先
        public virtual void Update(MusicScoreInfo currentFrameInfo, Action<EventBase> resultCallback)
        {
            if (State == EventState.Done)
            {
                return;
            }
            if (MusicScoreInfo.time <= currentFrameInfo.time)
            {
                State = EventState.Done;
                OffsetJudgeTime = currentFrameInfo.time - MusicScoreInfo.time;
                resultCallback?.Invoke(this);
            }
        }
    }
}
