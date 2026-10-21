using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BirdWatchingCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Volume globalVolume;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float pitchLimit = 85f;

    [Header("Binocular Zoom")]
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float minimumFov = 10f;
    [SerializeField] private float zoomStep = 4f;
    [SerializeField] private float zoomSmoothSpeed = 12f;

    [Header("Manual Focus - Meters")]
    [SerializeField] private float initialFocusDistance = 10f;
    [SerializeField] private float minimumFocusDistance = 0.5f;
    [SerializeField] private float maximumFocusDistance = 200f;
    [SerializeField] private float focusSpeed = 0.6f;

    [Header("Depth Of Field")]
    [SerializeField] private float baseFocalLength = 50f;
    [SerializeField] private float aperture = 2.8f;

    // Shift focus during zoom to encourage manual refocusing.
    [SerializeField] private float focusShiftPerZoomStep = 0.08f;

    private DepthOfField depthOfField;

    private float yaw;
    private float pitch;
    private float targetFov;
    private float focusDistance;

    public Camera ViewCamera => viewCamera;

    public float FocusDistance => focusDistance;
    private bool controlsLocked;
    [Header("Look Limits")]
    [Tooltip("Degrees the view may turn left and right from the starting direction.")]
    [SerializeField] private float yawRange = 45f;
    [SerializeField] private float pitchMin = -15f;
    [SerializeField] private float pitchMax = 30f;

    private float startYaw;

    public float Magnification
    {
        get
        {
            if (viewCamera == null)
            {
                return 1f;
            }

            return Mathf.Tan(normalFov * Mathf.Deg2Rad * 0.5f) /
                   Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        }
    }
    private void Start()
    {
        if (viewCamera == null || globalVolume == null)
        {
            Debug.LogError(
                "Assign the view camera and global volume.",
                this
            );

            enabled = false;
            return;
        }

        // Use a runtime profile instance to preserve the original asset.
        if (!globalVolume.profile.TryGet(out depthOfField))
        {
            Debug.LogError(
                "The volume profile requires a Depth Of Field override.",
                this
            );

            enabled = false;
            return;
        }

        yaw = transform.eulerAngles.y;
        startYaw = yaw;

        pitch = Mathf.DeltaAngle(
            0f,
            viewCamera.transform.localEulerAngles.x
        );

        targetFov = normalFov;
        viewCamera.fieldOfView = normalFov;

        focusDistance = Mathf.Clamp(
            initialFocusDistance,
            minimumFocusDistance,
            maximumFocusDistance
        );

        depthOfField.active = true;
        depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
        depthOfField.aperture.Override(aperture);

        ApplyDepthOfField();
        SetCursorLocked(true);
    }

    private void Update()
    {
        ReadInput(
            out Vector2 mouseDelta,
            out float scroll,
            out float focusInput,
            out bool escapePressed,
            out bool clickPressed
        );

        if (escapePressed)
        {
            SetCursorLocked(false);
        }
        else if (clickPressed)
        {
            SetCursorLocked(true);
        }

        // Pause camera controls while the cursor is unlocked.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }
        // Ignore all camera input while locked, for example during a perch flight.
        if (controlsLocked)
        {
            return;
        }
        UpdateLook(mouseDelta);
        UpdateZoom(scroll);
        UpdateFocus(focusInput);
        ApplyDepthOfField();
    }

    private void UpdateLook(Vector2 mouseDelta)
    {
        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;

        yaw = Mathf.Clamp(yaw, startYaw - yawRange, startYaw + yawRange);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Rotate the parent horizontally and the camera vertically.
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        viewCamera.transform.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateZoom(float scroll)
    {
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Use the scroll direction independently of device scaling.
            float direction = Mathf.Sign(scroll);
            float previousFov = targetFov;

            targetFov = Mathf.Clamp(
                targetFov - direction * zoomStep,
                minimumFov,
                normalFov
            );

            // Do not shift focus when zoom has reached its limit.
            if (!Mathf.Approximately(previousFov, targetFov))
            {
                focusDistance *= Mathf.Exp(
                    direction * focusShiftPerZoomStep
                );

                focusDistance = Mathf.Clamp(
                    focusDistance,
                    minimumFocusDistance,
                    maximumFocusDistance
                );
            }
        }

        // Smooth zoom using frame-rate-independent exponential blending.
        float blend =
            1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime);

        viewCamera.fieldOfView = Mathf.Lerp(
            viewCamera.fieldOfView,
            targetFov,
            blend
        );
    }

    private void UpdateFocus(float focusInput)
    {
        // A moves focus closer. D moves focus farther away.
        // Proportional adjustment provides finer control at short distances.
        focusDistance *= Mathf.Exp(
            focusInput * focusSpeed * Time.deltaTime
        );

        focusDistance = Mathf.Clamp(
            focusDistance,
            minimumFocusDistance,
            maximumFocusDistance
        );
    }

    private void ApplyDepthOfField()
    {
        // Calculate magnification relative to the initial field of view.
        float magnification =
            Mathf.Tan(normalFov * Mathf.Deg2Rad * 0.5f) /
            Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);

        depthOfField.focusDistance.Override(focusDistance);

        // Longer focal lengths make defocus more visible.
        depthOfField.focalLength.Override(
            Mathf.Clamp(
                baseFocalLength * magnification,
                1f,
                300f
            )
        );
    }
    /// <summary>Blocks look, zoom and focus input, for example during a flight.</summary>
    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;
    }

    /// <summary>
    /// Points the view in a new direction and makes it the center of the look limits.
    /// Used when the viewer lands on a new perch.
    /// </summary>
    public void SetLookDirection(float newYaw, float newPitch, bool resetOptics)
    {
        if (viewCamera == null)
        {
            return;
        }

        yaw = newYaw;
        startYaw = newYaw;
        pitch = Mathf.Clamp(newPitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (resetOptics)
        {
            // A new viewpoint starts zoomed out with the default focus.
            targetFov = normalFov;

            focusDistance = Mathf.Clamp(
                initialFocusDistance,
                minimumFocusDistance,
                maximumFocusDistance
            );
        }
    }
    /// <summary>Changes how far the view may turn left and right from its center.</summary>
    public void SetYawRange(float newYawRange)
    {
        yawRange = Mathf.Max(0f, newYawRange);
    }
    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void ReadInput(
        out Vector2 mouseDelta,
        out float scroll,
        out float focusInput,
        out bool escapePressed,
        out bool clickPressed)
    {
#if ENABLE_INPUT_SYSTEM
        // Read input through the new Input System.
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        mouseDelta = mouse != null
            ? mouse.delta.ReadValue()
            : Vector2.zero;

        scroll = mouse != null
            ? mouse.scroll.ReadValue().y
            : 0f;

        focusInput = 0f;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
            {
                focusInput -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                focusInput += 1f;
            }
        }

        escapePressed =
            keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame;

        clickPressed =
            mouse != null &&
            mouse.leftButton.wasPressedThisFrame;
#else
        // Read input through the legacy Input Manager.
        // Compensate for the default mouse axis scaling.
        mouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        ) * 10f;

        scroll = Input.mouseScrollDelta.y;

        focusInput =
            (Input.GetKey(KeyCode.D) ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) ? 1f : 0f);

        escapePressed = Input.GetKeyDown(KeyCode.Escape);
        clickPressed = Input.GetMouseButtonDown(0);
#endif
    }
}