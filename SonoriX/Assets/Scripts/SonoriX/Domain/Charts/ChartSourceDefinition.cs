using System.Collections.Generic;

namespace SonoriX.Domain.Charts
{
    public sealed class ChartSourceDefinition
    {
        public int TicksPerBeat { get; }
        public IReadOnlyList<ChartTimingPoint> TimingPoints { get; }
        public IReadOnlyList<ChartNoteSourcePoint> SourcePoints { get; }
        public IReadOnlyList<ChartEventSourcePoint> Events { get; }
        
        public ChartSourceDefinition(
            int ticksPerBeat,
            IReadOnlyList<ChartTimingPoint> timingPoints,
            IReadOnlyList<ChartNoteSourcePoint> sourcePoints,
            IReadOnlyList<ChartEventSourcePoint> events = null)
        {
            TicksPerBeat = ticksPerBeat;
            TimingPoints = timingPoints;
            SourcePoints = sourcePoints;
            Events = events ?? System.Array.Empty<ChartEventSourcePoint>();
        }
    }
}