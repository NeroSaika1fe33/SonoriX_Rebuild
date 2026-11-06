using UnityEngine;

namespace FFrameWork.Live
{
	public class MusicScore
	{
		// BPM イベントが登録されていない場合の警告文
		private const string NoBpmEventWarning = "BPM event is not registered.";

		// 譜面時間情報がない場合のエラー文
		private const string MusicScoreInfoArrayNotFound = "musicScoreInfoArray not found.";

		// 現在フレームの譜面時間情報
		public MusicScoreInfo CurrentFrameInfo { get; private set; }

		// BPM、拍子、速度、SE 音量の時間変化情報
		public MusicScoreInfo[] musicScoreInfoArray { get; private set; }

		// 譜面イベント配列
		public EventBase[] EventArray { get; private set; }

		// ノーツ配列
		public NoteBase[] NoteArray { get; private set; }

		// 現在フレーム情報の既定値を初期化する
		public MusicScore()
		{
			CurrentFrameInfo = new MusicScoreInfo(0, 0f, 0f, 120f, 4f, 1f, 1f);
		}

		// 譜面時間情報配列を設定する
		// musicScoreInfo: 設定する譜面時間情報配列
		public void SetMusicScoreInfo(MusicScoreInfo[] musicScoreInfo)
		{
			musicScoreInfoArray = musicScoreInfo;
		}

		// 譜面イベント配列を設定する
		// eventArray: 設定するイベント配列
		public void AddEventArray(EventBase[] eventArray)
		{
			EventArray = eventArray;
		}

		// ノーツ配列を設定する
		// noteArray: 設定するノーツ配列
		public void AddNoteArray(NoteBase[] noteArray)
		{
			NoteArray = noteArray;
		}

		// 指定小節時点で有効な拍子を取得する
		// barIndex: 取得対象の小節番号
		public float GetTimeSignature(int barIndex)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null)
			{
				return 4f;
			}

			float timeSignature = 4f;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].bar > barIndex)
				{
					return timeSignature;
				}
				timeSignature = array[i].timeSignature;
			}
			return timeSignature;
		}

		// 現在時刻に対応する譜面時間情報を更新する
		// time: 楽曲開始からの秒数
		public void Update(float time)
		{
			GetMusicScoreInfoFromTime(time, out MusicScoreInfo currentFrameInfo);
			CurrentFrameInfo = currentFrameInfo;
		}

		// 小節番号と小節内進行率から楽曲秒数を計算する
		// bar: 小節番号
		// barProgress: 小節内の進行率
		public float GetTimeFromBar(int bar, float barProgress)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null)
			{
				Debug.LogError(MusicScoreInfoArrayNotFound);
				return 0f;
			}
			if (array.Length == 0)
			{
				Debug.LogWarning(NoBpmEventWarning);
				return 0f;
			}

			int index = FindMusicScoreInfoIndex(bar, barProgress);
			if (index < 0 || index >= array.Length)
			{
				return 0f;
			}

			MusicScoreInfo info = array[index];
			float secondsPerBar = info.timeSignature * 60f / info.bpm;
			return info.time + secondsPerBar * (bar - info.bar) + secondsPerBar * (barProgress - info.barProgress);
		}

		// ノーツ位置に対応する MusicScoreInfo を生成する
		// bar: 小節番号
		// barProgress: 小節内の進行率
		public MusicScoreInfo GenerateNoteMusicScoreInfo(int bar, float barProgress)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null || array.Length == 0)
			{
				Debug.LogError(MusicScoreInfoArrayNotFound);
				return default(MusicScoreInfo);
			}

			int index = FindMusicScoreInfoIndex(bar, barProgress);
			if (index < 0 || index >= array.Length)
			{
				Debug.LogError(MusicScoreInfoArrayNotFound);
				return array[0];
			}

			MusicScoreInfo info = array[index];
			float secondsPerBar = info.timeSignature * 60f / info.bpm;
			float time = info.time + secondsPerBar * (bar - info.bar) + secondsPerBar * (barProgress - info.barProgress);
			return new MusicScoreInfo(bar, barProgress, time, info.bpm, info.timeSignature, info.speedRatio, info.seVolume);
		}

		// 楽曲秒数から現在有効な MusicScoreInfo を生成する
		// time: 楽曲開始からの秒数
		// scoreInfo: 生成された譜面時間情報
		public void GetMusicScoreInfoFromTime(float time, out MusicScoreInfo scoreInfo)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null)
			{
				Debug.LogError(MusicScoreInfoArrayNotFound);
				scoreInfo = default(MusicScoreInfo);
				return;
			}
			if (array.Length == 0)
			{
				Debug.LogWarning(NoBpmEventWarning);
				scoreInfo = default(MusicScoreInfo);
				return;
			}

			int index = 0;
			for (int i = array.Length - 1; i >= 0; i--)
			{
				if (array[i].time <= time)
				{
					index = i;
					break;
				}
			}
			GenerateScoreInfo(out scoreInfo, ref index, ref time);
		}

		// 基準イベント情報と指定秒数から補間済み MusicScoreInfo を生成する
		// scoreInfo: 生成された譜面時間情報
		// index: 基準にする musicScoreInfoArray の index
		// time: 楽曲開始からの秒数
		private void GenerateScoreInfo(out MusicScoreInfo scoreInfo, ref int index, ref float time)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null || index < 0 || index >= array.Length)
			{
				Debug.LogError(MusicScoreInfoArrayNotFound);
				scoreInfo = default(MusicScoreInfo);
				return;
			}

			MusicScoreInfo source = array[index];
			float secondsPerBar = source.timeSignature * 60f / source.bpm;
			float barPosition = source.barProgress + (time - source.time) / secondsPerBar;
			int barOffset = Mathf.FloorToInt(barPosition);
			scoreInfo = new MusicScoreInfo(
				source.bar + barOffset,
				barPosition - barOffset,
				time,
				source.bpm,
				source.timeSignature,
				source.speedRatio,
				source.seVolume);
		}

		// 小節位置に対応する基準 MusicScoreInfo の index を探す
		// bar: 小節番号
		// barProgress: 小節内の進行率
		private int FindMusicScoreInfoIndex(int bar, float barProgress)
		{
			MusicScoreInfo[] array = musicScoreInfoArray;
			if (array == null)
			{
				return -1;
			}

			for (int i = 0; i < array.Length; i++)
			{
				if (i + 1 == array.Length || IsAfter(bar, barProgress, ref array[i + 1]))
				{
					return i;
				}
			}
			return -1;
		}

		// MusicScoreInfo の位置が指定小節位置より後かを返す
		// bar: 小節番号
		// barProgress: 小節内の進行率
		// info: 比較対象の譜面時間情報
		private static bool IsAfter(int bar, float barProgress, ref MusicScoreInfo info)
		{
			return info.bar > bar || (info.bar == bar && info.barProgress > barProgress);
		}
	}
}
