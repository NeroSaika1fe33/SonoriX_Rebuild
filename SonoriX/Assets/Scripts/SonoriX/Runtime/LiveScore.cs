namespace SonoriX.Runtime
{
    public readonly struct LiveScore
    {
        public int TotalScore { get; }
        public int Combo { get; }
        public int MaxCombo { get; }

        public int JustPerfectCount { get; }
        public int PerfectCount { get; }
        public int GreatCount { get; }
        public int GoodCount { get; }
        public int BadCount { get; }
        public int MissCount { get; }

        public LiveScore(
            int totalScore,
            int combo,
            int maxCombo,
            int justPerfectCount,
            int perfectCount,
            int greatCount,
            int goodCount,
            int badCount,
            int missCount)
        {
            TotalScore = totalScore;
            Combo = combo;
            MaxCombo = maxCombo;
            JustPerfectCount = justPerfectCount;
            PerfectCount = perfectCount;
            GreatCount = greatCount;
            GoodCount = goodCount;
            BadCount = badCount;
            MissCount = missCount;
        }
    }
}