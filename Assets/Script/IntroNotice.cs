using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shows a notice on a black screen (fade in, hold, fade out), then loads the next scene.
/// Put it on an object in a scene that only holds a black screen and the notice.
/// </summary>
public class IntroNotice : MonoBehaviour
{
    [Tooltip("A CanvasGroup on the notice text or image.")]
    [SerializeField] private CanvasGroup noticeGroup;

    [SerializeField] private float fadeInSeconds = 1f;
    [SerializeField] private float holdSeconds = 3f;
    [SerializeField] private float fadeOutSeconds = 1f;

    [Tooltip("Press Space to skip the notice. Useful while testing.")]
    [SerializeField] private bool allowSkip = false;

    [Tooltip("Build index of the next scene. -1 = the scene after this one in Build Settings.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    private void Start()
    {
        Cursor.visible = false;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        noticeGroup.alpha = 0f;

        yield return Fade(0f, 1f, fadeInSeconds);
        yield return Wait(holdSeconds);
        yield return Fade(1f, 0f, fadeOutSeconds);

        int nextIndex = nextSceneBuildIndex >= 0
            ? nextSceneBuildIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                "IntroNotice: no next scene. Add it to the Build Settings scene list.", this);
            yield break;
        }

        SceneManager.LoadScene(nextIndex);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float duration = Mathf.Max(seconds, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            noticeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);

            if (allowSkip && IsSkipPressed())
            {
                break;
            }

            yield return null;
        }

        noticeGroup.alpha = to;
    }

    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;

            if (allowSkip && IsSkipPressed())
            {
                yield break;
            }

            yield return null;
        }
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