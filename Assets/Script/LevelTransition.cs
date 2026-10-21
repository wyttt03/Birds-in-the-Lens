using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ends a level: waits briefly, fades the screen to a color, then loads the
/// next scene in Build Settings. Also fades in when its own scene starts.
/// The fade overlay is created by this script at runtime, so no canvas or
/// image needs to be set up by hand.
/// Call Begin() from LevelProgress.onLevelComplete.
/// Keep this script on an object that is always active.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Build index of the next scene. -1 = the scene after this one in Build Settings.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Header("Fade")]
    [Tooltip("Color of the fade, for example black or white.")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private bool fadeInOnStart = true;

    [Header("Timing")]
    [Tooltip("Seconds to wait after the last photo before the fade starts.")]
    [SerializeField] private float delayBeforeTransition = 1.5f;

    [Header("Gameplay To Stop")]
    [SerializeField] private BirdWatchingCamera binocular;
    [SerializeField] private PhotoCapture photoCapture;

    [Tooltip("Other scripts to disable, for example the book UI and the perch mover.")]
    [SerializeField] private MonoBehaviour[] controlsToDisable;

    private Canvas overlayCanvas;
    private CanvasGroup overlayGroup;
    private bool started;
    private Coroutine fadeInRoutine;

    private void Awake()
    {
        BuildOverlay();

        // Cover the screen right away so a new level never flashes uncovered.
        overlayGroup.alpha = fadeInOnStart ? 1f : 0f;
        overlayCanvas.enabled = fadeInOnStart;
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            fadeInRoutine = StartCoroutine(FadeInAtStart());
        }
    }

    private void OnDestroy()
    {
        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
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

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "LevelTransition cannot start because its object is inactive.", this);
            return;
        }

        int nextIndex = FindNextSceneIndex();

        // Check first, so a setup problem never leaves the player frozen.
        if (nextIndex < 0)
        {
            return;
        }

        started = true;

        if (fadeInRoutine != null)
        {
            StopCoroutine(fadeInRoutine);
            fadeInRoutine = null;
        }

        Debug.Log(
            "[LevelTransition] " + SceneManager.GetActiveScene().name +
            " -> scene index " + nextIndex, this);

        StartCoroutine(RunTransition(nextIndex));
    }

    // Builds a full-screen canvas above everything, holding one solid image.
    private void BuildOverlay()
    {
        GameObject root = new GameObject("LevelFadeOverlay");

        overlayCanvas = root.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32000;

        overlayGroup = root.AddComponent<CanvasGroup>();
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("Fade");
        imageObject.transform.SetParent(root.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private int FindNextSceneIndex()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = nextSceneBuildIndex;

        if (nextIndex < 0)
        {
            // A scene missing from the Build Settings list reports index -1.
            if (currentIndex < 0)
            {
                Debug.LogError(
                    "LevelTransition: this scene is not in the Build Settings scene list " +
                    "(or is unchecked), so the next scene cannot be found.", this);
                return -1;
            }

            nextIndex = currentIndex + 1;
        }

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                "LevelTransition: no scene with build index " + nextIndex +
                ". Add the next scene to the Build Settings scene list.", this);
            return -1;
        }

        return nextIndex;
    }

    private IEnumerator RunTransition(int nextIndex)
    {
        StopGameplay();

        yield return new WaitForSecondsRealtime(delayBeforeTransition);

        yield return Fade(0f, 1f);

        SceneManager.LoadScene(nextIndex);
    }

    private IEnumerator FadeInAtStart()
    {
        yield return Fade(1f, 0f);

        overlayCanvas.enabled = false;
        fadeInRoutine = null;
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
        overlayCanvas.enabled = true;
        overlayGroup.alpha = from;

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
}