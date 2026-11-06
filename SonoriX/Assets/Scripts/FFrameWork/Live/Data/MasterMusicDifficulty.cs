namespace FFrameWork.Live
{
    // 楽曲難易度のマスター情報
    public class MasterMusicDifficulty
    {
        // 難易度 ID
        public int id;
        // 表示用プレイレベル
        public int playLevel;
        // 対応する楽曲 ID
        public int musicId;
        // 難易度名
        public string musicDifficulty = "easy";
        
        // 難易度名を enum として取得する
        public MusicDifficulty MusicDifficulty
        {
            get
            {
                return System.Enum.TryParse(musicDifficulty, true, out MusicDifficulty result)
                    ? result
                    : MusicDifficulty.Easy;
            }
        }
    }
}
