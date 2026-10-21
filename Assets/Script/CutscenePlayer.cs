using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lives in a scene that only plays one video. Plays it full screen,
/// then loads the next scene in Build Settings.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoClip clip;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private RawImage videoImage;

    [Tooltip("Press Space to skip the video. Useful while testing.")]
    [SerializeField] private bool allowSkip = false;

    [Tooltip("Give up on the video if it is not ready after this many seconds.")]
    [SerializeField] private float prepareTimeoutSeconds = 10f;

    [Header("Next Level")]
    [Tooltip("Build index of the next scene. -1 = the scene after this one in Build Settings.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Header("Fade")]
    [Tooltip("Seconds to fade the video in from black and out to black.")]
    [SerializeField] private float fadeDuration = 0.8f;

    private RenderTexture videoTexture;

    private void Start()
    {
        // The previous level unlocked the cursor when it unloaded. Hide it again.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }

    private IEnumerator Run()
    {
        if (clip == null || videoPlayer == null || videoImage == null)
        {
            Debug.LogError("Assign the clip, video player and video image.", this);
        }
        else
        {
            yield return PlayVideo();
        }

        LoadNextScene();
    }

    private IEnumerator PlayVideo()
    {
        videoImage.enabled = false;
        SetImageAlpha(0f);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Create the render texture at the video's native size.
        videoTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;

        AspectRatioFitter fitter = videoImage.GetComponent<AspectRatioFitter>();

        if (fitter != null)
        {
            fitter.aspectRatio = (float)clip.width / clip.height;
        }

        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        bool failed = false;
        bool finished = false;

        videoPlayer.errorReceived += (source, message) =>
        {
            Debug.LogError("Cutscene error: " + message, this);
            failed = true;
        };

        videoPlayer.loopPointReached += _ => finished = true;

        videoPlayer.Prepare();

        float waited = 0f;

        while (!videoPlayer.isPrepared && !failed && waited < prepareTimeoutSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (failed || !videoPlayer.isPrepared)
        {
            Debug.LogError("Cutscene could not be prepared. Skipping it.", this);
            yield break;
        }

        videoPlayer.Play();

        // Show the image only after the first frames arrive, to avoid a flash.
        waited = 0f;

        while (videoPlayer.frame < 1 && !finished && !failed && waited < 3f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        videoImage.enabled = true;
        yield return FadeImage(0f, 1f);

        while (!finished && !failed)
        {
            if (allowSkip && IsSkipPressed())
            {
                break;
            }

            yield return null;
        }

        yield return FadeImage(1f, 0f);
        videoPlayer.Stop();
    }

    private IEnumerator FadeImage(float from, float to)
    {
        float duration = Mathf.Max(fadeDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetImageAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetImageAlpha(to);
    }

    private void SetImageAlpha(float alpha)
    {
        if (videoImage != null)
        {
            videoImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void LoadNextScene()
    {
        int nextIndex = nextSceneBuildIndex >= 0
            ? nextSceneBuildIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "CutscenePlayer: no next scene in Build Settings (index " + nextIndex + ").",
                this);
            return;
        }

        SceneManager.LoadScene(nextIndex);
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