using System;
using FMOD.Studio;
using FMODUnity;
using FFrameWork.Live;
using FFrameWork.Songs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // タイトル、操作説明、選曲、ライブ、リザルトの画面遷移を管理する
    public class LivePanelFlowController : MonoBehaviour
    {
        // 現在表示しているパネル状態
        private enum FlowState
        {
            GameStart,
            SelectSong,
            NoteSpeedSettings,
            OperationGuidePrompt,
            OperationGuide,
            Live,
            Result,
            TitleIdleAnimation
        }

        [SerializeField] private SongSelectDataBase songDataBase;
        [SerializeField] private LiveSceneController liveController;
        [SerializeField] private GameObject selectSongPanel;
        [SerializeField] private GameObject gameStartPanel;
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject noteSpeedSettingsPanel;
        [SerializeField] private GameObject operationGuidePromptPanel;
        [SerializeField] private GameObject operationGuidePanel;
        [SerializeField] private SongSelectMenuController songSelectMenu;
        [SerializeField] private int defaultSongIndex;
        [SerializeField] private bool createDefaultNoteSpeedSettingsPanel = true;
        [SerializeField] private bool createDefaultOperationGuidePanels = true;
        [Header("Title Idle Animation")]
        [SerializeField] private bool enableTitleIdleAnimation = true;
        [SerializeField, Min(0.1f)] private float titleIdleWaitSeconds = 60f;
        [SerializeField] private TitleIdleAnimationView titleIdleAnimationView;
        [SerializeField] private UnityEngine.Video.VideoClip titleIdleVideoClip;
        [Tooltip("Optional FMOD 2D event for the video's audio. Place audio at timeline 0.")]
        [SerializeField] private EventReference titleIdleAudioEvent;
        [SerializeField, Range(0f, 2f)] private float titleIdleAudioVolume = 1f;
        [Tooltip("0 = play until the video or non-looping animation ends.")]
        [SerializeField, Min(0f)] private float titleIdlePlaybackSeconds = 0f;
        [SerializeField] private bool muteMenuBgmDuringTitleIdle = true;
        [Header("Selected Song Records")]
        [SerializeField] private SelectedSongInfoWindow selectedSongInfoWindowPrefab;
        [SerializeField] private bool createDefaultSelectedSongInfoWindow = true;
        [Header("Panel Transition")]
        [SerializeField] private PanelTransitionWipeController panelTransition;
        [SerializeField] private bool animatePanelTransitions = true;
        [SerializeField] private float panelTransitionInputLockSeconds = 0.5f;
        [Header("Panel BGM")]
        [SerializeField] private EventReference menuBgmEvent;
        [SerializeField] private EventReference resultBgmEvent;
        [SerializeField, Range(0f, 2f)] private float menuBgmVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float resultBgmVolume = 1f;
        [SerializeField] private bool logMissingBgm = true;
        [Header("Panel UI Sounds")]
        [SerializeField] private EventReference panelTransitionSoundEvent;
        [SerializeField, Range(0f, 2f)] private float panelTransitionSoundVolume = 1f;
        [SerializeField] private bool logMissingUiSound = true;
        [Header("Debug")]
        [SerializeField] private bool enableDebugPanelSwitch = true;
        [Header("Responsive Layout")]
        [SerializeField] private bool adaptPanelsToScreen = true;
        [SerializeField] private bool configureCanvasScaler = true;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // 現在の画面状態
        private FlowState currentState;
        // 現在選択中の楽曲
        private SongSelectEntry selectedSong;
        // ライブ画面を非表示にするための CanvasGroup
        private CanvasGroup inGameCanvasGroup;
        // ノーツ速度設定パネルの制御ビュー
        private NoteSpeedSettingsPanel noteSpeedSettingsView;
        // 操作説明確認パネルの制御ビュー
        private OperationGuidePromptPanel operationGuidePromptView;
        // 操作説明パネルの制御ビュー
        private OperationGuidePanel operationGuideView;
        // 前回レイアウト更新時の画面幅
        private int currentScreenWidth;
        // 前回レイアウト更新時の画面高さ
        private int currentScreenHeight;
        // ワイプ遷移中にパネル変更を適用しているか
        private bool isApplyingPanelTransition;
        // 次にパネル遷移入力を受け付ける時刻
        private float nextPanelTransitionInputTime;
        // 現在再生中のパネル BGM インスタンス
        private EventInstance panelBgmInstance;
        // 現在再生中の BGM 種別
        private PanelBgmKind currentPanelBgmKind = PanelBgmKind.None;
        // ライブ終了イベントからリザルト表示へ入った直後か
        private bool isShowingResultFromLiveEnd;
        private float titleIdleElapsed;
        private bool titleIdleReturnRequested;

        // 選択楽曲が変わった時に通知する
        public event Action<SongSelectEntry> SelectedSongChanged;

        // パネル BGM の種別
        private enum PanelBgmKind
        {
            None,
            Menu,
            Result
        }

        // 現在選択中の楽曲
        public SongSelectEntry SelectedSong => selectedSong;
        public int SelectedMusicId => selectedSong != null ? selectedSong.MusicId : 0;
        public string SelectedTitle => selectedSong != null ? selectedSong.Title : string.Empty;
        public string SelectedArtist => selectedSong != null ? GetArtist(selectedSong) : string.Empty;
        public Texture2D SelectedJacket => selectedSong != null ? selectedSong.Jacket : null;
        public MusicDifficulty SelectedDifficulty => selectedSong != null ? selectedSong.Difficulty : MusicDifficulty.None;
        public LiveScore LastScore => liveController != null ? liveController.LastScore : default(LiveScore);
        public LiveResult LastResult => liveController != null ? liveController.LastResult : LiveResult.None;
        public bool LastWasAuto => liveController != null && liveController.LastWasAuto;
        public int LastMaxClearGauge => liveController != null ? liveController.LastMaxClearGauge : LiveConfig.DefaultMaxClearGauge;
        public int LastClearGaugeThreshold => liveController != null ? liveController.LastClearGaugeThreshold : Mathf.FloorToInt(LiveConfig.DefaultMaxClearGauge * 0.7f);
        public bool CanUseAutoPlaySwitch =>
            currentState != FlowState.GameStart &&
            currentState != FlowState.TitleIdleAnimation &&
            currentState != FlowState.Live &&
            currentState != FlowState.Result;
        public bool IsAutoPlay => liveController != null ? liveController.IsAutoPlay : LivePlayerSettings.AutoPlay;

        // 現在の画面で許可されていればオートプレイ状態を設定する
        public bool TrySetAutoPlay(bool enabled)
        {
            if (!CanUseAutoPlaySwitch)
            {
                return false;
            }

            if (liveController != null)
            {
                liveController.SetAutoPlay(enabled);
            }
            else
            {
                LivePlayerSettings.SetAutoPlay(enabled);
            }

            return true;
        }

        // 現在の画面で許可されていればオートプレイ状態を反転する
        public bool TryToggleAutoPlay(out bool enabled)
        {
            enabled = IsAutoPlay;
            if (!CanUseAutoPlaySwitch)
            {
                return false;
            }

            enabled = !enabled;
            TrySetAutoPlay(enabled);
            return true;
        }

        private void Awake()
        {
            if (liveController != null)
            {
                liveController.LiveEnded += OnLiveEnded;
            }

            EnsureNoteSpeedSettingsPanel();
            EnsureOperationGuidePanels();
            ResolvePanelTransition();
            UpdateResponsiveLayout(true);
        }

        private void Start()
        {
            UpdateResponsiveLayout(true);
            SelectSong(defaultSongIndex);
            ApplyGameStartPanel();
        }

        private void Update()
        {
            UpdateResponsiveLayout(false);
            ReleaseStoppedPanelBgmIfNeeded();

            if (currentState == FlowState.TitleIdleAnimation)
            {
                titleIdleReturnRequested |= WasAnyStartInputPressed() || WasAdditionalTitleIdleInputPressed();
                if (titleIdleReturnRequested || titleIdleAnimationView == null || titleIdleAnimationView.HasFinished)
                {
                    ShowGameStartPanel();
                }
                return;
            }
            if (Time.unscaledTime < nextPanelTransitionInputTime || (panelTransition != null && panelTransition.IsPlaying)) return;
            if (currentState != FlowState.GameStart) titleIdleElapsed = 0f;

            if (enableDebugPanelSwitch && WasDebugPanelSwitchInputPressed())
            {
                ShowNextDebugPanel();
                return;
            }

            if (currentState == FlowState.GameStart && WasAnyStartInputPressed())
            {
                ShowOperationGuidePromptPanel();
            }
            else if (currentState == FlowState.GameStart)
            {
                UpdateTitleIdleTimer();
            }
            else if (currentState == FlowState.Result && WasResultTitleInputPressed())
            {
                ShowGameStartPanel();
            }
            else if (currentState == FlowState.Result && WasResultReturnInputPressed())
            {
                GoNext();
            }
        }

        private void OnDestroy()
        {
            if (liveController != null)
            {
                liveController.LiveEnded -= OnLiveEnded;
            }

            if (titleIdleAnimationView != null) titleIdleAnimationView.Stop();
            StopPanelBgm(false);
        }

        private void OnDisable()
        {
            titleIdleElapsed = 0f;
            if (currentState == FlowState.TitleIdleAnimation)
            {
                if (titleIdleAnimationView != null) titleIdleAnimationView.Stop();
                titleIdleReturnRequested = true;
            }
        }

        // データベースの index から選択楽曲を設定する
        public void SelectSong(int index)
        {
            SetSelectedSong(songDataBase != null ? songDataBase.GetSong(index) : null);
        }

        // 現在状態に応じて次の画面へ進む
        public void GoNext()
        {
            switch (currentState)
            {
                case FlowState.GameStart:
                    ShowOperationGuidePromptPanel();
                    break;
                case FlowState.SelectSong:
                    if (songSelectMenu == null || songSelectMenu.TryStartCurrentSelection())
                    {
                        ShowNoteSpeedSettingsPanel();
                    }
                    break;
                case FlowState.NoteSpeedSettings:
                    StartSelectedSong();
                    break;
                case FlowState.OperationGuidePrompt:
                    ShowOperationGuidePanel();
                    break;
                case FlowState.OperationGuide:
                    ShowSelectSongPanel();
                    break;
                case FlowState.Live:
                    break;
                case FlowState.Result:
                    ShowSelectSongPanel();
                    break;
            }
        }

        // デバッグ入力用に次のパネルへ強制遷移する
        private void ShowNextDebugPanel()
        {
            switch (currentState)
            {
                case FlowState.GameStart:
                    ShowOperationGuidePromptPanel();
                    break;
                case FlowState.OperationGuidePrompt:
                    ShowOperationGuidePanel();
                    break;
                case FlowState.OperationGuide:
                    ShowSelectSongPanel();
                    break;
                case FlowState.SelectSong:
                    ShowNoteSpeedSettingsPanel();
                    break;
                case FlowState.NoteSpeedSettings:
                    ShowLive();
                    break;
                case FlowState.Live:
                    ShowResultPanel();
                    break;
                case FlowState.Result:
                    ShowGameStartPanel();
                    break;
            }
        }

        // 選択中楽曲でライブ開始画面へ進む
        public void StartSelectedSong()
        {
            if (liveController == null)
            {
                Debug.LogWarning("LiveSceneController is not assigned.", this);
                return;
            }

            if (selectedSong == null)
            {
                SelectSong(defaultSongIndex);
            }

            ShowLive();
        }

        // 選択楽曲を更新し、購読者へ通知する
        public void SetSelectedSong(SongSelectEntry song)
        {
            if (selectedSong == song)
            {
                return;
            }

            selectedSong = song;
            SelectedSongChanged?.Invoke(selectedSong);
        }

        // Artist が空なら Composer を表示名として使う
        private static string GetArtist(SongSelectEntry song)
        {
            return string.IsNullOrEmpty(song.Artist) ? song.Composer : song.Artist;
        }

        // タイトル相当の開始パネルを表示する
        public void ShowGameStartPanel()
        {
            ShowPanelWithTransition(ApplyGameStartPanel);
        }

        // 開始パネルの表示状態を直接適用する
        private void ApplyGameStartPanel()
        {
            ResetLiveIfLeavingInGame();
            titleIdleElapsed = 0f;
            currentState = FlowState.GameStart;
            SetPanel(gameStartPanel, true);
            SetPanel(selectSongPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, false);
            PlayMenuBgm();
        }

        // 選曲パネルを表示する
        public void ShowSelectSongPanel()
        {
            ShowPanelWithTransition(ApplySelectSongPanel);
        }

        // 選曲パネルの表示状態を直接適用する
        private void ApplySelectSongPanel()
        {
            ResetLiveIfLeavingInGame();
            currentState = FlowState.SelectSong;
            SetPanel(selectSongPanel, true);
            InitializeSelectedSongInfoWindows();
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, false);
            PlayMenuBgm();
        }

        // ノーツ速度設定パネルを表示する
        public void ShowNoteSpeedSettingsPanel()
        {
            ShowPanelWithTransition(ApplyNoteSpeedSettingsPanel);
        }

        // ノーツ速度設定パネルの表示状態を直接適用する
        private void ApplyNoteSpeedSettingsPanel()
        {
            ResetLiveIfLeavingInGame();
            EnsureNoteSpeedSettingsPanel();
            currentState = FlowState.NoteSpeedSettings;
            SetPanel(selectSongPanel, false);
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, true);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, false);
            InitializeSelectedSongInfoViews(noteSpeedSettingsPanel);
            InitializeNoteSpeedSettingsView();
            PlayMenuBgm();
        }

        // 操作説明を見るか確認するパネルを表示する
        public void ShowOperationGuidePromptPanel()
        {
            ShowPanelWithTransition(ApplyOperationGuidePromptPanel);
        }

        // 操作説明確認パネルの表示状態を直接適用する
        private void ApplyOperationGuidePromptPanel()
        {
            ResetLiveIfLeavingInGame();
            EnsureOperationGuidePanels();
            currentState = FlowState.OperationGuidePrompt;
            SetPanel(selectSongPanel, false);
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, true);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, false);
            InitializeOperationGuidePromptView();
            PlayMenuBgm();
        }

        // 操作説明パネルを表示する
        public void ShowOperationGuidePanel()
        {
            ShowPanelWithTransition(ApplyOperationGuidePanel);
        }

        // 操作説明パネルの表示状態を直接適用する
        private void ApplyOperationGuidePanel()
        {
            ResetLiveIfLeavingInGame();
            EnsureOperationGuidePanels();
            currentState = FlowState.OperationGuide;
            SetPanel(selectSongPanel, false);
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, true);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, false);
            InitializeOperationGuideView();
            PlayMenuBgm();
        }

        // 操作説明をスキップして選曲へ進む
        public void SkipOperationGuideAndShowSelectSong()
        {
            ShowSelectSongPanel();
        }

        // 操作説明を終えて選曲へ進む
        public void FinishOperationGuide()
        {
            ShowSelectSongPanel();
        }

        // リザルトパネルを表示する
        public void ShowResultPanel()
        {
            ShowPanelWithTransition(ApplyResultPanel);
        }

        // リザルトパネルの表示状態を直接適用する
        private void ApplyResultPanel()
        {
            ResetLiveIfLeavingInGame(!isShowingResultFromLiveEnd);
            isShowingResultFromLiveEnd = false;
            currentState = FlowState.Result;
            SetPanel(selectSongPanel, false);
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(false);
            SetPanel(resultPanel, true);
            InitializeSelectedSongInfoViews(resultPanel);
            InitializeResultInfoViews(resultPanel);
            PlayResultBgmFromStart();
        }

        // ライブ画面を表示して楽曲を開始する
        public void ShowLive()
        {
            ShowPanelWithTransition(ApplyLive);
        }

        // ライブ画面の表示状態を直接適用する
        private void ApplyLive()
        {
            if (currentState == FlowState.Live)
            {
                liveController?.ResetLive();
            }

            currentState = FlowState.Live;
            SetPanel(selectSongPanel, false);
            SetPanel(gameStartPanel, false);
            SetPanel(noteSpeedSettingsPanel, false);
            SetPanel(operationGuidePromptPanel, false);
            SetPanel(operationGuidePanel, false);
            SetInGamePanelVisible(true);
            MoveMouseCursorToScreenCenter();
            SetPanel(resultPanel, false);
            InitializeSelectedSongInfoViews(inGamePanel);
            StopPanelBgm(true);
            StartLiveForSelectedSong();
        }

        // 必要に応じてワイプ演出を挟んでパネル変更を実行する
        private void ShowPanelWithTransition(Action applyPanelChange)
        {
            if (applyPanelChange == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                ApplyPanelChange(applyPanelChange);
                return;
            }

            if (isApplyingPanelTransition || Time.unscaledTime < nextPanelTransitionInputTime)
            {
                return;
            }

            if (!animatePanelTransitions)
            {
                LockPanelTransitionInput();
                PlayPanelTransitionSound();
                ApplyPanelChange(applyPanelChange);
                return;
            }

            ResolvePanelTransition();
            if (panelTransition == null )
            {
                LockPanelTransitionInput();
                PlayPanelTransitionSound();
                ApplyPanelChange(applyPanelChange);
                return;
            }

            if (panelTransition.IsPlaying)
            {
                return;
            }

            LockPanelTransitionInput();
            PlayPanelTransitionSound();
            panelTransition.Play(() =>
            {
                isApplyingPanelTransition = true;
                try
                {
                    ApplyPanelChange(applyPanelChange);
                }
                finally
                {
                    isApplyingPanelTransition = false;
                }
            });
        }

        private void ApplyPanelChange(Action applyPanelChange)
        {
            if (currentState == FlowState.TitleIdleAnimation)
            {
                if (titleIdleAnimationView != null)
                {
                    titleIdleAnimationView.Stop();
                    SetPanel(titleIdleAnimationView.gameObject, false);
                }
                titleIdleReturnRequested = false;
            }
            applyPanelChange();
        }

        private void UpdateTitleIdleTimer()
        {
            if (!enableTitleIdleAnimation || gameStartPanel == null || !gameStartPanel.activeInHierarchy)
            {
                titleIdleElapsed = 0f;
                return;
            }
            if (titleIdleAnimationView == null && titleIdleVideoClip == null) return;
            if (titleIdleAnimationView != null && !titleIdleAnimationView.CanPlay(titleIdleVideoClip)) return;
            titleIdleElapsed += Time.unscaledDeltaTime;
            if (titleIdleElapsed >= Mathf.Max(0.1f, titleIdleWaitSeconds)) ShowPanelWithTransition(ApplyTitleIdleAnimation);
        }

        private void ApplyTitleIdleAnimation()
        {
            if (currentState != FlowState.GameStart) return;
            if (titleIdleAnimationView == null)
            {
                titleIdleAnimationView = TitleIdleAnimationView.CreateDefault(gameStartPanel.transform.parent);
            }
            if (titleIdleAnimationView.gameObject == gameStartPanel)
            {
                Debug.LogWarning("Title idle animation must use its own panel, separate from the title.", this);
                enableTitleIdleAnimation = false;
                return;
            }
            if (titleIdleAnimationView.transform.IsChildOf(gameStartPanel.transform))
            {
                titleIdleAnimationView.transform.SetParent(gameStartPanel.transform.parent, false);
            }
            FitPanelToCanvas(titleIdleAnimationView.gameObject);
            titleIdleAnimationView.transform.SetSiblingIndex(gameStartPanel.transform.GetSiblingIndex());
            currentState = FlowState.TitleIdleAnimation;
            titleIdleReturnRequested = false;
            SetPanel(gameStartPanel, false);
            SetPanel(titleIdleAnimationView.gameObject, true);
            if (muteMenuBgmDuringTitleIdle) StopPanelBgm(true);
            titleIdleAnimationView.ConfigureAudio(titleIdleAudioEvent, titleIdleAudioVolume);
            titleIdleAnimationView.Play(titleIdleVideoClip, titleIdlePlaybackSeconds);
        }

        private static bool WasAdditionalTitleIdleInputPressed()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Gamepad.current == null) return false;
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame) return true;
            }
            return false;
        }

        // 連続入力でパネル遷移が重ならないよう入力を短時間ロックする
        private void LockPanelTransitionInput()
        {
            nextPanelTransitionInputTime = Time.unscaledTime + Mathf.Max(0f, panelTransitionInputLockSeconds);
        }

        // ライブ終了通知を受けてリザルトへ移動する
        private void OnLiveEnded(LiveResult liveResult)
        {
            if (selectedSong != null && (liveResult == LiveResult.Clear || liveResult == LiveResult.Failure))
            {
                SongPlayRecordStore.Default.RecordResult(selectedSong.MusicId, selectedSong.Difficulty,
                    LastScore, LastMaxClearGauge, LastClearGaugeThreshold, LastWasAuto);
            }

            isShowingResultFromLiveEnd = true;
            ShowResultPanel();
        }

        // ライブ画面から離れる時にライブ状態をリセットまたは停止する
        private void ResetLiveIfLeavingInGame(bool preserveResult = false)
        {
            if (currentState == FlowState.Live)
            {
                if (preserveResult)
                {
                    liveController?.StopLiveForPanelExit();
                }
                else
                {
                    liveController?.ResetLive();
                }
            }
        }

        // 選択中楽曲をライブ Controller へ適用して開始する
        private void StartLiveForSelectedSong()
        {
            if (liveController == null)
            {
                Debug.LogWarning("LiveSceneController is not assigned.", this);
                return;
            }

            if (selectedSong == null)
            {
                SelectSong(defaultSongIndex);
            }

            if (selectedSong != null)
            {
                liveController.ApplySong(SongCatalog.CreateItem(selectedSong));
            }

            liveController.ResetLive();
            liveController.SetupForManualStart();
            liveController.StartLive();
        }

        // メニュー系パネルの BGM を再生する
        private void PlayMenuBgm()
        {
            if (currentPanelBgmKind == PanelBgmKind.Menu &&
                panelBgmInstance.isValid())
            {
                return;
            }

            PlayPanelBgm(menuBgmEvent, menuBgmVolume, PanelBgmKind.Menu, false);
        }

        // リザルト BGM を先頭から再生する
        private void PlayResultBgmFromStart()
        {
            PlayPanelBgm(resultBgmEvent, resultBgmVolume, PanelBgmKind.Result, true);
        }

        // 指定された FMOD イベントをパネル BGM として再生する
        private void PlayPanelBgm(EventReference eventReference, float volume, PanelBgmKind bgmKind, bool restartFromBeginning)
        {
            if (eventReference.IsNull)
            {
                StopPanelBgm(false);
                LogMissingBgm(bgmKind);
                return;
            }

            if (!restartFromBeginning &&
                currentPanelBgmKind == bgmKind &&
                panelBgmInstance.isValid())
            {
                return;
            }

            StopPanelBgm(true);

            try
            {
                panelBgmInstance = RuntimeManager.CreateInstance(eventReference);
            }
            catch (Exception exception)
            {
                currentPanelBgmKind = PanelBgmKind.None;
                if (logMissingBgm)
                {
                    Debug.LogWarning("FMOD panel BGM create failed: " + bgmKind + " " + exception.Message, this);
                }
                return;
            }

            if (!panelBgmInstance.isValid())
            {
                currentPanelBgmKind = PanelBgmKind.None;
                return;
            }

            panelBgmInstance.setVolume(Mathf.Clamp(volume, 0f, 2f));
            panelBgmInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            panelBgmInstance.setTimelinePosition(0);
            panelBgmInstance.start();
            currentPanelBgmKind = bgmKind;
        }

        // ループ代替としてメニュー BGM を先頭から再生し直す
        private void RestartMenuBgmFromBeginning()
        {
            if (menuBgmEvent.IsNull)
            {
                StopPanelBgm(false);
                LogMissingBgm(PanelBgmKind.Menu);
                return;
            }

            ReleasePanelBgmInstance();
            PlayPanelBgm(menuBgmEvent, menuBgmVolume, PanelBgmKind.Menu, true);
        }

        // 現在のパネル BGM を停止して解放する
        private void StopPanelBgm(bool allowFadeOut)
        {
            if (!panelBgmInstance.isValid())
            {
                currentPanelBgmKind = PanelBgmKind.None;
                return;
            }

            panelBgmInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            panelBgmInstance.release();
            panelBgmInstance.clearHandle();
            currentPanelBgmKind = PanelBgmKind.None;
        }

        // 停止済み BGM を検出し、必要ならメニュー BGM を再始動する
        private void ReleaseStoppedPanelBgmIfNeeded()
        {
            if (currentPanelBgmKind == PanelBgmKind.None ||
                !panelBgmInstance.isValid() ||
                panelBgmInstance.getPlaybackState(out PLAYBACK_STATE playbackState) != FMOD.RESULT.OK)
            {
                return;
            }

            if (playbackState != PLAYBACK_STATE.STOPPED)
            {
                return;
            }

            if (currentPanelBgmKind == PanelBgmKind.Menu)
            {
                RestartMenuBgmFromBeginning();
                return;
            }

            ReleasePanelBgmInstance();
        }

        // BGM 未設定時の警告を出す
        private void LogMissingBgm(PanelBgmKind bgmKind)
        {
            if (!logMissingBgm)
            {
                return;
            }

            Debug.LogWarning("FMOD panel BGM is not assigned: " + bgmKind, this);
        }

        // 現在の BGM インスタンスを解放する
        private void ReleasePanelBgmInstance()
        {
            if (panelBgmInstance.isValid())
            {
                panelBgmInstance.release();
                panelBgmInstance.clearHandle();
            }

            currentPanelBgmKind = PanelBgmKind.None;
        }

        // パネル遷移時の UI 音を再生する
        private void PlayPanelTransitionSound()
        {
            PlayUiSound(panelTransitionSoundEvent, panelTransitionSoundVolume, "panel transition");
        }

        // FMOD のワンショット UI 音を再生する
        private void PlayUiSound(EventReference eventReference, float volume, string soundName)
        {
            if (eventReference.IsNull)
            {
                if (logMissingUiSound)
                {
                    Debug.LogWarning("FMOD UI sound is not assigned: " + soundName, this);
                }
                return;
            }

            EventInstance instance;
            try
            {
                instance = RuntimeManager.CreateInstance(eventReference);
            }
            catch (Exception exception)
            {
                if (logMissingUiSound)
                {
                    Debug.LogWarning("FMOD UI sound create failed: " + soundName + " " + exception.Message, this);
                }
                return;
            }

            if (!instance.isValid())
            {
                return;
            }

            instance.setVolume(Mathf.Clamp(volume, 0f, 2f));
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            instance.start();
            instance.release();
        }

        // タイトル開始に使える任意の主要入力が押されたか
        private static bool WasAnyStartInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null &&
                (mouse.leftButton.wasPressedThisFrame ||
                 mouse.rightButton.wasPressedThisFrame ||
                 mouse.middleButton.wasPressedThisFrame))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null &&
                   (gamepad.buttonSouth.wasPressedThisFrame ||
                    gamepad.buttonNorth.wasPressedThisFrame ||
                    gamepad.buttonEast.wasPressedThisFrame ||
                    gamepad.buttonWest.wasPressedThisFrame ||
                    gamepad.startButton.wasPressedThisFrame ||
                    gamepad.selectButton.wasPressedThisFrame);
        }

        // リザルトから選曲へ戻る入力が押されたか
        private static bool WasResultReturnInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
        }

        // リザルトからタイトルへ戻る入力が押されたか
        private static bool WasResultTitleInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.aKey.wasPressedThisFrame;
        }

        // デバッグ用パネル送り入力が押されたか
        private static bool WasDebugPanelSwitchInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.pKey.wasPressedThisFrame;
        }

        // Panel GameObject が存在する場合だけ表示状態を切り替える
        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        // ライブ画面を非アクティブ化せず CanvasGroup で表示可否を切り替える
        private void SetInGamePanelVisible(bool visible)
        {
            if (inGamePanel == null)
            {
                return;
            }

            if (!inGamePanel.activeSelf)
            {
                inGamePanel.SetActive(true);
            }

            CanvasGroup canvasGroup = GetInGameCanvasGroup();
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        // ライブ開始時にマウスカーソルを画面中央へ移動する
        private static void MoveMouseCursorToScreenCenter()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Mouse.current.WarpCursorPosition(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        // 選曲画面の記録ウィンドウを初期化し、未配置なら既定 Prefab を使用する
        private void InitializeSelectedSongInfoWindows()
        {
            if (selectSongPanel == null) return;
            SelectedSongInfoWindow[] windows = selectSongPanel.GetComponentsInChildren<SelectedSongInfoWindow>(true);
            if (windows.Length == 0 && createDefaultSelectedSongInfoWindow)
            {
                SelectedSongInfoWindow prefab = selectedSongInfoWindowPrefab;
                if (prefab == null)
                {
                    GameObject resource = Resources.Load<GameObject>("Prefab/FFrameWork/SelectedSongInfoWIndow");
                    if (resource != null) prefab = resource.GetComponent<SelectedSongInfoWindow>();
                }
                if (prefab != null)
                {
                    SelectedSongInfoWindow window = Instantiate(prefab, selectSongPanel.transform, false);
                    window.Initialize(this);
                }
                return;
            }
            for (int i = 0; i < windows.Length; i++) windows[i].Initialize(this);
        }

        // パネル内の選択楽曲表示を現在の Controller で初期化する
        private void InitializeSelectedSongInfoViews(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            SelectedSongInfoView[] views = panel.GetComponentsInChildren<SelectedSongInfoView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize(this);
            }
        }

        // パネル内のリザルト表示を現在の Controller で初期化する
        private void InitializeResultInfoViews(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            ResultInfoView[] views = panel.GetComponentsInChildren<ResultInfoView>(true);
            if (views.Length == 0)
            {
                ResultInfoView view = panel.GetComponent<ResultInfoView>();
                if (view == null)
                {
                    view = panel.AddComponent<ResultInfoView>();
                }
                view.Initialize(this);
                return;
            }

            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize(this);
            }
        }

        // ライブ画面の CanvasGroup を取得または追加する
        private CanvasGroup GetInGameCanvasGroup()
        {
            if (inGameCanvasGroup == null && inGamePanel != null)
            {
                inGameCanvasGroup = inGamePanel.GetComponent<CanvasGroup>();
                if (inGameCanvasGroup == null)
                {
                    inGameCanvasGroup = inGamePanel.AddComponent<CanvasGroup>();
                }
            }

            return inGameCanvasGroup;
        }

        // 画面サイズ変更時に各パネルの CanvasScaler と RectTransform を更新する
        private void UpdateResponsiveLayout(bool force)
        {
            if (!adaptPanelsToScreen)
            {
                return;
            }

            if (!force &&
                currentScreenWidth == Screen.width &&
                currentScreenHeight == Screen.height)
            {
                return;
            }

            currentScreenWidth = Screen.width;
            currentScreenHeight = Screen.height;

            ConfigureCanvasScaler(gameStartPanel);
            ConfigureCanvasScaler(selectSongPanel);
            ConfigureCanvasScaler(noteSpeedSettingsPanel);
            ConfigureCanvasScaler(operationGuidePromptPanel);
            ConfigureCanvasScaler(operationGuidePanel);
            ConfigureCanvasScaler(inGamePanel);
            ConfigureCanvasScaler(resultPanel);

            FitPanelToCanvas(gameStartPanel);
            FitPanelToCanvas(selectSongPanel);
            FitPanelToCanvas(noteSpeedSettingsPanel);
            FitPanelToCanvas(operationGuidePromptPanel);
            FitPanelToCanvas(operationGuidePanel);
            FitPanelToCanvas(inGamePanel);
            FitPanelToCanvas(resultPanel);
        }

        // ノーツ速度設定パネルを取得または既定 UI として生成する
        private void EnsureNoteSpeedSettingsPanel()
        {
            if (noteSpeedSettingsPanel == null && createDefaultNoteSpeedSettingsPanel)
            {
                Transform parent = ResolvePanelParent();
                noteSpeedSettingsPanel = NoteSpeedSettingsPanel.CreateDefault(parent, this);
            }

            if (noteSpeedSettingsPanel != null && noteSpeedSettingsView == null)
            {
                noteSpeedSettingsView = noteSpeedSettingsPanel.GetComponent<NoteSpeedSettingsPanel>();
                if (noteSpeedSettingsView == null)
                {
                    noteSpeedSettingsView = noteSpeedSettingsPanel.AddComponent<NoteSpeedSettingsPanel>();
                }
            }
        }

        // 操作説明関連パネルを取得または既定 UI として生成する
        private void EnsureOperationGuidePanels()
        {
            Transform parent = ResolvePanelParent();
            if (operationGuidePromptPanel == null && createDefaultOperationGuidePanels)
            {
                operationGuidePromptPanel = OperationGuidePromptPanel.CreateDefault(parent, this);
            }

            if (operationGuidePanel == null && createDefaultOperationGuidePanels)
            {
                operationGuidePanel = OperationGuidePanel.CreateDefault(parent, this);
            }

            if (operationGuidePromptPanel != null && operationGuidePromptView == null)
            {
                operationGuidePromptView = operationGuidePromptPanel.GetComponent<OperationGuidePromptPanel>();
                if (operationGuidePromptView == null)
                {
                    operationGuidePromptView = operationGuidePromptPanel.AddComponent<OperationGuidePromptPanel>();
                }
            }

            if (operationGuidePanel != null && operationGuideView == null)
            {
                operationGuideView = operationGuidePanel.GetComponent<OperationGuidePanel>();
                if (operationGuideView == null)
                {
                    operationGuideView = operationGuidePanel.AddComponent<OperationGuidePanel>();
                }
            }
        }

        // 子階層または親階層からワイプ遷移 Controller を探す
        private void ResolvePanelTransition()
        {
            if (panelTransition != null)
            {
                return;
            }

            panelTransition = GetComponentInChildren<PanelTransitionWipeController>(true);
            if (panelTransition == null && transform.parent != null)
            {
                panelTransition = transform.parent.GetComponentInChildren<PanelTransitionWipeController>(true);
            }
        }

        // 自動生成パネルを置く親 Transform を決める
        private Transform ResolvePanelParent()
        {
            if (selectSongPanel != null && selectSongPanel.transform.parent != null)
            {
                return selectSongPanel.transform.parent;
            }
            if (gameStartPanel != null && gameStartPanel.transform.parent != null)
            {
                return gameStartPanel.transform.parent;
            }
            if (resultPanel != null && resultPanel.transform.parent != null)
            {
                return resultPanel.transform.parent;
            }

            return transform;
        }

        // ノーツ速度設定ビューを初期化する
        private void InitializeNoteSpeedSettingsView()
        {
            EnsureNoteSpeedSettingsPanel();
            noteSpeedSettingsView?.Initialize(this);
        }

        // 操作説明確認ビューを初期化する
        private void InitializeOperationGuidePromptView()
        {
            EnsureOperationGuidePanels();
            operationGuidePromptView?.Initialize(this);
        }

        // 操作説明ビューを初期化する
        private void InitializeOperationGuideView()
        {
            EnsureOperationGuidePanels();
            operationGuideView?.Initialize(this);
        }

        // パネルを含む CanvasScaler をレスポンシブ設定へ合わせる
        private void ConfigureCanvasScaler(GameObject panel)
        {
            if (!configureCanvasScaler || panel == null)
            {
                return;
            }

            CanvasScaler canvasScaler = GetComponentInParents<CanvasScaler>(panel.transform);
            if (canvasScaler == null)
            {
                return;
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = screenMatchMode;
        }

        // Panel RectTransform を親 Canvas 全体へ合わせる
        private static void FitPanelToCanvas(GameObject panel)
        {
            if (panel == null || !(panel.transform is RectTransform rectTransform))
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
        }

        // 親方向へたどって指定 Component を探す
        private static T GetComponentInParents<T>(Transform transform) where T : Component
        {
            while (transform != null)
            {
                if (transform.TryGetComponent(out T component))
                {
                    return component;
                }

                transform = transform.parent;
            }

            return null;
        }
    }
}
