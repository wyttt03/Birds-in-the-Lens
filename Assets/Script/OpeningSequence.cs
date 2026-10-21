using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Opening of level 1. The view starts with a narrow turning range. When the
/// opening bird is photographed it disappears, the camera pulls back to the
/// level's start position, the view range widens, and the real level begins.
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BirdWatchingCamera binocular;
    [SerializeField] private PhotoCapture photoCapture;

    [Tooltip("Optional. Keeps the parallax from swinging during the pull back.")]
    [SerializeField] private ParallaxCameraOrbit parallax;

    [Header("Opening")]
    [Tooltip("The first bird. It disappears once photographed.")]
    [SerializeField] private PhotoTarget openingBird;

    [Tooltip("Empty object. Its position and rotation are the camera pose for the real level.")]
    [SerializeField] private Transform levelCameraPoint;

    [Tooltip("How far the view may turn left and right once the level begins.")]
    [SerializeField] private float levelYawRange = 45f;

    [Header("Timing")]
    [SerializeField] private float delayBeforeBirdHides = 0.8f;
    [SerializeField] private float delayAfterBirdHides = 0.4f;
    [SerializeField] private float pullBackDuration = 3f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start Of Level")]
    [Tooltip("Objects to switch on when the pull back starts, for example the level's birds and the counter text. Keep them inactive in the scene.")]
    [SerializeField] private GameObject[] activateAtPullBack;
    [Tooltip("Scripts to disable when the pull back starts, for example the opening's LevelProgress.")]
    [SerializeField] private MonoBehaviour[] disableAtPullBack;

    [Tooltip("Scripts to enable when the pull back starts, for example LevelProgress. Keep them disabled in the scene.")]
    [SerializeField] private MonoBehaviour[] enableAtPullBack;

    [Header("Feedback Events")]
    public UnityEvent onBirdHidden;
    public UnityEvent onPullBackStarted;
    public UnityEvent onOpeningFinished;

    private bool triggered;

    private void Start()
    {
        if (binocular == null || openingBird == null || levelCameraPoint == null)
        {
            Debug.LogError(
                "Assign the binocular camera, the opening bird and the level camera point.",
                this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        PhotoCapture.TargetPhotographed += HandleTargetPhotographed;
    }

    private void OnDisable()
    {
        PhotoCapture.TargetPhotographed -= HandleTargetPhotographed;
    }

    private void HandleTargetPhotographed(PhotoTarget photographed)
    {
        if (triggered || photographed != openingBird)
        {
            return;
        }

        triggered = true;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // Lock everything the player could do during the sequence.
        binocular.SetControlsLocked(true);

        if (photoCapture != null)
        {
            photoCapture.SetCaptureEnabled(false);
        }

        yield return new WaitForSeconds(delayBeforeBirdHides);

        openingBird.gameObject.SetActive(false);
        onBirdHidden?.Invoke();

        yield return new WaitForSeconds(delayAfterBirdHides);

        // Stop the opening's counter before the real level's counter takes over.
        foreach (MonoBehaviour script in disableAtPullBack)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }

        // Switch the real level on. Objects first, so the progress counter sees every bird.
        foreach (GameObject target in activateAtPullBack)
        {
            if (target != null)
            {
                target.SetActive(true);
            }
        }

        foreach (MonoBehaviour script in enableAtPullBack)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        
        onPullBackStarted?.Invoke();

        if (parallax != null)
        {
            parallax.SetFollowOnly(true);
        }

        Transform rig = binocular.transform;
        Transform camTransform = binocular.ViewCamera.transform;

        Vector3 startPosition = rig.position;
        float startYaw = rig.eulerAngles.y;
        float startPitch = Mathf.DeltaAngle(0f, camTransform.localEulerAngles.x);

        float endYaw = levelCameraPoint.eulerAngles.y;
        float endPitch = Mathf.DeltaAngle(0f, levelCameraPoint.eulerAngles.x);

        float duration = Mathf.Max(pullBackDuration, 0.05f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));

            rig.position = Vector3.Lerp(startPosition, levelCameraPoint.position, t);
            rig.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(startYaw, endYaw, t), 0f);
            camTransform.localRotation = Quaternion.Euler(
                Mathf.Lerp(startPitch, endPitch, t), 0f, 0f);

            yield return null;
        }

        // Land: the level's camera pose becomes the new center of the look limits.
        rig.position = levelCameraPoint.position;
        binocular.SetYawRange(levelYawRange);
        binocular.SetLookDirection(endYaw, endPitch, true);

        if (parallax != null)
        {
            parallax.SetFollowOnly(false);
            parallax.Recalibrate(0f);
        }

        binocular.SetControlsLocked(false);

        if (photoCapture != null)
        {
            photoCapture.SetCaptureEnabled(true);
        }

        onOpeningFinished?.Invoke();
    }
}