using FFrameWork.Live;

namespace FFrameWork.Songs
{
    public static class SongCatalog
    {
        public static SongCatalogItem CreateItem(SongSelectEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            return new SongCatalogItem
            {
                MusicId = entry.MusicId,
                Title = entry.Title,
                Artist = entry.Artist,
                Composer = entry.Composer,
                Jacket = entry.Jacket,
                ScoreText = entry.GetSusScoreText(),
                MusicEvent = entry.MusicEvent,
                Difficulty = entry.Difficulty,
                PlayLevel = entry.PlayLevel,
                TotalNoteCount = entry.TotalNoteCount,
                FillerSec = entry.FillerSec,
                InitialArrowLaneWidth = entry.InitialArrowLaneWidth,
                BundleBuildData = entry.BundleBuildData,
                VocalId = 1,
                LivePlayMode = LivePlayMode.Free,
                MusicCategory = MusicCategory.Default
            };
        }
    }
}
