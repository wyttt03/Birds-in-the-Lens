using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Tracks how many targets were photographed this level, updates the
/// "(captured/total)" counter, and raises an event when all are done.
/// </summary>
public class LevelProgress : MonoBehaviour
{
    [SerializeField] private Text counterText;

    [Tooltip("-1 counts every PhotoTarget in the scene at start. " +
             "Set a number if targets are spawned later.")]
    [SerializeField] private int totalOverride = -1;

    public UnityEvent onLevelComplete;

    private int total;
    private int captured;
    private bool completed;

    public int Captured => captured;
    public int Total => total;

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
        if (totalOverride >= 0)
        {
            total = totalOverride;
        }
        else
        {
            // Count distinct species, not individual objects.
            HashSet<string> species = new HashSet<string>();

            foreach (PhotoTarget target in PhotoTarget.All)
            {
                species.Add(target.SpeciesId);
            }

            total = species.Count;
        }

        RefreshCounter();
    }

    private void HandleTargetPhotographed(PhotoTarget target)
    {
        captured++;
        RefreshCounter();

        if (!completed && total > 0 && captured >= total)
        {
            completed = true;
            onLevelComplete?.Invoke();
        }
    }

    private void RefreshCounter()
    {
        if (counterText != null)
        {
            counterText.text = "(" + captured + "/" + total + ")";
        }
    }
}