using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // スコアからランクゲージの進行度を計算するクラス
    public class ScoreGaugeCalculator
    {
        // S ランクのゲージ位置
        public static readonly float RankRateS = 0.9f;
        // A ランクのゲージ位置
        public static readonly float RankRateA = 0.75f;
        // B ランクのゲージ位置
        public static readonly float RankRateB = 0.6f;
        // C ランクのゲージ位置
        public static readonly float RankRateC = 0.45f;
        
        // スコアしきい値とゲージ進行度の対応表
        private List<KeyValuePair<int, float>> gaugeValues;

        // マスタースコア情報からゲージ計算器を生成する
        // score: ランクごとの必要スコア情報
        public static ScoreGaugeCalculator Create(MasterPlayLevelScore score)
        {
            return score != null
                ? Create(score.s, score.a, score.b, score.c)
                : Create(1000000);
        }

        // 総スコアからランクしきい値を自動計算してゲージ計算器を生成する
        // totalScore: 満点として扱う総スコア
        public static ScoreGaugeCalculator Create(int totalScore)
        {
            float score = Mathf.Max(1f, totalScore);
            return Create(
                Mathf.FloorToInt(RankRateS * score),
                Mathf.FloorToInt(RankRateA * score),
                Mathf.FloorToInt(RankRateB * score),
                Mathf.FloorToInt(RankRateC * score));
        }

        // ランクごとのしきい値からゲージ計算器を生成する
        // s: S ランクの必要スコア
        // a: A ランクの必要スコア
        // b: B ランクの必要スコア
        // c: C ランクの必要スコア
        public static ScoreGaugeCalculator Create(int s, int a, int b, int c)
        {
            ScoreGaugeCalculator calculator = new ScoreGaugeCalculator();
            calculator.gaugeValues = new List<KeyValuePair<int, float>>
            {
                new KeyValuePair<int, float>(0, 0f),
                new KeyValuePair<int, float>(Mathf.Max(1, c), RankRateC),
                new KeyValuePair<int, float>(Mathf.Max(1, b), RankRateB),
                new KeyValuePair<int, float>(Mathf.Max(1, a), RankRateA),
                new KeyValuePair<int, float>(Mathf.Max(1, s), RankRateS)
            };
            return calculator;
        }

        // スコアから最終ランクを取得する
        // scoreInfo: ランクごとの必要スコア情報
        // score: 判定対象のスコア
        public static ScoreRank GetScoreRank(MasterPlayLevelScore scoreInfo, int score)
        {
            if (scoreInfo == null)
            {
                return ScoreRank.D;
            }
            if (scoreInfo.s <= score)
            {
                return ScoreRank.S;
            }
            if (scoreInfo.a <= score)
            {
                return ScoreRank.A;
            }
            if (scoreInfo.b <= score)
            {
                return ScoreRank.B;
            }
            if (scoreInfo.c <= score)
            {
                return ScoreRank.C;
            }
            return ScoreRank.D;
        }

        // スコアからゲージ表示用の進行度を計算する
        // score: 判定対象のスコア
        public float CalculateGaugeProgress(int score)
        {
            if (gaugeValues == null || gaugeValues.Count < 2)
            {
                return 0f;
            }

            for (int i = 0; i < gaugeValues.Count - 1; i++)
            {
                KeyValuePair<int, float> current = gaugeValues[i];
                KeyValuePair<int, float> next = gaugeValues[i + 1];
                if (next.Key <= score && i != gaugeValues.Count - 2)
                {
                    continue;
                }

                int scoreRange = next.Key - current.Key;
                if (scoreRange <= 0)
                {
                    return Mathf.Clamp01(next.Value);
                }

                float t = (score - current.Key) / (float)scoreRange;
                return Mathf.Clamp01(current.Value + (next.Value - current.Value) * t);
            }

            return 0f;
        }
    }
}
