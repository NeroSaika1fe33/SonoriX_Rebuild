using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FFrameWork.Live;
using UnityEngine;

namespace FFrameWork
{
	// SUS 譜面文字列を MusicScore、NoteBase、EventBase へ変換する
	public class Converter
	{
		// 4 レーン記法で許可する最小ノーツ幅
		private const int FourKeyMinNoteWidth = 2;

		// 4 レーン記法で許可する最大ノーツ幅
		private const int FourKeyMaxNoteWidth = 8;

		// 4 レーン記法の各キーに対応する開始レーン
		private static readonly int[] FourKeyLaneStarts = { 4, 6, 8, 10 };

		// 可読 SUS ノーツ行を解析する正規表現
		private static readonly Regex ReadableSusNotesRegex = new Regex("^#(?<bar>\\d{3})\\.(?<path>[0-9a-zA-Z+_.-]+):(?<data>.+)", RegexOptions.Compiled);

		// 4 レーンロングノーツ行を解析する正規表現
		private static readonly Regex FourKeyLongNotesRegex = new Regex("^#(?<bar>\\d{3})K(?<laneType>\\d{1})(?<keyNo>[0-3]{1})(?<longNo>[0-9a-z]{1}):(?<data>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// 4 レーンイベント行を解析する正規表現
		private static readonly Regex FourKeyEventNotesRegex = new Regex("^#(?<bar>\\d{3})K(?<event_type>[0-9a-z]{2}):(?<data>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// 通常イベント行を解析する正規表現
		private static readonly Regex EventNotesRegex = new Regex("^#(?<bar>\\d{3})(?<event_type>[0-9a-z]{2}):(?<data>.+)", RegexOptions.Compiled);

		// ノーツ値に付く速度倍率指定を解析する正規表現
		private static readonly Regex NotesSpeedRatioRegex = new Regex("(?<data>[0-9a-z]{2}),(?<speedRatio>[-]*[0-9]+\\.?[0-9]*) ", RegexOptions.Compiled);

		// ロングノーツイベント行を解析する正規表現
		private static readonly Regex EventLongNotesRegex = new Regex("^#(?<bar>\\d{3})(?<laneType>\\d{1})(?<lineNo>[0-9a-z]{1})(?<longNo>[0-9a-z]{1}):(?<data>.+)", RegexOptions.Compiled);

		// BPM 定義行を解析する正規表現
		private static readonly Regex BpmRegex = new Regex("^#BPM(?<bpm_id>[0-9A-Z]{2}):(?<bpm>.+)", RegexOptions.Compiled);

		// SE 音量定義を解析する正規表現
		private static readonly Regex VolumeRegex = new Regex("(?<barIndex>\\d*)'(?<tickOffset>[0-9]*):(?<volume>[-+]?\\d*\\.\\d*)[\\r]", RegexOptions.Compiled);

		// ticks_per_beat 指定を解析する正規表現
		private static readonly Regex TicksPerBeatRegex = new Regex("^#REQUEST ticks_per_beat (?<ticks_per_beat>\\d+)", RegexOptions.Compiled);

		// ハイスピード定義を解析する正規表現
		private static readonly Regex HighSpeedRegex = new Regex("(?<barIndex>\\d*)'(?<tickOffset>[0-9]*):(?<speedRatio>[-+]?\\d*\\.\\d*)[\\r]", RegexOptions.Compiled);

		// 可読 SUS の速度倍率指定を解析する正規表現
		private static readonly Regex ReadableSpeedRatioRegex = new Regex("(?<data>[0-9a-zA-Z+\\-<>]{2}),(?<speedRatio>[-+]*[0-9]+\\.?[0-9]*) ", RegexOptions.Compiled);

		// 小節位置ごとに集約した通常ノーツ情報
		private Dictionary<float, Dictionary<int, NoteInfo>> noteInfoDict;

		// 小節位置ごとに集約したガイドノーツ情報
		private Dictionary<float, Dictionary<int, NoteInfo>> guideInfoDict;

		// 変換後のイベント一覧
		private List<EventBase> musicScoreEventList;

		// 変換後のノーツ一覧
		private List<NoteBase> musicScoreNoteList;

		// 接続待ちのロングノーツ
		private Dictionary<int, LongNote> tmpLong;

		// 接続待ちのガイドノーツ
		private Dictionary<int, GuideNote> tmpGuide;

		// SUS の 1 拍あたり Tick 数
		private int ticksPerBeat;

		// 拍子変更情報
		private List<TimeSignatureInfo> timeSignaturesInfos;

		// BPM ID から BPM 値を引く辞書
		private Dictionary<string, float> bpmMap;

		// BPM 変更情報
		private List<BpmInfo> bpmInfos;

		// ハイスピード変更情報
		private List<HighSpeedInfo> highSpeedInfos;

		// SE 音量変更情報
		private List<VolumeInfo> volumeInfos;

		// 変換先の MusicScore
		private MusicScore musicScore;

		// 変換時に使うライブ設定
		private LiveBundleBuildData liveBundleBuildData;

		// ミラー譜面として変換するか
		private bool isMirror;

		// 変換時に使うライブ設定
		public LiveBundleBuildData LiveBundleBuildData { get; set; }

		// ミラー譜面として変換するか
		public bool IsMirror { get; set; }

		// 共通ロングノーツのホールドコンボ間隔
		public int LongNoteComboBeat { get; set; }

		// フリーレーンロングノーツのホールドコンボ間隔
		public int FreeLaneLongNoteComboBeat { get; set; }

		// スライダーロングノーツのホールドコンボ間隔
		public int SliderLongNoteComboBeat { get; set; }

		// ボリュームロングノーツのホールドコンボ間隔
		public int VolumeLongNoteComboBeat { get; set; }

		// 既定値で Converter を初期化する
		public Converter()
		{
			LongNoteComboBeat = 0;
			FreeLaneLongNoteComboBeat = 0;
			SliderLongNoteComboBeat = 0;
			VolumeLongNoteComboBeat = 0;
			Initialize();
		}

		// SUS 文字列を MusicScore へ変換する
		// musicScoreData: 変換元の SUS 文字列
		public MusicScore Convert(string musicScoreData)
		{
			Initialize();
			liveBundleBuildData = LiveBundleBuildData;
			isMirror = IsMirror;
			musicScore = new MusicScore();
			Load(musicScoreData);
			ConvertMusicEventList();
			ConvertMusicScoreNoteList();
			musicScore.AddNoteArray(musicScoreNoteList.ToArray());
			SortMusicEventList();
			musicScore.AddEventArray(musicScoreEventList.ToArray());
			return musicScore;
		}

		// 変換処理で使う中間データを初期化する
		private void Initialize()
		{
			noteInfoDict = new Dictionary<float, Dictionary<int, NoteInfo>>();
			guideInfoDict = new Dictionary<float, Dictionary<int, NoteInfo>>();
			musicScoreEventList = new List<EventBase>();
			musicScoreNoteList = new List<NoteBase>();
			tmpLong = new Dictionary<int, LongNote>();
			tmpGuide = new Dictionary<int, GuideNote>();
			ticksPerBeat = 480;
			timeSignaturesInfos = new List<TimeSignatureInfo>();
			bpmMap = new Dictionary<string, float>();
			bpmInfos = new List<BpmInfo>();
			highSpeedInfos = new List<HighSpeedInfo>();
			volumeInfos = new List<VolumeInfo>();
		}

		// BPM、拍子、ハイスピード、音量から MusicScoreInfo の時系列を構築する
		private void ConvertMusicEventList()
		{
			List<TimeSignatureInfo> timeSignatureList = timeSignaturesInfos.OrderBy(x => x.StartBarIndex).ToList();
			List<ScoreInfoUpdate> updates = new List<ScoreInfoUpdate>();

			updates.Add(new ScoreInfoUpdate(0, 0f)
			{
				Bpm = 160f,
				TimeSignature = 4f,
				SpeedRatio = 1f,
				SeVolume = 1f
			});
			foreach (TimeSignatureInfo info in timeSignatureList)
			{
				updates.Add(new ScoreInfoUpdate(info.StartBarIndex, 0f) { TimeSignature = info.TimeSignature });
			}
			foreach (BpmInfo info in bpmInfos)
			{
				updates.Add(new ScoreInfoUpdate(info.bar, info.barProgress) { Bpm = info.bpm });
			}
			foreach (HighSpeedInfo info in highSpeedInfos)
			{
				float barProgress = info.TickOffset / GetTimeSignatureForBar(info.BarIndex, timeSignatureList) / ticksPerBeat;
				updates.Add(new ScoreInfoUpdate(info.BarIndex, barProgress) { SpeedRatio = info.SpeedRatio });
			}
			foreach (VolumeInfo info in volumeInfos)
			{
				float barProgress = info.TickOffset / GetTimeSignatureForBar(info.BarIndex, timeSignatureList) / ticksPerBeat;
				updates.Add(new ScoreInfoUpdate(info.BarIndex, barProgress) { SeVolume = info.Volume });
			}

			updates = updates.OrderBy(x => x.Bar).ThenBy(x => x.BarProgress).ToList();
			List<MusicScoreInfo> scoreInfoList = new List<MusicScoreInfo>();
			float bpm = 120f;
			float timeSignature = 4f;
			float speedRatio = 1f;
			float seVolume = 1f;

			for (int i = 0; i < updates.Count;)
			{
				ScoreInfoUpdate update = updates[i];
				int bar = update.Bar;
				float barProgress = Mathf.Max(0f, update.BarProgress);

				while (i < updates.Count && updates[i].Bar == bar && updates[i].BarProgress.Equals(update.BarProgress))
				{
					updates[i].Apply(ref bpm, ref timeSignature, ref speedRatio, ref seVolume);
					i++;
				}
				scoreInfoList.Add(new MusicScoreInfo(bar, barProgress, 0f, bpm, timeSignature, speedRatio, seVolume));
			}

			float time = 0f;
			for (int j = 0; j < scoreInfoList.Count; j++)
			{
				MusicScoreInfo current = scoreInfoList[j];
				if (j == 0)
				{
					float secondsPerBar = current.timeSignature * 60f / current.bpm;
					time += secondsPerBar * current.bar + current.barProgress * secondsPerBar;
				}
				else
				{
					MusicScoreInfo previous = scoreInfoList[j - 1];
					float secondsPerBar = previous.timeSignature * 60f / previous.bpm;
					time += secondsPerBar * (current.bar - previous.bar) + secondsPerBar * (current.barProgress - previous.barProgress);
				}
				current.time = time;
				scoreInfoList[j] = current;
			}
			musicScore.SetMusicScoreInfo(scoreInfoList.ToArray());
		}

		// SUS 文字列を読み込み、中間データへ分解する
		// musicScoreData: 変換元の SUS 文字列
		private void Load(string musicScoreData)
		{
			if (string.IsNullOrEmpty(musicScoreData))
			{
				return;
			}

			using (StringReader reader = new StringReader(musicScoreData))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					if (!LeadHeader(line))
					{
						LoadEvent(line);
					}
				}
			}
		}

