using System;
using System.Runtime.InteropServices;
using FMODUnity;
using UnityEngine;
using UnityEngine.Audio;

namespace VerticalSlice.Live
{
    public class AudioSpectrum : MonoBehaviour
    {
        private enum SpectrumInput
        {
            // AudioSource から直接スペクトラムを読む
            AudioSource = 0,

            // AudioListener のミキサー後出力から読む
            AudioListenerAfterMixer = 1,

            // FMOD Master ChannelGroup から読む
            FmodMaster = 2,

            // 指定した FMOD EventEmitter から読む
            FmodEventEmitter = 3,

            // 指定した FMOD Bus から読む
            FmodBus = 4
        }

        [Header("Audio Source & Mixer")]
        // スペクトラム取得元
        [SerializeField] private SpectrumInput spectrumInput = SpectrumInput.FmodBus;

        // Unity AudioSource 入力時の取得元
        [SerializeField] private AudioSource audioSource;

        // AudioSource へ割り当てる MixerGroup
        [SerializeField] private AudioMixerGroup targetMixerGroup;

        [Header("FMOD Source")]
        // FMOD Bus 入力時に参照する Bus パス
        [SerializeField] private string fmodBusPath = "bus:/Music";

        // FMOD EventEmitter 入力時に参照する Emitter
        [SerializeField] private StudioEventEmitter fmodEventEmitter;

        // EventEmitter 取得失敗時に Master へフォールバックするか
        [SerializeField] private bool fallbackToFmodMaster;

        [Header("Shader Connection")]
        // スペクトラム配列を渡す Renderer
        [SerializeField] private Renderer targetRenderer;

        // FFT サンプル数
        [SerializeField] private int sampleSize = 512;

        // デバッグログを出すか
        [SerializeField] private bool debugLog;

        [Header("Spectrum Smoothing")]
        // 入力スペクトラムに掛けるゲイン
        [SerializeField] private float spectrumInputGain = 1f;

        // ノイズとして切り捨てる下限値
        [SerializeField] private float noiseFloor = 0.0005f;

        // 値が上がるときの追従速度
        [SerializeField] private float attackSpeed = 18f;

        // 値が下がるときの追従速度
        [SerializeField] private float releaseSpeed = 6f;

        // スペクトラム値の上限
        [SerializeField] private float maxSpectrumValue = 1f;

        // Shader へ渡す最大スペクトラム配列長
        private const int ShaderSpectrumSize = 512;

        // Shader のスペクトラム配列プロパティ ID
        private static readonly int SpectrumArrayID = Shader.PropertyToID("_AudioSpectrumData");

        // Shader のスペクトラム長プロパティ ID
        private static readonly int SpectrumLengthID = Shader.PropertyToID("_SpectrumLength");

        // 取得直後のスペクトラム値
        private float[] rawSpectrumData;

        // スムージング後に Shader へ渡すスペクトラム値
        private float[] spectrumData;

        // FMOD FFT からチャンネル分を受け取る一時バッファ
        private float[] fmodChannelSpectrumBuffer;

        // Shader へ渡す有効スペクトラム長
        private int spectrumLength;

        // Renderer へ値を渡す MaterialPropertyBlock
        private MaterialPropertyBlock propertyBlock;

        // FMOD の FFT DSP
        private FMOD.DSP fmodFftDsp;

        // FFT DSP を接続する ChannelGroup
        private FMOD.ChannelGroup fmodChannelGroup;

        // 現在 DSP を接続している入力種別
        private SpectrumInput attachedFmodInput;

        // 現在 DSP を接続している Bus パス
        private string attachedFmodBusPath;

        // 現在 DSP を接続している EventEmitter
        private StudioEventEmitter attachedFmodEventEmitter;

        // 接続時点の FMOD Event ハンドル
        private IntPtr attachedFmodEventHandle;

        // 接続時点の ChannelGroup ハンドル
        private IntPtr attachedFmodChannelGroupHandle;

        // FMOD DSP を接続済みか
        private bool isFmodDspAttached;

        // FMOD 読み取りエラーを既にログ出力したか
        private bool hasLoggedFmodReadError;

