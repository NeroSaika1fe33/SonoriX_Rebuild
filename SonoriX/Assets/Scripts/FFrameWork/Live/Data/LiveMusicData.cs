using UnityEngine;

namespace FFrameWork.Live
{
    // ライブで使用する楽曲関連データ
    public class LiveMusicData
    {
        // 楽曲マスター情報
        public MasterMusic Music { get; set; }

        // 難易度情報
        public MasterMusicDifficulty Difficulty { get; set; }

        // ランクしきい値情報
        public MasterPlayLevelScore Score { get; set; }

        // ジャケット画像
        public Texture2D JacketTexture { get; set; }
        
    }
}
