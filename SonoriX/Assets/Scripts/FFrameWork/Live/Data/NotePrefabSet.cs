using System;
using UnityEngine;

namespace FFrameWork.Live
{
	[Serializable]
	// ノート種別と表示 Prefab の対応
	public class NotePrefabSet
	{
		// 対応するノート種別
		public NoteCategory category;

		// デフォルトで使用する表示 Prefab
		public GameObject defaultPrefab;
	}
}
