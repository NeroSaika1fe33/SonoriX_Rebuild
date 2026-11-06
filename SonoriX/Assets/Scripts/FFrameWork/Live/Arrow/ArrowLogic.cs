using UnityEngine;

namespace FFrameWork.Live
{
    public struct SliderArrowModel
    {
        // 現在の矢印 X 座標
        public float CurrentX;

        // 移動できる最小 X 座標
        public float MinX;

        // 移動できる最大 X 座標
        public float MaxX;
    }

    public struct SliderArrowSizeModel
    {
        // 現在の矢印サイズ
        public float CurrentSize;

        // 設定できる最小サイズ
        public float MinSize;

        // 設定できる最大サイズ
        public float MaxSize;
    }

    public struct ArrowJudgmentFrame
    {
        // 矢印の位置状態
        public SliderArrowModel Position;

        // 矢印のサイズ状態
        public SliderArrowSizeModel Size;

        // 選択可能なサイズ段階
        public float[] SizeStages;

        // 現在選ばれているサイズ段階
        public int CurrentSizeStageIndex;

        // ロングノーツ中のサイズ変更処理中か
        public bool IsLongSizeChanging;

        // ロングサイズ変更開始時のサイズ
        public float LongStartSize;

        // ロングサイズ変更の目標サイズ
        public float LongTargetSize;

        // ロングサイズ変更開始時の段階
        public int LongStartStageIndex;

        // ロングサイズ変更の目標段階
        public int LongTargetStageIndex;
    }

    public class ArrowLogic
    {
        // 矢印サイズ段階の数
        public const int SizeStageCount = 9;

        // デフォルト 1 レーン半幅を算出するための基準スケール
        private const float DefaultHalfWidthScale = 19.5f;

        // 基準スケールが何レーン分の半幅かを表す倍率
        private const float DefaultHalfWidthMultiplier = 0.5f;

        // レーン幅ごとのサイズ倍率
        private static readonly float[] DefaultWidthMultipliers = { 0.5f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };

        // 現在の矢印判定枠状態
        private ArrowJudgmentFrame judgmentFrame;

        // ロジックが初期化済みか
        public bool IsSetup { get; private set; }

        // 現在の矢印 X 座標
        public float CurrentX => judgmentFrame.Position.CurrentX;

        // 現在の矢印サイズ
        public float CurrentSize => judgmentFrame.Size.CurrentSize;

        // 現在のサイズ段階
        public int CurrentSizeStageIndex => judgmentFrame.CurrentSizeStageIndex;

        // 矢印の移動範囲、初期位置、初期サイズを指定して初期化する
        // minX: 移動できる最小 X 座標
        // maxX: 移動できる最大 X 座標
        // startX: 初期 X 座標
        // startSize: 初期サイズ
        public void Setup(float minX, float maxX, float startX, float startSize)
        {
            judgmentFrame = CreateArrowJudgmentFrame(minX, maxX, startX, startSize);
            IsSetup = true;
        }

        // 入力差分から矢印位置を更新し、現在サイズに最も近い段階を再計算する
        // positionDeltaX: X 座標へ加算する入力差分
        // sizeDeltaY: 互換用のサイズ差分現状は使用しない
        public void OnUpdate(float positionDeltaX, float sizeDeltaY = 0f)
        {
            if (!IsSetup)
            {
                return;
            }

            UpdatePositionByDelta(ref judgmentFrame.Position, positionDeltaX);

            judgmentFrame.CurrentSizeStageIndex = FindNearestSizeStage(judgmentFrame.SizeStages, judgmentFrame.Size.CurrentSize);
        }

        // 矢印の X 座標を絶対値で設定する
        // rawX: 設定したい X 座標
        public void SetAbsoluteX(float rawX)
        {
            if (!IsSetup)
            {
                return;
            }

            SetAbsoluteX(ref judgmentFrame.Position, rawX);
        }

        // 矢印サイズを指定段階に切り替える
        // stageIndex: 適用するサイズ段階
        public void SetSizeStage(int stageIndex)
        {
            if (!IsSetup)
            {
                return;
            }

            SetFrameSizeStage(ref judgmentFrame, stageIndex);
        }

        // 指定サイズに最も近い段階へ矢印サイズを切り替える
        // size: 基準にするサイズ
        public void SetNearestSizeStage(float size)
        {
            if (!IsSetup)
            {
                return;
            }

            SetFrameSizeStage(ref judgmentFrame, FindNearestSizeStage(judgmentFrame.SizeStages, size));
        }

