using UnityEngine;

namespace FFrameWork.Live
{
    [ExecuteInEditMode]
    public class NumberView : MonoBehaviour
    {
        public enum Align
        {
            // 右揃えで桁を並べる
            Right = 0,

            // 左揃えで桁を並べる
            Left = 1
        }

        // 数字の並び方向
        [SerializeField] private Align align;

        // 桁ごとの表示間隔
        [SerializeField] private float numberSpacing;

        // 使用中の桁に適用する色
        [SerializeField] private Color defaultColor = Color.white;

        // 未使用桁に適用する色
        [SerializeField] private Color surplusColor = Color.clear;

        // 正数表示時のプラスアイコン Sprite
        [SerializeField] private Sprite plusIconSprite;

        // 負数表示時のマイナスアイコン Sprite
        [SerializeField] private Sprite minusiconSprite;

        // 0 から 9 の数字 Sprite
        [SerializeField] private Sprite[] numberSprites;

        private struct SpriteStatus
        {
            // 符号アイコンを示す内部 ID
            public const int IconSpriteId = -1;

            // 現在表示している数字またはアイコン ID
            public int id;

            // 未使用桁として扱うか
            public bool isSurplus;

            // 色の再設定が必要か
            public bool isColorDirty;
        }

        // 桁表示に使う SpriteRenderer 配列
        private SpriteRenderer[] numberSpriteRenderers;

        // 符号アイコンを除いた最大桁数
        private int maxDigitCount;

        // 表示値を桁ごとに分解した値
        private int[] digitValues;

        // 各 SpriteRenderer の表示状態
        private SpriteStatus[] spriteStatus;

        // 全桁へ掛けるアルファ値
        private float alpha = 1f;

        // 符号アイコンを表示可能か
        private bool hasIcon;

        // 基準 X 座標
        private float x;

        // 1 桁表示時の基準位置
        private Vector3 pos0;

        // 2 桁表示時の基準位置
        private Vector3 pos1;

        // 3 桁表示時の基準位置
        private Vector3 pos2;

        // 4 桁表示時の基準位置
        private Vector3 pos3;

        // 5 桁表示時の基準位置
        private Vector3 pos4;

        // 起動時に桁数ごとの基準位置をキャッシュする
        private void Awake()
        {
            CacheBasePositions();
        }

        // HTML カラー文字列で数字表示色を初期化する
        // defaultColorHtml: 使用中の桁色
        // surplusColorHtml: 未使用桁色
        public void Setup(string defaultColorHtml, string surplusColorHtml = "#ffffff00")
        {
            Setup(ColorUtility.Create(defaultColorHtml), ColorUtility.Create(surplusColorHtml));
        }

        // Color 値で数字表示色と桁 SpriteRenderer を初期化する
        // defaultColor: 使用中の桁色
        // surplusColor: 未使用桁色
        public void Setup(Color defaultColor, Color surplusColor)
        {
            this.defaultColor = defaultColor;
            this.surplusColor = surplusColor;
            alpha = 1f;
            hasIcon = plusIconSprite != null || minusiconSprite != null;
            CacheBasePositions();
            CacheRenderers();

            for (int i = 0; i < numberSpriteRenderers.Length; i++)
            {
                int positionIndex = align == Align.Left ? i : -i;
                numberSpriteRenderers[i].transform.localPosition = Vector3.right * numberSpacing * positionIndex;
                numberSpriteRenderers[i].sprite = GetNumberSprite(0);
                spriteStatus[i].id = 0;
                spriteStatus[i].isSurplus = true;
                spriteStatus[i].isColorDirty = true;
            }

            maxDigitCount = numberSpriteRenderers.Length - (hasIcon ? 1 : 0);
            UpdateColor();
        }

        // 新しい通常色を設定し、既存桁へ反映する
        // defaultColor: 使用中の桁色
        public void SetupNew(Color defaultColor)
        {
            CacheRenderers();
            this.defaultColor = defaultColor;
            for (int i = 0; i < spriteStatus.Length; i++)
            {
                spriteStatus[i].isColorDirty = true;
            }
            UpdateColor();
        }

        // 指定値を桁 Sprite と符号アイコンへ反映する
        // value: 表示する数値
        public void UpdateNumber(int value)
        {
            CacheRenderers();
            if (numberSpriteRenderers.Length == 0)
            {
                return;
            }

            int digitCount = CalculateDigit(value);
            if (hasIcon)
            {
                int iconIndex = align == Align.Left ? 0 : digitCount;
                if (iconIndex >= 0 && iconIndex < numberSpriteRenderers.Length)
                {
                    Sprite icon = null;
                    if (value > 0)
                    {
                        icon = plusIconSprite;
                    }
                    else if (value < 0)
                    {
                        icon = minusiconSprite;
                    }

                    numberSpriteRenderers[iconIndex].sprite = icon;
                    spriteStatus[iconIndex].id = value != 0 ? SpriteStatus.IconSpriteId : 0;
                    if (spriteStatus[iconIndex].isSurplus)
                    {
                        spriteStatus[iconIndex].isSurplus = false;
                        spriteStatus[iconIndex].isColorDirty = true;
                    }
                }
            }

            int digitStart = align == Align.Left && hasIcon && value != 0 ? 1 : 0;
            for (int i = 0; i < digitCount; i++)
            {
                int valueIndex = align == Align.Left ? digitCount - i - 1 : i;
                int rendererIndex = digitStart + i;
                if (rendererIndex < 0 || rendererIndex >= numberSpriteRenderers.Length)
                {
                    continue;
                }

                int digit = digitValues[valueIndex];
                if (spriteStatus[rendererIndex].id != digit)
                {
                    numberSpriteRenderers[rendererIndex].sprite = GetNumberSprite(digit);
                    spriteStatus[rendererIndex].id = digit;
                }
                if (spriteStatus[rendererIndex].isSurplus)
                {
                    spriteStatus[rendererIndex].isSurplus = false;
                    spriteStatus[rendererIndex].isColorDirty = true;
                }
            }

            int usedCount = digitCount + (hasIcon && value != 0 ? 1 : 0);
            for (int i = usedCount; i < numberSpriteRenderers.Length; i++)
            {
                if (spriteStatus[i].id != 0)
                {
                    numberSpriteRenderers[i].sprite = GetNumberSprite(0);
                    spriteStatus[i].id = 0;
                }
                if (!spriteStatus[i].isSurplus)
                {
                    spriteStatus[i].isSurplus = true;
                    spriteStatus[i].isColorDirty = true;
                }
            }

            UpdateColor();
        }

