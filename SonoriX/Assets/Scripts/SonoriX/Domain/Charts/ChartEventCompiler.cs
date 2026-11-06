using System;
using System.Collections.Generic;
using System.Linq;

namespace SonoriX.Domain.Charts
{
    public sealed class ChartEventCompiler
    {
        
        
        public IReadOnlyList<ChartEventDefinition> Compile(
            IReadOnlyList<ChartEventSourcePoint> sourceEvents,
            ChartTimingMap timingMap)
        {
            if (timingMap == null)
            {
                throw new ArgumentNullException(nameof(timingMap));
            }

            List<ChartEventDefinition> events = new List<ChartEventDefinition>();

            IEnumerable<ChartEventSourcePoint> orderdEvents =
                (sourceEvents ?? Array.Empty<ChartEventSourcePoint>())
                .OrderBy(sourceEvents => sourceEvents.Position.Bar)
                .ThenBy(sourceEvents => sourceEvents.Position.Tick);
            
            foreach (ChartEventSourcePoint sourceEvent in orderdEvents)
            {
                switch (sourceEvent.Type)
                {
                    case ChartEventType.BpmChange:
                    case ChartEventType.BeatsPerBarChange:
                    case ChartEventType.SpeedRatioChange:
                        continue;
                    
                    case ChartEventType.SeChange:
                    case ChartEventType.BackEffectChange:
                        if (!sourceEvent.PresetId.HasValue
                            || sourceEvent.PresetId.Value < 0
                            || sourceEvent.NumericValue.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"事件 {sourceEvent.Type} 需要非负整数预设编号。");
                        }
                        break;
                    
                    case ChartEventType.FlashNoteEvent:
                        if (sourceEvent.PresetId.HasValue
                            || sourceEvent.NumericValue.HasValue)
                        {
                            throw new InvalidOperationException(
                                "当前 Flash 事件不接受参数。");
                        }
                        break;
                    
                    default:
                        throw new InvalidOperationException(
                            $"不支持的事件类型：{sourceEvent.Type}");
                }

                if (sourceEvent.Position.Bar < 0
                    || sourceEvent.Position.Tick < 0)
                {
                    throw new InvalidOperationException(
                        "播放事件的位置不能为负数。");
                }

                float time = timingMap.ToSeconds(sourceEvent.Position);
                
                events.Add((new ChartEventDefinition(
                    time,
                    sourceEvent.Type,
                    sourceEvent.PresetId)));
            }

            return events;
        }
    }
}