		// 中間ノーツ情報を NoteBase 配列へ変換する
		private void ConvertMusicScoreNoteList()
		{
			musicScoreNoteList.Clear();
			ConvertNoteInfoDictionary(ref noteInfoDict);
			ConvertNoteInfoDictionary(ref guideInfoDict);
			musicScoreNoteList = musicScoreNoteList.OrderBy(x => x.MusicScoreInfo.time).ToList();
		}

		// 指定したノーツ情報辞書を時系列順に NoteBase へ変換する
		// noteInfoDictionary: 変換対象のノーツ情報辞書
		private void ConvertNoteInfoDictionary(ref Dictionary<float, Dictionary<int, NoteInfo>> noteInfoDictionary)
		{
			int id = 0;
			foreach (float key in noteInfoDictionary.Keys.OrderBy(x => x))
			{
				foreach (NoteInfo noteInfoValue in GetOrderedNoteInfos(noteInfoDictionary[key]))
				{
					NoteInfo noteInfo = noteInfoValue;
					MusicScoreInfo info = musicScore.GenerateNoteMusicScoreInfo(noteInfo.Bar, noteInfo.BarProgress);
					int laneStart;
					int laneEnd;
					if (isMirror)
					{
						laneStart = LiveConfig.LaneCount - noteInfo.Lane - noteInfo.Width;
						laneEnd = LiveConfig.LaneMaxIndex - noteInfo.Lane;
					}
					else
					{
						laneStart = noteInfo.Lane;
						laneEnd = noteInfo.Lane + noteInfo.Width - 1;
					}

					NoteCategory category = noteInfo.Category;
					switch (category)
					{
						case NoteCategory.FreeLaneNormal:
						case NoteCategory.FixedNormal:
							ConvertNormalNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd, ref category);
							break;
						case NoteCategory.FreeLaneLong:
							ConvertFreeLaneLongNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd);
							break;
						case NoteCategory.FixedLong:
							ConvertFixedLaneLongNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd);
							break;
						case NoteCategory.SliderLong:
							ConvertSliderLongNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd);
							break;
						case NoteCategory.SliderFlick:
							ConvertFlickNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd, ref category);
							break;
						case NoteCategory.VolumeFlick:
							ConvertVolumeFlickNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd);
							break;
						case NoteCategory.VolumeFlickLong:
							ConvertVolumeLongNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd);
							break;
						case NoteCategory.GuideLong:
							ConvertGuideNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd, ref category);
							break;
						case NoteCategory.GuideEnd:
							ConvertGuideEndNote(ref id, ref noteInfo, ref info, ref laneStart, ref laneEnd, ref category);
							break;
					}
					id++;
				}
			}
		}

		// 同一小節位置のノーツを変換順に並べる
		// notes: 同一小節位置にあるノーツ辞書
		private static IEnumerable<NoteInfo> GetOrderedNoteInfos(Dictionary<int, NoteInfo> notes)
		{
			if (notes == null)
			{
				yield break;
			}

			foreach (KeyValuePair<int, NoteInfo> pair in notes
				.OrderBy(pair => GetNoteConversionOrder(pair.Value)))
			{
				if (pair.Value != null)
				{
					yield return pair.Value;
				}
			}
		}

		// ノーツ種別ごとの変換優先度を取得する
		// noteInfo: 対象ノーツ情報
		private static int GetNoteConversionOrder(NoteInfo noteInfo)
		{
			if (noteInfo == null)
			{
				return 99;
			}
			if (noteInfo.LongNo != -1 && IsLongEndBoundary(noteInfo.Category, noteInfo.Direction, noteInfo.LongPointRole))
			{
				return 0;
			}
			if (noteInfo.LongNo != -1 && noteInfo.LongPointRole == LongPointRole.Relay)
			{
				return 1;
			}
			if (noteInfo.LongNo != -1 && IsLongStartBoundary(noteInfo.Category, noteInfo.Direction, noteInfo.LongPointRole))
			{
				return 3;
			}
			return 2;
		}

		// 通常ノーツまたはロング終端ノーツを変換する
		private void ConvertNormalNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd, ref NoteCategory category)
		{
			if (!CanJoinFixedLongEnd(noteInfo, laneStart, laneEnd))
			{
				return;
			}

			NoteBase note = CreateNormalOrLongEndNote(ref noteInfo, ref info, id, laneStart, laneEnd, category);
			LongJoinCheck(ref noteInfo, ref note);
			if (note.ParentNote == null)
			{
				musicScoreNoteList.Add(note);
			}
		}

		// ノーツ情報から通常ノーツまたはロング終端ノーツを作成する
		private NoteBase CreateNormalOrLongEndNote(ref NoteInfo noteInfo, ref MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category)
		{
			if (tmpLong.TryGetValue(noteInfo.LongNo, out LongNote longNote) && longNote != null)
			{
				if (longNote is FixedLongNote)
				{
					return new FixedLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio);
				}
				if (longNote is FreeLaneLongNote)
				{
					return new FreeLaneLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio);
				}
			}

			return new NormalNote(info, id, laneStart, laneEnd, category, liveBundleBuildData, noteInfo.SpeedRatio);
		}

		// 固定ロング終端として既存ロングへ接続できるかを返す
		private bool CanJoinFixedLongEnd(NoteInfo noteInfo, int laneStart, int laneEnd)
		{
			if (noteInfo.Category != NoteCategory.FixedNormal ||
				!tmpLong.TryGetValue(noteInfo.LongNo, out LongNote longNote) ||
				longNote == null ||
				longNote.Category != NoteCategory.FixedLong)
			{
				return true;
			}

			if (longNote.DefaultLeftLane == laneStart &&
				longNote.DefaultRightLane == laneEnd)
			{
				return true;
			}

			Debug.LogError(string.Format(
				"FLong end note must use the same fixed lane as its start. bar:{0} longNo:{1} startLane:{2}-{3} endLane:{4}-{5}",
				noteInfo.Bar,
				noteInfo.LongNo,
				longNote.DefaultLeftLane,
				longNote.DefaultRightLane,
				laneStart,
				laneEnd));
			return false;
		}

		// スライダーフリックノーツを変換する
		private void ConvertFlickNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd, ref NoteCategory category)
		{
			NoteDirection direction = GetMirroredDirection(noteInfo.Direction);
			NoteBase note = new SliderFlickNote(info, id, laneStart, laneEnd, category, liveBundleBuildData, noteInfo.SpeedRatio, direction);
			LongJoinCheck(ref noteInfo, ref note);
			if (note.ParentNote == null)
			{
				musicScoreNoteList.Add(note);
			}
		}

		// ボリュームフリックノーツを変換する
		private void ConvertVolumeFlickNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd)
		{
			NoteBase note = new VolumeFLickNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.Direction);
			LongJoinCheck(ref noteInfo, ref note);
			if (note.ParentNote == null)
			{
				musicScoreNoteList.Add(note);
			}
		}

		// ボリュームロングノーツを変換する
		private void ConvertVolumeLongNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd)
		{
			if (noteInfo.LongNo == -1)
			{
				Debug.LogError(string.Format("VolumeLong note requires long number. bar:{0}", noteInfo.Bar));
				return;
			}

			NoteBase note;
			if (noteInfo.LongPointRole == LongPointRole.End)
			{
				note = noteInfo.Direction == NoteDirection.Default
					? new VolumeLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType)
					: new VolumeLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType, noteInfo.Direction);
				LongJoinCheck(ref noteInfo, ref note);
				if (note.ParentNote == null)
				{
					Debug.LogError(string.Format("VolumeLong start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				}
				return;
			}

			if (noteInfo.LongPointRole == LongPointRole.Relay)
			{
				if (!tmpLong.ContainsKey(noteInfo.LongNo) || tmpLong[noteInfo.LongNo] == null)
				{
					if (noteInfo.LineType != NoteLineType.RightAngle)
					{
						Debug.LogError(string.Format("VolumeLong relay start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
						return;
					}
				}
				else
				{
					VolumeLongNote volumeLongNote = tmpLong[noteInfo.LongNo] as VolumeLongNote;
					if (volumeLongNote == null)
					{
						Debug.LogError(string.Format("Duplicate volume long note line: {0}", noteInfo.LongNo));
						return;
					}
					if (volumeLongNote.Direction != noteInfo.Direction)
					{
						Debug.LogError(string.Format("VolumeLong relay direction mismatch. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
						return;
					}

					AddRelayNote(noteInfo, info, id, laneStart, laneEnd, NoteCategory.VolumeFlickLong);
					return;
				}
			}

			if (noteInfo.LongPointRole == LongPointRole.Start &&
				tmpLong.ContainsKey(noteInfo.LongNo) &&
				tmpLong[noteInfo.LongNo] != null)
			{
				Debug.LogError(string.Format("Duplicate volume long note start: {0}", noteInfo.LongNo));
				return;
			}

			if (noteInfo.Direction == NoteDirection.Default)
			{
				note = new VolumeLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType);
				LongJoinCheck(ref noteInfo, ref note);
				if (note.ParentNote == null)
				{
					Debug.LogError(string.Format("VolumeLong start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				}
				return;
			}

			if (tmpLong.ContainsKey(noteInfo.LongNo) && tmpLong[noteInfo.LongNo] != null)
			{
				VolumeLongNote volumeLongNote = tmpLong[noteInfo.LongNo] as VolumeLongNote;
				if (volumeLongNote == null)
				{
					Debug.LogError(string.Format("Duplicate volume long note line: {0}", noteInfo.LongNo));
					return;
				}
				if (volumeLongNote.Direction != noteInfo.Direction)
				{
					Debug.LogError(string.Format("VolumeLong end direction mismatch. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
					return;
				}

				if (HasFutureVolumeLongPoint(noteInfo))
				{
					AddRelayNote(noteInfo, info, id, laneStart, laneEnd, NoteCategory.VolumeFlickLong);
					return;
				}

				note = new VolumeLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType, noteInfo.Direction);
				LongJoinCheck(ref noteInfo, ref note);
				return;
			}

			note = new VolumeLongNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType, noteInfo.Direction);
			tmpLong[noteInfo.LongNo] = note as LongNote;
			musicScoreNoteList.Add(note);
		}

		// フリーレーンロングノーツを変換する
		private void ConvertFreeLaneLongNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd)
		{
			tmpLong.TryGetValue(noteInfo.LongNo, out LongNote activeLongNote);
			if (activeLongNote != null &&
				!(activeLongNote is FreeLaneLongNote))
			{
				Debug.LogError(string.Format("Duplicate free lane long note line: {0}", noteInfo.LongNo));
				return;
			}
			if (activeLongNote != null)
			{
				if (noteInfo.LongPointRole == LongPointRole.Start)
				{
					Debug.LogError(string.Format("Duplicate free lane long note start: {0}", noteInfo.LongNo));
					return;
				}
				AddRelayNote(noteInfo, info, id, laneStart, laneEnd, NoteCategory.FreeLaneLong);
				return;
			}

			NoteBase note = new FreeLaneLongNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType);
			LongJoinCheck(ref noteInfo, ref note);
			tmpLong[noteInfo.LongNo] = note as LongNote;
			musicScoreNoteList.Add(note);
		}

		// 固定レーンロングノーツを変換する
		private void ConvertFixedLaneLongNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd)
		{
			tmpLong.TryGetValue(noteInfo.LongNo, out LongNote activeLongNote);
			if (activeLongNote != null)
			{
				Debug.LogError(string.Format("FLong does not support relay long note line: {0}", noteInfo.LongNo));
				return;
			}
			if (noteInfo.LongPointRole == LongPointRole.Relay)
			{
				Debug.LogError(string.Format("FLong does not support relay long note line: {0}", noteInfo.LongNo));
				return;
			}

			NoteBase note = new FixedLongNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio);
			LongJoinCheck(ref noteInfo, ref note);
			tmpLong[noteInfo.LongNo] = note as LongNote;
			musicScoreNoteList.Add(note);
		}

		// スライダーロングノーツを変換する
		private void ConvertSliderLongNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd)
		{
			if (noteInfo.LongNo == -1)
			{
				Debug.LogError(string.Format("SliderLong note requires long number. bar:{0}", noteInfo.Bar));
				return;
			}

			NoteBase note;
			if (noteInfo.Direction == NoteDirection.Right || noteInfo.LongPointRole == LongPointRole.End)
			{
				note = new SliderLongEndNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio);
				LongJoinCheck(ref noteInfo, ref note);
				if (note.ParentNote == null)
				{
					Debug.LogError(string.Format("SliderLong start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				}
				return;
			}

			LongNote activeLongNote;
			if (tmpLong.TryGetValue(noteInfo.LongNo, out activeLongNote) &&
				activeLongNote != null &&
				!(activeLongNote is SliderLongNote))
			{
				Debug.LogError(string.Format("Duplicate slider long note line: {0}", noteInfo.LongNo));
				return;
			}
			if (activeLongNote != null)
			{
				if (noteInfo.LongPointRole == LongPointRole.Start)
				{
					Debug.LogError(string.Format("Duplicate slider long note start: {0}", noteInfo.LongNo));
					return;
				}
				AddRelayNote(noteInfo, info, id, laneStart, laneEnd, NoteCategory.SliderLong);
				return;
			}
			if (noteInfo.LongPointRole == LongPointRole.Relay &&
				noteInfo.LineType != NoteLineType.RightAngle)
			{
				Debug.LogError(string.Format("SliderLong relay start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				return;
			}

			note = new SliderLongNote(info, id, laneStart, laneEnd, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType);
			tmpLong[noteInfo.LongNo] = note as LongNote;
			musicScoreNoteList.Add(note);
		}

		// ガイドノーツを変換する
		private void ConvertGuideNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd, ref NoteCategory category)
		{
			if (noteInfo.LongPointRole == LongPointRole.Relay &&
				noteInfo.LineType != NoteLineType.RightAngle &&
				(!tmpGuide.ContainsKey(noteInfo.LongNo) || tmpGuide[noteInfo.LongNo] == null))
			{
				Debug.LogError(string.Format("Guide relay start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				return;
			}
			if (noteInfo.LongPointRole == LongPointRole.Start &&
				tmpGuide.ContainsKey(noteInfo.LongNo) &&
				tmpGuide[noteInfo.LongNo] != null)
			{
				Debug.LogError(string.Format("Duplicate guide long note start: {0}", noteInfo.LongNo));
				return;
			}

			NoteBase note = new GuideNote(info, id, laneStart, laneEnd, category, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType);
			GuideJoinCheck(ref noteInfo, ref note);
			tmpGuide[noteInfo.LongNo] = note as GuideNote;
			musicScoreNoteList.Add(note);
		}

		// ガイド終端ノーツを変換する
		private void ConvertGuideEndNote(ref int id, ref NoteInfo noteInfo, ref MusicScoreInfo info, ref int laneStart, ref int laneEnd, ref NoteCategory category)
		{
			NoteBase note = new GuideEndNote(info, id, laneStart, laneEnd, category, liveBundleBuildData, noteInfo.SpeedRatio, NoteLineType.Linear);
			GuideJoinCheck(ref noteInfo, ref note);
			if (note.ParentNote == null)
			{
				if (noteInfo.LongPointRole == LongPointRole.End)
				{
					Debug.LogError(string.Format("Guide start note was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				}
				musicScoreNoteList.Add(note);
			}
		}

		// 生成したイベントを発火時刻順に並べ替える
		private void SortMusicEventList()
		{
			musicScoreEventList = musicScoreEventList.OrderBy(x => x.MusicScoreInfo.time).ToList();
		}

		// ロングノーツの始点、終点、中継点を接続する
		private void LongJoinCheck(ref NoteInfo noteInfo, ref NoteBase note)
		{
			if (tmpLong.ContainsKey(noteInfo.LongNo) && tmpLong[noteInfo.LongNo] != null)
			{
				LongNote longNote = tmpLong[noteInfo.LongNo];
				int comboBeat = GetComboBeat(longNote);
				int start = Mathf.FloorToInt(longNote.MusicScoreInfo.barProgress * comboBeat) + 1 + longNote.MusicScoreInfo.bar * comboBeat;
				int end = Mathf.CeilToInt(note.MusicScoreInfo.barProgress * comboBeat) + note.MusicScoreInfo.bar * comboBeat - 1;
				for (int i = start; i <= end; i++)
				{
					MusicScoreInfo info = musicScore.GenerateNoteMusicScoreInfo(i / comboBeat, (float)(i - comboBeat * (i / comboBeat)) / comboBeat);
					LongHoldCombo longHoldCombo = CreateHoldCombo(longNote, info, noteInfo.SpeedRatio);
					longNote.AddHoldCombo(longHoldCombo);
				}
				longNote.SetChildNote(note);
				note.SetParentNote(longNote);
				tmpLong[noteInfo.LongNo] = null;
			}
		}

		// ロングノーツ種別に応じたホールドコンボ間隔を取得する
		private int GetComboBeat(LongNote longNote)
		{
			if (longNote is FreeLaneLongNote)
			{
				return FreeLaneLongNoteComboBeat > 0
					? FreeLaneLongNoteComboBeat
					: liveBundleBuildData != null ? liveBundleBuildData.FreeLaneLongNoteComboBeat : GetDefaultComboBeat();
			}
			if (longNote is SliderLongNote)
			{
				return SliderLongNoteComboBeat > 0
					? SliderLongNoteComboBeat
					: liveBundleBuildData != null ? liveBundleBuildData.SliderLongNoteComboBeat : GetDefaultComboBeat();
			}
			if (longNote is VolumeLongNote)
			{
				return VolumeLongNoteComboBeat > 0
					? VolumeLongNoteComboBeat
					: liveBundleBuildData != null ? liveBundleBuildData.VolumeLongNoteComboBeat : GetDefaultComboBeat();
			}

			return GetDefaultComboBeat();
		}

		// 既定のホールドコンボ間隔を取得する
		private int GetDefaultComboBeat()
		{
			return LongNoteComboBeat > 0
				? LongNoteComboBeat
				: liveBundleBuildData != null ? liveBundleBuildData.LongNoteComboBeat : 8;
		}

		// ロングノーツ上のホールドコンボノーツを作成する
		private LongHoldCombo CreateHoldCombo(LongNote longNote, MusicScoreInfo info, float speedRatio)
		{
			float laneOffset = LiveUtility.GetLaneOffset(longNote.Category, liveBundleBuildData);
			if (longNote is FreeLaneLongNote)
			{
				return new FreeLaneLongHoldCombo(info, liveBundleBuildData, speedRatio, laneOffset);
			}
			if (longNote is SliderLongNote)
			{
				return new SliderLongHoldCombo(info, liveBundleBuildData, speedRatio, laneOffset);
			}
			if (longNote is VolumeLongNote)
			{
				return new VolumeLongHoldCombo(info, liveBundleBuildData, speedRatio, laneOffset);
			}

			return new LongHoldCombo(info, liveBundleBuildData, speedRatio, laneOffset, longNote.Category);
		}

		// ガイドノーツの始点と終点を接続する
		private void GuideJoinCheck(ref NoteInfo noteInfo, ref NoteBase note)
		{
			if (tmpGuide.ContainsKey(noteInfo.LongNo) && tmpGuide[noteInfo.LongNo] != null)
			{
				GuideNote guideNote = tmpGuide[noteInfo.LongNo];
				guideNote.SetChildNote(note);
				note.SetParentNote(guideNote);
				tmpGuide[noteInfo.LongNo] = null;
			}
		}

		// ロングノーツの中継点を追加する
		private void AddRelayNote(NoteInfo noteInfo, MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category)
		{
			if (!tmpLong.TryGetValue(noteInfo.LongNo, out LongNote longNote) || longNote == null)
			{
				Debug.LogError(string.Format("Relay long note start was not found. bar:{0} longNo:{1}", noteInfo.Bar, noteInfo.LongNo));
				return;
			}

			NoteBase relayNote = new LongRelayNote(info, id, laneStart, laneEnd, category, liveBundleBuildData, noteInfo.SpeedRatio, noteInfo.LineType);
			longNote.AddRelayNote(relayNote);
		}

		// 同じ LongNo のボリュームロング点が後続に存在するかを返す
		private bool HasFutureVolumeLongPoint(NoteInfo noteInfo)
		{
			float key = noteInfo.Bar + noteInfo.BarProgress;
			foreach (KeyValuePair<float, Dictionary<int, NoteInfo>> pair in noteInfoDict)
			{
				if (pair.Key <= key)
				{
					continue;
				}

				foreach (NoteInfo futureNoteInfo in pair.Value.Values)
				{
					if (futureNoteInfo.LongNo == noteInfo.LongNo &&
						futureNoteInfo.Category == NoteCategory.VolumeFlickLong &&
						(futureNoteInfo.Direction == noteInfo.Direction ||
							futureNoteInfo.Direction == NoteDirection.Default))
					{
						return true;
					}
				}
			}

			return false;
		}

		// ヘッダー行を処理し、処理済みなら true を返す
		private bool LeadHeader(string line)
		{
			if (line.Contains("#REQUEST \"ticks_per_beat"))
			{
				Match match = TicksPerBeatRegex.Match(line.Replace("\"", string.Empty));
				if (match.Success)
				{
					ticksPerBeat = int.Parse(match.Groups["ticks_per_beat"].Value, CultureInfo.InvariantCulture);
				}
				return false;
			}
			if (line.Contains("#TIL00"))
			{
				LoadHighSpeedInfo(line);
				return true;
			}
			if (line.Contains("#VOLUME"))
			{
				LoadVolumeInfo(line);
				return true;
			}
			if (line.Contains("#BPM"))
			{
				LoadBpmInfo(line);
			}
			return false;
		}

		// BPM 定義行を読み込む
		// lineStr: SUS の 1 行
		private void LoadBpmInfo(string lineStr)
		{
			Match match = BpmRegex.Match(lineStr);
			if (match.Success)
			{
				bpmMap[match.Groups["bpm_id"].Value] = float.Parse(match.Groups["bpm"].Value, CultureInfo.InvariantCulture);
			}
		}

		// ハイスピード定義行を読み込む
		// lineStr: SUS の 1 行
		private void LoadHighSpeedInfo(string lineStr)
		{
			string input = lineStr.Replace("\"", string.Empty).Replace("#TIL00: ", string.Empty).Replace(", ", "\r");
			MatchCollection matchCollection = HighSpeedRegex.Matches(input);
			List<HighSpeedInfo> list = new List<HighSpeedInfo>(matchCollection.Count);
			foreach (Match match in matchCollection)
			{
				if (match.Success)
				{
					list.Add(new HighSpeedInfo(
						int.Parse(match.Groups["barIndex"].Value, CultureInfo.InvariantCulture),
						int.Parse(match.Groups["tickOffset"].Value, CultureInfo.InvariantCulture),
						float.Parse(match.Groups["speedRatio"].Value, CultureInfo.InvariantCulture)));
				}
			}
			highSpeedInfos = list;
		}

		// SE 音量定義行を読み込む
		// lineStr: SUS の 1 行
		private void LoadVolumeInfo(string lineStr)
		{
			string input = lineStr.Replace("\"", string.Empty).Replace("#VOLUME: ", string.Empty).Replace(", ", "\r");
			MatchCollection matchCollection = VolumeRegex.Matches(input);
			List<VolumeInfo> list = new List<VolumeInfo>(matchCollection.Count);
			foreach (Match match in matchCollection)
			{
				if (match.Success)
				{
					list.Add(new VolumeInfo(
						int.Parse(match.Groups["barIndex"].Value, CultureInfo.InvariantCulture),
						int.Parse(match.Groups["tickOffset"].Value, CultureInfo.InvariantCulture),
						float.Parse(match.Groups["volume"].Value, CultureInfo.InvariantCulture)));
				}
			}
			volumeInfos = list;
		}

		// SUS のイベントまたはノーツ行を種類別に読み込む
		// lineStr: SUS の 1 行
		private void LoadEvent(string lineStr)
		{
			Match readableMatch = ReadableSusNotesRegex.Match(lineStr);
			if (readableMatch.Success)
			{
				LoadReadableSusEvent(readableMatch);
				return;
			}

			Match fourKeyLongMatch = FourKeyLongNotesRegex.Match(lineStr);
			if (fourKeyLongMatch.Success)
			{
				LoadFourKeyLongEvent(fourKeyLongMatch);
				return;
			}

			Match longMatch = EventLongNotesRegex.Match(lineStr);
			if (longMatch.Success)
			{
				LoadLongEvent(longMatch);
				return;
			}

			Match fourKeyMatch = FourKeyEventNotesRegex.Match(lineStr);
			if (fourKeyMatch.Success)
			{
				LoadFourKeyEvent(fourKeyMatch);
				return;
			}

			Match match = EventNotesRegex.Match(lineStr);
			if (match.Success)
			{
				int bar = int.Parse(match.Groups["bar"].Value, CultureInfo.InvariantCulture);
				string eventType = match.Groups["event_type"].Value;
				string data = match.Groups["data"].Value.Replace("\r", string.Empty);
				if (data.Contains(","))
				{
					string[] values;
					float[] speedRatios;
					GetSpeedRatioValues(data, out values, out speedRatios);
					if (eventType != "02" && eventType != "08")
					{
						string[] laneData = SplitAtCount(eventType, 1);
						CreateBarData(bar, int.Parse(laneData[0], CultureInfo.InvariantCulture), GetNoteLine(laneData[1]), values, speedRatios);
					}
				}
				else
				{
					string normalizedData = data.Replace(" ", string.Empty);
					string[] values = SplitAtCount(normalizedData, 2);
					if (eventType == "02")
					{
						CreateTimeSignaturesEventBarData(bar, normalizedData);
					}
					else if (eventType == "08")
					{
						CreateBpmEventBarData(bar, values);
					}
					else
					{
						string[] laneData = SplitAtCount(eventType, 1);
						CreateBarData(bar, int.Parse(laneData[0], CultureInfo.InvariantCulture), GetNoteLine(laneData[1]), values, null);
					}
				}
			}
		}

		// 通常 SUS 記法のロングイベント行を読み込む
		// match: EventLongNotesRegex のマッチ結果
		private void LoadLongEvent(Match match)
		{
			int bar = int.Parse(match.Groups["bar"].Value, CultureInfo.InvariantCulture);
			int laneType = int.Parse(match.Groups["laneType"].Value, CultureInfo.InvariantCulture);
			int lane = GetNoteLine(match.Groups["lineNo"].Value);
			int longNo = int.Parse(match.Groups["longNo"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			string data = match.Groups["data"].Value.Replace("\r", string.Empty);
			if (data.Contains(","))
			{
				string[] values;
				float[] speedRatios;
				GetSpeedRatioValues(data, out values, out speedRatios);
				CreateBarData(bar, laneType, lane, values, speedRatios, longNo);
			}
			else
			{
				CreateBarData(bar, laneType, lane, SplitAtCount(data, 2), null, longNo);
			}
		}

		// 4 レーン記法のロングイベント行を読み込む
		// match: FourKeyLongNotesRegex のマッチ結果
		private void LoadFourKeyLongEvent(Match match)
		{
			int bar = int.Parse(match.Groups["bar"].Value, CultureInfo.InvariantCulture);
			int laneType = int.Parse(match.Groups["laneType"].Value, CultureInfo.InvariantCulture);
			int keyIndex = GetFourKeyIndex(match.Groups["keyNo"].Value);
			if (keyIndex < 0)
			{
				Debug.LogError(string.Format("Invalid 4Key lane: {0}. bar:{1}", match.Groups["keyNo"].Value, bar));
				return;
			}

			int lane = FourKeyLaneStarts[keyIndex];
			int longNo = int.Parse(match.Groups["longNo"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			string data = match.Groups["data"].Value.Replace("\r", string.Empty);
			if (data.Contains(","))
			{
				string[] values;
				float[] speedRatios;
				GetSpeedRatioValues(data, out values, out speedRatios);
				CreateBarData(bar, laneType, lane, values, speedRatios, longNo, true, keyIndex);
			}
			else
			{
				CreateBarData(bar, laneType, lane, SplitAtCount(data, 2), null, longNo, true, keyIndex);
			}
		}

		// 4 レーン記法の通常イベント行を読み込む
		// match: FourKeyEventNotesRegex のマッチ結果
		private void LoadFourKeyEvent(Match match)
		{
			int bar = int.Parse(match.Groups["bar"].Value, CultureInfo.InvariantCulture);
			string eventType = match.Groups["event_type"].Value;
			string[] laneData = SplitAtCount(eventType, 1);
			int laneType = int.Parse(laneData[0], CultureInfo.InvariantCulture);
			int keyIndex = GetFourKeyIndex(laneData[1]);
			if (keyIndex < 0)
			{
				Debug.LogError(string.Format("Invalid 4Key lane: {0}. bar:{1}", laneData[1], bar));
				return;
			}

			int lane = FourKeyLaneStarts[keyIndex];
			string data = match.Groups["data"].Value.Replace("\r", string.Empty);
			if (data.Contains(","))
			{
				string[] values;
				float[] speedRatios;
				GetSpeedRatioValues(data, out values, out speedRatios);
				CreateBarData(bar, laneType, lane, values, speedRatios, -1, true, keyIndex);
			}
			else
			{
				string normalizedData = data.Replace(" ", string.Empty);
				CreateBarData(bar, laneType, lane, SplitAtCount(normalizedData, 2), null, -1, true, keyIndex);
			}
		}

		// 可読 SUS 記法のイベント行を読み込む
		// match: ReadableSusNotesRegex のマッチ結果
		private void LoadReadableSusEvent(Match match)
		{
			int bar = int.Parse(match.Groups["bar"].Value, CultureInfo.InvariantCulture);
			string[] path = match.Groups["path"].Value.Split('.');
			if (path.Length < 3)
			{
				Debug.LogError(string.Format("Invalid readable SUS path. bar:{0}", bar));
				return;
			}

			string family = path[0].ToLowerInvariant();
			NoteDirection headerDirection = NoteDirection.Default;
			string shape = RemoveDirectionSuffix(path[1], ref headerDirection).ToLowerInvariant();
			string position = RemoveDirectionSuffix(path[2], ref headerDirection).ToLowerInvariant();

			int lane;
			int fourKeyIndex;
			bool useFourKeyWidth;
			if (!TryGetReadableLane(position, bar, out lane, out fourKeyIndex, out useFourKeyWidth))
			{
				return;
			}

			int longNo = -1;
			if (shape == "long")
			{
				if (path.Length < 4 || !TryParseReadableLongNo(path[3], out longNo))
				{
					Debug.LogError(string.Format("Readable long note requires long id. bar:{0}", bar));
					return;
				}
			}

			string data = match.Groups["data"].Value.Replace("\r", string.Empty);
			string[] values;
			float[] speedRatios = null;
			if (data.Contains(","))
			{
				GetReadableSpeedRatioValues(data, out values, out speedRatios);
			}
			else
			{
				values = SplitAtCount(data.Replace(" ", string.Empty), 2);
			}

			CreateReadableNoteBarData(bar, family, shape, lane, values, speedRatios, longNo, useFourKeyWidth, fourKeyIndex, headerDirection);
		}

		// 可読 SUS の 1 小節分のノーツデータを作成する
		private void CreateReadableNoteBarData(int bar, string family, string shape, int lane, string[] values, float[] speedRatios, int longNo, bool useFourKeyWidth, int fourKeyIndex, NoteDirection headerDirection)
		{
			if (!IsValidReadableHeader(family, shape, useFourKeyWidth, headerDirection, bar))
			{
				return;
			}

			for (int i = 0; i < values.Length; i++)
			{
				string text = values[i];
				if (text == "00")
				{
					continue;
				}
				if (text.Length != 2)
				{
					Debug.LogError(string.Format("Invalid readable note token: {0}. bar:{1}", text, bar));
					continue;
				}

				float speedRatio = speedRatios == null || i >= speedRatios.Length ? 1f : speedRatios[i];
				char action = text[0];
				char value = text[1];
				int noteWidth = GetNoteWidth(value.ToString());
				if (noteWidth == -1)
				{
					Debug.LogError(string.Format("Invalid readable note width: {0}. bar:{1}", value, bar));
					continue;
				}
				if (useFourKeyWidth && !IsValidFourKeyWidth(fourKeyIndex, noteWidth))
				{
					Debug.LogError(string.Format("Invalid 4Key note width. bar:{0} lane:{1} width:{2}", bar, fourKeyIndex, noteWidth));
					continue;
				}

				float barProgress = (float)i / values.Length;
				if (!AddReadableNoteInfo(bar, barProgress, family, shape, lane, noteWidth, action, speedRatio, longNo, headerDirection, useFourKeyWidth))
				{
					Debug.LogError(string.Format("Unsupported readable note token: {0}. bar:{1}", text, bar));
				}
			}
		}

		// 可読 SUS の 1 ノーツを中間ノーツ情報へ追加する
		private bool AddReadableNoteInfo(int bar, float barProgress, string family, string shape, int lane, int width, char action, float speedRatio, int longNo, NoteDirection headerDirection, bool useFourKeyWidth)
		{
			char code = char.ToLowerInvariant(action);
			if (shape == "tap")
			{
				if (code != 'n' && code != 'c')
				{
					return false;
				}

				NoteCategory category = useFourKeyWidth ? NoteCategory.FixedNormal : NoteCategory.FreeLaneNormal;
				AddNoteInfo(bar, barProgress, lane, width, category, speedRatio);
				return true;
			}

			if (family == "slider" && shape == "flick")
			{
				NoteDirection direction;
				if (!TryGetReadableDirection(code, out direction))
				{
					return false;
				}

				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.SliderFlick, speedRatio, direction);
				return true;
			}

			if (family == "volume" && shape == "flick")
			{
				NoteDirection direction;
				if (!TryGetReadableVolumeDirection(code, out direction))
				{
					return false;
				}

				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.VolumeFlick, speedRatio, direction);
				return true;
			}

			if (shape != "long")
			{
				return false;
			}

			NoteLineType lineType = GetReadableLineType(code);
			LongPointRole role = GetReadableLongPointRole(code);
			if (code == 'e')
			{
				if (family == "free")
				{
					AddNoteInfo(bar, barProgress, lane, width, NoteCategory.FreeLaneNormal, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo, LongPointRole.End);
					return true;
				}
				if (family == "fixed")
				{
					AddNoteInfo(bar, barProgress, lane, width, NoteCategory.FixedNormal, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo, LongPointRole.End);
					return true;
				}
				if (family == "slider")
				{
					AddNoteInfo(bar, barProgress, lane, width, NoteCategory.SliderLong, speedRatio, NoteDirection.Right, NoteLineType.Linear, longNo, LongPointRole.End);
					return true;
				}
				if (family == "volume")
				{
					AddNoteInfo(bar, barProgress, lane, width, NoteCategory.VolumeFlickLong, speedRatio, headerDirection, NoteLineType.Linear, longNo, LongPointRole.End);
					return true;
				}
				if (family == "guide")
				{
					AddNoteInfo(bar, barProgress, lane, width, NoteCategory.GuideEnd, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo, LongPointRole.End);
					return true;
				}
			}

			if (code != 's' && code != 'm' && code != 'r' && code != 'i' && code != 'o' && code != 'a')
			{
				return false;
			}

			if (family == "free")
			{
				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.FreeLaneLong, speedRatio, NoteDirection.Default, lineType, longNo, role);
				return true;
			}
			if (family == "fixed")
			{
				if (code != 's')
				{
					return false;
				}
				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.FixedLong, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo, LongPointRole.Start);
				return true;
			}
			if (family == "slider")
			{
				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, lineType, longNo, role);
				return true;
			}
			if (family == "volume")
			{
				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.VolumeFlickLong, speedRatio, headerDirection, lineType, longNo, role);
				return true;
			}
			if (family == "guide")
			{
				AddNoteInfo(bar, barProgress, lane, width, NoteCategory.GuideLong, speedRatio, NoteDirection.Default, lineType, longNo, role);
				return true;
			}

			return false;
		}

		// 拍子イベント行から TimeSignatureInfo を作成する
		private void CreateTimeSignaturesEventBarData(int bar, string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				Debug.LogError(string.Format("Invalid time signature: {0}. bar:{1}", data, bar));
				return;
			}
			timeSignaturesInfos.Add(new TimeSignatureInfo(float.Parse(data, CultureInfo.InvariantCulture), bar));
		}

		// BPM イベント行から BpmInfo を作成する
		private void CreateBpmEventBarData(int bar, string[] values)
		{
			for (int i = 0; i < values.Length; i++)
			{
				string text = values[i];
				if (text == "00")
				{
					continue;
				}
				if (bpmMap.ContainsKey(text))
				{
					bpmInfos.Add(new BpmInfo(bpmMap[text], bar, (float)i / values.Length));
				}
				else
				{
					Debug.LogError(string.Format("Invalid BPM setting {0}. bar:{1}", text, bar));
				}
			}
		}

		// 通常 SUS の 1 小節分データを種別に応じて作成する
		private void CreateBarData(int bar, int laneType, int lane, string[] values, float[] speedRatios, int longNo = -1, bool useFourKeyWidth = false, int fourKeyIndex = -1)
		{
			if ((uint)laneType <= 9u && ((1 << laneType) & 0x22E) != 0)
			{
				CreateNoteBarData(bar, laneType, lane, values, speedRatios, longNo, useFourKeyWidth, fourKeyIndex);
			}
		}

		// 通常 SUS の 1 小節分ノーツデータを作成する
		private void CreateNoteBarData(int bar, int laneType, int lane, string[] values, float[] speedRatios, int longNo = -1, bool useFourKeyWidth = false, int fourKeyIndex = -1)
		{
			for (int i = 0; i < values.Length; i++)
			{
				string text = values[i];
				if (text == "00")
				{
					continue;
				}
				float speedRatio = speedRatios == null ? 1f : speedRatios[i];
				string[] array = SplitAtCount(text, 1);
				int num = int.Parse(array[0], CultureInfo.InvariantCulture);
				int noteWidth = useFourKeyWidth ? GetFourKeyNoteWidth(array[1]) : GetNoteWidth(array[1]);
				int noteLane = lane;
				if (useFourKeyWidth && !IsValidFourKeyWidth(fourKeyIndex, noteWidth))
				{
					Debug.LogError(string.Format("Invalid 4Key note width. bar:{0} lane:{1} width:{2}", bar, fourKeyIndex, noteWidth));
					continue;
				}
				float barProgress = (float)i / values.Length;

				if (longNo == -1)
				{
					if (laneType == 5)
					{
						switch (num)
						{
							case 0:
								if (useFourKeyWidth)
								{
									// 4Key レーンは可読性のためタップ専用として扱う
									// AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlick, speedRatio, NoteDirection.Left);
									break;
								}
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlick, speedRatio, NoteDirection.Left);
								break;
							case 1:
								if (useFourKeyWidth)
								{
									// 4Key レーンは可読性のためタップ専用として扱う
									// AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlick, speedRatio, NoteDirection.Right);
									break;
								}
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlick, speedRatio, NoteDirection.Right);
								break;
							case 3:
								if (useFourKeyWidth)
								{
									// 4Key lanes should stay tap-only for readability.
									// AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.Flick, useFourKeyWidth), speedRatio, NoteDirection.Left);
									break;
								}
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.SliderFlick, useFourKeyWidth), speedRatio, NoteDirection.Left);
								break;
							case 4:
								if (useFourKeyWidth)
								{
									// 4Key lanes should stay tap-only for readability.
									// AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.Flick, useFourKeyWidth), speedRatio, NoteDirection.Right);
									break;
								}
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.SliderFlick, useFourKeyWidth), speedRatio, NoteDirection.Right);
								break;
						}
					}
					else if (laneType == 1)
					{
						switch (num)
						{
							case 1:
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.FreeLaneNormal, useFourKeyWidth), speedRatio);
								break;
							case 2:
								AddNoteInfo(bar, barProgress, noteLane, noteWidth, GetFourKeyCategory(NoteCategory.FreeLaneNormal, useFourKeyWidth), speedRatio);
								break;
						}
					}
					else if (laneType == 2)
					{
						Debug.LogError(string.Format("SliderLong note requires long format #BBB2LN. bar:{0}", bar));
					}
				}
				else if (laneType == 9)
				{
					switch (num)
						{
							case 1:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.GuideLong, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 2:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.GuideEnd, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 5:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.GuideLong, speedRatio, NoteDirection.Default, NoteLineType.EaseIn, longNo);
							break;
						case 6:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.GuideLong, speedRatio, NoteDirection.Default, NoteLineType.EaseOut, longNo);
							break;
					}
				}
				else if (laneType == 5)
				{
					if (useFourKeyWidth)
					{
						// 4Key lanes should stay tap-only for readability.
						// AddNoteInfo(..., NoteCategory.VolumeFlickLong, ...);
						continue;
					}
					switch (num)
					{
						case 0:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Left, NoteLineType.Linear, longNo);
							break;
						case 1:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Right, NoteLineType.Linear, longNo);
							break;
						case 2:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Left, NoteLineType.EaseIn, longNo);
							break;
						case 3:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Left, NoteLineType.EaseOut, longNo);
							break;
						case 4:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Right, NoteLineType.EaseIn, longNo);
							break;
						case 5:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.VolumeFlickLong, speedRatio, NoteDirection.Right, NoteLineType.EaseOut, longNo);
							break;
					}
				}
				else if (laneType == 3)
				{
					NoteCategory longCategory = GetFourKeyCategory(NoteCategory.FreeLaneLong, useFourKeyWidth);
					NoteCategory endCategory = GetFourKeyCategory(NoteCategory.FreeLaneNormal, useFourKeyWidth);
					switch (num)
					{
						case 1:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 2:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, endCategory, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 3:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 4:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, endCategory, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 5:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, GetLineTypeForCategory(longCategory, NoteLineType.EaseIn), longNo);
							break;
						case 6:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, GetLineTypeForCategory(longCategory, NoteLineType.EaseOut), longNo);
							break;
						case 7:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, GetLineTypeForCategory(longCategory, NoteLineType.EaseIn), longNo);
							break;
						case 8:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, longCategory, speedRatio, NoteDirection.Default, GetLineTypeForCategory(longCategory, NoteLineType.EaseOut), longNo);
							break;
					}
				}
				else if (laneType == 2)
				{
					switch (num)
					{
						case 1:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 2:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Right, NoteLineType.Linear, longNo);
							break;
						case 3:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.Linear, longNo);
							break;
						case 4:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Right, NoteLineType.Linear, longNo);
							break;
						case 5:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.EaseIn, longNo);
							break;
						case 6:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.EaseOut, longNo);
							break;
						case 7:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.EaseIn, longNo);
							break;
						case 8:
							AddNoteInfo(bar, barProgress, noteLane, noteWidth, NoteCategory.SliderLong, speedRatio, NoteDirection.Default, NoteLineType.EaseOut, longNo);
							break;
					}
				}
			}
		}

		private static NoteCategory GetFourKeyCategory(NoteCategory category, bool useFourKeyWidth)
		{
			if (!useFourKeyWidth)
			{
				return category;
			}

			switch (category)
			{
				case NoteCategory.FreeLaneLong:
					return NoteCategory.FixedLong;
				case NoteCategory.FreeLaneNormal:
					return NoteCategory.FixedNormal;
				default:
					return category;
			}
		}

		private static NoteLineType GetLineTypeForCategory(NoteCategory category, NoteLineType lineType)
		{
			return category == NoteCategory.FixedLong ? NoteLineType.Linear : lineType;
		}

		private void AddNoteInfo(int bar, float barProgress, int lane, int width, NoteCategory category, float speedRatio, NoteDirection direction = NoteDirection.Default, NoteLineType lineType = NoteLineType.Linear, int longNo = -1, LongPointRole longPointRole = LongPointRole.Auto)
		{
			if (width == -1)
			{
				Debug.LogError(string.Format("Invalid note width. bar:{0}", bar));
				return;
			}
			if (lane < 0)
			{
				Debug.LogError(string.Format("Invalid lane. bar:{0}", bar));
				return;
			}
			if (lane >= LiveConfig.LaneCount)
			{
				Debug.LogError(string.Format("Invalid lane. bar:{0}", bar));
				return;
			}
			if (width + lane > LiveConfig.LaneCount)
			{
				Debug.LogError(string.Format("Invalid note right lane. bar:{0}", bar));
				return;
			}
			if (!IsGuideCategory(category))
			{
				AddNormalNoteInfo(bar, barProgress, lane, width, category, speedRatio, direction, lineType, longNo, longPointRole);
			}
			AddGuideInfo(bar, barProgress, lane, width, category, speedRatio, direction, lineType, longNo, longPointRole);
		}

		private void AddNormalNoteInfo(int bar, float barProgress, int lane, int width, NoteCategory category, float speedRatio, NoteDirection direction = NoteDirection.Default, NoteLineType lineType = NoteLineType.Linear, int longNo = -1, LongPointRole longPointRole = LongPointRole.Auto)
		{
			float key = bar + barProgress;
			int noteKey = GetNoteInfoKey(lane, category);
			if (!noteInfoDict.TryGetValue(key, out Dictionary<int, NoteInfo> notes))
			{
				notes = new Dictionary<int, NoteInfo>();
				noteInfoDict.Add(key, notes);
			}

			if (notes.TryGetValue(noteKey, out NoteInfo existingNoteInfo))
			{
				if (ShouldKeepSeparateLongBoundary(existingNoteInfo, category, direction, longNo, longPointRole))
				{
					noteKey = GetSeparateLongBoundaryNoteInfoKey(noteKey, category, direction, longNo, longPointRole);
					if (notes.TryGetValue(noteKey, out NoteInfo boundaryNoteInfo))
					{
						boundaryNoteInfo.Update(category, direction, lineType, speedRatio, longNo, longPointRole);
					}
					else
					{
						notes.Add(noteKey, new NoteInfo(bar, barProgress, lane, width, category, direction, lineType, speedRatio, longNo, longPointRole));
					}
					return;
				}

				existingNoteInfo.Update(category, direction, lineType, speedRatio, longNo, longPointRole);
			}
			else
			{
				notes.Add(noteKey, new NoteInfo(bar, barProgress, lane, width, category, direction, lineType, speedRatio, longNo, longPointRole));
			}
		}

		private static bool ShouldKeepSeparateLongBoundary(NoteInfo existingNoteInfo, NoteCategory category, NoteDirection direction, int longNo, LongPointRole longPointRole)
		{
			if (existingNoteInfo == null ||
				existingNoteInfo.LongNo == -1 ||
				longNo == -1 ||
				existingNoteInfo.LongNo == longNo)
			{
				return false;
			}

			bool existingIsStart = IsLongStartBoundary(existingNoteInfo.Category, existingNoteInfo.Direction, existingNoteInfo.LongPointRole);
			bool existingIsEnd = IsLongEndBoundary(existingNoteInfo.Category, existingNoteInfo.Direction, existingNoteInfo.LongPointRole);
			bool currentIsStart = IsLongStartBoundary(category, direction, longPointRole);
			bool currentIsEnd = IsLongEndBoundary(category, direction, longPointRole);
			return (existingIsEnd && currentIsStart) || (existingIsStart && currentIsEnd);
		}

		private static int GetSeparateLongBoundaryNoteInfoKey(int noteKey, NoteCategory category, NoteDirection direction, int longNo, LongPointRole longPointRole)
		{
			int boundaryLayer = IsLongEndBoundary(category, direction, longPointRole) ? 2 : 1;
			unchecked
			{
				return noteKey + 100000 + (int)category * 1000 + boundaryLayer * 100 + longNo;
			}
		}

		private static bool IsLongStartBoundary(NoteCategory category, NoteDirection direction, LongPointRole longPointRole)
		{
			if (longPointRole == LongPointRole.Start || longPointRole == LongPointRole.Relay)
			{
				return true;
			}
			if (longPointRole == LongPointRole.End)
			{
				return false;
			}

			if (category == NoteCategory.SliderLong)
			{
				return direction != NoteDirection.Right;
			}
			if (category == NoteCategory.VolumeFlickLong)
			{
				return direction != NoteDirection.Default;
			}
			return category == NoteCategory.FreeLaneLong ||
				category == NoteCategory.FixedLong ||
				category == NoteCategory.GuideLong;
		}

		private static bool IsLongEndBoundary(NoteCategory category, NoteDirection direction, LongPointRole longPointRole)
		{
			if (longPointRole == LongPointRole.End)
			{
				return true;
			}
			if (longPointRole == LongPointRole.Start || longPointRole == LongPointRole.Relay)
			{
				return false;
			}

			if (category == NoteCategory.SliderLong)
			{
				return direction == NoteDirection.Right;
			}
			if (category == NoteCategory.VolumeFlickLong)
			{
				return direction == NoteDirection.Default;
			}
			if (category == NoteCategory.SliderFlick)
			{
				return true;
			}
			return category == NoteCategory.FreeLaneNormal ||
				category == NoteCategory.FixedNormal ||
				category == NoteCategory.GuideEnd;
		}

		private void AddGuideInfo(int bar, float barProgress, int lane, int width, NoteCategory category, float speedRatio, NoteDirection direction = NoteDirection.Default, NoteLineType lineType = NoteLineType.Linear, int longNo = -1, LongPointRole longPointRole = LongPointRole.Auto)
		{
			float key = bar + barProgress;
			NoteInfo noteInfo;
			if (TryGetNoteInfo(key, lane, category, out noteInfo))
			{
				if (noteInfo.Direction != NoteDirection.Default)
				{
					direction = noteInfo.Direction;
				}
				if (noteInfo.LineType != NoteLineType.Linear)
				{
					lineType = noteInfo.LineType;
				}
			}
			if (IsGuideCategory(category))
			{
				int guideKey = GetNoteInfoKey(lane, category);
				if (!guideInfoDict.TryGetValue(key, out Dictionary<int, NoteInfo> guides))
				{
					guides = new Dictionary<int, NoteInfo>();
					guideInfoDict.Add(key, guides);
				}
				if (!guides.ContainsKey(guideKey))
				{
					guides.Add(guideKey, new NoteInfo(bar, barProgress, lane, width, category, direction, lineType, speedRatio, longNo, longPointRole));
				}
			}
			int noteKey = GetNoteInfoKey(lane, category);
			if (guideInfoDict.TryGetValue(key, out Dictionary<int, NoteInfo> existingGuides) &&
				existingGuides.TryGetValue(noteKey, out NoteInfo guideToUpdate))
			{
				guideToUpdate.Update(category, direction, lineType, speedRatio, longNo, longPointRole);
			}
		}

		private static int GetNoteInfoKey(int lane, NoteCategory category)
		{
			return lane * 100 + GetNoteInfoLayer(category);
		}

		private static int GetNoteInfoLayer(NoteCategory category)
		{
			switch (category)
			{
				case NoteCategory.FreeLaneNormal:
					return 0;
				case NoteCategory.FreeLaneLong:
					return 1;
				case NoteCategory.SliderFlick:
					return 2;
				case NoteCategory.GuideLong:
					return 3;
				case NoteCategory.GuideEnd:
					return 4;
				case NoteCategory.FixedNormal:
					return 10;
				case NoteCategory.FixedLong:
					return 11;
				case NoteCategory.SliderLong:
					return 12;
				case NoteCategory.VolumeFlick:
					return 13;
				case NoteCategory.VolumeFlickLong:
					return 14;
				default:
					return (int)category;
			}
		}

		private bool TryGetNoteInfo(float key, int lane, NoteCategory category, out NoteInfo noteInfo)
		{
			noteInfo = null;
			if (!noteInfoDict.TryGetValue(key, out Dictionary<int, NoteInfo> notes))
			{
				return false;
			}

			if (notes.TryGetValue(GetNoteInfoKey(lane, category), out noteInfo))
			{
				return true;
			}

			int bestPriority = int.MaxValue;
			int bestKey = int.MaxValue;
			foreach (KeyValuePair<int, NoteInfo> pair in notes)
			{
				NoteInfo value = pair.Value;
				if (value == null || value.Lane != lane || !CanInheritGuideInfoFrom(value))
				{
					continue;
				}

				int priority = GetGuideInheritancePriority(value);
				if (priority < bestPriority || priority == bestPriority && pair.Key < bestKey)
				{
					bestPriority = priority;
					bestKey = pair.Key;
					noteInfo = value;
				}
			}

			return noteInfo != null;
		}

		private static bool CanInheritGuideInfoFrom(NoteInfo noteInfo)
		{
			return noteInfo != null && !IsGuideCategory(noteInfo.Category);
		}

		private static int GetGuideInheritancePriority(NoteInfo noteInfo)
		{
			if (noteInfo == null)
			{
				return int.MaxValue;
			}

			if (noteInfo.LongNo != -1)
			{
				return 0;
			}

			switch (noteInfo.Category)
			{
				case NoteCategory.FreeLaneNormal:
				case NoteCategory.FixedNormal:
					return 1;
				case NoteCategory.SliderFlick:
				case NoteCategory.VolumeFlick:
					return 2;
				default:
					return 3;
			}
		}

		private static bool IsGuideCategory(NoteCategory category)
		{
			return category == NoteCategory.GuideLong ||
				category == NoteCategory.GuideEnd;
		}

		private bool IsValidReadableHeader(string family, string shape, bool useFourKeyWidth, NoteDirection headerDirection, int bar)
		{
			if (useFourKeyWidth && family != "fixed")
			{
				Debug.LogError(string.Format("Only fixed readable notes can use k1-k4 position. bar:{0}", bar));
				return false;
			}
			if (!useFourKeyWidth && family == "fixed")
			{
				Debug.LogError(string.Format("Fixed readable notes require k1-k4 position. bar:{0}", bar));
				return false;
			}
			if (family == "fixed")
			{
				return shape == "tap" || shape == "long";
			}
			if (family == "free")
			{
				return shape == "tap" || shape == "long";
			}
			if (family == "slider")
			{
				return shape == "flick" || shape == "long";
			}
			if (family == "volume")
			{
				if (shape == "long" && headerDirection == NoteDirection.Default)
				{
					Debug.LogError(string.Format("Volume readable long notes require long+ or long-. bar:{0}", bar));
					return false;
				}
				return shape == "flick" || shape == "long";
			}
			if (family == "guide")
			{
				return shape == "long";
			}

			Debug.LogError(string.Format("Invalid readable note family: {0}. bar:{1}", family, bar));
			return false;
		}

		private bool TryGetReadableLane(string position, int bar, out int lane, out int fourKeyIndex, out bool useFourKeyWidth)
		{
			lane = -1;
			fourKeyIndex = -1;
			useFourKeyWidth = false;
			if (string.IsNullOrEmpty(position) || position.Length < 2)
			{
				Debug.LogError(string.Format("Invalid readable note position. bar:{0}", bar));
				return false;
			}

			int value;
			if (!int.TryParse(position.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
			{
				Debug.LogError(string.Format("Invalid readable note position: {0}. bar:{1}", position, bar));
				return false;
			}

			if (position[0] == 'l')
			{
				lane = value - 1;
				if (lane >= 0 && lane < LiveConfig.LaneCount)
				{
					return true;
				}
			}
			else if (position[0] == 'k')
			{
				fourKeyIndex = value - 1;
				if (fourKeyIndex >= 0 && fourKeyIndex < FourKeyLaneStarts.Length)
				{
					lane = FourKeyLaneStarts[fourKeyIndex];
					useFourKeyWidth = true;
					return true;
				}
			}

			Debug.LogError(string.Format("Readable note position out of range: {0}. bar:{1}", position, bar));
			return false;
		}

		private static string RemoveDirectionSuffix(string value, ref NoteDirection direction)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			char suffix = value[value.Length - 1];
			if (suffix == '+')
			{
				direction = NoteDirection.Right;
				return value.Substring(0, value.Length - 1);
			}
			if (suffix == '-')
			{
				direction = NoteDirection.Left;
				return value.Substring(0, value.Length - 1);
			}

			return value;
		}

		private bool TryParseReadableLongNo(string value, out int longNo)
		{
			longNo = -1;
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			int result = 0;
			foreach (char rawChar in value)
			{
				char c = char.ToLowerInvariant(rawChar);
				int digit;
				if (c >= '0' && c <= '9')
				{
					digit = c - '0';
				}
				else if (c >= 'a' && c <= 'z')
				{
					digit = c - 'a' + 10;
				}
				else
				{
					return false;
				}
				result = result * 36 + digit;
			}

			longNo = result;
			return true;
		}

		private bool TryGetReadableDirection(char code, out NoteDirection direction)
		{
			if (code == 'l' || code == '<' || code == '-')
			{
				direction = NoteDirection.Left;
				return true;
			}
			if (code == 'r' || code == '>' || code == '+')
			{
				direction = NoteDirection.Right;
				return true;
			}

			direction = NoteDirection.Default;
			return false;
		}

		private bool TryGetReadableVolumeDirection(char code, out NoteDirection direction)
		{
			if (code == 'd' || code == '-')
			{
				direction = NoteDirection.Left;
				return true;
			}
			if (code == 'u' || code == '+')
			{
				direction = NoteDirection.Right;
				return true;
			}

			direction = NoteDirection.Default;
			return false;
		}

		private static NoteLineType GetReadableLineType(char code)
		{
			if (code == 'i')
			{
				return NoteLineType.EaseIn;
			}
			if (code == 'o')
			{
				return NoteLineType.EaseOut;
			}
			if (code == 'a')
			{
				return NoteLineType.RightAngle;
			}

			return NoteLineType.Linear;
		}

		private static LongPointRole GetReadableLongPointRole(char code)
		{
			if (code == 's')
			{
				return LongPointRole.Start;
			}
			if (code == 'm' || code == 'r')
			{
				return LongPointRole.Relay;
			}
			if (code == 'a')
			{
				return LongPointRole.Relay;
			}
			if (code == 'e')
			{
				return LongPointRole.End;
			}

			return LongPointRole.Auto;
		}

		private int GetFourKeyIndex(string keyNo)
		{
			int keyIndex = ParseSusLaneValue(keyNo);
			if (keyIndex >= 0 && keyIndex < FourKeyLaneStarts.Length)
			{
				return keyIndex;
			}

			return -1;
		}

		private bool IsValidFourKeyWidth(int keyIndex, int noteWidth)
		{
			if (keyIndex < 0 || keyIndex >= FourKeyLaneStarts.Length)
			{
				return false;
			}

			int maxWidth = FourKeyMaxNoteWidth - keyIndex * 2;
			return noteWidth >= FourKeyMinNoteWidth && noteWidth <= maxWidth && noteWidth % 2 == 0;
		}

		private int GetFourKeyNoteWidth(string noteWidth)
		{
			return ParseSusLaneValue(noteWidth);
		}

		private int GetNoteLine(string lineNo)
		{
			int lane = ParseSusLaneValue(lineNo);
			return lane >= 0 && lane < LiveConfig.LaneCount ? lane : -1;
		}

		private int ParseSusLaneValue(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return -1;
			}

			char c = char.ToLowerInvariant(value[0]);
			if (c >= '0' && c <= '9')
			{
				return c - '0';
			}
			if (c >= 'a' && c <= 'f')
			{
				return c - 'a' + 10;
			}
			return -1;
		}

		private int GetNoteWidth(string noteWidth)
		{
			int width = ParseSusWidthValue(noteWidth);
			return width > 0 && width <= LiveConfig.LaneCount ? width : -1;
		}

		private int ParseSusWidthValue(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return -1;
			}

			char c = char.ToLowerInvariant(value[0]);
			if (c >= '1' && c <= '9')
			{
				return c - '0';
			}
			if (c >= 'a' && c <= 'g')
			{
				return c - 'a' + 10;
			}
			return -1;
		}

		private static string[] SplitAtCount(string value, int count)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < value.Length; i += count)
			{
				list.Add(value.Substring(i, Math.Min(count, value.Length - i)));
			}
			return list.ToArray();
		}

		private static void GetSpeedRatioValues(string data, out string[] values, out float[] speedRatios)
		{
			List<string> valueList = new List<string>();
			List<float> speedRatioList = new List<float>();
			string input = data.EndsWith(" ", StringComparison.Ordinal) ? data : data + " ";
			foreach (Match match in NotesSpeedRatioRegex.Matches(input))
			{
				if (match.Success)
				{
					valueList.Add(match.Groups["data"].Value);
					speedRatioList.Add(float.Parse(match.Groups["speedRatio"].Value, CultureInfo.InvariantCulture));
				}
			}
			values = valueList.ToArray();
			speedRatios = speedRatioList.ToArray();
		}

		private static void GetReadableSpeedRatioValues(string data, out string[] values, out float[] speedRatios)
		{
			List<string> valueList = new List<string>();
			List<float> speedRatioList = new List<float>();
			string input = data.EndsWith(" ", StringComparison.Ordinal) ? data : data + " ";
			foreach (Match match in ReadableSpeedRatioRegex.Matches(input))
			{
				if (match.Success)
				{
					valueList.Add(match.Groups["data"].Value);
					speedRatioList.Add(float.Parse(match.Groups["speedRatio"].Value, CultureInfo.InvariantCulture));
				}
			}
			values = valueList.ToArray();
			speedRatios = speedRatioList.ToArray();
		}

		private static float GetTimeSignatureForBar(int barIndex, List<TimeSignatureInfo> timeSignatureList)
		{
			TimeSignatureInfo timeSignatureInfo = timeSignatureList.FirstOrDefault();
			for (int i = 0; i < timeSignatureList.Count; i++)
			{
				if (timeSignatureList[i].StartBarIndex > barIndex)
				{
					break;
				}
				timeSignatureInfo = timeSignatureList[i];
			}
			return timeSignatureInfo != null ? timeSignatureInfo.TimeSignature : 4f;
		}

		private NoteDirection GetMirroredDirection(NoteDirection direction)
		{
			if (!isMirror)
			{
				return direction;
			}
			if (direction == NoteDirection.Right)
			{
				return NoteDirection.Left;
			}
			if (direction == NoteDirection.Left)
			{
				return NoteDirection.Right;
			}
			return NoteDirection.Default;
		}

		private class ScoreInfoUpdate
		{
			public int Bar { get; private set; }

			public float BarProgress { get; private set; }

			public float? Bpm { get; set; }

			public float? TimeSignature { get; set; }

			public float? SpeedRatio { get; set; }

			public float? SeVolume { get; set; }

			public ScoreInfoUpdate(int bar, float barProgress)
			{
				Bar = bar;
				BarProgress = barProgress;
			}

			public void Apply(ref float bpm, ref float timeSignature, ref float speedRatio, ref float seVolume)
			{
				if (Bpm.HasValue)
				{
					bpm = Bpm.Value;
				}
				if (TimeSignature.HasValue)
				{
					timeSignature = TimeSignature.Value;
				}
				if (SpeedRatio.HasValue)
				{
					speedRatio = SpeedRatio.Value;
				}
				if (SeVolume.HasValue)
				{
					seVolume = SeVolume.Value;
				}
			}
		}
	}
}
