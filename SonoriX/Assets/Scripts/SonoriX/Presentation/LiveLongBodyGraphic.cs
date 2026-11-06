using SonoriX.Domain;
using SonoriX.Domain.Charts;
using SonoriX.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SonoriX.Presentation
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class LiveLongBodyGraphic : MaskableGraphic
    {
        
        
        [SerializeField, Range(2, 32)] private int samplesPerSegment = 12;
        
        //调用无状态范围计算
        private readonly HoldNoteSystem rangeCalculator = new HoldNoteSystem();

        private LongNote note;
        private LiveNoteLayout layout;
        private float musicTime;
        private Vector2 origin;

        public void SetData(
            LongNote note,
            LiveNoteLayout layout,
            float musicTime,
            Vector2 origin)
        {
            this.note = note;
            this.layout = layout;
            this.musicTime = musicTime;
            this.origin = origin;
            
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (note == null
                || layout == null
                || musicTime >= note.EndTime)
            {
                return;
            }

            var points = note.Definition.LongPoints;

            for (int i = 1; i < points.Count; i++)
            {
                ChartLongPointDefinition start = points[i - 1];
                ChartLongPointDefinition end = points[i];
                
                //已经过判定线的部分裁掉
                float fromTime = Mathf.Max(
                    start.Time, musicTime);

                float toTime = end.Time;

                if (fromTime >= toTime)
                {
                    continue;
                }

                GetSection(start, fromTime,
                    out Vector2 previousLeft,
                    out Vector2 previousRight);

                for (int sample = 1;sample <= samplesPerSegment; sample ++)
                {
                    float progress = (float)sample / samplesPerSegment;
                    
                    float sampleTime = Mathf.Lerp(fromTime, toTime, progress);
                    
                    GetSection(start,sampleTime,out Vector2 nextLeft,out Vector2 nextRight);
                    
                    AddSection(vh,previousLeft,previousRight,nextLeft,nextRight);
                    
                    previousLeft = nextLeft;
                    previousRight = nextRight;
                }
            }
        }

        private void GetSection(ChartLongPointDefinition segmentStart,
            float sampleTime,
            out Vector2 leftPosition,
            out Vector2 rightPosition)
        {
            float left;
            float right;

            if (segmentStart.LineType == ChartLineType.RightAngle)
            {
                left = segmentStart.LaneRange.StartLane - 0.5f;
                right = segmentStart.LaneRange.EndLane + 0.5f;
            }
            else
            {
                rangeCalculator.CalculateCurrentLaneEdges(
                    note,
                    sampleTime,
                    out left,
                    out right);
            }
            
            //layout返回相对于Notes父对象的位置
            //Graphic位于NoteView内，因此减去根对象位置
            leftPosition = layout.CalculatePathPosition(
                left, sampleTime,musicTime)-origin;
            
            rightPosition = layout.CalculatePathPosition(
                right, sampleTime,musicTime)-origin;
        }

        private void AddSection(
            VertexHelper vh,
            Vector2 bottomLeft,
            Vector2 bottomRight,
            Vector2 topLeft,
            Vector2 topRight)
        {
            int first = vh.currentVertCount;

            vh.AddVert(bottomLeft, color, new Vector2(0f, 0f));
            vh.AddVert(bottomRight, color, new Vector2(1f, 0f));
            vh.AddVert(topRight, color, new Vector2(1f, 1f));
            vh.AddVert(topLeft, color, new Vector2(0f, 1f));

            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 2, first + 3, first);
        }
    }
}