using FFrameWork.Live;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // ライブ結果のスコア、判定数、ランク、クリア状態を UI に反映する
    public class ResultInfoView : MonoBehaviour
    {
        // 結果情報を取得する画面遷移 Controller
        [SerializeField] private LivePanelFlowController flowController;

        [Header("Texts")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text rankText;
        [SerializeField] private Text clearText;
        [FormerlySerializedAs("lifeGaugeText")]
        [SerializeField] private Text clearGaugeText;
        [SerializeField] private Text maxComboText;
        [SerializeField] private Text justPerfectText;
        [SerializeField] private Text perfectText;
        [SerializeField] private Text greatText;
        [SerializeField] private Text greatFastText;
        [SerializeField] private Text greatLateText;
        [SerializeField] private Text goodText;
        [SerializeField] private Text goodFastText;
        [SerializeField] private Text goodLateText;
        [SerializeField] private Text badText;
        [SerializeField] private Text badFastText;
        [SerializeField] private Text badLateText;
        [SerializeField] private Text missText;
        [SerializeField] private Text missFastText;
        [SerializeField] private Text missLateText;
        [SerializeField] private Text totalFastText;
        [SerializeField] private Text totalLateText;

        [Header("Images")]
        [SerializeField] private Image rankImage;
        [SerializeField] private Image clearImage;

        [Header("Rank Sprites")]
        [SerializeField] private Sprite rankS;
        [SerializeField] private Sprite rankA;
        [SerializeField] private Sprite rankB;
        [SerializeField] private Sprite rankC;
        [SerializeField] private Sprite rankD;
        private bool usesGeneratedRankText;

        [Header("Clear Sprites")]
        [FormerlySerializedAs("clearImageOverrideSprite")]
        [SerializeField] private Sprite autoPlaySprite;
        [SerializeField] private Sprite clearSprite;
        [SerializeField] private Sprite failedSprite;
        [SerializeField] private Sprite fullComboSprite;
        [SerializeField] private Sprite allPerfectSprite;

        // Controller を解決し、直近のライブ結果を表示へ反映する
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null)
            {
                flowController = controller;
            }

            if (flowController == null)
            {
                flowController = GetComponentInParent<LivePanelFlowController>();
            }

            if (flowController == null)
            {
                flowController = FindAnyObjectByType<LivePanelFlowController>();
            }

            ResolveMissingReferences();
            Refresh(
                flowController != null ? flowController.LastScore : default(LiveScore),
                flowController != null ? flowController.LastMaxClearGauge : LiveConfig.DefaultMaxClearGauge,
                flowController != null ? flowController.LastClearGaugeThreshold : Mathf.FloorToInt(LiveConfig.DefaultMaxClearGauge * 0.7f),
                flowController != null && flowController.LastWasAuto);
        }

        // Inspector 未設定のテキスト参照を名前から補完する
        private void ResolveMissingReferences()
        {
            if (scoreText == null)
            {
                scoreText = FindText("ScoreText");
            }
            if (perfectText == null)
            {
                perfectText = FindText("PerfectCount");
            }
            if (greatFastText == null)
            {
                greatFastText = FindText("FastGreatCount");
            }
            if (greatLateText == null)
            {
                greatLateText = FindText("LateGreatCount");
            }
            if (goodFastText == null)
            {
                goodFastText = FindText("FastGoodCount");
            }
            if (goodLateText == null)
            {
                goodLateText = FindText("LateGoodCount");
            }
            if (badFastText == null)
            {
                badFastText = FindText("FastBadCount");
            }
            if (badLateText == null)
            {
                badLateText = FindText("LateBadCount");
            }
            if (missText == null)
            {
                missText = FindText("MissCount");
            }
        }

        // 子階層から指定名の Text を探す
        private Text FindText(string objectName)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        // LiveScore から各結果表示を更新する
        public void Refresh(LiveScore score, int maxClearGauge, int clearGaugeThreshold, bool wasAuto)
        {
            maxClearGauge = Mathf.Max(1, maxClearGauge);
            clearGaugeThreshold = Mathf.Clamp(clearGaugeThreshold, 1, maxClearGauge);
            float clearGaugeRate = Mathf.Clamp01((float)score.clearGauge / maxClearGauge);
            int clearGaugePercent = Mathf.RoundToInt(clearGaugeRate * 100f);
            LiveClearStatus clearStatus = score.GetClearStatus(clearGaugeThreshold, wasAuto);
            wasAuto = clearStatus == LiveClearStatus.AutoPlay;
            bool isClear = score.clearGauge >= clearGaugeThreshold;
            bool isFullCombo = clearStatus == LiveClearStatus.FullCombo || clearStatus == LiveClearStatus.AllPerfect;
            bool isAllPerfect = clearStatus == LiveClearStatus.AllPerfect;
            ScoreRank resultRank = CalculateResultRank(score);
            Sprite resultRankSprite = GetRankSprite(resultRank);
            if (resultRankSprite == null && rankText == null && rankImage != null)
            {
                GameObject label = new GameObject("RankText", typeof(RectTransform), typeof(Text));
                label.layer = rankImage.gameObject.layer;
                label.transform.SetParent(rankImage.transform, false);
                RectTransform rect = label.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rankText = label.GetComponent<Text>();
                rankText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rankText.fontSize = 64;
                rankText.alignment = TextAnchor.MiddleCenter;
                rankText.raycastTarget = false;
                usesGeneratedRankText = true;
            }
            if (usesGeneratedRankText) rankText.enabled = resultRankSprite == null;

            SetText(scoreText, score.totalScore.ToString("N0"));
            SetText(rankText, resultRank.ToString());
            SetText(clearText, GetClearLabel(wasAuto, isClear, isFullCombo, isAllPerfect));
            SetText(clearGaugeText, clearGaugePercent + "%");
            SetText(maxComboText, score.maxCombo.ToString());
            SetText(justPerfectText, score.justPerfectCount.ToString());
            SetText(perfectText, score.perfectCount.ToString());
            SetText(greatText, score.greatCount.ToString());
            SetText(greatFastText, score.greatFastCount.ToString());
            SetText(greatLateText, score.greatLateCount.ToString());
            SetText(goodText, score.goodCount.ToString());
            SetText(goodFastText, score.goodFastCount.ToString());
            SetText(goodLateText, score.goodLateCount.ToString());
            SetText(badText, score.badCount.ToString());
            SetText(badFastText, score.badFastCount.ToString());
            SetText(badLateText, score.badLateCount.ToString());
            SetText(missText, score.missCount.ToString());
            SetText(missFastText, score.missFastCount.ToString());
            SetText(missLateText, score.missLateCount.ToString());
            SetText(totalFastText, score.fastCount.ToString());
            SetText(totalLateText, score.lateCount.ToString());

            SetImage(rankImage, resultRankSprite);
            SetImage(clearImage, GetClearSprite(wasAuto, isClear, isFullCombo, isAllPerfect));
        }

        // スコア到達率から表示用ランクを計算する
        private static ScoreRank CalculateResultRank(LiveScore score)
        {
            return score.GetResultRank();
        }

        // ランクに対応する画像を取得する
        private Sprite GetRankSprite(ScoreRank rank)
        {
            switch (rank)
            {
                case ScoreRank.S:
                    return rankS;
                case ScoreRank.A:
                    return rankA;
                case ScoreRank.B:
                    return rankB;
                case ScoreRank.C:
                    return rankC;
                default:
                    return rankD;
            }
        }

        // クリア状態に対応する画像を取得する
        private Sprite GetClearSprite(bool wasAuto, bool isClear, bool isFullCombo, bool isAllPerfect)
        {
            if (wasAuto && autoPlaySprite != null)
            {
                return autoPlaySprite;
            }
            if (isAllPerfect && allPerfectSprite != null)
            {
                return allPerfectSprite;
            }
            if (isFullCombo && fullComboSprite != null)
            {
                return fullComboSprite;
            }

            return isClear ? clearSprite : failedSprite;
        }

        // クリア状態に対応する表示ラベルを取得する
        private static string GetClearLabel(bool wasAuto, bool isClear, bool isFullCombo, bool isAllPerfect)
        {
            if (wasAuto)
            {
                return "Auto Play";
            }
            if (isAllPerfect)
            {
                return "All Perfect";
            }
            if (isFullCombo)
            {
                return "Full Combo";
            }

            return isClear ? "Clear" : "Failed";
        }

        // Text が存在する場合だけ文字列を設定する
        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        // Image が存在する場合だけ Sprite と表示状態を設定する
        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
