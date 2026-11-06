using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    public class FreeLaneInput
    {
        // フリーレーン全体の押下状態を読む InputAction
        private readonly InputAction _freeLaneAction;

        // フリーレーンが押され続けているか
        public bool IsPressed => _freeLaneAction != null && _freeLaneAction.IsPressed();

        // フリーレーンがこのフレームで押されたか
        public bool WasPressedThisFrame => _freeLaneAction != null && _freeLaneAction.WasPressedThisFrame();

        // フリーレーンがこのフレームで離されたか
        public bool WasReleasedThisFrame => _freeLaneAction != null && _freeLaneAction.WasReleasedThisFrame();

        // フリーレーン入力の現在値
        public float Value => _freeLaneAction != null ? _freeLaneAction.ReadValue<float>() : 0f;

        // 自動生成された入力アクションからフリーレーン用のアクションを取得する
        // inputActions: Live 入力で使う Input System のアクション集合
        public FreeLaneInput(xxAxxInput inputActions)
        {
            _freeLaneAction = inputActions.InLiveInput.FreeLaneInput;
        }
    }
}