        // 桁数に応じた位置調整込みで数値を更新する
        // value: 表示する数値
        public void UpdateNumberNew(int value)
        {
            CacheRenderers();
            int digitCount = CalculateDigit(value);
            hasIcon = plusIconSprite != null || minusiconSprite != null;
            if (value == 0 && numberSpriteRenderers.Length > 0)
            {
                hasIcon = false;
                spriteStatus[0].id = 0;
                spriteStatus[0].isSurplus = false;
                spriteStatus[0].isColorDirty = true;
                digitValues[0] = 0;
                numberSpriteRenderers[0].sprite = GetNumberSprite(0);
            }

            UpdateNumber(value);
            UpdatePosition(value == 0 ? digitCount : digitCount + 1);
        }

        // 数字表示全体のアルファ値を更新する
        // alpha: 0 から 1 のアルファ値
        public void UpdateAlpha(float alpha)
        {
            CacheRenderers();
            float value = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(this.alpha, value))
            {
                return;
            }

            this.alpha = value;
            for (int i = 0; i < spriteStatus.Length; i++)
            {
                spriteStatus[i].isColorDirty = true;
            }
            UpdateColor();
        }

        // 桁数ごとの表示基準位置をキャッシュする
        private void CacheBasePositions()
        {
            x = transform.localPosition.x;
            Vector3 position = transform.localPosition;
            pos0 = position;
            pos1 = new Vector3(x - 0.13f, position.y, position.z);
            pos2 = new Vector3(x - 0.26f, position.y, position.z);
            pos3 = new Vector3(x - 0.39f, position.y, position.z);
            pos4 = new Vector3(x - 0.52f, position.y, position.z);
        }

        // 子階層の SpriteRenderer と表示状態配列をキャッシュする
        private void CacheRenderers()
        {
            if (numberSpriteRenderers != null && spriteStatus != null && digitValues != null)
            {
                return;
            }

            numberSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            spriteStatus = new SpriteStatus[numberSpriteRenderers.Length];
            digitValues = new int[numberSpriteRenderers.Length];
            maxDigitCount = numberSpriteRenderers.Length - (hasIcon ? 1 : 0);
        }

        // 表示桁数に応じて NumberView 自体の位置を調整する
        // count: 表示に使う桁数
        private void UpdatePosition(int count)
        {
            switch (count)
            {
                case 1:
                    transform.localPosition = pos0;
                    break;
                case 2:
                    transform.localPosition = pos1;
                    break;
                case 3:
                    transform.localPosition = pos2;
                    break;
                case 4:
                    transform.localPosition = pos3;
                    break;
                case 5:
                    transform.localPosition = pos4;
                    break;
                default:
                    Vector3 position = transform.localPosition;
                    position.x = x + (count - 1) * -0.13f;
                    transform.localPosition = position;
                    break;
            }
        }

        // Dirty の付いた桁だけ色を反映する
        private void UpdateColor()
        {
            if (numberSpriteRenderers == null || spriteStatus == null)
            {
                return;
            }

            for (int i = 0; i < numberSpriteRenderers.Length; i++)
            {
                if (numberSpriteRenderers[i] == null || !spriteStatus[i].isColorDirty)
                {
                    continue;
                }

                Color color = spriteStatus[i].isSurplus ? surplusColor : defaultColor;
                color.a *= alpha;
                numberSpriteRenderers[i].color = color;
                spriteStatus[i].isColorDirty = false;
            }
        }

        // 表示値の桁数を計算し、digitValues に各桁を格納する
        // value: 表示する数値
        private int CalculateDigit(int value)
        {
            if (digitValues == null || digitValues.Length == 0)
            {
                return 0;
            }
            if (value == 0)
            {
                digitValues[0] = 0;
                return 1;
            }

            int number = Mathf.Abs(value);
            int digit = 1;
            int threshold = 10;
            while (number >= threshold && threshold <= int.MaxValue / 10)
            {
                digit++;
                threshold *= 10;
            }

            if (digit > maxDigitCount)
            {
                do
                {
                    threshold /= 10;
                    digit--;
                    number %= Mathf.Max(1, threshold);
                }
                while (digit > maxDigitCount);
            }

            for (int i = digit - 1; i >= 0; i--)
            {
                threshold /= 10;
                int divisor = Mathf.Max(1, threshold);
                digitValues[i] = number / divisor;
                number -= digitValues[i] * divisor;
            }
            return digit;
        }

        // 数値に対応する Sprite を取得する
        // value: 0 から 9 の数値
        private Sprite GetNumberSprite(int value)
        {
            if (numberSprites == null || numberSprites.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(value, 0, numberSprites.Length - 1);
            return numberSprites[index];
        }
    }
}
