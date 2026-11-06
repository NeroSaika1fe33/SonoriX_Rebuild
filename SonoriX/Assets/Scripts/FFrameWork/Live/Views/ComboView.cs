using UnityEngine;
using UnityEngine.UI;

namespace FFrameWork.Live
{
    public class ComboView : MonoBehaviour
    {
        // All Perfect 演出の点滅速度
        private const float ApEffectSpeed = 4.1887903f;

        [Header("Canvas Root References")]
        // 通常コンボ数字のルート
        [SerializeField] private RectTransform numberRoot;

        // コンボ数字の拡散エフェクト用ルート
        [SerializeField] private RectTransform effectNumberRoot;

        // All Perfect 用エフェクト数字のルート
        [SerializeField] private RectTransform allPerfectEffectRoot;

        [Header("Canvas Layout Settings")]
        // 桁ごとの表示間隔
        [SerializeField] private float digitSpacing = 60f;

        // 各桁 Image の表示サイズ
        [SerializeField] private Vector2 digitSize = new Vector2(64f, 80f);

        [Header("Sprites")]
        // 通常コンボ数字 Sprite
        [SerializeField] private Sprite[] numberSprites;

        // All Perfect 中のコンボ数字 Sprite
        [SerializeField] private Sprite[] allPerfectNumberSprites;

        // All Perfect 中のエフェクト数字 Sprite
        [SerializeField] private Sprite[] allPerfectEffectSprites;

        // COMBO 文字表示
        [SerializeField] private Image comboTextGraphic;

        // All Perfect 用 COMBO エフェクト文字表示
        [SerializeField] private Image apEffectComboGraphic;

        // 通常 COMBO 文字 Sprite
        [SerializeField] private Sprite comboTextSprite;

        // All Perfect 中の COMBO 文字 Sprite
        [SerializeField] private Sprite allPerfectComboTextSprite;

        // 通常コンボ数字 Image 配列
        private Image[] numberImages;

        // 拡散エフェクト数字 Image 配列
        private Image[] effectNumberImages;

        // All Perfect エフェクト数字 Image 配列
        private Image[] allPerfectEffectImages;

        // コンボ表示アニメーションの経過時間
        private float animTime;

        // 現在表示中のコンボ数
        private int currentCombo = -1;

        // 現在 All Perfect 表示か
        private bool isAllPerfect;

        // All Perfect 演出を使用するか
        private bool useAllPerfectEffect;

        // 初期スケールをキャッシュ済みか
        private bool hasCachedInitialScales;

        // 自身の初期スケール
        private Vector3 initialScale = Vector3.one;

        // 通常数字ルートの初期スケール
        private Vector3 initialNumberRootScale = Vector3.one;

        // エフェクト数字ルートの初期スケール
        private Vector3 initialEffectNumberRootScale = Vector3.one;

        // All Perfect エフェクトルートの初期スケール
        private Vector3 initialAllPerfectEffectRootScale = Vector3.one;

        // コンボ表示に必要な参照をキャッシュして初期化する
        // useAllPerfectEffect: All Perfect 演出を有効にするか
        public void Setup(bool useAllPerfectEffect)
        {
            this.useAllPerfectEffect = useAllPerfectEffect;
            CacheInitialScales();
            CacheGraphics();
            Clear();
        }

        // コンボ表示を非表示状態へ戻す
        public void Clear()
        {
            transform.localScale = Vector3.zero;
            if (numberRoot != null) numberRoot.localScale = Vector3.zero;
            if (effectNumberRoot != null) effectNumberRoot.localScale = Vector3.zero;
            if (allPerfectEffectRoot != null) allPerfectEffectRoot.localScale = Vector3.zero;

            currentCombo = 0;
            isAllPerfect = false;
        }

        // LiveScore からコンボ数と All Perfect 状態を反映する
        // score: 表示に使うスコア情報
        public void Excute(LiveScore score) => Excute(score.combo, score.IsAllPerfect);

        // コンボ数を表示し、必要なら All Perfect 用の見た目へ切り替える
        // combo: 表示するコンボ数
        // isApScore: 現在 All Perfect 条件を満たしているか
        public void Excute(int combo, bool isApScore)
        {
            if (combo == currentCombo) return;

            currentCombo = combo;
            animTime = 0f;

            if (combo <= 0)
            {
                transform.localScale = Vector3.zero;
                return;
            }

            transform.localScale = initialScale;
            isAllPerfect = useAllPerfectEffect && isApScore;

            Sprite[] targetSprites = isAllPerfect ? allPerfectNumberSprites : numberSprites;
            if (comboTextGraphic != null)
                comboTextGraphic.sprite = isAllPerfect ? allPerfectComboTextSprite : comboTextSprite;

            if (allPerfectEffectRoot != null) allPerfectEffectRoot.gameObject.SetActive(isAllPerfect);
            if (apEffectComboGraphic != null) apEffectComboGraphic.gameObject.SetActive(isAllPerfect);

            int digitCount = 0;
            int temp = combo;
            int[] digits = new int[numberImages.Length];

            while (temp > 0 && digitCount < numberImages.Length)
            {
                digits[digitCount++] = temp % 10;
                temp /= 10;
            }

            float startX = (digitCount - 1) * 0.5f * digitSpacing;

            for (int i = 0; i < numberImages.Length; i++)
            {
                bool isActive = i < digitCount;
                Vector3 scale = isActive ? Vector3.one : Vector3.zero;
                Vector2 anchoredPos = new Vector2(startX - i * digitSpacing, 0f);

                int digitVal = isActive ? digits[i] : 0;
                Sprite sprite = GetSprite(targetSprites, digitVal);

                SetGraphicState(numberImages, i, sprite, anchoredPos, scale);
                SetGraphicState(effectNumberImages, i, sprite, anchoredPos, scale);
                if (isAllPerfect)
                {
                    SetGraphicState(allPerfectEffectImages, i, GetSprite(allPerfectEffectSprites, digitVal), anchoredPos, scale);
                }
            }
        }

