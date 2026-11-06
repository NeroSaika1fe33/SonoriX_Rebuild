using System;
using System.Globalization;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    // 初始 BPM 和 BPM 变化事件共用的数值解析规则。
    internal static class SnxValueParser
    {
        internal static float ReadBpmValue(string valueText)
        {
            bool isNumber = float.TryParse(
                valueText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float bpm);

            if (!isNumber
                || float.IsNaN(bpm)
                || float.IsInfinity(bpm)
                || bpm <= 0f)
            {
                throw new FormatException(
                    $"BPM 必须是大于 0 的有限数值，实际内容：{valueText}");
            }

            return bpm;
        }

        internal static ChartLaneRange ReadLaneRange(
            string lanePosition,
            int width)
        {
            if (string.IsNullOrEmpty(lanePosition)
                || lanePosition.Length < 2
                || lanePosition[0] != 'L')
            {
                throw new FormatException(
                    "自由坐标必须使用大写 L1～L32。");
            }

            bool validNumber = int.TryParse(
                lanePosition.Substring(1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int laneNumber);

            if (!validNumber
                || laneNumber < 1
                || laneNumber > ChartLaneLayout.LaneCount)
            {
                throw new FormatException(
                    $"轨道位置超出范围：{lanePosition}");
            }
            
            int laneStart = laneNumber - 1;
            int maxWidth = ChartLaneLayout.LaneCount - laneStart;

            if (width <= 0 || width > maxWidth)
            {
                throw new FormatException(
                    $"Note 宽度无效或越界：{lanePosition}，宽度 {width}");
            }

            return new ChartLaneRange(
                laneStart,
                laneStart + width - 1);
        }
        
        internal static void ReadLongPoint(
            char action,
            out ChartLongPointRole role,
            out ChartLineType lineType)
        {
            lineType = ChartLineType.None;

            switch (action)
            {
                case 's':
                    role = ChartLongPointRole.Start;
                    lineType  = ChartLineType.Linear;
                    return;

                case 'e':
                    role = ChartLongPointRole.End;
                    return;

                case 'm':
                    role = ChartLongPointRole.Relay;
                    lineType = ChartLineType.Linear;
                    return;

                case 'i':
                    role = ChartLongPointRole.Relay;
                    lineType = ChartLineType.EaseIn;
                    return;

                case 'o':
                    role = ChartLongPointRole.Relay;
                    lineType = ChartLineType.EaseOut;
                    return;

                case 'a':
                    role = ChartLongPointRole.Relay;
                    lineType = ChartLineType.RightAngle;
                    return;

                default:
                    throw new FormatException(
                        $"长条不支持动作：{action}");
            }
        }
    }
}
