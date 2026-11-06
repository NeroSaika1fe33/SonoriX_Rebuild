using FFrameWork.Live;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // ライブ開始前にノーツ速度を調整するパネル
    public class NoteSpeedSettingsPanel : MonoBehaviour
    {
        // 次画面への遷移を担当する Controller
        [SerializeField] private LivePanelFlowController flowController;
        [SerializeField] private Slider noteSpeedSlider;
        [SerializeField] private Text valueText;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button startButton;
        [SerializeField] private float keyboardStep = LivePlayerSettings.NoteSpeedStep;
        [SerializeField] private float deltaThreshold = 2f;
        [SerializeField] private float moveRepeatSeconds = 0.12f;

        // UI イベントと設定変更イベントを購読中か
        private bool isSubscribed;
        // UI 更新中の Slider コールバック再入を防ぐ
        private bool isUpdatingView;
        // マウス移動入力のリピート抑制時刻
        private float nextMoveTime;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    AddNoteSpeed(-keyboardStep);
                    return;
                }

                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    AddNoteSpeed(keyboardStep);
                    return;
                }

                if (keyboard.downArrowKey.wasPressedThisFrame)
                {
                    AddNoteSpeed(-keyboardStep);
                    return;
                }

                if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    AddNoteSpeed(keyboardStep);
                    return;
                }

                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.rKey.wasPressedThisFrame)
                {
                    flowController?.GoNext();
                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                {
                    flowController?.ShowSelectSongPanel();
                    return;
                }
            }

            float deltaY = Mouse.current != null ? Mouse.current.delta.y.ReadValue() : 0f;
            if (Mathf.Abs(deltaY) < deltaThreshold || Time.unscaledTime < nextMoveTime)
            {
                return;
            }

            AddNoteSpeed(deltaY > 0f ? keyboardStep : -keyboardStep);
            nextMoveTime = Time.unscaledTime + moveRepeatSeconds;
        }

        // Controller と UI 参照を解決し、現在のノーツ速度を表示する
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null)
            {
                flowController = controller;
            }

            EnsureFlowController();
            ResolveMissingReferences();
            ConfigureSlider();
            Subscribe();
            Refresh(LivePlayerSettings.NoteSpeed);
        }

        // 自動生成した UI 部品を登録して初期化する
        public void Configure(
            LivePanelFlowController controller,
            Slider slider,
            Text speedValueText,
            Button speedDecreaseButton,
            Button speedIncreaseButton,
            Button panelBackButton,
            Button panelStartButton)
        {
            flowController = controller;
            noteSpeedSlider = slider;
            valueText = speedValueText;
            decreaseButton = speedDecreaseButton;
            increaseButton = speedIncreaseButton;
            backButton = panelBackButton;
            startButton = panelStartButton;
            Initialize(controller);
        }

        // Inspector 未設定時に使う既定のノーツ速度設定パネルを生成する
        public static GameObject CreateDefault(Transform parent, LivePanelFlowController controller)
        {
            GameObject panel = CreateUiObject("NoteSpeedSettingsPanel", parent);
            panel.SetActive(false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            Stretch(panelRect);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.96f);

            NoteSpeedSettingsPanel view = panel.AddComponent<NoteSpeedSettingsPanel>();

            GameObject content = CreateUiObject("Content", panel.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(960f, 540f);

            Image contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.08f, 0.095f, 0.11f, 0.98f);

            Text title = CreateText("Title", content.transform, "NOTE SPEED", 52, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(820f, 80f));

            Text hint = CreateText("Hint", content.transform, "DeltaY / Arrow: 0.1", 24, TextAnchor.MiddleCenter);
            hint.color = new Color(0.74f, 0.78f, 0.82f, 1f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(820f, 48f));

            Text value = CreateText("Value", content.transform, string.Empty, 72, TextAnchor.MiddleCenter);
            value.color = new Color(0.98f, 0.86f, 0.34f, 1f);
            SetRect(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(500f, 100f));

            Slider slider = CreateSlider("NoteSpeedSlider", content.transform);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(700f, 56f));

            Button decrease = CreateButton("DecreaseButton", content.transform, "-", 34);
            SetRect(decrease.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430f, -18f), new Vector2(96f, 64f));

            Button increase = CreateButton("IncreaseButton", content.transform, "+", 34);
            SetRect(increase.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, -18f), new Vector2(96f, 64f));

            Button back = CreateButton("BackButton", content.transform, "BACK", 28);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 64f), new Vector2(260f, 72f));

            Button start = CreateButton("StartButton", content.transform, "START", 28);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(170f, 64f), new Vector2(260f, 72f));

            view.Configure(controller, slider, value, decrease, increase, back, start);
            return panel;
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
            if (noteSpeedSlider == null)
            {
                noteSpeedSlider = GetComponentInChildren<Slider>(true);
            }

            if (valueText == null)
            {
                valueText = FindText("Value");
            }

            if (decreaseButton == null)
            {
                decreaseButton = FindButton("DecreaseButton");
            }

            if (increaseButton == null)
            {
                increaseButton = FindButton("IncreaseButton");
            }

            if (backButton == null)
            {
                backButton = FindButton("BackButton");
            }

            if (startButton == null)
            {
                startButton = FindButton("StartButton");
            }
        }

        // ノーツ速度の最小値、最大値を Slider へ設定する
        private void ConfigureSlider()
        {
            if (noteSpeedSlider == null)
            {
                return;
            }

            noteSpeedSlider.minValue = LivePlayerSettings.MinNoteSpeed;
            noteSpeedSlider.maxValue = LivePlayerSettings.MaxNoteSpeed;
            noteSpeedSlider.wholeNumbers = false;
        }

        // Slider、Button、設定変更イベントを購読する
        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (noteSpeedSlider != null)
            {
                noteSpeedSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            decreaseButton?.onClick.AddListener(OnDecreaseClicked);
            increaseButton?.onClick.AddListener(OnIncreaseClicked);
            backButton?.onClick.AddListener(OnBackClicked);
            startButton?.onClick.AddListener(OnStartClicked);
            LivePlayerSettings.NoteSpeedChanged += Refresh;
            isSubscribed = true;
        }

        // Slider、Button、設定変更イベントの購読を解除する
        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (noteSpeedSlider != null)
            {
                noteSpeedSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }

            decreaseButton?.onClick.RemoveListener(OnDecreaseClicked);
            increaseButton?.onClick.RemoveListener(OnIncreaseClicked);
            backButton?.onClick.RemoveListener(OnBackClicked);
            startButton?.onClick.RemoveListener(OnStartClicked);
            LivePlayerSettings.NoteSpeedChanged -= Refresh;
            isSubscribed = false;
        }

        // ノーツ速度値を Slider と表示文字列へ反映する
        private void Refresh(float noteSpeed)
        {
            float normalized = LivePlayerSettings.NormalizeNoteSpeed(noteSpeed);
            isUpdatingView = true;

            if (noteSpeedSlider != null)
            {
                noteSpeedSlider.SetValueWithoutNotify(normalized);
            }

            if (valueText != null)
            {
                valueText.text = normalized.ToString("0.0");
            }

            isUpdatingView = false;
        }

        // Slider 操作からノーツ速度を更新する
        private void OnSliderValueChanged(float value)
        {
            if (isUpdatingView)
            {
                return;
            }

            float normalized = LivePlayerSettings.SetNoteSpeed(value);
            Refresh(normalized);
        }

        private void OnDecreaseClicked()
        {
            AddNoteSpeed(-keyboardStep);
        }

        private void OnIncreaseClicked()
        {
            AddNoteSpeed(keyboardStep);
        }

        private void OnBackClicked()
        {
            flowController?.ShowSelectSongPanel();
        }

        private void OnStartClicked()
        {
            flowController?.GoNext();
        }

        // 指定差分だけノーツ速度を変更する
        private void AddNoteSpeed(float delta)
        {
            float noteSpeed = LivePlayerSettings.SetNoteSpeed(LivePlayerSettings.NoteSpeed + delta);
            Refresh(noteSpeed);
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
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        // 既定見た目のノーツ速度 Slider を生成する
        private static Slider CreateSlider(string name, Transform parent)
        {
            GameObject sliderObject = CreateUiObject(name, parent);

            GameObject backgroundObject = CreateUiObject("Background", sliderObject.transform);
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            Stretch(backgroundObject.GetComponent<RectTransform>());

            GameObject fillAreaObject = CreateUiObject("Fill Area", sliderObject.transform);
            RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(12f, 0f);
            fillAreaRect.offsetMax = new Vector2(-12f, 0f);

            GameObject fillObject = CreateUiObject("Fill", fillAreaObject.transform);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.96f, 0.72f, 0.2f, 1f);
            Stretch(fillObject.GetComponent<RectTransform>());

            GameObject handleAreaObject = CreateUiObject("Handle Slide Area", sliderObject.transform);
            RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
            Stretch(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(24f, 0f);
            handleAreaRect.offsetMax = new Vector2(-24f, 0f);

            GameObject handleObject = CreateUiObject("Handle", handleAreaObject.transform);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = Color.white;
            SetRect(handleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.targetGraphic = handleImage;
            slider.fillRect = fillObject.GetComponent<RectTransform>();
            slider.handleRect = handleObject.GetComponent<RectTransform>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = LivePlayerSettings.MinNoteSpeed;
            slider.maxValue = LivePlayerSettings.MaxNoteSpeed;
            slider.value = LivePlayerSettings.NoteSpeed;
            return slider;
        }

        // 既定見た目の Button を生成する
        private static Button CreateButton(string name, Transform parent, string label, int fontSize)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.3f, 1f);

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
