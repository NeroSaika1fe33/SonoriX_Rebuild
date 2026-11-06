using FFrameWork.Live;
using UnityEngine;
using UnityEngine.UI;

namespace FFrameWork
{
    public class ScoreView : MonoBehaviour
    {
        [Header("Canvas References")]
        // Canvas UI 版のスコアゲージ塗り表示
        [SerializeField] private Image scoreFillImage;

        // Canvas UI 版のスコア値テキスト
        [SerializeField] private Text scoreText;

        // 加算スコア表示テキスト
        [SerializeField] private Text addScoreText;

        // ランク表示テキスト
        [SerializeField] private Text rankText;

        // ランク表示 Image
        [SerializeField] private Image rankImage;

        [Header("Legacy Sprite References")]
        // SpriteRenderer 版のスコアゲージ
        [SerializeField] private SpriteRenderer scoreGauge;

        // SpriteRenderer 版のランク表示
        [SerializeField] private SpriteRenderer rankSpriteRenderer;

        // ランクごとの Sprite
        [SerializeField] private Sprite[] rankSprites;

        // ランク文字色を反映する SpriteRenderer
        [SerializeField] private SpriteRenderer rankTextSpriteRenderer;

        // S ランク到達位置マーカー
        [SerializeField] private Transform gaugeRankS;

        // A ランク到達位置マーカー
        [SerializeField] private Transform gaugeRankA;

        // B ランク到達位置マーカー
        [SerializeField] private Transform gaugeRankB;

        // C ランク到達位置マーカー
        [SerializeField] private Transform gaugeRankC;

        // スコア数字表示
        [SerializeField] private NumberView number;

        // スコア数字のアウトライン表示
        [SerializeField] private NumberView outlineNumber;

        // 加算スコア表示のルート
        [SerializeField] private Transform addScoreRoot;

        // 加算スコア数字表示
        [SerializeField] private NumberView addScoreNumber;

        // 加算スコア数字のアウトライン表示
        [SerializeField] private NumberView addScoreOutlineNumber;

        // 前回表示したランク
        private int lastScoreRank;

        // スコアからゲージ進行率を計算するクラス
        private ScoreGaugeCalculator scoreGaugeCalculator;

        // SpriteRenderer 版ゲージの初期サイズ
        private Vector2 scoreGaugeSizeCache = new Vector2(5.648148f, 0.2222222f);

        // 加算スコア表示の基準位置
        private Vector3 addScoreBasePosition;

        // 加算スコアアニメーションの経過時間
        private float addScoreAnimationTime;

        // 加算スコアアニメーション中か
        private bool addScoreAnimating;

        // 加算スコア表示の Y スケールが反転しているか
        private bool inverseScale;

        // スコア表示とランクマーカーを初期化する
        // musicData: スコアゲージ計算に使う楽曲データ
        public void Setup(LiveMusicData musicData)
        {
            SetupCanvasGauge();
            if (scoreGauge != null)
            {
                scoreGaugeSizeCache = scoreGauge.size;
            }

            SetupGaugeRankPosition(gaugeRankS, ScoreGaugeCalculator.RankRateS);
            SetupGaugeRankPosition(gaugeRankA, ScoreGaugeCalculator.RankRateA);
            SetupGaugeRankPosition(gaugeRankB, ScoreGaugeCalculator.RankRateB);
            SetupGaugeRankPosition(gaugeRankC, ScoreGaugeCalculator.RankRateC);

            scoreGaugeCalculator = ScoreGaugeCalculator.Create(musicData != null ? musicData.Score : null);
            if (addScoreRoot != null)
            {
                inverseScale = addScoreRoot.localScale.y < 0f;
                addScoreBasePosition = addScoreRoot.localPosition;
            }

            Clear();
        }

        // スコアゲージ計算器を差し替える
        // newCalculator: 新しいゲージ計算器
        public void ChangeScoreCalculator(ScoreGaugeCalculator newCalculator)
        {
            scoreGaugeCalculator = newCalculator;
        }