        // バッファと参照を初期化する
        private void Start()
        {
            sampleSize = Mathf.Clamp(Mathf.ClosestPowerOfTwo(sampleSize), 64, ShaderSpectrumSize);
            rawSpectrumData = new float[sampleSize];
            spectrumData = new float[sampleSize];
            spectrumLength = sampleSize;
            propertyBlock = new MaterialPropertyBlock();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (audioSource != null && targetMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = targetMixerGroup;
            }

            if (debugLog)
            {
                Debug.Log($"[AudioSpectrumProvider] Input={spectrumInput}, AudioSource={(audioSource != null ? audioSource.name : "null")}, FmodEmitter={(fmodEventEmitter != null ? fmodEventEmitter.name : "null")}, Renderer={(targetRenderer != null ? targetRenderer.name : "null")}, SampleSize={sampleSize}");
            }
        }

        // 無効化時に FMOD DSP を解放する
        private void OnDisable()
        {
            ReleaseFmodDsp();
        }

        // 破棄時に FMOD DSP を解放する
        private void OnDestroy()
        {
            ReleaseFmodDsp();
        }

        // スペクトラムを読み取り、スムージングして Shader へ渡す
        private void Update()
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (!ReadSpectrumData())
            {
                return;
            }

            SmoothSpectrumData();

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloatArray(SpectrumArrayID, spectrumData);
            propertyBlock.SetInt(SpectrumLengthID, Mathf.Clamp(spectrumLength, 1, sampleSize));
            targetRenderer.SetPropertyBlock(propertyBlock);

            if (debugLog)
            {
                Debug.Log($"[AudioSpectrumProvider] Spectrum[0]={spectrumData[0]:F6}, Spectrum[8]={spectrumData[Mathf.Min(8, sampleSize - 1)]:F6}");
            }
        }

        // 設定された入力元からスペクトラム値を読み取る
        private bool ReadSpectrumData()
        {
            if (spectrumInput == SpectrumInput.FmodMaster ||
                spectrumInput == SpectrumInput.FmodEventEmitter ||
                spectrumInput == SpectrumInput.FmodBus)
            {
                return ReadFmodSpectrumData();
            }

            if (spectrumInput == SpectrumInput.AudioListenerAfterMixer)
            {
                AudioListener.GetSpectrumData(rawSpectrumData, 0, FFTWindow.BlackmanHarris);
                spectrumLength = sampleSize;
                return true;
            }

            if (audioSource == null || !audioSource.isPlaying)
            {
                return false;
            }

            audioSource.GetSpectrumData(rawSpectrumData, 0, FFTWindow.BlackmanHarris);
            spectrumLength = sampleSize;
            return true;
        }

        // FMOD FFT DSP からスペクトラム値を読み取る
        private bool ReadFmodSpectrumData()
        {
            if (!EnsureFmodDsp())
            {
                ClearRawSpectrumData();
                return true;
            }

            FMOD.RESULT result = fmodFftDsp.getParameterData(
                (int)FMOD.DSP_FFT.SPECTRUMDATA,
                out IntPtr data,
                out uint length);
            if (result != FMOD.RESULT.OK || data == IntPtr.Zero || length == 0)
            {
                if (debugLog && !hasLoggedFmodReadError)
                {
                    Debug.LogWarning($"[AudioSpectrumProvider] FMOD FFT data is unavailable: {result}", this);
                    hasLoggedFmodReadError = true;
                }

                if (IsRecoverableFmodDspError(result))
                {
                    ReleaseFmodDsp();
                }

                ClearRawSpectrumData();
                return true;
            }

            FMOD.DSP_PARAMETER_FFT fftData =
                (FMOD.DSP_PARAMETER_FFT)Marshal.PtrToStructure(data, typeof(FMOD.DSP_PARAMETER_FFT));
            if (fftData.length <= 0 || fftData.numchannels <= 0)
            {
                ClearRawSpectrumData();
                return true;
            }

            if (fmodChannelSpectrumBuffer == null || fmodChannelSpectrumBuffer.Length != fftData.length)
            {
                fmodChannelSpectrumBuffer = new float[fftData.length];
            }

            fftData.getSpectrum(0, ref fmodChannelSpectrumBuffer);
            int copyCount = Mathf.Min(sampleSize, fmodChannelSpectrumBuffer.Length);
            Array.Copy(fmodChannelSpectrumBuffer, rawSpectrumData, copyCount);
            spectrumLength = Mathf.Max(copyCount, 1);
            for (int i = copyCount; i < rawSpectrumData.Length; i++)
            {
                rawSpectrumData[i] = 0f;
            }

            hasLoggedFmodReadError = false;
            return true;
        }

