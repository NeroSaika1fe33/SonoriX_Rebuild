using FFrameWork;

namespace FFrameWork.Live
{
    public static class LiveViewExt
    {
        // すべてのライブビューへ初期スコアを通知する
        // liveViews: 通知先のライブビュー配列
        // score: 初期表示するスコア情報
        public static void SetupScore(this LiveViewBase[] liveViews, LiveScore score)
        {
            ForEach(liveViews, view => view.SetupScore(score));
        }

        // すべてのライブビューへスコアゲージ計算器の変更を通知する
        // liveViews: 通知先のライブビュー配列
        // scoreGaugeCalculator: 新しいゲージ計算器
        public static void ChangeScoreCalculator(this LiveViewBase[] liveViews,
            ScoreGaugeCalculator scoreGaugeCalculator)
        {
            ForEach(liveViews, view => view.ChangeScoreCalculator(scoreGaugeCalculator));
        }

        // すべてのライブビューへノーツ判定を通知する
        // liveViews: 通知先のライブビュー配列
        // note: 判定済みノーツ
        public static void JudgmentNote(this LiveViewBase[] liveViews, INote note)
        {
            ForEach(liveViews, view => view.JudgmentNote(note));

        }

        // すべてのライブビューへ未取得入力を通知する
        // liveViews: 通知先のライブビュー配列
        // lane: 入力されたレーン番号
        // input: 拾われなかった入力情報
        public static void Unpicked(this LiveViewBase[] liveViews, int lane, ref LiveInput input)
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.Unpicked(lane, ref input);
            }
        }

        // すべてのライブビューへスコア更新を通知する
        // liveViews: 通知先のライブビュー配列
        // score: 更新後のスコア情報
        // addScore: 今回加算されたスコア
        public static void UpdateScore(this LiveViewBase[] liveViews, ref LiveScore score, int addScore)
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.UpdateScore(ref score, addScore);
            }
        }

        public static void UpdateClearGauge(this LiveViewBase[] liveViews, int clearGauge)
        {
            ForEach(liveViews, view => view.UpdateClearGauge(clearGauge));
        }

        // すべてのライブビューへコンボ更新を通知する
        // liveViews: 通知先のライブビュー配列
        // score: 更新後のスコア情報
        public static void UpdateCombo(this LiveViewBase[] liveViews, LiveScore score)
        {
            ForEach(liveViews, view => view.UpdateCombo(score));
        }

        // public static void Result(this LiveViewBase[] liveViews, LiveResultAnimationType animationType)
        // {
        //     ForEach(liveViews, view => view.Result(animationType));
        // }

        // すべてのライブビューへライブ終了を通知する
        // liveViews: 通知先のライブビュー配列
        public static void Finish(this LiveViewBase[] liveViews)
        {
            ForEach(liveViews, view => view.Finish());
        }

        // すべてのライブビューへ画面サイズ変更を通知する
        // liveViews: 通知先のライブビュー配列
        public static void OnScreenSizeChanged(this LiveViewBase[] liveViews)
        {
            ForEach(liveViews, view => view.OnScreenSizeChanged());
        }

        // null を除外しながらすべてのライブビューへ処理を適用する
        // liveViews: 処理対象のライブビュー配列
        // action: 各ビューへ適用する処理
        private static void ForEach(LiveViewBase[] liveViews, System.Action<LiveViewBase> action)
        {
            if (liveViews == null || action == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    action(liveViews[i]);
                }
            }
        }
    }
}