        // ロングノーツ中のサイズ変更を開始し、開始値と目標値を記録する
        // targetStageIndex: 変更先のサイズ段階
        public void BeginLongSizeChange(int targetStageIndex)
        {
            if (!IsSetup)
            {
                return;
            }

            int clampedTargetStageIndex = ClampStageIndex(targetStageIndex);
            judgmentFrame.IsLongSizeChanging = true;
            judgmentFrame.LongStartSize = judgmentFrame.Size.CurrentSize;
            judgmentFrame.LongTargetSize = judgmentFrame.SizeStages[clampedTargetStageIndex];
            judgmentFrame.LongStartStageIndex = FindNearestSizeStage(judgmentFrame.SizeStages, judgmentFrame.LongStartSize);
            judgmentFrame.LongTargetStageIndex = clampedTargetStageIndex;
        }

        // ロングノーツ中のサイズ変更進捗から現在サイズを補間する
        // progress: 0 から 1 の進捗率
        public void UpdateLongSizeChange(float progress)
        {
            if (!IsSetup || !judgmentFrame.IsLongSizeChanging)
            {
                return;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            float size = Mathf.Lerp(judgmentFrame.LongStartSize, judgmentFrame.LongTargetSize, clampedProgress);
            SetSize(ref judgmentFrame.Size, size);
        }

        // ロングノーツ中のサイズ変更を確定し、失敗時は現在サイズに近い途中段階へ戻す
        // success: 成功時は目標段階、失敗時は途中段階へ確定する
        public void FinishLongSizeChange(bool success)
        {
            if (!IsSetup || !judgmentFrame.IsLongSizeChanging)
            {
                return;
            }

            int stageIndex = success
                ? judgmentFrame.LongTargetStageIndex
                : FindNearestLongFallbackStage(judgmentFrame);
            SetFrameSizeStage(ref judgmentFrame, stageIndex);
            judgmentFrame.IsLongSizeChanging = false;
        }

        // 初期値から矢印判定枠状態を作成する
        // minX: 移動できる最小 X 座標
        // maxX: 移動できる最大 X 座標
        // startX: 初期 X 座標
        // startSize: 初期サイズ
        public static ArrowJudgmentFrame CreateArrowJudgmentFrame(float minX, float maxX, float startX, float startSize)
        {
            float[] sizeStages = CreateDefaultSizeStages();
            int stageIndex = FindNearestSizeStage(sizeStages, startSize);
            float stageSize = sizeStages[stageIndex];
            return new ArrowJudgmentFrame
            {
                Position = CreateSliderArrowModel(minX, maxX, startX),
                Size = CreateSliderArrowSizeModel(sizeStages[0], sizeStages[SizeStageCount - 1], stageSize),
                SizeStages = sizeStages,
                CurrentSizeStageIndex = stageIndex,
                IsLongSizeChanging = false,
                LongStartSize = stageSize,
                LongTargetSize = stageSize,
                LongStartStageIndex = stageIndex,
                LongTargetStageIndex = stageIndex
            };
        }

        // デフォルトのサイズ段階配列を作成する
        public static float[] CreateDefaultSizeStages()
        {
            float[] stages = new float[SizeStageCount];
            float oneWidthScale = DefaultHalfWidthScale / DefaultHalfWidthMultiplier;
            for (int i = 0; i < SizeStageCount; i++)
            {
                stages[i] = DefaultWidthMultipliers[i] * oneWidthScale;
            }
            return stages;
        }

        // レーン幅から最も近い矢印サイズ段階を取得する
        // laneWidth: 判定対象のレーン幅
        public static int GetStageIndexForLaneWidth(float laneWidth)
        {
            float widthMultiplier = Mathf.Clamp(laneWidth * 0.5f, DefaultWidthMultipliers[0], DefaultWidthMultipliers[SizeStageCount - 1]);
            int nearestIndex = 0;
            float nearestDistance = Mathf.Abs(DefaultWidthMultipliers[0] - widthMultiplier);
            for (int i = 1; i < SizeStageCount; i++)
            {
                float distance = Mathf.Abs(DefaultWidthMultipliers[i] - widthMultiplier);
                if (distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = distance;
                }
            }
            return nearestIndex;
        }

        // 矢印位置モデルを作成する
        // minX: 移動できる最小 X 座標
        // maxX: 移動できる最大 X 座標
        // startX: 初期 X 座標
        public static SliderArrowModel CreateSliderArrowModel(float minX, float maxX, float startX = 0f)
        {
            return new SliderArrowModel
            {
                MinX = minX,
                MaxX = maxX,
                CurrentX = Mathf.Clamp(startX, minX, maxX)
            };
        }

        // 入力差分を加算して矢印位置を更新する
        // model: 更新対象の位置モデル
        // deltaX: X 座標へ加算する差分
        public static void UpdatePositionByDelta(ref SliderArrowModel model, float deltaX)
        {
            model.CurrentX = Mathf.Clamp(model.CurrentX + deltaX, model.MinX, model.MaxX);
        }

        // 矢印位置を絶対座標で設定する
        // model: 更新対象の位置モデル
        // rawX: 設定したい X 座標
        public static void SetAbsoluteX(ref SliderArrowModel model, float rawX)
        {
            model.CurrentX = Mathf.Clamp(rawX, model.MinX, model.MaxX);
        }

        // 矢印サイズモデルを作成する
        // minSize: 設定できる最小サイズ
        // maxSize: 設定できる最大サイズ
        // startSize: 初期サイズ
        public static SliderArrowSizeModel CreateSliderArrowSizeModel(float minSize, float maxSize, float startSize = 1f)
        {
            return new SliderArrowSizeModel
            {
                MinSize = minSize,
                MaxSize = maxSize,
                CurrentSize = Mathf.Clamp(startSize, minSize, maxSize)
            };
        }

        // 入力差分を加算して矢印サイズを更新する
        // model: 更新対象のサイズモデル
        // deltaSize: サイズへ加算する差分
        public static void UpdateSizeByDelta(ref SliderArrowSizeModel model, float deltaSize)
        {
            model.CurrentSize = Mathf.Clamp(model.CurrentSize + deltaSize, model.MinSize, model.MaxSize);
        }

        // 矢印サイズを絶対値で設定する
        // model: 更新対象のサイズモデル
        // size: 設定したいサイズ
        public static void SetSize(ref SliderArrowSizeModel model, float size)
        {
            model.CurrentSize = Mathf.Clamp(size, model.MinSize, model.MaxSize);
        }

        // 判定枠全体のサイズ段階を設定する
        // frame: 更新対象の判定枠
        // stageIndex: 適用するサイズ段階
        public static void SetFrameSizeStage(ref ArrowJudgmentFrame frame, int stageIndex)
        {
            int clampedStageIndex = ClampStageIndex(stageIndex);
            SetSize(ref frame.Size, frame.SizeStages[clampedStageIndex]);
            frame.CurrentSizeStageIndex = clampedStageIndex;
            frame.IsLongSizeChanging = false;
        }

        // サイズ配列から指定サイズに最も近い段階を探す
        // sizeStages: 候補になるサイズ段階配列
        // size: 比較対象のサイズ
        public static int FindNearestSizeStage(float[] sizeStages, float size)
        {
            if (sizeStages == null || sizeStages.Length == 0)
            {
                return 0;
            }

            int nearestIndex = 0;
            float nearestDistance = Mathf.Abs(sizeStages[0] - size);
            for (int i = 1; i < sizeStages.Length; i++)
            {
                float distance = Mathf.Abs(sizeStages[i] - size);
                if (distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = distance;
                }
            }
            return nearestIndex;
        }

        // ロングサイズ変更失敗時に戻す最も近い途中段階を探す
        // frame: ロングサイズ変更中の判定枠
        private static int FindNearestLongFallbackStage(ArrowJudgmentFrame frame)
        {
            int start = frame.LongStartStageIndex;
            int target = frame.LongTargetStageIndex;
            if (start == target)
            {
                return start;
            }

            int direction = target > start ? 1 : -1;
            int fallbackIndex = start;
            float nearestDistance = Mathf.Abs(frame.SizeStages[start] - frame.Size.CurrentSize);
            for (int i = start; i != target; i += direction)
            {
                float distance = Mathf.Abs(frame.SizeStages[i] - frame.Size.CurrentSize);
                if (distance < nearestDistance)
                {
                    fallbackIndex = i;
                    nearestDistance = distance;
                }
            }
            return fallbackIndex;
        }

        // サイズ段階を有効範囲へ丸める
        // stageIndex: 丸めるサイズ段階
        private static int ClampStageIndex(int stageIndex)
        {
            return Mathf.Clamp(stageIndex, 0, SizeStageCount - 1);
        }

        // 現在の矢印判定枠状態を取得する
        public ArrowJudgmentFrame GetArrowJudgmentFrame()
        {
            return judgmentFrame;
        }
    }
}
