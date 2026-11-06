using System.Collections.Generic;

namespace SonoriX.Domain.Charts
{
    //谱面总构建接口
    public sealed class ChartDefinition
    {
        public int TicksPerBeat { get; }
        public IReadOnlyList<ChartNoteDefinition> Notes { get; }
        public IReadOnlyList<ChartTimingPoint> TimingPoints { get; }
        public IReadOnlyList<ChartEventDefinition> Events { get; }
        
        public ChartDefinition(
            int ticksPerBeat,
            IReadOnlyList<ChartNoteDefinition> notes,
            IReadOnlyList<ChartTimingPoint> timingPoints,
            IReadOnlyList<ChartEventDefinition> events = null)
        {
            TicksPerBeat = ticksPerBeat;
            Notes = notes;
            TimingPoints = timingPoints;
            Events = events
                     ?? System.Array.Empty<ChartEventDefinition>();
        }
    }
}