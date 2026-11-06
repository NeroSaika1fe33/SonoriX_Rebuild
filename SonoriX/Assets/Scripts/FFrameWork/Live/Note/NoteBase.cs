using System;
using System.Collections.Generic;

namespace FFrameWork.Live
{
	// ノート共通の状態、判定情報、表示クリアゲージサイクルを管理する基底クラス
	public class NoteBase : INote
	{
		// ロングノートなどで使用する親ノート
		protected LongNote parentNote;

		// ロングノートなどで使用する子ノート、または終端ノート
		protected NoteBase childNote;

		// 判定完了時にスコアを更新するためのコールバック
		protected Action<NoteBase> onUpdateScore;

		// 判定完了時にコンボを更新するためのコールバック
		protected Action<NoteBase> onUpdateCombo;

		// ミスなどでダメージ処理を行うためのコールバック
		protected Action<NoteBase> onDamage;

		// ノートが出現状態になったときに呼ばれるコールバック
		protected Action<NoteBase> onSpawn;

		// ノートの判定結果が確定したときに呼ばれるコールバック
		protected Action<NoteBase> onJudgment;

		// ノートが終了して表示対象から外れるときに呼ばれるコールバック
		protected Action<NoteBase> onUnspawn;

		// 判定結果を外部ルールで補正するためのコールバック
		protected Func<(NoteResult, NoteResultDescription), (NoteResult, NoteResultDescription)> onUpdateResult;

		// 子ノートごとの表示開始時間または実行オフセットを計算する関数
		public Func<NoteBase, float> CalcTimeOffset;

		// 現在のノート状態
		protected NoteState state;

		// 判定完了時にスコア更新処理として呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnUpdateScore
		{
			set { onUpdateScore = value; }
		}

		// 判定完了時にコンボ更新処理として呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnUpdateCombo
		{
			set { onUpdateCombo = value; }
		}

		// ミスなどの失敗判定時にダメージ処理として呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnDamage
		{
			set { onDamage = value; }
		}

		// 判定結果が確定したときに呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnJudgment
		{
			set { onJudgment = value; }
		}

		// 算出済みの判定結果を外部ルールで補正する関数を設定する
		public virtual Func<(NoteResult, NoteResultDescription), (NoteResult, NoteResultDescription)> OnUpdateResult
		{
			set { onUpdateResult = value; }
		}

		// ノートが出現状態になったときに呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnSpawn
		{
			set { onSpawn = value; }
		}

		// ノートが終了して表示対象から外れるときに呼び出すコールバックを設定する
		public virtual Action<NoteBase> OnUnspawn
		{
			set { onUnspawn = value; }
		}

		// 譜面上で定義されたノート左端の初期レーン位置
		public int DefaultLeftLane { get; private set; }

		// 譜面上で定義されたノート右端の初期レーン位置
		public int DefaultRightLane { get; private set; }

		// 実行中に更新されるノート左端のレーン位置
		public virtual float LaneStartF { get; set; }

		// 実行中に更新されるノート右端のレーン位置
		public virtual float LaneEndF { get; set; }

		
		// 入力判定に使用する左端のレーン範囲通常はレーン補正を含む
		public virtual float JudgeLaneStart { get; set; }

		// 入力判定に使用する右端のレーン範囲通常はレーン補正を含む
		public virtual float JudgeLaneEnd { get; set; }

		// 最初の入力アクションだけで処理を完了できるかどうか
		public virtual bool FinishFirstAction
		{
			get { return true; }
		}

		// このノートが譜面上で持つ時間や位置などの情報
		public MusicScoreInfo MusicScoreInfo { get; protected set; }

		// 現在の楽曲時間とノート判定時間の差負の値は早い入力、正の値は遅い入力を表す
		public float OffsetJudgeTime { get; set; }

		// ノート出現から判定時刻までの進行度
		public float Progress { get; protected set; }

		// ノートのクリアゲージサイクル状態Playing と Done への遷移時に表示生成・破棄処理を呼ぶ
		public virtual NoteState State
		{
			get { return state; }
			protected set
			{
				state = value;
				if (value == NoteState.Done)
				{
					OnUnSpawnNote();
				}
				else if (value == NoteState.Playing)
				{
					OnSpawnNote();
				}
			}
		}

