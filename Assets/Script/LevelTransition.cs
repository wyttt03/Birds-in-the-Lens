using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ends a level: waits briefly, fades to black, plays an optional cutscene video,
/// then loads the next scene. Call Begin() from LevelProgress.onLevelComplete.
/// Also fades in from black when its own scene starts.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Build index of the next scene. -1 = the scene after this one in Build Settings.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Header("Cutscene (optional)")]
    [Tooltip("Leave empty for a level change without a video.")]
    [SerializeField] private VideoClip cutsceneClip;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;
    [SerializeField] private RawImage videoImage;

    [Tooltip("Press Space to skip the video. Useful while testing.")]
    [SerializeField] private bool allowSkip = false;

    [Header("Fade")]
    [Tooltip("Full-screen group with a black Image. Used for fade out and fade in.")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private bool fadeInOnStart = true;

    [Header("Timing")]
    [SerializeField] private float delayBeforeTransition = 1.5f;

    [Header("Gameplay To Stop")]
    [SerializeField] private BirdWatchingCamera binocular;
    [SerializeField] private PhotoCapture photoCapture;

    [Tooltip("Other scripts to disable, for example the book UI and the perch mover.")]
    [SerializeField] private MonoBehaviour[] controlsToDisable;

    private bool started;
    private RenderTexture videoTexture;

    private void Start()
    {
        if (fadeInOnStart && overlayGroup != null)
        {
            StartCoroutine(FadeInAtStart());
        }
    }

    private void OnDestroy()
    {
        // AudioListener.pause is static and would otherwise survive the scene change.
        if (started)
        {
            AudioListener.pause = false;
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }

    /// <summary>Starts the level ending. Safe to call more than once.</summary>
    [ContextMenu("Begin Transition (test)")]
    public void Begin()
    {
        if (started)
        {
            return;
        }

        started = true;
        StartCoroutine(RunTransition());
    }

    private IEnumerator FadeInAtStart()
    {
        overlayGroup.alpha = 1f;

        if (videoImage != null)
        {
            videoImage.enabled = false;
        }

        yield return Fade(1f, 0f);

        overlayGroup.blocksRaycasts = false;
        overlayGroup.gameObject.SetActive(false);
    }

    private IEnumerator RunTransition()
    {
        StopGameplay();

        yield return new WaitForSecondsRealtime(delayBeforeTransition);

        int nextIndex = nextSceneBuildIndex >= 0
            ? nextSceneBuildIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "LevelTransition: no next scene in Build Settings (index " + nextIndex + ").",
                this);
            yield break;
        }

        yield return Fade(0f, 1f);

        if (cutsceneClip != null && videoPlayer != null && videoImage != null)
        {
            yield return PlayCutscene();
        }

        AudioListener.pause = false;
        SceneManager.LoadScene(nextIndex);
    }

    private void StopGameplay()
    {
        if (binocular != null)
        {
            binocular.SetControlsLocked(true);
        }

        if (photoCapture != null)
        {
            photoCapture.SetCaptureEnabled(false);
        }

        if (controlsToDisable == null)
        {
            return;
        }

        foreach (MonoBehaviour control in controlsToDisable)
        {
            if (control != null)
            {
                control.enabled = false;
            }
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        if (overlayGroup == null)
        {
            yield break;
        }

        overlayGroup.gameObject.SetActive(true);
        overlayGroup.blocksRaycasts = true;

        float duration = Mathf.Max(fadeDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        overlayGroup.alpha = to;
    }

    private IEnumerator PlayCutscene()
    {
        // Silence the game and keep only the video's own audio.
        AudioListener.pause = true;

        if (videoAudioSource != null)
        {
            videoAudioSource.ignoreListenerPause = true;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = cutsceneClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Create the render texture at the video's native size.
        videoTexture = new RenderTexture(
            (int)cutsceneClip.width, (int)cutsceneClip.height, 0);

        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
        videoImage.enabled = false;

        AspectRatioFitter fitter = videoImage.GetComponent<AspectRatioFitter>();

        if (fitter != null)
        {
            fitter.aspectRatio = (float)cutsceneClip.width / cutsceneClip.height;
        }

        if (videoAudioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        bool finished = false;
        videoPlayer.loopPointReached += _ => finished = true;
        videoPlayer.Play();

        // Show the image only after the first frames arrive, to avoid a flash.
        while (videoPlayer.frame < 1 && !finished)
        {
            yield return null;
        }

        videoImage.enabled = true;

        while (!finished)
        {
            if (allowSkip && IsSkipPressed())
            {
                break;
            }

            yield return null;
        }

        videoPlayer.Stop();
    }

    private bool IsSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}