using System;
using System.Collections.Generic;
using SonoriX.Domain;
using SonoriX.Domain.Charts;
using Unity.VisualScripting;

namespace SonoriX.Runtime
{
    // Fixed 本帧的输入快照，供起终点判定读取；不代表整条完成状态。
    public readonly struct FixedHoldState
    {
        private readonly int heldKeyMask;
        
        public bool WasHolding { get; }
        public bool BeganThisFrame { get; }
        
        public bool IsHolding => heldKeyMask != 0;

        public FixedHoldState(
            bool wasHolding,
            bool beganThisFrame,
            int heldKeyMask)
        {
            WasHolding = wasHolding;
            BeganThisFrame = beganThisFrame;
            this.heldKeyMask = heldKeyMask;
        }

        public bool IsKeyHeld(int keyIndex)
        {
            if (keyIndex < 0
                || keyIndex >= ChartLaneLayout.FixedKeyCount)
            {
                return false;
            }
            
            return (heldKeyMask & (1 << keyIndex)) != 0;
        }
    }
    
    public sealed class HoldNoteSystem
    {
        // 输入读取器每帧为仍按住的固定键提供 Holded。
        public FixedHoldState UpdateFixedHold(
            LongNote note,
            IReadOnlyList<LiveInput> inputs)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            if (note.Category != ChartNoteCategory.FixedLong)
            {
                throw new ArgumentException(
                    "当前方法只处理 FixedLong。", nameof(note));
            }

            bool wasHolding = note.IsHolding;
            bool beganThisFrame = false;
            int heldKeyMask = 0;
            float lastInputTime = float.NegativeInfinity;

            foreach (LiveInput input in inputs)
            {
                if (input.Type != LiveInputType.FixedLane
                    || !ChartLaneLayout.ContainsFixedKey(
                        note.LaneRange, input.LaneID))
                {
                    continue;
                }

                int keyBit = 1 << input.LaneID;

                switch (input.Phase)
                {
                    case LiveInputPhase.Began:
                        beganThisFrame = true;
                        heldKeyMask |= keyBit;
                        lastInputTime = Math.Max(lastInputTime, input.Time);
                        break;

                    case LiveInputPhase.Holded:
                        heldKeyMask |= keyBit;
                        lastInputTime = Math.Max(lastInputTime, input.Time);
                        break;

                    case LiveInputPhase.Ended:
                        heldKeyMask &= ~keyBit;
                        break;
                }
            }

            FixedHoldState state = new FixedHoldState(
                wasHolding, beganThisFrame, heldKeyMask);

            if (state.IsHolding)
            {
                // Fixed 无输入间隔容许，使用真实输入的音乐时间。
                UpdateHold(note, lastInputTime, true, true, 0f);
            }
            else
            {
                note.SetHolding(false);
            }

            return state;
        }

        // 所有 Long 共用持续状态规则，范围和方向条件由本系统的玩法判断提供。
        // 调用者只传入尚未完成且已进入判定流程的长条。
        public bool UpdateHold(
            LongNote note,
            float musicTime,
            bool canMaintain,
            bool hasValidInput,
            float graceSeconds)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            if (float.IsNaN(musicTime) || float.IsInfinity(musicTime))
            {
                throw new ArgumentOutOfRangeException(nameof(musicTime));
            }

            if (graceSeconds < 0f
                || float.IsNaN(graceSeconds)
                || float.IsInfinity(graceSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(graceSeconds));
            }
            
            //起点尚未结算时，不计入持续阶段
            //StartJudged包含起点Miss，因此仍允许后续恢复
            if (!note.StartJudged || !canMaintain)
            {
                note.SetHolding(false);
                return false;
            }

            if (hasValidInput)
            {
                note.RegisterHoldInput(musicTime);
                return true;
            }

            float elapsed =
                musicTime - note.LastValidHoldInputTime;

            bool withinGrace =
                note.IsHolding
                && graceSeconds > 0f
                && elapsed >= 0f
                && elapsed <= graceSeconds;
            
