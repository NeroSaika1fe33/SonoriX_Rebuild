using UnityEngine;

namespace FFrameWork.Songs
{
    [CreateAssetMenu(fileName = "SongSelectDataBase", menuName = "FFrameWork/Songs/Song Select DataBase")]
    // 選曲画面で使用する楽曲一覧データベース
    public class SongSelectDataBase : ScriptableObject
    {
        // 登録されている楽曲一覧
        [SerializeField] private SongSelectEntry[] songs;

        // 登録されている楽曲一覧
        public SongSelectEntry[] Songs => songs;
        // 登録曲数
        public int Count => songs != null ? songs.Length : 0;

        // 指定インデックスの楽曲情報を取得する
        // index: 取得する楽曲のインデックス
        public SongSelectEntry GetSong(int index)
        {
            return songs != null && index >= 0 && index < songs.Length ? songs[index] : null;
        }
    }
}
