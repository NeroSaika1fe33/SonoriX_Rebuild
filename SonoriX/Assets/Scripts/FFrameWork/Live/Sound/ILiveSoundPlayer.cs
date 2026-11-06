namespace FFrameWork.Live
{
	public interface ILiveSoundPlayer
	{
		// 指定したキュー名のゲーム内 SE をワンショット再生する
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側で指定する再生音量倍率
		void PlayIngameSEOneShot(string cueName, float seVolume = 1f);

		// 指定したキュー名のゲーム内 SE を再生し、停止やパラメータ操作に使う再生 ID を返す
		// cueName: 再生する SE キュー名
		// seVolume: 呼び出し側で指定する再生音量倍率
		uint PlayIngameSE(string cueName, float seVolume = 1f);

		// 再生 ID に対応する SE を停止する
		// playbackId: PlayIngameSE が返した再生 ID
		// allowFadeOut: フェードアウトを許可するか
		void StopSE(uint playbackId, bool allowFadeOut = true);

		// 再生中 SE のパラメータ値を設定する
		// playbackId: PlayIngameSE が返した再生 ID
		// parameterName: 設定対象のパラメータ名
		// value: 設定する値
		void SetParameter(uint playbackId, string parameterName, float value);

		// 管理中のすべての SE を停止する
		void StopAllSE();
	}
}
