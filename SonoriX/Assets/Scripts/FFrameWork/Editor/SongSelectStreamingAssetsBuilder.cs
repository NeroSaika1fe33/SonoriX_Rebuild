//Create By Codex for Debug
using System.IO;
using FFrameWork.Songs;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FFrameWork.Editor
{
    public class SongSelectStreamingAssetsBuilder : IPreprocessBuildWithReport
    {
        // 手動で StreamingAssets 譜面を書き出すメニュー
        private const string MenuPath = "FFrameWork/Songs/Build StreamingAssets Charts";

        // StreamingAssets 内の譜面出力先
        private const string StreamingChartsRoot = "Assets/StreamingAssets/Charts";

        // Unity Build 前処理の実行順
        public int callbackOrder => 0;

        // 全 SongSelectEntry の譜面を StreamingAssets へ書き出す
        [MenuItem(MenuPath)]
        public static void BuildStreamingAssetsCharts()
        {
            int count = SyncAllSongSelectEntries();
            AssetDatabase.Refresh();
            Debug.LogFormat("Built {0} StreamingAssets chart files.", count);
        }

        // ビルド前に StreamingAssets の譜面ファイルを同期する
        // report: Unity のビルドレポート
        public void OnPreprocessBuild(BuildReport report)
        {
            SyncAllSongSelectEntries();
            AssetDatabase.Refresh();
        }

        // すべての SongSelectEntry を走査して SUS テキストを StreamingAssets へ保存する
        private static int SyncAllSongSelectEntries()
        {
            string[] guids = AssetDatabase.FindAssets("t:SongSelectEntry");
            int count = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                SongSelectEntry entry = AssetDatabase.LoadAssetAtPath<SongSelectEntry>(assetPath);
                if (entry == null || entry.SusScore == null)
                {
                    continue;
                }

                string relativePath = GetStreamingChartPath(entry);
                string outputPath = Path.Combine(Application.dataPath, "..", relativePath);
                string outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath, entry.SusScore.text);
                ConfigureEntryForStreamingAssets(entry, relativePath);
                count++;
            }

            AssetDatabase.SaveAssets();
            return count;
        }

        // SongSelectEntry に対応する StreamingAssets 内の譜面パスを作成する
        // entry: 対象の選曲エントリ
        private static string GetStreamingChartPath(SongSelectEntry entry)
        {
            string folderName = SanitizePathPart(!string.IsNullOrEmpty(entry.Title) ? entry.Title : entry.MusicId.ToString());
            string fileName = entry.Difficulty + ".txt";
            return NormalizePath(Path.Combine(StreamingChartsRoot, folderName, fileName));
        }

        // SongSelectEntry を StreamingAssets 読み込み設定へ更新する
        // entry: 更新対象の選曲エントリ
        // assetRelativePath: Assets から始まる出力パス
        private static void ConfigureEntryForStreamingAssets(SongSelectEntry entry, string assetRelativePath)
        {
            SerializedObject serializedObject = new SerializedObject(entry);
            serializedObject.FindProperty("loadScoreFromStreamingAssets").boolValue = true;
            serializedObject.FindProperty("streamingAssetsScorePath").stringValue = ToStreamingAssetsRelativePath(assetRelativePath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        // Assets/StreamingAssets からの相対パスへ変換する
        // assetRelativePath: Assets から始まるパス
        private static string ToStreamingAssetsRelativePath(string assetRelativePath)
        {
            const string prefix = "Assets/StreamingAssets/";
            string normalized = NormalizePath(assetRelativePath);
            return normalized.StartsWith(prefix) ? normalized.Substring(prefix.Length) : normalized;
        }

        // ファイル名に使えない文字を置換する
        // value: フォルダ名やファイル名に使う文字列
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

        // Unity で扱いやすいスラッシュ区切りパスへ正規化する
        // path: 正規化するパス
        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
