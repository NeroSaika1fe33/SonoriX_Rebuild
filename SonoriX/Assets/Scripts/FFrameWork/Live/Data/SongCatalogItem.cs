using FMODUnity;
using UnityEngine;

namespace FFrameWork.Live
{
    public sealed class SongCatalogItem
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Composer { get; set; }
        public Texture2D Jacket { get; set; }
        public string ScoreText { get; set; }
        public EventReference MusicEvent { get; set; }
        public MusicDifficulty Difficulty { get; set; }
        public int PlayLevel { get; set; }
        public int TotalNoteCount { get; set; }
        public float FillerSec { get; set; }
        public int InitialArrowLaneWidth { get; set; }
        public LiveBundleBuildData BundleBuildData { get; set; }
        public int VocalId { get; set; }
        public LivePlayMode LivePlayMode { get; set; }
        public MusicCategory MusicCategory { get; set; }
        public float TimingAdjustData { get; set; }
        public bool CanSkipDisplayMusicInfo { get; set; }
    }
}
