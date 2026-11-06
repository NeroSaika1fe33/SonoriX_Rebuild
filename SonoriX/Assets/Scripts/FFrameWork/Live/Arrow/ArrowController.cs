using UnityEngine;
using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    public class ArrowController : MonoBehaviour
    {
        // 矢印本体の表示を更新するビュー
        [SerializeField] private SliderArrowView arrowView;

        // 現在位置を示すポインタービュー
        [SerializeField] private ArrowPointerView pointerView;

        // 判定枠をデバッグ表示するビュー
        [SerializeField] private ArrowFrameDebugView frameDebugView;

        [Header("Pointer Settings")]
        // 矢印が移動できる最小ワールド X 座標
        [SerializeField] private float minWorldX = -270.0f;

        // 矢印が移動できる最大ワールド X 座標
        [SerializeField] private float maxWorldX = 270.0f;

        // 端（|X| >= 180〜270）における基本移動感度
        [SerializeField] private float sensitivity = 3f;

        // ⚡ [调整] 中央区域衰减设置
        [Header("Center Sensitivity Settings")]
        // 衰减结束的临界 X 坐标 (180.0f)
        [SerializeField] private float centerZoneThreshold = 180.0f; 

        // 正中央 (X = 0) 时的敏感度衰减系数 (例如 0.5 表示中央敏感度削减为 50%)
        [SerializeField] private float centerSensitivityMultiplier = 0.8f; 

        // 初期化時のワールド X 座標
        [SerializeField] private float startWorldX = 0f;

        [Header("Sprite Size Settings")]
        // 初期化時の矢印表示サイズ
        [SerializeField] private float startSpriteSize = 39f;

        // 矢印操作用の入力管理
        private InputManager inputManager;
        // 入力管理をこの Controller が生成したか
        private bool ownsInputManager;

        // 矢印位置とサイズの状態計算
        private ArrowLogic arrowLogic;

        // カーソル位置リセット直後の入力差分を無視するか
        private bool ignoreNextPositionDelta;

        // 矢印ロジックが利用可能か
        public bool HasLogic => arrowLogic != null && arrowLogic.IsSetup;

        // ライブ全体で共有する入力管理を受け取る
        public void SetInputManager(InputManager inputManager)
        {
            if (this.inputManager == inputManager)
            {
                return;
            }

            DisposeOwnedInputManager();
            this.inputManager = inputManager;
            ownsInputManager = false;
            ignoreNextPositionDelta = true;
        }

        // 初期化時に入力、ロジック、ビューを準備してカーソル状態を設定する
        private void Start()
        {
            CenterMouseCursor();
            SetupArrowLogic();
            SetupArrowViews();
            SetupInputManager();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            ResetPositionToCenter();
        }

        // 毎フレーム入力差分を読み取り、矢印状態と表示を更新する
        private void Update()
        {
            if (arrowLogic == null || !arrowLogic.IsSetup)
            {
                SetupArrowLogic();
            }

            float deltaX = ReadArrowPositionDelta();
            arrowLogic.OnUpdate(deltaX);
            ApplyArrowViews();
        }

        // 矢印ロジックが未初期化なら Inspector 設定で初期化する
        private void SetupArrowLogic()
        {
            if (arrowLogic != null && arrowLogic.IsSetup)
            {
                return;
            }

            arrowLogic = new ArrowLogic();
            arrowLogic.Setup(minWorldX, maxWorldX, startWorldX, startSpriteSize);
        }

        // 矢印関連ビューを初期化する
        private void SetupArrowViews()
        {
            pointerView?.Setup();
            arrowView?.Setup();
            frameDebugView?.Setup();
        }

        // ライブ中入力を受け取る InputManager を作成する
        private void SetupInputManager()
        {
            if (inputManager != null)
            {
                return;
            }

            inputManager = new InputManager();
            ownsInputManager = true;
            inputManager.EnableInLiveInput();
        }

        // ⚡ [核心修改] 中央区域 (0 ~ 180) 衰减敏感度，180 外侧恢复完整敏感度
        private float ReadArrowPositionDelta()
        {
            if (ignoreNextPositionDelta)
            {
                ignoreNextPositionDelta = false;
                return 0f;
            }

            if (inputManager == null)
            {
                return 0f;
            }

            float rawDelta = inputManager.ArrowPositionDelta;
            float currentSensitivity = sensitivity;

            if (arrowLogic != null && arrowLogic.IsSetup)
            {
                float absX = Mathf.Abs(arrowLogic.CurrentX);

                // 在中央区域 |X| < 180 时进行平滑削弱
                if (absX < centerZoneThreshold)
                {
                    // 计算从 X = 0 (t = 0) 到 X = 180 (t = 1) 的归一化进度
                    float t = Mathf.InverseLerp(0f, centerZoneThreshold, absX);

                    // 在 centerSensitivityMultiplier (中央最迟钝) 与 1.0f (180 处恢复正常) 之间平滑插值
                    float multiplier = Mathf.Lerp(centerSensitivityMultiplier, 1.0f, t);

                    currentSensitivity *= multiplier;
                }
                // 当 |X| >= 180 时，multiplier 保持 1.0，使用完整的 sensitivity
            }

            return rawDelta * currentSensitivity;
        }

        // 現在の矢印ロジック状態を各ビューへ反映する
        private void ApplyArrowViews()
        {
            if (arrowLogic == null || !arrowLogic.IsSetup)
            {
                return;
            }

            float currentX = arrowLogic.CurrentX;
            float currentSize = arrowLogic.CurrentSize;

            pointerView?.ApplyPosition(currentX);
            arrowView?.Apply(currentX, currentSize);
            frameDebugView?.Apply(currentX, currentSize);
        }

        public void SetSizeStage(int stageIndex)
        {
            SetupArrowLogic();
            arrowLogic?.SetSizeStage(stageIndex);
            ApplyArrowViews();
        }

        public void SetSizeByLaneWidth(int laneWidth)
        {
            int stageIndex = ArrowLogic.GetStageIndexForLaneWidth(Mathf.Max(1, laneWidth));
            SetSizeStage(stageIndex);
        }

        public ArrowJudgmentFrame GetArrowJudgmentFrame()
        {
            return HasLogic ? arrowLogic.GetArrowJudgmentFrame() : default;
        }

        public void ResetForSong(int initialLaneWidth)
        {
            SetupArrowLogic();
            SetSizeByLaneWidth(initialLaneWidth);
            arrowLogic.SetAbsoluteX(0f);
            ignoreNextPositionDelta = true;
            ApplyArrowViews();
        }

        public void BeginLongSizeChange(int targetStageIndex)
        {
            SetupArrowLogic();
            arrowLogic.BeginLongSizeChange(targetStageIndex);
        }

        public void UpdateLongSizeChange(float progress)
        {
            if (!HasLogic)
            {
                return;
            }

            arrowLogic.UpdateLongSizeChange(progress);
            ApplyArrowViews();
        }

        public void FinishLongSizeChange(bool success)
        {
            if (!HasLogic)
            {
                return;
            }

            arrowLogic.FinishLongSizeChange(success);
            ApplyArrowViews();
        }

        public void ResetPositionToCenter()
        {
            SetupArrowLogic();

            arrowLogic.SetAbsoluteX(0f);
            ignoreNextPositionDelta = true;
            ApplyArrowViews();
        }

        private void CenterMouseCursor()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Mouse.current.WarpCursorPosition(screenCenter);
        }

        private void OnDestroy()
        {
            DisposeOwnedInputManager();
            inputManager = null;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void DisposeOwnedInputManager()
        {
            if (!ownsInputManager)
            {
                return;
            }

            inputManager?.Dispose();
            ownsInputManager = false;
        }
    }
}