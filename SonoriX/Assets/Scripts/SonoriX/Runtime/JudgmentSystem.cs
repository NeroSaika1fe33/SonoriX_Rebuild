using System.Collections.Generic;
using SonoriX.Domain;
using SonoriX.Domain.Charts;
using LongNote = SonoriX.Domain.LongNote;

namespace SonoriX.Runtime
{
    //Note判定系统
    public class JudgmentSystem
    {
        private readonly struct JudgmentCandidate
        {
            public int InputIndex { get; }
            public float JudgeTime { get; }
            public float TimeDistance { get; }
            public  bool HasValue => InputIndex >= 0;

            public JudgmentCandidate(int inputIndex, float judgeTime, float timeDistance)
            {
                InputIndex = inputIndex;
                JudgeTime = judgeTime;
                TimeDistance = timeDistance;
            }
        
            public static JudgmentCandidate None => new JudgmentCandidate(-1, 0f, 0f);
        }
        
        private JudgmentRules rules { get; }
        
        private readonly HoldNoteSystem holdNoteSystem =
            new HoldNoteSystem();
        
        public JudgmentSystem(JudgmentRules rules)
        {
            this.rules = rules;
        }
        
        //对比输入和谱面时间序列数据生产判定结果
        public IReadOnlyList<JudgmentResult> Evaluate(
            float musicTime,
            IReadOnlyList<RuntimeNote> notes,
            IReadOnlyList<LiveInput> inputs)
        {
            List<JudgmentResult> results =
                new List<JudgmentResult>();
        
            HashSet<int> usedInputIndexes =
                new HashSet<int>();
        
            foreach (RuntimeNote note in notes)
            {
                LongNote longNote = note as LongNote;
                
                //Slider 起点由区域重叠触发，不消费按键事件
                //同一份区域快照可以支持多条Slider
                if(longNote!=null 
                   && longNote.Category == ChartNoteCategory.SliderLong
                   && !longNote.StartJudged
                   && musicTime < longNote.EndTime
                   && holdNoteSystem.CanHoldSlider(
                       longNote,musicTime,inputs))
                {
                    longNote.MarkStartJudged();

                    results.Add(new JudgmentResult(
                        longNote,
                        null,
                        NoteJudgment.JustPerfect,
                        NoteJudgmentDescriptor.None,
                        ChartLongPointRole.Start));
                }
        
                bool needsStartJudgment =
                    longNote == null || !longNote.StartJudged;
        
                if (needsStartJudgment)
                {
                    JudgmentCandidate match =
                        FindFirstInputInWindow(
                            note, inputs, usedInputIndexes);
        
                    if (match.HasValue)
                    {
                        LiveInput input = inputs[match.InputIndex];
                        usedInputIndexes.Add(match.InputIndex);
        
                        NoteJudgment judgment =
                            Judge(match.TimeDistance, match.JudgeTime);
        
                        NoteJudgmentDescriptor descriptor =
                            judgment == NoteJudgment.JustPerfect
                                ? NoteJudgmentDescriptor.None
                                : JudgeDescribe(match.JudgeTime);
        
                        if (longNote != null)
                        {
                            longNote.MarkStartJudged();
                        }
        
                        results.Add(new JudgmentResult(
                            note,
                            input,
                            judgment,
                            descriptor,
                            longNote != null
                                ? ChartLongPointRole.Start
                                : ChartLongPointRole.None));
                    }
                }
        
                if (longNote == null)
                {
                    continue;
                }
        
                // 计算当前范围
                // 更新是否保持
                // 结算到期持续节点
                // Presenter读取当前范围显示
                if (!longNote.StartJudged
                    && musicTime >= longNote.EndTime)
                {
                    longNote.MarkStartJudged();
        
                    results.Add(new JudgmentResult(
                        longNote,
                        null,
                        NoteJudgment.Miss,
                        NoteJudgmentDescriptor.Late,
                        ChartLongPointRole.Start));
                }
        
                // 当前 Runtime 仍只接入 FixedLong。
                // 先更新保持状态，再检查持续节点，包括终点。
                holdNoteSystem.UpdateLongHold(
                    longNote,
                    musicTime,
                    inputs);
        
                holdNoteSystem.EvaluateHoldPoints(
                    longNote,
                    musicTime,
                    results);
            }
        
            return results;
        }

        //找到最早进入可判定时间区间的note匹配输入
        private JudgmentCandidate FindFirstInputInWindow(
            RuntimeNote note,
            IReadOnlyList<LiveInput> inputs,
            HashSet<int> usedInputIndexes
        )
        {
            JudgmentCandidate firstMatch = JudgmentCandidate.None ;
            float firstInputTime = float.MaxValue;

            for (int i = 0; i < inputs.Count; i++)
            {
                if (usedInputIndexes.Contains(i))
                {
                    continue;
                }

                LiveInput input = inputs[i];

                if (!CanUseInputForNote(note, input))
                {
                    continue;
                }
                
                //判定时间
                float judgeTime = input.Time - note.Time;
                //判定时间绝对值
                float timeDistance = judgeTime < 0 ? -judgeTime : judgeTime;

                if (timeDistance > rules.MaxWindow)
                {
                    continue;
                }

                if (input.Time < firstInputTime)
                {
                    firstInputTime = input.Time;
                    firstMatch = new JudgmentCandidate(i,judgeTime, timeDistance);
                }
            }

            return firstMatch;
        }

        private NoteJudgment Judge(float  timeDistance , float  judgeTime)
        {
            //判定
            if (timeDistance <= rules.JustPerfectWindow)  
            {  
                return NoteJudgment.JustPerfect;  
            }else 
            if (timeDistance <= rules.PerfectWindow && timeDistance > rules.JustPerfectWindow)
            {
                return NoteJudgment.Perfect;
            }else 
            if (timeDistance <= rules.GreatWindow && timeDistance > rules.PerfectWindow)  
            {  
                return NoteJudgment.Great;
            }  else 
            if (timeDistance <= rules.GoodWindow && timeDistance > rules.GreatWindow)
            {
                return NoteJudgment.Good;
            } else 
            if (timeDistance <= rules.BadWindow && timeDistance > rules.GoodWindow)
            {
                return NoteJudgment.Bad;
            }
            return NoteJudgment.None;
        }
        
        private NoteJudgmentDescriptor JudgeDescribe(float judgeTime)
        {
            return judgeTime < 0f? NoteJudgmentDescriptor.Fast : NoteJudgmentDescriptor.Late;
        }

        //是否可进行FixedLaneNote的判定
        private bool CanUseInputForNote(
            RuntimeNote note,
            LiveInput input)
        {
            //起点暂时沿用按下瞬间匹配的规则
            if (input.Phase != LiveInputPhase.Began)
            {
                return false;
            }

            if (note.Category == ChartNoteCategory.FreeLong
                && note is LongNote freeLong)
            {
                return input.Type == LiveInputType.FreeLane
                       && !freeLong.StartJudged
                       && holdNoteSystem.IsOverlappingCurrentLane(
                           freeLong,
                           input.Time,
                           input.CenterLane,
                           input.Width);
            }

            if (input.Type != LiveInputType.FixedLane
                || !ChartLaneLayout.ContainsFixedKey(
                    note.LaneRange, input.LaneID))
            {
                return false;
            }

            if (note.Category == ChartNoteCategory.FixedLong
                && note is LongNote fixedLong)
            {
                return !fixedLong.StartJudged;
            }

            return note.Category == ChartNoteCategory.FixedTap;
        }
        
    }
}