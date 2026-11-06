using System;
using SonoriX.Domain.Charts;
using SonoriX.Infrastructure.Charts;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SonoriX.Editor
{
    public static class ChartCompilerCheck
    {
        [MenuItem("SonoriX/Checks/Chart Compiler")]
        private static void Run()
        {
            bool parsingPassed = RunCheck("SNX 文本解析", CheckParsing);
            bool compilationPassed = RunCheck("谱面编译", CheckCompilation);
            bool rejectionPassed = RunCheck("错误拒绝", CheckInvalidInputs);

            if (parsingPassed && compilationPassed && rejectionPassed)
            {
                Debug.Log("Chart Compiler：3/3 组检查通过（文本解析、谱面编译、错误拒绝）。");
            }
            else
            {
                Debug.LogError("Chart Compiler：检查未全部通过，请查看上方失败组的日志。");
            }
        }

        private static bool RunCheck(string name, Action check)
        {
            try
            {
                check();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Chart Compiler [{name}] 检查失败：\n{exception}");
                return false;
            }
        }

        private static void CheckParsing()
        {
            string text = string.Join("\n", new[]
            {
                "// 最小 SNX 事件谱面",
                "#REQUEST \"ticks_per_beat 960\"",
                "#BPM 150",
                "",
                "#EVENT BPM 8 180",
                "#EVENT SPEED 12 1.5",
                "#EVENT SEC 16 2"
            });

            ChartSourceDefinition source = new SnxChartParser().Parse(text);

            Check(source.TicksPerBeat == 960, "应读取文本中的 Tick 设置");
            Check(source.TimingPoints.Count == 1, "应生成一个初始时间线节点");
            CheckNear(source.TimingPoints[0].Bpm, 150f, "初始 BPM");
            Check(source.SourcePoints.Count == 0 && source.Events.Count == 3,
                "应收集三个事件，当前样例没有 Note");

            ChartEventSourcePoint bpm = source.Events[0];
            ChartEventSourcePoint speed = source.Events[1];
            ChartEventSourcePoint se = source.Events[2];

            Check(
                bpm.Type == ChartEventType.BpmChange && bpm.Position.Bar == 8
                && bpm.NumericValue == 180f && !bpm.PresetId.HasValue,
                "BPM 事件数据");
            Check(
                speed.Type == ChartEventType.SpeedRatioChange && speed.Position.Bar == 12
                && speed.NumericValue == 1.5f && !speed.PresetId.HasValue,
                "速度事件数据");
            Check(
                se.Type == ChartEventType.SeChange && se.Position.Bar == 16
                && se.PresetId == 2 && !se.NumericValue.HasValue,
                "SE 预设事件数据");
            
            CheckMixedNotes();
        }

        private static void CheckCompilation()
        {
            ChartNoteSourcePoint tap = Point(1, 0, 1440, ChartLongPointRole.None);
            ChartNoteSourcePoint start = Point(2, 0, 480, ChartLongPointRole.Start);
            ChartNoteSourcePoint relay = Point(3, 0, 960, ChartLongPointRole.Relay);
            ChartNoteSourcePoint end = Point(4, 1, 0, ChartLongPointRole.End);

            ChartSourceCompiler compiler = new ChartSourceCompiler();
            ChartDefinition chart = compiler.Compile(new ChartSourceDefinition(
                480, null, new[] { end, tap, relay, start }));

            Check(chart.Notes.Count == 2 && chart.Notes[0].Id == 2 && chart.Notes[1].Id == 1,
                "混合谱面应生成一条长条和一个 Tap，并按时间排序");
            Check(chart.TimingPoints.Count == 1, "缺少时间线时应输出默认节点");
            CheckNear(chart.TimingPoints[0].Bpm, 120f, "默认 BPM");

            ChartNoteDefinition longNote = chart.Notes[0];
            Check(longNote.LongPoints.Count == 3, "长条应保留三个路径点");
            Check(
                longNote.LongPoints[0].Role == ChartLongPointRole.Start
                && longNote.LongPoints[1].Role == ChartLongPointRole.Relay
                && longNote.LongPoints[2].Role == ChartLongPointRole.End,
                "长条路径角色顺序");
            CheckNear(longNote.Time, 0.5f, "长条开始时间");
            CheckNear(longNote.LongPoints[1].Time, 1f, "Relay 时间");
            CheckNear(longNote.EndTime, 2f, "长条结束时间");
            CheckNear(chart.Notes[1].Time, 1.5f, "Tap 时间");

            ChartDefinition empty = compiler.Compile(null);
            Check(empty.Notes.Count == 0 && empty.TimingPoints.Count == 1,
                "空谱面应包含默认时间线");

            // 保留完整时间线节点的换算检查，再验证独立 EVENT 的编译。
            ChartTimingPoint[] changingBpm =
            {
                new ChartTimingPoint(new ChartPosition(1, 0), 240f, 4f, 1f, 1f),
                new ChartTimingPoint(new ChartPosition(0, 0), 120f, 4f, 1f, 1f)
            };
            ChartDefinition changingChart = compiler.Compile(new ChartSourceDefinition(
                480, changingBpm, new[] { Point(5, 2, 0, ChartLongPointRole.None) }));
            CheckNear(changingChart.Notes[0].Time, 3f, "BPM 分段换算");

            CheckTimingEvents(compiler);
            CheckPlaybackEvents(compiler);
            CheckCurrentLongRange();
            CheckFreeLongJudgment();
            CheckLongPathSegments();
        }

        private static void CheckPlaybackEvents(ChartSourceCompiler compiler)
        {
            // 故意打乱行顺序，确认播放事件使用最终时间线并按时间输出。
            ChartSourceDefinition source = new SnxChartParser().Parse(string.Join("\n", new[]
            {
                "#BPM 120",
                "#EVENT BACK 3 4",
                "#EVENT SEC 2 3",
                "#EVENT SPEED 2 1.5",
                "#EVENT BPM 1 240",
                "#EVENT FLASH 4"
            }));
            ChartDefinition chart = compiler.Compile(source);

            Check(chart.Events.Count == 3, "播放事件应包含 SEC、BACK 和预留的 FLASH");
            Check(chart.Events[0].Type == ChartEventType.SeChange && chart.Events[0].PresetId == 3,
                "SE 预设编号应保留到编译结果");
            CheckNear(chart.Events[0].Time, 3f, "SE 事件应使用变化后的 BPM 换算秒数");
            Check(chart.Events[1].Type == ChartEventType.BackEffectChange && chart.Events[1].PresetId == 4,
                "背景特效事件应保留并按时间排序");
            CheckNear(chart.Events[1].Time, 4f, "背景特效事件时间");
            Check(chart.Events[2].Type == ChartEventType.FlashNoteEvent && !chart.Events[2].PresetId.HasValue,
                "FLASH 暂时只保存类型和时间");
            CheckNear(chart.Events[2].Time, 5f, "FLASH 事件时间");
            Check(compiler.Compile(null).Events.Count == 0, "空谱面应输出空事件集合");
        }

        private static void CheckTimingEvents(ChartSourceCompiler compiler)
        {
            // 事件位于两个完整基础节点之间，应继承较早节点的状态。
            ChartTimingPoint[] basePoints =
            {
                new ChartTimingPoint(new ChartPosition(0, 0), 120f, 4f, 1f, 1f),
                new ChartTimingPoint(new ChartPosition(2, 0), 240f, 4f, 1f, 1f)
            };
            ChartDefinition mixed = compiler.Compile(new ChartSourceDefinition(
                480, basePoints, new[] { Point(6, 2, 0, ChartLongPointRole.None) },
                new[]
                {
                    new ChartEventSourcePoint(new ChartPosition(1, 0),
                        ChartEventType.SpeedRatioChange, numericValue: 1.5f)
                }));
            CheckNear(mixed.TimingPoints[1].Bpm, 120f, "中间事件不能提前使用后续 BPM");
            CheckNear(mixed.Notes[0].Time, 4f, "基础节点和事件应按统一顺序编译");

            // 原点覆盖、同位置多类型变化，以及 BPM/BPB/SPEED 的状态继承。
            ChartSourceDefinition parsed = new SnxChartParser().Parse(string.Join("\n", new[]
            {
                "#BPM 120",
                "#EVENT SPEED 0 1.5",
                "#EVENT SPEED 1 2",
                "#EVENT BPB 1 3",
                "#EVENT BPM 2 240"
            }));
            ChartDefinition chart = compiler.Compile(new ChartSourceDefinition(
                parsed.TicksPerBeat, parsed.TimingPoints,
                new[] { Point(7, 3, 0, ChartLongPointRole.None) }, parsed.Events));

            Check(chart.TimingPoints.Count == 3, "同位置事件应合并为一个时间线节点");
            CheckNear(chart.TimingPoints[0].SpeedRatio, 1.5f, "原点速度事件");
            CheckNear(chart.TimingPoints[1].TimeSignature, 3f, "每小节拍数变化");
            CheckNear(chart.TimingPoints[2].SpeedRatio, 2f, "BPM 变化应继承速度倍率");
            CheckNear(chart.Notes[0].Time, 4.25f, "事件参与 Note 时间换算，速度倍率不改变秒数");
        }

        private static void CheckInvalidInputs()
        {
            SnxChartParser parser = new SnxChartParser();
            ExpectException<FormatException>(
                () => parser.Parse("#EVENT BPM 8 140"), "缺少初始 BPM");
            ExpectException<FormatException>(
                () => parser.Parse("#BPM 120\n#EVENT SEC 1 -1"), "非法 SE 预设编号");

            ChartSourceCompiler compiler = new ChartSourceCompiler();
            ExpectException<InvalidOperationException>(
                () => compiler.Compile(new ChartSourceDefinition(
                    480, null, null,
                    new[]
                    {
                        new ChartEventSourcePoint(new ChartPosition(1, 0),
                            ChartEventType.SeChange, numericValue: 2f)
                    })),
                "预设事件不能使用数值参数替代预设编号");
            ExpectException<InvalidOperationException>(
                () => compiler.Compile(parser.Parse(
                    "#BPM 120\n#EVENT BPM 1 140\n#EVENT BPM 1 160")),
                "同位置重复设置 BPM");
            ExpectException<InvalidOperationException>(
                () => compiler.Compile(new ChartSourceDefinition(
                    480, null, new[] { Point(1, 0, 480, ChartLongPointRole.Start) })),
                "长条缺少终点");
            ExpectException<InvalidOperationException>(
                () => compiler.Compile(new ChartSourceDefinition(
                    480,
                    new[] { new ChartTimingPoint(new ChartPosition(1, 0), 120f, 4f, 1f, 1f) },
                    null)),
                "时间线缺少原点");
        }

        private static ChartNoteSourcePoint Point(
            int id, int bar, int tick, ChartLongPointRole role)
        {
            bool isLong = role != ChartLongPointRole.None;
            return new ChartNoteSourcePoint(
                id, new ChartPosition(bar, tick), new ChartLaneRange(2, 2),
                isLong ? ChartNoteCategory.SliderLong : ChartNoteCategory.FixedTap,
                ChartNoteDirection.Default, isLong ? ChartLineType.Linear : ChartLineType.None,
                role, isLong ? 10 : -1, 1f);
        }

        private static void Check(bool condition, string description)
        {
            if (!condition)
            {
                throw new Exception($"检查失败：{description}");
            }
        }

        private static void CheckNear(float actual, float expected, string description)
        {
            Check(Mathf.Abs(actual - expected) < 0.0001f,
                $"{description}，预期 {expected}，实际 {actual}");
        }

        private static void ExpectException<TException>(Action check, string description)
            where TException : Exception
        {
            try
            {
                check();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception($"检查失败：{description}，没有抛出预期异常。");
        }
        
        private static void CheckMixedNotes()
        {
            string text = string.Join("\n", new[]
            {
                "#BPM 120",
        
                "#000 Fixed.Tap.K1:n4000000",
                "#000 Free.Tap.L1:00n20000",
                "#000 Slider.Flick.L5:0000r400",
                "#000 Volume.Flick.L9:000000d2",
        
                "#001 Fixed.Long.K1.a:s40000e4",
                "#001 Free.Long.L1.b:s2m2e2",
                "#001 Slider.Long.L5.c:s4i4e4",
                "#001 Volume.Long+.L9.v:s2o2e2",
                "#001 Guide.Long.L17.g:s4a4e4",
        
                // 故意写在 Note 后面，检查是否使用最终拍数。
                "#EVENT BPB 1 3"
            });
        
            ChartSourceDefinition source =
                new SnxChartParser().Parse(text);
        
            ChartDefinition chart =
                new ChartSourceCompiler().Compile(source);
        
            Check(source.SourcePoints.Count == 18,
                "混合谱面应展开为 18 个原始点");
        
            Check(source.SourcePoints.Select(point => point.Id)
                    .Distinct().Count() == 18,
                "原始点 ID 应在全谱面内唯一");
        
            Check(chart.Notes.Count == 9,
                "混合谱面应编译为 9 个 Note");
        
            Check(chart.Notes.Select(note => note.Category)
                    .Distinct().Count() == 9,
                "应覆盖九种 Note 类别");
        
            Check(chart.Notes.Where(note => !note.HasDuration)
                    .All(note => note.LineType == ChartLineType.None),
                "非 Long Note 的线型应为 None");
        
            ChartNoteDefinition fixedTap = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.FixedTap);
        
            Check(fixedTap.LaneRange.StartLane == 8
                  && fixedTap.LaneRange.EndLane == 11,
                "K1 应映射到内部 Lane 8～11");
        
            ChartNoteDefinition freeTap = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.FreeTap);
        
            CheckNear(freeTap.Time, 0.5f, "00 应保留等分位置");
            Check(freeTap.LaneRange.StartLane == 0
                  && freeTap.LaneRange.EndLane == 1,
                "FreeTap 的位置和宽度");
        
            ChartNoteDefinition fixedLong = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.FixedLong);
        
            CheckNear(fixedLong.Time, 2f, "FixedLong 开始时间");
            CheckNear(fixedLong.EndTime, 3.125f,
                "Note 应使用后声明的三拍小节");
        
            ChartNoteDefinition sliderFlick = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.SliderFlick);
        
            Check(sliderFlick.Direction == ChartNoteDirection.Right,
                "SliderFlick 的 r 应保留为 Right");
        
            ChartNoteDefinition volumeFlick = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.VolumeFlick);
        
            Check(volumeFlick.Direction == ChartNoteDirection.Down,
                "VolumeFlick 的 d 应保留为 Down");
        
            ChartNoteDefinition sliderLong = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.SliderLong);
        
            Check(sliderLong.LongPoints.Count == 3
                  && sliderLong.LongPoints[1].LineType == ChartLineType.EaseIn,
                "SliderLong 应保留中继点和 EaseIn 线型");
            CheckNear(sliderLong.LongPoints[1].Time, 2.5f,
                "三拍小节的三等分中继时间");
        
            ChartNoteDefinition volumeLong = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.VolumeLong);
        
            Check(volumeLong.LongPoints.Count == 3
                  && volumeLong.LongPoints.All(
                      point => point.Direction == ChartNoteDirection.Up)
                  && volumeLong.LongPoints[1].LineType == ChartLineType.EaseOut,
                "VolumeLong 应保留方向和 EaseOut 线型");
        
            ChartNoteDefinition guide = chart.Notes.Single(
                note => note.Category == ChartNoteCategory.Guide);
        
            Check(guide.LongPoints.Count == 3
                  && guide.LongPoints[1].LineType == ChartLineType.RightAngle,
                "Guide 应保留独立类别和路径线型");
        }
        
        private static void CheckCurrentLongRange()
        {
            string text = string.Join("\n", new[]
            {
                "#BPM 120",
                "#001 Free.Long.L1.a:s4",
                "#002 Free.Long.L9.a:e4"
            });

            ChartDefinition chart = new ChartSourceCompiler()
                .Compile(new SnxChartParser().Parse(text));

            LongRangeCheck(chart.Notes.Single());
        }

        private static void LongRangeCheck(
            ChartNoteDefinition definition)
        {
            var note = new SonoriX.Domain.LongNote(definition);
            var system = new SonoriX.Runtime.HoldNoteSystem();

            system.CalculateCurrentLaneEdges(
                note, 2f, out float left, out float right);

            CheckNear(left, -0.5f, "起点左边缘");
            CheckNear(right, 3.5f, "起点右边缘");

            system.CalculateCurrentLaneEdges(
                note, 3f, out left, out right);

            CheckNear(left, 3.5f, "中途左边缘");
            CheckNear(right, 7.5f, "中途右边缘");

            system.CalculateCurrentLaneEdges(
                note, 4f, out left, out right);

            CheckNear(left, 7.5f, "终点左边缘");
            CheckNear(right, 11.5f, "终点右边缘");

            Check(
                system.IsOverlappingCurrentLane(
                    note, 3f, 5.5f, 4f),
                "操作区域应与中途范围重叠");

            Check(
                !system.IsOverlappingCurrentLane(
                    note, 3f, 0f, 1f),
                "停在起点附近应无法维持中途范围");
        }
        
        private static void CheckFreeLongJudgment()
        {
            string text = string.Join("\n", new[]
            {
                "#BPM 120",
                "#001 Free.Long.L1.a:s4",
                "#002 Free.Long.L9.a:e4"
            });

            ChartDefinition chart = new ChartSourceCompiler()
                .Compile(new SnxChartParser().Parse(text));

            var note = new SonoriX.Domain.LongNote(
                chart.Notes.Single());

            var system = new SonoriX.Runtime.JudgmentSystem(
                SonoriX.Runtime.JudgmentRules.Default);

            var notes = new SonoriX.Domain.RuntimeNote[] { note };

            int resultCount = 0;
            bool completed = false;

            // 2～4 秒，每次推进 0.25 秒。
            // 操作区域跟随长条，从中心 1.5 移动到中心 9.5。
            for (int i = 0; i <= 8; i++)
            {
                float time = 2f + i * 0.25f;
                float center = 1.5f + i;

                var phase = i == 0
                    ? SonoriX.Runtime.LiveInputPhase.Began
                    : SonoriX.Runtime.LiveInputPhase.Holded;

                var input = SonoriX.Runtime.LiveInput.FreeLane(
                    time, center, 2f, phase);

                var results = system.Evaluate(
                    time,
                    notes,
                    new[] { input });

                foreach (var result in results)
                {
                    resultCount++;

                    Check(
                        result.Judgment
                        == SonoriX.Runtime.NoteJudgment.JustPerfect,
                        "跟随范围保持输入时应成功");

                    if (result.CompletesNote)
                    {
                        Check(i == 8, "只能在终点完成");
                        completed = true;
                    }
                }
            }

            Check(resultCount == 9,
                "应产生起点一次、持续节点八次");

            Check(completed && !note.IsHolding,
                "终点完成后应清除保持状态");
        }
        
        private static void CheckLongPathSegments()
        {
            var parser = new SnxChartParser();
            var compiler = new ChartSourceCompiler();
            var system = new SonoriX.Runtime.HoldNoteSystem();

            ChartDefinition chart = compiler.Compile(
                parser.Parse(string.Join("\n", new[]
                {
                    "#BPM 120",
                    "#002 Free.Long.L9.path:s4",
                    "#003 Free.Long.L17.path:i8",
                    "#004 Free.Long.L9.path:o4",
                    "#005 Free.Long.L13.path:e4"
                })));

            var note = new SonoriX.Domain.LongNote(
                chart.Notes.Single());

            Check(note.Definition.LongPoints.Count == 4,
                "应保留 Start、两个 Relay 和 End");

            // 起点：内部 Lane 8～11。
            CheckRangeAt(system, note, 4f, 7.5f, 11.5f);

            // 第一段线性变化到一半，宽度应为 6。
            CheckRangeAt(system, note, 5f, 11.5f, 17.5f);

            // 第一个 Relay：内部 Lane 16～23，宽度 8。
            CheckRangeAt(system, note, 6f, 15.5f, 23.5f);

            // EaseIn 的半程进度为 0.75。
            CheckRangeAt(system, note, 7f, 9.5f, 14.5f);

            // 第二个 Relay：回到内部 Lane 8～11。
            CheckRangeAt(system, note, 8f, 7.5f, 11.5f);

            // EaseOut 的半程进度为 0.25。
            CheckRangeAt(system, note, 9f, 8.5f, 12.5f);

            // 终点：内部 Lane 12～15。
            CheckRangeAt(system, note, 10f, 11.5f, 15.5f);
            
            ChartDefinition stepChart = compiler.Compile(
                parser.Parse(string.Join("\n", new[]
                {
                    "#BPM 120",
                    "#002 Free.Long.L9.step:s4",
                    "#003 Free.Long.L9.step:a4",
                    "#004 Free.Long.L17.step:e4"
                })));

            var stepNote = new SonoriX.Domain.LongNote(
                stepChart.Notes.Single());

            CheckRangeAt(system, stepNote, 6f, 7.5f, 11.5f);
            CheckRangeAt(system, stepNote, 7.9f, 7.5f, 11.5f);
            CheckRangeAt(system, stepNote, 8f, 15.5f, 19.5f);
        }

        private static void CheckRangeAt(
            SonoriX.Runtime.HoldNoteSystem system,
            SonoriX.Domain.LongNote note,
            float time,
            float expectedLeft,
            float expectedRight)
        {
            system.CalculateCurrentLaneEdges(
                note,
                time,
                out float left,
                out float right);

            CheckNear(left, expectedLeft, $"{time} 秒左边缘");
            CheckNear(right, expectedRight, $"{time} 秒右边缘");
        }
    }
}
