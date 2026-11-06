using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
	public static class LiveUtility
	{
		// ノーツ終端判定を無視する短い猶予時間
		public const float NoteEndIgnoreFrameTime = 1f / 6f;

		// ライブで使うレーン数
		public const int LaneCount = LiveConfig.LaneCount;

		// 60fps 換算の 1 フレーム時間
		public const float OneFrameTime = 1f / 60f;

		// 120fps 換算の 1 フレーム時間
		public const float HalfFrameTime = 1f / 120f;

		// 再利用用の Vector2.zero
		public static readonly Vector2 Vector2Zero = Vector2.zero;

		// 再利用用の Vector3.zero
		public static readonly Vector3 Vector3Zero = Vector3.zero;

		// 再利用用の Vector3.one
		public static readonly Vector3 Vector3One = Vector3.one;

		// X 方向だけ反転した Vector3.one
		public static readonly Vector3 Vector3OneMinusX = new Vector3(-1f, 1f, 1f);

		// 左向き表示用の Vector2
		public static readonly Vector2 Vector2Left = new Vector2(-1f, 1f);

		// 右向き表示用の Vector2
		public static readonly Vector2 Vector2Right = Vector2.one;

		// 左フリック表示用の回転角
		public static readonly Vector3 FlickLeftEulerAngles = new Vector3(0f, 0f, 30f);

		// 右フリック表示用の回転角
		public static readonly Vector3 FlickRightEulerAngles = new Vector3(0f, 0f, -30f);

		// ノーツ種別に応じた表示 X オフセットを取得する
		// category: 対象ノーツ種別
		// bundleBuildData: 調整値を持つビルド設定
		public static float GetLaneOffset(NoteCategory category, LiveBundleBuildData bundleBuildData)
		{
			if (category == NoteCategory.FixedNormal || category == NoteCategory.FixedLong)
			{
				return 0f;
			}

			if (bundleBuildData == null)
			{
				switch (category)
				{
					case NoteCategory.FreeLaneNormal:
						return 1.25f;
					case NoteCategory.GuideLong:
					case NoteCategory.GuideEnd:
						return 0f;
					default:
						return 1.5f;
				}
			}

			switch (category)
			{
				case NoteCategory.FreeLaneNormal:
					return bundleBuildData.NormalNoteOffsetX;
				case NoteCategory.FreeLaneLong:
				case NoteCategory.SliderLong:
					return bundleBuildData.LongNoteOffsetX;
				case NoteCategory.SliderFlick:
				case NoteCategory.VolumeFlick:
				case NoteCategory.VolumeFlickLong:
					return bundleBuildData.FlickNoteOffsetX;
				case NoteCategory.GuideLong:
				case NoteCategory.GuideEnd:
					return 0f;
				default:
					throw new ArgumentOutOfRangeException("category", category, null);
			}
		}

		// 画面 DPI を使って指定距離をピクセル相当へ変換する
		// distance: センチメートル基準の距離
		public static float ScreenDpiToInch(float distance)
		{
			float dpi = Screen.dpi;
			float pixelPerCentimeter = dpi == 0f ? 118.11f : dpi / 2.54f;
			return pixelPerCentimeter * distance;
		}

		// INote の種別と親子関係から判定フレーム種別を取得する
		// note: 判定対象ノーツ
		public static JudgeFrameType GetINoteToJudgeFrameType(INote note)
		{
			bool isLongEnd = note.ParentNote != null;
			switch (note.Category)
			{
				case NoteCategory.FixedNormal:
					return JudgeFrameType.FixedLane_Normal;
				case NoteCategory.FixedLong:
					return isLongEnd ? JudgeFrameType.FixedLane_Long_End : JudgeFrameType.FixedLane_Long;
				case NoteCategory.FreeLaneNormal:
					if (isLongEnd)
					{
						return JudgeFrameType.FreeLane_Long_End;
					}
					return JudgeFrameType.FreeLane_Normal;
				case NoteCategory.FreeLaneLong:
					if (isLongEnd)
					{
						return JudgeFrameType.FreeLane_Long_End;
					}
					return JudgeFrameType.FreeLane_Long;
				case NoteCategory.SliderLong:
					if (isLongEnd)
					{
						return JudgeFrameType.Slider_Long_End;
					}
					return JudgeFrameType.Slider_Long;
				case NoteCategory.SliderFlick:
					if (isLongEnd)
					{
						return GetLongEndSliderFlickFrameType(note.ParentNote);
					}
					return JudgeFrameType.Slider_Flick;
				case NoteCategory.VolumeFlick:
					return JudgeFrameType.Volume_Flick;
				case NoteCategory.VolumeFlickLong:
					if (isLongEnd)
					{
						return JudgeFrameType.Volume_Long_End;
					}
					return JudgeFrameType.Volume_Long;
				default:
					return JudgeFrameType.FreeLane_Normal;
			}
		}

		// スライダーフリック終端用の判定フレーム種別を親ノーツから取得する
		// parentNote: フリック終端を持つ親ノーツ
		private static JudgeFrameType GetLongEndSliderFlickFrameType(INote parentNote)
		{
			if (parentNote == null)
			{
				return JudgeFrameType.Slider_Flick;
			}

			switch (parentNote.Category)
			{
				case NoteCategory.FixedLong:
					return JudgeFrameType.FixedLane_Long_End_SliderFlick;
				case NoteCategory.FreeLaneLong:
					return JudgeFrameType.FreeLane_Long_End_SliderFlick;
				case NoteCategory.SliderLong:
					return JudgeFrameType.Slider_Long_End_SliderFlick;
				case NoteCategory.VolumeFlickLong:
					return JudgeFrameType.Volume_Long_End_SliderFlick;
				default:
					return JudgeFrameType.Slider_Flick;
			}
		}

		// 指定判定フレームで現在のズレ時間が判定可能範囲かを返す
		// frameType: 判定フレーム種別
		// offsetJudgeTime: 判定基準からのズレ時間
		public static bool IsJudgmentTiming(JudgeFrameType frameType, float offsetJudgeTime)
		{
			return LiveConfig.IsJudgeTiming(frameType, offsetJudgeTime);
		}

		// t が負でも始点より前へ戻さない Vector2 Lerp
		// a: 始点
		// b: 終点
		// t: 補間率
		public static Vector2 EarlyVec2Lerp(Vector2 a, Vector2 b, float t)
		{
			return a + (b - a) * Mathf.Max(t, 0f);
		}

		// レーン幅から SpriteRenderer の表示サイズを計算する
		// size: レーン幅基準のサイズ
		public static Vector2 CalcSpriteRendererSize(float size)
		{
			return new Vector2(size * 1.2178f + 2.06f, 1.7222f);
		}

		// 整数レーン番号から X 座標を計算する
		// lane: レーン番号
		public static float CalcLaneTransformX(int lane)
		{
			return CalcLaneTransformX((float)lane);
		}

		// 小数レーン位置から X 座標を計算する
		// lane: レーン位置
		public static float CalcLaneTransformX(float lane)
		{
			return (lane - LiveConfig.LaneMaxIndex * 0.5f) * 0.84f;
		}

		// ロングノーツ親子の現在レーン位置と判定ズレ時間を更新する
		// noteBase: 更新対象ノーツ
		// currentNote: 現在区間の開始ノーツ
		// nextNote: 現在区間の終了ノーツ
		// currentFrameInfo: 現在フレームの譜面時間情報
		// musicScoreInfo: noteBase 自身の譜面時間情報
		// childNote: 対応する子ノーツ
		public static void CalcExcuteNoteLane(NoteBase noteBase, ref INote currentNote, ref INote nextNote, ref MusicScoreInfo currentFrameInfo, MusicScoreInfo musicScoreInfo, INote childNote)
		{
			if (noteBase == null || currentNote == null || nextNote == null || childNote == null)
			{
				return;
			}

			float progress = noteBase.Category == NoteCategory.SliderLong || noteBase.Category == NoteCategory.VolumeFlickLong
				? LiveConfig.GetSliderLongLineParentProgress(currentFrameInfo.time, currentNote, nextNote)
				: LiveConfig.GetNoteLineParentProgress(currentFrameInfo.time, currentNote, nextNote);
			noteBase.LaneStartF = Mathf.Lerp(currentNote.DefaultLeftLane, nextNote.DefaultLeftLane, progress);
			noteBase.LaneEndF = Mathf.Lerp(currentNote.DefaultRightLane, nextNote.DefaultRightLane, progress);
			float childTime = childNote.MusicScoreInfo.time;
			noteBase.OffsetJudgeTime = Mathf.Min(currentFrameInfo.time - musicScoreInfo.time, Mathf.Max(currentFrameInfo.time - childTime, 0f));
		}

		// 譜面内で同時表示される最大ノーツ数を種別ごとに計算する
		// musicScore: 対象譜面
		// baseDisplayTime: 基準表示時間
		public static Dictionary<NoteCategory, int> CalculateMaxSimultaneousNotes(MusicScore musicScore, float baseDisplayTime)
		{
			Dictionary<NoteCategory, List<(float, float)>> allNoteDisplayTime = new Dictionary<NoteCategory, List<(float, float)>>();
			Dictionary<NoteCategory, int> maxSimultaneousNotes = CreateNoteCountDictionary();
			if (musicScore == null || musicScore.NoteArray == null || musicScore.musicScoreInfoArray == null)
			{
				return maxSimultaneousNotes;
			}

			NoteCategory[] keys = GetViewNoteCountKeys();
			for (int i = 0; i < keys.Length; i++)
			{
				allNoteDisplayTime[keys[i]] = new List<(float, float)>();
				maxSimultaneousNotes[keys[i]] = 0;
			}

			CalculateAllNoteDisplayTime(musicScore.NoteArray, musicScore.musicScoreInfoArray, baseDisplayTime, ref allNoteDisplayTime);
			for (int i = 0; i < keys.Length; i++)
			{
				maxSimultaneousNotes[keys[i]] = CalculateMaxSimultaneousNotes(allNoteDisplayTime[keys[i]]);
			}

			return maxSimultaneousNotes;
		}

		// 全ノーツの表示開始時間と終了時間を種別ごとに集計する
		// noteArray: 対象ノーツ配列
		// musicScoreInfoArray: 速度変化を含む譜面時間情報配列
		// baseDisplayTime: 基準表示時間
		// allNoteDisplayTime: 集計先の表示時間辞書
		private static void CalculateAllNoteDisplayTime(NoteBase[] noteArray, MusicScoreInfo[] musicScoreInfoArray, float baseDisplayTime, ref Dictionary<NoteCategory, List<(float, float)>> allNoteDisplayTime)
		{
			for (int i = 0; i < noteArray.Length; i++)
			{
				NoteBase note = noteArray[i];
				if (note == null || IsNoViewCategory(note.Category))
				{
					continue;
				}

				float displayTime = CalculateNoteDisplayTime(note, musicScoreInfoArray, baseDisplayTime);
				NoteCategory key = GetNoteViewPoolCategory(note.Category);
				EnsureDisplayTimeList(key, ref allNoteDisplayTime);
				if (note.NoteList == null || note.NoteList.Count < 2)
				{
					float startTime = note.MusicScoreInfo.time;
					allNoteDisplayTime[key].Add((startTime, startTime + displayTime));
					continue;
				}

				(float, float) lastDisplayTime = default;
				for (int j = 1; j < note.NoteList.Count; j++)
				{
					NoteBase childNote = note.NoteList[j];
					if (childNote == null || IsNoViewCategory(childNote.Category))
					{
						continue;
					}

					CalculateChildNoteDisplayTime(childNote, musicScoreInfoArray, baseDisplayTime, ref allNoteDisplayTime);
					NoteCategory childKey = GetNoteViewPoolCategory(childNote.Category);
					if (!allNoteDisplayTime.TryGetValue(childKey, out List<(float, float)> childTimes) || childTimes.Count == 0)
					{
						continue;
					}
					lastDisplayTime = childTimes[childTimes.Count - 1];
				}

				float endDisplayTime = lastDisplayTime.Item2 > 0f
					? lastDisplayTime.Item2
					: note.MusicScoreInfo.time + displayTime;
				allNoteDisplayTime[key].Add((note.MusicScoreInfo.time, endDisplayTime));
			}
		}

		// 子ノーツ 1 件分の表示時間を集計する
		// note: 対象の子ノーツ
		// musicScoreInfoArray: 速度変化を含む譜面時間情報配列
		// baseDisplayTime: 基準表示時間
		// allNoteDisplayTime: 集計先の表示時間辞書
		private static void CalculateChildNoteDisplayTime(NoteBase note, MusicScoreInfo[] musicScoreInfoArray, float baseDisplayTime, ref Dictionary<NoteCategory, List<(float, float)>> allNoteDisplayTime)
		{
			if (note == null || IsNoViewCategory(note.Category))
			{
				return;
			}

			float displayTime = CalculateNoteDisplayTime(note, musicScoreInfoArray, baseDisplayTime);
			NoteCategory key = GetNoteViewPoolCategory(note.Category);
			EnsureDisplayTimeList(key, ref allNoteDisplayTime);
			float startTime = note.MusicScoreInfo.time;
			allNoteDisplayTime[key].Add((startTime, startTime + displayTime));
		}

		// 速度変化を考慮してノーツが画面に出ている時間を計算する
		// note: 対象ノーツ
		// musicScoreInfoArray: 速度変化を含む譜面時間情報配列
		// baseDisplayTime: 基準表示時間
		private static float CalculateNoteDisplayTime(NoteBase note, MusicScoreInfo[] musicScoreInfoArray, float baseDisplayTime)
		{
			if (note == null || musicScoreInfoArray == null || musicScoreInfoArray.Length == 0)
			{
				return 0.25f;
			}

			float noteTime = note.MusicScoreInfo.time;
			int eventIndex = -1;
			for (int i = musicScoreInfoArray.Length - 1; i >= 0; i--)
			{
				if (musicScoreInfoArray[i].time < noteTime)
				{
					eventIndex = i;
					break;
				}
			}

			if (eventIndex < 0)
			{
				return 0.25f;
			}

			float consumedTime = 0f;
			float displayTime = 0f;
			float currentTime = noteTime;
			for (int i = eventIndex; i >= 0 && consumedTime < baseDisplayTime; i--)
			{
				float segmentTime = currentTime - musicScoreInfoArray[i].time;
				float useTime = Mathf.Min(segmentTime, baseDisplayTime - consumedTime);
				displayTime += SafeCalcDisplayTime(useTime, musicScoreInfoArray[i].speedRatio, note.speedRatio);
				consumedTime += useTime;
				currentTime = musicScoreInfoArray[i].time;
			}

			if (consumedTime < baseDisplayTime)
			{
				displayTime += SafeCalcDisplayTime(baseDisplayTime - consumedTime, musicScoreInfoArray[0].speedRatio, note.speedRatio);
			}

			return displayTime * 1.2f + 0.25f;
		}

		// 速度倍率が極端な場合でも破綻しないよう表示時間を計算する
		// useTime: 使用する時間幅
		// eventSpeedRatio: イベント側の速度倍率
		// noteSpeedRatio: ノーツ側の速度倍率
		private static float SafeCalcDisplayTime(float useTime, float eventSpeedRatio, float noteSpeedRatio)
		{
			return useTime / Mathf.Min(eventSpeedRatio * noteSpeedRatio, 100f);
		}

		// 表示時間区間の重なりから最大同時表示数を計算する
		// noteDisplayTimes: ノーツごとの表示時間区間
		private static int CalculateMaxSimultaneousNotes(List<(float start, float end)> noteDisplayTimes)
		{
			int maxCount = 0;
			for (int i = 0; i < noteDisplayTimes.Count; i++)
			{
				int count = 0;
				for (int j = 0; j < noteDisplayTimes.Count; j++)
				{
					if (noteDisplayTimes[i].start <= noteDisplayTimes[j].end && noteDisplayTimes[j].start <= noteDisplayTimes[i].end)
					{
						count++;
					}
				}

				maxCount = Mathf.Max(maxCount, count);
			}

			return maxCount;
		}

		// 表示プール対象カテゴリのカウント辞書を作成する
		private static Dictionary<NoteCategory, int> CreateNoteCountDictionary()
		{
			Dictionary<NoteCategory, int> result = new Dictionary<NoteCategory, int>();
			NoteCategory[] keys = GetViewNoteCountKeys();
			for (int i = 0; i < keys.Length; i++)
			{
				result[keys[i]] = 0;
			}

			return result;
		}

		// 表示プール数を計算する対象カテゴリ一覧を取得する
		private static NoteCategory[] GetViewNoteCountKeys()
		{
			return new NoteCategory[]
			{
				NoteCategory.FreeLaneNormal,
				NoteCategory.FreeLaneLong,
				NoteCategory.SliderLong,
				NoteCategory.SliderFlick,
				NoteCategory.VolumeFlick,
				NoteCategory.VolumeFlickLong,
				NoteCategory.FixedNormal,
				NoteCategory.FixedLong
			};
		}

		// 指定カテゴリの表示時間リストを辞書に確保する
		// key: ノーツカテゴリ
		// allNoteDisplayTime: 表示時間辞書
		private static void EnsureDisplayTimeList(NoteCategory key, ref Dictionary<NoteCategory, List<(float, float)>> allNoteDisplayTime)
		{
			if (!allNoteDisplayTime.ContainsKey(key))
			{
				allNoteDisplayTime[key] = new List<(float, float)>();
			}
		}

		// 表示プール対象外カテゴリかを返す
		// category: 判定するノーツカテゴリ
		private static bool IsNoViewCategory(NoteCategory category)
		{
			return category == NoteCategory.GuideLong ||
				category == NoteCategory.GuideEnd;
		}

		// 実表示で共有するカテゴリへ正規化する
		// category: 元のノーツカテゴリ
		public static NoteCategory NormalizeViewCategory(NoteCategory category)
		{
			switch (category)
			{
				case NoteCategory.FixedNormal:
					return NoteCategory.FreeLaneNormal;
				case NoteCategory.FixedLong:
					return NoteCategory.FreeLaneLong;
				case NoteCategory.VolumeFlick:
				case NoteCategory.VolumeFlickLong:
					return NoteCategory.SliderFlick;
				default:
					return category;
			}
		}

		// ノーツ表示プールのカテゴリを取得する
		// category: 元のノーツカテゴリ
		public static NoteCategory GetNoteViewPoolCategory(NoteCategory category)
		{
			return category;
		}
	}
}
