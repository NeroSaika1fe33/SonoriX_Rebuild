using System;
using System.Globalization;
using System.Runtime;
using System.Text.RegularExpressions;
using SonoriX.Domain.Charts;
using System.Collections.Generic;

namespace SonoriX.Infrastructure.Charts
{
    //Note转换
    public sealed class SnxNoteParser
    {
        private static readonly Regex NoteLineRegex = new Regex(
            @"^#(?<bar>[0-9]{3})(?:[ \t]+|\.)"
            + @"(?<path>[^:\s]+)[ \t]*:[ \t]*(?<data>.*)$");

        public bool TryReadNoteLine(
            string line,
            out SnxNoteLine noteLine)
        {
            noteLine = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string normalized = line.Trim();

            if (!Regex.IsMatch(normalized, @"^#[0-9]"))
            {
                return false;
            }
            
            Match match = NoteLineRegex.Match(normalized);

            if (!match.Success)
            {
                throw new FormatException(
                    "Note 行格式应为：#001 Fixed.Tap.K1:n4");
            }

            int bar = int.Parse(
                match.Groups["bar"].Value,
                CultureInfo.InvariantCulture);
            
            string[] path = match.Groups["path"].Value.Split('.');

            if (path.Length < 3)
            {
                throw new FormatException(
                    "Note 路径缺少家族、类型或位置。");
            }
            
            //家族，类型名称继续忽略大小写
            //不对位置，Token或整条路径统一转小写
            string family = path[0].ToLowerInvariant();
            string shape = path[1].ToLowerInvariant();
            
            string allowedActions = ReadAllowedActions(family, shape);

            bool isLong = shape == "long"
                          || shape == "long+"
                          || shape == "long-";
            
            int expectedPathCount = isLong ? 4 : 3;

            if (path.Length != expectedPathCount)
            {
                throw new FormatException(
                    isLong
                        ? "Long 路径应为：家族.类型.位置.长条编号"
                        : "普通 Note 路径应为：家族.类型.位置");
            }

            if (isLong
                && !Regex.IsMatch(path[3], @"\A[0-9a-zA-Z]+\z"))
            {
                throw new FormatException(
                    "长条编号只能包含数字和英文字母。");
            }

            bool isFixed = family == "fixed";

            int maxWidth = ReadMaxWidth(
                path[2],
                isFixed);

            string[] tokens = ReadTokens(
                match.Groups["data"].Value,
                allowedActions, 
                isFixed, 
                maxWidth);

            noteLine = new SnxNoteLine(bar, path, tokens);
            return true;
        }

        private static string ReadAllowedActions(
            string family,
            string shape)
        {
            if (family == "fixed")
            {
                return SnxFixedNoteParser.ReadAllowActions(shape);
            }
            
            switch (family + "." + shape)
            {
                case "free.tap":
                    return "n";
                case "free.long":
                case"slider.long":
                case"volume.long+":
                case"volume.long-":
                case"guide.long":
                    return "smioae";
                
                case"slider.flick":
                    return "lr";
                
                case "volume.flick":
                    return "du";
                
                default:
                    throw new FormatException(
                        $"不支持的 Note 类型：{family}.{shape}");
            }
        }
        