        // 生スペクトラム値を Attack/Release 速度で平滑化する
        private void SmoothSpectrumData()
        {
            if (rawSpectrumData == null || spectrumData == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float attack = Mathf.Max(0f, attackSpeed);
            float release = Mathf.Max(0f, releaseSpeed);
            float gain = Mathf.Max(0f, spectrumInputGain);
            float floor = Mathf.Max(0f, noiseFloor);
            float maxValue = Mathf.Max(0f, maxSpectrumValue);

            for (int i = 0; i < spectrumData.Length; i++)
            {
                float target = Mathf.Max(0f, rawSpectrumData[i] - floor) * gain;
                if (maxValue > 0f)
                {
                    target = Mathf.Min(target, maxValue);
                }

                float speed = target > spectrumData[i] ? attack : release;
                float t = speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * deltaTime);
                spectrumData[i] = Mathf.Lerp(spectrumData[i], target, t);
            }
        }

        // 現在設定に合う FMOD ChannelGroup へ FFT DSP を接続する
        private bool EnsureFmodDsp()
        {
            if (isFmodDspAttached && IsFmodAttachmentCurrent())
            {
                return true;
            }

            ReleaseFmodDsp();

            if (!TryGetFmodChannelGroup(out fmodChannelGroup))
            {
                return false;
            }

            FMOD.RESULT result = RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out fmodFftDsp);
            if (result != FMOD.RESULT.OK)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[AudioSpectrumProvider] Failed to create FMOD FFT DSP: {result}", this);
                }

                return false;
            }

            fmodFftDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, sampleSize);
            fmodFftDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOW, (int)FMOD.DSP_FFT_WINDOW_TYPE.BLACKMANHARRIS);
            fmodFftDsp.setParameterInt((int)FMOD.DSP_FFT.DOWNMIX, (int)FMOD.DSP_FFT_DOWNMIX_TYPE.MONO);

            result = fmodChannelGroup.addDSP(0, fmodFftDsp);
            if (result != FMOD.RESULT.OK)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[AudioSpectrumProvider] Failed to attach FMOD FFT DSP: {result}", this);
                }

                fmodFftDsp.release();
                fmodFftDsp.clearHandle();
                return false;
            }

            isFmodDspAttached = true;
            attachedFmodInput = spectrumInput;
            attachedFmodBusPath = fmodBusPath;
            attachedFmodEventEmitter = fmodEventEmitter;
            attachedFmodEventHandle = GetCurrentFmodEventHandle();
            attachedFmodChannelGroupHandle = fmodChannelGroup.handle;
            return true;
        }

        // 現在の設定から FMOD ChannelGroup を取得する
        // channelGroup: 取得できた ChannelGroup
        private bool TryGetFmodChannelGroup(out FMOD.ChannelGroup channelGroup)
        {
            return TryGetFmodChannelGroup(out channelGroup, true);
        }

        // ログ出力指定付きで現在の設定から FMOD ChannelGroup を取得する
        // channelGroup: 取得できた ChannelGroup
        // logWarning: 失敗時に警告を出すか
        private bool TryGetFmodChannelGroup(out FMOD.ChannelGroup channelGroup, bool logWarning)
        {
            channelGroup = default(FMOD.ChannelGroup);

            if (spectrumInput == SpectrumInput.FmodEventEmitter &&
                fmodEventEmitter != null &&
                fmodEventEmitter.EventInstance.isValid() &&
                fmodEventEmitter.EventInstance.getChannelGroup(out channelGroup) == FMOD.RESULT.OK &&
                channelGroup.hasHandle())
            {
                return true;
            }

            if (spectrumInput == SpectrumInput.FmodBus && TryGetFmodBusChannelGroup(out channelGroup, logWarning))
            {
                return true;
            }

            if (spectrumInput == SpectrumInput.FmodBus)
            {
                return false;
            }

            if (!fallbackToFmodMaster && spectrumInput == SpectrumInput.FmodEventEmitter)
            {
                return false;
            }

            return RuntimeManager.CoreSystem.getMasterChannelGroup(out channelGroup) == FMOD.RESULT.OK &&
                   channelGroup.hasHandle();
        }

        // 設定された FMOD Bus から ChannelGroup を取得する
        // channelGroup: 取得できた ChannelGroup
        private bool TryGetFmodBusChannelGroup(out FMOD.ChannelGroup channelGroup)
        {
            return TryGetFmodBusChannelGroup(out channelGroup, true);
        }

        // ログ出力指定付きで FMOD Bus から ChannelGroup を取得する
        // channelGroup: 取得できた ChannelGroup
        // logWarning: 失敗時に警告を出すか
        private bool TryGetFmodBusChannelGroup(out FMOD.ChannelGroup channelGroup, bool logWarning)
        {
            channelGroup = default(FMOD.ChannelGroup);
            if (string.IsNullOrEmpty(fmodBusPath))
            {
                return false;
            }

            FMOD.RESULT result = RuntimeManager.StudioSystem.getBus(fmodBusPath, out FMOD.Studio.Bus bus);
            if (result != FMOD.RESULT.OK || !bus.isValid())
            {
                if (debugLog && logWarning)
                {
                    Debug.LogWarning($"[AudioSpectrumProvider] FMOD bus was not found: {fmodBusPath} ({result})", this);
                }

                return false;
            }

            result = bus.getChannelGroup(out channelGroup);
            if (result == FMOD.RESULT.OK && channelGroup.hasHandle())
            {
                return true;
            }

            if (debugLog && logWarning)
            {
                Debug.LogWarning($"[AudioSpectrumProvider] FMOD bus channel group is unavailable: {fmodBusPath} ({result})", this);
            }

            return false;
        }

        // 現在の DSP 接続が設定や FMOD ハンドルと一致しているかを返す
        private bool IsFmodAttachmentCurrent()
        {
            if (!fmodFftDsp.hasHandle() ||
                !fmodChannelGroup.hasHandle() ||
                attachedFmodInput != spectrumInput ||
                attachedFmodBusPath != fmodBusPath ||
                attachedFmodEventEmitter != fmodEventEmitter ||
                attachedFmodEventHandle != GetCurrentFmodEventHandle())
            {
                return false;
            }

            if (!TryGetFmodChannelGroup(out FMOD.ChannelGroup currentChannelGroup, false))
            {
                return false;
            }

            return currentChannelGroup.handle == attachedFmodChannelGroupHandle &&
                   currentChannelGroup.handle == fmodChannelGroup.handle;
        }

        // DSP を作り直せば復旧できる FMOD エラーかを返す
        // result: FMOD の結果コード
        private static bool IsRecoverableFmodDspError(FMOD.RESULT result)
        {
            return result == FMOD.RESULT.ERR_INVALID_HANDLE ||
                   result == FMOD.RESULT.ERR_DSP_NOTFOUND ||
                   result == FMOD.RESULT.ERR_CHANNEL_STOLEN;
        }

        // 接続済み FFT DSP を ChannelGroup から外して解放する
        private void ReleaseFmodDsp()
        {
            if (isFmodDspAttached && fmodChannelGroup.hasHandle() && fmodFftDsp.hasHandle())
            {
                fmodChannelGroup.removeDSP(fmodFftDsp);
            }

            if (fmodFftDsp.hasHandle())
            {
                fmodFftDsp.release();
                fmodFftDsp.clearHandle();
            }

            fmodChannelGroup.clearHandle();
            isFmodDspAttached = false;
            attachedFmodBusPath = null;
            attachedFmodEventEmitter = null;
            attachedFmodEventHandle = IntPtr.Zero;
            attachedFmodChannelGroupHandle = IntPtr.Zero;
            ClearSpectrumData();
        }

        // 現在の FMOD EventInstance ハンドルを取得する
        private IntPtr GetCurrentFmodEventHandle()
        {
            if (spectrumInput != SpectrumInput.FmodEventEmitter || fmodEventEmitter == null)
            {
                return IntPtr.Zero;
            }

            return fmodEventEmitter.EventInstance.handle;
        }

        // 生スペクトラム配列をゼロクリアする
        private void ClearRawSpectrumData()
        {
            if (rawSpectrumData == null)
            {
                return;
            }

            Array.Clear(rawSpectrumData, 0, rawSpectrumData.Length);
        }

        // 生データとスムージング後データをゼロクリアする
        private void ClearSpectrumData()
        {
            ClearRawSpectrumData();

            if (spectrumData != null)
            {
                Array.Clear(spectrumData, 0, spectrumData.Length);
            }
        }
    }
}
