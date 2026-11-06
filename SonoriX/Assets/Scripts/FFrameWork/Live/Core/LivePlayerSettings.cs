using System;
using UnityEngine;

namespace FFrameWork.Live
{
    // プレイヤーごとのライブ設定を PlayerPrefs 経由で管理するクラス
    public static class LivePlayerSettings
    {
        // 設定可能な最低ノート速度
        public const float MinNoteSpeed = 1f;
        // 設定可能な最高ノート速度
        public const float MaxNoteSpeed = 12f;
        // ノート速度の丸め単位
        public const float NoteSpeedStep = 0.1f;

        // ノート速度保存用の PlayerPrefs キー
        private const string NoteSpeedPlayerPrefsKey = "FFrameWork.Live.NoteSpeed";
        // オートプレイ保存用の PlayerPrefs キー
        private const string AutoPlayPlayerPrefsKey = "FFrameWork.Live.AutoPlay";

        // ノート速度を読み込み済みかどうか
        private static bool isNoteSpeedLoaded;
        // オートプレイ設定を読み込み済みかどうか
        private static bool isAutoPlayLoaded;
        // 現在のノート速度
        private static float noteSpeed = LiveConfig.DefaultNoteSpeed;
        // 現在のオートプレイ設定
        private static bool autoPlay;

        // ノート速度変更時に呼ばれるイベント
        public static event Action<float> NoteSpeedChanged;
        // オートプレイ設定変更時に呼ばれるイベント
        public static event Action<bool> AutoPlayChanged;

        // 現在のノート速度
        public static float NoteSpeed
        {
            get
            {
                EnsureNoteSpeedLoaded();
                return noteSpeed;
            }
            set
            {
                SetNoteSpeed(value);
            }
        }

        // ノート速度を設定し、必要なら保存する
        // value: 設定したいノート速度
        // save: PlayerPrefs に保存するかどうか
        public static float SetNoteSpeed(float value, bool save = true)
        {
            EnsureNoteSpeedLoaded();

            float normalized = NormalizeNoteSpeed(value);
            if (Mathf.Approximately(noteSpeed, normalized))
            {
                return noteSpeed;
            }

            noteSpeed = normalized;
            LiveConfig.CacheSpeedTime = 0f;

            if (save)
            {
                PlayerPrefs.SetFloat(NoteSpeedPlayerPrefsKey, noteSpeed);
                PlayerPrefs.Save();
            }

            NoteSpeedChanged?.Invoke(noteSpeed);
            return noteSpeed;
        }

        // 現在のオートプレイ設定
        public static bool AutoPlay
        {
            get
            {
                EnsureAutoPlayLoaded(false);
                return autoPlay;
            }
            set
            {
                SetAutoPlay(value);
            }
        }

        // デフォルト値を指定してオートプレイ設定を取得する
        // defaultValue: 保存値がない場合に使用する初期値
        public static bool GetAutoPlay(bool defaultValue)
        {
            EnsureAutoPlayLoaded(defaultValue);
            return autoPlay;
        }

        // オートプレイ設定を変更し、必要なら保存する
        // enabled: オートプレイを有効にするかどうか
        // save: PlayerPrefs に保存するかどうか
        public static bool SetAutoPlay(bool enabled, bool save = true)
        {
            EnsureAutoPlayLoaded(enabled);
            if (autoPlay == enabled)
            {
                return autoPlay;
            }

            autoPlay = enabled;

            if (save)
            {
                PlayerPrefs.SetInt(AutoPlayPlayerPrefsKey, autoPlay ? 1 : 0);
                PlayerPrefs.Save();
            }

            AutoPlayChanged?.Invoke(autoPlay);
            return autoPlay;
        }

        // ノート速度を許容範囲とステップに合わせて正規化する
        // value: 正規化対象のノート速度
        public static float NormalizeNoteSpeed(float value)
        {
            float clamped = Mathf.Clamp(value, MinNoteSpeed, MaxNoteSpeed);
            return Mathf.Round(clamped / NoteSpeedStep) * NoteSpeedStep;
        }

        // ノート速度を PlayerPrefs から一度だけ読み込む
        private static void EnsureNoteSpeedLoaded()
        {
            if (isNoteSpeedLoaded)
            {
                return;
            }

            noteSpeed = NormalizeNoteSpeed(PlayerPrefs.GetFloat(NoteSpeedPlayerPrefsKey, LiveConfig.DefaultNoteSpeed));
            isNoteSpeedLoaded = true;
        }

        // オートプレイ設定を PlayerPrefs から一度だけ読み込む
        // defaultValue: 保存値がない場合に使用する初期値
        private static void EnsureAutoPlayLoaded(bool defaultValue)
        {
            if (isAutoPlayLoaded)
            {
                return;
            }

            autoPlay = PlayerPrefs.GetInt(AutoPlayPlayerPrefsKey, defaultValue ? 1 : 0) != 0;
            isAutoPlayLoaded = true;
        }
    }
}
