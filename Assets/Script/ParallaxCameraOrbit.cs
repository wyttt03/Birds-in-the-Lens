using UnityEngine;

/// <summary>
/// Moves the camera slightly along an orbit while it rotates,
/// so 2D layers placed at different depths show real parallax.
/// Attach to the view camera (the child that handles pitch).
/// Does not touch camera rotation.
/// </summary>
public class ParallaxCameraOrbit : MonoBehaviour
{
    [Header("Parallax")]
    [Tooltip("Higher magnitude = stronger parallax. Negative values make near layers move faster.")]
    [SerializeField] private float parallaxDistance = -3f;

    [Tooltip("0 = instant. Small values add a soft follow.")]
    [SerializeField] private float smoothTime = 0.05f;

    [Tooltip("Smoothing used briefly after Recalibrate.")]
    [SerializeField] private float recalibrateSmoothTime = 0.4f;

    [Tooltip("Smoothing used while the rig is flying and the parallax offset is removed.")]
    [SerializeField] private float followSmoothTime = 0.25f;

    private Vector3 baseLocalPosition;
    private Vector3 baseForward;
    private Vector3 velocity;
    private float blendRemaining;
    private bool followOnly;

    private void Start()
    {
        // The script assumes it sits on the view camera, which is a child of the rig.
        if (transform.parent == null || GetComponent<Camera>() == null)
        {
            Debug.LogError(
                "ParallaxCameraOrbit must be on the view camera, " +
                "and the camera must be a child of the camera rig.",
                this);

            enabled = false;
            return;
        }

        baseLocalPosition = transform.localPosition;
        baseForward = transform.forward;
    }

    /// <summary>
    /// While true, the parallax offset is kept at zero so the camera
    /// follows its parent exactly, for example during a perch flight.
    /// </summary>
    public void SetFollowOnly(bool value)
    {
        followOnly = value;
    }

    /// <summary>
    /// Uses the current view direction as the new neutral direction.
    /// Call this after the view is re-aimed, for example when landing on a perch.
    /// </summary>
    public void Recalibrate(float blendSeconds)
    {
        baseForward = transform.forward;
        velocity = Vector3.zero;
        blendRemaining = blendSeconds;
    }

    private void LateUpdate()
    {
        // Keep the neutral direction equal to the current one, so no offset builds up.
        if (followOnly)
        {
            baseForward = transform.forward;
        }

        // Anchor follows the parent so the rig can move between perches.
        Vector3 anchor = transform.parent != null
            ? transform.parent.TransformPoint(baseLocalPosition)
            : baseLocalPosition;

        Vector3 forwardDelta = transform.forward - baseForward;
        Vector3 targetPosition = anchor - forwardDelta * parallaxDistance;

        float activeSmoothTime = followOnly
            ? followSmoothTime
            : (blendRemaining > 0f ? recalibrateSmoothTime : smoothTime);

        blendRemaining -= Time.deltaTime;

        transform.position = activeSmoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(
                transform.position, targetPosition, ref velocity, activeSmoothTime);
    }
}