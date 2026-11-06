using System;
using UnityEngine;
using FFrameWork.Live;

namespace FFrameWork
{
    public static class ColorUtility
    {
        // HTML カラー文字列から Color を作成する
        // code: #RRGGBB または #RRGGBBAA 形式の文字列
        public static Color Create(string code)
        {
            if (UnityEngine.ColorUtility.TryParseHtmlString(code, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        // HTML カラー文字列から Color を作成し、アルファ値だけ上書きする
        // code: #RRGGBB または #RRGGBBAA 形式の文字列
        // alpha: 設定するアルファ値
        public static Color Create(string code, float alpha)
        {
            Color color = Create(code);
            color.a = alpha;
            return color;
        }

        // 0 から 255 の RGBA 値から Color を作成する
        // r: 赤成分
        // g: 緑成分
        // b: 青成分
        // a: アルファ成分
        public static Color Create(int r, int g, int b, int a = 255)
        {
            return new Color(
                Mathf.Clamp01(r / 255f),
                Mathf.Clamp01(g / 255f),
                Mathf.Clamp01(b / 255f),
                Mathf.Clamp01(a / 255f));
        }

        // 楽曲難易度に対応する表示色を取得する
        // difficulty: 対象の難易度
        public static Color GetDifficultyColor(MusicDifficulty difficulty)
        {
            switch (difficulty)
            {
                case MusicDifficulty.Easy:
                    return new Color(0.06666667f, 0.8666667f, 0.46666667f, 1f);
                case MusicDifficulty.Normal:
                    return new Color(0.2f, 0.8f, 1f, 1f);
                case MusicDifficulty.Hard:
                    return new Color(1f, 0.8f, 0f, 1f);
                case MusicDifficulty.Extra:
                    return new Color(1f, 0.26666668f, 0.46666667f, 1f);
                case MusicDifficulty.Debug:
                    return new Color(1f, 0.35f, 0.85f, 1f);
                // case MusicDifficulty.Master:
                //     return new Color(0.8f, 0.2f, 1f, 1f);
                // case MusicDifficulty.Append:
                //     return new Color(1f, 0.49019608f, 0.7882353f, 1f);
                default:
                    return Color.white;
            }
        }

        // 文字列の難易度名から表示色を取得する
        // difficulty: 難易度名
        public static Color GetDifficultyColor(string difficulty)
        {
            if (Enum.TryParse(difficulty, true, out MusicDifficulty parsed))
            {
                return GetDifficultyColor(parsed);
            }

            return Color.white;
        }

        // スコアランクに対応する表示色を取得する
        // rank: 対象のスコアランク
        public static Color GetScoreRankColor(ScoreRank rank)
        {
            return GetScoreRankColor((int)rank);
        }

        // スコアランク index に対応する表示色を取得する
        // index: ScoreRank に変換する index
        public static Color GetScoreRankColor(int index)
        {
            switch ((ScoreRank)index)
            {
                case ScoreRank.S:
                    return Create("#FF8ec6");
                case ScoreRank.A:
                    return Create("#e18aff");
                case ScoreRank.B:
                    return Create("#78adff");
                case ScoreRank.C:
                    return Create("#55fdf7");
                case ScoreRank.D:
                    return Create("#73ffcc");
                default:
                    return Color.white;
            }
        }
    }
}
