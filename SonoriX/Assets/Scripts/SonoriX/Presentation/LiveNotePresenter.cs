using System.Collections.Generic;
using SonoriX.App;
using SonoriX.Domain;
using SonoriX.Runtime;
using UnityEngine;
using SonoriX.Domain.Charts;

namespace SonoriX.Presentation
{
    public sealed class LiveNotePresenter: MonoBehaviour
    {
        [SerializeField] private LivePresentationRoot presentationRoot;
        [SerializeField] private LiveNoteView[] noteViews;
        private LiveNoteLayout noteLayout;
        
        private readonly Dictionary<int, LiveNoteView> activeViews = new Dictionary<int, LiveNoteView>();
        private readonly Queue<LiveNoteView> freeViews = new Queue<LiveNoteView>();
        
        private LiveSceneController liveSceneController;

        private void Awake()
        {
            ResolveDependencies();
        }

        //初始化View池
        private void InitializeViewPool()
        {
            //清理残留内容
            activeViews.Clear();
            freeViews.Clear();

            //判空
            if (noteViews == null)
            {
                return;
            }

            //隐藏全部view并放入可以调用的free池
            foreach (LiveNoteView view in noteViews)
            {
                if (view == null)
                {
                    continue;
                }
                
                view.Hide();
                freeViews.Enqueue(view);
            }
        }

        //订阅每帧更新事件
        private void OnEnable()
        {
            ResolveDependencies();
            
            InitializeViewPool();
            
            if (liveSceneController != null)
            {
                liveSceneController.FrameUpdated += ApplyFrame;
                liveSceneController.JudgmentProduced += ApplyJudgment;
                liveSceneController.RuntimeReset += InitializeViewPool;
            }
        }

        //取消订阅每帧更新事件
        private void OnDisable()
        {
            if (liveSceneController != null)
            {
                liveSceneController.FrameUpdated -= ApplyFrame;
                liveSceneController.JudgmentProduced -= ApplyJudgment;
                liveSceneController.RuntimeReset -= InitializeViewPool;
            }
            
            InitializeViewPool();
        }

        //每帧跟新可视Note和Note位置
        private void ApplyFrame(LiveFrame frame)
        {
            HashSet<int> visibleNoteIds = new HashSet<int>();

            foreach (RuntimeNote note in frame.ChartFrame.VisibleNotes)
            {
                visibleNoteIds.Add(note.Id);

                LiveNoteView view = GetOrCreateView(note);

                if (view == null)
                {
                    continue;
                }

                if (noteLayout == null)
                {
                    continue;
                }
                
                float musicTime = frame.ChartFrame.MusicTime;
                float width = noteLayout.CalculateNoteWidth(note);

                if (note is LongNote longNote)
                {
                    if (longNote.Category == ChartNoteCategory.FreeLong
                        || longNote.Category == ChartNoteCategory.SliderLong)
                    {
                        view.ShowFreeLong(
                            longNote,
                            noteLayout,
                            musicTime);
                    }
                    else
                    {
                        noteLayout.CalculateLongPosition(
                            longNote,musicTime,
                            out Vector2 startPosition,
                            out Vector2 endPosition);
                        
                        view.ShowLong(
                            startPosition,endPosition,width,musicTime<longNote.Time,
                            longNote.IsHolding);
                    }
                }
                else
                {
                    Vector2 position =
                        noteLayout.CalculateNotePosition(
                            note, musicTime);

                    view.Show(position, width);
                }
            }
            
            HideInvisibleNotes(visibleNoteIds);
        }

        //查看Note状态，如果是已经active就是返回active，如果不是则放入active池
        private LiveNoteView GetOrCreateView(RuntimeNote note)
        {
            if (activeViews.TryGetValue(note.Id, out LiveNoteView activeView))
            {
                return activeView;
            }

            if (freeViews.Count == 0)
            {
                return null;
            }
            
            LiveNoteView view = freeViews.Dequeue();
            activeViews.Add(note.Id, view);

            return view;
        }
        
        //把上一帧还在显示、但这一帧已经不该显示的 NoteView 回收到对象池里
        private void HideInvisibleNotes(HashSet<int> visibleNoteIds)
        {
            List<int> removeIds = new List<int>();

            //C# 不允许你在 foreach 遍历一个 Dictionary 的同时修改它
            //两阶段删除
            //第一轮：遍历 activeViews，只记录哪些要删
            foreach (KeyValuePair<int, LiveNoteView> pair in activeViews)
            {
                if (visibleNoteIds.Contains(pair.Key))
                {
                    continue;
                }

                pair.Value.Hide();
                freeViews.Enqueue(pair.Value);
                removeIds.Add(pair.Key);
            }

            //第二轮：真正从 activeViews 删除
            foreach (int noteId in removeIds)
            {
                activeViews.Remove(noteId);
            }
        }

        private void ApplyJudgment(JudgmentResult result)
        {
            if (result.CompletesNote)
            {
                HideNote(result.Note.Id);
            }
        }

        private void HideNote(int noteId)
        {
            if (!activeViews.TryGetValue(noteId, out LiveNoteView view))
            {
                return;
            }
            
            view.Hide();
            freeViews.Enqueue(view);
            activeViews.Remove(noteId);
        }
        
        private void ResolveDependencies()
        {
            if (presentationRoot == null)
            {
                presentationRoot = GetComponentInParent<LivePresentationRoot>();
            }

            if (presentationRoot == null)
            {
                return;
            }
            liveSceneController = presentationRoot.LiveSceneController;
            noteLayout = presentationRoot.LiveNoteLayout;
        }
    }
}