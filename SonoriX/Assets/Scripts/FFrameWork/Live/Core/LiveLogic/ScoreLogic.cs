using System;
using System.Collections.Generic;
using System.Linq;
using FFrameWork;
using UnityEngine;

namespace FFrameWork.Live
{
    // ライブ中のスコア、コンボ、クリアゲージ、ランク集計を管理するクラス
    public class ScoreLogic
    {
        // スコア設定がない場合の最大スコア
        private const int DefaultMaxScore = 1000000;
        // クリアゲージ設定がない場合の初期クリアゲージ
        private const int DefaultInitialClearGauge = 0;
        // クリアゲージ設定がない場合の最大クリアゲージ
        private const int DefaultMaxClearGauge = 100;
        // クリアクリアゲージ設定がない場合の必要クリアゲージ
        private const int DefaultClearGaugeThreshold = 70;
        // 最大スコア近辺の丸め誤差を吸収する許容値
        private const float MaxScoreRoundingTolerance = 1f;

        // ライブ起動時のデータ
        private LiveBootDataBase bootData;
        // スコアやコンボを反映するライブ View
        // 現在のライブスコア
        protected LiveScore score;
        // スコアやクリアゲージなどのライブ構築設定
        protected LiveBundleBuildData liveBundleBuildData;
        // ランク判定用スコア情報
        private MasterPlayLevelScore scoreInfo;
        // 丸め前の累積スコア
        private float totalScoreF;
        // 1 コンボあたりの基準スコア
        private float baseNoteScore;
        // ノート重みによるスコア総量を補正する倍率
        private float scoreWeightNormalization = 1f;
        // コンティニューした楽曲時間
        private float? continueTime;

        // 現在のライブスコア
        public LiveScore Score
        {
            get { return score; }
        }

        // 1 コンボあたりの基準スコア
        public float BaseNoteScore
        {
            get { return baseNoteScore; }
            private set { baseNoteScore = value; }
        }

        // 全コンボが Perfect 系判定かどうか
        public bool IsAllPerfectCombo
        {
            get { return score.totalComboCount == score.perfectCount; }
        }

        // 最大コンボが総コンボ数に到達しているかどうか
        public bool IsPerfectCombo
        {
            get { return score.totalComboCount == score.maxCombo; }
        }

        // 現在クリアゲージがクリア条件を満たしているかどうか
        public bool IsClearGauge
        {
            get
            {
                int clearGauge = GetClearGaugeThreshold();
                return score.clearGauge >= clearGauge;
            }
        }

        // スコア計算ロジックを生成する
        // data: スコア倍率やクリアゲージ値を含むライブ構築設定
        public ScoreLogic(LiveBundleBuildData data)
        {
            liveBundleBuildData = data;
        }

        public delegate void ScoreUpdatedHandler(LiveScore score, int addScore);

        public event Action<ScoreGaugeCalculator> OnScoreCalculatorChanged;
        public event Action<LiveScore> OnScoreSetup;
        public event ScoreUpdatedHandler OnScoreUpdated;
        public event Action<int> OnClearGaugeUpdated;
        public event Action<LiveScore> OnComboUpdated;
        public event Action<INote> OnNoteJudged;

        // スコア、コンボ総数、ランクしきい値、初期クリアゲージを初期化する
        // bootData: ライブ起動時のデータ
        // noteBases: 譜面内のルートノート一覧
        // eventBases: 譜面イベント一覧
        public virtual void Setup(LiveBootDataBase bootData, NoteBase[] noteBases, EventBase[] eventBases)
        {
            this.bootData = bootData;

            int totalComboCount = CountComboNotes(noteBases);
            float totalNoteScoreFactor = Mathf.Max(1f, SumNoteScoreFactors(noteBases, liveBundleBuildData));
            float baseTotalScore = GetBaseTotalScore(liveBundleBuildData);
            scoreInfo = EnsureScoreInfo(bootData, baseTotalScore);
            score = new LiveScore
            {
                clearGauge = GetInitialClearGauge(),
                rank = ScoreRank.D,
                totalComboCount = totalComboCount,
                baseTotalScore = baseTotalScore
            };
            BaseNoteScore = score.baseTotalScore / Mathf.Max(1, totalComboCount);
            scoreWeightNormalization = totalComboCount / totalNoteScoreFactor;
            totalScoreF = 0f;
            continueTime = null;

            RaiseScoreCalculatorChanged(ScoreGaugeCalculator.Create(scoreInfo));
            RaiseScoreSetup(score);
            RaiseClearGaugeUpdated(score.clearGauge);
        }

