using System;
using UnityEngine;

namespace FFrameWork.Live
{
    internal sealed class LiveInputCollector
    {
        private static readonly int[] FixedLaneTapEffectLanes = { 4, 6, 8, 10 };

        private const int FreeLaneInputIdBase = 1000;
        private const int ArrowInputIdBase = 2000;
        private const int ArrowVolumeInputIdBase = 3000;

        public void CollectFixedLaneInputs(InputManager inputManager, float musicTime, Action<int, LiveInput> addInput)
        {
            if (inputManager == null || addInput == null) return;

            for (int i = 0; i < FixedLaneTapEffectLanes.Length; i++)
            {
                if (!inputManager.TryGetFixedLaneInput(i, musicTime, out LiveInput input))
                {
                    continue;
                }

                AddFixedLaneInput(FixedLaneTapEffectLanes[i], ref input, addInput);
            }
        }

        public void CollectManualInputs(
            InputManager inputManager,
            ArrowController arrowController,
            float musicTime,
            Action<int, LiveInput> addInput)
        {
            if (inputManager == null || addInput == null) return;

            CollectFixedLaneInputs(inputManager, musicTime, addInput);

            if (arrowController == null || !arrowController.HasLogic)
            {
                return;
            }

            ArrowJudgmentFrame frame = arrowController.GetArrowJudgmentFrame();
            float frameLeft = frame.Position.CurrentX - frame.Size.CurrentSize;
            float frameRight = frame.Position.CurrentX + frame.Size.CurrentSize;

            CollectFreeLaneInput(inputManager, musicTime, frameLeft, frameRight, addInput);
            CollectSliderFlickInput(inputManager, musicTime, frameLeft, frameRight, addInput);
            CollectArrowVolumeFlickInput(inputManager, musicTime, frameLeft, frameRight, addInput);
        }

        private void CollectFreeLaneInput(
            InputManager inputManager,
            float musicTime,
            float frameLeft,
            float frameRight,
            Action<int, LiveInput> addInput)
        {
            if (!inputManager.TryGetFreeLaneInput(musicTime, out LiveInput input))
            {
                return;
            }

            int laneCount = GetJudgmentLaneCount();
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (!IsLaneOverlappingFrame(lane, frameLeft, frameRight)) continue;

                LiveInput laneInput = input;
                AddFreeLaneInput(lane, frameLeft, frameRight, ref laneInput, addInput);
            }
        }

        private void CollectArrowVolumeFlickInput(
            InputManager inputManager,
            float musicTime,
            float frameLeft,
            float frameRight,
            Action<int, LiveInput> addInput)
        {
            float sizeDelta = inputManager.ArrowSizeDelta;
            if (Mathf.Approximately(sizeDelta, 0f)) return;

            int laneCount = GetJudgmentLaneCount();
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (!IsLaneOverlappingFrame(lane, frameLeft, frameRight)) continue;
                AddArrowVolumeFlickInput(lane, frameLeft, frameRight, sizeDelta, musicTime, addInput);
            }
        }

        private void CollectSliderFlickInput(
            InputManager inputManager,
            float musicTime,
            float frameLeft,
            float frameRight,
            Action<int, LiveInput> addInput)
        {
            float positionDelta = inputManager.ArrowPositionDelta;
            if (Mathf.Approximately(positionDelta, 0f)) return;

            int laneCount = GetJudgmentLaneCount();
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (!IsLaneOverlappingFrame(lane, frameLeft, frameRight)) continue;
                AddSliderFlickInput(lane, frameLeft, frameRight, positionDelta, musicTime, addInput);
            }
        }

        private void AddFixedLaneInput(int lane, ref LiveInput input, Action<int, LiveInput> addInput)
        {
            input.lane = lane;
            input.laneStart = lane;
            input.laneEnd = lane + 1f;
            input.width = 2f;
            input.worldPosition = GetLaneWorldPosition(lane);
            input.source = LiveInputSource.FixedLane;
            addInput(lane, input);
        }

        private void AddFreeLaneInput(
            int lane,
            float frameLeft,
            float frameRight,
            ref LiveInput input,
            Action<int, LiveInput> addInput)
        {
            input.inputId = FreeLaneInputIdBase + lane;
            input.source = LiveInputSource.FreeLane;
            SetFrameLane(ref input, lane, frameLeft, frameRight);
            addInput(lane, input);
        }

        private void AddArrowVolumeFlickInput(
            int lane,
            float frameLeft,
            float frameRight,
            float sizeDelta,
            float musicTime,
            Action<int, LiveInput> addInput)
        {
            LiveInput input = default;
            input.inputId = ArrowVolumeInputIdBase + lane;
            input.phase = LiveInputPhase.Moved;
            input.musicTime = musicTime;
            input.delta = new Vector2(0f, sizeDelta);
            input.source = LiveInputSource.ArrowVolume;
            SetFrameLane(ref input, lane, frameLeft, frameRight);
            addInput(lane, input);
        }

        private void AddSliderFlickInput(
            int lane,
            float frameLeft,
            float frameRight,
            float positionDelta,
            float musicTime,
            Action<int, LiveInput> addInput)
        {
            LiveInput input = default;
            input.inputId = ArrowInputIdBase + lane;
            input.phase = LiveInputPhase.Moved;
            input.musicTime = musicTime;
            input.delta = new Vector2(positionDelta, 0f);
            input.source = LiveInputSource.Arrow;
            SetFrameLane(ref input, lane, frameLeft, frameRight);
            addInput(lane, input);
        }

        private void SetFrameLane(ref LiveInput input, int lane, float frameLeft, float frameRight)
        {
            input.lane = lane;
            input.laneStart = GetLaneByWorldX(frameLeft);
            input.laneEnd = GetLaneByWorldX(frameRight);
            input.width = Mathf.Max(1f, input.laneEnd - input.laneStart + 1f);
            input.worldPosition = GetLaneWorldPosition(lane);
        }

        private static bool IsLaneOverlappingFrame(int lane, float frameLeft, float frameRight)
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            if (judgmentPositions == null || judgmentPositions.Length == 0) return false;

            int index = Mathf.Clamp(lane, 0, judgmentPositions.Length - 1);
            float laneX = judgmentPositions[index].x;
            float halfLaneWidth = LiveConfig.widthX * 0.5f;
            float laneLeft = laneX - halfLaneWidth;
            float laneRight = laneX + halfLaneWidth;
            return frameLeft <= laneRight && frameRight >= laneLeft;
        }

        private Vector3 GetLaneWorldPosition(int lane)
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            if (judgmentPositions == null || judgmentPositions.Length == 0) return Vector3.zero;

            int index = Mathf.Clamp(lane, 0, judgmentPositions.Length - 1);
            Vector2 position = judgmentPositions[index];
            return new Vector3(position.x, position.y, 0f);
        }

        private float GetLaneByWorldX(float worldX)
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            if (judgmentPositions == null || judgmentPositions.Length == 0) return 0f;

            if (worldX <= judgmentPositions[0].x) return 0f;

            for (int i = 1; i < judgmentPositions.Length; i++)
            {
                if (worldX <= judgmentPositions[i].x)
                {
                    float laneDistance = judgmentPositions[i].x - judgmentPositions[i - 1].x;
                    float t = laneDistance <= 0f ? 0f : (worldX - judgmentPositions[i - 1].x) / laneDistance;
                    return i - 1 + Mathf.Clamp01(t);
                }
            }

            return judgmentPositions.Length - 1;
        }

        private static int GetJudgmentLaneCount()
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            return judgmentPositions != null ? judgmentPositions.Length : 0;
        }
    }
}
