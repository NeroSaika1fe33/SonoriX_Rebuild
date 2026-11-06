using SonoriX.Domain;
using UnityEngine;
using SonoriX.Domain.Charts;

namespace SonoriX.Presentation
{
    [System.Serializable]
    public sealed class LiveNoteLayout
    {
        [SerializeField] private float laneWidth = 120f;
        [SerializeField] private float judgeLineY = -200f;
        [SerializeField] private float timeScale = 300f;
        
        //计算note位置
        public Vector2 CalculateNotePosition(RuntimeNote note, float musicTime)
        {
            float x = CalculateNoteX(note);
            float y = judgeLineY +(note.Time -  musicTime)*timeScale;
            
            return new Vector2(x, y);
        }

        //计算对应note的判定线位置
        public Vector2 CalculateJudgeLinePosition(RuntimeNote note)
        {
            float x = CalculateNoteX(note);
            return new Vector2(x,judgeLineY);
        }

        private float CalculateNoteX(RuntimeNote note)
        {
            float center = (note.LaneRange.StartLane + note.LaneRange.EndLane)*0.5f;

            ChartLaneRange firstKey = ChartLaneLayout.GetFixedKeyRange(0);
            
            float origin = (firstKey.StartLane + firstKey.EndLane)*0.5f;
            
            float pixelsPerLane = laneWidth / ChartLaneLayout.FixedKeyWidth;
            
            return (center - origin) * pixelsPerLane;
        }
        
        //计算当前测试布局中的长条可见端点
        //适用于当前Runtime支持的起终点同范围直线长条
        public void CalculateLongPosition(
            LongNote note,
            float musicTime,
            out Vector2 startPosition,
            out Vector2 endPosition)
        {
            float x = CalculateNoteX(note);
            
            //起点到达判定线后，条沈从判定先开始缩短
            float remainingStartTime = Mathf.Max(
                0f,
                note.Time - musicTime);
            
            float remainingEndTime = Mathf.Max(
                0f,
                note.EndTime - musicTime);

            startPosition = new Vector2(
                x,
                judgeLineY + remainingStartTime * timeScale);
            
            endPosition = new Vector2(
                x,
                judgeLineY + remainingEndTime * timeScale);
        }
        
        //将内部Lane宽度换算为当前测试布局的像素宽度
        public float CalculateNoteWidth(RuntimeNote note)
        {
            int laneCount =
                note.LaneRange.EndLane
                - note.LaneRange.StartLane + 1;
            
            float pixelsPerLane = 
                laneWidth / ChartLaneLayout.FixedKeyWidth;
            
            return laneCount * pixelsPerLane;
        }

        public Vector2 CalculateLaneJudgePosition(float centerLane)
        {
            ChartLaneRange firstKey =
                ChartLaneLayout.GetFixedKeyRange(0);

            float origin =
                (firstKey.StartLane + firstKey.EndLane)*0.5f;
            
            float pixelsPerLane = 
                laneWidth / ChartLaneLayout.FixedKeyWidth;
            
            return new Vector2(
                (centerLane - origin)*pixelsPerLane,
                judgeLineY);
        }

        public float CalculateLaneWidth(float widthInLanes)
        {
            return widthInLanes * laneWidth / ChartLaneLayout.FixedKeyWidth;
        }

        public Vector2 CalculateLongHeadPosition(
            LongNote note,
            float musicTime)
        {
            float centerLane = (note.CurrentLeftLane + note.CurrentRightLane)*0.5f;
            
            Vector2 position = CalculateLaneJudgePosition(centerLane);
            
            //起点到达判定线前向下移动
            //到达之后留在判定线上，继续跟随横向范围
            position.y += Mathf.Max(0f, note.Time - musicTime) * timeScale;
            
            return position;
        }

        public Vector2 CalculatePathPosition(
            float lane,
            float sampleTime,
            float musicTime)
        {
            Vector2 position = CalculateLaneJudgePosition(lane);
            
            position.y +=
                (sampleTime - musicTime)*timeScale;
            
            return position;
        }
    }
}