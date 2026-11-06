using System.Collections.Generic;
using FFrameWork;
using UnityEngine;

namespace FFrameWork.Live
{
	// ノートの表示生成、表示更新、プール、ロングノート線をまとめて管理するクラス
	public class NotesViewManager
	{
		// 生成中ノート辞書の初期容量
		private const int DefaultNoteDictCapacity = 128;

		// ガイドライン表示の透明度倍率
		private const float GuideLineAlphaMultiplier = 0.35f;

		// 生成中のノートと、それに対応する表示オブジェクトの辞書
		private Dictionary<INote, BaseNoteView> spawnNoteDict;

		// ノート表示オブジェクトをまとめる親 Transform
		private Transform noteRoot;

		// ノート種別ごとの表示プール
		private Dictionary<NoteCategory, NotePool> notePoolDict;

		// 通常ロングノート用のライン表示
		private NoteLineView noteLineView;

		// 固定レーンロングノート用のライン表示
		private NoteLineView fixedNoteLineView;

		// スライダーロングノート用のライン表示
		private SliderLineView sliderLineView;

		// ボリュームロングノートの下方向ライン表示
		private VolumeLongLineView volumeLongDownLineView;

		// ボリュームロングノートの上方向ライン表示
		private VolumeLongLineView volumeLongUpLineView;

		// ガイドノート用のライン表示
		private NoteLineView guideLineView;

		// ノート種別ごとの表示 Prefab
		private Dictionary<NoteCategory, GameObject> notePrefabDict;

		// 通常ロングノート線のテクスチャ
		private Texture longNoteLineTexture;

		// 固定レーンロングノート線のテクスチャ
		private Texture fLongNoteLineTexture;

		// スライダーロングノート線のテクスチャ
		private Texture sliderLongLineTexture;

		// ボリュームロングノート下方向線のテクスチャ
		private Texture volumeLongDownLineTexture;

		// ボリュームロングノート上方向線のテクスチャ
		private Texture volumeLongUpLineTexture;

		// ガイドライン用のテクスチャ
		private Texture guideLineTexture;

		// 通常ロングノート線のマテリアル
		private Material longNoteLineMaterial;

		// 固定レーンロングノート線のマテリアル
		private Material fLongNoteLineMaterial;

		// ガイドライン用のマテリアル
		private Material guideLineMaterial;

		// スライダーロングノート線のマテリアル
		private Material sliderLongLineMaterial;

		// ボリュームロングノート線の共通マテリアル
		private Material volumeLongLineMaterial;

		// ボリュームロングノート下方向線のマテリアル
		private Material volumeLongDownLineMaterial;

		// ボリュームロングノート上方向線のマテリアル
		private Material volumeLongUpLineMaterial;

		// ノート表示用のルートと生成中ノート辞書を初期化する
		// liveRoot: NoteRoot をぶら下げる親 Transformnull の場合は親なしで生成する
		public void Setup(Transform liveRoot)
		{
			spawnNoteDict = new Dictionary<INote, BaseNoteView>(DefaultNoteDictCapacity);

			GameObject noteRootObject = new GameObject("NoteRoot");
			noteRoot = noteRootObject.transform;
			if (liveRoot != null)
			{
				noteRoot.SetParent(liveRoot, false);
			}
		}

