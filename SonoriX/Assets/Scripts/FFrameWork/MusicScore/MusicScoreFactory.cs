using FFrameWork;

namespace FFrameWork.Live
{
	public static class MusicScoreFactory
	{
		// SUS 文字列から MusicScore を生成する
		// susData: 変換元の SUS データ
		// bundleBuildData: ライブ表示と判定の調整値
		public static MusicScore Create(string susData, LiveBundleBuildData bundleBuildData)
		{
			if (string.IsNullOrEmpty(susData))
			{
				return new MusicScore();
			}

			Converter converter = new Converter
			{
				LiveBundleBuildData = bundleBuildData,
				LongNoteComboBeat = bundleBuildData != null ? bundleBuildData.LongNoteComboBeat : 0,
				FreeLaneLongNoteComboBeat = bundleBuildData != null ? bundleBuildData.FreeLaneLongNoteComboBeat : 0,
				SliderLongNoteComboBeat = bundleBuildData != null ? bundleBuildData.SliderLongNoteComboBeat : 0,
				VolumeLongNoteComboBeat = bundleBuildData != null ? bundleBuildData.VolumeLongNoteComboBeat : 0
			};
			return converter.Convert(susData);
		}
	}
}
