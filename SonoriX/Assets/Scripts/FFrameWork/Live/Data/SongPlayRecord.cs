using FFrameWork.Live;

namespace FFrameWork.Songs
{
    // 楽曲設定とは独立した、曲 ID・難易度ごとのプレイヤー記録。
    public readonly struct SongPlayRecord
    {
        public int MusicId { get; }
        public MusicDifficulty Difficulty { get; }
        public int HighScore { get; }
        public ScoreRank BestRank { get; }
        public LiveClearStatus BestClearStatus { get; }
        public bool HasPlayRecord => BestClearStatus != LiveClearStatus.None;

        internal SongPlayRecord(int musicId, MusicDifficulty difficulty, int highScore = 0,
            ScoreRank bestRank = ScoreRank.None, LiveClearStatus bestClearStatus = LiveClearStatus.None)
        {
            MusicId = musicId;
            Difficulty = difficulty;
            HighScore = highScore;
            BestRank = bestRank;
            BestClearStatus = bestClearStatus;
        }

        internal SongPlayRecord Merge(SongPlayRecord other)
        {
            ScoreRank rank = BestRank;
            if (other.BestRank != ScoreRank.None && (rank == ScoreRank.None || other.BestRank < rank))
            {
                rank = other.BestRank;
            }
            return new SongPlayRecord(MusicId, Difficulty,
                System.Math.Max(HighScore, other.HighScore), rank,
                other.BestClearStatus > BestClearStatus ? other.BestClearStatus : BestClearStatus);
        }
    }
}
