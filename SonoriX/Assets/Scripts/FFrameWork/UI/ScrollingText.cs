using TMPro;
using UnityEngine;

namespace FFrameWork
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    // 表示幅を超える TMP_Text を横スクロール表示する
    public class ScrollingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform viewport; 

        [Header("Scroll Settings")]
        [SerializeField] private float speed = 40f;          
        [SerializeField] private float startDelay = 1.0f;    
        [SerializeField] private float endDelay = 1.0f;      
        [SerializeField] private float spacing = 30f;        

        // スクロール対象の TextMeshPro
        private TMP_Text tmpText;
        // スクロール対象の RectTransform
        private RectTransform textRect;
        // 初期アンカー位置
        private Vector2 initialPos;

        // 端まで流す距離
        private float scrollDistance;
        // 現在スクロール済みの距離
        private float currentDistance;
        // 開始前または終端での待ち時間
        private float delayTimer;
        // 現在スクロールが必要か
        private bool isScrolling;
        // 終端待ち状態か
        private bool waitingAtEnd;

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
            textRect = transform as RectTransform;
            initialPos = textRect.anchoredPosition;

            if (viewport == null && transform.parent != null)
            {
                viewport = transform.parent as RectTransform;
            }
            
            tmpText.textWrappingMode = TextWrappingModes.NoWrap;
            tmpText.overflowMode = TextOverflowModes.Overflow;
        }

        private void OnEnable()
        {
            ResetScroll();
        }

        private void OnDisable()
        {
            ResetPosition();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                CheckAndResetScroll();
            }
        }

        private void Update()
        {
            if (!isScrolling) return;
            
            if (delayTimer > 0f)
            {
                delayTimer -= Time.deltaTime;
                return;
            }

            if (waitingAtEnd)
            {
                ResetPosition();
                delayTimer = startDelay;
                waitingAtEnd = false;
                return;
            }
            
            currentDistance += speed * Time.deltaTime;

            if (currentDistance >= scrollDistance)
            {
                currentDistance = scrollDistance;
                textRect.anchoredPosition = new Vector2(initialPos.x - currentDistance, initialPos.y);
                delayTimer = endDelay;
                waitingAtEnd = true;
            }
            else
            {
                textRect.anchoredPosition = new Vector2(initialPos.x - currentDistance, initialPos.y);
            }
        }
        
        // テキスト幅を測定し、必要ならスクロール状態を初期化する
        public void CheckAndResetScroll()
        {
            if (tmpText == null || viewport == null) return;
            
            tmpText.GetRenderedValues(false);

            float viewportWidth = viewport.rect.width;
            float textWidth = tmpText.preferredWidth;

            textRect.sizeDelta = new Vector2(textWidth, textRect.sizeDelta.y);
            
            if (textWidth > viewportWidth)
            {
                scrollDistance = (textWidth - viewportWidth) + spacing;
                ResetPosition();
                delayTimer = startDelay;
                waitingAtEnd = false;
                isScrolling = true;
            }
            else
            {
                scrollDistance = 0f;
                isScrolling = false;
                ResetPosition();
            }
        }

        // テキスト位置を初期位置へ戻す
        public void ResetPosition()
        {
            currentDistance = 0f;
            if (textRect != null)
            {
                textRect.anchoredPosition = initialPos;
            }
        }

        // スクロール要否を再判定して初期化する
        public void ResetScroll()
        {
            CheckAndResetScroll();
        }
    }
}