        // Backward-compatible entry point for callers that still pass views directly.
        public virtual void Setup(LiveBootDataBase bootData, NoteBase[] noteBases, EventBase[] eventBases, LiveViewBase[] liveViews)
        {
            Setup(bootData, noteBases, eventBases);
        }

        // コンティニュー時にクリアゲージとダメージ猶予時間を更新する
        // time: コンティニューした楽曲時間
        public virtual void Continue(float time)
        {
            continueTime = time;
            score.clearGauge = GetInitialClearGauge();
            RaiseClearGaugeUpdated(score.clearGauge);
        }

        // スコアに影響するイベントを実行するための拡張ポイント
        // eventInfo: 実行対象のイベント
        public virtual void ExcuteEvent(EventBase eventInfo)
        {
        }

        // 判定結果をスコア側のルールで補正するための拡張ポイント
        // result: 補正前の判定結果
        public virtual (NoteResult, NoteResultDescription) UpdateResult((NoteResult, NoteResultDescription) result)
        {
            return result;
        }

        // ノートの判定結果から加算スコアを計算し、表示へ反映する
        // note: スコア計算対象のノート
        // dropOut: 途中離脱などでスコアを減衰させるかどうか
        public virtual void CalculateScore(INote note, bool dropOut = false)
        {
            float addScore = CalculateAddScore(note, dropOut);
            totalScoreF += addScore;
            score.totalScore = CalculateDisplayedTotalScore(totalScoreF, score.baseTotalScore);
            int addScoreInt = Mathf.FloorToInt(addScore);
            UpdateScoreRank();
            RaiseScoreUpdated(score, addScoreInt);
        }

        // ノート単体の加算スコアを計算する
        // note: スコア計算対象のノート
        // dropOut: 途中離脱などでスコアを減衰させるかどうか
        protected virtual float CalculateAddScore(INote note, bool dropOut = false)
        {
            if (note == null || note.Result <= NoteResult.Pass)
            {
                return 0f;
            }

            float resultFactor = GetResultScoreFactor(note.Result);
            if (resultFactor <= 0f)
            {
                return 0f;
            }

            float noteFactor = GetNoteScoreFactor(note, liveBundleBuildData);
            float dropOutFactor = dropOut ? 0.1f : 1f;
            float addScore = BaseNoteScore * resultFactor * noteFactor * scoreWeightNormalization * dropOutFactor;
            return Mathf.Max(0f, addScore);
        }

        // 現在の総スコアからランクを更新する
        protected virtual void UpdateScoreRank()
        {
            score.rank = ScoreGaugeCalculator.GetScoreRank(scoreInfo, score.totalScore);
        }

        // ノート判定結果の件数、クリアゲージ、判定表示を更新する
        // note: 判定済みのノート
        public virtual void UpdateNoteResult(NoteBase note)
        {
            switch (note.Result)
            {
                case NoteResult.Miss:
                    score.missCount++;
                    CountTimingDescription(note.Result, note.Description);
                    break;
                case NoteResult.Bad:
                    score.badCount++;
                    CountTimingDescription(note.Result, note.Description);
                    break;
                case NoteResult.Auto:
                    score.autoCount++;
                    break;
                case NoteResult.Good:
                    score.goodCount++;
                    CountTimingDescription(note.Result, note.Description);
                    break;
                case NoteResult.Great:
                    score.greatCount++;
                    CountTimingDescription(note.Result, note.Description);
                    break;
                case NoteResult.JustPerfect:
                    score.justPerfectCount++;
                    score.perfectCount++;
                    break;
                case NoteResult.Perfect:
                    score.perfectCount++;
                    break;
            }

            ApplyClearGauge(note);
            RaiseNoteJudged(note);
        }