        // コンボ表示のスケールと All Perfect 点滅を更新する
        private void Update()
        {
            if (transform.localScale == Vector3.zero || currentCombo <= 0) return;

            float numberRate = Mathf.Clamp01(animTime / 0.1f);
            float numberScale = numberRate * 0.4f + 0.6f;
            if (numberRoot != null) numberRoot.localScale = initialNumberRootScale * numberScale;

            float effectRate = Mathf.Clamp01((animTime - 0.1f) / 0.1f);
            float effectScale = animTime >= 0.1f ? effectRate * 0.4f + 1f : 0f;
            if (effectNumberRoot != null) effectNumberRoot.localScale = initialEffectNumberRootScale * effectScale;

            Color effectColor = new Color(1f, 1f, 1f, animTime >= 0.1f ? (1f - effectRate) * 0.6f : 0f);
            for (int i = 0; i < effectNumberImages.Length; i++)
            {
                if (effectNumberImages[i] != null)
                    effectNumberImages[i].color = effectColor;
            }

            if (isAllPerfect)
            {
                if (allPerfectEffectRoot != null) allPerfectEffectRoot.localScale = initialAllPerfectEffectRootScale * numberScale;

                float alpha = (Mathf.Sin(Time.time * ApEffectSpeed) + 1f) * 0.5f;
                Color apColor = new Color(1f, 1f, 1f, alpha);

                if (allPerfectEffectImages != null)
                {
                    for (int i = 0; i < allPerfectEffectImages.Length; i++)
                    {
                        if (allPerfectEffectImages[i] != null)
                            allPerfectEffectImages[i].color = apColor;
                    }
                }
                if (apEffectComboGraphic != null) apEffectComboGraphic.color = apColor;
            }

            animTime += Time.deltaTime;
        }

        // 各ルート配下の Image をキャッシュする
        private void CacheGraphics()
        {
            numberImages = GetGraphicsFromRoot(numberRoot);
            effectNumberImages = GetGraphicsFromRoot(effectNumberRoot);
            allPerfectEffectImages = GetGraphicsFromRoot(allPerfectEffectRoot);
        }

        // アニメーションの基準にする初期スケールをキャッシュする
        private void CacheInitialScales()
        {
            if (hasCachedInitialScales)
            {
                return;
            }

            initialScale = GetInitialScale(transform);
            initialNumberRootScale = GetInitialScale(numberRoot);
            initialEffectNumberRootScale = GetInitialScale(effectNumberRoot);
            initialAllPerfectEffectRootScale = GetInitialScale(allPerfectEffectRoot);
            hasCachedInitialScales = true;
        }

        // Transform の初期スケールとして使える値を取得する
        // target: スケールを取得する Transform
        private static Vector3 GetInitialScale(Transform target)
        {
            return target != null && target.localScale != Vector3.zero ? target.localScale : Vector3.one;
        }

        // ルート配下の Image をすべて取得する
        // root: 検索対象のルート
        private Image[] GetGraphicsFromRoot(Transform root)
        {
            return root != null ? root.GetComponentsInChildren<Image>(true) : new Image[0];
        }

        // 指定桁の Image に Sprite、位置、サイズ、スケールを設定する
        // images: 対象 Image 配列
        // index: 更新する配列位置
        // sprite: 設定する Sprite
        // anchoredPos: 設定する UI 座標
        // scale: 設定するスケール
        private void SetGraphicState(Image[] images, int index, Sprite sprite, Vector2 anchoredPos, Vector3 scale)
        {
            if (images != null && index < images.Length && images[index] != null)
            {
                images[index].sprite = sprite;
                images[index].rectTransform.anchoredPosition = anchoredPos;
                images[index].rectTransform.sizeDelta = digitSize;
                images[index].transform.localScale = scale;
            }
        }

        // Sprite 配列から指定 index の Sprite を取得する
        // sprites: 候補 Sprite 配列
        // index: 取得したい Sprite 番号
        private static Sprite GetSprite(Sprite[] sprites, int index)
        {
            if (sprites == null || sprites.Length == 0) return null;
            return sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];
        }
    }
}
