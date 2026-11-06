using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
	public class NoteLineView : MonoBehaviour
	{
		// ライン表示対象として管理しているロング系ノーツ
		protected List<NoteBase> longNotes;

		// ロングノーツラインを描画するメッシュ生成器
		private LongNoteMesh longNoteMesh;

		// ライン表示に必要なメッシュと素材を初期化する
		// bundleBuildData: ライブ表示用のビルド設定
		// texture: ラインに使うテクスチャ
		// sourceMaterial: 元になるマテリアル
		// alphaMultiplier: ライン透明度の倍率
		public void Setup(LiveBundleBuildData bundleBuildData, Texture texture, Material sourceMaterial, float alphaMultiplier = 1f)
		{
			longNotes = new List<NoteBase>(64);
			longNoteMesh = new LongNoteMesh(GetLineProgress);
			longNoteMesh.Setup(gameObject, texture, sourceMaterial, alphaMultiplier);
		}

		// ロングノーツをライン表示対象へ追加する
		// note: 追加するロングノーツ
		public virtual void Add(LongNote note)
		{
			if (note != null && longNotes != null && !longNotes.Contains(note))
			{
				longNotes.Add(note);
			}
		}

		// ロングノーツをライン表示対象から削除する
		// note: 削除するロングノーツ
		public virtual void Remove(LongNote note)
		{
			if (note != null && longNotes != null)
			{
				longNotes.Remove(note);
			}
		}

		// ガイドノーツをライン表示対象へ追加する
		// note: 追加するガイドノーツ
		public void Add(GuideNote note)
		{
			if (note != null && longNotes != null && !longNotes.Contains(note))
			{
				longNotes.Add(note);
			}
		}

		// ガイドノーツをライン表示対象から削除する
		// note: 削除するガイドノーツ
		public void Remove(GuideNote note)
		{
			if (note != null && longNotes != null)
			{
				longNotes.Remove(note);
			}
		}

		// ライン表示対象と生成済みメッシュを消去する
		public void Clear()
		{
			longNotes?.Clear();
			longNoteMesh?.Clear();
		}

		// 現在時刻でラインメッシュを更新する
		// time: 現在の楽曲時間
		public void Excute(float time)
		{
			longNoteMesh?.Execute(longNotes, time);
		}

		// ライン中心の進行率を取得する
		// t: 現在のライン進行率
		// startNote: ライン開始側のノーツ
		protected virtual float GetLineProgress(float t, INote startNote)
		{
			return LiveConfig.GetNoteLineCenterProgress(t, startNote);
		}
	}
}
