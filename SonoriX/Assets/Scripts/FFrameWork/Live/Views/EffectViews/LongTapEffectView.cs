using System.Collections.Generic;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
	public class LongTapEffectView : MonoBehaviour
	{
		// ロングホールド中の Aura と Gen エフェクトをまとめて扱うペア
		public class EffectPair
		{
			// 継続表示用の Aura エフェクト
			public ParticleSystemController Aura;

			// 開始表示用の Gen エフェクト
			public ParticleSystemController Gen;

			// ループ SE の再生 ID
			public uint PlaybackId;

			// 現在プールから使用中か
			public bool IsUsed;

			// ロング SE 再生に使うサウンドプレイヤー
			private readonly ILiveSoundPlayer liveSoundPlayer;

			// エフェクト全体に掛けるスケール
			private readonly float effectScale;

			// Prefab 回転を上書きする場合の回転値
			private readonly Quaternion effectRotation;

			// Prefab の回転を上書きするか
			private readonly bool overridePrefabRotation;

			// エフェクト Prefab からペアを生成する
			public EffectPair(
				GameObject genPrefab,
				GameObject auraPrefab,
				Transform parent,
				BaseLiveController liveController,
				ILiveSoundPlayer liveSoundPlayer,
				float effectScale,
				Vector3 effectEulerAngles,
				bool overridePrefabRotation,
				bool useTapEffectProjection,
				bool forceLocalParticleAlignment,
				bool forceLocalSimulationSpace)
			{
				this.liveSoundPlayer = liveSoundPlayer;
				this.effectScale = effectScale;
				effectRotation = Quaternion.Euler(effectEulerAngles);
				this.overridePrefabRotation = overridePrefabRotation;
				Gen = CreateEffect(genPrefab, parent, liveController, useTapEffectProjection, forceLocalParticleAlignment, forceLocalSimulationSpace);
				Aura = CreateEffect(auraPrefab, parent, liveController, useTapEffectProjection, forceLocalParticleAlignment, forceLocalSimulationSpace);
				Stop();
			}

			// 指定ノーツ位置でロングホールドエフェクトを再生する
			// note: 表示対象のロングノーツ
			// seVolume: 現在フレームの SE 音量倍率
			public void Play(INote note, float seVolume)
			{
				IsUsed = true;
				SetTransform(note);
				bool shouldStart = (Aura != null && !Aura.IsPlaying) || (Gen != null && !Gen.IsPlaying);
				if (shouldStart)
				{
					Aura?.Play();
					Gen?.Play();
					PlaySE(note, seVolume);
				}
			}

			// 指定ノーツの中心位置と幅に合わせてエフェクト Transform を更新する
			// note: 表示対象のロングノーツ
			public void SetTransform(INote note)
			{
				Vector3 position = GetNoteCenterPosition(note);
				if (Aura != null)
				{
					Aura.transform.localPosition = position;
					if (overridePrefabRotation)
					{
						Aura.transform.localRotation = effectRotation;
					}
					float width = note.LaneEndF + 1f - note.LaneStartF;
					Aura.transform.localScale = new Vector3(width * effectScale, effectScale, effectScale);
				}
				if (Gen != null)
				{
					Gen.transform.localPosition = position;
					if (overridePrefabRotation)
					{
						Gen.transform.localRotation = effectRotation;
					}
					Gen.transform.localScale = Vector3.one * effectScale;
				}
			}

			// エフェクトを未使用に戻して再生中 SE も停止する
			public void Stop()
			{
				IsUsed = false;
				StopPlayback();
			}

			// Particle と SE の再生だけを停止する
			public void StopPlayback()
			{
				Aura?.Stop();
				Gen?.Stop();
				if (PlaybackId != 0)
				{
					liveSoundPlayer?.StopSE(PlaybackId);
					PlaybackId = 0;
				}
			}

			// ロングホールド SE を再生する
			// note: 再生基準のノーツ
			// seVolume: 現在フレームの SE 音量倍率
			private void PlaySE(INote note, float seVolume)
			{
				if (note == null || liveSoundPlayer == null)
				{
					return;
				}
				string cueName = LiveSoundDefine.SE_LIVE_LONG;
				if (seVolume <= 0f)
				{
					seVolume = 1f;
				}
				PlaybackId = liveSoundPlayer.PlayIngameSE(cueName, seVolume);
			}

			// Prefab から ParticleSystemController 付きのエフェクトを生成する
			private static ParticleSystemController CreateEffect(
				GameObject prefab,
				Transform parent,
				BaseLiveController liveController,
				bool useTapEffectProjection,
				bool forceLocalParticleAlignment,
				bool forceLocalSimulationSpace)
			{
				if (prefab == null)
				{
					return null;
				}

				GameObject instance = Object.Instantiate(prefab, parent, false);
				instance.name = prefab.name;
				ConfigureParticleSystems(instance, forceLocalSimulationSpace);
				ConfigureParticleRenderers(instance, useTapEffectProjection, forceLocalParticleAlignment);
				ParticleSystemController controller = instance.AddComponent<ParticleSystemController>();
				controller.RegisterToLiveController(liveController);
				return controller;
			}

			// 必要に応じて ParticleSystem の SimulationSpace を Local に揃える
			// instance: 設定対象のエフェクトインスタンス
			// forceLocalSimulationSpace: Local 指定を強制するか
			private static void ConfigureParticleSystems(GameObject instance, bool forceLocalSimulationSpace)
			{
				if (instance == null || !forceLocalSimulationSpace)
				{
					return;
				}

				ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
				for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
				{
					ParticleSystem particleSystem = particleSystems[i];
					if (particleSystem == null)
					{
						continue;
					}

					ParticleSystem.MainModule main = particleSystem.main;
					main.simulationSpace = ParticleSystemSimulationSpace.Local;
				}
			}

			// ParticleSystemRenderer の描画空間と反転行列パラメータを設定する
			// instance: 設定対象のエフェクトインスタンス
			// useTapEffectProjection: タップエフェクト用の投影補正を使うか
			// forceLocalParticleAlignment: Local alignment を強制するか
			private static void ConfigureParticleRenderers(GameObject instance, bool useTapEffectProjection, bool forceLocalParticleAlignment)
			{
				if (instance == null)
				{
					return;
				}

				ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
				for (int i = 0; renderers != null && i < renderers.Length; i++)
				{
					ParticleSystemRenderer renderer = renderers[i];
					if (renderer == null)
					{
						continue;
					}

					if (forceLocalParticleAlignment)
					{
						renderer.alignment = ParticleSystemRenderSpace.Local;
					}

					Material[] materials = renderer.materials;
					for (int j = 0; materials != null && j < materials.Length; j++)
					{
						Material material = materials[j];
						if (material != null && material.HasProperty(UseFlipMatrixPropertyName))
						{
							material.SetFloat(UseFlipMatrixPropertyName, useTapEffectProjection ? 1f : 0f);
						}
					}
					renderer.materials = materials;
				}
			}
		}

		// ロングホールド中の Aura Prefab
		[SerializeField] private GameObject holdAuraPrefab;

		// ロングホールド開始時の Gen Prefab
		[SerializeField] private GameObject holdGenPrefab;

		// エフェクトペアのプール数
		[SerializeField] private int poolCount = 16;

		// エフェクト表示スケール
		[SerializeField] private float effectScale = 1f;

		// エフェクト回転の上書き値
		[SerializeField] private Vector3 effectEulerAngles = new Vector3(0f, 0f, 0f);

		// Prefab の回転を上書きするか
		[SerializeField] private bool overridePrefabRotation = true;

		// タップエフェクト投影補正を使うか
		[SerializeField] private bool useTapEffectProjection;

		// ParticleRenderer の Local alignment を強制するか
		[SerializeField] private bool forceLocalParticleAlignment = true;

		// ParticleSystem の Local SimulationSpace を強制するか
		[SerializeField] private bool forceLocalSimulationSpace = true;

		// 反転行列を使うかを指定するマテリアルプロパティ名
		private const string UseFlipMatrixPropertyName = "_UseFlipMatrix";

		// ロングタップエフェクトのプール
		private EffectPair[] tapKeeps;

		// 表示中ロングノーツとエフェクトペアの対応
		private Dictionary<INote, EffectPair> longNotes;

		// ロングホールド SE 再生に使うサウンドプレイヤー
		private ILiveSoundPlayer liveSoundPlayer;

		// ロングタップエフェクトの Prefab、プール、サウンド参照を初期化する
		public void Setup(GameObject holdAuraPrefab = null, GameObject holdGenPrefab = null, int poolCount = 16, BaseLiveController liveController = null, ILiveSoundPlayer liveSoundPlayer = null)
		{
			if (holdAuraPrefab != null)
			{
				this.holdAuraPrefab = holdAuraPrefab;
			}
			if (holdGenPrefab != null)
			{
				this.holdGenPrefab = holdGenPrefab;
			}
			this.poolCount = Mathf.Max(1, poolCount);
			this.liveSoundPlayer = liveSoundPlayer != null
				? liveSoundPlayer
				: FindLiveSoundPlayer(liveController);
			longNotes = new Dictionary<INote, EffectPair>();
			tapKeeps = CreatePairs("LongTapKeep", this.holdGenPrefab, this.holdAuraPrefab, this.poolCount, liveController);
		}

		// ライブコントローラー配下から ILiveSoundPlayer を探す
		// liveController: 探索元のライブコントローラー
		private static ILiveSoundPlayer FindLiveSoundPlayer(BaseLiveController liveController)
		{
			if (liveController == null)
			{
				return null;
			}

			MonoBehaviour[] behaviours = liveController.GetComponentsInChildren<MonoBehaviour>(true);
			for (int i = 0; behaviours != null && i < behaviours.Length; i++)
			{
				if (behaviours[i] is ILiveSoundPlayer soundPlayer)
				{
					return soundPlayer;
				}
			}
			return null;
		}

		// 親ロングノーツをエフェクト管理対象へ追加する
		// note: 追加するロングノーツ
		public void Add(INote note)
		{
			// ガイドなど判定を持たない表示専用ノーツにはホールド演出を割り当てない。
			if (!(note is LongNote longNote) || !longNote.HasJudgment ||
				note.ParentNote != null || longNotes == null || longNotes.ContainsKey(note))
			{
				return;
			}

			EffectPair pair = GetFreePair();
			if (pair == null)
			{
				return;
			}

			pair.IsUsed = true;
			longNotes[note] = pair;
		}

		// ロングノーツのエフェクト管理を解除する
		// note: 解除するロングノーツ
		public void Remove(INote note)
		{
			if (note == null || longNotes == null)
			{
				return;
			}

			if (longNotes.TryGetValue(note, out EffectPair pair))
			{
				pair.Stop();
				longNotes.Remove(note);
			}
		}

		// すべてのロングタップエフェクトを停止して管理状態を消去する
		public void Clear()
		{
			if (longNotes != null)
			{
				foreach (EffectPair pair in longNotes.Values)
				{
					pair?.Stop();
				}
				longNotes.Clear();
			}

			StopPairs(tapKeeps);
		}

		// ロングノーツ状態に応じてエフェクト再生と停止を更新する
		// time: 現在の楽曲時間
		// seVolume: 現在フレームの SE 音量倍率
		public void Excute(float time, float seVolume)
		{
			if (longNotes == null || longNotes.Count == 0)
			{
				return;
			}

			List<INote> removeList = null;
			foreach (KeyValuePair<INote, EffectPair> pair in longNotes)
			{
				INote note = pair.Key;
				EffectPair effect = pair.Value;
				if (note == null || effect == null)
				{
					if (removeList == null)
					{
						removeList = new List<INote>();
					}
					removeList.Add(note);
					continue;
				}

				if (note.State == NoteState.InputBegan || note.State == NoteState.Input)
				{
					effect.Play(note, seVolume);
				}
				else if (note.State == NoteState.InputRelease)
				{
					effect.StopPlayback();
				}
			}

			for (int i = 0; removeList != null && i < removeList.Count; i++)
			{
				Remove(removeList[i]);
			}
		}

		// 指定数のエフェクトペアを持つプールを作成する
		private EffectPair[] CreatePairs(string rootName, GameObject genPrefab, GameObject auraPrefab, int count, BaseLiveController liveController)
		{
			Transform root = CreateChild(rootName);
			EffectPair[] pairs = new EffectPair[count];
			for (int i = 0; i < pairs.Length; i++)
			{
				pairs[i] = new EffectPair(
					genPrefab,
					auraPrefab,
					root,
					liveController,
					liveSoundPlayer,
					Mathf.Max(0.01f, effectScale),
					effectEulerAngles,
					overridePrefabRotation,
					useTapEffectProjection,
					forceLocalParticleAlignment,
					forceLocalSimulationSpace);
			}
			return pairs;
		}

		// 未使用のエフェクトペアを取得する
		private EffectPair GetFreePair()
		{
			EffectPair[] pairs = tapKeeps;
			for (int i = 0; pairs != null && i < pairs.Length; i++)
			{
				if (pairs[i] != null && !pairs[i].IsUsed)
				{
					return pairs[i];
				}
			}
			return null;
		}

		// 指定名の子 Transform を取得または作成する
		// objectName: 子 GameObject 名
		private Transform CreateChild(string objectName)
		{
			Transform child = transform.Find(objectName);
			if (child != null)
			{
				return child;
			}

			GameObject gameObject = new GameObject(objectName);
			child = gameObject.transform;
			child.SetParent(transform, false);
			return child;
		}

		// ノーツの中心レーンに対応するローカル座標を取得する
		// note: 位置を求めるノーツ
		private static Vector3 GetNoteCenterPosition(INote note)
		{
			float lane = (note.LaneStartF + note.LaneEndF) * 0.5f;
			Vector2[] positions = LiveConfig.JudgmentPositions;
			if (positions == null || positions.Length == 0)
			{
				return new Vector3(LiveUtility.CalcLaneTransformX(lane), LiveConfig.JudgmentPositionY, 0f);
			}

			float clampedLane = Mathf.Clamp(lane, 0f, positions.Length - 1);
			int start = Mathf.FloorToInt(clampedLane);
			int end = Mathf.CeilToInt(clampedLane);
			float t = Mathf.Clamp01(clampedLane - start);
			Vector2 position = Vector2.Lerp(positions[start], positions[end], t);
			return new Vector3(position.x, position.y, 0f);
		}

		// プール内のエフェクトペアをすべて停止する
		// pairs: 停止対象のエフェクトペア配列
		private static void StopPairs(EffectPair[] pairs)
		{
			for (int i = 0; pairs != null && i < pairs.Length; i++)
			{
				pairs[i]?.Stop();
			}
		}
	}
}
