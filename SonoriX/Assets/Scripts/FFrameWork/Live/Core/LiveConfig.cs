using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
	// ライブ全体の判定時間、表示位置、ノート速度などの共通設定
	public static class LiveConfig
	{
		// ライブ判定に使用するマスターパラメータ
		public struct MasterParamater
		{
			// ホールド入力をミス扱いにするまでの許容フレーム数
			public int MissMesh;

			// 固定レーン系の判定フレーム設定
			public Dictionary<JudgeFrameType, MasterFixedLaneJudgeFrame> fixedLaneJudgeFrames;
			
			// フリーレーン系の判定フレーム設定
			public Dictionary<JudgeFrameType, MasterFreeLaneJudgeFrame> freeLaneJudgeFrames;

			// 判定時刻より前に許容する最大時間
			public float JudgeTimeBefore;

			// 判定時刻より後に許容する最大時間
			public float JudgeTimeAfter;

			// 判定時間の代表値
			public float JudgeTime;
		}

		// 初期ノート速度
		public const float DefaultNoteSpeed = 6f;

		// コンティニュー直後にダメージを無効化する時間
		public const float ContinueNoDamageTime = 1f;

		// JustPerfect 判定の許容時間
		public static readonly float JustPerfectJudgeTime = 0.016667f;

		// JustPerfect 判定の片側許容時間
		public static readonly float JustPerfectJudgeTimeHalf = JustPerfectJudgeTime * 0.5f;

		// Perfect 判定の基準時間
		public static readonly float PerfectJudgeTime = 0.041667f;

		// Great 判定の基準時間
		public static readonly float GreatJudgeTime = 0.11667f;

		// Good 判定の基準時間
		public static readonly float GoodJudgeTime = 0.125f;

		// ライブ設定アセットの Resources パス
		public static readonly string ConfigBundleNamePath = "Live/Config/LiveBundleBuildData";

		// デフォルトクリアゲージ
		public static int DefaultMaxClearGauge = 100;

		// デフォルト判定時間
		public static float JudgeTime = 0.125f;

		// レーン数
		public const int LaneCount = 16;

		// 最大レーンインデックス
		public const int LaneMaxIndex = LaneCount - 1;

		// レーン間のワールド座標間隔
		public const float LanePositionScale = 22.5f;

		// 判定ラインの Y 座標
		public const float JudgmentPositionY = -380.0f;

		// ノート出現位置
		public static readonly Vector2 SpawnPosition = new Vector2(0f, 1500.0f);

		// 各レーンの判定位置
		public static readonly Vector2[] JudgmentPositions = CreateJudgmentPositions();

		// レーン幅として使用する X 方向スケール
		public static readonly float widthX = LanePositionScale;

		// 再利用用の Vector3.one
		public static readonly Vector3 Vector3One = Vector3.one;

		// 再利用用の Vector3.up
		public static readonly Vector3 Vector3Up = Vector3.up;

		// 再利用用の Vector3.zero
		public static readonly Vector3 Vector3Zero = Vector3.zero;

		// ノート速度から算出した表示オフセット時間のキャッシュ
		public static float CacheSpeedTime { get; set; }

		// ライブ中に使用する判定マスターデータ
		public static MasterParamater LiveMasterData = new MasterParamater
		{
			MissMesh = 2,
			fixedLaneJudgeFrames = createFixedLaneJudgeFrames(),
			//freeLaneJudgeFrames = createFixedLaneJudgeFrames(),
			JudgeTimeBefore = 0.2f,
			JudgeTimeAfter = 0.2f,
			JudgeTime = 0.2f
		};

		// ロングノート表示の透明度
		public static float LongNoteAlpha = 1f;

		// レーン数と表示スケールから判定位置配列を生成する
		private static Vector2[] CreateJudgmentPositions()
		{
			Vector2[] positions = new Vector2[LaneCount];
			for (int i = 0; i < positions.Length; i++)
			{
				float offset = i * 2f - LaneMaxIndex;
				positions[i] = new Vector2(SpawnPosition.x + offset * LanePositionScale, JudgmentPositionY);
			}
			return positions;
		}

		// ノート速度からノート表示開始までの時間オフセットを取得する
		// noteSpeed: 現在のノート速度
		public static float GetNoteDisplayOffsetTime(float noteSpeed)
		{
			if (CacheSpeedTime <= 0f)
			{
				float t = Mathf.Clamp01(Mathf.Max(0f, noteSpeed - 1f) / 11f);
				CacheSpeedTime = Mathf.Lerp(4f, 0.35f, 1f - Mathf.Pow(1f - t, 1.31f));
			}

			return CacheSpeedTime;
		}

		// ノート表示用の進行度を指数カーブに変換する
		// progress: 譜面時間基準の進行度
		public static float GetNoteViewProgress(float progress)
		{
			float value = Mathf.Pow(1.06f, (Mathf.Min(progress, 2f) - 1f) * 45f);
			if (value <= 0.04f || float.IsNaN(value))
			{
				return 0f;
			}
			if (float.IsInfinity(value))
			{
				return 2f;
			}
			return value;
		}

		// ロングノート線の親側進行度を線種に合わせて計算する
		// time: 現在の楽曲時間
		// startNote: ロングノートの開始ノート
		// endNote: ロングノートの終端ノート
		public static float GetNoteLineParentProgress(float time, INote startNote, INote endNote)
		{
			float endTime = endNote.MusicScoreInfo.time;
			float startTime = startNote.MusicScoreInfo.time;
			float duration = endTime - startTime;
			float t = Mathf.Approximately(duration, 0f) ? 0f : 1f - Mathf.Clamp01((endTime - time) / duration);

			if (startNote.LineType == NoteLineType.RightAngle)
			{
				return 0f;
			}
			if (startNote.LineType == NoteLineType.EaseOut)
			{
				return t * t;
			}
			if (startNote.LineType == NoteLineType.EaseIn)
			{
				return 1f - (1f - t) * (1f - t);
			}
			return t;
		}

		// ロングノート線の中心側進行度を線種に合わせて計算する
		// t: 0 から 1 の基準進行度
		// startNote: ロングノートの開始ノート
		public static float GetNoteLineCenterProgress(float t, INote startNote)
		{
			if (startNote.LineType == NoteLineType.RightAngle)
			{
				return 0f;
			}
			if (startNote.LineType == NoteLineType.EaseIn)
			{
				return 1f - (1f - t) * (1f - t);
			}
			if (startNote.LineType == NoteLineType.EaseOut)
			{
				return t * t;
			}
			return t;
		}

		// スライダーロング線の親側進行度を計算する
		// time: 現在の楽曲時間
		// startNote: ロングノートの開始ノート
		// endNote: ロングノートの終端ノート
		public static float GetSliderLongLineParentProgress(float time, INote startNote, INote endNote)
		{
			float endTime = endNote.MusicScoreInfo.time;
			float startTime = startNote.MusicScoreInfo.time;
			float duration = endTime - startTime;
			float t = Mathf.Approximately(duration, 0f) ? 0f : 1f - Mathf.Clamp01((endTime - time) / duration);
			return GetSliderLongLineCenterProgress(t, startNote);
		}

		// スライダーロング線の中心側進行度を線種に合わせて計算する
		// t: 0 から 1 の基準進行度
		// startNote: ロングノートの開始ノート
		public static float GetSliderLongLineCenterProgress(float t, INote startNote)
		{
			if (startNote.LineType == NoteLineType.RightAngle)
			{
				return 0f;
			}
			if (startNote.LineType == NoteLineType.EaseIn)
			{
				return 1f - (1f - t) * (1f - t) * (1f - t);
			}
			if (startNote.LineType == NoteLineType.EaseOut)
			{
				return t * t * t;
			}
			return t;
		}

		// ロングノート終端を判定できる時間内か確認する
		// offsetTime: 終端ノート時刻との差分
		public static bool IsLongEndJudgeTime(float offsetTime)
		{
			EnsureJudgeFrames();
			return -LiveMasterData.JudgeTimeAfter <= offsetTime && offsetTime <= LiveMasterData.JudgeTimeBefore;
		}

		// 指定した判定フレーム種別で Bad まで含めた判定時間内か確認する
		// judgeFrameType: 判定フレーム種別
		// offset: 判定時刻との差分
		public static bool IsJudgeTiming(JudgeFrameType judgeFrameType, float offset)
		{
			EnsureJudgeFrames();
			MasterFixedLaneJudgeFrame frame = LiveMasterData.fixedLaneJudgeFrames[judgeFrameType];
			return IsWithinJudgeWindow(offset, frame.BadBeforeJudgeTime, frame.BadAfterJudgeTime);
		}

		// 指定した判定フレーム種別で入力が遅すぎるか確認する
		// judgeFrameType: 判定フレーム種別
		// offset: 判定時刻との差分
		public static bool IsJudgeLate(JudgeFrameType judgeFrameType, float offset)
		{
			EnsureJudgeFrames();
			MasterFixedLaneJudgeFrame frame = LiveMasterData.fixedLaneJudgeFrames[judgeFrameType];
			return offset > frame.BadAfterJudgeTime;
		}

		// 方向入力が正しい場合だけ Perfect または Miss 用の判定時間内か確認する
		// judgeFrameType: 判定フレーム種別
		// offset: 判定時刻との差分
		// isDirection: 方向入力が正しいかどうか
		public static bool IsPerfectOrMissJudgeTiming(JudgeFrameType judgeFrameType, float offset, bool isDirection)
		{
			if (!isDirection)
			{
				return false;
			}

			EnsureJudgeFrames();
			MasterFixedLaneJudgeFrame frame = LiveMasterData.fixedLaneJudgeFrames[judgeFrameType];
			return IsWithinJudgeWindow(offset, frame.GreatBeforeJudgeTime, frame.GreatAfterJudgeTime);
		}

		// 通常ノートの判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		public static (NoteResult, NoteResultDescription) CalculateDefaultNoteResult(INote note, float time)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetJudgeResultInternal(offset, LiveUtility.GetINoteToJudgeFrameType(note), true);
		}

		// ロングノート開始位置の判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		public static (NoteResult, NoteResultDescription) CalculateLongStartNoteResult(INote note, float time)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetJudgeResultInternal(offset, LiveUtility.GetINoteToJudgeFrameType(note), true);
		}

		// ロングノート終端の判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		public static (NoteResult, NoteResultDescription) CalculateLongEndNoteResult(INote note, float time)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetJudgeResultInternal(offset, LiveUtility.GetINoteToJudgeFrameType(note), true);
		}

		// フリーレーンフリックの判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		// isDirection: フリック方向が正しいかどうか
		public static (NoteResult, NoteResultDescription) CalculateFlickNoteResult(INote note, float time, bool isDirection)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetPerfectOrMissJudgeResult(offset, JudgeFrameType.FreeLane_Flick, isDirection);
		}

		// スライダーフリックの判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		// isDirection: フリック方向が正しいかどうか
		public static (NoteResult, NoteResultDescription) CalculateSliderFlickNoteResult(INote note, float time, bool isDirection)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetPerfectOrMissJudgeResult(offset, LiveUtility.GetINoteToJudgeFrameType(note), isDirection);
		}

		// ボリュームフリックの判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		// isDirection: サイズ変化方向が正しいかどうか
		public static (NoteResult, NoteResultDescription) CalculateVolumeFlickNoteResult(INote note, float time, bool isDirection)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetPerfectOrMissJudgeResult(offset, JudgeFrameType.Volume_Flick, isDirection);
		}

		// ボリュームロング開始位置の判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		// isDirection: サイズ変化方向が正しいかどうか
		public static (NoteResult, NoteResultDescription) CalculateVolumeFlickLongNoteResult(INote note, float time, bool isDirection)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetPerfectOrMissJudgeResult(offset, LiveUtility.GetINoteToJudgeFrameType(note), isDirection);
		}

		// ロングノート終端フリックの判定結果を計算する
		// note: 判定対象のノート
		// time: 入力が発生した楽曲時間
		// isDirection: フリック方向が正しいかどうか
		public static (NoteResult, NoteResultDescription) CalculateLongEndFlickNoteResult(INote note, float time, bool isDirection)
		{
			float offset = note.MusicScoreInfo.time - time;
			return GetPerfectOrMissJudgeResult(offset, LiveUtility.GetINoteToJudgeFrameType(note), isDirection);
		}

		// プレビュー表示用の判定結果を取得する
		// viewTime: 判定時刻との差分として扱う表示時間
		public static (NoteResult, NoteResultDescription) GetPreviewJudgeResult(float viewTime)
		{
			return GetJudgeResultInternal(viewTime, JudgeFrameType.FreeLane_Normal, true);
		}

		// 判定フレーム設定を使って通常系ノートの判定結果を計算する
		// offset: ノート時刻から入力時刻を引いた差分
		// judgeFrameType: 判定フレーム種別
		// isDirection: 方向入力が正しいかどうか
		private static (NoteResult, NoteResultDescription) GetJudgeResultInternal(float offset, JudgeFrameType judgeFrameType, bool isDirection)
		{
			EnsureJudgeFrames();
			MasterFixedLaneJudgeFrame frame = LiveMasterData.fixedLaneJudgeFrames[judgeFrameType];
			if (isDirection && IsWithinJudgeWindow(offset, JustPerfectJudgeTimeHalf, JustPerfectJudgeTimeHalf))
			{
				return (NoteResult.JustPerfect, GetNoteResultDescription(offset));
			}
			if (IsWithinJudgeWindow(offset, frame.PerfectBeforeJudgeTime, frame.PerfectAfterJudgeTime))
			{
				if (!isDirection)
				{
					return (NoteResult.Great, NoteResultDescription.None);
				}
				return (NoteResult.Perfect, GetNoteResultDescription(offset));
			}
			if (IsWithinJudgeWindow(offset, frame.GreatBeforeJudgeTime, frame.GreatAfterJudgeTime))
			{
				return (NoteResult.Great, GetNoteResultDescription(offset));
			}
			if (IsWithinJudgeWindow(offset, frame.GoodBeforeJudgeTime, frame.GoodAfterJudgeTime))
			{
				return (NoteResult.Good, GetNoteResultDescription(offset));
			}
			if (IsWithinJudgeWindow(offset, frame.BadBeforeJudgeTime, frame.BadAfterJudgeTime))
			{
				return (NoteResult.Bad, GetNoteResultDescription(offset));
			}
			return (NoteResult.Miss, NoteResultDescription.None);
		}

		// 方向入力系ノート用に Perfect または None の判定結果を計算する
		// offset: ノート時刻から入力時刻を引いた差分
		// judgeFrameType: 判定フレーム種別
		// isDirection: 方向入力が正しいかどうか
		private static (NoteResult, NoteResultDescription) GetPerfectOrMissJudgeResult(float offset, JudgeFrameType judgeFrameType, bool isDirection)
		{
			EnsureJudgeFrames();
			MasterFixedLaneJudgeFrame frame = LiveMasterData.fixedLaneJudgeFrames[judgeFrameType];
			if (!isDirection)
			{
				return (NoteResult.None, NoteResultDescription.None);
			}
			if (IsWithinJudgeWindow(offset, frame.GreatBeforeJudgeTime, frame.GreatAfterJudgeTime))
			{
				return (NoteResult.Perfect, GetNoteResultDescription(offset));
			}
			return (NoteResult.None, NoteResultDescription.None);
		}

		// 判定時刻との差分から Fast、Late、Just を取得する
		// judgeOffsetTime: ノート時刻から入力時刻を引いた差分
		private static NoteResultDescription GetNoteResultDescription(float judgeOffsetTime)
		{
			if (-JustPerfectJudgeTimeHalf <= judgeOffsetTime && JustPerfectJudgeTimeHalf >= judgeOffsetTime)
			{
				return NoteResultDescription.Just;
			}
			return judgeOffsetTime > JustPerfectJudgeTimeHalf ? NoteResultDescription.Fast : NoteResultDescription.Late;
		}

		// 指定された前後幅の判定時間内か確認する
		// offset: 判定時刻との差分
		// beforeTime: 早い方向の許容時間
		// afterTime: 遅い方向の許容時間
		private static bool IsWithinJudgeWindow(float offset, float beforeTime, float afterTime)
		{
			return -afterTime <= offset && offset <= beforeTime;
		}

		// 判定フレーム辞書を初期化し、全判定種別の設定を揃える
		private static void EnsureJudgeFrames()
		{
			if (LiveMasterData.fixedLaneJudgeFrames == null || LiveMasterData.fixedLaneJudgeFrames.Count == 0)
			{
				LiveMasterData.fixedLaneJudgeFrames = createFixedLaneJudgeFrames();
			}

			foreach (JudgeFrameType judgeFrameType in Enum.GetValues(typeof(JudgeFrameType)))
			{
				if (judgeFrameType == JudgeFrameType.Undefined)
				{
					continue;
				}
				if (!LiveMasterData.fixedLaneJudgeFrames.ContainsKey(judgeFrameType))
				{
					LiveMasterData.fixedLaneJudgeFrames[judgeFrameType] = CloneDefaultJudgeFrame(judgeFrameType);
				}
			}

			float judgeTimeBefore = 0f;
			float judgeTimeAfter = 0f;
			foreach (MasterFixedLaneJudgeFrame judgeFrame in LiveMasterData.fixedLaneJudgeFrames.Values)
			{
				if (judgeFrame == null)
				{
					continue;
				}
				judgeTimeBefore = Mathf.Max(judgeTimeBefore, judgeFrame.BadBeforeJudgeTime);
				judgeTimeAfter = Mathf.Max(judgeTimeAfter, judgeFrame.BadAfterJudgeTime);
			}

			LiveMasterData.JudgeTimeBefore = judgeTimeBefore;
			LiveMasterData.JudgeTimeAfter = judgeTimeAfter;
			LiveMasterData.JudgeTime = Mathf.Max(judgeTimeBefore, judgeTimeAfter);
		}

		// 固定レーン判定フレームの初期値を生成する
		private static Dictionary<JudgeFrameType, MasterFixedLaneJudgeFrame> createFixedLaneJudgeFrames()
		{
			Dictionary<JudgeFrameType, MasterFixedLaneJudgeFrame> frames = new Dictionary<JudgeFrameType, MasterFixedLaneJudgeFrame>();
			AddJudgeFrame(frames, JudgeFrameType.FreeLane_Normal, "normal", 2.5f, 2.5f, 6.5f, 6.5f, 9f, 9f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FreeLane_Flick, "flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.FreeLane_Long, "long", 2.5f, 2.5f, 6.5f, 6.5f, 9f, 9f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FreeLane_Long_End, "long_end", 3.5f, 4f, 7.5f, 9f, 9.5f, 10.5f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FreeLane_Long_End_SliderFlick, "free_lane_long_end_slider_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.FixedLane_Normal, "fixed_normal", 2.5f, 2.5f, 6.5f, 6.5f, 9f, 9f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FixedLane_Long, "fixed_long", 2.5f, 2.5f, 6.5f, 6.5f, 9f, 9f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FixedLane_Long_End, "fixed_long_end", 3.5f, 4f, 7.5f, 9f, 9.5f, 10.5f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.FixedLane_Long_End_SliderFlick, "fixed_long_end_slider_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Slider_Flick, "slider_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Slider_Long, "slider_long", 2.5f, 2.5f, 6.5f, 6.5f, 9f, 9f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.Slider_Long_End, "slider_long_end", 3.5f, 4f, 7.5f, 9f, 9.5f, 10.5f, 12f, 12f);
			AddJudgeFrame(frames, JudgeFrameType.Slider_Long_End_SliderFlick, "slider_long_end_slider_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Volume_Flick, "volume_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Volume_Long, "volume_long", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Volume_Long_End, "volume_long_end", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			AddJudgeFrame(frames, JudgeFrameType.Volume_Long_End_SliderFlick, "volume_long_end_slider_flick", 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f, 6.5f);
			return frames;
		}

		// 判定フレーム設定を辞書へ追加する
		// frames: 追加先の判定フレーム辞書
		// type: 判定フレーム種別
		// name: 判定フレーム名
		// perfectBefore: Perfect の早い方向許容時間
		// perfectAfter: Perfect の遅い方向許容時間
		// greatBefore: Great の早い方向許容時間
		// greatAfter: Great の遅い方向許容時間
		// goodBefore: Good の早い方向許容時間
		// goodAfter: Good の遅い方向許容時間
		// badBefore: Bad の早い方向許容時間
		// badAfter: Bad の遅い方向許容時間
		private static void AddJudgeFrame(
			Dictionary<JudgeFrameType, MasterFixedLaneJudgeFrame> frames,
			JudgeFrameType type,
			string name,
			float perfectBefore,
			float perfectAfter,
			float greatBefore,
			float greatAfter,
			float goodBefore,
			float goodAfter,
			float badBefore,
			float badAfter)
		{
			frames[type] = new MasterFixedLaneJudgeFrame(type, name, perfectBefore, perfectAfter, greatBefore, greatAfter, goodBefore, goodAfter, badBefore, badAfter);
		}

		// 未定義の判定種別に通常ノート相当の判定フレームを複製する
		// type: 複製先の判定フレーム種別
		private static MasterFixedLaneJudgeFrame CloneDefaultJudgeFrame(JudgeFrameType type)
		{
			return CloneJudgeFrame(type, type.ToString().ToLowerInvariant(), createFixedLaneJudgeFrames()[JudgeFrameType.FreeLane_Normal]);
		}

		// 既存の判定フレーム設定を別の種別・名前で複製する
		// type: 複製先の判定フレーム種別
		// name: 複製先の判定フレーム名
		// source: 複製元の判定フレーム設定
		private static MasterFixedLaneJudgeFrame CloneJudgeFrame(JudgeFrameType type, string name, MasterFixedLaneJudgeFrame source)
		{
			return new MasterFixedLaneJudgeFrame(
				type,
				name,
				source.perfectBefore,
				source.perfectAfter,
				source.greatBefore,
				source.greatAfter,
				source.goodBefore,
				source.goodAfter,
				source.badBefore,
				source.badAfter);
		}
	}
}
