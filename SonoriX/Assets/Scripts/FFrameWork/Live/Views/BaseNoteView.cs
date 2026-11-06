using UnityEngine;

namespace FFrameWork.Live
{
	public abstract class BaseNoteView : MonoBehaviour
	{
		// ノーツが出現する基準位置
		protected Vector2 spawnPosition;

		// 各レーンの判定位置
		protected Vector2[] judgmentPositions;

		// ノーツ表示に使う SpriteRenderer
		[SerializeField]
		protected SpriteRenderer spriteRenderer;

		// 現在表示しているノーツ
		protected INote note;

		// ノーツ表示に使う Z 座標
		protected float posZ;

		// Prefab の基準スケール
		protected Vector3 prefabScale = Vector3.one;

		// SpriteRenderer の初期 SortingOrder
		protected int defaultSortingOrder;

		// ノーツビューの基準座標と判定位置を初期化する
		// spawnPosition: ノーツ生成位置
		// judgmentPositions: 各レーンの判定位置
		public virtual void Setup(Vector2 spawnPosition, Vector2[] judgmentPositions)
		{
			this.spawnPosition = spawnPosition;
			this.judgmentPositions = judgmentPositions;
			prefabScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
			defaultSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
		}

		// ノーツ進行率に合わせて表示位置と素材パラメータを更新する
		public virtual void Move()
		{
			if (note == null)
			{
				return;
			}

			if (note.Progress <= 0f)
			{
				transform.localScale = Vector3.zero;
				return;
			}

			float progress = Mathf.Clamp01(note.Progress);
			transform.localPosition = CalcNotePosition(ref progress);
			UpdateNoteSpriteSize();
			UpdateMaterialParameters(progress);
		}

		// 指定ノーツを表示対象として有効化する
		// note: 表示対象のノーツ
		// posZ: ノーツ表示に使う Z 座標
		public virtual void Spawn(INote note, float posZ)
		{
			this.note = note;
			this.posZ = posZ;
			gameObject.SetActive(true);

			if (note == null || judgmentPositions == null || spriteRenderer == null)
			{
				return;
			}

			Color color = spriteRenderer.color;
			color.r = 1f;
			color.g = 1f;
			color.b = 1f;
			spriteRenderer.color = color;
			spriteRenderer.sortingOrder = defaultSortingOrder + GetSortingOrderOffset(note);
			UpdateNoteSpriteSize();
		}

		// ノーツ表示を解除し、初期表示状態へ戻す
		public virtual void Unspawn()
		{
			note = null;
			if (spriteRenderer != null)
			{
				spriteRenderer.sortingOrder = defaultSortingOrder;
			}
			gameObject.SetActive(false);
			transform.localScale = Vector3.zero;
		}

		// 派生クラスで素材パラメータ変更を受け取るための入口
		// vector: 変更に使うパラメータ値
		public virtual void Change(Vector2 vector)
		{
		}

		// 現在の SpriteRenderer へ素材パラメータを反映する
		// progress: ノーツの表示進行率
		protected void UpdateMaterialParameters(float progress)
		{
			UpdateMaterialParameters(spriteRenderer, progress);
		}

		// 指定 SpriteRenderer へ素材パラメータを反映する
		// target: 反映対象の SpriteRenderer
		// progress: ノーツの表示進行率
		protected void UpdateMaterialParameters(SpriteRenderer target, float progress)
		{
			if (note == null || judgmentPositions == null || target == null)
			{
				return;
			}

			target.color = new Color(1f, 1f, 1f, target.color.a);
			Change(new Vector2(progress, 1f));
		}

		// シェーダーへ渡すためのレーン幅や進行率を Color にパックする
		// alpha: 表示アルファ値
		// progress: ノーツの表示進行率
		protected Color CreateMaterialParameterColor(float alpha, float progress)
		{
			float width = note.DefaultRightLane - note.DefaultLeftLane;
			float laneCount = judgmentPositions.Length;
			float center = (note.DefaultLeftLane + note.DefaultRightLane) * 0.5f;
			Color color = new Color(1f, 1f, progress, alpha);
			color.r = (center + 0.5f) / laneCount;
			color.g = (width + 0.5f) / laneCount;
			return EncodeMaterialParameterColor(color);
		}