        // Fast/Late の総数と判定別内訳を加算する
        // result: 判定結果
        // description: Fast/Late などの判定詳細
        private void CountTimingDescription(NoteResult result, NoteResultDescription description)
        {
            if (result == NoteResult.Miss)
            {
                return;
            }

            switch (description)
            {
                case NoteResultDescription.Fast:
                    score.fastCount++;
                    CountFast(result);
                    break;
                case NoteResultDescription.Late:
                    score.lateCount++;
                    CountLate(result);
                    break;
            }
        }

        // Fast 判定の内訳を加算する
        // result: Fast が付いた判定結果
        private void CountFast(NoteResult result)
        {
            switch (result)
            {
                case NoteResult.Great:
                    score.greatFastCount++;
                    break;
                case NoteResult.Good:
                    score.goodFastCount++;
                    break;
                case NoteResult.Bad:
                    score.badFastCount++;
                    break;
                case NoteResult.Miss:
                    score.missFastCount++;
                    break;
            }
        }

        // Late 判定の内訳を加算する
        // result: Late が付いた判定結果
        private void CountLate(NoteResult result)
        {
            switch (result)
            {
                case NoteResult.Great:
                    score.greatLateCount++;
                    break;
                case NoteResult.Good:
                    score.goodLateCount++;
                    break;
                case NoteResult.Bad:
                    score.badLateCount++;
                    break;
                case NoteResult.Miss:
                    score.missLateCount++;
                    break;
            }
        }

        // 判定結果に応じて現在コンボと最大コンボを更新する
        // note: 判定済みのノート
        public virtual void UpdateCombo(NoteBase note)
        {
            if (note.Result == NoteResult.None)
            {
                return;
            }

            score.combo = note.Result < NoteResult.Great ? 0 : score.combo + 1;
            score.maxCombo = Math.Max(score.maxCombo, score.combo);
            RaiseComboUpdated(score);
        }

        // クリアゲージを回復して表示へ反映する
        // recoveryValue: 回復量
        public virtual void RecoverClearGauge(int recoveryValue)
        {
            if (recoveryValue > 0)
            {
                int maxGauge = GetMaxClearGauge();
                score.clearGauge = Mathf.Min(score.clearGauge + recoveryValue, maxGauge);
            }
            RaiseClearGaugeUpdated(score.clearGauge);
        }

        // ダメージ処理の拡張ポイント現在は UpdateNoteResult に集約している
        // noteInfo: ダメージ判定対象のノート
        public virtual void Damage(INote noteInfo)
        {
            // Clear gauge changes are centralized in UpdateNoteResult so hits and misses use one rule table.
        }

        // ルートノート配列から判定対象のノートを列挙する
        // noteBases: 譜面内のルートノート一覧
        private static IEnumerable<NoteBase> EnumerateJudgmentNotes(NoteBase[] noteBases)
        {
            if (noteBases == null)
            {
                yield break;
            }

            foreach (NoteBase rootNote in noteBases)
            {
                if (rootNote == null || rootNote.NoteList == null)
                {
                    continue;
                }

                foreach (NoteBase note in rootNote.NoteList)
                {
                    if (note != null && note.HasJudgment)
                    {
                        yield return note;
                    }
                }
            }
        }

        // 判定対象ノート数をコンボ総数として数える
        // noteBases: 譜面内のルートノート一覧
        private static int CountComboNotes(NoteBase[] noteBases)
        {
            return EnumerateJudgmentNotes(noteBases).Count();
        }

        // 判定対象ノートのスコア重み合計を計算する
        // noteBases: 譜面内のルートノート一覧
        // data: スコア重み設定
        private static float SumNoteScoreFactors(NoteBase[] noteBases, LiveBundleBuildData data)
        {
            return EnumerateJudgmentNotes(noteBases).Sum(note => GetNoteScoreFactor(note, data));
        }

