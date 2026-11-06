namespace FFrameWork.Live
{
    // 楽曲の基本マスター情報
    public class MasterMusic
    {
        // 楽曲 ID
        public int id;
        // 楽曲タイトル
        public string title;
        // アーティスト名
        public string artist;
        // 楽曲開始前のフィラー時間
        public float fillerSec;
        
        // 表示用タイトル
        public string Title
        {
            get { return string.IsNullOrEmpty(title) ? $"music_{id:0000}" : title; }
        }
        
        // 表示用アーティスト名
        public string Artist
        {
            get { return artist ?? string.Empty; }
        }
    }
}
