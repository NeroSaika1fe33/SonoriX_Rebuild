using UnityEngine;

namespace FFrameWork.Live
{
    public static class LiveBootFactory
    {
        public static FreeLiveBootData CreateFreeLiveBootData(SongCatalogItem song, bool isAuto)
        {
            if (song == null)
            {
                return null;
            }

            string difficulty = GetDifficultyName(song.Difficulty);
            FreeLiveBootData bootData = new FreeLiveBootData(
                song.MusicId,
                difficulty,
                song.VocalId,
                0,
                song.LivePlayMode,
                song.MusicCategory);

            bootData.IsAuto = isAuto;
            bootData.canSkipDisplayMusicInfo = song.CanSkipDisplayMusicInfo;
            bootData.LiveSettingData = new LiveSettingData
            {
                NoteSpeed = LivePlayerSettings.NoteSpeed,
                TimingAdjustData = song.TimingAdjustData
            };
            bootData.BundleBuildData = song.BundleBuildData;
            bootData.InitialArrowLaneWidth = song.InitialArrowLaneWidth;

            LiveMusicData musicData = bootData.MusicData ?? new LiveMusicData();
            musicData.Music = new MasterMusic
            {
                id = song.MusicId,
                title = song.Title,
                artist = song.Artist,
                fillerSec = song.FillerSec
            };
            musicData.Difficulty = new MasterMusicDifficulty
            {
                id = song.MusicId * 10 + (int)song.Difficulty,
                musicId = song.MusicId,
                musicDifficulty = difficulty,
                playLevel = song.PlayLevel
            };
            if (song.TotalNoteCount > 0)
            {
                musicData.Score = CreatePreviewScore(song.PlayLevel, song.TotalNoteCount);
            }

            musicData.JacketTexture = song.Jacket;
            bootData.MusicData = musicData;
            return bootData;
        }

        public static void ApplyGlobalPlayerSettings(LiveSettingData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.NoteSpeed = LivePlayerSettings.NoteSpeed;
        }

        public static MasterPlayLevelScore CreatePreviewScore(int playLevel, int totalNoteCount)
        {
            int noteCount = Mathf.Max(1, totalNoteCount);
            int targetScore = noteCount * 1000;
            return new MasterPlayLevelScore
            {
                liveType = "free",
                playLevel = playLevel,
                s = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateS),
                a = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateA),
                b = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateB),
                c = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateC)
            };
        }

        public static string GetDifficultyName(MusicDifficulty difficulty)
        {
            return difficulty == MusicDifficulty.None ? "easy" : difficulty.ToString().ToLowerInvariant();
        }
    }
}
