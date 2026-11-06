using System.Collections;
using FFrameWork;
using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using FMODUnity;

namespace FFrameWork.Live
{
    // 単独ライブの起動、進行、音楽再生、ポーズ、終了処理を管理するコントローラー
    public class LiveSceneController : BaseLiveController
    {
        private const float BaseGameStartWaitTime = 6f;
        private const float TimingAdjustFrameTime = 0.016667f;
        private const float StartTimeEpsilon = 0.0001f;

        [SerializeField] private LiveViewBase[] liveViews;
        [SerializeField] private int renderTextureWidth = 1920;
        [SerializeField] private int renderTextureHeight = 1080;
        [SerializeField] private int previewMusicId = 1;
        [SerializeField] private MusicCategory musicCategory = MusicCategory.Default;
        [SerializeField] private LivePlayMode livePlayMode = LivePlayMode.Free;
        [SerializeField] private int previewVocalId = 1;
        [SerializeField] private MusicDifficulty previewDifficulty = MusicDifficulty.Easy;
        [SerializeField] private int previewPlayLevel = 1;
        [SerializeField] private int previewTotalNoteCount;
        [SerializeField] private float previewTimingAdjustData;
        [SerializeField] private float previewFillerSec = 9f;
        [SerializeField, Range(1, 16)] private int previewInitialArrowLaneWidth = 2;
        [SerializeField] private string previewTitle = "DefaultMusic";
        [SerializeField] private string previewArtist = "Miku";
        [SerializeField] private string previewLyricist = "Miku";
        [SerializeField] private string previewComposer = "Miku";
        [SerializeField] private string previewArranger = "Miku";
        [SerializeField] private string previewVocalCaption = "Miku";
        [SerializeField] private string previewVocalType = "Miku";
        [SerializeField] private string previewVocalAssetBundleName;
        [SerializeField] private Texture2D previewJacket;
        [SerializeField] private bool isAuto;
        [SerializeField] private bool canSkipDisplayMusicInfo;
        [SerializeField] private bool isCollaboration;
        [SerializeField] private int collaborationId;
        [SerializeField] private string collaborationLabel;
        [SerializeField] private bool startOnAwake = true;
        [SerializeField] private bool musicStartOnAwake = true;
        [SerializeField] private float liveStartWaitTime = BaseGameStartWaitTime;
        [SerializeField] private TextAsset previewSusScore;
        [SerializeField] private LiveBundleBuildData liveBundleBuildData;
        [SerializeField] private ArrowController arrowController;
        [SerializeField] private AudioEffectController audioEffectController;
        
        [Header("FMOD")]
        [SerializeField] private EventReference previewMusicEvent;
        private EventInstance musicEventInstance;
        private bool hasMusicEventInstance;
        
        private Coroutine liveStartCoroutine;
        private Coroutine resumeCoroutine;
        private Coroutine resultCoroutine;
        private LiveLogic liveLogic;
        private InputManager liveInputManager;
        private LiveResult result;
        private SongCatalogItem appliedSong;
        private bool isRhythmGameRunning;
        private bool isPaused;
        private float currentMusicTime;
        private float adjustedMusicTime;
        private float liveTimelineStartUnityTime = -1f;
        private float musicTimelineOffset;
        private IMusicTimer musicTimer;
        private int currentScreenWidth;
        private int currentScreenHeight;
        private LiveScore lastScore;
        private LiveResult lastResult;
        private bool lastWasAuto;

        public event System.Action<LiveResult> LiveEnded;

        public LiveScore LastScore => lastScore;
        public LiveResult LastResult => lastResult;
        public bool LastWasAuto => lastWasAuto;
        public bool IsAutoPlay => LivePlayerSettings.GetAutoPlay(isAuto);
        public int LastMaxClearGauge => GetConfiguredMaxClearGauge();
        public int LastClearGaugeThreshold => GetConfiguredClearGaugeThreshold();

        // 起動時に必要ならライブセットアップを実行する
        protected virtual void Awake()
        {
            if (startOnAwake)
            {
                Setup();
            }
        }

        // BootData、描画、View、UI を初期化し、設定に応じてライブを開始する
        public virtual void Setup()
        {
            EnsureBootData();
            ApplyLiveFrameRateSettings();
            EnsureBackgroundTexture();
            SetupLiveViews();
            SetupLiveOutUIController();
            CacheScreenSize();

            if (!musicStartOnAwake)
            {
                return;
            }

            StartLive();
        }

