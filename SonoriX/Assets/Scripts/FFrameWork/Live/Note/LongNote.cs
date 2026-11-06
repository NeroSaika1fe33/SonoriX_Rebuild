using System;
using System.Linq;
using UnityEngine;

namespace FFrameWork.Live
{
	// ロングノートの開始、継続、終端、子ノート実行を管理する基底クラス
	public class LongNote : NoteBase
	{
		// ロングノート線の描画タイプ
		private readonly NoteLineType lineType;

		// 最後にホールド入力を受け取ったフレーム番号
		protected int lastInputFrame;

		// ホールド判定が有効な状態かどうか
		private bool isHoldJudgmentActive;

		// ホールド中の判定レーンに追加する補正幅
		public float LaneOffset { get; set; }

		// 補正込みの右端判定レーン
		public float JudgedLaneEnd
		{
			get { return LaneEndF + LaneOffset; }
		}

		// 補正込みの左端判定レーン
		public float JudgedLaneStart
		{
			get { return LaneStartF - LaneOffset; }
		}

		// 現在の状態がホールド入力中かどうか
		public bool IsHoldInputActive
		{
			get { return IsHoldInputState(State); }
		}

		// ホールド判定が有効かどうか
		public bool IsHoldJudgmentActive
		{
			get { return isHoldJudgmentActive; }
		}

		// ホールド結果が成功扱いかどうか
		public bool IsHoldResultSuccessful
		{
			get { return isHoldJudgmentActive; }
		}

		// ロングノート線の描画タイプ
		public override NoteLineType LineType
		{
			get { return lineType; }
		}

		// ロングノートの状態入力開始時に始点判定の各更新処理を呼ぶ
		public override NoteState State
		{
			get { return base.State; }
			protected set
			{
				NoteState current = State;
				if ((current == NoteState.Playing && value == NoteState.InputBegan) ||
					(current == NoteState.Last && value == NoteState.InputBegan))
				{
					onJudgment?.Invoke(this);
					onUpdateCombo?.Invoke(this);
					onUpdateScore?.Invoke(this);
					onDamage?.Invoke(this);
				}

				base.State = value;
			}
		}

		// スコア更新コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnUpdateScore
		{
			set
			{
				base.OnUpdateScore = value;
				ForEachSubNote(note => note.OnUpdateScore = value);
			}
		}

		// コンボ更新コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnUpdateCombo
		{
			set
			{
				base.OnUpdateCombo = value;
				ForEachSubNote(note => note.OnUpdateCombo = value);
			}
		}

		// ダメージ処理コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnDamage
		{
			set
			{
				base.OnDamage = value;
				ForEachSubNote(note => note.OnDamage = value);
			}
		}

		// 出現コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnSpawn
		{
			set
			{
				base.OnSpawn = value;
				ForEachSubNote(note => note.OnSpawn = value);
			}
		}

		// 判定コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnJudgment
		{
			set
			{
				base.OnJudgment = value;
				ForEachSubNote(note => note.OnJudgment = value);
			}
		}

		// 表示解除コールバックを子ノートにも伝搬する
		public override Action<NoteBase> OnUnspawn
		{
			set
			{
				base.OnUnspawn = value;
				ForEachSubNote(note => note.OnUnspawn = value);
			}
		}

		// 判定結果補正コールバックを子ノートにも伝搬する
		public override Func<(NoteResult, NoteResultDescription), (NoteResult, NoteResultDescription)> OnUpdateResult
		{
			set
			{
				base.OnUpdateResult = value;
				ForEachSubNote(note => note.OnUpdateResult = value);
			}
		}

		// ロングノートを生成する
		// info: このノートの譜面情報
		// id: 譜面内のノート ID
		// laneStart: ノート左端のレーン位置
		// laneEnd: ノート右端のレーン位置
		// category: ノート種別
		// bundleBuildData: レーン補正などの構築データ
		// speedRatio: ノート速度倍率
		// lineType: ロングノート線の描画タイプ
		public LongNote(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio, NoteLineType lineType = NoteLineType.Linear)
			: base(info, id, laneStart, laneEnd, category, bundleBuildData, speedRatio)
		{
			this.lineType = lineType;
			LaneOffset = LiveUtility.GetLaneOffset(category, bundleBuildData);
		}

		// ホールド中に発生するコンボ用子ノートを追加する
		// longHoldCombo: 追加するホールドコンボノート
		public void AddHoldCombo(LongHoldCombo longHoldCombo)
		{
			longHoldCombo.speedRatio = speedRatio;
			NoteList.Add(longHoldCombo);
			longHoldCombo.SetParentNote(this);
		}