		// 判定結果と、その詳細情報
		public (NoteResult, NoteResultDescription) JudgeInfo { get; protected set; }

		// 判定結果のみを取得するショートカット
		public NoteResult Result
		{
			get { return JudgeInfo.Item1; }
		}

		// 判定結果の詳細のみを取得するショートカット
		public NoteResultDescription Description
		{
			get { return JudgeInfo.Item2; }
		}

		// ノートの種類
		public virtual NoteCategory Category { get; protected set; }

		// フリックなど方向を持つノートの方向
		public NoteDirection Direction { get; protected set; }

		// ロングノートのライン描画タイプ
		public virtual NoteLineType LineType
		{
			get { return NoteLineType.Linear; }
		}

		// このノートに紐づく親ノート
		public INote ParentNote
		{
			get { return parentNote; }
		}

		// このノートに紐づく子ノート
		public INote ChildNote
		{
			get { return childNote; }
		}

		// このノートが入力判定の対象かどうか
		public virtual bool HasJudgment
		{
			get { return true; }
		}

		// 判定時に効果音を再生するかどうか
		public virtual bool PlayJudgmentSound
		{
			get { return true; }
		}

		// フィーバー状態のノートかどうか
		public virtual bool IsFever { get; protected set; }

		// 譜面内でノートを識別するための ID
		public virtual int Id { get; protected set; }

		// ノート速度の倍率
		public float speedRatio { get; set; }

		// 現在の実行で使用している表示・判定時間のオフセット
		public float offsetTime { get; protected set; }

		// ロングノートなどで管理する論理ノートの一覧
		public List<NoteBase> NoteList { get; protected set; }

		// 表示側が参照するノートの一覧
		public List<INote> ViewNoteList { get; set; }

		// 譜面情報とレーン情報からノートの初期状態を生成する
		// info: このノートが持つ譜面上の時間や位置などの情報
		// id: 譜面内でノートを識別するための ID
		// laneStart: 譜面上で定義された左端のレーン位置
		// laneEnd: 譜面上で定義された右端のレーン位置
		// category: ノートの種類
		// bundleBuildData: レーン補正など、ライブ用の構築データ
		// speedRatio: ノート速度の倍率
		public NoteBase(MusicScoreInfo info, int id, int laneStart, int laneEnd, NoteCategory category, LiveBundleBuildData bundleBuildData, float speedRatio)
		{
			MusicScoreInfo = info;
			Id = id;
			DefaultLeftLane = laneStart;
			DefaultRightLane = laneEnd;
			LaneStartF = laneStart;
			LaneEndF = laneEnd;
			float laneOffset = LiveUtility.GetLaneOffset(category, bundleBuildData);
			JudgeLaneStart = laneStart - laneOffset;
			JudgeLaneEnd = laneEnd + laneOffset;
			State = NoteState.Wait;
			Category = category;
			Direction = NoteDirection.Default;
			this.speedRatio = speedRatio;
			NoteList = new List<NoteBase> { this };
			ViewNoteList = new List<INote> { this };
			JudgeInfo = (NoteResult.None, NoteResultDescription.None);
		}

		// このノートに親ロングノートを設定する
		// note: 親として扱うロングノート
		public virtual void SetParentNote(LongNote note)
		{
			parentNote = note;
		}

		// このノートに子ノート、またはロングノートの終端ノートを設定する
		// note: 子として扱うノート
		public virtual void SetChildNote(NoteBase note)
		{
			childNote = note;
		}

		// このノートがフィーバー状態かどうかを設定する
		// fever: フィーバー状態にする場合は true
		public virtual void SetFever(bool fever)
		{
			IsFever = fever;
		}

