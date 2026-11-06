using UnityEngine;
using UnityEngine.Serialization;

namespace FFrameWork.Live
{
    public partial class LiveFrontView
    {
        // ライブ前面表示のルート
        [SerializeField] private Transform liveRoot;

        // フェード表示用 SpriteRenderer
        [SerializeField] private SpriteRenderer fade;

        // レーン背景表示
        [SerializeField] private LaneView laneView;

        // 判定レーンの基準 Transform
        [SerializeField] private Transform mainJudgeLane;

        // 判定結果表示
        [SerializeField] private JudgmentView judgmentView;

        // 判定の Fast/Late などの詳細表示
        [SerializeField] private JudgmentDescriptionView judgmentDescriptionView;

        // カウントダウン表示
        [SerializeField] private CountdownView countdownView;

        // 画面エフェクト表示
        [SerializeField] private ScreenEffectView screenEffectView;

        // タップエフェクトの生成先
        [SerializeField] private Transform effectRoot;

        // エフェクト描画用カメラ
        [SerializeField] private Camera effectCamera;

        // 単発タップエフェクト Prefab
        [SerializeField] private GameObject tapSingleEffectPrefab;

        // フリックタップエフェクト Prefab
        [SerializeField] private GameObject tapFlickEffectPrefab;

        // ループタップエフェクト Prefab
        [SerializeField] private GameObject tapLoopEffectPrefab;

        // ライブ SE 再生に使うコンポーネント
        [SerializeField] private MonoBehaviour liveSoundPlayer;

        // ロングホールド中のオーラ Prefab
        [SerializeField] private GameObject longHoldAuraPrefab;

        // ロングホールド生成エフェクト Prefab
        [SerializeField] private GameObject longHoldGenPrefab;

        // ロングタップエフェクトのプール数
        [SerializeField] private int longTapEffectPoolCount = 16;

        // オートプレイ表示ラベル
        [SerializeField] private GameObject autoLabel;

        // 通常ロングノーツラインのテクスチャ
        [SerializeField] private Texture longNoteLineTexture;

        // フィクスロングノーツラインのテクスチャ
        [SerializeField] private Texture fixedLongNoteLineTexture;

        // スライダーロングラインのテクスチャ
        [SerializeField] private Texture sliderLongLineTexture;
        
        // ボリュームダウンロングラインのテクスチャ
        [SerializeField] private Texture volumeLongDownLineTexture;

        // ボリュームアップロングラインのテクスチャ
        [SerializeField] private Texture volumeLongUpLineTexture;

        // ガイドラインのテクスチャ
        [SerializeField] private Texture guideLineTexture;

        // 通常ロングノーツラインのマテリアル
        [SerializeField] private Material longNoteLineMaterial;

        // フィクスロングノーツラインのマテリアル
        [SerializeField] private Material fixedLongNoteLineMaterial;

        // ガイドラインのマテリアル
        [SerializeField] private Material guideLineMaterial;

        // スライダーロングラインのマテリアル
        [SerializeField] private Material sliderLongLineMaterial;

        // ボリュームダウンロングラインのマテリアル
        [SerializeField] private Material volumeLongDownLineMaterial;

        // ボリュームアップロングラインのマテリアル
        [SerializeField] private Material volumeLongUpLineMaterial;

        // コンボ表示
        [SerializeField] private ComboView comboView;

        // スコア表示
        [SerializeField] private ScoreView scoreView;

        //クリアゲージ表示
        [SerializeField] private ClearGaugeView clearGaugeView;

        // タップエフェクト種別ごとの Prefab 設定
        [SerializeField] private TapEffectView.EffectPrefab[] tapEffectPrefabs;

        // ノーツ種別ごとの Prefab 設定
        [SerializeField] private NotePrefabSet[] notePrefabs;
    }
}
