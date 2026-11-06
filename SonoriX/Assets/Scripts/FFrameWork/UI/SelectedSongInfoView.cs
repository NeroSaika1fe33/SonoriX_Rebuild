using System.Collections.Generic;
using FFrameWork.Songs;
using UnityEngine;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 選択中楽曲のジャケット、曲名、難易度などを表示する
    public class SelectedSongInfoView : MonoBehaviour
    {
        // 選択楽曲を提供する画面遷移 Controller
        [SerializeField] private LivePanelFlowController flowController;
        [SerializeField] private RawImage jacketRawImage;
        [SerializeField] private Image jacketImage;
        [SerializeField] private Text musicIdText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text artistText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text playLevelText;
        [SerializeField] private string emptyText = "";

        // 選択楽曲変更イベントを購読中か
        private bool isSubscribed;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // Controller を解決して現在選択中の曲情報を反映する
        public void Initialize(LivePanelFlowController controller = null)
        {
            if (controller != null && flowController != controller)
            {
                Unsubscribe();
                flowController = controller;
            }

            EnsureFlowController();
            Subscribe();
            Refresh(flowController != null ? flowController.SelectedSong : null);
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

        // 選択楽曲変更イベントを購読する
        private void Subscribe()
        {
            if (flowController != null && !isSubscribed)
            {
                flowController.SelectedSongChanged += Refresh;
                isSubscribed = true;
            }
        }

        // 選択楽曲変更イベントの購読を解除する
        private void Unsubscribe()
        {
            if (flowController != null && isSubscribed)
            {
                flowController.SelectedSongChanged -= Refresh;
                isSubscribed = false;
            }
        }

        // 指定楽曲の情報を表示へ反映する
        public void Refresh(SongSelectEntry song)
        {
            SetJacket(song != null ? song.Jacket : null);
            SetText(musicIdText, song != null ? song.MusicId.ToString() : emptyText);
            SetText(titleText, song != null ? song.Title : emptyText);
            SetText(artistText, song != null ? "By　" + GetArtist(song) : emptyText);
            SetText(difficultyText, song != null ? song.Difficulty.ToString() : emptyText);
            SetText(playLevelText, song != null ? "Lv."+song.PlayLevel.ToString() : emptyText);
        }

        // Artist が空なら Composer を表示名として使う
        private static string GetArtist(SongSelectEntry song)
        {
            return string.IsNullOrEmpty(song.Artist) ? song.Composer : song.Artist;
        }

        // Text が存在する場合だけ文字列を設定する
        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        // RawImage または Image へジャケット画像を反映する
        private void SetJacket(Texture2D jacket)
        {
            if (jacketRawImage != null)
            {
                jacketRawImage.texture = jacket;
                jacketRawImage.color = jacket != null ? Color.white : Color.clear;
            }

            if (jacketImage != null)
            {
                jacketImage.sprite = SpriteCache.GetSprite(jacket);
                jacketImage.color = jacket != null ? Color.white : Color.clear;
            }
        }

        // Texture2D から作った Sprite を再利用する
        private static class SpriteCache
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
