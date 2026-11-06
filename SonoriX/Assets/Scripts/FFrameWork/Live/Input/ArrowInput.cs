using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    public class ArrowInput
    {
        // 矢印レーンの位置変化を読む InputAction
        private readonly InputAction _arrowPositionAction;

        // 矢印レーンのサイズ変化を読む InputAction
        private readonly InputAction _arrowSizeAction;

        // 現在フレームの矢印位置入力差分
        public float PositionDelta => _arrowPositionAction != null ? _arrowPositionAction.ReadValue<float>() : 0f;

        // 現在フレームの矢印サイズ入力差分
        public float SizeDelta => _arrowSizeAction != null ? _arrowSizeAction.ReadValue<float>() : 0f;

        // 自動生成された入力アクションから矢印入力用のアクションを取得する
        // inputActions: Live 入力で使う Input System のアクション集合
        public ArrowInput(xxAxxInput inputActions)
        {
            _arrowPositionAction = inputActions.InLiveInput.ArrowInputPosition;
            _arrowSizeAction = inputActions.InLiveInput.ArrowInputSize;
        }
    }
}