        private static int ReadMaxWidth(
            string position,
            bool isFixed)
        {
            if (isFixed)
            {
                return SnxFixedNoteParser.ReadMaxWidth(position);
            }

            if (string.IsNullOrEmpty(position)
                || position.Length < 2
                || position[0] != 'L')
            {
                throw new FormatException(
                    "自由轨道位置必须使用大写 L1～L32。");            
            }
            
            bool validNumber = int.TryParse(
                position.Substring(1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int number);

            if (!validNumber
                || number < 1
                || number > ChartLaneLayout.LaneCount)
            {
                throw new FormatException(
                    $"轨道位置超出范围：{position}");
            }
            
            int laneStart = number - 1;
            return ChartLaneLayout.LaneCount - laneStart;
        }
        
        private static string[] ReadTokens(
            string data,
            string allowedActions,
            bool isFixed,
            int maxWidth
        )
        {
            string commpact = Regex.Replace(data, @"\s+", "");

            if (commpact.Length == 0 || commpact.Length % 2 != 0)
            {
                throw new FormatException(
                    "Note 数据不能为空，且必须每两个字符组成一组。");
            }
            
            int count = commpact.Length / 2;
            string[] tokens = new string[count];

            for (int i = 0; i < count; i++)
            {
                string token = commpact.Substring(i * 2, 2);
                tokens[i] = token;

                if (token == "00")
                {
                    continue;
                }
                
                //不转大小写
                char action = token[0];
                if (allowedActions.IndexOf(action) < 0)
                {
                    throw new FormatException(
                        $"第 {i + 1} 组的动作不适用于当前 Note：{token}");
                }

                int width = ReadWidth(token[1]);

                if (width > maxWidth)
                {
                    throw new FormatException(
                        $"第 {i + 1} 组超出轨道范围：{token}");
                }

                if (isFixed)
                {
                    SnxFixedNoteParser.ValidateWidth(width);
                }
            }
            
            return tokens;
        }
        
        private static int ReadWidth(char code)
        {
            if (code >= '1' && code <= '9')
            {
                return code - '0';
            }

            if (code >= 'a' && code <= 'w')
            {
                return code - 'a' + 10;
            }
            
            throw new FormatException(
                $"无效的 Note 宽度：{code}。"
                + "请使用 1～9 或小写 a～w。");
        }

        public void AppendSourcePoints(
            SnxNoteLine line,
            int ticksPerBeat,
            float beatsPerBar,
            ICollection<ChartNoteSourcePoint> points,
            IDictionary<string,int> longGroupIds,
            ref int nextId)
        {
            string family = line.Path[0].ToLowerInvariant();
            string shape = line.Path[1].ToLowerInvariant();
            
            switch (family + "." + shape)
            {
                case "fixed.tap":
                case "fixed.long":
                case "free.tap":
                case "free.long":
                case "slider.flick":
                case "slider.long":
                case "volume.flick":
                case "volume.long+":
                case "volume.long-":
                case "guide.long":
                    break;

                default:
                    throw new FormatException(
                        $"当前暂不支持生成此类 Note 原始点：{family}.{shape}");
            }

            int longGroupId = -1;
            
            bool isLong = shape == "long"
                          || shape == "long+"
                          || shape == "long-";
            
            if (isLong)
            {
                string groupName = line.Path[3];

                if (!longGroupIds.TryGetValue(groupName, out longGroupId))
                {
                    longGroupId = longGroupIds.Count;
                    longGroupIds.Add(groupName, longGroupId);
                }
            }
            
            float ticksPerBar = ticksPerBeat * beatsPerBar;

            for (int i = 0; i < line.DivisionCount; i++)
            {
                string token = line.Tokens[i];
                if (token == "00")
                {
                    continue;
                }
                
                float tick = line.GetBarProgress(i) * ticksPerBar;

                ChartPosition position = new ChartPosition(
                    line.Bar,
                    tick);

                int width = ReadWidth(token[1]);

                ChartNoteSourcePoint point;
                
                if (family == "guide")
                {
                    point = SnxGuideNoteParser.CreateLongPoint(
                        nextId,
                        position,
                        line.Path[2],
                        width,
                        token[0],
                        longGroupId);
                }
                else if (family == "volume")
                {
                    if (shape == "flick")
                    {
                        point = SnxVolumeNoteParser.CreateFlickPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width,
                            token[0]);
                    }
                    else
                    {
                        point = SnxVolumeNoteParser.CreateLongPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width,
                            shape,
                            token[0],
                            longGroupId);
                    }
                }
                else if (family == "slider")
                {
                    if (shape == "flick")
                    {
                        point = SnxSliderNoteParser.CreateFlickPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width,
                            token[0]);
                    }
                    else
                    {
                        point = SnxSliderNoteParser.CreateLongPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width,
                            token[0],
                            longGroupId);
                    }
                }
                else if (family == "free")
                {
                    if (shape == "tap")
                    {
                        point = SnxFreeLaneNoteParser.CreateTapPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width);
                    }
                    else
                    {
                        point = SnxFreeLaneNoteParser.CreateLongPoint(
                            nextId,
                            position,
                            line.Path[2],
                            width,
                            token[0],
                            longGroupId);
                    }
                }
                else if (shape == "tap")
                {
                    point = SnxFixedNoteParser.CreateTapPoint(
                        nextId,
                        position,
                        line.Path[2],
                        width);
                }else {
                    point = SnxFixedNoteParser.CreateLongPoint(
                        nextId,
                        position,
                        line.Path[2],
                        width,
                        token[0],
                        longGroupId);
                }
                
                points.Add(point);
                nextId++;
            }
        }
    }
}