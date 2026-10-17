using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Plays zoom and focus sounds by watching BirdWatchingCamera values.
/// Add this to the book's Gameplay Controls array so it stops while the book is open.
/// </summary>
public class BinocularAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BirdWatchingCamera binocular;

    [Header("Zoom Sound")]
    [Tooltip("Played once each time the zoom starts changing. Loop should be off.")]
    [SerializeField] private AudioSource zoomSource;

    [Header("Focus Sound")]
    [Tooltip("Plays while focus is being adjusted. Loop should be on.")]
    [SerializeField] private AudioSource focusSource;
    [SerializeField] private float focusVolume = 1f;

    [Tooltip("Volume change per second when fading in and out.")]
    [SerializeField] private float focusFadeSpeed = 8f;

    [Header("Detection")]
    [Tooltip("Minimum relative change per second to count as 'changing'.")]
    [SerializeField] private float changeThreshold = 0.05f;

    private float lastMagnification;
    private float lastFocusDistance;
    private bool hasBaseline;

    private void OnDisable()
    {
        // Runs when the book disables this script, so sounds stop cleanly.
        hasBaseline = false;

        if (focusSource != null)
        {
            focusSource.Stop();
            focusSource.volume = 0f;
        }

        if (zoomSource != null)
        {
            zoomSource.Stop();
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (binocular == null || dt <= 0f)
        {
            return;
        }

        float magnification = Mathf.Max(binocular.Magnification, 0.0001f);
        float focusDistance = Mathf.Max(binocular.FocusDistance, 0.0001f);

        // Skip the first frame so initial values are not read as a change.
        if (!hasBaseline)
        {
            lastMagnification = magnification;
            lastFocusDistance = focusDistance;
            hasBaseline = true;
            return;
        }

        float zoomRate = Mathf.Abs(Mathf.Log(magnification / lastMagnification)) / dt;
        float focusRate = Mathf.Abs(Mathf.Log(focusDistance / lastFocusDistance)) / dt;

        lastMagnification = magnification;
        lastFocusDistance = focusDistance;

        UpdateZoomSound(zoomRate);
        UpdateFocusSound(focusRate, dt);
    }

    private void UpdateZoomSound(float zoomRate)
    {
        if (zoomSource == null)
        {
            return;
        }

        if (zoomRate > changeThreshold && !zoomSource.isPlaying)
        {
            zoomSource.Play();
        }
    }

    private void UpdateFocusSound(float focusRate, float dt)
    {
        if (focusSource == null)
        {
            return;
        }

        // Focus counts only when A or D is held and the distance really moves.
        bool isFocusing = IsFocusKeyHeld() && focusRate > changeThreshold;

        if (isFocusing && !focusSource.isPlaying)
        {
            focusSource.Play();
        }

        float targetVolume = isFocusing ? focusVolume : 0f;

        focusSource.volume = Mathf.MoveTowards(
            focusSource.volume,
            targetVolume,
            focusFadeSpeed * dt);

        if (!isFocusing && focusSource.isPlaying && focusSource.volume <= 0.001f)
        {
            focusSource.Stop();
        }
    }

    private bool IsFocusKeyHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.aKey.isPressed || keyboard.dKey.isPressed);
#else
        return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
#endif
    }
}