            //只有真实有效输入才刷新时间
            //容许期间只保留状态
            note.SetHolding(withinGrace);
            
            return withinGrace;
        }
        
        public bool TryCompleteHoldPoint(
            LongNote note,
            int pointIndex,
            NoteJudgment judgment,
            ICollection<JudgmentResult> results)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            // 沿用旧持续节点的成功 / Miss 两种结果。
            if (judgment != NoteJudgment.JustPerfect
                && judgment != NoteJudgment.Miss)
            {
                throw new ArgumentException(
                    "持续节点只接受 JustPerfect 或 Miss。",
                    nameof(judgment));
            }

            if (!note.TryCompleteHoldPoint(pointIndex))
            {
                return false;
            }

            //终点的 End 标记现在只是通知“整条完成”，不表示松手评级
            bool isLastPoint =
                pointIndex == note.Definition.HoldTimes.Count - 1;

            results.Add(new JudgmentResult(
                note,
                null,
                judgment,
                NoteJudgmentDescriptor.None,
                pointRole: isLastPoint
                    ? ChartLongPointRole.End
                    : ChartLongPointRole.None,
                holdPointIndex: pointIndex));

            if (isLastPoint)
            {
                note.SetHolding(false);
            }


            return true;
        }

        //先更新本帧是否保持，再处理到期节点
        public void EvaluateHoldPoints(
            LongNote note,
            float musicTime,
            ICollection<JudgmentResult> results)
        {
            while (note.TryGetNextHoldTimes(out float pointTime)
                   && pointTime <= musicTime)
            {
                int pointIndex = note.NextHoldIndex;
                
                NoteJudgment judgment = note.IsHolding
                    ? NoteJudgment.JustPerfect
                    : NoteJudgment.Miss;

                if (!TryCompleteHoldPoint(
                        note,
                        pointIndex, 
                        judgment, 
                        results))
                {
                    break;
                }
            }
        }

        public void CalculateCurrentLaneEdges(
            LongNote note,
            float musicTime,
            out float left,
            out float right)
        {
            IReadOnlyList<ChartLongPointDefinition> points =
                note.Definition.LongPoints;

            if (points.Count == 0)
            {
                left = note.LaneRange.StartLane - 0.5f;
                right = note.LaneRange.EndLane + 0.5f;
                return;
            }
            
            ChartLongPointDefinition first = points[0];

            if (musicTime < first.Time)
            {
                left = first.LaneRange.StartLane - 0.5f;
                right = first.LaneRange.EndLane + 0.5f;
                return;
            }

            for (int i = 1; i < points.Count; i++)
            {
                ChartLongPointDefinition next = points[i];
                
                //到达下一个节点后，使用下一段路径
                if (musicTime >= next.Time)
                {
                    continue;
                }
                
                ChartLongPointDefinition previous = points[i - 1];
                
                float duartion = next.Time - previous.Time;

                float progress = duartion > 0f
                    ? (musicTime - previous.Time) / duartion
                    : 1f;
                
                progress = Math.Max(0f,Math.Min(1f,progress));

                progress = CalculateLaneProgress(
                    progress,
                    previous.LineType,
                    note.Category);

                float previousLeft =
                    previous.LaneRange.StartLane - 0.5f;
                
                float previousRight =
                    previous.LaneRange.EndLane + 0.5f;
                
                float nextLeft =
                    next.LaneRange.StartLane - 0.5f;
                
                float nextRight = 
                    next.LaneRange.EndLane + 0.5f;

                left = previousLeft + (nextLeft - previousLeft) * progress;
                
                right = previousRight + (nextRight - previousRight) * progress;
                
                return;
            }
            
            ChartLongPointDefinition last = points[points.Count - 1];
            
            left = last.LaneRange.StartLane - 0.5f;
            right = last.LaneRange.EndLane + 0.5f;
        }
        
        private static float CalculateLaneProgress(
            float progress,
            ChartLineType lineType,
            ChartNoteCategory category)
        {
            if (lineType == ChartLineType.RightAngle)
            {
                // 当前段保持原位置，到下一节点时切换。
                return 0f;
            }

            bool useCubic =
                category == ChartNoteCategory.SliderLong
                || category == ChartNoteCategory.VolumeLong;

            switch (lineType)
            {
                case ChartLineType.EaseIn:
                {
                    float remaining = 1f - progress;

                    return useCubic
                        ? 1f - remaining * remaining * remaining
                        : 1f - remaining * remaining;
                }

                case ChartLineType.EaseOut:
                    return useCubic
                        ? progress * progress * progress
                        : progress * progress;

                default:
                    return progress;
            }
        }

        public bool IsOverlappingCurrentLane(
            LongNote note,
            float musicTime,
            float inputCenterLane,
            float inputWidth)
        {
            if (inputWidth <= 0f
                || float.IsNaN(inputWidth)
                || float.IsInfinity(inputWidth)
                || float.IsNaN(inputCenterLane)
                || float.IsInfinity(inputCenterLane))
            {
                return false;
            }
            
            CalculateCurrentLaneEdges(
                note,
                musicTime,
                out float noteLeft,
                out float noteRight);
            
            float halfWidth = inputWidth * 0.5f;
            float inputLeft = inputCenterLane - halfWidth;
            float inputRight = inputCenterLane + halfWidth;
            
            return inputLeft <= noteRight
                   && inputRight >= noteLeft;
        }

        public bool UpdateFreeHold(
            LongNote note,
            float musicTime,
            IReadOnlyList<LiveInput> inputs)
        {
            //当前只有一个Free操作区域
            //如果同帧有多条记录，一最后一条最为本帧最终状态
            LiveInput? currentInput = null;

            foreach (LiveInput input in inputs)
            {
                if (input.Type == LiveInputType.FreeLane)
                {
                    currentInput = input;
                }
            }

            bool valid = false;

            if (currentInput.HasValue)
            {
                LiveInput input = currentInput.Value;
                
                valid = input.IsPressed && IsOverlappingCurrentLane(
                    note,
                    musicTime,
                    input.CenterLane,
                    input.Width);
            }

            return UpdateHold(
                note,
                musicTime,
                canMaintain: valid,
                hasValidInput: valid,
                graceSeconds: 0f);
        }

        public void UpdateLongHold(
            LongNote note,
            float musicTime,
            IReadOnlyList<LiveInput> inputs)
        {
            CalculateCurrentLaneEdges(
                note,
                musicTime,
                out float left,
                out float right);
            
            note.UpdateCurrentLaneEdges(left, right);
            
            switch (note.Category)
            {
                case ChartNoteCategory.FixedLong:
                    UpdateFixedHold(note, inputs);
                    break;
                
                case ChartNoteCategory.FreeLong:
                    UpdateFreeHold(note,musicTime,inputs);
                    break;
                
                case ChartNoteCategory.SliderLong:
                    UpdateSliderHold(note, musicTime, inputs);
                    break;
                
                default:
                    throw new NotSupportedException(
                        $"当前尚未接入持续输入：{note.Category}");
            }
        }

        public bool CanHoldSlider(
            LongNote note,
            float musicTime,
            IReadOnlyList<LiveInput> inputs)
        {
            if (musicTime < note.Time)
            {
                return false;
            }
            
            LiveInput?currentInput = null;
            foreach (LiveInput input in inputs)
            {
                if (input.Type == LiveInputType.Arrow)
                {
                    currentInput = input;
                }
            }

            if (!currentInput.HasValue)
            {
                return false;
            }
            
            LiveInput inputFrame = currentInput.Value;
            
            return IsOverlappingCurrentLane(
                note,
                musicTime,
                inputFrame.CenterLane,
                inputFrame.Width);
        }

        public bool UpdateSliderHold(
            LongNote note,
            float musicTime,
            IReadOnlyList<LiveInput> inputs)
        {
            bool valid = CanHoldSlider(
                note,musicTime,inputs);


            return UpdateHold(
                note,
                musicTime,
                canMaintain: valid,
                hasValidInput: valid,
                graceSeconds: 0f);
        }
    }
}