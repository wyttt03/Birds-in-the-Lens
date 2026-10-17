using UnityEngine;

public class BirdPhotoTarget : MonoBehaviour
{
    [Header("Bird Visual")]
    public SpriteRenderer birdRenderer;

    public bool IsAvailable =>
        isActiveAndEnabled &&
        birdRenderer != null &&
        birdRenderer.enabled &&
        birdRenderer.gameObject.activeInHierarchy &&
        birdRenderer.sprite != null;

    public int VisualLayer =>
        birdRenderer != null ? birdRenderer.gameObject.layer : 0;

    public bool TryGetWorldCorners(Vector3[] corners)
    {
        if (!IsAvailable || corners == null || corners.Length < 4)
        {
            return false;
        }

        Bounds bounds = birdRenderer.localBounds;
        Transform visualTransform = birdRenderer.transform;

        float z = bounds.center.z;

        corners[0] = visualTransform.TransformPoint(
            new Vector3(bounds.min.x, bounds.min.y, z)
        );

        corners[1] = visualTransform.TransformPoint(
            new Vector3(bounds.max.x, bounds.min.y, z)
        );

        corners[2] = visualTransform.TransformPoint(
            new Vector3(bounds.max.x, bounds.max.y, z)
        );

        corners[3] = visualTransform.TransformPoint(
            new Vector3(bounds.min.x, bounds.max.y, z)
        );

        return true;
    }

    public bool OwnsTransform(Transform candidate)
    {
        return candidate == transform ||
               candidate.IsChildOf(transform);
    }

    private void Reset()
    {
        birdRenderer = GetComponentInChildren<SpriteRenderer>();
    }
}