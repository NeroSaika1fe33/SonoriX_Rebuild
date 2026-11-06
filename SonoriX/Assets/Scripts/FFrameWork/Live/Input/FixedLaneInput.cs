using System;
using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    public class FixedLaneInput
    {
        // 固定レーン入力の本数
        public const int FixedLaneInputCount = 4;

        // 各固定レーンに対応する InputAction
        private readonly InputAction[] _laneActions = new InputAction[FixedLaneInputCount];

        // 自動生成された入力アクションから固定レーン用のアクションを取得する
        // inputActions: Live 入力で使う Input System のアクション集合
        public FixedLaneInput(xxAxxInput inputActions)
        {
            _laneActions[0] = inputActions.InLiveInput.FixedLaneInput0;
            _laneActions[1] = inputActions.InLiveInput.FixedLaneInput1;
            _laneActions[2] = inputActions.InLiveInput.FixedLaneInput2;
            _laneActions[3] = inputActions.InLiveInput.FixedLaneInput3;
        }

        // 指定した固定レーンがこのフレームで押されたかを返す
        // index: 判定したい固定レーン番号
        public bool WasFixedLanePressedThisFrame(int index)
        {
            return IsValidIndex(index) && _laneActions[index].WasPressedThisFrame();
        }

        // 指定した固定レーンが押され続けているかを返す
        // index: 判定したい固定レーン番号
        public bool IsFixedLanePressed(int index)
        {
            return IsValidIndex(index) && _laneActions[index].IsPressed();
        }

        // 指定した固定レーンがこのフレームで離されたかを返す
        // index: 判定したい固定レーン番号
        public bool WasFixedLaneReleasedThisFrame(int index)
        {
            return IsValidIndex(index) && _laneActions[index].WasReleasedThisFrame();
        }

        // 固定レーン番号が配列範囲内かを返す
        // index: 検証する固定レーン番号
        private bool IsValidIndex(int index) => index >= 0 && index < FixedLaneInputCount;
    }
}
