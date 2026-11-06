using System;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    public static class SnxSliderNoteParser
    {
        public static ChartNoteSourcePoint CreateFlickPoint(
            int id,
            ChartPosition position,
            string lanePosition,
            int width,
            char action)
        {
            ChartNoteDirection direction;

            switch (action)
            {
                case'l':
                    direction =  ChartNoteDirection.Left;
                    break;
                case'r':
                    direction =  ChartNoteDirection.Right;
                    break;
                default:
                    throw new FormatException(
                        $"SliderFlick 不支持动作：{action}");
            }
            
            return new ChartNoteSourcePoint(
                id,
                position,
                SnxValueParser.ReadLaneRange(lanePosition, width),
                ChartNoteCategory.SliderFlick,
                direction,
                ChartLineType.None,
                ChartLongPointRole.None,
                -1,
                1f);
        }

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
                SnxValueParser.ReadLaneRange(lanePosition, width),
                ChartNoteCategory.SliderLong,
                ChartNoteDirection.Default,
                lineType,
                role,
                longGroupId,
                1f);
        }
    }
}