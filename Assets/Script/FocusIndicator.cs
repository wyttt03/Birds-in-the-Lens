using UnityEngine;

/// <summary>
/// Moves a small marker along a line to show the current focus distance.
/// Left end = nearest focus (A), right end = farthest focus (D).
/// Put it on the object that holds the line, for example "FocusMeter".
/// </summary>
public class FocusIndicator : MonoBehaviour
{
    [SerializeField] private BirdWatchingCamera binocular;

    [Tooltip("The line. Its width is the whole focus range.")]
    [SerializeField] private RectTransform track;

    [Tooltip("The small triangle. It must be a child of the track, with anchors at the middle center.")]
    [SerializeField] private RectTransform marker;

    [Tooltip("Keeps the marker this many pixels away from each end of the line.")]
    [SerializeField] private float edgePadding = 0f;

    private void Start()
    {
        if (binocular == null || track == null || marker == null)
        {
            Debug.LogError("Assign the binocular camera, the track and the marker.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        float t = binocular.FocusPosition01;
        float halfWidth = Mathf.Max(track.rect.width * 0.5f - edgePadding, 0f);

        // Only the horizontal position changes. The height is set in the editor.
        Vector2 position = marker.anchoredPosition;
        position.x = Mathf.Lerp(-halfWidth, halfWidth, t);
        marker.anchoredPosition = position;
    }
}