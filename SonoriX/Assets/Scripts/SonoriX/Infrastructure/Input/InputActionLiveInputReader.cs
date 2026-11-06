using System.Collections.Generic;
using SonoriX.Runtime;
using UnityEngine.InputSystem;

namespace SonoriX.Infrastructure.Input
{
    public sealed class InputActionLiveInputReader : ILiveInputReader
    {
        private readonly InputAction[] laneActions;

        public InputActionLiveInputReader(params InputAction[] laneActions)
        {
            this.laneActions = laneActions;
        }

        //启用所有Input
        public void Enable()
        {
            foreach (InputAction action in laneActions)
            {
                action?.Enable();
            }
        }

        //关闭所有Input
        public void Disable()
        {
            foreach (InputAction action in laneActions)
            {
                action?.Disable();
            }
        }

        //通过InputAction生产Tap类型Input
        public IReadOnlyList<LiveInput> Read(float musicTime)
        {
            List<LiveInput> inputs = new List<LiveInput>();

            for (int i = 0; i < laneActions.Length; i++)
            {
                InputAction action = laneActions[i];

                if (action != null && action.WasPressedThisFrame())
                {
                    inputs.Add(LiveInput.FixedLane(musicTime,i,LiveInputPhase.Began));
                }
            }

            return inputs;
        }
    }
}