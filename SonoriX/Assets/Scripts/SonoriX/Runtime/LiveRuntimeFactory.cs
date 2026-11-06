using SonoriX.Domain;
using System.Collections.Generic;
using SonoriX.Domain.Charts;

namespace SonoriX.Runtime
{
    /// <summary>
    /// Runtime 内部组装集中到 Factory
    /// </summary>
    public sealed class LiveRuntimeFactory
    {
        public LiveRuntime Create(
            ChartDefinition chart,
            JudgmentRules rules,
            float appearWindow
        )
        {
            if (chart == null)
            {
                throw new System.ArgumentNullException(nameof(chart));
            }
            
            List<RuntimeNote> notes = new List<RuntimeNote>();

            foreach (ChartNoteDefinition definition in chart.Notes)
            {
                switch (definition.Category)
                {
                    case ChartNoteCategory.FixedTap:
                        notes.Add(new TapNote(definition));
                        break;
                    
                    case ChartNoteCategory.FixedLong:
                        //当前先接入其重点同范围长条
                        if (definition.LongPoints.Count != 2
                            || definition.LongPoints[1].LaneRange.StartLane
                            != definition.LaneRange.StartLane
                            || definition.LongPoints[1].LaneRange.EndLane
                            != definition.LaneRange.EndLane)
                        {
                            throw new System.NotSupportedException(
                                "当前 FixedLong 仅支持两个端点且范围相同的长条。");
                        }
                        notes.Add(new LongNote(definition));
                        break;
                    
                    case ChartNoteCategory.FreeLong:
                    case ChartNoteCategory.SliderLong:
                        notes.Add(new LongNote(definition));
                        break;
                    
                    default:
                        throw new System.NotSupportedException(
                            $"当前 Runtime 尚未接入：{definition.Category}");
                }
            }

            ChartData chartData = new ChartData(chart, notes);
            
            return new LiveRuntime(
                new ChartPlaybackSystem(chartData, rules, appearWindow),
                new JudgmentSystem(rules),
                new ScoreSystem());
        }
    }
}