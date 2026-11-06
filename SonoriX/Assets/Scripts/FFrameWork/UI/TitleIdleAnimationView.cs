using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using FMODUnity;
using FMOD.Studio;

namespace FFrameWork.UI
{
    // タイトル専用の待機映像。動画または Animator を再生する。
    public sealed class TitleIdleAnimationView : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private Animator animator;
        [SerializeField] private string animatorStateName;
        [SerializeField, Min(1f)] private float videoPrepareTimeoutSeconds = 15f;

        private RenderTexture videoTexture;
        private bool running;
        private bool preparing;
        private bool waitingForFirstFrame;
        [SerializeField] private string playbackStatus = "Stopped";
        private float elapsed;
        private float duration;
        private AnimatorUpdateMode originalAnimatorUpdateMode;
        private bool changedAnimatorMode;
        private GameObject videoSession;
        private EventReference audioEvent;
        private float audioVolume = 1f;
        private EventInstance audioInstance;

        public bool HasFinished { get; private set; }

        public void ConfigureAudio(EventReference eventReference, float volume)
        {
            audioEvent = eventReference;
            audioVolume = Mathf.Clamp(volume, 0f, 2f);
        }

        public bool CanPlay(VideoClip clip)
        {
            return clip != null || (videoPlayer != null &&
                ((videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip != null) ||
                 (videoPlayer.source == VideoSource.Url && !string.IsNullOrWhiteSpace(videoPlayer.url)))) ||
                (animator != null && animator.runtimeAnimatorController != null);
        }

        public void Play(VideoClip clip, float durationSeconds)
        {
            Stop();
            HasFinished = false;
            elapsed = 0f;
            duration = Mathf.Max(0f, durationSeconds);
            running = true;
            if (clip != null && videoPlayer == null)
            {
                // 自動生成プレイヤーは毎回新しいネイティブ再生セッションを使用する。
                videoSession = new GameObject("VideoPlaybackSession");
                videoSession.SetActive(false);
                videoSession.transform.SetParent(transform, false);
                videoPlayer = videoSession.AddComponent<VideoPlayer>();
            }
            bool useVideo = clip != null || (videoPlayer != null &&
                ((videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip != null) ||
                 (videoPlayer.source == VideoSource.Url && !string.IsNullOrWhiteSpace(videoPlayer.url))));
            if (useVideo)
            {
                // clip の setter がネイティブ再生を初期化する。Unity 音声への出力を先に無効化する。
                videoPlayer.playOnAwake = false;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.isLooping = false;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.playbackSpeed = 1f;
                // 自動の追いつき処理で先頭フレームを飛ばし続けないよう、各フレームをデコードする。
                videoPlayer.skipOnDrop = false;
                videoPlayer.enabled = true;
                if (clip != null)
                {
                    videoPlayer.source = VideoSource.VideoClip;
                    videoPlayer.clip = clip;
                }
                if (videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip != null)
                {
                    ushort audioTracks = (ushort)Mathf.Min(videoPlayer.clip.audioTrackCount, VideoPlayer.controlledAudioTrackMaxCount);
                    videoPlayer.controlledAudioTrackCount = audioTracks;
                    for (ushort track = 0; track < audioTracks; track++) videoPlayer.EnableAudioTrack(track, false);
                }
                videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
                videoPlayer.sendFrameReadyEvents = true;
                videoPlayer.frameReady += OnVideoFrameReady;
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.loopPointReached += OnVideoEnded;
                videoPlayer.errorReceived += OnVideoError;
                if (videoImage != null)
                {
                    videoImage.enabled = false;
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    int width = videoPlayer.clip != null ? (int)videoPlayer.clip.width : 1920;
                    int height = videoPlayer.clip != null ? (int)videoPlayer.clip.height : 1080;
                    videoTexture = new RenderTexture(Mathf.Max(1, width), Mathf.Max(1, height), 0, RenderTextureFormat.ARGB32);
                    videoTexture.Create();
                    videoPlayer.targetTexture = videoTexture;
                    videoImage.texture = videoTexture;
                }
                preparing = true;
                playbackStatus = "Preparing";
                if (videoSession != null) videoSession.SetActive(true);
                videoPlayer.Prepare();
            }
            else if (animator != null && animator.runtimeAnimatorController != null)
            {
                originalAnimatorUpdateMode = animator.updateMode;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                changedAnimatorMode = true;
                if (string.IsNullOrEmpty(animatorStateName)) animator.Rebind();
                else animator.Play(animatorStateName, 0, 0f);
                animator.Update(0f);
                StartAudio();
            }
            else
            {
                HasFinished = true;
            }
        }

