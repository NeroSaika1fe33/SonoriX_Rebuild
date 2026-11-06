using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 操作説明を見るかスキップするかを選ばせる確認パネル。
    public class OperationGuidePromptPanel : MonoBehaviour
    {
        // 遷移先を制御する Controller。
        [SerializeField] private LivePanelFlowController flowController;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    flowController?.ShowOperationGuidePanel();
                    return;
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    flowController?.SkipOperationGuideAndShowSelectSong();
                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                {
                    flowController?.ShowGameStartPanel();
                }
            }
        }

        // Controller と Button 参照を解決して入力可能にする。
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null)
            {
                flowController = controller;
            }

            EnsureFlowController();
            ResolveMissingReferences();
        }

        // 自動生成した UI 部品を登録して初期化する。
        public void Configure(
            LivePanelFlowController controller,
            Button panelYesButton,
            Button panelNoButton,
            Button panelBackButton)
        {
            flowController = controller;
            yesButton = panelYesButton;
            noButton = panelNoButton;
            backButton = panelBackButton;
            Initialize(controller);
        }

        // Inspector 未設定時に使う既定の操作説明確認パネルを生成する。
        public static GameObject CreateDefault(Transform parent, LivePanelFlowController controller)
        {
            GameObject panel = CreateUiObject("OperationGuidePromptPanel", parent);
            panel.SetActive(false);
            Stretch(panel.GetComponent<RectTransform>());

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.96f);

            OperationGuidePromptPanel view = panel.AddComponent<OperationGuidePromptPanel>();

            GameObject content = CreateUiObject("Content", panel.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 520f));

            Image contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.08f, 0.095f, 0.11f, 0.98f);

            Text title = CreateText("Title", content.transform, "TUTORIAL", 56, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(820f, 90f));

            Text message = CreateText("Message", content.transform, "View the operation guide before song select?", 34, TextAnchor.MiddleCenter);
            message.color = new Color(0.88f, 0.92f, 0.96f, 1f);
            SetRect(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 68f), new Vector2(820f, 88f));

            Text hint = CreateText("Hint", content.transform, "R: View guide    Space: Skip", 24, TextAnchor.MiddleCenter);
            hint.color = new Color(0.62f, 0.68f, 0.74f, 1f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(820f, 54f));

            Button back = CreateButton("BackButton", content.transform, "BACK", 28, new Color(0.18f, 0.22f, 0.26f, 1f));
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-310f, 70f), new Vector2(220f, 72f));

            Button no = CreateButton("NoButton", content.transform, "SKIP", 28, new Color(0.18f, 0.22f, 0.26f, 1f));
            SetRect(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(220f, 72f));

            Button yes = CreateButton("YesButton", content.transform, "VIEW GUIDE", 28, new Color(0.16f, 0.38f, 0.58f, 1f));
            SetRect(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(310f, 70f), new Vector2(260f, 72f));

            view.Configure(controller, yes, no, back);
            return panel;
        }

        // Inspector 未設定時に親またはシーンから Controller を探す。
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

        // Button 参照が未設定なら子階層の名前から補完する。
        private void ResolveMissingReferences()
        {
            if (yesButton == null)
            {
                yesButton = FindButton("YesButton");
            }

            if (noButton == null)
            {
                noButton = FindButton("NoButton");
            }

            if (backButton == null)
            {
                backButton = FindButton("BackButton");
            }
        }

        // 子階層から指定名の Button を探す。
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

        // RectTransform と CanvasRenderer を持つ UI オブジェクトを作る。
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

        // 既定 Font を使った Text を生成する。
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

        // 既定見た目の Button を生成する。
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

        // RectTransform を親いっぱいに伸ばす。
        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // RectTransform の基本レイアウト値をまとめて設定する。
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

        // 実行環境で使える既定 Font を取得する。
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
