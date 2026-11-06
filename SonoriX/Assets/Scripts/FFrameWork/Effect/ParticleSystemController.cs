using UnityEngine;

namespace FFrameWork.Live
{
	// ParticleSystem 群の再生、停止、フェード停止をまとめて扱う
	public class ParticleSystemController : MonoBehaviour
	{
		// 子階層から取得した ParticleSystem 一覧
		private ParticleSystem[] particleSystems;

		// フェードアウト停止中の Coroutine
		private Coroutine fadeCoroutine;

		// この Controller から再生開始した状態か
		private bool isPlayInternal;

		// 再生開始フレーム
		private int playFrame = -1;

		// 更新登録先のライブコントローラー
		private BaseLiveController liveController;

		// いずれかの ParticleSystem が再生中または粒子保持中か
		public bool IsPlaying
		{
			get
			{
				if (particleSystems == null)
				{
					CacheComponents();
				}

				for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
				{
					ParticleSystem target = particleSystems[i];
					if (target != null && (target.particleCount > 0 || target.isPlaying))
					{
						return true;
					}
				}
				return false;
			}
		}

		// 起動時に子 ParticleSystem をキャッシュし、停止状態へ初期化する
		private void Awake()
		{
			CacheComponents();
			StopParticleSystems();
			ClearParticleSystems();
		}

		// 親ライブコントローラーへ登録し、外部再生されていなければ停止する
		private void Start()
		{
			RegisterToLiveController(GetComponentInParent<BaseLiveController>());
			if (!isPlayInternal)
			{
				Stop();
			}
		}

		// ライブ更新側から呼ばれ、再生完了したエフェクトを停止状態へ戻す
		public void OnUpdate()
		{
			if (isPlayInternal && Time.frameCount > playFrame && !IsPlaying)
			{
				Stop();
			}
		}

		// 登録を解除して参照を解放する
		private void OnDestroy()
		{
			if (liveController != null)
			{
				liveController.UnregisterParticleSystemController(this);
				liveController = null;
			}

			particleSystems = null;
		}

		// 指定されたライブコントローラーへ更新対象として登録する
		public void RegisterToLiveController(BaseLiveController controller)
		{
			if (controller == null || liveController == controller)
			{
				return;
			}

			if (liveController != null)
			{
				liveController.UnregisterParticleSystemController(this);
			}

			liveController = controller;
			liveController.RegisterParticleSystemController(this);
		}

		// 全 ParticleSystem を先頭から再生する
		public void Play()
		{
			if (particleSystems == null)
			{
				CacheComponents();
			}

			CancelFade();
			if (isPlayInternal)
			{
				StopParticleSystems();
			}
			PlayParticleSystems();
			playFrame = Time.frameCount;
			isPlayInternal = true;
		}

		// 全 ParticleSystem を停止し、内部再生フラグを落とす
		public void Stop()
		{
			if (particleSystems == null)
			{
				CacheComponents();
			}

			CancelFade();
			StopParticleSystems();
			isPlayInternal = false;
		}

		// 既存粒子のアルファを下げてから停止する
		public void FadeOutAndStop(float duration)
		{
			if (particleSystems == null)
			{
				CacheComponents();
			}

			CancelFade();
			if (duration <= 0f)
			{
				Stop();
				return;
			}

			fadeCoroutine = StartCoroutine(FadeOutAndStopRoutine(duration));
		}

		// ホールド演出用にループを切り、寿命を安定させる
		public void ConfigureStableHold(float lifetime)
		{
			if (particleSystems == null)
			{
				CacheComponents();
			}

			for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
			{
				ParticleSystem target = particleSystems[i];
				if (target == null)
				{
					continue;
				}

				ParticleSystem.MainModule main = target.main;
				main.loop = false;
				main.startLifetime = Mathf.Max(0.1f, lifetime);
			}
		}

		// 子階層の ParticleSystem を取得し直す
		private void CacheComponents()
		{
			particleSystems = GetComponentsInChildren<ParticleSystem>(true);
		}

		// 粒子カラーを直接操作してフェードアウトを進める
		private System.Collections.IEnumerator FadeOutAndStopRoutine(float duration)
		{
			// Fade already-spawned particles directly so prefab materials do not need extra color properties.
			ParticleSystem.Particle[][] particles = new ParticleSystem.Particle[particleSystems != null ? particleSystems.Length : 0][];
			Color32[][] startColors = new Color32[particles.Length][];
			for (int i = 0; i < particles.Length; i++)
			{
				ParticleSystem target = particleSystems[i];
				if (target == null)
				{
					continue;
				}

				particles[i] = new ParticleSystem.Particle[target.main.maxParticles];
				int count = target.GetParticles(particles[i]);
				startColors[i] = new Color32[count];
				for (int j = 0; j < count; j++)
				{
					startColors[i][j] = particles[i][j].startColor;
				}
			}

			float elapsed = 0f;
			while (elapsed < duration)
			{
				float alphaRate = 1f - Mathf.Clamp01(elapsed / duration);
				ApplyParticleAlphaRate(particles, startColors, alphaRate);
				elapsed += Time.deltaTime;
				yield return null;
			}

			ApplyParticleAlphaRate(particles, startColors, 0f);
			fadeCoroutine = null;
			Stop();
		}

		// 保存した開始色を基準に粒子アルファを反映する
		private void ApplyParticleAlphaRate(ParticleSystem.Particle[][] particles, Color32[][] startColors, float alphaRate)
		{
			for (int i = 0; particles != null && i < particles.Length; i++)
			{
				ParticleSystem target = particleSystems != null && i < particleSystems.Length ? particleSystems[i] : null;
				if (target == null || particles[i] == null || startColors[i] == null)
				{
					continue;
				}

				int count = target.GetParticles(particles[i]);
				int colorCount = Mathf.Min(count, startColors[i].Length);
				for (int j = 0; j < colorCount; j++)
				{
					Color32 color = startColors[i][j];
					color.a = (byte)Mathf.RoundToInt(color.a * alphaRate);
					particles[i][j].startColor = color;
				}
				target.SetParticles(particles[i], count);
			}
		}

		// 実行中のフェードアウトを停止する
		private void CancelFade()
		{
			if (fadeCoroutine == null)
			{
				return;
			}

			StopCoroutine(fadeCoroutine);
			fadeCoroutine = null;
		}

		// キャッシュ済みの ParticleSystem をすべて再生する
		private void PlayParticleSystems()
		{
			for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
			{
				particleSystems[i]?.Play(true);
			}
		}

		// キャッシュ済みの ParticleSystem をすべて停止して粒子を消す
		private void StopParticleSystems()
		{
			for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
			{
				particleSystems[i]?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
		}

		// キャッシュ済みの ParticleSystem に残った粒子を消す
		private void ClearParticleSystems()
		{
			for (int i = 0; particleSystems != null && i < particleSystems.Length; i++)
			{
				particleSystems[i]?.Clear(true);
			}
		}
	}
}
