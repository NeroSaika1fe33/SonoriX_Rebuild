using System.Collections.Generic;
using System.Linq;

namespace SonoriX.Domain.Charts
{
    public class ChartSourceCompiler
    {
        //由编译器转换成铺面定义接口
        public ChartDefinition Compile(ChartSourceDefinition source)
        {
            if (source == null)
            {
                source = new ChartSourceDefinition(
                    480,
                    null,
                    null);
            }

            IReadOnlyList<ChartTimingPoint> timingPoints =
                new ChartTimingCompiler().Compile(source);
            
            ChartTimingMap timingMap = new ChartTimingMap(
                source.TicksPerBeat,
                timingPoints);

            IReadOnlyList<ChartEventDefinition> events =
                new ChartEventCompiler().Compile(source.Events, timingMap);
            
            // 获取原始 Note 点；没有数据时使用空集合。
            IReadOnlyList<ChartNoteSourcePoint> sourcePoints = source.SourcePoints ?? System.Array.Empty<ChartNoteSourcePoint>();

            List<ChartNoteDefinition> notes = new List<ChartNoteDefinition>();

            foreach (ChartNoteSourcePoint point in sourcePoints
                         .OrderBy(point => point.Position.Bar)
                         .ThenBy(point => point.Position.Tick))
            {
                if (point.IsLongPoint)
                {
                    continue;
                }

                float time = timingMap.ToSeconds(point.Position);

                notes.Add(new ChartNoteDefinition(
                    point.Id,
                    time,
                    time,
                    point.LaneRange,
                    point.Category,
                    point.Direction,
                    point.LineType,
                    point.SpeedRatio));
            }
            
            foreach (var group in sourcePoints
                         .Where(point => point.IsLongPoint)
                         .GroupBy(point => point.LongGroupId))
            {
                notes.Add(CompileLongNote(group, timingMap,source.TicksPerBeat));
            }

            // 普通 Note 和长条分别生成后，重新合并排序。
            notes.Sort((a, b) => a.Time.CompareTo(b.Time));            
            
            return new ChartDefinition(
                source.TicksPerBeat,
                notes,
                timingMap.TimingPoints,
                events);
        }

        //编译长条型Note
            private ChartNoteDefinition CompileLongNote(
                IEnumerable<ChartNoteSourcePoint> sourcePoints,
                ChartTimingMap timingMap,
                int ticksPerBeat
            )
            {
                List<ChartNoteSourcePoint> points = sourcePoints
                    .OrderBy(point => point.Position.Bar)
                    .ThenBy(point => point.Position.Tick).ToList();

                //能编译长条的前提是内部连接点为两个及以上
                if (points.Count < 2)
                {
                    throw new System.InvalidOperationException(
                        "长条至少需要一个 Start 和一个 End。");
                }

                ChartNoteSourcePoint start = points[0];
                ChartNoteSourcePoint end = points[points.Count - 1];

                if (points.Any(point => point.Category != start.Category))
                {
                    throw new System.InvalidOperationException(
                        $"长条组 {start.LongGroupId} 不能混合不同的 Note 类型。");
                }
                
                if (start.Category == ChartNoteCategory.VolumeLong
                    && points.Any(point => point.Direction != start.Direction))
                {
                    throw new System.InvalidOperationException(
                        $"VolumeLong 组 {start.LongGroupId} 的方向必须一致。");
                }
                
                //开头Note必须是Start类型，结束Note必须是End类型
                if (start.LongPointRole != ChartLongPointRole.Start
                    || end.LongPointRole != ChartLongPointRole.End)
                {
                    throw new System.InvalidOperationException(
                        $"长条组 {start.LongGroupId} 必须以 Start 开始、End 结束。");
                }

                //同一个长条的中间点必须是Relay
                for (int i = 1; i < points.Count-1; i++)
                {
                    if (points[i].LongPointRole != ChartLongPointRole.Relay)
                    {
                        throw new System.InvalidOperationException(
                            $"长条组 {start.LongGroupId} 的中间点必须是 Relay。");
                    }
                }

                List<ChartLongPointDefinition> longPoints = new List<ChartLongPointDefinition>();

                foreach (ChartNoteSourcePoint point in points)
                {
                    longPoints.Add(new ChartLongPointDefinition(
                        point.Id,
                        timingMap.ToSeconds(point.Position),
                        point.LaneRange,
                        point.LongPointRole,
                        point.Direction,
                        point.LineType,
                        point.SpeedRatio));
                }

                float startTime = longPoints[0].Time;
                float endTime = longPoints[longPoints.Count - 1].Time;

                if (endTime <= startTime)
                {
                    throw new System.InvalidOperationException(
                        $"长条组 {start.LongGroupId} 的结束时间必须晚于开始时间。");
                }

                //沿用每小节八等分的hold判定频率
                //后续接入玩法配置时，再从配置传入
                const int divisionPerBar = 8;

                //若非Guide则产出Hold判定时间信息
                IReadOnlyList<float> holdTimes =
                    start.Category == ChartNoteCategory.Guide
                        ? System.Array.Empty<float>()
                        : CompileHoldTimes(
                            start.Position,
                            end.Position,
                            ticksPerBeat,
                            timingMap,
                            divisionPerBar);
                
                return new ChartNoteDefinition(
                    start.Id,
                    startTime,
                    endTime,
                    start.LaneRange,
                    start.Category,
                    start.Direction,
                    start.LineType,
                    start.SpeedRatio,
                    longPoints,
                    holdTimes);
                }

            private IReadOnlyList<float> CompileHoldTimes(
                ChartPosition start,
                ChartPosition end,
                int ticksPerBeat,
                ChartTimingMap timingMap,
                int divisionsPerBar)
            {
                if (divisionsPerBar <= 0)
                {
                    throw new System.ArgumentOutOfRangeException(
                        nameof(divisionsPerBar));
                }
                
                List<float> times = new List<float>();

                for (int bar = start.Bar; bar <= end.Bar; bar++)
                {
                    float beatsPerBar = ReadBeatsPerBarAtBarStart(
                        bar, timingMap.TimingPoints);
                    
                    float ticksPerBar = ticksPerBeat * beatsPerBar;
                    
                    //包含每个小杰起点，以便跨小节长条产生小节线上的节点
                    //小节末端由下一小节的第0格表示，避免重复
                    for (int division = 0; division < divisionsPerBar; division ++)
                    {
                        float tick = ticksPerBar * division / divisionsPerBar;
                        
                        bool afterStart =
                            bar > start.Bar
                            || (bar == start.Bar && tick > start.Tick);
                        
                        bool beforeEnd =
                            bar < end.Bar
                            || (bar == end.Bar && tick < end.Tick);

                        if (!afterStart || !beforeEnd)
                        {
                            continue;
                        }

                        ChartPosition position =
                            new ChartPosition(bar, tick);
                        
                        times.Add(timingMap.ToSeconds(position));
                    }
                }
                // 终点作为最后一次持续检查，不要求准确松手。
                times.Add(timingMap.ToSeconds(end));
                
                return times;
            }

            private static float ReadBeatsPerBarAtBarStart(
                int bar,
                IReadOnlyList<ChartTimingPoint> timingPoints)
            {
                float beatsPerBar = timingPoints[0].TimeSignature;
                foreach (ChartTimingPoint point in timingPoints)
                {
                    bool afterBarStart =
                        point.Position.Bar > bar
                        || (point.Position.Bar == bar
                            && point.Position.Tick > 0f);

                    if (afterBarStart)
                    {
                        break;
                    }

                    beatsPerBar = point.TimeSignature;
                }
                return beatsPerBar;
            }
        }
    }