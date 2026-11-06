using System;
using System.Collections.Generic;

namespace FFrameWork.Live
{
    internal sealed class ChartPlaybackSystem
    {
        private MusicScore musicScore;
        private NoteBase[] noteArray = Array.Empty<NoteBase>();
        private int noteStartIndex;
        private float noteDisplayTimeOffset;
        private MusicScoreInfo currentFrameInfo;

        public MusicScore MusicScore => musicScore;
        public NoteBase[] NoteArray => noteArray;
        public MusicScoreInfo CurrentFrameInfo => currentFrameInfo;
        public bool HasMusicScore => musicScore != null;
        public bool IsNotesAllFinished => noteArray == null || noteStartIndex >= noteArray.Length;

        public void Setup(MusicScore musicScore, float noteDisplayTimeOffset, Action<NoteBase, bool> configureNote)
        {
            this.musicScore = musicScore ?? new MusicScore();
            this.noteDisplayTimeOffset = noteDisplayTimeOffset;
            this.musicScore.Update(0f);
            currentFrameInfo = this.musicScore.CurrentFrameInfo;
            noteArray = this.musicScore.NoteArray ?? Array.Empty<NoteBase>();
            noteStartIndex = 0;

            for (int i = 0; i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;

                ConfigureNote(note, true, configureNote);

                if (note.NoteList == null) continue;
                for (int j = 0; j < note.NoteList.Count; j++)
                {
                    ConfigureNote(note.NoteList[j], false, configureNote);
                }
            }
        }

        public void UpdateTime(float musicTime)
        {
            if (musicScore == null)
            {
                return;
            }

            musicScore.Update(musicTime);
            currentFrameInfo = musicScore.CurrentFrameInfo;
        }

        public void UpdateNotes(
            List<NoteBase> judgmentNoteList,
            Action<NoteBase> onNoteDone,
            Action<NoteBase> onNoteExecuted)
        {
            for (int i = noteStartIndex; noteArray != null && i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;

                if (note.State == NoteState.Done)
                {
                    if (i == noteStartIndex) noteStartIndex = i + 1;
                    onNoteDone?.Invoke(note);
                    continue;
                }

                float timeOffset = CalcTimeOffset(note);
                if (timeOffset + currentFrameInfo.time + LiveUtility.OneFrameTime <= note.MusicScoreInfo.time)
                {
                    if (note.State != NoteState.Playing) continue;
                }

                note.Excute(currentFrameInfo, timeOffset);
                onNoteExecuted?.Invoke(note);

                if (note.State != NoteState.Done && note.HasJudgment)
                {
                    judgmentNoteList?.Add(note);
                }
            }
        }

        public void ExecuteAutoJudgment(Action<NoteBase, bool> onAutoJudged)
        {
            for (int i = noteStartIndex; noteArray != null && i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;
                if (note.MusicScoreInfo.time > currentFrameInfo.time) break;
                if (note.State == NoteState.Done) continue;

                bool wasFreeLaneLongActive = note is FreeLaneLongNote freeLaneLongNote &&
                                             freeLaneLongNote.IsHoldJudgmentActive;
                note.AutoJudgment(currentFrameInfo);
                onAutoJudged?.Invoke(note, wasFreeLaneLongActive);
            }
        }

        private void ConfigureNote(NoteBase note, bool isRoot, Action<NoteBase, bool> configureNote)
        {
            if (note == null)
            {
                return;
            }

            note.CalcTimeOffset = CalcTimeOffset;
            configureNote?.Invoke(note, isRoot);
        }

        private float CalcTimeOffset(NoteBase noteInfo)
        {
            if (noteInfo == null) return 0f;

            float currentProgress = currentFrameInfo.bar + currentFrameInfo.barProgress;
            float noteProgress = noteInfo.MusicScoreInfo.bar + noteInfo.MusicScoreInfo.barProgress;
            float speedRatio = CalcNoteSpeedRatio(currentProgress, noteProgress);

            if (speedRatio.Equals(0f)) return 0f;
            return noteDisplayTimeOffset / speedRatio / noteInfo.speedRatio;
        }

        private float CalcNoteSpeedRatio(float currentProgress, float noteProgress)
        {
            if (noteProgress <= currentProgress) return currentFrameInfo.speedRatio;
            MusicScoreInfo[] infos = musicScore?.musicScoreInfoArray;
            if (infos == null || infos.Length == 0) return currentFrameInfo.speedRatio;

            float weighted = 0f;
            float cursor = currentProgress;
            float ratio = currentFrameInfo.speedRatio;

            for (int i = 0; i < infos.Length; i++)
            {
                float eventProgress = infos[i].bar + infos[i].barProgress;
                if (eventProgress <= currentProgress)
                {
                    ratio = infos[i].speedRatio;
                    cursor = currentProgress;
                    continue;
                }
                if (eventProgress >= noteProgress) break;

                weighted += ratio * (eventProgress - cursor);
                cursor = eventProgress;
                ratio = infos[i].speedRatio;
            }

            weighted += ratio * (noteProgress - cursor);
            float length = noteProgress - currentProgress;
            return length.Equals(0f) ? currentFrameInfo.speedRatio : weighted / length;
        }
    }
}
