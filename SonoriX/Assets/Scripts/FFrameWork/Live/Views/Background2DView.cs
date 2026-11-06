using FFrameWork;
using UnityEngine;

namespace FFrameWork.Live
{
    public class Background2DView : LiveViewBase
    {
        // 背景未指定時に使うデフォルト名
        private const string DefaultBackgroundName = "default";

        // original MV の楽曲 ID を 4 桁に整形するフォーマット
        private const string OriginalMovieIdFormat = "0000";

        // ムービー用マテリアルのフィルター強度プロパティ名
        private const string MovieFilterProperty = "_Filter";

        // 2D ムービーを背景テクスチャへ描画するカメラ
        [SerializeField] private Camera movieCamera;

        // ジャケット画像を背景テクスチャへ描画するカメラ
        [SerializeField] private Camera jacketCamera;

        // ジャケット表示モードのルート
        [SerializeField] private GameObject jacketModeRoot;

        // ムービー表示モードのルート
        [SerializeField] private GameObject movieModeRoot;

        // 通常背景用の SpriteRenderer
        [SerializeField] private SpriteRenderer backgroundRenderer;

        // ムービー表示用の SpriteRenderer
        [SerializeField] private SpriteRenderer movieRenderer;

        // ジャケット表示用の SpriteRenderer 配列
        [SerializeField] private SpriteRenderer[] jackets;

        // 背景未指定時に使う Sprite
        [SerializeField] private Sprite defaultBackgroundSprite;

        // ジャケット未指定時に使う Texture
        [SerializeField] private Texture2D defaultJacketTexture;

        // ムービー準備済み状態で適用する Material
        [SerializeField] private Material moviePreparedMaterial;

        // ライブ全体のデータ参照元
        private BaseLiveController baseController;

        // 読み込むムービー AssetBundle 名
        private string movieAssetBundleName;

        // 背景描画に必要な参照を初期化する
        // baseController: ライブ全体を管理するコントローラー
        public override void Setup(BaseLiveController baseController)
        {
            // this.baseController = baseController;
            //
            // MusicCategory category = GetMusicCategory();
            // // if (category == MusicCategory.original || category == MusicCategory.mv_2d)
            // // {
            // //     if (movieCamera != null)
            // //     {
            // //         movieCamera.targetTexture = baseController != null ? baseController.BackgroundTexture : null;
            // //     }
            // // }
            // // else 
            // if (jacketCamera != null)
            // {
            //     jacketCamera.targetTexture = baseController != null ? baseController.BackgroundTexture : null;
            // }
            //
            // if (jackets != null)
            // {
            //     for (int i = 0; i < jackets.Length; i++)
            //     {
            //         SpriteRenderer spriteRenderer = jackets[i];
            //         if (spriteRenderer != null)
            //         {
            //             spriteRenderer.gameObject.SetActive(false);
            //         }
            //     }
            // }
            //
            // if (movieRenderer != null)
            // {
            //     movieRenderer.gameObject.SetActive(false);
            // }
        }

        // 楽曲カテゴリに応じてジャケットまたはムービー背景を準備する
        public override void OnLoad()
        {
            MusicCategory category = GetMusicCategory();
            // if (category == MusicCategory.original || category == MusicCategory.mv_2d)
            // {
            //     SetupMovie(GetMusicData(), category);
            // }
            // else
            // {
                SetupJacket();
            //}

            ChangeLayer(gameObject, gameObject.layer, true);
        }

        // ジャケット画像を背景として表示する
        private void SetupJacket()
        {
            movieAssetBundleName = string.Empty;

            if (jacketModeRoot != null)
            {
                jacketModeRoot.SetActive(true);
            }

            if (movieModeRoot != null)
            {
                movieModeRoot.SetActive(false);
            }

            // if (backgroundRenderer != null)
            // {
            //     backgroundRenderer.sprite = defaultBackgroundSprite;
            // }

            Texture2D jacketTexture = GetJacketTexture();
            Sprite jacketSprite = null;
            if (jacketTexture != null)
            {
                jacketSprite = Sprite.Create(
                    jacketTexture,
                    new Rect(0f, 0f, jacketTexture.width, jacketTexture.height),
                    new Vector2(0.5f, 0.5f));
            }

            if (jackets != null)
            {
                for (int i = 0; i < jackets.Length; i++)
                {
                    SpriteRenderer jacket = jackets[i];
                    if (jacket == null)
                    {
                        continue;
                    }

                    if (jacketSprite != null)
                    {
                        jacket.sprite = jacketSprite;
                        jacket.gameObject.SetActive(true);
                    }
                }
            }

            if (jacketCamera != null)
            {
                jacketCamera.Render();
            }

            //gameObject.SetActive(false);
        }

