using FFrameWork.Live;

namespace FFrameWork
{
	// スライダーロングノーツのライン表示ビュー
	public class SliderLineView : NoteLineView
	{
		// スライダーロング用の中心進行率を取得する
		// t: 現在のライン進行率
		// startNote: ライン開始側のノーツ
		protected override float GetLineProgress(float t, INote startNote)
		{
			return startNote != null ? LiveConfig.GetSliderLongLineCenterProgress(t, startNote) : t;
		}
	}
}
