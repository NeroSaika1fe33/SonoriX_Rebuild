using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
	public class LiveSoundPlayer : MonoBehaviour, ILiveSoundPlayer
	{
		[Serializable]
		public class CueClip
		{
			// コードから指定する SE キュー名
			public string cueName;

			// 単一クリップ指定時に再生する AudioClip
			public AudioClip clip;

			// 複数クリップ指定時に順番に再生する AudioClip 配列
			public AudioClip[] clips;

			// キュー単位の音量倍率
			[Range(0f, 2f)] public float volume = 1f;

			// 複数クリップ再生時に次に試すクリップ位置
			private int nextClipIndex;

			// 再生可能なクリップを取得し、複数指定時は次回位置を進める
			public AudioClip GetClip()
			{
				if (clips != null && clips.Length > 0)
				{
					for (int i = 0; i < clips.Length; i++)
					{
						int index = (nextClipIndex + i) % clips.Length;
						if (clips[index] == null)
						{
							continue;
						}

						nextClipIndex = (index + 1) % clips.Length;
						return clips[index];
					}
				}

				return clip;
			}

			// 単一または複数クリップのどちらかに有効な AudioClip があるかを返す
			public bool HasClip()
			{
				if (clip != null)
				{
					return true;
				}

				for (int i = 0; clips != null && i < clips.Length; i++)
				{
					if (clips[i] != null)
					{
						return true;
					}
				}
				return false;
			}
		}

		// ワンショット SE を再生する AudioSource
		[SerializeField] private AudioSource oneShotSource;

		// Inspector で設定する SE キュー一覧
		[SerializeField] private CueClip[] cueClips;

		// すべての SE に掛けるマスター音量倍率
		[SerializeField] private float masterVolume = 1f;

		// 未設定キューを Warning として出すか
		[SerializeField] private bool logMissingCue;

		// キュー名から AudioClip 設定を引くための辞書
		private readonly Dictionary<string, CueClip> cueClipDict = new Dictionary<string, CueClip>();

		// 停止操作のために保持しているループ再生用 AudioSource
		private readonly Dictionary<uint, AudioSource> playingSources = new Dictionary<uint, AudioSource>();

		// PlayIngameSE が返す再生 ID の採番値
		private uint nextPlaybackId = 1;

		// Inspector 追加時に代表的な SE キューを初期値として用意する
		private void Reset()
		{
			if (cueClips != null && cueClips.Length > 0)
			{
				return;
			}

			cueClips = new[]
			{
				CreateCueClip(LiveSoundDefine.SE_LIVE_TAP),
				CreateCueClip(LiveSoundDefine.SE_LIVE_GOOD),
				CreateCueClip(LiveSoundDefine.SE_LIVE_GREAT),
				CreateCueClip(LiveSoundDefine.SE_LIVE_PERFECT),
				CreateCueClip(LiveSoundDefine.SE_LIVE_LONG),
				CreateCueClip(LiveSoundDefine.SE_LIVE_CONNECT),
				CreateCueClip(LiveSoundDefine.SE_LIVE_FLICK),
				CreateCueClip(LiveSoundDefine.SE_LIVE_TRACE)
			};
		}

		// 起動時に AudioSource とキュー辞書を準備する
		private void Awake()
		{
			Setup();
		}

		// AudioSource を確保し、Inspector のキュー配列から検索用辞書を再構築する
		public void Setup()
		{
			EnsureOneShotSource();
			cueClipDict.Clear();
			if (cueClips == null)
			{
				return;
			}

			for (int i = 0; i < cueClips.Length; i++)
			{
				CueClip cueClip = cueClips[i];
				if (cueClip == null || string.IsNullOrEmpty(cueClip.cueName) || !cueClip.HasClip())
				{
					continue;
				}

				cueClipDict[cueClip.cueName] = cueClip;
			}
		}

		// 指定キューの AudioClip をワンショット再生する
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側の音量倍率
		public void PlayIngameSEOneShot(string cueName, float seVolume = 1f)
		{
			if (!TryGetCueClip(cueName, out CueClip cueClip))
			{
				return;
			}

			EnsureOneShotSource();
			AudioClip clip = cueClip.GetClip();
			if (clip != null)
			{
				oneShotSource.PlayOneShot(clip, GetVolume(cueClip, seVolume));
			}
		}

		// 指定キューの AudioClip をループ再生し、管理用の再生 ID を返す
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側の音量倍率
		public uint PlayIngameSE(string cueName, float seVolume = 1f)
		{
			if (!TryGetCueClip(cueName, out CueClip cueClip))
			{
				return 0;
			}

			AudioSource source = CreateLoopSource(cueName, cueClip, seVolume);
			uint playbackId = nextPlaybackId++;
			if (nextPlaybackId == 0)
			{
				nextPlaybackId = 1;
			}

			playingSources[playbackId] = source;
			source.Play();
			return playbackId;
		}

		// 再生 ID に対応する AudioSource を停止して破棄する
		// playbackId: PlayIngameSE が返した再生 ID
		// allowFadeOut: 通常 AudioSource 実装では使用しないフェードアウト指定
		public void StopSE(uint playbackId, bool allowFadeOut = true)
		{
			if (playbackId == 0 || !playingSources.TryGetValue(playbackId, out AudioSource source))
			{
				return;
			}

			playingSources.Remove(playbackId);
			if (source == null)
			{
				return;
			}

			source.Stop();
			Destroy(source.gameObject);
		}

		// 通常 AudioSource 実装ではパラメータ制御を行わない
		// playbackId: PlayIngameSE が返した再生 ID
		// parameterName: 設定対象として要求されたパラメータ名
		// value: 設定値
		public void SetParameter(uint playbackId, string parameterName, float value)
		{
		}

		// 管理しているすべての AudioSource を停止して破棄する
		public void StopAllSE()
		{
			foreach (KeyValuePair<uint, AudioSource> pair in playingSources)
			{
				if (pair.Value != null)
				{
					pair.Value.Stop();
					Destroy(pair.Value.gameObject);
				}
			}

			playingSources.Clear();
		}

		// キュー名から有効な AudioClip 設定を取得する
		// cueName: 検索する SE キュー名
		// cueClip: 取得できた AudioClip 設定
		private bool TryGetCueClip(string cueName, out CueClip cueClip)
		{
			if (cueClipDict.Count == 0 && cueClips != null && cueClips.Length > 0)
			{
				Setup();
			}

			if (!string.IsNullOrEmpty(cueName) && cueClipDict.TryGetValue(cueName, out cueClip) && cueClip.HasClip())
			{
				return true;
			}

			if (logMissingCue && !string.IsNullOrEmpty(cueName))
			{
				Debug.LogWarning("LiveSoundPlayer cue is not assigned: " + cueName, this);
			}
			cueClip = null;
			return false;
		}

		// 指定したキュー名だけを持つ AudioClip 設定を作成する
		// cueName: 初期設定する SE キュー名
		private static CueClip CreateCueClip(string cueName)
		{
			return new CueClip
			{
				cueName = cueName,
				volume = 1f
			};
		}

		// ループ再生用の子 GameObject と AudioSource を作成する
		// cueName: 作成する GameObject 名に含める SE キュー名
		// cueClip: 再生する AudioClip 設定
		// seVolume: 呼び出し側の音量倍率
		private AudioSource CreateLoopSource(string cueName, CueClip cueClip, float seVolume)
		{
			GameObject sourceObject = new GameObject("LiveSE_" + cueName);
			sourceObject.transform.SetParent(transform, false);

			AudioSource source = sourceObject.AddComponent<AudioSource>();
			source.clip = cueClip.GetClip();
			source.loop = true;
			source.playOnAwake = false;
			source.spatialBlend = 0f;
			source.volume = GetVolume(cueClip, seVolume);
			return source;
		}

		// マスター音量、呼び出し側音量、キュー音量を掛け合わせた最終音量を返す
		// cueClip: キュー単位の音量設定
		// seVolume: 呼び出し側の音量倍率
		private float GetVolume(CueClip cueClip, float seVolume)
		{
			return Mathf.Clamp01(masterVolume * seVolume * (cueClip != null ? cueClip.volume : 1f));
		}

		// ワンショット再生用 AudioSource を取得または作成する
		private void EnsureOneShotSource()
		{
			if (oneShotSource != null)
			{
				return;
			}

			oneShotSource = GetComponent<AudioSource>();
			if (oneShotSource == null)
			{
				oneShotSource = gameObject.AddComponent<AudioSource>();
			}
			oneShotSource.playOnAwake = false;
			oneShotSource.loop = false;
			oneShotSource.spatialBlend = 0f;
		}

		// 破棄時に残っている SE を停止する
		private void OnDestroy()
		{
			StopAllSE();
		}
	}
}
