using System.Collections.Generic;
using SonoriX.Runtime;
using UnityEngine.InputSystem;

namespace SonoriX.Infrastructure.Input
{
    public sealed class InputActionRawInputReader : IRawInputReader
    {
        private readonly InputAction[] laneActions;

        public InputActionRawInputReader(params InputAction[] laneActions)
        {
            this.laneActions = laneActions;
        }

        public void Enable()
        {
            foreach (InputAction action in laneActions)
            {
                action?.Enable();
            }
        }

        public void Disable()
        {
            foreach (InputAction action in laneActions)
            {
                action?.Disable();
            }
        }

        public IReadOnlyList<RawInputCommand> Read()
        {
            List<RawInputCommand> commands = new List<RawInputCommand>();

            for (int i = 0; i < laneActions.Length; i++)
            {
                InputAction action = laneActions[i];
                if (action == null)
                {
                    continue;
                }

                bool began = action.WasPressedThisFrame();
                bool ended = action.WasReleasedThisFrame();

                if (began)
                {
                    commands.Add(new RawInputCommand(
                        RawInputType.FixedLane, RawInputPhase.Began, i, 1f));
                }

                if (ended)
                {
                    commands.Add(new RawInputCommand(
                        RawInputType.FixedLane, RawInputPhase.Ended, i, 0f));
                }

                if (!began && !ended && action.IsPressed())
                {
                    commands.Add(new RawInputCommand(
                        RawInputType.FixedLane, RawInputPhase.Holded, i, 1f));
                }
            }
            return commands;
        }
    }
}