using System;
using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
	public class LongNoteMesh
	{
		// ライン片側の左右端点
		private struct TwoPoint
		{
			public Vector2 Left;

			public Vector2 Right;
		}

		// メッシュ分割点ごとのライン生成情報
		private struct Setting
		{
			public float CurrentViewProgress;

			public TwoPoint LanePosition;

			public float LineProgress;
		}

		// 隣り合う分割点から作る台形ポリゴンの端点
		private struct Trapezoid
		{
			public TwoPoint Top;

			public TwoPoint Bottom;
		}

		// 台形を横方向に分割する数
		private static readonly int TCount = 3;

		// 1 本のラインを最大何分割するか
		private static int SplitMeshCount = 100;

		// 事前確保する台形メッシュ数
		private static int PoolCount = SplitMeshCount * 10;

		// 1 台形あたりの頂点数
		private static readonly int TrapezoidVertexCount = 4 * TCount - 4;

		// 1 台形あたりのインデックス数
		private static readonly int TrapezoidIndexCount = 6 * TCount;

		// ライン幅を少し内側へ寄せるための余白
		private const float LineWidthPadding = 0.1f;

		// 押下中ラインの頂点色
		private static readonly Color PressColor = Color.white;

		// 離した後ラインの頂点色
		private static readonly Color ReleaseColor = Color.white;

		// 左右ラインに渡す UV オフセット値
		private static readonly Vector2[] OffsetValue =
		{
			new Vector2(0f, 0.125f),
			new Vector2(0.875f, 1f)
		};

		// ライン中心の進行率を外部ロジックから取得する関数
		private readonly Func<float, INote, float> getLineProgress;

		// Mesh を保持する MeshFilter
		private MeshFilter filter;

		// 更新対象の Mesh
		private Mesh mesh;

		private Vector3[] vertices;

		private Vector2[] uv;

		private Vector2[] leftPoint;

		private Vector2[] rightPoint;

		private Vector2[] uvOffsetLeft;

		private Vector2[] uvOffsetRight;

		private Color[] colors;

		private int[] triangles;

		private Vector2 statusValue;

		private Setting[] meshSettingBuffer;

		private Trapezoid[] trapezoidVertexInfo;

		// 現在使用している台形メッシュ数
		private int meshCount;

		// ライン進行率計算関数を受け取ってロングノーツメッシュ生成器を作成する
		// getLineProgress: ライン中心進行率を補正する関数
		public LongNoteMesh(Func<float, INote, float> getLineProgress)
		{
			this.getLineProgress = getLineProgress;
		}

		// MeshFilter、MeshRenderer、頂点配列を準備する
		// owner: Mesh コンポーネントを持たせる GameObject
		// texture: ラインに使うテクスチャ
		// sourceMaterial: 元になるマテリアル
		// alphaMultiplier: 透明度倍率
		public void Setup(GameObject owner, Texture texture, Material sourceMaterial, float alphaMultiplier = 1f)
		{
			if (owner == null)
			{
				return;
			}

			filter = owner.GetComponent<MeshFilter>();
			if (filter == null)
			{
				filter = owner.AddComponent<MeshFilter>();
			}

			MeshRenderer meshRenderer = owner.GetComponent<MeshRenderer>();
			if (meshRenderer == null)
			{
				meshRenderer = owner.AddComponent<MeshRenderer>();
			}

			Material material = sourceMaterial != null ? new Material(sourceMaterial) : CreateFallbackMaterial();
			if (material != null)
			{
				if (texture != null)
				{
					if (material.HasProperty("Texture"))
					{
						material.SetTexture("Texture", texture);
					}
					material.mainTexture = texture;
				}
				float alpha = Mathf.Clamp01(LiveConfig.LongNoteAlpha * alphaMultiplier);
				material.color = new Color(1f, 1f, 1f, alpha);
				meshRenderer.material = material;
				meshRenderer.sortingOrder = 29;
			}

			trapezoidVertexInfo = new Trapezoid[2];
			meshSettingBuffer = new Setting[SplitMeshCount + 10];
			SetupVertexArray();
			mesh = new Mesh { name = "NoteLineMesh" };
			mesh.MarkDynamic();
			ApplyMeshData();
			filter.sharedMesh = mesh;
			meshCount = 0;
		}

		// 現在のラインメッシュをすべて消去する
		public void Clear()
		{
			ClearVertex(0, PoolCount);
			meshCount = 0;
			ApplyMeshData();
		}

		// 表示対象ノーツ一覧からラインメッシュを再構築する
		// longNotes: ライン表示対象のロング系ノーツ
		// time: 現在の楽曲時間
		public void Execute(IList<NoteBase> longNotes, float time)
		{
			if (longNotes == null)
			{
				return;
			}

			int previousMeshCount = meshCount;
			meshCount = 0;
			for (int i = 0; i < longNotes.Count; i++)
			{
				NextExecute(longNotes[i], time, ref meshCount);
				if (meshCount >= PoolCount)
				{
					meshCount = PoolCount;
					break;
				}
			}

			if (previousMeshCount > meshCount)
			{
				ClearVertex(meshCount, previousMeshCount - meshCount);
			}

			ApplyMeshData();
		}

		// 1 つの親ノーツに含まれる表示ノーツ列から必要なライン片を生成する
		// baseNote: ライン生成元の親ノーツ
		// time: 現在の楽曲時間
		// currentMeshCount: 使用中メッシュ数
		private void NextExecute(INote baseNote, float time, ref int currentMeshCount)
		{
			List<INote> viewNotes = baseNote != null ? baseNote.ViewNoteList : null;
			if (viewNotes == null || viewNotes.Count < 2)
			{
				return;
			}

			INote startNote = viewNotes[0];
			for (int i = 1; i < viewNotes.Count; i++)
			{
				INote endNote = viewNotes[i];
				if (endNote == null)
				{
					continue;
				}
				if (startNote == null)
				{
					startNote = endNote;
					continue;
				}

				if (startNote.Progress > endNote.Progress)
				{
					int meshDataCount = GetCenterPosition(time, startNote, endNote);
					if (meshDataCount >= 2)
					{
						if (currentMeshCount + meshDataCount > PoolCount)
						{
							currentMeshCount += meshDataCount;
							return;
						}
						MeshCreate(ref currentMeshCount, time, baseNote, meshDataCount);
					}
				}
				startNote = endNote;
			}
		}

		// 計算済み分割点から台形メッシュを作成する
		// currentMeshCount: 使用中メッシュ数
		// time: 現在の楽曲時間
		// baseNote: ライン生成元の親ノーツ
		// meshDataCount: 使用する分割点数
		private void MeshCreate(ref int currentMeshCount, float time, INote baseNote, int meshDataCount)
		{
			if (meshSettingBuffer == null || meshDataCount < 2)
			{
				return;
			}

			SetStatusValue(time, baseNote);
			Color color = baseNote != null && baseNote.State == NoteState.InputRelease ? ReleaseColor : PressColor;
			for (int i = 0; i < meshDataCount - 1; i++)
			{
				if (currentMeshCount >= PoolCount)
				{
					return;
				}

				int vertexIndex = currentMeshCount * TrapezoidVertexCount;
				CalculateTrapezoidVertex(meshSettingBuffer[i], meshSettingBuffer[i + 1]);
				UpdateVertex(vertexIndex, color);
				UpdateUV(vertexIndex, i);
				currentMeshCount++;
			}
		}

		// 始点ノーツと終点ノーツの間を分割し、中心位置と左右端点を計算する
		// time: 現在の楽曲時間
		// startNote: ライン片の始点ノーツ
		// endNote: ライン片の終点ノーツ
		private int GetCenterPosition(float time, INote startNote, INote endNote)
		{
			if (startNote == null || endNote == null || meshSettingBuffer == null)
			{
				return 0;
			}

			float startProgress = Mathf.Clamp01(startNote.Progress);
			float endProgress = Mathf.Clamp01(endNote.Progress);
			int splitCount = Mathf.CeilToInt((startProgress - endProgress) * SplitMeshCount);
			if (splitCount <= 0)
			{
				return 0;
			}

			splitCount = Mathf.Min(splitCount, meshSettingBuffer.Length - 1);
			float startCenterLane = GetCenterLane(startNote);
			float endCenterLane = GetCenterLane(endNote);
			float startLaneSpan = GetLaneSpan(startNote);
			float endLaneSpan = GetLaneSpan(endNote);
			List<INote> viewNotes = startNote.ParentNote != null ? startNote.ParentNote.ViewNoteList : startNote.ViewNoteList;
			int viewNoteIndex = viewNotes != null ? Mathf.Max(viewNotes.IndexOf(startNote), 0) : 0;
			int viewNoteMaxIndex = viewNotes != null ? viewNotes.Count - 1 : 0;

			for (int i = 0; i <= splitCount && i < meshSettingBuffer.Length; i++)
			{
				float t = splitCount == 0 ? 0f : (float)i / splitCount;
				float lineProgress = getLineProgress != null ? getLineProgress(t, startNote) : t;
				float viewProgress = Mathf.Lerp(startProgress, endProgress, t);

				float centerLane = Mathf.Lerp(startCenterLane, endCenterLane, lineProgress);
				Vector2 centerPosition = GetJudgmentPosition(centerLane);
				Vector2 laneSpawnPosition = new Vector2(centerPosition.x, LiveConfig.SpawnPosition.y);
				centerPosition = LiveUtility.EarlyVec2Lerp(laneSpawnPosition, centerPosition, viewProgress);

				float laneSpan = Mathf.Lerp(startLaneSpan, endLaneSpan, lineProgress);
				float lineHalfWidth = CalcLineHalfWidth(laneSpan);
				meshSettingBuffer[i] = new Setting
				{
					CurrentViewProgress = 1f,
					LanePosition = new TwoPoint
					{
						Left = new Vector2(centerPosition.x - lineHalfWidth, centerPosition.y),
						Right = new Vector2(centerPosition.x + lineHalfWidth, centerPosition.y)
					},
					LineProgress = viewNoteMaxIndex > 0 ? (t + viewNoteIndex) / viewNoteMaxIndex : 0f
				};
			}

			return splitCount + 1;
		}

		// 接続対象ノーツの中心レーンを取得する
		// note: 中心レーンを求めるノーツ
		private static float GetCenterLane(INote note)
		{
			INote connectionNote = GetConnectionNote(note);
			return connectionNote != null ? (connectionNote.LaneStartF + connectionNote.LaneEndF) * 0.5f : 0f;
		}

		// 接続対象ノーツのレーン幅を取得する
		// note: レーン幅を求めるノーツ
		private static float GetLaneSpan(INote note)
		{
			INote connectionNote = GetConnectionNote(note);
			return connectionNote != null ? Mathf.Abs(connectionNote.LaneEndF - connectionNote.LaneStartF) : 0f;
		}

		// 表示上の接続元として扱うノーツを取得する
		// note: 判定するノーツ
		private static INote GetConnectionNote(INote note)
		{
			if (note == null)
			{
				return null;
			}

			// 連結ガイドは各区間が自分の移動レーンを持つ。
			// 通過後に前区間の親へ置き換えると、始点が中継位置に固定される。
			if (note is GuideNote)
			{
				return note;
			}

			return note.ParentNote != null && note.Progress >= 1f ? note.ParentNote : note;
		}

		// レーン幅からライン半幅を計算する
		// laneSpan: ノーツがまたぐレーン幅
		private static float CalcLineHalfWidth(float laneSpan)
		{
			return (laneSpan + 1f) * LiveConfig.widthX - LineWidthPadding;
		}

		// メッシュ生成に使う頂点、UV、色、インデックス配列を確保する
		private void SetupVertexArray()
		{
			int vertexCount = PoolCount * TrapezoidVertexCount;
			vertices = new Vector3[vertexCount];
			leftPoint = new Vector2[vertexCount];
			rightPoint = new Vector2[vertexCount];
			uvOffsetLeft = new Vector2[vertexCount];
			uvOffsetRight = new Vector2[vertexCount];
			uv = new Vector2[vertices.Length];
			colors = new Color[vertices.Length];
			triangles = new int[PoolCount * TrapezoidIndexCount];
			ClearVertex(0, PoolCount);

			for (int i = 0; i < PoolCount; i++)
			{
				int vertexBase = i * TrapezoidVertexCount;
				for (int j = 0; j < TrapezoidVertexCount; j++)
				{
					uv[vertexBase + j] = new Vector2(0f, j & 1);
					colors[vertexBase + j] = Color.white;
				}

				int indexBase = i * TrapezoidIndexCount;
				int triangleVertex = vertexBase + 3;
				for (int j = 0; j < TCount; j++)
				{
					int triangleBase = indexBase + j * 6;
					triangles[triangleBase] = triangleVertex - 3;
					triangles[triangleBase + 1] = triangleVertex - 2;
					triangles[triangleBase + 2] = triangleVertex - 1;
					triangles[triangleBase + 3] = triangleVertex - 2;
					triangles[triangleBase + 4] = triangleVertex;
					triangles[triangleBase + 5] = triangleVertex - 1;
					triangleVertex += 2;
				}
			}
		}

		// ノーツ状態に応じてシェーダーへ渡す状態値を更新する
		// time: 現在の楽曲時間
		// baseNote: 状態値の基準になる親ノーツ
		private void SetStatusValue(float time, INote baseNote)
		{
			if (baseNote == null)
			{
				statusValue = Vector2.zero;
				return;
			}

			float status = 0f;
			if (baseNote is NoteBase noteBase && noteBase.HasJudgment && baseNote.State >= NoteState.Input)
			{
				float t = (time - baseNote.MusicScoreInfo.time) * Mathf.PI * 4f;
				status = 0.5f - Mathf.Cos(t) * 0.5f;
			}

			statusValue.x = status;
			statusValue.y = 0.525f;
		}

		// 隣接する 2 分割点から左右の台形端点を計算する
		// startSetting: 始点側のライン情報
		// endSetting: 終点側のライン情報
		private void CalculateTrapezoidVertex(Setting startSetting, Setting endSetting)
		{
			if (trapezoidVertexInfo == null || trapezoidVertexInfo.Length < 2)
			{
				trapezoidVertexInfo = new Trapezoid[2];
			}

			float startWidth = startSetting.CurrentViewProgress;
			float endWidth = endSetting.CurrentViewProgress;

			trapezoidVertexInfo[0].Top.Right = endSetting.LanePosition.Left;
			trapezoidVertexInfo[0].Top.Left = endSetting.LanePosition.Left + new Vector2(-endWidth, 0f);
			trapezoidVertexInfo[0].Bottom.Right = startSetting.LanePosition.Left;
			trapezoidVertexInfo[0].Bottom.Left = startSetting.LanePosition.Left + new Vector2(-startWidth, 0f);

			trapezoidVertexInfo[1].Top.Right = endSetting.LanePosition.Right + new Vector2(endWidth, 0f);
			trapezoidVertexInfo[1].Top.Left = endSetting.LanePosition.Right;
			trapezoidVertexInfo[1].Bottom.Right = startSetting.LanePosition.Right + new Vector2(startWidth, 0f);
			trapezoidVertexInfo[1].Bottom.Left = startSetting.LanePosition.Right;
		}

		// 計算済み台形端点を頂点配列へ書き込む
		// vertexIndex: 書き込み開始頂点 index
		// color: 設定する頂点色
		private void UpdateVertex(int vertexIndex, Color color)
		{
			if (trapezoidVertexInfo == null)
			{
				return;
			}

			for (int i = 0; i < trapezoidVertexInfo.Length; i++)
			{
				int index = vertexIndex + i * 4;
				if (index + 3 >= vertices.Length)
				{
					return;
				}

				Trapezoid trapezoid = trapezoidVertexInfo[i];
				SetVertex(index + 3, trapezoid.Top.Right, trapezoid.Top.Left.x, trapezoid.Top.Right.x, i, color);
				SetVertex(index + 1, trapezoid.Top.Left, trapezoid.Top.Left.x, trapezoid.Top.Right.x, i, color);
				SetVertex(index + 2, trapezoid.Bottom.Right, trapezoid.Bottom.Left.x, trapezoid.Bottom.Right.x, i, color);
				SetVertex(index, trapezoid.Bottom.Left, trapezoid.Bottom.Left.x, trapezoid.Bottom.Right.x, i, color);
			}
		}

		// 台形メッシュの UV 進行率を更新する
		// vertexIndex: 書き込み開始頂点 index
		// meshDataIndex: 分割点バッファの index
		private void UpdateUV(int vertexIndex, int meshDataIndex)
		{
			if (uv == null || meshSettingBuffer == null || meshDataIndex + 1 >= meshSettingBuffer.Length)
			{
				return;
			}

			float start = meshSettingBuffer[meshDataIndex].LineProgress;
			float end = meshSettingBuffer[meshDataIndex + 1].LineProgress;
			for (int i = 0; i < 2; i++)
			{
				int index = vertexIndex + i * 4;
				if (index + 3 >= uv.Length)
				{
					return;
				}

				Vector2 v0 = uv[index];
				Vector2 v1 = uv[index + 1];
				Vector2 v2 = uv[index + 2];
				Vector2 v3 = uv[index + 3];
				v0.y = start;
				v2.y = start;
				v1.y = end;
				v3.y = end;
				uv[index] = v0;
				uv[index + 1] = v1;
				uv[index + 2] = v2;
				uv[index + 3] = v3;
			}
		}

		// 1 頂点分の位置、左右端、UV オフセット、色を設定する
		// index: 更新する頂点 index
		// position: 頂点位置
		// left: 左端 X 座標
		// right: 右端 X 座標
		// sideIndex: 左右どちらのラインかを示す index
		// color: 頂点色
		private void SetVertex(int index, Vector2 position, float left, float right, int sideIndex, Color color)
		{
			vertices[index] = new Vector3(position.x, position.y, 0f);
			if (leftPoint != null && index < leftPoint.Length)
			{
				leftPoint[index] = new Vector2(0f, left);
			}
			if (rightPoint != null && index < rightPoint.Length)
			{
				rightPoint[index] = new Vector2(0f, right);
			}

			Vector2 offset = OffsetValue != null && sideIndex < OffsetValue.Length ? OffsetValue[sideIndex] : Vector2.zero;
			if (uvOffsetLeft != null && index < uvOffsetLeft.Length)
			{
				uvOffsetLeft[index] = new Vector2(statusValue.x, offset.x);
			}
			if (uvOffsetRight != null && index < uvOffsetRight.Length)
			{
				uvOffsetRight[index] = new Vector2(statusValue.y, offset.y);
			}
			if (colors != null && index < colors.Length)
			{
				colors[index] = color;
			}
		}

		// 指定範囲の頂点情報を空にする
		// startIndex: 消去開始メッシュ index
		// length: 消去するメッシュ数
		private void ClearVertex(int startIndex, int length)
		{
			if (vertices == null)
			{
				return;
			}

			int vertexStart = Mathf.Clamp(startIndex, 0, PoolCount) * TrapezoidVertexCount;
			int vertexLength = Mathf.Clamp(length, 0, PoolCount - startIndex) * TrapezoidVertexCount;
			for (int i = vertexStart; i < vertexStart + vertexLength && i < vertices.Length; i++)
			{
				vertices[i] = Vector3.zero;
				if (leftPoint != null && i < leftPoint.Length)
				{
					leftPoint[i] = Vector2.zero;
				}
				if (rightPoint != null && i < rightPoint.Length)
				{
					rightPoint[i] = Vector2.zero;
				}
				if (uvOffsetLeft != null && i < uvOffsetLeft.Length)
				{
					uvOffsetLeft[i] = Vector2.zero;
				}
				if (uvOffsetRight != null && i < uvOffsetRight.Length)
				{
					uvOffsetRight[i] = Vector2.zero;
				}
				if (colors != null && i < colors.Length)
				{
					colors[i] = Color.clear;
				}
			}
		}

		// 現在の配列内容を Mesh へ反映する
		private void ApplyMeshData()
		{
			if (mesh == null)
			{
				return;
			}

			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.uv = uv;
			mesh.uv2 = leftPoint;
			mesh.uv3 = rightPoint;
			mesh.uv4 = uvOffsetLeft;
			mesh.uv5 = uvOffsetRight;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
			if (filter != null)
			{
				filter.sharedMesh = mesh;
			}
		}

		// 小数レーンを含む判定位置を補間して取得する
		// lane: 取得したいレーン位置
		private static Vector2 GetJudgmentPosition(float lane)
		{
			Vector2[] positions = LiveConfig.JudgmentPositions;
			if (positions == null || positions.Length == 0)
			{
				return Vector2.zero;
			}

			float clampedLane = Mathf.Clamp(lane, 0f, positions.Length - 1);
			int start = Mathf.FloorToInt(clampedLane);
			int end = Mathf.CeilToInt(clampedLane);
			float t = Mathf.Clamp01(clampedLane - start);
			return LiveUtility.EarlyVec2Lerp(positions[start], positions[end], t);
		}

		// 専用シェーダーがない場合のフォールバック Material を作成する
		private static Material CreateFallbackMaterial()
		{
			Shader shader = Shader.Find("FFrameWork/Unlit/NoteLine");
			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}
			return shader != null ? new Material(shader) : null;
		}
	}
}
