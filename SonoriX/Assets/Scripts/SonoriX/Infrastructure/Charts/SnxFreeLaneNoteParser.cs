using System;
using System.Globalization;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    public sealed class SnxFreeLaneNoteParser
    {
        public static ChartNoteSourcePoint CreateTapPoint(
            int id,
            ChartPosition position,
            string lanePosition,
            int width,
            ChartNoteCategory category,
            ChartLongPointRole role,
            ChartLineType lineType,
            int longGroupId)
        {
            return new ChartNoteSourcePoint(
                id,
                position,
                SnxValueParser.ReadLaneRange(lanePosition, width),
                category,
                ChartNoteDirection.Default,
                lineType,
                role,
                longGroupId,
                1f);
        }

        public static ChartNoteSourcePoint CreateTapPoint(
            int id,
            ChartPosition position,
            string lanePosition,
            int width)
        {
            return CreateTapPoint(
                id, position, lanePosition, width,
                ChartNoteCategory.FreeTap,
                ChartLongPointRole.None,
                ChartLineType.None,
                -1);
        }

        public static ChartNoteSourcePoint CreateLongPoint(
            int id,
            ChartPosition position,
            string lanePosition,
            int width,
            char action,
            int longGroupId)
        {
            SnxValueParser.ReadLongPoint(action,out ChartLongPointRole role,out ChartLineType lineType);
            
            return CreateTapPoint(
                id,position,lanePosition,width,
                ChartNoteCategory.FreeLong,
                role,
                lineType,
                longGroupId);
        }
    }
}