		// ノート表示とロングノート線で使用する Prefab、Texture、Material を設定する
		// notePrefabs: ノート種別ごとの表示 Prefab 設定
		// longNoteLineTexture: 通常ロングノート線のテクスチャ
		// fLongNoteLineTexture: 固定レーンロングノート線のテクスチャnull の場合は通常線を使う
		// sliderLongLineTexture: スライダーロングノート線のテクスチャnull の場合は通常線を使う
		// volumeLongDownLineTexture: ボリューム下方向線のテクスチャnull の場合はスライダー線を使う
		// volumeLongUpLineTexture: ボリューム上方向線のテクスチャnull の場合は下方向線を使う
		// guideLineTexture: ガイドライン用のテクスチャ
		// longNoteLineMaterial: 通常ロングノート線のマテリアル
		// fLongNoteLineMaterial: 固定レーンロングノート線のマテリアルnull の場合は通常線を使う
		// guideLineMaterial: ガイドライン用のマテリアルnull の場合は通常線を使う
		// sliderLongLineMaterial: スライダーロングノート線のマテリアルnull の場合は通常線を使う
		// volumeLongLineMaterial: ボリューム線の共通マテリアルnull の場合はスライダー線を使う
		// volumeLongDownLineMaterial: ボリューム下方向線のマテリアルnull の場合は共通マテリアルを使う
		// volumeLongUpLineMaterial: ボリューム上方向線のマテリアルnull の場合は共通マテリアルを使う
		public void SetAssets(
			NotePrefabSet[] notePrefabs,
			Texture longNoteLineTexture,
			Texture fLongNoteLineTexture,
			Texture sliderLongLineTexture,
			Texture volumeLongDownLineTexture,
			Texture volumeLongUpLineTexture,
			Texture guideLineTexture,
			Material longNoteLineMaterial,
			Material fLongNoteLineMaterial,
			Material guideLineMaterial,
			Material sliderLongLineMaterial,
			Material volumeLongDownLineMaterial,
			Material volumeLongUpLineMaterial)
		{
			notePrefabDict = new Dictionary<NoteCategory, GameObject>();
			if (notePrefabs != null)
			{
				for (int i = 0; i < notePrefabs.Length; i++)
				{
					NotePrefabSet set = notePrefabs[i];
					if (set == null)
					{
						continue;
					}

					if (set.defaultPrefab != null)
					{
						notePrefabDict[set.category] = set.defaultPrefab;
					}
				}
			}

			this.longNoteLineTexture = longNoteLineTexture;
			this.fLongNoteLineTexture = fLongNoteLineTexture != null ? fLongNoteLineTexture : longNoteLineTexture;
			this.sliderLongLineTexture = sliderLongLineTexture != null ? sliderLongLineTexture : longNoteLineTexture;
			this.volumeLongDownLineTexture = volumeLongDownLineTexture != null ? volumeLongDownLineTexture : this.sliderLongLineTexture;
			this.volumeLongUpLineTexture = volumeLongUpLineTexture != null ? volumeLongUpLineTexture : this.volumeLongDownLineTexture;
			this.guideLineTexture = guideLineTexture;
			this.longNoteLineMaterial = longNoteLineMaterial;
			this.fLongNoteLineMaterial = fLongNoteLineMaterial != null ? fLongNoteLineMaterial : longNoteLineMaterial;
			this.guideLineMaterial = guideLineMaterial != null ? guideLineMaterial : longNoteLineMaterial;
			this.sliderLongLineMaterial = sliderLongLineMaterial != null ? sliderLongLineMaterial : longNoteLineMaterial;
			this.volumeLongDownLineMaterial = volumeLongDownLineMaterial != null ? volumeLongDownLineMaterial : this.volumeLongLineMaterial;
			this.volumeLongUpLineMaterial = volumeLongUpLineMaterial != null ? volumeLongUpLineMaterial : this.volumeLongLineMaterial;
		}

		// ノート種別ごとの表示プールとロングノート線表示を生成する
		// baseController: ライブ全体の設定と譜面情報を持つコントローラー
		// noteCountDictionary: ノート種別ごとの必要プール数
		public void CreateNotePool(BaseLiveController baseController, Dictionary<NoteCategory, int> noteCountDictionary)
		{
			if (baseController == null || noteRoot == null || notePoolDict != null)
			{
				return;
			}

			notePoolDict = new Dictionary<NoteCategory, NotePool>(11);

			AddPoolSet(noteCountDictionary, NoteCategory.FreeLaneNormal, "NormalNotePool");
			AddPoolSet(noteCountDictionary, NoteCategory.FreeLaneLong, "LongNotePool");
			AddPoolSet(noteCountDictionary, NoteCategory.SliderLong, "SliderLongNotePool");
			AddPoolSet(noteCountDictionary, NoteCategory.SliderFlick, "FlickNotePool");
			AddPoolSet(noteCountDictionary, NoteCategory.VolumeFlick, "VolumeFlickNotePool");
			AddPoolSet(noteCountDictionary, NoteCategory.VolumeFlickLong, "VolumeLongNotePool");
			AddPoolSet(noteCountDictionary,NoteCategory.FixedNormal,"FNormalNotePool");
			AddPoolSet(noteCountDictionary,NoteCategory.FixedLong,"FLongNotePool");
			
			noteLineView = CreateLineView<NoteLineView>("NoteLineView");
			noteLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, longNoteLineTexture, longNoteLineMaterial);

