using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    // 現在有効にしている入力マップの種類
    public enum LiveInputScope
    {
        // すべての入力を無効化している状態
        None,

        // ライブ中のプレイ入力だけを受け付ける状態
        InLive,

        // ライブ外の選曲入力だけを受け付ける状態
        OutLive
    }

    public class InputManager : IDisposable
    {
        // Input System が生成した入力アクション全体
        private readonly xxAxxInput inputActions;

        // ライブ中に使う入力マップ
        private readonly xxAxxInput.InLiveInputActions inLiveInput;

        // ライブ外に使う入力マップ
        private readonly xxAxxInput.OutLiveInputActions outLiveInput;

        // 固定レーン入力を扱うラッパー
        public FixedLaneInput FixedLaneInput { get; }

        // 矢印入力を扱うラッパー
        public ArrowInput ArrowInput { get; }

        // フリーレーン入力を扱うラッパー
        public FreeLaneInput FreeLaneInput { get; }

        // 現在有効な入力スコープ
        public LiveInputScope CurrentScope { get; private set; }

        // ライブ中入力マップが有効か
        public bool IsInLiveInputEnabled => inLiveInput.enabled;

        // ライブ外入力マップが有効か
        public bool IsOutLiveInputEnabled => outLiveInput.enabled;

        // 矢印位置の入力差分
        public float ArrowPositionDelta => ArrowInput.PositionDelta;

        // 矢印サイズの入力差分
        public float ArrowSizeDelta => ArrowInput.SizeDelta;

        // フリーレーンが押されているか
        public bool IsFreeLanePressed => FreeLaneInput.IsPressed;

        // 選曲入力が押されているか
        public bool IsSelectSongsPressed => outLiveInput.SelectSongs.IsPressed();

        // 選曲入力がこのフレームで押されたか
        public bool WasSelectSongsPressedThisFrame => outLiveInput.SelectSongs.WasPressedThisFrame();

        // 選曲入力がこのフレームで離されたか
        public bool WasSelectSongsReleasedThisFrame => outLiveInput.SelectSongs.WasReleasedThisFrame();

        // 選曲入力の現在値
        public float SelectSongsValue => outLiveInput.SelectSongs.ReadValue<float>();

        // 選曲入力の started/performed/canceled を外部へ通知するイベント
        public event Action<InputAction.CallbackContext> SelectSongsInputReceived;

        // 各入力ラッパーを作成し、初期状態ではすべての入力を無効化する
        public InputManager()
        {
            inputActions = new xxAxxInput();
            inLiveInput = inputActions.InLiveInput;
            outLiveInput = inputActions.OutLiveInput;

            FixedLaneInput = new FixedLaneInput(inputActions);
            ArrowInput = new ArrowInput(inputActions);
            FreeLaneInput = new FreeLaneInput(inputActions);

            outLiveInput.SelectSongs.started += OnSelectSongs;
            outLiveInput.SelectSongs.performed += OnSelectSongs;
            outLiveInput.SelectSongs.canceled += OnSelectSongs;

            DisableAll();
        }

        // 固定レーン入力を LiveInput 構造体に変換できるかを試す
        // fixedLaneIndex: 取得したい固定レーン番号
        // musicTime: 入力時点の楽曲時間
        // input: 変換できた場合に返すライブ入力情報
        public bool TryGetFixedLaneInput(int fixedLaneIndex, float musicTime, out LiveInput input)
        {
            input = default;
            LiveInputPhase phase;

            if (FixedLaneInput.WasFixedLanePressedThisFrame(fixedLaneIndex)) phase = LiveInputPhase.Began;
            else if (FixedLaneInput.IsFixedLanePressed(fixedLaneIndex)) phase = LiveInputPhase.Moved;
            else if (FixedLaneInput.WasFixedLaneReleasedThisFrame(fixedLaneIndex)) phase = LiveInputPhase.Ended;
            else return false;

            input.inputId = fixedLaneIndex;
            input.phase = phase;
            input.musicTime = musicTime;
            input.source = LiveInputSource.FixedLane;
            return true;
        }

        // フリーレーン入力を LiveInput 構造体に変換できるかを試す
        // musicTime: 入力時点の楽曲時間
        // input: 変換できた場合に返すライブ入力情報
        public bool TryGetFreeLaneInput(float musicTime, out LiveInput input)
        {
            input = default;
            LiveInputPhase phase;

            if (FreeLaneInput.WasPressedThisFrame) phase = LiveInputPhase.Began;
            else if (FreeLaneInput.IsPressed) phase = LiveInputPhase.Moved;
            else if (FreeLaneInput.WasReleasedThisFrame) phase = LiveInputPhase.Ended;
            else return false;

            input.inputId = -1;
            input.phase = phase;
            input.musicTime = musicTime;
            input.source = LiveInputSource.FreeLane;
            return true;
        }

        // ライブ中入力だけを有効化する
        public void EnableInLiveInput()
        {
            outLiveInput.Disable();
            inLiveInput.Enable();
            CurrentScope = LiveInputScope.InLive;
        }

        // ライブ外入力だけを有効化する
        public void EnableOutLiveInput()
        {
            inLiveInput.Disable();
            outLiveInput.Enable();
            CurrentScope = LiveInputScope.OutLive;
        }

        // すべての入力マップを無効化する
        public void DisableAll()
        {
            inLiveInput.Disable();
            outLiveInput.Disable();
            CurrentScope = LiveInputScope.None;
        }

        // 入力イベント購読を解除し、InputActionAsset を破棄する
        public void Dispose()
        {
            outLiveInput.SelectSongs.started -= OnSelectSongs;
            outLiveInput.SelectSongs.performed -= OnSelectSongs;
            outLiveInput.SelectSongs.canceled -= OnSelectSongs;

            DisableAll();
            inputActions.Dispose();
        }

        // 選曲入力のコールバックを購読者へ中継する
        // context: Input System から渡される入力状態
        private void OnSelectSongs(InputAction.CallbackContext context)
        {
            SelectSongsInputReceived?.Invoke(context);
        }
    }
}
