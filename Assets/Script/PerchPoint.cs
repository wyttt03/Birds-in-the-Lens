using UnityEngine;

/// <summary>
/// A fixed spot the bird can fly to. The object's position is where the view
/// will sit, and its rotation is the starting view direction
/// (Y = left/right, X = up/down, positive X looks down).
/// </summary>
public class PerchPoint : MonoBehaviour
{
    [Tooltip("UI icon in the HUD canvas shown while this perch is flyable and in view.")]
    [SerializeField] private RectTransform hintIcon;

    public Vector3 Position => transform.position;
    public float Yaw => transform.eulerAngles.y;
    public float Pitch => Mathf.DeltaAngle(0f, transform.eulerAngles.x);

    /// <summary>Shows the icon at a screen position (pixels, overlay canvas).</summary>
    public void ShowHint(Vector3 screenPosition)
    {
        if (hintIcon == null)
        {
            return;
        }

        if (!hintIcon.gameObject.activeSelf)
        {
            hintIcon.gameObject.SetActive(true);
        }

        hintIcon.position = screenPosition;
    }

    public void HideHint()
    {
        if (hintIcon != null && hintIcon.gameObject.activeSelf)
        {
            hintIcon.gameObject.SetActive(false);
        }
    }

    // Draws the perch and its view direction in the Scene view for easy placement.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}