			fixedNoteLineView = CreateLineView<NoteLineView>("FNoteLineView");
			fixedNoteLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, fLongNoteLineTexture, fLongNoteLineMaterial);

			sliderLineView = CreateLineView<SliderLineView>("SliderLineView");
			sliderLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, sliderLongLineTexture, sliderLongLineMaterial);

			volumeLongDownLineView = CreateLineView<VolumeLongLineView>("VolumeLongDownLineView");
			volumeLongDownLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, volumeLongDownLineTexture, volumeLongDownLineMaterial);
			volumeLongDownLineView?.SetDirectionFilter(NoteDirection.Left);

			volumeLongUpLineView = CreateLineView<VolumeLongLineView>("VolumeLongUpLineView");
			volumeLongUpLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, volumeLongUpLineTexture, volumeLongUpLineMaterial);
			volumeLongUpLineView?.SetDirectionFilter(NoteDirection.Right);
			
			guideLineView = CreateLineView<NoteLineView>("GuideLineView");
			guideLineView?.Setup(baseController.BootData != null ? baseController.BootData.BundleBuildData : null, guideLineTexture, guideLineMaterial, GuideLineAlphaMultiplier);
		}

		// 生成中ノートの表示位置とロングノート線を更新する
		// time: 現在の楽曲時間
		public void OnUpdate(float time)
		{
			if (spawnNoteDict != null)
			{
				foreach (BaseNoteView noteView in spawnNoteDict.Values)
				{
					noteView?.Move();
				}
			}

			noteLineView?.Excute(time);
			fixedNoteLineView?.Excute(time);
			sliderLineView?.Excute(time);
			volumeLongDownLineView?.Excute(time);
			volumeLongUpLineView?.Excute(time);
			guideLineView?.Excute(time);
		}

		// 生成中のノート表示とライン表示をすべて解除する
		public void Clear()
		{
			if (spawnNoteDict != null)
			{
				foreach (BaseNoteView noteView in spawnNoteDict.Values)
				{
					noteView?.Unspawn();
				}
				spawnNoteDict.Clear();
			}

			noteLineView?.Clear();
			fixedNoteLineView?.Clear();
			sliderLineView?.Clear();
			volumeLongDownLineView?.Clear();
			volumeLongUpLineView?.Clear();
			guideLineView?.Clear();
		}

		// 指定されたノートの表示を生成し、必要ならロングノート線にも追加する
		// note: 表示生成対象のノート
		public void SpawnNote(INote note)
		{
			if (note == null || spawnNoteDict == null || spawnNoteDict.ContainsKey(note))
			{
				return;
			}

			if (note is GuideNote guideNote)
			{
				guideLineView?.Add(guideNote);
				return;
			}

			if (note is LongNote longNote)
			{
				AddLongLine(longNote);
			}

			NoteCategory category = LiveUtility.GetNoteViewPoolCategory(note.Category);
			if (notePoolDict != null &&
				notePoolDict.TryGetValue(category, out NotePool pool))
			{
				BaseNoteView noteView = pool.Spawn(note);
				if (noteView != null)
				{
					spawnNoteDict.Add(note, noteView);
				}
			}

		}

		// 指定されたノートの表示を解除し、必要ならロングノート線からも削除する
		// note: 表示解除対象のノート
		public void UnspawnNote(INote note)
		{
			if (note == null)
			{
				return;
			}

			if (note is GuideNote guideNote)
			{
				guideLineView?.Remove(guideNote);
				return;
			}

			if (note is LongNote longNote)
			{
				RemoveLongLine(longNote);
			}

			if (spawnNoteDict != null && spawnNoteDict.TryGetValue(note, out BaseNoteView noteView))
			{
				noteView?.Unspawn();
				spawnNoteDict.Remove(note);
			}
		}

		// 指定されたノート種別の表示プールを辞書へ追加する
		// noteCountDictionary: ノート種別ごとの必要プール数
		// category: 追加するノート種別
		// poolName: 生成するプール GameObject の名前
		private void AddPoolSet(Dictionary<NoteCategory, int> noteCountDictionary, NoteCategory category, string poolName)
		{
			notePoolDict[category] = CreateNotePool(GetPoolCount(noteCountDictionary, category), poolName, GetPrefab(category));
		}

		// ノート表示用のプール GameObject を生成する
		// poolCount: 事前生成する表示オブジェクト数
		// poolName: 生成するプール GameObject の名前
		// notePrefab: プールで使用するノート表示 Prefab
		private NotePool CreateNotePool(int poolCount, string poolName, GameObject notePrefab)
		{
			GameObject poolObject = new GameObject(poolName);
			NotePool notePool = poolObject.AddComponent<NotePool>();
			if (noteRoot != null)
			{
				notePool.transform.SetParent(noteRoot, false);
			}

			if (notePrefab != null)
			{
				notePool.Setup(Mathf.Max(poolCount, 1), notePrefab);
			}
			return notePool;
		}

		// ノート種別に対応する Prefab を取得する
		// category: Prefab を取得するノート種別
		private GameObject GetPrefab(NoteCategory category)
		{
			if (notePrefabDict == null)
			{
				return null;
			}
			if (notePrefabDict.TryGetValue(category, out GameObject prefab))
			{
				return prefab;
			}
			Debug.LogError($"[NotesViewManager] Missing note prefab for {category}. Assign it in LiveFrontView notePrefabs.");
			return null;
		}

		// ノート種別に対応するプール数を取得する
		// noteCountDictionary: ノート種別ごとの必要プール数
		// category: プール数を取得するノート種別
		private static int GetPoolCount(Dictionary<NoteCategory, int> noteCountDictionary, NoteCategory category)
		{
			if (noteCountDictionary != null && noteCountDictionary.TryGetValue(category, out int count))
			{
				return count;
			}
			return 0;
		}

		// ノート種別に対応するロングノート線表示を取得する
		// category: ライン表示を取得するノート種別
		private NoteLineView GetLineView(NoteCategory category)
		{
			if (category == NoteCategory.FixedLong)
			{
				return fixedNoteLineView;
			}
			if (category == NoteCategory.SliderLong)
			{
				return sliderLineView;
			}
			return noteLineView;
		}

		// ロングノートを対応するライン表示へ追加する
		// longNote: ライン表示に追加するロングノート
		private void AddLongLine(LongNote longNote)
		{
			if (longNote == null)
			{
				return;
			}
			if (longNote.Category == NoteCategory.VolumeFlickLong)
			{
				volumeLongDownLineView?.Add(longNote);
				volumeLongUpLineView?.Add(longNote);
				return;
			}

			GetLineView(longNote.Category)?.Add(longNote);
		}

		// ロングノートを対応するライン表示から削除する
		// longNote: ライン表示から削除するロングノート
		private void RemoveLongLine(LongNote longNote)
		{
			if (longNote == null)
			{
				return;
			}
			if (longNote.Category == NoteCategory.VolumeFlickLong)
			{
				volumeLongDownLineView?.Remove(longNote);
				volumeLongUpLineView?.Remove(longNote);
				return;
			}

			GetLineView(longNote.Category)?.Remove(longNote);
		}

		// 指定した型のライン表示コンポーネントを持つ GameObject を生成する
		// objectName: 生成するライン表示 GameObject の名前
		private T CreateLineView<T>(string objectName) where T : Component
		{
			GameObject lineObject = new GameObject(objectName);
			T lineView = lineObject.AddComponent<T>();
			if (noteRoot != null)
			{
				lineView.transform.SetParent(noteRoot, false);
			}
			return lineView;
		}
	}
}
