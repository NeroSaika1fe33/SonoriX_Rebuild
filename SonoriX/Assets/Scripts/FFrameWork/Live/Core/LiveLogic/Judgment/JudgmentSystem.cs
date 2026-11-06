using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    internal sealed class JudgmentSystem
    {
        private const int CandidateCapacity = 128;

        private readonly List<int> physicalInputKeyList = new List<int>(CandidateCapacity);
        private readonly List<InputTmp> physicalInputBestInputList = new List<InputTmp>(CandidateCapacity);
        private readonly List<NoteBase> physicalInputBestNoteList = new List<NoteBase>(CandidateCapacity);
        private readonly LiveBundleBuildData liveBundleBuildData;

        private List<NoteBase> judgmentNoteList;
        private List<InputTmp> activeInputList;
        private MusicScoreInfo currentFrameInfo;
        private PresentationRoot presentationRoot;

        public JudgmentSystem(LiveBundleBuildData liveBundleBuildData)
        {
            this.liveBundleBuildData = liveBundleBuildData;
        }

        public void Execute(
            List<NoteBase> judgmentNoteList,
            List<InputTmp> activeInputList,
            MusicScoreInfo currentFrameInfo,
            PresentationRoot presentationRoot)
        {
            this.judgmentNoteList = judgmentNoteList;
            this.activeInputList = activeInputList;
            this.currentFrameInfo = currentFrameInfo;
            this.presentationRoot = presentationRoot;

            SortJudgmentNotesByTime();
            AllocateInputJudgableEachNotes();
            OverlapJudgmentSorting();
            LeaveNotesFastGeneration();
            LeavePhysicalInputCandidates();
            EvaluateJudgment();
        }

        private void SortJudgmentNotesByTime()
        {
            for (int i = 1; i < judgmentNoteList.Count; i++)
            {
                NoteBase key = judgmentNoteList[i];
                int j = i - 1;
                while (j >= 0 && judgmentNoteList[j].MusicScoreInfo.time > key.MusicScoreInfo.time)
                {
                    judgmentNoteList[j + 1] = judgmentNoteList[j];
                    j--;
                }
                judgmentNoteList[j + 1] = key;
            }
        }

        private void AllocateInputJudgableEachNotes()
        {
            for (int i = 0; i < judgmentNoteList.Count; i++)
            {
                NoteBase judgmentNote = judgmentNoteList[i];
                if (judgmentNote == null) continue;

                for (int j = activeInputList.Count - 1; j >= 0; j--)
                {
                    InputTmp activeInput = activeInputList[j];
                    if (!CanJudgeByInputSource(activeInput.liveInput.source, judgmentNote)) continue;

                    float lane = ConvertLaneBy(judgmentNote, activeInput);
                    if (lane >= 0f && judgmentNote.IsJudgment(ref activeInput.liveInput, lane))
                    {
                        activeInput.CandidateNotes.Add(judgmentNote);
                    }
                }
            }
        }

        private void OverlapJudgmentSorting()
        {
            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int k = activeInputList.Count - 1; k >= 0; k--)
                {
                    if (i == k) continue;

                    InputTmp other = activeInputList[k];
                    for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                    {
                        NoteBase note = input.CandidateNotes[j];
                        if (!other.CandidateNotes.Contains(note)) continue;

                        float inputDistance = note.LaneDistance(ref currentFrameInfo, ConvertLaneBy(note, input));
                        float otherDistance = note.LaneDistance(ref currentFrameInfo, ConvertLaneBy(note, other));
                        if (inputDistance <= otherDistance)
                        {
                            other.CandidateNotes.Remove(note);
                        }
                        else
                        {
                            input.CandidateNotes.Remove(note);
                        }
                    }
                }
            }
        }

        private void LeaveNotesFastGeneration()
        {
            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    if (input.CandidateNotes.Count < 2) continue;

                    NoteBase firstCandidate = input.CandidateNotes[0];
                    NoteBase candidate = input.CandidateNotes[j];
                    bool removeCandidate = CompareCandidatePriority(firstCandidate, input, candidate, input) >= 0;

                    if (removeCandidate)
                    {
                        input.CandidateNotes.Remove(candidate);
                    }
                    else
                    {
                        input.CandidateNotes.Remove(firstCandidate);
                        j = input.CandidateNotes.Count;
                    }
                }
            }
        }

        private int CompareCandidatePriority(NoteBase leftCandidate, InputTmp leftInput, NoteBase rightCandidate, InputTmp rightInput)
        {
            NoteBase left = SelectLeaveComparableNote(leftCandidate, leftInput, leftCandidate);
            NoteBase right = SelectLeaveComparableNote(rightCandidate, rightInput, rightCandidate);

            float progressDiff = left.Progress - right.Progress;
            if (!Mathf.Approximately(progressDiff, 0f))
            {
                return progressDiff > 0f ? 1 : -1;
            }

            float leftDistance = left.LaneDistance(ref currentFrameInfo, ConvertLaneBy(left, leftInput));
            float rightDistance = right.LaneDistance(ref currentFrameInfo, ConvertLaneBy(right, rightInput));
            float distanceDiff = rightDistance - leftDistance;
            if (!Mathf.Approximately(distanceDiff, 0f))
            {
                return distanceDiff > 0f ? 1 : -1;
            }

            if (!left.MusicScoreInfo.time.Equals(right.MusicScoreInfo.time))
            {
                return left.MusicScoreInfo.time < right.MusicScoreInfo.time ? 1 : -1;
            }

            if (left.Id != right.Id)
            {
                return left.Id < right.Id ? 1 : -1;
            }

            return 0;
        }

        private NoteBase SelectLeaveComparableNote(NoteBase note, InputTmp input, NoteBase fallbackNote)
        {
            if (note.NoteList.Count == 1) return note;

            NoteBase comparable;
            if (note.Result == NoteResult.None && input.liveInput.phase == LiveInputPhase.Began)
            {
                comparable = note.NoteList[0];
            }
            else
            {
                comparable = note.NoteList[note.NoteList.Count - 1];
            }

            float lane = ConvertLaneBy(comparable, input);
            return comparable.IsJudgment(ref input.liveInput, lane) ? comparable : fallbackNote;
        }

        private void LeavePhysicalInputCandidates()
        {
            physicalInputKeyList.Clear();
            physicalInputBestInputList.Clear();
            physicalInputBestNoteList.Clear();

            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    NoteBase candidate = input.CandidateNotes[j];
                    if (candidate == null) continue;

                    int physicalInputKey = GetPhysicalInputKey(input);
                    int index = physicalInputKeyList.IndexOf(physicalInputKey);
                    if (index < 0)
                    {
                        physicalInputKeyList.Add(physicalInputKey);
                        physicalInputBestInputList.Add(input);
                        physicalInputBestNoteList.Add(candidate);
                        continue;
                    }

                    if (CompareCandidatePriority(candidate, input, physicalInputBestNoteList[index], physicalInputBestInputList[index]) > 0)
                    {
                        physicalInputBestInputList[index] = input;
                        physicalInputBestNoteList[index] = candidate;
                    }
                }
            }

            if (physicalInputKeyList.Count == 0) return;

            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                int index = physicalInputKeyList.IndexOf(GetPhysicalInputKey(input));
                InputTmp bestInput = index >= 0 ? physicalInputBestInputList[index] : null;
                NoteBase bestNote = index >= 0 ? physicalInputBestNoteList[index] : null;

                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    if (input == bestInput && input.CandidateNotes[j] == bestNote) continue;
                    input.CandidateNotes.RemoveAt(j);
                }
            }
        }

        private static int GetPhysicalInputKey(InputTmp input)
        {
            if (input == null) return 0;

            int sourceKey = ((int)input.liveInput.source + 1) << 16;
            switch (input.liveInput.source)
            {
                case LiveInputSource.FixedLane:
                    return sourceKey + input.liveInput.inputId;
                case LiveInputSource.FreeLane:
                case LiveInputSource.Arrow:
                case LiveInputSource.ArrowVolume:
                    return sourceKey;
                default:
                    return sourceKey + input.liveInput.inputId;
            }
        }

        private void EvaluateJudgment()
        {
            for (int i = 0; i < activeInputList.Count; i++)
            {
                InputTmp input = activeInputList[i];
                if (input.CandidateNotes.Count > 0)
                {
                    for (int j = 0; j < input.CandidateNotes.Count; j++)
                    {
                        NoteBase note = input.CandidateNotes[j];
                        float lane = ConvertLaneBy(note, input);
                        note.Judgment(ref input.liveInput, lane);
                        judgmentNoteList.Remove(note);
                    }
                    input.CandidateNotes.Clear();
                }
            }

            for (int i = 0; i < activeInputList.Count; i++)
            {
                InputTmp input = activeInputList[i];
                if (input.Lane >= 0f && input.liveInput.source == LiveInputSource.FixedLane)
                {
                    presentationRoot?.Unpicked(Mathf.RoundToInt(input.Lane), ref input.liveInput);
                }
            }

            activeInputList.Clear();
        }

        private float ConvertLaneBy(NoteBase note, InputTmp input)
        {
            if (note.Category != NoteCategory.SliderFlick || note.State != NoteState.Playing)
            {
                return input.Lane;
            }
            return GetJudgmentLane(input.liveInput.worldPosition, !note.FinishFirstAction);
        }

        private float GetJudgmentLane(Vector3 touchPosition, bool checkJudgmentY)
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            if (judgmentPositions == null || judgmentPositions.Length == 0) return -1f;

            float offsetX = liveBundleBuildData != null ? liveBundleBuildData.JudgmentOffsetX : 1.8f;
            float offsetY = liveBundleBuildData != null ? liveBundleBuildData.JudgmentOffsetY : 2.5f;
            if (checkJudgmentY && offsetY < touchPosition.y - judgmentPositions[0].y) return -1f;
            if (offsetX < judgmentPositions[0].x - touchPosition.x) return -1f;
            if (offsetX < touchPosition.x - judgmentPositions[judgmentPositions.Length - 1].x) return -1f;

            for (int i = 0; i < judgmentPositions.Length; i++)
            {
                if (touchPosition.x - judgmentPositions[i].x <= 0f)
                {
                    if (i == 0) return 0f;
                    return (touchPosition.x - judgmentPositions[i - 1].x) / (judgmentPositions[i].x - judgmentPositions[i - 1].x) + i - 1;
                }
            }
            return judgmentPositions.Length - 1;
        }

        private static bool CanJudgeByInputSource(LiveInputSource source, NoteBase note)
        {
            if (note == null) return false;
            if (note.Category == NoteCategory.SliderLong || note.Category == NoteCategory.VolumeFlickLong)
            {
                return false;
            }

            bool isFixedLaneNote = note.Category == NoteCategory.FixedNormal || note.Category == NoteCategory.FixedLong;
            switch (source)
            {
                case LiveInputSource.FixedLane:
                    return isFixedLaneNote;
                case LiveInputSource.Arrow:
                    return note.Category == NoteCategory.SliderFlick;
                case LiveInputSource.ArrowVolume:
                    return note.Category == NoteCategory.VolumeFlick;
                case LiveInputSource.FreeLane:
                    return !isFixedLaneNote && note.Category != NoteCategory.SliderFlick && note.Category != NoteCategory.VolumeFlick;
                default:
                    return false;
            }
        }
    }
}
