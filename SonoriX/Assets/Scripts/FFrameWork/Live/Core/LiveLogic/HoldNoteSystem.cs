using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    internal sealed class HoldNoteSystem
    {
        private readonly List<NoteBase> activeHoldNoteList = new List<NoteBase>(16);
        private readonly List<NoteBase> activeAudioEffectNoteList = new List<NoteBase>(16);

        private PresentationRoot presentationRoot;

        public void SetPresentationRoot(PresentationRoot presentationRoot)
        {
            this.presentationRoot = presentationRoot;
        }

        public void Clear()
        {
            EndAllTrackedAudioEffects();
            activeHoldNoteList.Clear();
            activeAudioEffectNoteList.Clear();
        }

        public void TrackIfHoldNote(NoteBase note)
        {
            if (!IsHoldNote(note) || activeHoldNoteList.Contains(note))
            {
                return;
            }

            activeHoldNoteList.Add(note);
        }

        public void Remove(NoteBase note)
        {
            EndTrackedAudioEffect(note);
            activeHoldNoteList.Remove(note);
        }

        public void RefreshActiveHoldNotes(
            bool isAutoMode,
            bool hasArrowLogic,
            ArrowJudgmentFrame frame,
            float musicTime,
            float sizeDelta,
            bool isFreeLanePressed,
            bool hasInputManager)
        {
            if (activeHoldNoteList.Count == 0) return;

            for (int i = activeHoldNoteList.Count - 1; i >= 0; i--)
            {
                NoteBase note = activeHoldNoteList[i];
                if (note.State == NoteState.Done)
                {
                    EndTrackedAudioEffect(note);
                    activeHoldNoteList.RemoveAt(i);
                    continue;
                }

                if (note is SliderLongNote slider && hasArrowLogic)
                {
                    bool wasActive = slider.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    slider.RefreshArrowHoldState(frame, musicTime);
                    EndLongEffectIfInactive(wasActive, slider.IsHoldJudgmentActive, slider);
                }
                else if (note is FreeLaneLongNote freeLane && !isAutoMode && hasInputManager && hasArrowLogic)
                {
                    bool wasActive = freeLane.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    freeLane.RefreshFreeLaneHoldState(frame, musicTime, isFreeLanePressed);
                    EndLongEffectIfInactive(wasActive, freeLane.IsHoldJudgmentActive, freeLane);
                }
                else if (note is VolumeLongNote volume && hasInputManager && hasArrowLogic)
                {
                    bool wasActive = volume.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    volume.RefreshArrowVolumeHoldState(frame, musicTime, sizeDelta);
                    EndLongEffectIfInactive(wasActive, volume.IsHoldJudgmentActive, volume);
                }
            }
        }

        public void UpdateActiveHoldInputs(
            bool isAutoMode,
            bool hasArrowLogic,
            ArrowJudgmentFrame frame,
            float musicTime,
            float sizeDelta,
            bool isFreeLanePressed,
            bool hasInputManager)
        {
            if (activeHoldNoteList.Count == 0 || !hasArrowLogic) return;

            for (int i = 0; i < activeHoldNoteList.Count; i++)
            {
                NoteBase note = activeHoldNoteList[i];
                if (note == null || note.State == NoteState.Done) continue;

                if (note is SliderLongNote slider)
                {
                    bool wasActive = slider.IsHoldJudgmentActive;
                    slider.TryUpdateByArrow(frame, musicTime);
                    UpdateSliderLongEffect(wasActive, slider);
                }
                else if (note is FreeLaneLongNote freeLane && !isAutoMode && hasInputManager)
                {
                    bool wasActive = freeLane.IsHoldJudgmentActive;
                    freeLane.TryUpdateFreeLaneHold(frame, musicTime, isFreeLanePressed);
                    UpdateFreeLaneLongEffect(wasActive, freeLane);
                }
                else if (note is VolumeLongNote volume && hasInputManager)
                {
                    bool wasActive = volume.IsHoldJudgmentActive;
                    volume.TryUpdateByArrowVolume(frame, musicTime, sizeDelta, out float effectiveSizeDelta);
                    UpdateVolumeLongEffect(wasActive, volume, effectiveSizeDelta);
                }
            }
        }

        public void UpdateFreeLaneLongAutoEffect(NoteBase note, bool wasActive)
        {
            if (!(note is FreeLaneLongNote freeLaneLongNote)) return;

            bool isActive = freeLaneLongNote.IsHoldJudgmentActive;
            bool isDone = freeLaneLongNote.State == NoteState.Done;
            if (!wasActive && isActive && !isDone)
            {
                BeginTrackedAudioEffect(freeLaneLongNote, () => presentationRoot != null && presentationRoot.BeginFreeLaneLongEffect());
            }
            if (isActive && !isDone)
            {
                presentationRoot?.UpdateFreeLaneLongEffect();
            }
            if (isDone || (wasActive && !isActive))
            {
                EndTrackedAudioEffect(freeLaneLongNote);
            }
        }

        public void ReconcileTrackedAudioEffects()
        {
            for (int i = activeAudioEffectNoteList.Count - 1; i >= 0; i--)
            {
                NoteBase note = activeAudioEffectNoteList[i];
                if (note == null)
                {
                    activeAudioEffectNoteList.RemoveAt(i);
                    continue;
                }

                if (note.State == NoteState.Done || !IsLongEffectActive(note))
                {
                    EndTrackedAudioEffect(note);
                }
            }
        }

        public void EndAllTrackedAudioEffects()
        {
            while (activeAudioEffectNoteList.Count > 0)
            {
                NoteBase note = activeAudioEffectNoteList[activeAudioEffectNoteList.Count - 1];
                if (note == null)
                {
                    activeAudioEffectNoteList.RemoveAt(activeAudioEffectNoteList.Count - 1);
                    continue;
                }

                EndTrackedAudioEffect(note);
            }
        }

        public void ForceEndLongAudioEffectByEndNote(NoteBase noteInfo)
        {
            // 終端の Miss / Pass も、成功時と同じようにパラメータを通常値へ戻す。
            if (noteInfo == null || !(noteInfo.ParentNote is NoteBase parentNote) ||
                !(noteInfo is FreeLaneLongEndNote || noteInfo is FixedLongEndNote ||
                  noteInfo is SliderLongEndNote || noteInfo is VolumeLongEndNote))
            {
                return;
            }

            activeAudioEffectNoteList.Remove(parentNote);
            EndAudioEffectByLongNote(parentNote);
        }

        private void UpdateSliderLongEffect(bool wasActive, SliderLongNote note)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginSliderLongEffect());
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateSliderLongEffect();
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        private void UpdateFreeLaneLongEffect(bool wasActive, FreeLaneLongNote note)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginFreeLaneLongEffect());
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateFreeLaneLongEffect();
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        private void UpdateVolumeLongEffect(bool wasActive, VolumeLongNote note, float effectiveSizeDelta)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                float initialDirection = Mathf.Approximately(effectiveSizeDelta, 0f) ? 1f : effectiveSizeDelta;
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginVolumeLongEffect(initialDirection));
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateVolumeLongEffect(effectiveSizeDelta);
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        private static bool ShouldUpdateLongEffect(bool isActive, NoteBase note)
        {
            return isActive && note.State != NoteState.Done;
        }

        private static bool ShouldEndLongEffect(bool wasActive, bool isActive, NoteBase note)
        {
            return note.State == NoteState.Done || (wasActive && !isActive);
        }

        private void EndLongEffectIfInactive(bool wasActive, bool isActive, NoteBase note)
        {
            if (ShouldEndLongEffect(wasActive, isActive, note))
            {
                EndTrackedAudioEffect(note);
            }
        }

        private void BeginTrackedAudioEffect(NoteBase note, Func<bool> beginEffect)
        {
            if (note == null || beginEffect == null || activeAudioEffectNoteList.Contains(note))
            {
                return;
            }

            // 一部のパラメータだけ反映された場合も、終了時の復元対象として保持する。
            activeAudioEffectNoteList.Add(note);
            beginEffect();
        }

        private void EndTrackedAudioEffect(NoteBase note)
        {
            if (note == null)
            {
                return;
            }

            int index = activeAudioEffectNoteList.IndexOf(note);
            if (index < 0)
            {
                return;
            }

            activeAudioEffectNoteList.RemoveAt(index);
            EndAudioEffectByLongNote(note);
        }

        private static bool IsLongEffectActive(NoteBase note)
        {
            if (note is SliderLongNote slider)
            {
                return slider.IsHoldJudgmentActive;
            }
            if (note is FreeLaneLongNote freeLane)
            {
                return freeLane.IsHoldJudgmentActive;
            }
            if (note is VolumeLongNote volume)
            {
                return volume.IsHoldJudgmentActive;
            }

            return false;
        }

        private void EndAudioEffectByLongNote(NoteBase note)
        {
            if (note is SliderLongNote)
            {
                presentationRoot?.EndSliderLongEffect();
            }
            else if (note is FreeLaneLongNote)
            {
                presentationRoot?.EndFreeLaneLongEffect();
            }
            else if (note is VolumeLongNote)
            {
                presentationRoot?.EndVolumeLongEffect();
            }
        }

        private static bool IsHoldNote(NoteBase note)
        {
            return note is SliderLongNote || note is FreeLaneLongNote || note is VolumeLongNote;
        }
    }
}
