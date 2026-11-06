using System;

namespace FFrameWork.Live
{
	[Serializable]
	// 固定レーン系ノートの判定幅設定
	public class MasterFixedLaneJudgeFrame
	{
		// 判定フレーム種別の ID
		public int id;

		// ライブ中で扱う判定フレーム名
		public string ingameJudgeFrameType;

		// Perfect の早い方向許容フレーム数
		public float perfectBefore;

		// Perfect の遅い方向許容フレーム数
		public float perfectAfter;

		// Great の早い方向許容フレーム数
		public float greatBefore;

		// Great の遅い方向許容フレーム数
		public float greatAfter;

		// Good の早い方向許容フレーム数
		public float goodBefore;

		// Good の遅い方向許容フレーム数
		public float goodAfter;

		// Bad の早い方向許容フレーム数
		public float badBefore;

		// Bad の遅い方向許容フレーム数
		public float badAfter;

		// id から変換した判定フレーム種別
		public JudgeFrameType FrameType { get; private set; }

		// Perfect の早い方向許容秒数
		public float PerfectBeforeJudgeTime { get; private set; }

		// Perfect の遅い方向許容秒数
		public float PerfectAfterJudgeTime { get; private set; }

		// Great の早い方向許容秒数
		public float GreatBeforeJudgeTime { get; private set; }

		// Great の遅い方向許容秒数
		public float GreatAfterJudgeTime { get; private set; }

		// Good の早い方向許容秒数
		public float GoodBeforeJudgeTime { get; private set; }

		// Good の遅い方向許容秒数
		public float GoodAfterJudgeTime { get; private set; }

		// Bad の早い方向許容秒数
		public float BadBeforeJudgeTime { get; private set; }

		// Bad の遅い方向許容秒数
		public float BadAfterJudgeTime { get; private set; }

		// シリアライズ用の空コンストラクタ
		public MasterFixedLaneJudgeFrame()
		{
		}

		// 判定幅設定を生成する
		// frameType: 判定フレーム種別
		// ingameJudgeFrameType: ライブ中で扱う判定フレーム名
		// perfectBefore: Perfect の早い方向許容フレーム数
		// perfectAfter: Perfect の遅い方向許容フレーム数
		// greatBefore: Great の早い方向許容フレーム数
		// greatAfter: Great の遅い方向許容フレーム数
		// goodBefore: Good の早い方向許容フレーム数
		// goodAfter: Good の遅い方向許容フレーム数
		// badBefore: Bad の早い方向許容フレーム数
		// badAfter: Bad の遅い方向許容フレーム数
		public MasterFixedLaneJudgeFrame(
			JudgeFrameType frameType,
			string ingameJudgeFrameType,
			float perfectBefore,
			float perfectAfter,
			float greatBefore,
			float greatAfter,
			float goodBefore,
			float goodAfter,
			float badBefore,
			float badAfter)
		{
			id = (int)frameType;
			this.ingameJudgeFrameType = ingameJudgeFrameType;
			this.perfectBefore = perfectBefore;
			this.perfectAfter = perfectAfter;
			this.greatBefore = greatBefore;
			this.greatAfter = greatAfter;
			this.goodBefore = goodBefore;
			this.goodAfter = goodAfter;
			this.badBefore = badBefore;
			this.badAfter = badAfter;
			RefreshCalculatedValues();
		}

		// フレーム数で保持している判定幅を秒数へ変換してキャッシュする
		public void RefreshCalculatedValues()
		{
			FrameType = (JudgeFrameType)id;
			PerfectBeforeJudgeTime = FramesToSeconds(perfectBefore);
			PerfectAfterJudgeTime = FramesToSeconds(perfectAfter);
			GreatBeforeJudgeTime = FramesToSeconds(greatBefore);
			GreatAfterJudgeTime = FramesToSeconds(greatAfter);
			GoodBeforeJudgeTime = FramesToSeconds(goodBefore);
			GoodAfterJudgeTime = FramesToSeconds(goodAfter);
			BadBeforeJudgeTime = FramesToSeconds(badBefore);
			BadAfterJudgeTime = FramesToSeconds(badAfter);
		}

		// フレーム数を秒数に変換する
		// frame: 変換対象のフレーム数
		private static float FramesToSeconds(float frame)
		{
			return frame * Live.LiveUtility.OneFrameTime;
		}
	}
}