		// ロングノート線の途中点として扱う中継ノートを追加する
		// relayNote: 追加する中継ノート
		public void AddRelayNote(NoteBase relayNote)
		{
			if (relayNote == null)
			{
				return;
			}

			relayNote.speedRatio = speedRatio;
			NoteList.Add(relayNote);
			ViewNoteList.Add(relayNote);
			relayNote.SetParentNote(this);
		}

		// ロングノートの終端ノートを設定し、表示用リストにも追加する
		// note: 終端として扱う子ノート
		public override void SetChildNote(NoteBase note)
		{
			note.speedRatio = speedRatio;
			NoteList = NoteList.OrderBy(x => x.MusicScoreInfo.time).ToList();
			ViewNoteList = ViewNoteList.OrderBy(x => x.MusicScoreInfo.time).ToList();
			base.SetChildNote(note);
			NoteList.Add(note);
			ViewNoteList.Add(note);
		}

		// ロングノート本体、子ノート、実行中レーンを更新する
		// currentFrameInfo: 現在フレームの楽曲情報
		// offsetTime: 表示・判定に使用する時間オフセット
		public override void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			this.offsetTime = offsetTime;
			if (State == NoteState.Wait)
			{
				State = NoteState.Playing;
			}
			if (State == NoteState.Last)
			{
				JudgeInfo = (NoteResult.Miss, NoteResultDescription.None);
				State = NoteState.InputRelease;
				onDamage?.Invoke(this);
				onUpdateCombo?.Invoke(this);
				onJudgment?.Invoke(this);
			}

			OffsetJudgeTime = currentFrameInfo.time - MusicScoreInfo.time;
			Progress = CalcProgress(currentFrameInfo, offsetTime);
			if (IsHoldInputTimedOut())
			{
				DeactivateHold();
			}

			ExecuteSubNotes(currentFrameInfo);
			UpdateExecutingLane(currentFrameInfo);

			if (State == NoteState.Playing && LiveConfig.IsJudgeLate(LiveUtility.GetINoteToJudgeFrameType(this), OffsetJudgeTime))
			{
				State = NoteState.Last;
				return;
			}
		}

		// 子ノートの成功状態を見てロングノートを強制終了する
		public virtual void ForceTerminate()
		{
			if (State == NoteState.Done)
			{
				return;
			}

			JudgeInfo = IsChildSuccessful()
				? (NoteResult.JustPerfect, NoteResultDescription.None)
				: (NoteResult.Miss, NoteResultDescription.None);
			State = NoteState.Done;
		}

		// ロングノート終了時に内部子ノートを終了し、表示解除を通知する
		protected override void OnUnSpawnNote()
		{
			SubNoteTerminate();
			onUnspawn?.Invoke(this);
		}

