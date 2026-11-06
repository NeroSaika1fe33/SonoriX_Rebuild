using UnityEngine;
using FFrameWork;
namespace FFrameWork.Live
{
    // ライブ入力のフェーズ
    public enum LiveInputPhase
    {
        // 入力開始
        Began,
        // 入力継続または移動
        Moved,
        // 入力終了
        Ended,
        // 入力キャンセル
        Canceled
    }

    // ライブ入力の発生元
    public enum LiveInputSource
    {
        // 固定レーン入力
        FixedLane,
        // 矢印位置入力
        Arrow,
        // 矢印サイズ入力
        ArrowVolume,
        // フリーレーン入力
        FreeLane
    }
    
    // ノート判定で使用する入力情報
    public struct LiveInput
    {
        // 入力を識別する ID
        public int inputId;
        // 入力フェーズ
        public LiveInputPhase phase;
        // 入力が発生した楽曲時間
        public float musicTime;

        // 入力のワールド座標
        public Vector3 worldPosition;
        // 前回入力からの移動量
        public Vector2 delta;

        // 入力の中心レーン位置
        public float lane;
        // 入力範囲の左端レーン
        public float laneStart;
        // 入力範囲の右端レーン
        public float laneEnd;
        // 入力幅
        public float width;

        // 入力の発生元
        public LiveInputSource source;
        
        // ライブ入力情報を生成する
        // inputId: 入力を識別する ID
        // phase: 入力フェーズ
        // musicTime: 入力が発生した楽曲時間
        // worldPosition: 入力のワールド座標
        // delta: 前回入力からの移動量
        // lane: 入力の中心レーン位置
        // laneStart: 入力範囲の左端レーン
        // laneEnd: 入力範囲の右端レーン
        // width: 入力幅
        // source: 入力の発生元
        public LiveInput(int inputId, LiveInputPhase phase, float musicTime, Vector3 worldPosition, Vector2 delta, float lane, float laneStart, float laneEnd, float width, LiveInputSource source)
        {
            this.inputId = inputId;
            this.phase = phase;
            this.musicTime = musicTime;
            this.worldPosition = worldPosition;
            this.delta = delta;
            this.lane = lane;
            this.laneStart = laneStart;
            this.laneEnd = laneEnd;
            this.width = width;
            this.source = source;
        }
    }
}
