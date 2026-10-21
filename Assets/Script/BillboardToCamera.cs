using UnityEngine;

/// <summary>
/// Keeps an object facing the same way as the camera, so a flat 2D Quad always
/// shows its front. Put it on an anchor object placed at the base of a tree or
/// character, with the art Quad as a child.
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    [Tooltip("Leave empty to use the camera tagged MainCamera.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("0 = stays upright and only turns left and right. " +
             "1 = tilts fully with the camera. Try about 0.8 when looking down from above.")]
    [Range(0f, 1f)]
    [SerializeField] private float pitchFollow = 1f;

    private Transform cameraTransform;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (viewCamera != null)
        {
            cameraTransform = viewCamera.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // Copy the camera's turn, and a chosen share of its up and down tilt.
        Vector3 euler = cameraTransform.eulerAngles;
        float pitch = Mathf.DeltaAngle(0f, euler.x);

        transform.rotation = Quaternion.Euler(pitch * pitchFollow, euler.y, 0f);
    }
}