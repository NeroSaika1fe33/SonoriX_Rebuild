namespace FFrameWork.Live
{
    // フリーライブ用の起動データ
    public class FreeLiveBootData : LiveBootDataBase
    {
        // 使用するデッキ ID
        public int DeckId { get; set; }

        // デッキ編成のインデックス一覧
        public int[] DeckFormationIndexes { get; set; }

        // フリーライブ用の起動データを生成する
        // musicId: 楽曲 ID
        // difficulty: 難易度名
        // vocalId: ボーカル ID
        // deckId: 使用するデッキ ID
        // playMode: ライブのプレイモード
        // category: 楽曲カテゴリ
        public FreeLiveBootData(
            int musicId,
            string difficulty,
            int vocalId,
            int deckId,
            LivePlayMode playMode,
            MusicCategory category = MusicCategory.Default)
        {
            DeckId = deckId;
            LivePlayMode = playMode;
            MusicCategory = category;
            MusicData = CreateMusicData(musicId, difficulty);
        }

        // 楽曲 ID と難易度名から最低限の楽曲データを生成する
        // musicId: 楽曲 ID
        // difficulty: 難易度名
        private static LiveMusicData CreateMusicData(int musicId, string difficulty)
        {
            return new LiveMusicData
            {
                Music = new MasterMusic
                {
                    id = musicId
                },
                Difficulty = new MasterMusicDifficulty
                {
                    musicId = musicId,
                    musicDifficulty = difficulty
                },
            };
        }
    }
}
