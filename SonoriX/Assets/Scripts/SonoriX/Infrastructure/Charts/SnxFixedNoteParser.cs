using System;
using System.Globalization;
using SonoriX.Domain.Charts;

namespace SonoriX.Infrastructure.Charts
{
    //Fixed Note 类型，位置，宽度转换规则
    public static class SnxFixedNoteParser
    {
        public static string ReadAllowActions(string shape)
        {
            switch (shape)
            {
                case "tap":
                    return "n";
                
                case "long":
                    return "se";
                
                default:
                    throw new FormatException(
                        $"不支持的 Fixed Note 类型：{shape}");
            }
        }

        public static int ReadMaxWidth(string position)
        {
            if (string.IsNullOrEmpty(position)
                || position.Length < 2
                || position[0] != 'K')
            {
                throw new FormatException(
                    "Fixed Note 的位置必须使用大写 K1～K4。");
            }

            bool validNumber = int.TryParse(
                position.Substring(1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int keyNumber);

            if (!validNumber
                || keyNumber < 1
                || keyNumber > ChartLaneLayout.FixedKeyCount)
            {
                throw new FormatException(
                    $"固定键编号超出范围：{position}");
            }
            
            int laneStart = ChartLaneLayout
                .GetFixedKeyRange(keyNumber - 1)
                .StartLane;

            return ChartLaneLayout.FixedEndLaneExclusive - laneStart;
        }

        public static void ValidateWidth(int width)
        {
            if (width <= 0
                || width % ChartLaneLayout.FixedKeyWidth != 0)
            {
                throw new FormatException(
                    $"Fixed Note 的宽度必须为 "
                    + $"{ChartLaneLayout.FixedKeyWidth} 的正整数倍，"
                    + $"实际宽度：{width}");
            }
        }
        
        public static ChartNoteSourcePoint CreateTapPoint(
            int id,
            ChartPosition position,
            string keyPosition,
            int width,
            ChartNoteCategory category,
            ChartLongPointRole longPointRole,
            int longGroupId)
        {
            // 同时校验 K1～K4，并取得当前位置的最大宽度。
            int maxWidth = ReadMaxWidth(keyPosition);

            ValidateWidth(width);

            if (width > maxWidth)
            {
                throw new FormatException(
                    $"Fixed Note 超出固定区域：{keyPosition}，宽度 {width}");
            }

            int keyNumber = int.Parse(
                keyPosition.Substring(1),
                CultureInfo.InvariantCulture);

            int laneStart = ChartLaneLayout
                .GetFixedKeyRange(keyNumber - 1)
                .StartLane;

            ChartLaneRange laneRange = new ChartLaneRange(
                laneStart,
                laneStart + width - 1);

 
            
            return new ChartNoteSourcePoint(
                id,
                position,
                laneRange,
                category,
                ChartNoteDirection.Default,
                longPointRole == ChartLongPointRole.None
                    ? ChartLineType.None
                    : ChartLineType.Linear,
                longPointRole,
                longGroupId,
                1f);
        }

        public static ChartNoteSourcePoint CreateTapPoint(
            int id,
            ChartPosition position,
            string keyPosition,
            int width)
        {
            return CreateTapPoint(
                id, position, keyPosition, width,
                ChartNoteCategory.FixedTap,
                ChartLongPointRole.None,
                -1);
        }

        public static ChartNoteSourcePoint CreateLongPoint(
            int id,
            ChartPosition position,
            string keyPosition,
            int width,
            char action,
            int longGroupId)
        {
            ChartLongPointRole role;

            switch (action)
            {
                case 's':
                    role = ChartLongPointRole.Start;
                    break;
                case 'e':
                    role = ChartLongPointRole.End;
                    break;
                default:
                    throw new FormatException(
                        $"FixedLong 不支持动作：{action}");
            }
            return CreateTapPoint(
                id,position, keyPosition, width,
                ChartNoteCategory.FixedLong, role, longGroupId);
        }
    }
}