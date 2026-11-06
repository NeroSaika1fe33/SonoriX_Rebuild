namespace FFrameWork
{
    using FFrameWork.Live;

    // ライブ起動時に必要な共通データ
    public class LiveBootDataBase
    {
        // 楽曲、難易度、スコアなどの楽曲データ
        public LiveMusicData MusicData { get; set; }

        // ライブ中の表示や判定に関する設定
        public LiveSettingData LiveSettingData { get; set; } = new LiveSettingData();

        // ライブ用アセットとゲームバランス設定
        public LiveBundleBuildData BundleBuildData { get; set; }

        // 楽曲カテゴリ
        public MusicCategory MusicCategory { get; set; } = MusicCategory.Default;

        // ライブのプレイモード
        public LivePlayMode LivePlayMode { get; set; } = LivePlayMode.Free;

        // 矢印入力の初期レーン幅
        public int InitialArrowLaneWidth { get; set; } = 2;

        // オートプレイかどうか
        public bool IsAuto = false;// { get; set; }

        // 楽曲情報表示をスキップできるかどうか
        public bool canSkipDisplayMusicInfo { get; set; }
    }
}