        // 初期スコアを表示へ反映する
        // score: 表示するスコア情報
        public void SetupScore(LiveScore score)
        {
            UpdateScore(ref score, 0);
        }

        // スコア表示を初期状態へ戻す
        public void Clear()
        {
            lastScoreRank = -1;
            addScoreAnimating = false;

            if (scoreFillImage != null)
            {
                scoreFillImage.fillAmount = 0f;
            }
            if (scoreText != null)
            {
                scoreText.text = "0";
            }
            if (rankText != null)
            {
                rankText.text = string.Empty;
            }
            if (rankImage != null)
            {
                rankImage.sprite = null;
            }
            if (addScoreText != null)
            {
                addScoreText.text = string.Empty;
                SetTextAlpha(addScoreText, 0f);
            }

            if (scoreGauge != null)
            {
                scoreGauge.size = new Vector2(0f, scoreGaugeSizeCache.y);
            }
            if (rankSpriteRenderer != null)
            {
                rankSpriteRenderer.sprite = null;
            }

            number?.Setup("#ffffff", "#CCCCEE");
            outlineNumber?.Setup("#444466", "#444466");
            addScoreNumber?.Setup("#ffffff", "#ffffff00");
            addScoreOutlineNumber?.Setup("#444466", "#ffffff00");

            if (addScoreRoot != null)
            {
                addScoreRoot.localScale = Vector3.zero;
                addScoreRoot.localPosition = addScoreBasePosition;
            }
        }

        // 現在スコアからゲージ進行率を計算する
        // score: 計算対象のスコア情報
        public float CalculateGaugeProgress(LiveScore score)
        {
            if (scoreGaugeCalculator == null)
            {
                return 0f;
            }

            return scoreGaugeCalculator.CalculateGaugeProgress(score.totalScore);
        }

        // スコア、ランク、加算スコア表示を更新する
        // score: 更新後のスコア情報
        // addScore: 今回加算されたスコア
        public void UpdateScore(ref LiveScore score, int addScore)
        {
            float progress = CalculateGaugeProgress(score);
            if (float.IsNaN(progress))
            {
                progress = 0f;
            }

            if (scoreGauge != null)
            {
                scoreGauge.size = new Vector2(progress * scoreGaugeSizeCache.x, scoreGaugeSizeCache.y);
            }
            if (scoreFillImage != null)
            {
                scoreFillImage.fillAmount = progress;
            }

            if (scoreText != null)
            {
                scoreText.text = score.totalScore.ToString("N0");
            }
            number?.UpdateNumber(score.totalScore);
            outlineNumber?.UpdateNumber(score.totalScore);

            int rank = (int)score.rank;
            string rankName = score.rank.ToString();
            if (rankText != null)
            {
                rankText.text = rankName;
                rankText.color = ColorUtility.GetScoreRankColor(rank);
            }
            if (rankTextSpriteRenderer != null)
            {
                rankTextSpriteRenderer.color = ColorUtility.GetScoreRankColor(rank);
            }

            if (lastScoreRank != rank)
            {
                if (rankSpriteRenderer != null)
                {
                    rankSpriteRenderer.sprite = rankSprites != null && rank >= 0 && rank < rankSprites.Length
                        ? rankSprites[rank]
                        : null;
                }
                if (rankImage != null)
                {
                    rankImage.sprite = rankSprites != null && rank >= 0 && rank < rankSprites.Length
                        ? rankSprites[rank]
                        : null;
                    rankImage.enabled = rankImage.sprite != null;
                }
                lastScoreRank = rank;
            }

            if (addScore >= 1)
            {
                PlayAddScore(addScore);
            }
        }

