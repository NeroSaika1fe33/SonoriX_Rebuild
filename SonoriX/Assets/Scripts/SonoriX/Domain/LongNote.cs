using SonoriX.Domain.Charts;

namespace SonoriX.Domain
{
    public sealed class LongNote : RuntimeNote
    {
        //每帧更新的该Note的判定范围
        public float CurrentLeftLane { get; private set; }
        public float CurrentRightLane { get; private set; }
        
        public float EndTime => Definition.EndTime;
        
        //本局下一个尚未结算的持续节点
        public int NextHoldIndex { get; private set; }

        public bool HasPendingHoldPoint =>
            NextHoldIndex < Definition.HoldTimes.Count;
        
        //记录最后一次有效持续输入
        public float LastValidHoldInputTime { get; private set; }
            = float.NegativeInfinity;
        
        //起点是否已经结算包括起点Miss
        public bool StartJudged { get; private set; }
        //当前是否满足该长条的持续输入条件
        public bool IsHolding { get; private set; }

        public LongNote(ChartNoteDefinition definition) : base(definition)
        {
            switch (definition.Category)
            {
                case ChartNoteCategory.FixedLong:
                case ChartNoteCategory.FreeLong:
                case ChartNoteCategory.SliderLong:
                case ChartNoteCategory.VolumeLong:
                    break;
                
                default:
                    throw new System.ArgumentException(
                        "LongNote 需要可判定的长条分类。",
                        nameof(definition));
            }

            UpdateCurrentLaneEdges(
                definition.LaneRange.StartLane - 0.5f,
                definition.LaneRange.EndLane + 0.5f);
        }

        public void UpdateCurrentLaneEdges(float left, float right)
        {
            CurrentLeftLane = left;
            CurrentRightLane = right;
        }
        
        public void MarkStartJudged()
        {
            StartJudged = true;
        }
        
        public void SetHolding(bool isHolding)
        {
            IsHolding = isHolding;

            if (!isHolding)
            {
                LastValidHoldInputTime = float.NegativeInfinity;
            }
        }
        
        public void RegisterHoldInput(float musicTime)
        {
            LastValidHoldInputTime = musicTime;
            IsHolding = true;
        }

        public bool TryGetNextHoldTimes(out float time)
        {
            if (!HasPendingHoldPoint)
            {
                time = 0f;
                return false;
            }
            
            time = Definition.HoldTimes[NextHoldIndex];
            return true;
        }
        
        //只允许按顺序结算，重复提交旧节点不会再次推进
        public bool TryCompleteHoldPoint(int pointIndex)
        {
            if (!HasPendingHoldPoint
                || pointIndex != NextHoldIndex)
            {
                return false;
            }

            NextHoldIndex++;
            return true;
        }
    }
}