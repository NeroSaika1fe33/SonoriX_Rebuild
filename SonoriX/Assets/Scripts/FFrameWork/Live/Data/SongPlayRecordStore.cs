using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork.Songs
{
    // 1 曲・1 難易度を 1 JSON に保存する。Entry アセットへの書き戻しは行わない。
    public sealed class SongPlayRecordStore
    {
        [Serializable]
        private sealed class RecordJson
        {
            public int version = 1;
            public int musicId;
            public string difficulty;
            public int highScore;
            // enum の数値変更に影響されないよう名前で保存する。
            public string bestRank;
            public string bestClearStatus;
        }

        private static SongPlayRecordStore defaultStore;
        private static readonly Encoding JsonEncoding = new UTF8Encoding(false);
        private readonly Dictionary<string, SongPlayRecord> records = new Dictionary<string, SongPlayRecord>();
        private readonly HashSet<string> pendingSaves = new HashSet<string>();
        private bool hasWarnedAboutFallback;

        public string RecordsDirectory { get; }
        public string FallbackDirectory { get; }

        public static SongPlayRecordStore Default
        {
            get
            {
                if (defaultStore == null)
                {
                    string streamingPath = Application.streamingAssetsPath;
                    bool isUrl = streamingPath.Contains("://") || streamingPath.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
                    defaultStore = new SongPlayRecordStore(
                        isUrl ? null : Path.Combine(streamingPath, "PlayRecords"),
                        Path.Combine(Application.persistentDataPath, "PlayRecords"));
                    Application.quitting -= FlushDefault;
                    Application.quitting += FlushDefault;
                }
                return defaultStore;
            }
        }

        // パスを指定して独立したストアを作成できるため、テストで実際の存档を汚さない。
        public SongPlayRecordStore(string recordsDirectory, string fallbackDirectory = null)
        {
            if (string.IsNullOrEmpty(recordsDirectory) && string.IsNullOrEmpty(fallbackDirectory))
            {
                throw new ArgumentException("At least one play record directory is required.");
            }
            RecordsDirectory = recordsDirectory;
            FallbackDirectory = fallbackDirectory;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDefaultStore()
        {
            defaultStore = null;
        }

        private static void FlushDefault()
        {
            defaultStore?.Flush();
        }

        public SongPlayRecord GetRecord(int musicId, MusicDifficulty difficulty, int maxScore = 1000000)
        {
            string key = GetRelativePath(musicId, difficulty);
            if (records.TryGetValue(key, out SongPlayRecord cached)) return cached;

            SongPlayRecord record = new SongPlayRecord(musicId, difficulty);
            string primaryPath = GetPath(RecordsDirectory, key);
            bool hasPrimaryFile = !string.IsNullOrEmpty(primaryPath) && File.Exists(primaryPath);
            if (TryRead(primaryPath, musicId, difficulty, out SongPlayRecord primary))
            {
                record = record.Merge(primary);
            }
            // 主 JSON を空にした場合は記録なし。旧バックアップや別保存先で復元しない。
            // 各 JSON は完全な記録なので、保存先が変わった場合も最新ファイルだけを採用する。
            string fallbackPath = GetPath(FallbackDirectory, key);
            bool useFallback = !hasPrimaryFile || (record.HasPlayRecord && File.Exists(fallbackPath) &&
                File.GetLastWriteTimeUtc(fallbackPath) > File.GetLastWriteTimeUtc(primaryPath));
            if (useFallback && TryRead(fallbackPath, musicId, difficulty, out SongPlayRecord fallback))
            {
                record = fallback;
            }
            records[key] = record;
            return record;
        }

        // Score、Rank、Clear を個別の自己ベストとして更新する。Auto は保存しない。
        public bool RecordResult(int musicId, MusicDifficulty difficulty, LiveScore score,
            int maxClearGauge, int clearGaugeThreshold, bool wasAuto)
        {
            clearGaugeThreshold = Mathf.Clamp(clearGaugeThreshold, 1, Mathf.Max(1, maxClearGauge));
            LiveClearStatus clearStatus = score.GetClearStatus(clearGaugeThreshold, wasAuto);
            if (clearStatus == LiveClearStatus.AutoPlay) return false;

            SongPlayRecord previous = GetRecord(musicId, difficulty);
            SongPlayRecord current = previous.Merge(new SongPlayRecord(musicId, difficulty,
                Mathf.Max(0, score.totalScore), score.GetResultRank(), clearStatus));
            string key = GetRelativePath(musicId, difficulty);
            if (previous.HighScore == current.HighScore && previous.BestRank == current.BestRank &&
                previous.BestClearStatus == current.BestClearStatus && !pendingSaves.Contains(key))
            {
                return true;
            }

            records[key] = current;
            pendingSaves.Add(key);
            return Save(key, current);
        }

        // 一時的な I/O 失敗時はメモリに保持し、次の保存または終了時に再試行する。
        public bool Flush()
        {
            bool success = true;
            foreach (string key in new List<string>(pendingSaves))
            {
                if (!Save(key, records[key])) success = false;
            }
            return success;
        }

        private bool Save(string key, SongPlayRecord record)
        {
            string json = JsonUtility.ToJson(new RecordJson
            {
                musicId = record.MusicId,
                difficulty = record.Difficulty.ToString(),
                highScore = record.HighScore,
                bestRank = record.BestRank.ToString(),
                bestClearStatus = record.BestClearStatus.ToString()
            }, true);

            bool saved = TryWrite(GetPath(RecordsDirectory, key), json, out string primaryError);
            if (!saved && !string.IsNullOrEmpty(FallbackDirectory))
            {
                saved = TryWrite(GetPath(FallbackDirectory, key), json, out string fallbackError);
                if (saved && !hasWarnedAboutFallback)
                {
                    Debug.LogWarning("Play records are saved to " + FallbackDirectory +
                                     " because StreamingAssets is not writable. " + primaryError);
                    hasWarnedAboutFallback = true;
                }
                if (!saved) primaryError += " Fallback: " + fallbackError;
            }
            if (saved)
            {
                pendingSaves.Remove(key);
            }
            else
            {
                Debug.LogError("Failed to save play record " + key + ". The record remains in memory for retry. " + primaryError);
            }
            return saved;
        }

        private static bool TryWrite(string path, string json, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, json, JsonEncoding);
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporaryPath, path, path + ".bak");
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(path, path + ".bak", true);
                        File.Copy(temporaryPath, path, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
                return true;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                error = path + ": " + exception.Message;
                return false;
            }
        }

        private static bool TryRead(string path, int musicId, MusicDifficulty difficulty, out SongPlayRecord record)
        {
            record = new SongPlayRecord(musicId, difficulty);
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path, JsonEncoding);
                if (string.IsNullOrWhiteSpace(json)) return false;
                RecordJson data = JsonUtility.FromJson<RecordJson>(json);
                if (data == null || (string.IsNullOrEmpty(data.bestRank) && string.IsNullOrEmpty(data.bestClearStatus)) ||
                    (data.bestRank == "None" && data.bestClearStatus == "None")) return false;
                if (data.version != 1 || data.musicId != musicId || data.difficulty != difficulty.ToString() ||
                    data.highScore < 0 || !Enum.TryParse(data.bestRank, out ScoreRank rank) ||
                    rank < ScoreRank.S || rank > ScoreRank.D ||
                    !Enum.TryParse(data.bestClearStatus, out LiveClearStatus clear) ||
                    clear < LiveClearStatus.TrackCrash || clear > LiveClearStatus.AllPerfect)
                {
                    throw new InvalidDataException("Invalid play record fields or version.");
                }
                record = new SongPlayRecord(musicId, difficulty, data.highScore, rank, clear);
                return true;
            }
            catch (Exception exception) when (IsFileError(exception) || exception is ArgumentException)
            {
                Debug.LogWarning("Failed to load play record " + path + ": " + exception.Message);
                return false;
            }
        }

        private static string GetRelativePath(int musicId, MusicDifficulty difficulty)
        {
            if (!Enum.IsDefined(typeof(MusicDifficulty), difficulty)) throw new ArgumentOutOfRangeException(nameof(difficulty));
            return Path.Combine(musicId.ToString(CultureInfo.InvariantCulture), difficulty + ".json");
        }

        private static string GetPath(string directory, string relativePath)
        {
            return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, relativePath);
        }

        private static bool IsFileError(Exception exception)
        {
            return exception is IOException || exception is UnauthorizedAccessException ||
                   exception is SecurityException || exception is NotSupportedException;
        }
    }
}
