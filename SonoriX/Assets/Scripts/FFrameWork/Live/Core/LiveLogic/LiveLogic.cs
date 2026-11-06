using System;
using System.Collections.Generic;
using System.Diagnostics; 
using UnityEngine;
using Debug = UnityEngine.Debug;

//1. LiveLogic は譜面時間を1フレーム進める
//2. 表示対象ノーツを更新し、判定可能なノーツを集める
//3. 入力をレーン単位の LiveInput に正規化する
//4. 入力とノーツの候補関係を作る
//5. 判定結果を Note / Score / View / Effect に反映する

namespace FFrameWork.Live
{
    // 譜面時間の更新、入力正規化、ノート判定、View/Effect 反映をまとめるライブ進行ロジック
    public class LiveLogic
    {
        // 同時に扱う入力一時データの最大数
        private const int InputTmpCount = 128;

        private LiveBootDataBase bootData;
        private ArrowController arrowController;
        private InputManager inputManager;
        private bool ownsInputManager;

        private LiveResult result;

        private readonly List<NoteBase> judgmentNoteList = new List<NoteBase>(64);
        private readonly InputTmp[] inputTmpArray = new InputTmp[InputTmpCount];
        private readonly List<InputTmp> activeInputList = new List<InputTmp>(InputTmpCount);

        private readonly LiveBundleBuildData liveBundleBuildData;
        private readonly LiveInputCollector inputCollector;
        private readonly JudgmentSystem judgmentSystem;
        private readonly ChartPlaybackSystem chartPlaybackSystem = new ChartPlaybackSystem();
        private readonly PresentationRoot presentationRoot = new PresentationRoot();
        private readonly HoldNoteSystem holdNoteSystem = new HoldNoteSystem();
        private readonly ScoreSystem scoreSystem;

        // ライブロジックを生成する
        // data: ライブ中の速度、判定、スコアなどに使用する構築データ
        public LiveLogic(LiveBundleBuildData data)
        {
            liveBundleBuildData = data;
            inputCollector = new LiveInputCollector();
            judgmentSystem = new JudgmentSystem(liveBundleBuildData);
            scoreSystem = new ScoreSystem(liveBundleBuildData, presentationRoot);
            holdNoteSystem.SetPresentationRoot(presentationRoot);
            for (int i = 0; i < inputTmpArray.Length; i++)
            {
                inputTmpArray[i] = new InputTmp();
            }
        }

        public LiveScore Score => scoreSystem.Score;
        public bool IsAllPerfectCombo => scoreSystem.IsAllPerfectCombo;
        public bool IsPerfectCombo => scoreSystem.IsPerfectCombo;
        public bool IsClearGauge => scoreSystem.IsClearGauge;
        public bool IsNotesAllFinished => chartPlaybackSystem.IsNotesAllFinished;
        public MusicScoreInfo CurrentFrameInfo => chartPlaybackSystem.CurrentFrameInfo;

        public LiveResult Result
        {
            get => result;
            private set
            {
                if (result == value) return;
                result = value;
                if (value == LiveResult.Failure) OnFailure?.Invoke();
                if (value >= LiveResult.Clear) OnFinished?.Invoke();
            }
        }

        public event Action OnFailure;
        public event Action OnFinished;

        private bool IsAutoMode => bootData != null && bootData.IsAuto;
        private bool HasArrowLogic => arrowController != null && arrowController.HasLogic;

        // ライブロジックを譜面、View、起動データで初期化する
        // bootData: ライブ起動時の設定データ
        // liveViews: 表示更新先の View 一覧
        // musicScore: 実行する譜面データ
        public void Setup(LiveBootDataBase bootData, LiveViewBase[] liveViews, MusicScore musicScore)
        {
            presentationRoot.SetLiveViews(liveViews);
            this.bootData = bootData;
            LiveConfig.CacheSpeedTime = 0f;

            float noteSpeed = bootData?.LiveSettingData != null
                ? bootData.LiveSettingData.NoteSpeed
                : LiveConfig.DefaultNoteSpeed;
            noteSpeed = LivePlayerSettings.NormalizeNoteSpeed(noteSpeed);
            float noteDisplayTimeOffset = LiveConfig.GetNoteDisplayOffsetTime(noteSpeed);
            chartPlaybackSystem.Setup(musicScore, noteDisplayTimeOffset, ConfigurePlaybackNote);
            presentationRoot.CreateNotePool(chartPlaybackSystem.MusicScore, noteDisplayTimeOffset);
            holdNoteSystem.Clear();
            scoreSystem.Setup(bootData, chartPlaybackSystem.NoteArray);
            SetupInputManager();
            Result = LiveResult.None;
            RefreshInput();
        }

