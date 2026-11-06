using System;
using System.Globalization;
using System.Text.RegularExpressions;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    // 负责 #EVENT 行的识别、分派及参数校验，不执行事件。
    public sealed class SnxEventParser
    {
        public bool TryReadEventLine(
            string line,
            out ChartEventSourcePoint sourceEvent)
        {
            sourceEvent = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            // 按一个或多个空白分割，允许空格和 Tab。
            string[] parts = Regex.Split(line.Trim(), @"\s+");

            if (parts[0] != "#EVENT")
            {
                return false;
            }

            if (parts.Length < 3)
            {
                throw new FormatException(
                    "事件至少需要：#EVENT 类型 小节号");
            }
            
            ChartEventType eventType;
            
            switch (parts[1])
            {
                case "BPM":
                    eventType = ChartEventType.BpmChange;
                    break;

                case "BPB":
                    eventType = ChartEventType.BeatsPerBarChange;
                    break;

                case "SPEED":
                    eventType = ChartEventType.SpeedRatioChange;
                    break;

                case "SEC":
                    eventType = ChartEventType.SeChange;
                    break;

                case "BACK":
                    eventType = ChartEventType.BackEffectChange;
                    break;
                
                case "FLASH":
                    eventType = ChartEventType.FlashNoteEvent;
                    break;

                default:
                    throw new FormatException(
                        $"未知的事件类型：{parts[1]}");
            }

            int expectedCount =
                eventType == ChartEventType.FlashNoteEvent ? 3 : 4;

            if (parts.Length != expectedCount)
            {
                throw new FormatException(
                    $"事件 {parts[1]} 应包含 {expectedCount} 个字段。");
            }

            bool validBar = int.TryParse(
                parts[2],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int bar);

            if (!validBar)
            {
                throw new FormatException(
                    $"小节号必须是非负整数：{parts[2]}");
            }

            // Flash 暂时只读取类型和位置。
            ChartPosition position = new ChartPosition(bar, 0);

            string valueText =
                eventType == ChartEventType.FlashNoteEvent
                    ? string.Empty
                    : parts[3];

            sourceEvent = CreateEvent(position, eventType, valueText);
            return true;
        }
        
        
        private static ChartEventSourcePoint CreateEvent(
            ChartPosition position,
            ChartEventType eventType,
            string valueText)
        {
    switch (eventType)
    {
        case ChartEventType.BpmChange:
        {
            float bpm = SnxValueParser.ReadBpmValue(valueText);

            return new ChartEventSourcePoint(
                position,
                eventType,
                numericValue: bpm);
        }

        case ChartEventType.BeatsPerBarChange:
        case ChartEventType.SpeedRatioChange:
        {
            bool isNumber = float.TryParse(
                valueText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value);

            if (!isNumber
                || float.IsNaN(value)
                || float.IsInfinity(value))
            {
                throw new FormatException(
                    $"事件 {eventType} 的参数必须是有限数值。");
            }

            if (eventType == ChartEventType.BeatsPerBarChange
                && value <= 0f)
            {
                throw new FormatException(
                    "每小节拍数必须大于 0。");
            }

            return new ChartEventSourcePoint(
                position,
                eventType,
                numericValue: value);
        }

        case ChartEventType.SeChange:
        case ChartEventType.BackEffectChange:
        {
            bool validId = int.TryParse(
                valueText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int presetId);

            if (!validId)
            {
                throw new FormatException(
                    $"事件 {eventType} 的预设编号必须是非负整数。");
            }

            return new ChartEventSourcePoint(
                position,
                eventType,
                presetId: presetId);
        }

        case ChartEventType.FlashNoteEvent:
            return new ChartEventSourcePoint(position, eventType);

        default:
            throw new InvalidOperationException(
                $"不支持创建事件：{eventType}");
    }
}
    }
}