        private void Update()
        {
            if (!running || HasFinished) return;
            // 再生バックエンドが再入場時にイベントを送らない場合も進行を確認する。
            if (videoPlayer != null)
            {
                if (preparing && videoPlayer.isPrepared) OnVideoPrepared(videoPlayer);
                if (waitingForFirstFrame && videoPlayer.isPlaying && videoPlayer.frame >= 0)
                {
                    OnVideoFrameReady(videoPlayer, videoPlayer.frame);
                }
            }
            elapsed += Time.unscaledDeltaTime;
            if (preparing || waitingForFirstFrame)
            {
                if (elapsed >= videoPrepareTimeoutSeconds)
                {
                    playbackStatus = "Timed out: " + (preparing ? "Prepare" : "first video frame");
                    Debug.LogWarning("Title idle video " + playbackStatus + ". Source: " +
                        (videoPlayer.clip != null ? videoPlayer.clip.name : videoPlayer.url) +
                        ", prepared=" + videoPlayer.isPrepared + ", playing=" + videoPlayer.isPlaying +
                        ", frame=" + videoPlayer.frame, this);
                    HasFinished = true;
                }
                return;
            }
            if (duration > 0f && elapsed >= duration) HasFinished = true;
            if (changedAnimatorMode && !animator.IsInTransition(0))
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.loop && state.normalizedTime >= 1f) HasFinished = true;
            }
            if (HasFinished) StopAudio();
        }

        private void OnVideoPrepared(VideoPlayer player)
        {
            if (!running || !preparing) return;
            preparing = false;
            waitingForFirstFrame = true;
            playbackStatus = "Waiting for first video frame";
            elapsed = 0f;
            if (videoImage != null)
            {
                int width = player.width > 0 ? (int)player.width : 1920;
                int height = player.height > 0 ? (int)player.height : 1080;
                AspectRatioFitter fitter = videoImage.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)width / height;
            }
            player.Play();
        }

        private void OnVideoFrameReady(VideoPlayer player, long frameIndex)
        {
            if (!running || !waitingForFirstFrame || !player.isPlaying) return;
            waitingForFirstFrame = false;
            elapsed = 0f;
            playbackStatus = "Playing";
            if (videoImage != null) videoImage.enabled = true;
            StartAudio();
        }

        private void OnVideoEnded(VideoPlayer player)
        {
            HasFinished = true;
            StopAudio();
        }

        private void OnVideoError(VideoPlayer player, string message)
        {
            Debug.LogWarning("Title idle video failed: " + message, this);
            playbackStatus = "Error: " + message;
            HasFinished = true;
            StopAudio();
        }

        private void StartAudio()
        {
            StopAudio();
            if (audioEvent.IsNull) return;
            try
            {
                audioInstance = RuntimeManager.CreateInstance(audioEvent);
                if (!audioInstance.isValid()) return;
                audioInstance.setVolume(audioVolume);
                audioInstance.setTimelinePosition(0);
                FMOD.RESULT result = audioInstance.start();
                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogWarning("Title idle FMOD audio could not start: " + result, this);
                    StopAudio();
                }
            }
            catch (System.Exception exception)
            {
                StopAudio();
                Debug.LogWarning("Title idle FMOD audio failed: " + exception.Message, this);
            }
        }

        private void StopAudio()
        {
            if (!audioInstance.isValid()) return;
            audioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            audioInstance.release();
            audioInstance.clearHandle();
        }

        public void Stop()
        {
            bool wasRunning = running;
            running = false;
            preparing = false;
            waitingForFirstFrame = false;
            playbackStatus = "Stopped";
            StopAudio();
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.loopPointReached -= OnVideoEnded;
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.frameReady -= OnVideoFrameReady;
                videoPlayer.sendFrameReadyEvents = false;
                if (wasRunning || videoPlayer.isPlaying || videoPlayer.isPrepared) videoPlayer.Stop();
                if (videoPlayer.targetTexture == videoTexture) videoPlayer.targetTexture = null;
            }
            if (videoSession != null)
            {
                videoSession.SetActive(false);
                Destroy(videoSession);
                videoSession = null;
                videoPlayer = null;
            }
            if (changedAnimatorMode && animator != null) animator.updateMode = originalAnimatorUpdateMode;
            changedAnimatorMode = false;
            if (videoImage != null && videoImage.texture == videoTexture) videoImage.texture = null;
            if (videoTexture != null)
            {
                videoTexture.Release();
                Destroy(videoTexture);
                videoTexture = null;
            }
        }

        private void OnDisable() => Stop();

        public static TitleIdleAnimationView CreateDefault(Transform parent)
        {
            GameObject panel = new GameObject("TitleIdleAnimationPanel", typeof(RectTransform), typeof(Image));
            panel.SetActive(false);
            panel.layer = parent.gameObject.layer;
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = Color.black;
            GameObject image = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
            image.layer = panel.layer;
            image.transform.SetParent(panel.transform, false);
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TitleIdleAnimationView view = panel.AddComponent<TitleIdleAnimationView>();
            view.videoImage = image.GetComponent<RawImage>();
            view.videoImage.raycastTarget = false;
            return view;
        }
    }
}