        // 楽曲データのランクしきい値を取得し、なければ基準スコアから作成する
        // bootData: ライブ起動時のデータ
        // baseTotalScore: 満点として扱う基準スコア
        private static MasterPlayLevelScore EnsureScoreInfo(LiveBootDataBase bootData, float baseTotalScore)
        {
            LiveMusicData musicData = bootData != null ? bootData.MusicData : null;
            if (musicData != null &&
                musicData.Score != null &&
                musicData.Score.s > 0)
            {
                return musicData.Score;
            }

            int targetScore = Mathf.Max(1, Mathf.FloorToInt(baseTotalScore));
            int playLevel = musicData != null && musicData.Difficulty != null ? musicData.Difficulty.playLevel : 0;
            MasterPlayLevelScore score = new MasterPlayLevelScore
            {
                liveType = "free",
                playLevel = playLevel,
                s = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateS),
                a = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateA),
                b = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateB),
                c = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateC)
            };
            if (musicData != null)
            {
                musicData.Score = score;
            }
            return score;
        }

        // ライブ構築設定から基準満点スコアを取得する
        // data: スコア設定を含むライブ構築データ
        private static float GetBaseTotalScore(LiveBundleBuildData data)
        {
            int maxScore = data != null ? data.MaxScore : DefaultMaxScore;
            return Mathf.Max(1f, maxScore);
        }

        // 内部スコアを表示用整数スコアに変換する
        // totalScore: 丸め前の累積スコア
        // baseTotalScore: 満点として扱う基準スコア
        private static int CalculateDisplayedTotalScore(float totalScore, float baseTotalScore)
        {
            int maxScore = Mathf.FloorToInt(baseTotalScore);
            if (maxScore <= 0)
            {
                return 0;
            }

            if (totalScore > maxScore - MaxScoreRoundingTolerance)
            {
                return maxScore;
            }

            return Mathf.Min(Mathf.FloorToInt(totalScore + 0.0001f), maxScore);
        }

        // 判定結果ごとのスコア倍率を取得する
        // result: 判定結果
        private float GetResultScoreFactor(NoteResult result)
        {
            switch (result)
            {
                case NoteResult.JustPerfect:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.JustPerfectScoreRate : 1f, 1f);
                case NoteResult.Perfect:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.PerfectScoreRate : 1f, 1f);
                case NoteResult.Auto:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.AutoScoreRate : 1f, 1f);
                case NoteResult.Great:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.GreatScoreRate : 0.7f, 0.7f);
                case NoteResult.Good:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.GoodScoreRate : 0.5f, 0.5f);
                case NoteResult.Bad:
                    return GetConfiguredScoreRate(liveBundleBuildData != null ? liveBundleBuildData.BadScoreRate : 0f, 0f);
                default:
                    return 0f;
            }
        }

        // ノートごとのスコア重みを取得する
        // note: 対象ノート
        // data: スコア重み設定
        private static float GetNoteScoreFactor(INote note, LiveBundleBuildData data)
        {
            if (note == null)
            {
                return 1f;
            }
            return GetConfiguredScoreRate(data != null ? data.DefaultNoteWeight : 1f, 1f);
        }

        // スコア上でフリック系として扱うノートか確認する
        // note: 対象ノート
        private static bool IsFlickScoreNote(INote note)
        {
            return note != null && (note.Category == NoteCategory.SliderFlick || note.Category == NoteCategory.VolumeFlick);
        }

        // 設定値が不正な場合はデフォルト値へ戻し、0 以上に丸める
        // value: 設定された倍率または値
        // defaultValue: 不正値の場合に使用する値
        private static float GetConfiguredScoreRate(float value, float defaultValue)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return defaultValue;
            }
            return Mathf.Max(0f, value);
        }

        // 判定結果に応じたクリアゲージ増減を適用する
        // note: 判定済みのノート
        private void ApplyClearGauge(INote note)
        {
            if (note == null)
            {
                return;
            }

            int delta = GetClearGaugeDelta(note);
            if (delta == 0)
            {
                return;
            }

            if (continueTime.HasValue &&
                delta < 0 &&
                note.MusicScoreInfo.time + note.OffsetJudgeTime < continueTime.Value + LiveConfig.ContinueNoDamageTime)
            {
                return;
            }

            int beforeGauge = score.clearGauge;
            int maxGauge = GetMaxClearGauge();
            score.clearGauge = Mathf.Clamp(score.clearGauge + delta, 0, maxGauge);
            RaiseClearGaugeUpdated(score.clearGauge);
            Debug.Log(string.Format(
                "[ClearGauge] {0} {1:+#;-#;0} gauge:{2}->{3}/{4} result:{5} noteId:{6} time:{7:F3}",
                delta > 0 ? "Recovery" : "Damage",
                delta,
                beforeGauge,
                score.clearGauge,
                maxGauge,
                note.Result,
                note.Id,
                note.MusicScoreInfo.time));
        }

        // 判定結果からクリアゲージ増減量を取得する
        // note: 判定済みのノート
        private int GetClearGaugeDelta(INote note)
        {
            switch (note.Result)
            {
                case NoteResult.JustPerfect:
                    return GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.JustPerfectClearGaugeRecovery : 2);
                case NoteResult.Perfect:
                case NoteResult.Auto:
                    return GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.PerfectClearGaugeRecovery : 2);
                case NoteResult.Great:
                    return GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.GreatClearGaugeRecovery : 1);
                case NoteResult.Good:
                    return GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.GoodClearGaugeRecovery : 1);
                case NoteResult.Bad:
                    return -GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.BadClearGaugeDamage : 3);
                case NoteResult.Miss:
                    return -GetConfiguredClearGaugeValue(liveBundleBuildData != null ? liveBundleBuildData.MissClearGaugeDamage : 5);
                default:
                    return 0;
            }
        }

        // クリアに必要なクリアゲージ値を取得する
        private int GetClearGaugeThreshold()
        {
            int maxClearGauge = GetMaxClearGauge();
            int clearGaugeThreshold = liveBundleBuildData != null ? liveBundleBuildData.ClearGaugeThreshold : DefaultClearGaugeThreshold;
            return Mathf.Clamp(clearGaugeThreshold, 1, Mathf.Max(1, maxClearGauge));
        }

        // ライブ開始時のクリアゲージ値を取得する
        private int GetInitialClearGauge()
        {
            int initialClearGauge = liveBundleBuildData != null ? liveBundleBuildData.InitialClearGauge : DefaultInitialClearGauge;
            return Mathf.Clamp(initialClearGauge, 0, GetMaxClearGauge());
        }

        // 最大クリアゲージ値を取得する
        private int GetMaxClearGauge()
        {
            int maxClearGauge = liveBundleBuildData != null ? liveBundleBuildData.MaxClearGauge : DefaultMaxClearGauge;
            return Mathf.Max(1, maxClearGauge);
        }

        // クリアゲージ設定値を 0 以上に補正する
        // value: 補正対象のクリアゲージ値
        private static int GetConfiguredClearGaugeValue(int value)
        {
            return Mathf.Max(0, value);
        }

        protected virtual void RaiseScoreCalculatorChanged(ScoreGaugeCalculator calculator)
        {
            OnScoreCalculatorChanged?.Invoke(calculator);
        }

        protected virtual void RaiseScoreSetup(LiveScore score)
        {
            OnScoreSetup?.Invoke(score);
        }

        protected virtual void RaiseScoreUpdated(LiveScore score, int addScore)
        {
            OnScoreUpdated?.Invoke(score, addScore);
        }

        protected virtual void RaiseClearGaugeUpdated(int clearGauge)
        {
            OnClearGaugeUpdated?.Invoke(clearGauge);
        }

        protected virtual void RaiseComboUpdated(LiveScore score)
        {
            OnComboUpdated?.Invoke(score);
        }

        protected virtual void RaiseNoteJudged(INote note)
        {
            OnNoteJudged?.Invoke(note);
        }
    }
}