        // 入力とエフェクトを破棄し、ライブロジックを終了する
        public void Dispose()
        {
            scoreSystem.Dispose();
            holdNoteSystem.EndAllTrackedAudioEffects();
            presentationRoot.StopAllAudio();
            DisposeOwnedInputManager();
            inputManager = null;
        }

        // スコアロジックを差し替え、現在の譜面情報で初期化する
        // scoreLogic: 使用するスコアロジック
        public void SetScoreLogic(ScoreLogic scoreLogic)
        {
            scoreSystem.SetScoreLogic(scoreLogic, bootData, chartPlaybackSystem.NoteArray);
        }

        // 矢印入力の制御クラスを設定する
        // arrowController: 使用する矢印制御
        public void SetArrowController(ArrowController arrowController) => this.arrowController = arrowController;

        // ライブ全体で共有する入力管理を設定する
        public void SetInputManager(InputManager inputManager)
        {
            if (this.inputManager == inputManager)
            {
                return;
            }

            DisposeOwnedInputManager();
            this.inputManager = inputManager;
            ownsInputManager = false;
            this.inputManager?.EnableInLiveInput();
        }

        // 音声エフェクト制御クラスを設定する
        // controller: 使用する音声エフェクト制御
        public void SetAudioEffectController(AudioEffectController controller)
        {
            presentationRoot.SetAudioEffectController(controller);
        }

        // コンティニュー時に結果と入力状態、スコア状態をリセットする
        // time: コンティニューした楽曲時間
        public void Continue(float time)
        {
            Result = LiveResult.None;
            RefreshInput();
            scoreSystem.Continue(time);
        }

        // 毎フレーム、譜面時間、ノート、ホールド入力、エフェクト、終了判定を更新する
        // scoreInfoTime: 現在の楽曲時間
        public void OnUpdate(float scoreInfoTime)
        {
            if (Result > LiveResult.Failure || !chartPlaybackSystem.HasMusicScore) return;

            chartPlaybackSystem.UpdateTime(scoreInfoTime);

            judgmentNoteList.Clear();

            RefreshActiveHoldNotes();
            chartPlaybackSystem.UpdateNotes(judgmentNoteList, holdNoteSystem.Remove, holdNoteSystem.TrackIfHoldNote);
            UpdateActiveHoldInputs();
            holdNoteSystem.ReconcileTrackedAudioEffects();

            if (IsNotesAllFinished && Result == LiveResult.None)
            {
                Result = IsClearGauge ? LiveResult.Clear : LiveResult.Failure;
            }
        }

        // 表示・判定対象のノートを進行させ、判定候補一覧へ追加する
        // すでにホールド中のノートが入力継続条件を満たしているか再確認する
        private void RefreshActiveHoldNotes()
        {
            bool hasArrowLogic = HasArrowLogic;
            ArrowJudgmentFrame frame = hasArrowLogic ? arrowController.GetArrowJudgmentFrame() : default;
            float sizeDelta = inputManager?.ArrowSizeDelta ?? 0f;
            bool isFreeLanePressed = inputManager?.IsFreeLanePressed ?? false;
            holdNoteSystem.RefreshActiveHoldNotes(
                IsAutoMode,
                hasArrowLogic,
                frame,
                CurrentFrameInfo.time,
                sizeDelta,
                isFreeLanePressed,
                inputManager != null);
        }

        // アクティブなホールドノートへ矢印やフリーレーンの継続入力を反映する
        private void UpdateActiveHoldInputs()
        {
            bool hasArrowLogic = HasArrowLogic;
            ArrowJudgmentFrame frame = hasArrowLogic ? arrowController.GetArrowJudgmentFrame() : default;
            float sizeDelta = inputManager?.ArrowSizeDelta ?? 0f;
            bool isFreeLanePressed = inputManager?.IsFreeLanePressed ?? false;
            holdNoteSystem.UpdateActiveHoldInputs(
                IsAutoMode,
                hasArrowLogic,
                frame,
                CurrentFrameInfo.time,
                sizeDelta,
                isFreeLanePressed,
                inputManager != null);
        }

        // 現在フレームの入力一時リストをクリアする
        public void RefreshInput()
        {
            activeInputList.Clear();
        }

        // オートプレイ用に現在時刻へ到達したノートを自動判定する
        public void OnAutoInput()
        {
            activeInputList.Clear();
            inputCollector.CollectFixedLaneInputs(inputManager, CurrentFrameInfo.time, AddActiveInput);
            chartPlaybackSystem.ExecuteAutoJudgment(holdNoteSystem.UpdateFreeLaneLongAutoEffect);
        }

