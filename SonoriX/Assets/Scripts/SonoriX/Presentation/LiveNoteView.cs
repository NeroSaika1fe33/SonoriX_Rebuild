using UnityEngine;
using UnityEngine.UI;
using SonoriX.Domain;

namespace SonoriX.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class LiveNoteView : MonoBehaviour
    {
        [Header("显示部件")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform head;
        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform tail;
        [SerializeField] private Image bodyImage;

        [Header("长条状态颜色")]
        [SerializeField] private Color releasedColor = Color.gray;
        [SerializeField] private Color holdingColor = Color.white;
        
        [SerializeField,Min(1f)] private float holdTargetHeight = 16f;
        
        [SerializeField] private LiveLongBodyGraphic longBody;

        // Tap：只显示起点图像。
        public void Show(Vector2 position, float width)
        {
            if (longBody != null)
            {
                longBody.gameObject.SetActive(false);
            }
            
            SetRootPosition(position);

            SetPartVisible(head, true);
            SetPartVisible(body, false);
            SetPartVisible(tail, false);

            UpdateCap(head, Vector2.zero, width);

            gameObject.SetActive(true);
        }

        // Long：显示起点、连接条身和尾端。
        public void ShowLong(
            Vector2 startPosition,
            Vector2 endPosition,
            float width,
            bool showHead,
            bool isHolding)
        {
            if (longBody != null)
            {
                longBody.gameObject.SetActive(false);
            }
            
            SetRootPosition(startPosition);

            Vector2 offset = endPosition - startPosition;

            SetPartVisible(head, showHead);
            SetPartVisible(body, offset.sqrMagnitude > 0f);
            SetPartVisible(tail, true);

            UpdateCap(head, Vector2.zero, width);
            UpdateCap(tail, offset, width);

            if (body != null)
            {
                body.anchoredPosition = offset * 0.5f;

                // 当前只接入起终点同范围的竖直长条。
                body.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, width);

                body.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    Mathf.Abs(offset.y));
            }

            if (bodyImage != null)
            {
                bodyImage.color = isHolding
                    ? holdingColor
                    : releasedColor;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetRootPosition(Vector2 position)
        {
            // 对象池中的对象可能初始未激活，
            // 因此不依赖 Awake 一定已经执行。
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            rectTransform.anchoredPosition = position;
        }

        private static void UpdateCap(
            RectTransform part,
            Vector2 position,
            float width)
        {
            if (part == null)
            {
                return;
            }

            part.anchoredPosition = position;

            // 高度沿用 Inspector 中配置的值。
            part.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, width);
        }

        private static void SetPartVisible(
            RectTransform part,
            bool visible)
        {
            if (part != null)
            {
                part.gameObject.SetActive(visible);
            }
        }
        
        //用现有BodyImage显示当前保持目标
        //暂时显示判定范围 不会只完整路径条身
        public void ShowHoldTarget(
            Vector2 position,
            float width,
            bool isHolding)
        {
            SetRootPosition(position);
            
            SetPartVisible(head, false);
            SetPartVisible(tail, false);
            SetPartVisible(body, true);
            
            //复用对象池时的检查
            if(body != null)
            {
                body.anchoredPosition = Vector2.zero;
                body.localRotation = Quaternion.identity;

                body.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    width);

                body.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    holdTargetHeight);
            }

            if (bodyImage != null)
            {
                bodyImage.color = isHolding
                    ? holdingColor
                    : releasedColor;
            }
            
            gameObject.SetActive(true);
        }

        public void ShowFreeLong(
            LongNote note,
            LiveNoteLayout layout,
            float musicTime)
        {
            Vector2 position = layout.CalculateLongHeadPosition(note,musicTime);

            float width = layout.CalculateLaneWidth(
                note.CurrentRightLane - note.CurrentLeftLane);
            
            //继续使用之前的判定头
            ShowHoldTarget(position,width,note.IsHolding);

            if (longBody != null)
            {
                longBody.gameObject.SetActive(true);
                
                Color tint = note.IsHolding ? holdingColor : releasedColor;

                tint.a *= 0.45f;
                longBody.color = tint;
                
                longBody.SetData(
                    note,layout,musicTime,position);
            }

            var points = note.Definition.LongPoints;

            if (points.Count > 0)
            {
                var end = points[points.Count - 1];
                
                float endCenter = (end.LaneRange.StartLane + end.LaneRange.EndLane) * 0.5f;

                float endWidth = layout.CalculateLaneWidth(end.LaneRange.EndLane - end.LaneRange.StartLane + 1);
                
                Vector2 endPosition = layout.CalculatePathPosition(
                    endCenter,end.Time,musicTime);
                
                SetPartVisible(tail, true);
                
                UpdateCap(tail,endPosition-position, endWidth);
            }
        }
    }
}