		// リニア色空間でも SpriteRenderer 頂点カラーが元値としてシェーダーへ届くよう補正する
		// color: パラメータとして使う頂点カラー
		private static Color EncodeMaterialParameterColor(Color color)
		{
			if (QualitySettings.activeColorSpace != ColorSpace.Linear)
			{
				return color;
			}

			// ノーツシェーダーは SpriteRenderer の頂点 RGB を表示色ではなくパック済みパラメータとして使う
			// Unity の sRGB から Linear への頂点カラー変換を相殺し、元の値をシェーダーへ渡す
			color.r = Mathf.LinearToGammaSpace(color.r);
			color.g = Mathf.LinearToGammaSpace(color.g);
			color.b = Mathf.LinearToGammaSpace(color.b);
			return color;
		}

		// 小数レーンを含むレーン位置を補間して取得する
		// lane: 取得したいレーン位置
		protected Vector2 GetLanePosition(float lane)
		{
			if (judgmentPositions == null || judgmentPositions.Length == 0)
			{
				return Vector2.zero;
			}

			float clampedLane = Mathf.Clamp(lane, 0f, judgmentPositions.Length - 1);
			int start = Mathf.FloorToInt(clampedLane);
			int end = Mathf.CeilToInt(clampedLane);
			float t = Mathf.Clamp01(clampedLane - start);
			return Vector2.Lerp(judgmentPositions[start], judgmentPositions[end], t);
		}

		// レーン間を補間してノーツ表示位置を計算する
		// progress: ノーツの表示進行率
		// flipZWhenInput: 互換用引数現状は Z 反転に使用しない
		protected Vector3 CalcLaneInterpolatedNotePosition(ref float progress, bool flipZWhenInput)
		{
			if (note == null)
			{
				return new Vector3(spawnPosition.x, spawnPosition.y, posZ);
			}

			float centerLane = (note.LaneStartF + note.LaneEndF) * 0.5f;
			Vector2 target = GetLanePosition(centerLane);
			Vector2 laneSpawnPosition = new Vector2(target.x, spawnPosition.y);
			Vector2 position = LiveUtility.EarlyVec2Lerp(laneSpawnPosition, target, progress);
			// Z 座標は呼び出し側が指定した値をそのまま使う
			float z = posZ;
			return new Vector3(position.x, position.y, z);
		}

		// ノーツのレーン幅に合わせて SpriteRenderer サイズと Transform スケールを更新する
		protected void UpdateNoteSpriteSize()
		{
			if (note == null || spriteRenderer == null)
			{
				return;
			}

			float laneWidth = GetNoteSpriteLaneWidth();
			Vector2 spriteSize = LiveUtility.CalcSpriteRendererSize(laneWidth - 1f);
			spriteRenderer.size = spriteSize;

			transform.localScale = new Vector3(prefabScale.x * laneWidth, prefabScale.y, prefabScale.z);
		}

		// ノーツ表示に使うレーン幅を取得する
		protected virtual float GetNoteSpriteLaneWidth()
		{
			if (note == null)
			{
				return 1f;
			}

			// DefaultRightLane は範囲に含まれるため、距離ではなくレーン数として幅を計算する
			return Mathf.Max(1f, note.DefaultRightLane - note.DefaultLeftLane + 1f);
		}

		// ノーツ種別ごとに SortingOrder を調整するための加算値を返す
		// note: 表示対象のノーツ
		protected virtual int GetSortingOrderOffset(INote note)
		{
			return 0;
		}

		// 通常ノーツの表示位置を計算する
		// progress: ノーツの表示進行率
		protected virtual Vector3 CalcNotePosition(ref float progress)
		{
			float centerLane = (note.DefaultLeftLane + note.DefaultRightLane) * 0.5f;
			Vector2 target = GetLanePosition(centerLane);
			Vector2 laneSpawnPosition = new Vector2(target.x, spawnPosition.y);
			Vector2 position = LiveUtility.EarlyVec2Lerp(laneSpawnPosition, target, progress);
			return new Vector3(position.x, position.y, posZ);
		}
	}
}
