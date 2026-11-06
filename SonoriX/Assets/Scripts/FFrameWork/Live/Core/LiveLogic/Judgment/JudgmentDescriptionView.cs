using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
    // Fast、Late などの判定詳細表示を制御する View
    public class JudgmentDescriptionView : MonoBehaviour
    {
        // Fast 表示用 Sprite
        [SerializeField] private Sprite fast;
        // Late 表示用 Sprite
        [SerializeField] private Sprite late;
        // Flick Miss 表示用 Sprite
        [SerializeField] private Sprite flickMiss;
        // 判定詳細を表示する SpriteRenderer
        [SerializeField] private SpriteRenderer spriteRenderer;

        // 判定詳細ごとの表示 Sprite
        private Dictionary<NoteResultDescription, Sprite> sprites;
        // 表示開始からの経過時間
        private float time = 1f;
        // Fast/Late/Flick 表示を有効にするかどうか
        private bool isFastLateFlick = true;

        // Sprite 辞書を初期化し、初期状態では非表示にする
        private void Start()
        {
            SetupSprites();
            Hide();
        }

        // 判定詳細に対応する Sprite を表示する
        // judgeInfo: 判定結果と詳細情報
        public void Excute((NoteResult, NoteResultDescription) judgeInfo)
        {
            if (spriteRenderer == null || !isFastLateFlick || judgeInfo.Item1 < NoteResult.Bad || judgeInfo.Item1 > NoteResult.Great)
            {
                return;
            }

            if (sprites == null)
            {
                SetupSprites();
            }

            Sprite sprite = null;
            sprites.TryGetValue(judgeInfo.Item2, out sprite);
            spriteRenderer.sprite = sprite;
            spriteRenderer.transform.localScale = Vector3.zero;

            if (sprite != null)
            {
                time = 0f;
            }
        }

        // 表示中の判定詳細を拡大アニメーションし、一定時間後に隠す
        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Transform target = spriteRenderer.transform;
            if (time <= 0.3f)
            {
                float t = Mathf.Clamp01(time * 20f);
                float scale = 1f - Mathf.Pow(1f - t, 3f);
                //SizeSet
                target.localScale = new Vector3(40f,40f,1f) * scale;
                time += Time.deltaTime;
            }
            else
            {
                Hide();
            }
        }

        // 判定詳細表示を非表示にする
        private void Hide()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.transform.localScale = Vector3.zero;
        }

        // 判定詳細と Sprite の対応を設定する
        private void SetupSprites()
        {
            sprites = new Dictionary<NoteResultDescription, Sprite>
            {
                { NoteResultDescription.Fast, fast },
                { NoteResultDescription.Late, late },
            };
        }
    }
}
