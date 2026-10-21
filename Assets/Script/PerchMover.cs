using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lets the bird fly between fixed perches with the E key.
/// Perches in view show a UI hint. E flies to the one closest to the screen center.
/// Add this to the book's Gameplay Controls array so it stops while the book is open.
/// </summary>
[DefaultExecutionOrder(100)]
public class PerchMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BirdWatchingCamera binocular;
    [SerializeField] private PhotoCapture photoCapture;

    [Tooltip("Optional. Re-centers the parallax after landing.")]
    [SerializeField] private ParallaxCameraOrbit parallax;

    [Header("Perches")]
    [Tooltip("All perches of this level, including the starting one.")]
    [SerializeField] private PerchPoint[] perches;
    [SerializeField] private int startIndex = 0;

    [Header("Targeting")]
    [Tooltip("Radius of the area where a perch counts as in view, as a fraction of screen height.")]
    [SerializeField] private float viewRadius = 0.45f;
    [SerializeField] private float maxPerchDistance = 200f;

    [Header("Flight")]
    [SerializeField] private float flightDuration = 1.2f;
    [Tooltip("Extra height at the middle of the flight, in meters.")]
    [SerializeField] private float arcHeight = 1f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hint Timing")]
    [Tooltip("Seconds after the level starts before the hints appear and E works.")]
    [SerializeField] private float hintDelayAtStart = 3f;

    [Tooltip("Seconds after each landing before the hints appear again. 0 = right away.")]
    [SerializeField] private float hintDelayAfterLanding = 0f;

    private float hintsAllowedTime;
    [Header("Prompt")]
    [Tooltip("Optional object such as a text saying '[E] Fly here'. Shown when a perch can be targeted.")]
    [SerializeField] private GameObject promptObject;

    [Header("Feedback Events")]
    public UnityEvent onFlightStarted;
    public UnityEvent onFlightFinished;

    private int currentIndex;
    private bool isFlying;

    private void Start()
    {
        if (perches == null || perches.Length == 0 || binocular == null)
        {
            Debug.LogError("Assign the binocular camera and at least one perch.", this);
            return;
        }

        // Snap to the starting perch. This script runs after the camera scripts.
        currentIndex = Mathf.Clamp(startIndex, 0, perches.Length - 1);
        PerchPoint start = perches[currentIndex];

        binocular.transform.position = start.Position;
        binocular.SetLookDirection(start.Yaw, start.Pitch, false);

        if (parallax != null)
        {
            parallax.Recalibrate(0f);
        }
        ValidateSetup();
        hintsAllowedTime = Time.time + hintDelayAtStart;
        HideAllHints();
    }

    private void OnDisable()
    {
        // The book disables this script, so hide hints while it is open.
        HideAllHints();
    }

    private void Update()
    {
        if (isFlying || perches == null || perches.Length == 0 || binocular == null)
        {
            return;
        }
        // Keep the hints hidden and ignore E until the delay has passed.
        if (Time.time < hintsAllowedTime)
        {
            HideAllHints();
            return;
        }
        // Escape unlocks the cursor, which also pauses perch input.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Camera cam = binocular.ViewCamera;

        if (cam == null)
        {
            return;
        }

        int candidateIndex = UpdateHints(cam);

        if (promptObject != null)
        {
            promptObject.SetActive(candidateIndex >= 0);
        }

        if (candidateIndex >= 0 && IsInteractPressed())
        {
            StartCoroutine(FlyTo(candidateIndex));
        }
    }

    /// <summary>Updates every hint icon and returns the best perch to fly to, or -1.</summary>
    private int UpdateHints(Camera cam)
    {
        int bestIndex = -1;
        float bestOffset = float.MaxValue;

        for (int i = 0; i < perches.Length; i++)
        {
            PerchPoint perch = perches[i];

            if (perch == null)
            {
                continue;
            }

            if (i == currentIndex ||
                !TryGetScreenOffset(cam, perch, out float offset, out Vector3 screenPosition))
            {
                perch.HideHint();
                continue;
            }

            perch.ShowHint(screenPosition);

            if (offset < bestOffset)
            {
                bestOffset = offset;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool TryGetScreenOffset(
        Camera cam,
        PerchPoint perch,
        out float offset,
        out Vector3 screenPosition)
    {
        offset = 0f;
        screenPosition = Vector3.zero;

        Vector3 viewport = cam.WorldToViewportPoint(perch.Position);

        if (viewport.z <= 0f || viewport.z > maxPerchDistance)
        {
            return false;
        }

        Vector2 centered = new Vector2(
            (viewport.x - 0.5f) * cam.aspect,
            viewport.y - 0.5f);

        offset = centered.magnitude;

        if (offset > viewRadius)
        {
            return false;
        }

        screenPosition = cam.WorldToScreenPoint(perch.Position);
        return true;
    }

    private IEnumerator FlyTo(int targetIndex)
    {
        isFlying = true;

        PerchPoint target = perches[targetIndex];
        Transform rig = binocular.transform;
        Transform camTransform = binocular.ViewCamera.transform;

        // Lock everything the player could do during the flight.
        binocular.SetControlsLocked(true);

        if (photoCapture != null)
        {
            photoCapture.SetCaptureEnabled(false);
        }

        HideAllHints();
        onFlightStarted?.Invoke();
        if (parallax != null)
        {
            parallax.SetFollowOnly(true);
        }

        Vector3 startPosition = rig.position;
        float startYaw = rig.eulerAngles.y;
        float startPitch = Mathf.DeltaAngle(0f, camTransform.localEulerAngles.x);

        float duration = Mathf.Max(flightDuration, 0.05f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));

            rig.position =
                Vector3.Lerp(startPosition, target.Position, t) +
                Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);

            rig.rotation = Quaternion.Euler(
                0f, Mathf.LerpAngle(startYaw, target.Yaw, t), 0f);

            camTransform.localRotation = Quaternion.Euler(
                Mathf.Lerp(startPitch, target.Pitch, t), 0f, 0f);

            yield return null;
        }

        // Land: place exactly, then make this direction the new center of the look limits.
        rig.position = target.Position;
        binocular.SetLookDirection(target.Yaw, target.Pitch, true);

        if (parallax != null)
        {
            parallax.SetFollowOnly(false);
            parallax.Recalibrate(0f);
        }

        currentIndex = targetIndex;
        hintsAllowedTime = Time.time + hintDelayAfterLanding;

        binocular.SetControlsLocked(false);

        if (photoCapture != null)
        {
            photoCapture.SetCaptureEnabled(true);
        }

        isFlying = false;
        onFlightFinished?.Invoke();
        StartCoroutine(LogLandingState(target));
    }

    private void HideAllHints()
    {
        if (perches != null)
        {
            foreach (PerchPoint perch in perches)
            {
                if (perch != null)
                {
                    perch.HideHint();
                }
            }
        }

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
    }
    /// <summary>Logs warnings for scene setups that stop the camera from landing on a perch.</summary>
    private void ValidateSetup()
    {
        Transform rig = binocular.transform;
        Camera cam = binocular.ViewCamera;

        if (cam == null)
        {
            return;
        }

        if (!cam.transform.IsChildOf(rig))
        {
            Debug.LogWarning(
                "[PerchMover] The view camera is not a child of the object that holds " +
                "BirdWatchingCamera, so it will not follow the rig.", this);
        }

        if (cam.transform.localPosition.sqrMagnitude > 0.0001f)
        {
            Debug.LogWarning(
                "[PerchMover] The view camera is offset from the rig pivot by " +
                cam.transform.localPosition.ToString("F2") +
                ". The view will land that far from the perch marker.", this);
        }

        if (parallax != null && parallax.transform != cam.transform)
        {
            Debug.LogWarning(
                "[PerchMover] ParallaxCameraOrbit is not on the view camera object (" +
                parallax.name + "). It must be on the camera, not on the rig.", this);
        }

        if (rig.GetComponent<CharacterController>() != null ||
            rig.GetComponent<Rigidbody>() != null ||
            rig.GetComponentInParent<Animator>() != null)
        {
            Debug.LogWarning(
                "[PerchMover] The rig has a CharacterController, Rigidbody or Animator " +
                "that may override its position.", this);
        }

        foreach (PerchPoint perch in perches)
        {
            if (perch != null && perch.transform.IsChildOf(rig))
            {
                Debug.LogError(
                    "[PerchMover] " + perch.name + " is a child of the camera rig, " +
                    "so it moves with the camera and the flight never reaches it.", perch);
            }
        }
    }

    private IEnumerator LogLandingState(PerchPoint target)
    {
        LogPositions("Landed", target);
        yield return new WaitForSeconds(1f);
        LogPositions("1 s after landing", target);
    }

    private void LogPositions(string label, PerchPoint target)
    {
        Debug.Log(
            "[PerchMover] " + label +
            " | perch " + target.Position.ToString("F2") +
            " | rig " + binocular.transform.position.ToString("F2") +
            " | camera " + binocular.ViewCamera.transform.position.ToString("F2"),
            this);
    }
    private bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}