        // 加算スコア表示の移動とフェードを更新する
        private void Update()
        {
            if (!addScoreAnimating)
            {
                return;
            }

            addScoreAnimationTime += Time.deltaTime;
            float moveRate = Mathf.Clamp01(addScoreAnimationTime / 0.2f);
            float ease = 1f - (1f - moveRate) * (1f - moveRate);
            if (addScoreRoot != null)
            {
                addScoreRoot.localPosition = Vector3.Lerp(
                    new Vector3(-0.5f, addScoreBasePosition.y, addScoreBasePosition.z),
                    addScoreBasePosition,
                    ease);
            }

            float alpha = Mathf.Clamp01(addScoreAnimationTime / 0.2f);
            addScoreNumber?.UpdateAlpha(alpha);
            addScoreOutlineNumber?.UpdateAlpha(alpha);
            SetTextAlpha(addScoreText, alpha);

            if (addScoreAnimationTime >= 0.5f)
            {
                if (addScoreRoot != null)
                {
                    addScoreRoot.localScale = Vector3.zero;
                }
                addScoreAnimating = false;
            }
        }

        // ランク到達マーカーをゲージ上の指定割合へ配置する
        // t: 配置対象の Transform
        // value: 0 から 1 のゲージ割合
        private void SetupGaugeRankPosition(Transform t, float value)
        {
            if (t == null)
            {
                return;
            }

            RectTransform marker = t as RectTransform;
            RectTransform fillRect = scoreFillImage != null ? scoreFillImage.rectTransform : null;
            RectTransform parentRect = fillRect != null ? fillRect.parent as RectTransform : null;
            if (marker != null && parentRect != null)
            {
                Vector2 anchorMin = marker.anchorMin;
                Vector2 anchorMax = marker.anchorMax;
                anchorMin.x = Mathf.Clamp01(value);
                anchorMax.x = Mathf.Clamp01(value);
                marker.anchorMin = anchorMin;
                marker.anchorMax = anchorMax;

                Vector2 anchoredPosition = marker.anchoredPosition;
                anchoredPosition.x = 0f;
                marker.anchoredPosition = anchoredPosition;
                return;
            }

            if (scoreGauge == null)
            {
                return;
            }
            float x = scoreGauge.transform.localPosition.x;
            float maxX = x + scoreGaugeSizeCache.x;
            Vector3 position = t.localPosition;
            position.x = x + (maxX - x) * Mathf.Clamp01(value);
            t.localPosition = position;
        }

        // 加算スコアの表示アニメーションを開始する
        // addScore: 表示する加算スコア
        private void PlayAddScore(int addScore)
        {
            if (addScoreText != null)
            {
                addScoreText.text = "+" + addScore.ToString("N0");
                SetTextAlpha(addScoreText, 0f);
            }

            if (addScoreRoot != null)
            {
                addScoreRoot.localScale = Vector3.one;
                if (inverseScale)
                {
                    Vector3 scale = addScoreRoot.localScale;
                    scale.y = -1f;
                    addScoreRoot.localScale = scale;
                }
                addScoreRoot.localPosition = new Vector3(-0.5f, addScoreBasePosition.y, addScoreBasePosition.z);
            }

            addScoreAnimationTime = 0f;
            addScoreAnimating = true;
            addScoreNumber?.UpdateNumber(addScore);
            addScoreOutlineNumber?.UpdateNumber(addScore);
            addScoreNumber?.UpdateAlpha(0f);
            addScoreOutlineNumber?.UpdateAlpha(0f);
        }

        // Canvas UI 版のスコアゲージを横方向 Filled に設定する
        private void SetupCanvasGauge()
        {
            if (scoreFillImage == null)
            {
                return;
            }

            scoreFillImage.type = Image.Type.Filled;
            scoreFillImage.fillMethod = Image.FillMethod.Horizontal;
            scoreFillImage.fillOrigin = 0;
            scoreFillImage.fillAmount = 0f;
        }

        // Text のアルファ値だけを変更する
        // text: 対象 Text
        // alpha: 設定するアルファ値
        private static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null)
            {
                return;
            }

            Color color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }
    }
}