		// 現在フレームの楽曲情報をもとに、ノートの状態と進行度を更新する
		// currentFrameInfo: 現在フレームの楽曲時間などの情報
		// offsetTime: ノートを判定時刻より前から表示・実行するための時間オフセット
		public virtual void Excute(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			this.offsetTime = offsetTime;
			if (State == NoteState.Done)
			{
				return;
			}
			if (State >= NoteState.Last)
			{
				JudgeInfo = (NoteResult.Miss, NoteResultDescription.None);
				State = NoteState.Done;
				return;
			}

			OffsetJudgeTime = currentFrameInfo.time - MusicScoreInfo.time;
			float progress = CalcProgress(currentFrameInfo, offsetTime);
			if (progress < 0f && state != NoteState.Playing)
			{
				return;
			}

			if (State == NoteState.Wait)
			{
				State = NoteState.Playing;
			}
			Progress = progress;
			if (OffsetJudgeTime > LiveConfig.LiveMasterData.JudgeTimeAfter)
			{
				State = NoteState.Last;
			}
		}

		// 現在の楽曲時間から、ノート出現から判定時刻までの進行度を計算する
		// currentFrameInfo: 現在フレームの楽曲時間などの情報
		// offsetTime: ノートを判定時刻より前から表示・実行するための時間オフセット
		// 戻り値: 判定時刻に到達したとき 1 になる進行度
		protected float CalcProgress(MusicScoreInfo currentFrameInfo, float offsetTime)
		{
			if (offsetTime.Equals(0f))
			{
				return 1f;
			}
			return (currentFrameInfo.time + offsetTime - MusicScoreInfo.time) / offsetTime;
		}

		// ノートを強制終了するときの追加処理を行う基底クラスでは何もしない
		public virtual void Terminate()
		{
		}

		// 入力内容とレーン位置をもとに、このノートの判定を実行する
		// input: 判定に使用するライブ入力情報
		// lane: 入力されたレーン位置
		// 戻り値: 判定処理を実行した場合は true
		public virtual bool Judgment(ref LiveInput input, float lane)
		{
			return false;
		}

		// ノートが出現状態になったときの処理を呼び出す
		protected virtual void OnSpawnNote()
		{
			onSpawn?.Invoke(this);
		}

		// ノートが終了したときに、判定、コンボ、スコア、ダメージ、表示解除の各処理を呼び出す
		protected virtual void OnUnSpawnNote()
		{
			onJudgment?.Invoke(this);
			onUpdateCombo?.Invoke(this);
			onUpdateScore?.Invoke(this);
			onDamage?.Invoke(this);
			onUnspawn?.Invoke(this);
		}

		// 入力レーンとノート中心位置の距離を計算する
		// currentFrameInfo: 現在フレームの楽曲時間などの情報
		// inputLane: 入力されたレーン位置
		// 戻り値: ノート中心と入力レーンの二乗距離
		public virtual float LaneDistance(ref MusicScoreInfo currentFrameInfo, float inputLane)
		{
			float center = (LaneStartF + LaneEndF) * 0.5f;
			float diff = center - inputLane;
			return diff * diff;
		}

		// 指定された楽曲時間でこのノートを判定した場合の結果を計算する
		// musicTime: 判定に使用する楽曲時間
		// 戻り値: 計算された判定結果基底クラスでは None を返す
		public virtual NoteResult CalcNoteResult(float musicTime)
		{
			return NoteResult.None;
		}

		// 現在の入力がこのノートの判定条件を満たしているかどうかを調べる
		// liveInput: 判定に使用するライブ入力情報
		// lane: 入力されたレーン位置
		// 戻り値: 判定可能な入力であれば true
		public virtual bool IsJudgment(ref LiveInput liveInput, float lane)
		{
			return false;
		}

		// 現在の時間差が判定可能時間内にあるかどうかを調べる
		// 戻り値: 判定可能時間内であれば true
		public virtual bool IsJudgmentTime()
		{
			return OffsetJudgeTime >= -LiveConfig.LiveMasterData.JudgeTimeBefore && OffsetJudgeTime <= LiveConfig.LiveMasterData.JudgeTimeAfter;
		}

		// オートプレイ用に、現在時刻に到達したノートを自動で Perfect 判定にする
		// currentFrameInfo: 現在フレームの楽曲時間などの情報
		// 戻り値: 自動判定を実行した場合は true
		public virtual bool AutoJudgment(MusicScoreInfo currentFrameInfo)
		{
			if (State == NoteState.Done || MusicScoreInfo.time > currentFrameInfo.time)
			{
				return false;
			}
			JudgeInfo = (NoteResult.Perfect, NoteResultDescription.None);
			State = NoteState.Done;
			return true;
		}
	}
}