        // 手動開始用に、音楽自動開始を一時的に無効化してセットアップする
        public void SetupForManualStart()
        {
            bool originalMusicStartOnAwake = musicStartOnAwake;
            musicStartOnAwake = false;
            Setup();
            musicStartOnAwake = originalMusicStartOnAwake;
        }

        // ライブ開始コルーチンを開始する
        public void StartLive()
        {
            if (liveStartCoroutine != null)
            {
                StopCoroutine(liveStartCoroutine);
            }

            arrowController?.ResetForSong(GetInitialArrowLaneWidth());
            liveStartCoroutine = StartCoroutine(LiveStart(liveStartWaitTime));
        }

        // 再生、コルーチン、ロジック、View を初期状態へ戻す
        public void ResetLive()
        {
            DestroyActivePauseDialog();
            if (liveStartCoroutine != null)
            {
                StopCoroutine(liveStartCoroutine);
                liveStartCoroutine = null;
            }
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            if (resultCoroutine != null)
            {
                StopCoroutine(resultCoroutine);
                resultCoroutine = null;
            }

            StopMusic();
            UnsubscribeLiveLogic();
            liveLogic = null;
            result = LiveResult.None;
            isPaused = false;
            isRhythmGameRunning = false;
            currentMusicTime = 0f;
            liveTimelineStartUnityTime = -1f;
            musicTimelineOffset = 0f;
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);

            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.Retry();
            }
        }

        // 選曲画面から渡された楽曲情報をプレビュー設定へ反映する
        // song: 適用する楽曲選択情報
        public void ApplySong(SongCatalogItem song)
        {
            if (song == null)
            {
                Debug.LogWarning("SongCatalogItem is null.", this);
                return;
            }

            previewMusicId = song.MusicId;
            previewTitle = song.Title;
            previewArtist = song.Artist;
            previewDifficulty = song.Difficulty;
            previewPlayLevel = song.PlayLevel;
            previewTotalNoteCount = song.TotalNoteCount;
            previewFillerSec = song.FillerSec;
            previewInitialArrowLaneWidth = song.InitialArrowLaneWidth;
            previewJacket = song.Jacket;
            previewSusScore = null;
            previewMusicEvent = song.MusicEvent;
            liveBundleBuildData = song.BundleBuildData;
            appliedSong = song;
            BootData = null;
        }

        // BootData がなければプレビュー用データを作成し、既存データには共通設定を適用する
        protected virtual void EnsureBootData()
        {
            if (BootData != null)
            {
                if (BootData.LiveSettingData == null)
                {
                    BootData.LiveSettingData = new LiveSettingData();
                }
                LiveBootFactory.ApplyGlobalPlayerSettings(BootData.LiveSettingData);
                if (BootData.BundleBuildData == null)
                {
                    BootData.BundleBuildData = liveBundleBuildData;
                }

                return;
            }

            SetupBootDataForPreview(LiveBootFactory.CreateFreeLiveBootData(CreatePreviewCatalogItem(), IsAutoPlay));
        }

        // インスペクタ設定からプレビュー用 BootData を作成する
        private SongCatalogItem CreatePreviewCatalogItem()
        {
            return new SongCatalogItem
            {
                MusicId = previewMusicId,
                Title = previewTitle,
                Artist = previewArtist,
                Composer = previewComposer,
                Jacket = previewJacket,
                ScoreText = previewSusScore != null ? previewSusScore.text : null,
                MusicEvent = previewMusicEvent,
                Difficulty = previewDifficulty,
                PlayLevel = previewPlayLevel,
                TotalNoteCount = previewTotalNoteCount,
                FillerSec = previewFillerSec,
                InitialArrowLaneWidth = previewInitialArrowLaneWidth,
                BundleBuildData = liveBundleBuildData,
                VocalId = previewVocalId,
                LivePlayMode = livePlayMode,
                MusicCategory = musicCategory,
                TimingAdjustData = previewTimingAdjustData,
                CanSkipDisplayMusicInfo = canSkipDisplayMusicInfo
            };
        }

        // プレイヤー共通設定をライブ設定へ反映する
        // settings: 反映対象のライブ設定
        // プレビュー用のランクしきい値を生成する
        // playLevel: 表示用の難易度レベル
        // totalNoteCount: 譜面内の総ノート数
        // 難易度 enum を譜面読み込み用の文字列へ変換する
        // difficulty: 変換対象の難易度
        // 音楽再生を開始し、各 View に音楽開始を通知する
        protected virtual void OnMusicStart()
        {
            PlayMusic();

            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    liveViews[i].MusicStart(GetAdjustedMusicTime(currentMusicTime));
                }
            }
        }

        // リズムゲーム本体を開始し、ロジックと View を初期化する
        protected virtual void OnRhythmGameStart()
        {
            SetupLiveLogic();
            liveLogic?.RefreshInput();
            result = LiveResult.None;
            lastScore = liveLogic != null ? liveLogic.Score : default(LiveScore);
            lastResult = LiveResult.None;
            lastWasAuto = BootData != null && BootData.IsAuto;
            isRhythmGameRunning = true;
            isPaused = false;

            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    liveViews[i].RhythmGameStart();
                }
            }
        }

        // 毎フレーム、入力、譜面進行、View 更新、終了判定を処理する
        protected virtual void Update()
        {
            base.OnUpdate();
            CheckBackKey();
            CheckScreenSizeChanged();

            if (!isRhythmGameRunning || isPaused)
            {
                return;
            }

            currentMusicTime = GetCurrentMusicTime();
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
            float scoreInfoTime = adjustedMusicTime - GetMusicFillerSec();
            liveLogic?.OnUpdate(scoreInfoTime);
            if (liveLogic != null && liveLogic.Result == LiveResult.Failure && liveLogic.IsNotesAllFinished)
            {
                OnFinished();
            }

            if (BootData != null && BootData.IsAuto)
            {
                liveLogic?.OnAutoInput();
            }
            else
            {
                liveLogic?.OnInput();
            }

            if (liveViews == null)
            {
                return;
            }

            MusicScoreInfo currentFrameInfo = liveLogic != null ? liveLogic.CurrentFrameInfo : default(MusicScoreInfo);
            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.OnUpdate(adjustedMusicTime, currentFrameInfo);
            }
        }

        // 破棄時に View、音楽、ロジック、RenderTexture を解放する
        protected override void OnDestroy()
        {
            if (liveViews != null)
            {
                for (int i = 0; i < liveViews.Length; i++)
                {
                    if (liveViews[i] != null)
                    {
                        liveViews[i].OnUnload();
                    }
                }
            }

            isRhythmGameRunning = false;
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            StopMusic();
            UnsubscribeLiveLogic();
            DisposeLiveInputManager();
            if (resultCoroutine != null)
            {
                StopCoroutine(resultCoroutine);
                resultCoroutine = null;
            }

            if (BackgroundTexture != null)
            {
                BackgroundTexture.Release();
                BackgroundTexture = null;
            }

            base.OnDestroy();
        }

        // ライブを一時停止し、ポーズ表示を出す
        public override void Pause()
        {
            if (!isRhythmGameRunning || result != LiveResult.None)
            {
                return;
            }

            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }

            if (isPaused)
            {
                ShowPauseDialog();
                return;
            }

            isPaused = true;
            currentMusicTime = GetCurrentMusicTime();
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
            PauseMusic();
            NotifyPause(adjustedMusicTime);
            ShowPauseDialog();
        }

        // カウントダウン付きでライブ再開を開始する
        public override void Resume()
        {
            if (!isRhythmGameRunning || !isPaused || result != LiveResult.None)
            {
                return;
            }

            NotifyCountdown();
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
            }
            DestroyActivePauseDialog();
            resumeCoroutine = StartCoroutine(ResumeCoroutine());
        }

        // カウントダウンなしでライブを即時再開する
        public override void ResumeNoCountDown()
        {
            if (!isRhythmGameRunning || !isPaused || result != LiveResult.None)
            {
                return;
            }

            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            DestroyActivePauseDialog();
            ResumeLiveNow();
        }

        // FMOD イベントのパラメータ値を取得する
        // parameterName: 取得対象のパラメータ名
        // value: 取得できた値
        public override bool TryGetMusicEventParameter(string parameterName, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(parameterName) ||
                !hasMusicEventInstance ||
                !musicEventInstance.isValid())
            {
                return false;
            }

            return musicEventInstance.getParameterByName(parameterName, out value) == FMOD.RESULT.OK;
        }

        // FMOD イベントのパラメータ値を設定する
        // parameterName: 設定対象のパラメータ名
        // value: 設定する値
        // ignoreSeekSpeed: FMOD のシーク速度を無視するかどうか
        public override bool TrySetMusicEventParameter(string parameterName, float value, bool ignoreSeekSpeed = false)
        {
            if (string.IsNullOrEmpty(parameterName) ||
                !hasMusicEventInstance ||
                !musicEventInstance.isValid())
            {
                return false;
            }

            return musicEventInstance.setParameterByName(parameterName, value, ignoreSeekSpeed) == FMOD.RESULT.OK;
        }

        // 指定時刻からライブをコンティニューする
        // time: 再開する楽曲時間
        public override void Continue(float time)
        {
            if (liveLogic == null)
            {
                return;
            }

            result = LiveResult.None;
            isPaused = false;
            isRhythmGameRunning = true;
            liveLogic.Continue(time);
            currentMusicTime = time;
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
            SeekMusic(time);
            ResumeMusic();
            NotifyResume(adjustedMusicTime);
        }

        // オートプレイ設定を変更する
        // enabled: オートプレイを有効にするかどうか
        public void SetAutoPlay(bool enabled)
        {
            isAuto = LivePlayerSettings.SetAutoPlay(enabled);
            if (BootData != null && !isRhythmGameRunning)
            {
                BootData.IsAuto = isAuto;
            }
        }

        // オートプレイ設定を反転して返す
        public bool ToggleAutoPlay()
        {
            SetAutoPlay(!IsAutoPlay);
            return IsAutoPlay;
        }

        // パネル退出時にライブを停止し、最後の結果を強制設定する
        // forcedResult: 停止時に記録する結果
        public void StopLiveForPanelExit(LiveResult forcedResult = LiveResult.Retire)
        {
            bool hasActiveLive = liveLogic != null || isRhythmGameRunning || liveStartCoroutine != null ||
                                 resultCoroutine != null || hasMusicEventInstance;

            if (liveLogic != null)
            {
                lastScore = liveLogic.Score;
                lastWasAuto = BootData != null && BootData.IsAuto;
            }

            ResetLive();

            if (hasActiveLive)
            {
                lastResult = forcedResult;
            }
        }

        // 再開カウントダウン後にライブを実際に再開する
        private IEnumerator ResumeCoroutine()
        {
            liveLogic?.RefreshInput();
            yield return new WaitForSeconds(3f);
            ResumeLiveNow();
            resumeCoroutine = null;
        }

        // 現在時刻から音楽と譜面入力を即時再開する
        private void ResumeLiveNow()
        {
            if (!isRhythmGameRunning || result != LiveResult.None)
            {
                return;
            }

            isPaused = false;
            SeekMusic(currentMusicTime);
            ResumeMusic();
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
            liveLogic?.RefreshInput();
            NotifyResume(adjustedMusicTime);
        }

        // 譜面を読み込み、LiveLogic を生成して必要な依存を設定する
        private void SetupLiveLogic()
        {
            string scoreText = appliedSong != null ? appliedSong.ScoreText : CreatePreviewCatalogItem().ScoreText;
            MusicScore musicScore = MusicScoreFactory.Create(scoreText, liveBundleBuildData);
            UnsubscribeLiveLogic();
            InputManager inputManager = EnsureLiveInputManager();
            arrowController?.SetInputManager(inputManager);
            liveLogic = new LiveLogic(liveBundleBuildData);
            liveLogic.SetInputManager(inputManager);
            liveLogic.Setup(BootData, liveViews, musicScore);
            liveLogic.SetArrowController(arrowController);
            liveLogic.SetAudioEffectController(ResolveAudioEffectController());
            liveLogic.OnFinished += OnFinished;
            liveLogic.OnFailure += OnFailure;
        }

        // ライブシーン内で共有する入力管理を用意する
        private InputManager EnsureLiveInputManager()
        {
            if (liveInputManager == null)
            {
                liveInputManager = new InputManager();
            }

            liveInputManager.EnableInLiveInput();
            return liveInputManager;
        }

        // ライブシーンが所有する入力管理を破棄する
        private void DisposeLiveInputManager()
        {
            liveInputManager?.Dispose();
            liveInputManager = null;
        }

        // AudioEffectController を取得または生成し、ライブコントローラーを設定する
        private AudioEffectController ResolveAudioEffectController()
        {
            if (audioEffectController == null)
            {
                audioEffectController = GetComponentInChildren<AudioEffectController>(true);
            }

            if (audioEffectController == null)
            {
                FmodAudioEffectSystem audioEffectSystem = GetComponentInChildren<FmodAudioEffectSystem>(true);
                if (audioEffectSystem != null)
                {
                    audioEffectController = audioEffectSystem.GetComponent<AudioEffectController>();
                    if (audioEffectController == null)
                    {
                        audioEffectController = audioEffectSystem.gameObject.AddComponent<AudioEffectController>();
                    }
                }
            }

            if (audioEffectController != null)
            {
                audioEffectController.Setup(this);
            }

            return audioEffectController;
        }

        // 現在の画面サイズを保存する
        private void CacheScreenSize()
        {
            currentScreenWidth = Screen.width;
            currentScreenHeight = Screen.height;
        }

        // 画面サイズ変更を検知して View と入力状態を更新する
        private void CheckScreenSizeChanged()
        {
            if (currentScreenWidth == Screen.width && currentScreenHeight == Screen.height)
            {
                return;
            }

            CacheScreenSize();
            liveViews.OnScreenSizeChanged();
            liveLogic?.RefreshInput();
        }

        // ライブ View をセットアップし、ロード通知を送る
        private void SetupLiveViews()
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    liveViews[i].Setup(this);
                }
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                if (liveViews[i] != null)
                {
                    liveViews[i].OnLoad();
                }
            }
        }

        // 音楽準備後、音楽開始とリズムゲーム開始を指定タイミングで実行する
        // waitTime: リズムゲーム開始までの待機時間
        private IEnumerator LiveStart(float waitTime)
        {
            yield return StartCoroutine(MusicReady());

            float musicStartDelay = GetMusicStartDelaySec();
            float rhythmGameStartDelay = Mathf.Max(waitTime, 0f);
            BeginLiveTimeline(musicStartDelay);

            bool isMusicStarted = false;
            bool isRhythmGameStarted = false;

            while (!isMusicStarted || !isRhythmGameStarted)
            {
                float nextStartTime = GetNextStartTime(
                    isMusicStarted,
                    musicStartDelay,
                    isRhythmGameStarted,
                    rhythmGameStartDelay);

                yield return StartCoroutine(WaitUntilLiveTime(nextStartTime));

                float liveTime = GetCurrentMusicTime();
                if (!isMusicStarted && musicStartDelay <= liveTime + StartTimeEpsilon)
                {
                    OnMusicStart();
                    isMusicStarted = true;
                }

                if (!isRhythmGameStarted && rhythmGameStartDelay <= liveTime + StartTimeEpsilon)
                {
                    OnRhythmGameStart();
                    isRhythmGameStarted = true;
                }
            }

            liveStartCoroutine = null;
        }

        // 失敗時に現在の楽曲時間を保存する
        private void OnFailure()
        {
            currentMusicTime = GetCurrentMusicTime();
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
        }

        // ライブ終了時に結果表示へ進む
        protected virtual void OnFinished()
        {
            float musicLength = GetMusicLength();
            float waitTime = Mathf.Max(musicLength - currentMusicTime - 1f, 4f);
            PreExit(1f, waitTime);
        }

        // ライブ終了前処理を開始し、結果表示コルーチンへ進む
        // delay: 結果表示前の待機時間
        // waitTime: 結果表示中の待機時間
        protected virtual void PreExit(float delay = 0f, float waitTime = 4f)
        {
            if (result != LiveResult.None && result != LiveResult.Retire)
            {
                return;
            }

            LiveResult requestedResult = result;
            isRhythmGameRunning = false;
            isPaused = false;
            if (liveStartCoroutine != null)
            {
                StopCoroutine(liveStartCoroutine);
                liveStartCoroutine = null;
            }
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            DestroyActivePauseDialog();
            result = requestedResult != LiveResult.None
                ? requestedResult
                : liveLogic != null && liveLogic.Result == LiveResult.Failure ? LiveResult.Clear : liveLogic != null ? liveLogic.Result : LiveResult.None;
            lastScore = liveLogic != null ? liveLogic.Score : lastScore;
            lastResult = result;
            lastWasAuto = BootData != null && BootData.IsAuto;
            UnsubscribeLiveLogic();

            if (resultCoroutine != null)
            {
                StopCoroutine(resultCoroutine);
            }
            resultCoroutine = StartCoroutine(PreExitCoroutine(delay, waitTime));
        }

        // 結果表示開始までの遅延を待ってから結果アニメーションへ進む
        // delay: 結果表示前の待機時間
        // waitTime: 結果表示中の待機時間
        private IEnumerator PreExitCoroutine(float delay, float waitTime)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return StartCoroutine(ResultAnim(waitTime));
            resultCoroutine = null;
        }

        // 結果表示の待機を行い、最後に終了通知へ進む
        // waitTime: 結果表示中の待機時間
        private IEnumerator ResultAnim(float waitTime = 4f)
        {
            //liveViews.Result(GetLiveResultAnimationType());
            yield return new WaitForSeconds(waitTime);
            //liveViews.Finish();
            yield return new WaitForSeconds(2f);
            Exit();
        }

        // ライブ終了イベントを通知する
        protected virtual void Exit()
        {
            LiveEnded?.Invoke(result);
        }

        private int GetConfiguredMaxClearGauge()
        {
            return liveBundleBuildData != null ? Mathf.Max(1, liveBundleBuildData.MaxClearGauge) : LiveConfig.DefaultMaxClearGauge;
        }

        private int GetConfiguredClearGaugeThreshold()
        {
            int maxClearGauge = GetConfiguredMaxClearGauge();
            int clearGaugeThreshold = liveBundleBuildData != null ? liveBundleBuildData.ClearGaugeThreshold : Mathf.FloorToInt(maxClearGauge * 0.7f);
            return Mathf.Clamp(clearGaugeThreshold, 1, maxClearGauge);
        }

        // ライブをリセットして再挑戦する
        protected virtual void Retry()
        {
            ResetLive();
            StartLive();
        }

        // ライブをリタイア扱いで終了する
        protected virtual void Retire()
        {
            DestroyActivePauseDialog();
            result = LiveResult.Retire;
            PreExit();
        }

        // LiveLogic のイベント購読を解除し、ロジックを破棄する
        private void UnsubscribeLiveLogic()
        {
            if (liveLogic == null)
            {
                return;
            }

            liveLogic.OnFinished -= OnFinished;
            liveLogic.OnFailure -= OnFailure;
            liveLogic.Dispose();
            liveLogic = null;
        }

        // 背景表示用 RenderTexture がなければ生成する
        private void EnsureBackgroundTexture()
        {
            if (BackgroundTexture != null)
            {
                return;
            }

            BackgroundTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0)
            {
                name = "LiveBackgroundTexture"
            };
            BackgroundTexture.Create();
        }

        // 音楽再生前の準備処理を返す
        private IEnumerator MusicReady()
        {
            return FmodMusicReady();
        }

        // 現在のタイムライン位置から音楽を再生する
        private void PlayMusic()
        {
            currentMusicTime = Mathf.Max(GetCurrentMusicTime(), musicTimelineOffset);
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
            PlayFmodMusic();
        }

        // 音楽再生を停止し、タイマー状態を初期化する
        private void StopMusic()
        {
            StopFmodMusic();
            musicTimer = null;
            liveTimelineStartUnityTime = -1f;
            musicTimelineOffset = 0f;
        }

        // 音楽再生を一時停止する
        private void PauseMusic()
        {
            PauseFmodMusic();
        }

        // 音楽再生を再開する
        private void ResumeMusic()
        {
            ResumeFmodMusic();
        }

        // 音楽タイムラインを指定時刻へ移動する
        // time: 移動先の楽曲時間
        private void SeekMusic(float time)
        {
            if (musicTimer != null)
            {
                float audioTime = Mathf.Max(time - musicTimelineOffset, 0f);
                musicTimer.Reset(Mathf.FloorToInt(audioTime * 1000f), Time.time);
                return;
            }

            if (liveTimelineStartUnityTime >= 0f)
            {
                liveTimelineStartUnityTime = Time.time - Mathf.Max(time, 0f);
            }
        }

        // 各 View へポーズ通知を送る
        // time: ポーズ時点の補正済み楽曲時間
        private void NotifyPause(float time)
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.Pause(time);
            }
        }

        // 各 View へ再開カウントダウン通知を送る
        private void NotifyCountdown()
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.Countdown();
            }
        }

        // 各 View へ再開通知を送る
        // time: 再開時点の補正済み楽曲時間
        private void NotifyResume(float time)
        {
            if (liveViews == null)
            {
                return;
            }

            for (int i = 0; i < liveViews.Length; i++)
            {
                liveViews[i]?.Resume(time);
            }
        }

        // 現在の楽曲時間を音楽タイマーまたは Unity 時間から取得する
        private float GetCurrentMusicTime()
        {
            if (musicTimer != null)
            {
                musicTimer.Execute(Time.time);
                return Mathf.Max((float)musicTimer.PlaybackTime / 1000f + musicTimelineOffset, 0f);
            }

            if (liveTimelineStartUnityTime >= 0f)
            {
                return isPaused ? currentMusicTime : Mathf.Max(Time.time - liveTimelineStartUnityTime, 0f);
            }

            return currentMusicTime;
        }

        // ライブタイムラインの開始基準を設定する
        // musicStartDelay: 音楽開始までの遅延時間
        private void BeginLiveTimeline(float musicStartDelay)
        {
            musicTimelineOffset = Mathf.Max(musicStartDelay, 0f);
            liveTimelineStartUnityTime = Time.time;
            currentMusicTime = 0f;
            adjustedMusicTime = GetAdjustedMusicTime(currentMusicTime);
        }

        // 指定されたライブ時間に到達するまで待つ
        // targetTime: 待機終了するライブ時間
        private IEnumerator WaitUntilLiveTime(float targetTime)
        {
            while (GetCurrentMusicTime() + StartTimeEpsilon < targetTime)
            {
                yield return null;
            }
        }

        // 次に待つべき音楽開始またはリズムゲーム開始時刻を取得する
        // isMusicStarted: 音楽が開始済みかどうか
        // musicStartDelay: 音楽開始時刻
        // isRhythmGameStarted: リズムゲームが開始済みかどうか
        // rhythmGameStartDelay: リズムゲーム開始時刻
        private static float GetNextStartTime(
            bool isMusicStarted,
            float musicStartDelay,
            bool isRhythmGameStarted,
            float rhythmGameStartDelay)
        {
            if (isMusicStarted)
            {
                return rhythmGameStartDelay;
            }

            if (isRhythmGameStarted)
            {
                return musicStartDelay;
            }

            return Mathf.Min(musicStartDelay, rhythmGameStartDelay);
        }

        // 楽曲の譜面開始前フィラー時間を取得する
        private float GetMusicFillerSec()
        {
            return BootData != null &&
                   BootData.MusicData != null &&
                   BootData.MusicData.Music != null
                ? BootData.MusicData.Music.fillerSec
                : 0f;
        }

        // 初期矢印レーン幅を取得する
        private int GetInitialArrowLaneWidth()
        {
            return BootData != null ? BootData.InitialArrowLaneWidth : previewInitialArrowLaneWidth;
        }

        // 音楽開始までの遅延秒数を取得する
        private float GetMusicStartDelaySec()
        {
            return Mathf.Max(GetMusicFillerSec(), 0f);
        }

        // タイミング調整を反映した楽曲時間を取得する
        // time: 補正前の楽曲時間
        private float GetAdjustedMusicTime(float time)
        {
            LiveSettingData settings = Settings;
            float timingAdjust = settings != null ? settings.TimingAdjustData * TimingAdjustFrameTime : 0f;
            return Mathf.Max(time + timingAdjust, 0f);
        }

        // FMOD music
        // FMOD イベントのサンプルデータ読み込み完了を待つ
        private IEnumerator FmodMusicReady()
        {
            if (previewMusicEvent.IsNull)
            {
                yield break;
            }

            EventDescription description = RuntimeManager.GetEventDescription(previewMusicEvent);
            if (!description.isValid())
            {
                yield break;
            }

            description.loadSampleData();
            while (description.getSampleLoadingState(out LOADING_STATE loadingState) == FMOD.RESULT.OK &&
                   loadingState == LOADING_STATE.LOADING)
            {
                yield return null;
            }
        }

        // FMOD イベントインスタンスを生成して再生する
        private void PlayFmodMusic()
        {
            StopFmodMusic();

            if (previewMusicEvent.IsNull)
            {
                musicTimer = null;
                return;
            }

            musicEventInstance = RuntimeManager.CreateInstance(previewMusicEvent);
            if (!musicEventInstance.isValid())
            {
                musicTimer = null;
                hasMusicEventInstance = false;
                return;
            }

            musicEventInstance.setTimelinePosition(0);
            musicEventInstance.start();
            hasMusicEventInstance = true;

            musicTimer = new FmodSyncedUnityTimer(musicEventInstance);
            musicTimer.Reset(0, Time.time);
        }

        // FMOD イベントインスタンスを停止して解放する
        private void StopFmodMusic()
        {
            if (!hasMusicEventInstance || !musicEventInstance.isValid())
            {
                hasMusicEventInstance = false;
                return;
            }

            musicEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicEventInstance.release();
            hasMusicEventInstance = false;
        }

        // FMOD イベントインスタンスを一時停止する
        private void PauseFmodMusic()
        {
            if (hasMusicEventInstance && musicEventInstance.isValid())
            {
                musicEventInstance.setPaused(true);
            }
        }

        // FMOD イベントインスタンスを再開する
        private void ResumeFmodMusic()
        {
            if (hasMusicEventInstance && musicEventInstance.isValid())
            {
                musicEventInstance.setPaused(false);
            }
        }

        // FMOD イベントの長さを秒単位で取得する
        private float GetMusicLength()
        {
            if (previewMusicEvent.IsNull)
            {
                return currentMusicTime;
            }

            EventDescription description = RuntimeManager.GetEventDescription(previewMusicEvent);
            if (!description.isValid() || description.getLength(out int lengthMs) != FMOD.RESULT.OK)
            {
                return currentMusicTime;
            }

            return Mathf.Max(lengthMs / 1000f + musicTimelineOffset, currentMusicTime);
        }

        // Escape キーでポーズまたは再開を切り替える
        private void CheckBackKey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // if (liveOutUIController != null && liveOutUIController.ActiveDialog != null)
            // {
            //     liveOutUIController.ExecuteBackKeyProcess();
            //     return;
            // }

            if (isPaused)
            {
                Resume();
            }
            else if (isRhythmGameRunning)
            {
                Pause();
            }
        }

        // 既存のポーズダイアログを破棄してからポーズダイアログを表示する
        private void ShowPauseDialog()
        {
            DestroyActivePauseDialog();
            ShowPauseDialogInternal();
        }

        // ポーズダイアログ本体を表示する
        private void ShowPauseDialogInternal()
        {
            // if (liveOutUIController == null)
            // {
            //     return;
            // }

            // if (AutoLiveUtility.IsRunningConsecutiveAutoLive())
            // {
            //     liveOutUIController.ShowConsecutiveAutoLivePauseDialog(Resume, ShowConsecutiveAutoLiveRetireDialog);
            //     return;
            // }

            //liveOutUIController.ShowPauseDialog(Resume, OnRetireConfirm, OnRetryConfirm);
        }

        // ライブ退出 UI の初期化を行う
        private void SetupLiveOutUIController()
        {
            // if (liveOutUIController == null)
            // {
            //     liveOutUIController = GetComponent<LiveOutUIController>();
            // }
            //
            // if (liveOutUIController == null)
            // {
            //     return;
            // }

            LivePlayMode currentPlayMode = BootData != null ? BootData.LivePlayMode : livePlayMode;
            //liveOutUIController.Initialize(currentPlayMode, BaseCamera);
        }

        // リタイア確認ダイアログを表示する
        private void OnRetireConfirm()
        {
            ShowConfirmRetireDialog();
        }

        // リトライ確認ダイアログを表示する
        private void OnRetryConfirm()
        {
            ShowConfirmRetryDialog();
        }

        // 確認ダイアログを閉じてポーズダイアログへ戻る
        private void OnConfirmCancel()
        {
            ShowPauseDialogInternal();
        }

        // リタイア確認ダイアログを表示する
        private void ShowConfirmRetireDialog()
        {
            // if (liveOutUIController == null)
            // {
            //     OnConfirmCancel();
            //     return;
            // }
            //
            // liveOutUIController.ShowConfirmRetireDialog(Retire, OnConfirmCancel);
        }

        // リトライ確認ダイアログを表示する
        private void ShowConfirmRetryDialog()
        {
            // if (liveOutUIController == null)
            // {
            //     OnConfirmCancel();
            //     return;
            // }
            //
            // liveOutUIController.ShowConfirmRetryDialog(Retry, OnConfirmCancel);
        }

        // 連続オートライブ中のリタイア確認ダイアログを表示する
        private void ShowConsecutiveAutoLiveRetireDialog()
        {
            // if (liveOutUIController == null)
            // {
            //     Resume();
            //     return;
            // }
            //
            // liveOutUIController.ShowConsecutiveAutoLiveConfirmRetireDialog(Retire, Resume);
        }

        // 表示中のポーズ関連ダイアログを破棄する
        private void DestroyActivePauseDialog()
        {
            //liveOutUIController?.DestroyInstance();
        }

    }
}
