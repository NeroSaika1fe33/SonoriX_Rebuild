using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 操作説明画像をページ送りで表示するパネル
    public class OperationGuidePanel : MonoBehaviour
    {
        // 遷移先を制御する Controller
        [SerializeField] private LivePanelFlowController flowController;
        [SerializeField] private Image slideImage;
        [SerializeField] private Text pageText;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private Sprite[] slideSprites;

        // 現在表示中のスライド index
        private int slideIndex;

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.wasPressedThisFrame)
                {
                    if (slideIndex > 0)
                    {
                        MoveSlide(-1);
                    }
                    else
                    {
                        flowController?.ShowOperationGuidePromptPanel();
                    }
                    return;
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    if (slideIndex < SlideCount - 1)
                    {
                        MoveSlide(1);
                    }
                    else
                    {
                        flowController?.FinishOperationGuide();
                    }

                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    flowController?.ShowOperationGuidePromptPanel();
                    return;
                }
            }
        }

        // 表示可能なスライド数未設定時も 1 ページとして扱う
        private int SlideCount => slideSprites != null && slideSprites.Length > 0 ? slideSprites.Length : 1;

        // Controller と UI 参照を解決し、現在ページを表示する
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null)
            {
                flowController = controller;
            }

            EnsureFlowController();
            ResolveMissingReferences();
            slideIndex = 0;
            Refresh();
        }

        // 自動生成した UI 部品を登録して初期化する
        public void Configure(
            LivePanelFlowController controller,
            Image guideSlideImage,
            Text guidePageText,
            Text guideEmptyText,
            Button slidePreviousButton,
            Button slideNextButton,
            Button panelBackButton,
            Button panelFinishButton)
        {
            flowController = controller;
            slideImage = guideSlideImage;
            pageText = guidePageText;
            emptyText = guideEmptyText;
            previousButton = slidePreviousButton;
            nextButton = slideNextButton;
            backButton = panelBackButton;
            finishButton = panelFinishButton;
            Initialize(controller);
        }

        // Inspector 未設定時に使う既定の操作説明パネルを生成する
        public static GameObject CreateDefault(Transform parent, LivePanelFlowController controller)
        {
            GameObject panel = CreateUiObject("OperationGuidePanel", parent);
            panel.SetActive(false);
            Stretch(panel.GetComponent<RectTransform>());

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.96f);

            OperationGuidePanel view = panel.AddComponent<OperationGuidePanel>();

            GameObject content = CreateUiObject("Content", panel.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 760f));

            Image contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.08f, 0.095f, 0.11f, 0.98f);

            Text heading = CreateText("Heading", content.transform, "HOW TO PLAY", 34, TextAnchor.MiddleCenter);
            heading.color = new Color(0.62f, 0.74f, 0.82f, 1f);
            SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(820f, 48f));

            GameObject slideArea = CreateUiObject("SlideArea", content.transform);
            RectTransform slideAreaRect = slideArea.GetComponent<RectTransform>();
            SetRect(slideAreaRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(980f, 500f));

            Image slideAreaImage = slideArea.AddComponent<Image>();
            slideAreaImage.color = new Color(0.12f, 0.14f, 0.16f, 1f);

            GameObject slideImageObject = CreateUiObject("SlideImage", slideArea.transform);
            Image image = slideImageObject.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 440f));

            Text empty = CreateText("EmptyText", slideArea.transform, "Assign slide sprites in Inspector.", 30, TextAnchor.MiddleCenter);
            empty.color = new Color(0.72f, 0.78f, 0.84f, 1f);
            Stretch(empty.rectTransform);

            Text page = CreateText("Page", content.transform, string.Empty, 24, TextAnchor.MiddleCenter);
            page.color = new Color(0.7f, 0.76f, 0.82f, 1f);
            SetRect(page.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(220f, 46f));

            Text hint = CreateText("Hint", content.transform, "A: previous/back    R: next/finish", 22, TextAnchor.MiddleCenter);
            hint.color = new Color(0.62f, 0.68f, 0.74f, 1f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(900f, 42f));

            Button previous = CreateButton("PreviousButton", content.transform, "<", 34, new Color(0.18f, 0.22f, 0.26f, 1f));
            SetRect(previous.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-570f, 38f), new Vector2(84f, 180f));

            Button next = CreateButton("NextButton", content.transform, ">", 34, new Color(0.18f, 0.22f, 0.26f, 1f));
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(570f, 38f), new Vector2(84f, 180f));

            Button back = CreateButton("BackButton", content.transform, "BACK", 28, new Color(0.18f, 0.22f, 0.26f, 1f));
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 44f), new Vector2(260f, 72f));

            Button finish = CreateButton("FinishButton", content.transform, "SONG SELECT", 28, new Color(0.16f, 0.38f, 0.58f, 1f));
            SetRect(finish.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 44f), new Vector2(300f, 72f));

            view.Configure(controller, image, page, empty, previous, next, back, finish);
            return panel;
        }

        // 指定方向へスライドを移動する
        private void MoveSlide(int direction)
        {
            int previousIndex = slideIndex;
            slideIndex = Mathf.Clamp(slideIndex + direction, 0, SlideCount - 1);
            if (slideIndex != previousIndex)
            {
                Refresh();
            }
        }

        // 現在スライドの画像、ページ表記、Button 状態を反映する
        private void Refresh()
        {
            Sprite sprite = GetSlideSprite(slideIndex);

            if (slideImage != null)
            {
                slideImage.sprite = sprite;
                slideImage.enabled = sprite != null;
                slideImage.preserveAspect = true;
            }

            if (emptyText != null)
            {
                emptyText.enabled = sprite == null;
            }

            if (pageText != null)
            {
                pageText.text = (slideIndex + 1).ToString() + " / " + SlideCount.ToString();
            }

            if (previousButton != null)
            {
                previousButton.interactable = slideIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = slideIndex < SlideCount - 1;
            }
        }

        // 指定 index のスライド画像を安全に取得する
        private Sprite GetSlideSprite(int index)
        {
            if (slideSprites == null || slideSprites.Length == 0)
            {
                return null;
            }

            return slideSprites[Mathf.Clamp(index, 0, slideSprites.Length - 1)];
        }

        // Inspector 未設定時に親またはシーンから Controller を探す
        private void EnsureFlowController()
        {
            if (flowController != null)
            {
                return;
            }

            flowController = GetComponentInParent<LivePanelFlowController>();
            if (flowController == null)
            {
                flowController = FindAnyObjectByType<LivePanelFlowController>();
            }
        }

        // UI 部品参照が未設定なら子階層の名前から補完する
        private void ResolveMissingReferences()
        {
            if (slideImage == null)
            {
                slideImage = FindImage("SlideImage");
            }

            if (pageText == null)
            {
                pageText = FindText("Page");
            }

            if (emptyText == null)
            {
                emptyText = FindText("EmptyText");
            }

            if (previousButton == null)
            {
                previousButton = FindButton("PreviousButton");
            }

            if (nextButton == null)
            {
                nextButton = FindButton("NextButton");
            }

            if (backButton == null)
            {
                backButton = FindButton("BackButton");
            }

            if (finishButton == null)
            {
                finishButton = FindButton("FinishButton");
            }
        }

        // 子階層から指定名の Text を探す
        private Text FindText(string objectName)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        // 子階層から指定名の Image を探す
        private Image FindImage(string objectName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == objectName)
                {
                    return images[i];
                }
            }

            return null;
        }

        // 子階層から指定名の Button を探す
        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == objectName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        // RectTransform と CanvasRenderer を持つ UI オブジェクトを作る
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
                gameObject.layer = parent.gameObject.layer;
            }

            return gameObject;
        }

        // 既定 Font を使った Text を生成する
        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            Text uiText = gameObject.AddComponent<Text>();
            uiText.font = ResolveDefaultFont();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = Color.white;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        // 既定見た目の Button を生成する
        private static Button CreateButton(string name, Transform parent, string label, int fontSize, Color color)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Text", buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        // RectTransform を親いっぱいに伸ばす
        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // RectTransform の基本レイアウト値をまとめて設定する
        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        // 実行環境で使える既定 Font を取得する
        private static Font ResolveDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
