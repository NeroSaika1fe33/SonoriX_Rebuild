using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using FFrameWork.Live;
using FFrameWork.Songs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FFrameWork.UI
{
    // 選曲メニューの曲選択、難易度選択、UI サウンドを制御する
    public class SongSelectMenuController : MonoBehaviour
    {
        // 同じ MusicId の難易度別 SongSelectEntry をまとめた表示単位
        private sealed class SongGroup
        {
            public int MusicId;
            public string Title;
            public string Artist;
            public Texture Jacket;
            public SongSelectEntry Easy;
            public SongSelectEntry Normal;
            public SongSelectEntry Hard;
            public SongSelectEntry Extra;
            public SongSelectEntry Debug;

            // 指定難易度の譜面エントリを取得する
            public SongSelectEntry Get(MusicDifficulty difficulty)
            {
                switch (difficulty)
                {
                    case MusicDifficulty.Easy:
                        return Easy;
                    case MusicDifficulty.Normal:
                        return Normal;
                    case MusicDifficulty.Hard:
                        return Hard;
                    case MusicDifficulty.Extra:
                        return Extra;
                    case MusicDifficulty.Debug:
                        return Debug;
                    default:
                        return null;
                }
            }

            // デバッグ難易度生成元に使う譜面を取得する
            public SongSelectEntry GetDebugSource()
            {
                return Extra ?? Hard ?? Normal ?? Easy;
            }
        }

        // メニュー上で移動する難易度の並び
        private static readonly MusicDifficulty[] DifficultyOrder =
        {
            MusicDifficulty.Easy,
            MusicDifficulty.Normal,
            MusicDifficulty.Hard,
            MusicDifficulty.Extra
        };

        [SerializeField] private SongSelectDataBase songDataBase;
        [SerializeField] private LivePanelFlowController flowController;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private SongSelectMenuItemView itemPrefab;
        [SerializeField] private Vector2 itemSize = new Vector2(920f, 170f);
        [SerializeField] private Vector2 topPosition = Vector2.zero;
        [SerializeField] private float itemSpacing = 16f;
        [SerializeField] private float deltaThreshold = 2f;
        [SerializeField] private float moveRepeatSeconds = 0.12f;
        [SerializeField] private bool createRuntimeDebugDifficulty;

        [Header("Song List Scrolling")]
        [SerializeField, Min(1)] private int visibleItemCount = 3;
        [SerializeField, Min(0.01f)] private float scrollSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float viewportPadding = 10f;

        [Header("FMOD UI Sounds")]
        [SerializeField] private EventReference songSelectSoundEvent;
        [SerializeField] private EventReference songToDifficultySoundEvent;
        [SerializeField] private EventReference difficultySelectSoundEvent;
        [SerializeField, Range(0f, 2f)] private float songSelectSoundVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float songToDifficultySoundVolume = 1f;
        [SerializeField, Range(0f, 2f)] private float difficultySelectSoundVolume = 1f;
        [SerializeField] private bool logMissingUiSound = true;

        // 楽曲 ID ごとにまとめた選曲リスト
        private readonly List<SongGroup> songGroups = new List<SongGroup>();
        // 表示中の行ビュー
        private readonly List<SongSelectMenuItemView> itemViews = new List<SongSelectMenuItemView>();
        // 選択中の曲グループ index
        private int selectedSongIndex;
        // 選択中の難易度 index
        private int selectedDifficultyIndex;
        // 難易度選択レイヤーに入っているか
        private bool isDifficultyMode;
        // マウス移動入力のリピート抑制時刻
        private float nextMoveTime;

        private RectTransform scrollViewport;
        private RectTransform scrollContent;
        private float currentScrollOffset;
        private float targetScrollOffset;
        private float scrollVelocity;

        private float ItemStep => Mathf.Max(1f, itemSize.y + itemSpacing);

        // 現在選択中の曲グループ
        private SongGroup CurrentSong =>
            songGroups.Count > 0 && selectedSongIndex >= 0 && selectedSongIndex < songGroups.Count
                ? songGroups[selectedSongIndex]
                : null;

        // 現在選択中の難易度
        private MusicDifficulty CurrentDifficulty =>
            DifficultyOrder[Mathf.Clamp(selectedDifficultyIndex, 0, DifficultyOrder.Length - 1)];

        private void OnEnable()
        {
            BuildMenu();
            isDifficultyMode = false;
            ApplyCurrentDifficultySelection();
            RefreshView();
            currentScrollOffset = targetScrollOffset;
            scrollVelocity = 0f;
            nextMoveTime = 0f;
            ApplyScrollPosition();
        }

        private void LateUpdate()
        {
            // 入力処理の早期 return やポーズ中もスクロールを継続する。
            currentScrollOffset = Mathf.SmoothDamp(
                currentScrollOffset, targetScrollOffset, ref scrollVelocity,
                Mathf.Max(0.01f, scrollSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
            ApplyScrollPosition();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    if (TryStartCurrentSelection())
                    {
                        flowController?.ShowNoteSpeedSettingsPanel();
                    }
                    return;
                }

                if (keyboard.aKey.wasPressedThisFrame)
                {
                    if (isDifficultyMode)
                    {
                        BackToSongLayer();
                    }
                    else
                    {
                        flowController?.ShowOperationGuidePromptPanel();
                    }

                    return;
                }

                if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    MoveSelection(-1);
                    return;
                }

                if (keyboard.downArrowKey.wasPressedThisFrame)
                {
                    MoveSelection(1);
                    return;
                }
            }

            Mouse mouse = Mouse.current;
            float scrollY = mouse != null ? mouse.scroll.y.ReadValue() : 0f;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                MoveSelection(scrollY > 0f ? -1 : 1);
                nextMoveTime = Time.unscaledTime + moveRepeatSeconds;
                return;
            }

            float deltaY = mouse != null ? mouse.delta.y.ReadValue() : 0f;
            if (Mathf.Abs(deltaY) < deltaThreshold || Time.unscaledTime < nextMoveTime)
            {
                return;
            }

            MoveSelection(deltaY > 0f ? 1 : -1);
            nextMoveTime = Time.unscaledTime + moveRepeatSeconds;
        }

        // 曲選択中なら難易度選択へ入り、難易度選択中なら曲を確定する
        public bool TryStartCurrentSelection()
        {
            SongGroup song = CurrentSong;
            if (song == null)
            {
                return false;
            }

            if (!isDifficultyMode)
            {
                isDifficultyMode = true;
                selectedDifficultyIndex = FindFirstAvailableDifficultyIndex(song);
                ApplyCurrentDifficultySelection();
                RefreshView();
                PlaySongToDifficultySound();
                return false;
            }

            SongSelectEntry selectedSong = song.Get(CurrentDifficulty);
            if (selectedSong == null)
            {
                int previousDifficultyIndex = selectedDifficultyIndex;
                selectedDifficultyIndex = FindFirstAvailableDifficultyIndex(song);
                selectedSong = song.Get(CurrentDifficulty);
                if (selectedSong != null && selectedDifficultyIndex != previousDifficultyIndex)
                {
                    PlayDifficultySelectSound();
                }
            }

            if (selectedSong == null)
            {
                return false;
            }

            flowController?.SetSelectedSong(selectedSong);
            return true;
        }

        // 難易度選択から曲選択レイヤーへ戻る
        public void BackToSongLayer()
        {
            if (!isDifficultyMode)
            {
                return;
            }

            isDifficultyMode = false;
            RefreshView();
        }

        // SongSelectDataBase から曲グループと行ビューを構築する
        private void BuildMenu()
        {
            songGroups.Clear();

            SongSelectEntry[] songs = songDataBase != null ? songDataBase.Songs : null;
            songs = songs ?? Array.Empty<SongSelectEntry>();

            Dictionary<int, SongGroup> groups = new Dictionary<int, SongGroup>();
            for (int i = 0; i < songs.Length; i++)
            {
                SongSelectEntry song = songs[i];
                if (song == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(song.MusicId, out SongGroup group))
                {
                    group = new SongGroup
                    {
                        MusicId = song.MusicId,
                        Title = song.Title,
                        Artist = string.IsNullOrEmpty(song.Artist) ? song.Composer : song.Artist,
                        Jacket = song.Jacket
                    };
                    groups.Add(song.MusicId, group);
                    songGroups.Add(group);
                }

                if (group.Jacket == null && song.Jacket != null)
                {
                    group.Jacket = song.Jacket;
                }

                AssignDifficulty(group, song);
            }

            if (createRuntimeDebugDifficulty)
            {
                AddRuntimeDebugDifficulties();
            }

            selectedSongIndex = Mathf.Clamp(selectedSongIndex, 0, Mathf.Max(songGroups.Count - 1, 0));
            selectedDifficultyIndex = FindFirstAvailableDifficultyIndex(CurrentSong);
            EnsureItemViews();
        }

        // SongSelectEntry を難易度別のスロットへ割り当てる
        private static void AssignDifficulty(SongGroup group, SongSelectEntry song)
        {
            switch (song.Difficulty)
            {
                case MusicDifficulty.Easy:
                    group.Easy = song;
                    break;
                case MusicDifficulty.Normal:
                    group.Normal = song;
                    break;
                case MusicDifficulty.Hard:
                    group.Hard = song;
                    break;
                case MusicDifficulty.Extra:
                    group.Extra = song;
                    break;
            }
        }

        // Extra などを元に実行時デバッグ難易度を追加する
        private void AddRuntimeDebugDifficulties()
        {
            for (int i = 0; i < songGroups.Count; i++)
            {
                SongGroup group = songGroups[i];
                if (group == null || group.Debug != null)
                {
                    continue;
                }

                SongSelectEntry source = group.GetDebugSource();
                if (source != null)
                {
                    group.Debug = SongSelectEntry.CreateRuntimeDebugEntry(source);
                }
            }
        }

        // 曲数に合わせて行ビューを確保する
        private void EnsureItemViews()
        {
            if (itemRoot == null)
            {
                itemRoot = transform;
            }

            EnsureScrollLayout();

            while (itemViews.Count < songGroups.Count)
            {
                itemViews.Add(CreateItemView());
            }

            for (int i = 0; i < itemViews.Count; i++)
            {
                if (itemViews[i] != null)
                {
                    itemViews[i].gameObject.SetActive(i < songGroups.Count);
                }
            }
        }

        // 既存の行も使い、シーンの組み直しなしで表示範囲を作る。
        private void EnsureScrollLayout()
        {
            if (scrollViewport == null)
            {
                for (int i = 0; i < itemRoot.childCount; i++)
                {
                    SongSelectMenuItemView existing = itemRoot.GetChild(i).GetComponent<SongSelectMenuItemView>();
                    if (existing != null && !itemViews.Contains(existing))
                    {
                        itemViews.Add(existing);
                    }
                }

                GameObject viewportObject = new GameObject("SongListViewport", typeof(RectTransform), typeof(RectMask2D));
                viewportObject.layer = itemRoot.gameObject.layer;
                scrollViewport = viewportObject.GetComponent<RectTransform>();
                scrollViewport.SetParent(itemRoot, false);
                scrollViewport.anchorMin = new Vector2(0.5f, 1f);
                scrollViewport.anchorMax = new Vector2(0.5f, 1f);
                scrollViewport.pivot = new Vector2(0.5f, 0.5f);

                GameObject contentObject = new GameObject("SongListContent", typeof(RectTransform));
                contentObject.layer = itemRoot.gameObject.layer;
                scrollContent = contentObject.GetComponent<RectTransform>();
                scrollContent.SetParent(scrollViewport, false);
                scrollContent.anchorMin = new Vector2(0f, 1f);
                scrollContent.anchorMax = new Vector2(1f, 1f);
                scrollContent.pivot = new Vector2(0.5f, 1f);

                for (int i = 0; i < itemViews.Count; i++)
                {
                    itemViews[i].transform.SetParent(scrollContent, false);
                }
            }

            int visibleCount = Mathf.Max(1, visibleItemCount);
            float windowHeight = itemSize.y + (visibleCount - 1) * ItemStep;
            float padding = Mathf.Max(0f, viewportPadding);
            scrollViewport.sizeDelta = new Vector2(itemSize.x + padding * 2f, windowHeight + padding * 2f);
            scrollViewport.anchoredPosition = topPosition + new Vector2(0f, -windowHeight * 0.5f);
            scrollContent.sizeDelta = new Vector2(0f, itemSize.y + Mathf.Max(0, songGroups.Count - 1) * ItemStep);

            // 中央の行へ追従し、先頭と末尾では空白が出ない位置に留める。
            int firstVisibleIndex = Mathf.Clamp(
                selectedSongIndex - visibleCount / 2, 0, Mathf.Max(0, songGroups.Count - visibleCount));
            targetScrollOffset = firstVisibleIndex * ItemStep;
        }

        private void ApplyScrollPosition()
        {
            if (scrollContent != null)
            {
                scrollContent.anchoredPosition = new Vector2(0f, currentScrollOffset - Mathf.Max(0f, viewportPadding));
            }
        }

        // Prefab または空 GameObject から 1 行分のビューを作成する
        private SongSelectMenuItemView CreateItemView()
        {
            SongSelectMenuItemView view;
            if (itemPrefab != null)
            {
                view = Instantiate(itemPrefab, scrollContent, false);
            }
            else if (itemViews.Count > 0 && itemViews[0] != null)
            {
                view = Instantiate(itemViews[0], scrollContent, false);
            }
            else
            {
                GameObject itemObject = new GameObject("SongSelectMenuItem", typeof(RectTransform), typeof(SongSelectMenuItemView));
                itemObject.layer = itemRoot.gameObject.layer;
                itemObject.transform.SetParent(scrollContent, false);
                view = itemObject.GetComponent<SongSelectMenuItemView>();
            }

            return view;
        }

        // 現在の選択状態をすべての行ビューへ反映する
        private void RefreshView()
        {
            EnsureItemViews();

            for (int i = 0; i < songGroups.Count; i++)
            {
                SongSelectMenuItemView view = itemViews[i];
                SongGroup group = songGroups[i];
                if (view == null || group == null)
                {
                    continue;
                }

                RectTransform rectTransform = view.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.anchoredPosition = new Vector2(0f, -i * ItemStep);
                }

                view.SetItemSize(itemSize);
                view.Bind(
                    group.Jacket,
                    group.Title,
                    group.Artist,
                    group.Easy,
                    group.Normal,
                    group.Hard,
                    group.Extra,
                    createRuntimeDebugDifficulty ? group.Debug : null);
                view.SetSelection(i == selectedSongIndex, isDifficultyMode && i == selectedSongIndex, CurrentDifficulty);
            }
        }

        // 曲または難易度の選択位置を移動する
        private void MoveSelection(int direction)
        {
            if (songGroups.Count == 0)
            {
                return;
            }

            if (!isDifficultyMode)
            {
                int previousSongIndex = selectedSongIndex;
                selectedSongIndex = Mathf.Clamp(selectedSongIndex + direction, 0, songGroups.Count - 1);
                selectedDifficultyIndex = FindFirstAvailableDifficultyIndex(CurrentSong);
                if (selectedSongIndex != previousSongIndex)
                {
                    PlaySongSelectSound();
                }
                ApplyCurrentDifficultySelection();
                RefreshView();
                return;
            }

            int previousDifficultyIndex = selectedDifficultyIndex;
            MoveDifficulty(direction);
            if (selectedDifficultyIndex != previousDifficultyIndex)
            {
                PlayDifficultySelectSound();
            }
            ApplyCurrentDifficultySelection();
            RefreshView();
        }

        // 存在する難易度だけを対象に難易度選択を移動する
        private void MoveDifficulty(int direction)
        {
            SongGroup song = CurrentSong;
            if (song == null)
            {
                return;
            }

            int nextDifficultyIndex = selectedDifficultyIndex + direction;
            while (nextDifficultyIndex >= 0 && nextDifficultyIndex < DifficultyOrder.Length)
            {
                if (song.Get(DifficultyOrder[nextDifficultyIndex]) != null)
                {
                    selectedDifficultyIndex = nextDifficultyIndex;
                    return;
                }

                nextDifficultyIndex += direction;
            }
        }

        // 現在選択中の難易度を LivePanelFlowController へ反映する
        private void ApplyCurrentDifficultySelection()
        {
            SongSelectEntry selectedSong = CurrentSong?.Get(CurrentDifficulty);
            if (selectedSong != null)
            {
                flowController?.SetSelectedSong(selectedSong);
            }
        }

        // 指定曲で最初に存在する難易度 index を取得する
        private static int FindFirstAvailableDifficultyIndex(SongGroup song)
        {
            if (song == null)
            {
                return 0;
            }

            for (int i = 0; i < DifficultyOrder.Length; i++)
            {
                if (song.Get(DifficultyOrder[i]) != null)
                {
                    return i;
                }
            }

            return 0;
        }

        // 曲移動時の UI 音を再生する
        private void PlaySongSelectSound()
        {
            PlayUiSound(songSelectSoundEvent, songSelectSoundVolume);
        }

        // 曲選択から難易度選択へ入る UI 音を再生する
        private void PlaySongToDifficultySound()
        {
            if (!songToDifficultySoundEvent.IsNull)
            {
                PlayUiSound(songToDifficultySoundEvent, songToDifficultySoundVolume);
                return;
            }

            PlayDifficultySelectSound();
        }

        // 難易度移動時の UI 音を再生する
        private void PlayDifficultySelectSound()
        {
            if (!difficultySelectSoundEvent.IsNull)
            {
                PlayUiSound(difficultySelectSoundEvent, difficultySelectSoundVolume);
                return;
            }

            PlaySongSelectSound();
        }

        // FMOD のワンショット UI 音を再生する
        private void PlayUiSound(EventReference eventReference, float volume)
        {
            if (eventReference.IsNull)
            {
                return;
            }

            EventInstance instance;
            try
            {
                instance = RuntimeManager.CreateInstance(eventReference);
            }
            catch (Exception exception)
            {
                if (logMissingUiSound)
                {
                    Debug.LogWarning("FMOD UI sound create failed: " + exception.Message, this);
                }
                return;
            }

            if (!instance.isValid())
            {
                return;
            }

            instance.setVolume(Mathf.Clamp(volume, 0f, 2f));
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            instance.start();
            instance.release();
        }
    }
}
