using UnityEngine;
using UnityEngine.UI;

namespace FFrameWork.Live
{
    public class ComboView : MonoBehaviour
    {
        [Header("NumberView")]
        [SerializeField] private Transform numberRoot;
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField, Min(0f)] private float digitSpacing = 60f;
        [SerializeField] private Vector2 digitSize = new Vector2(64f, 80f);

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float popDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float diffusionDuration = 0.1f;
        [SerializeField, Min(1f)] private float diffusionScale = 1.4f;

        private Image[] numberImages;
        private Image[] diffusionImages;
        private Transform diffusionRoot;
        private Vector3 initialScale;
        private Vector3 initialNumberScale;
        private float animationTime;
        private int currentCombo = -1;

        public void Setup()
        {
            if (Initialize())
            {
                Clear();
            }
        }
        
        public void Setup(bool useAllPerfectEffect) => Setup();

        public void Clear()
        {
            if (!Initialize())
            {
                return;
            }

            currentCombo = 0;
            transform.localScale = Vector3.zero;
            numberRoot.localScale = initialNumberScale;
            diffusionRoot.gameObject.SetActive(false);
        }

        public void Excute(int combo)
        {
            if (!Initialize())
            {
                return;
            }

            combo = Mathf.Max(0, combo);
            if (combo == currentCombo)
            {
                return;
            }

            if (combo == 0)
            {
                Clear();
                return;
            }

            string digits = combo.ToString();
            EnsureDigitCapacity(digits.Length);

            currentCombo = combo;
            transform.localScale = initialScale;
            animationTime = 0f;
            
            float rightX = (digits.Length - 1) * digitSpacing * 0.5f;
            for (int i = 0; i < numberImages.Length; i++)
            {
                Image image = numberImages[i];
                Image diffusion = diffusionImages[i];
                bool visible = i < digits.Length;
                image.enabled = visible;
                diffusion.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                int digit = digits[digits.Length - 1 - i] - '0';
                image.sprite = numberSprites[digit];
                image.raycastTarget = false;
                image.gameObject.SetActive(true);
                image.transform.localScale = Vector3.one;
                image.rectTransform.anchoredPosition =
                    new Vector2(rightX - i * digitSpacing, 0f);
                image.rectTransform.sizeDelta = digitSize;

                diffusion.sprite = image.sprite;
                diffusion.rectTransform.anchoredPosition = image.rectTransform.anchoredPosition;
                diffusion.rectTransform.sizeDelta = digitSize;
                diffusion.transform.localScale = Vector3.one;
            }

            ApplyAnimation(0f);
        }

        public void Execute(LiveScore score) => Excute(score.combo);

        // 兼容旧调用与迁移中的 Presenter。
        public void Execute(int combo, bool isApScore) => Excute(combo);

        private bool Initialize()
        {
            if (numberImages != null)
            {
                return true;
            }

            if (numberRoot == null || numberSprites == null
                || numberSprites.Length != 10)
            {
                Debug.LogError("ComboView 需要数字根节点和按 0～9 排列的十张 Sprite。", this);
                return false;
            }

            foreach (Sprite sprite in numberSprites)
            {
                if (sprite == null)
                {
                    Debug.LogError("ComboView 的数字 Sprite 不能留空。", this);
                    return false;
                }
            }

            Image[] images = numberRoot.GetComponentsInChildren<Image>(true);
            if (images.Length == 0)
            {
                Debug.LogError("ComboView 的数字根节点下需要至少一个 Image。", this);
                return false;
            }

            initialScale = transform.localScale == Vector3.zero
                ? Vector3.one
                : transform.localScale;
            initialNumberScale = numberRoot.localScale == Vector3.zero
                ? Vector3.one
                : numberRoot.localScale;
            numberImages = images;
            
            GameObject diffusionObject = new GameObject("ComboDiffusion",
                numberRoot is RectTransform ? typeof(RectTransform) : typeof(Transform));
            diffusionRoot = diffusionObject.transform;
            diffusionRoot.SetParent(numberRoot.parent, false);
            diffusionRoot.SetSiblingIndex(numberRoot.GetSiblingIndex());
            if (numberRoot is RectTransform sourceRect)
            {
                RectTransform targetRect = (RectTransform)diffusionRoot;
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.pivot = sourceRect.pivot;
                targetRect.sizeDelta = sourceRect.sizeDelta;
            }
            diffusionRoot.localPosition = numberRoot.localPosition;
            diffusionRoot.localRotation = numberRoot.localRotation;
            diffusionRoot.localScale = initialNumberScale;
            diffusionImages = new Image[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                diffusionImages[i] = CreateDiffusionImage(images[i]);
            }
            diffusionObject.SetActive(false);
            return true;
        }

        private Image CreateDiffusionImage(Image template)
        {
            Image image = Instantiate(template, diffusionRoot);
            image.name = template.name + "Diffusion";
            image.raycastTarget = false;
            image.gameObject.SetActive(true);
            return image;
        }

        private void Update()
        {
            if (numberImages == null || currentCombo <= 0
                || animationTime >= popDuration + diffusionDuration)
            {
                return;
            }

            animationTime += Time.deltaTime;
            ApplyAnimation(animationTime);
        }

        private void ApplyAnimation(float time)
        {
            numberRoot.localScale = initialNumberScale
                * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(time / popDuration));

            float progress = Mathf.Clamp01((time - popDuration) / diffusionDuration);
            diffusionRoot.gameObject.SetActive(time >= popDuration && progress < 1f);
            diffusionRoot.localScale = initialNumberScale * Mathf.Lerp(1f, diffusionScale, progress);
            for (int i = 0; i < diffusionImages.Length; i++)
            {
                Color tint = numberImages[i].color;
                tint.a *= 0.6f * (1f - progress);
                diffusionImages[i].color = tint;
            }
        }
        
        private void EnsureDigitCapacity(int count)
        {
            int previousCount = numberImages.Length;
            if (count <= previousCount)
            {
                return;
            }

            Image template = numberImages[0];
            System.Array.Resize(ref numberImages, count);
            System.Array.Resize(ref diffusionImages, count);
            for (int i = previousCount; i < count; i++)
            {
                Image image = Instantiate(template, template.transform.parent);
                image.name = "ComboDigit" + i;
                numberImages[i] = image;
                diffusionImages[i] = CreateDiffusionImage(image);
            }
        }
    }
}