        // 手動入力を収集し、ノート判定まで実行する
        public void OnInput()
        {
            activeInputList.Clear();
            inputCollector.CollectManualInputs(inputManager, arrowController, CurrentFrameInfo.time, AddActiveInput);
            judgmentSystem.Execute(judgmentNoteList, activeInputList, CurrentFrameInfo, presentationRoot);
        }

        // ライブ入力用の InputManager を生成して有効化する
        private void SetupInputManager()
        {
            if (inputManager != null)
            {
                inputManager.EnableInLiveInput();
                return;
            }

            inputManager = new InputManager();
            ownsInputManager = true;
            inputManager.EnableInLiveInput();
        }

        private void DisposeOwnedInputManager()
        {
            if (!ownsInputManager)
            {
                return;
            }

            inputManager?.Dispose();
            ownsInputManager = false;
        }

        // 入力一時データを判定対象リストへ追加する
        // lane: 入力の中心レーン
        // input: 登録する入力情報
        private void AddActiveInput(int lane, LiveInput input)
        {
            if (activeInputList.Count >= InputTmpCount) return;

            InputTmp inputTmp = inputTmpArray[activeInputList.Count];
            inputTmp.CandidateNotes.Clear();
            inputTmp.Lane = lane;
            inputTmp.liveInput = input;
            activeInputList.Add(inputTmp);
        }

        private void ConfigurePlaybackNote(NoteBase note, bool isRoot)
        {
            if (note == null)
            {
                return;
            }

            if (isRoot)
            {
                note.OnSpawn = OnSpawnNote;
                note.OnJudgment = OnJudgmentNote;
                note.OnUnspawn = OnUnspawnNote;
                scoreSystem.ConfigurePlaybackNote(note);
            }

            SetupVolumeFlickCallback(note);
        }

        private void SetupVolumeFlickCallback(NoteBase note)
        {
            if (note is VolumeFLickNote volumeFlickNote)
            {
                volumeFlickNote.OnArrowSizeStageTarget = SetArrowSizeStage;
            }
            if (note is VolumeLongNote volumeLongNote)
            {
                volumeLongNote.OnBeginArrowSizeStageTarget = BeginArrowLongSizeChange;
                volumeLongNote.OnUpdateArrowSizeProgress = UpdateArrowLongSizeChange;
                volumeLongNote.OnFinishArrowSizeChange = FinishArrowLongSizeChange;
            }
        }

        // 矢印サイズを指定段階へ変更する
        // stageIndex: 変更先のサイズ段階
        private void SetArrowSizeStage(int stageIndex)
        {
            if (!HasArrowLogic) return;
            arrowController.SetSizeStage(stageIndex);
        }

        // ボリュームロングのサイズ変更演出を開始する
        // targetStageIndex: 目標となるサイズ段階
        private void BeginArrowLongSizeChange(int targetStageIndex)
        {
            if (!HasArrowLogic) return;
            arrowController.BeginLongSizeChange(targetStageIndex);
        }

        // ボリュームロングのサイズ変更進行度を更新する
        // progress: 0 から 1 の進行度
        private void UpdateArrowLongSizeChange(float progress)
        {
            if (!HasArrowLogic) return;
            arrowController.UpdateLongSizeChange(progress);
        }

        // ボリュームロングのサイズ変更演出を終了する
        // success: 成功扱いで終了するかどうか
        private void FinishArrowLongSizeChange(bool success)
        {
            if (!HasArrowLogic) return;
            arrowController.FinishLongSizeChange(success);
        }

        // ノート出現時に各 View へ生成を通知する
        // noteInfo: 出現したノート
        private void OnSpawnNote(NoteBase noteInfo)
        {
            presentationRoot.SpawnNote(noteInfo);
        }

        // ノート判定時にスコア更新、音声エフェクト終了、デバッグログを行う
        // noteInfo: 判定済みのノート
        private void OnJudgmentNote(NoteBase noteInfo)
        {
            scoreSystem.UpdateNoteResult(noteInfo);
            holdNoteSystem.ForceEndLongAudioEffectByEndNote(noteInfo);
            LogDebugJudgment(noteInfo);
        }

        // ノート表示解除時に各 View へ解除を通知する
        // noteInfo: 表示解除されたノート
        private void OnUnspawnNote(NoteBase noteInfo)
        {
            presentationRoot.UnspawnNote(noteInfo);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        // 開発環境向けに判定結果をログ出力する
        // noteInfo: 判定済みのノート
        private void LogDebugJudgment(NoteBase noteInfo)
        {
            if (noteInfo == null) return;
            float offset = CurrentFrameInfo.time - noteInfo.MusicScoreInfo.time;
            Debug.Log($"[LiveDebug] {noteInfo.Result} {noteInfo.Category} id {noteInfo.Id} offset {offset:0.000}s");
        }
    }
}
