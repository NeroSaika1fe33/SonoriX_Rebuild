using System;
using FFrameWork.Live;
using FFrameWork.Songs;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 選択中の曲と難易度に対応する自己ベストを表示する
    [RequireComponent(typeof(RectTransform))]
    public class SelectedSongInfoWindow : MonoBehaviour
    {
        [Serializable]
        private sealed class SlotView
        {
            public RectTransform root;
            public RawImage image;
            public Image uiImage;
            public Text text;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta = new Vector2(280f, 50f);
        }

        [SerializeField] private LivePanelFlowController flowController;
        [Header("Layout")]
        [SerializeField] private Vector2 windowSize = new Vector2(320f, 760f);
        [SerializeField] private Image background;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        [SerializeField] private Color slotColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        [SerializeField] private Color textColor = Color.white;

        [Header("Font Sizes")]
        [FormerlySerializedAs("textFontSize")]
        [SerializeField, Min(1)] private int titleFontSize = 26;
        [SerializeField, Min(1)] private int artistFontSize = 26;
        [SerializeField, Min(1)] private int difficultyFontSize = 26;
        [SerializeField, Min(1)] private int bestRankFontSize = 26;
        [SerializeField, Min(1)] private int highScoreFontSize = 26;
        [SerializeField, Min(1)] private int bestClearFontSize = 26;

        [Header("Slots")]
        [SerializeField] private SlotView jacketSlot = new SlotView { anchoredPosition = new Vector2(0f, 220f), sizeDelta = new Vector2(240f, 240f) };
        [SerializeField] private SlotView titleSlot = new SlotView { anchoredPosition = new Vector2(0f, 65f), sizeDelta = new Vector2(280f, 55f) };
        [SerializeField] private SlotView artistSlot = new SlotView { anchoredPosition = new Vector2(0f, 15f), sizeDelta = new Vector2(280f, 40f) };
        [SerializeField] private SlotView difficultySlot = new SlotView { anchoredPosition = new Vector2(0f, -35f), sizeDelta = new Vector2(280f, 40f) };
        [SerializeField] private SlotView bestRankSlot = new SlotView { anchoredPosition = new Vector2(0f, -95f), sizeDelta = new Vector2(280f, 55f) };
        [SerializeField] private SlotView highScoreSlot = new SlotView { anchoredPosition = new Vector2(0f, -165f), sizeDelta = new Vector2(280f, 65f) };
        [SerializeField] private SlotView bestClearSlot = new SlotView { anchoredPosition = new Vector2(0f, -245f), sizeDelta = new Vector2(280f, 70f) };

        [Header("Optional Status Sprites")]
        [Tooltip("S, A, B, C, D. Missing sprites use text.")]
        [SerializeField] private Sprite[] rankSprites = new Sprite[5];
        [Tooltip("None, Track Crash, Track Clear, Full Combo, All Perfect.")]
        [SerializeField] private Sprite[] clearSprites = new Sprite[5];
        [SerializeField] private string notPlayedText = "Not Played";

        private bool isSubscribed;
        private Texture2D jacketTexture;
        private Sprite jacketSprite;

        private void OnEnable() => Initialize();
        private void OnDisable() => Unsubscribe();
        private void OnValidate() => ApplyLayout();

        private void OnDestroy()
        {
            Unsubscribe();
            if (jacketSprite != null) Destroy(jacketSprite);
        }

        // 再表示時にも読み直し、同じ曲の新記録を反映する
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null && controller != flowController)
            {
                Unsubscribe();
                flowController = controller;
            }
            if (flowController == null) flowController = GetComponentInParent<LivePanelFlowController>();
            if (flowController == null) flowController = FindAnyObjectByType<LivePanelFlowController>();
            if (isActiveAndEnabled && flowController != null && !isSubscribed)
            {
                flowController.SelectedSongChanged += Refresh;
                isSubscribed = true;
            }
            Refresh(flowController != null ? flowController.SelectedSong : null);
        }

        private void Unsubscribe()
        {
            if (flowController != null && isSubscribed) flowController.SelectedSongChanged -= Refresh;
            isSubscribed = false;
        }

        public void Refresh(SongSelectEntry song)
        {
            EnsureView();
            BindJacket(song != null ? song.Jacket : null);
            SetText(titleSlot, song != null ? song.Title : string.Empty);
            SetText(artistSlot, song != null ? (string.IsNullOrEmpty(song.Artist) ? song.Composer : song.Artist) : string.Empty);
            SetText(difficultySlot, song != null ? song.Difficulty + "  Lv." + song.PlayLevel : string.Empty);
            SongPlayRecord record = song != null ? song.PlayRecord : default;
            bool hasRecord = record.HasPlayRecord;
            SetStatus(bestRankSlot, GetSprite(rankSprites, (int)record.BestRank),
                "Best Rank: " + (hasRecord ? record.BestRank.ToString() : "--"));
            SetText(highScoreSlot, "High Score\n" + (song != null ? record.HighScore.ToString("N0") : "--"));
            SetStatus(bestClearSlot, GetSprite(clearSprites, (int)record.BestClearStatus),
                hasRecord ? GetClearLabel(record.BestClearStatus) : notPlayedText);
        }

        private void EnsureView()
        {
            if (background == null) background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = false;
            if (backgroundSprite != null) background.sprite = backgroundSprite;
            EnsureSlot(jacketSlot, "JacketSlot", false);
            EnsureSlot(titleSlot, "TitleSlot", true);
            EnsureSlot(artistSlot, "ArtistSlot", true);
            EnsureSlot(difficultySlot, "DifficultySlot", true);
            EnsureSlot(bestRankSlot, "BestRankSlot", true);
            EnsureSlot(highScoreSlot, "HighScoreSlot", true);
            EnsureSlot(bestClearSlot, "BestClearSlot", true);
            ApplyLayout();
        }

        private void EnsureSlot(SlotView slot, string slotName, bool needsText)
        {
            if (slot.root == null) slot.root = transform.Find(slotName) as RectTransform;
            if (slot.root == null)
            {
                GameObject obj = new GameObject(slotName, typeof(RectTransform), typeof(RawImage));
                obj.layer = gameObject.layer;
                obj.transform.SetParent(transform, false);
                slot.root = obj.GetComponent<RectTransform>();
            }
            if (slot.image == null) slot.image = slot.root.GetComponent<RawImage>();
            if (slot.uiImage == null) slot.uiImage = slot.root.GetComponent<Image>();
            if (slot.image == null && slot.uiImage == null) slot.image = slot.root.gameObject.AddComponent<RawImage>();
            if (slot.image != null) slot.image.raycastTarget = false;
            if (slot.uiImage != null) slot.uiImage.raycastTarget = false;
            if (slot.text == null && needsText) slot.text = slot.root.GetComponentInChildren<Text>(true);
            if (slot.text == null && needsText)
            {
                GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                obj.layer = gameObject.layer;
                obj.transform.SetParent(slot.root, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                slot.text = obj.GetComponent<Text>();
                slot.text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                slot.text.alignment = TextAnchor.MiddleCenter;
            }
            if (slot.text != null)
            {
                slot.text.color = textColor;
                slot.text.raycastTarget = false;
            }
        }

        private void ApplyLayout()
        {
            if (transform is RectTransform rect) rect.sizeDelta = windowSize;
            ApplySlotLayout(jacketSlot);
            ApplySlotLayout(titleSlot);
            ApplySlotLayout(artistSlot);
            ApplySlotLayout(difficultySlot);
            ApplySlotLayout(bestRankSlot);
            ApplySlotLayout(highScoreSlot);
            ApplySlotLayout(bestClearSlot);

            ApplyFontSize(titleSlot?.text, titleFontSize);
            ApplyFontSize(artistSlot?.text, artistFontSize);
            ApplyFontSize(difficultySlot?.text, difficultyFontSize);
            ApplyFontSize(bestRankSlot?.text, bestRankFontSize);
            ApplyFontSize(highScoreSlot?.text, highScoreFontSize);
            ApplyFontSize(bestClearSlot?.text, bestClearFontSize);
        }

        // 内容ごとの文字サイズを Inspector と実行時の両方で反映する
        private static void ApplyFontSize(Text text, int fontSize)
        {
            if (text != null) text.fontSize = Mathf.Max(1, fontSize);
        }

        private static void ApplySlotLayout(SlotView slot)
        {
            if (slot?.root == null) return;
            slot.root.anchorMin = slot.root.anchorMax = slot.root.pivot = new Vector2(0.5f, 0.5f);
            slot.root.anchoredPosition = slot.anchoredPosition;
            slot.root.sizeDelta = slot.sizeDelta;
        }

        private void BindJacket(Texture2D texture)
        {
            if (jacketSlot.image != null)
            {
                jacketSlot.image.texture = texture;
                jacketSlot.image.color = texture != null ? Color.white : Color.clear;
            }
            if (jacketSlot.uiImage == null) return;
            if (jacketTexture != texture)
            {
                if (jacketSprite != null) Destroy(jacketSprite);
                jacketTexture = texture;
                jacketSprite = texture != null ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : null;
            }
            jacketSlot.uiImage.sprite = jacketSprite;
            jacketSlot.uiImage.preserveAspect = true;
            jacketSlot.uiImage.color = texture != null ? Color.white : Color.clear;
        }

        private void SetStatus(SlotView slot, Sprite sprite, string label)
        {
            SetText(slot, label);
            if (slot.text != null) slot.text.enabled = sprite == null;
            if (slot.uiImage != null)
            {
                slot.uiImage.sprite = sprite;
                slot.uiImage.preserveAspect = true;
                slot.uiImage.color = sprite != null ? Color.white : slotColor;
            }
            if (slot.image != null)
            {
                slot.image.texture = sprite != null ? sprite.texture : null;
                slot.image.color = sprite != null ? Color.white : slotColor;
                Rect uv = sprite != null ? sprite.textureRect : new Rect(0f, 0f, 1f, 1f);
                if (sprite != null)
                {
                    uv.x /= sprite.texture.width;
                    uv.y /= sprite.texture.height;
                    uv.width /= sprite.texture.width;
                    uv.height /= sprite.texture.height;
                }
                slot.image.uvRect = uv;
            }
        }

        private static void SetText(SlotView slot, string value)
        {
            if (slot.text != null) slot.text.text = value;
        }

        private static Sprite GetSprite(Sprite[] sprites, int index)
        {
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }

        private static string GetClearLabel(LiveClearStatus status)
        {
            switch (status)
            {
                case  LiveClearStatus.None: return "NotPlayed";
                case LiveClearStatus.AllPerfect: return "All Perfect";
                case LiveClearStatus.FullCombo: return "Full Combo";
                case LiveClearStatus.TrackClear: return "Track Clear";
                case LiveClearStatus.TrackCrash: return "Track Crash";
                default: return "NotPlayed";
            }
        }
    }
}
