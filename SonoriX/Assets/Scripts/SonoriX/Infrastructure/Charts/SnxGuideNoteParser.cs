using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    public class SnxGuideNoteParser
    {
        public static ChartNoteSourcePoint CreateLongPoint(
            int id,
            ChartPosition position,
            string lanePosition,
            int width,
            char action,
            int longGroupId)
        {
            SnxValueParser.ReadLongPoint(
                action,
                out ChartLongPointRole role,
                out ChartLineType lineType);
            
            return new ChartNoteSourcePoint(
                id,
                position,
                SnxValueParser.ReadLaneRange(lanePosition,width),
                ChartNoteCategory.Guide,
                ChartNoteDirection.Default,
                lineType,
                role,
                longGroupId,
                1f);
        }
    }
}