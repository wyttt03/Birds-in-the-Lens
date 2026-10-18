using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Flies a bird around a looping route of waypoints. Perch waypoints make the
/// bird land and wait; fly-through waypoints only bend the route.
/// Put this on the bird root. The art Quad must be a child (the Visual).
/// </summary>
public class NpcBirdFlight : MonoBehaviour
{
    [System.Serializable]
    public class Waypoint
    {
        [Tooltip("A scene object. Must NOT be a child of the bird.")]
        public Transform point;

        [Tooltip("On: the bird lands here and waits. Off: it only passes through.")]
        public bool isPerch = true;

        [Tooltip("Min and max seconds to wait here (perch only).")]
        public Vector2 dwellSeconds = new Vector2(4f, 8f);
    }

    [Header("Path")]
    [SerializeField] private Waypoint[] waypoints;
    [SerializeField] private int startIndex = 0;

    [Tooltip("Random wait before the first takeoff, so birds do not move in sync.")]
    [SerializeField] private Vector2 initialDelaySeconds = new Vector2(0f, 3f);

    [Header("Flight")]
    [Tooltip("Meters per second. A good start is about 20% of the bird's distance from the camera.")]
    [SerializeField] private float flySpeed = 4f;

    [Tooltip("Distance over which the bird speeds up after takeoff and slows down before landing.")]
    [SerializeField] private float rampDistance = 2f;

    [Range(0.05f, 1f)]
    [SerializeField] private float minSpeedFactor = 0.2f;

    [Tooltip("Small up-and-down motion while flying, to suggest wing beats. 0 = off.")]
    [SerializeField] private float flapBobHeight = 0.1f;
    [SerializeField] private float flapBobFrequency = 8f;

    [Header("Visual")]
    [Tooltip("The child object with the art Quad.")]
    [SerializeField] private Transform visual;
    [SerializeField] private bool faceCamera = true;

    [Tooltip("Leave empty to use the camera tagged MainCamera.")]
    [SerializeField] private Camera viewCamera;

    [Header("Feedback Events")]
    public UnityEvent onTakeOff;
    public UnityEvent onLand;

    private int currentIndex;
    private int targetIndex;
    private bool isFlying;
    private float waitTimer;
    private Vector3 segmentStart;
    private Vector3 velocity;
    private float visualSign = 1f;
    private Vector3 baseVisualLocalPosition;

    private void Start()
    {
        if (!ValidatePath())
        {
            enabled = false;
            return;
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (visual != null)
        {
            baseVisualLocalPosition = visual.localPosition;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, waypoints.Length - 1);
        transform.position = waypoints[currentIndex].point.position;

        if (waypoints[currentIndex].isPerch)
        {
            isFlying = false;
            waitTimer = Random.Range(initialDelaySeconds.x, initialDelaySeconds.y);

            // Applies the perched look and sound state.
            onLand?.Invoke();
        }
        else
        {
            StartFlight(true);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
        {
            return;
        }

        velocity = Vector3.zero;

        if (isFlying)
        {
            MoveAlongPath(dt);
        }
        else
        {
            waitTimer -= dt;

            if (waitTimer <= 0f)
            {
                StartFlight(true);
            }
        }

        UpdateVisual();
    }

    private void StartFlight(bool raiseTakeOffEvent)
    {
        targetIndex = (currentIndex + 1) % waypoints.Length;
        segmentStart = transform.position;
        isFlying = true;

        if (raiseTakeOffEvent)
        {
            onTakeOff?.Invoke();
        }
    }

    private void MoveAlongPath(float dt)
    {
        Waypoint target = waypoints[targetIndex];
        Vector3 targetPosition = target.point.position;
        Vector3 position = transform.position;

        float remaining = Vector3.Distance(position, targetPosition);
        float traveled = Vector3.Distance(position, segmentStart);
        float ramp = Mathf.Max(rampDistance, 0.01f);

        // Speed up after leaving a perch and slow down before landing on one.
        float factor = 1f;

        if (waypoints[currentIndex].isPerch)
        {
            factor = Mathf.Min(
                factor, Mathf.Lerp(minSpeedFactor, 1f, Mathf.Clamp01(traveled / ramp)));
        }

        if (target.isPerch)
        {
            factor = Mathf.Min(
                factor, Mathf.Lerp(minSpeedFactor, 1f, Mathf.Clamp01(remaining / ramp)));
        }

        Vector3 newPosition = Vector3.MoveTowards(
            position, targetPosition, flySpeed * factor * dt);

        velocity = (newPosition - position) / dt;
        transform.position = newPosition;

        if (Vector3.Distance(newPosition, targetPosition) <= 0.001f)
        {
            ArriveAt(targetIndex);
        }
    }

    private void ArriveAt(int index)
    {
        currentIndex = index;
        Waypoint waypoint = waypoints[index];

        if (waypoint.isPerch)
        {
            isFlying = false;
            waitTimer = Random.Range(waypoint.dwellSeconds.x, waypoint.dwellSeconds.y);
            onLand?.Invoke();
        }
        else
        {
            // Pass-through points keep the bird flying without a takeoff event.
            StartFlight(false);
        }
    }

    private void UpdateVisual()
    {
        if (faceCamera && viewCamera != null)
        {
            // Turn around the vertical axis only, so the bird stays upright.
            Vector3 away = transform.position - viewCamera.transform.position;
            away.y = 0f;

            if (away.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(away);
            }
        }

        if (visual == null)
        {
            return;
        }

        // The art is drawn facing right. Flip it when flying toward screen left.
        if (viewCamera != null && velocity.sqrMagnitude > 0.0001f)
        {
            float side = Vector3.Dot(velocity, viewCamera.transform.right);

            if (Mathf.Abs(side) > 0.01f)
            {
                visualSign = side >= 0f ? 1f : -1f;
            }
        }

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * visualSign;
        visual.localScale = scale;

        float bob = isFlying
            ? Mathf.Sin(Time.time * flapBobFrequency) * flapBobHeight
            : 0f;

        visual.localPosition = baseVisualLocalPosition + Vector3.up * bob;
    }

    private bool ValidatePath()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("Add at least two waypoints.", this);
            return false;
        }

        foreach (Waypoint waypoint in waypoints)
        {
            if (waypoint.point == null)
            {
                Debug.LogError("A waypoint has no point assigned.", this);
                return false;
            }

            if (waypoint.point.IsChildOf(transform))
            {
                Debug.LogError(
                    waypoint.point.name + " is a child of the bird, so it moves with " +
                    "the bird. Move it out of the bird object.", this);
                return false;
            }
        }

        return true;
    }

    // Draws the route in the Scene view while the bird is selected.
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null)
        {
            return;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoint current = waypoints[i];

            if (current.point == null)
            {
                continue;
            }

            Gizmos.color = current.isPerch ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(current.point.position, 0.3f);

            Waypoint next = waypoints[(i + 1) % waypoints.Length];

            if (next.point != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(current.point.position, next.point.position);
            }
        }
    }
}