using FFrameWork.Live;
using FFrameWork.Songs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 選曲メニューの 1 行分の表示と選択状態を管理する
    public class SongSelectMenuItemView : MonoBehaviour
    {
        [System.Serializable]
        // 1 スロットの位置とサイズ設定
        private sealed class SlotLayout
        {
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta = new Vector2(160f, 72f);
        }

        [System.Serializable]
        // ジャケット、曲名、難易度などの表示スロット参照
        private sealed class SlotView
        {
            public RectTransform root;
            public RawImage image;
            public Image uiImage;
            public Text text;

            [System.NonSerialized] public bool hasDefaultColor;
            [System.NonSerialized] public Color defaultColor;
        }

        [Header("Generated Layout")]
        [SerializeField] private Vector2 itemSize = new Vector2(920f, 170f);
        [SerializeField] private SlotLayout jacketLayout = new SlotLayout { anchoredPosition = new Vector2(-365f, 0f), sizeDelta = new Vector2(130f, 130f) };
        [SerializeField] private SlotLayout titleLayout = new SlotLayout { anchoredPosition = new Vector2(-120f, 42f), sizeDelta = new Vector2(330f, 58f) };
        [SerializeField] private SlotLayout artistLayout = new SlotLayout { anchoredPosition = new Vector2(245f, 42f), sizeDelta = new Vector2(270f, 58f) };
        [SerializeField] private SlotLayout easyLayout = new SlotLayout { anchoredPosition = new Vector2(-170f, -45f), sizeDelta = new Vector2(128f, 64f) };
        [SerializeField] private SlotLayout normalLayout = new SlotLayout { anchoredPosition = new Vector2(-28f, -45f), sizeDelta = new Vector2(128f, 64f) };
        [SerializeField] private SlotLayout hardLayout = new SlotLayout { anchoredPosition = new Vector2(114f, -45f), sizeDelta = new Vector2(128f, 64f) };
        [SerializeField] private SlotLayout extraLayout = new SlotLayout { anchoredPosition = new Vector2(256f, -45f), sizeDelta = new Vector2(128f, 64f) };
        [SerializeField] private SlotLayout debugLayout = new SlotLayout { anchoredPosition = new Vector2(398f, -45f), sizeDelta = new Vector2(128f, 64f) };

        [Header("Optional Prefab Slots")]
        [SerializeField] private Image background;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Outline selectionOutline;
        [SerializeField] private SlotView jacketSlot = new SlotView();
        [SerializeField] private SlotView titleSlot = new SlotView();
        [SerializeField] private SlotView artistSlot = new SlotView();
        [SerializeField] private SlotView easySlot = new SlotView();
        [SerializeField] private SlotView normalSlot = new SlotView();
        [SerializeField] private SlotView hardSlot = new SlotView();
        [SerializeField] private SlotView extraSlot = new SlotView();
        [SerializeField] private SlotView debugSlot = new SlotView();

        [Header("Textures")]
        [SerializeField] private Texture titleTexture;
        [SerializeField] private Texture artistTexture;
        [SerializeField] private Texture easyTexture;
        [SerializeField] private Texture normalTexture;
        [SerializeField] private Texture hardTexture;
        [SerializeField] private Texture extraTexture;
        [SerializeField] private Texture debugTexture;

        [Header("Colors")]
        [SerializeField] private Color rowColor = new Color(0.05f, 0.06f, 0.08f, 0.88f);
        [SerializeField] private Color selectedRowColor = new Color(0.2f, 0.42f, 0.68f, 0.98f);
        [SerializeField] private Color selectionOutlineColor = new Color(0.48f, 0.86f, 1f, 1f);
        [SerializeField] private Color slotColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        [SerializeField] private Color selectedSlotColor = new Color(1f, 0.92f, 0.24f, 1f);
        [SerializeField] private Color unavailableSlotColor = new Color(0.08f, 0.08f, 0.09f, 0.55f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color unavailableTextColor = new Color(1f, 1f, 1f, 0.32f);
        [SerializeField] private Vector2 selectionOutlineDistance = new Vector2(5f, -5f);

        [Header("Font Sizes")]
        [FormerlySerializedAs("textFontSize")]
        [SerializeField, Min(1)] private int titleFontSize = 24;
        [SerializeField, Min(1)] private int artistFontSize = 24;
        [SerializeField, Min(1)] private int easyFontSize = 24;
        [SerializeField, Min(1)] private int normalFontSize = 24;
        [SerializeField, Min(1)] private int hardFontSize = 24;
        [SerializeField, Min(1)] private int extraFontSize = 24;
        [SerializeField, Min(1)] private int debugFontSize = 24;

        // 各難易度が選択可能か
        private bool hasEasy;
        private bool hasNormal;
        private bool hasHard;
        private bool hasExtra;
        private bool hasDebug;

        // 現在の行サイズ
        public Vector2 ItemSize => itemSize;

        private void OnValidate()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            ApplyBackgroundSprite();
            ApplyLayout();
        }

        // 行サイズを変更してレイアウトへ反映する
        public void SetItemSize(Vector2 size)
        {
            itemSize = size;
            ApplyLayout();
        }

        // 楽曲情報と難易度情報を行ビューへ反映する
        public void Bind(
            Texture jacket,
            string title,
            string artist,
            SongSelectEntry easy,
            SongSelectEntry normal,
            SongSelectEntry hard,
            SongSelectEntry extra,
            SongSelectEntry debug)
        {
            EnsureView();
            BindJacket(jacket);
            SetText(titleSlot, title);
            SetText(artistSlot, artist);

            hasEasy = easy != null;
            hasNormal = normal != null;
            hasHard = hard != null;
            hasExtra = extra != null;
            hasDebug = debug != null;

            BindDifficulty(easySlot, easy, easyTexture, "Easy");
            BindDifficulty(normalSlot, normal, normalTexture, "Normal");
            BindDifficulty(hardSlot, hard, hardTexture, "Hard");
            BindDifficulty(extraSlot, extra, extraTexture, "Extra");
            BindDifficulty(debugSlot, debug, debugTexture, "Debug");
            SetSlotVisible(debugSlot, hasDebug);
        }

        // 曲選択と難易度選択のハイライトを更新する
        public void SetSelection(bool isSongSelected, bool isDifficultyMode, MusicDifficulty selectedDifficulty)
        {
            EnsureView();

            if (background != null)
            {
                background.color = isSongSelected ? selectedRowColor : rowColor;
            }

            SetSelectionOutline(isSongSelected);
            SetJacketColor(jacketSlot, HasSlotTexture(jacketSlot));
            SetSlotSelection(titleSlot, false, true);
            SetSlotSelection(artistSlot, false, true);
            SetSlotSelection(easySlot, isSongSelected && isDifficultyMode && selectedDifficulty == MusicDifficulty.Easy, hasEasy);
            SetSlotSelection(normalSlot, isSongSelected && isDifficultyMode && selectedDifficulty == MusicDifficulty.Normal, hasNormal);
            SetSlotSelection(hardSlot, isSongSelected && isDifficultyMode && selectedDifficulty == MusicDifficulty.Hard, hasHard);
            SetSlotSelection(extraSlot, isSongSelected && isDifficultyMode && selectedDifficulty == MusicDifficulty.Extra, hasExtra);
            SetSlotSelection(debugSlot, isSongSelected && isDifficultyMode && selectedDifficulty == MusicDifficulty.Debug, hasDebug);
            SetSlotVisible(debugSlot, hasDebug);
        }

        // Prefab 未設定でも必要な UI 部品を生成して参照を揃える
        private void EnsureView()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = itemSize;
            }

            if (background == null)
            {
                background = GetComponent<Image>();
                if (background == null)
                {
                    background = gameObject.AddComponent<Image>();
                }
            }

            RawImage legacyBackground = GetComponent<RawImage>();
            if (legacyBackground != null && legacyBackground.gameObject == gameObject)
            {
                legacyBackground.enabled = false;
            }

            ApplyBackgroundSprite();
            EnsureSelectionOutline();
            EnsureSlot(ref jacketSlot, "JacketSlot", jacketLayout, null, false);
            EnsureSlot(ref titleSlot, "TitleSlot", titleLayout, titleTexture, true);
            EnsureSlot(ref artistSlot, "ArtistSlot", artistLayout, artistTexture, true);
            EnsureSlot(ref easySlot, "EasySlot", easyLayout, easyTexture, true);
            EnsureSlot(ref normalSlot, "NormalSlot", normalLayout, normalTexture, true);
            EnsureSlot(ref hardSlot, "HardSlot", hardLayout, hardTexture, true);
            EnsureSlot(ref extraSlot, "ExtraSlot", extraLayout, extraTexture, true);
            EnsureSlot(ref debugSlot, "DebugSlot", debugLayout, debugTexture, true);
            ApplyLayout();
        }

        // 指定スロットの RectTransform、画像、テキストを確保する
        private void EnsureSlot(ref SlotView slot, string name, SlotLayout layout, Texture texture, bool applyDefaultTexture)
        {
            if (slot == null)
            {
                slot = new SlotView();
            }

            if (slot.root == null)
            {
                Transform existing = transform.Find(name);
                slot.root = existing as RectTransform;
            }

            if (slot.root == null)
            {
                GameObject slotObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                slotObject.transform.SetParent(transform, false);
                slot.root = slotObject.GetComponent<RectTransform>();
            }

            if (slot.image == null)
            {
                slot.image = slot.root.GetComponent<RawImage>();
            }

            if (slot.uiImage == null)
            {
                slot.uiImage = slot.root.GetComponent<Image>();
            }

            if (slot.image == null && slot.uiImage == null)
            {
                slot.image = slot.root.gameObject.AddComponent<RawImage>();
                slot.image.color = slotColor;
            }

            CaptureDefaultSlotColor(slot);

            if (slot.text == null)
            {
                slot.text = slot.root.GetComponentInChildren<Text>(true);
            }

            if (slot.text == null && name != "JacketSlot")
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(slot.root, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                slot.text = textObject.GetComponent<Text>();
                slot.text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                slot.text.alignment = TextAnchor.MiddleCenter;
                slot.text.raycastTarget = false;
            }

            if (applyDefaultTexture)
            {
                SetSlotTexture(slot, texture);
            }

            ApplySlotLayout(slot.root, layout);
            ApplyText(slot.text);
        }

        // 全スロットのレイアウト設定を反映する
        private void ApplyLayout()
        {
            ApplySlotLayout(jacketSlot?.root, jacketLayout);
            ApplySlotLayout(titleSlot?.root, titleLayout);
            ApplySlotLayout(artistSlot?.root, artistLayout);
            ApplySlotLayout(easySlot?.root, easyLayout);
            ApplySlotLayout(normalSlot?.root, normalLayout);
            ApplySlotLayout(hardSlot?.root, hardLayout);
            ApplySlotLayout(extraSlot?.root, extraLayout);
            ApplySlotLayout(debugSlot?.root, debugLayout);

            ApplyFontSize(titleSlot?.text, titleFontSize);
            ApplyFontSize(artistSlot?.text, artistFontSize);
            ApplyFontSize(easySlot?.text, easyFontSize);
            ApplyFontSize(normalSlot?.text, normalFontSize);
            ApplyFontSize(hardSlot?.text, hardFontSize);
            ApplyFontSize(extraSlot?.text, extraFontSize);
            ApplyFontSize(debugSlot?.text, debugFontSize);
        }

        // 選択中表示用の Outline を確保する
        private void EnsureSelectionOutline()
        {
            if (background == null)
            {
                return;
            }

            if (selectionOutline == null)
            {
                selectionOutline = background.GetComponent<Outline>();
                if (selectionOutline == null)
                {
                    selectionOutline = background.gameObject.AddComponent<Outline>();
                }
            }

            selectionOutline.effectColor = selectionOutlineColor;
            selectionOutline.effectDistance = selectionOutlineDistance;
            selectionOutline.useGraphicAlpha = false;
        }

        // 背景 Sprite が設定されていれば Image へ反映する
        private void ApplyBackgroundSprite()
        {
            if (background != null && backgroundSprite != null)
            {
                background.sprite = backgroundSprite;
            }
        }

        // 曲行選択中の Outline 表示を切り替える
        private void SetSelectionOutline(bool isSongSelected)
        {
            EnsureSelectionOutline();

            if (selectionOutline == null)
            {
                return;
            }

            selectionOutline.effectColor = selectionOutlineColor;
            selectionOutline.effectDistance = selectionOutlineDistance;
            selectionOutline.enabled = isSongSelected;
        }

        // スロットのアンカー、位置、サイズを設定する
        private static void ApplySlotLayout(RectTransform target, SlotLayout layout)
        {
            if (target == null || layout == null)
            {
                return;
            }

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = layout.anchoredPosition;
            target.sizeDelta = layout.sizeDelta;
        }

        // スロット内テキストの基本表示を設定する
        private void ApplyText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // 内容ごとの字号を Inspector と実行時の両方で反映する
        private static void ApplyFontSize(Text text, int fontSize)
        {
            if (text != null)
            {
                text.fontSize = Mathf.Max(1, fontSize);
            }
        }

        // ジャケットスロットへ Texture を反映する
        private void BindJacket(Texture jacket)
        {
            if (!HasSlotGraphic(jacketSlot))
            {
                return;
            }

            SetSlotTexture(jacketSlot, jacket);
            SetJacketColor(jacketSlot, jacket != null);
        }

        // スロット内 Text が存在する場合だけ文字列を設定する
        private void SetText(SlotView slot, string value)
        {
            if (slot?.text != null)
            {
                slot.text.text = value ?? string.Empty;
            }
        }

        // 難易度スロットへ存在有無、レベル、テクスチャを反映する
        private void BindDifficulty(SlotView slot, SongSelectEntry song, Texture texture, string fallbackLabel)
        {
            if (HasSlotGraphic(slot))
            {
                SetSlotTexture(slot, texture);
                SetSlotColor(slot, song != null ? GetDefaultSlotColor(slot) : unavailableSlotColor);
            }

            if (slot?.text != null)
            {
                slot.text.text = song != null && song.PlayLevel >= 0 ? $"{song.PlayLevel}" : fallbackLabel;
                slot.text.color = song != null ? textColor : unavailableTextColor;
            }
        }

        // 難易度スロットの選択色と利用不可色を切り替える
        private void SetSlotSelection(SlotView slot, bool selected, bool available)
        {
            if (HasSlotGraphic(slot))
            {
                SetSlotColor(slot, selected ? selectedSlotColor : available ? GetDefaultSlotColor(slot) : unavailableSlotColor);
            }

            if (slot?.text != null)
            {
                slot.text.color = available ? textColor : unavailableTextColor;
            }
        }

        // スロットに RawImage または Image があるか
        private static bool HasSlotGraphic(SlotView slot)
        {
            return slot?.image != null || slot?.uiImage != null;
        }

        // スロットに表示用テクスチャまたは Sprite があるか
        private static bool HasSlotTexture(SlotView slot)
        {
            return slot?.image != null && slot.image.texture != null || slot?.uiImage != null && slot.uiImage.sprite != null;
        }

        // RawImage と Image の両対応で Texture を反映する
        private static void SetSlotTexture(SlotView slot, Texture texture)
        {
            if (slot?.image != null)
            {
                slot.image.texture = texture;
            }

            if (slot?.uiImage != null)
            {
                slot.uiImage.sprite = TextureSpriteCache.GetSprite(texture as Texture2D);
            }
        }

        // RawImage と Image の両対応で色を反映する
        private static void SetSlotColor(SlotView slot, Color color)
        {
            if (slot?.image != null)
            {
                slot.image.color = color;
            }

            if (slot?.uiImage != null)
            {
                slot.uiImage.color = color;
            }
        }

        // Prefab 側で設定された初期色を保持する
        private static void CaptureDefaultSlotColor(SlotView slot)
        {
            if (slot == null || slot.hasDefaultColor)
            {
                return;
            }

            slot.defaultColor = GetSlotColor(slot, Color.white);
            slot.hasDefaultColor = true;
        }

        // スロットの初期色を取得し、未取得なら既定色を返す
        private Color GetDefaultSlotColor(SlotView slot)
        {
            if (slot == null)
            {
                return slotColor;
            }

            CaptureDefaultSlotColor(slot);
            return slot.hasDefaultColor ? slot.defaultColor : slotColor;
        }

        // スロット画像の現在色を取得する
        private static Color GetSlotColor(SlotView slot, Color fallback)
        {
            if (slot?.image != null)
            {
                return slot.image.color;
            }

            if (slot?.uiImage != null)
            {
                return slot.uiImage.color;
            }

            return fallback;
        }

        // スロット GameObject の表示を切り替える
        private static void SetSlotVisible(SlotView slot, bool visible)
        {
            if (slot?.root != null)
            {
                slot.root.gameObject.SetActive(visible);
            }
        }

        // ジャケットの有無に応じて表示色を設定する
        private void SetJacketColor(SlotView slot, bool available)
        {
            SetSlotColor(slot, available ? Color.white : unavailableSlotColor);
        }

        // Texture2D から作った Sprite を再利用する
        private static class TextureSpriteCache
        {
            private static readonly Dictionary<Texture2D, Sprite> Sprites = new Dictionary<Texture2D, Sprite>();

            // Texture2D に対応する Sprite を取得または生成する
            public static Sprite GetSprite(Texture2D texture)
            {
                if (texture == null)
                {
                    return null;
                }

                if (!Sprites.TryGetValue(texture, out Sprite sprite) || sprite == null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                    Sprites[texture] = sprite;
                }

                return sprite;
            }
        }
    }
}
