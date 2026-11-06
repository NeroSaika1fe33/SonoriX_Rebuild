using System;
using System.Collections.Generic;
using System.Linq;

namespace SonoriX.Domain.Charts
{
    //将小节，Tick换算成播放秒数
    public sealed class ChartTimingMap
    {
        private readonly int ticksPerBeat;
        private readonly IReadOnlyList<ChartTimingPoint> timingPoints;
        public IReadOnlyList<ChartTimingPoint> TimingPoints => timingPoints;
        //先按小节 Bar 排序
        //如果 Bar 相同，再按 Tick 排序
        //最后变成 List 存起来
        public ChartTimingMap(int ticksPerBeat, IReadOnlyList<ChartTimingPoint> timingPoints)
        {
            this.ticksPerBeat = ticksPerBeat;
            //创建临时列表，将 timing point 按谱面位置排序，保证后续可以从前往后累计时间
            List<ChartTimingPoint> sortedTimingPoints = timingPoints != null
                ? timingPoints
                    .OrderBy(point => point.Position.Bar)
                    .ThenBy(point => point.Position.Tick)
                    .ToList()
                : new List<ChartTimingPoint>();

            if (sortedTimingPoints.Count == 0)
            {
                sortedTimingPoints.Add(new ChartTimingPoint(
                    new ChartPosition(0,0),
                    120f,
                    4f,
                    1f,
                    1f));
            }
            
            ChartPosition firstPosition = sortedTimingPoints[0].Position;
            if (firstPosition.Bar != 0 || firstPosition.Tick != 0)
            {
                throw new InvalidOperationException(
                    "时间线的第一个节点必须位于 (0, 0)。");
            }
            
            this.timingPoints = sortedTimingPoints;
        }

        // 将谱面编辑坐标 Bar/Tick 转换成音乐播放秒数
        // Runtime 判定和播放需要秒数，但编辑器更适合用小节和 tick 表示位置
        public float ToSeconds(ChartPosition position)
        {
            float seconds = 0f;
            ChartTimingPoint current = timingPoints[0];

            for (int i = 1; i < timingPoints.Count; i++)
            {
                ChartTimingPoint next = timingPoints[i];
                if (ComparePosition(position, next.Position) < 0)
                {
                    break;
                }
                
                seconds += GetDurationSeconds(current.Position, next.Position,current);
                current = next;
            }
            
            seconds += GetDurationSeconds(current.Position, position, current);
            return seconds;
        }

        private static int ComparePosition(ChartPosition a, ChartPosition b)
        {
            int barCompare = a.Bar.CompareTo(b.Bar);
            if (barCompare != 0)
            {
                return barCompare;
            }
            
            return a.Tick.CompareTo(b.Tick);
        }

        // 根据当前 timing point 的 BPM 和拍子，计算 from 到 to 之间经过的秒数
        private float GetDurationSeconds(ChartPosition from, ChartPosition to, ChartTimingPoint timing)
        {
            float ticks = GetTickDistance(from, to, timing.TimeSignature);
            float secondsPerTick = 60f / timing.Bpm / ticksPerBeat;
            return secondsPerTick * ticks;
        }

        // 计算两个谱面坐标之间相差多少 tick
        // 每小节 tick 数 = 每拍 tick 数 * 每小节拍数
        private float GetTickDistance(ChartPosition from, ChartPosition to, float timeSignature)
        {
            float ticksPerBar = ticksPerBeat *  timeSignature;
            return (to.Bar - from.Bar) * ticksPerBar + (to.Tick - from.Tick);
        }
    }
}