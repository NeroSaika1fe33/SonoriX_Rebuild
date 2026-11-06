using System;
using System.Collections.Generic;
using System.Linq;

namespace SonoriX.Domain.Charts
{
    public sealed class ChartTimingCompiler
    {
        public IReadOnlyList<ChartTimingPoint> Compile(
            ChartSourceDefinition source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            // 复用现有的默认节点、排序及原点检查。
            ChartTimingMap baseMap = new ChartTimingMap(
                source.TicksPerBeat,
                source.TimingPoints);

            var baseGroup = baseMap.TimingPoints.ToLookup(point => point.Position);
            var eventGroups = source.Events.Where(IsTimingEvent).ToLookup(sourceEvent => sourceEvent.Position);
            var positions = baseGroup.Select(group => group.Key)
                .Concat(eventGroups.Select(group => group.Key))
                .Distinct()
                .OrderBy(position => position.Bar)
                .ThenBy(position => position.Tick);

            List<ChartTimingPoint> result = new List<ChartTimingPoint>();
            
            ChartTimingPoint current = baseMap.TimingPoints[0];

            foreach (ChartPosition position in positions )
            {
                if (position.Bar < 0 || position.Tick < 0)
                {
                    throw new InvalidOperationException(
                        "时间线位置不能为负数。");
                }
                
                //同位置最多允许一个完整的基础节点
                ChartTimingPoint basePoint =
                    baseGroup[position].SingleOrDefault();

                if (basePoint != null)
                {
                    current = basePoint;
                }

                float bpm = current.Bpm;
                float beatsPerBar = current.TimeSignature;
                float speedRatio = current.SpeedRatio;

                HashSet<ChartEventType> appliedTypes = new HashSet<ChartEventType>();

                foreach (ChartEventSourcePoint sourceEvent in eventGroups[position])
                {
                    if (!appliedTypes.Add(sourceEvent.Type))
                    {
                        throw new InvalidOperationException(
                            $"位置 ({position.Bar}, {position.Tick}) "
                            + $"重复设置了 {sourceEvent.Type}。");
                    }

                    float value = ReadValue(sourceEvent);

                    switch (sourceEvent.Type)
                    {
                        case ChartEventType.BpmChange:
                            bpm = value;
                            break;
                        case ChartEventType.BeatsPerBarChange:
                            beatsPerBar = value;
                            break;
                        case ChartEventType.SpeedRatioChange:
                            speedRatio = value;
                            break;
                    }
                }
                
                current = new ChartTimingPoint(
                    position,
                    bpm,
                    beatsPerBar,
                    speedRatio,
                    current.SeVolume);
                
                result.Add(current);
            }
            
            return result;
        }

        private static bool IsTimingEvent(
            ChartEventSourcePoint sourceEvent)
        {
            return sourceEvent.Type == ChartEventType.BpmChange
                   || sourceEvent.Type == ChartEventType.BeatsPerBarChange
                   || sourceEvent.Type == ChartEventType.SpeedRatioChange;
        }
        
        private static float ReadValue(ChartEventSourcePoint sourceEvent)
        {
            if (!sourceEvent.NumericValue.HasValue || sourceEvent.PresetId.HasValue)
            {
                throw new InvalidOperationException(
                    $"事件 {sourceEvent.Type} 的参数类型不正确。");
            }
            
            float value = sourceEvent.NumericValue.Value;
            
            if(float.IsNaN(value)||float.IsInfinity(value)
                                 ||(sourceEvent.Type != ChartEventType.SpeedRatioChange
                                 && value <= 0f))
            {
                throw new InvalidOperationException(
                    $"事件 {sourceEvent.Type} 的数值不合法。");
            }

            return value;
        }
    }
}