namespace FFrameWork
{
	// 判定範囲の種類
	public enum JudgeFrameType
	{
		// 未定義の判定範囲
		Undefined = 0,
		// フリーレーン通常ノートの判定範囲
		FreeLane_Normal = 1,
		// フリーレーンフリックノートの判定範囲
		FreeLane_Flick = 2,
		// フリーレーンロングノート開始の判定範囲
		FreeLane_Long = 3,
		// フリーレーンロングノート終端の判定範囲
		FreeLane_Long_End = 4,
		// フリーレーンロング終端フリックの判定範囲
		FreeLane_Long_End_SliderFlick = 5,
		// 固定レーン通常ノートの判定範囲
		FixedLane_Normal = 6,
		// 固定レーンロングノート開始の判定範囲
		FixedLane_Long = 7,
		// 固定レーンロングノート終端の判定範囲
		FixedLane_Long_End = 8,
		// 固定レーンロング終端フリックの判定範囲
		FixedLane_Long_End_SliderFlick = 9,
		// スライダーフリックノートの判定範囲
		Slider_Flick = 10,
		// スライダーロングノート開始の判定範囲
		Slider_Long = 11,
		// スライダーロングノート終端の判定範囲
		Slider_Long_End = 12,
		// スライダーロング終端フリックの判定範囲
		Slider_Long_End_SliderFlick = 13,
		// ボリュームフリックノートの判定範囲
		Volume_Flick = 14,
		// ボリュームロングノート開始の判定範囲
		Volume_Long = 15,
		// ボリュームロングノート終端の判定範囲
		Volume_Long_End = 16,
		// ボリュームロング終端フリックの判定範囲
		Volume_Long_End_SliderFlick = 17
	}
}
