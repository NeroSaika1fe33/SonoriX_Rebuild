using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;
using UnityEngine.Serialization;

namespace FFrameWork
{
    public class JudgmentView : MonoBehaviour
    {
        // Miss 判定表示用 Sprite
        [SerializeField] private Sprite miss;

        // Bad 判定表示用 Sprite
        [SerializeField] private Sprite bad;

        // Good 判定表示用 Sprite
        [SerializeField] private Sprite good;

        // Great 判定表示用 Sprite
        [SerializeField] private Sprite great;

        // Perfect 判定表示用 Sprite
        [SerializeField] private Sprite perfect;

        // JustPerfect 判定表示用 Sprite
        [SerializeField] private Sprite justPerfect;

        // Auto 判定表示用 Sprite
        [SerializeField] private Sprite auto;

        // 判定結果を表示する SpriteRenderer
        [SerializeField] private SpriteRenderer spriteRenderer;

        // 非表示時に退避させる座標
        private readonly Vector3 farPos = new Vector3(100000f, 100000f, 0f);

        // 判定結果から表示 Sprite を引く辞書
        private Dictionary<NoteResult, Sprite> sprites;

        // 表示開始からの経過時間
        private float time = 1f;

        // Sprite 辞書を作成し、初期状態では非表示にする
        private void Start()
        {
            SetupSprites();
            Hide();
        }

        // 判定結果に対応する Sprite を表示する
        // judgeInfo: 判定結果と詳細情報の組
        public void Excute((NoteResult, NoteResultDescription) judgeInfo)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (sprites == null)
            {
                SetupSprites();
            }

            if (!sprites.TryGetValue(judgeInfo.Item1, out Sprite sprite) || sprite == null)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            time = 0f;
            spriteRenderer.transform.localPosition = farPos;
        }

        // 判定表示の拡大アニメーションと非表示切り替えを更新する
        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Transform target = spriteRenderer.transform;
            if (time <= 0.3f)
            {
                if ((Time.frameCount & 1) == 0 || time == 0f)
                {
                    if (time == 0f)
                    {
                        target.localPosition = Vector3.zero;
                    }

                    float t = Mathf.Clamp01(time * 20f);
                    float scale = 1f - Mathf.Pow(1f - t, 3f);
                    target.localScale = new Vector3(40f,40f,1f) * scale;
                }

                time += Time.deltaTime;
            }
            else
            {
                Hide();
            }
        }

        // 判定表示を画面外へ退避させる
        private void Hide()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.transform.localPosition = farPos;
        }

        // 判定結果と Sprite の対応表を作成する
        private void SetupSprites()
        {
            sprites = new Dictionary<NoteResult, Sprite>
            {
                { NoteResult.Miss, miss },
                { NoteResult.Bad, bad },
                { NoteResult.Good, good },
                { NoteResult.Great, great },
                { NoteResult.Perfect, perfect },
                { NoteResult.JustPerfect, perfect },
                { NoteResult.Auto, auto }
            };
        }
    }
}
