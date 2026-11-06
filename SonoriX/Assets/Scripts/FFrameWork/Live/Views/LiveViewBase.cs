using System.Collections.Generic;
using FFrameWork;
using UnityEngine;

namespace FFrameWork.Live
{
    public abstract class LiveViewBase : MonoBehaviour
    {
        // ライブビュー共通の初期化入口
        // baseController: ライブ全体を管理するコントローラー
        public virtual void Setup(BaseLiveController baseController)
        {
        }

        // ライブデータ読み込み後に呼ばれる
        public virtual void OnLoad()
        {
        }

        // ライブデータ解放時に呼ばれる
        public virtual void OnUnload()
        {
        }

        // ノーツ種別ごとの必要数に合わせて表示プールを作成する
        // notePoolCount: ノーツ種別ごとのプール数
        public virtual void CreateNotePool(Dictionary<NoteCategory, int> notePoolCount)
        {
        }

        // 初期スコア表示を設定する
        // score: 初期表示するスコア情報
        public virtual void SetupScore(LiveScore score)
        {
        }

        // スコアゲージ計算器を差し替える
        // scoreGaugeCalculator: 新しいゲージ計算器
        public virtual void ChangeScoreCalculator(ScoreGaugeCalculator scoreGaugeCalculator)
        {
        }

        // ライブ中の毎フレーム更新
        // time: 現在の楽曲時間
        // currentFrameInfo: 現在フレームの譜面時間情報
        public virtual void OnUpdate(float time, MusicScoreInfo currentFrameInfo)
        {
        }

        // 楽曲再生開始時に呼ばれる
        // time: 開始時刻
        public virtual void MusicStart(float time)
        {
        }

        // リズムゲーム開始時に呼ばれる
        public virtual void RhythmGameStart()
        {
        }

        // 画面サイズ変更時に呼ばれる
        public virtual void OnScreenSizeChanged()
        {
        }

        // 一時停止時に呼ばれる
        // time: 一時停止した楽曲時間
        public virtual void Pause(float time)
        {
        }

        // 再開時に呼ばれる
        // time: 再開した楽曲時間
        public virtual void Resume(float time)
        {
        }

        // リトライ時にビュー状態を戻す
        public virtual void Retry()
        {
        }

        // カウントダウン開始時に呼ばれる
        public virtual void Countdown()
        {
        }

        // ライブ失敗時に呼ばれる
        // time: 失敗した楽曲時間
        public virtual void OnFailure(float time)
        {
        }

        // public virtual void Result(LiveResultAnimationType animationType)
        // {
        // }

        // ライブ終了時に呼ばれる
        public virtual void Finish()
        {
        }

        // ノーツ表示生成時に呼ばれる
        // note: 表示対象のノーツ
        public virtual void SpawnNote(INote note)
        {
        }

        // ノーツ表示解放時に呼ばれる
        // note: 解放対象のノーツ
        public virtual void UnspawnNote(INote note)
        {
        }

        // ノーツ判定時に呼ばれる
        // note: 判定済みノーツ
        public virtual void JudgmentNote(INote note)
        {
        }

        // レーン入力が拾われなかった時に呼ばれる
        // lane: 入力されたレーン番号
        // input: 拾われなかった入力情報
        public virtual void Unpicked(int lane, ref LiveInput input)
        {
        }

        // スコア更新時に呼ばれる
        // score: 更新後のスコア情報
        // addScore: 今回加算されたスコア
        public virtual void UpdateScore(ref LiveScore score, int addScore)
        {
        }

        public virtual void UpdateClearGauge(int clearGauge)
        {
        }

        // コンボ更新時に呼ばれる
        // score: 更新後のスコア情報
        public virtual void UpdateCombo(LiveScore score)
        {
        }
    }
}
