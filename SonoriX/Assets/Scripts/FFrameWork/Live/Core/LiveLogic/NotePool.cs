using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
	// ノート表示オブジェクトを再利用するためのプール
	public class NotePool : MonoBehaviour
	{
		// プールで生成するノート表示 Prefab
		private GameObject notePrefab;

		// プール内のノート表示一覧
		private List<BaseNoteView> noteArray;

		// 次に使用するプール内インデックス
		private int nextSpawinIndex;

		// 指定数のノート表示を事前生成する
		// poolCount: 事前生成する表示数
		// notePrefab: 生成に使用するノート Prefab
		public void Setup(int poolCount, GameObject notePrefab)
		{
			this.notePrefab = notePrefab;
			nextSpawinIndex = 0;
			noteArray = new List<BaseNoteView>();
			for (int i = 0; i < poolCount; i++)
			{
				CreateNote();
			}
		}

		// Prefab からノート表示を 1 つ生成してプールへ追加する
		private BaseNoteView CreateNote()
		{
			if (notePrefab == null)
			{
				return null;
			}

			GameObject noteObject = Instantiate(notePrefab, transform);
			BaseNoteView noteView = noteObject != null ? noteObject.GetComponent<BaseNoteView>() : null;
			if (noteView == null)
			{
				if (noteObject != null)
				{
					Destroy(noteObject);
				}
				return null;
			}

			noteView.Setup(LiveConfig.SpawnPosition, LiveConfig.JudgmentPositions);
			noteView.Unspawn();
			noteArray.Add(noteView);
			return noteView;
		}

		// 指定ノート用の表示をプールから取得して出現させる
		// note: 表示対象のノート
		public BaseNoteView Spawn(INote note)
		{
			if (noteArray == null)
			{
				noteArray = new List<BaseNoteView>();
			}

			if (noteArray.Count == 0)
			{
				CreateNote();
			}
			if (noteArray.Count == 0)
			{
				return null;
			}

			BaseNoteView noteView = noteArray[nextSpawinIndex];
			nextSpawinIndex = (nextSpawinIndex + 1) % noteArray.Count;
			if (noteView != null && noteView.gameObject.activeSelf)
			{
				noteView = CreateNote();
			}

			if (noteView == null)
			{
				return null;
			}

			float z = note.MusicScoreInfo.time * 0.001f + note.DefaultLeftLane * 0.0000001f;
			noteView.Spawn(note, z);
			return noteView;
		}
	}
}
