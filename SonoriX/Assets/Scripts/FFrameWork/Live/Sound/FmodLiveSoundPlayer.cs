using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace FFrameWork.Live
{
	public class FmodLiveSoundPlayer : MonoBehaviour, ILiveSoundPlayer
	{
		// FMOD インスタンスに設定する最小音量
		private const float MinVolume = 0f;

		// FMOD インスタンスに設定する最大音量
		private const float MaxVolume = 2f;

		[Serializable]
		public class CueEvent
		{
			// コードから指定する SE キュー名
			public string cueName;

			// 再生する FMOD EventReference
			public EventReference eventReference;

			// キュー単位の音量倍率
			[Range(0f, 2f)] public float volume = 1f;

			// 再生インスタンスをこの GameObject に追従させるか
			public bool attachToGameObject;

			// キュー名と FMOD イベントが再生可能な状態かを返す
			public bool IsValid()
			{
				return !string.IsNullOrEmpty(cueName) && !eventReference.IsNull;
			}
		}

		// Inspector で設定する FMOD キュー一覧
		[SerializeField] private CueEvent[] cueEvents;

		// すべての SE に掛けるマスター音量倍率
		[SerializeField] private float masterVolume = 1f;

		// 未設定キューや生成失敗を Warning として出すか
		[SerializeField] private bool logMissingCue = true;

		// キュー名から FMOD イベント設定を引くための辞書
		private readonly Dictionary<string, CueEvent> cueEventDict = new Dictionary<string, CueEvent>();

		// 停止やパラメータ設定のために保持している再生中インスタンス
		private readonly Dictionary<uint, EventInstance> playingInstances = new Dictionary<uint, EventInstance>();

		// PlayIngameSE が返す再生 ID の採番値
		private uint nextPlaybackId = 1;

		// 起動時に Inspector 設定を辞書化する
		private void Awake()
		{
			Setup();
		}

		// Inspector のキュー配列から検索用辞書を再構築する
		public void Setup()
		{
			cueEventDict.Clear();
			for (int i = 0; cueEvents != null && i < cueEvents.Length; i++)
			{
				CueEvent cueEvent = cueEvents[i];
				if (cueEvent != null && cueEvent.IsValid())
				{
					cueEventDict[cueEvent.cueName] = cueEvent;
				}
			}
		}

		// 指定キューの FMOD イベントをワンショット再生する
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側の音量倍率
		public void PlayIngameSEOneShot(string cueName, float seVolume = 1f)
		{
			if (!TryGetCueEvent(cueName, out CueEvent cueEvent))
			{
				return;
			}

			if (!TryCreateInstance(cueEvent, out EventInstance instance))
			{
				return;
			}

			SetupInstance(instance, cueEvent, seVolume);

			instance.start();
			instance.release();
		}

		// 指定キューの FMOD イベントを再生し、管理用の再生 ID を返す
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側の音量倍率
		public uint PlayIngameSE(string cueName, float seVolume = 1f)
		{
			if (!TryGetCueEvent(cueName, out CueEvent cueEvent))
			{
				return 0;
			}

			if (!TryCreateInstance(cueEvent, out EventInstance instance))
			{
				return 0;
			}

			SetupInstance(instance, cueEvent, seVolume);

			uint playbackId = nextPlaybackId++;
			if (nextPlaybackId == 0)
			{
				nextPlaybackId = 1;
			}

			playingInstances[playbackId] = instance;
			instance.start();
			return playbackId;
		}

		// 再生 ID に対応する FMOD イベントを停止して解放する
		// playbackId: PlayIngameSE が返した再生 ID
		// allowFadeOut: FMOD のフェードアウト停止を許可するか
		public void StopSE(uint playbackId, bool allowFadeOut = true)
		{
			if (playbackId == 0 || !playingInstances.TryGetValue(playbackId, out EventInstance instance))
			{
				return;
			}

			playingInstances.Remove(playbackId);
			StopAndRelease(ref instance, allowFadeOut);
		}

		// 再生中 FMOD イベントのパラメータを設定する
		// playbackId: PlayIngameSE が返した再生 ID
		// parameterName: FMOD パラメータ名
		// value: 設定する値
		public void SetParameter(uint playbackId, string parameterName, float value)
		{
			if (playbackId == 0 ||
				string.IsNullOrEmpty(parameterName) ||
				!playingInstances.TryGetValue(playbackId, out EventInstance instance) ||
				!instance.isValid())
			{
				return;
			}

			instance.setParameterByName(parameterName, value);
		}

		// 管理しているすべての FMOD イベントを即時停止する
		public void StopAllSE()
		{
			List<uint> ids = new List<uint>(playingInstances.Keys);
			for (int i = 0; i < ids.Count; i++)
			{
				StopSE(ids[i], false);
			}
		}

		// キュー名から有効な FMOD イベント設定を取得する
		// cueName: 検索する SE キュー名
		// cueEvent: 取得できた FMOD イベント設定
		private bool TryGetCueEvent(string cueName, out CueEvent cueEvent)
		{
			if (cueEventDict.Count == 0 && cueEvents != null && cueEvents.Length > 0)
			{
				Setup();
			}

			if (!string.IsNullOrEmpty(cueName) && cueEventDict.TryGetValue(cueName, out cueEvent) && cueEvent.IsValid())
			{
				return true;
			}

			if (logMissingCue && !string.IsNullOrEmpty(cueName))
			{
				Debug.LogWarning("FmodLiveSoundPlayer cue is not assigned: " + cueName, this);
			}
			cueEvent = null;
			return false;
		}

		// FMOD EventReference から再生インスタンスを作成する
		// cueEvent: インスタンス化する FMOD イベント設定
		// instance: 作成できた EventInstance
		private bool TryCreateInstance(CueEvent cueEvent, out EventInstance instance)
		{
			instance = default(EventInstance);
			if (cueEvent == null || cueEvent.eventReference.IsNull)
			{
				return false;
			}

			try
			{
				instance = RuntimeManager.CreateInstance(cueEvent.eventReference);
			}
			catch (Exception exception)
			{
				if (logMissingCue)
				{
					Debug.LogWarning("FMOD event create failed: " + cueEvent.cueName + " " + exception.Message, this);
				}
				return false;
			}

			return instance.isValid();
		}

		// FMOD インスタンスを停止してハンドルを解放する
		// instance: 停止と解放を行う FMOD インスタンス
		// allowFadeOut: フェードアウト停止を許可するか
		private void StopAndRelease(ref EventInstance instance, bool allowFadeOut)
		{
			if (!instance.isValid())
			{
				return;
			}

			instance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
			instance.release();
			instance.clearHandle();
		}

		// マスター音量、呼び出し側音量、キュー音量を掛け合わせた最終音量を返す
		// cueEvent: キュー単位の音量設定
		// seVolume: 呼び出し側の音量倍率
		private float GetVolume(CueEvent cueEvent, float seVolume)
		{
			float cueVolume = cueEvent != null ? cueEvent.volume : 1f;
			float volume = masterVolume * seVolume * cueVolume;
			return Mathf.Clamp(volume, MinVolume, MaxVolume);
		}

		// 音量と 3D 属性を設定して再生前の FMOD インスタンスを準備する
		// instance: セットアップ対象の FMOD インスタンス
		// cueEvent: キュー単位の再生設定
		// seVolume: 呼び出し側の音量倍率
		private void SetupInstance(EventInstance instance, CueEvent cueEvent, float seVolume)
		{
			instance.setVolume(GetVolume(cueEvent, seVolume));

			if (cueEvent.attachToGameObject)
			{
				RuntimeManager.AttachInstanceToGameObject(instance, gameObject);
				return;
			}

			instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
		}

		// 破棄時に残っている SE を停止する
		private void OnDestroy()
		{
			StopAllSE();
		}
	}
}
