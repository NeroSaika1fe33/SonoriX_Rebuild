using FMODUnity;
using FFrameWork.Live;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FFrameWork.Songs
{
    [CreateAssetMenu(fileName = "SongSelectEntry", menuName = "FFrameWork/Songs/Song Select Entry")]
    // 選曲画面で使用する楽曲 1 件分の設定
    public class SongSelectEntry : ScriptableObject
    {
        // 楽曲 ID
        [SerializeField] private int musicId = 1;
        // 楽曲タイトル
        [SerializeField] private string title = "DefaultMusic";
        // アーティスト名
        [SerializeField] private string artist;
        // 作曲者名
        [SerializeField] private string composer;
        // ジャケット画像
        [SerializeField] private Texture2D jacket;
        // バンドル内の SUS 譜面 TextAsset
        [SerializeField] private TextAsset susScore;
        // 再生する FMOD 楽曲イベント
        [SerializeField] private EventReference musicEvent;
        // 楽曲難易度
        [SerializeField] private MusicDifficulty difficulty = MusicDifficulty.Easy;
        // 表示用プレイレベル
        [SerializeField] private int playLevel = 0;
        // 表示用総ノート数
        [SerializeField] private int totalNoteCount;
        // 楽曲開始前のフィラー時間
        [SerializeField] private float fillerSec = 9f;
        // 初期矢印レーン幅
        [SerializeField, Range(1, 16)] private int initialArrowLaneWidth = 2;
        // ライブ用の構築設定
        [SerializeField] private LiveBundleBuildData bundleBuildData;
        // StreamingAssets から譜面を読み込むかどうか
        [SerializeField] private bool loadScoreFromStreamingAssets;
        // StreamingAssets 内の譜面相対パス
        [SerializeField] private string streamingAssetsScorePath;

        // 総ノート数計算用にキャッシュした譜面テキスト
        [NonSerialized] private string cachedTotalNoteScoreText;
        // 総ノート数計算時に使った構築設定
        [NonSerialized] private LiveBundleBuildData cachedTotalNoteBundleBuildData;
        // キャッシュ済みの総ノート数
        [NonSerialized] private int cachedScoreTotalNoteCount;
        // 総ノート数キャッシュが有効かどうか
        [NonSerialized] private bool hasCachedScoreTotalNoteCount;

        // 楽曲 ID
        public int MusicId => musicId;
        // 楽曲タイトル
        public string Title => title;
        // アーティスト名
        public string Artist => artist;
        // 作曲者名未設定ならアーティスト名を使う
        public string Composer => string.IsNullOrEmpty(composer) ? artist : composer;
        // ジャケット画像
        public Texture2D Jacket => jacket;
        // バンドル内の SUS 譜面 TextAsset
        public TextAsset SusScore => susScore;
        // 再生する FMOD 楽曲イベント
        public EventReference MusicEvent => musicEvent;
        // 楽曲難易度
        public MusicDifficulty Difficulty => difficulty;
        // SUS 内の PLAYLEVEL が読めればそれを優先したプレイレベル
        public int PlayLevel => TryGetScorePlayLevel(GetSusScoreText(false), out int scorePlayLevel) ? scorePlayLevel : playLevel;
        // SUS から算出できればそれを優先した総ノート数
        public int TotalNoteCount => TryGetScoreTotalNoteCount(GetSusScoreText(false), out int scoreTotalNoteCount) ? scoreTotalNoteCount : totalNoteCount;
        // 楽曲開始前のフィラー時間
        public float FillerSec => fillerSec;
        // 初期矢印レーン幅
        public int InitialArrowLaneWidth => initialArrowLaneWidth;
        // ライブ用の構築設定
        public LiveBundleBuildData BundleBuildData => bundleBuildData;
        // StreamingAssets から譜面を読み込むかどうか
        public bool LoadsScoreFromStreamingAssets => loadScoreFromStreamingAssets;
        // StreamingAssets 内の譜面相対パス
        public string StreamingAssetsScorePath => GetStreamingAssetsScorePath();

        // 記録本体は独立した JSON ストアで管理する。既存の参照用 API はそのまま利用可能。
        public SongPlayRecord PlayRecord => SongPlayRecordStore.Default.GetRecord(musicId, difficulty);
        public int HighScore => PlayRecord.HighScore;
        public LiveClearStatus BestClearStatus => PlayRecord.BestClearStatus;
        public bool HasPlayRecord => PlayRecord.HasPlayRecord;
        public ScoreRank BestRank => PlayRecord.BestRank;

        // 既存の呼び出しも JSON ストアへ転送する。
        public void RecordResult(LiveScore score, int maxClearGauge, int clearGaugeThreshold, bool wasAuto)
        {
            SongPlayRecordStore.Default.RecordResult(musicId, difficulty, score,
                maxClearGauge, clearGaugeThreshold, wasAuto);
        }

        // SUS 譜面テキストを取得する
        public string GetSusScoreText()
        {
            return GetSusScoreText(true);
        }

        // 外部譜面を優先して SUS 譜面テキストを取得する
        // logExternalReadFailure: 外部譜面の読み込み失敗をログ出力するかどうか
        private string GetSusScoreText(bool logExternalReadFailure)
        {
            if (loadScoreFromStreamingAssets)
            {
                string path = GetStreamingAssetsScorePath();
                string fullPath = GetStreamingAssetsFullPath(path);
                if (TryReadExternalScore(fullPath, out string scoreText))
                {
                    return scoreText;
                }

                if (logExternalReadFailure)
                {
                    Debug.LogWarningFormat(
                        "Debug score file was not found or could not be read. path:{0}. Falling back to bundled TextAsset.",
                        fullPath);
                }
            }

            return susScore != null ? susScore.text : null;
        }

        // StreamingAssets 内の譜面フルパスを取得する
        public string GetStreamingAssetsFullScorePath()
        {
            return GetStreamingAssetsFullPath(GetStreamingAssetsScorePath());
        }

        // 通常楽曲設定からランタイム用の Debug 楽曲設定を生成する
        // source: 複製元の楽曲設定
        public static SongSelectEntry CreateRuntimeDebugEntry(SongSelectEntry source)
        {
            if (source == null)
            {
                return null;
            }

            SongSelectEntry entry = CreateInstance<SongSelectEntry>();
            entry.name = source.name + " Debug";
            entry.musicId = source.musicId;
            entry.title = source.title;
            entry.artist = source.artist;
            entry.composer = source.composer;
            entry.jacket = source.jacket;
            entry.susScore = source.susScore;
            entry.musicEvent = source.musicEvent;
            entry.difficulty = MusicDifficulty.Debug;
            entry.playLevel = source.PlayLevel;
            entry.totalNoteCount = source.TotalNoteCount;
            entry.fillerSec = source.fillerSec;
            entry.initialArrowLaneWidth = source.initialArrowLaneWidth;
            entry.bundleBuildData = source.bundleBuildData;
            entry.loadScoreFromStreamingAssets = true;
            entry.streamingAssetsScorePath = CreateDefaultStreamingAssetsScorePath(source.title, source.musicId);
            return entry;
        }

        // StreamingAssets 内の譜面相対パスを取得する
        private string GetStreamingAssetsScorePath()
        {
            if (!string.IsNullOrEmpty(streamingAssetsScorePath))
            {
                return NormalizePath(streamingAssetsScorePath);
            }

            return CreateDefaultStreamingAssetsScorePath(title, musicId);
        }

        // タイトルと楽曲 ID からデフォルトの StreamingAssets 譜面パスを作成する
        // title: 楽曲タイトル
        // musicId: 楽曲 ID
        private static string CreateDefaultStreamingAssetsScorePath(string title, int musicId)
        {
            string folderName = SanitizePathPart(!string.IsNullOrEmpty(title) ? title : musicId.ToString());
            return NormalizePath(Path.Combine("Charts", folderName, "Debug.txt"));
        }

        // StreamingAssets のフルパスへ変換する
        // relativePath: StreamingAssets からの相対パス
        private static string GetStreamingAssetsFullPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, relativePath ?? string.Empty);
        }

        // 外部譜面ファイルを UTF-8 として読み込む
        // fullPath: 読み込み対象のフルパス
        // scoreText: 読み込んだ譜面テキスト
        private static bool TryReadExternalScore(string fullPath, out string scoreText)
        {
            scoreText = null;

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                scoreText = File.ReadAllText(fullPath, Encoding.UTF8);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to read debug score file: " + exception.Message);
                return false;
            }
        }

        // SUS テキストから PLAYLEVEL を取得する
        // scoreText: 解析対象の SUS テキスト
        // scorePlayLevel: 取得したプレイレベル
        private static bool TryGetScorePlayLevel(string scoreText, out int scorePlayLevel)
        {
            scorePlayLevel = 0;

            if (string.IsNullOrEmpty(scoreText))
            {
                return false;
            }

            using (StringReader reader = new StringReader(scoreText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!IsPlayLevelHeader(line))
                    {
                        continue;
                    }

                    string value = line.Substring("#PLAYLEVEL".Length).TrimStart(' ', '\t', ':').Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }

                    string[] parts = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 && int.TryParse(parts[0].Trim('"'), out scorePlayLevel);
                }
            }

            return false;
        }

        // SUS テキストから判定対象ノート数を計算する
        // scoreText: 解析対象の SUS テキスト
        // scoreTotalNoteCount: 計算した総ノート数
        private bool TryGetScoreTotalNoteCount(string scoreText, out int scoreTotalNoteCount)
        {
            scoreTotalNoteCount = 0;

            if (string.IsNullOrEmpty(scoreText))
            {
                return false;
            }

            if (hasCachedScoreTotalNoteCount &&
                cachedTotalNoteBundleBuildData == bundleBuildData &&
                string.Equals(cachedTotalNoteScoreText, scoreText, StringComparison.Ordinal))
            {
                scoreTotalNoteCount = cachedScoreTotalNoteCount;
                return true;
            }

            try
            {
                MusicScore musicScore = MusicScoreFactory.Create(scoreText, bundleBuildData);
                scoreTotalNoteCount = CountJudgmentNotes(musicScore != null ? musicScore.NoteArray : null);
                cachedTotalNoteScoreText = scoreText;
                cachedTotalNoteBundleBuildData = bundleBuildData;
                cachedScoreTotalNoteCount = scoreTotalNoteCount;
                hasCachedScoreTotalNoteCount = true;
                return scoreTotalNoteCount > 0;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to calculate total note count from score: " + exception.Message, this);
                return false;
            }
        }

        // 譜面ノート配列から判定対象ノート数を数える
        // noteBases: ルートノート配列
        private static int CountJudgmentNotes(NoteBase[] noteBases)
        {
            int count = 0;
            if (noteBases == null)
            {
                return count;
            }

            for (int i = 0; i < noteBases.Length; i++)
            {
                NoteBase rootNote = noteBases[i];
                if (rootNote == null || rootNote.NoteList == null)
                {
                    continue;
                }

                for (int j = 0; j < rootNote.NoteList.Count; j++)
                {
                    NoteBase note = rootNote.NoteList[j];
                    if (note != null && note.HasJudgment)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        // 行が PLAYLEVEL ヘッダーかどうか確認する
        // line: 確認対象の行
        private static bool IsPlayLevelHeader(string line)
        {
            const string Header = "#PLAYLEVEL";
            if (!line.StartsWith(Header, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return line.Length == Header.Length ||
                char.IsWhiteSpace(line[Header.Length]) ||
                line[Header.Length] == ':';
        }

        // ファイル名に使えない文字を置換する
        // value: パス要素として使う文字列
        private static string SanitizePathPart(string value)
        {
            string sanitized = value;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                sanitized = sanitized.Replace(invalidChars[i], '_');
            }

            return sanitized;
        }

        // パス区切り文字を / に統一する
        // path: 正規化対象のパス
        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
