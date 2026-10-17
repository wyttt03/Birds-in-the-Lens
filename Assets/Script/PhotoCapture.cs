using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles the shutter (Space key). A photo succeeds when a target of a
/// not-yet-photographed species is fully inside the scope circle, large
/// enough, and in focus. Photographing an already photographed species
/// always counts as a failed photo.
/// Put this on its own child object and add it to the book's
/// Gameplay Controls array so it stops while the book is open.
/// </summary>
public class PhotoCapture : MonoBehaviour
{
    /// <summary>Raised once for every newly photographed species.</summary>
    public static event Action<PhotoTarget> TargetPhotographed;

    private static readonly HashSet<string> capturedSpecies = new HashSet<string>();

    [Header("References")]
    [SerializeField] private BirdWatchingCamera binocular;

    [Header("Frame")]
    [Tooltip("Circle radius as a fraction of screen height. 0.4 = circle fills 80% of height.")]
    [Range(0.1f, 0.6f)]
    [SerializeField] private float circleRadius = 0.4f;

    [Tooltip("Minimum target size as a fraction of the circle diameter.")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumFillRatio = 0.15f;

    [Header("Focus")]
    [Tooltip("Allowed focus error as a fraction of the target distance.")]
    [Range(0.02f, 0.5f)]
    [SerializeField] private float focusTolerance = 0.12f;

    [Tooltip("Smallest allowed focus error in meters, for very close targets.")]
    [SerializeField] private float minimumFocusToleranceMeters = 0.3f;

    [SerializeField] private float maximumDistance = 250f;

    [Header("Feedback Events")]
    public UnityEvent onPhotoSuccess;
    public UnityEvent onPhotoFail;

    private bool captureEnabled = true;

    /// <summary>True if this species was already photographed this level.</summary>
    public static bool IsSpeciesCaptured(string speciesId)
    {
        return capturedSpecies.Contains(speciesId);
    }

    /// <summary>Call this to block the shutter from other scripts.</summary>
    public void SetCaptureEnabled(bool enabled)
    {
        captureEnabled = enabled;
    }

    private void Awake()
    {
        // Statics survive scene loads, so reset the record for each new level.
        capturedSpecies.Clear();
    }

    private void Update()
    {
        // Escape unlocks the cursor, which also pauses the shutter.
        bool locked = Cursor.lockState == CursorLockMode.Locked;

        if (captureEnabled && locked && IsShutterPressed())
        {
            TakePhoto();
        }
    }

    private void TakePhoto()
    {
        Camera cam = binocular.ViewCamera;

        PhotoTarget best = null;
        float bestCenterDistance = float.MaxValue;
        string lastFailure = "No target in view.";

        foreach (PhotoTarget target in PhotoTarget.All)
        {
            // A species that was already photographed never counts again,
            // no matter which individual object is in the frame.
            if (target.IsCaptured || capturedSpecies.Contains(target.SpeciesId))
            {
                lastFailure = target.SpeciesId + " was already photographed.";
                continue;
            }

            if (!Evaluate(cam, target, out float centerDistance, out string failure))
            {
                lastFailure = failure;
                continue;
            }

            // If several targets qualify, take the one closest to the center.
            if (centerDistance < bestCenterDistance)
            {
                bestCenterDistance = centerDistance;
                best = target;
            }
        }

        if (best == null)
        {
            Debug.Log("Photo failed: " + lastFailure);
            onPhotoFail?.Invoke();
            return;
        }

        best.MarkCaptured();
        capturedSpecies.Add(best.SpeciesId);

        Debug.Log("Photo success: " + best.SpeciesId);

        // Play feedback first so a failing listener can never silence it.
        onPhotoSuccess?.Invoke();
        NotifyTargetPhotographed(best);
    }

    /// <summary>
    /// Calls every listener separately, so one failing listener cannot
    /// block the others, and logs the real error in the Console.
    /// </summary>
    private void NotifyTargetPhotographed(PhotoTarget target)
    {
        Action<PhotoTarget> handlers = TargetPhotographed;

        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<PhotoTarget>)handler)(target);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    private bool Evaluate(
        Camera cam,
        PhotoTarget target,
        out float centerDistance,
        out string failure)
    {
        centerDistance = 0f;
        failure = null;

        if (!target.TryGetBounds(out Bounds bounds))
        {
            failure = target.name + " has no renderer bounds.";
            return false;
        }

        // Depth along the view axis, which is what Depth Of Field uses.
        float depth = Vector3.Dot(
            bounds.center - cam.transform.position,
            cam.transform.forward);

        if (depth <= 0.01f || depth > maximumDistance)
        {
            failure = target.name + " is behind the camera or too far away.";
            return false;
        }

        float aspect = cam.aspect;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // Project all eight bounding box corners into aspect-corrected screen space.
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z);

            Vector3 viewport = cam.WorldToViewportPoint(corner);

            if (viewport.z <= 0f)
            {
                failure = target.name + " is partly behind the camera.";
                return false;
            }

            Vector2 point = new Vector2(
                (viewport.x - 0.5f) * aspect,
                viewport.y - 0.5f);

            if (point.magnitude > circleRadius)
            {
                failure = target.name + " is not fully inside the circle.";
                return false;
            }

            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        float size = Mathf.Max(max.x - min.x, max.y - min.y);

        if (size < circleRadius * 2f * minimumFillRatio)
        {
            failure = target.name + " is too small. Zoom in.";
            return false;
        }

        float allowedError = Mathf.Max(
            minimumFocusToleranceMeters,
            depth * focusTolerance);

        if (Mathf.Abs(binocular.FocusDistance - depth) > allowedError)
        {
            failure = target.name + " is out of focus.";
            return false;
        }

        Vector3 centerViewport = cam.WorldToViewportPoint(center);
        centerDistance = new Vector2(
            (centerViewport.x - 0.5f) * aspect,
            centerViewport.y - 0.5f).magnitude;

        return true;
    }

    private bool IsShutterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}