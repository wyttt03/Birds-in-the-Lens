using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A light item (leaf, feather) that drifts down with sway and spin, lands on the
/// ground, stays there, and can disappear after a while.
/// Put this on the root of the item prefab. The art Quad is a child (Visual).
/// </summary>
public class FallingItem : MonoBehaviour
{
    private enum State
    {
        Falling,
        Landed,
        Disappearing
    }

    // Items that are lying on the ground, oldest first.
    private static readonly List<FallingItem> landedItems = new List<FallingItem>();

    [Header("Falling")]
    [Tooltip("On: always turns to face the camera (for views with camera-facing trees). " +
         "Off: keeps the direction it was dropped in and stays put in the world.")]
    [SerializeField] private bool faceCamera = false;
    [Tooltip("The child object with the art Quad. It spins around its own center.")]

    [SerializeField] private Transform visual;

    [Tooltip("Meters per second, picked at random between the two values.")]
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(0.8f, 1.6f);

    [SerializeField] private float swayDistance = 0.4f;
    [SerializeField] private Vector2 swayFrequencyRange = new Vector2(0.6f, 1.2f);

    [Tooltip("Degrees per second.")]
    [SerializeField] private float spinSpeed = 60f;

    [Tooltip("0 = stays upright. About 0.8 for a view looking down from above.")]
    [Range(0f, 1f)]
    [SerializeField] private float pitchFollow = 0f;

    [Header("Landing")]
    [Tooltip("Used when the dropper gives no ground point. Meters below the spawn point.")]
    [SerializeField] private float defaultFallDistance = 8f;

    [Tooltip("How far above the ground height the item's center rests, in meters.")]
    [SerializeField] private float landedHeightOffset = 0.05f;

    [Tooltip("On: lies flat on the ground (for views looking down). Off: keeps facing the camera.")]
    [SerializeField] private bool lieFlatOnGround = false;

    [Tooltip("Side views only: vertical squash when landed, to suggest the item lies down. 1 = none.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float landedScaleY = 0.6f;

    [Tooltip("Side views only: random tilt in degrees once landed.")]
    [SerializeField] private float landedRandomTilt = 15f;

    [Tooltip("Seconds the item stays on the ground. 0 = forever.")]
    [SerializeField] private float stayDuration = 0f;

    [SerializeField] private float disappearDuration = 1f;

    [Tooltip("When more items than this lie on the ground, the oldest ones disappear. 0 = no limit.")]
    [SerializeField] private int maxLandedItems = 30;

    private State state = State.Falling;
    private Transform cameraTransform;
    private Vector3 startPosition;
    private float groundHeight;
    private bool hasGroundHeight;
    private float fallSpeed;
    private float swayFrequency;
    private float swayPhase;
    private float spinDirection;
    private float elapsed;
    private float landedTime;
    private float disappearTimer;

    /// <summary>Sets the world height of the ground this item lands on.</summary>
    public void SetGroundHeight(float worldY)
    {
        groundHeight = worldY;
        hasGroundHeight = true;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        startPosition = transform.position;
        // Sway along the item's own sideways direction, fixed at the moment it is dropped.
        swayAxis = transform.right;

        if (!hasGroundHeight)
        {
            groundHeight = startPosition.y - defaultFallDistance;
        }
        Debug.Log(
    "[FallingItem] spawn y " + startPosition.y.ToString("F2") +
    ", ground y " + groundHeight.ToString("F2") +
    (hasGroundHeight ? " (from Ground Point)" : " (DEFAULT, no Ground Point received)"),
    this);
        fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);
        swayFrequency = Random.Range(swayFrequencyRange.x, swayFrequencyRange.y);
        swayPhase = Random.Range(0f, Mathf.PI * 2f);
        spinDirection = Random.value < 0.5f ? -1f : 1f;
    }

    private void OnDestroy()
    {
        landedItems.Remove(this);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Falling:
                UpdateFalling();
                break;

            case State.Landed:
                UpdateLanded();
                break;

            case State.Disappearing:
                UpdateDisappearing();
                break;
        }
    }

    private void UpdateFalling()
    {
        elapsed += Time.deltaTime;

        // Sway along a fixed direction, so the item stays put when the camera turns.
        Vector3 sideways = swayAxis;

        if (faceCamera)
        {
            FaceCamera();

            if (cameraTransform != null)
            {
                sideways = cameraTransform.right;
            }
        }
        float sway = Mathf.Sin(elapsed * swayFrequency * Mathf.PI * 2f + swayPhase) * swayDistance;

        Vector3 position =
            startPosition + Vector3.down * (fallSpeed * elapsed) + sideways * sway;

        float restHeight = groundHeight + landedHeightOffset;

        if (position.y <= restHeight)
        {
            position.y = restHeight;
            transform.position = position;
            Land();
            return;
        }

        transform.position = position;

        if (visual != null)
        {
            visual.Rotate(0f, 0f, spinSpeed * spinDirection * Time.deltaTime, Space.Self);
        }
    }

    private void Land()
    {
        state = State.Landed;
        landedTime = 0f;

        if (lieFlatOnGround)
        {
            // Lie flat on a horizontal ground, turned to a random direction.
            transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        }
        else if (visual != null)
        {
            // In side views, tilt a little and squash so it looks like it lies down.
            visual.localRotation = Quaternion.Euler(
                0f, 0f, Random.Range(-landedRandomTilt, landedRandomTilt));

            Vector3 scale = visual.localScale;
            scale.y *= landedScaleY;
            visual.localScale = scale;
        }

        RegisterLanded();
    }

    private void UpdateLanded()
    {
        // The position stays fixed. In side views it keeps facing the camera.
        if (faceCamera && !lieFlatOnGround)
        {
            FaceCamera();
        }

        if (stayDuration > 0f)
        {
            landedTime += Time.deltaTime;

            if (landedTime >= stayDuration)
            {
                StartDisappearing();
            }
        }
    }

    private void UpdateDisappearing()
    {
        disappearTimer += Time.deltaTime;

        float t = Mathf.Clamp01(disappearTimer / Mathf.Max(disappearDuration, 0.01f));
        transform.localScale = Vector3.one * (1f - t);

        if (!lieFlatOnGround)
        {
            FaceCamera();
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void StartDisappearing()
    {
        landedItems.Remove(this);

        if (state == State.Disappearing)
        {
            return;
        }

        state = State.Disappearing;
        disappearTimer = 0f;
    }

    private void RegisterLanded()
    {
        // Forget items that were destroyed, for example by a scene change.
        landedItems.RemoveAll(item => item == null);
        landedItems.Add(this);

        if (maxLandedItems <= 0)
        {
            return;
        }

        while (landedItems.Count > maxLandedItems)
        {
            // StartDisappearing removes the item from the list.
            landedItems[0].StartDisappearing();
        }
    }

    private void FaceCamera()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 euler = cameraTransform.eulerAngles;
        float pitch = Mathf.DeltaAngle(0f, euler.x);
        transform.rotation = Quaternion.Euler(pitch * pitchFollow, euler.y, 0f);
    }
    private Vector3 swayAxis;
}