        // 2D ムービー背景を準備し、失敗時はジャケット表示へ戻す
        // musicData: 楽曲データ
        // category: 背景カテゴリ
        private void SetupMovie(LiveMusicData musicData, MusicCategory category)
        {
            // if (category == MusicCategory.mv_2d)
            // {
            //     movieAssetBundleName = Get2DModeSekaiMVAssetBundleNameByMusicData(musicData);
            // }
            //
            // if (category == MusicCategory.original)
            // {
            //     int musicId = musicData != null && musicData.Music != null ? musicData.Music.id : 0;
            //     movieAssetBundleName = Get2DModeOriginalMVAssetBundleName(musicId.ToString(OriginalMovieIdFormat));
            // }

            if (movieRenderer != null)
            {
                movieRenderer.gameObject.SetActive(true);
            }

            if (TryPrepareMovie())
            {
                if (jacketModeRoot != null)
                {
                    jacketModeRoot.SetActive(false);
                }

                return;
            }

            movieAssetBundleName = string.Empty;
            if (movieRenderer != null)
            {
                //movieRenderer.gameObject.SetActive(false);
            }

            if (movieCamera != null)
            {
                //movieCamera.gameObject.SetActive(false);
            }

            if (jacketCamera != null && baseController != null)
            {
                jacketCamera.targetTexture = baseController.BackgroundTexture;
            }

            SetupJacket();
        }

        // 楽曲開始時にムービー表示用のマテリアル設定を反映する
        // time: 楽曲開始時刻
        public override void MusicStart(float time)
        {
            if (string.IsNullOrEmpty(movieAssetBundleName) || movieRenderer == null)
            {
                return;
            }

            // ムービー用マテリアルを設定する
            //Material material = movieRenderer.material;
            // if (material != null && material.HasProperty(MovieFilterProperty))
            // {
            //     material.SetFloat(MovieFilterProperty, GetLivePlayMode() == LivePlayMode.MusicVideo ? 0.95f : 0.75f);
            // }
        }

        // 背景再生を一時停止する
        // time: 一時停止した楽曲時間
        public override void Pause(float time)
        {
        }

        // 背景再生を再開する
        // time: 再開した楽曲時間
        public override void Resume(float time)
        {
        }

        // リトライ時にムービー参照状態を初期化する
        public override void Retry()
        {
            movieAssetBundleName = string.Empty;
        }

        // ライブ失敗時は背景再生を一時停止扱いにする
        // time: 失敗した楽曲時間
        public override void OnFailure(float time)
        {
            Pause(time);
        }

        // ムービー表示用の Material を適用できるかを試す
        private bool TryPrepareMovie()
        {
            if (movieRenderer == null || moviePreparedMaterial == null)
            {
                return false;
            }

            movieRenderer.sharedMaterial = moviePreparedMaterial;
            return !string.IsNullOrEmpty(movieAssetBundleName);
        }

        // 起動データから楽曲カテゴリを取得する
        private MusicCategory GetMusicCategory()
        {
            return baseController != null && baseController.BootData != null ? baseController.BootData.MusicCategory : MusicCategory.Default;
        }

        // 起動データからライブ再生モードを取得する
        private LivePlayMode GetLivePlayMode()
        {
            return baseController != null && baseController.BootData != null ? baseController.BootData.LivePlayMode : LivePlayMode.Free;
        }

        // 起動データから楽曲データを取得する
        private LiveMusicData GetMusicData()
        {
            return baseController != null && baseController.BootData != null ? baseController.BootData.MusicData : null;
        }

        // 楽曲ジャケット Texture を取得し、未指定ならデフォルトを返す
        private Texture2D GetJacketTexture()
        {
            LiveMusicData musicData = GetMusicData();
            return musicData != null && musicData.JacketTexture != null ? musicData.JacketTexture : defaultJacketTexture;
        }

        // セカイ 2D MV 用の AssetBundle 名を楽曲データから作成する
        // musicData: AssetBundle 名の基準になる楽曲データ
        private static string Get2DModeSekaiMVAssetBundleNameByMusicData(LiveMusicData musicData)
        {
            int musicId = musicData != null && musicData.Music != null ? musicData.Music.id : 0;
            return "mv_2d_" + musicId.ToString(OriginalMovieIdFormat);
        }

        // original MV 用の AssetBundle 名を楽曲 ID から作成する
        // musicId: 4 桁文字列化された楽曲 ID
        private static string Get2DModeOriginalMVAssetBundleName(string musicId)
        {
            return "original_" + musicId;
        }

        // 指定 GameObject と子階層の Layer を変更する
        // target: Layer を変更する対象
        // layer: 設定する Layer
        // recursive: 子階層にも適用するか
        private static void ChangeLayer(GameObject target, int layer, bool recursive)
        {
            if (target == null)
            {
                return;
            }

            target.layer = layer;
            if (!recursive)
            {
                return;
            }

            for (int i = 0; i < target.transform.childCount; i++)
            {
                ChangeLayer(target.transform.GetChild(i).gameObject, layer, true);
            }
        }
    }
}
