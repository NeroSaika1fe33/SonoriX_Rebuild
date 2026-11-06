using System.Collections.Generic;

namespace SonoriX.Runtime
{
    public sealed class LiveInputCollector : ILiveInputReader
    {
        private readonly IRawInputReader rawInputReader;

        public LiveInputCollector(IRawInputReader rawInputReader)
        {
            this.rawInputReader = rawInputReader;
        }

        public IReadOnlyList<LiveInput> Read(float musicTime)
        {
            List<LiveInput> inputs = new List<LiveInput>();

            foreach (RawInputCommand command in rawInputReader.Read())
            {
                if (command.Type != RawInputType.FixedLane)
                {
                    continue;
                }
                
                if (command.Phase == RawInputPhase.Began 
                    || command.Phase == RawInputPhase.Holded
                    || command.Phase == RawInputPhase.Moved
                    || command.Phase == RawInputPhase.Ended)
                {
                    inputs.Add(LiveInput.FixedLane(
                        musicTime,
                        command.LaneId,
                        ConvertPhase(command.Phase)));
                }
            }
            
            return inputs;
        }

        private static LiveInputPhase ConvertPhase(RawInputPhase phase)
        {
            switch (phase)
            {
                case RawInputPhase.Began:
                    return LiveInputPhase.Began;
                case RawInputPhase.Holded:
                    return LiveInputPhase.Holded;
                case RawInputPhase.Moved:
                    return LiveInputPhase.Moved;
                case RawInputPhase.Ended:
                    return LiveInputPhase.Ended;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(phase), phase, "未知输入阶段。");
            }
        }
    }
}