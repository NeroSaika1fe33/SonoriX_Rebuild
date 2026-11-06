using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // ライブ制御クラス共通の基底処理を持つ MonoBehaviour
    public class BaseLiveController : MonoBehaviour
    {
        // 背景表示に使用する RenderTexture
        [SerializeField] private RenderTexture backgroundTexture;

        // ライブ表示の基準となるカメラ
        [SerializeField] private Camera baseCamera;

        // ライブ中に更新対象となるパーティクル制御一覧
        public readonly List<ParticleSystemController> ParticleSystemControllers = new List<ParticleSystemController>();

        // ライブ開始前の VSync 設定値
        private int previousVsyncCount;

        // VSync 設定値を保存済みかどうか
        private bool hasPreviousVsyncCount;

        // 背景表示用 RenderTexture
        public RenderTexture BackgroundTexture
        {
            get { return backgroundTexture; }
            protected set { backgroundTexture = value; }
        }

        // ライブ起動時に使用する設定と譜面データ
        public LiveBootDataBase BootData { get; protected set; }

        // BootData から取得するライブ設定
        public LiveSettingData Settings
        {
            get { return BootData != null ? BootData.LiveSettingData : null; }
        }

        // ライブ表示の基準カメラ
        public Camera BaseCamera
        {
            get { return baseCamera; }
            set { baseCamera = value; }
        }

        // プレビュー用に BootData を直接設定する
        // bootData: プレビューで使用するライブ起動データ
        public void SetupBootDataForPreview(LiveBootDataBase bootData)
        {
            BootData = bootData;
        }

        // 楽曲イベントのパラメータ値を取得する
        // parameterName: 取得対象のパラメータ名
        // value: 取得できたパラメータ値
        public virtual bool TryGetMusicEventParameter(string parameterName, out float value)
        {
            value = 0f;
            return false;
        }

        // 楽曲イベントのパラメータ値を設定する
        // parameterName: 設定対象のパラメータ名
        // value: 設定する値
        // ignoreSeekSpeed: シーク速度などの補正を無視するかどうか
        public virtual bool TrySetMusicEventParameter(string parameterName, float value, bool ignoreSeekSpeed = false)
        {
            return false;
        }

        // ライブ設定に合わせてフレームレートと VSync を適用する
        protected void ApplyLiveFrameRateSettings()
        {
            LiveSettingData settings = Settings;
            if (settings == null)
            {
                return;
            }

            Application.targetFrameRate = settings.Use120FPS ? 120 : 60;
            if (!hasPreviousVsyncCount)
            {
                previousVsyncCount = QualitySettings.vSyncCount;
                hasPreviousVsyncCount = true;
            }

            QualitySettings.vSyncCount = settings.UseVSync.GetValueOrDefault() ? 1 : 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // 更新対象のパーティクル制御を登録する
        // controller: 登録するパーティクル制御
        public void RegisterParticleSystemController(ParticleSystemController controller)
        {
            if (controller == null || ParticleSystemControllers.Contains(controller))
            {
                return;
            }

            ParticleSystemControllers.Add(controller);
        }

        // 更新対象のパーティクル制御を解除する
        // controller: 解除するパーティクル制御
        public void UnregisterParticleSystemController(ParticleSystemController controller)
        {
            if (controller == null)
            {
                return;
            }

            ParticleSystemControllers.Remove(controller);
        }

        // ライブを一時停止する
        public virtual void Pause()
        {
        }

        // ライブを再開する
        public virtual void Resume()
        {
        }

        public virtual void ResumeNoCountDown()
        {
            Resume();
        }

        // 指定時刻からライブを継続する
        // time: 継続開始する楽曲時間
        public virtual void Continue(float time)
        {
        }

        // 共通の毎フレーム更新処理
        protected virtual void OnUpdate()
        {
            if (Time.frameCount % 5 != 0)
            {
                return;
            }

            for (int i = ParticleSystemControllers.Count - 1; i >= 0; i--)
            {
                ParticleSystemController controller = ParticleSystemControllers[i];
                if (controller == null)
                {
                    ParticleSystemControllers.RemoveAt(i);
                    continue;
                }

                controller.OnUpdate();
            }
        }

        // 破棄時にライブ中に変更した端末設定を元に戻す
        protected virtual void OnDestroy()
        {
            if (hasPreviousVsyncCount)
            {
                QualitySettings.vSyncCount = previousVsyncCount;
                hasPreviousVsyncCount = false;
            }

            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
