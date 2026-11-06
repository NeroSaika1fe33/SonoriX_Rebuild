namespace FFrameWork.Live
{
	// ノート種類の列挙
	public enum NoteCategory 
	{
		// 固定レーン通常ノート
		FixedNormal = 0,
		// 固定レーンロングノート
		FixedLong = 1,
		// フリーレーン通常ノート
		FreeLaneNormal = 2,
		// フリーレーンロングノート
		FreeLaneLong = 3,
		// スライダーフリックノート
		SliderFlick = 4,
		// スライダーロングノート
		SliderLong = 5,
		// ボリュームフリックノート
		VolumeFlick = 6,
		// ボリュームロングノート
		VolumeFlickLong = 7,
		// ガイドロングノート
		GuideLong = 8,
		// ガイド終端ノート
		GuideEnd = 9
	}
	
	// ノート方向の列挙
	public enum NoteDirection
	{
		// 方向指定なし
		Default = 0,
		// 左方向
		Left = 1,
		// 右方向
		Right = 2
	}

	// ロングノート描画用の線タイプ
	public enum NoteLineType
	{
		Linear = 0,		// 直線
		EaseIn = 1,		// イージーイン
		EaseOut = 2,	// イージーアウト
		RightAngle = 3　// 直角
	}

	// ノート状態
	public enum NoteState
	{
		// 出現待ち
		Wait = 0,
		// 出現中
		Playing = 1,
		// 入力開始
		InputBegan = 2,
		// 入力継続中
		Input = 3,
		// 入力リリース
		InputRelease = 4,
		// 判定終了待ち
		Last = 5,
		// 完了
		Done = 6
	}

	// ノート判定結果
	public enum NoteResult
	{
		// 未判定
		None = 0,
		// ミス
		Miss = 1,
		// 通過扱い
		Pass = 2,
		// Bad 判定
		Bad = 3,
		// オート判定
		Auto = 4,
		// Good 判定
		Good = 5,
		// Great 判定
		Great = 6,
		// Perfect 判定
		Perfect = 7,
		// JustPerfect 判定
		JustPerfect = 8
	}

	// 判定タイミングの詳細
	public enum NoteResultDescription
	{
		// 詳細なし
		None = 0,
		// 早い入力
		Fast = 1,
		// 遅い入力
		Late = 2,
		// ちょうどの入力
		Just = 3,
	}
}
