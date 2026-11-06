using UnityEngine;

namespace FFrameWork.Live
{
    public struct LiveScore
    {
        public int clearGauge;

        // 現在継続中のコンボ数
        public int combo;

        // ライブ中に到達した最大コンボ数
        public int maxCombo;

        // 早押し側にずれた判定回数
        public int fastCount;

        // 遅押し側にずれた判定回数
        public int lateCount;

        // フリックノーツの成功回数
        public int flickCount;

        // コンボ対象になる総ノーツ数
        public int totalComboCount;

        // 現在のスコアランク
        public ScoreRank rank;

        // 表示用の合計スコア
        public int totalScore;

        // 小数込みで保持する基礎合計スコア
        public float baseTotalScore;

        // Just Perfect 判定の回数
        public int justPerfectCount;

        // Perfect 判定の回数
        public int perfectCount;

        // Great 判定の合計回数
        public int greatCount;

        // Great のうち早押し側だった回数
        public int greatFastCount;

        // Great のうち遅押し側だった回数
        public int greatLateCount;

        // Good 判定の合計回数
        public int goodCount;

        // Good のうち早押し側だった回数
        public int goodFastCount;

        // Good のうち遅押し側だった回数
        public int goodLateCount;

        // Bad 判定の合計回数
        public int badCount;

        // Bad のうち早押し側だった回数
        public int badFastCount;

        // Bad のうち遅押し側だった回数
        public int badLateCount;

        // Miss 判定の合計回数
        public int missCount;

        // Miss のうち早押し側だった回数
        public int missFastCount;

        // Miss のうち遅押し側だった回数
        public int missLateCount;
        // public int feverCount;
        // public int totalFeverCount;
        // public bool joinFever;
        // public bool ignoreFeverPerfomance;

        // オートプレイで処理された入力回数
        public int autoCount;

        // 結果表示と保存記録で共通の S ～ D 評価を取得する
        public ScoreRank GetResultRank()
        {
            float maxScore = baseTotalScore > 0f ? baseTotalScore : 1000000f;
            float rate = Mathf.Clamp01(totalScore / maxScore);
            if (rate >= ScoreGaugeCalculator.RankRateS) return ScoreRank.S;
            if (rate >= ScoreGaugeCalculator.RankRateA) return ScoreRank.A;
            if (rate >= ScoreGaugeCalculator.RankRateB) return ScoreRank.B;
            if (rate >= ScoreGaugeCalculator.RankRateC) return ScoreRank.C;
            return ScoreRank.D;
        }

        // リザルト表示と保存記録で共通のクリア状態を取得する
        public LiveClearStatus GetClearStatus(int clearGaugeThreshold, bool wasAuto)
        {
            if (wasAuto || autoCount > 0)
            {
                return LiveClearStatus.AutoPlay;
            }

            bool hasJudgments = justPerfectCount > 0 || perfectCount > 0 || greatCount > 0 ||
                                goodCount > 0 || badCount > 0 || missCount > 0;
            if (hasJudgments && goodCount == 0 && badCount == 0 && missCount == 0)
            {
                return greatCount == 0 ? LiveClearStatus.AllPerfect : LiveClearStatus.FullCombo;
            }

            return clearGauge >= clearGaugeThreshold ? LiveClearStatus.TrackClear : LiveClearStatus.TrackCrash;
        }

        // Great 以下の失敗寄り判定がないかを返す
        public bool IsAllPerfect
        {
            get { return badCount + missCount + goodCount + greatCount < 1; }
        }
    }
}
