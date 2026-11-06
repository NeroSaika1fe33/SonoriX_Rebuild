using System;
using System.Globalization;
using System.Text.RegularExpressions;
using SonoriX.Domain.Charts;
using System.Collections.Generic;
using System.IO;

namespace SonoriX.Infrastructure.Charts
{
    public sealed class SnxChartParser
    {
        private readonly SnxEventParser eventParser = new SnxEventParser();
        private readonly SnxNoteParser noteParser = new SnxNoteParser();

        private static readonly Regex TicksPerBeatRegex = new Regex(
            @"^#REQUEST\s+""ticks_per_beat\s+(?<value>[^""\s]+)\s*""\s*$");

        private static readonly Regex InitialBpmRegex = new Regex(
            @"^#BPM\s+(?<value>\S+)\s*$");

        public ChartSourceDefinition Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int ticksPerBeat = 480;
            bool hasTicksPerBeat = false;
            float? initialBpm = null;
            
            List<ChartEventSourcePoint> events = new List<ChartEventSourcePoint>();
            List<(int LineNumber, SnxNoteLine Note)> noteLines = new List<(int LineNumber, SnxNoteLine Note)>();

            //逐行检查文本
            using (StringReader reader = new StringReader(text))
            {
                string rawLine;
                int lineNumber = 0;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    
                    string line = rawLine.Trim();

                    if (line.Length == 0 || line.StartsWith("//"))
                    {
                        continue;
                    }

                    try
                    {
                        if (TryReadTicksPerBeat(line, out int parsedTicks))
                        {
                            if (hasTicksPerBeat)
                            {
                                throw new FormatException(
                                    "ticks_per_beat 不能重复声明。");
                            }

                            ticksPerBeat = parsedTicks;
                            hasTicksPerBeat = true;
                            continue;
                        }

                        if (TryReadInitialBpm(line, out float parsedBpm))
                        {
                            if (initialBpm.HasValue)
                            {
                                throw new FormatException(
                                    "初始 BPM 不能重复声明，请使用 BPM 变化事件。");
                            }

                            initialBpm = parsedBpm;
                            continue;
                        }

                        if (TryReadEventLine(line, out ChartEventSourcePoint sourceEvent))
                        {
                            events.Add(sourceEvent);
                            continue;
                        }

                        if (noteParser.TryReadNoteLine(
                                line,
                                out SnxNoteLine noteLine))
                        {
                            noteLines.Add((lineNumber, noteLine));
                            continue;
                        }
                        
                        throw new FormatException(
                            $"当前暂不支持此语句：{line}");
                    }
                    catch (FormatException exception)
                    {
                        throw new FormatException(
                            $"第 {lineNumber} 行：{exception.Message}",
                            exception);
                    }
                }
            }

            if (!initialBpm.HasValue)
            {
                throw new FormatException(
                    "谱面缺少初始 BPM，请声明 #BPM 数值。");
            }

            ChartTimingPoint[] timingPoints =
            {
                new ChartTimingPoint(
                    new ChartPosition(0, 0),
                    initialBpm.Value, 4f, 1f, 1f)
            };

            ChartSourceDefinition timingSource = new ChartSourceDefinition(
                ticksPerBeat,
                timingPoints,
                Array.Empty<ChartNoteSourcePoint>(),
                events);
            
            IReadOnlyList<ChartTimingPoint> compliedTimingPoints = 
                new ChartTimingCompiler().Compile(timingSource);
            
            List<ChartNoteSourcePoint> sourcePoints = new List<ChartNoteSourcePoint>();
            
            //整张谱面公用，不能在每行开始时归零
            int nextId = 0;

            Dictionary<string, int> longGroupIds = 
                new Dictionary<string, int>(StringComparer.Ordinal);
            
            foreach (var entry in noteLines)
            {
                try
                {
                    float beatsPerBar = ReadBeatsPerBar(entry.Note.Bar,
                        compliedTimingPoints);
                    noteParser.AppendSourcePoints(
                        entry.Note,
                        ticksPerBeat,
                        beatsPerBar,
                        sourcePoints,
                        longGroupIds,
                        ref nextId);
                }
                catch (FormatException exception)
                {
                    throw new FormatException(
                        $"第 {entry.LineNumber} 行：{exception.Message}",
                        exception);
                }
            }
            
            return new ChartSourceDefinition(
                ticksPerBeat,
                timingPoints,
                sourcePoints,
                events);
        }
        
        public bool TryReadTicksPerBeat(
            string line,
            out int ticksPerBeat)
        {
            ticksPerBeat = 0;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            //清理首尾空格
            string normalized = line.Trim();
            //检查文本，保存为Match对象
            Match match = TicksPerBeatRegex.Match(normalized);
            //是否匹配为正则文本
            if (!match.Success)
            {
                return false;
            }

            //获得正则中的ticksPerBeat值
            string valueText = match.Groups["value"].Value;

            //检查是否是为整数
            bool isInteger = int.TryParse(
                valueText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out ticksPerBeat);

            if (!isInteger)
            {
                throw new FormatException(
                    $"ticks_per_beat 必须是整数，实际内容：{valueText}");
            }

            if (ticksPerBeat <= 0)
            {
                throw new FormatException(
                    "ticks_per_beat 必须大于 0。");
            }

            return true;
        }

        public bool TryReadInitialBpm(string line, out float bpm)
        {
            bpm = 0f;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string normalized = line.Trim();
            Match match = InitialBpmRegex.Match(normalized);

            if (!match.Success)
            {
                if (Regex.IsMatch(normalized, @"^#BPM(?:\s|$)"))
                {
                    throw new FormatException(
                        "初始 BPM 格式应为：#BPM 数值");
                }

                return false;
            }

            bpm = SnxValueParser.ReadBpmValue(match.Groups["value"].Value);
            return true;
        }

        // 事件行的具体解析交给独立解析器。
        public bool TryReadEventLine(
            string line,
            out ChartEventSourcePoint sourceEvent)
        {
            return eventParser.TryReadEventLine(
                line,
                out sourceEvent);
        }

        private static float ReadBeatsPerBar(
            int bar,
            IReadOnlyList<ChartTimingPoint> timingPoints)
        {
            ChartTimingPoint current = timingPoints[0];

            foreach (ChartTimingPoint point in timingPoints)
            {
                //查找在目标小节起点已经生效的最后一个节点
                if (point.Position.Bar > bar
                    || (point.Position.Bar == bar
                        && point.Position.Tick > 0f))
                {
                    break;
                }
                
                current = point;
            }

            return current.TimeSignature;
        }
    }
}