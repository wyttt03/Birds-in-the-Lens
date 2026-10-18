using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a looping 3D bird call from this object and fades it out
/// once the matching PhotoTarget has been photographed.
/// Attach to the same object as the PhotoTarget and an AudioSource.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BirdCallAudio : MonoBehaviour
{
    [SerializeField] private PhotoTarget target;
    [SerializeField] private AudioSource source;

    [Tooltip("Starts the long clip at a random point so birds do not call in sync.")]
    [SerializeField] private bool randomizeStartTime = true;

    [Tooltip("Seconds the call takes to fade out after the bird is photographed.")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        source = GetComponent<AudioSource>();
        target = GetComponent<PhotoTarget>();
    }

    private void Awake()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        if (target == null)
        {
            target = GetComponentInParent<PhotoTarget>();
        }
    }

    private void OnEnable()
    {
        PhotoCapture.TargetPhotographed += HandleTargetPhotographed;
    }

    private void OnDisable()
    {
        PhotoCapture.TargetPhotographed -= HandleTargetPhotographed;
    }

    private void Start()
    {
        if (source == null || source.clip == null)
        {
            return;
        }

        // Stay silent if this bird was already photographed.
        if (target != null && PhotoCapture.IsSpeciesCaptured(target.SpeciesId))
        {
            return;
        }

        source.loop = true;

        if (randomizeStartTime)
        {
            source.time = Random.Range(0f, Mathf.Max(0f, source.clip.length - 0.1f));
        }

        source.Play();
    }

    private void HandleTargetPhotographed(PhotoTarget photographed)
    {
        if (target == null ||
            photographed.SpeciesId != target.SpeciesId ||
            fadeRoutine != null)
        {
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVolume = source.volume;
        float duration = Mathf.Max(fadeOutDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
        fadeRoutine = null;
    }
}