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
    [Tooltip("Higher = stronger parallax. Start with 2-5.")]
    [SerializeField] private float parallaxDistance = 3f;

    [Tooltip("0 = instant. Small values add a soft follow.")]
    [SerializeField] private float smoothTime = 0.05f;

    private Vector3 baseLocalPosition;
    private Vector3 baseForward;
    private Vector3 velocity;

    private void Start()
    {
        baseLocalPosition = transform.localPosition;
        baseForward = transform.forward;
    }

    private void LateUpdate()
    {
        // Anchor follows the parent so the rig can move later (bird perches).
        Vector3 anchor = transform.parent != null
            ? transform.parent.TransformPoint(baseLocalPosition)
            : baseLocalPosition;

        Vector3 forwardDelta = transform.forward - baseForward;
        Vector3 targetPosition = anchor - forwardDelta * parallaxDistance;

        transform.position = smoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(
                transform.position, targetPosition, ref velocity, smoothTime);
    }
}