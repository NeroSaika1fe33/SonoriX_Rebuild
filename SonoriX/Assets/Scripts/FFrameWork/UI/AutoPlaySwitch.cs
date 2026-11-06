// Codex 生成、デバッグ用
using FFrameWork.Live;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FFrameWork.UI
{
    [DisallowMultipleComponent]
    // オートプレイ切り替え入力と表示を管理する
    public class AutoPlaySwitch : MonoBehaviour
    {
        // パネル状態に応じた使用可否を判定する遷移 Controller
        [SerializeField] private LivePanelFlowController flowController;
        // 直接オートプレイを設定するライブ Controller
        [SerializeField] private LiveSceneController liveController;
        // U キーで切り替えるか
        [SerializeField] private bool toggleWithUKey = true;
        // 使用不可状態で操作した時にログを出すか
        [SerializeField] private bool logBlockedUse;
        // オートプレイ中に表示するテキスト
        [SerializeField] private TMP_Text autoText;
        // 使用不可の画面ではテキストを隠すか
        [SerializeField] private bool hideTextWhenUnavailable = true;

        // 現在のオートプレイ状態
        public bool IsOn => ResolveCurrentValue();
        // 現在の画面で切り替え可能か
        public bool CanUse => ResolveFlowController() == null || ResolveFlowController().CanUseAutoPlaySwitch;

        private void Awake()
        {
            ResolveAutoText();
        }

        private void OnEnable()
        {
            LivePlayerSettings.AutoPlayChanged += OnAutoPlayChanged;
            RefreshAutoText();
        }

        private void OnDisable()
        {
            LivePlayerSettings.AutoPlayChanged -= OnAutoPlayChanged;
        }

        private void Update()
        {
            if (toggleWithUKey)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.uKey.wasPressedThisFrame)
                {
                    TryToggleAutoPlay();
                }
            }

            RefreshAutoText();
        }

        // UI Button からオートプレイを反転する
        public void ToggleAutoPlay()
        {
            TryToggleAutoPlay();
        }

        // UI Button からオートプレイ状態を指定する
        public void SetAutoPlay(bool enabled)
        {
            TrySetAutoPlay(enabled);
        }

        // オートプレイを有効にする
        public void TurnOn()
        {
            TrySetAutoPlay(true);
        }

        // オートプレイを無効にする
        public void TurnOff()
        {
            TrySetAutoPlay(false);
        }

        // 現在状態を反転し、適用できたかを返す
        public bool TryToggleAutoPlay()
        {
            return TrySetAutoPlay(!ResolveCurrentValue());
        }

        // 画面状態を確認してからオートプレイを設定する
        public bool TrySetAutoPlay(bool enabled)
        {
            LivePanelFlowController flow = ResolveFlowController();
            if (flow != null)
            {
                bool applied = flow.TrySetAutoPlay(enabled);
                if (!applied)
                {
                    LogBlockedUse();
                }

                RefreshAutoText();
                return applied;
            }

            LiveSceneController live = ResolveLiveController();
            if (live != null)
            {
                live.SetAutoPlay(enabled);
            }
            else
            {
                LivePlayerSettings.SetAutoPlay(enabled);
            }

            RefreshAutoText();
            return true;
        }

        private void OnAutoPlayChanged(bool enabled)
        {
            RefreshAutoText();
        }

        // オートプレイ表示の表示/非表示を現在状態へ合わせる
        private void RefreshAutoText()
        {
            TMP_Text text = ResolveAutoText();
            if (text == null)
            {
                return;
            }

            bool visible = ResolveCurrentValue();
            if (hideTextWhenUnavailable && !CanUse)
            {
                visible = false;
            }

            if (text.gameObject.activeSelf != visible)
            {
                text.gameObject.SetActive(visible);
            }
        }

        // Inspector 未設定時に子階層から表示テキストを探す
        private TMP_Text ResolveAutoText()
        {
            if (autoText != null)
            {
                return autoText;
            }

            autoText = GetComponentInChildren<TMP_Text>(true);
            return autoText;
        }

        // 優先 Controller から現在のオートプレイ状態を取得する
        private bool ResolveCurrentValue()
        {
            LivePanelFlowController flow = ResolveFlowController();
            if (flow != null)
            {
                return flow.IsAutoPlay;
            }

            LiveSceneController live = ResolveLiveController();
            return live != null ? live.IsAutoPlay : LivePlayerSettings.AutoPlay;
        }

        // パネル遷移 Controller を取得する
        private LivePanelFlowController ResolveFlowController()
        {
            if (flowController != null)
            {
                return flowController;
            }

            flowController = GetComponentInParent<LivePanelFlowController>();
            if (flowController == null)
            {
                flowController = FindAnyObjectByType<LivePanelFlowController>();
            }

            return flowController;
        }

        // ライブ Controller を取得する
        private LiveSceneController ResolveLiveController()
        {
            if (liveController != null)
            {
                return liveController;
            }

            liveController = GetComponentInParent<LiveSceneController>();
            if (liveController == null)
            {
                liveController = FindAnyObjectByType<LiveSceneController>();
            }

            return liveController;
        }

        // 使用不可状態で操作したことを必要に応じて出力する
        private void LogBlockedUse()
        {
            if (logBlockedUse)
            {
                Debug.LogWarning("AutoPlaySwitch can only be used outside Title, InGame, and Result panels.", this);
            }
        }
    }
}
