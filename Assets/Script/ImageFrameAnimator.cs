using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a few sprites in a loop on a UI Image, without an Animator.
/// Put it on the icon. Remove any Animator from the same object.
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageFrameAnimator : MonoBehaviour
{
    [Tooltip("The frames, in order.")]
    [SerializeField] private Sprite[] frames;

    [SerializeField] private float framesPerSecond = 8f;

    [Tooltip("Starts at a random frame so several icons do not move in sync.")]
    [SerializeField] private bool randomizeStart = false;

    private Image image;
    private float time;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    // The icon is switched on and off, so every time it appears the loop restarts.
    private void OnEnable()
    {
        bool hasFrames = frames != null && frames.Length > 0;

        time = randomizeStart && hasFrames
            ? Random.value * frames.Length / Mathf.Max(framesPerSecond, 0.01f)
            : 0f;

        Apply();
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2)
        {
            return;
        }

        time += Time.unscaledDeltaTime;
        Apply();
    }

    private void Apply()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        int index = Mathf.FloorToInt(time * framesPerSecond) % frames.Length;
        image.sprite = frames[index];
    }
}