		// 入力を使ってロングノートの開始、継続、終端判定を進める
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool Judgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Done)
			{
				return false;
			}

			if (IsEndedOrCanceled(liveInput.phase))
			{
				if (TryJudgeChildEnd(ref liveInput, lane))
				{
					State = NoteState.Done;
					return true;
				}

				if (IsHoldInputActive)
				{
					DeactivateHold();
				}

				return false;
			}
		
			if (State == NoteState.Playing || State == NoteState.Last)
			{
				if (Result == NoteResult.None)
				{
					(NoteResult, NoteResultDescription) result = LiveConfig.CalculateLongStartNoteResult(this, liveInput.musicTime);
					JudgeInfo = onUpdateResult != null ? onUpdateResult(result) : result;
				}
				State = NoteState.InputBegan;
			}
			else
			{
				State = State == NoteState.InputRelease ? NoteState.InputBegan : NoteState.Input;
			}

			ActivateHold();
		
			lastInputFrame = Time.frameCount;
			OffsetJudgeTime = 0f;
			SubNoteJudgment(ref liveInput, lane);
			if (TryJudgeChildEnd(ref liveInput, lane))
			{
				State = NoteState.Done;
			}
			return true;
		}
		
		// ホールド中に中間子ノートの判定を実行する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		protected void SubNoteJudgment(ref LiveInput liveInput, float lane)
		{
			if (!CanJudgeSubNotes())
			{
				return;
			}
		
			for (int i = 1; i < NoteList.Count - 1; i++)
			{
				NoteList[i].Judgment(ref liveInput, lane);
			}
		}

		// 中間子ノートを強制終了する
		protected void SubNoteTerminate()
		{
			for (int i = 1; i < NoteList.Count - 1; i++)
			{
				NoteList[i].Terminate();
			}
		}

		// 入力レーンと、現在判定対象になっているロングノート端点との距離を計算する
		// currentFrameInfo: 現在フレームの楽曲情報
		// inputLane: 入力されたレーン位置
		public override float LaneDistance(ref MusicScoreInfo currentFrameInfo, float inputLane)
		{
			if (childNote != null && LiveConfig.IsLongEndJudgeTime(childNote.MusicScoreInfo.time - currentFrameInfo.time))
			{
				return childNote.LaneDistance(ref currentFrameInfo, inputLane);
			}

			return base.LaneDistance(ref currentFrameInfo, inputLane);
		}

		// 入力がロングノートの判定条件を満たすか確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public override bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			if (State == NoteState.Done)
			{
				return false;
			}
		
			float laneStart = Result != NoteResult.None || Description != NoteResultDescription.None ? JudgedLaneStart : JudgeLaneStart;
			float laneEnd = Result != NoteResult.None || Description != NoteResultDescription.None ? JudgedLaneEnd : JudgeLaneEnd;
			if (laneStart > lane || laneEnd < lane)
			{
				return false;
			}

			if (IsHoldInputActive || State == NoteState.InputRelease)
			{
				if (IsEndedOrCanceled(liveInput.phase))
				{
					return IsChildEndJudgment(ref liveInput, lane);
				}

				return IsActiveHoldInputPhase(liveInput.phase);
			}
		
			return LiveUtility.IsJudgmentTiming(LiveUtility.GetINoteToJudgeFrameType(this), OffsetJudgeTime) &&
				(State != NoteState.Playing && State != NoteState.Last || CanStartLong(ref liveInput));
		}

		// ロングノートを開始できる入力フェーズか確認する
		// liveInput: 判定に使用する入力情報
		private static bool CanStartLong(ref LiveInput liveInput)
		{
			if (liveInput.phase == LiveInputPhase.Began)
			{
				return true;
			}

			return liveInput.phase == LiveInputPhase.Moved &&
				liveInput.source == LiveInputSource.Arrow;
		}

		// ホールド継続中として扱う入力フェーズか確認する
		// phase: 確認対象の入力フェーズ
		private static bool IsActiveHoldInputPhase(LiveInputPhase phase)
		{
			return phase == LiveInputPhase.Began ||
				phase == LiveInputPhase.Moved;
		}

		// 指定状態がホールド入力中の状態か確認する
		// state: 確認対象のノート状態
		protected static bool IsHoldInputState(NoteState state)
		{
			return state == NoteState.InputBegan || state == NoteState.Input;
		}

		// 入力フェーズが終了またはキャンセルか確認する
		// phase: 確認対象の入力フェーズ
		private static bool IsEndedOrCanceled(LiveInputPhase phase)
		{
			return phase == LiveInputPhase.Ended ||
				phase == LiveInputPhase.Canceled;
		}

		// 子の終端ノートを判定できる入力か確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		private bool IsChildEndJudgment(ref LiveInput liveInput, float lane)
		{
			return childNote != null &&
				isHoldJudgmentActive &&
				childNote.HasJudgment &&
				LiveConfig.IsLongEndJudgeTime(childNote.OffsetJudgeTime) &&
				childNote.IsJudgment(ref liveInput, lane);
		}

		// 子の終端ノートの判定を試行する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		private bool TryJudgeChildEnd(ref LiveInput liveInput, float lane)
		{
			return IsChildEndJudgment(ref liveInput, lane) &&
				childNote.Judgment(ref liveInput, lane);
		}

		// ホールド判定を有効化する
		protected void ActivateHold()
		{
			isHoldJudgmentActive = true;
		}

		// ホールド判定を無効化し、入力中ならリリース状態へ移行する
		protected void DeactivateHold()
		{
			isHoldJudgmentActive = false;
			if (State != NoteState.InputBegan && State != NoteState.Input)
			{
				return;
			}

			State = NoteState.InputRelease;
		}

		// ホールド入力が一定フレーム途切れているか確認する
		protected bool IsHoldInputTimedOut()
		{
			return IsHoldInputActive && lastInputFrame + LiveConfig.LiveMasterData.MissMesh < Time.frameCount;
		}

		// 現在の状態で中間子ノートを判定できるか確認する
		private bool CanJudgeSubNotes()
		{
			return IsHoldJudgmentActive &&
				(State == NoteState.InputBegan || State == NoteState.Input || State == NoteState.InputRelease);
		}

		// 中間子ノートのどれかが判定可能か確認する
		// liveInput: 判定に使用する入力情報
		// lane: 入力されたレーン位置
		public virtual bool IsJudgmentChild(ref LiveInput liveInput, float lane)
		{
			for (int i = 1; i < NoteList.Count - 1; i++)
			{
				if (NoteList[i].IsJudgment(ref liveInput, lane))
				{
					return true;
				}
			}
		
			return false;
		}

		// オートプレイ用にロングノートと子ノートを自動判定する
		// currentFrameInfo: 現在フレームの楽曲情報
		public override bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State == NoteState.Done || MusicScoreInfo.time > currentFrameInfo.time)
			{
				return false;
			}

			lastInputFrame = Time.frameCount;
			JudgeInfo = (NoteResult.Perfect, NoteResultDescription.None);
			State = State == NoteState.Playing || State == NoteState.Last || State == NoteState.InputRelease
				? NoteState.InputBegan
				: NoteState.Input;
			ActivateHold();
			OffsetJudgeTime = 0f;
			SubNoteAutoJudgment(currentFrameInfo);
			if (childNote != null && childNote.AutoJudgment(currentFrameInfo))
			{
				DeactivateHold();
				State = NoteState.Done;
			}
			return true;
		}

		// オートプレイ中に中間子ノートを自動判定する
		// currentFrameInfo: 現在フレームの楽曲情報
		private void SubNoteAutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State != NoteState.InputBegan && State != NoteState.Input)
			{
				return;
			}

			for (int i = 1; i < NoteList.Count - 1; i++)
			{
				NoteList[i].AutoJudgment(currentFrameInfo);
			}
		}

		// 時刻に到達した子ノートを順番に実行する
		// currentFrameInfo: 現在フレームの楽曲情報
		protected void ExecuteSubNotes(MusicScoreInfo currentFrameInfo)
		{
			for (int i = 1; i < NoteList.Count; i++)
			{
				NoteBase note = NoteList[i];
				float timeOffset = CalcTimeOffset != null ? CalcTimeOffset(note) : offsetTime;
				if (currentFrameInfo.time <= note.MusicScoreInfo.time - timeOffset)
				{
					break;
				}
				note.Excute(currentFrameInfo, timeOffset);
			}
		}

		// ロングノート線上で現在実行中のレーン位置を更新する
		// currentFrameInfo: 現在フレームの楽曲情報
		protected void UpdateExecutingLane(MusicScoreInfo currentFrameInfo)
		{
			if (childNote == null)
			{
				return;
			}

			INote currentNote = this;
			INote nextNote = childNote;
			for (int i = 1; i < ViewNoteList.Count - 1; i++)
			{
				INote note = ViewNoteList[i];
				if (note.State == NoteState.Done)
				{
					currentNote = note;
					continue;
				}

				nextNote = note;
				break;
			}

			MusicScoreInfo startInfo = MusicScoreInfo;
			LiveUtility.CalcExcuteNoteLane(this, ref currentNote, ref nextNote, ref currentFrameInfo, startInfo, childNote);
		}

		// 終端ノートが成功扱いか確認する
		protected bool IsChildSuccessful()
		{
			INote child = ChildNote;
			return child != null &&
				(child.Result > NoteResult.Pass || child.State == NoteState.Input);
		}

		// 指定された楽曲時間でロングノート始点の判定結果を計算する
		// musicTime: 判定に使用する楽曲時間
		public override NoteResult CalcNoteResult(float musicTime)
		{
			return LiveConfig.CalculateLongStartNoteResult(this, musicTime).Item1;
		}

		// 保持している子ノートへ処理を適用する
		// action: 子ノートに適用する処理
		private void ForEachSubNote(Action<NoteBase> action)
		{
			if (NoteList == null)
			{
				return;
			}
			for (int i = 1; i < NoteList.Count; i++)
			{
				action(NoteList[i]);
			}
		}

		// 矢印フレームが現在のノートレーンと重なっているか確認する
		// frame: 矢印の現在位置とサイズを持つ判定フレーム
		protected bool IsArrowFrameOverlappingCurrentLane(ArrowJudgmentFrame frame)
		{
			float noteCenterX = GetLaneWorldX((LaneStartF + LaneEndF) * 0.5f);
			float noteHalfWidth = GetNoteHalfWidth();
			float noteLeft = noteCenterX - noteHalfWidth;
			float noteRight = noteCenterX + noteHalfWidth;
			float frameLeft = frame.Position.CurrentX - frame.Size.CurrentSize;
			float frameRight = frame.Position.CurrentX + frame.Size.CurrentSize;
			return frameLeft <= noteRight && frameRight >= noteLeft;
		}

		// 現在のノート幅をワールド座標上の半幅として取得する
		private float GetNoteHalfWidth()
		{
			float laneSpan = Mathf.Abs(LaneEndF - LaneStartF);
			return (laneSpan + 1f) * LiveConfig.widthX;
		}

		// レーン番号を判定位置のワールド X 座標へ変換する
		// lane: 変換対象のレーン位置
		private static float GetLaneWorldX(float lane)
		{
			Vector2[] positions = LiveConfig.JudgmentPositions;
			if (positions == null || positions.Length == 0)
			{
				return 0f;
			}

			float clampedLane = Mathf.Clamp(lane, 0f, positions.Length - 1);
			int start = Mathf.FloorToInt(clampedLane);
			int end = Mathf.CeilToInt(clampedLane);
			float t = Mathf.Clamp01(clampedLane - start);
			return Mathf.Lerp(positions[start].x, positions[end].x, t);
		}
	}
}
