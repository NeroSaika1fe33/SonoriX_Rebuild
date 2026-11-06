namespace FFrameWork.Live
{
    // FMOD パラメータ制御で扱うエフェクト種別
    public enum AudioEffectType
    {
        // ハイパスフィルターのカットオフ
        HighpassCutoff = 0,
        // ローパスフィルターのカットオフ
        LowpassCutoff = 1,
        // フランジャーのミックス
        FlangerMix = 2,
        // ワブル系エフェクト
        Wobble = 3,
        // 未使用状態
        Free = 4
    }
}
