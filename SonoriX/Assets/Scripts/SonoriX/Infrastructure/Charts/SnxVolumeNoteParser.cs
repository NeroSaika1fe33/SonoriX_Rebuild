using System;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    public class SnxVolumeNoteParser
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
                case'u':
                    direction = ChartNoteDirection.Up;
                    break;
                case'd':
                    direction =  ChartNoteDirection.Down;
                    break;
                
                default:
                    throw new FormatException(
                        $"VolumeFlick 不支持动作：{action}");
                    
            }
            
            return new ChartNoteSourcePoint(
                id,
                position,
                SnxValueParser.ReadLaneRange(lanePosition,width),
                ChartNoteCategory.VolumeFlick,
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
            string shape,
            char action,
            int longGroupId)
        {
            ChartNoteDirection direction;

            switch (shape)
            {
                case"long+":
                    direction = ChartNoteDirection.Up;
                    break;
                
                case"long-":
                    direction =  ChartNoteDirection.Down;
                    break;
                
                default:
                    throw new FormatException(
                        $"VolumeLong 必须声明 Long+ 或 Long-：{shape}");
            }
            
            SnxValueParser.ReadLongPoint(
                action,
                out ChartLongPointRole role,
                out ChartLineType lineType);
            
            return new ChartNoteSourcePoint(
                id,
                position,
                SnxValueParser.ReadLaneRange(lanePosition,width),
                ChartNoteCategory.VolumeLong,
                direction,
                lineType,
                role,
                longGroupId,
                1f);
        }
    }
}